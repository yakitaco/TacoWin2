using System;

namespace TacoWin2 {
    class tw2ai {

        public (int, int, int, int, bool) RandomeMove(pturn turn, tw2ban ban) {

            // 持ち駒がある
            // どこかに打つ

            // 駒を移動する

            ban.ForEachAll(turn, (int _ox, int _oy, int _nx, int _ny, pturn _turn, bool _nari) => {
                Console.Write("{0} ", _turn);
                }
            );

            return (0, 0, 0, 0, false);
        }




    }
}
