namespace TacoWin2 {
    public struct kmove {
        int op;
        int np;
        bool nari;
        Pturn turn;

        public void set(int _op, int _np, bool _nari, Pturn _turn) {
            op = _op;
            np = _np;
            nari = _nari;
            turn = _turn;
        }

    }
}
