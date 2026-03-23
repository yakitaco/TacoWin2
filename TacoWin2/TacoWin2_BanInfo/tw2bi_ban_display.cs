using System;

namespace TacoWin2_BanInfo
{
    /// <summary>
    /// ban構造体の表示・デバッグ用拡張メソッド群
    /// </summary>
    public static class tw2bi_ban_display
    {

        /// <summary>
        /// ban情報のデバッグ表示
        /// </summary>
        public static unsafe string debugShow(this ref ban b)
        {
            string str = "";
            for (int i = 0; i < 137; i++)
            {
                str += b.data[i].ToString("X8") + " ";
                if ((i & 15) == 15) str += Environment.NewLine;
            }
            str += Environment.NewLine;
            str += "[HASH]" + b.hash.ToString("X16");
            return str;
        }

        /// <summary>
        /// ban情報表示
        /// </summary>
        public static unsafe string banShow(this ref ban b)
        {
            string str = "";
            for (int i = 0; i < 81; i++)
            {
                if (b.getOnBoardKtype(((8 - i % 9) << 4) + (i / 9)) > ktype.None)
                {
                    // 先手
                    if (b.getOnBoardPturn(((8 - i % 9) << 4) + (i / 9)) == Pturn.Sente)
                    {
                        switch (b.getOnBoardKtype(((8 - i % 9) << 4) + (i / 9)))
                        {
                            case ktype.Fuhyou: str += "▲歩|"; break;
                            case ktype.Kyousha: str += "▲香|"; break;
                            case ktype.Keima: str += "▲桂|"; break;
                            case ktype.Ginsyou: str += "▲銀|"; break;
                            case ktype.Hisya: str += "▲飛|"; break;
                            case ktype.Kakugyou: str += "▲角|"; break;
                            case ktype.Kinsyou: str += "▲金|"; break;
                            case ktype.Ousyou: str += "▲王|"; break;
                            case ktype.Tokin: str += "▲と|"; break;
                            case ktype.Narikyou: str += "▲杏|"; break;
                            case ktype.Narikei: str += "▲圭|"; break;
                            case ktype.Narigin: str += "▲全|"; break;
                            case ktype.Ryuuou: str += "▲竜|"; break;
                            case ktype.Ryuuma: str += "▲馬|"; break;
                            default: str += "▲??|"; break;
                        }
                    } else if (b.getOnBoardPturn(((8 - i % 9) << 4) + (i / 9)) == Pturn.Gote)
                    {
                        switch (b.getOnBoardKtype(((8 - i % 9) << 4) + (i / 9)))
                        {
                            case ktype.Fuhyou: str += "▽歩|"; break;
                            case ktype.Kyousha: str += "▽香|"; break;
                            case ktype.Keima: str += "▽桂|"; break;
                            case ktype.Ginsyou: str += "▽銀|"; break;
                            case ktype.Hisya: str += "▽飛|"; break;
                            case ktype.Kakugyou: str += "▽角|"; break;
                            case ktype.Kinsyou: str += "▽金|"; break;
                            case ktype.Ousyou: str += "▽王|"; break;
                            case ktype.Tokin: str += "▽と|"; break;
                            case ktype.Narikyou: str += "▽杏|"; break;
                            case ktype.Narikei: str += "▽圭|"; break;
                            case ktype.Narigin: str += "▽全|"; break;
                            case ktype.Ryuuou: str += "▽竜|"; break;
                            case ktype.Ryuuma: str += "▽馬|"; break;
                            default: str += "▽??|"; break;
                        }
                    } else
                    {
                        switch (b.getOnBoardKtype(((8 - i % 9) << 4) + (i / 9)))
                        {
                            case ktype.Fuhyou: str += "??歩|"; break;
                            case ktype.Kyousha: str += "??香|"; break;
                            case ktype.Keima: str += "??桂|"; break;
                            case ktype.Ginsyou: str += "??銀|"; break;
                            case ktype.Hisya: str += "??飛|"; break;
                            case ktype.Kakugyou: str += "??角|"; break;
                            case ktype.Kinsyou: str += "??金|"; break;
                            case ktype.Ousyou: str += "??王|"; break;
                            case ktype.Tokin: str += "??と|"; break;
                            case ktype.Narikyou: str += "??杏|"; break;
                            case ktype.Narikei: str += "??圭|"; break;
                            case ktype.Narigin: str += "??全|"; break;
                            case ktype.Ryuuou: str += "??竜|"; break;
                            case ktype.Ryuuma: str += "??馬|"; break;
                            default: str += "????|"; break;
                        }
                    }
                } else
                {
                    str += " ___|";
                }

                if ((i + 1) % 9 == 0)
                {
                    str += "    ";
                    // 移動可能リスト
                    for (int j = 0; j < 9; j++)
                    {
                        str += (b.data[((8 - j % 9) << 4) + (i / 9)] >> (8 + ((int)Pturn.Sente << 2)) & 0x0F) + "," + (b.data[((8 - j % 9) << 4) + (i / 9)] >> (8 + ((int)Pturn.Gote << 2)) & 0x0F) + "|";
                    }
                    // 改行
                    str += Environment.NewLine;
                }
            }

            // 持ち駒情報 (定数は ban.XXX としてアクセス)
            str += "先手持駒/歩:" + b.data[ban.hand + (int)ktype.Fuhyou] + "/香:" + b.data[ban.hand + (int)ktype.Kyousha] + "/桂:" + b.data[ban.hand + (int)ktype.Keima] + "/銀:" + b.data[ban.hand + (int)ktype.Ginsyou] + "/飛:" + b.data[ban.hand + (int)ktype.Hisya] + "/角:" + b.data[ban.hand + (int)ktype.Kakugyou] + "/金:" + b.data[ban.hand + (int)ktype.Kinsyou] + Environment.NewLine;
            str += "後手持駒/歩:" + b.data[ban.GoOffset + ban.hand + (int)ktype.Fuhyou] + "/香:" + b.data[ban.GoOffset + ban.hand + (int)ktype.Kyousha] + "/桂:" + b.data[ban.GoOffset + ban.hand + (int)ktype.Keima] + "/銀:" + b.data[ban.GoOffset + ban.hand + (int)ktype.Ginsyou] + "/飛:" + b.data[ban.GoOffset + ban.hand + (int)ktype.Hisya] + "/角:" + b.data[ban.GoOffset + ban.hand + (int)ktype.Kakugyou] + "/金:" + b.data[ban.GoOffset + ban.hand + (int)ktype.Kinsyou] + Environment.NewLine;

            str += Environment.NewLine;
            return str;
        }
    }
}