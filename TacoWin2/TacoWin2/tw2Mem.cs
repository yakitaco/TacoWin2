using System;
using System.Collections.Generic;

namespace TacoWin2 {

    static class mList {
        public static int aListCnt = 0;
        public static int rListCnt = 0;
        const int aListMax = 1000;
        const int aListElMax = 600;
        const int rListMax = 100;
        const int rListElMax = 10;

        public static bool[] aUseList = new bool[aListMax];
        public static bool[] rUseList = new bool[rListMax];

        public static int[,] lsNum;
        //static Queue<kmove[]> aQueue = new Queue<kmove[]>();
        public static kmove[][] aList = new kmove[aListMax][]; // 移動候補リスト
        public static kmove[][] rList = new kmove[rListMax][]; // 探索結果リスト

        static mList() {
            reset();
        }

        public static void reset() {
            //Console.Write("mList[a={0}/r={1}]", aListCnt, rListCnt);
            aListCnt = 0;
            //rListCnt = 0;
            for (int i = 0; i < aListMax; i++) {
                aList[i] = new kmove[aListElMax];
                //aQueue.Enqueue(new kmove[aListElMax]);
            }
            //for (int i = 0; i < rListMax; i++) {
            //    rList[i] = new kmove[rListElMax];
            //}
            aUseList = new bool[aListMax];
            rUseList = new bool[rListMax];
        }

        public static int assignAlist(out kmove[] aLst) {
            //if (aQueue.Count >0 ) {
            //    aLst = aQueue.Dequeue();
            //    return 0;
            //} else {
            //    aListCnt++;
            //    aLst = new kmove[aListElMax];
            //    return -1;
            //}


            for (int cnt = 0; cnt < aListMax; cnt++) {
                if (aUseList[aListCnt] == false) {
                    aLst = aList[aListCnt];
                    aUseList[aListCnt] = true;
                    //Console.Write("assignAlist ERROR[cnt={0}/max={1}]", aListCnt, aListMax);
                    return aListCnt;
                }
                aListCnt++;
                if (aListCnt >= aListMax) aListCnt = 0;
            }
            Console.Write("assignAlist ERROR[cnt={0}/max={1}]", aListCnt, aListMax);
            aLst = null;
            return -1;
        }

        public static int assignRlist(out kmove[] rLst) {
            for (int cnt = 0; cnt < rListMax; cnt++) {
                if (rUseList[rListCnt] == false) {
                    rLst = rList[rListCnt];
                    rUseList[rListCnt] = true;
                    return rListCnt;
                }
                rListCnt++;
                if (rListCnt >= rListMax) rListCnt = 0;
            }
            Console.Write("assignRlist ERROR[cnt={0}/max={1}]", rListCnt, rListMax);
            rLst = null;
            return -1;
        }

        //public static void freeAlist(kmove[] aLst) {
        //    aQueue.Enqueue(aLst);
        //}

        public static void freeAlist(int aid) {
            aUseList[aid] = false;
        }

        public static void freeRlist(int rid) {
            rUseList[rid] = false;
        }

    }

    class tw2mem {





    }
}
