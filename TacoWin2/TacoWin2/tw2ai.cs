using System;

namespace TacoWin2 {
    class tw2ai {
        Random rnds = new System.Random();

        public (int, int, int, int, bool) RandomeMove(Pturn turn, tw2ban ban) {
            int ox = 0;
            int oy = 0;
            int nx = 0;
            int ny = 0;
            int best = -1000;
            bool nari = false;

            unsafe {
                // 持ち駒がある
                // どこかに打つ
                int vla = ban.ForEachAll(turn, mList.ls[0]);
                for (int i = 0; i < vla; i++) {
                    int _rnd = rnds.Next(0, 100);
                    tw2ban tmps = ban;
                    if (tmps.onBoard[mList.ls[0][i].np] > 0) {
                        _rnd += 100;
                    }
                    tmps.moveKoma(mList.ls[0][i].op / 9, mList.ls[0][i].op % 9, mList.ls[0][i].np / 9, mList.ls[0][i].np % 9, mList.ls[0][i].turn, mList.ls[0][i].nari, false);
                    if (tmps.moveable[pturn.aturn((int)turn) * 81 + tmps.putOusyou[(int)mList.ls[0][i].turn]] > 0) _rnd -= 900;
                    if (_rnd > best) {
                        best = _rnd;
                        ox = mList.ls[0][i].op / 9;
                        oy = mList.ls[0][i].op % 9;
                        nx = mList.ls[0][i].np / 9;
                        ny = mList.ls[0][i].np % 9;
                        nari = mList.ls[0][i].nari;
                    }
                }

                if (best < -500) {
                    Console.WriteLine("bestmove resign");
                } else {
                    Console.WriteLine("bestmove " + tw2usiIO.pos2usi(ox, oy, nx, ny, nari));
                }
            }

            return (0, 0, 0, 0, false);
        }

        public (int, int, int, int, bool) ThinkMove(Pturn turn, tw2ban ban) {
            int ox = 0;
            int oy = 0;
            int nx = 0;
            int ny = 0;
            int best = -1000;
            bool nari = false;

            //ban.ForEachAll(turn, (int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari) => {
            //    Console.Write("({0},{1})->({2},{3})\n", _ox + 1, _oy + 1, _nx + 1, _ny + 1);
            //    int _rnd = rnds.Next(0, 100);
            //    unsafe {
            //        tw2ban tmps2 = tmps;
            //        if (tmps.onBoard[_nx + _ny * 9] > 0) {
            //            _rnd += 100;
            //        }
            //        tmps2.moveKoma(_ox, _oy, _nx, _ny, _turn, _nari, false);
            //        if (tmps2.moveable[pturn.aturn((int)turn) * 81 + tmps2.putOusyou[(int)_turn]] > 0) _rnd -= 900;
            //    }
            //    if (_rnd > best) {
            //        best = _rnd;
            //        ox = _ox; oy = _oy; nx = _nx; ny = _ny; nari = _nari;
            //    }
            //});
            //ban.ForEachAll(turn, test);

            if (best < -500) {
                Console.WriteLine("bestmove resign");
            } else {
                Console.WriteLine("bestmove " + tw2usiIO.pos2usi(ox, oy, nx, ny, nari));
            }

            return (0, 0, 0, 0, false);
        }

        public int test(int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari, tw2ban ban) {
            Console.Write("({0},{1})->({2},{3})\n", _ox + 1, _oy + 1, _nx + 1, _ny + 1);




            return 0;
        }

        public (int, int, int, int, bool, int) ThinkMove(Pturn turn, tw2ban ban, int depth, int depMax) {
            int ox = 0;
            int oy = 0;
            int nx = 0;
            int ny = 0;
            bool nari = false;
            int best = -1000;







            return (ox, oy, nx, ny, nari, best);
        }

    }
}
