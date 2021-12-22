using System;

namespace TacoWin2 {

    static class mList {
        public static int aListCnt = 0;
        public static int rListCnt = 0;
        const int aListMax = 100000;
        const int aListElMax = 500;
        const int rListMax = 100000;
        const int rListElMax = 100;

        public static int[,] lsNum;
        public static kmove[][] aList = new kmove[aListMax][]; // 移動候補リスト
        public static kmove[][] rList = new kmove[rListMax][]; // 探索結果リスト

        static mList() {
            reset();
        }

        public static void reset() {
            Console.Write("mList[a={0}/r={1}]", aListCnt, rListCnt);
            aListCnt = 0;
            rListCnt = 0;
            for (int i = 0; i < aListMax; i++) {
                aList[i] = new kmove[aListElMax];
            }
            for (int i = 0; i < rListMax; i++) {
                rList[i] = new kmove[rListElMax];
            }
        }

        public static kmove[] assignAlist() {
            if (aListCnt < aListMax) {
                return aList[aListCnt++];
            } else {
                Console.Write("assignAlist ERROR[cnt={0}/max={1}]", aListCnt, aListMax);
                aListCnt++;
                return null;
            }
        }

        public static kmove[] assignRlist() {
            if (rListCnt < rListMax) {
                return rList[rListCnt++];
            } else {
                Console.Write("assignRlist ERROR[cnt={0}/max={1}]", rListCnt, rListMax);
                rListCnt++;
                return null;
            }
        }

    }

    class tw2mem {





    }
}
