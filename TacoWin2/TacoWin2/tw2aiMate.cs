using System.Threading.Tasks;
using TacoWin2_BanInfo;

namespace TacoWin2 {
    partial class tw2ai {

        public enum mType : byte {
            NoNari = 0x00, //不成・成済
            Both = 0x01,   //両方
            Nari = 0x02,   //成
        }

        public (kmove[], int) thinkMateMoveTest(Pturn turn, ban ban, int depth) {
            kmove[] moveList = null;
            int aid = mList.assignAlist(out moveList);
            int vla = getAllCheckList(ref ban, turn, moveList);

            for (int i = 0; i < vla; i++) {
                if (moveList[i].nari == true) {
                    DebugForm.instance.addMsg("MV: " + (moveList[i].op + 0x11).ToString("X2") + "-" + (moveList[i].np + 0x11).ToString("X2") + "*");
                } else {
                    DebugForm.instance.addMsg("MV: " + (moveList[i].op + 0x11).ToString("X2") + "-" + (moveList[i].np + 0x11).ToString("X2"));
                }
            }
            mList.freeAlist(aid);

            DebugForm.instance.addMsg("checkmate nomate");
            return (null, 0);
        }

        /// <summary>
        /// 詰将棋思考メイン
        /// </summary>
        /// <param name="turn">手番</param>
        /// <param name="ban">盤情報</param>
        /// <param name="depth">深さ</param>
        /// <returns>kmove[] 詰め手筋 int 999 手筋なし 1-詰め数</returns>
        public (kmove[], int) thinkMateMove(Pturn turn, ban ban, int depth) {
            int best = 999;
            mateDepMax = depth;

            kmove[] bestmove = null;

            int teCnt = 0; //手の進捗

            unsafe {

                int aid = mList.assignAlist(out kmove[] moveList);

                //[攻め方]王手を指せる手を全てリスト追加
                int vla = getAllCheckList(ref ban, turn, moveList);

                Parallel.For(0, workMin, id => {
                    int cnt_local;

                    while (true) {

                        lock (lockObj) {
                            if (vla <= teCnt) break;
                            cnt_local = teCnt;
                            teCnt++;
                        }

                        // debug
                        if (moveList[cnt_local].nari == true) {
                            DebugForm.instance.addMsg("MVV:" + (moveList[cnt_local].op + 0x11).ToString("X2") + "-" + (moveList[cnt_local].np + 0x11).ToString("X2") + "*," + moveList[cnt_local].val);
                        } else {
                            DebugForm.instance.addMsg("MVV:" + (moveList[cnt_local].op + 0x11).ToString("X2") + "-" + (moveList[cnt_local].np + 0x11).ToString("X2") + "," + moveList[cnt_local].val);
                        }

                        // 駒移動
                        ban tmp_ban = ban;
                        int val = depth;
                        int retVal;
                        kmove[] retList = null;

                        //駒を動かす
                        tmp_ban.moveKoma(moveList[cnt_local].op, moveList[cnt_local].np, turn, moveList[cnt_local].nari, true);

                        // 王手はスキップ
                        if (((byte)tmp_ban.data[((int)turn << 6) + ban.setOu] != 0xFF) &&
                        ((tmp_ban.data[tmp_ban.data[((int)turn << 6) + ban.setOu] & 0xFF] >> (8 + ((int)pturn.aturn(turn) << 2)) & 0x0F) > 0)) {
                            retVal = 0;
                        } else {
                            retVal = thinkMateDef(pturn.aturn(turn), ref tmp_ban, (byte)moveList[cnt_local].val, out retList, 1, mateDepMax);

                            /* 打ち歩詰めチェック */
                            if ((retVal == 1) && (moveList[cnt_local].op == 0x91)) { /* 9*9+(int)ktype.Fuhyou */
                                continue;
                            };

                            retList[0] = moveList[cnt_local];
                            string str = "";
                            int i;
                            for (i = 0; retList[i].op > 0 || retList[i].np > 0; i++) {
                                if (retList[i].nari == true) {
                                    str += "/" + (retList[i].op + 0x11).ToString("X2") + "-" + (retList[i].np + 0x11).ToString("X2") + "*:" + retList[i].val.ToString("X2");
                                } else {
                                    str += "/" + (retList[i].op + 0x11).ToString("X2") + "-" + (retList[i].np + 0x11).ToString("X2") + ":" + retList[i].val.ToString("X2");
                                }
                            }
                            if ((retVal < 999) && (i < mateDepMax)) mateDepMax = i;

                            DebugForm.instance.addMsg("TASK[" + Task.CurrentId + ":" + cnt_local + "]MV[" + retVal + "]" + str);


                            lock (lockObj) {
                                if (retVal < best) {
                                    best = retVal;
                                    bestmove = retList;
                                }
                            }
                        }
                    }
                });

                mList.freeAlist(aid);
            }

            DebugForm.instance.addMsg("FIN:" + best);

            return (bestmove, best);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="turn"></param>
        /// <param name="ban"></param>
        /// <param name="cPos">王手をしている手(-1の場合は空き王手+移動->王を移動する必要あり)</param>
        /// <param name="bestMoveList"></param>
        /// <param name="depth"></param>
        /// <param name="depMax"></param>
        /// <returns> 0<:詰め数 / 999:手筋なし</returns>
        public int thinkMateDef(Pturn turn, ref ban ban, byte cPos, out kmove[] bestMoveList, int depth, int depMax) {

            int best = 0;
            bestMoveList = null;
            int teNum = 0;

            if (stopFlg) {
                bestMoveList = new kmove[30];
                return 0;
            }

            unsafe {

                kmove[] moveList = null;
                int aid;
                lock (lockObj) {
                    aid = mList.assignAlist(out moveList);
                }
                teNum = getAllDefList(ref ban, turn, moveList, cPos);
                for (int cnt = 0; cnt < teNum; cnt++) {

                    kmove[] retList = null;

                    // 駒を動かす
                    ban tmp_ban = ban;
                    tmp_ban.moveKoma(moveList[cnt].op, moveList[cnt].np, turn, moveList[cnt].nari, true);

                    // 王手が解けない
                    if (((byte)tmp_ban.data[((int)turn << 6) + ban.setOu] != 0xFF) &&
                        ((tmp_ban.data[tmp_ban.data[((int)turn << 6) + ban.setOu] & 0xFF] >> (8 + ((int)pturn.aturn(turn) << 2)) & 0x0F) > 0)) {
                        // なにもしない
                    } else {
                        int retVal = thinkMateAtk(pturn.aturn(turn), ref tmp_ban, out retList, depth + 1, depMax);
                        // 詰みがない手が1つ以上あった
                        if (retVal >= 999) {
                            bestMoveList = retList;
                            bestMoveList[depth] = moveList[cnt];
                            lock (lockObj) {
                                mList.freeAlist(aid);
                            }
                            return retVal;
                        }

                        if (retVal > best) {
                            best = retVal;
                            bestMoveList = retList;
                            bestMoveList[depth] = moveList[cnt];
                        }
                    }
                }

                lock (lockObj) {
                    mList.freeAlist(aid);
                }

                //ここで詰んだ
                if (best == 0) {
                    bestMoveList = new kmove[30];
                    best = depth;
                }
                return best;
            }
        }

        /// <summary>
        /// [詰将棋]攻め手側思考メイン
        /// </summary>
        /// <param name="turn"></param>
        /// <param name="ban"></param>
        /// <param name="bestMoveList"></param>
        /// <param name="depth"></param>
        /// <param name="depMax"></param>
        /// <returns></returns>
        public int thinkMateAtk(Pturn turn, ref ban ban, out kmove[] bestMoveList, int depth, int depMax) {
            bestMoveList = null;
            int best = 999;
            kmove[] retList;

            if (stopFlg) {
                bestMoveList = new kmove[30];
                return 0;
            }

            unsafe {

                kmove[] moveList;
                int aid;
                lock (lockObj) {
                    aid = mList.assignAlist(out moveList);
                }

                //[攻め方]王手を指せる手を全てリスト追加
                if ((depth < mateDepMax) && (depth < depMax)) {// 
                                                               //[攻め方]王手を指せる手を全てリスト追加
                    int vla = getAllCheckList(ref ban, turn, moveList);
                    for (int cnt = 0; cnt < vla; cnt++) {

                        //駒を動かす
                        ban tmp_ban = ban;
                        tmp_ban.moveKoma(moveList[cnt].op, moveList[cnt].np, turn, moveList[cnt].nari, true);

                        // 自分の駒が王手の場合はNG
                        if (((byte)tmp_ban.data[((int)turn << 6) + ban.setOu] != 0xFF) &&
                        ((tmp_ban.data[tmp_ban.data[((int)turn << 6) + ban.setOu] & 0xFF] >> (8 + ((int)pturn.aturn(turn) << 2)) & 0x0F) > 0)) {
                            if (bestMoveList == null) {
                                bestMoveList = new kmove[30];
                            }
                            continue;
                        }
                        int retVal = thinkMateDef(pturn.aturn(turn), ref tmp_ban, (byte)moveList[cnt].val, out retList, depth + 1, best);

                        /* 打ち歩詰めチェック */
                        if ((depth + 1 == retVal) && (moveList[cnt].op == 0x91)) { /* 9*9+(int)ktype.Fuhyou */
                            continue;
                        };

                        if (retVal < best) {
                            best = retVal;
                            bestMoveList = retList;
                            bestMoveList[depth] = moveList[cnt];
                        }
                    }

                } else {
                    /* なにもしない */
                }
                lock (lockObj) {
                    mList.freeAlist(aid);
                }
                if (bestMoveList == null) bestMoveList = new kmove[30];
                return best;
            }
        }

        //指定した位置に次に移動可能となる
        int getAllCheckList(ref ban ban, Pturn turn, kmove[] kmv) {
            int kCnt = 0;
            emove emv = new emove();
            unsafe {
                byte bPos;  // 攻め駒の位置
                byte aOpos = (byte)ban.data[(pturn.aturn((int)turn) << 6) + ban.setOu];  //相手王将の位置

                //香車・飛車・角以外の駒の動きチェック
                //01 02 03 04 05
                //06 07 08 09 10
                //11 12 ● 13 14
                //15 16 17 18 19
                //20 21 22 23 24
                //25 26 27 28 29
                //30 × 31 × 32

                // 01
                if ((pturn.ps(turn, aOpos) & 0x0F) < 7) {
                    bPos = (byte)pturn.mv(turn, aOpos, -0x20 + 0x02);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x10) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }

                    // 02
                    bPos = (byte)pturn.mv(turn, aOpos, -0x10 + 0x02);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x00) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 03
                    bPos = (byte)pturn.mv(turn, aOpos, 0x00 + 0x02);
                    if (ban.getOnBoardPturn(bPos) == turn) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 04
                    bPos = (byte)pturn.mv(turn, aOpos, 0x10 + 0x02);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x80) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 05
                    bPos = (byte)pturn.mv(turn, aOpos, 0x20 + 0x02);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x70) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }
                }

                if ((pturn.ps(turn, aOpos) & 0x0F) < 8) {
                    // 06
                    bPos = (byte)pturn.mv(turn, aOpos, -0x20 + 0x01);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x10) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 07
                    bPos = (byte)pturn.mv(turn, aOpos, -0x10 + 0x01);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x00) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 08
                    bPos = (byte)pturn.mv(turn, aOpos, 0x00 + 0x01);
                    if (ban.getOnBoardPturn(bPos) == turn) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                            addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 09
                    bPos = (byte)pturn.mv(turn, aOpos, 0x10 + 0x01);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x80) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 10
                    bPos = (byte)pturn.mv(turn, aOpos, 0x20 + 0x01);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x70) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }
                }

                // 11
                bPos = (byte)pturn.mv(turn, aOpos, -0x20 + 0x00);
                if (((pturn.ps(turn, aOpos) & 0xF0) > 0x10) && (ban.getOnBoardPturn(bPos) == turn)) {
                    switch (ban.getOnBoardKtype(bPos)) {
                        case (ktype.Ginsyou): //銀将
                            addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.Both, kmv, ref kCnt);
                            addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        case (ktype.Kinsyou):  //金将
                        case (ktype.Tokin):    //と金
                        case (ktype.Narikyou): //成香
                        case (ktype.Narikei):  //成桂
                        case (ktype.Narigin):  //成銀
                            addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        default:
                            break;
                    }
                }

                // 12
                bPos = (byte)pturn.mv(turn, aOpos, -0x10 + 0x00);
                if (((pturn.ps(turn, aOpos) & 0xF0) > 0x00) && (ban.getOnBoardPturn(bPos) == turn)) {
                    if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                        addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.Both, kmv, ref kCnt);
                        addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                        addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                    }
                }

                // 13
                bPos = (byte)pturn.mv(turn, aOpos, 0x10 + 0x00);
                if (((pturn.ps(turn, aOpos) & 0xF0) < 0x80) && (ban.getOnBoardPturn(bPos) == turn)) {
                    if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                        addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.Both, kmv, ref kCnt);
                        addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                        addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                    }
                }

                // 14
                bPos = (byte)pturn.mv(turn, aOpos, 0x20 + 0x00);
                if (((pturn.ps(turn, aOpos) & 0xF0) < 0x70) && (ban.getOnBoardPturn(bPos) == turn)) {
                    switch (ban.getOnBoardKtype(bPos)) {
                        case (ktype.Ginsyou): //銀将
                            addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.Both, kmv, ref kCnt);
                            addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        case (ktype.Kinsyou):  //金将
                        case (ktype.Tokin):    //と金
                        case (ktype.Narikyou): //成香
                        case (ktype.Narikei):  //成桂
                        case (ktype.Narigin):  //成銀
                            addCheckMovePos(ref ban, bPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        default:
                            break;
                    }
                }

                if ((pturn.ps(turn, aOpos) & 0x0F) > 0) {
                    // 15
                    bPos = (byte)pturn.mv(turn, aOpos, -0x20 - 0x01);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x10) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 16
                    bPos = (byte)pturn.mv(turn, aOpos, -0x10 - 0x01);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x00) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 17(該当なし)
                    //AposX = aOuPosX;
                    //if ((ban.onBoard[bPos] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                    //    if (ban.getOnBoardKtype(bPos) == ktype.Ginsyou) { //銀将のみ
                    //        addCheckMovePos(ref ban, bPos, 1, -1, turn, mType.Nari, kmv, ref kCnt);
                    //        addCheckMovePos(ref ban, bPos, -1, -1, turn, mType.Nari, kmv, ref kCnt);
                    //    }
                    //}

                    // 18
                    bPos = (byte)pturn.mv(turn, aOpos, 0x10 - 0x01);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x80) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 19
                    bPos = (byte)pturn.mv(turn, aOpos, 0x20 - 0x01);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x70) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if ((pturn.ps(turn, aOpos) & 0x0F) > 1) {
                    // 20
                    bPos = (byte)pturn.mv(turn, aOpos, -0x20 - 0x02);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x10) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 21
                    bPos = (byte)pturn.mv(turn, aOpos, -0x10 - 0x02);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x00) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 22
                    bPos = (byte)pturn.mv(turn, aOpos, 0x00 - 0x02);
                    if (ban.getOnBoardPturn(bPos) == turn) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 23
                    bPos = (byte)pturn.mv(turn, aOpos, 0x10 - 0x02);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x80) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 24
                    bPos = (byte)pturn.mv(turn, aOpos, 0x20 - 0x02);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x70) && (ban.getOnBoardPturn(bPos) == turn)) {
                        switch (ban.getOnBoardKtype(bPos)) {
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if ((pturn.ps(turn, aOpos) & 0x0F) > 2) {
                    // 25
                    bPos = (byte)pturn.mv(turn, aOpos, -0x20 - 0x03);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x10) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, bPos, 0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 26
                    bPos = (byte)pturn.mv(turn, aOpos, -0x10 - 0x03);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x00) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, bPos, 0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 27
                    bPos = (byte)pturn.mv(turn, aOpos, 0x00 - 0x03);
                    if (ban.getOnBoardPturn(bPos) == turn) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, bPos, 0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                            addCheckMovePos(ref ban, bPos, -0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 28
                    bPos = (byte)pturn.mv(turn, aOpos, 0x10 - 0x03);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x80) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, bPos, -0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 29
                    bPos = (byte)pturn.mv(turn, aOpos, 0x20 - 0x03);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x70) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, bPos, -0x10 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }
                }

                if ((pturn.ps(turn, aOpos) & 0x0F) > 3) {
                    // 30
                    bPos = (byte)pturn.mv(turn, aOpos, -0x20 - 0x04);
                    if (((pturn.ps(turn, aOpos) & 0xF0) > 0x10) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, bPos, 0x10 + 0x02, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }

                    // 31
                    bPos = (byte)pturn.mv(turn, aOpos, 0x00 - 0x04);
                    if (ban.getOnBoardPturn(bPos) == turn) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, bPos, 0x10 + 0x02, turn, mType.NoNari, kmv, ref kCnt);
                            addCheckMovePos(ref ban, bPos, -0x10 + 0x02, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }

                    // 32
                    bPos = (byte)pturn.mv(turn, aOpos, 0x20 - 0x04);
                    if (((pturn.ps(turn, aOpos) & 0xF0) < 0x70) && (ban.getOnBoardPturn(bPos) == turn)) {
                        if (ban.getOnBoardKtype(bPos) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, bPos, -0x10 + 0x02, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        bPos = (byte)(ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3) & 0xFF);
                        int dx = pturn.dx(turn, bPos, aOpos);
                        int dy;
                        int ret;
                        byte tPos;
                        switch (dx) {
                            case (0x00):
                                // [不成]敵を取って直進
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, 0x01);
                                if (ret >= 0) {
                                    dy = pturn.dy(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, 0x00 - dy, turn, mType.NoNari, kmv, ref kCnt);
                                        if (pturn.dy(turn, (byte)ret, aOpos) == -1) {//一つ手前
                                            addCheckMovePos(ref ban, bPos, 0x00 - dy, turn, mType.Nari, kmv, ref kCnt);
                                        }
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret & 0xF0) != (_kmv[j].np & 0xF0)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                                break;

                            case (-0x10):

                                tPos = pturn.mv(turn, aOpos, -0x10);
                                ret = chkRectMove(ref ban, turn, bPos, tPos, 0x01);
                                if ((ret == pturn.mv(turn, tPos, -0x01)) && (ban.getOnBoardPturn(ret) != turn)) {// 王の横手前
                                    dy = pturn.dy(turn, bPos, (byte)ret);
                                    addCheckMovePos(ref ban, bPos, 0x00 - dy, turn, mType.Nari, kmv, ref kCnt);
                                } else if (ret == -1) {// 王の横手前
                                    dy = pturn.dy(turn, bPos, tPos) - 0x01;
                                    addCheckMovePos(ref ban, bPos, 0x00 - dy, turn, mType.Nari, kmv, ref kCnt);
                                    if ((ban.getOnBoardKtype(tPos) == ktype.None) || (ban.getOnBoardPturn(tPos) != turn)) { // 王の横
                                        addCheckMovePos(ref ban, bPos, 0x00 - dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                }

                                break;

                            case (0x10):
                                tPos = pturn.mv(turn, aOpos, 0x10);
                                ret = chkRectMove(ref ban, turn, bPos, tPos, 0x01);
                                if ((ret == pturn.mv(turn, tPos, -0x01)) && (ban.getOnBoardPturn(ret) != turn)) {// 王の横手前
                                    dy = pturn.dy(turn, bPos, (byte)ret);
                                    addCheckMovePos(ref ban, bPos, 0x00 - dy, turn, mType.Nari, kmv, ref kCnt);
                                } else if (ret == -1) {// 王の横手前
                                    dy = pturn.dy(turn, bPos, tPos) - 0x01;
                                    addCheckMovePos(ref ban, bPos, 0x00 - dy, turn, mType.Nari, kmv, ref kCnt);
                                    if ((ban.getOnBoardKtype(tPos) == ktype.None) || (ban.getOnBoardPturn(tPos) != turn)) { // 王の横
                                        addCheckMovePos(ref ban, bPos, 0x00 - dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                }

                                break;

                            default:
                                break;
                        }
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setHi] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        bPos = (byte)(ban.data[((int)turn << 6) + ban.setHi] >> ((i & 3) << 3) & 0xFF);
                        int dx = pturn.dx(turn, bPos, aOpos);
                        int dy = pturn.dy(turn, bPos, aOpos);
                        int ret;
                        if (dx == 0) {// 同じ筋
                                      // [不成&成り]敵を取って直進
                            if (dy < 0) { // 前方
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, 0x00 + 0x01);
                                if (ret >= 0) {
                                    dy = pturn.dy(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, 0x00 - dy, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret & 0xF0) != (_kmv[j].np & 0xF0)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            } else { // 後方
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, 0x00 - 0x01);
                                if (ret >= 0) {
                                    dy = pturn.dy(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, 0x00 - dy, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret & 0xF0) != (_kmv[j].np & 0xF0)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            }

                        } else if (dy == 0) {//同じ段
                                             // [不成&成り]敵を取って直進
                            if (dx < 0) { // 左
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, 0x10 + 0x00);
                                if (ret >= 0) {
                                    dx = pturn.dx(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, (-dx << 4) + 0x00, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret & 0x0F) != (_kmv[j].np & 0x0F)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            } else { // 右
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, -0x10 + 0x00);
                                if (ret >= 0) {
                                    dx = pturn.dx(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, (-dx << 4) + 0x00, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret & 0x0F) != (_kmv[j].np & 0x0F)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            }
                        } else {// 段・筋が異なる
                                //筋移動
                            if (dx < 0) { // 左
                                if (chkRectMove(ref ban, turn, bPos, (byte)((aOpos & 0xF0) + (bPos & 0x0F)), 0x10 + 0x00) == -1) {
                                    int rdx = pturn.dx(turn, bPos, aOpos);
                                    if (dy < 0) {
                                        if (chkRectMove(ref ban, turn, (byte)((aOpos & 0xF0) + (bPos & 0x0F)), aOpos, 0x00 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(rdx << 4) + 0, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    } else {
                                        if (chkRectMove(ref ban, turn, (byte)((aOpos & 0xF0) + (bPos & 0x0F)), aOpos, 0x00 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(rdx << 4) + 0, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    }
                                }
                            } else { // 右
                                if (chkRectMove(ref ban, turn, bPos, (byte)((aOpos & 0xF0) + (bPos & 0x0F)), -0x10 + 0x00) == -1) {
                                    int rdx = pturn.dx(turn, bPos, aOpos);
                                    if (dy < 0) {
                                        if (chkRectMove(ref ban, turn, (byte)((aOpos & 0xF0) + (bPos & 0x0F)), aOpos, 0x00 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(rdx << 4) + 0, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    } else {
                                        if (chkRectMove(ref ban, turn, (byte)((aOpos & 0xF0) + (bPos & 0x0F)), aOpos, 0x00 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(rdx << 4) + 0, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    }
                                }
                            }
                            //段移動
                            if (dy < 0) { // 下
                                if (chkRectMove(ref ban, turn, bPos, (byte)((bPos & 0xF0) + (aOpos & 0x0F)), 0x00 + 0x01) == -1) {
                                    int rdy = pturn.dy(turn, bPos, aOpos);
                                    if (dx < 0) {
                                        if (chkRectMove(ref ban, turn, (byte)((bPos & 0xF0) + (aOpos & 0x0F)), aOpos, 0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - rdy, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    } else {
                                        if (chkRectMove(ref ban, turn, (byte)((bPos & 0xF0) + (aOpos & 0x0F)), aOpos, -0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - rdy, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    }
                                }


                            } else { // 上
                                if (chkRectMove(ref ban, turn, bPos, (byte)((bPos & 0xF0) + (aOpos & 0x0F)), 0x00 - 0x01) == -1) {
                                    int rdy = pturn.dy(turn, bPos, aOpos);
                                    if (dx < 0) {
                                        if (chkRectMove(ref ban, turn, (byte)((bPos & 0xF0) + (aOpos & 0x0F)), aOpos, 0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - rdy, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    } else {
                                        if (chkRectMove(ref ban, turn, (byte)((bPos & 0xF0) + (aOpos & 0x0F)), aOpos, -0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - rdy, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    }
                                }
                            }

                            // ××××◎××××
                            // ×××①×③×××
                            // ××××◎××××
                            // ×⑧×〇×〇×⑦×
                            // ◎×◎×●×◎×◎
                            // ×⑥×〇×〇×⑤×
                            // ××××◎××××
                            // ×××②×④×××
                            // ××××◎××××
                            // TODO: 要実装
                            if (dx == -1) {
                                if (dy > 0) { // 1
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x10 - 0x01), aOpos, 0x00 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 + 0x01), 0x00 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - dy + 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x00 - dy - 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 + 0x01), 0x00 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x00 - dy - 1, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else {  // 2
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x10 + 0x01), aOpos, 0x00 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 - 0x01), 0x00 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - dy - 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x00 - dy + 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 - 0x01), 0x00 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - dy - 1, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x00 - dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dx == 1) {
                                if (dy > 0) {  // 3
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, -0x10 - 0x01), aOpos, 0x00 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, bPos, -0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, bPos, -0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 + 0x01), 0x00 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - dy + 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x00 - dy - 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 + 0x01), 0x00 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x00 - dy - 1, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else {  // 4
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, -0x10 + 0x01), aOpos, 0x00 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, bPos, -0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, bPos, -0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 - 0x01), 0x00 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0 - dy - 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0 - dy + 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 - 0x01), 0x00 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0 - dy - 1, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x00)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0 - dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dy == -1) {
                                if (dx > 0) {  // 5
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, -0x10 + 0x01), aOpos, -0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, bPos, -0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x00 + 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 - 0x01), -0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) + 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 - 0x01), -0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) + 0x10 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - 0x10 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else {  // 6
                                    //DebugForm.instance.addMsg("666");
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x10 + 0x01), aOpos, 0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x00 + 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 - 0x01), 0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) - 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) + 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 - 0x01), 0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) - 0x10 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) + 0x10 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dy == 1) {
                                //DebugForm.instance.addMsg("777");
                                if (dx > 0) {  // 7
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, -0x10 - 0x01), aOpos, -0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, bPos, -0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x00 - 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 + 0x01), -0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) + 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 + 0x01), -0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) + 0x10 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - 0x10 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else {  // 8
                                    //DebugForm.instance.addMsg("888");
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x10 - 0x01), aOpos, 0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x10 - 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x00 - 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 + 0x01), 0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) - 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) + 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 + 0x01), 0x10 + 0x00) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) - 0x10 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x01)) == ktype.None) &&
                                                    (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x01)) == ktype.None)) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) + 0x10 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            }

                        }

                        if (ban.getOnBoardKtype(bPos) == ktype.Hisya) {
                            //×××××
                            //×①×②×
                            //××●××
                            //×③×④×
                            //×××××
                            dx = pturn.dx(turn, bPos, aOpos);
                            dy = pturn.dy(turn, bPos, aOpos);
                            switch (dx, dy) {
                                case (-1, 1):
                                    if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x01)) == ktype.None) {
                                        addCheckMovePos(ref ban, bPos, 0x20 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None) {
                                        addCheckMovePos(ref ban, bPos, 0x00 - 0x02, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    break;
                                case (1, 1):
                                    if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x00)) == ktype.None) {
                                        addCheckMovePos(ref ban, bPos, 0x00 - 0x02, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x01)) == ktype.None) {
                                        addCheckMovePos(ref ban, bPos, -0x20 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    break;
                                case (-1, -1):
                                    if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None) {
                                        addCheckMovePos(ref ban, bPos, 0x00 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None) {
                                        addCheckMovePos(ref ban, bPos, 0x20 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    break;
                                case (1, -1):
                                    if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None) {
                                        addCheckMovePos(ref ban, bPos, -0x20 + 0x00, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x00)) == ktype.None) {
                                        addCheckMovePos(ref ban, bPos, 0x00 + 0x02, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    break;

                                default:
                                    break;
                            }

                        } else {
                            // 竜王特有の王手
                            //①×②×③
                            //××■××
                            //⑧■●■⑨
                            //××■××
                            //⑫×⑭×⑯
                            dx = pturn.dx(turn, bPos, aOpos);
                            dy = pturn.dy(turn, bPos, aOpos);
                            switch (dx, dy) {
                                case (-2, 2):
                                    addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (0, 2):
                                    addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (2, 2):
                                    addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-2, 0):
                                    addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (2, 0):
                                    addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-2, -2):
                                    addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (0, -2):
                                    addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (2, -2):
                                    addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKa] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        bPos = (byte)(ban.data[((int)turn << 6) + ban.setKa] >> ((i & 3) << 3) & 0xFF);
                        int dx = pturn.dx(turn, bPos, aOpos);
                        int dy = pturn.dy(turn, bPos, aOpos);
                        int ret;
                        if (dx == dy) {// 同じ右斜め(／)
                                       // [不成&成り]敵を取って直進
                            if (dy < 0) { // 前方
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, 0x10 + 0x01);
                                if (ret >= 0) {
                                    int rdy = pturn.dy(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, -(rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret >> 4) - (ret & 0x0F) != (_kmv[j].np >> 4) - (_kmv[j].np & 0x0F)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            } else { // 後方
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, -0x10 - 0x01);
                                if (ret >= 0) {
                                    int rdy = pturn.dy(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, -(rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret >> 4) - (ret & 0x0F) != (_kmv[j].np >> 4) - (_kmv[j].np & 0x0F)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            }

                        } else if (dx == -dy) {// 同じ左斜め(＼)
                                               // [不成&成り]敵を取って直進
                            if (dx < 0) { // 左
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, 0x10 - 0x01);
                                if (ret >= 0) {
                                    int rdx = pturn.dx(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, -(rdx << 4) + rdx, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret >> 4) + (ret & 0x0F) != (_kmv[j].np >> 4) + (_kmv[j].np & 0x0F)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            } else { // 右
                                ret = chkRectMove(ref ban, turn, bPos, aOpos, -0x10 + 0x01);
                                if (ret >= 0) {
                                    int rdx = pturn.dx(turn, bPos, (byte)ret);
                                    if (ban.getOnBoardPturn(ret) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, bPos, -(rdx << 4) + rdx, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if ((ret >> 4) + (ret & 0x0F) != (_kmv[j].np >> 4) + (_kmv[j].np & 0x0F)) {
                                                _kmv[j].val = bPos;
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            }
                        } else if ((dx + dy) % 2 == 0) {// 段・筋が異なる
                                                        // x - kx = y - ky
                                                        // x - ox = -y + oy
                                                        // x = (ox + oy + kx - ky)/2
                                                        // x = ( 4 + 6 - 7 - 5 ) / 2
                                                        // y + kx - ky = (ox + oy + kx - ky)/2
                            byte mPos = (byte)((((aOpos >> 4) + (aOpos & 0x0F) + (bPos >> 4) - (bPos & 0x0F)) << 3) + (((aOpos >> 4) + (aOpos & 0x0F) - (bPos >> 4) + (bPos & 0x0F)) >> 1));
                            //右斜め(／)移動
                            if ((mPos < 0x90) && ((mPos & 0x0F) < 9)) {
                                if (pturn.dx(turn, bPos, mPos) < 0) { //左
                                    if (chkRectMove(ref ban, turn, bPos, mPos, 0x10 + 0x01) == -1) {
                                        int rdy = pturn.dy(turn, bPos, mPos);
                                        if (pturn.dy(turn, mPos, aOpos) < 0) {   // 相手より下側
                                            if (chkRectMove(ref ban, turn, mPos, aOpos, -0x10 + 0x01) == -1) {
                                                addCheckMovePos(ref ban, bPos, -(rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                            }

                                        } else {
                                            if (chkRectMove(ref ban, turn, mPos, aOpos, 0x10 - 0x01) == -1) {
                                                addCheckMovePos(ref ban, bPos, -(rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (chkRectMove(ref ban, turn, bPos, mPos, -0x10 - 0x01) == -1) {
                                        int rdy = pturn.dy(turn, bPos, mPos);
                                        if (pturn.dy(turn, mPos, aOpos) < 0) {
                                            if (chkRectMove(ref ban, turn, mPos, aOpos, -0x10 + 0x01) == -1) {
                                                addCheckMovePos(ref ban, bPos, -(rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                            }


                                        } else {
                                            if (chkRectMove(ref ban, turn, mPos, aOpos, 0x10 - 0x01) == -1) {
                                                addCheckMovePos(ref ban, bPos, -(rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            }

                            //左斜め(＼)移動
                            // x - kx = -y + ky
                            // x - ox = y - oy
                            // x = (ox - oy + kx + ky)/2
                            // y = (-ox + oy + kx + ky)/2
                            //kx - ky < ox - oy
                            mPos = (byte)((((aOpos >> 4) - (aOpos & 0x0F) + (bPos >> 4) + (bPos & 0x0F)) << 3) + ((-(aOpos >> 4) + (aOpos & 0x0F) + (bPos >> 4) + (bPos & 0x0F)) >> 1));
                            if ((mPos < 0x90) && ((mPos & 0x0F) < 9)) {
                                if (pturn.dx(turn, bPos, mPos) < 0) { // 下
                                    if (chkRectMove(ref ban, turn, bPos, mPos, 0x10 - 0x01) == -1) {
                                        int rdy = pturn.dy(turn, bPos, mPos);
                                        if (pturn.dy(turn, mPos, aOpos) < 0) {
                                            if (chkRectMove(ref ban, turn, mPos, aOpos, 0x10 + 0x01) == -1) {
                                                addCheckMovePos(ref ban, bPos, (rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                            }
                                        } else {
                                            if (chkRectMove(ref ban, turn, mPos, aOpos, -0x10 - 0x01) == -1) {
                                                addCheckMovePos(ref ban, bPos, (rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 上
                                    if (chkRectMove(ref ban, turn, bPos, mPos, -0x10 + 0x01) == -1) {
                                        int rdy = pturn.dy(turn, bPos, mPos);
                                        if (pturn.dy(turn, mPos, aOpos) < 0) {
                                            if (chkRectMove(ref ban, turn, mPos, aOpos, 0x10 + 0x01) == -1) {
                                                addCheckMovePos(ref ban, bPos, (rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                            }

                                        } else {
                                            if (chkRectMove(ref ban, turn, mPos, aOpos, -0x10 - 0x01) == -1) {
                                                addCheckMovePos(ref ban, bPos, (rdy << 4) - rdy, turn, mType.Both, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            }
                        } else {
                            // ◎⑤×××④◎
                            // ⑦◎×××◎②
                            // ×××〇×××
                            // ××〇●〇××
                            // ×××〇×××
                            // ③◎×××◎④
                            // ◎①×××③◎
                            if (dx - dy == 1) {
                                if (dx < 0) { // 左
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x00 + 0x01), aOpos, 0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x00 + 0x01)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x00 - 0x01), 0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dy << 4) - dy, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x00 - 0x01), 0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dy << 4) - dy, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, -0x10 - 0x00), aOpos, -0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, bPos, -0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 - 0x00), -0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dy << 4) - dy, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 - 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 - 0x00), -0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dy << 4) - dy, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 - 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dx - dy == -1) {
                                if (dx < 0) { // 左
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x10 + 0x00), aOpos, 0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 + 0x00), 0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dy << 4) - dy, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 + 0x00), 0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dy << 4) - dy, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x00 - 0x01), aOpos, -0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x00 - 0x01)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x00 + 0x01), -0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x10)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x00 + 0x01), -0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x10)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) - dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dx + dy == 1) {
                                if (dx < 0) { // 左
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x00 - 0x01), aOpos, 0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x00 - 0x01)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x00 + 0x01), 0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) + dx, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x10)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, (dy << 4) - dy, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x00 + 0x01), 0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) + dx, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 + 0x10)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, (dy << 4) - dy, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, -0x01 + 0x00), aOpos, -0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, bPos, -0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 + 0x00), -0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, (dy << 4) - dy, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) + dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x10 + 0x00), -0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, (dy << 4) - dy, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) + dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dx + dy == -1) {
                                //DebugForm.instance.addMsg("777:" + dx + "," + dy);
                                if (dx < 0) { // 左
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x10 + 0x00), aOpos, 0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 + 0x00), 0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, (dy << 4) - dy, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) + dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, -0x10 + 0x00), 0x10 - 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, (dy << 4) - dy, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, -0x10 + 0x00)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, -(dx << 4) + dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (ban.getOnBoardKtype(bPos) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mv(turn, bPos, 0x00 + 0x01), aOpos, -0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, bPos, 0x00 + 0x01)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x00 - 0x01), -0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) + dx, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, (dy << 4) - dy, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, bPos, pturn.mv(turn, aOpos, 0x00 - 0x01), -0x10 + 0x01) == -1) {
                                            addCheckMovePos(ref ban, bPos, -(dx << 4) + dx, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.getOnBoardKtype(pturn.mv(turn, aOpos, 0x00 - 0x01)) == ktype.None) {
                                                addCheckMovePos(ref ban, bPos, (dy << 4) - dy, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (ban.getOnBoardKtype(bPos) == ktype.Kakugyou) {
                            // 角行特有の王手
                            //×××××
                            //××①××
                            //×②●③×
                            //××④××
                            //×××××
                            switch (dx, dy) {
                                case (0, 1):
                                    addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                                    break;
                                case (-1, 0):
                                    addCheckMovePos(ref ban, bPos, 0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                    break;
                                case (1, 0):
                                    addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, bPos, -0x10 - 0x01, turn, mType.Nari, kmv, ref kCnt);
                                    break;
                                case (0, -1):
                                    addCheckMovePos(ref ban, bPos, 0x10 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, bPos, -0x10 + 0x01, turn, mType.Nari, kmv, ref kCnt);
                                    break;
                                default:
                                    break;
                            }
                        } else {
                            // 竜馬特有の王手
                            //××①××
                            //×××××
                            //②×●×③
                            //×××××
                            //××④××
                            switch (dx, dy) {
                                case (0, 2):
                                    addCheckMovePos(ref ban, bPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-2, 0):
                                    addCheckMovePos(ref ban, bPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (2, 0):
                                    addCheckMovePos(ref ban, bPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (0, -2):
                                    addCheckMovePos(ref ban, bPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                // 歩打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Fuhyou] > 0) {
                    // 二歩チェック
                    if ((ban.data[((int)turn << 6) + ban.setFu + ((aOpos >> 4) >> 2)] >> (((aOpos >> 4) & 3) << 3) & 0xFF) == 0xFF) {
                        addCheckPutPos(ref ban, ktype.Fuhyou, aOpos, 0x00 - 0x01, turn, kmv, ref kCnt);
                    }
                }

                // 香打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Kyousha] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kyousha, aOpos, 0x00 - i, turn, kmv, ref kCnt);
                    }
                }

                // 桂打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Keima] > 0) {
                    addCheckPutPos(ref ban, ktype.Keima, aOpos, 0x10 - 0x02, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Keima, aOpos, -0x10 - 0x02, turn, kmv, ref kCnt);
                }

                // 銀打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Ginsyou] > 0) {
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOpos, -0x10 + 0x01, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOpos, 0x10 + 0x01, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOpos, 0x10 - 0x01, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOpos, 0x00 - 0x01, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOpos, -0x10 - 0x01, turn, kmv, ref kCnt);
                }

                // 飛打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Hisya] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOpos, 0x00 - i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOpos, 0x00 + i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOpos, -(i << 4) + 0x00, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOpos, (i << 4) + 0x00, turn, kmv, ref kCnt);
                    }
                }

                // 角打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Kakugyou] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOpos, -(i << 4) - i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOpos, (i << 4) - i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOpos, -(i << 4) + i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOpos, (i << 4) + i, turn, kmv, ref kCnt);
                    }
                }

                // 金打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Kinsyou] > 0) {
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOpos, 0 + 0x01, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOpos, -0x10 + 0x00, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOpos, 0x10 + 0x00, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOpos, -0x10 - 0x01, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOpos, 0x00 - 0x01, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOpos, 0x10 - 0x01, turn, kmv, ref kCnt);
                }

            }
            return kCnt;
        }

        /// <summary>
        /// [詰将棋]守り側移動候補一覧作成
        /// </summary>
        /// <param name="ban"></param>
        /// <param name="turn"></param>
        /// <param name="kmv"></param>
        /// <param name="cPos"></param>
        /// <returns></returns>
        public int getAllDefList(ref ban ban, Pturn turn, kmove[] kmv, byte cPos) {
            int kCnt = 0;
            unsafe {
                if (((ban.data[ban.data[((int)turn << 6) + ban.setOu] & 0xFF] >> (8 + ((int)pturn.aturn(turn) << 2))) & 0x0F) == 1) {
                    // 駒を取る
                    getPosMoveList(ref kCnt, ref ban, turn, cPos, kmv);

                    // 合い駒
                    int dx = pturn.dx(turn, cPos, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF));
                    int dy = pturn.dy(turn, cPos, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF));
                    switch (ban.getOnBoardKtype(cPos)) {
                        case ktype.Kyousha:
                            if (dy > 1) {
                                if (turn == Pturn.Sente) {
                                    for (int i = 1; i < dy; i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + i), kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + i), kmv);//合い駒を打つ
                                    }
                                } else {
                                    for (int i = 1; i < dy; i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - i), kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - i), kmv);//合い駒を打つ
                                    }
                                }
                            } else if (dy < -1) {
                                for (int i = 1; i < -dy; i++) {
                                    if (turn == Pturn.Sente) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - i), kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - i), kmv);//合い駒を打つ
                                    } else {
                                        getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + i), kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + i), kmv);//合い駒を打つ
                                    }
                                }
                            }
                            break;
                        case ktype.Ryuuou:
                        case ktype.Hisya:
                            // 敵基準で判定
                            if (dx == 0) {
                                if (dy > 1) {
                                    for (int i = 1; i < dy; i++) {
                                        if (turn == Pturn.Sente) {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + i), kmv);//合い駒を打つ
                                        } else {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - i), kmv);//合い駒を打つ
                                        }
                                    }
                                } else if (dy < -1) {
                                    for (int i = 1; i < -dy; i++) {
                                        if (turn == Pturn.Sente) {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - i), kmv);//合い駒を打つ
                                        } else {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + i), kmv);//合い駒を打つ
                                        }
                                    }
                                }
                            } else if (dy == 0) {
                                if (dx > 1) {
                                    for (int i = 1; i < dx; i++) {
                                        if (turn == Pturn.Sente) {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4)), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4)), kmv);//合い駒を打つ
                                        } else {

                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4)), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4)), kmv);//合い駒を打つ
                                        }
                                    }
                                } else if (dx < -1) {
                                    for (int i = 1; i < -dx; i++) {
                                        if (turn == Pturn.Sente) {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4)), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4)), kmv);//合い駒を打つ
                                        } else {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4)), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4)), kmv);//合い駒を打つ
                                        }
                                    }
                                }
                            }
                            break;

                        case ktype.Ryuuma:
                        case ktype.Kakugyou:
                            // 敵基準で判定
                            if (dx == dy) { // 右斜め(／)
                                if (dx > 1) {
                                    for (int i = 1; i < dx; i++) {
                                        if (turn == Pturn.Sente) {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4) + i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4) + i), kmv);//合い駒を打つ
                                        } else {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4) - i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4) - i), kmv);//合い駒を打つ
                                        }
                                    }
                                } else if (dx < -1) {
                                    for (int i = 1; i < -dx; i++) {
                                        if (turn == Pturn.Sente) {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4) - i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4) - i), kmv);//合い駒を打つ
                                        } else {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4) + i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4) + i), kmv);//合い駒を打つ
                                        }
                                    }
                                }
                            } else if (dx == -dy) { // 左斜め(／)
                                if (dx > 1) {
                                    for (int i = 1; i < dx; i++) {
                                        if (turn == Pturn.Sente) {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4) - i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4) - i), kmv);//合い駒を打つ
                                        } else {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4) + i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4) + i), kmv);//合い駒を打つ
                                        }
                                    }
                                } else if (dx < -1) {
                                    for (int i = 1; i < -dx; i++) {
                                        if (turn == Pturn.Sente) {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4) + i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos - (i << 4) + i), kmv);//合い駒を打つ
                                        } else {
                                            getPosMoveList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4) - i), kmv);//合い駒を移動
                                            getPosPutList(ref kCnt, ref ban, turn, (byte)(cPos + (i << 4) - i), kmv);//合い駒を打つ
                                        }
                                    }
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }


                // 王を移動(8方向)
                addCheckMovePos(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF), 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF), 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF), -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF), 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF), -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF), 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF), 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF), -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
            }

            return kCnt;
        }
        /// <summary>
        /// 指定した位置(cPos)へ盤上の駒を移動する(王以外)
        /// </summary>
        /// <param name="kCnt">駒移動候補カウンタ</param>
        /// <param name="ban">盤情報</param>
        /// <param name="turn">ターン</param>
        /// <param name="cPos">駒を動かす位置(x*9+y)</param>
        /// <param name="kmv">駒移動候補リスト</param>
        void getPosMoveList(ref int kCnt, ref ban ban, Pturn turn, byte cPos, kmove[] kmv) {
            unsafe {

                byte dPos;  // 守り駒の位置
                byte cpPos = pturn.ps(turn, cPos); //移動対象の自分中心位置

                //01 02 03
                //04 ● 05
                //06 07 08
                //09 × 10

                if ((cpPos & 0x0F) < 8) {
                    // 01
                    dPos = pturn.mv(turn, cPos, -0x10 + 0x01);
                    if (((cpPos >> 4) > 0) && (ban.getOnBoardKtype(dPos) == ktype.Ginsyou) && (ban.getOnBoardPturn(dPos) == turn)) {//銀将のみ
                        addCheckMovePos(ref ban, dPos, 0x10 - 0x01, turn, mType.Both, kmv, ref kCnt);
                    }

                    // 02
                    dPos = pturn.mv(turn, cPos, 0x00 + 0x01);
                    if (ban.getOnBoardPturn(dPos) == turn) {
                        switch (ban.getOnBoardKtype(dPos)) {
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, dPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 03
                    dPos = pturn.mv(turn, cPos, 0x10 + 0x01);
                    if (((cpPos >> 4) < 8) && (ban.getOnBoardKtype(dPos) == ktype.Ginsyou) && (ban.getOnBoardPturn(dPos) == turn)) {//銀将のみ
                        addCheckMovePos(ref ban, dPos, -0x10 - 0x01, turn, mType.Both, kmv, ref kCnt);
                    }
                }

                // 04
                dPos = pturn.mv(turn, cPos, -0x10 + 0x00);
                if (((cpPos >> 4) > 0) && (ban.getOnBoardPturn(dPos) == turn)) {
                    switch (ban.getOnBoardKtype(dPos)) {
                        case (ktype.Kinsyou):  //金将
                        case (ktype.Tokin):    //と金
                        case (ktype.Narikyou): //成香
                        case (ktype.Narikei):  //成桂
                        case (ktype.Narigin):  //成銀
                            addCheckMovePos(ref ban, dPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        default:
                            break;
                    }
                }

                // 05
                dPos = pturn.mv(turn, cPos, 0x10 + 0x00);
                if (((cpPos >> 4) < 8) && (ban.getOnBoardPturn(dPos) == turn)) {
                    switch (ban.getOnBoardKtype(dPos)) {
                        case (ktype.Kinsyou):  //金将
                        case (ktype.Tokin):    //と金
                        case (ktype.Narikyou): //成香
                        case (ktype.Narikei):  //成桂
                        case (ktype.Narigin):  //成銀
                            addCheckMovePos(ref ban, dPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        default:
                            break;
                    }
                }

                if ((cpPos & 0x0F) > 0) {
                    // 06
                    dPos = pturn.mv(turn, cPos, -0x10 - 0x01);
                    if (((cpPos >> 4) > 0) && (ban.getOnBoardPturn(dPos) == turn)) {
                        switch (ban.getOnBoardKtype(dPos)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, dPos, 0x10 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, dPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 07
                    dPos = pturn.mv(turn, cPos, 0x00 - 0x01);
                    if (ban.getOnBoardPturn(dPos) == turn) {
                        switch (ban.getOnBoardKtype(dPos)) {
                            case (ktype.Fuhyou):  //歩兵
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, dPos, 0x00 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, dPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }
                    // 08
                    dPos = pturn.mv(turn, cPos, 0x10 - 0x01);
                    if (((cpPos >> 4) < 8) && (ban.getOnBoardPturn(dPos) == turn)) {
                        switch (ban.getOnBoardKtype(dPos)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, dPos, -0x10 + 0x01, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, dPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if ((cpPos & 0x0F) > 1) {
                    // 09
                    dPos = pturn.mv(turn, cPos, -0x10 - 0x02);
                    if (((cpPos >> 4) > 0) && (ban.getOnBoardKtype(dPos) == ktype.Keima) && (ban.getOnBoardPturn(dPos) == turn)) { //桂馬のみ
                        addCheckMovePos(ref ban, dPos, 0x10 + 0x02, turn, mType.Both, kmv, ref kCnt);
                    }

                    // 10
                    dPos = pturn.mv(turn, cPos, 0x10 - 0x02);
                    if (((cpPos >> 4) < 8) && (ban.getOnBoardKtype(dPos) == ktype.Keima) && (ban.getOnBoardPturn(dPos) == turn)) { //桂馬のみ
                        addCheckMovePos(ref ban, dPos, -0x10 + 0x02, turn, mType.Both, kmv, ref kCnt);
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        dPos = (byte)(ban.data[((int)turn << 6) + ban.setKyo] >> ((i & 3) << 3) & 0xFF);
                        int dx = pturn.dx(turn, dPos, cPos);
                        int dy = pturn.dy(turn, dPos, cPos);
                        if (dx == 0) {
                            if (dy < 0) { // 上
                                if (chkRectMove(ref ban, turn, dPos, cPos, 0x00 + 0x01) == -1) {
                                    addCheckMovePos(ref ban, dPos, 0x00 - dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        }
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setHi] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        dPos = (byte)(ban.data[((int)turn << 6) + ban.setHi] >> ((i & 3) << 3) & 0xFF);
                        int dx = pturn.dx(turn, dPos, cPos);
                        int dy = pturn.dy(turn, dPos, cPos);
                        if (dx == 0) {
                            if (dy < 0) { // 上
                                if (chkRectMove(ref ban, turn, dPos, cPos, 0x00 + 0x01) == -1) {
                                    addCheckMovePos(ref ban, dPos, 0x00 - dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            } else { // (dy > 0) 下
                                if (chkRectMove(ref ban, turn, dPos, cPos, 0x00 - 0x01) == -1) {
                                    addCheckMovePos(ref ban, dPos, 0x00 - dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        } else if (dy == 0) {
                            if (dx < 0) { // 右
                                if (chkRectMove(ref ban, turn, dPos, cPos, 0x10 + 0x00) == -1) {
                                    addCheckMovePos(ref ban, dPos, -(dx << 4) + 0x00, turn, mType.Both, kmv, ref kCnt);
                                }
                            } else { // (dx > 0) 左
                                if (chkRectMove(ref ban, turn, dPos, cPos, -0x10 + 0x00) == -1) {
                                    addCheckMovePos(ref ban, dPos, -(dx << 4) + 0x00, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        }
                        // 竜王の動き
                        if (ban.getOnBoardKtype(dPos) == ktype.Ryuuou) {
                            switch (dx, dy) {
                                case (-1, 1):
                                    addCheckMovePos(ref ban, dPos, 0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (1, 1):
                                    addCheckMovePos(ref ban, dPos, -0x10 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-1, -1):
                                    addCheckMovePos(ref ban, dPos, 0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (1, -1):
                                    addCheckMovePos(ref ban, dPos, -0x10 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                default:
                                    break;
                            }
                        }

                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if ((ban.data[((int)turn << 6) + ban.setKa] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        dPos = (byte)(ban.data[((int)turn << 6) + ban.setKa] >> ((i & 3) << 3) & 0xFF);
                        int dx = pturn.dx(turn, dPos, cPos);
                        int dy = pturn.dy(turn, dPos, cPos);
                        if (dx == dy) {
                            if (dy < 0) {
                                if (chkRectMove(ref ban, turn, dPos, cPos, 0x10 + 0x01) == -1) {
                                    addCheckMovePos(ref ban, dPos, -(dx << 4) - dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            } else { // (dy > 0)
                                if (chkRectMove(ref ban, turn, dPos, cPos, -0x10 - 0x01) == -1) {
                                    addCheckMovePos(ref ban, dPos, -(dx << 4) - dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        } else if (dx == -dy) {
                            if (dx < 0) {
                                if (chkRectMove(ref ban, turn, dPos, cPos, 0x10 - 0x01) == -1) {
                                    addCheckMovePos(ref ban, dPos, -(dx << 4) - dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            } else { // (dx > 0)
                                if (chkRectMove(ref ban, turn, dPos, cPos, -0x10 + 0x01) == -1) {
                                    addCheckMovePos(ref ban, dPos, -(dx << 4) - dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        }
                        // 竜馬の動き
                        if (ban.getOnBoardKtype(dPos) == ktype.Ryuuma) {
                            switch (dx, dy) {
                                case (0, 1):
                                    addCheckMovePos(ref ban, dPos, 0x00 - 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (1, 0):
                                    addCheckMovePos(ref ban, dPos, -0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-1, 0):
                                    addCheckMovePos(ref ban, dPos, 0x10 + 0x00, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (0, -1):
                                    addCheckMovePos(ref ban, dPos, 0x00 + 0x01, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// 指定した位置(cPos)へ駒を置く
        /// </summary>
        /// <param name="kCnt">駒移動候補カウンタ</param>
        /// <param name="ban">盤情報</param>
        /// <param name="turn">ターン</param>
        /// <param name="cPos">駒を置く位置(0xXY)</param>
        /// <param name="kmv">駒移動候補リスト</param>
        void getPosPutList(ref int kCnt, ref ban ban, Pturn turn, byte cPos, kmove[] kmv) {
            unsafe {
                // 歩打ち
                if (((ban.data[((int)turn << 6) + ban.hand + (int)ktype.Fuhyou] > 0) && ((pturn.ps(turn, cPos) & 0x0F) < 8))) {
                    // 二歩チェック
                    if (((ban.data[((int)turn << 6) + ban.setFu + (cPos >> 6)] >> (((cPos >> 4) & 3) << 3)) & 0xFF) == 0xFF) {

                        kmv[kCnt++].set(0x90 + (byte)ktype.Fuhyou, cPos, 0, 0, false, turn);
                    }
                }

                // 香打ち
                if ((ban.data[((int)turn << 6) + ban.hand + (int)ktype.Kyousha] > 0) && ((pturn.ps(turn, cPos) & 0x0F) < 8)) {
                    kmv[kCnt++].set(0x90 + (byte)ktype.Kyousha, cPos, 0, 0, false, turn);
                }

                // 桂打ち
                if ((ban.data[((int)turn << 6) + ban.hand + (int)ktype.Keima] > 0) && ((pturn.ps(turn, cPos) & 0x0F) < 7)) {
                    kmv[kCnt++].set(0x90 + (byte)ktype.Keima, cPos, 0, 0, false, turn);
                }

                // 銀打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Ginsyou] > 0) {
                    kmv[kCnt++].set(0x90 + (byte)ktype.Ginsyou, cPos, 0, 0, false, turn);
                }

                // 飛打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Hisya] > 0) {
                    kmv[kCnt++].set(0x90 + (byte)ktype.Hisya, cPos, 0, 0, false, turn);
                }

                // 角打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Kakugyou] > 0) {
                    kmv[kCnt++].set(0x90 + (byte)ktype.Kakugyou, cPos, 0, 0, false, turn);
                }

                // 金打ち
                if (ban.data[((int)turn << 6) + ban.hand + (int)ktype.Kinsyou] > 0) {
                    kmv[kCnt++].set(0x90 + (byte)ktype.Kinsyou, cPos, 0, 0, false, turn);
                }

            }
        }
        /// <summary>
        /// oPosの駒が自分中心位置でmPosへ動かすことができるか
        /// </summary>
        /// <param name="ban"></param>
        /// <param name="oPos"></param>
        /// <param name="mx"></param>
        /// <param name="my"></param>
        /// <param name="turn"></param>
        /// <param name="nari"></param>
        /// <param name="kmv"></param>
        /// <param name="kCnt"></param>
        /// <returns>移動できる 0 / 移動できる(敵駒取り) 1 / 移動できない(味方駒) 2 / 移動できない 3(範囲外)</returns>
        public int addCheckMovePos(ref ban ban, byte oPos, int mPos, Pturn turn, mType nari, kmove[] kmv, ref int kCnt) {
            byte nPos = (byte)pturn.mv(turn, oPos, mPos);
            unsafe {
                if ((oPos > 0x88) || ((oPos & 0xF) > 0x08) || (nPos > 0x88) || ((nPos & 0xF) > 0x08)) return 3; // 範囲外
                if ((ban.getOnBoardKtype(nPos) > ktype.None) && (ban.getOnBoardPturn(nPos) == turn)) return 2; // 味方の駒

                //成り
                if (nari >= mType.Both) {
                    //成れない場所・成れない駒は不可(飛角香のためコンティニュー可能にする)
                    if ((ban.getOnBoardKtype(oPos) < ktype.Kinsyou) && (((pturn.ps(turn, oPos) & 0x0F) > 5) || ((pturn.ps(turn, nPos) & 0x0F) > 5))) {
                        kmv[kCnt++].set((byte)oPos, nPos, nPos, 0, true, turn);
                    }
                }

                //不成
                if (nari <= mType.Both) {
                    if ((((pturn.ps(turn, nPos) & 0x0F) == 8) && ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) || (ban.getOnBoardKtype(oPos) == ktype.Fuhyou))) ||
                        (((pturn.ps(turn, nPos) & 0x0F) > 6) && (ban.getOnBoardKtype(oPos) == ktype.Keima))) return 3; //不成NG
                    kmv[kCnt++].set((byte)oPos, nPos, nPos, 0, false, turn);
                }

                if (ban.getOnBoardKtype(nPos) > ktype.None) {
                    return 1;
                } else {
                    return 0;
                }

            }
        }

        /// <summary>
        /// ターゲット位置(tPos)からmx,myの自分中心位置に駒を置けるかチェック
        /// </summary>
        /// <param name="ban"></param>
        /// <param name="type"></param>
        /// <param name="tPos"></param>
        /// <param name="mx"></param>
        /// <param name="my"></param>
        /// <param name="turn"></param>
        /// <param name="kmv"></param>
        /// <param name="kCnt"></param>
        /// <returns></returns>
        int addCheckPutPos(ref ban ban, ktype type, byte tPos, int mPos, Pturn turn, kmove[] kmv, ref int kCnt) {
            unsafe {
                byte nPos = (byte)pturn.mv(turn, tPos, mPos);
                if ((nPos > 0x88) || ((nPos & 0xF) > 0x08)) return 2;
                if (ban.getOnBoardKtype(nPos) > ktype.None) return 1;
                kmv[kCnt++].set((byte)(0x90 + (int)type), nPos, nPos, 0, false, turn); //移動候補リストに追加
                return 0;
            }
        }

        // 指定先まで駒が存在するかチェック(指定先含めず)
        // 0～80:指定位置(X*9+Y)に駒あり -1 :駒無し -2 :駒2個以上あり -3 :opにたどり着かない
        int chkRectMove(ref ban ban, Pturn turn, byte oPos, byte tPos, int mv) {
            unsafe {
                int ret = -1;
                byte mPos = oPos;
                for (int i = 0; ; i++) {
                    mPos = pturn.mv(turn, mPos, mv);
                    if (mPos == tPos) return ret;
                    if ((mPos > 0x88) || ((mPos & 0xF) > 0x08)) return -3;
                    if (ban.getOnBoardKtype(mPos) > ktype.None) {
                        if (ret == -1) {
                            ret = mPos;
                        } else {
                            return -2;
                        }
                    }
                }
            }
        }

    }
}