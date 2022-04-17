using System.Collections.Generic;
using System.IO;
using TacoWin2_BanInfo;

namespace TacoWin2 {

    public enum OPLIST : int {
        None = 0,
        IBISYA = 100,            //居飛車
        IBISYA_YAGURA = 110,     //居飛車矢倉
        IBISYA_GENSHI = 111,     //原始棒銀
        IBISYA_KAKUGAWARI = 120, //角換わり
        IBISYA_KOSIKAKE = 121, //腰掛け銀
        IBISYA_HAYAKURI = 122, //早繰り銀
        IBISYA_YOKOFUDORI = 130, //横歩取り
        IBISYA_TAIFURI = 150,   //対振り飛車

        FURIBISYA = 200,   //振り飛車

        NAKBISYA = 210,//中飛車
        SIKENBISYA = 220,//四間飛車
        SANKENBISYA = 230,//三間飛車
        MUKAIBISYA = 240,//向かい飛車
        MIGISIKENBISYA = 250,//右四間飛車
        SODEBISYA = 260,//袖飛車
        HINERIHISYA = 270, //ひねり飛車

        KISYU = 300,          //奇襲・不明
        ONIGOROSHI = 310,     //鬼殺し
        SHINONIGOROSHI = 311, //新鬼殺し
        PACKMAN = 320,        //パックマン
        SUZICHIGAIKAKU = 330, //筋違い角
        HAYAISHIDA = 340, //筋違い角

        MIGICHIKATETU = 350,//右地下鉄
        HIDARICHIKATETU = 360,//左地下鉄

    }

    class tw2stval {
        OPLIST type;
        int move;
        public int[,,] val;

        static List<tw2stval> mV = new List<tw2stval>();
        static int senTeNum = 0;
        static int goTeNum = 0;
        static int stage = 0;  //0:序盤(型作成) 1:中盤(攻防開始) 2:終盤(詰め重視) 

        public static int loadFile(string dirPath) {
            if (System.IO.Directory.Exists(dirPath)) {
                string[] files = Directory.GetFiles(@dirPath, "*.mvl");
                //Console.WriteLine("sss" + files.Length);
                foreach (string cFile in files) {
                    //Form1.Form1Instance.addMsg(cFile);
                    int count = 0;
                    tw2stval tmp = new tw2stval(0, 0);
                    tmp.val = new int[14, 9, 9];
                    foreach (string line in System.IO.File.ReadLines(@cFile)) {
                        if (line[0] == '#') continue; // コメント行はスキップ
                        if (count == 0) tmp.type = (OPLIST)int.Parse(line);
                        if (count == 1) tmp.move = int.Parse(line);
                        if (count > 1) {
                            string[] arr = line.Split(',');
                            for (int i = 0; i < 9; i++) {
                                tmp.val[(count - 2) / 9, (count - 2) % 9, i] = int.Parse(arr[i]);
                            }
                        }
                        //Console.WriteLine("ddd" + ((count - 2) / 9) + ":" + ((count - 2) % 9) + ":" + line);
                        count++;
                    }
                    mV.Add(tmp);
                }
                return files.Length;
            } else {
                return -1;
            }

        }

        tw2stval(OPLIST _type, int _move) {
            type = _type;
            move = _move;
        }

        public static void reset() {
            setType(OPLIST.None, (int)Pturn.Sente, 0);
            setType(OPLIST.None, (int)Pturn.Gote, 0);
            stage = 0;
        }

        public static void setType(OPLIST _type, int turn, int count) {
            int tmpCount = -1;
            int cnt = 0;
            for (cnt = 0; cnt < mV.Count; cnt++) {
                if ((mV[cnt].type == _type) && (mV[cnt].move <= count) && (tmpCount < mV[cnt].move)) {
                    if (turn == 0) {
                        senTeNum = cnt;
                    } else {
                        goTeNum = cnt;
                    }
                    tmpCount = mV[cnt].move;
                }


            }
            if (tmpCount == -1) {
                if (turn == 0) {
                    senTeNum = 0;
                } else {
                    goTeNum = 0;
                }
            }
        }


        // 局面のチェック★暫定版
        public static void tmpChk(ban ban) {
            unsafe {
                //序盤のみ
                if (stage == 0) {
                    //先手
                    byte hisya = (byte)(ban.data[((int)Pturn.Sente << 6) + ban.setHi] & 0xFF);
                    if (hisya != 0xFF) {
                        switch (hisya >> 4) {
                            case 0:    // 1筋 (右地下鉄？)
                                setType(OPLIST.MIGICHIKATETU, (int)Pturn.Sente, 0);
                                break;
                            case 1:    // 2筋 (居飛車？)
                                if (((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (1 >> 2)] >> ((1 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (1 >> 2)] >> ((1 & 3) << 3) & 0x0F) < 0x06)) {
                                    if (mV[senTeNum].type != OPLIST.IBISYA_KAKUGAWARI) {
                                        setType(OPLIST.IBISYA, (int)Pturn.Sente, 0);
                                    }
                                    if ((ban.data[((int)Pturn.Sente << 6) + ban.hand + (int)ktype.Kakugyou] > 0)&& (ban.data[((int)Pturn.Gote << 6) + ban.hand + (int)ktype.Kakugyou] > 0)) {
                                        setType(OPLIST.IBISYA_KAKUGAWARI, (int)Pturn.Sente, 0);
                                    }
                                }
                                break;
                            case 2:    // 3筋 (袖飛車？)
                                if (((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (2 >> 2)] >> ((2 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (2 >> 2)] >> ((2 & 3) << 3) & 0x0F) < 0x06)) {
                                    setType(OPLIST.SODEBISYA, (int)Pturn.Sente, 0);
                                }
                                break;
                            case 3:    // 4筋 (右四間飛車？)
                                if (((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (3 >> 2)] >> ((3 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (3 >> 2)] >> ((3 & 3) << 3) & 0x0F) < 0x06)) {
                                    setType(OPLIST.MIGISIKENBISYA, (int)Pturn.Sente, 0);
                                }
                                break;
                            case 4:    // 5筋 (中飛車？)
                                if (((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (4 >> 2)] >> ((4 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (4 >> 2)] >> ((4 & 3) << 3) & 0x0F) < 0x06)) {
                                    setType(OPLIST.NAKBISYA, (int)Pturn.Sente, 0);
                                }
                                break;
                            case 5:    // 6筋 (四間飛車？)
                                if (((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (5 >> 2)] >> ((5 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (5 >> 2)] >> ((5 & 3) << 3) & 0x0F) < 0x06)) {
                                    setType(OPLIST.SIKENBISYA, (int)Pturn.Sente, 0);
                                }
                                break;
                            case 6:    // 7筋 (三間飛車？)
                                if (((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (6 >> 2)] >> ((6 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (6 >> 2)] >> ((6 & 3) << 3) & 0x0F) < 0x06)) {
                                    setType(OPLIST.SANKENBISYA, (int)Pturn.Sente, 0);
                                }
                                if (((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (6 >> 2)] >> ((6 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (6 >> 2)] >> ((6 & 3) << 3) & 0x0F) < 0x06)) {
                                    setType(OPLIST.SANKENBISYA, (int)Pturn.Sente, 0);
                                }
                                break;
                            case 7:    // 8筋 (向かい飛車？)
                                if (((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (7 >> 2)] >> ((7 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Sente << 6) + ban.setFu + (7 >> 2)] >> ((7 & 3) << 3) & 0x0F) < 0x06)) {
                                    setType(OPLIST.MUKAIBISYA, (int)Pturn.Sente, 0);
                                }
                                break;
                            case 8:    // 9筋 (左地下鉄？)
                                setType(OPLIST.HIDARICHIKATETU, (int)Pturn.Sente, 0);
                                break;
                        }
                    }
                
                    hisya = (byte)(ban.data[((int)Pturn.Gote << 6) + ban.setHi] & 0xFF);
                    if (hisya != 0xFF) {
                        switch (hisya >> 4) {
                            case 8:    // 1筋 (右地下鉄？)
                                setType(OPLIST.MIGICHIKATETU, (int)Pturn.Gote, 0);
                                break;
                            case 7:    // 2筋 (居飛車？)
                                if (((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (7 >> 2)] >> ((7 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (7 >> 2)] >> ((7 & 3) << 3) & 0x0F) > 0x02)) {
                                    if (mV[goTeNum].type != OPLIST.IBISYA_KAKUGAWARI) {
                                        setType(OPLIST.IBISYA, (int)Pturn.Gote, 0);
                                    }
                                }
                                break;
                            case 6:    // 3筋 (袖飛車？)
                                if (((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (6 >> 2)] >> ((6 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (6 >> 2)] >> ((6 & 3) << 3) & 0x0F) > 0x02)) {
                                    setType(OPLIST.SODEBISYA, (int)Pturn.Gote, 0);
                                }
                                break;
                            case 5:    // 4筋 (右四間飛車？)
                                if (((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (5 >> 2)] >> ((5 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (5 >> 2)] >> ((5 & 3) << 3) & 0x0F) > 0x02)) {
                                    setType(OPLIST.MIGISIKENBISYA, (int)Pturn.Gote, 0);
                                }
                                break;
                            case 4:    // 5筋 (中飛車？)
                                if (((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (4 >> 2)] >> ((4 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (4 >> 2)] >> ((4 & 3) << 3) & 0x0F) > 0x02)) {
                                    setType(OPLIST.NAKBISYA, (int)Pturn.Gote, 0);
                                }
                                break;
                            case 3:    // 6筋 (四間飛車？)
                                if (((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (3 >> 2)] >> ((3 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (3 >> 2)] >> ((3 & 3) << 3) & 0x0F) > 0x02)) {
                                    setType(OPLIST.SIKENBISYA, (int)Pturn.Gote, 0);
                                }
                                break;
                            case 2:    // 7筋 (三間飛車？)
                                if (((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (2 >> 2)] >> ((2 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (2 >> 2)] >> ((2 & 3) << 3) & 0x0F) > 0x02)) {
                                    setType(OPLIST.SANKENBISYA, (int)Pturn.Gote, 0);
                                }
                                break;
                            case 1:    // 8筋 (向かい飛車？)
                                if (((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (1 >> 2)] >> ((1 & 3) << 3) & 0xFF) == 0xFF) ||
                                    ((ban.data[((int)Pturn.Gote << 6) + ban.setFu + (1 >> 2)] >> ((1 & 3) << 3) & 0x0F) > 0x02)) {
                                    setType(OPLIST.MUKAIBISYA, (int)Pturn.Gote, 0);
                                }
                                break;
                            case 0:    // 9筋 (左地下鉄？)
                                setType(OPLIST.HIDARICHIKATETU, (int)Pturn.Gote, 0);
                                break;
                        }
                    }

                    if (mV[senTeNum].type == OPLIST.IBISYA) {
                        if (mV[goTeNum].type > OPLIST.FURIBISYA) {
                            setType(OPLIST.IBISYA_TAIFURI, (int)Pturn.Sente, 0);
                        }
                    }

                    if (mV[goTeNum].type == OPLIST.IBISYA) {
                        if (mV[senTeNum].type > OPLIST.FURIBISYA) {
                            setType(OPLIST.IBISYA_TAIFURI, (int)Pturn.Gote, 0);
                        }
                    }

                    DebugForm.instance.addMsg("SENKEI[" + senTeNum + ":" + mV[senTeNum].type + "]-[" + goTeNum + ":" + mV[goTeNum].type + "]");
                }
            }

        }

        //public static void countUp(int count) {
        //    int tmpCount = 0;
        //    for (int cnt = 0; cnt < mV.Count; cnt++) {
        //        if ((mV[cnt].type == _type) && (mV[cnt].move <= count) && (tmpCount < mV[cnt].move)) {
        //            if (turn == 0) {
        //                senTeNum = cnt;
        //            } else {
        //                goTeNum = cnt;
        //            }
        //            tmpCount = mV[cnt].move;
        //        }
        //
        //
        //    }
        //
        //}

        // 指定評価値テーブルを取得
        public int[,,] getTbl() {
            return mV[0].val;
        }

        public static void setStage(int _stage) {
            if (stage < _stage) {
                stage = _stage;
            }
        }

        // 指定評価値を取得
        /// <summary>
        /// 指定評価値を取得 ()
        /// </summary>
        /// <param name="type">駒種</param>
        /// <param name="oPos">移動元</param>
        /// <param name="nPos">移動先</param>
        /// <param name="turn">手番</param>
        /// <returns>指定評価値</returns>
        public static int get(ktype type, byte oPos, byte nPos, Pturn turn) {
            if (stage == 0) {
                if (turn == Pturn.Sente) {
                    //Console.WriteLine("VAL[=" + mV[senTeNum].val[(int)type - 1, ny, 8 - nx] +"-"+ mV[senTeNum].val[(int)type - 1, oy, 8 - ox] +  "/" + nx + "," + ny + "/" + ox + "," + oy + "/" + (int)type);
                    return mV[senTeNum].val[(int)type - 1, nPos & 0x0F, 8 - (nPos >> 4)] - mV[senTeNum].val[(int)type - 1, oPos & 0x0F, 8 - (oPos >> 4)];
                } else {
                    return mV[goTeNum].val[(int)type - 1, 8 - (nPos & 0x0F), nPos >> 4] - mV[goTeNum].val[(int)type - 1, 8 - (oPos & 0x0F), oPos >> 4];
                }
            } else {
                return 0;
            }

        }


    }
}

