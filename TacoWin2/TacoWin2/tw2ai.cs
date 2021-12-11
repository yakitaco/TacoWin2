using System;
using System.Threading.Tasks;

namespace TacoWin2 {





    class tw2ai {


        static int[] kVal = {
        0,        //なし
        100,   //歩兵
        500,  //香車
        600,    //桂馬
        800,  //銀将
        1500,    //飛車
        1200, //角行
        900,  //金将
        99999,   //王将
        200,    //と金(成歩兵)
        550, //成香
        650,  //成桂
        900,  //成銀
        1800,   //竜王
        1400,   //竜馬
    };

    Random rnds = new System.Random();

        // thread同時数
        static int workMin;
        static int ioMin;
        public bool stopFlg = false;

        static tw2ai() {
            // thread同時数取得
            //ThreadPool.GetMinThreads(out workMin, out ioMin);
        }

        // ランダムに動く(王手は逃げる)
        public (kmove, int) RandomeMove(Pturn turn, tw2ban ban) {
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
            kmove[] bestmove = null;
            int teCnt = 0; //手の進捗
            Object lockObj = new Object();

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

                }

                Parallel.For(0, workMin, id => {
                    int cnt_local;

                    while (true) {

                        lock (lockObj) {
                            cnt_local = teCnt;
                            teCnt++;
                        }

                        (kmove[] retmove, int retval) = think(pturn.aturn(turn), ban, mList.ls[0][cnt_local], 1, 1);

                        if (retval > best) {
                            bestmove = retmove;
                        }


                    }
                });

            }

            return (bestmove, best);
        }

        public (kmove[], int) think(Pturn turn, tw2ban ban, kmove pmove, int depth, int depMax) {
            int val = 0;



            int ln = 0;
            int best = -1000;

            unsafe {

                if (ban.onBoard[pmove.np] > 0) {
                    val = kVal[(int)ban.getOnBoardKtype(pmove.np / 9, pmove.np % 9)];
                }

                ban.moveKoma(pmove.op / 9, pmove.op % 9, pmove.np / 9, pmove.np % 9, pmove.turn, pmove.nari, false, true);


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

            return (mList.ls[0], best + val);
        }

    }
}
