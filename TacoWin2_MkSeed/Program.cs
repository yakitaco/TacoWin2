using System;
using System.Text;

namespace TacoWin2_MkSeed {
    class Program {
        static void Main(string[] args) {

            Random rnd = new Random();
            Byte[] b = new Byte[8];
            ulong ulongRand;

            StringBuilder sb = new StringBuilder("        public static ulong[,] okiSeed = new ulong[28, 81] {\n");
            for (int m = 0; m < 28; m++) {
                sb.Append("    {");

                for (int k = 0; k < 9; k++) {

                    sb.Append("\n        ");
                    for (int j = 0; j < 9; j++) {
                        rnd.NextBytes(b);
                        //Console.WriteLine("The Random bytes are: ");
                        //for (int i = 0; i <= b.GetUpperBound(0); i++)
                        //    Console.WriteLine("{0}: {1}", i, b[i]);

                        ulongRand = (ulong)BitConverter.ToInt64(b, 0);
                        sb.Append("0x").Append(ulongRand.ToString("X16")).Append(", ");
                        //Console.WriteLine("{0}", ulongRand.ToString("X16"));
                    }

                }

                sb.Append("\n    },\n");
            }

            sb.Append("\n };\n");
            Console.WriteLine(sb);

            sb = new StringBuilder("        public static ulong[,] mochiSeed = new ulong[14, 18] {\n");
            for (int m = 0; m < 14; m++) {
                sb.Append("    {");

                sb.Append("\n        ");
                for (int j = 0; j < 18; j++) {
                    rnd.NextBytes(b);
                    //Console.WriteLine("The Random bytes are: ");
                    //for (int i = 0; i <= b.GetUpperBound(0); i++)
                    //    Console.WriteLine("{0}: {1}", i, b[i]);
                    if ((m == 0) || (m == 7) || (j < 4)) {
                        ulongRand = (ulong)BitConverter.ToInt64(b, 0);

                        sb.Append("0x").Append(ulongRand.ToString("X16")).Append(", ");
                    } else {
                        ulongRand = 0;

                        sb.Append("0x0").Append(", ");
                    }
                    //sb.Append("0x").Append(ulongRand.ToString("X16")).Append(", ");
                    //Console.WriteLine("{0}", ulongRand.ToString("X16"));
                }

                sb.Append("\n    },\n");
            }

            sb.Append("\n };\n");
            Console.WriteLine(sb);
        }
    }
}
