using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace TacoWin2_SMV {
    public class sMove {
        public static string[] sList = new string[500];
        public int sListNum = 0;
        static SHA1CryptoServiceProvider algorithm = new SHA1CryptoServiceProvider();

        //ファイルから読み取り
        public static void load() {
            //開くファイルを選択するダイアログを開く
            //kmove loadedData = null;
            //OpenFileDialog ofd = new OpenFileDialog();
            //ofd.Filter = "YTSG定跡テキスト(*.ytx)|*.ytx";
            //ofd.InitialDirectory = Directory.GetCurrentDirectory() + @"\Userdata";
            //if (ofd.ShowDialog() == DialogResult.OK) {
            //    //ファイルを読込
            //    Stream fileStream = ofd.OpenFile();
            //    BinaryFormatter binaryFormatter = new BinaryFormatter();
            //    loadedData = (kmove)binaryFormatter.Deserialize(fileStream);
            //    fileStream.Close();
            //}
        }

        public static void load(string filePath) {
            //Console.WriteLine("test.");
            //if (System.IO.File.Exists(filePath)) {
            //    sList = File.ReadAllLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line))
            //    .Select(line => line.Split(','), sList[0,0]= line[0]);
            //
            //    sList.ToArray().ForEach(line => {
            //        Console.WriteLine(string.Join(" ", line));
            //    });
            //} else {
            //    Console.WriteLine("error.");
            //}
        }

        //ファイルへセーブ
        public bool save() {
            //保存先を指定するダイアログを開く
            //System.IO.Directory.CreateDirectory(@"Userdata");
            //SaveFileDialog sfd = new SaveFileDialog();
            //sfd.Filter = "YTSG定跡データ(*.ytj)|*.ytj";
            //sfd.InitialDirectory = Directory.GetCurrentDirectory() + @"\Userdata";
            //sfd.FileName = "dat" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "_" + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second;
            //if (sfd.ShowDialog() == DialogResult.OK) {
            //    //指定したパスにファイルを保存する
            //    Stream fileStream = sfd.OpenFile();
            //    BinaryFormatter bF = new BinaryFormatter();
            //    bF.Serialize(fileStream, this);
            //    fileStream.Close();
            //}
            return true;
        }

        public bool save(string filePath) {
            //File.WriteAllLines(filePath, sList.toArray().Select(val => string.Join(",", val)));

            //Stream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            //BinaryFormatter binaryFormatter = new BinaryFormatter();
            //binaryFormatter.Serialize(fileStream, this);
            //fileStream.Close();
            return true;
        }

        public static string addList(string position, string move, int value, int weight, int type) {
            byte[] data = Encoding.UTF8.GetBytes(position);

            // SHA1ハッシュアルゴリズム生成
            //var algorithm = new SHA1CryptoServiceProvider();
            byte[] bs = algorithm.ComputeHash(data);

            // バイト型配列を16進数文字列に変換
            var result = new StringBuilder();
            foreach (byte b in bs) {
                result.Append(b.ToString("X2"));
            }

            string str = result.ToString() + "," + position + "," + move + "," + value + "," + weight + "/" + type;

            return str;
        }

        public static string getList(string position) {
            // SHA1ハッシュアルゴリズム生成
            //var algorithm = new SHA1CryptoServiceProvider();
            byte[] data = Encoding.UTF8.GetBytes(position);
            byte[] bs = algorithm.ComputeHash(data);

            int myIndex = Array.BinarySearch(sList, 1);
            if (myIndex < 0) {
                Console.WriteLine("The object to search for ({0}) is not found. The next larger object is at index {1}.", 1, ~myIndex);
            } else {
                Console.WriteLine("The object to search for ({0}) is at index {1}.", 1, myIndex);
            }

            return null;
        }



    }
}
