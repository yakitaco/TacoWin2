using System;
using System.Collections.Generic;
using System.Text;

namespace TacoWin2 {

    //1局面での将棋盤情報
    public unsafe struct tw2ban {
        public fixed byte onBoard[81]; // 盤上情報(X*9+Y)
        // 000XYYYY [X]0:先手/1:後手 [Y]enum ktype

        public fixed byte putPieceNum[2]; // 置き駒数情報
        // 0 : 先手 / 1:後手

        public fixed byte putPiece[40]; // 置き駒情報
        // [0-19]先手 [20-39]後手
        // enum ktype

        public fixed byte captPiece[14]; // 持ち駒情報
        // [0-6]先手 [7-13]後手


        // ox : 0-8 移動元筋 / 9 駒打ち(先手) / 10 駒打ち(後手)
        // oy : (ox<9の場合) 0-8 移動元段 / (ox=9の場合) 打駒種(enum ktype)
        // nx : 0-8 移動先筋
        // nx : 0-8 移動先段
        // turn : 先手 / 後手 (駒打ち時のみ使用)
        // nari : 成り(false 不成 true 成)
        // chk : 移動整合性チェック(false チェック無 true チェック有)
        // 戻り値 0 OK / -1 NG(chk=true時のみ)
        public int moveKoma(int ox, int oy, int nx, int ny, int turn , bool nari, bool chk) {
            // 駒打ち
            if (ox > 8) {
                // 合法手チェック
                if (chk) if ((onBoard[ox * 9 + oy] == 0)) return -1;
                captPiece[oy + turn * 7]--;

                



            // 駒移動
            } else {
                // 合法手チェック
                if (chk) if ((captPiece[oy + turn * 7] < 1) || (onBoard[nx * 9 + ny] > 0)) return -1;


            }


            return 0;
        }

        // 指定位置から指定位置への駒移動可能チェック
        // 戻り値 0:OK / 1:NG
        public int checkMoveable(ktype type, int ox, int oy, int nx, int ny) {
            switch (type) {
                case ktype.Fuhyou:
                    if ((ox == nx) && (oy == ny + 1)) return 0;

                    break;

                case ktype.Kyousha:
                    if ((ox == nx) && (oy == ny + 1)) return 0;

                    break;

                case ktype.Keima:

                    break;

                case ktype.Ginsyou:

                    break;

                case ktype.Hisya:

                    break;

                case ktype.Kakugyou:

                    break;

                case ktype.Kinsyou:

                    break;

                case ktype.Ousyou:

                    break;

                case ktype.Tokin:

                    break;

                case ktype.Narikyou:

                    break;

                case ktype.Narikei:

                    break;

                case ktype.Narigin:

                    break;

                case ktype.Ryuuou:

                    break;

                case ktype.Ryuuma:

                    break;

                default:
                    break;
            }

            return 0;
        }


    }


}
