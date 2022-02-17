using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TacoWin2_BanInfo;
using TacoWin2_SMV;

namespace TacoWin2 {

    partial class tw2ai {

        struct hashTbl : IComparable<hashTbl> {
            public ulong hash;
            public int val;
            public int depth;
            public kmove[] kmv;

            public hashTbl(ulong _hash, int _val, int _depth, kmove[] _kmv) {
                hash = _hash;
                val = _val;
                depth = _depth;
                kmv = _kmv;
            }

            public int CompareTo(hashTbl ohash) {
                return hash.CompareTo(ohash.hash);
            }

        }

        List<hashTbl> aList = new List<hashTbl>();

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
        Object lockObj_hash = new Object();
        int mateDepMax = 0;

        static tw2ai() {
            // thread同時数取得
            ThreadPool.GetMinThreads(out workMin, out ioMin);
            Console.Write("workMin={0},ioMin={1}\n", workMin, ioMin);
        }

        public tw2ai() {
            resetHash();
        }

        void resetHash() {
            aList = new List<hashTbl>();
        }

        int chkHash(ulong hash, int depth, out int val, out kmove[] kmv) {
            val = 0;
            kmv = null;
            if (aList.Count == 0) {
                return 0;
            } else {
                int idx = aList.BinarySearch(new hashTbl(hash, val, depth, null));
                if (idx < 0) {
                    return 0;
                } else {
                    /* ハッシュに存在 */
                    val = aList[idx].val;
                    kmv = aList[idx].kmv;
                    return 1;
                }
            }
        }

        int addHash(ulong hash, int depth, int val, kmove[] kmv) {
            if (aList.Count == 0) {
                /* 最初の登録 */
                aList.Add(new hashTbl(hash, val, depth, kmv));
                return 0;
            } else {
                hashTbl n = new hashTbl(hash, val, depth, kmv);
                int idx = aList.BinarySearch(n, null);
                if (idx < 0) {
                    /* ハッシュに存在しない */
                    aList.Insert(~idx, n);
                    return 0;
                } else {
                    /* ハッシュに存在 */
                    if (depth < aList[idx].depth) {
                        /* 自分のほうが浅い場合は更新 */
                        aList.RemoveAt(idx);
                        aList.Insert(idx, n);
                    }

                    return -1;
                }
            }
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
        int depMax;
        public (kmove[], int) thinkMove(Pturn turn, ban ban, int depth) {
            int best = -999999;
            int beta = 999999;
            int alpha = -999999;

            kmove[] bestmove = null;

            int teCnt = 0; //手の進捗
            depMax = depth;
            tw2stval.tmpChk(ban);

            unsafe {

                // 定跡チェック
                //string oki = "";
                //string mochi = "";
                //sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                //string strs = sMove.get(oki + " " + mochi, turn);
                string strs = sMove.get(ban.hash, turn);
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
                    bestmove[0].set(ox * 9 + oy, nx * 9 + ny, best, 0, nari, turn);

                    string str = "";
                    for (int i = 0; bestmove[i].op > 0 || bestmove[i].np > 0; i++) {
                        str += "(" + (bestmove[i].op / 9 + 1) + "," + (bestmove[i].op % 9 + 1) + ")->(" + (bestmove[i].np / 9 + 1) + "," + (bestmove[i].np % 9 + 1) + ")/";
                    }
                    Console.Write("JOSEKI MV[{0}]{1}\n", best, str);
                    resetHash();
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
                        int retVal;
                        kmove[] retList = null;

                        //駒を動かす
                        tmp_ban.moveKoma(moveList[cnt_local].op / 9, moveList[cnt_local].op % 9, moveList[cnt_local].np / 9, moveList[cnt_local].np % 9, turn, moveList[cnt_local].nari, false, true);

                        // 王手はスキップ
                        if (tmp_ban.moveable[pturn.aturn((int)turn) * 81 + tmp_ban.putOusyou[(int)turn]] > 0) {
                            retVal = moveList[cnt_local].val - 99999;
                            //DebugForm.Form1Instance.addMsg("TASK[{0}:{1}]MV[{2}]({3},{4})->({5},{6})[{7}]\n", Task.CurrentId, cnt_local, retVal, moveList[cnt_local].op / 9 + 1, moveList[cnt_local].op % 9 + 1, moveList[cnt_local].np / 9 + 1, moveList[cnt_local].np % 9 + 1, moveList[cnt_local].val);
                        } else {
                            retVal = -think(pturn.aturn(turn), ref tmp_ban, out retList, -beta, -alpha, moveList[cnt_local].val, 1, depth);
                            retList[0] = moveList[cnt_local];

                            string str = "";
                            for (int i = 0; retList[i].op > 0 || retList[i].np > 0; i++) {
                                str += "(" + (retList[i].op / 9 + 1) + "," + (retList[i].op % 9 + 1) + ")->(" + (retList[i].np / 9 + 1) + "," + (retList[i].np % 9 + 1) + "):" + retList[i].val + "," + retList[i].aval + "/";
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

                foreach (var a in aList) {
                    DebugForm.instance.addMsg("aList:" + a.hash.ToString("X16") + "/" + a.depth + "/" + a.val);
                }

                mList.freeAlist(aid);
            }
            resetHash();
            return (bestmove, best);
        }

        public int think(Pturn turn, ref ban ban, out kmove[] bestMoveList, int alpha, int beta, int pVal, int depth, int depMammmx) {
            int val = -pVal;
            bestMoveList = null;
            int best = -999999;
            ulong bestHash = 0;
            kmove[] retList;

            if (stopFlg) {
                bestMoveList = new kmove[30];
                return 0;
            }

            unsafe {

                // 定跡チェック
                //string oki = "";
                //string mochi = "";
                //sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                //string strs = sMove.get(oki + " " + mochi, turn);
                string strs = sMove.get(ban.hash, turn);
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

                    bestMoveList[depth].set(ox * 9 + oy, nx * 9 + ny, best, 0, nari, turn);
                    return best;
                }

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
                        tmp_ban.moveKoma(moveList[cnt].op / 9, moveList[cnt].op % 9, moveList[cnt].np / 9, moveList[cnt].np % 9, turn, moveList[cnt].nari, false, true);

                        // 同一局面がすでに出ている場合
                        lock (lockObj_hash) {
                            if (chkHash(tmp_ban.hash, depth, out int reth, out kmove[] retm) > 0) {
                                if (reth > best) {
                                    best = reth;
                                    bestMoveList = retm;
                                    bestMoveList[depth] = moveList[cnt];
                                    if (best > alpha) {
                                        alpha = best;
                                    }
                                    if (best >= beta) {
                                        lock (lockObj) {
                                            mList.freeAlist(aid);
                                        }
                                        return best;
                                    }
                                }
                                continue;
                            }
                        }

                        if (tmp_ban.moveable[pturn.aturn((int)turn) * 81 + tmp_ban.putOusyou[(int)turn]] > 0) {
                            if (bestMoveList == null) {
                                bestMoveList = new kmove[30];
                                bestMoveList[depth] = moveList[cnt];
                                best = -999999 + depth * 10000 + moveList[cnt].val;
                            }
                            continue;
                        }

                        int retVal = -think(pturn.aturn(turn), ref tmp_ban, out retList, -beta, -alpha, moveList[cnt].val - pVal, depth + 1, depMax);
                        if (retVal > best) {
                            best = retVal;
                            bestMoveList = retList;
                            bestMoveList[depth] = moveList[cnt];
                            bestHash = tmp_ban.hash;
                            if (best > alpha) {
                                alpha = best;
                                //mList[depth] = tmpList[i];
                            }
                            if (best >= beta) {
                                lock (lockObj) {
                                    mList.freeAlist(aid);
                                }
                                if (depth < 2) {
                                    lock (lockObj_hash) {
                                        addHash(tmp_ban.hash, depth, best, bestMoveList);
                                    }
                                }
                                return best;
                            }
                        }
                    }
                    if (depth < 2) {
                        lock (lockObj_hash) {
                            addHash(bestHash, depth, best, bestMoveList);
                        }
                    }

                } else {

                    (int vla, int sp) = getBestMove(ref ban, turn, moveList);

                    //best = val - moveList[sp + vla - 1].val;
                    moveList[sp].val -= moveList[sp].aval;
                    best = val + moveList[sp].val;
                    //moveList[sp].val = best;
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
            //emove emv;
            unsafe {
                // 敵の次移動ポイントを計算
                getEnemyMoveList(ref ban, (int)turn, out emove emv);

                // 王将
                if (ban.putOusyou[(int)turn] != 0xFF) {
                    getEachMoveList(ref ban, ban.putOusyou[(int)turn], turn, emv, kmv, ref kCnt, ref startPoint);
                }

                // 歩兵
                for (int i = 0; i < 9; i++) {
                    if (ban.putFuhyou[(int)turn * 9 + i] != 9) {
                        getEachMoveList(ref ban, i * 9 + ban.putFuhyou[(int)turn * 9 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if (ban.putKyousha[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKyousha[(int)turn * 4 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 桂馬
                for (int i = 0; i < 4; i++) {
                    if (ban.putKeima[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKeima[(int)turn * 4 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if (ban.putGinsyou[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putGinsyou[(int)turn * 4 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if (ban.putHisya[(int)turn * 2 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putHisya[(int)turn * 2 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if (ban.putKakugyou[(int)turn * 2 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKakugyou[(int)turn * 2 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 金将
                for (int i = 0; i < 4; i++) {
                    if (ban.putKinsyou[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKinsyou[(int)turn * 4 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 成駒
                for (int i = 0, j = 0; j < ban.putNarigomaNum[(int)turn]; i++) {
                    if (ban.putNarigoma[(int)turn * 30 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putNarigoma[(int)turn * 30 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                        j++;
                    }
                }
            }

            return (kCnt, startPoint);
        }

        // type 0:全検索 1:駒打ち(無駄に取られる場所)省略 2: 1+駒打ち(効きに駒がない)省略 3;駒打ち全省略  
        public (int, int) getAllMoveList(ref ban ban, Pturn turn, kmove[] kmv, int type = 0) {
            int startPoint = 100;
            int kCnt = 0;
            ///emove emv;
            unsafe {
                // 敵の次移動ポイントを計算
                getEnemyMoveList(ref ban, (int)turn, out emove emv);

                // 駒移動

                // 王将
                if (ban.putOusyou[(int)turn] != 0xFF) {
                    getEachMoveList(ref ban, ban.putOusyou[(int)turn], turn, emv, kmv, ref kCnt, ref startPoint);
                }

                // 歩兵
                for (int i = 0; i < 9; i++) {
                    if (ban.putFuhyou[(int)turn * 9 + i] != 9) {
                        getEachMoveList(ref ban, i * 9 + ban.putFuhyou[(int)turn * 9 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if (ban.putKyousha[(int)turn * 4 + i] != 0xFF) {
                        if (ban.moveable[pturn.aturn((int)turn) * 81 + ban.putKyousha[(int)turn * 4 + i]] > 0) { //敵の効きがある
                            getEachMoveList(ref ban, ban.putKyousha[(int)turn * 4 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                        } else {
                            getEachMoveListKyousya(ref ban, ban.putKyousha[(int)turn * 4 + i], turn, emv.val[0], kmv, ref kCnt, ref startPoint);
                        }
                    }
                }

                // 桂馬
                for (int i = 0; i < 4; i++) {
                    if (ban.putKeima[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKeima[(int)turn * 4 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if (ban.putGinsyou[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putGinsyou[(int)turn * 4 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if (ban.putHisya[(int)turn * 2 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putHisya[(int)turn * 2 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if (ban.putKakugyou[(int)turn * 2 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKakugyou[(int)turn * 2 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 金将
                for (int i = 0; i < 4; i++) {
                    if (ban.putKinsyou[(int)turn * 4 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putKinsyou[(int)turn * 4 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 成駒
                for (int i = 0, j = 0; j < ban.putNarigomaNum[(int)turn]; i++) {
                    if (ban.putNarigoma[(int)turn * 30 + i] != 0xFF) {
                        getEachMoveList(ref ban, ban.putNarigoma[(int)turn * 30 + i], turn, emv, kmv, ref kCnt, ref startPoint);
                        j++;
                    }
                }

                // 駒打ち
                if (type < 3) {
                    for (int i = 0; i < 7; i++) {
                        if (ban.captPiece[(int)turn * 7 + i] > 0) {
                            getEachMoveList(ref ban, 81 + i + 1, turn, emv, kmv, ref kCnt, ref startPoint, type);
                        }
                    }
                }
            }

            return (kCnt, startPoint);
        }

        public unsafe struct emove {
            public fixed int pos[2];
            public fixed int val[2];
        }

        // 敵の次移動ポイントを計算
        void getEnemyMoveList(ref ban ban, int turn, out emove emv) {
            emv = new emove();
            int cnt = 0;
            unsafe {
                // 王将
                if ((ban.putOusyou[turn] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putOusyou[turn]] > 0)) {
                    emv.pos[cnt] = ban.putOusyou[turn];
                    emv.val[cnt++] = kVal[(int)ktype.Ousyou];
                    if (cnt > 1) return;
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if ((ban.putHisya[turn * 2 + i] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putHisya[turn * 2 + i]] > 0)) {
                        emv.pos[cnt] = ban.putHisya[turn * 2 + i];
                        emv.val[cnt++] = kVal[(int)ktype.Hisya];
                        if (cnt > 1) return;
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if ((ban.putKakugyou[(int)turn * 2 + i] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putKakugyou[turn * 2 + i]] > 0)) {
                        emv.pos[cnt] = ban.putKakugyou[turn * 2 + i];
                        emv.val[cnt++] = kVal[(int)ktype.Kakugyou];
                        if (cnt > 1) return;
                    }
                }

                // 金将
                for (int i = 0; i < 4; i++) {
                    if ((ban.putKinsyou[(int)turn * 4 + i] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putKinsyou[(int)turn * 4 + i]] > 0)) {
                        emv.pos[cnt] = ban.putKinsyou[(int)turn * 4 + i];
                        emv.val[cnt++] = kVal[(int)ktype.Kinsyou];
                        if (cnt > 1) return;
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if ((ban.putGinsyou[(int)turn * 4 + i] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putGinsyou[(int)turn * 4 + i]] > 0)) {
                        emv.pos[cnt] = ban.putGinsyou[(int)turn * 4 + i];
                        emv.val[cnt++] = kVal[(int)ktype.Ginsyou];
                        if (cnt > 1) return;
                    }
                }

            }


        }

        //public void getEachMoveListDepth(ref ban ban, int oPos, Pturn turn, kmove[] kmv, ref int kCnt, ref int startPoint) {
        //    unsafe {
        //        for (int i = 0; i < 9; i++) {
        //
        //            // 二歩は打てない
        //            if (((oPos % 9) == (int)ktype.Fuhyou) && (ban.putFuhyou[(int)turn * 9 + i] < 9)) {
        //                continue;
        //
        //            }
        //            for (int j = 0; j < 9; j++) {
        //                // 1段目には打てない
        //                if ((((oPos % 9) == (int)ktype.Fuhyou) || ((oPos % 9) == (int)ktype.Kyousha)) && (pturn.psX(turn, j) > 7)) {
        //                    continue;
        //                    // 2段目には打てない
        //                } else if (((oPos % 9) == (int)ktype.Keima) && (pturn.psX(turn, j) > 6)) {
        //                    continue;
        //                }
        //                // 駒があると打てない
        //                if (ban.onBoard[i * 9 + j] > 0) {
        //                    continue;
        //                }
        //
        //                kmv[startPoint + kCnt++].set(oPos, i * 9 + j, 0, false, turn);
        //            }
        //        }
        //    }
        //}

        //香車専用移動リスト作成
        public void getEachMoveListKyousya(ref ban ban, int oPos, Pturn turn, int eVal, kmove[] kmv, ref int kCnt, ref int startPoint) {
            for (int i = 1; i < 9; ++i) {
                int nx = oPos / 9;
                int ny = pturn.mvY(turn, oPos % 9, i);
                if ((ny < 0) || (ny > 8)) return;
                unsafe {
                    if ((ban.onBoard[nx * 9 + ny] > 0) || (pturn.psY(turn, ny) > 5)) {
                        getEachMovePos(ref ban, oPos, 0, i, turn, eVal, kmv, ref kCnt, ref startPoint);
                    }
                    if (ban.onBoard[nx * 9 + ny] > 0) return;
                }
                //int val;
                //unsafe {
                //    if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return;
                //    if (ban.onBoard[nx * 9 + ny] > 0) {
                //        if (ban.getOnBoardPturn(nx, ny) != turn) {
                //            if (pturn.psY(turn, ny) > 5) {
                //                if (pturn.psY(turn, ny) < 7) {
                //                    if (val >= kmv[startPoint].val) {
                //                        kmv[--startPoint].set(oPos, nx * 9 + ny, val, eVal, false, turn);
                //                        kCnt++;
                //                    } else {
                //                        kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, val, eVal, false, turn);
                //                    }
                //                }
                //                if (ban.moveable[pturn.aturn((int)turn) * 81 + nx * 9 + ny] > 0) {
                //                    val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)] - tw2ai.kVal[(int)ban.getOnBoardKtype(oPos)] + 250;
                //                } else {
                //                    val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)] + 250;
                //                }
                //                if (val >= kmv[startPoint].val) {
                //                    kmv[--startPoint].set(oPos, nx * 9 + ny, val, eVal, true, turn);
                //                    kCnt++;
                //                } else {
                //                    kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, val, eVal, true, turn);
                //                }
                //            } else {
                //
                //                if (ban.moveable[pturn.aturn((int)turn) * 81 + nx * 9 + ny] > 0) {
                //                    val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)] - tw2ai.kVal[(int)ban.getOnBoardKtype(oPos)];
                //                } else {
                //                    val = tw2ai.kVal[(int)ban.getOnBoardKtype(nx, ny)];
                //                }
                //                if (val >= kmv[startPoint].val) {
                //                    kmv[--startPoint].set(oPos, nx * 9 + ny, val, eVal, false, turn);
                //                    kCnt++;
                //                } else {
                //                    kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, val, eVal, false, turn);
                //                }
                //            }
                //            break; // 敵の駒(取れる)
                //        } else {
                //            break; // 味方の駒(取れない)
                //        }
                //    }
                //    if ((pturn.psY(turn, ny) > 5) && ((int)ban.getOnBoardKtype(oPos) < 7)) {
                //        if ((ban.moveable[pturn.aturn((int)turn) * 81 + nx * 9 + ny] == 0) || (ban.moveable[(int)turn * 81 + nx * 9 + ny] > 0)) {
                //            kmv[startPoint + kCnt++].set(oPos, nx * 9 + ny, 0, eVal, true, turn);
                //        }
                //    } else {
                //    }
                //}
            };
        }

        public void getEachMoveList(ref ban ban, int oPos, Pturn turn, emove emv, kmove[] kmv, ref int kCnt, ref int startPoint, int type = 0) {
            // 駒打ち
            unsafe {
                int cnt = 0;
                if (emv.pos[0] == oPos) cnt = 1;

                if (oPos > 80) { // (oPos / 9) == 9 持ち駒
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

                            //敵の効きのみある所には打たない
                            if (((oPos % 9) != (int)ktype.Fuhyou) && (ban.moveable[pturn.aturn((int)turn) * 81 + i * 9 + j] > 0) && (ban.moveable[(int)turn * 81 + i * 9 + j] == 0)) {
                                continue;
                            }

                            // 歩飛角以外の駒で、移動先に敵味方の駒がない場合、無駄なため置かない
                            switch (oPos % 9) {
                                case (int)ktype.Kyousha:
                                    int kret = 0;
                                    for (int k = 1; k < 9; k++) {
                                        int nx = i;
                                        int ny = pturn.mvY(turn, j, k);
                                        if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) break;
                                        if (ban.onBoard[nx * 9 + ny] > 0) {
                                            kret = 1;
                                            break;
                                        }
                                    }
                                    if (kret == 0) continue;
                                    break;

                                case (int)ktype.Keima:
                                    if ((chkMoveable(ref ban, i * 9 + j, 1, 2, turn) < 1) && (chkMoveable(ref ban, i * 9 + j, -1, 2, turn) < 1)) continue;
                                    break;

                                case (int)ktype.Ginsyou:
                                    if ((chkMoveable(ref ban, i * 9 + j, 1, 1, turn) < 1) && (chkMoveable(ref ban, i * 9 + j, 0, 1, turn) < 1) &&
                                        (chkMoveable(ref ban, i * 9 + j, -1, 1, turn) < 1) && (chkMoveable(ref ban, i * 9 + j, 1, -1, turn) < 1) && (chkMoveable(ref ban, i * 9 + j, -1, -1, turn) < 1)) continue;
                                    break;

                                case (int)ktype.Kinsyou:
                                    if ((chkMoveable(ref ban, i * 9 + j, 1, 1, turn) < 1) && (chkMoveable(ref ban, i * 9 + j, 0, 1, turn) < 1) && (chkMoveable(ref ban, i * 9 + j, -1, 1, turn) < 1) &&
                                         (chkMoveable(ref ban, i * 9 + j, 1, 0, turn) < 1) && (chkMoveable(ref ban, i * 9 + j, -1, 0, turn) < 1) && (chkMoveable(ref ban, i * 9 + j, 0, -1, turn) < 1)) continue;
                                    break;

                                default:
                                    break;
                            }

                            // リストに追加
                            if (-emv.val[0] > kmv[startPoint].val) {
                                kmv[--startPoint].set(oPos, i * 9 + j, 0, emv.val[0], false, turn);
                                kCnt++;
                            } else {
                                kmv[startPoint + kCnt++].set(oPos, i * 9 + j, 0, emv.val[0], false, turn);
                            }
                        }
                    }

                    // 移動
                } else {

                    switch (ban.getOnBoardKtype(oPos)) {
                        case ktype.Fuhyou:
                            //action(ox, oy, ox, ptuen.mvY(getOnBoardPturn(ox, oy), oy, 1), turn, false);
                            getEachMovePos(ref ban, oPos, 0, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Kyousha:
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Keima:
                            getEachMovePos(ref ban, oPos, 1, 2, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 2, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ginsyou:
                            getEachMovePos(ref ban, oPos, 1, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Hisya:
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, -i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Kakugyou:
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, -i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, -i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Kinsyou:
                        case ktype.Tokin:
                        case ktype.Narikyou:
                        case ktype.Narikei:
                        case ktype.Narigin:
                            getEachMovePos(ref ban, oPos, 1, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ousyou:
                            getEachMovePos(ref ban, oPos, 1, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ryuuou:
                            getEachMovePos(ref ban, oPos, 1, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, 0, -i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Ryuuma:
                            getEachMovePos(ref ban, oPos, 0, 1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 1, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, -1, 0, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, oPos, 0, -1, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, i, -i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, oPos, -i, -i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        //指定移動先(mx,my)
        //移動できる 0 / 移動できる(敵駒取り) 1 / 移動できない(味方駒) 2 / 移動できない 3(範囲外)
        public int getEachMovePos(ref ban ban, int oPos, int mx, int my, Pturn turn, int eVal, kmove[] kmv, ref int kCnt, ref int startPoint) {
            int nx = pturn.mvX(turn, oPos / 9, mx);
            int ny = pturn.mvY(turn, oPos % 9, my);
            int val;
            unsafe {
                if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 3;
                int nPos = nx * 9 + ny;
                int sval = tw2stval.get(ban.getOnBoardKtype(oPos), nx, ny, oPos / 9, oPos % 9, (int)turn);
                if ((ban.moveable[pturn.aturn((int)turn) * 81 + nPos] > 0) && (kVal[(int)ban.getOnBoardKtype(oPos)] > eVal)) {
                    eVal = tw2ai.kVal[(int)ban.getOnBoardKtype(oPos)];
                }
                if (ban.onBoard[nPos] > 0) {
                    if (ban.getOnBoardPturn(nx, ny) != turn) {
                        val = kVal[(int)ban.getOnBoardKtype(nx, ny)] + sval;
                        if ((pturn.psY(turn, ny) > 5) && ((int)ban.getOnBoardKtype(oPos) < 7)) {

                            // 不成
                            if ((ban.getOnBoardKtype(oPos) == ktype.Ginsyou) || ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) && (pturn.psY(turn, ny) < 8)) || ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) && (pturn.psY(turn, ny) < 7))) {
                                if (val - eVal >= kmv[startPoint].val) {
                                    kmv[--startPoint].set(oPos, nPos, val, eVal, false, turn);
                                    kCnt++;
                                } else {
                                    kmv[startPoint + kCnt++].set(oPos, nPos, val, eVal, false, turn);
                                }
                            }

                            // 成り
                            if (val + 250 - eVal >= kmv[startPoint].val) {
                                kmv[--startPoint].set(oPos, nPos, val + 250, eVal, true, turn);
                                kCnt++;
                            } else {
                                kmv[startPoint + kCnt++].set(oPos, nPos, val + 250, eVal, true, turn);
                            }

                        } else {

                            if (val - eVal >= kmv[startPoint].val) {
                                kmv[--startPoint].set(oPos, nPos, val, eVal, false, turn);
                                kCnt++;
                            } else {
                                kmv[startPoint + kCnt++].set(oPos, nPos, val, eVal, false, turn);
                            }

                        }

                        return 1; // 敵の駒(取れる)
                    } else {
                        return 2; // 味方の駒
                    }

                }
                if ((pturn.psY(turn, ny) > 5) && ((int)ban.getOnBoardKtype(oPos) < 7)) {
                    if ((ban.getOnBoardKtype(oPos) == ktype.Ginsyou) || ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) && (pturn.psY(turn, ny) < 8)) || ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) && (pturn.psY(turn, ny) < 7))) {
                        kmv[startPoint + kCnt++].set(oPos, nPos, sval, eVal, false, turn);
                    }
                    if (ban.moveable[(int)turn * 81 + nPos] > ban.moveable[pturn.aturn((int)turn) * 81 + nPos]) {
                        kmv[startPoint + kCnt++].set(oPos, nPos, 250 + sval, eVal, true, turn); // 成りボーナス
                    } else {
                        kmv[startPoint + kCnt++].set(oPos, nPos, sval, eVal, true, turn);
                    }
                } else {
                    kmv[startPoint + kCnt++].set(oPos, nPos, sval, eVal, false, turn);
                }
                return 0; // 駒がない
            }
        }

        //指定移動先(mx,my)に移動できるかチェック
        public int chkMoveable(ref ban ban, int oPos, int mx, int my, Pturn turn) {
            int nx = pturn.mvX(turn, oPos / 9, mx);
            int ny = pturn.mvY(turn, oPos % 9, my);
            unsafe {
                if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return -1;
                if (ban.onBoard[nx * 9 + ny] > 0) {
                    if (ban.getOnBoardPturn(nx, ny) != turn) {
                        return 1; // 敵の駒(取れる)
                    } else {
                        return 2; // 味方の駒
                    }
                }
            }
            return 0; // 駒がない(移動可能)
        }


            }
        }
    }
}
