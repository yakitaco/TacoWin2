using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TacoWin2_BanInfo;
using TacoWin2_sfenIO;
using TacoWin2_SMV;

namespace TacoWin2
{
    class tw2Main
    {
        // 棋譜
        private static List<kmove> kifu;

        // --- エンジンの状態管理 ---
        private static int tesuu = 0;
        private static Pturn turn = Pturn.Sente;
        private static tw2ai ai = new tw2ai();
        private static tw2aiChild aic = new tw2aiChild();
        private static ban ban = new ban();

        private static int inGame = 0;
        private static int nokori = 0;
        private static kmove[] mateMove = null;
        private static int mateMovePos = 0;
        private static int mateMoveNum = 0;

        private static Task<(List<diagTbl>, int)> aiTaskMain = null;
        private static Stopwatch sw = new Stopwatch();
        private static Process thisProcess = Process.GetCurrentProcess();
        private static Random rnds = new Random();

        // AI探索タイムアウト・中断用の非同期処理キャンセル管理クラス
        private static CancellationTokenSource searchCts;
        // AI詰み探索タイムアウト・中断用の非同期処理キャンセル管理クラス
        private static CancellationTokenSource mateCts;

        [STAThread]
        static void Main(string[] args)
        {
            InitializeApplication();
            InitializeEngineFiles();

            // USIメインループ
            while (true)
            {
                string str = Console.ReadLine();
                if (string.IsNullOrEmpty(str))
                {
                    Thread.Sleep(10000);
                    continue;
                }

                DebugForm.instance.addMsg("[RECV]" + str);

                string[] tokens = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command = tokens[0].ToLower();

                try
                {
                    switch (command)
                    {
                        case "usi":
                            HandleUsi(str);
                            break;
                        case "isready":
                            HandleIsReady(str);
                            break;
                        case "usinewgame":
                            HandleUsiNewGame(str);
                            break;
                        case "position":
                            HandlePosition(str, tokens);
                            break;
                        case "go":
                            HandleGo(str, tokens);
                            break;
                        case "ponderhit":
                            HandlePonderHit();
                            break;
                        case "stop":
                            HandleStop();
                            break;
                        case "gameover":
                            HandleGameOver();
                            break;
                        case "test":
                            HandleTestCommands(tokens);
                            break;
                        case "quit":
                            HandleQuit();
                            return; // プログラム終了
                        default:
                            break; // 未知のコマンドは無視
                    }
                } catch (Exception ex)
                {
                    DebugForm.instance.addMsg($"[ERROR] {ex.Message}");
                }
            }
        }

        #region 初期化処理

        private static void InitializeApplication()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Task.Run(() =>
            {
                Application.Run(new DebugForm()); // デバッグフォーム
                Console.WriteLine("bestmove resign");
            });
            Thread.Sleep(1000);

            Console.SetIn(new StreamReader(Console.OpenStandardInput(8192)));
            if (File.Exists("stdin.txt"))
            {
                DebugForm.instance.addMsg("[DEBUG]System.IO.StreamReader stdin.txt");
                Console.SetIn(new StreamReader("stdin.txt"));
            }
        }

        private static void InitializeEngineFiles()
        {
            LoadFile("default.ytj", sMove.load);
            LoadFile("mList", tw2stval.loadFile);
            LoadFile("childapp.bat", aic.load);
        }

        private static void LoadFile(string path, Func<string, int> loadMethod)
        {
            int ret = loadMethod(path);
            if (ret < 0)
            {
                DebugForm.instance.addMsg($"[NG]Load {path}");
            } else
            {
                DebugForm.instance.addMsg($"[OK]Load {path}({ret})");
            }
        }

        #endregion

        #region コマンドハンドラ

        private static void HandleUsi(string rawStr)
        {
            aic.input(rawStr);
            Console.WriteLine("id name たこウインナー 2.4.6");
            Console.WriteLine("id author YAKITACO");
            Console.WriteLine("option name BookFile type string default default.ytj");
            Console.WriteLine("option name UseBook type check default true");
            Console.WriteLine("usiok");
        }

        private static void HandleIsReady(string rawStr)
        {
            aic.input(rawStr);
            if (inGame > 0)
            { // 連続対戦用
                if (inGame == 1) tw2_log.save(DebugForm.instance.getText(), (int)turn);
                DebugForm.instance.resetMsg();
            }
            ai.resetHash();
            Thread.Sleep(1000);
            Console.WriteLine("readyok");
            inGame = 1;
        }

        private static void HandleUsiNewGame(string rawStr)
        {
            aic.input(rawStr);
            tw2stval.reset();
            mateMove = null;
        }

        private static void HandlePosition(string rawStr, string[] tokens)
        {
            aic.input(rawStr);
            kifu = new List<kmove>();
            ban = new ban();
            turn = Pturn.Sente;

            int movesStartIndex = 0;

            if (tokens[1] == "startpos")
            {
                movesStartIndex = 3;
                ban.startpos();
            } else if (tokens[1] == "sfen")
            {
                movesStartIndex = 7;
                sfenIO.sfen2ban(ref ban, tokens[2], tokens[4]);
                turn = (tokens[3] == "b") ? Pturn.Sente : Pturn.Gote;
            }

            // 手を更新(差分のみ)
            for (tesuu = 0; tesuu + movesStartIndex < tokens.Length; tesuu++)
            {
                byte oPos, nPos;
                bool nari;
                tw2usiIO.usi2pos(tokens[tesuu + movesStartIndex], out oPos, out nPos, out nari);

                ban.moveKoma(oPos, nPos, turn, nari, false);

                kmove tmp = new kmove();
                tmp.set(oPos, nPos, 0, 0, nari, turn);
                kifu.Add(tmp);
                turn = (Pturn)pturn.aturn((int)turn);
            }
            ban.renewMoveable();

            DebugForm.instance.addMsg(ban.debugShow());
            DebugForm.instance.addMsg(ban.banShow());
        }

        private static void HandleGo(string rawStr, string[] tokens)
        {
            aic.input(rawStr);

            if (tokens.Length > 1 && tokens[1] == "btime")
            {
                nokori = Convert.ToInt32(turn == Pturn.Sente ? tokens[2] : tokens[4]);
                DebugForm.instance.addMsg($"NOKORI = {nokori} / TESUU = {tesuu}");

                thisProcess.PriorityClass = ProcessPriorityClass.RealTime; // 優先度高
                sw.Restart();

                // 通常探索用Ctsをリセット
                searchCts?.Dispose();
                searchCts = new CancellationTokenSource();

                // 詰み探索用Ctsをリセット
                mateCts?.Dispose();
                mateCts = new CancellationTokenSource();

                aiTaskMain = Task.Run(() => ThinkTask(nokori, tesuu, searchCts.Token), searchCts.Token);

                if (nokori > 3600000)
                {
                    Thread.Sleep(2000 + rnds.Next(0, nokori / 2000));
                }

                // 思考結果の受け取りと送信
                Task.Run(() => ProcessAndSendBestMove());

            } else if (tokens.Length > 1 && tokens[1] == "ponder")
            {
                nokori = Convert.ToInt32(turn == Pturn.Sente ? tokens[3] : tokens[5]);
                DebugForm.instance.addMsg($"nokori = {nokori}");

                if (mateMove != null)
                {
                    DebugForm.instance.addMsg($"Think Ponder. <<mate>>{mateMove.Length}");
                } else
                {
                    // 詰みが見えてない場合のみ先読み実施 (時間は固定の60を使用)
                    aiTaskMain = Task.Run(() => ThinkPonderTask(nokori, tesuu));
                }

            } else if (tokens.Length > 1 && tokens[1] == "mate")
            {
                thisProcess.PriorityClass = ProcessPriorityClass.RealTime;
                (kmove[] km, int best) = ai.thinkMateMove(turn, ban, 15, searchCts.Token, mateCts.Token);
                thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal;

                if (best < 999)
                {
                    string pstr = "";
                    for (int i = 0; i < km.Length && (km[i].op > 0 || km[i].np > 0); i++)
                    {
                        pstr += " " + tw2usiIO.pos2usi(km[i].op, km[i].np, km[i].nari);
                    }
                    Console.WriteLine("checkmate" + pstr);
                    DebugForm.instance.addMsg("checkmate" + pstr);
                } else
                {
                    Console.WriteLine("checkmate nomate");
                    DebugForm.instance.addMsg("checkmate nomate");
                }
            } else if (tokens.Length > 1 && tokens[1] == "matetest")
            {
                ai.thinkMateMoveTest(turn, ban, 8);
            }
        }

        private static void HandlePonderHit()
        {
            if (mateMove != null)
            {
                // 詰みが見える場合
                string pstr = "";
                for (int i = mateMovePos + 1; i < mateMove.Length && mateMove[i].op > 0 && mateMove[i].np > 0; i++)
                {
                    pstr += " " + tw2usiIO.pos2usi(mateMove[i].op, mateMove[i].np, mateMove[i].nari);
                }
                Console.WriteLine($"info score mate {(mateMoveNum - mateMovePos)} pv {pstr}");
                Console.WriteLine($"bestmove {tw2usiIO.pos2usi(mateMove[mateMovePos].op, mateMove[mateMovePos].np, mateMove[mateMovePos].nari)} ponder {tw2usiIO.pos2usi(mateMove[mateMovePos + 1].op, mateMove[mateMovePos + 1].np, mateMove[mateMovePos + 1].nari)}");
                mateMovePos += 2;
            } else
            {
                AdjustTimerForPonderHit(nokori, tesuu);

                if (nokori > 3600000)
                {
                    Thread.Sleep(2000 + rnds.Next(0, nokori / 2000));
                }

                Task.Run(() => ProcessAndSendBestMove());
            }
        }

        private static void HandleStop()
        {
            mateMove = null;

            // キャンセルを発行(AI通常探索用)
            if (searchCts != null && !searchCts.IsCancellationRequested)
            {
                searchCts.Cancel();
            }

            // キャンセルを発行(AI詰み探索用)
            if (mateCts != null && !mateCts.IsCancellationRequested)
            {
                mateCts.Cancel();
            }

            // 思考の終了を待機
            if (aiTaskMain != null)
            {
                (List<diagTbl> retMove, _) = aiTaskMain.Result;
                if (retMove != null && retMove.Count > 0)
                {
                    kmove[] km = retMove[0].kmv;
                    string sendStr = BuildBestMoveString(km);
                    Console.WriteLine(sendStr);
                    DebugForm.instance.addMsg("[SEND]" + sendStr);
                } else
                {
                    Console.WriteLine("bestmove 4a3b");
                }
            }

            ResetEngineState();
        }

        private static void HandleGameOver()
        {
            if (aiTaskMain != null)
            {             // キャンセルを発行(AI通常探索用)
                if (searchCts != null && !searchCts.IsCancellationRequested)
                {
                    searchCts.Cancel();
                }

                // キャンセルを発行(AI詰み探索用)
                if (mateCts != null && !mateCts.IsCancellationRequested)
                {
                    mateCts.Cancel();
                }
            }
            if (aiTaskMain != null)
            {
                _ = aiTaskMain.Result; // 終了待機
            }

            if (inGame == 1) tw2_log.save(DebugForm.instance.getText(), (int)turn);
            inGame = 2;
        }

        private static void HandleQuit()
        {
            if (aiTaskMain != null)
            {             // キャンセルを発行(AI通常探索用)
                if (searchCts != null && !searchCts.IsCancellationRequested)
                {
                    searchCts.Cancel();
                }

                // キャンセルを発行(AI詰み探索用)
                if (mateCts != null && !mateCts.IsCancellationRequested)
                {
                    mateCts.Cancel();
                }
            }
            if (aiTaskMain != null)
            {
                _ = aiTaskMain.Result;
            }

            if (inGame == 1) tw2_log.save(DebugForm.instance.getText(), (int)turn);
        }

        private static void HandleTestCommands(string[] tokens)
        {
            if (tokens.Length > 1)
            {
                string csvFile = tokens[1];
                string filterType = null;
                string filterValue = null;
                int iterations = 1;

                // 引数が4つある場合 (例: test testdata.csv group Tsumi  または  test testdata.csv avg 5)
                if (tokens.Length >= 4)
                {
                    string option = tokens[2].ToLower();

                    if (option == "avg")
                    {
                        if (!int.TryParse(tokens[3], out iterations) || iterations < 1)
                        {
                            DebugForm.instance.addMsg("[ERROR] Invalid iteration count. Usage: test <csv> avg <count>");
                            return;
                        }
                    } else
                    {
                        // "group" または "name" として扱う
                        filterType = option;
                        filterValue = tokens[3];
                    }
                } else if (tokens.Length == 3)
                {
                    DebugForm.instance.addMsg("[ERROR] test command is missing arguments. Usage: test <csv> [group|name|avg] <value>");
                    return;
                }

                DebugForm.instance.addMsg($"[INFO] Starting test with CSV: {csvFile}");

                // メインスレッドで同期的に実行
                AITestRunner.RunTest(csvFile, ai, filterType, filterValue, iterations);
            } else
            {
                DebugForm.instance.addMsg("[ERROR] test command requires a CSV file name. Usage: test <csv> [group|name|avg] <value>");
            }
        }

        #endregion

        #region AI思考処理と結果送信の共通化

        // Goコマンド用のタイマーと深さの設定
        private static (List<diagTbl>, int) ThinkTask(int timeRest, int currentTurn, CancellationToken token)
        {
            if (timeRest < 30000)
            { // 0 - 30 sec
                // 通常探索(残り時間 - 2秒) : 詰み探索5秒
                searchCts.CancelAfter(((timeRest / 1000) - 2 < 10) ? ((timeRest / 1000) - 2) * 1000 : 10000);
                mateCts.CancelAfter(5000);
                return ai.thinkMove(turn, ban, kifu, 3, 0, 0, 5, 5, searchCts.Token, mateCts.Token);
            } else if (timeRest < 60000)
            { // 30 sec - 1min
                // 通常探索10秒 : 詰み探索5秒
                searchCts.CancelAfter(10000);
                mateCts.CancelAfter(5000);
                return ai.thinkMove(turn, ban, kifu, 4, 1, 5, 7, 5, searchCts.Token, mateCts.Token);
            } else if (currentTurn < 30 || timeRest < 180000)
            { // 1 - 3min
                // 通常探索15秒 : 詰み探索10秒
                searchCts.CancelAfter(15000);
                mateCts.CancelAfter(10000);
                return ai.thinkMove(turn, ban, kifu, 5, 1, 5, 7, 5, searchCts.Token, mateCts.Token);
            } else if (currentTurn < 40 || timeRest < 300000)
            { // 3 - 5min
                // 通常探索15秒 : 詰み探索10秒
                searchCts.CancelAfter(15000);
                mateCts.CancelAfter(10000);
                return ai.thinkMove(turn, ban, kifu, 5, 1, 6, 9, 5, searchCts.Token, mateCts.Token);
            } else if (currentTurn < 50 || timeRest < 450000)
            { // 5 - 7.5min
                // 通常探索15秒 : 詰み探索10秒
                searchCts.CancelAfter(15000);
                mateCts.CancelAfter(10000);
                return ai.thinkMove(turn, ban, kifu, 5, 1, 6, 11, 5, searchCts.Token, mateCts.Token);
            } else if (currentTurn < 50 || timeRest < 900000)
            { // 7.5 - 15min
                tw2stval.setStage(1);
                // 通常探索180秒 : 詰み探索50秒
                searchCts.CancelAfter(180000);
                mateCts.CancelAfter(50000);
                return ai.thinkMove(turn, ban, kifu, 6, 1, 8, 11, 5, searchCts.Token, mateCts.Token);
            } else if (currentTurn < 50 || timeRest < 3600000)
            { // 15 - 60min
                tw2stval.setStage(1);
                // 通常探索300秒 : 詰み探索50秒
                searchCts.CancelAfter(300000);
                mateCts.CancelAfter(50000);
                return ai.thinkMove(turn, ban, kifu, timeRest < 1800000 ? 6 : 7, 1, timeRest < 1800000 ? 16 : 0, 11, 5, searchCts.Token, mateCts.Token);
            } else if (currentTurn < 50 || timeRest < 7200000)
            { // 60 - 120min
                tw2stval.setStage(1);
                // 通常探索300秒 : 詰み探索50秒
                searchCts.CancelAfter(300000);
                mateCts.CancelAfter(50000);
                return ai.thinkMove(turn, ban, kifu, 7, 1, 8, 11, 5, searchCts.Token, mateCts.Token);
            } else
            { // 120min -
                tw2stval.setStage(1);
                // 通常探索600秒 : 詰み探索120秒
                searchCts.CancelAfter(600000);
                mateCts.CancelAfter(120000);
                return ai.thinkMove(turn, ban, kifu, 7, 1, 10, 11, 5, searchCts.Token, mateCts.Token);
            }



        }

        // Ponder用のタスク
        private static (List<diagTbl>, int) ThinkPonderTask(int timeRest, int currentTurn)
        {
            int depth = 4;
            if (timeRest >= 60000) depth = 5;
            if (currentTurn >= 40 && timeRest >= 300000) depth = 5; // 元のロジックに準拠
            if (currentTurn >= 50 && timeRest >= 450000) depth = 6;
            if (currentTurn >= 50 && timeRest >= 3600000) depth = 7;

            int wid = 5;
            if (timeRest >= 60000) wid = 7;
            if (currentTurn >= 40 && timeRest >= 300000) wid = 9;
            if (currentTurn >= 50 && timeRest >= 450000) wid = 11;

            return ai.thinkMove(turn, ban, kifu, depth, 1, 60, wid, 5, searchCts.Token, mateCts.Token);
        }

        private static void AdjustTimerForPonderHit(int timeRest, int currentTurn)
        {
            // 先読みからの切り替え時に、残り時間と現在の手数に応じてタイマーと深さを調整
            if (timeRest < 120000)
            {
                // 通常探索(残り時間 - 2秒) : 詰み探索5秒
                searchCts.CancelAfter(((timeRest / 1000) - 2 < 10) ? ((timeRest / 1000) - 2) * 1000 : 10000);
                mateCts.CancelAfter(5000);
                ai.deepWidth = 0;

            } else if (currentTurn < 30 || timeRest < 180000)
            {
                // 通常探索15秒 : 詰み探索10秒
                searchCts.CancelAfter(15000);
                mateCts.CancelAfter(10000);
                ai.deepWidth = 0;
            } else if (currentTurn < 40 || timeRest < 300000)
            {
                // 通常探索20秒 : 詰み探索10秒
                searchCts.CancelAfter(20000);
                mateCts.CancelAfter(10000);
                ai.deepWidth = 10;
            } else if (currentTurn < 50 || timeRest < 450000)
            {
                // 通常探索20秒 : 詰み探索10秒
                searchCts.CancelAfter(20000);
                mateCts.CancelAfter(10000);
                ai.deepWidth = 12;
            } else if (currentTurn < 50 || timeRest < 900000)
            {
                // 通常探索180秒 : 詰み探索50秒
                searchCts.CancelAfter(180000);
                mateCts.CancelAfter(50000);
                ai.deepWidth = 8;
            } else if (currentTurn < 50 || timeRest < 1800000)
            {
                // 通常探索600秒 : 詰み探索120秒
                searchCts.CancelAfter(600000);
                mateCts.CancelAfter(120000);
                ai.deepWidth = 16;
            } else if (currentTurn < 50 || timeRest < 3600000)
            {
                // 通常探索1200秒 : 詰み探索120秒
                searchCts.CancelAfter(1200000);
                mateCts.CancelAfter(120000);
                ai.deepWidth = 0;
            } else if (currentTurn < 50 || timeRest < 7200000)
            {
                // 通常探索1800秒 : 詰み探索120秒
                searchCts.CancelAfter(1800000);
                mateCts.CancelAfter(120000);
                ai.deepWidth = 8;
            } else
            {
                // 通常探索7200秒 : 詰み探索120秒
                searchCts.CancelAfter(7200000);
                mateCts.CancelAfter(120000);
                tw2stval.setStage(1);
                ai.deepWidth = 10;
            }
        }

        // タスクの結果を受け取り、コンソールに出力する共通ロジック
        private static void ProcessAndSendBestMove()
        {
            if (aiTaskMain == null) return;

            (List<diagTbl> retMove, int best) = aiTaskMain.Result;
            sw.Stop();
            thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal; // 優先度戻す

            // タイムアウトやキャンセルが発生していない場合のみ、結果を送信
            if (!searchCts.Token.IsCancellationRequested)
            {
                if (best < -10000)
                {
                    Console.WriteLine("bestmove resign");
                    DebugForm.instance.addMsg("[SEND]bestmove resign");
                } else
                {
                    kmove[] km = GenerateInfoScore(retMove, best);
                    string sendStr = BuildBestMoveString(km);

                    Console.WriteLine(sendStr);
                    DebugForm.instance.addMsg("[SEND]" + sendStr);

                    if (best > 5000)
                    {
                        mateMove = km;
                        mateMovePos = 2;
                    }
                }

                TimeSpan ts = sw.Elapsed;
                DebugForm.instance.addMsg($"　{ts}");
            }

            ResetEngineState();
        }

        private static kmove[] GenerateInfoScore(List<diagTbl> retMove, int best)
        {
            kmove[] km;
            string pstr = "";

            if (best > 5000)
            {
                for (mateMoveNum = 0; mateMoveNum < retMove[0].kmv.Length && retMove[0].kmv[mateMoveNum].op > 0 && retMove[0].kmv[mateMoveNum].np > 0; mateMoveNum++)
                {
                    pstr += " " + tw2usiIO.pos2usi(retMove[0].kmv[mateMoveNum].op, retMove[0].kmv[mateMoveNum].np, retMove[0].kmv[mateMoveNum].nari);
                }
                km = retMove[0].kmv;
                Console.WriteLine($"info score mate {mateMoveNum} pv {pstr}");
            } else
            {
                km = compBestMove(retMove, aic);
                for (mateMoveNum = 0; mateMoveNum < km.Length && km[mateMoveNum].op > 0 && km[mateMoveNum].np > 0; mateMoveNum++)
                {
                    pstr += " " + tw2usiIO.pos2usi(km[mateMoveNum].op, km[mateMoveNum].np, km[mateMoveNum].nari);
                }
                Console.WriteLine($"info score cp {ai.chkScore(ref ban, turn)} pv {pstr}");
            }
            return km;
        }

        private static string BuildBestMoveString(kmove[] km)
        {
            if (km == null || km.Length == 0) return "bestmove resign";

            if (km.Length > 1 && (km[1].op > 0 || km[1].np > 0))
            {
                return $"bestmove {tw2usiIO.pos2usi(km[0].op, km[0].np, km[0].nari)} ponder {tw2usiIO.pos2usi(km[1].op, km[1].np, km[1].nari)}";
            } else
            {
                return $"bestmove {tw2usiIO.pos2usi(km[0].op, km[0].np, km[0].nari)}";
            }
        }

        private static void ResetEngineState()
        {
            mList.reset();
            ai.resetHash();
            aic.clear();
        }

        #endregion

        private static kmove[] compBestMove(List<diagTbl> dlist, tw2aiChild aic)
        {
            kmove[] ret = null;
            int best = -99999;

            if (dlist.Count == 1 || aic.mList.Count < 1) return dlist[0].kmv;

            foreach (var dCnt in dlist)
            {
                int i;
                for (i = 0; i < aic.mList.Count; i++)
                {
                    if (dCnt.kmv[0].op == aic.mList[i].op &&
                        dCnt.kmv[0].np == aic.mList[i].np &&
                        dCnt.kmv[0].nari == aic.mList[i].nari)
                    {
                        DebugForm.instance.addMsg($"[cval]{(aic.mList[i].op + 0x11):X2}-{(aic.mList[i].np + 0x11):X2}:{aic.mList[i].val}");
                        break;
                    }
                }
                if (i == aic.mList.Count) continue;

                if (aic.mList[i].val > best)
                {
                    best = aic.mList[i].val;
                    ret = dCnt.kmv;
                }
            }

            return ret ?? dlist[0].kmv;
        }
    }
}