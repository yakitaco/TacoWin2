using System;
using TacoWin2_BanInfo;

namespace TacoWin2 {
    // 動的評価値計算
    class tw2acval {
        public static int mvGet(ref ban ban, ktype type, byte oPos, byte nPos, Pturn turn) {
            unsafe {
                int val = 0;

                byte aOuPos = (byte)(ban.data[((int)pturn.aturn(turn) << 6) + ban.setOu] & 0xFF);
                byte sOuPos = (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF);

                switch (type) {
                    case ktype.Fuhyou:
                        /* 飛車先を突くほうが評価値が高い */

                        /* 相手の角桂をつくほうが評価値が高い */
                        break;
                    case ktype.Kyousha:
                        break;
                    case ktype.Keima:
                        break;
                    case ktype.Ginsyou:
                        val = (Math.Abs(pturn.dx(turn, aOuPos, nPos)) + Math.Abs(pturn.dy(turn, aOuPos, nPos)) - Math.Abs(pturn.dx(turn, aOuPos, oPos)) - Math.Abs(pturn.dy(turn, aOuPos, oPos)));
                        break;
                    case ktype.Hisya:
                        val = (Math.Abs(pturn.dx(turn, aOuPos, nPos)) + Math.Abs(pturn.dy(turn, aOuPos, nPos)) - Math.Abs(pturn.dx(turn, aOuPos, oPos)) - Math.Abs(pturn.dy(turn, aOuPos, oPos)));
                        break;
                    case ktype.Kakugyou:
                        val = (Math.Abs(pturn.dx(turn, aOuPos, nPos)) + Math.Abs(pturn.dy(turn, aOuPos, nPos)) - Math.Abs(pturn.dx(turn, aOuPos, oPos)) - Math.Abs(pturn.dy(turn, aOuPos, oPos)));
                        break;
                    case ktype.Kinsyou:
                        val = (Math.Abs(pturn.dx(turn, sOuPos, nPos)) + Math.Abs(pturn.dy(turn, sOuPos, nPos)) - Math.Abs(pturn.dx(turn, sOuPos, oPos)) - Math.Abs(pturn.dy(turn, sOuPos, oPos)));
                        break;
                    case ktype.Ousyou:
                        break;
                    case ktype.Tokin:
                    case ktype.Narikyou:
                    case ktype.Narikei:
                    case ktype.Narigin:
                    case ktype.Ryuuou:
                    case ktype.Ryuuma:
                        /* 相手の玉に近いほうが評価値が高い */
                        val = (Math.Abs(pturn.dx(turn, aOuPos, nPos)) + Math.Abs(pturn.dy(turn, aOuPos, nPos)) - Math.Abs(pturn.dx(turn, aOuPos, oPos)) - Math.Abs(pturn.dy(turn, aOuPos, oPos))) << 3;
                        break;
                    default:
                        /* 何もしない */

                        break;
                }

                return val;
            }
        }

        public static int ptGet(ref ban ban, ktype type, byte nPos, Pturn turn) {
            unsafe {
                int val = 0;

                byte aOuPos = (byte)(ban.data[((int)pturn.aturn(turn) << 6) + ban.setOu] & 0xFF);
                byte sOuPos = (byte)(ban.data[((int)turn << 6) + ban.setOu] & 0xFF);

                switch (type) {
                    case ktype.Fuhyou:
                        /* 飛車先を突くほうが評価値が高い */

                        /* 相手の角桂をつくほうが評価値が高い */
                        break;
                    case ktype.Kyousha:
                        break;
                    case ktype.Keima:
                        break;
                    case ktype.Ginsyou:
                        val = (Math.Abs(pturn.dx(turn, aOuPos, nPos)) + Math.Abs(pturn.dy(turn, aOuPos, nPos))) << 1;
                        break;
                    case ktype.Hisya:
                        val = (Math.Abs(pturn.dx(turn, aOuPos, nPos)) + Math.Abs(pturn.dy(turn, aOuPos, nPos))) << 2;
                        break;
                    case ktype.Kakugyou:
                        val = (Math.Abs(pturn.dx(turn, aOuPos, nPos)) + Math.Abs(pturn.dy(turn, aOuPos, nPos))) << 2;
                        break;
                    case ktype.Kinsyou:
                        val = (Math.Abs(pturn.dx(turn, sOuPos, nPos)) + Math.Abs(pturn.dy(turn, sOuPos, nPos))) << 1;
                        break;
                    case ktype.Ousyou:
                        break;
                    case ktype.Tokin:
                    case ktype.Narikyou:
                    case ktype.Narikei:
                    case ktype.Narigin:
                    case ktype.Ryuuou:
                    case ktype.Ryuuma:
                        /* 相手の玉に近いほうが評価値が高い */
                        val = (Math.Abs(pturn.dx(turn, aOuPos, nPos)) + Math.Abs(pturn.dy(turn, aOuPos, nPos))) << 3;
                        break;
                    default:
                        /* 何もしない */

                        break;
                }

                return val;
            }
        }

    }
}
