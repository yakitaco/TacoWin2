using System;
using System.Threading;
using System.Threading.Tasks;

namespace TacoWin2 {





    class tw2ai {


        public static int[] kVal = {
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
            ThreadPool.GetMinThreads(out workMin, out ioMin);
            Console.Write("workMin={0},ioMin={1}\n", workMin, ioMin);
        }

        // ランダムに動く(王手は逃げる)
        public (kmove, int) RandomeMove(Pturn turn, tw2ban ban) {
            int ln = 0;
            int best = -1000;

            unsafe {
                (int vla, int sp) = ban.ForEachAll(turn, mList.ls[0]);
                for (int i = 0; i < vla; i++) {
                    int _rnd = rnds.Next(0, 100);
                    tw2ban tmps = ban;
                    if ((tmps.moveable[pturn.aturn((int)turn) * 81 + mList.ls[0][sp+i].np] >= tmps.moveable[(int)turn * 81 + mList.ls[0][sp + i].np])) {
                        _rnd -= 50;
                    }
                    if (tmps.onBoard[mList.ls[0][sp + i].np] > 0) {
                        _rnd += 100;
                    }
                    tmps.moveKoma(mList.ls[0][sp + i].op / 9, mList.ls[0][i].op % 9, mList.ls[0][sp + i].np / 9, mList.ls[0][sp + i].np % 9, mList.ls[0][sp + i].turn, mList.ls[0][sp + i].nari, false, true);
                    if (tmps.moveable[pturn.aturn((int)turn) * 81 + tmps.putOusyou[(int)mList.ls[0][sp + i].turn]] > 0) _rnd -= 900;
                    if (_rnd > best) {
                        best = _rnd;
                        ln = sp + i;
                    }
                }
            }

            return (mList.ls[0][ln], best);
        }

        public (kmove[], int) thinkMove(Pturn turn, tw2ban ban, int depth) {
            int ln = 0;
            int best = -999999;
            int beta =  999999;
            int alpha = -999999;

            kmove[] bestmove = null;
            int teCnt = 0; //手の進捗
            Object lockObj = new Object();

            unsafe {
                (int vla, int sp) = ban.ForEachAll(turn, mList.ls[0]);

                Parallel.For(0, workMin, id => {
                    int cnt_local;

                    while (true) {

                        lock (lockObj) {
                            if (vla <= teCnt) break;
                            cnt_local = teCnt;
                            teCnt++;
                        }
                        mList.ls[cnt_local + 1][0] = mList.ls[0][sp + cnt_local];
                        int retval = -think(pturn.aturn(turn), ban, ref mList.ls[cnt_local + 1], -beta, -alpha, 0, 1, depth);

                        Console.Write("TASK[{0}:{1}]MV[{2}]({3},{4})->({5},{6})/({7},{8})->({9},{10})\n", Task.CurrentId, sp + cnt_local, retval,
                            mList.ls[cnt_local + 1][0].op / 9 + 1, mList.ls[cnt_local + 1][0].op % 9 + 1, mList.ls[cnt_local + 1][0].np / 9 + 1, mList.ls[cnt_local + 1][0].np % 9 + 1,
                            mList.ls[cnt_local + 1][1].op / 9 + 1, mList.ls[cnt_local + 1][1].op % 9 + 1, mList.ls[cnt_local + 1][1].np / 9 + 1, mList.ls[cnt_local + 1][1].np % 9 + 1);

                        lock (lockObj) {
                            if (retval > best) {
                                best = retval;
                                bestmove = mList.ls[cnt_local + 1];
                                if (best > alpha) {
                                    alpha = best;
                                }
                            }
                        }


                    }
                });

            }

            return (bestmove, best);
        }

        public int think(Pturn turn, tw2ban ban, ref kmove[] mList, int alpha, int beta, int pVal, int depth, int depMax) {
            int val = -pVal;

            int ln = 0;
            int best = -999999;

            unsafe {

                /* ひとつ前の駒移動 */
                if (ban.onBoard[mList[0].np] > 0) {
                    val += -kVal[(int)ban.getOnBoardKtype(mList[0].np / 9, mList[0].np % 9)];
                    //Console.Write("kVal[{0}]{1}", val, (int)ban.getOnBoardKtype(mList[0].np / 9, mList[0].np % 9));
                    if (val < -5000) return val;
                }
                ban.moveKoma(mList[0].op / 9, mList[0].op % 9, mList[0].np / 9, mList[0].np % 9, mList[0].turn, mList[0].nari, false, true);

                // 持ち駒がある
                // どこかに打つ
                kmove[] tmpList = new kmove[500];
                (int vla, int sp) = ban.ForEachAll(turn, tmpList);
                int _val = 0;
                for (int i = 0; i < vla; i++) {

                    if (depth < depMax) {
                        kmove[] tmpList_ = new kmove[500];
                        tmpList_[0] = tmpList[sp+i];
                        _val = -think(pturn.aturn(turn), ban, ref tmpList_, -beta, -alpha, val, depth + 1, depMax);
                        if (_val > best) {
                            best = _val;
                            mList[depth] = tmpList[sp + i];
                            if (best > alpha) {
                                alpha = best;
                                //mList[depth] = tmpList[i];
                            }
                            if (best >= beta) {
                                return best;
                            }
                        }
                    } else {

                        best = val - tmpList[sp].val;
                        mList[depth] = tmpList[sp];
                        //tw2ban tmps = ban;
                        //if (ban.onBoard[tmpList[i].np] > 0) {
                        //    //if (depth % 2 == 1) {
                        //        _val = kVal[(int)ban.getOnBoardKtype(tmpList[i].np / 9, tmpList[i].np % 9)] - pVal;
                        //    //} else {
                        //    //    _val = val - kVal[(int)ban.getOnBoardKtype(tmpList[i].np / 9, tmpList[i].np % 9)];
                        //    //}
                        //    //Console.Write("kVal({0},{1})={2}\n", tmpList[i].np / 9, tmpList[i].np % 9, _val);
                        //} else {
                        //    //if (depth % 2 == 1) {
                        //        _val = -pVal;
                        //    //} else {
                        //    //    _val = val;
                        //    //}
                        //}
                        //tmps.moveKoma(tmpList[i].op / 9, tmpList[i].op % 9, tmpList[i].np / 9, tmpList[i].np % 9, tmpList[i].turn, tmpList[i].nari, false, true);
                        ////if (tmps.moveable[pturn.aturn((int)turn) * 81 + tmps.putOusyou[(int)tmpList[i].turn]] > 0) _val -= 99999;
                        //if (_val > best) {
                        //    best = _val;
                        //    mList[depth] = tmpList[i];
                        //}

                    }

                }
            }

            return best;
        }

    }
}
