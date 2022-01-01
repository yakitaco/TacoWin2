using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TacoWin2_Mkjs {
    public partial class tw2mkjs_mForm : Form {
        public tw2mkjs_mForm() {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) {
            var ret = tw2mkjs_csaIO.loadFile(textBox1.Text ,0);
            for (int i = 0; i < ret[0].Count; i++) {
                listBox1.Items.Add("" + ret[0][i] + "/" + ret[0][i]);
            }

        }
    }
}
