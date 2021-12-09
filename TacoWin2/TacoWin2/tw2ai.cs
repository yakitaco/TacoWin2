using System;
using System.Threading;
using System.Threading.Tasks;

namespace TacoWin2 {
    class tw2ai {
        Random rnds = new System.Random();

        // thread同時数
        static int workMin;
        static int ioMin;
        public bool stopFlg = false;

        static tw2ai() {
            // thread同時数取得
            ThreadPool.GetMinThreads(out workMin, out ioMin);
        }

        // ランダムに動く(王手は逃げる)
        public (kmove,int) RandomeMove(Pturn turn, tw2ban ban) {
            int ln = 0;
            int best = -1000;

            unsafe {
                int vla = ban.ForEachAll(turn, mList.ls[0]);
                for (int i = 0; i < vla; i++) {
                    int _rnd = rnds.Next(0, 100);
                    tw2ban tmps = ban;
                    if ((tmps.moveable[pturn.aturn((int)turn) * 81 + mList.ls[0][i].np] >= tmps.moveable[(int)turn * 81 + mList.ls[0][i].np])) {
                        _rnd -= 50;
                    }
                    if (tmps.onBoard[mList.ls[0][i].np] > 0) {
                        _rnd += 100;
                    }
                    tmps.moveKoma(mList.ls[0][i].op / 9, mList.ls[0][i].op % 9, mList.ls[0][i].np / 9, mList.ls[0][i].np % 9, mList.ls[0][i].turn, mList.ls[0][i].nari, false, true);
                    if (tmps.moveable[pturn.aturn((int)turn) * 81 + tmps.putOusyou[(int)mList.ls[0][i].turn]] > 0) _rnd -= 900;
                    if (_rnd > best) {
                        best = _rnd;
                        ln = i;
                    }
                }
            }

            return (mList.ls[0][ln], best);
        }

        public (kmove[], int) thinkMove(Pturn turn, tw2ban ban, int depth) {
            int ln = 0;
            int best = -1000;

            unsafe {
                int vla = ban.ForEachAll(turn, mList.ls[0]);
                for (int i = 0; i < vla; i++) {
                    int _rnd = rnds.Next(0, 100);
                    tw2ban tmps = ban;
                    if (tmps.onBoard[mList.ls[0][i].np] > 0) {
                        _rnd += 100;
                    }
                    tmps.moveKoma(mList.ls[0][i].op / 9, mList.ls[0][i].op % 9, mList.ls[0][i].np / 9, mList.ls[0][i].np % 9, mList.ls[0][i].turn, mList.ls[0][i].nari, false, true);
                    if (tmps.moveable[pturn.aturn((int)turn) * 81 + tmps.putOusyou[(int)mList.ls[0][i].turn]] > 0) _rnd -= 900;
                    if (_rnd > best) {
                        best = _rnd;
                        ln = i;
                    }
                }

                Parallel.For(0, workMin, id => {
                    while (true) {
                    }
                });

            }

            return (mList.ls[0], best);
        }

        public (kmove[], int) think(Pturn turn, tw2ban ban, int depth, int depMax) {
            int ln = 0;
            int best = -1000;

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
                    tmps.moveKoma(mList.ls[0][i].op / 9, mList.ls[0][i].op % 9, mList.ls[0][i].np / 9, mList.ls[0][i].np % 9, mList.ls[0][i].turn, mList.ls[0][i].nari, false, true);
                    if (tmps.moveable[pturn.aturn((int)turn) * 81 + tmps.putOusyou[(int)mList.ls[0][i].turn]] > 0) _rnd -= 900;
                    if (_rnd > best) {
                        best = _rnd;
                        ln = i;
                    }
                }
            }

            return (mList.ls[0], best);
        }

    }
}
