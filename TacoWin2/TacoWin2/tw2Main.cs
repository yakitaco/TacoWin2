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


                // usi 起動
                if ((str.Length == 3) && (str.Substring(0, 3) == "usi")) {
                    Console.WriteLine("id name たこウインナー 2.1.0");
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
                        int ox;
                        int oy;
                        int nx;
                        int ny;
                        bool nari;
                        tw2usiIO.usi2pos(arr[tesuu + startStrPos], out ox, out oy, out nx, out ny, out nari);

                        //Console.Write("MV({0},{1})->({2},{3})\n", ox + 1, oy + 1, nx + 1, ny + 1);
                        ban.moveKoma(ox, oy, nx, ny, turn, nari, false, false);

                        turn = (Pturn)pturn.aturn((int)turn);

                    }
                    ban.renewMoveable();

                    DebugForm.instance.addMsg(ban.debugShow());

                } else if ((str.Length > 1) && (str.Substring(0, 2) == "go")) {
                    string[] arr = str.Split(' ');

                    //通常読み
                    if (arr[1] == "btime") {

                        nokori = Convert.ToInt32(turn == Pturn.Sente ? arr[2] : arr[4]);
                        DebugForm.instance.addMsg("nokori = " + nokori);

                        // 定跡チェック
                        //string oki = "";
                        //string mochi = "";
                        //sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                        //string strs = sMove.get(oki + " " + mochi, turn);
                        //if (strs != null) {
                        //    Console.WriteLine("bestmove " + strs.Substring(1));
                        //    continue;
                        //}

                        thisProcess.PriorityClass = ProcessPriorityClass.RealTime; //優先度高
                        sw.Restart();
                        aiTaskMain = Task.Run(() => {
                            return ai.thinkMove(turn, ban, 6);
                        });

                        if (nokori > 3600000) {
                            Thread.Sleep(2000 + rnds.Next(0, nokori / 2000));
                        }
                        Task.Run(() => {
                            (kmove[] km, int best) = aiTaskMain.Result;
                            //(kmove[] km, int best) = ai.thinkMove(turn, ban, 6);
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
                                            pstr += " " + tw2usiIO.pos2usi(km[mateMoveNum].op / 9, km[mateMoveNum].op % 9, km[mateMoveNum].np / 9, km[mateMoveNum].np % 9, km[mateMoveNum].nari);
                                        }
                                        Console.WriteLine("info score mate " + mateMoveNum + " pv " + pstr);  //標準出力
                                    }

                                    if ((km[1].op > 0) || (km[1].np > 0)) {
                                        sendStr = "bestmove " + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari) + " ponder " + tw2usiIO.pos2usi(km[1].op / 9, km[1].op % 9, km[1].np / 9, km[1].np % 9, km[1].nari);
                                    } else {
                                        sendStr = "bestmove " + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari);
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
                                return ai.thinkMove(turn, ban, 6);
                            });
                        }

                    } else if (arr[1] == "mate") {

                        //position sfen 9/4k4/9/4G4/9/9/9/9/9 b 2G2r2bg4s4n4l18p 1
                        //go mate infinite

                        thisProcess.PriorityClass = ProcessPriorityClass.RealTime; //優先度高

                        (kmove[] km, int best) = ai.thinkMateMove(turn, ban, 8);

                        thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal; //優先度普通
                        if (best > 5000) {
                            string pstr = "";
                            for (int i = 0; i < km.Length && (km[i].op > 0 || km[i].np > 0); i++) {
                                pstr += " " + tw2usiIO.pos2usi(km[i].op / 9, km[i].op % 9, km[i].np / 9, km[i].np % 9, km[i].nari);
                            }
                            Console.WriteLine("checkmate" + pstr);
                            DebugForm.instance.addMsg("checkmate" + pstr);
                        } else {
                            Console.WriteLine("checkmate nomate");
                            DebugForm.instance.addMsg("checkmate nomate");
                        }
                    }

                } else if ((str.Length > 7) && (str.Substring(0, 8) == "testmove")) {



                    string[] arr = str.Split(' ');

                    // 手を更新(差分のみ)
                    for (tesuu = 0; tesuu + 1 < arr.Length; tesuu++) {
                        int ox;
                        int oy;
                        int nx;
                        int ny;
                        bool nari;
                        tw2usiIO.usi2pos(arr[tesuu + 1], out ox, out oy, out nx, out ny, out nari);

                        //Console.Write("MV({0},{1})->({2},{3})\n", ox + 1, oy + 1, nx + 1, ny + 1);
                        ban.moveKoma(ox, oy, nx, ny, turn, nari, false, true);

                        turn = (Pturn)pturn.aturn((int)turn);

                    }
                    //ban.renewMoveable();

                    DebugForm.instance.addMsg(ban.debugShow());

                } else if ((str.Length > 8) && (str.Substring(0, 9) == "ponderhit")) {

                    // 詰みが見える場合
                    if (mateMove != null) {
                        string pstr = "";
                        for (int i = mateMovePos + 1; i < mateMove.Length && mateMove[i].op > 0 && mateMove[i].np > 0; i++) {
                            pstr += " " + tw2usiIO.pos2usi(mateMove[i].op / 9, mateMove[i].op % 9, mateMove[i].np / 9, mateMove[i].np % 9, mateMove[i].nari);
                        }
                        Console.WriteLine("info score mate " + (mateMoveNum - mateMovePos) + " pv " + pstr);  //標準出力
                        Console.WriteLine("bestmove " + tw2usiIO.pos2usi(mateMove[mateMovePos].op / 9, mateMove[mateMovePos].op % 9, mateMove[mateMovePos].np / 9, mateMove[mateMovePos].np % 9, mateMove[mateMovePos].nari) + " ponder " + tw2usiIO.pos2usi(mateMove[mateMovePos + 1].op / 9, mateMove[mateMovePos + 1].op % 9, mateMove[mateMovePos + 1].np / 9, mateMove[mateMovePos + 1].np % 9, mateMove[mateMovePos + 1].nari));
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
                                        Console.WriteLine("bestmove " + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari) + " ponder " + tw2usiIO.pos2usi(km[1].op / 9, km[1].op % 9, km[1].np / 9, km[1].np % 9, km[1].nari));
                                    } else {
                                        Console.WriteLine("bestmove " + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari));
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
                } else if ((str.Length == 4) && (str.Substring(0, 4) == "stop")) {
                    mateMove = null;
                    ai.stopFlg = true;

                    (kmove[] km, int best) = aiTaskMain.Result;

                    Console.WriteLine("bestmove 4a3b");  //標準出力

                    ai.stopFlg = false;

                    // 最後にメモリ初期化
                    mList.reset();

                } else if ((str.Length > 8) && (str.Substring(0, 8) == "gameover")) {
                    ai.stopFlg = true;
                    if (inGame == 1) tw2_log.save(DebugForm.instance.getText(), (int)turn);
                    inGame = 2;

                } else if ((str.Length == 4) && (str.Substring(0, 4) == "quit")) {
                    ai.stopFlg = true;
                    if (inGame == 1) tw2_log.save(DebugForm.instance.getText(), (int)turn);

                } else {

                }

            }


        }
    }
}
