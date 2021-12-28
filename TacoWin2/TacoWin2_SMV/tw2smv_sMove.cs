using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using TacoWin2_BanInfo;

namespace TacoWin2_SMV {
    public class sMove : IComparable {
        public static List<sMove> sList = new List<sMove>();
        static SHA1CryptoServiceProvider algorithm = new SHA1CryptoServiceProvider();

        private string hash;
        private string contents;

        public sMove(string _hash, string _contents) {
            hash = _hash;
            contents = _contents;
        }

        // 次の手リストを追加or更新
        public void set(string position, string move, int value, int weight, int type) {
            int idx = sList.BinarySearch(new sMove(sha1(position, 8), ""));
            if (idx < 0) {
                /* 新規 */
                Console.WriteLine("The object to search for ({0}) is not found. The next larger object is at index {1}.", 0, ~idx);
            } else {
                /* 更新 */
                string[] arr = sList[idx].contents.Split(',');


                Console.WriteLine("The object to search for ({0}) is at index {1}.", 0, idx);
            }
        }

        // 次の手を取得
        public string get(string position, Pturn turn) {

            int idx = sList.BinarySearch(new sMove(sha1(position, 8), ""));
            if (idx < 0) {
                /* 手がない */
                return null;
            } else {
                /* 手がある(候補から手を選択) */

                string[] arr = sList[idx].contents.Split(',');

                // シノニムチェック
                bool ret = position.Equals(arr[0]);


                Console.WriteLine("The object to search for ({0}) is at index {1}.", 0, idx);
            }
            return null;
        }

        //指定した文字列のSHA1を取得
        public string sha1(string str, int length) {
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] bs = algorithm.ComputeHash(data);

            // バイト型配列を16進数文字列に変換
            var result = new StringBuilder();
            for (int i = 0; i < length && i < bs.Length; i++) {
                result.Append(bs[i].ToString("X2"));
            }
            return result.ToString();
        }

        //自分自身がobjより小さいときはマイナスの数、大きいときはプラスの数、
        //同じときは0を返す
        public int CompareTo(object obj) {
            //nullより大きい
            if (obj == null) {
                return 1;
            }

            //違う型とは比較できない
            if (this.GetType() != obj.GetType()) {
                throw new ArgumentException("別の型とは比較できません。", "obj");
            }
            //このクラスが継承されることが無い（構造体など）ならば、次のようにできる
            //if (!(other is TestClass)) { }

            //Priceを比較する
            return this.hash.CompareTo(((sMove)obj).hash);
            //または、次のようにもできる
            //return this.Price - ((Product)other).Price;
        }
    }
}
