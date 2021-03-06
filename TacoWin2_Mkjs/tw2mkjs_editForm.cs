using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            ban = new ban();
            // 初期
            ban.startpos();
            string oki = "";
            string mochi = "";
            sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
            JmpIpt.Text = oki + " " + mochi;
            DebugTxt.Text = ban.banShow();
            showList(ban.hash);
            history.Clear();
            history.Add(ban.hash);
            BackBtn.Enabled = false;
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
            history.Add(ban.hash);
            DebugTxt.Text = ban.banShow();
            BackBtn.Enabled = false;
            showList(ban.hash);
        }

        void showList(ulong hash) {
            label8.Text = "[hash] " + hash.ToString("X16");
            string str = sMove.getTxt(hash);
            NextLst.Items.Clear();
            if (str != null) {
                Debug.WriteLine(str);
                string[] arr2 = str.Split(',');
                for (int i = 1; i < arr2.Length; i++) {
                    NextLst.Items.Add(arr2[i]);
                }
            }
        }



        private void SavBtn_Click(object sender, EventArgs e) {
            sMove.save(FleIpt.Text + ".bak");
            MessageBox.Show("done");
        }

        private void JumpBtn_Click(object sender, EventArgs e) {
            string[] arr = JmpIpt.Text.Split(' ');
            ban = new ban();
            sfenIO.sfen2ban(ref ban, arr[0], arr[1]);
            showList(ban.hash);
            history.Add(ban.hash);
            DebugTxt.Text = ban.banShow();
            BackBtn.Enabled = true;
        }

        private void BackBtn_Click(object sender, EventArgs e) {
            ban = new ban();

            history.RemoveAt(history.Count - 1);
            showList(history[history.Count - 1]);
            string str = sMove.getTxt(history[history.Count - 1]);
            if (str != null) {
                string[] arr2 = str.Split(',');
                JmpIpt.Text = arr2[0];
                string[] arr = arr2[0].Split(' ');
                sfenIO.sfen2ban(ref ban, arr[0], arr[1]);

                DebugTxt.Text = ban.banShow();
            }
            if (history.Count == 1) {
                BackBtn.Enabled = false;
            }
        }

        // 次へ移動
        private void NextLst_DoubleClick(object sender, EventArgs e) {
            if ((NextLst.SelectedIndex > -1) && (NextLst.SelectedIndex < NextLst.Items.Count)) {
                Pturn turn = Pturn.Sente;
                string[] arr = NextLst.Items[NextLst.SelectedIndex].ToString().Split('/');
                byte oPos;
                byte nPos;
                bool nari;
                tw2usiIO.usi2pos(arr[0].Substring(1), out oPos, out nPos, out nari);
                if (arr[0][0] == '+') {
                    turn = Pturn.Sente;
                } else {
                    turn = Pturn.Gote;
                }
                ban.moveKoma(oPos, nPos, turn, nari, false);
                string oki = "";
                string mochi = "";
                sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                JmpIpt.Text = oki + " " + mochi;
                showList(ban.hash);
                BackBtn.Enabled = true;
                DebugTxt.Text = ban.banShow();
                history.Add(ban.hash);
            }
        }

        private void AddBtn_Click(object sender, EventArgs e) {
            Pturn tursn = TebanChk.Checked ? Pturn.Sente : Pturn.Gote;
            int ret = ban.chkMoveable((byte)((((int)OxIpt.Value - 1) << 4) + OyIpt.Value - 1), (byte)((((int)NxIpt.Value - 1) << 4) + NyIpt.Value - 1), tursn, NariChk.Checked);
            if (ret == 0) {
                int ox = (int)OxIpt.Value;
                string oy = tw2mkjs_csaIO.int2Dafb((int)OyIpt.Value - 1);
                int nx = (int)NxIpt.Value;
                string ny = tw2mkjs_csaIO.int2Dafb((int)NyIpt.Value - 1);
                string nari;
                if (NariChk.Checked == true) {
                    nari = "+";
                } else {
                    nari = "";
                }
                string turn;
                if (TebanChk.Checked == true) {
                    turn = "+";
                } else {
                    turn = "-";
                }

                int val = ((int)ValIpt.Value);
                int weight = ((int)WeyIpt.Value);
                int type = ((int)TpeIpt.Value);
                string sstr = tw2usiIO.pos2usi((byte)((((int)OxIpt.Value - 1) << 4) + OyIpt.Value - 1), (byte)((((int)NxIpt.Value - 1) << 4) + NyIpt.Value - 1), NariChk.Checked);
                NextLst.Items.Add(turn + sstr + "/" + val + "/" + weight + "/" + type);

                string oki = "";
                string mochi = "";
                sfenIO.ban2sfen(ref ban, ref oki, ref mochi);

                List<string> str = new List<string>();
                for (int i = 0; i < NextLst.Items.Count; i++) {
                    str.Add(NextLst.Items[i].ToString());
                    Debug.WriteLine(str[i]);
                }
                Debug.WriteLine(oki + " " + mochi);

                sMove.updateAll(ban.hash, oki + " " + mochi, str);
            } else {
                Debug.WriteLine(" chkMoveable = " + ret);
            }
        }

        private void SetBtn_Click(object sender, EventArgs e) {
            if ((NextLst.SelectedIndex > -1) && (NextLst.SelectedIndex < NextLst.Items.Count)) {

                Pturn tursn = TebanChk.Checked ? Pturn.Sente : Pturn.Gote;
                int ret = ban.chkMoveable((byte)((((int)OxIpt.Value - 1) << 4) + OyIpt.Value - 1), (byte)((((int)NxIpt.Value - 1) << 4) + NyIpt.Value - 1), tursn, NariChk.Checked);
                if (ret == 0) {

                    int ox = (int)OxIpt.Value;
                    string oy = tw2mkjs_csaIO.int2Dafb((int)OyIpt.Value - 1);
                    int nx = (int)NxIpt.Value;
                    string ny = tw2mkjs_csaIO.int2Dafb((int)NyIpt.Value - 1);
                    string nari;
                    if (NariChk.Checked == true) {
                        nari = "+";
                    } else {
                        nari = "";
                    }
                    string turn;
                    if (TebanChk.Checked == true) {
                        turn = "+";
                    } else {
                        turn = "-";
                    }

                    int val = ((int)ValIpt.Value);
                    int weight = ((int)WeyIpt.Value);
                    int type = ((int)TpeIpt.Value);
                    string sstr = tw2usiIO.pos2usi((byte)((((int)OxIpt.Value - 1) << 4) + OyIpt.Value - 1), (byte)((((int)NxIpt.Value - 1) << 4) + NyIpt.Value - 1), NariChk.Checked);
                    NextLst.Items[NextLst.SelectedIndex] = turn + sstr + "/" + val + "/" + weight + "/" + type;

                    string oki = "";
                    string mochi = "";
                    sfenIO.ban2sfen(ref ban, ref oki, ref mochi);

                    List<string> str = new List<string>();
                    for (int i = 0; i < NextLst.Items.Count; i++) {
                        str.Add(NextLst.Items[i].ToString());
                        Debug.WriteLine(str[i]);
                    }
                    Debug.WriteLine(oki + " " + mochi);

                    sMove.updateAll(ban.hash, oki + " " + mochi, str);
                }
            }
        }

        private void DelBtn_Click(object sender, EventArgs e) {
            if ((NextLst.SelectedIndex > -1) && (NextLst.SelectedIndex < NextLst.Items.Count)) {
                NextLst.Items.RemoveAt(NextLst.SelectedIndex);
                string oki = "";
                string mochi = "";
                sfenIO.ban2sfen(ref ban, ref oki, ref mochi);

                List<string> str = new List<string>();
                for (int i = 0; i < NextLst.Items.Count; i++) {
                    str.Add(NextLst.Items[i].ToString());
                    Debug.WriteLine(str[i]);
                }
                Debug.WriteLine(oki + " " + mochi);

                sMove.updateAll(ban.hash, oki + " " + mochi, str);
            }
        }

        private void RootBtn_Click(object sender, EventArgs e) {
            ban = new ban();
            // 初期
            ban.startpos();
            string oki = "";
            string mochi = "";
            sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
            JmpIpt.Text = oki + " " + mochi;
            DebugTxt.Text = ban.banShow();
            showList(ban.hash);
        }

        private void NextLst_SelectedIndexChanged(object sender, EventArgs e) {
            if ((NextLst.SelectedIndex > -1) && (NextLst.SelectedIndex < NextLst.Items.Count)) {

                string[] arr = NextLst.Items[NextLst.SelectedIndex].ToString().Split('/');
                if (arr[0][0] == '+') {
                    TebanChk.Checked = true; // 先手
                } else {
                    TebanChk.Checked = false; // 後手
                }
                byte oPos;
                byte nPos;
                bool nari;
                tw2usiIO.usi2pos(arr[0].Substring(1), out oPos, out nPos, out nari);
                OxIpt.Value = (oPos >> 4) + 1;
                OyIpt.Value = (oPos & 0x0F) + 1;
                NxIpt.Value = (nPos >> 4) + 1;
                NyIpt.Value = (nPos & 0x0F) + 1;
                NariChk.Checked = nari;
                ValIpt.Value = Convert.ToInt32(arr[1]);
                WeyIpt.Value = Convert.ToInt32(arr[2]);
                TpeIpt.Value = Convert.ToInt32(arr[3]);
            }
        }

        private void NextBtn_Click(object sender, EventArgs e) {
            if ((NextLst.SelectedIndex > -1) && (NextLst.SelectedIndex < NextLst.Items.Count)) {
                Pturn turn = Pturn.Sente;
                string[] arr = NextLst.Items[NextLst.SelectedIndex].ToString().Split('/');
                byte oPos;
                byte nPos;
                bool nari;
                tw2usiIO.usi2pos(arr[0].Substring(1), out oPos, out nPos, out nari);
                if (arr[0][0] == '+') {
                    turn = Pturn.Sente;
                } else {
                    turn = Pturn.Gote;
                }
                ban.moveKoma(oPos, nPos, turn, nari, false);
                string oki = "";
                string mochi = "";
                sfenIO.ban2sfen(ref ban, ref oki, ref mochi);
                JmpIpt.Text = oki + " " + mochi;
                showList(ban.hash);
                BackBtn.Enabled = true;
                DebugTxt.Text = ban.banShow();
                history.Add(ban.hash);
            }
        }

        private void FleIpt_DragEnter(object sender, DragEventArgs e) {
            e.Effect = DragDropEffects.Copy;
        }

        private void FleIpt_DragDrop(object sender, DragEventArgs e) {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            // 渡されたファイルに対して処理を行う
            string[] dragFilePathArr = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            FleIpt.Text = dragFilePathArr[0];
        }
    }
}
