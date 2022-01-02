using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TacoWin2_SMV;

namespace TacoWin2_Mkjs {
    public partial class tw2mkjs_mForm : Form {

        bool isRunning = false;

        private class WorkerParams {
            public string ytjFilePath;
            public string csaDirPath;
        }

        public tw2mkjs_mForm() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {

            if (isRunning == false) {
                WorkerParams wp = new WorkerParams();
                wp.ytjFilePath = @textBox2.Text;
                wp.csaDirPath = @textBox1.Text;
                csaLoad.RunWorkerAsync(wp);
                isRunning = true;
                button1.Text = "STOP";
            } else {
                csaLoad.CancelAsync();
                isRunning = false;
                button1.Text = "START";
            }

            listBox1.Items.Clear();
            var ret = tw2mkjs_csaIO.loadFile(textBox1.Text , 0, out var str);
            for (int i = 0; i < str[0].Count; i++) {
                listBox1.Items.Add("" + str[0][i] + "/" + str[1][i]);
            }
            if (ret == 0) {
                label1.Text = "先手勝ち";
            } else if (ret == 1) {
                label1.Text = "後手勝ち";
            } else {
                label1.Text = "その他";
            }
        }

        private void csaLoad_DoWork(object sender, DoWorkEventArgs e) {
            WorkerParams wp = (WorkerParams)e.Argument;

            // senderの値はbgWorkerの値と同じ
            BackgroundWorker worker = (BackgroundWorker)sender;

            sMove.load(wp.ytjFilePath);

            string[] csrDirFiles = tw2mkjs_csaIO.loadDir(wp.csaDirPath);





        }

        private void csaLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                MessageBox.Show("canceled");
                // この場合にはe.Resultにはアクセスできない
            } else {
                // 処理結果の表示
                MessageBox.Show("done");
            }
            isRunning = false;
            button1.Text = "START";
        }
    }
}
