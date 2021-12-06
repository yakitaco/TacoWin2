using System;

namespace TacoWin2 {
    class tw2ai {
        Random rnds = new System.Random();
        tw2ban tmps;
        public (int, int, int, int, bool) RandomeMove(Pturn turn, tw2ban ban) {
            int ox = 0;
            int oy = 0;
            int nx = 0;
            int ny = 0;
            int rnd = -1000;
            bool nari = false;
            tmps = ban;

            // 持ち駒がある
            // どこかに打つ
            ban.ForEachAll(turn, (int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari) => {
                Console.Write("({0},{1})->({2},{3})\n", _ox + 1, _oy + 1, _nx + 1, _ny + 1);
                int _rnd = rnds.Next(0, 100);
                unsafe {
                    tw2ban tmps2 = tmps;
                    if (tmps.onBoard[_nx + _ny * 9] > 0) {
                        _rnd += 100;
                    }
                    tmps2.moveKoma(_ox, _oy, _nx, _ny, _turn, _nari, false);
                    if (tmps2.moveable[pturn.aturn((int)turn) * 81 + tmps2.putOusyou[(int)_turn]] > 0) _rnd -= 900;
                }
                if (_rnd > rnd) {
                    rnd = _rnd;
                    ox = _ox; oy = _oy; nx = _nx; ny = _ny; nari = _nari;
                }
            });

            if (rnd < -500) {
                Console.WriteLine("bestmove resign");
            } else {
                Console.WriteLine("bestmove " + tw2usiIO.pos2usi(ox, oy, nx, ny, nari));
            }

            return (0, 0, 0, 0, false);
        }




    }
}
