using System;
using System.Collections.Generic;
using System.Text;

namespace TacoWin2_BanInfo {
    public struct kmsove {
        public int op;  // x + y * 10
        public int np;  // x + y * 10
        public int val; // 相対価値
        public bool nari;
        public Pturn turn;

        public void set(int _op, int _np, int _val, bool _nari, Pturn _turn) {
            op = _op;
            np = _np;
            val = _val;
            nari = _nari;
            turn = _turn;
        }

    }
}
