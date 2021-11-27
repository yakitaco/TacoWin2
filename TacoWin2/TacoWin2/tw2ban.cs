using System;

namespace TacoWin2 {

    //1局面での将棋盤情報
    public unsafe struct tw2ban {
        public fixed ushort onBoard[81]; // 盤上情報(X*9+Y)
        // WWWWWWWW000XYYYY [X]0:先手/1:後手 [Y]enum ktype [W]置き駒情報

        public fixed byte putPieceNum[2]; // 置き駒数情報(盤上情報の番号を入れる)
        // 0 : 先手 / 1:後手

        public fixed byte putPiece[80]; // 置き駒情報
        // [0-39]先手 [40-79]後手
        // enum ktype

        public fixed byte captPiece[14]; // 持ち駒情報
        // [0-6]先手 [7-13]後手

        public fixed byte fuPos[18]; // 歩情報(0-9) 9:無し
        // [0-8]先手 / [9-17]後手

        // 初期盤情報
        public void startpos() {

            //王の配置
            addKoma(4, 8, pturn.Sente, ktype.Ousyou);
            addKoma(4, 0, pturn.Gote, ktype.Ousyou);

            //金の配置
            addKoma(3, 8, pturn.Sente, ktype.Kinsyou);
            addKoma(5, 8, pturn.Sente, ktype.Kinsyou);
            addKoma(3, 0, pturn.Gote, ktype.Kinsyou);
            addKoma(5, 0, pturn.Gote, ktype.Kinsyou);

            //銀の配置
            addKoma(2, 8, pturn.Sente, ktype.Ginsyou);
            addKoma(6, 8, pturn.Sente, ktype.Ginsyou);
            addKoma(2, 0, pturn.Gote, ktype.Ginsyou);
            addKoma(6, 0, pturn.Gote, ktype.Ginsyou);

            //桂の配置
            addKoma(1, 8, pturn.Sente, ktype.Keima);
            addKoma(7, 8, pturn.Sente, ktype.Keima);
            addKoma(1, 0, pturn.Gote, ktype.Keima);
            addKoma(7, 0, pturn.Gote, ktype.Keima);

            //香の配置
            addKoma(0, 8, pturn.Sente, ktype.Kyousha);
            addKoma(8, 8, pturn.Sente, ktype.Kyousha);
            addKoma(0, 0, pturn.Gote, ktype.Kyousha);
            addKoma(8, 0, pturn.Gote, ktype.Kyousha);

            //角の配置
            addKoma(7, 7, pturn.Sente, ktype.Kakugyou);
            addKoma(1, 1, pturn.Gote, ktype.Kakugyou);

            //飛の配置
            addKoma(1, 7, pturn.Sente, ktype.Hisya);
            addKoma(7, 1, pturn.Gote, ktype.Hisya);

            //歩の配置
            for (int i = 0; i < 9; i++) {
                addKoma(i, 6, pturn.Sente, ktype.Fuhyou);
                addKoma(i, 2, pturn.Gote, ktype.Fuhyou);
            }

        }

        void addKoma(int x, int y, pturn turn, ktype type) {
            onBoard[x * 9 + y] = setOnBordDatat(turn, putPieceNum[(int)turn], type);
            Console.WriteLine(onBoard[x * 9 + y] +":" + (int)turn + ":" + (int)putPieceNum[(int)turn] + ":" + (int)type);
            putPiece[putPieceNum[(int)turn]] = (byte)(x * 9 + y);
            putPieceNum[(int)turn]++;
        }

        ushort setOnBordDatat(pturn trn, int putPieceNum, ktype type) {
            return (ushort)(putPieceNum << 8 + (int)trn << 4 + (int)type);
        }

        // ox : 0-8 移動元筋 / 9 駒打ち(先手) / 10 駒打ち(後手)
        // oy : (ox<9の場合) 0-8 移動元段 / (ox=9の場合) 打駒種(enum ktype)
        // nx : 0-8 移動先筋
        // nx : 0-8 移動先段
        // turn : 先手 / 後手 (駒打ち時のみ使用)
        // nari : 成り(false 不成 true 成)
        // chk : 移動整合性チェック(false チェック無 true チェック有)
        // 戻り値 0 OK / -1 NG(chk=true時のみ)
        public int moveKoma(int ox, int oy, int nx, int ny, int turn, bool nari, bool chk) {
            // 駒打ち
            if (ox > 8) {
                // 合法手チェック
                if (chk) if ((onBoard[nx * 9 + ny] == 0)) return -1;
                captPiece[oy + turn * 7]--;

                // 更新
                onBoard[nx * 9 + ny] = (byte)((turn << 4) + oy);

                // 歩情報更新
                if ((ktype)oy == ktype.Fuhyou) {
                    fuPos[turn * 9 + nx] = (byte)ny;
                }

                // 駒移動
            } else {
                // 合法手チェック
                if (chk) if ((captPiece[oy + turn * 7] < 1) || (checkMoveable(getOnBoardKtype(ox, oy), getOnBoardPturn(ox, oy), ox, oy, nx, ny) < 0)) return -1;

                // 移動先に既にある
                if (onBoard[nx * 9 + ny] > 0) {
                    // 味方駒は取れない
                    if (chk) if (getOnBoardPturn(ox, oy) == getOnBoardPturn(nx, ny)) return -1;

                    // 歩情報更新
                    if (getOnBoardKtype(ox, oy) == ktype.Fuhyou) {
                        fuPos[ptuen.aturn(turn) * 9 + nx] = 9;
                    }

                    // 追加
                    captPiece[oy + (int)getOnBoardPturn(ox, oy) * 7]++;

                    // 成れる位置ではない
                    if (chk) if ((ptuen.psX(getOnBoardPturn(ox, oy), nx) > 3) && (nari == true)) return -1;

                    if (nari) {
                        // 歩情報更新
                        if (getOnBoardKtype(ox, oy) == ktype.Fuhyou) {
                            fuPos[turn * 9 + nx] = 9;
                        }
                        kDoNari(getOnBoardKtype(ox, oy));
                    }

                    // 更新
                    onBoard[nx * 9 + ny] = onBoard[ox * 9 + oy];
                    onBoard[ox * 9 + oy] = 0;



                }

            }

            return 0;
        }

        // 盤上情報上の駒情報を取得
        public ktype getOnBoardKtype(int x, int y) {
            return (ktype)(onBoard[x * 9 + y] & 0x0F);
        }

        // 盤上情報上の駒所有者を取得
        public pturn getOnBoardPturn(int x, int y) {
            return (pturn)(onBoard[x * 9 + y] & 0xF0 >> 4);
        }

        // 指定位置から指定位置への駒移動可能チェック
        // 戻り値 0:OK / 1:NG
        public int checkMoveable(ktype type, pturn trn, int ox, int oy, int nx, int ny) {
            switch (type) {
                case ktype.Fuhyou:
                    if ((ox == nx) && (oy == ny + ptuen.mvY(trn, ny, 1))) return 0;

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

        // 指定位置から指定位置への駒移動可能チェック
        // 戻り値 0:OK / 1:NG
        public int pickMoveable(ktype type, pturn trn, int ox, int oy, int nx, int ny) {
            switch (type) {
                case ktype.Fuhyou:
                    if ((ox == nx) && (oy == ny + ptuen.mvY(trn, ny, 1))) return 0;

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

        // 処理軽減のためチェック省略
        public ktype kDoNari(ktype t) {
            //if (( t > 0 )&&( t < ktype.Kinsyou)) {
            return t + 8;
            //} else {
            //    return t;
            //}
        }

        public ktype kNoNari(ktype t) {
            if (t > ktype.Ousyou) {
                return t - 8;
            } else {
                return t;
            }
        }

        // 全駒の移動可能位置を返す
        public void ForEachAll(pturn turn, Action<int, int, int, int, pturn, bool> action) {
            // 駒移動
            for (int i = 0; i < putPieceNum[(int)turn]; i++) {
                //putPiece[(int)turn * 20 + i].x;
            }

            // 駒打ち
            for (int i = 0; i < 7; i++) {
                if (captPiece[(int)turn * 7 + i] > 0) {
                    ForEachKoma(9, i, turn, action);
                }
            }

            //if (turn == pturn.Sente) action(0, 0, 0, 0, 0, true, true);

        }

        // 指定駒の移動位置を返す
        public void ForEachKoma(int ox, int oy, pturn turn, Action<int, int, int, int, pturn, bool> action) {
            // 駒打ち
            if (ox == 9) {
                for (int i = 0; i < 9; i++) {
                    for (int j = 0; j < 9; j++) {
                        action(ox, oy, i, j, turn, false);
                    }
                }

                // 移動
            } else {

                switch (getOnBoardKtype(ox, oy)) {
                    case ktype.Fuhyou:
                        //action(ox, oy, ox, ptuen.mvY(getOnBoardPturn(ox, oy), oy, 1), turn, false);
                        ForEachKomaContMove(ox, oy, 0, -1, turn, action);
                        break;

                    case ktype.Kyousha:
                        for (int i = 1; ForEachKomaContMove(ox, oy, 0, i, turn, action) < 2; i++) ;

                        break;

                    case ktype.Keima:
                        ForEachKomaContMove(ox, oy, 1, 2, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 2, turn, action);
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
            }
        }

        //指定移動先(mx,my)
        //移動できる 0 / 移動できる(敵駒取り) 1 / 移動できない(味方駒) 2 / 移動できない 3(範囲外)
        public int ForEachKomaContMove(int ox, int oy, int mx, int my, pturn turn, Action<int, int, int, int, pturn, bool> action) {
            int nx = ptuen.mvX(turn, ox, mx);
            int ny = ptuen.mvY(turn, oy, my);
            if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 3;
            if (onBoard[nx * 9 + ny] > 0) {
                if (getOnBoardPturn(nx, ny) != turn) {
                    action(ox, oy, nx, ny, turn, false);
                    return 1; // 敵の駒(取れる)
                } else {
                    return 2; // 味方の駒
                }

            }
            action(ox, oy, nx, ny, turn, false);
            return 0; // 駒がない
        }

        // ban情報のデバッグ表示
        public string debugShow() {
            string str = "";
            for (int i = 0; i < 81; i++) {
                if (onBoard[i] != 0) {
                    str += "<" + onBoard[i] + ">";
                    // 先手
                    if (getOnBoardPturn(i / 9, i % 9) == pturn.Sente) {
                        switch (getOnBoardKtype(i / 9, i % 9)) {
                            case ktype.Fuhyou:
                                str += "P_|";
                                break;
                            case ktype.Kyousha:
                                str += "L_|";
                                break;
                            case ktype.Keima:
                                str += "N_|";
                                break;
                            case ktype.Ginsyou:
                                str += "S_|";
                                break;
                            case ktype.Hisya:
                                str += "R_|";
                                break;
                            case ktype.Kakugyou:
                                str += "B_|";
                                break;
                            case ktype.Kinsyou:
                                str += "G_|";
                                break;
                            case ktype.Ousyou:
                                str += "K_|";
                                break;
                            case ktype.Tokin:
                                str += "+P|";
                                break;
                            case ktype.Narikyou:
                                str += "+L|";
                                break;
                            case ktype.Narikei:
                                str += "+N|";
                                break;
                            case ktype.Narigin:
                                str += "+G|";
                                break;
                            case ktype.Ryuuou:
                                str += "+R|";
                                break;
                            case ktype.Ryuuma:
                                str += "+B|";
                                break;
                            default:
                                str += "!_|";
                                break;
                        }
                    } else {
                        switch (getOnBoardKtype(i / 9, i % 9)) {
                            case ktype.Fuhyou:
                                str += "p_|";
                                break;
                            case ktype.Kyousha:
                                str += "l_|";
                                break;
                            case ktype.Keima:
                                str += "n_|";
                                break;
                            case ktype.Ginsyou:
                                str += "s_|";
                                break;
                            case ktype.Hisya:
                                str += "r_|";
                                break;
                            case ktype.Kakugyou:
                                str += "b_|";
                                break;
                            case ktype.Kinsyou:
                                str += "g_|";
                                break;
                            case ktype.Ousyou:
                                str += "k_|";
                                break;
                            case ktype.Tokin:
                                str += "+p|";
                                break;
                            case ktype.Narikyou:
                                str += "+l|";
                                break;
                            case ktype.Narikei:
                                str += "+n|";
                                break;
                            case ktype.Narigin:
                                str += "+g|";
                                break;
                            case ktype.Ryuuou:
                                str += "+r|";
                                break;
                            case ktype.Ryuuma:
                                str += "+b|";
                                break;
                            default:
                                str += "!_|";
                                break;
                        }
                    }
                    getOnBoardKtype(i / 9, i % 9);

                } else {
                    str += "__|";
                }
                if ((i+1)%9==0) str += "\n";
            }


            return str;
        }

    }

}
