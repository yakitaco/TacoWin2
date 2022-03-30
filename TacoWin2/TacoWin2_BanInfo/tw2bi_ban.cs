using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TacoWin2_BanInfo {
    public unsafe struct ban {

        /// <summary>
        /// 盤情報
        /// [0x0FMMPQTK]
        /// </summary>
        public fixed uint data[137]; // [9*9(盤上) + 7*8(位置・持ち駒)]

        public const int setOu = 0x09;
        public const int setKin = 0x0A;
        public const int setGin = 0x0B;
        public const int setKei = 0x0C;
        public const int setKyo = 0x0D;
        public const int setHi = 0x0E;
        public const int setKa = 0x0F;
        public const int setFu = 0x19;
        public const int setNaNum = 0x1F;
        public const int setNa = 0x29;

        // 持ち駒情報オフセット
        public const int hand = 0x38; //(+GoOffset +ktype)

        public const int GoOffset = 0x40;

        //public fixed ushort onBoard[81]; // 盤上情報(X<筋>*9+Y<段>)
        // WWWWWWWW000XYYYY [X]0:先手/1:後手 [Y]enum ktype [W]置き駒情報

        //public fixed byte putPieceNum[2]; // 置き駒数情報(盤上情報の番号を入れる)
        // 0 : 先手 / 1:後手

        //public fixed byte putPiece[80]; // 置き駒情報(盤上情報の番号を格納)
        // [0-39]先手 [40-79]後手
        // enum ktype

        //public fixed int putOusyou[2]; // 王将置き駒情報(X*9+Y)
        //public fixed int putKinsyou[8]; // 金将置き駒情報(X*9+Y)
        //public fixed int putKyousha[8]; // 香車置き駒情報(X*9+Y)
        //public fixed int putKeima[8]; // 桂馬置き駒情報(X*9+Y)
        //public fixed int putGinsyou[8]; // 銀将置き駒情報(X*9+Y)
        //public fixed int putHisya[4]; // 飛車置き駒情報(X*9+Y)
        //public fixed int putKakugyou[4]; // 角行置き駒情報(X*9+Y)
        //public fixed int putFuhyou[18]; // 歩兵置き駒情報(0-9) 9:無し// [0-8]先手 / [9-17]後手
        //public fixed int putNarigoma[60]; // 成り駒置き駒情報(と金・成香・成桂・成銀)(X*9+Y)
        //public fixed int putNarigomaNum[2]; // 成り駒数



        //public fixed byte captPiece[14]; // 持ち駒情報
        // [0-6]先手 [7-13]後手


        //public fixed byte moveable[162]; //駒の移動可能リスト(turn*81+X*9+Y)

        public ulong hash; //現局面のハッシュ値

        /// <summary>
        /// 初期位置設定
        /// </summary>
        public void startpos() {
            unsafe {
                data[setOu] = System.UInt32.MaxValue;
                data[GoOffset + setOu] = System.UInt32.MaxValue;
                data[setKin] = System.UInt32.MaxValue;
                data[GoOffset + setKin] = System.UInt32.MaxValue;
                data[setGin] = System.UInt32.MaxValue;
                data[GoOffset + setGin] = System.UInt32.MaxValue;
                data[setKei] = System.UInt32.MaxValue;
                data[GoOffset + setKei] = System.UInt32.MaxValue;
                data[setKyo] = System.UInt32.MaxValue;
                data[GoOffset + setKyo] = System.UInt32.MaxValue;
                data[setHi] = System.UInt32.MaxValue;
                data[GoOffset + setHi] = System.UInt32.MaxValue;
                data[setKa] = System.UInt32.MaxValue;
                data[GoOffset + setKa] = System.UInt32.MaxValue;

                for (int i = 0; i < 3; i++) {
                    data[setFu + i] = System.UInt32.MaxValue;
                    data[GoOffset + setFu + i] = System.UInt32.MaxValue;
                }

                for (int i = 0; i < 7; i++) {
                    data[setNa + i] = System.UInt32.MaxValue;
                    data[GoOffset + setNa + i] = System.UInt32.MaxValue;
                }

                //王の配置
                putKoma(0x48, Pturn.Sente, 0, ktype.Ousyou);
                putKoma(0x40, Pturn.Gote, 0, ktype.Ousyou);

                //金の配置
                putKoma(0x38, Pturn.Sente, 0, ktype.Kinsyou);
                putKoma(0x58, Pturn.Sente, 0, ktype.Kinsyou);
                putKoma(0x30, Pturn.Gote, 0, ktype.Kinsyou);
                putKoma(0x50, Pturn.Gote, 0, ktype.Kinsyou);

                //銀の配置
                putKoma(0x28, Pturn.Sente, 0, ktype.Ginsyou);
                putKoma(0x68, Pturn.Sente, 0, ktype.Ginsyou);
                putKoma(0x20, Pturn.Gote, 0, ktype.Ginsyou);
                putKoma(0x60, Pturn.Gote, 0, ktype.Ginsyou);

                //桂の配置
                putKoma(0x18, Pturn.Sente, 0, ktype.Keima);
                putKoma(0x78, Pturn.Sente, 0, ktype.Keima);
                putKoma(0x10, Pturn.Gote, 0, ktype.Keima);
                putKoma(0x70, Pturn.Gote, 0, ktype.Keima);

                //香の配置
                putKoma(0x08, Pturn.Sente, 0, ktype.Kyousha);
                putKoma(0x88, Pturn.Sente, 0, ktype.Kyousha);
                putKoma(0x00, Pturn.Gote, 0, ktype.Kyousha);
                putKoma(0x80, Pturn.Gote, 0, ktype.Kyousha);

                //角の配置
                putKoma(0x77, Pturn.Sente, 0, ktype.Kakugyou);
                putKoma(0x11, Pturn.Gote, 0, ktype.Kakugyou);

                //飛の配置
                putKoma(0x17, Pturn.Sente, 0, ktype.Hisya);
                putKoma(0x71, Pturn.Gote, 0, ktype.Hisya);

                //歩の配置
                for (int i = 0; i < 9; i++) {
                    putKoma((i << 4) + 6, Pturn.Sente, 0, ktype.Fuhyou);
                    putKoma((i << 4) + 2, Pturn.Gote, 0, ktype.Fuhyou);
                }

                //hash値作成(固定値事前保持のため削除)
                //for (int i = 0; i < 81; i++) {
                //    if (onBoard[i] > 0) {
                //        hash ^= tw2bi_hash.okiSeed[(int)getOnBoardPturn(i) * 14 + (int)getOnBoardKtype(i) - 1, i];
                //    }
                //}
                //Console.WriteLine("[HASH]" + (hash).ToString("X16"));
                hash = tw2bi_hash.startHash;

                // 移動リスト新規作成
                //renewMoveable();

                //for (int i = 0; i < 162; i++) {
                //    Console.WriteLine(i + ":" + moveable[i]);
                //}
            }
        }

        //移動可能リスト生成(先手・後手の駒が移動可能場所を加算する)
        public void renewMoveable() {
            //指せる手を全てリスト追加
            for (int turn = 0; turn < 2; turn++) {
                // 王将
                if ((byte)data[((int)turn << 6) + setOu] != 0xFF) {
                    chgMoveable((byte)data[((int)turn << 6) + setOu], (Pturn)turn, 1);
                }

                // 歩兵
                for (int i = 0; i < 9; i++) {
                    if ((data[((int)turn << 6) + setFu + (i >> 2)] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        chgMoveable((byte)(data[((int)turn << 6) + setFu + (i >> 2)] >> ((i & 3) << 3)), (Pturn)turn, 1);
                    }
                }

                // 香車
                for (int i = 0; i < 4; i++) {
                    if ((data[((int)turn << 6) + setKyo] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        chgMoveable((byte)(data[((int)turn << 6) + setKyo] >> ((i & 3) << 3) & 0xFF), (Pturn)turn, 1);
                    }
                }

                // 桂馬
                for (int i = 0; i < 4; i++) {
                    if ((data[((int)turn << 6) + setKei] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        chgMoveable((byte)(data[((int)turn << 6) + setKei] >> ((i & 3) << 3) & 0xFF), (Pturn)turn, 1);
                    }
                }

                // 銀将
                for (int i = 0; i < 4; i++) {
                    if ((data[((int)turn << 6) + setGin] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        chgMoveable((byte)(data[((int)turn << 6) + setGin] >> ((i & 3) << 3) & 0xFF), (Pturn)turn, 1);
                    }
                }

                // 飛車
                for (int i = 0; i < 2; i++) {
                    if ((data[((int)turn << 6) + setHi] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        chgMoveable((byte)(data[((int)turn << 6) + setHi] >> ((i & 3) << 3) & 0xFF), (Pturn)turn, 1);
                    }
                }

                // 角行
                for (int i = 0; i < 2; i++) {
                    if ((data[((int)turn << 6) + setKa] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        chgMoveable((byte)(data[((int)turn << 6) + setKa] >> ((i & 3) << 3) & 0xFF), (Pturn)turn, 1);
                    }
                }

                // 金将
                for (int i = 0; i < 4; i++) {
                    if ((data[((int)turn << 6) + setKin] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        chgMoveable((byte)(data[((int)turn << 6) + setKin] >> ((i & 3) << 3) & 0xFF), (Pturn)turn, 1);
                    }
                }

                // 成駒
                for (int i = 0, j = 0; j < data[((int)turn << 6) + setNaNum] && i < 28; i++) {
                    //Console.WriteLine(99999.ToString("X"));
                    if ((data[((int)turn << 6) + setNa + (i >> 2)] >> ((i & 3) << 3) & 0xFF) != 0xFF) {
                        chgMoveable((byte)(data[((int)turn << 6) + setNa + (i >> 2)] >> ((i & 3) << 3) & 0xFF), (Pturn)turn, 1);
                        j++;
                    }
                }
            }

        }

        public void changeMoveableDir(byte oPos, int val) {
            //上
            for (byte tPos = (byte)(oPos + 0x01); (tPos & 0x0F) < 0x09; tPos++) {
                //Console.WriteLine("hh1" + tPos.ToString("X") + "," + oPos.ToString("X") + getOnBoardKtype(tPos));
                if (getOnBoardKtype(tPos) == ktype.None) continue;
                if ((getOnBoardKtype(tPos) == ktype.Hisya) || (getOnBoardKtype(tPos) == ktype.Ryuuou) || ((getOnBoardKtype(tPos) == ktype.Kyousha) && (getOnBoardPturn(tPos) == Pturn.Sente))) {
                    //Console.WriteLine("hh1" + tPos.ToString("X"));
                    // 下を更新
                    for (byte cPos = (byte)(oPos - 0x01); (cPos & 0x0F) < 0x09; cPos--) {
                        data[cPos] += (uint)(val << (8 + (int)getOnBoardPturn(tPos) * 4));
                        if (getOnBoardKtype(cPos) > ktype.None) break;
                    }
                }
                break;
            }

            //下
            for (byte tPos = (byte)(oPos - 0x01); (tPos & 0x0F) < 0x09; tPos--) {
                if (getOnBoardKtype(tPos) == ktype.None) continue;
                if ((getOnBoardKtype(tPos) == ktype.Hisya) || (getOnBoardKtype(tPos) == ktype.Ryuuou) || ((getOnBoardKtype(tPos) == ktype.Kyousha) && (getOnBoardPturn(tPos) == Pturn.Gote))) {
                    // 上を更新
                    //Console.WriteLine("hh2" + tPos.ToString("X"));
                    for (byte cPos = (byte)(oPos + 0x01); (cPos & 0x0F) < 0x09; cPos++) {
                        data[cPos] += (uint)(val << (8 + (int)getOnBoardPturn(tPos) * 4));
                        if (getOnBoardKtype(cPos) > ktype.None) break;
                    }
                }
                break;
            }

            //右
            for (byte tPos = (byte)(oPos + 0x10); tPos < 0x89; tPos += 0x10) {
                if (getOnBoardKtype(tPos) == ktype.None) continue;
                if ((getOnBoardKtype(tPos) == ktype.Hisya) || (getOnBoardKtype(tPos) == ktype.Ryuuou)) {
                    //Console.WriteLine("hh3" + tPos.ToString("X"));
                    // 左を更新
                    for (byte cPos = (byte)(oPos - 0x10); cPos < 0x89; cPos -= 0x10) {
                        data[cPos] += (uint)(val << (8 + (int)getOnBoardPturn(tPos) * 4));
                        if (getOnBoardKtype(cPos) > ktype.None) break;
                    }
                }
                break;
            }

            //左
            for (byte tPos = (byte)(oPos - 0x10); tPos < 0x89; tPos -= 0x10) {
                if (getOnBoardKtype(tPos) == ktype.None) continue;
                if ((getOnBoardKtype(tPos) == ktype.Hisya) || (getOnBoardKtype(tPos) == ktype.Ryuuou)) {
                    //Console.WriteLine("hh4" + tPos.ToString("X"));
                    // 右を更新
                    for (byte cPos = (byte)(oPos + 0x10); cPos < 0x89; cPos += 0x10) {
                        data[cPos] += (uint)(val << (8 + (int)getOnBoardPturn(tPos) * 4));
                        if (getOnBoardKtype(cPos) > ktype.None) break;
                    }
                }
                break;
            }

            //右上 (+0x10 +0x01)
            for (byte tPos = (byte)(oPos + 0x11); tPos < 0x89 && (tPos & 0x0F) < 0x09; tPos += 0x11) {
                //Console.WriteLine("kk1" + tPos.ToString("X") + "," + oPos.ToString("X") + getOnBoardKtype(tPos));
                if (getOnBoardKtype(tPos) == ktype.None) continue;
                if ((getOnBoardKtype(tPos) == ktype.Kakugyou) || (getOnBoardKtype(tPos) == ktype.Ryuuma)) {
                    //Console.WriteLine("kk1" + tPos.ToString("X"));
                    // 左下を更新
                    for (byte cPos = (byte)(oPos - 0x11); cPos < 0x89 && (cPos & 0x0F) < 0x09; cPos -= 0x11) {
                        data[cPos] += (uint)(val << (8 + (int)getOnBoardPturn(tPos) * 4));
                        if (getOnBoardKtype(cPos) > ktype.None) break;
                    }
                }
                break;
            }

            //左下 (-0x10 -0x01)
            for (byte tPos = (byte)(oPos - 0x11); tPos < 0x89 && (tPos & 0x0F) < 0x09; tPos -= 0x11) {
                if (getOnBoardKtype(tPos) == ktype.None) continue;
                if ((getOnBoardKtype(tPos) == ktype.Kakugyou) || (getOnBoardKtype(tPos) == ktype.Ryuuma)) {
                    //Console.WriteLine("kk4" + tPos.ToString("X"));
                    // 右上を更新
                    for (byte cPos = (byte)(oPos + 0x11); cPos < 0x89 && (cPos & 0x0F) < 0x09; cPos += 0x11) {
                        data[cPos] += (uint)(val << (8 + (int)getOnBoardPturn(tPos) * 4));
                        if (getOnBoardKtype(cPos) > ktype.None) break;
                    }
                }
                break;
            }

            //右下 (+0x10 -0x01)
            for (byte tPos = (byte)(oPos + 0x0F); tPos < 0x89 && (tPos & 0x0F) < 0x09; tPos += 0x0F) {
                if (getOnBoardKtype(tPos) == ktype.None) continue;
                if ((getOnBoardKtype(tPos) == ktype.Kakugyou) || (getOnBoardKtype(tPos) == ktype.Ryuuma)) {
                    //Console.WriteLine("kk2" + tPos.ToString("X"));
                    // 左上を更新
                    for (byte cPos = (byte)(oPos - 0x0F); cPos < 0x89 && (cPos & 0x0F) < 0x09; cPos -= 0x0F) {
                        data[cPos] += (uint)(val << (8 + (int)getOnBoardPturn(tPos) * 4));
                        if (getOnBoardKtype(cPos) > ktype.None) break;
                    }
                }
                break;
            }

            //左上 (-0x10 +0x01)
            for (byte tPos = (byte)(oPos - 0x0F); tPos < 0x89 && (tPos & 0x0F) < 0x09; tPos -= 0x0F) {
                //Console.WriteLine("kk3[1]" + tPos.ToString("X") + "," + oPos.ToString("X") + getOnBoardKtype(tPos));
                if (getOnBoardKtype(tPos) == ktype.None) continue;
                if ((getOnBoardKtype(tPos) == ktype.Kakugyou) || (getOnBoardKtype(tPos) == ktype.Ryuuma)) {
                    //Console.WriteLine("kk3[2]" + tPos.ToString("X"));
                    // 右下を更新
                    for (byte cPos = (byte)(oPos + 0x0F); cPos < 0x89 && (cPos & 0x0F) < 0x09; cPos += 0x0F) {
                        data[cPos] += (uint)(val << (8 + (int)getOnBoardPturn(tPos) * 4));
                        if (getOnBoardKtype(cPos) > ktype.None) break;
                    }
                }
                break;
            }

        }

        //盤上に駒を置く
        public void putKoma(int pos, Pturn turn, uint moveable, ktype type) {
            switch (type) {
                case ktype.Fuhyou:
                    data[((int)turn << 6) + setFu + (pos >> 6)] = (uint)(data[((int)turn << 6) + setFu + (pos >> 6)] & ~(0xFF << (((pos >> 4) & 3) << 3)) | (uint)(pos << (((pos >> 4) & 3) << 3)));
                    data[pos] = setOnBoardData(((int)turn << 6) + setFu + (pos >> 6), (pos >> 4) & 3, turn, moveable, type);
                    break;

                case ktype.Kyousha:
                    for (int i = 0; i < 4; i++) {
                        if ((data[((int)turn << 6) + setKyo] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                            data[((int)turn << 6) + setKyo] = (uint)(data[((int)turn << 6) + setKyo] & ~(0xFF << ((i & 3) << 3)) | (uint)(pos << ((i & 3) << 3)));
                            data[pos] = setOnBoardData(((int)turn << 6) + setKyo, i & 3, turn, moveable, type);
                            break;
                        }
                    }
                    break;

                case ktype.Keima:
                    for (int i = 0; i < 4; i++) {
                        if ((data[((int)turn << 6) + setKei] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                            data[((int)turn << 6) + setKei] = (uint)(data[((int)turn << 6) + setKei] & ~(0xFF << ((i & 3) << 3)) | (uint)(pos << ((i & 3) << 3)));
                            data[pos] = setOnBoardData(((int)turn << 6) + setKei, i & 3, turn, moveable, type);
                            break;
                        }
                    }
                    break;

                case ktype.Ginsyou:
                    for (int i = 0; i < 4; i++) {
                        if ((data[((int)turn << 6) + setGin] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                            data[((int)turn << 6) + setGin] = (uint)(data[((int)turn << 6) + setGin] & ~(0xFF << ((i & 3) << 3)) | (uint)(pos << ((i & 3) << 3)));
                            data[pos] = setOnBoardData(((int)turn << 6) + setGin, i & 3, turn, moveable, type);
                            break;
                        }
                    }
                    break;

                case ktype.Hisya:
                case ktype.Ryuuou:
                    for (int i = 0; i < 2; i++) {
                        if ((data[((int)turn << 6) + setHi] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                            data[((int)turn << 6) + setHi] = (uint)(data[((int)turn << 6) + setHi] & ~(0xFF << ((i & 3) << 3)) | (uint)(pos << ((i & 3) << 3)));
                            data[pos] = setOnBoardData(((int)turn << 6) + setHi, i & 3, turn, moveable, type);
                            break;
                        }
                    }
                    break;

                case ktype.Kakugyou:
                case ktype.Ryuuma:
                    for (int i = 0; i < 2; i++) {
                        if ((data[((int)turn << 6) + setKa] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                            data[((int)turn << 6) + setKa] = (uint)(data[((int)turn << 6) + setKa] & ~(0xFF << ((i & 3) << 3)) | (uint)(pos << ((i & 3) << 3)));
                            data[pos] = setOnBoardData(((int)turn << 6) + setKa, i & 3, turn, moveable, type);
                            break;
                        }
                    }
                    break;

                case ktype.Kinsyou:
                    for (int i = 0; i < 4; i++) {
                        if ((data[((int)turn << 6) + setKin] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                            data[((int)turn << 6) + setKin] = (uint)(data[((int)turn << 6) + setKin] & ~(0xFF << ((i & 3) << 3)) | (uint)(pos << ((i & 3) << 3)));
                            data[pos] = setOnBoardData(((int)turn << 6) + setKin, i & 3, turn, moveable, type);
                            break;
                        }
                    }
                    break;

                case ktype.Ousyou:
                    data[((int)turn << 6) + setOu] = (uint)pos;
                    data[pos] = setOnBoardData(((int)turn << 6) + setOu, 0, turn, moveable, type);

                    //putOusyou[(int)turn] = pos;
                    //onBoard[pos] = setOnBordDatat(turn, 0, type);
                    break;

                case ktype.Tokin:
                case ktype.Narikyou:
                case ktype.Narikei:
                case ktype.Narigin:
                    for (int i = 0; i < 28; i++) {
                        if ((data[((int)turn << 6) + setNa + (i >> 2)] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                            data[((int)turn << 6) + setNa + (i >> 2)] = (uint)(data[((int)turn << 6) + setNa + (i >> 2)] & ~(0xFF << ((i & 3) << 3)) | (uint)(pos << ((i & 3) << 3)));
                            data[pos] = setOnBoardData(((int)turn << 6) + setNa + (i >> 2), i & 3, turn, moveable, type);
                            ++data[((int)turn << 6) + setNaNum];
                            break;
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        //盤上から駒を取り除く(onBoardは更新不要)
        void removeKoma(int pos) {
            //byte map_offset, Pturn turn, ktype type
            //Debug.WriteLine("removeKoma 1 = " + pos.ToString("X2") + " " + data[pos].ToString("X8") + " " + (data[((data[pos] >> 16) & 0xFF)]).ToString("X8"));
            data[((data[pos] >> 16) & 0xFF)] |= (uint)(0xFF << (int)((data[pos] >> 24) << 3));
            //Debug.WriteLine("removeKoma 2 = " + pos.ToString("X2") + " " + data[pos].ToString("X8") + " " + (data[((data[pos] >> 16) & 0xFF)]).ToString("X8"));
            if ((getOnBoardKtype(pos) >= ktype.Tokin) && (getOnBoardKtype(pos) <= ktype.Narigin)) {
                data[(((data[pos] >> 4) & 1) << 6) + setNaNum]--;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        uint setOnBoardData(int map, int offset, Pturn turn, uint moveable, ktype type) {
            return (uint)((offset << 24) + (map << 16) + (moveable << 8) + ((int)turn << 4) + (int)type);
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
        public int moveKoma(byte oPos, byte nPos, Pturn turn, bool nari, bool renewMoveable) {
            // 駒打ち
            if (oPos > 0x90) {
                if (nPos > 0xF0) Debug.WriteLine("move " + debugShow());
                // 持ち駒情報からデクリメント
                data[((int)turn << 6) + hand + (oPos & 0x0F)]--;

                // hash更新
                if (oPos == 245) Debug.WriteLine("move " +  debugShow());
                    hash ^= tw2bi_hash.mochiSeed[(int)turn * 7 + (oPos & 0x0F) - 1, data[((int)turn << 6) + hand + (oPos & 0x0F)]];
                hash ^= tw2bi_hash.okiSeed[(int)turn * 14 + (int)(oPos & 0x0F) - 1, (nPos >> 4) * 9 + (nPos & 0x0F)];

                //置き駒情報・盤上情報を更新
                putKoma(nPos, turn, (data[nPos] >> 8) & 0xFF, (ktype)(oPos & 0x0F));

                if (renewMoveable == true) {
                    changeMoveableDir(nPos, -1);
                    chgMoveable((byte)nPos, turn, 1);
                }

                // 駒移動
            } else {
                turn = getOnBoardPturn(oPos);// 移動する駒の持ち手
                // 移動先に既にある
                if (getOnBoardKtype(nPos) > ktype.None) {
                    if (renewMoveable == true) chgMoveable((byte)nPos, getOnBoardPturn(nPos), -1);

                    // hash更新
                    hash ^= tw2bi_hash.okiSeed[(int)getOnBoardPturn(nPos) * 14 + (int)getOnBoardKtype(nPos) - 1, (nPos >> 4) * 9 + (nPos & 0x0F)];
                    hash ^= tw2bi_hash.mochiSeed[(int)turn * 7 + (int)kNoNari(getOnBoardKtype(nPos)) - 1, data[((int)turn << 6) + hand + (int)kNoNari(getOnBoardKtype(nPos))]];

                    removeKoma(nPos);

                    // 持ち駒情報追加
                    data[((int)turn << 6) + hand + (int)kNoNari(getOnBoardKtype(nPos))]++;

                    data[nPos] |= 0xFFU; //駒情報を一時クリア
                } else {
                    //changeMoveableDir(nx, ny, -1);
                }


                uint mk = data[oPos];

                // hash更新
                hash ^= tw2bi_hash.okiSeed[(int)turn * 14 + (int)getOnBoardKtype(oPos) - 1, (oPos >> 4) * 9 + (oPos & 0x0F)];

                if (renewMoveable == true) chgMoveable((byte)(oPos), turn, -1);

                // 成り
                if (nari) {
                    // 歩情報更新
                    switch (getOnBoardKtype(oPos)) {
                        case ktype.Fuhyou:
                            data[((data[oPos] >> 16) & 0xFF)] |= (uint)(0xFF << (int)((data[oPos] >> 24) << 3)); //マップ情報クリア
                            for (int i = 0; i < 28; i++) {
                                if ((data[((int)turn << 6) + setNa + (i >> 2)] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                                    data[((int)turn << 6) + setNa + (i >> 2)] = (uint)(data[((int)turn << 6) + setNa + (i >> 2)] & ~(0xFF << ((i & 3) << 3)) | (uint)(nPos << ((i & 3) << 3)));
                                    mk = setOnBoardData(((int)turn << 6) + setNa + (i >> 2), i & 3, turn, (data[nPos] >> 8) & 0xFF, ktype.Tokin);
                                    ++data[((int)turn << 6) + setNaNum];
                                    break;
                                }
                            }
                            break;

                        case ktype.Kyousha:
                            data[((data[oPos] >> 16) & 0xFF)] |= (uint)(0xFF << (int)((data[oPos] >> 24) << 3)); //マップ情報クリア
                            for (int i = 0; i < 30; i++) {
                                if ((data[((int)turn << 6) + setNa + (i >> 2)] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                                    data[((int)turn << 6) + setNa + (i >> 2)] = (uint)(data[((int)turn << 6) + setNa + (i >> 2)] & ~(0xFF << ((i & 3) << 3)) | (uint)(nPos << ((i & 3) << 3)));
                                    mk = setOnBoardData(((int)turn << 6) + setNa + (i >> 2), i & 3, turn, (data[nPos] >> 8) & 0xFF, ktype.Narikyou);
                                    ++data[((int)turn << 6) + setNaNum];
                                    break;
                                }
                            }
                            break;
                        case ktype.Keima:
                            data[((data[oPos] >> 16) & 0xFF)] |= (uint)(0xFF << (int)((data[oPos] >> 24) << 3)); //マップ情報クリア
                            for (int i = 0; i < 30; i++) {
                                if ((data[((int)turn << 6) + setNa + (i >> 2)] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                                    data[((int)turn << 6) + setNa + (i >> 2)] = (uint)(data[((int)turn << 6) + setNa + (i >> 2)] & ~(0xFF << ((i & 3) << 3)) | (uint)(nPos << ((i & 3) << 3)));
                                    mk = setOnBoardData(((int)turn << 6) + setNa + (i >> 2), i & 3, turn, (data[nPos] >> 8) & 0xFF, ktype.Narikei);
                                    ++data[((int)turn << 6) + setNaNum];
                                    break;
                                }
                            }
                            break;
                        case ktype.Ginsyou:
                            data[((data[oPos] >> 16) & 0xFF)] |= (uint)(0xFF << (int)((data[oPos] >> 24) << 3)); //マップ情報クリア
                            for (int i = 0; i < 30; i++) {
                                if ((data[((int)turn << 6) + setNa + (i >> 2)] >> ((i & 3) << 3) & 0xFF) == 0xFF) {
                                    data[((int)turn << 6) + setNa + (i >> 2)] = (uint)(data[((int)turn << 6) + setNa + (i >> 2)] & ~(0xFF << ((i & 3) << 3)) | (uint)(nPos << ((i & 3) << 3)));
                                    mk = setOnBoardData(((int)turn << 6) + setNa + (i >> 2), i & 3, turn, (data[nPos] >> 8) & 0xFF, ktype.Narigin);
                                    ++data[((int)turn << 6) + setNaNum];
                                    break;
                                }
                            }
                            break;
                        default:
                            data[(data[oPos] >> 16) & 0xFF] = (uint)(data[(data[oPos] >> 16) & 0xFF] & ~(0xFF << ((int)(data[oPos] >> 24) << 3)) | (uint)(nPos << (int)((data[oPos] >> 24) << 3)));
                            mk = setOnBoardData((int)(data[oPos] >> 16 & 0xFF), (int)(data[oPos] >> 24), turn, (data[nPos] >> 8) & 0xFF, kDoNari(getOnBoardKtype(oPos)));
                            break;
                    }



                    // 不成or通常移動
                } else {
                    data[(data[oPos] >> 16) & 0xFF] = (uint)(data[(data[oPos] >> 16) & 0xFF] & ~(0xFF << ((int)(data[oPos] >> 24) << 3)) | (uint)(nPos << (int)((data[oPos] >> 24) << 3))); //Map情報更新
                    mk = setOnBoardData((int)(data[oPos] >> 16 & 0xFF), (int)(data[oPos] >> 24), turn, (data[nPos] >> 8) & 0xFF, getOnBoardKtype(oPos));
                }

                data[oPos] &= ~0xFFU; //移動元駒情報クリア

                if (renewMoveable == true) {
                    if (getOnBoardKtype(nPos) == ktype.None) {
                        changeMoveableDir(nPos, -1);
                    }
                    changeMoveableDir(oPos, 1);
                }
                data[nPos] = setOnBoardData((int)mk >> 16 & 0xFF, (int)(mk >> 24), turn, (data[nPos] >> 8) & 0xFF, (ktype)(mk & 0x0F));

                if (renewMoveable == true) chgMoveable(nPos, turn, 1);


                // hash更新
                hash ^= tw2bi_hash.okiSeed[(int)turn * 14 + (int)getOnBoardKtype(nPos) - 1, (nPos >> 4) * 9 + (nPos & 0x0F)];

            }

            return 0;
        }

        /// <summary>
        /// 指定した駒の移動が可能かチェック
        /// </summary>
        /// <param name="ox"></param>
        /// <param name="oy"></param>
        /// <param name="nx"></param>
        /// <param name="ny"></param>
        /// <param name="turn"></param>
        /// <param name="nari"></param>
        /// <returns>0:OK -1:NG</returns>
        public int chkMoveable(byte oPos, byte nPos, Pturn turn, bool nari) {
            if ((turn != Pturn.Sente) && (turn != Pturn.Gote)) return -1;
            if (oPos == nPos) return -2;
            if ((oPos > 0x98) || ((oPos & 0x0F) > 8)) return -3;
            if ((nPos > 0x88) || ((nPos & 0x0F) > 8)) return -4;

            // 駒打ち
            if (oPos > 0x90) {
                if (nari == true) return -7;
                // 置き場所に駒があるか
                if (getOnBoardKtype(nPos) > ktype.None) return -8;

                // 持ち駒を持っていない
                if (data[((int)turn << 6) + ban.hand + (oPos & 0x0F)] == 0) return -9;

                if ((((oPos & 0x0F) == (int)ktype.Fuhyou) || ((oPos & 0x0F) == (int)ktype.Kyousha)) && (pturn.psY(turn, (byte)(nPos & 0x0F)) == 8)) return -10;
                if (((oPos & 0x0F) == (int)ktype.Keima) && (pturn.psY(turn, (byte)(nPos & 0x0F)) > 6)) return -11;

                // 駒移動
            } else if (oPos < 0x90) {
                if (getOnBoardKtype(oPos) == ktype.None) return -12;
                if (getOnBoardPturn(oPos) != turn) return -13;
                // TODO : 個々の駒移動チェック
                if ((getOnBoardKtype(nPos) > ktype.None) && (getOnBoardPturn(nPos) == turn)) return -14;
                if ((nari == true) && (getOnBoardKtype(oPos) > ktype.Kakugyou)) return -15;
                if ((nari == true) && (pturn.psY(turn, (byte)(oPos & 0x0F)) < 6) && (pturn.psY(turn, (byte)(nPos & 0x0F)) < 6)) return -16;

                if ((nari == false) && ((getOnBoardKtype(oPos) == ktype.Fuhyou) || (getOnBoardKtype(oPos) == ktype.Kyousha)) && (pturn.psX(turn, (byte)(nPos & 0x0F)) == 8)) return -17;
                if ((nari == false) && (getOnBoardKtype(oPos) == ktype.Keima) && (pturn.psX(turn, (byte)(nPos & 0x0F)) > 6)) return -18;

            } else {
                return -19;
            }
            return 0;
        }

        // 盤上情報上の駒情報を取得
        public ktype getOnBoardKtype(byte pos) {
            return (ktype)(data[pos] & 0x0F);
        }

        public ktype getOnBoardKtype(int pos) {
            return (ktype)(data[pos] & 0x0F);
        }

        public Pturn getOnBoardPturn(int pos) {
            return (Pturn)((data[pos] & 0xF0) >> 4);
        }

        // 処理軽減のためチェック省略
        public static ktype kDoNari(ktype t) {
            return t + 8;
        }

        public ktype kNoNari(ktype t) {
            if (t > ktype.Ousyou) {
                return t - 8;
            } else {
                return t;
            }
        }

        public void chgMoveable(byte oPos, Pturn turn, int val) {
            //Console.WriteLine("chgMoveable "+ oPos.ToString("X"));
            switch (getOnBoardKtype(oPos)) {
                case ktype.Fuhyou:
                    addMoveableEachKoma(oPos, 0x01, turn, val);
                    break;

                case ktype.Kyousha:
                    for (int i = 1; addMoveableEachKoma(oPos, i, turn, val) < 1; i++) ;
                    break;

                case ktype.Keima:
                    addMoveableEachKoma(oPos, 0x10 + 0x02, turn, val);
                    addMoveableEachKoma(oPos, -0x10 + 0x02, turn, val);
                    break;

                case ktype.Ginsyou:
                    addMoveableEachKoma(oPos, 0x10 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, 0x00 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, -0x10 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, 0x10 - 0x01, turn, val);
                    addMoveableEachKoma(oPos, -0x10 - 0x01, turn, val);
                    break;

                case ktype.Hisya:
                    for (int i = 1; addMoveableEachKoma(oPos, i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, -i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, i << 4, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, -(i << 4), turn, val) < 1; i++) ;
                    break;

                case ktype.Kakugyou:
                    for (int i = 1; addMoveableEachKoma(oPos, (i << 4) + i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, (i << 4) - i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, -(i << 4) + i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, -(i << 4) - i, turn, val) < 1; i++) ;
                    break;

                case ktype.Kinsyou:
                case ktype.Tokin:
                case ktype.Narikyou:
                case ktype.Narikei:
                case ktype.Narigin:
                    addMoveableEachKoma(oPos, 0x10 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, 0x00 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, -0x10 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, -0x10 + 0x00, turn, val);
                    addMoveableEachKoma(oPos, 0x10 + 0x00, turn, val);
                    addMoveableEachKoma(oPos, 0x00 - 0x01, turn, val);
                    break;

                case ktype.Ousyou:
                    addMoveableEachKoma(oPos, 0x10 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, 0x00 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, -0x10 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, 0x10 + 0x00, turn, val);
                    addMoveableEachKoma(oPos, -0x10 + 0x00, turn, val);
                    addMoveableEachKoma(oPos, 0x10 - 0x01, turn, val);
                    addMoveableEachKoma(oPos, 0x00 - 0x01, turn, val);
                    addMoveableEachKoma(oPos, -0x10 - 0x01, turn, val);
                    break;

                case ktype.Ryuuou:
                    addMoveableEachKoma(oPos, 0x10 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, -0x10 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, 0x10 - 0x01, turn, val);
                    addMoveableEachKoma(oPos, -0x10 - 0x01, turn, val);
                    for (int i = 1; addMoveableEachKoma(oPos, i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, -i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, i << 4, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, -(i << 4), turn, val) < 1; i++) ;
                    break;

                case ktype.Ryuuma:
                    addMoveableEachKoma(oPos, 0x00 + 0x01, turn, val);
                    addMoveableEachKoma(oPos, 0x10 + 0x00, turn, val);
                    addMoveableEachKoma(oPos, -0x10 + 0x00, turn, val);
                    addMoveableEachKoma(oPos, 0x00 - 0x01, turn, val);
                    for (int i = 1; addMoveableEachKoma(oPos, (i << 4) + i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, (i << 4) - i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, -(i << 4) + i, turn, val) < 1; i++) ;
                    for (int i = 1; addMoveableEachKoma(oPos, -(i << 4) - i, turn, val) < 1; i++) ;
                    break;

                default:
                    break;
            }
        }

        int addMoveableEachKoma(uint oPos, int mPos, Pturn turn, int val) {
            byte nPos = (byte)pturn.mv(turn, (byte)oPos, mPos);
            if ((nPos > 0x88) || ((nPos & 0xF) > 0x08)) {
                //Console.WriteLine("[n]" + nPos.ToString("X") + " " + turn);
                return 1;
            }; // 移動できない

            //Console.WriteLine("[N]" + nPos.ToString("X") + " " + turn);
            data[nPos] += (uint)(val << (8 + (int)turn * 4));
            if (getOnBoardKtype(nPos) > ktype.None) {
                return 1; // 駒がある
            } else {
                return 0; // 駒がない
            }
        }

        /// <summary>
        /// ban情報のデバッグ表示
        /// </summary>
        /// <returns></returns>
        public string debugShow() {
            string str = "";
            for (int i = 0; i < 137; i++) {
                str += data[i].ToString("X8") + " ";
                if ((i & 15) == 15) str += Environment.NewLine;
            }
            str += Environment.NewLine;
            str += "[HASH]" + hash.ToString("X16");
            return str;
        }

        // ban情報表示
        public string banShow() {
            string str = "";
            for (int i = 0; i < 81; i++) {
                if (getOnBoardKtype(((8 - i % 9) << 4) + (i / 9)) > ktype.None) {
                    // 先手
                    if (getOnBoardPturn(((8 - i % 9) << 4) + (i / 9)) == Pturn.Sente) {
                        switch (getOnBoardKtype(((8 - i % 9) << 4) + (i / 9))) {
                            case ktype.Fuhyou:
                                str += "▲歩|";
                                break;
                            case ktype.Kyousha:
                                str += "▲香|";
                                break;
                            case ktype.Keima:
                                str += "▲桂|";
                                break;
                            case ktype.Ginsyou:
                                str += "▲銀|";
                                break;
                            case ktype.Hisya:
                                str += "▲飛|";
                                break;
                            case ktype.Kakugyou:
                                str += "▲角|";
                                break;
                            case ktype.Kinsyou:
                                str += "▲金|";
                                break;
                            case ktype.Ousyou:
                                str += "▲王|";
                                break;
                            case ktype.Tokin:
                                str += "▲と|";
                                break;
                            case ktype.Narikyou:
                                str += "▲杏|";
                                break;
                            case ktype.Narikei:
                                str += "▲圭|";
                                break;
                            case ktype.Narigin:
                                str += "▲全|";
                                break;
                            case ktype.Ryuuou:
                                str += "▲竜|";
                                break;
                            case ktype.Ryuuma:
                                str += "▲馬|";
                                break;
                            default:
                                str += "▲??|";
                                break;
                        }
                    } else if (getOnBoardPturn(((8 - i % 9) << 4) + (i / 9)) == Pturn.Gote) {
                        switch (getOnBoardKtype(((8 - i % 9) << 4) + (i / 9))) {
                            case ktype.Fuhyou:
                                str += "▽歩|";
                                break;
                            case ktype.Kyousha:
                                str += "▽香|";
                                break;
                            case ktype.Keima:
                                str += "▽桂|";
                                break;
                            case ktype.Ginsyou:
                                str += "▽銀|";
                                break;
                            case ktype.Hisya:
                                str += "▽飛|";
                                break;
                            case ktype.Kakugyou:
                                str += "▽角|";
                                break;
                            case ktype.Kinsyou:
                                str += "▽金|";
                                break;
                            case ktype.Ousyou:
                                str += "▽王|";
                                break;
                            case ktype.Tokin:
                                str += "▽と|";
                                break;
                            case ktype.Narikyou:
                                str += "▽杏|";
                                break;
                            case ktype.Narikei:
                                str += "▽圭|";
                                break;
                            case ktype.Narigin:
                                str += "▽全|";
                                break;
                            case ktype.Ryuuou:
                                str += "▽竜|";
                                break;
                            case ktype.Ryuuma:
                                str += "▽馬|";
                                break;
                            default:
                                str += "▽??|";
                                break;
                        }
                    } else {
                        switch (getOnBoardKtype(((8 - i % 9) << 4) + (i / 9))) {
                            case ktype.Fuhyou:
                                str += "??歩|";
                                break;
                            case ktype.Kyousha:
                                str += "??香|";
                                break;
                            case ktype.Keima:
                                str += "??桂|";
                                break;
                            case ktype.Ginsyou:
                                str += "??銀|";
                                break;
                            case ktype.Hisya:
                                str += "??飛|";
                                break;
                            case ktype.Kakugyou:
                                str += "??角|";
                                break;
                            case ktype.Kinsyou:
                                str += "??金|";
                                break;
                            case ktype.Ousyou:
                                str += "??王|";
                                break;
                            case ktype.Tokin:
                                str += "??と|";
                                break;
                            case ktype.Narikyou:
                                str += "??杏|";
                                break;
                            case ktype.Narikei:
                                str += "??圭|";
                                break;
                            case ktype.Narigin:
                                str += "??全|";
                                break;
                            case ktype.Ryuuou:
                                str += "??竜|";
                                break;
                            case ktype.Ryuuma:
                                str += "??馬|";
                                break;
                            default:
                                str += "????|";
                                break;
                        }
                    }
        
                } else {
                    str += " ___|";
                }
                if ((i + 1) % 9 == 0) {
                    str += "    ";
                    // 移動可能リスト
                    for (int j = 0; j < 9; j++) {
                        str += (data[((8 - j % 9) << 4) + (i / 9)] >> (8 + ((int)Pturn.Sente << 2)) & 0x0F) + "," + (data[((8 - j % 9) << 4) + (i / 9)] >> (8 + ((int)Pturn.Gote << 2)) & 0x0F) + "|";
                    
                    }
                
                    // 改行
                    str += Environment.NewLine;
                
                }
            }

            // 持ち駒情報
            str += "先手持駒/歩:" + data[hand + (int)ktype.Fuhyou] + "/香:" + data[hand + (int)ktype.Kyousha] + "/桂:" + data[hand + (int)ktype.Keima] + "/銀:" + data[hand + (int)ktype.Ginsyou] + "/飛:" + data[hand + (int)ktype.Hisya] + "/角:" + data[hand + (int)ktype.Kakugyou] + "/金:" + data[hand + (int)ktype.Kinsyou] + Environment.NewLine;
            str += "後手持駒/歩:" + data[GoOffset + hand + (int)ktype.Fuhyou] + "/香:" + data[GoOffset + hand + (int)ktype.Kyousha] + "/桂:" + data[GoOffset + hand + (int)ktype.Keima] + "/銀:" + data[GoOffset + hand + (int)ktype.Ginsyou] + "/飛:" + data[GoOffset + hand + (int)ktype.Hisya] + "/角:" + data[GoOffset + hand + (int)ktype.Kakugyou] + "/金:" + data[GoOffset + hand + (int)ktype.Kinsyou] + Environment.NewLine;

            str += Environment.NewLine;
            return str;
        }


    }
}
