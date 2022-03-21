using System;
using System.Collections.Generic;
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


        /// <summary>
        /// 各駒の固定価値
        /// </summary>
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
        //public (kmove, int) RandomeMove(Pturn turn, ban ban) {
        //    int ln = 0;
        //    int best = -1000;
        //
        //    int aid = mList.assignAlist(out kmove[] moveList);
        //
        //    unsafe {
        //        (int vla, int sp) = getAllMoveList(ref ban, turn, moveList);
        //        for (int i = 0; i < vla; i++) {
        //            int _rnd = rnds.Next(0, 100);
        //            ban tmps = ban;
        //            if ((tmps.moveable[pturn.aturn((int)turn) * 81 + moveList[sp + i].np] >= tmps.moveable[(int)turn * 81 + moveList[sp + i].np])) {
        //                _rnd -= 50;
        //            }
        //            if (tmps.onBoard[moveList[sp + i].np] > 0) {
        //                _rnd += 100;
        //            }
        //            tmps.moveKoma(moveList[sp + i].op, moveList[sp + i].np, moveList[sp + i].turn, moveList[sp + i].nari, true);
        //            if (tmps.moveable[pturn.aturn((int)turn) * 81 + tmps.putOusyou[(int)moveList[sp + i].turn]] > 0) _rnd -= 900;
        //            if (_rnd > best) {
        //                best = _rnd;
        //                ln = sp + i;
        //            }
        //        }
        //    }
        //
        //    mList.freeAlist(aid);
        //    return (moveList[ln], best);
        //}

        int depMax;
        public (kmove[], int) thinkMove(Pturn turn, ban ban, int depth, int mateDepth) {
            int best = -999999;
            int beta = 999999;
            int alpha = -999999;

            kmove[] bestmove = null;

            /* 詰み */
            if (mateDepth > 0) {
                int ret;
                DebugForm.instance.addMsg("thinkMateMove" + mateDepth);
                (bestmove, ret) = thinkMateMove(turn, ban, 9);
                if (ret < 999) return (bestmove, 99999);
            }

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
                    byte oPos;
                    byte nPos;
                    bool nari;
                    int val = 0;
                    tw2usiIO.usi2pos(strs.Substring(1), out oPos, out nPos, out nari);

                    ban tmp_ban = ban;
                    if (ban.getOnBoardKtype(nPos) > ktype.None) {
                        val += kVal[(int)ban.getOnBoardKtype(nPos)] + tw2stval.get(ban.getOnBoardKtype(oPos), oPos, nPos, turn);
                    } else if (oPos < 0x90) {
                        val += tw2stval.get(ban.getOnBoardKtype(oPos), oPos, nPos, turn);
                    }

                    ban.moveKoma(oPos, nPos, turn, nari, true);

                    best = -think(pturn.aturn(turn), ref ban, out bestmove, -beta, -alpha, val, 1, depth);
                    bestmove[0].set(oPos, nPos, best, 0, nari, turn);

                    string str = "";
                    for (int i = 0; bestmove[i].op > 0 || bestmove[i].np > 0; i++) {
                        str += ":" + (bestmove[i].op + 0x11).ToString("X2") + "-" + (bestmove[i].np + 0x11).ToString("X2") + "";
                    }

                    DebugForm.instance.addMsg("JOSEKI MV["+ best +"]" + str);

                    resetHash();
                    return (bestmove, best);
                }

                //kmove[] moveList = new kmove[500];
                int aid = mList.assignAlist(out kmove[] moveList);

                (int vla, int sp) = getAllMoveList(ref ban, turn, moveList);

                //手数が多い場合
                if ((vla > 150) && (depMax > 5)) depMax = 5;
                if ((vla > 100) && (depMax > 6)) depMax = 6;
                DebugForm.instance.addMsg("tenum = " + vla + "/ depMax = " + depMax + "/ workMin =" + workMin);

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
                        tmp_ban.moveKoma(moveList[cnt_local].op, moveList[cnt_local].np, turn, moveList[cnt_local].nari, true);

                        // 王手はスキップ
                        if (((byte)tmp_ban.data[((int)turn << 6) + ban.setOu] != 0xFF) &&
                        ((tmp_ban.data[tmp_ban.data[((int)turn << 6) + ban.setOu] & 0xFF] >> (8 + ((int)pturn.aturn(turn) << 2)) & 0x0F) > 0)) {
                            retVal = moveList[cnt_local].val - 99999;
                            //DebugForm.instance.addMsg("NG " + (tmp_ban.data[((int)turn << 6) + ban.setOu] & 0xFF) + " 0x"  + tmp_ban.data[tmp_ban.data[((int)turn << 6) + ban.setOu] & 0xFF].ToString("X8"));
                        } else {
                            retVal = -think(pturn.aturn(turn), ref tmp_ban, out retList, -beta, -alpha, moveList[cnt_local].val, 1, depth);
                            retList[0] = moveList[cnt_local];

                            /* 打ち歩詰めチェック */
                            if ((retVal > 5000) && (moveList[cnt_local].op == 0x91) && ((retList[2].op == 0) || (retList[2].np == 0))) { /* 9*9+(int)ktype.Fuhyou */
                                continue;
                            };

                            string str = "";
                            for (int i = 0; retList[i].op > 0 || retList[i].np > 0; i++) {
                                str += ((retList[i].op + 0x11).ToString("X2")) + "-" + ((retList[i].np + 0x11).ToString("X2")) + ":" + retList[i].val + "," + retList[i].aval + "/";
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

                //foreach (var a in aList) {
                //    DebugForm.instance.addMsg("aList:" + a.hash.ToString("X16") + "/" + a.depth + "/" + a.val);
                //}

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
                    byte oPos;
                    byte nPos;
                    bool nari;
                    tw2usiIO.usi2pos(strs.Substring(1), out oPos, out nPos, out nari);

                    //★★if (ban.onBoard[nx * 9 + ny] > 0) {
                    //★★    val += kVal[(int)ban.getOnBoardKtype(nx * 9 + ny)] + tw2stval.get(ban.getOnBoardKtype(ox * 9 + oy), nx, ny, ox, oy, (int)turn);
                    //★★} else if (oPos < 0x90) {
                    //★★    val += tw2stval.get(ban.getOnBoardKtype(ox * 9 + oy), nx, ny, ox, oy, (int)turn);
                    //★★}
                    if (ban.getOnBoardKtype(nPos) > ktype.None) {
                        val += kVal[(int)ban.getOnBoardKtype(nPos)] + tw2stval.get(ban.getOnBoardKtype(oPos), oPos, nPos, turn);
                    } else if (oPos < 0x90) {
                        val += tw2stval.get(ban.getOnBoardKtype(oPos), oPos, nPos, turn);
                    }

                    ban.moveKoma(oPos, nPos, turn, nari, true);
                    if (depth < 20) {
                        best = -think(pturn.aturn(turn), ref ban, out retList, -999999, 999999, val, depth + 1, depth);
                        bestMoveList = retList;
                    } else {
                        bestMoveList = new kmove[30];
                        best = val;
                    }

                    bestMoveList[depth].set(oPos, nPos, best, 0, nari, turn);
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
                        tmp_ban.moveKoma(moveList[cnt].op, moveList[cnt].np, turn, moveList[cnt].nari, true);

                        // 同一局面がすでに出ている場合
                        //lock (lockObj_hash) {
                        //    if (chkHash(tmp_ban.hash, depth, out int reth, out kmove[] retm) > 0) {
                        //        if (reth > best) {
                        //            best = reth;
                        //            bestMoveList = retm;
                        //            bestMoveList[depth] = moveList[cnt];
                        //            if (best > alpha) {
                        //                alpha = best;
                        //            }
                        //            if (best >= beta) {
                        //                lock (lockObj) {
                        //                    mList.freeAlist(aid);
                        //                }
                        //                return best;
                        //            }
                        //        }
                        //        continue;
                        //    }
                        //}

                        // 王手はスキップ
                        if (((byte)tmp_ban.data[((int)turn << 6) + ban.setOu] != 0xFF) &&
                            ((tmp_ban.data[tmp_ban.data[((int)turn << 6) + ban.setOu] & 0xFF] >> (8 + ((int)pturn.aturn(turn) << 2)) & 0x0F) > 0)) {
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
                    //if (depth < 2) {
                    //    lock (lockObj_hash) {
                    //        addHash(bestHash, depth, best, bestMoveList);
                    //    }
                    //}

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
                if ((byte)ban.data[((int)turn << 6) + ban.setOu] != 0xFF) {
                    getEachMoveList(ref ban, (byte)ban.data[((int)turn << 6) + ban.setOu] & 0xFF, turn, emv, kmv, ref kCnt, ref startPoint);
                }

                // 歩兵
                for (int i = 0; i < 9; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setFu + (i >> 2)] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setFu + (i >> 2)] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        if ((ban.data[ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3) & 0xFF] & (pturn.aturn((int)turn) << 4) + 8) > 0) { //敵の効きがある
                            getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                        } else {
                            getEachMoveListKyousya(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3)), turn, emv.val[0], kmv, ref kCnt, ref startPoint);
                        }
                    }
                }

                // 桂馬
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKei] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKei] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setGin] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setGin] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setHi] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setHi] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKa] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKa] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 金将
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKin] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKin] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
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
                if ((byte)ban.data[((int)turn << 6) + ban.setOu] != 0xFF) {
                    getEachMoveList(ref ban, (byte)ban.data[((int)turn << 6) + ban.setOu], turn, emv, kmv, ref kCnt, ref startPoint);
                }

                // 歩兵
                for (int i = 0; i < 9; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setFu + (i >> 2)] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        //DebugForm.instance.addMsg("aList:" + (ban.data[((int)turn << 6) + ban.setFu + (i >> 2)] >> ((i & 3) << 3) & 0xFF).ToString("X2"));
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setFu + (i >> 2)] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        if ((ban.data[ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3) & 0xFF] & (pturn.aturn((int)turn) << 4) + 8) > 0) { //敵の効きがある
                            getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                        } else {
                            getEachMoveListKyousya(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3)), turn, emv.val[0], kmv, ref kCnt, ref startPoint);
                        }
                    }
                }

                // 桂馬
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKei] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKei] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setGin] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setGin] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setHi] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setHi] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKa] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKa] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 金将
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKin] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setKin] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                    }
                }

                // 成駒
                for (int i = 0, j = 0; j < ban.data[((int)turn << 6) + ban.setNaNum] && i < 28; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setNa + (i >> 2)] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        getEachMoveList(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setNa + (i >> 2)] >> ((i & 3) << 3)), turn, emv, kmv, ref kCnt, ref startPoint);
                        j++;
                    }
                }

                // 駒打ち
                if (type < 3) {
                    for (int i = 1; i < 8; i++) {
                        if (ban.data[((int)turn << 6) + ban.hand + i] > 0) {
                            getEachMoveList(ref ban, (byte)(0x90 + i), turn, emv, kmv, ref kCnt, ref startPoint, type);
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
            return;
            //int cnt = 0;
            //unsafe {
            //    // 王将
            //    if ((ban.putOusyou[turn] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putOusyou[turn]] > 0)) {
            //        emv.pos[cnt] = ban.putOusyou[turn];
            //        emv.val[cnt++] = kVal[(int)ktype.Ousyou];
            //        if (cnt > 1) return;
            //    }
            //
            //    // 飛車
            //    for (int i = 0; i < 2; i++) {
            //        if ((ban.putHisya[turn * 2 + i] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putHisya[turn * 2 + i]] > 0)) {
            //            emv.pos[cnt] = ban.putHisya[turn * 2 + i];
            //            emv.val[cnt++] = kVal[(int)ktype.Hisya];
            //            if (cnt > 1) return;
            //        }
            //    }
            //
            //    // 角行
            //    for (int i = 0; i < 2; i++) {
            //        if ((ban.putKakugyou[(int)turn * 2 + i] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putKakugyou[turn * 2 + i]] > 0)) {
            //            emv.pos[cnt] = ban.putKakugyou[turn * 2 + i];
            //            emv.val[cnt++] = kVal[(int)ktype.Kakugyou];
            //            if (cnt > 1) return;
            //        }
            //    }
            //
            //    // 金将
            //    for (int i = 0; i < 4; i++) {
            //        if ((ban.putKinsyou[(int)turn * 4 + i] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putKinsyou[(int)turn * 4 + i]] > 0)) {
            //            emv.pos[cnt] = ban.putKinsyou[(int)turn * 4 + i];
            //            emv.val[cnt++] = kVal[(int)ktype.Kinsyou];
            //            if (cnt > 1) return;
            //        }
            //    }
            //
            //    // 銀将
            //    for (int i = 0; i < 4; i++) {
            //        if ((ban.putGinsyou[(int)turn * 4 + i] != 0xFF) && (ban.moveable[pturn.aturn(turn) * 81 + ban.putGinsyou[(int)turn * 4 + i]] > 0)) {
            //            emv.pos[cnt] = ban.putGinsyou[(int)turn * 4 + i];
            //            emv.val[cnt++] = kVal[(int)ktype.Ginsyou];
            //            if (cnt > 1) return;
            //        }
            //    }
            //
            //}


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
        public void getEachMoveListKyousya(ref ban ban, byte oPos, Pturn turn, int eVal, kmove[] kmv, ref int kCnt, ref int startPoint) {
            unsafe {
                for (int i = 1; i < 9; ++i) {
                    byte nPos = pturn.mv(turn, oPos, 0x00 + i);
                    if ((nPos > 0x90) || ((nPos & 0x0F) > 8)) return;
                    if ((ban.getOnBoardKtype(nPos) > ktype.None) || (pturn.psY(turn, nPos & 0x0F) > 5)) {
                        getEachMovePos(ref ban, oPos, i, turn, eVal, kmv, ref kCnt, ref startPoint);
                    }
                    if (ban.getOnBoardKtype(nPos) > ktype.None) return;
                }
            }
        }

        public void getEachMoveList(ref ban ban, int oPos, Pturn turn, emove emv, kmove[] kmv, ref int kCnt, ref int startPoint, int type = 0) {
            unsafe {
                int cnt = 0;
                if (emv.pos[0] == oPos) cnt = 1;

                if (oPos > 0x90) { // (oPos / 9) == 9 駒打ち
                    for (byte i = 0; i < 9; i++) {

                        // 二歩は打てない
                        if (((oPos & 0x0F) == (int)ktype.Fuhyou) && (ban.data[((int)turn << 6) + ban.setFu + (i >> 2)] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                            continue;
                        }
                        for (byte j = 0; j < 9; j++) {
                            // 1段目には打てない
                            if ((((oPos & 0x0F) == (int)ktype.Fuhyou) || ((oPos & 0x0F) == (int)ktype.Kyousha)) && (pturn.psX(turn, j) > 7)) {
                                continue;
                                // 2段目には打てない
                            } else if (((oPos & 0x0F) == (int)ktype.Keima) && (pturn.psX(turn, j) > 6)) {
                                continue;
                            }
                            // 駒があると打てない
                            if (ban.getOnBoardKtype((i << 4) + j) > ktype.None) {
                                continue;
                            }

                            //敵の効きのみある所には打たない
                            if (((oPos & 0x0F) != (int)ktype.Fuhyou) && ((ban.data[(i << 4) + j] & (pturn.aturn((int)turn) << 4) + 8) > 0) && ((ban.data[(i << 4) + j] & ((int)turn << 4) + 8) == 0)) {
                                continue;
                            }

                            // 歩飛角以外の駒で、移動先に敵味方の駒がない場合、無駄なため置かない
                            switch (oPos & 0x0F) {
                                case (int)ktype.Kyousha:
                                    int kret = 0;
                                    for (int k = 1; k < 9; k++) {
                                        int ny = pturn.mvY(turn, j, k);
                                        if ((ny < 0) || (ny > 8)) break;
                                        if (ban.getOnBoardKtype((i << 4) + ny) > ktype.None) {
                                            kret = 1;
                                            break;
                                        }
                                    }
                                    if (kret == 0) continue;
                                    break;

                                case (int)ktype.Keima:
                                    if ((chkMoveable(ref ban, (byte)((i << 4) + j), 0x12, turn) < 1) && (chkMoveable(ref ban, (byte)((i << 4) + j), -0x10 + 0x02, turn) < 1)) continue;
                                    break;

                                case (int)ktype.Ginsyou:
                                    if ((chkMoveable(ref ban, (byte)((i << 4) + j), 0x10 + 0x01, turn) < 1) && (chkMoveable(ref ban, (byte)((i << 4) + j), 0x00 + 0x01, turn) < 1) &&
                                        (chkMoveable(ref ban, (byte)((i << 4) + j), -0x10 + 0x01, turn) < 1) && (chkMoveable(ref ban, (byte)((i << 4) + j), 0x10 - 0x01, turn) < 1) && (chkMoveable(ref ban, (byte)((i << 4) + j), -0x10 - 0x01, turn) < 1)) continue;
                                    break;

                                case (int)ktype.Kinsyou:
                                    if ((chkMoveable(ref ban, (byte)((i << 4) + j), 0x10 + 0x01, turn) < 1) && (chkMoveable(ref ban, (byte)((i << 4) + j), 0x00 + 0x01, turn) < 1) && (chkMoveable(ref ban, (byte)((i << 4) + j), -0x10 + 0x01, turn) < 1) &&
                                         (chkMoveable(ref ban, (byte)((i << 4) + j), 0x10 + 0x00, turn) < 1) && (chkMoveable(ref ban, (byte)((i << 4) + j), -0x10 + 0x00, turn) < 1) && (chkMoveable(ref ban, (byte)((i << 4) + j), 0x00 - 0x10, turn) < 1)) continue;
                                    break;

                                default:
                                    break;
                            }

                            // リストに追加
                            if (-emv.val[0] > kmv[startPoint].val) {
                                kmv[--startPoint].set((byte)oPos, (byte)((i << 4) + j), 0, emv.val[0], false, turn);
                                kCnt++;
                            } else {
                                kmv[startPoint + kCnt++].set((byte)oPos, (byte)((i << 4) + j), 0, emv.val[0], false, turn);
                            }
                        }
                    }
                } else {  // 駒移動

                    switch (ban.getOnBoardKtype(oPos)) {
                        case ktype.Fuhyou:
                            getEachMovePos(ref ban, (byte)oPos, 0x00 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Kyousha:
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, 0x00 + i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Keima:
                            getEachMovePos(ref ban, (byte)oPos, 0x10 + 0x02, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 + 0x02, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ginsyou:
                            getEachMovePos(ref ban, (byte)oPos, 0x10 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x00 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x10 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Hisya:
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, 0x00 + i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, 0x00 - i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, i << 4, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, -(i << 4), turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Kakugyou:
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, (i << 4) + i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, (i << 4) - i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, -(i << 4) + i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, -(i << 4) - i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Kinsyou:
                        case ktype.Tokin:
                        case ktype.Narikyou:
                        case ktype.Narikei:
                        case ktype.Narigin:
                            getEachMovePos(ref ban, (byte)oPos, 0x10 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x00 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x10 + 0x00, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 + 0x00, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x00 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ousyou:
                            getEachMovePos(ref ban, (byte)oPos, 0x10 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x00 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x10 + 0x00, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 + 0x00, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x10 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x00 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            break;

                        case ktype.Ryuuou:
                            getEachMovePos(ref ban, (byte)oPos, 0x10 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x10 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, 0x00 + i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, 0x00 - i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, i << 4, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, -(i << 4), turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        case ktype.Ryuuma:
                            getEachMovePos(ref ban, (byte)oPos, 0x00 + 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x10 + 0x00, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, -0x10 + 0x00, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            getEachMovePos(ref ban, (byte)oPos, 0x00 - 0x01, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint);
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, (i << 4) + i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, (i << 4) - i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, -(i << 4) + i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            for (int i = 1; getEachMovePos(ref ban, (byte)oPos, -(i << 4) - i, turn, emv.val[cnt], kmv, ref kCnt, ref startPoint) < 1; i++) ;
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 指定移動先への移動リスト登録
        /// </summary>
        /// <param name="ban"></param>
        /// <param name="oPos"></param>
        /// <param name="mPos"></param>
        /// <param name="turn"></param>
        /// <param name="eVal"></param>
        /// <param name="kmv"></param>
        /// <param name="kCnt"></param>
        /// <param name="startPoint"></param>
        /// <returns>移動可 0 / 移動可(敵駒取り) 1 / 移動不可(味方駒) 2 / 移動不可 3(範囲外)</returns>
        public int getEachMovePos(ref ban ban, byte oPos, int mPos, Pturn turn, int eVal, kmove[] kmv, ref int kCnt, ref int startPoint) {
            unsafe {
                int val;
                byte nPos = (byte)pturn.mv(turn, oPos, mPos);
                if ((nPos > 0x88) || ((nPos & 0xF) > 0x08)) return 3; // 範囲外(移動できない)
                int sval = tw2stval.get(ban.getOnBoardKtype((byte)oPos), (byte)oPos, nPos, turn);
                //現在の敵の取得候補より価値が高い場合、自分が取得候補となる
                if (((ban.data[nPos] & (pturn.aturn((int)turn) << 4) + 8) > 0) && (kVal[(int)ban.getOnBoardKtype((byte)oPos)] > eVal)) {
                    eVal = tw2ai.kVal[(int)ban.getOnBoardKtype((byte)oPos)];
                }
                if (ban.getOnBoardKtype(nPos) > ktype.None) { //駒が存在
                    if (ban.getOnBoardPturn(nPos) != turn) {
                        val = kVal[(int)ban.getOnBoardKtype(nPos)] + sval;
                        if ((((pturn.ps(turn, oPos) & 0x0F) > 5) || ((pturn.ps(turn, nPos) & 0x0F) > 5)) && ((int)ban.getOnBoardKtype(oPos) < 7)) {

                            // 不成(歩は対象外)
                            if ((ban.getOnBoardKtype((byte)oPos) == ktype.Ginsyou) || ((ban.getOnBoardKtype((byte)oPos) == ktype.Kyousha) && ((pturn.ps(turn, nPos) & 0x0F) < 8)) || ((ban.getOnBoardKtype((byte)oPos) == ktype.Keima) && ((pturn.ps(turn, nPos) & 0x0F) < 7))) {
                                if (val - eVal >= kmv[startPoint].val - kmv[startPoint].aval) {
                                    kmv[--startPoint].set(oPos, nPos, val, eVal, false, turn);
                                    kCnt++;
                                } else {
                                    kmv[startPoint + kCnt++].set(oPos, nPos, val, eVal, false, turn);
                                }
                            }

                            // 成り
                            if (val + 250 - eVal >= kmv[startPoint].val - kmv[startPoint].aval) {
                                kmv[--startPoint].set(oPos, nPos, val + 250, eVal, true, turn);
                                kCnt++;
                            } else {
                                kmv[startPoint + kCnt++].set(oPos, nPos, val + 250, eVal, true, turn);
                            }

                        } else {

                            if (val - eVal >= kmv[startPoint].val - kmv[startPoint].aval) {
                                kmv[--startPoint].set(oPos, nPos, val, eVal, false, turn);
                                kCnt++;
                            } else {
                                kmv[startPoint + kCnt++].set(oPos, nPos, val, eVal, false, turn);
                            }

                        }

                        return 1; // 敵の駒(取れる)
                    } else {
                        return 2; // 味方の駒(取れない)
                    }

                } else { //駒がない
                    if ((((pturn.ps(turn, oPos) & 0x0F) > 5) || ((pturn.ps(turn, nPos) & 0x0F) > 5)) && ((int)ban.getOnBoardKtype(oPos) < 7)) {

                        // 不成(歩は対象外)
                        if ((ban.getOnBoardKtype(oPos) == ktype.Ginsyou) || ((ban.getOnBoardKtype((byte)oPos) == ktype.Kyousha) && ((pturn.ps(turn, nPos) & 0x0F) < 8)) || ((ban.getOnBoardKtype(oPos) == ktype.Keima) && ((pturn.ps(turn, nPos) & 0x0F) < 7))) {
                            kmv[startPoint + kCnt++].set(oPos, nPos, sval, eVal, false, turn);
                        }
                        // 自分の効きが敵より多い
                        if ((ban.data[nPos] >> (((int)turn << 2) + 8) & 0x0F) > (ban.data[nPos] >> ((pturn.aturn((int)turn) << 2) + 8) & 0x0F)) {
                            kmv[startPoint + kCnt++].set(oPos, nPos, 250 + sval, eVal, true, turn); // 成りボーナス
                        } else {
                            kmv[startPoint + kCnt++].set(oPos, nPos, sval, eVal, true, turn);
                        }
                    } else {
                        if (sval - eVal >= kmv[startPoint].val - kmv[startPoint].aval) {
                            kmv[--startPoint].set(oPos, nPos, sval, eVal, false, turn);
                            kCnt++;
                        } else {
                            kmv[startPoint + kCnt++].set(oPos, nPos, sval, eVal, false, turn);
                        }
                    }
                    return 0; // 駒がない
                }
            }
        }

        /// <summary>
        /// 指定移動先(mPos)に移動できるかチェック
        /// </summary>
        /// <param name="ban">盤情報</param>
        /// <param name="oPos">現在位置[絶対位置]</param>
        /// <param name="mPos">移動先[自分中心]</param>
        /// <param name="turn">手番</param>
        /// <returns>-1:範囲外/0:駒なし/1:敵駒/2:味方駒</returns>
        public int chkMoveable(ref ban ban, uint oPos, int mPos, Pturn turn) {
            unsafe {
                byte nPos = (byte)pturn.mv(turn, (byte)oPos, mPos);
                if ((nPos > 0x88) || ((nPos & 0xF) > 0x08)) return -1; // 範囲外(移動できない)
                if (ban.getOnBoardKtype(nPos) > ktype.None) {
                    if (ban.getOnBoardPturn(nPos) != turn) {
                        return 1; // 敵の駒(取れる)
                    } else {
                        return 2; // 味方の駒(移動できない)
                    }
                }
            }
            return 0; // 駒がない(移動可能)
        }

    }
}
