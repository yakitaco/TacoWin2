using System;
using System.Collections.Generic;
using System.Text;

namespace TacoWin2 {
    class tw2usiIO {
        // USIプロトコルのインタフェース



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
                case 1:
                    usiStr = "a";
                    break;
                case 2:
                    usiStr = "b";
                    break;
                case 3:
                    usiStr = "c";
                    break;
                case 4:
                    usiStr = "d";
                    break;
                case 5:
                    usiStr = "e";
                    break;
                case 6:
                    usiStr = "f";
                    break;
                case 7:
                    usiStr = "g";
                    break;
                case 8:
                    usiStr = "h";
                    break;
                case 9:
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
