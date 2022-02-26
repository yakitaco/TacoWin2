using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TacoWin2_BanInfo;

namespace TacoWin2_SMV {
    public class sMove : IComparable {
        public static List<sMove> sList = new List<sMove>();
        //static SHA1CryptoServiceProvider algorithm = new SHA1CryptoServiceProvider();
        static Random rnd = new Random();

        private ulong hash;
        // <SFEN>,<move1>/<value1>/<weight1>/<type1>,<move2>/<value2>/<weight2>/<type2>,...
        private string contents;

        public sMove(ulong _hash, string _contents) {
            hash = _hash;
            contents = _contents;
        }

        //ファイルから読み取り
        public static int load(string filePath) {
            if (System.IO.File.Exists(filePath)) {
                string[] tmpList = File.ReadAllLines(filePath);

                foreach (var tmp in tmpList) {
                    sList.Add(new sMove(Convert.ToUInt64(tmp.Substring(0, 16), 16), tmp.Substring(17)));
                }
                return tmpList.Length;
                //Console.WriteLine("[OK]Load " + filePath);

            } else {
                return -1;
                //Console.WriteLine("[NG]Load " + filePath);
            }

        }

        //ファイルへセーブ
        public static void save(string filePath) {
            File.WriteAllLines(filePath, sList.Select(str => String.Format("{0},{1}", str.hash.ToString("X16"), str.contents)));
        }

        public static void addDummyData() {
            byte[] m_Buff = new byte[32];
            byte[] m_Buff2 = new byte[0x02];
            rnd.NextBytes(m_Buff);
            rnd.NextBytes(m_Buff2);
            set(0, BytesToString(m_Buff) + " -", "+" + BytesToString(m_Buff2), 1, 1, 0);
        }

        public static string BytesToString(byte[] bs) {
            var str = BitConverter.ToString(bs);
            // "-"がいらないなら消しておく
            str = str.Replace("-", string.Empty);
            return str;
        }

        // 次の手リストを追加or更新
        public static void set(ulong hash, string position, string move, int value, int weight, int type) {
            sMove tmpSmv = new sMove(hash, position + "," + move + "/" + value + "/" + weight + "/" + type);

            int idx = sList.BinarySearch(tmpSmv);
            if (idx < 0) {
                /* 新規 */
                //Console.WriteLine("The object to search for ({0}) is not found. The next larger object is at index {1}.", 0, ~idx);

                sList.Insert(~idx, tmpSmv);

            } else {
                /* 更新 */
                string[] arr = sList[idx].contents.Split(',');

                // シノニムチェック(TODO)
                //bool ret = position.Equals(arr[0]);
                int i;
                for (i = 1; i < arr.Length; i++) {
                    string[] arr2 = arr[i].Split('/');
                    // 一致移動候補あり
                    if (move.Equals(arr2[0])) {
                        int _weight = int.Parse(arr2[2]) + weight;
                        int _value = int.Parse(arr2[1]) + value;// / _weight;
                        if (_value == 100) _value += 100;
                        if (_value > 500) _value = 500;
                        int _type = int.Parse(arr2[3]);
                        if (_type < type) _type = type;
                        arr[i] = arr2[0] + "/" + _value + "/" + _weight + "/" + _type;
                        sList[idx].contents = string.Join(",", arr);
                        break;
                    }
                }
                // 一致なし
                if (i == arr.Length) {
                    sList[idx].contents += "," + move + "/" + value + "/" + weight + "/" + type;
                }


                //Console.WriteLine("The object to search for ({0}) is at index {1}.", 0, idx);
            }
        }

        // 次の手を取得
        public static string get(ulong hash, Pturn turn) {

            int idx = sList.BinarySearch(new sMove(hash, ""));
            if (idx < 0) {
                /* 手がない */
                return null;
            } else {
                /* 手がある(候補から手を選択) */

                string[] arr = sList[idx].contents.Split(',');

                // シノニムチェック
                //bool ret = position.Equals(arr[0]);

                // 要改善
                int sum = 0;
                for (int i = 1; i < arr.Length; i++) {
                    string[] arr2 = arr[i].Split('/');
                    if (((arr2[0].Substring(0, 1) == "+") && (turn == Pturn.Sente)) ||
                        ((arr2[0].Substring(0, 1) == "-") && (turn == Pturn.Gote))) {
                        if (int.Parse(arr2[1]) > 0) sum += int.Parse(arr2[1]);
                    }
                }
                if (sum < 1) return null;

                int rVal = rnd.Next(0, sum);

                for (int i = 1; i < arr.Length; i++) {
                    string[] arr2 = arr[i].Split('/');
                    if (((arr2[0].Substring(0, 1) == "+") && (turn == Pturn.Gote)) ||
                        ((arr2[0].Substring(0, 1) == "-") && (turn == Pturn.Sente))) {
                        continue;
                    }
                    if (int.Parse(arr2[1]) < 1) continue;
                    if (rVal > int.Parse(arr2[1])) {
                        rVal -= int.Parse(arr2[1]);
                        continue;
                    }
                    return arr2[0];
                }


                Console.WriteLine("The object to search for ({0}) is at index {1}.", 0, idx);

                return null;
            }

        }

        // 次の手リストを全更新
        public static void updateAll(ulong hash, string position, List<string> str) {
            string contents = "";
            /* 手がある(テキストを返す) */
            for (int i = 0; i < str.Count; i++) {
                contents += "," + str[i];
            }
            sMove tmpSmv = new sMove(hash, position + contents);
            int idx = sList.BinarySearch(tmpSmv);
            if (idx < 0) {
                /* 手がない */
                sList.Insert(~idx, tmpSmv);
            } else {
                sList[idx] = tmpSmv;
            }
        }

        public static string getTxt(ulong hash) {
            int idx = sList.BinarySearch(new sMove(hash, ""));
            if (idx < 0) {
                /* 手がない */
                return null;
            } else {
                /* 手がある(テキストを返す) */
                return sList[idx].contents;
            }
        }

        public static void debugShow() {
            foreach (var s in sList) {
                Console.WriteLine("[{0}]{1}", s.hash, s.contents);
            }

        }

        //指定した文字列のSHA1を取得
        //public static ulong sha1(string str, int length) {
        //    byte[] data = Encoding.UTF8.GetBytes(str);
        //    byte[] bs = algorithm.ComputeHash(data);
        //
        //    // バイト型配列を16進数文字列に変換
        //    var result = new StringBuilder();
        //    for (int i = 0; i < length && i < bs.Length; i++) {
        //        result.Append(bs[i].ToString("X2"));
        //    }
        //    return Convert.ToUInt64(result.ToString(), 16);
        //}

        //自分自身がobjより小さいときはマイナスの数、大きいときはプラスの数、
        //同じときは0を返す
        public int CompareTo(object obj) {
            //nullより大きい
            if (obj == null) {
                return 1;
            }

            //違う型とは比較できない
            //if (this.GetType() != obj.GetType()) {
            //    throw new ArgumentException("別の型とは比較できません。", "obj");
            //}
            //このクラスが継承されることが無い（構造体など）ならば、次のようにできる
            //if (!(other is TestClass)) { }

            //Priceを比較する
            if (this.hash > ((sMove)obj).hash) {
                return 1;
            } else if (this.hash == ((sMove)obj).hash) {
                return 0;
            } else {
                return -1;
            }
            //return this.hash.CompareTo(((sMove)obj).hash);
            //または、次のようにもできる
            //return this.Price - ((Product)other).Price;
        }
    }
}
