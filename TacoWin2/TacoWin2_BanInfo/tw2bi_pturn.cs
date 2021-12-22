using System;
using System.Collections.Generic;
using System.Text;

namespace TacoWin2_BanInfo {
    public enum Pturn : byte {
        Sente = 0x00,    //
        Gote = 0x01,    //
    }

    public static class pturn {

        // turn考慮位置(X) x 0 下 / 8 上
        public static int psX(Pturn t, int x) {
            if (t == Pturn.Sente) {
                return 8 - x;
            } else {
                return x;
            }
        }

        // turn考慮位置(Y) x 0 左 / 8 右
        public static int psY(Pturn t, int y) {
            if (t == Pturn.Sente) {
                return 8 - y;
            } else {
                return y;
            }
        }



        // turn考慮移動(X) moval +右 -左
        public static int mvX(Pturn t, int x, int moval) {
            if (t == Pturn.Sente) {
                return x - moval;
            } else {
                return x + moval;
            }
        }

        // turn考慮移動(Y) moval +前 -後
        public static int mvY(Pturn t, int y, int moval) {
            if (t == Pturn.Sente) {
                return y - moval;
            } else {
                return y + moval;
            }
        }

        // 前後の移動
        public static (int, int) mvXY(Pturn t, int x, int y, int movalx, int movaly) {
            return (mvX(t, x, movalx), mvY(t, y, movaly));
        }

        // 前後の移動(XY統合)[pos=x*9+y/move=movalx*9+movaly]
        public static int mvXY(Pturn t, int pos, int move) {
            if (t == Pturn.Sente) {
                return pos - move;
            } else {
                return pos + move;
            }
        }

        public static int aturn(int turn) {
            return 1 - turn;
        }

        public static Pturn aturn(Pturn turn) {
            return 1 - turn;
        }

    }
}
