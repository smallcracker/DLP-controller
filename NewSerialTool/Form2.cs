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
        public Graphics g ;
        public Form2()
        {
            InitializeComponent();
            Screen[] sc;
            sc = Screen.AllScreens;
            int NumberofScreens = sc.Length-1;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(sc[NumberofScreens].Bounds.Left, sc[NumberofScreens].Bounds.Top);
            this.WindowState = FormWindowState.Maximized;
            g = pictureBox1.CreateGraphics();
            pictureBox1.Width = sc[NumberofScreens].Bounds.Width;
            pictureBox1.Height = sc[NumberofScreens].Bounds.Height;
            pictureBox1.Image = new Bitmap(sc[NumberofScreens].Bounds.Width, sc[NumberofScreens].Bounds.Height);
            Console.WriteLine(sc[NumberofScreens].Bounds.Width);
        }

        private void Form2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
