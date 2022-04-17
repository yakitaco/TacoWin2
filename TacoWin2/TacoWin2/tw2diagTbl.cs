using System;
using System.Collections.Generic;
using System.Text;
using TacoWin2_BanInfo;

namespace TacoWin2 {

    class diagTbl : IComparable<diagTbl> {
        public int val;
        public int tmpVal;
        public ban ban;
        public kmove[] kmv;

        public diagTbl(int _tmpVal, ban _ban) {
            tmpVal = _tmpVal;
            ban = _ban;
            val = 0;
            kmv = null;
        }

        public int CompareTo(diagTbl otbl) {
            return -val.CompareTo(otbl.val);
        }

    }
}
