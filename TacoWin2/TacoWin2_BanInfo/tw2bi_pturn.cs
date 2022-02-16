using System;
using System.Collections.Generic;
using System.Text;

namespace TacoWin2_BanInfo {
    public enum Pturn : byte {
        ///<summary>先手</summary>
        Sente = 0x00,
        ///<summary>後手</summary>
        Gote = 0x01,
    }

    public static class pturn {

        /// <summary>
        /// 自分中心X位置を取得
        /// </summary>
        /// <param name="t">自分のターン</param>
        /// <param name="x">絶対X位置</param>
        /// <returns>自分中心X位置(0 左側/8 右側)</returns>
        public static int psX(Pturn t, int x) {
            if (t == Pturn.Sente) {
                return 8 - x;
            } else {
                return x;
            }
        }

        /// <summary>
        /// 自分中心Y位置を取得
        /// </summary>
        /// <param name="t">自分のターン</param>
        /// <param name="y">絶対X位置</param>
        /// <returns>自分中心Y位置(0 下側/8 上側)</returns>
        public static int psY(Pturn t, int y) {
            if (t == Pturn.Sente) {
                return 8 - y;
            } else {
                return y;
            }
        }

        /// <summary>
        /// 自分中心X相対位置を取得
        /// </summary>
        /// <param name="turn">自分のターン</param>
        /// <param name="sx">自分の絶対X位置</param>
        /// <param name="dx">相手の絶対X位置</param>
        /// <returns>自分中心X相対位置(+ 自分が相手より左側/- 自分が相手より右側)</returns>
        public static int dfX(Pturn turn, int sx, int dx) {
            if (turn == Pturn.Sente) {
                return dx - sx;
            } else {
                return sx - dx;
            }
        }

        /// <summary>
        /// 自分中心Y相対位置を取得
        /// </summary>
        /// <param name="turn">自分のターン</param>
        /// <param name="sy">自分の絶対Y位置</param>
        /// <param name="dy">相手の絶対Y位置</param>
        /// <returns>自分中心Y相対位置(+ 自分が相手より上側/- 自分が相手より下側)</returns>
        public static int dfY(Pturn turn, int sy, int dy) {
            if (turn == Pturn.Sente) {
                return dy - sy;
            } else {
                return sy - dy;
            }
        }

        /// <summary>
        /// 自分中心X移動後の絶対位置を取得
        /// </summary>
        /// <param name="t">自分のターン</param>
        /// <param name="x">絶対X位置</param>
        /// <param name="moval">自分中心X移動量(+ 右側へ/- 左側へ)</param>
        /// <returns>移動後X絶対位置</returns>
        public static int mvX(Pturn t, int x, int moval) {
            if (t == Pturn.Sente) {
                return x - moval;
            } else {
                return x + moval;
            }
        }

        /// <summary>
        /// 自分中心Y移動後の絶対位置を取得
        /// </summary>
        /// <param name="t">自分のターン</param>
        /// <param name="y">絶対Y位置</param>
        /// <param name="moval">自分中心Y移動量(+ 前側へ/- 後側へ)</param>
        /// <returns>移動後Y絶対位置</returns>
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

        /// <summary>
        /// 相手のターンを取得
        /// </summary>
        /// <param name="turn">自分のターン</param>
        /// <returns>相手のターン</returns>
        public static int aturn(int turn) {
            return 1 - turn;
        }

        /// <summary>
        /// 相手のターンを取得
        /// </summary>
        /// <param name="turn">自分のターン</param>
        /// <returns>相手のターン</returns>
        public static Pturn aturn(Pturn turn) {
            return 1 - turn;
        }

    }
}
