namespace TacoWin2 {

    static class mList {
        public static int[] lsCnt;
        public static int[,] lsNum;
        public static kmove[][] ls = new kmove[100][]; // 移動候補リスト

        static mList() {
            for (int i = 0; i < 100; i++) {
                ls[i] = new kmove[250];
            }
        }

    }

    class tw2mem {





    }
}
