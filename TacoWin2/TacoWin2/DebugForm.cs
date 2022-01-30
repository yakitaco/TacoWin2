using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TacoWin2 {
    public partial class DebugForm : Form {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")] // この行を追加
        private static extern bool AllocConsole();                 // この行を追加

        private static DebugForm _formInstance;

        public DebugForm() {
            InitializeComponent();
            AllocConsole();
            _formInstance = this;
        }

        //Form1オブジェクトを取得、設定するためのプロパティ
        public static DebugForm instance {
            get {
                return _formInstance;
            }
            set {
                _formInstance = value;
            }
        }

        delegate void delegate1(String text1);
        delegate void delegate2();

        public void addMsg(string msg) {
            Invoke(new delegate1(_addMsg), msg);
        }

        public void _addMsg(string msg) {
            textBox1.AppendText(msg + "\r\n");
            System.Diagnostics.Debug.WriteLine(msg);
        }

        public void resetMsg() {
            Invoke(new delegate2(_resetMsg));
        }

        public void _resetMsg() {
            textBox1.ResetText();
        }

        public string getText() {
            return textBox1.Text;
        }

    }
}
