using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace NewSerialTool
{
    public partial class Form2 : Form
    {
        public Graphics g;
        public Form2()
        {
            InitializeComponent();
            Screen[] sc;
            sc = Screen.AllScreens;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(sc[1].Bounds.Left, sc[1].Bounds.Top);
            this.WindowState = FormWindowState.Maximized;
            g = this.CreateGraphics();

        }

        private void Form2_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
