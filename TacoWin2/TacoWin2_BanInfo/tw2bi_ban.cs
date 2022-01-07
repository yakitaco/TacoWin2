﻿using System;

namespace TacoWin2_BanInfo {
    public unsafe struct ban {
        public fixed ushort onBoard[81]; // 盤上情報(X*9+Y)
        // WWWWWWWW000XYYYY [X]0:先手/1:後手 [Y]enum ktype [W]置き駒情報

        //public fixed byte putPieceNum[2]; // 置き駒数情報(盤上情報の番号を入れる)
        // 0 : 先手 / 1:後手

        //public fixed byte putPiece[80]; // 置き駒情報(盤上情報の番号を格納)
        // [0-39]先手 [40-79]後手
        // enum ktype

        public fixed int putOusyou[2]; // 王将置き駒情報(X*9+Y)
        public fixed int putKinsyou[8]; // 金将置き駒情報(X*9+Y)
        public fixed int putKyousha[8]; // 香車置き駒情報(X*9+Y)
        public fixed int putKeima[8]; // 桂馬置き駒情報(X*9+Y)
        public fixed int putGinsyou[8]; // 銀将置き駒情報(X*9+Y)
        public fixed int putHisya[4]; // 飛車置き駒情報(X*9+Y)
        public fixed int putKakugyou[4]; // 角行置き駒情報(X*9+Y)
        public fixed int putFuhyou[18]; // 歩兵置き駒情報(0-9) 9:無し// [0-8]先手 / [9-17]後手
        public fixed int putNarigoma[60]; // 成り駒置き駒情報(と金・成香・成桂・成銀)(X*9+Y)
        public fixed int putNarigomaNum[2]; // 成り駒数



        public fixed byte captPiece[14]; // 持ち駒情報
        // [0-6]先手 [7-13]後手


        public fixed byte moveable[162]; //駒の移動可能リスト(turn*81+X*9+Y)

        // 初期盤情報
        public void startpos() {

            putOusyou[0] = 0xFF;
            putOusyou[1] = 0xFF;
            for (int i = 0; i < 4; i++) {
                putHisya[i] = 0xFF;
                putKakugyou[i] = 0xFF;
            }
            for (int i = 0; i < 8; i++) {
                putKinsyou[i] = 0xFF;
                putKyousha[i] = 0xFF;
                putKeima[i] = 0xFF;
                putGinsyou[i] = 0xFF;
            }
            for (int i = 0; i < 18; i++) {
                putFuhyou[i] = 9;
            }
            for (int i = 0; i < 60; i++) {
                putNarigoma[i] = 0xFF;
            }

            //王の配置
            putKoma(4, 8, Pturn.Sente, ktype.Ousyou);
            putKoma(4, 0, Pturn.Gote, ktype.Ousyou);

            //金の配置
            putKoma(3, 8, Pturn.Sente, ktype.Kinsyou);
            putKoma(5, 8, Pturn.Sente, ktype.Kinsyou);
            putKoma(3, 0, Pturn.Gote, ktype.Kinsyou);
            putKoma(5, 0, Pturn.Gote, ktype.Kinsyou);

            //銀の配置
            putKoma(2, 8, Pturn.Sente, ktype.Ginsyou);
            putKoma(6, 8, Pturn.Sente, ktype.Ginsyou);
            putKoma(2, 0, Pturn.Gote, ktype.Ginsyou);
            putKoma(6, 0, Pturn.Gote, ktype.Ginsyou);

            //桂の配置
            putKoma(1, 8, Pturn.Sente, ktype.Keima);
            putKoma(7, 8, Pturn.Sente, ktype.Keima);
            putKoma(1, 0, Pturn.Gote, ktype.Keima);
            putKoma(7, 0, Pturn.Gote, ktype.Keima);

            //香の配置
            putKoma(0, 8, Pturn.Sente, ktype.Kyousha);
            putKoma(8, 8, Pturn.Sente, ktype.Kyousha);
            putKoma(0, 0, Pturn.Gote, ktype.Kyousha);
            putKoma(8, 0, Pturn.Gote, ktype.Kyousha);

            //角の配置
            putKoma(7, 7, Pturn.Sente, ktype.Kakugyou);
            putKoma(1, 1, Pturn.Gote, ktype.Kakugyou);

            //飛の配置
            putKoma(1, 7, Pturn.Sente, ktype.Hisya);
            putKoma(7, 1, Pturn.Gote, ktype.Hisya);

            //歩の配置
            for (int i = 0; i < 9; i++) {
                putKoma(i, 6, Pturn.Sente, ktype.Fuhyou);
                putKoma(i, 2, Pturn.Gote, ktype.Fuhyou);
            }

            // 移動リスト新規作成
            //renewMoveable();

            //for (int i = 0; i < 162; i++) {
            //    Console.WriteLine(i + ":" + moveable[i]);
            //}

        }

        //移動可能リスト生成(先手・後手の駒が移動可能場所を加算する)
        public void renewMoveable() {
            //指せる手を全てリスト追加
            for (int p = 0; p < 2; p++) {
                // 王将
                if (putOusyou[p] != 0xFF) {
                    chgMoveable(putOusyou[p] / 9, putOusyou[p] % 9, (Pturn)p, 1);
                }

                // 歩兵
                for (int i = 0; i < 9; i++) {
                    if (putFuhyou[p * 9 + i] != 9) {
                        chgMoveable(i, putFuhyou[p * 9 + i], (Pturn)p, 1);
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if (putKyousha[p * 4 + i] != 0xFF) {
                        chgMoveable(putKyousha[p * 4 + i] / 9, putKyousha[p * 4 + i] % 9, (Pturn)p, 1);
                    }
                }

                // 桂馬
                for (int i = 0; i < 4; i++) {
                    if (putKeima[p * 4 + i] != 0xFF) {
                        chgMoveable(putKeima[p * 4 + i] / 9, putKeima[p * 4 + i] % 9, (Pturn)p, 1);
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if (putGinsyou[p * 4 + i] != 0xFF) {
                        chgMoveable(putGinsyou[p * 4 + i] / 9, putGinsyou[p * 4 + i] % 9, (Pturn)p, 1);
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if (putHisya[p * 2 + i] != 0xFF) {
                        chgMoveable(putHisya[p * 2 + i] / 9, putHisya[p * 2 + i] % 9, (Pturn)p, 1);
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if (putKakugyou[p * 2 + i] != 0xFF) {
                        chgMoveable(putKakugyou[p * 2 + i] / 9, putKakugyou[p * 2 + i] % 9, (Pturn)p, 1);
                    }
                }

                // 金将
                for (int i = 0; i < 4; i++) {
                    if (putKinsyou[p * 4 + i] != 0xFF) {
                        chgMoveable(putKinsyou[p * 4 + i] / 9, putKinsyou[p * 4 + i] % 9, (Pturn)p, 1);
                    }
                }

                // 成駒
                for (int i = 0, j = 0; j < putNarigomaNum[p]; i++) {
                    if (putNarigoma[p * 30 + i] != 0xFF) {
                        chgMoveable(putNarigoma[p * 30 + i] / 9, putNarigoma[p * 30 + i] % 9, (Pturn)p, 1);
                        j++;
                    }
                }
            }

        }

        public void changeMoveableDir(int x, int y, int val) {
            //上
            for (int i = 1; y - i >= 0; i++) {
                if (onBoard[x * 9 + (y - i)] == 0) continue;
                if ((getOnBoardKtype(x, y - i) == ktype.Hisya) || (getOnBoardKtype(x, y - i) == ktype.Ryuuou) || ((getOnBoardKtype(x, y - i) == ktype.Kyousha) && (getOnBoardPturn(x, y - i) == Pturn.Gote))) {
                    // 下を更新
                    for (int j = 1; y + j < 9; j++) {
                        moveable[(int)getOnBoardPturn(x, y - i) * 81 + x * 9 + (y + j)] += (byte)val;
                        if (onBoard[x * 9 + (y + j)] > 0) break;
                    }
                }
                break;
            }

            //下
            for (int i = 1; y + i < 9; i++) {
                if (onBoard[x * 9 + (y + i)] == 0) continue;
                if ((getOnBoardKtype(x, y + i) == ktype.Hisya) || (getOnBoardKtype(x, y + i) == ktype.Ryuuou) || ((getOnBoardKtype(x, y + i) == ktype.Kyousha) && (getOnBoardPturn(x, y + i) == Pturn.Sente))) {
                    // 下を更新
                    for (int j = 1; y - j >= 0; j++) {
                        moveable[(int)getOnBoardPturn(x, y + i) * 81 + x * 9 + (y - j)] += (byte)val;
                        if (onBoard[x * 9 + (y - j)] > 0) break;
                    }
                }
                break;
            }

            //右
            for (int i = 1; x + i < 9; i++) {
                if (onBoard[(x + i) * 9 + y] == 0) continue;
                if ((getOnBoardKtype(x + i, y) == ktype.Hisya) || (getOnBoardKtype(x + i, y) == ktype.Ryuuou)) {
                    // 左を更新
                    for (int j = 1; x - j >= 0; j++) {
                        moveable[(int)getOnBoardPturn(x + i, y) * 81 + (x - j) * 9 + y] += (byte)val;
                        if (onBoard[(x - j) * 9 + y] > 0) break;
                    }
                }
                break;
            }

            //左
            for (int i = 1; x - i >= 0; i++) {
                if (onBoard[(x - i) * 9 + y] == 0) continue;
                if ((getOnBoardKtype(x - i, y) == ktype.Hisya) || (getOnBoardKtype(x - i, y) == ktype.Ryuuou)) {
                    // 左を更新
                    for (int j = 1; x + j < 9; j++) {
                        moveable[(int)getOnBoardPturn(x - i, y) * 81 + (x + j) * 9 + y] += (byte)val;
                        if (onBoard[(x + j) * 9 + y] > 0) break;
                    }
                }
                break;
            }

            //右上
            for (int i = 1; x - i >= 0 && y - i >= 0; i++) {
                if (onBoard[(x - i) * 9 + (y - i)] == 0) continue;
                if ((getOnBoardKtype(x - i, y - i) == ktype.Kakugyou) || (getOnBoardKtype(x - i, y - i) == ktype.Ryuuma)) {
                    // 左を更新
                    for (int j = 1; x + j < 9 && y + j < 9; j++) {
                        moveable[(int)getOnBoardPturn(x - i, y - i) * 81 + (x + j) * 9 + y + j] += (byte)val;
                        if (onBoard[(x + j) * 9 + y + j] > 0) break;
                    }
                }
                break;
            }

            //右下
            for (int i = 1; x - i >= 0 && y + i < 9; i++) {
                if (onBoard[(x - i) * 9 + y + i] == 0) continue;
                if ((getOnBoardKtype(x - i, y + i) == ktype.Kakugyou) || (getOnBoardKtype(x - i, y + i) == ktype.Ryuuma)) {
                    // 左を更新
                    for (int j = 1; x + j < 9 && y - j >= 0; j++) {
                        moveable[(int)getOnBoardPturn(x - i, y + i) * 81 + (x + j) * 9 + y - j] += (byte)val;
                        if (onBoard[(x + j) * 9 + y - j] > 0) break;
                    }
                }
                break;
            }

            //左上
            for (int i = 1; x + i < 9 && y - i >= 0; i++) {
                if (onBoard[(x + i) * 9 + y - i] == 0) continue;
                if ((getOnBoardKtype(x + i, y - i) == ktype.Kakugyou) || (getOnBoardKtype(x + i, y - i) == ktype.Ryuuma)) {
                    // 左を更新
                    for (int j = 1; x - j >= 0 && y + j < 9; j++) {
                        moveable[(int)getOnBoardPturn(x + i, y - i) * 81 + (x - j) * 9 + y + j] += (byte)val;
                        if (onBoard[(x - j) * 9 + y + j] > 0) break;
                    }
                }
                break;
            }

            //左下
            for (int i = 1; x + i < 9 && y + i < 9; i++) {
                if (onBoard[(x + i) * 9 + y + i] == 0) continue;
                if ((getOnBoardKtype(x + i, y + i) == ktype.Kakugyou) || (getOnBoardKtype(x + i, y + i) == ktype.Ryuuma)) {
                    // 左を更新
                    for (int j = 1; x - j >= 0 && y - j >= 0; j++) {
                        moveable[(int)getOnBoardPturn(x + i, y + i) * 81 + (x - j) * 9 + y - j] += (byte)val;
                        if (onBoard[(x - j) * 9 + y - j] > 0) break;
                    }
                }
                break;
            }
        }

        //盤上に駒を置く
        public void putKoma(int x, int y, Pturn turn, ktype type) {
            switch (type) {
                case ktype.Fuhyou:
                    putFuhyou[(int)turn * 9 + x] = y;
                    onBoard[x * 9 + y] = setOnBordDatat(turn, 0, type);
                    break;

                case ktype.Kyousha:
                    for (int i = 0; i < 4; i++) {
                        if (putKyousha[(int)turn * 4 + i] == 0xFF) {
                            putKyousha[(int)turn * 4 + i] = x * 9 + y;
                            onBoard[x * 9 + y] = setOnBordDatat(turn, i, type);
                            break;
                        }
                    }
                    break;

                case ktype.Keima:
                    for (int i = 0; i < 4; i++) {
                        if (putKeima[(int)turn * 4 + i] == 0xFF) {
                            putKeima[(int)turn * 4 + i] = x * 9 + y;
                            onBoard[x * 9 + y] = setOnBordDatat(turn, i, type);
                            break;
                        }
                    }
                    break;

                case ktype.Ginsyou:
                    for (int i = 0; i < 4; i++) {
                        if (putGinsyou[(int)turn * 4 + i] == 0xFF) {
                            putGinsyou[(int)turn * 4 + i] = x * 9 + y;
                            onBoard[x * 9 + y] = setOnBordDatat(turn, i, type);
                            break;
                        }
                    }
                    break;

                case ktype.Hisya:
                case ktype.Ryuuou:
                    for (int i = 0; i < 2; i++) {
                        if (putHisya[(int)turn * 2 + i] == 0xFF) {
                            putHisya[(int)turn * 2 + i] = x * 9 + y;
                            onBoard[x * 9 + y] = setOnBordDatat(turn, i, type);
                            break;
                        }
                    }
                    break;

                case ktype.Kakugyou:
                case ktype.Ryuuma:
                    for (int i = 0; i < 2; i++) {
                        if (putKakugyou[(int)turn * 2 + i] == 0xFF) {
                            putKakugyou[(int)turn * 2 + i] = x * 9 + y;
                            onBoard[x * 9 + y] = setOnBordDatat(turn, i, type);
                            break;
                        }
                    }
                    break;

                case ktype.Kinsyou:
                    for (int i = 0; i < 4; i++) {
                        if (putKinsyou[(int)turn * 4 + i] == 0xFF) {
                            putKinsyou[(int)turn * 4 + i] = x * 9 + y;
                            onBoard[x * 9 + y] = setOnBordDatat(turn, i, type);
                            break;
                        }
                    }
                    break;

                case ktype.Ousyou:
                    putOusyou[(int)turn] = x * 9 + y;
                    onBoard[x * 9 + y] = setOnBordDatat(turn, 0, type);
                    break;

                case ktype.Tokin:
                case ktype.Narikyou:
                case ktype.Narikei:
                case ktype.Narigin:
                    for (int i = 0; i < 30; i++) {
                        if (putNarigoma[(int)turn * 30 + i] == 0xFF) {
                            putNarigoma[(int)turn * 30 + i] = (byte)(x * 9 + y);
                            onBoard[x * 9 + y] = setOnBordDatat(turn, i, type);
                            putNarigomaNum[(int)turn]++;
                            break;
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        //盤上から駒を取り除く(onBoardは更新不要)
        void removeKoma(int x, int id, Pturn turn, ktype type) {
            switch (type) {
                case ktype.Fuhyou:
                    putFuhyou[(int)turn * 9 + x] = 9;
                    break;

                case ktype.Kyousha:
                    putKyousha[(int)turn * 4 + id] = 0xFF;
                    break;

                case ktype.Keima:
                    putKeima[(int)turn * 4 + id] = 0xFF;
                    break;

                case ktype.Ginsyou:
                    putGinsyou[(int)turn * 4 + id] = 0xFF;
                    break;

                case ktype.Hisya:
                case ktype.Ryuuou:
                    putHisya[(int)turn * 2 + id] = 0xFF;
                    break;

                case ktype.Kakugyou:
                case ktype.Ryuuma:
                    putKakugyou[(int)turn * 2 + id] = 0xFF;
                    break;

                case ktype.Kinsyou:
                    putKinsyou[(int)turn * 4 + id] = 0xFF;
                    break;

                case ktype.Ousyou: // ありえないが
                    putOusyou[(int)turn] = 0xFF;
                    break;

                case ktype.Tokin:
                case ktype.Narikyou:
                case ktype.Narikei:
                case ktype.Narigin:
                    putNarigoma[(int)turn * 30 + id] = 0xFF;
                    putNarigomaNum[(int)turn]--;
                    break;

                default:
                    break;
            }
        }

        //盤上から駒を移動する
        void moveKoma(int x, int y, int id, Pturn turn, ktype type) {
            switch (type) {
                case ktype.Fuhyou:
                    putFuhyou[(int)turn * 9 + x] = y;
                    break;

                case ktype.Kyousha:
                    putKyousha[(int)turn * 4 + id] = x * 9 + y;
                    break;

                case ktype.Keima:
                    putKeima[(int)turn * 4 + id] = x * 9 + y;
                    break;

                case ktype.Ginsyou:
                    putGinsyou[(int)turn * 4 + id] = x * 9 + y;
                    break;

                case ktype.Hisya:
                case ktype.Ryuuou:
                    putHisya[(int)turn * 2 + id] = x * 9 + y;
                    break;

                case ktype.Kakugyou:
                case ktype.Ryuuma:
                    putKakugyou[(int)turn * 2 + id] = x * 9 + y;
                    break;

                case ktype.Kinsyou:
                    putKinsyou[(int)turn * 4 + id] = x * 9 + y;
                    break;

                case ktype.Ousyou: // ありえないが
                    putOusyou[(int)turn] = x * 9 + y;
                    break;

                case ktype.Tokin:
                case ktype.Narikyou:
                case ktype.Narikei:
                case ktype.Narigin:
                    putNarigoma[(int)turn * 30 + id] = x * 9 + y;
                    break;

                default:
                    break;
            }
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
        public int moveKoma(int ox, int oy, int nx, int ny, Pturn turn, bool nari, bool chk, bool renewMoveable) {

            //Span<int> renewMoveableList = stackalloc int[40];
            //int renewMoveableListNum = 0;

            // 駒打ち
            if (ox == 9) {
                // 合法手チェック
                if (chk) if ((onBoard[nx * 9 + ny] == 0)) return -1;
                captPiece[oy - 1 + (int)turn * 7]--;

                // 更新
                //onBoard[nx + ny * 9] = (byte)(((int)turn << 4) + oy);

                //置き駒情報・盤上情報を更新
                putKoma(nx, ny, turn, (ktype)oy);

                if (renewMoveable == true) {
                    changeMoveableDir(nx, ny, -1);
                    chgMoveable(nx, ny, getOnBoardPturn(nx, ny), 1);
                }

                //renewMoveableList[renewMoveableListNum++] = ox + oy * 9;
                //renewMoveableLink(ox, oy, ref renewMoveableListNum, ref renewMoveableList);

                // 駒移動
            } else {
                // 合法手チェック
                if (chk) if ((captPiece[oy + (int)turn * 7] < 1) || (checkMoveable(getOnBoardKtype(ox, oy), getOnBoardPturn(ox, oy), ox, oy, nx, ny) < 0)) return -1;

                // 移動先に既にある
                if (onBoard[nx * 9 + ny] > 0) {
                    // 味方駒は取れない
                    if (chk) if (getOnBoardPturn(ox, oy) == getOnBoardPturn(nx, ny)) return -1;
                    if (renewMoveable == true) chgMoveable(nx, ny, getOnBoardPturn(nx, ny), -1);
                    removeKoma(nx, getOnBoardPutPiece(nx, ny), (Pturn)pturn.aturn((int)turn), getOnBoardKtype(nx, ny));

                    // 追加
                    captPiece[(int)kNoNari(getOnBoardKtype(nx, ny)) - 1 + (int)getOnBoardPturn(ox, oy) * 7]++;
                    onBoard[nx * 9 + ny] = setOnBordDatat(pturn.aturn(turn), 0, ktype.None); //駒情報を一時クリア
                } else {
                    //changeMoveableDir(nx, ny, -1);
                }

                // 成れる位置ではない
                if (chk) if ((pturn.psX(getOnBoardPturn(ox, oy), nx) > 5) && (nari == true)) return -1;

                ushort mk = 0;

                if (renewMoveable == true) chgMoveable(ox, oy, getOnBoardPturn(ox, oy), -1);

                // 成り
                if (nari) {
                    // 歩情報更新
                    switch (getOnBoardKtype(ox, oy)) {
                        case ktype.Fuhyou:
                            putFuhyou[(int)turn * 9 + nx] = 9;
                            for (int i = 0; i < 30; i++) {
                                if (putNarigoma[(int)turn * 30 + i] == 0xFF) {
                                    putNarigoma[(int)turn * 30 + i] = (byte)(nx * 9 + ny);
                                    mk = setOnBordDatat(turn, i, ktype.Tokin);
                                    putNarigomaNum[(int)turn]++;
                                    break;
                                }
                            }
                            break;

                        case ktype.Kyousha:
                            putKyousha[(int)turn * 4 + getOnBoardPutPiece(ox, oy)] = 0xFF;
                            for (int i = 0; i < 30; i++) {
                                if (putNarigoma[(int)turn * 30 + i] == 0xFF) {
                                    putNarigoma[(int)turn * 30 + i] = (byte)(nx * 9 + ny);
                                    mk = setOnBordDatat(turn, i, ktype.Narikyou);
                                    putNarigomaNum[(int)turn]++;
                                    break;
                                }
                            }
                            break;
                        case ktype.Keima:
                            putKeima[(int)turn * 4 + getOnBoardPutPiece(ox, oy)] = 0xFF;
                            for (int i = 0; i < 30; i++) {
                                if (putNarigoma[(int)turn * 30 + i] == 0xFF) {
                                    putNarigoma[(int)turn * 30 + i] = (byte)(nx * 9 + ny);
                                    mk = setOnBordDatat(turn, i, ktype.Narikei);
                                    putNarigomaNum[(int)turn]++;
                                    break;
                                }
                            }
                            break;
                        case ktype.Ginsyou:
                            putGinsyou[(int)turn * 4 + getOnBoardPutPiece(ox, oy)] = 0xFF;
                            for (int i = 0; i < 30; i++) {
                                if (putNarigoma[(int)turn * 30 + i] == 0xFF) {
                                    putNarigoma[(int)turn * 30 + i] = (byte)(nx * 9 + ny);
                                    mk = setOnBordDatat(turn, i, ktype.Narigin);
                                    putNarigomaNum[(int)turn]++;
                                    break;
                                }
                            }
                            break;
                        default:
                            moveKoma(nx, ny, getOnBoardPutPiece(ox, oy), turn, getOnBoardKtype(ox, oy));
                            mk = setOnBordDatat(turn, getOnBoardPutPiece(ox, oy), kDoNari(getOnBoardKtype(ox, oy)));
                            break;
                    }
                    onBoard[ox * 9 + oy] = 0;
                    if (renewMoveable == true) {
                        if (onBoard[nx * 9 + ny] == 0) {
                            changeMoveableDir(nx, ny, -1);
                        }
                        changeMoveableDir(ox, oy, 1);
                    }

                    onBoard[nx * 9 + ny] = mk;

                    if (renewMoveable == true) chgMoveable(nx, ny, getOnBoardPturn(nx, ny), 1);

                    // 不成or通常移動
                } else {
                    moveKoma(nx, ny, getOnBoardPutPiece(ox, oy), turn, getOnBoardKtype(ox, oy));
                    mk = onBoard[ox * 9 + oy];
                    onBoard[ox * 9 + oy] = 0;
                    if (renewMoveable == true) {
                        if (onBoard[nx * 9 + ny] == 0) {
                            changeMoveableDir(nx, ny, -1);
                        }
                        changeMoveableDir(ox, oy, 1);
                    }

                    onBoard[nx * 9 + ny] = mk;

                    if (renewMoveable == true) chgMoveable(nx, ny, getOnBoardPturn(nx, ny), 1);
                }



            }

            return 0;
        }

        // 盤上情報上の駒情報を取得**将来廃止
        public ktype getOnBoardKtype(int x, int y) {
            return (ktype)(onBoard[x * 9 + y] & 0x0F);
        }

        // 盤上情報上の駒情報を取得
        public ktype getOnBoardKtype(int pos) {
            return (ktype)(onBoard[pos] & 0x0F);
        }

        // 盤上情報上の駒所有者を取得**将来廃止
        public Pturn getOnBoardPturn(int x, int y) {
            return (Pturn)((onBoard[x * 9 + y] & 0xF0) >> 4);
        }

        public Pturn getOnBoardPturn(int pos) {
            return (Pturn)((onBoard[pos] & 0xF0) >> 4);
        }

        // 盤上情報上の置き駒情報を取得
        public int getOnBoardPutPiece(int x, int y) {
            return ((onBoard[x * 9 + y] & 0xFF00) >> 8);
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

        //void renewMoveableLink(int x, int y, ref int num, ref Span<int> list) {
        //
        //}

        // 処理軽減のためチェック省略
        public static ktype kDoNari(ktype t) {
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

        public void chgMoveable(int ox, int oy, Pturn turn, int val) {
            switch (getOnBoardKtype(ox, oy)) {
                case ktype.Fuhyou:
                    addMoveableEachKoma(ox, oy, 0, 1, turn, val);
                    break;

                case ktype.Kyousha:
                    for (int i = 1; addMoveableEachKoma(ox, oy, 0, i, turn, val) < 1; i++) ;
                    break;

                case ktype.Keima:
                    addMoveableEachKoma(ox, oy, 1, 2, turn, val);
                    addMoveableEachKoma(ox, oy, -1, 2, turn, val);
                    break;

                case ktype.Ginsyou:
                    addMoveableEachKoma(ox, oy, 1, 1, turn, val);
                    addMoveableEachKoma(ox, oy, 0, 1, turn, val);
                    addMoveableEachKoma(ox, oy, -1, 1, turn, val);
                    addMoveableEachKoma(ox, oy, 1, -1, turn, val);
                    addMoveableEachKoma(ox, oy, -1, -1, turn, val);
                    break;

                case ktype.Hisya:
                    for (int i = 1; addMoveableEachKoma(ox, oy, 0, i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, 0, -i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, i, 0, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, -i, 0, turn, val) < 1; i++) ;
                    break;

                case ktype.Kakugyou:
                    for (int i = 1; addMoveableEachKoma(ox, oy, i, i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, i, -i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, -i, i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, -i, -i, turn, val) < 1; i++) ;
                    break;

                case ktype.Kinsyou:
                case ktype.Tokin:
                case ktype.Narikyou:
                case ktype.Narikei:
                case ktype.Narigin:
                    addMoveableEachKoma(ox, oy, 1, 1, turn, val);
                    addMoveableEachKoma(ox, oy, 0, 1, turn, val);
                    addMoveableEachKoma(ox, oy, -1, 1, turn, val);
                    addMoveableEachKoma(ox, oy, 1, 0, turn, val);
                    addMoveableEachKoma(ox, oy, -1, 0, turn, val);
                    addMoveableEachKoma(ox, oy, 0, -1, turn, val);
                    break;

                case ktype.Ousyou:
                    addMoveableEachKoma(ox, oy, 1, 1, turn, val);
                    addMoveableEachKoma(ox, oy, 0, 1, turn, val);
                    addMoveableEachKoma(ox, oy, -1, 1, turn, val);
                    addMoveableEachKoma(ox, oy, 1, 0, turn, val);
                    addMoveableEachKoma(ox, oy, -1, 0, turn, val);
                    addMoveableEachKoma(ox, oy, 1, -1, turn, val);
                    addMoveableEachKoma(ox, oy, 0, -1, turn, val);
                    addMoveableEachKoma(ox, oy, -1, -1, turn, val);
                    break;

                case ktype.Ryuuou:
                    addMoveableEachKoma(ox, oy, 1, 1, turn, val);
                    addMoveableEachKoma(ox, oy, -1, 1, turn, val);
                    addMoveableEachKoma(ox, oy, 1, -1, turn, val);
                    addMoveableEachKoma(ox, oy, -1, -1, turn, val);
                    for (int i = 1; addMoveableEachKoma(ox, oy, 0, i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, 0, -i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, i, 0, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, -i, 0, turn, val) < 1; i++) ;
                    break;

                case ktype.Ryuuma:
                    addMoveableEachKoma(ox, oy, 0, 1, turn, val);
                    addMoveableEachKoma(ox, oy, 1, 0, turn, val);
                    addMoveableEachKoma(ox, oy, -1, 0, turn, val);
                    addMoveableEachKoma(ox, oy, 0, -1, turn, val);
                    for (int i = 1; addMoveableEachKoma(ox, oy, i, i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, i, -i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, -i, i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(ox, oy, -i, -i, turn, val) < 1; i++) ;
                    break;

                default:
                    break;
            }
        }

        int addMoveableEachKoma(int ox, int oy, int mx, int my, Pturn turn, int val) {
            int nx = pturn.mvX(turn, ox, mx);
            int ny = pturn.mvY(turn, oy, my);
            if ((nx < 0) || (nx > 8) || (ny < 0) || (ny > 8)) return 1; // 移動できない
            moveable[(int)turn * 81 + nx * 9 + ny] += (byte)val;
            if (onBoard[nx * 9 + ny] > 0) {
                return 1; // 駒がある
            } else {
                return 0; // 駒がない
            }
        }

        // ban情報のデバッグ表示
        public string debugShow() {
            string str = "";
            for (int i = 0; i < 81; i++) {
                if (onBoard[(i / 9) + (8 - i % 9) * 9] != 0) {
                    // 先手
                    if (getOnBoardPturn(8 - i % 9, (i / 9)) == Pturn.Sente) {
                        switch (getOnBoardKtype(8 - i % 9, (i / 9))) {
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
                        switch (getOnBoardKtype(8 - i % 9, (i / 9))) {
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
                        str +=  /* "("+ j + ","+ ((i + 1)/9-1) + ")" + */ moveable[j * 9 + ((i + 1) / 9 - 1)] + "," + moveable[81 + j * 9 + ((i + 1) / 9 - 1)] + "|";

                    }

                    // 改行
                    str += Environment.NewLine;

                }
            }

            // 持ち駒情報
            str += "FU:" + captPiece[0] + "/KY:" + captPiece[1] + "/KE:" + captPiece[2] + "/GI:" + captPiece[3] + "/HI:" + captPiece[4] + "/KA:" + captPiece[5] + "/KI:" + captPiece[6] + "\n";
            str += "FU:" + captPiece[7] + "/KY:" + captPiece[8] + "/KE:" + captPiece[9] + "/GI:" + captPiece[10] + "/HI:" + captPiece[11] + "/KA:" + captPiece[12] + "/KI:" + captPiece[13] + "\n";

            for (int i = 0; i < 9; i++) {
                str += putFuhyou[i] + " ";

            }
            str += "\n";
            for (int i = 0; i < 9; i++) {
                str += putFuhyou[9 + i] + " ";

            }
            str += "\n";
            return str;
        }


    }
}
