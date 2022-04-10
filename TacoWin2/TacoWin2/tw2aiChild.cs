using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TacoWin2 {
    class tw2aiChild {
        Process p;

        public List<kmove> mList = new List<kmove>();
        StreamWriter sw = null;

        public int load(string fileName) {
            if (System.IO.File.Exists(fileName)) {


                p = new Process();
                p.StartInfo.FileName = @fileName;
                p.StartInfo.CreateNoWindow = false;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;

                // リダイレクトした標準出力の内容を受信するイベントハンドラを設定する
                p.OutputDataReceived += (sender, e) => {
                    if ((e?.Data?.Length > 11) && (e.Data.Substring(0, 11) == "info string")) {
                        string[] arr = e.Data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        byte oPos;
                        byte nPos;
                        bool nari;
                        tw2usiIO.usi2pos(arr[2], out oPos, out nPos, out nari);
                        kmove kmv = new kmove();
                        kmv.set(oPos, nPos, (int)(float.Parse(arr[4]) * 10000), 0, nari, TacoWin2_BanInfo.Pturn.Sente);
                        mList.Add(kmv);
                    }

                    // 子プロセスの標準出力から受信した内容を自プロセスの標準出力に書き込む
                    //Console.WriteLine($"<stdout> {(e.Data ?? "(end of stream)")}");
                };

                p.Start();
                p.BeginOutputReadLine();
                sw = p.StandardInput;
                return 0;
            } else {
                return -1;
            }
        }

        public void input(string str) {
            if (sw != null) {
                sw.WriteLine(str);
            }
        }

        public void clear() {
            mList.Clear();
        }
    }
}
