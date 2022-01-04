using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using TacoWin2_BanInfo;
using TacoWin2_sfenIO;
using TacoWin2_SMV;

namespace TacoWin2 {
    class tw2Main {
        static void Main(string[] args) {
            int tesuu = 0;
            Pturn turn = Pturn.Sente;
            tw2ai ai = new tw2ai();

            var sw = new System.Diagnostics.Stopwatch();  // 時間計測用

            Process thisProcess = System.Diagnostics.Process.GetCurrentProcess();

            ban ban = new ban();
            //ban.startpos();
            //Console.WriteLine(ban.debugShow());
            //ban.ForEachAll(Pturn.Sente, (int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari) => {
            //    Console.Write("S({0},{1})->({2},{3})\n", _ox + 1, _oy + 1, _nx + 1, _ny + 1);
            //});
            //ban.ForEachAll(Pturn.Gote, (int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari) => {
            //    Console.Write("G({0},{1})->({2},{3})\n", _ox + 1, _oy + 1, _nx + 1, _ny + 1);
            //});
            Console.SetIn(new StreamReader(Console.OpenStandardInput(8192)));

            //smvCtl.load("s.txt");

            string test = smvCtl.addList("ASBA","",0,0,0);
            Console.WriteLine(test);
            //smvCtl.getList("ASBA");

            while (true) {
                string str = Console.ReadLine();

                // usi 起動
                if ((str.Length == 3) && (str.Substring(0, 3) == "usi")) {
                    Console.WriteLine("id name たこウインナー 2.1.0");
                    Console.WriteLine("id authoer YAKITACO");
                    Console.WriteLine("option name BookFile type string default default.ytj");
                    Console.WriteLine("option name UseBook type check default true");
                    Console.WriteLine("usiok");

                    // isready 対局開始前
                } else if ((str.Length == 7) && (str.Substring(0, 7) == "isready")) {
                    Thread.Sleep(1000);
                    Console.WriteLine("readyok");

                    //usinewgame 新規
                } else if ((str.Length == 10) && (str.Substring(0, 10) == "usinewgame")) {
                    tw2stval.reset();

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
                        Console.WriteLine(ban.debugShow());

                        string oki = "";
                        string mochi = "";
                        sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                        Console.WriteLine(oki + " / " + mochi);

                    }

                    // 手を更新(差分のみ)
                    for (tesuu = 0; tesuu + startStrPos < arr.Length; tesuu++) {
                        int ox;
                        int oy;
                        int nx;
                        int ny;
                        bool nari;
                        tw2usiIO.usi2pos(arr[tesuu + startStrPos], out ox, out oy, out nx, out ny, out nari);

                        Console.Write("MV({0},{1})->({2},{3})\n", ox + 1, oy + 1, nx + 1, ny + 1);
                        ban.moveKoma(ox, oy, nx, ny, turn, nari, false, false);

                        turn = (Pturn)pturn.aturn((int)turn);

                    }
                    ban.renewMoveable();

                    Console.WriteLine(ban.debugShow());

                } else if ((str.Length > 1) && (str.Substring(0, 2) == "go")) {
                    string[] arr = str.Split(' ');

                    //通常読み
                    if (arr[1] == "btime") {

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
                        (kmove[] km, int best) = ai.thinkMove(turn, ban, 6);
                        sw.Stop();
                        thisProcess.PriorityClass = ProcessPriorityClass.AboveNormal; //優先度普通
                        if (best < -10000) {
                            Console.WriteLine("bestmove resign");
                        } else {
                            Console.WriteLine("bestmove " + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari));
                        }
                        TimeSpan ts = sw.Elapsed;
                        Console.WriteLine($"　{ts}");

                        //★テスト
                        //if (turn == Pturn.Sente) {
                        //    sMove.set(oki + " " + mochi, "+" + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari), 1, 1, 0);
                        //} else {
                        //    sMove.set(oki + " " + mochi, "-" + tw2usiIO.pos2usi(km[0].op / 9, km[0].op % 9, km[0].np / 9, km[0].np % 9, km[0].nari), 1, 1, 0);
                        //}
                        //sMove.save("s2.txt");

                        // 最後にメモリ初期化
                        mList.reset();

                        // 先読み
                    } else if (arr[1] == "ponder") {






                        // 最後にメモリ初期化
                        mList.reset();
                    }

                } else {

                }


                //Form1.Form1Instance.addMsg("[RECV]" + str);
            }


        }
    }
}
