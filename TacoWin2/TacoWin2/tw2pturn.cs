namespace TacoWin2 {
    public enum pturn : byte {
        Sente = 0x00,    //
        Gote  = 0x01,    //
    }

    public static class ptuen {

        // turn考慮位置(X) x 0 上 / 8 下
        public static int psX(pturn t, int x) {
            if (t == pturn.Sente) {
                return 9 - x;
            } else {
                return x;
            }
        }

        // turn考慮位置(Y) x 0 左 / 8 右
        public static int psY(pturn t, int y) {
            if (t == pturn.Sente) {
                return 9 - y;
            } else {
                return y;
            }
        }

        // turn考慮移動(X) moval +右 -左
        public static int mvX(pturn t, int x, int moval) {
            if (t == pturn.Sente) {
                return x - moval;
            } else {
                return x + moval;
            }
        }

        // turn考慮移動(Y) moval +前 -後
        public static int mvY(pturn t, int y, int moval) {
            if (t == pturn.Sente) {
                return y - moval;
            } else {
                return y + moval;
            }
        }

        // 前後の移動
        public static (int, int) mvXY(pturn t, int x, int y, int movalx, int movaly) {
            return (mvX(t,x ,movalx), mvY(t, y, movaly));
        }

    }

}
