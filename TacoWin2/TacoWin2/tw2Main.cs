using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TacoWin2_BanInfo;
using TacoWin2_sfenIO;
using TacoWin2_SMV;

namespace TacoWin2 {
    class tw2Main {
        static void Main(string[] args) {
            int tesuu = 0;
            Pturn turn = Pturn.Sente;
            tw2ai ai = new tw2ai();
            int inGame = 0;

            kmove[] mateMove = null;
            int mateMovePos = 0;
            int mateMoveNum = 0;

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Task.Run(() => {
                Application.Run(new DebugForm()); // デバッグフォーム
                Console.WriteLine("bestmove resign");
            });
            Thread.Sleep(1000);

            Random rnds = new System.Random();

            Task<(kmove[], int)> aiTaskMain = null;

            var sw = new System.Diagnostics.Stopwatch();  // 時間計測用

            Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();

            ban ban = new ban();
            Console.SetIn(new StreamReader(Console.OpenStandardInput(8192)));


            if (System.IO.File.Exists("stdin.txt")) {
                DebugForm.instance.addMsg("[DEBUG]System.IO.StreamReader stdin.txt");
                var exStdIn = new System.IO.StreamReader("stdin.txt");
                System.Console.SetIn(exStdIn);
            }

            // 定跡ファイル
            string fileName = "default.ytj";
            int ret = sMove.load(fileName);
            if (ret < 0) {
                DebugForm.instance.addMsg("[NG]Load " + fileName);
            } else {
                DebugForm.instance.addMsg("[OK]Load " + fileName + "(" + ret + ")");
            }

            string dirName = "mList";
            ret = tw2stval.loadFile(dirName);
            if (ret < 0) {
                DebugForm.instance.addMsg("[NG]Load " + dirName);
            } else {
                DebugForm.instance.addMsg("[OK]Load " + dirName + "(" + ret + ")");
            }

            int nokori = 0;

            while (true) {
                string str = Console.ReadLine();
                DebugForm.instance.addMsg("[RECV]" + str);

                if (str == null) {
                    Thread.Sleep(10000);
                    // usi 起動
                } else if ((str.Length == 3) && (str.Substring(0, 3) == "usi")) {
                    Console.WriteLine("id name たこウインナー 2.2.0");
                    Console.WriteLine("id authoer YAKITACO");
                    Console.WriteLine("option name BookFile type string default default.ytj");
                    Console.WriteLine("option name UseBook type check default true");
                    Console.WriteLine("usiok");

                    // isready 対局開始前
                } else if ((str.Length == 7) && (str.Substring(0, 7) == "isready")) {
                    if (inGame > 0) { /* 連続対戦用 */
                        if (inGame == 1) tw2_log.save(DebugForm.instance.getText(), (int)turn);
                        DebugForm.instance.resetMsg();
                    }
                    Thread.Sleep(1000);
                    Console.WriteLine("readyok");
                    inGame = 1;

                    //usinewgame 新規
                } else if ((str.Length == 10) && (str.Substring(0, 10) == "usinewgame")) {
                    tw2stval.reset();
                    mateMove = null;

                    // position 盤面情報
                } else if ((str.Length > 7) && (str.Substring(0, 8) == "position")) {
                    string[] arr = str.Split(' ');
                    int startStrPos = 0;
                    turn = Pturn.Sente;
                    ban = new ban();

                    // 平手
                    if (arr[1] == "startpos") {
                        startStrPos = 3;
                        ban.startpos();
                        // 駒落ち・指定局面
                    } else if (arr[1] == "sfen") {
                        startStrPos = 7;
                        sfenIO.sfen2ban(ref ban, arr[2], arr[4]);

                        if (arr[3] == "b") {
                            turn = Pturn.Sente;
                        } else {
                            turn = Pturn.Gote;
                        }
                        //Console.WriteLine(ban.debugShow());

                        //string oki = "";
                        //string mochi = "";
                        //sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                        //Console.WriteLine(oki + " / " + mochi);

                    }

                    // 手を更新(差分のみ)
                    for (tesuu = 0; tesuu + startStrPos < arr.Length; tesuu++) {
                        byte oPos;
                        byte nPos;
                        bool nari;
                        tw2usiIO.usi2pos(arr[tesuu + startStrPos], out oPos, out nPos, out nari);

                        //Console.Write("MV({0},{1})->({2},{3})\n", ox + 1, oy + 1, nx + 1, ny + 1);
                        ban.moveKoma(oPos, nPos, turn, nari, false);
                        turn = (Pturn)pturn.aturn((int)turn);

                    }
                    ban.renewMoveable();

                    DebugForm.instance.addMsg(ban.debugShow());

                } else if ((str.Length > 1) && (str.Substring(0, 2) == "go")) {
                    string[] arr = str.Split(' ');

                    //通常読み
                    if (arr[1] == "btime") {

                        nokori = Convert.ToInt32(turn == Pturn.Sente ? arr[2] : arr[4]);
                        DebugForm.instance.addMsg("NOKORI = " + nokori + " / TESUU = " + tesuu);

                        thisProcess.PriorityClass = ProcessPriorityClass.RealTime; //優先度高
                        sw.Restart();
                        aiTaskMain = Task.Run(() => {
                            if (nokori < 120000) {
                                return ai.thinkMove(turn, ban, 2, 0);
                            } else if ((tesuu < 50) || (nokori < 180000)) {
                                return ai.thinkMove(turn, ban, 6, 0);
                            } else if ((tesuu < 50) || (nokori < 300000)) {
                                return ai.thinkMove(turn, ban, 6, 7);
                            } else if ((tesuu < 50) || (nokori < 600000)) {
                                return ai.thinkMove(turn, ban, 6, 11);
                            } else {
                                return ai.thinkMove(turn, ban, 7, 11);
                            }
                        });

                        if (nokori > 3600000) {
                            Thread.Sleep(2000 + rnds.Next(0, nokori / 2000));
                        }
                        Task.Run(() => {
                            (kmove[] km, int best) = aiTaskMain.Result;
                            sw.Stop();
                            thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal; //優先度普通
                            string sendStr;

                            if (ai.stopFlg == false) {

                                if (best < -10000) {
                                    sendStr = "bestmove resign";
                                } else {
                                    if (best > 5000) {
                                        string pstr = "";
                                        for (mateMoveNum = 0; mateMoveNum < km.Length && km[mateMoveNum].op > 0 && km[mateMoveNum].np > 0; mateMoveNum++) {
                                            pstr += " " + tw2usiIO.pos2usi(km[mateMoveNum].op, km[mateMoveNum].np, km[mateMoveNum].nari);
                                        }
                                        Console.WriteLine("info score mate " + mateMoveNum + " pv " + pstr);  //標準出力
                                    } else {
                                        string pstr = "";
                                        for (mateMoveNum = 0; mateMoveNum < km.Length && km[mateMoveNum].op > 0 && km[mateMoveNum].np > 0; mateMoveNum++) {
                                            pstr += " " + tw2usiIO.pos2usi(km[mateMoveNum].op, km[mateMoveNum].np, km[mateMoveNum].nari);
                                        }
                                        Console.WriteLine("info score " + best + " pv " + pstr);  //標準出力
                                    }

                                    if ((km[1].op > 0) || (km[1].np > 0)) {
                                        sendStr = "bestmove " + tw2usiIO.pos2usi(km[0].op, km[0].np, km[0].nari) + " ponder " + tw2usiIO.pos2usi(km[1].op, km[1].np, km[1].nari);
                                    } else {
                                        sendStr = "bestmove " + tw2usiIO.pos2usi(km[0].op, km[0].np, km[0].nari);
                                    }
                                }
                                Console.WriteLine(sendStr);
                                DebugForm.instance.addMsg("[SEND]" + sendStr);

                                TimeSpan ts = sw.Elapsed;
                                DebugForm.instance.addMsg($"　{ts}");

                                if (best > 5000) {
                                    mateMove = km;
                                    mateMovePos = 2;
                                }

                            }

                            //★テスト
                            //if (turn == Pturn.Sente) {
                            //    sMove.set(oki + " " + mochi, "+" + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari), 1, 1, 0);
                            //} else {
                            //    sMove.set(oki + " " + mochi, "-" + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari), 1, 1, 0);
                            //}
                            //sMove.save("s2.txt");

                            // 最後にメモリ初期化
                            mList.reset();
                            ai.stopFlg = false;

                        });


                        // 先読み
                    } else if (arr[1] == "ponder") {

                        nokori = Convert.ToInt32(turn == Pturn.Sente ? arr[3] : arr[5]);
                        DebugForm.instance.addMsg("nokori = " + nokori);

                        // 詰みが見える場合は何もしない
                        if (mateMove != null) {
                            DebugForm.instance.addMsg("Think Ponder. <<mate>>" + mateMove.Length);

                        } else {
                            // 詰みが見えてない場合のみ先読み実施
                            aiTaskMain = Task.Run(() => {
                                if (nokori < 120000) {
                                    return ai.thinkMove(turn, ban, 4, 0);
                                } else if ((tesuu < 50) || (nokori < 180000)) {
                                    return ai.thinkMove(turn, ban, 5, 0);
                                } else if ((tesuu < 50) || (nokori < 300000)) {
                                    return ai.thinkMove(turn, ban, 6, 7);
                                } else if ((tesuu < 50) || (nokori < 600000)) {
                                    return ai.thinkMove(turn, ban, 6, 11);
                                } else {
                                    return ai.thinkMove(turn, ban, 7, 11);
                                }
                            });
                        }

                    } else if (arr[1] == "mate") {
                        //(kmove[] km, int best) = ai.thinkMateMoveTest(turn, ban, 8);

                        thisProcess.PriorityClass = ProcessPriorityClass.RealTime; //優先度高
                        (kmove[] km, int best) = ai.thinkMateMove(turn, ban, 15);
                        thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal; //優先度普通

                        if (best < 999) {
                            string pstr = "";
                            for (int i = 0; i < km.Length && (km[i].op > 0 || km[i].np > 0); i++) {
                                pstr += " " + tw2usiIO.pos2usi(km[i].op, km[i].np, km[i].nari);
                            }
                            Console.WriteLine("checkmate" + pstr);
                            DebugForm.instance.addMsg("checkmate" + pstr);
                        } else {
                            Console.WriteLine("checkmate nomate");
                            DebugForm.instance.addMsg("checkmate nomate");
                        }

                    } else if (arr[1] == "matetest") {

                        (kmove[] km, int best) = ai.thinkMateMoveTest(turn, ban, 8);

                    }

                } else if ((str.Length > 7) && (str.Substring(0, 8) == "testmove")) {

                    string[] arr = str.Split(' ');

                    // 手を更新(差分のみ)
                    for (tesuu = 0; tesuu + 1 < arr.Length; tesuu++) {
                        byte oPos;
                        byte nPos;
                        bool nari;
                        tw2usiIO.usi2pos(arr[tesuu + 1], out oPos, out nPos, out nari);

                        ban.moveKoma(oPos, nPos, turn, nari, true);
                        turn = (Pturn)pturn.aturn((int)turn);

                    }
                    //ban.renewMoveable();

                    DebugForm.instance.addMsg(ban.debugShow());

                } else if ((str.Length > 8) && (str.Substring(0, 9) == "ponderhit")) {

                    // 詰みが見える場合
                    if (mateMove != null) {
                        string pstr = "";
                        for (int i = mateMovePos + 1; i < mateMove.Length && mateMove[i].op > 0 && mateMove[i].np > 0; i++) {
                            pstr += " " + tw2usiIO.pos2usi(mateMove[i].op, mateMove[i].np, mateMove[i].nari);
                        }
                        Console.WriteLine("info score mate " + (mateMoveNum - mateMovePos) + " pv " + pstr);  //標準出力
                        Console.WriteLine("bestmove " + tw2usiIO.pos2usi(mateMove[mateMovePos].op, mateMove[mateMovePos].np, mateMove[mateMovePos].nari) + " ponder " + tw2usiIO.pos2usi(mateMove[mateMovePos + 1].op, mateMove[mateMovePos + 1].np, mateMove[mateMovePos + 1].nari));
                        //+ usiIO.pos2usi(mateMove[mateMovePos].ko, mateMove[0]) + " ponder " + usiIO.pos2usi(mateMove[mateMovePos+1].ko, mateMove[1]));
                        mateMovePos += 2;
                    } else {

                        if (nokori > 3600000) {
                            Thread.Sleep(2000 + rnds.Next(0, nokori / 2000));
                        }
                        Task.Run(() => {
                            (kmove[] km, int best) = aiTaskMain.Result;
                            thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal; //優先度普通
                            if (ai.stopFlg == false) {
                                if (best < -10000) {
                                    Console.WriteLine("bestmove resign");
                                } else {
                                    if ((km[1].op > 0) || (km[1].np > 0)) {
                                        Console.WriteLine("bestmove " + tw2usiIO.pos2usi(km[0].op, km[0].np, km[0].nari) + " ponder " + tw2usiIO.pos2usi(km[1].op, km[1].np, km[1].nari));
                                    } else {
                                        Console.WriteLine("bestmove " + tw2usiIO.pos2usi(km[0].op, km[0].np, km[0].nari));
                                    }
                                }

                                if (best > 5000) {
                                    mateMove = km;
                                    mateMovePos = 2;
                                }

                            }
                            // 最後にメモリ初期化
                            mList.reset();
                            ai.stopFlg = false;
                        });
                    }
                } else if ((str.Length > 4) && (str.Substring(0, 4) == "test")) {
                    // テスト用
                    string[] arr = str.Split(' ');

                    if (arr[1] == "bestmove") {
                        kmove[] mLst = new kmove[500];
                        (int vla, int sp) = ai.getBestMove(ref ban, turn, mLst);
                        DebugForm.instance.addMsg(vla + " " + sp + " " + mLst[sp].aval);
                        for (int i = sp; i < vla + sp; i++) {
                            DebugForm.instance.addMsg("aList" + i + ":" + (mLst[i].op + 0x11).ToString("X2") + "->" + (mLst[i].np + 0x11).ToString("X2") + "/" + mLst[i].val + "/" + mLst[i].aval);
                        }
                    } else if (arr[1] == "move") {
                        kmove[] km;
                        int best;
                        sw.Restart();
                        if (arr.Length > 3) {

                            (km, best) = ai.thinkMove(turn, ban, Convert.ToInt32(arr[2]), Convert.ToInt32(arr[3]));
                        } else {
                            (km, best) = ai.thinkMove(turn, ban, Convert.ToInt32(arr[2]), 0);
                        }
                        sw.Stop();
                        if ((km[1].op > 0) || (km[1].np > 0)) {
                            Console.WriteLine("bestmove " + tw2usiIO.pos2usi(km[0].op, km[0].np, km[0].nari) + " ponder " + tw2usiIO.pos2usi(km[1].op, km[1].np, km[1].nari));
                        } else {
                            Console.WriteLine("bestmove " + tw2usiIO.pos2usi(km[0].op, km[0].np, km[0].nari));
                        }

                        TimeSpan ts = sw.Elapsed;
                        DebugForm.instance.addMsg($"TIME : {ts}");
                    } else if (arr[1] == "matedef") {
                        kmove[] km = new kmove[100];
                        int rets = ai.getAllDefList(ref ban, turn, km, (byte)Convert.ToInt32(arr[2]));
                        for (int i = 0; i < rets; i++) {
                            if (km[i].nari == true) {
                                DebugForm.instance.addMsg("MV: " + (km[i].op + 0x11).ToString("X2") + "-" + (km[i].np + 0x11).ToString("X2") + "*");
                            } else {
                                DebugForm.instance.addMsg("MV: " + (km[i].op + 0x11).ToString("X2") + "-" + (km[i].np + 0x11).ToString("X2"));
                            }
                        }
                    }
                } else if ((str.Length == 4) && (str.Substring(0, 4) == "stop")) {
                    mateMove = null;
                    if (aiTaskMain != null) ai.stopFlg = true;

                    (kmove[] km, int best) = aiTaskMain.Result;

                    Console.WriteLine("bestmove 4a3b");  //標準出力

                    ai.stopFlg = false;

                    // 最後にメモリ初期化
                    mList.reset();

                } else if ((str.Length > 8) && (str.Substring(0, 8) == "gameover")) {
                    if (aiTaskMain != null) ai.stopFlg = true;
                    (kmove[] km, int best) = aiTaskMain.Result;
                    ai.stopFlg = false;
                    if (inGame == 1) tw2_log.save(DebugForm.instance.getText(), (int)turn);
                    inGame = 2;

                } else if ((str.Length == 4) && (str.Substring(0, 4) == "quit")) {
                    if (aiTaskMain != null) ai.stopFlg = true;
                    (kmove[] km, int best) = aiTaskMain.Result;
                    ai.stopFlg = false;
                    if (inGame == 1) tw2_log.save(DebugForm.instance.getText(), (int)turn);

                } else {

                }

            }


        }
    }
}
