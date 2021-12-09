using System;

namespace TacoWin2 {
    public struct kmove {
        public int op; // x + y * 10
        public int np; // x + y * 10
        public bool nari;
        public Pturn turn;

        public void set(int _op, int _np, bool _nari, Pturn _turn) {
            op = _op;
            np = _np;
            nari = _nari;
            turn = _turn;
        }

        public static implicit operator kmove(int v) {
            throw new NotImplementedException();
        }
    }
}
