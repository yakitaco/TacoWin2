using TacoWin2_BanInfo;

namespace TacoWin2_sfenIO {
    public class sfenIO {

        public static ban sfen2ban(string sfen) {
            ban retBan = new ban();

            // 場面の設定
            int j = 0;
            for (int i = 0; i < 81; i++) {
                int suzi = TEIGI.SIZE_SUZI - 1;
                while (suzi > -1) {
                    // 数字(空白の数)
                    if (int.TryParse(sfen.Substring(j, 1), out var n)) {
                        suzi -= n;
                        j++;
                    } else {
                        // 駒配置
                        toKoma(retBan, sfen, ref j, suzi, i);
                        suzi--;
                    }
                }
                j++;
            }

            // 持ち駒

            return retBan;
        }

        public static void toKoma(ban ban, string str, ref int cnt, int suzi, int dan) {
            bool nari = false;
            ktype k;

            // 成り
            if (str[cnt] == '+') {
                cnt++;
                nari = true;
            }
            // 駒
            switch (str[cnt]) {
                case 'P':
                    k = ktype.Fuhyou;
                    teban = TEIGI.TEBAN_SENTE;
                    break;
                case 'L':
                    k = ktype.Kyousha;
                    teban = TEIGI.TEBAN_SENTE;
                    break;
                case 'N':
                    k = ktype.Keima;
                    teban = TEIGI.TEBAN_SENTE;
                    break;
                case 'S':
                    k = ktype.Ginsyou;
                    teban = TEIGI.TEBAN_SENTE;
                    break;
                case 'R':
                    k = ktype.Hisya;
                    teban = TEIGI.TEBAN_SENTE;
                    break;
                case 'B':
                    k = ktype.Kakugyou;
                    teban = TEIGI.TEBAN_SENTE;
                    break;
                case 'G':
                    k = ktype.Kinsyou;
                    teban = TEIGI.TEBAN_SENTE;
                    break;
                case 'K':
                    k = ktype.Ousyou;
                    teban = TEIGI.TEBAN_SENTE;
                    break;
                case 'p':
                    k = ktype.Fuhyou;
                    teban = TEIGI.TEBAN_GOTE;
                    break;
                case 'l':
                    k = ktype.Kyousha;
                    teban = TEIGI.TEBAN_GOTE;
                    break;
                case 'n':
                    k = ktype.Keima;
                    teban = TEIGI.TEBAN_GOTE;
                    break;
                case 's':
                    k = ktype.Ginsyou;
                    teban = TEIGI.TEBAN_GOTE;
                    break;
                case 'r':
                    k = ktype.Hisya;
                    teban = TEIGI.TEBAN_GOTE;
                    break;
                case 'b':
                    k = ktype.Kakugyou;
                    teban = TEIGI.TEBAN_GOTE;
                    break;
                case 'g':
                    k = ktype.Kinsyou;
                    teban = TEIGI.TEBAN_GOTE;
                    break;
                case 'k':
                    k = ktype.Ousyou;
                    teban = TEIGI.TEBAN_GOTE;
                    break;
                default:
                    break;
            }
            cnt++;

            ban.putKoma(0,0, Pturn.Gote, ktype.Fuhyou);
        }
    }
}
