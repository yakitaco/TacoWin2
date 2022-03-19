using TacoWin2_BanInfo;

namespace TacoWin2_sfenIO {
    public unsafe class sfenIO {

        public static void sfen2ban(ref ban ban, string oki, string mochi) {

            ban.data[ban.setOu] = System.UInt32.MaxValue;
            ban.data[ban.GoOffset + ban.setOu] = System.UInt32.MaxValue;
            ban.data[ban.setKin] = System.UInt32.MaxValue;
            ban.data[ban.GoOffset + ban.setKin] = System.UInt32.MaxValue;
            ban.data[ban.setGin] = System.UInt32.MaxValue;
            ban.data[ban.GoOffset + ban.setGin] = System.UInt32.MaxValue;
            ban.data[ban.setKei] = System.UInt32.MaxValue;
            ban.data[ban.GoOffset + ban.setKei] = System.UInt32.MaxValue;
            ban.data[ban.setKyo] = System.UInt32.MaxValue;
            ban.data[ban.GoOffset + ban.setKyo] = System.UInt32.MaxValue;
            ban.data[ban.setHi] = System.UInt32.MaxValue;
            ban.data[ban.GoOffset + ban.setHi] = System.UInt32.MaxValue;
            ban.data[ban.setKa] = System.UInt32.MaxValue;
            ban.data[ban.GoOffset + ban.setKa] = System.UInt32.MaxValue;

            for (int i = 0; i < 3; i++) {
                ban.data[ban.setFu + i] = System.UInt32.MaxValue;
                ban.data[ban.GoOffset + ban.setFu + i] = System.UInt32.MaxValue;
            }

            for (int i = 0; i < 7; i++) {
                ban.data[ban.setNa + i] = System.UInt32.MaxValue;
                ban.data[ban.GoOffset + ban.setNa + i] = System.UInt32.MaxValue;
            }

            // 場面の設定
            int j = 0;
            for (int i = 0; i < 9; i++) {
                int suzi = 9 - 1;
                while (suzi > -1) {
                    // 数字(空白の数)
                    if (int.TryParse(oki.Substring(j, 1), out var n)) {
                        suzi -= n;
                        j++;
                    } else {
                        // 駒配置
                        (ktype k, Pturn p) = toKoma(oki, ref j);
                        ban.putKoma((suzi<< 4) + i, p, 0, k);
                        ban.hash ^= tw2bi_hash.okiSeed[(int)p * 14 + (int)k - 1, suzi * 9 + i];
                        suzi--;
                    }
                }
                j++;
            }

            // 持ち駒の設定
            j = 0;
            int num = 0;
            while ((j < mochi.Length) && (mochi[j] != '-')) {
                // 数字(駒の数) ★2桁もアリ
                if (int.TryParse(mochi.Substring(j, 1), out var tmp)) {
                    j++;
                    num = num * 10 + tmp;
                } else {
                    if (num == 0) num = 1;
                    // 持ち駒追加(複数駒ありを考慮)
                    (ktype k, Pturn p) = toKoma(mochi, ref j);
                    ban.data[((int)p << 6) + ban.hand + (int)k] += (byte)num;

                    for (int i = 0; i < num; i++) {
                        ban.hash ^= tw2bi_hash.mochiSeed[(int)p * 7 + (int)k - 1, i];
                    }
                    num = 0;
                }

            }


        }

        public static void ban2sfen(ref ban ban, ref string oki, ref string mochi) {
            // 場面の設定
            int empty = 0;
            for (int i = 0; i < 9; i++) {
                int suzi = 9 - 1;
                while (suzi > -1) {
                    // 数字(空白の数)
                    if (ban.getOnBoardKtype((suzi << 4) + i) == ktype.None) {
                        empty++;
                    } else {
                        // 駒配置
                        if (empty > 0) {
                            oki += empty;
                            empty = 0;
                        }
                        oki += fromKoma(ban.getOnBoardKtype((suzi << 4) + i), ban.getOnBoardPturn((suzi << 4) + i));
                    }
                    suzi--;
                }
                if (empty > 0) {
                    oki += empty;
                    empty = 0;
                }
                if (i < 8) oki += "/";
            }

            // 持ち駒の設定(先手→後手、飛車→角→金→銀→桂→香→歩の順)
            for (int i = 0; i < 2; i++) {
                if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Hisya] > 0) {
                    if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Hisya] > 1) {
                        mochi += ban.data[((int)i << 6) + ban.hand + (int)ktype.Hisya];
                    }
                    mochi += fromKoma(ktype.Hisya, (Pturn)i);
                }

                if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Kakugyou] > 0) {
                    if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Kakugyou] > 1) {
                        mochi += ban.data[((int)i << 6) + ban.hand + (int)ktype.Kakugyou];
                    }
                    mochi += fromKoma(ktype.Kakugyou, (Pturn)i);
                }

                if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Kinsyou] > 0) {
                    if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Kinsyou] > 1) {
                        mochi += ban.data[((int)i << 6) + ban.hand + (int)ktype.Kinsyou];
                    }
                    mochi += fromKoma(ktype.Kinsyou, (Pturn)i);
                }

                if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Ginsyou] > 0) {
                    if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Ginsyou] > 1) {
                        mochi += ban.data[((int)i << 6) + ban.hand + (int)ktype.Ginsyou];
                    }
                    mochi += fromKoma(ktype.Ginsyou, (Pturn)i);
                }

                if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Keima] > 0) {
                    if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Keima] > 1) {
                        mochi += ban.data[((int)i << 6) + ban.hand + (int)ktype.Keima];
                    }
                    mochi += fromKoma(ktype.Keima, (Pturn)i);
                }

                if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Kyousha] > 0) {
                    if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Kyousha] > 1) {
                        mochi += ban.data[((int)i << 6) + ban.hand + (int)ktype.Kyousha];
                    }
                    mochi += fromKoma(ktype.Kyousha, (Pturn)i);
                }

                if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Fuhyou] > 0) {
                    if (ban.data[((int)i << 6) + ban.hand + (int)ktype.Fuhyou] > 1) {
                        mochi += ban.data[((int)i << 6) + ban.hand + (int)ktype.Fuhyou];
                    }
                    mochi += fromKoma(ktype.Fuhyou, (Pturn)i);
                }
            }

            if (mochi.Length < 1) mochi = "-";

        }

        public static (ktype, Pturn) toKoma(string str, ref int cnt) {
            bool nari = false;
            ktype k = ktype.None;
            Pturn p = Pturn.Sente;

            // 成り
            if (str[cnt] == '+') {
                cnt++;
                nari = true;
            }
            // 駒
            switch (str[cnt]) {
                case 'P':
                    k = ktype.Fuhyou;
                    p = Pturn.Sente;
                    break;
                case 'L':
                    k = ktype.Kyousha;
                    p = Pturn.Sente;
                    break;
                case 'N':
                    k = ktype.Keima;
                    p = Pturn.Sente;
                    break;
                case 'S':
                    k = ktype.Ginsyou;
                    p = Pturn.Sente;
                    break;
                case 'R':
                    k = ktype.Hisya;
                    p = Pturn.Sente;
                    break;
                case 'B':
                    k = ktype.Kakugyou;
                    p = Pturn.Sente;
                    break;
                case 'G':
                    k = ktype.Kinsyou;
                    p = Pturn.Sente;
                    break;
                case 'K':
                    k = ktype.Ousyou;
                    p = Pturn.Sente;
                    break;
                case 'p':
                    k = ktype.Fuhyou;
                    p = Pturn.Gote;
                    break;
                case 'l':
                    k = ktype.Kyousha;
                    p = Pturn.Gote;
                    break;
                case 'n':
                    k = ktype.Keima;
                    p = Pturn.Gote;
                    break;
                case 's':
                    k = ktype.Ginsyou;
                    p = Pturn.Gote;
                    break;
                case 'r':
                    k = ktype.Hisya;
                    p = Pturn.Gote;
                    break;
                case 'b':
                    k = ktype.Kakugyou;
                    p = Pturn.Gote;
                    break;
                case 'g':
                    k = ktype.Kinsyou;
                    p = Pturn.Gote;
                    break;
                case 'k':
                    k = ktype.Ousyou;
                    p = Pturn.Gote;
                    break;
                default:
                    break;
            }
            cnt++;
            if (nari == true) k = ban.kDoNari(k);

            return (k, p);
        }

        public static string fromKoma(ktype k, Pturn p) {
            string str = "";

            // 駒
            if (p == Pturn.Sente) {
                switch (k) {
                    case ktype.Fuhyou:
                        str = "P";
                        break;
                    case ktype.Kyousha:
                        str = "L";
                        break;
                    case ktype.Keima:
                        str = "N";
                        break;
                    case ktype.Ginsyou:
                        str = "S";
                        break;
                    case ktype.Hisya:
                        str = "R";
                        break;
                    case ktype.Kakugyou:
                        str = "B";
                        break;
                    case ktype.Kinsyou:
                        str = "G";
                        break;
                    case ktype.Ousyou:
                        str = "K";
                        break;
                    case ktype.Tokin:
                        str = "+P";
                        break;
                    case ktype.Narikyou:
                        str = "+L";
                        break;
                    case ktype.Narikei:
                        str = "+N";
                        break;
                    case ktype.Narigin:
                        str = "+S";
                        break;
                    case ktype.Ryuuou:
                        str = "+R";
                        break;
                    case ktype.Ryuuma:
                        str = "+B";
                        break;
                    default:
                        break;
                }


            } else {
                switch (k) {
                    case ktype.Fuhyou:
                        str = "p";
                        break;
                    case ktype.Kyousha:
                        str = "l";
                        break;
                    case ktype.Keima:
                        str = "n";
                        break;
                    case ktype.Ginsyou:
                        str = "s";
                        break;
                    case ktype.Hisya:
                        str = "r";
                        break;
                    case ktype.Kakugyou:
                        str = "b";
                        break;
                    case ktype.Kinsyou:
                        str = "g";
                        break;
                    case ktype.Ousyou:
                        str = "k";
                        break;
                    case ktype.Tokin:
                        str = "+p";
                        break;
                    case ktype.Narikyou:
                        str = "+l";
                        break;
                    case ktype.Narikei:
                        str = "+n";
                        break;
                    case ktype.Narigin:
                        str = "+s";
                        break;
                    case ktype.Ryuuou:
                        str = "+r";
                        break;
                    case ktype.Ryuuma:
                        str = "+b";
                        break;
                    default:
                        break;
                }
            }

            return str;
        }
    }
}
