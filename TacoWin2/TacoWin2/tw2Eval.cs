using System;

namespace TacoWin2
{
    /// <summary>
    /// 駒の価値や評価値を一元管理するクラス
    /// </summary>
    public static class KomaValues
    {
        /// <summary>
        /// 各駒の固定価値
        /// </summary>
        public static int[] kVal = {
        0,        //なし
        100,   //歩兵
        500,  //香車
        600,    //桂馬
        800,  //銀将
        1500,    //飛車
        1200, //角行
        900,  //金将
        99999,   //王将
        200,    //と金(成歩兵)
        550, //成香
        650,  //成桂
        900,  //成銀
        1800,   //竜王
        1400,   //竜馬
    };

        /// <summary>
        /// 各駒の固定価値
        /// </summary>
        public static int[,] kpVal = {
        { 0   , 0    },   //なし
        { 50  , 100  },   //歩兵
        { 120 , 170  },   //香車
        { 150 , 200  },   //桂馬
        { 200 , 300  },   //銀将
        { 350 , 500  },   //飛車
        { 300 , 400  },   //角行
        { 250 , 350  },   //金将
        {99999, 99999},   //王将
        {99999, 99999},   //と金(成歩兵)
        {99999, 99999},   //成香
        {99999, 99999},   //成桂
        {99999, 99999},   //成銀
        {99999, 99999},   //竜王
        {99999, 99999},   //竜馬
    };

        /// <summary>
        /// 持ち駒の評価 {1個め,2個目以降}
        /// </summary>
        public static int[,] mScore = {
        { 0    , 0      }, //なし
        { 150  , 10     }, //歩兵
        { 700  , 100    }, //香車
        { 800  , 150    }, //桂馬
        { 1000  , 500   }, //銀将
        { 2000  , 2500  }, //飛車
        { 1500  , 2000  }, //角行
        { 1200  , 1000  },  //金将
    };

        /// <summary>
        /// 駒の価値を外部ファイルから読み込みます。
        /// </summary>
        public static int LoadKomaValues(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return -1;

            try
            {
                string[] lines = System.IO.File.ReadAllLines(filePath);
                int index = 0;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    string[] parts = line.Split(',');

                    if (parts.Length >= 4 && index < 15)
                    {
                        kVal[index] = int.Parse(parts[1].Trim());
                        kpVal[index, 0] = int.Parse(parts[2].Trim());
                        kpVal[index, 1] = int.Parse(parts[3].Trim());

                        if (index < 8 && parts.Length >= 6)
                        {
                            mScore[index, 0] = int.Parse(parts[4].Trim());
                            mScore[index, 1] = int.Parse(parts[5].Trim());
                        }
                        index++;
                    }
                }
                return index;
            } catch (Exception)
            {
                return -1;
            }
        }
    }
}