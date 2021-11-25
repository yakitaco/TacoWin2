using System;
using System.Collections.Generic;
using System.Text;

namespace TacoWin2 {
    class tw2usiIO {
        // USIプロトコルのインタフェース

        //USI座標→内部座標変換
        public static int usi2pos(string usi, out int ox, out int oy, out int nx, out int ny, out bool nari) {

            //駒打ち
            if (usi.Substring(1, 1) == "*") {
                ox = 9;
                oy = (int)kafb2ktype(usi.Substring(0, 1));
                nx = Convert.ToInt32(usi.Substring(2, 1)) - 1;
                ny = dafb2int(usi.Substring(3, 1));
                nari = false;

            //駒移動
            } else {
                ox = Convert.ToInt32(usi.Substring(0, 1)) - 1;
                oy = dafb2int(usi.Substring(1, 1));
                nx = Convert.ToInt32(usi.Substring(2, 1)) - 1;
                ny = dafb2int(usi.Substring(3, 1));
                if ((usi.Length == 5) && (usi.Substring(4, 1) == "+"))  //駒成り
                {
                    nari = true;
                } else {
                    nari = false;
                }
            }
            return 0;
        }

        //内部座標→USI座標変換
        public static string pos2usi(int ox, int oy, int nx, int ny, bool nari) {
            string usiStr = "";
            if (ox == 9) {
                usiStr = ktype2Kafb((ktype)oy) + "*" + (nx + 1).ToString() + int2Dafb(ny);
            } else {
                if (nari == true) {
                    usiStr = (ox + 1).ToString() + int2Dafb(oy) + (nx + 1).ToString() + int2Dafb(ny) + "+"; //成有り
                } else {
                    usiStr = (ox + 1).ToString() + int2Dafb(oy) + (nx + 1).ToString() + int2Dafb(ny);
                }
            }
            return usiStr;
        }

        //[ktype->USI]駒打ち用
        public static string ktype2Kafb(ktype type) {
            string usiStr = "";
            switch (type) {
                case ktype.Fuhyou:
                    usiStr = "P";
                    break;
                case ktype.Kyousha:
                    usiStr = "L";
                    break;
                case ktype.Keima:
                    usiStr = "N";
                    break;
                case ktype.Ginsyou:
                    usiStr = "S";
                    break;
                case ktype.Hisya:
                    usiStr = "R";
                    break;
                case ktype.Kakugyou:
                    usiStr = "B";
                    break;
                case ktype.Kinsyou:
                    usiStr = "G";
                    break;
                case ktype.Ousyou:  //ありえないが
                    usiStr = "K";
                    break;
                default:
                    break;
            }

            return usiStr;
        }

        //駒打ち用
        public static ktype kafb2ktype(string usiStr) {
            ktype type = ktype.None;
            switch (usiStr) {
                case "P":
                    type = ktype.Fuhyou;
                    break;
                case "L":
                    type = ktype.Kyousha;
                    break;
                case "N":
                    type = ktype.Keima;
                    break;
                case "S":
                    type = ktype.Ginsyou;
                    break;
                case "R":
                    type = ktype.Hisya;
                    break;
                case "B":
                    type = ktype.Kakugyou;
                    break;
                case "G":
                    type = ktype.Kinsyou;
                    break;
                case "K":  //ありえないが
                    type = ktype.Ousyou;
                    break;
                default:
                    break;
            }

            return type;
        }

        public static string int2Dafb(int val) {
            string usiStr = "";
            switch (val) {
                case 0:
                    usiStr = "a";
                    break;
                case 1:
                    usiStr = "b";
                    break;
                case 2:
                    usiStr = "c";
                    break;
                case 3:
                    usiStr = "d";
                    break;
                case 4:
                    usiStr = "e";
                    break;
                case 5:
                    usiStr = "f";
                    break;
                case 6:
                    usiStr = "g";
                    break;
                case 7:
                    usiStr = "h";
                    break;
                case 8:
                    usiStr = "i";
                    break;
                default:
                    break;
            }

            return usiStr;
        }

        public static int dafb2int(string str) {
            int val = 0; ;
            switch (str) {
                case "a":
                    val = 0;
                    break;
                case "b":
                    val = 1;
                    break;
                case "c":
                    val = 2;
                    break;
                case "d":
                    val = 3;
                    break;
                case "e":
                    val = 4;
                    break;
                case "f":
                    val = 5;
                    break;
                case "g":
                    val = 6;
                    break;
                case "h":
                    val = 7;
                    break;
                case "i":
                    val = 8;
                    break;
                default:
                    break;
            }

            return val;
        }

    }
}
