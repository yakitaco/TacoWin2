using System;
using TacoWin2_BanInfo;

namespace TacoWin2 {
    public struct kmove {
        public int op;  // x + y * 10
        public int np;  // x + y * 10
        public int val; // 自分の相対価値
        public int aval; // 相手の相対価値
        public bool nari;
        public Pturn turn;

        public void set(int _op, int _np, int _val, int _aval, bool _nari, Pturn _turn) {
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
