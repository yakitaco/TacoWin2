using System;
using System.Threading;

namespace TacoWin2 {
    class tw2Main {
        static void Main(string[] args) {

            tw2ban w;
            w.startpos();
            Console.WriteLine(w.debugShow()); 

            while (true) {
                string str = Console.ReadLine();

                // usi 起動
                if ((str.Length == 3) && (str.Substring(0, 3) == "usi")) {
                    Console.WriteLine("id name TACO WINNER 2.0");
                    Console.WriteLine("id authoer YAKITACO");
                    Console.WriteLine("option name BookFile type string default default.ytj");
                    Console.WriteLine("option name UseBook type check default true");
                    Console.WriteLine("usiok");

                    // isready 対局開始前
                } else if ((str.Length == 7) && (str.Substring(0, 7) == "isready")) {
                    Thread.Sleep(1000);
                    Console.WriteLine("readyok");

                    //usinewgame 新規
                } else if ((str.Length == 10) && (str.Substring(0, 10) == "usinewgame")) {
                    // 何もしない

                    // position 盤面情報
                } else if ((str.Length > 7) && (str.Substring(0, 8) == "position")) {
                    string[] arr = str.Split(' ');



                } else {

                }


                //Form1.Form1Instance.addMsg("[RECV]" + str);
            }


        }
    }
}
