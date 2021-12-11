using System;
using System.Threading;

namespace TacoWin2 {
    class tw2Main {
        static void Main(string[] args) {
            int tesuu = 0;
            Pturn turn = Pturn.Sente;
            tw2ai ai = new tw2ai();

            tw2ban ban;
            //ban.startpos();
            //Console.WriteLine(ban.debugShow());
            //ban.ForEachAll(Pturn.Sente, (int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari) => {
            //    Console.Write("S({0},{1})->({2},{3})\n", _ox + 1, _oy + 1, _nx + 1, _ny + 1);
            //});
            //ban.ForEachAll(Pturn.Gote, (int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari) => {
            //    Console.Write("G({0},{1})->({2},{3})\n", _ox + 1, _oy + 1, _nx + 1, _ny + 1);
            //});

            while (true) {
                string str = Console.ReadLine();

                // usi 起動
                if ((str.Length == 3) && (str.Substring(0, 3) == "usi")) {
                    Console.WriteLine("id name たこウインナー 2.0.0");
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
                    // 何もしない

                    // position 盤面情報
                } else if ((str.Length > 7) && (str.Substring(0, 8) == "position")) {
                    string[] arr = str.Split(' ');
                    int startStrPos = 0;
                    turn = Pturn.Sente;
                    ban = new tw2ban();

                    // 平手
                    if (arr[1] == "startpos") {
                        startStrPos = 3;
                        ban.startpos();
                        // 駒落ち・指定局面
                    } else if (arr[1] == "sfen") {
                        startStrPos = 7;
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
                        (kmove km, int best) = ai.RandomeMove(turn, ban);

                        if (best < -500) {
                            Console.WriteLine("bestmove resign");
                        } else {
                            Console.WriteLine("bestmove " + tw2usiIO.pos2usi(km.op / 9, km.op % 9, km.np / 9, km.np % 9, km.nari));
                        }

                        // 先読み
                    } else if (arr[1] == "ponder") {


                    }

                    } else {

                }


                //Form1.Form1Instance.addMsg("[RECV]" + str);
            }


        }
    }
}
