namespace TacoWin2 {
    public enum ktype : byte {
        None = 0x00,     //なし
        Fuhyou = 0x01,   //歩兵
        Kyousha = 0x02,  //香車
        Keima = 0x03,    //桂馬
        Ginsyou = 0x04,  //銀将
        Hisya = 0x05,    //飛車
        Kakugyou = 0x06, //角行
        Kinsyou = 0x07,  //金将
        Ousyou = 0x08,   //王将
        Tokin = 0x09,    //と金(成歩兵)
        Narikyou = 0x0A, //成香
        Narikei = 0x0B,  //成桂
        Narigin = 0x0C,  //成銀
        Ryuuou = 0x0D,   //竜王
        Ryuuma = 0x0E,   //竜馬
    }

    public enum kVal : int {
        None = 0,        //なし
        Fuhyou = 100,   //歩兵
        Kyousha = 500,  //香車
        Keima = 600,    //桂馬
        Ginsyou = 800,  //銀将
        Hisya = 1500,    //飛車
        Kakugyou = 1200, //角行
        Kinsyou = 900,  //金将
        Ousyou = 99999,   //王将
        Tokin = 200,    //と金(成歩兵)
        Narikyou = 550, //成香
        Narikei = 650,  //成桂
        Narigin = 900,  //成銀
        Ryuuou = 1800,   //竜王
        Ryuuma = 1400,   //竜馬
    }

}
