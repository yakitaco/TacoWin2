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
                    DebugForm.instance.addMsg("MV:(" + (moveList[i].op / 9 + 1) + "," + (moveList[i].op % 9 + 1) + ")->(" + (moveList[i].np / 9 + 1) + "," + (moveList[i].np % 9 + 1) + ")*");
                } else {
                    DebugForm.instance.addMsg("MV:(" + (moveList[i].op / 9 + 1) + "," + (moveList[i].op % 9 + 1) + ")->(" + (moveList[i].np / 9 + 1) + "," + (moveList[i].np % 9 + 1) + ")");
                }
            }
            mList.freeAlist(aid);
            return (null, 0);
        }


        public (kmove[], int) thinkMateMove(Pturn turn, ban ban, int depth) {
            int best = 0;
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
                            DebugForm.instance.addMsg("MVV:(" + (moveList[cnt_local].op / 9 + 1) + "," + (moveList[cnt_local].op % 9 + 1) + ")->(" + (moveList[cnt_local].np / 9 + 1) + "," + (moveList[cnt_local].np % 9 + 1) + ")* + " + moveList[cnt_local].val);
                        } else {
                            DebugForm.instance.addMsg("MVV:(" + (moveList[cnt_local].op / 9 + 1) + "," + (moveList[cnt_local].op % 9 + 1) + ")->(" + (moveList[cnt_local].np / 9 + 1) + "," + (moveList[cnt_local].np % 9 + 1) + ") + " + moveList[cnt_local].val);
                        }

                        //mList.ls[cnt_local + 1][0] = mList.ls[0][sp + cnt_local];

                        // 駒移動
                        ban tmp_ban = ban;
                        int val = 0;
                        int retVal;
                        kmove[] retList = null;

                        //駒を動かす
                        tmp_ban.moveKoma(moveList[cnt_local].op / 9, moveList[cnt_local].op % 9, moveList[cnt_local].np / 9, moveList[cnt_local].np % 9, turn, moveList[cnt_local].nari, false, true);

                        // 王手はスキップ
                        if ((tmp_ban.putOusyou[(int)turn] < 0xFF) && (tmp_ban.moveable[pturn.aturn((int)turn) * 81 + tmp_ban.putOusyou[(int)turn]] > 0)) {
                            retVal = 0;
                            //DebugForm.Form1Instance.addMsg("TASK[{0}:{1}]MV[{2}]({3},{4})->({5},{6})[{7}]\n", Task.CurrentId, cnt_local, retVal, moveList[cnt_local].op / 9 + 1, moveList[cnt_local].op % 9 + 1, moveList[cnt_local].np / 9 + 1, moveList[cnt_local].np % 9 + 1, moveList[cnt_local].val);
                        } else {
                            //moveList[cnt_local].val = val;
                            retVal = -thinkMateDef(pturn.aturn(turn), ref tmp_ban, moveList[cnt_local].val, out retList, 1, depth);
                            retList[0] = moveList[cnt_local];

                            string str = "";
                            int i;
                            for (i = 0; retList[i].op > 0 || retList[i].np > 0; i++) {
                                str += "(" + (retList[i].op / 9 + 1) + "," + (retList[i].op % 9 + 1) + ")->(" + (retList[i].np / 9 + 1) + "," + (retList[i].np % 9 + 1) + "):" + retList[i].val + "/";
                            }
                            if ((retVal > 5000) && (i < mateDepMax)) mateDepMax = i;

                            DebugForm.instance.addMsg("TASK[" + Task.CurrentId + ":" + cnt_local + "]MV[" + retVal + "]" + str);
                        }

                        lock (lockObj) {
                            if (retVal > best) {
                                best = retVal;
                                bestmove = retList;
                            }
                        }


                    }
                });

                mList.freeAlist(aid);
            }

            DebugForm.instance.addMsg("FIN:" + best);

            return (bestmove, best);
        }


        //cPos : 王手をしている手(-1の場合は空き王手+移動->王を移動する必要あり)
        public int thinkMateDef(Pturn turn, ref ban ban, int cPos, out kmove[] bestMoveList, int depth, int depMax) {

            int best = -999999;
            bestMoveList = null;
            int teNum = 0;
            bool tumi = true;

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
                //Debug.WriteLine("DDD ");
                for (int cnt = 0; cnt < teNum; cnt++) {

                    kmove[] retList = null;

                    //駒を動かす
                    ban tmp_ban = ban;
                    tmp_ban.moveKoma(moveList[cnt].op / 9, moveList[cnt].op % 9, moveList[cnt].np / 9, moveList[cnt].np % 9, turn, moveList[cnt].nari, false, true);

                    if (tmp_ban.moveable[pturn.aturn((int)turn) * 81 + tmp_ban.putOusyou[(int)turn]] > 0) {
                        if (bestMoveList == null) {
                            bestMoveList = new kmove[30];
                            //bestMoveList[depth] = moveList[cnt];
                            best = -999999 + depth * 10000;
                            //DebugForm.instance.addMsg("wa");
                        }
                        continue;
                    }

                    tumi = false;
                    if (depth < mateDepMax) {
                        int retVal = -thinkMateAtk(pturn.aturn(turn), ref tmp_ban, out retList, depth + 1, depMax);
                        if (retVal > best) {
                            best = retVal;
                            bestMoveList = retList;
                            bestMoveList[depth] = moveList[cnt];
                        }
                    } else {
                        // 詰み逃れが一つ以上ある
                        best = 0;
                        break;
                    }
                }

                //ここで詰んだ
                if ((tumi == true) && (mateDepMax > depth)) {
                    //DebugForm.instance.addMsg("depth = " + depth);
                    //mateDepMax = depth;
                }

                lock (lockObj) {
                    mList.freeAlist(aid);
                }
            }
            if (bestMoveList == null) bestMoveList = new kmove[30];
            return best;
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
            int best = 0;
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

                if (depth < mateDepMax) {
                    //[攻め方]王手を指せる手を全てリスト追加
                    int vla = getAllCheckList(ref ban, turn, moveList);
                    for (int cnt = 0; cnt < vla; cnt++) {

                        //駒を動かす
                        ban tmp_ban = ban;
                        tmp_ban.moveKoma(moveList[cnt].op / 9, moveList[cnt].op % 9, moveList[cnt].np / 9, moveList[cnt].np % 9, turn, moveList[cnt].nari, false, true);

                        // 自分の駒が王手の場合はNG
                        if ((tmp_ban.putOusyou[(int)turn] < 0xFF) && (tmp_ban.moveable[pturn.aturn((int)turn) * 81 + tmp_ban.putOusyou[(int)turn]] > 0)) {
                            if (bestMoveList == null) {
                                bestMoveList = new kmove[30];
                                //bestMoveList[depth] = moveList[cnt];
                            }
                            continue;
                        }
                        int retVal = -thinkMateDef(pturn.aturn(turn), ref tmp_ban, moveList[cnt].val, out retList, depth + 1, depMax);
                        if (retVal > best) {
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
            }
            if (bestMoveList == null) bestMoveList = new kmove[30];
            return best;
        }


        //指定した位置に次に移動可能となる
        int getAllCheckList(ref ban ban, Pturn turn, kmove[] kmv) {
            int kCnt = 0;
            emove emv = new emove();
            unsafe {
                int AposX;  // 攻め駒のX位置
                int AposY;  // 攻め駒のY位置
                int aOuPosX = ban.putOusyou[pturn.aturn((int)turn)] / 9; //相手王将のX位置
                int aOuPosY = ban.putOusyou[pturn.aturn((int)turn)] % 9; //相手王将のY位置
                int aOuPos = ban.putOusyou[pturn.aturn((int)turn)]; //相手王将の位置★要削除

                //香車・飛車・角以外の駒の動きチェック
                //01 02 03 04 05
                //06 07 08 09 10
                //11 12 ● 13 14
                //15 16 17 18 19
                //20 21 22 23 24
                //25 26 27 28 29
                //30 × 31 × 32

                // 01
                if (pturn.psY(turn, aOuPosY) < 7) {
                    AposX = pturn.mvX(turn, aOuPosX, -2);
                    AposY = pturn.mvY(turn, aOuPosY, 2);
                    if ((pturn.psY(turn, aOuPosX) > 1) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }

                    // 02
                    AposX = pturn.mvX(turn, aOuPosX, -1);
                    if ((pturn.psY(turn, aOuPosX) > 0) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                            //DebugForm.instance.addMsg("DDDDDD:" + AposX + "," + AposY);
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, -1, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 03
                    AposX = aOuPosX;
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 04
                    AposX = pturn.mvX(turn, aOuPosX, 1);
                    if ((pturn.psY(turn, aOuPosX) < 8) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, -1, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 05
                    AposX = pturn.mvX(turn, aOuPosX, 2);
                    if ((pturn.psY(turn, aOuPosX) < 7) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }
                }

                if (pturn.psY(turn, aOuPosY) < 8) {
                    // 06
                    AposX = pturn.mvX(turn, aOuPosX, -2);
                    AposY = pturn.mvY(turn, aOuPosY, 1);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, -1, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 07
                    AposX = pturn.mvX(turn, aOuPosX, -1);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 08
                    AposX = aOuPosX;
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, -1, turn, mType.Nari, kmv, ref kCnt);
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, -1, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 09
                    AposX = pturn.mvX(turn, aOuPosX, 1);
                    if ((pturn.psY(turn, aOuPosX) < 8) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 10
                    AposX = pturn.mvX(turn, aOuPosX, 2);
                    if ((pturn.psY(turn, aOuPosX) < 7) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, -1, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }
                }

                // 11
                AposX = pturn.mvX(turn, aOuPosX, -2);
                AposY = aOuPosY;
                if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                    switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                        case (ktype.Ginsyou): //銀将
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        case (ktype.Kinsyou):  //金将
                        case (ktype.Tokin):    //と金
                        case (ktype.Narikyou): //成香
                        case (ktype.Narikei):  //成桂
                        case (ktype.Narigin):  //成銀
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        default:
                            break;
                    }
                }

                // 12
                AposX = pturn.mvX(turn, aOuPosX, -1);
                if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                    if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                        addCheckMovePos(ref ban, AposX * 9 + AposY, 1, -1, turn, mType.Both, kmv, ref kCnt);
                        addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                        addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.Nari, kmv, ref kCnt);
                    }
                }

                // 13
                AposX = pturn.mvX(turn, aOuPosX, 1);
                if ((pturn.psX(turn, aOuPosX) < 8) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                    if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                        addCheckMovePos(ref ban, AposX * 9 + AposY, -1, -1, turn, mType.Both, kmv, ref kCnt);
                        addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                        addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.Nari, kmv, ref kCnt);
                    }
                }

                // 14
                AposX = pturn.mvX(turn, aOuPosX, 2);
                if ((pturn.psX(turn, aOuPosX) < 7) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                    switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                        case (ktype.Ginsyou): //銀将
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        case (ktype.Kinsyou):  //金将
                        case (ktype.Tokin):    //と金
                        case (ktype.Narikyou): //成香
                        case (ktype.Narikei):  //成桂
                        case (ktype.Narigin):  //成銀
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        default:
                            break;
                    }
                }

                if (pturn.psY(turn, aOuPosY) > 0) {
                    // 15
                    AposX = pturn.mvX(turn, aOuPosX, -2);
                    AposY = pturn.mvY(turn, aOuPosY, -1);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 16
                    AposX = pturn.mvX(turn, aOuPosX, -1);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 2, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 17(該当なし)
                    //AposX = aOuPosX;
                    //if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                    //    if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Ginsyou) { //銀将のみ
                    //        addCheckMovePos(ref ban, AposX * 9 + AposY, 1, -1, turn, mType.Nari, kmv, ref kCnt);
                    //        addCheckMovePos(ref ban, AposX * 9 + AposY, -1, -1, turn, mType.Nari, kmv, ref kCnt);
                    //    }
                    //}

                    // 18
                    AposX = pturn.mvX(turn, aOuPosX, 1);
                    if ((pturn.psX(turn, aOuPosX) < 6) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 2, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 19
                    AposX = pturn.mvX(turn, aOuPosX, 2);
                    if ((pturn.psX(turn, aOuPosX) < 7) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (pturn.psY(turn, aOuPosY) > 1) {
                    // 20
                    AposX = pturn.mvX(turn, aOuPosX, -2);
                    AposY = pturn.mvY(turn, aOuPosY, -2);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 2, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 21
                    AposX = pturn.mvX(turn, aOuPosX, -1);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.Both, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 0, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 22
                    AposX = aOuPosX;
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 2, turn, mType.Nari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 2, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.Both, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.Both, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 23
                    AposX = pturn.mvX(turn, aOuPosX, 1);
                    if ((pturn.psY(turn, aOuPosX) < 6) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, AposX * 9 + AposY, 0, 1, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.Both, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 0, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 24
                    AposX = pturn.mvX(turn, aOuPosX, 2);
                    if ((pturn.psY(turn, aOuPosX) < 7) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        switch (ban.getOnBoardKtype(AposX * 9 + AposY)) {
                            case (ktype.Keima):  //桂馬
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 2, turn, mType.Nari, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (pturn.psY(turn, aOuPosY) > 2) {
                    // 25
                    AposX = pturn.mvX(turn, aOuPosX, -2);
                    AposY = pturn.mvY(turn, aOuPosY, -3);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 2, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 26
                    AposX = pturn.mvX(turn, aOuPosX, -1);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 2, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 27
                    AposX = aOuPosX;
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 2, turn, mType.Nari, kmv, ref kCnt);
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 2, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 28
                    AposX = pturn.mvX(turn, aOuPosX, 1);
                    if ((pturn.psY(turn, aOuPosX) < 6) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 2, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }

                    // 29
                    AposX = pturn.mvX(turn, aOuPosX, 2);
                    if ((pturn.psY(turn, aOuPosX) < 7) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 2, turn, mType.Nari, kmv, ref kCnt);
                        }
                    }
                }

                if (pturn.psY(turn, aOuPosY) > 2) {
                    // 30
                    AposX = pturn.mvX(turn, aOuPosX, -2);
                    AposY = pturn.mvY(turn, aOuPosY, -4);
                    if ((ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 2, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }

                    // 31
                    AposX = aOuPosX;
                    if (((pturn.psY(turn, aOuPosX) < 6) && ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, 1, 2, turn, mType.NoNari, kmv, ref kCnt);
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 2, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }

                    // 32
                    AposX = pturn.mvX(turn, aOuPosX, 2);
                    if ((pturn.psY(turn, aOuPosX) < 7) && (ban.onBoard[AposX * 9 + AposY] > 0) && (ban.getOnBoardPturn(AposX, AposY) == turn)) {
                        if (ban.getOnBoardKtype(AposX * 9 + AposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, AposX * 9 + AposY, -1, 2, turn, mType.NoNari, kmv, ref kCnt);
                        }
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if (ban.putKyousha[(int)turn * 4 + i] != 0xFF) {
                        int dx = pturn.dfX(turn, ban.putKyousha[(int)turn * 4 + i] / 9, aOuPos / 9);
                        int dy;
                        int ret;
                        int tPos;
                        switch (dx) {
                            case (0):
                                // [不成]敵を取って直進
                                ret = chkRectMove(ref ban, turn, ban.putKyousha[(int)turn * 4 + i], aOuPos, 0, 1);
                                if (ret >= 0) {
                                    dy = pturn.dfY(turn, ban.putKyousha[(int)turn * 4 + i] % 9, ret % 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                        if (pturn.dfY(turn, ret % 9, aOuPos % 9) == -1) {//一つ手前
                                            addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, -dy, turn, mType.Nari, kmv, ref kCnt);
                                        }
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret / 9 != _kmv[j].np / 9) {
                                                _kmv[j].val = ban.putKyousha[(int)turn * 4 + i];
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                                break;

                            case (-1):

                                tPos = pturn.mvXY(turn, aOuPos, -9);
                                ret = chkRectMove(ref ban, turn, ban.putKyousha[(int)turn * 4 + i], tPos, 0, 1);
                                if ((ret == pturn.mvXY(turn, tPos, -1)) && (ban.getOnBoardPturn(ret / 9, ret % 9) != turn)) {// 王の横手前
                                    dy = pturn.dfY(turn, ban.putKyousha[(int)turn * 4 + i] % 9, ret % 9);
                                    addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, -dy, turn, mType.Nari, kmv, ref kCnt);
                                } else if (ret == -1) {// 王の横手前
                                    dy = pturn.dfY(turn, ban.putKyousha[(int)turn * 4 + i] % 9, tPos % 9) + 1;
                                    addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, -dy, turn, mType.Nari, kmv, ref kCnt);
                                    if ((ban.onBoard[tPos] == 0) || (ban.getOnBoardPturn(tPos) != turn)) { // 王の横
                                        addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, -dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                }

                                break;

                            case (1):
                                tPos = pturn.mvXY(turn, aOuPos, 9);
                                ret = chkRectMove(ref ban, turn, ban.putKyousha[(int)turn * 4 + i], tPos, 0, 1);
                                if ((ret == pturn.mvXY(turn, tPos, -1)) && (ban.getOnBoardPturn(ret / 9, ret % 9) != turn)) {// 王の横手前
                                    dy = pturn.dfY(turn, ban.putKyousha[(int)turn * 4 + i] % 9, ret % 9);
                                    addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, -dy, turn, mType.Nari, kmv, ref kCnt);
                                } else if (ret == -1) {// 王の横手前
                                    dy = pturn.dfY(turn, ban.putKyousha[(int)turn * 4 + i] % 9, tPos % 9) + 1;
                                    addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, -dy, turn, mType.Nari, kmv, ref kCnt);
                                    if ((ban.onBoard[tPos] == 0) || (ban.getOnBoardPturn(tPos) != turn)) { // 王の横
                                        addCheckMovePos(ref ban, ban.putKyousha[(int)turn * 4 + i], 0, -dy + 1, turn, mType.Nari, kmv, ref kCnt);
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
                    if (ban.putHisya[(int)turn * 2 + i] != 0xFF) {
                        int dx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, aOuPos / 9);
                        int dy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, aOuPos % 9);
                        int ret;
                        if (dx == 0) {// 同じ筋
                                      // [不成&成り]敵を取って直進
                            if (dy < 0) { // 前方
                                ret = chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], aOuPos, 0, 1);
                                if (ret >= 0) {
                                    dy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, ret % 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret / 9 != _kmv[j].np / 9) {
                                                _kmv[j].val = ban.putHisya[(int)turn * 2 + i];
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            } else { // 後方
                                ret = chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], aOuPos, 0, -1);
                                if (ret >= 0) {
                                    dy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, ret % 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret / 9 != _kmv[j].np / 9) {
                                                _kmv[j].val = ban.putHisya[(int)turn * 2 + i];
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            }

                        } else if (dy == 0) {//同じ段
                                             // [不成&成り]敵を取って直進
                            if (dx < 0) { // 左
                                ret = chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], aOuPos, 1, 0);
                                if (ret >= 0) {
                                    dx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, ret / 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx, 0, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret % 9 != _kmv[j].np % 9) {
                                                _kmv[j].val = ban.putHisya[(int)turn * 2 + i];
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            } else { // 右
                                ret = chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], aOuPos, -1, 0);
                                if (ret >= 0) {
                                    dx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, ret / 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx, 0, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret % 9 != _kmv[j].np % 9) {
                                                _kmv[j].val = ban.putHisya[(int)turn * 2 + i];
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            }
                        } else {// 段・筋が異なる
                                //筋移動
                            if (dx < 0) { // 左
                                if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], (aOuPos / 9) * 9 + ban.putHisya[(int)turn * 2 + i] % 9, 1, 0) == -1) {
                                    if (dy < 0) {
                                        if (chkRectMove(ref ban, turn, (aOuPos / 9) * 9 + ban.putHisya[(int)turn * 2 + i] % 9, aOuPos, 0, 1) == -1) {
                                            int rdx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, aOuPos / 9);
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -rdx, 0, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    } else {
                                        if (chkRectMove(ref ban, turn, (aOuPos / 9) * 9 + ban.putHisya[(int)turn * 2 + i] % 9, aOuPos, 0, -1) == -1) {
                                            int rdx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, aOuPos / 9);
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -rdx, 0, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    }
                                }
                            } else { // 右
                                if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], (aOuPos / 9) * 9 + ban.putHisya[(int)turn * 2 + i] % 9, -1, 0) == -1) {
                                    if (dy < 0) {
                                        if (chkRectMove(ref ban, turn, (aOuPos / 9) * 9 + ban.putHisya[(int)turn * 2 + i] % 9, aOuPos, 0, 1) == -1) {
                                            int rdx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, aOuPos / 9);
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -rdx, 0, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    } else {
                                        if (chkRectMove(ref ban, turn, (aOuPos / 9) * 9 + ban.putHisya[(int)turn * 2 + i] % 9, aOuPos, 0, -1) == -1) {
                                            int rdx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, aOuPos / 9);
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -rdx, 0, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    }
                                }
                            }
                            //段移動
                            if (dy < 0) { // 下
                                if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], (ban.putHisya[(int)turn * 2 + i] / 9) * 9 + aOuPos % 9, 0, 1) == -1) {
                                    if (dx < 0) {
                                        if (chkRectMove(ref ban, turn, (ban.putHisya[(int)turn * 2 + i] / 9) * 9 + aOuPos % 9, aOuPos, 1, 0) == -1) {
                                            int rdy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, aOuPos % 9);
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -rdy, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    } else {
                                        if (chkRectMove(ref ban, turn, (ban.putHisya[(int)turn * 2 + i] / 9) * 9 + aOuPos % 9, aOuPos, -1, 0) == -1) {
                                            int rdy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, aOuPos % 9);
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -rdy, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    }
                                }


                            } else { // 上
                                if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], (ban.putHisya[(int)turn * 2 + i] / 9) * 9 + aOuPos % 9, 0, -1) == -1) {
                                    if (dx < 0) {
                                        if (chkRectMove(ref ban, turn, (ban.putHisya[(int)turn * 2 + i] / 9) * 9 + aOuPos % 9, aOuPos, 1, 0) == -1) {
                                            int rdy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, aOuPos % 9);
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -rdy, turn, mType.Both, kmv, ref kCnt);
                                        }
                                    } else {
                                        if (chkRectMove(ref ban, turn, (ban.putHisya[(int)turn * 2 + i] / 9) * 9 + aOuPos % 9, aOuPos, -1, 0) == -1) {
                                            int rdy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, aOuPos % 9);
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -rdy, turn, mType.Both, kmv, ref kCnt);
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
                                if (dy > 0) {
                                    //DebugForm.instance.addMsg("111:" + dx + "," + dy);
                                    if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1), aOuPos, 0, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + ban.putHisya[(int)turn * 2 + i] % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, 1), 0, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy + 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy - 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, 1), 0, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy - 1, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else {
                                    //DebugForm.instance.addMsg("222:" + dx + "," + dy);
                                    if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1), aOuPos, 0, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + ban.putHisya[(int)turn * 2 + i] % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, -1), 0, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy + 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy - 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, -1), 0, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy - 1, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dx == 1) {
                                if (dy > 0) {
                                    //DebugForm.instance.addMsg("333:" + dx + "," + dy);
                                    if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1), aOuPos, 0, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + ban.putHisya[(int)turn * 2 + i] % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, 1), 0, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy + 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy - 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, 1), 0, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy - 1, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else {
                                    //DebugForm.instance.addMsg("444:" + dx + "," + dy);
                                    if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1), aOuPos, 0, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + ban.putHisya[(int)turn * 2 + i] % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, -1), 0, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy - 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy + 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, -1), 0, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy - 1, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) &&
                                                    (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy + 1, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dy == -1) {
                                if (dx > 0) {
                                    //DebugForm.instance.addMsg("555:" + dx + "," + dy);
                                    if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1), aOuPos, -1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1)] == 0) &&
                                                    (ban.onBoard[(ban.putHisya[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, -1), -1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx + 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) &&
                                                    (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx - 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, -1), -1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx + 1, 0, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) &&
                                                    (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx - 1, 0, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else {
                                    //DebugForm.instance.addMsg("666:" + dx + "," + dy);
                                    if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1), aOuPos, 1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1)] == 0) &&
                                                    (ban.onBoard[(ban.putHisya[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, 1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, -1), 1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx - 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) &&
                                                    (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx + 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, -1), 1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx - 1, 0, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) &&
                                                    (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx + 1, 0, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dy == 1) {
                                if (dx > 0) {
                                    //DebugForm.instance.addMsg("777:" + dx + "," + dy);
                                    if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1), aOuPos, -1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, -1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1)] == 0) &&
                                                    (ban.onBoard[(ban.putHisya[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, 1), -1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx + 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) &&
                                                    (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx - 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, 1), -1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx + 1, 0, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) &&
                                                    (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx - 1, 0, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else {
                                    //DebugForm.instance.addMsg("888:" + dx + "," + dy);
                                    if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                                        // 竜王を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1), aOuPos, 1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, ban.putHisya[(int)turn * 2 + i] / 9, 1) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1)] == 0) &&
                                                    (ban.onBoard[(ban.putHisya[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putHisya[(int)turn * 2 + i] % 9, -1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        // 竜王を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, 1), 1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx - 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) &&
                                                    (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx + 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        // 飛車を王の隣に移動
                                        if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, 1), 1, 0) == -1) {
                                            addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx - 1, 0, turn, mType.Nari, kmv, ref kCnt);
                                            if ((ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) &&
                                                    (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0)) {
                                                addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx + 1, 0, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            }

                        }

                        if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Hisya) {
                            //×××××
                            //×①×②×
                            //××●××
                            //×③×④×
                            //×××××
                            dx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, aOuPos / 9);
                            dy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, aOuPos % 9);
                            switch (dx, dy) {
                                case (-1, 1):
                                    if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) {
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 2, 0, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0) {
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -2, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    break;
                                case (1, 1):
                                    if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0) {
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -2, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) {
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -2, 0, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    break;
                                case (-1, -1):
                                    if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0) {
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, 2, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) {
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 2, 0, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    break;
                                case (1, -1):
                                    if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) {
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -2, 0, turn, mType.Nari, kmv, ref kCnt);
                                    }
                                    if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0) {
                                        addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, 2, turn, mType.Nari, kmv, ref kCnt);
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
                            dx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, aOuPos / 9);
                            dy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, aOuPos % 9);
                            switch (dx, dy) {
                                case (-2, 2):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (0, 2):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (2, 2):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-2, 0):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (2, 0):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-2, -2):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (0, -2):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (2, -2):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if (ban.putKakugyou[(int)turn * 2 + i] != 0xFF) {
                        int dx = pturn.dfX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, aOuPos / 9);
                        int dy = pturn.dfY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, aOuPos % 9);
                        int ret;
                        if (dx == dy) {// 同じ右斜め(／)
                                       // [不成&成り]敵を取って直進
                            if (dy < 0) { // 前方
                                ret = chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], aOuPos, 1, 1);
                                //Debug.WriteLine("JOSEKI HITa:({0})\n", ret);
                                if (ret >= 0) {
                                    int rdy = pturn.dfY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, ret % 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret / 9 - ret % 9 != _kmv[j].np / 9 - _kmv[j].np % 9) {
                                                _kmv[j].val = ban.putKakugyou[(int)turn * 2 + i];
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            } else { // 後方
                                ret = chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], aOuPos, -1, -1);
                                //Debug.WriteLine("JOSEKI HITb:({0})\n", ret);
                                if (ret >= 0) {
                                    int rdy = pturn.dfY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, ret % 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret / 9 != _kmv[j].np / 9) {
                                                _kmv[j].val = ban.putKakugyou[(int)turn * 2 + i];
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            }

                        } else if (dx == -dy) {// 同じ左斜め(＼)
                                               // [不成&成り]敵を取って直進
                            if (dx < 0) { // 左
                                ret = chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], aOuPos, 1, -1);
                                //Debug.WriteLine("JOSEKI HITc:({0})\n", ret);
                                if (ret >= 0) {
                                    int rdx = pturn.dfX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, ret / 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -rdx, rdx, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret % 9 != _kmv[j].np % 9) {
                                                _kmv[j].val = ban.putKakugyou[(int)turn * 2 + i];
                                                kmv[kCnt++] = _kmv[j];
                                            }
                                        }
                                    }
                                }
                            } else { // 右
                                ret = chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], aOuPos, -1, 1);
                                //Debug.WriteLine("JOSEKI HITd:({0})\n", ret);
                                if (ret >= 0) {
                                    int rdx = pturn.dfX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, ret / 9);
                                    if (ban.getOnBoardPturn(ret / 9, ret % 9) != turn) { // 敵の駒がある-> 駒を取る
                                        addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -rdx, rdx, turn, mType.Both, kmv, ref kCnt);
                                    } else { // 味方の駒がある-> 駒を移動する(空き王手)
                                        int _kCnt = 0;
                                        int _startPoint = 100;
                                        kmove[] _kmv = new kmove[200];
                                        getEachMoveList(ref ban, ret, turn, emv, _kmv, ref _kCnt, ref _startPoint); // TODO : 
                                        for (int j = _startPoint; j < _startPoint + _kCnt; j++) {//重なる手は追加しない
                                            if (ret % 9 != _kmv[j].np % 9) {
                                                _kmv[j].val = ban.putKakugyou[(int)turn * 2 + i];
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
                            int mx = (aOuPos / 9 + aOuPos % 9 + ban.putKakugyou[(int)turn * 2 + i] / 9 - ban.putKakugyou[(int)turn * 2 + i] % 9) / 2;
                            int my = (aOuPos / 9 + aOuPos % 9 - ban.putKakugyou[(int)turn * 2 + i] / 9 + ban.putKakugyou[(int)turn * 2 + i] % 9) / 2;
                            //右斜め(／)移動
                            if ((mx > -1) && (my > -1) && (mx < 9) && (my < 9)) {
                                if (pturn.dfX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, mx) < 0) { //左
                                    if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], mx * 9 + my, 1, 1) == -1) {
                                        int rdy = pturn.dfY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, my % 9);
                                        if (pturn.dfY(turn, my % 9, aOuPos % 9) < 0) {   // 相手より下側
                                            if (chkRectMove(ref ban, turn, mx * 9 + my, aOuPos, -1, 1) == -1) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
                                            }

                                        } else {
                                            if (chkRectMove(ref ban, turn, mx * 9 + my, aOuPos, 1, -1) == -1) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], mx * 9 + my, -1, -1) == -1) {
                                        int rdy = pturn.dfY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, my % 9);
                                        if (pturn.dfY(turn, my % 9, aOuPos % 9) < 0) {
                                            if (chkRectMove(ref ban, turn, mx * 9 + my, aOuPos, -1, 1) == -1) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
                                            }


                                        } else {
                                            if (chkRectMove(ref ban, turn, mx * 9 + my, aOuPos, 1, -1) == -1) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
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
                            mx = (aOuPos / 9 - aOuPos % 9 + ban.putKakugyou[(int)turn * 2 + i] / 9 + ban.putKakugyou[(int)turn * 2 + i] % 9) / 2;
                            my = (-aOuPos / 9 + aOuPos % 9 + ban.putKakugyou[(int)turn * 2 + i] / 9 + ban.putKakugyou[(int)turn * 2 + i] % 9) / 2;
                            if ((mx > -1) && (my > -1) && (mx < 9) && (my < 9)) {
                                if (pturn.dfX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, mx) < 0) { // 下
                                    if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], mx * 9 + my, 1, -1) == -1) {
                                        int rdy = pturn.dfY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, my % 9);
                                        if (pturn.dfY(turn, my % 9, aOuPos % 9) < 0) {
                                            if (chkRectMove(ref ban, turn, mx * 9 + my, aOuPos, 1, 1) == -1) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
                                            }
                                        } else {
                                            if (chkRectMove(ref ban, turn, mx * 9 + my, aOuPos, -1, -1) == -1) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 上
                                    if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], mx * 9 + my, -1, 1) == -1) {
                                        int rdy = pturn.dfY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, my % 9);
                                        if (pturn.dfY(turn, my % 9, aOuPos % 9) < 0) {
                                            if (chkRectMove(ref ban, turn, mx * 9 + my, aOuPos, 1, 1) == -1) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
                                            }

                                        } else {
                                            if (chkRectMove(ref ban, turn, mx * 9 + my, aOuPos, -1, -1) == -1) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], rdy, -rdy, turn, mType.Both, kmv, ref kCnt);
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
                            // TODO: 要実装
                            if (dx - dy == 1) {
                                //DebugForm.instance.addMsg("111:" + dx + "," + dy);
                                if (dx < 0) { // 左
                                    if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, (ban.putKakugyou[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, 1), aOuPos, 1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[(ban.putKakugyou[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, 1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], (aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1), 1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dx, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dy, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], (aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1), 1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dx, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dy, -dy, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, -1) * 9 + ban.putKakugyou[(int)turn * 2 + i] % 9, aOuPos, -1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, -1) * 9 + ban.putKakugyou[(int)turn * 2 + i] % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9, -1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dy, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9, -1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dy, -dy, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dx - dy == -1) {
                                //DebugForm.instance.addMsg("333:" + dx + "," + dy);
                                if (dx < 0) { // 左
                                    if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, 1) * 9 + ban.putKakugyou[(int)turn * 2 + i] % 9, aOuPos, 1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, 1) * 9 + ban.putKakugyou[(int)turn * 2 + i] % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9, 1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dy, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9, 1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dy, -dy, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, (ban.putKakugyou[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, -1), aOuPos, -1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[(ban.putKakugyou[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, -1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], (aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1), -1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dx, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dy, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], (aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1), -1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dx, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dy, -dy, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dx + dy == 1) {
                                //DebugForm.instance.addMsg("555:" + dx + "," + dy);
                                if (dx < 0) { // 左
                                    if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, (ban.putKakugyou[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, -1), aOuPos, 1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[(ban.putKakugyou[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, -1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], (aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1), 1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, dx, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], dy, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], (aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1), 1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, dx, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, 1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], dy, -dy, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, -1) * 9 + ban.putKakugyou[(int)turn * 2 + i] % 9, aOuPos, -1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, -1) * 9 + ban.putKakugyou[(int)turn * 2 + i] % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9, -1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], dy, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9, -1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], dy, -dy, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, 1) * 9 + aOuPos % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            } else if (dx + dy == -1) {
                                //DebugForm.instance.addMsg("777:" + dx + "," + dy);
                                if (dx < 0) { // 左
                                    if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, pturn.mvX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, 1) * 9 + ban.putKakugyou[(int)turn * 2 + i] % 9, aOuPos, 1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, 1) * 9 + ban.putKakugyou[(int)turn * 2 + i] % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9, 1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], dy, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, dx, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9, 1, -1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], dy, -dy, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.onBoard[pturn.mvX(turn, aOuPos / 9, -1) * 9 + aOuPos % 9] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, dx, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                } else { // 右
                                    if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                                        // 竜馬を一つ移動
                                        if (chkRectMove(ref ban, turn, (ban.putKakugyou[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, 1), aOuPos, -1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[(ban.putKakugyou[(int)turn * 2 + i] / 9) * 9 + pturn.mvY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, 1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                        //竜馬を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], (aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1), -1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, dx, turn, mType.NoNari, kmv, ref kCnt);
                                            if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], dy, -dy, turn, mType.NoNari, kmv, ref kCnt);
                                            }
                                        }
                                    } else {
                                        //角を王の隣へ移動
                                        if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], (aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1), -1, 1) == -1) {
                                            addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, dx, turn, mType.Nari, kmv, ref kCnt);
                                            if (ban.onBoard[(aOuPos / 9) * 9 + pturn.mvY(turn, aOuPos % 9, -1)] == 0) {
                                                addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], dy, -dy, turn, mType.Nari, kmv, ref kCnt);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Kakugyou) {
                            // 角行特有の王手
                            //×××××
                            //××①××
                            //×②●③×
                            //××④××
                            //×××××
                            switch (dx, dy) {
                                case (0, 1):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, -1, turn, mType.Nari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, -1, turn, mType.Nari, kmv, ref kCnt);
                                    break;
                                case (-1, 0):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, -1, turn, mType.Nari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, 1, turn, mType.Nari, kmv, ref kCnt);
                                    break;
                                case (1, 0):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, 1, turn, mType.Nari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, -1, turn, mType.Nari, kmv, ref kCnt);
                                    break;
                                case (0, -1):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, 1, turn, mType.Nari, kmv, ref kCnt);
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, 1, turn, mType.Nari, kmv, ref kCnt);
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
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-2, 0):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (2, 0):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (0, -2):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                // 歩打ち
                if (ban.captPiece[(int)turn * 7 + 0] > 0) {
                    // 二歩チェック
                    if (ban.putFuhyou[(int)turn * 9 + (aOuPos / 9)] == 9) {
                        addCheckPutPos(ref ban, ktype.Fuhyou, aOuPos, 0, -1, turn, kmv, ref kCnt);
                    }
                }

                // 香打ち
                if (ban.captPiece[(int)turn * 7 + 1] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kyousha, aOuPos, 0, -i, turn, kmv, ref kCnt);
                    }
                }

                // 桂打ち
                if (ban.captPiece[(int)turn * 7 + 2] > 0) {
                    addCheckPutPos(ref ban, ktype.Keima, aOuPos, 1, -2, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Keima, aOuPos, -1, -2, turn, kmv, ref kCnt);
                }

                // 銀打ち
                if (ban.captPiece[(int)turn * 7 + 3] > 0) {
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, -1, 1, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, 1, 1, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, 1, -1, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, 0, -1, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Ginsyou, aOuPos, -1, -1, turn, kmv, ref kCnt);
                }

                // 飛打ち
                if (ban.captPiece[(int)turn * 7 + 4] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOuPos, 0, -i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOuPos, 0, i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOuPos, -i, 0, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Hisya, aOuPos, i, 0, turn, kmv, ref kCnt);
                    }
                }

                // 角打ち
                if (ban.captPiece[(int)turn * 7 + 5] > 0) {
                    int ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOuPos, -i, -i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOuPos, i, -i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOuPos, -i, i, turn, kmv, ref kCnt);
                    }
                    ret = 0;
                    for (int i = 1; ret == 0; i++) {
                        ret = addCheckPutPos(ref ban, ktype.Kakugyou, aOuPos, i, i, turn, kmv, ref kCnt);
                    }
                }

                // 金打ち
                if (ban.captPiece[(int)turn * 7 + 6] > 0) {
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, 0, 1, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, -1, 0, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, 1, 0, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, -1, -1, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, 0, -1, turn, kmv, ref kCnt);
                    addCheckPutPos(ref ban, ktype.Kinsyou, aOuPos, 1, -1, turn, kmv, ref kCnt);
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
        int getAllDefList(ref ban ban, Pturn turn, kmove[] kmv, int cPos) {
            int kCnt = 0;
            unsafe {
                if (ban.moveable[pturn.aturn((int)turn) * 81 + ban.putOusyou[(int)turn]] == 1) {
                    //DebugForm.instance.addMsg("cPos = " + cPos + " " + ban.getOnBoardKtype(cPos));
                    // 駒を取る
                    getPosMoveList(ref kCnt, ref ban, turn, cPos, kmv);

                    // 合い駒
                    int dx = (cPos / 9) - (ban.putOusyou[(int)turn] / 9);
                    int dy = (cPos % 9) - (ban.putOusyou[(int)turn] % 9);
                    switch (ban.getOnBoardKtype(cPos)) {
                        case ktype.Kyousha:
                            if (dy > 1) {
                                for (int i = 1; ((cPos % 9) - i) > (ban.putOusyou[(int)turn] % 9); i++) {
                                    getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9) * 9 + (cPos % 9) - i, kmv);//合い駒を移動
                                    getPosPutList(ref kCnt, ref ban, turn, (cPos / 9) * 9 + (cPos % 9) - i, kmv);//合い駒を打つ
                                }
                            } else if (dy < 1) {
                                for (int i = 1; ((cPos % 9) + i) < (ban.putOusyou[(int)turn] % 9); i++) {
                                    getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9) * 9 + (cPos % 9) + i, kmv);//合い駒を移動
                                    getPosPutList(ref kCnt, ref ban, turn, (cPos / 9) * 9 + (cPos % 9) + i, kmv);//合い駒を打つ
                                }
                            }
                            break;
                        case ktype.Ryuuou:
                        case ktype.Hisya:
                            // 敵基準で判定
                            if (dx == 0) {
                                if (dy > 1) {
                                    for (int i = 1; ((cPos % 9) - i) > (ban.putOusyou[(int)turn] % 9); i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9) * 9 + (cPos % 9) - i, kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (cPos / 9) * 9 + (cPos % 9) - i, kmv);//合い駒を打つ
                                    }
                                } else if (dy < 1) {
                                    for (int i = 1; ((cPos % 9) + i) < (ban.putOusyou[(int)turn] % 9); i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9) * 9 + (cPos % 9) + i, kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (cPos / 9) * 9 + (cPos % 9) + i, kmv);//合い駒を打つ
                                    }
                                }
                            } else if (dy == 0) {
                                if (dx > 1) {
                                    for (int i = 1; ((cPos / 9) - i) > (ban.putOusyou[(int)turn] / 9); i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9 - i) * 9 + (cPos % 9), kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (cPos / 9 - i) * 9 + (cPos % 9), kmv);//合い駒を打つ
                                    }
                                } else if (dx < 1) {
                                    for (int i = 1; ((cPos / 9) + i) < (ban.putOusyou[(int)turn] / 9); i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9 + i) * 9 + (cPos % 9), kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (cPos / 9 + i) * 9 + (cPos % 9), kmv);//合い駒を打つ
                                    }
                                }
                            }
                            break;

                        case ktype.Ryuuma:
                        case ktype.Kakugyou:
                            //DebugForm.instance.addMsg("pturn.mvXY = " + (dx + 1) + "," + (dy + 1));
                            // 敵基準で判定
                            if (dx == dy) { // 右斜め(／)
                                if (dx > 1) {
                                    for (int i = 1; ((cPos / 9) - i) > (ban.putOusyou[(int)turn] / 9); i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9 - i) * 9 + (cPos % 9) - i, kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (cPos / 9 - i) * 9 + (cPos % 9) - i, kmv);//合い駒を打つ
                                    }
                                } else if (dx < 1) {
                                    for (int i = 1; ((cPos / 9) + i) < (ban.putOusyou[(int)turn] / 9); i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9 + i) * 9 + (cPos % 9) + i, kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (cPos / 9 + i) * 9 + (cPos % 9) + i, kmv);//合い駒を打つ
                                    }
                                }
                            } else if (dx == -dy) { // 左斜め(／)
                                if (dx > 1) {
                                    for (int i = 1; ((cPos / 9) - i) > (ban.putOusyou[(int)turn] / 9); i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9 - i) * 9 + (cPos % 9) + i, kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (cPos / 9 - i) * 9 + (cPos % 9) + i, kmv);//合い駒を打つ
                                    }
                                } else if (dx < 1) {
                                    for (int i = 1; ((cPos / 9) + i) < (ban.putOusyou[(int)turn] / 9); i++) {
                                        getPosMoveList(ref kCnt, ref ban, turn, (cPos / 9 + i) * 9 + (cPos % 9) - i, kmv);//合い駒を移動
                                        getPosPutList(ref kCnt, ref ban, turn, (cPos / 9 + i) * 9 + (cPos % 9) - i, kmv);//合い駒を打つ
                                    }
                                }
                            }
                            break;

                        default:
                            break;
                    }
                }


                // 王を移動(8方向)
                addCheckMovePos(ref ban, ban.putOusyou[(int)turn], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, ban.putOusyou[(int)turn], 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, ban.putOusyou[(int)turn], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, ban.putOusyou[(int)turn], 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, ban.putOusyou[(int)turn], -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, ban.putOusyou[(int)turn], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, ban.putOusyou[(int)turn], 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                addCheckMovePos(ref ban, ban.putOusyou[(int)turn], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
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
        void getPosMoveList(ref int kCnt, ref ban ban, Pturn turn, int cPos, kmove[] kmv) {
            unsafe {

                int DposX;  // 守り駒のX位置
                int DposY;  // 守り駒のY位置
                int cPosX = cPos / 9; //移動対象のX位置
                int cPosY = cPos % 9; //移動対象のY位置

                //01 02 03
                //04 ● 05
                //06 07 08
                //09 × 10

                if (pturn.psY(turn, cPosY) < 8) {
                    // 01
                    DposX = pturn.mvX(turn, cPosX, -1);
                    DposY = pturn.mvY(turn, cPosY, 1);
                    if ((pturn.psX(turn, cPosX) > 0) && (ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                        if (ban.getOnBoardKtype(DposX * 9 + DposY) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, DposX * 9 + DposY, 1, -1, turn, mType.Both, kmv, ref kCnt);
                        }
                    }

                    // 02
                    DposX = cPosX;
                    if ((ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                        switch (ban.getOnBoardKtype(DposX * 9 + DposY)) {
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, DposX * 9 + DposY, 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 03
                    DposX = pturn.mvX(turn, cPosX, 1);
                    if ((pturn.psX(turn, cPosX) < 8) && (ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                        if (ban.getOnBoardKtype(DposX * 9 + DposY) == ktype.Ginsyou) { //銀将のみ
                            addCheckMovePos(ref ban, DposX * 9 + DposY, -1, -1, turn, mType.Both, kmv, ref kCnt);
                        }
                    }
                }

                // 04
                DposX = pturn.mvX(turn, cPosX, -1);
                DposY = cPosY;
                if ((pturn.psX(turn, cPosX) > 0) && (ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                    switch (ban.getOnBoardKtype(DposX * 9 + DposY)) {
                        case (ktype.Kinsyou):  //金将
                        case (ktype.Tokin):    //と金
                        case (ktype.Narikyou): //成香
                        case (ktype.Narikei):  //成桂
                        case (ktype.Narigin):  //成銀
                            addCheckMovePos(ref ban, DposX * 9 + DposY, 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        default:
                            break;
                    }
                }

                // 05
                DposX = pturn.mvX(turn, cPosX, 1);
                if ((pturn.psX(turn, cPosX) < 8) && (ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                    switch (ban.getOnBoardKtype(DposX * 9 + DposY)) {
                        case (ktype.Kinsyou):  //金将
                        case (ktype.Tokin):    //と金
                        case (ktype.Narikyou): //成香
                        case (ktype.Narikei):  //成桂
                        case (ktype.Narigin):  //成銀
                            addCheckMovePos(ref ban, DposX * 9 + DposY, -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                            break;
                        default:
                            break;
                    }
                }


                if (pturn.psY(turn, cPosY) > 0) {
                    // 06
                    DposX = pturn.mvX(turn, cPosX, -1);
                    DposY = pturn.mvY(turn, cPosY, -1);
                    if ((pturn.psX(turn, cPosX) > 0) && (ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                        switch (ban.getOnBoardKtype(DposX * 9 + DposY)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, DposX * 9 + DposY, 1, 1, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, DposX * 9 + DposY, 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }

                    // 07
                    DposX = cPosX;
                    if ((ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                        switch (ban.getOnBoardKtype(DposX * 9 + DposY)) {
                            case (ktype.Fuhyou):  //歩兵
                                addCheckMovePos(ref ban, DposX * 9 + DposY, 0, 1, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, DposX * 9 + DposY, 0, 1, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, DposX * 9 + DposY, 0, 1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }
                    // 08
                    DposX = pturn.mvX(turn, cPosX, 1);
                    if ((pturn.psX(turn, cPosX) < 8) && (ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                        switch (ban.getOnBoardKtype(DposX * 9 + DposY)) {
                            case (ktype.Ginsyou): //銀将
                                addCheckMovePos(ref ban, DposX * 9 + DposY, -1, 1, turn, mType.Both, kmv, ref kCnt);
                                break;
                            case (ktype.Kinsyou):  //金将
                            case (ktype.Tokin):    //と金
                            case (ktype.Narikyou): //成香
                            case (ktype.Narikei):  //成桂
                            case (ktype.Narigin):  //成銀
                                addCheckMovePos(ref ban, DposX * 9 + DposY, -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (pturn.psY(turn, cPosY) > 1) {
                    // 09
                    DposX = pturn.mvX(turn, cPosX, -1);
                    DposY = pturn.mvY(turn, cPosY, -2);
                    if ((pturn.psX(turn, cPosX) > 0) && (ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                        if (ban.getOnBoardKtype(DposX * 9 + DposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, DposX * 9 + DposY, 1, 2, turn, mType.Both, kmv, ref kCnt);
                        }
                    }

                    // 10
                    DposX = pturn.mvX(turn, cPosX, 1);
                    if ((pturn.psX(turn, cPosX) < 8) && (ban.onBoard[DposX * 9 + DposY] > 0) && (ban.getOnBoardPturn(DposX, DposY) == turn)) {
                        if (ban.getOnBoardKtype(DposX * 9 + DposY) == ktype.Keima) { //桂馬のみ
                            addCheckMovePos(ref ban, DposX * 9 + DposY, -1, 2, turn, mType.Both, kmv, ref kCnt);
                        }
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if (ban.putHisya[(int)turn * 2 + i] != 0xFF) {
                        int dx = pturn.dfX(turn, ban.putHisya[(int)turn * 2 + i] / 9, cPos / 9);
                        int dy = pturn.dfY(turn, ban.putHisya[(int)turn * 2 + i] % 9, cPos % 9);
                        if (dx == 0) {
                            if (dy < 0) {
                                if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], cPos, 0, 1) == -1) {
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            } else { // (dy > 0)
                                if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], cPos, 0, -1) == -1) {
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 0, -dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        } else if (dy == 0) {
                            if (dx < 0) {
                                if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], cPos, 1, 0) == -1) {
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx, 0, turn, mType.Both, kmv, ref kCnt);
                                }
                            } else { // (dx > 0)
                                if (chkRectMove(ref ban, turn, ban.putHisya[(int)turn * 2 + i], cPos, -1, 0) == -1) {
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -dx, 0, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        }
                        // 竜王の動き
                        if (ban.getOnBoardKtype(ban.putHisya[(int)turn * 2 + i]) == ktype.Ryuuou) {
                            switch (dx, dy) {
                                case (-1, 1):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (1, 1):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-1, -1):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], 1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (1, -1):
                                    addCheckMovePos(ref ban, ban.putHisya[(int)turn * 2 + i], -1, 1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                default:
                                    break;
                            }
                        }

                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if (ban.putKakugyou[(int)turn * 2 + i] != 0xFF) {
                        int dx = pturn.dfX(turn, ban.putKakugyou[(int)turn * 2 + i] / 9, cPos / 9);
                        int dy = pturn.dfY(turn, ban.putKakugyou[(int)turn * 2 + i] % 9, cPos % 9);
                        if (dx == dy) {
                            if (dy < 0) {
                                if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], cPos, 1, 1) == -1) {
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            } else { // (dy > 0)
                                if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], cPos, -1, -1) == -1) {
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        } else if (dx == -dy) {
                            if (dx < 0) {
                                if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], cPos, 1, -1) == -1) {
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            } else { // (dx > 0)
                                if (chkRectMove(ref ban, turn, ban.putKakugyou[(int)turn * 2 + i], cPos, -1, 1) == -1) {
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -dx, -dy, turn, mType.Both, kmv, ref kCnt);
                                }
                            }
                        }
                        // 竜馬の動き
                        if (ban.getOnBoardKtype(ban.putKakugyou[(int)turn * 2 + i]) == ktype.Ryuuma) {
                            switch (dx, dy) {
                                case (0, 1):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, -1, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (1, 0):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], -1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (-1, 0):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 1, 0, turn, mType.NoNari, kmv, ref kCnt);
                                    break;
                                case (0, -1):
                                    addCheckMovePos(ref ban, ban.putKakugyou[(int)turn * 2 + i], 0, 1, turn, mType.NoNari, kmv, ref kCnt);
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
        /// <param name="cPos">駒を置く位置(x*9+y)</param>
        /// <param name="kmv">駒移動候補リスト</param>
        void getPosPutList(ref int kCnt, ref ban ban, Pturn turn, int cPos, kmove[] kmv) {
            unsafe {
                // 歩打ち
                if ((ban.captPiece[(int)turn * 7 + 0] > 0) && (pturn.psY(turn, cPos % 9) < 8)) {
                    // 二歩チェック
                    if (ban.putFuhyou[(int)turn * 9 + (cPos / 9)] == 9) {
                        kmv[kCnt++].set(81 + (int)ktype.Fuhyou, cPos, 0, 0, false, turn);
                    }
                }

                // 香打ち
                if ((ban.captPiece[(int)turn * 7 + 1] > 0) && (pturn.psY(turn, cPos % 9) < 8)) {
                    kmv[kCnt++].set(81 + (int)ktype.Kyousha, cPos, 0, 0, false, turn);
                }

                // 桂打ち
                if ((ban.captPiece[(int)turn * 7 + 2] > 0) && (pturn.psY(turn, cPos % 9) < 7)) {
                    kmv[kCnt++].set(81 + (int)ktype.Keima, cPos, 0, 0, false, turn);
                }

                // 銀打ち
                if (ban.captPiece[(int)turn * 7 + 3] > 0) {
                    kmv[kCnt++].set(81 + (int)ktype.Ginsyou, cPos, 0, 0, false, turn);
                }

                // 飛打ち
                if (ban.captPiece[(int)turn * 7 + 4] > 0) {
                    kmv[kCnt++].set(81 + (int)ktype.Hisya, cPos, 0, 0, false, turn);
                }

                // 角打ち
                if (ban.captPiece[(int)turn * 7 + 5] > 0) {
                    kmv[kCnt++].set(81 + (int)ktype.Kakugyou, cPos, 0, 0, false, turn);
                }

                // 金打ち
                if (ban.captPiece[(int)turn * 7 + 6] > 0) {
                    kmv[kCnt++].set(81 + (int)ktype.Kinsyou, cPos, 0, 0, false, turn);
                }

            }
        }
        /// <summary>
        /// oPosの駒が自分中心位置で(mx,my)動かすことができるか
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
        public int addCheckMovePos(ref ban ban, int oPos, int mx, int my, Pturn turn, mType nari, kmove[] kmv, ref int kCnt) {
            int nx = pturn.mvX(turn, oPos / 9, mx);
            int ny = pturn.mvY(turn, oPos % 9, my);
            unsafe {
                if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 3;
                int nPos = nx * 9 + ny;
                if (((ban.onBoard[nPos] > 0)) && (ban.getOnBoardPturn(nx, ny) == turn)) return 2; // 味方の駒

                //成り
                if (nari >= mType.Both) {
                    //成れない場所・成れない駒は不可(飛角香のためコンティニュー可能にする)
                    if ((ban.getOnBoardKtype(oPos) < ktype.Kinsyou) && ((pturn.psY(turn, oPos % 9) > 5) || (pturn.psY(turn, ny) > 5))) {
                        kmv[kCnt++].set(oPos, nPos, nPos, 0, true, turn);
                    }
                }

                //不成
                if (nari <= mType.Both) {
                    if (((pturn.psY(turn, ny) == 8) && ((ban.getOnBoardKtype(oPos) == ktype.Kyousha) || (ban.getOnBoardKtype(oPos) == ktype.Fuhyou))) ||
                        ((pturn.psY(turn, ny) > 6) && (ban.getOnBoardKtype(oPos) == ktype.Keima))) return 3; //不成NG
                    kmv[kCnt++].set(oPos, nPos, nPos, 0, false, turn);
                }

                if (ban.onBoard[nPos] > 0) {
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
        int addCheckPutPos(ref ban ban, ktype type, int tPos, int mx, int my, Pturn turn, kmove[] kmv, ref int kCnt) {
            unsafe {
                (int nx, int ny) = pturn.mvXY(turn, tPos / 9, tPos % 9, mx, my);
                if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 2;
                int nPos = nx * 9 + ny;
                if (ban.onBoard[nPos] > 0) return 1;
                kmv[kCnt++].set(81 + (int)type, nPos, nPos, 0, false, turn); //移動候補リストに追加
                return 0;
            }
        }

        // 指定先まで駒が存在するかチェック(指定先含めず)
        // 0～80:指定位置(X*9+Y)に駒あり -1 :駒無し -2 :駒2個以上あり -3 :opにたどり着かない
        int chkRectMove(ref ban ban, Pturn turn, int op, int np, int mx, int my) {
            //DebugForm.instance.addMsg("chkRectMove = " + ((op / 9) + 1) + "," + ((op % 9) + 1) + "/" + ((np / 9) + 1) + "," + ((np % 9) + 1) + " mv=" + mx + "," + my);
            unsafe {
                int ret = -1;
                int nx = op / 9;
                int ny = op % 9;
                for (int i = 0; ; i++) {
                    (nx, ny) = pturn.mvXY(turn, nx, ny, mx, my);
                    if (nx * 9 + ny == np) return ret;
                    if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return -3;
                    //DebugForm.instance.addMsg("pturn.mvXY = " + (nx + 1) + "," + (ny + 1));
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

    }
}
