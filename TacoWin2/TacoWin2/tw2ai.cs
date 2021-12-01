using System;

namespace TacoWin2 {
    class tw2ai {
        Random rnds = new System.Random();

        public (int, int, int, int, bool) RandomeMove(Pturn turn, tw2ban ban) {
            int ox = 0;
            int oy = 0;
            int nx = 0;
            int ny = 0;
            int rnd = 0;
            bool nari = false;

            // 持ち駒がある
            // どこかに打つ
            ban.ForEachAll(turn, (int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari) => {
                Console.Write("({0},{1})->({2},{3})\n", _ox + 1, _oy + 1, _nx + 1, _ny + 1);
                int _rnd = rnds.Next(0, 100);
                if (_rnd > rnd) {
                    rnd = _rnd;
                    ox = _ox; oy = _oy; nx = _nx; ny = _ny; nari = _nari;
                }
            });
            Console.WriteLine("bestmove " + tw2usiIO.pos2usi(ox, oy, nx, ny, nari));

            return (0, 0, 0, 0, false);
        }




    }
}
