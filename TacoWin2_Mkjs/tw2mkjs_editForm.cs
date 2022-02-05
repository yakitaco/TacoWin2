﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TacoWin2_BanInfo;
using TacoWin2_sfenIO;
using TacoWin2_SMV;

namespace TacoWin2_Mkjs {

    public partial class tw2mkjs_editForm : Form {

        // 移動履歴
        List<ulong> history = new List<ulong>();
        ban ban = new ban();

        public tw2mkjs_editForm() {
            InitializeComponent();
        }

        private void tw2mkjs_editForm_Load(object sender, EventArgs e) {

        }

        private void LrdBtn_Click(object sender, EventArgs e) {
            sMove.load(FleIpt.Text);
            ban = new ban();

            if (JmpIpt.Text == "") {
                // 初期
                ban.startpos();
                string oki = "";
                string mochi = "";
                sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                JmpIpt.Text = oki + " " + mochi;
            } else {
                // 指定局面
                string[] arr = JmpIpt.Text.Split(' ');
                sfenIO.sfen2ban(ref ban, arr[0], arr[1]);
            }
            history.Clear();
            BackBtn.Enabled = false;
            showList(ban.hash);
        }

        void showList(ulong hash) {
            string str = sMove.getTxt(hash);
            NextLst.Items.Clear();
            if (str != null) {
                string[] arr2 = str.Split(',');
                for (int i = 1; i < arr2.Length; i++) {
                    NextLst.Items.Add(arr2[i]);
                }
            }
        }

        private void SavBtn_Click(object sender, EventArgs e) {

        }

        private void JumpBtn_Click(object sender, EventArgs e) {
            string[] arr = JmpIpt.Text.Split(' ');
            ban = new ban();
            sfenIO.sfen2ban(ref ban, arr[0], arr[1]);
            showList(ban.hash);
            BackBtn.Enabled = false;
        }

        private void BackBtn_Click(object sender, EventArgs e) {

        }

        // 次へ移動
        private void NextLst_DoubleClick(object sender, EventArgs e) {
            if ((NextLst.SelectedIndex > -1) && (NextLst.SelectedIndex < NextLst.Items.Count)) {
                Pturn turn = Pturn.Sente;
                string[] arr = NextLst.Items[NextLst.SelectedIndex].ToString().Split('/');
                int ox;
                int oy;
                int nx;
                int ny;
                bool nari;
                tw2usiIO.usi2pos(arr[0].Substring(1), out ox, out oy, out nx, out ny, out nari);
                if (arr[0][0] == '+') {
                    turn = Pturn.Sente;
                } else {
                    turn = Pturn.Gote;
                }
                ban.moveKoma(ox, oy, nx, ny, turn, nari, false, false);
                string oki = "";
                string mochi = "";
                sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                JmpIpt.Text = oki + " " + mochi;
                showList(ban.hash);
            }
        }
    }
}
