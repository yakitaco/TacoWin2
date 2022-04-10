using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        [Obsolete("このメソッドの使用は非推奨です")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [Obsolete("このメソッドの使用は非推奨です")]
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
        /// <returns>自分中心X相対位置(+ 自分が相手より右側/- 自分が相手より左側)</returns>
        [Obsolete("このメソッドの使用は非推奨です")]
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
        [Obsolete("このメソッドの使用は非推奨です")]
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
        [Obsolete("このメソッドの使用は非推奨です")]
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
        [Obsolete("このメソッドの使用は非推奨です")]
        public static int mvY(Pturn t, int y, int moval) {
            if (t == Pturn.Sente) {
                return y - moval;
            } else {
                return y + moval;
            }
        }

        // 前後の移動
        [Obsolete("このメソッドの使用は非推奨です")]
        public static (int, int) mvXY(Pturn t, int x, int y, int movalx, int movaly) {
            return (mvX(t, x, movalx), mvY(t, y, movaly));
        }

        // 前後の移動(XY統合)[pos=x*9+y/move=movalx*9+movaly]
        [Obsolete("このメソッドの使用は非推奨です")]
        public static int mvXY(Pturn t, int pos, int move) {
            if (t == Pturn.Sente) {
                return pos - move;
            } else {
                return pos + move;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int dx(Pturn turn, byte sPos, byte dPos) {
            if (turn == Pturn.Sente) {
                return (dPos >> 4) - (sPos >> 4);
            } else {
                return (sPos >> 4) - (dPos >> 4);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int dy(Pturn turn, byte sPos, byte dPos) {
            if (turn == Pturn.Sente) {
                return (dPos & 0xF) - (sPos & 0xF);
            } else {
                return (sPos & 0xF) - (dPos & 0xF);
            }
        }

        /// <summary>
        /// 駒の移動絶対位置(手番,現在の絶対位置,自分中心移動相対)
        /// </summary>
        /// <param name="turn"></param>
        /// <param name="pos"></param>
        /// <param name="move"></param>
        /// <returns>移動先の絶対位置</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte mv(Pturn turn, byte pos, int move) {
            if (turn == Pturn.Sente) {
                return (byte)(pos - move);
            } else {
                return (byte)(pos + move);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ps(Pturn turn, byte pos) {
            if (turn == Pturn.Sente) {
                return (byte)(0x88 - pos);
            } else {
                return (byte)(pos);
            }
        }

        /// <summary>
        /// 相手のターンを取得
        /// </summary>
        /// <param name="turn">自分のターン</param>
        /// <returns>相手のターン</returns>
       [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int aturn(int turn) {
            return 1 - turn;
        }

        /// <summary>
        /// 相手のターンを取得
        /// </summary>
        /// <param name="turn">自分のターン</param>
        /// <returns>相手のターン</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pturn aturn(Pturn turn) {
            return 1 - turn;
        }

    }
}
