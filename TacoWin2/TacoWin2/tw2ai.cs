using System;
using System.Threading;
using System.Threading.Tasks;
using TacoWin2_BanInfo;
using TacoWin2_sfenIO;
using TacoWin2_SMV;

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

                // 定跡チェック
                string oki = "";
                string mochi = "";
                sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                string strs = sMove.get(oki + " " + mochi, turn);
                if (strs != null) {
                    int ox;
                    int oy;
                    int nx;
                    int ny;
                    bool nari;
                    int val = 0;

                    tw2usiIO.usi2pos(strs.Substring(1), out ox, out oy, out nx, out ny, out nari);
                    ban tmp_ban = ban;
                    if (tmp_ban.onBoard[nx * 9 + ny] > 0) {
                        val += kVal[(int)ban.getOnBoardKtype(nx * 9 + ny)] + tw2stval.get(ban.getOnBoardKtype(ox * 9 + oy), nx, ny, ox, oy, (int)turn);
                    } else if (ox < 9) {
                        val += tw2stval.get(ban.getOnBoardKtype(ox * 9 + oy), nx, ny, ox, oy, (int)turn);
                    }

                    ban.moveKoma(ox, oy, nx, ny, turn, nari, false, false);

                    best = -think(pturn.aturn(turn), ref ban, out bestmove, -beta, -alpha, val, 1, depth);
                    bestmove[0].set(ox * 9 + oy, nx * 9 + ny, best, nari, turn);

                    string str = "";
                    for (int i = 0; bestmove[i].op > 0 || bestmove[i].np > 0; i++) {
                        str += "(" + (bestmove[i].op / 9 + 1) + "," + (bestmove[i].op % 9 + 1) + ")->(" + (bestmove[i].np / 9 + 1) + "," + (bestmove[i].np % 9 + 1) + ")/";
                    }
                    Console.Write("JOSEKI MV[{0}]{1}\n", best, str);

                    return (bestmove, best);
                }

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
                            //DebugForm.Form1Instance.addMsg("TASK[{0}:{1}]MV[{2}]({3},{4})->({5},{6})[{7}]\n", Task.CurrentId, cnt_local, retVal, moveList[cnt_local].op / 9 + 1, moveList[cnt_local].op % 9 + 1, moveList[cnt_local].np / 9 + 1, moveList[cnt_local].np % 9 + 1, moveList[cnt_local].val);
                        } else {
                            moveList[cnt_local].val = val;
                            retVal = -think(pturn.aturn(turn), ref tmp_ban, out retList, -beta, -alpha, val, 1, depth);
                            retList[0] = moveList[cnt_local];

                            string str = "";
                            for (int i = 0; retList[i].op > 0 || retList[i].np > 0; i++) {
                                str += "(" + (retList[i].op / 9 + 1) + "," + (retList[i].op % 9 + 1) + ")->(" + (retList[i].np / 9 + 1) + "," + (retList[i].np % 9 + 1) + "):" + retList[i].val + "/";
                            }

                            DebugForm.instance.addMsg("TASK[" + Task.CurrentId + ":" + cnt_local + "]MV[" + retVal + "]" + str);
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

            if (stopFlg) {
                bestMoveList = new kmove[30];
                return 0;
            }

            unsafe {

                //定跡チェック
                string oki = "";
                string mochi = "";
                sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                string strs = sMove.get(oki + " " + mochi, turn);
                if (strs != null) {
                    int ox;
                    int oy;
                    int nx;
                    int ny;
                    bool nari;

                    tw2usiIO.usi2pos(strs.Substring(1), out ox, out oy, out nx, out ny, out nari);
                    //Console.Write("JOSEKI HIT:({0},{1})->({2},{3})\n", ox, oy, nx, ny);

                    if (ban.onBoard[nx * 9 + ny] > 0) {
                        val += kVal[(int)ban.getOnBoardKtype(nx * 9 + ny)] + tw2stval.get(ban.getOnBoardKtype(ox * 9 + oy), nx, ny, ox, oy, (int)turn);
                    } else if (ox < 9) {
                        val += tw2stval.get(ban.getOnBoardKtype(ox * 9 + oy), nx, ny, ox, oy, (int)turn);
                    }
                    ban.moveKoma(ox, oy, nx, ny, turn, nari, false, false);
                    if (depth < 20) {
                        best = -think(pturn.aturn(turn), ref ban, out retList, -999999, 999999, val, depth + 1, depth);
                        bestMoveList = retList;
                    } else {
                        bestMoveList = new kmove[30];
                        best = val;
                    }

                    bestMoveList[depth].set(ox * 9 + oy, nx * 9 + ny, best, nari, turn);
                    return best;
                }

                // 持ち駒がある
                // どこかに打つ
                //kmove[] moveList = new kmove[500];
                kmove[] moveList;
                int aid;
                lock (lockObj) {
                    aid = mList.assignAlist(out moveList);
                }

                if (depth < depMax) {
                    (int vla, int sp) = getAllMoveList(ref ban, turn, moveList);
                    for (int cnt = sp; cnt < vla + sp; cnt++) {

                        //駒を動かす
                        ban tmp_ban = ban;
                        if ((tmp_ban.onBoard[moveList[cnt].np] > 0)) {
                            val = kVal[(int)tmp_ban.getOnBoardKtype(moveList[cnt].np)] + tw2stval.get(tmp_ban.getOnBoardKtype(moveList[cnt].op), moveList[cnt].np / 9, moveList[cnt].np % 9, moveList[cnt].op / 9, moveList[cnt].op % 9, (int)turn) - pVal;
                        } else if ((moveList[cnt].op / 9) < 9) {
                            val = tw2stval.get(tmp_ban.getOnBoardKtype(moveList[cnt].op), moveList[cnt].np / 9, moveList[cnt].np % 9, moveList[cnt].op / 9, moveList[cnt].op % 9, (int)turn) - pVal;
                        }
                        tmp_ban.moveKoma(moveList[cnt].op / 9, moveList[cnt].op % 9, moveList[cnt].np / 9, moveList[cnt].np % 9, turn, moveList[cnt].nari, false, true);

                        if (tmp_ban.moveable[pturn.aturn((int)turn) * 81 + tmp_ban.putOusyou[(int)turn]] > 0) {
                            if (bestMoveList == null) {
                                bestMoveList = new kmove[500];
                                bestMoveList[depth] = moveList[cnt];
                                best = -999999 + depth * 10000 + val;
                            }
                            continue;
                        }
                        moveList[cnt].val = val;
                        int retVal = -think(pturn.aturn(turn), ref tmp_ban, out retList, -beta, -alpha, val, depth + 1, depMax);
                        if (retVal > best) {
                            best = retVal;
                            bestMoveList = retList;
                            bestMoveList[depth] = moveList[cnt];
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

                    (int vla, int sp) = getBestMove(ref ban, turn, moveList);

                    //best = val - moveList[sp + vla - 1].val;
                    best = val + moveList[sp].val;
                    moveList[sp].val = best;
                    bestMoveList = new kmove[30];
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

        // 最深
        public (int, int) getBestMove(ref ban ban, Pturn turn, kmove[] kmv) {
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
            }

            return (kCnt, startPoint);
        }

        public (int, int) getAllMoveListDepth(ref ban ban, Pturn turn, kmove[] kmv) {
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

        public void getEachMoveListDepth(ref ban ban, int oPos, Pturn turn, kmove[] kmv, ref int kCnt, ref int startPoint) {
            unsafe {
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
            }
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


        public (kmove[], int) thinkMateMove(Pturn turn, ban ban, int depth) {
            int best = -999999;
            int beta = 999999;
            int alpha = -999999;

            kmove[] bestmove = null;

            int teCnt = 0; //手の進捗

            tw2stval.tmpChk(ban);

            unsafe {

                int aid = mList.assignAlist(out kmove[] moveList);

                //[攻め方]王手を指せる手を全てリスト追加
                (int vla, int sp) = getAllCheckList(ref ban, turn, moveList, );

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
                            //DebugForm.Form1Instance.addMsg("TASK[{0}:{1}]MV[{2}]({3},{4})->({5},{6})[{7}]\n", Task.CurrentId, cnt_local, retVal, moveList[cnt_local].op / 9 + 1, moveList[cnt_local].op % 9 + 1, moveList[cnt_local].np / 9 + 1, moveList[cnt_local].np % 9 + 1, moveList[cnt_local].val);
                        } else {
                            moveList[cnt_local].val = val;
                            retVal = -thinkMateDef(pturn.aturn(turn), ref tmp_ban, out retList, 1, depth);
                            retList[0] = moveList[cnt_local];

                            string str = "";
                            for (int i = 0; retList[i].op > 0 || retList[i].np > 0; i++) {
                                str += "(" + (retList[i].op / 9 + 1) + "," + (retList[i].op % 9 + 1) + ")->(" + (retList[i].np / 9 + 1) + "," + (retList[i].np % 9 + 1) + "):" + retList[i].val + "/";
                            }

                            DebugForm.instance.addMsg("TASK[" + Task.CurrentId + ":" + cnt_local + "]MV[" + retVal + "]" + str);
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

        //指定した位置に次に移動可能となる
        (int vla, int sp) getAllCheckList(ref ban ban, Pturn turn, kmove[] kmv, int kingPos) {
            int startPoint = 0;
            int kCnt = 0;
            unsafe {
                int aOuPos = ban.putOusyou[pturn.aturn((int)turn)]; //相手王将の位置

                // 歩兵
                // [不成・成り]相手玉の2段前
                if (ban.putFuhyou[(int)turn * 9 + aOuPos / 9] == pturn.mvY(pturn.aturn(turn), aOuPos % 9, -2)) {
                    addCheckMovePos(ref ban, (aOuPos / 9) * 9 + ban.putFuhyou[(int)turn * 9 + aOuPos / 9], 0, 1, turn, false, kmv, ref kCnt);
                    addCheckMovePos(ref ban, (aOuPos / 9) * 9 + ban.putFuhyou[(int)turn * 9 + aOuPos / 9], 0, 1, turn, true, kmv, ref kCnt);
                }
                //[成り] 相手玉の右
                if ((aOuPos / 9 < 8) &&
                    ((ban.putFuhyou[(int)turn * 9 + aOuPos / 9 + 1] == pturn.mvY(pturn.aturn(turn), aOuPos % 9, -2)) |
                    (ban.putFuhyou[(int)turn * 9 + aOuPos / 9 + 1] == pturn.mvY(pturn.aturn(turn), aOuPos % 9, -1)))) {
                    addCheckMovePos(ref ban, ((aOuPos / 9) + 1) * 9 + ban.putFuhyou[(int)turn * 9 + aOuPos / 9 + 1], 0, 1, turn, true, kmv, ref kCnt);
                }
                //[成り] 相手玉の左
                if ((aOuPos / 9 > 0) &&
                    ((ban.putFuhyou[(int)turn * 9 + aOuPos / 9 - 1] == pturn.mvY(pturn.aturn(turn), aOuPos % 9, -2)) |
                    (ban.putFuhyou[(int)turn * 9 + aOuPos / 9 - 1] == pturn.mvY(pturn.aturn(turn), aOuPos % 9, -1)))) {
                    addCheckMovePos(ref ban, ((aOuPos / 9) - 1) * 9 + ban.putFuhyou[(int)turn * 9 + aOuPos / 9 - 1], 0, 1, turn, true, kmv, ref kCnt);
                }


                // 香車
                for (int i = 0; i < 4; i++) {
                    if (ban.putKyousha[(int)turn * 4 + i] != 0xFF) {
                        int dx = pturn.psX(turn, aOuPos / 9 - ban.putKeima[(int)turn * 4 + i] / 9);
                        int dy;
                        int ret;
                        int tPos;
                        switch (dx) {
                            case (0):
                                // [不成]敵を取って直進
                                ret = chkRectMove(ref ban, turn, ban.putKyousha[(int)turn * 4 + i], aOuPos, 0, 1);
                                if (ret >= 0) {
                                    dy = pturn.psX(turn, ret % 9 - ban.putKeima[(int)turn * 4 + i] % 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, dy, turn, false, kmv, ref kCnt);
                                        if (pturn.psY(turn, aOuPos / 9 - ban.putKyousha[(int)turn * 4 + i] / 9) == 1) {//一つ手前
                                            addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, dy, turn, true, kmv, ref kCnt);
                                        }
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        getEachMoveList(ref ban, ret, turn, kmv, ref kCnt, ref startPoint); // TODO : 
                                    }
                                }
                                break;

                            case (-1):
                                tPos = pturn.mvXY(turn, aOuPos, -9);
                                dy = pturn.psX(turn, aOuPos % 9 - ban.putKeima[(int)turn * 4 + i] % 9) - 1;
                                ret = chkRectMove(ref ban, turn, ban.putKyousha[(int)turn * 4 + i], tPos, 0, 1);
                                if ((ret == pturn.mvXY(turn, tPos, -1)) && (ban.getOnBoardPturn(ret / 9, ret % 9) != turn)) {// 王の横手前
                                    addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, dy, turn, true, kmv, ref kCnt);
                                } else if (ret == -1) {// 王の横手前
                                    addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, dy, turn, true, kmv, ref kCnt);
                                    if ((ban.onBoard[tPos] == 0) || (ban.getOnBoardPturn(tPos) != turn)) { // 王の横
                                        addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, dy + 1, turn, true, kmv, ref kCnt);
                                    }
                                }
                                break;

                            case (1):
                                tPos = pturn.mvXY(turn, aOuPos, 9);
                                dy = pturn.psX(turn, aOuPos % 9 - ban.putKeima[(int)turn * 4 + i] % 9) - 1;
                                ret = chkRectMove(ref ban, turn, ban.putKyousha[(int)turn * 4 + i], tPos, 0, 1);
                                if ((ret == pturn.mvXY(turn, tPos, -1)) && (ban.getOnBoardPturn(ret / 9, ret % 9) != turn)) {// 王の横手前
                                    addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, dy, turn, true, kmv, ref kCnt);
                                } else if (ret == -1) {// 王の横手前
                                    addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, dy, turn, true, kmv, ref kCnt);
                                    if ((ban.onBoard[tPos] == 0) || (ban.getOnBoardPturn(tPos) != turn)) { // 王の横
                                        addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, dy + 1, turn, true, kmv, ref kCnt);
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }

                // 桂馬
                for (int i = 0; i < 4; i++) {
                    if (ban.putKeima[(int)turn * 4 + i] != 0xFF) {
                        //×××××
                        //××●××
                        //×①×②×
                        //③×④×⑤
                        //⑥⑦⑧⑨⑩
                        //⑪×⑫×⑬

                        int dx = pturn.psX(turn, aOuPos / 9 - ban.putKeima[(int)turn * 4 + i] / 9);
                        int dy = pturn.psY(turn, aOuPos % 9 - ban.putKeima[(int)turn * 4 + i] % 9);

                        switch (dx, dy) {
                            case (-1, -1):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], 1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (1, -1):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], -1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (-2, -2):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], 1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (0, -2):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], 1, 2, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], -1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (2, -2):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], -1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (-2, -3):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], 1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (-1, -3):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], 1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (0, -3):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], 1, 2, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], -1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (1, -3):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], -1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (2, -3):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], -1, 2, turn, true, kmv, ref kCnt);
                                break;
                            case (-2, -4):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], -1, 2, turn, false, kmv, ref kCnt);
                                break;
                            case (0, -4):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], 1, 2, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], -1, 2, turn, false, kmv, ref kCnt);
                                break;
                            case (2, -4):
                                addCheckMovePos(ref ban, ban.putKeima[(int)turn * 4 + i], 1, 2, turn, false, kmv, ref kCnt);
                                break;


                            default:
                                break;
                        }
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if (ban.putGinsyou[(int)turn * 4 + i] != 0xFF) {
                        //①②③④⑤
                        //⑥×⑦×⑧
                        //⑨⑩●⑪⑫
                        //⑬×××⑭
                        //⑮⑯⑰⑱⑲

                        int dx = pturn.psX(turn, aOuPos / 9 - ban.putGinsyou[(int)turn * 4 + i] / 9);
                        int dy = pturn.psY(turn, aOuPos % 9 - ban.putGinsyou[(int)turn * 4 + i] % 9);

                        switch (dx, dy) {
                            case (-2, 2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, -1, turn, false, kmv, ref kCnt);
                                break;
                            case (-1, 2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, -1, turn, true, kmv, ref kCnt);
                                break;
                            case (0, 2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, -1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, -1, turn, false, kmv, ref kCnt);
                                break;
                            case (1, 2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, -1, turn, true, kmv, ref kCnt);
                                break;
                            case (2, 2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, -1, turn, false, kmv, ref kCnt);
                                break;
                            case (-2, 1):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, -1, turn, true, kmv, ref kCnt);
                                break;
                            case (0, 1):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, -1, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, -1, turn, true, kmv, ref kCnt);
                                break;
                            case (2, 1):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, -1, turn, true, kmv, ref kCnt);
                                break;
                            case (-2, 0):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, -1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (-1, 0):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, -1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, -1, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, true, kmv, ref kCnt);
                                break;
                            case (1, 0):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, -1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, -1, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, true, kmv, ref kCnt);
                                break;
                            case (2, 0):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, -1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (-2, -1):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, true, kmv, ref kCnt);
                                break;
                            case (2, -1):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, true, kmv, ref kCnt);
                                break;
                            case (-2, -2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, true, kmv, ref kCnt);
                                break;
                            case (-1, -2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 0, 1, turn, true, kmv, ref kCnt);
                                break;
                            case (0, -2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 1, 1, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 0, 1, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, true, kmv, ref kCnt);
                                break;
                            case (1, -2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], 0, 1, turn, true, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, true, kmv, ref kCnt);
                                break;
                            case (2, -2):
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putGinsyou[(int)turn * 4 + i], -1, 1, turn, true, kmv, ref kCnt);
                                break;

                            default:
                                break;
                        }
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
                        //××①××
                        //×②×③×
                        //④×●×⑤
                        //⑥×××⑦
                        //⑧⑨⑩⑪⑫


                        int dx = pturn.psX(turn, aOuPos / 9 - ban.putKinsyou[(int)turn * 4 + i] / 9);
                        int dy = pturn.psY(turn, aOuPos % 9 - ban.putKinsyou[(int)turn * 4 + i] % 9);

                        switch (dx, dy) {
                            case (0, 2):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 0, -1, turn, false, kmv, ref kCnt);
                                break;
                            case (-1, 1):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 0, -1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (1, 1):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], -1, 0, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 0, -1, turn, false, kmv, ref kCnt);
                                break;
                            case (-2, 0):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (2, 0):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], -1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (-2, -1):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (2, -1):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], -1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (-2, -2):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (-1, -2):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (0, -2):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (1, -2):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (2, -2):
                                addCheckMovePos(ref ban, ban.putKinsyou[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                break;

                            default:
                                break;
                        }
                    }
                }

                // 成駒
                for (int i = 0, j = 0; j < ban.putNarigomaNum[(int)turn]; i++) {
                    if (ban.putNarigoma[(int)turn * 30 + i] != 0xFF) {
                        //××①××
                        //×②×③×
                        //④×●×⑤
                        //⑥×××⑦
                        //⑧⑨⑩⑪⑫


                        int dx = pturn.psX(turn, aOuPos / 9 - ban.putNarigoma[(int)turn * 4 + i] / 9);
                        int dy = pturn.psY(turn, aOuPos % 9 - ban.putNarigoma[(int)turn * 4 + i] % 9);

                        switch (dx, dy) {
                            case (0, 2):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 0, -1, turn, false, kmv, ref kCnt);
                                break;
                            case (-1, 1):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 0, -1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (1, 1):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], -1, 0, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 0, -1, turn, false, kmv, ref kCnt);
                                break;
                            case (-2, 0):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (2, 0):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], -1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (-2, -1):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (2, -1):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], -1, 0, turn, false, kmv, ref kCnt);
                                break;
                            case (-2, -2):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (-1, -2):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (0, -2):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 1, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (1, -2):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], 0, 1, turn, false, kmv, ref kCnt);
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                break;
                            case (2, -2):
                                addCheckMovePos(ref ban, ban.putNarigoma[(int)turn * 4 + i], -1, 1, turn, false, kmv, ref kCnt);
                                break;

                            default:
                                break;
                        }
                        j++;
                    }
                }

                // 駒打ち
                //for (int i = 0; i < 7; i++) {
                //    if (ban.captPiece[(int)turn * 7 + i] > 0) {
                //        getEachMoveList(ref ban, 81 + i + 1, turn, kmv, ref kCnt, ref startPoint);
                //    }
                //}

                // 歩打ち
                if (ban.captPiece[(int)turn * 7 + 0] > 0) {
                    // 二歩チェック
                    if (ban.putFuhyou[(int)turn * 9 + (aOuPos / 9)] < 9) {
                        addCheckPutPos(ref ban, ktype.Fuhyou, aOuPos, 0, -1, turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 香打ち
                if (ban.captPiece[(int)turn * 7 + 1] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kyousha, aOuPos, 0, -i, turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 桂打ち
                if (ban.captPiece[(int)turn * 7 + 2] > 0) {
                    addCheckPutPos(ref ban, ktype.Keima, aOuPos, 1, -2, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Keima, aOuPos, -1, -2, turn, kmv, ref kCnt, ref startPoint);
                }

                // 銀打ち
                if (ban.captPiece[(int)turn * 7 + 3] > 0) {
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, -1, 1, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, 1, 1, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, 1, -1, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, 0, -1, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, -1, -1, turn, kmv, ref kCnt, ref startPoint);
                }

                // 飛打ち
                if (ban.captPiece[(int)turn * 7 + 4] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOuPos, 0, -i, turn, kmv, ref kCnt, ref startPoint);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOuPos, 0, i, turn, kmv, ref kCnt, ref startPoint);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOuPos, -i, 0, turn, kmv, ref kCnt, ref startPoint);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOuPos, i, 0, turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 角打ち
                if (ban.captPiece[(int)turn * 7 + 5] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOuPos, -i, -i, turn, kmv, ref kCnt, ref startPoint);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOuPos, i, -i, turn, kmv, ref kCnt, ref startPoint);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOuPos, -i, i, turn, kmv, ref kCnt, ref startPoint);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOuPos, i, i, turn, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 金打ち
                if (ban.captPiece[(int)turn * 7 + 6] > 0) {
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, 0, 1, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, -1, 0, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, 1, 0, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, -1, -1, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, 0, -1, turn, kmv, ref kCnt, ref startPoint);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, 1, -1, turn, kmv, ref kCnt, ref startPoint);
                }

            }
            return (kCnt, startPoint);
        }



        //指定移動先(mx,my)
        //移動できる 0 / 移動できる(敵駒取り) 1 / 移動できない(味方駒) 2 / 移動できない 3(範囲外)
        public int addCheckMovePos(ref ban ban, int oPos, int mx, int my, Pturn turn, bool nari, kmove[] kmv, ref int kCnt) {
            int nx = pturn.mvX(turn, oPos / 9, mx);
            int ny = pturn.mvY(turn, oPos % 9, my);
            unsafe {
                if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 3;
                if (((ban.onBoard[nx * 9 + ny] > 0)) && (ban.getOnBoardPturn(nx, ny) == turn)) return 2; // 味方の駒

                if (nari == true) {
                    //成れない場所は不可(飛角香のためコンティニュー可能にする)
                    if ((pturn.psY(turn, oPos % 9) > 5) || (pturn.psY(turn, ny) > 5)) {
                        kmv[kCnt++].set(oPos, nx * 9 + ny, 0, true, turn);
                    }
                } else {
                    if (((pturn.psY(turn, ny) == 8) && ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) || (ban.getOnBoardKtype(oPos) == ktype.Fuhyou))) ||
                        ((pturn.psY(turn, ny) > 6) && (ban.getOnBoardKtype(oPos) == ktype.Keima))) return 3; //不成NG
                    kmv[kCnt++].set(oPos, nx * 9 + ny, 0, true, turn);
                }

                if (ban.onBoard[nx * 9 + ny] > 0) {
                    return 1;
                } else {
                    return 0;
                }

            }
        }

        // ターゲット位置(tPos)からmx,myの相対位置に駒を置けるかチェック
        int addCheckPutPos(ref ban ban, ktype type, int tPos, int mx, int my, Pturn turn, kmove[] kmv, ref int kCnt, ref int startPoint) {
            unsafe {
                (int nx, int ny) = pturn.mvXY(turn, tPos / 9, tPos % 9, mx, my);
                if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 2;
                if (ban.onBoard[nx * 9 + ny] > 0) return 1;
                kmv[startPoint + kCnt++].set(81 + (int)type, nx * 9 + ny, 0, false, turn); //移動候補リストに追加
                return 0;
            }
        }

        // 指定先まで駒が存在するかチェック(指定先含めず)
        // 0～80:指定位置(X*9+Y)に駒あり -1 :駒無し -2 :駒2個以上あり -3 :opにたどり着かない
        int chkRectMove(ref ban ban, Pturn turn, int op, int np, int mx, int my) {
            unsafe {
                int ret = -1;
                int nx = op / 9;
                int ny = op % 9;
                for (int i = 0; nx * 9 + ny != np; i++) {
                    (nx, ny) = pturn.mvXY(turn, nx, ny, mx, my);
                    if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return -3;
                    if (ban.onBoard[nx * 9 + ny] > 0) {
                        if (ret == -1) {
                            ret = nx * 9 + ny;
                        } else {
                            return -2;
                        }
                    }
                }

                return ret;
            }
        }

        public int thinkMateDef(Pturn turn, ref ban ban, out kmove[] bestMoveList, int depth, int depMax) {





        }

    }
}
