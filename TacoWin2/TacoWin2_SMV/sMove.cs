using System;

namespace TacoWin2_SMV {
    public class sMove {
        public static string[,] sList = new string[500,500];
        public int sListNum = 0;

        //ファイルから読み取り
        public static void load() {
            //開くファイルを選択するダイアログを開く
            kmove loadedData = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "YTSG定跡テキスト(*.ytx)|*.ytx";
            ofd.InitialDirectory = Directory.GetCurrentDirectory() + @"\Userdata";
            if (ofd.ShowDialog() == DialogResult.OK) {
                //ファイルを読込
                Stream fileStream = ofd.OpenFile();
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                loadedData = (kmove)binaryFormatter.Deserialize(fileStream);
                fileStream.Close();
            }
        }

        public static void load(string filePath) {
            //開くファイルを選択するダイアログを開く
            kmove loadedData = null;
            if (System.IO.File.Exists(filePath)) {
                //ファイルを読込
                Stream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                loadedData = (kmove)binaryFormatter.Deserialize(fileStream);
                fileStream.Close();
            } else {
                //MessageBox.Show("'" + filePath + "'は存在しません。");
            }
        }

        //ファイルへセーブ
        public bool save() {
            //保存先を指定するダイアログを開く
            System.IO.Directory.CreateDirectory(@"Userdata");
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "YTSG定跡データ(*.ytj)|*.ytj";
            sfd.InitialDirectory = Directory.GetCurrentDirectory() + @"\Userdata";
            sfd.FileName = "dat" + DateTime.Now.Year + DateTime.Now.Month + DateTime.Now.Day + "_" + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second;
            if (sfd.ShowDialog() == DialogResult.OK) {
                //指定したパスにファイルを保存する
                Stream fileStream = sfd.OpenFile();
                BinaryFormatter bF = new BinaryFormatter();
                bF.Serialize(fileStream, this);
                fileStream.Close();
            }
            return true;
        }

        public bool save(string filePath) {
            Stream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, this);
            fileStream.Close();
            return true;
        }





    }
}
