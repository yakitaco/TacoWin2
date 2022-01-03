using System.Collections.Generic;
using System.IO;
using TacoWin2_BanInfo;
using TacoWin2_sfenIO;

namespace TacoWin2_Mkjs {
    class tw2mkjs_csaIO {


        public static string[] loadDir(string dirPath) {
            return Directory.GetFiles(dirPath, "*.csa");
        }

        // ret 0 先手勝ち 1:後手勝ち 2:その他(千日手)
        public static int loadFile(string filePath, int num, out List<string>[] outStr) {
            outStr = new List<string>[2];
            outStr[0] = new List<string>();
            outStr[1] = new List<string>();

            int ret = 2;
            ban ban;
            ban.startpos();
            Pturn turn = Pturn.Sente;
            int count = 0;

            foreach (string line in System.IO.File.ReadLines(@filePath)) {
                if ((line.Length == 1) && (line[0] == '-')) return 3; //後手開始(駒落ち)はスキップ

                if (((line[0] == '+') || (line[0] == '-')) && (line.Length > 4)) {

                    if (line[0] == '+') {
                        turn = Pturn.Sente;
                    } else {
                        turn = Pturn.Gote;
                    }

                    string oki = "";
                    string mochi = "";
                    sfenIO.ban2sfen(ref ban, ref oki, ref mochi);

                    bool nari = false;

                    var ox = int.Parse(line[1].ToString());
                    var oy = int.Parse(line[2].ToString());
                    var nx = int.Parse(line[3].ToString());
                    var ny = int.Parse(line[4].ToString());

                    var a = csa2ktype(line.Substring(5, 2));


                    //前と駒タイプが異なる->駒が成った
                    if ((ox > 0) && (oy > 0) && (ban.getOnBoardKtype(ox - 1, oy - 1) != a)) nari = true;

                    outStr[0].Add(oki + " " + mochi);
                    if (nari == true) {
                        outStr[1].Add(line[0].ToString() + ox + "" + int2Dafb(oy - 1) + "" + nx + "" + int2Dafb(ny - 1) + "+");
                    } else if (ox == 0) {
                        outStr[1].Add(line[0].ToString() + csa2usit(line.Substring(5, 2)) + "*" + nx + "" + int2Dafb(ny - 1));
                    } else {
                        outStr[1].Add(line[0].ToString() + ox + "" + int2Dafb(oy - 1) + "" + nx + "" + int2Dafb(ny - 1));
                    }


                    if (ox == 0) {
                        ban.moveKoma(9, csa2num(line.Substring(5, 2)), nx - 1, ny - 1, turn, nari, false, false);
                    } else {
                        ban.moveKoma(ox - 1, oy - 1, nx - 1, ny - 1, turn, nari, false, false);
                    }

                    if ((num > 0) && (count > num)) break;
                } else if ((line.Length == 6) && (line.Substring(0, 6) == "%TORYO")) {
                    ret = (int)turn;
                    break;
                }

            }

            return ret;
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

        public static ktype csa2ktype(string str) {
            ktype ret = ktype.None;

            switch (str) {
                case "FU":    // 歩兵
                    ret = ktype.Fuhyou;
                    break;
                case "KY":    // 香車
                    ret = ktype.Kyousha;
                    break;
                case "KE":    // 桂馬
                    ret = ktype.Keima;
                    break;
                case "GI":    // 銀将
                    ret = ktype.Ginsyou;
                    break;
                case "HI":    // 飛車
                    ret = ktype.Hisya;
                    break;
                case "KA":    // 角行
                    ret = ktype.Kakugyou;
                    break;
                case "KI":    // 金将
                    ret = ktype.Kinsyou;
                    break;
                case "OU":    // 王将
                    ret = ktype.Ousyou;
                    break;
                case "TO":    // と金
                    ret = ktype.Tokin;
                    break;
                case "NY":    // 成香
                    ret = ktype.Narikyou;
                    break;
                case "NK":    // 成桂
                    ret = ktype.Narikei;
                    break;
                case "NG":    // 成銀
                    ret = ktype.Narigin;
                    break;
                case "RY":    // 竜王
                    ret = ktype.Ryuuou;
                    break;
                case "UM":    // 竜馬
                    ret = ktype.Ryuuma;
                    break;
                default:
                    ret = ktype.None;
                    break;
            }

            return ret;
        }

        public static string csa2usit(string str) {
            string ret = "";

            switch (str) {
                case "FU":    // 歩兵
                    ret = "P";
                    break;
                case "KY":    // 香車
                    ret = "L";
                    break;
                case "KE":    // 桂馬
                    ret = "N";
                    break;
                case "GI":    // 銀将
                    ret = "S";
                    break;
                case "HI":    // 飛車
                    ret = "R";
                    break;
                case "KA":    // 角行
                    ret = "B";
                    break;
                case "KI":    // 金将
                    ret = "G";
                    break;
                case "OU":    // 王将
                    ret = "K";
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        public static int csa2num(string str) {
            int ret;

            switch (str) {
                case "FU":    // 歩兵
                    ret = 1;
                    break;
                case "KY":    // 香車
                    ret = 2;
                    break;
                case "KE":    // 桂馬
                    ret = 3;
                    break;
                case "GI":    // 銀将
                    ret = 4;
                    break;
                case "HI":    // 飛車
                    ret = 5;
                    break;
                case "KA":    // 角行
                    ret = 6;
                    break;
                case "KI":    // 金将
                    ret = 7;
                    break;
                case "OU":    // 王将
                    ret = 8;
                    break;
                case "TO":    // と金
                    ret = 9;
                    break;
                case "NY":    // 成香
                    ret = 10;
                    break;
                case "NK":    // 成桂
                    ret = 11;
                    break;
                case "NG":    // 成銀
                    ret = 12;
                    break;
                case "RY":    // 竜王
                    ret = 13;
                    break;
                case "UM":    // 竜馬
                    ret = 14;
                    break;
                default:
                    ret = -1;
                    break;
            }

            return ret;
        }

    }
}
