using System;
using System.IO;
using System.Text;

namespace TacoWin2 {
    class tw2_log {
        public static void save(string str, int teban) {
            DateTime dt = DateTime.Now;
            string result = dt.ToString("yyyyMMdd_HHmmss");
            if (!Directory.Exists("log")) {
                Directory.CreateDirectory("log");
            }
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); // memo: Shift-JISを扱うためのおまじない
            StreamWriter sw = new StreamWriter("log/" + result + "_" + teban + ".txt", false, Encoding.GetEncoding("shift_jis"));
            sw.Write(str);
            sw.Close();

        }

    }
}
