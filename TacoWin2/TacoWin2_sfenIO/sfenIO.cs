using TacoWin2_BanInfo;

namespace TacoWin2_sfenIO {
    public unsafe class sfenIO {

        public static void sfen2ban(ref ban ban, string oki, string mochi) {

            ban.putOusyou[0] = 0xFF;
            ban.putOusyou[1] = 0xFF;
            for (int i = 0; i < 4; i++) {
                ban.putHisya[i] = 0xFF;
                ban.putKakugyou[i] = 0xFF;
            }
            for (int i = 0; i < 8; i++) {
                ban.putKinsyou[i] = 0xFF;
                ban.putKyousha[i] = 0xFF;
                ban.putKeima[i] = 0xFF;
                ban.putGinsyou[i] = 0xFF;
            }
            for (int i = 0; i < 18; i++) {
                ban.putFuhyou[i] = 9;
            }
            for (int i = 0; i < 60; i++) {
                ban.putNarigoma[i] = 0xFF;
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
                        ban.putKoma(suzi, i, p, k);
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
                    ban.captPiece[(int)p * 7 + (int)k - 1] += (byte)num;
                    for (int i = 0; i < num; i++) {
                        ban.hash ^= tw2bi_hash.mochiSeed[(int)p * 7 + (int)k - 1, i];
                    }
                    num = 0;
                }

            }


        }

        public static void ban2sfen(ref ban ban, ref string oki, ref string mochi) {
            // 場面の設定
            int j = 0;
            int empty = 0;
            for (int i = 0; i < 9; i++) {
                int suzi = 9 - 1;
                while (suzi > -1) {
                    // 数字(空白の数)
                    if (ban.onBoard[suzi * 9 + i] == 0) {
                        empty++;
                    } else {
                        // 駒配置
                        if (empty > 0) {
                            oki += empty;
                            empty = 0;
                        }
                        oki += fromKoma(ban.getOnBoardKtype(suzi * 9 + i), ban.getOnBoardPturn(suzi * 9 + i));
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
                if (ban.captPiece[(int)i * 7 + (int)ktype.Hisya - 1] > 0) {
                    if (ban.captPiece[(int)i * 7 + (int)ktype.Hisya - 1] > 1) {
                        mochi += ban.captPiece[(int)i * 7 + (int)ktype.Hisya - 1];
                    }
                    mochi += fromKoma(ktype.Hisya, (Pturn)i);
                }

                if (ban.captPiece[(int)i * 7 + (int)ktype.Kakugyou - 1] > 0) {
                    if (ban.captPiece[(int)i * 7 + (int)ktype.Kakugyou - 1] > 1) {
                        mochi += ban.captPiece[(int)i * 7 + (int)ktype.Kakugyou - 1];
                    }
                    mochi += fromKoma(ktype.Kakugyou, (Pturn)i);
                }

                if (ban.captPiece[(int)i * 7 + (int)ktype.Kinsyou - 1] > 0) {
                    if (ban.captPiece[(int)i * 7 + (int)ktype.Kinsyou - 1] > 1) {
                        mochi += ban.captPiece[(int)i * 7 + (int)ktype.Kinsyou - 1];
                    }
                    mochi += fromKoma(ktype.Kinsyou, (Pturn)i);
                }

                if (ban.captPiece[(int)i * 7 + (int)ktype.Ginsyou - 1] > 0) {
                    if (ban.captPiece[(int)i * 7 + (int)ktype.Ginsyou - 1] > 1) {
                        mochi += ban.captPiece[(int)i * 7 + (int)ktype.Ginsyou - 1];
                    }
                    mochi += fromKoma(ktype.Ginsyou, (Pturn)i);
                }

                if (ban.captPiece[(int)i * 7 + (int)ktype.Keima - 1] > 0) {
                    if (ban.captPiece[(int)i * 7 + (int)ktype.Keima - 1] > 1) {
                        mochi += ban.captPiece[(int)i * 7 + (int)ktype.Keima - 1];
                    }
                    mochi += fromKoma(ktype.Keima, (Pturn)i);
                }

                if (ban.captPiece[(int)i * 7 + (int)ktype.Kyousha - 1] > 0) {
                    if (ban.captPiece[(int)i * 7 + (int)ktype.Kyousha - 1] > 1) {
                        mochi += ban.captPiece[(int)i * 7 + (int)ktype.Kyousha - 1];
                    }
                    mochi += fromKoma(ktype.Kyousha, (Pturn)i);
                }

                if (ban.captPiece[(int)i * 7 + (int)ktype.Fuhyou - 1] > 0) {
                    if (ban.captPiece[(int)i * 7 + (int)ktype.Fuhyou - 1] > 1) {
                        mochi += ban.captPiece[(int)i * 7 + (int)ktype.Fuhyou - 1];
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
