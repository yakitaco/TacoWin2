using System;
using TacoWin2_BanInfo;

namespace TacoWin2 {
    public struct kmove {
        public byte op;  // x << 4 + y (x=9:持ち駒)
        public byte np;  // x << 4 + y 
        public int val; // 自分の相対価値
        public int aval; // 相手の相対価値
        public bool nari;
        public Pturn turn;

        /// <summary>
        /// kmoveリストに移動候補を追加 (_op/_np x <<4 + y)
        /// </summary>
        /// <param name="_op"></param>
        /// <param name="_np"></param>
        /// <param name="_val"></param>
        /// <param name="_aval"></param>
        /// <param name="_nari"></param>
        /// <param name="_turn"></param>
        public void set(byte _op, byte _np, int _val, int _aval, bool _nari, Pturn _turn) {
            op = _op;
            np = _np;
            val = _val;
            aval = _aval;
            nari = _nari;
            turn = _turn;
        }

        public static implicit operator kmove(int v) {
            throw new NotImplementedException();
        }


    }
}
