using System;
using System.Threading;
using System.Threading.Tasks;
using TacoWin2_BanInfo;

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
        Object lockObj = new Object();

        static tw2ai() {
            // thread同時数取得
            ThreadPool.GetMinThreads(out workMin, out ioMin);
            Console.Write("workMin={0},ioMin={1}\n", workMin, ioMin);
        }

        // ランダムに動く(王手は逃げる)
        public (kmove, int) RandomeMove(Pturn turn, ban ban) {
            int ln = 0;
            int best = -1000;

            int aid = mList.assignAlist(out kmove[] moveList);

            unsafe {
                (int vla, int sp) = getAllMoveList(ref ban, turn, moveList);
                for (int i = 0; i < vla; i++) {
                    int _rnd = rnds.Next(0, 100);
                    ban tmps = ban;
                    if ((tmps.moveable[pturn.aturn((int)turn) * 81 + moveList[sp + i].np] >= tmps.moveable[(int)turn * 81 + moveList[sp + i].np])) {
                        _rnd -= 50;
                    }
                    if (tmps.onBoard[moveList[sp + i].np] > 0) {
                        _rnd += 100;
                    }
                    tmps.moveKoma(moveList[sp + i].op / 9, moveList[i].op % 9, moveList[sp + i].np / 9, moveList[sp + i].np % 9, moveList[sp + i].turn, moveList[sp + i].nari, false, true);
                    if (tmps.moveable[pturn.aturn((int)turn) * 81 + tmps.putOusyou[(int)moveList[sp + i].turn]] > 0) _rnd -= 900;
                    if (_rnd > best) {
                        best = _rnd;
                        ln = sp + i;
                    }
                }
            }

            mList.freeAlist(aid);
            return (moveList[ln], best);
        }

        public (kmove[], int) thinkMove(Pturn turn, ban ban, int depth) {
            int best = -999999;
            int beta = 999999;
            int alpha = -999999;

            kmove[] bestmove = null;

            int teCnt = 0; //手の進捗

            tw2stval.tmpChk(ban);

            unsafe {

                //kmove[] moveList = new kmove[500];
                int aid = mList.assignAlist(out kmove[] moveList);

                (int vla, int sp) = getAllMoveList(ref ban, turn, moveList);

                Parallel.For(0, workMin, id => {
                    int cnt_local;

                    while (true) {

                        lock (lockObj) {
                            if (vla <= teCnt) break;
                            cnt_local = teCnt + sp;
                            teCnt++;
                        }
                        //mList.ls[cnt_local + 1][0] = mList.ls[0][sp + cnt_local];

                        // 駒移動
                        ban tmp_ban = ban;
                        int val = 0;
                        int retVal;
                        kmove[] retList = null;

                        //駒を動かす
                        if ((tmp_ban.onBoard[moveList[cnt_local].np] > 0)) {
                            val += kVal[(int)tmp_ban.getOnBoardKtype(moveList[cnt_local].np)] + tw2stval.get(tmp_ban.getOnBoardKtype(moveList[cnt_local].op), moveList[cnt_local].np / 9, moveList[cnt_local].np % 9, moveList[cnt_local].op / 9, moveList[cnt_local].op % 9, (int)turn);
                        } else if (((moveList[cnt_local].op / 9) < 9)) {
                            val += tw2stval.get(tmp_ban.getOnBoardKtype(moveList[cnt_local].op), moveList[cnt_local].np / 9, moveList[cnt_local].np % 9, moveList[cnt_local].op / 9, moveList[cnt_local].op % 9, (int)turn);
                        }
                        tmp_ban.moveKoma(moveList[cnt_local].op / 9, moveList[cnt_local].op % 9, moveList[cnt_local].np / 9, moveList[cnt_local].np % 9, turn, moveList[cnt_local].nari, false, true);

                        // 王手はスキップ
                        if (tmp_ban.moveable[pturn.aturn((int)turn) * 81 + tmp_ban.putOusyou[(int)turn]] > 0) {
                            retVal = val - 99999;
                            Console.Write("TASK[{0}:{1}]MV[{2}]({3},{4})->({5},{6})\n", Task.CurrentId, cnt_local, retVal, moveList[cnt_local].op / 9 + 1, moveList[cnt_local].op % 9 + 1, moveList[cnt_local].np / 9 + 1, moveList[cnt_local].np % 9 + 1);
                        } else {
                            retVal = -think(pturn.aturn(turn), ref tmp_ban, out retList, -beta, -alpha, val, 1, depth);
                            retList[0] = moveList[cnt_local];

                            string str = "";
                            for (int i = 0; retList[i].op > 0 || retList[i].np > 0; i++) {
                                str += "(" + (retList[i].op / 9 + 1) + "," + (retList[i].op % 9 + 1) + ")->(" + (retList[i].np / 9 + 1) + "," + (retList[i].np % 9 + 1) + ")/";
                            }

                            Console.Write("TASK[{0}:{1}]MV[{2}]{3}\n", Task.CurrentId, cnt_local, retVal, str);
                        }

                        lock (lockObj) {
                            if (retVal > best) {
                                best = retVal;
                                bestmove = retList;
                                if (best > alpha) {
                                    alpha = best;
                                }
                            }
                        }


                    }
                });

                mList.freeAlist(aid);
            }

            return (bestmove, best);
        }

        public int think(Pturn turn, ref ban ban, out kmove[] bestMoveList, int alpha, int beta, int pVal, int depth, int depMax) {
            int val = -pVal;
            bestMoveList = null;
            int best = -999999;
            kmove[] retList;

            unsafe {

                // 持ち駒がある
                // どこかに打つ
                //kmove[] moveList = new kmove[500];
                kmove[] moveList;
                int aid;
                lock (lockObj) {
                    aid = mList.assignAlist(out moveList);
                }

                (int vla, int sp) = getAllMoveList(ref ban, turn, moveList);
                if (depth < depMax) {

                    for (int cnt = 0; cnt < vla; cnt++) {

                        //駒を動かす
                        ban tmp_ban = ban;
                        if ((tmp_ban.onBoard[moveList[sp + cnt].np] > 0)) {
                            val += kVal[(int)tmp_ban.getOnBoardKtype(moveList[sp + cnt].np)] + tw2stval.get(tmp_ban.getOnBoardKtype(moveList[sp + cnt].op), moveList[sp + cnt].np / 9, moveList[sp + cnt].np % 9, moveList[sp + cnt].op / 9, moveList[sp + cnt].op % 9, (int)turn);
                        } else if (((moveList[sp + cnt].op / 9) < 9)) {
                            val += tw2stval.get(tmp_ban.getOnBoardKtype(moveList[sp + cnt].op), moveList[sp + cnt].np / 9, moveList[sp + cnt].np % 9, moveList[sp + cnt].op / 9, moveList[sp + cnt].op % 9, (int)turn);
                        }
                        tmp_ban.moveKoma(moveList[sp + cnt].op / 9, moveList[sp + cnt].op % 9, moveList[sp + cnt].np / 9, moveList[sp + cnt].np % 9, turn, moveList[sp + cnt].nari, false, true);

                        if (tmp_ban.moveable[pturn.aturn((int)turn) * 81 + tmp_ban.putOusyou[(int)turn]] > 0) {
                            if (bestMoveList == null) {
                                bestMoveList = new kmove[500];
                                bestMoveList[depth] = moveList[sp + cnt];
                            }
                            continue;
                        }

                        int retVal = -think(pturn.aturn(turn), ref tmp_ban, out retList, -beta, -alpha, val, depth + 1, depMax);
                        if (retVal > best) {
                            best = retVal;
                            bestMoveList = retList;
                            bestMoveList[depth] = moveList[sp + cnt];
                            if (best > alpha) {
                                alpha = best;
                                //mList[depth] = tmpList[i];
                            }
                            if (best >= beta) {
                                lock (lockObj) {
                                    mList.freeAlist(aid);
                                }
                                return best;
                            }
                        }
                    }

                } else {

                    //best = val - moveList[sp + vla - 1].val;
                    best = val + moveList[sp].val;

                    bestMoveList = new kmove[10];
                    //bestMoveList = mList.assignRlist();

                    //bestMoveList[depth] = moveList[sp + vla - 1];
                    bestMoveList[depth] = moveList[sp];
                }
                lock (lockObj) {
                    mList.freeAlist(aid);
                }
            }

            return best;
        }


        public (int, int) getAllMoveList(ref ban ban, Pturn turn, kmove[] kmv) {
            int startPoint = 100;
            int kCnt = 0;
            unsafe {
                // 駒移動

                // 王将
                if (ban.putOusyou[(int)turn] != 0xFF) {
                    getEachMoveList(ref ban, ban.putOusyou[(int)turn], turn, kmv, ref kCnt, ref startPoint);
                }

                // 歩兵
                for (int i = 0; i < 9; i++) {
                    if (ban.putFuhyou[(int)turn * 9 + i] != 9) {
                        getEachMoveList(ref ban, i * 9 + ban.putFuhyou[(int)turn * 9 + i], turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if (ban.putKyousha[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKyousha[(int)turn * 4 + i], turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 桂馬
                for (int i = 0; i < 4; i++) {
                    if (ban.putKeima[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKeima[(int)turn * 4 + i], turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if (ban.putGinsyou[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putGinsyou[(int)turn * 4 + i], turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if (ban.putHisya[(int)turn * 2 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putHisya[(int)turn * 2 + i], turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if (ban.putKakugyou[(int)turn * 2 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKakugyou[(int)turn * 2 + i], turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 金将
                for (int i = 0; i < 4; i++) {
                    if (ban.putKinsyou[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKinsyou[(int)turn * 4 + i], turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 成駒
                for (int i = 0, j = 0; j < ban.putNarigomaNum[(int)turn]; i++) {
                    if (ban.putNarigoma[(int)turn * 30 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putNarigoma[(int)turn * 30 + i], turn, kmv, ref kCnt, ref startPoint);
                        j++;
                    }
                }

                // 駒打ち
                for (int i = 0; i < 7; i++) {
                    if (ban.captPiece[(int)turn * 7 + i] > 0) {
                        getEachMoveList(ref ban, 81 + i + 1, turn, kmv, ref kCnt, ref startPoint);
                    }
                }
            }

            return (kCnt, startPoint);
        }

        public void getEachMoveList(ref ban ban, int oPos, Pturn turn, kmove[] kmv, ref int kCnt, ref int startPoint) {
            // 駒打ち
            unsafe {
                if ((oPos / 9) == 9) {
                    for (int i = 0; i < 9; i++) {

                        // 二歩は打てない
                        if (((oPos % 9) == (int)ktype.Fuhyou) && (ban.putFuhyou[(int)turn * 9 + i] < 9)) {
                            continue;

                        }
                        for (int j = 0; j < 9; j++) {
                            // 1段目には打てない
                            if ((((oPos % 9) == (int)ktype.Fuhyou) || ((oPos % 9) == (int)ktype.Kyousha)) && (pturn.psX(turn, j) > 7)) {
                                continue;
                                // 2段目には打てない
                            } else if (((oPos % 9) == (int)ktype.Keima) && (pturn.psX(turn, j) > 6)) {
                                continue;
                            }
                            // 駒があると打てない
                            if (ban.onBoard[i * 9 + j] > 0) {
                                continue;
                            }

                            kmv[startPoint + kCnt++].set(oPos, i * 9 + j, 0, false, turn);
                        }
                    }

                    // 移動
                } else {

                    switch (ban.getOnBoardKtype(oPos)) {
                        case ktype.Fuhyou:
                            //action(ox, oy, ox, ptuen.mvY(getOnBoardPturn(ox, oy), oy, 1), turn, false);
                            getEachMovePos(ref ban, oPos, 0, 1, turn, kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Kyousha:
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Keima:
                            getEachMovePos(ref ban, oPos, 1, 2, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 2, turn, kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ginsyou:
                            getEachMovePos(ref ban, oPos, 1, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, -1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, -1, turn, kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Hisya:
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, -i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, 0, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, 0, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Kakugyou:
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, -i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, -i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Kinsyou:
                        case ktype.Tokin:
                        case ktype.Narikyou:
                        case ktype.Narikei:
                        case ktype.Narigin:
                            getEachMovePos(ref ban, oPos, 1, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, 0, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 0, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, -1, turn, kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ousyou:
                            getEachMovePos(ref ban, oPos, 1, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, 0, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 0, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, -1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, -1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, -1, turn, kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ryuuou:
                            getEachMovePos(ref ban, oPos, 1, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, -1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, -1, turn, kmv, ref kCnt, ref startPoint);
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, -i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, 0, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, 0, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Ryuuma:
                            getEachMovePos(ref ban, oPos, 0, 1, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, 0, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 0, turn, kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, -1, turn, kmv, ref kCnt, ref startPoint);
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, -i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, -i, turn, kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        //指定移動先(mx,my)
        //移動できる 0 / 移動できる(敵駒取り) 1 / 移動できない(味方駒) 2 / 移動できない 3(範囲外)
        public int getEachMovePos(ref ban ban, int oPos, int mx, int my, Pturn turn, kmove[] kmv, ref int kCnt, ref int startPoint) {
            int nx = pturn.mvX(turn, oPos / 9, mx);
            int ny = pturn.mvY(turn, oPos % 9, my);
            int val;
            unsafe {
                if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 3;
                if (ban.onBoard[nx * 9 + ny] > 0) {
                    if (ban.getOnBoardPturn(nx, ny) != turn) {
                        if ((pturn.psY(turn, ny) > 5) && ((int)ban.getOnBoardKtype(oPos) < 7)) {
                            if ((ban.getOnBoardKtype(oPos) == ktype.Ginsyou) || ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) && (pturn.psY(turn, ny) < 8)) || ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) && (pturn.psY(turn, ny) < 7))) {

                                if (ban.moveable[pturn.aturn((int)turn) * 81 + nx * 9 + ny] > 0) {
                                    val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)] - tw2ai.kVal[(int)ban.getOnBoardKtype(oPos)];
                                } else {
                                    val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)];
                                }
                                if (val >= kmv[startPoint].val) {
                                    kmv[--startPoint].set(oPos, nx * 9 + ny, val, false, turn);
                                    kCnt++;
                                } else {
                                    kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, val, false, turn);
                                }
                            }
                            if (ban.moveable[pturn.aturn((int)turn) * 81 + nx * 9 + ny] > 0) {
                                val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)] - tw2ai.kVal[(int)ban.getOnBoardKtype(oPos)] + 250;
                            } else {
                                val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)] + 250;
                            }
                            if (val >= kmv[startPoint].val) {
                                kmv[--startPoint].set(oPos, nx * 9 + ny, val, true, turn);
                                kCnt++;
                            } else {
                                kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, val, true, turn);
                            }
                        } else {

                            if (ban.moveable[pturn.aturn((int)turn) * 81 + nx * 9 + ny] > 0) {
                                val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)] - tw2ai.kVal[(int)ban.getOnBoardKtype(oPos)];
                            } else {
                                val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)];
                            }
                            if (val >= kmv[startPoint].val) {
                                kmv[--startPoint].set(oPos, nx * 9 + ny, val, false, turn);
                                kCnt++;
                            } else {
                                kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, val, false, turn);
                            }


                        }

                        return 1; // 敵の駒(取れる)
                    } else {
                        return 2; // 味方の駒
                    }

                }
                if ((pturn.psY(turn, ny) > 5) && ((int)ban.getOnBoardKtype(oPos) < 7)) {
                    if ((ban.getOnBoardKtype(oPos) == ktype.Ginsyou) || ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) && (pturn.psY(turn, ny) < 8)) || ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) && (pturn.psY(turn, ny) < 7))) {
                        kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, 250, false, turn);
                    }
                    kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, 0, true, turn);
                } else {
                    kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, 0, false, turn);
                }
                return 0; // 駒がない
            }
        }
    }
}
