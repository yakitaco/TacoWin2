using System;

namespace TacoWin2 {

    //1局面での将棋盤情報
    public unsafe struct tw2ban {
        public fixed ushort onBoard[81]; // 盤上情報(X+Y*9)
        // WWWWWWWW000XYYYY [X]0:先手/1:後手 [Y]enum ktype [W]置き駒情報

        public fixed byte putPieceNum[2]; // 置き駒数情報(盤上情報の番号を入れる)
        // 0 : 先手 / 1:後手

        public fixed byte putPiece[80]; // 置き駒情報(盤上情報の番号を格納)
        // [0-39]先手 [40-79]後手
        // enum ktype

        public fixed byte captPiece[14]; // 持ち駒情報
        // [0-6]先手 [7-13]後手

        public fixed byte fuPos[18]; // 歩情報(0-9) 9:無し
        // [0-8]先手 / [9-17]後手

        public fixed byte moveable[162]; //駒の移動可能リスト

        // 初期盤情報
        public void startpos() {

            for (int i = 0; i < 40; i++) {
                putPieceNum[i] = 0xFF;
                putPieceNum[i + 40] = 0xFF;
            }

            //王の配置
            addKoma(4, 8, Pturn.Sente, ktype.Ousyou);
            addKoma(4, 0, Pturn.Gote, ktype.Ousyou);

            //金の配置
            addKoma(3, 8, Pturn.Sente, ktype.Kinsyou);
            addKoma(5, 8, Pturn.Sente, ktype.Kinsyou);
            addKoma(3, 0, Pturn.Gote, ktype.Kinsyou);
            addKoma(5, 0, Pturn.Gote, ktype.Kinsyou);

            //銀の配置
            addKoma(2, 8, Pturn.Sente, ktype.Ginsyou);
            addKoma(6, 8, Pturn.Sente, ktype.Ginsyou);
            addKoma(2, 0, Pturn.Gote, ktype.Ginsyou);
            addKoma(6, 0, Pturn.Gote, ktype.Ginsyou);

            //桂の配置
            addKoma(1, 8, Pturn.Sente, ktype.Keima);
            addKoma(7, 8, Pturn.Sente, ktype.Keima);
            addKoma(1, 0, Pturn.Gote, ktype.Keima);
            addKoma(7, 0, Pturn.Gote, ktype.Keima);

            //香の配置
            addKoma(0, 8, Pturn.Sente, ktype.Kyousha);
            addKoma(8, 8, Pturn.Sente, ktype.Kyousha);
            addKoma(0, 0, Pturn.Gote, ktype.Kyousha);
            addKoma(8, 0, Pturn.Gote, ktype.Kyousha);

            //角の配置
            addKoma(7, 7, Pturn.Sente, ktype.Kakugyou);
            addKoma(1, 1, Pturn.Gote, ktype.Kakugyou);

            //飛の配置
            addKoma(1, 7, Pturn.Sente, ktype.Hisya);
            addKoma(7, 1, Pturn.Gote, ktype.Hisya);

            //歩の配置
            for (int i = 0; i < 9; i++) {
                addKoma(i, 6, Pturn.Sente, ktype.Fuhyou);
                addKoma(i, 2, Pturn.Gote, ktype.Fuhyou);
            }

            // 移動リスト新規作成
            renewMoveable();

        }

        //移動可能リスト生成(先手・後手の駒が移動可能場所を加算する)
        public void renewMoveable() {
            //指せる手を全てリスト追加
            tw2ban t = this;
            int nx = 0, ny = 0;
            byte[] _moveable = new byte[162];


            for (int i = 0; i < 81; i++) {
                if (onBoard[i] > 0) {
                    ForEachKoma(i % 9, i / 9, getOnBoardPturn(i % 9, i / 9), (int _ox, int _oy, int _nx, int _ny, Pturn _turn, bool _nari) => {
                        nx = _nx;
                        ny = _ny;
                        Console.Write("LIST1({0},{1},{2},{3},{4})\n", _turn, _ox + 1, _oy + 1, _nx + 1, _ny + 1);
                        _moveable[(int)_turn * 81 + nx + ny * 9]++;
                    });
                    Console.Write("LIST2({0},{1},{2},{3},{4})\n", getOnBoardPturn(i % 9, i / 9), i % 9 + 1, i / 9 + 1, nx + 1, ny + 1);
                    //moveable[(int)getOnBoardPturn(i % 9, i / 9) * 81 + nx + ny * 9]++;
                }
            }

            string str = "";
            foreach (var ppp in _moveable) {
                str += ppp + " ";
            }
            Console.Write("LIST2({0})\n", str);

            fixed (byte* p = moveable, pp = _moveable) {
                Buffer.MemoryCopy(pp, p, 162, 162);
            }
        }

        void addKoma(int x, int y, Pturn turn, ktype type) {
            onBoard[x + y * 9] = setOnBordDatat(turn, putPieceNum[(int)turn], type);
            Console.WriteLine(onBoard[x + y * 9] + ":" + (int)turn + ":" + (int)putPieceNum[(int)turn] + ":" + (int)type);
            putPiece[40 * (int)turn + putPieceNum[(int)turn]] = (byte)(x + y * 9);
            putPieceNum[(int)turn]++;
            if (type == ktype.Fuhyou) fuPos[(int)turn * 9 + x] = (byte)y;

        }

        ushort setOnBordDatat(Pturn trn, int putPieceNum, ktype type) {
            return (ushort)((putPieceNum << 8) + ((int)trn << 4) + (int)type);
        }

        // ox : 0-8 移動元筋 / 9 駒打ち(先手) / 10 駒打ち(後手)
        // oy : (ox<9の場合) 0-8 移動元段 / (ox=9の場合) 打駒種(enum ktype)
        // nx : 0-8 移動先筋
        // nx : 0-8 移動先段
        // turn : 先手 / 後手 (駒打ち時のみ使用)
        // nari : 成り(false 不成 true 成)
        // chk : 移動整合性チェック(false チェック無 true チェック有)
        // 戻り値 0 OK / -1 NG(chk=true時のみ)
        public int moveKoma(int ox, int oy, int nx, int ny, Pturn turn, bool nari, bool chk) {

            Span<int> renewMoveableList = stackalloc int[40];
            int renewMoveableListNum = 0;

            // 駒打ち
            if (ox > 8) {
                // 合法手チェック
                if (chk) if ((onBoard[nx + ny * 9] == 0)) return -1;
                captPiece[oy - 1 + (int)turn * 7]--;

                // 更新
                //onBoard[nx + ny * 9] = (byte)(((int)turn << 4) + oy);

                for (int i = 0; i < 40; i++) {
                    if (putPiece[(int)turn * 40 + i] == 0xFF) {
                        putPiece[(int)turn * 40 + i] = (byte)(nx + ny * 9);
                        onBoard[nx + ny * 9] = setOnBordDatat(turn, putPieceNum[(int)turn], (ktype)oy);
                        break;
                    }
                }

                putPieceNum[(int)turn]++;

                // 歩情報更新
                if ((ktype)oy == ktype.Fuhyou) {
                    fuPos[(int)turn * 9 + nx] = (byte)ny;
                }

                renewMoveableList[renewMoveableListNum++] = ox + oy * 9;
                renewMoveableLink(ox, oy, ref renewMoveableListNum, ref renewMoveableList);

                // 駒移動
            } else {
                // 合法手チェック
                if (chk) if ((captPiece[oy + (int)turn * 7] < 1) || (checkMoveable(getOnBoardKtype(ox, oy), getOnBoardPturn(ox, oy), ox, oy, nx, ny) < 0)) return -1;

                // 移動先に既にある
                if (onBoard[nx + ny * 9] > 0) {
                    // 味方駒は取れない
                    if (chk) if (getOnBoardPturn(ox, oy) == getOnBoardPturn(nx, ny)) return -1;

                    // 歩情報更新
                    if (getOnBoardKtype(nx, ny) == ktype.Fuhyou) {
                        fuPos[pturn.aturn((int)turn) * 9 + nx] = 9;
                    }

                    putPiece[pturn.aturn((int)turn) * 40 + getOnBoardPutPiece(nx, ny)] = 0xFF;
                    putPieceNum[pturn.aturn((int)turn)]--;

                    // 追加
                    captPiece[(int)kNoNari(getOnBoardKtype(nx, ny)) - 1 + (int)getOnBoardPturn(ox, oy) * 7]++;

                }

                // 成れる位置ではない
                if (chk) if ((pturn.psX(getOnBoardPturn(ox, oy), nx) > 5) && (nari == true)) return -1;

                ushort mk = onBoard[ox + oy * 9];

                if (nari) {
                    // 歩情報更新
                    if (getOnBoardKtype(ox, oy) == ktype.Fuhyou) {
                        fuPos[(int)turn * 9 + nx] = 9;
                    }
                    mk = (ushort)(((mk >> 4) << 4) + (ushort)kDoNari(getOnBoardKtype(ox, oy)));
                }

                putPiece[(int)turn * 40 + getOnBoardPutPiece(ox, oy)] = (byte)(nx + ny * 9);

                // 更新
                onBoard[nx + ny * 9] = mk;
                onBoard[ox + oy * 9] = 0;

            }

            return 0;
        }

        // 盤上情報上の駒情報を取得
        public ktype getOnBoardKtype(int x, int y) {
            return (ktype)(onBoard[x + y * 9] & 0x0F);
        }

        // 盤上情報上の駒所有者を取得
        public Pturn getOnBoardPturn(int x, int y) {
            return (Pturn)((onBoard[x + y * 9] & 0xF0) >> 4);
        }

        // 盤上情報上の置き駒情報を取得
        public int getOnBoardPutPiece(int x, int y) {
            return ((onBoard[x + y * 9] & 0xFF00) >> 8);
        }

        // 指定位置から指定位置への駒移動可能チェック
        // 戻り値 0:OK / 1:NG
        public int checkMoveable(ktype type, Pturn trn, int ox, int oy, int nx, int ny) {
            switch (type) {
                case ktype.Fuhyou:
                    if ((ox == nx) && (oy == ny + pturn.mvY(trn, ny, 1))) return 0;

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
        public int pickMoveable(ktype type, Pturn trn, int ox, int oy, int nx, int ny) {
            switch (type) {
                case ktype.Fuhyou:
                    if ((ox == nx) && (oy == ny + pturn.mvY(trn, ny, 1))) return 0;

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

        void renewMoveableLink(int x, int y, ref int num ,ref Span<int> list) {

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
        public void ForEachAll(Pturn turn, Action<int, int, int, int, Pturn, bool> action) {
            // 駒移動
            for (int i = 0, j = 0; j < putPieceNum[(int)turn]; i++) {
                //putPiece[(int)turn * 20 + i].x;
                if (putPiece[(int)turn * 40 + i] != 0xFF) {
                    ForEachKoma(putPiece[(int)turn * 40 + i] % 9, putPiece[(int)turn * 40 + i] / 9, turn, action);
                    j++;
                }
            }

            // 駒打ち
            for (int i = 0; i < 7; i++) {
                if (captPiece[(int)turn * 7 + i] > 0) {
                    ForEachKoma(9, i + 1, turn, action);
                }
            }

            //if (turn == pturn.Sente) action(0, 0, 0, 0, 0, true, true);

        }

        // 指定駒の移動位置を返す
        public void ForEachKoma(int ox, int oy, Pturn turn, Action<int, int, int, int, Pturn, bool> action) {
            // 駒打ち
            if (ox == 9) {
                for (int i = 0; i < 9; i++) {

                    // 二歩は打てない
                    if ((oy == (int)ktype.Fuhyou) && (fuPos[(int)turn * 9 + i] < 9)) {
                        continue;

                    }
                    for (int j = 0; j < 9; j++) {
                        // 1段目には打てない
                        if (((oy == (int)ktype.Fuhyou) || (oy == (int)ktype.Kyousha)) && (pturn.psX(turn, j) > 7)) {
                            continue;
                            // 2段目には打てない
                        } else if ((oy == (int)ktype.Keima) && (pturn.psX(turn, j) > 6)) {
                            continue;
                        }
                        // 駒があると打てない
                        if (onBoard[i + j * 9] > 0) {
                            continue;
                        }

                        action(ox, oy, i, j, turn, false);
                    }
                }

                // 移動
            } else {

                switch (getOnBoardKtype(ox, oy)) {
                    case ktype.Fuhyou:
                        //action(ox, oy, ox, ptuen.mvY(getOnBoardPturn(ox, oy), oy, 1), turn, false);
                        ForEachKomaContMove(ox, oy, 0, 1, turn, action);
                        break;

                    case ktype.Kyousha:
                        for (int i = 1; ForEachKomaContMove(ox, oy, 0, i, turn, action) < 1; i++) ;
                        break;

                    case ktype.Keima:
                        ForEachKomaContMove(ox, oy, 1, 2, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 2, turn, action);
                        break;

                    case ktype.Ginsyou:
                        ForEachKomaContMove(ox, oy, 1, 1, turn, action);
                        ForEachKomaContMove(ox, oy, 0, 1, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 1, turn, action);
                        ForEachKomaContMove(ox, oy, 1, -1, turn, action);
                        ForEachKomaContMove(ox, oy, -1, -1, turn, action);
                        break;

                    case ktype.Hisya:
                        for (int i = 1; ForEachKomaContMove(ox, oy, 0, i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, 0, -i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, i, 0, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, -i, 0, turn, action) < 1; i++) ;
                        break;

                    case ktype.Kakugyou:
                        for (int i = 1; ForEachKomaContMove(ox, oy, i, i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, i, -i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, -i, i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, -i, -i, turn, action) < 1; i++) ;
                        break;

                    case ktype.Kinsyou:
                    case ktype.Tokin:
                    case ktype.Narikyou:
                    case ktype.Narikei:
                    case ktype.Narigin:
                        ForEachKomaContMove(ox, oy, 1, 1, turn, action);
                        ForEachKomaContMove(ox, oy, 0, 1, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 1, turn, action);
                        ForEachKomaContMove(ox, oy, 1, 0, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 0, turn, action);
                        ForEachKomaContMove(ox, oy, 0, -1, turn, action);
                        break;

                    case ktype.Ousyou:
                        ForEachKomaContMove(ox, oy, 1, 1, turn, action);
                        ForEachKomaContMove(ox, oy, 0, 1, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 1, turn, action);
                        ForEachKomaContMove(ox, oy, 1, 0, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 0, turn, action);
                        ForEachKomaContMove(ox, oy, 1, -1, turn, action);
                        ForEachKomaContMove(ox, oy, 0, -1, turn, action);
                        ForEachKomaContMove(ox, oy, -1, -1, turn, action);
                        break;

                    case ktype.Ryuuou:
                        ForEachKomaContMove(ox, oy, 1, 1, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 1, turn, action);
                        ForEachKomaContMove(ox, oy, 1, -1, turn, action);
                        ForEachKomaContMove(ox, oy, -1, -1, turn, action);
                        for (int i = 1; ForEachKomaContMove(ox, oy, 0, i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, 0, -i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, i, 0, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, -i, 0, turn, action) < 1; i++) ;
                        break;

                    case ktype.Ryuuma:
                        ForEachKomaContMove(ox, oy, 0, 1, turn, action);
                        ForEachKomaContMove(ox, oy, 1, 0, turn, action);
                        ForEachKomaContMove(ox, oy, -1, 0, turn, action);
                        ForEachKomaContMove(ox, oy, 0, -1, turn, action);
                        for (int i = 1; ForEachKomaContMove(ox, oy, i, i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, i, -i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, -i, i, turn, action) < 1; i++) ;
                        for (int i = 1; ForEachKomaContMove(ox, oy, -i, -i, turn, action) < 1; i++) ;
                        break;

                    default:
                        break;
                }
            }
        }

        //指定移動先(mx,my)
        //移動できる 0 / 移動できる(敵駒取り) 1 / 移動できない(味方駒) 2 / 移動できない 3(範囲外)
        public int ForEachKomaContMove(int ox, int oy, int mx, int my, Pturn turn, Action<int, int, int, int, Pturn, bool> action) {
            int nx = pturn.mvX(turn, ox, mx);
            int ny = pturn.mvY(turn, oy, my);
            if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 3;
            if (onBoard[nx + ny * 9] > 0) {
                if (getOnBoardPturn(nx, ny) != turn) {
                    if ((pturn.psY(turn, ny) > 5) && ((int)getOnBoardKtype(ox, oy) < 7)) {
                        if ((getOnBoardKtype(ox, oy) == ktype.Ginsyou) || ((getOnBoardKtype(ox, oy) == ktype.Kyousha) && (pturn.psY(turn, ny) < 8)) || ((getOnBoardKtype(ox, oy) == ktype.Kyousha) && (pturn.psY(turn, ny) < 7))) {
                            action(ox, oy, nx, ny, turn, false);
                        }
                        action(ox, oy, nx, ny, turn, true);
                    } else {
                        action(ox, oy, nx, ny, turn, false);
                    }

                    return 1; // 敵の駒(取れる)
                } else {
                    return 2; // 味方の駒
                }

            }
            if ((pturn.psY(turn, ny) > 5) && ((int)getOnBoardKtype(ox, oy) < 7)) {
                if ((getOnBoardKtype(ox, oy) == ktype.Ginsyou) || ((getOnBoardKtype(ox, oy) == ktype.Kyousha) && (pturn.psY(turn, ny) < 8)) || ((getOnBoardKtype(ox, oy) == ktype.Kyousha) && (pturn.psY(turn, ny) < 7))) {
                    action(ox, oy, nx, ny, turn, false);
                }
                action(ox, oy, nx, ny, turn, true);
            } else {
                action(ox, oy, nx, ny, turn, false);
            }
            return 0; // 駒がない
        }

        // ban情報のデバッグ表示
        public string debugShow() {
            string str = "";
            for (int i = 0; i < 81; i++) {
                if (onBoard[i / 9 * 9 + 8 - (i % 9)] != 0) {
                    // 先手
                    if (getOnBoardPturn(8 - (i % 9), i / 9) == Pturn.Sente) {
                        switch (getOnBoardKtype(8 - (i % 9), i / 9)) {
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
                        switch (getOnBoardKtype(8 - (i % 9), i / 9)) {
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

                } else {
                    str += "__|";
                }
                if ((i + 1) % 9 == 0) {
                    str += "    ";
                    // 移動可能リスト
                    for (int j = 8; j >= 0; j--) {
                        str += moveable[j + i - 8] + "" + moveable[81 + j + i - 8] + "|";

                    }

                    // 改行
                    str += Environment.NewLine;

                }
            }

            // 持ち駒情報
            str += "FU:" + captPiece[0] + "/KY:" + captPiece[1] + "/KE:" + captPiece[2] + "/GI:" + captPiece[3] + "/HI:" + captPiece[4] + "/KA:" + captPiece[5] + "/KI:" + captPiece[6] + "\n";
            str += "FU:" + captPiece[7] + "/KY:" + captPiece[8] + "/KE:" + captPiece[9] + "/GI:" + captPiece[10] + "/HI:" + captPiece[11] + "/KA:" + captPiece[12] + "/KI:" + captPiece[13] + "\n";

            for (int i = 0; i < 9; i++) {
                str += fuPos[i] + " ";

            }
            str += "\n";
            for (int i = 0; i < 9; i++) {
                str += fuPos[9 + i] + " ";

            }
            str += "\n";
            return str;
        }

    }

}
