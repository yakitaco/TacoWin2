using System;
using System.Collections.Generic;
using System.Text;

namespace TacoWin2 {
    class tw2stmove {
        public static kmove rootKmv;  //大元の定跡リスト
        public static kmove currentKmv;  //現在の定跡リスト
        static Random rnd = new Random();

        public static int readFile(string filePath) {
            int ret = -1;
            rootKmv = kmove.load(filePath);
            if (rootKmv != null) ret = 0;
            return ret;
        }

        // 定跡リストを取得
        public static List<koPos> getSmove(BanInfo ban, int teban) {
            BanInfo tmpBan = new BanInfo(ban);
            List<koPos> retList = new List<koPos>();
            kmove tmpKmv = currentKmv;

            for (int cnt = 0; (tmpKmv != null) && (tmpKmv.nxSum > 0) && (cnt < 20); cnt++) {
                int rVal = rnd.Next(0, tmpKmv.nxSum);
                for (int i = 0; i < tmpKmv.nxMove.Count; i++) {
                    if (tmpKmv.nxMove[i].val < 0) continue;
                    if (rVal > tmpKmv.nxMove[i].val) {
                        rVal -= tmpKmv.nxMove[i].val;
                        continue;
                    }
                    Form1.Form1Instance.addMsg("Hit Move : (" + tmpKmv.nxMove[i].ox + "," + tmpKmv.nxMove[i].oy + ")->(" + tmpKmv.nxMove[i].nx + "," + tmpKmv.nxMove[i].ny + "):" + tmpKmv.nxMove[i].val);

                    // 移動チェック
                    int ret;
                    koPos moveKoma;

                    if (tmpKmv.nxMove[i].ox == 9) { // 駒打ち
                        moveKoma = new koPos(tmpKmv.nxMove[i].nx, tmpKmv.nxMove[i].ny);
                        moveKoma.ko = new koma(tmpBan.MochiKo[teban, tmpKmv.nxMove[i].oy - 1][0]);
                        ret = tmpBan.moveKoma(teban, moveKoma.ko.type, moveKoma, true);
                    } else { // 移動
                        koPos src = new koPos(tmpKmv.nxMove[i].ox, tmpKmv.nxMove[i].oy);
                        moveKoma = new koPos(tmpKmv.nxMove[i].nx, tmpKmv.nxMove[i].ny);
                        moveKoma.ko = new koma(tmpBan.BanKo[tmpKmv.nxMove[i].ox, tmpKmv.nxMove[i].oy]);
                        ret = tmpBan.moveKoma(src, moveKoma, tmpKmv.nxMove[i].nari, true);
                    }

                    if (ret == 0) {  //移動OK
                                     // 移動リストに追加
                        retList.Add(moveKoma);
                        tmpKmv = tmpKmv.nxMove[i];
                    } else {  //移動NG
                        Form1.Form1Instance.addMsg("Move chk NG!!");
                        break;
                    }
                    break;
                }
                teban = (teban == TEIGI.TEBAN_SENTE ? TEIGI.TEBAN_GOTE : TEIGI.TEBAN_SENTE);
            }

            return retList;
        }
    }
}
