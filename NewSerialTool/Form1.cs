using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using static System.Net.Mime.MediaTypeNames;

struct controllerState
{
    public double rx, ry;//控制器输入
    public double cx, cy;//测量量
    public double ux, uy;//控制器输出

    public void changeState(double rx, double ry, double cx, double cy, double ux, double uy)
    {
        this.rx = rx;
        this.ry = ry;
        this.cx = cx;
        this.cy = cy;
        this.ux = ux;
        this.uy = uy;
    }
    public void copyState(controllerState a)
    {
        this.rx = a.rx;
        this.ry = a.ry;
        this.cx = a.cx;
        this.cy = a.cy;
        this.ux = a.ux;
        this.uy = a.uy;
    }
}

namespace NewSerialTool
{
    public partial class Form1 : Form
    {
        private long ReceiveCount = 0;
        private long SendCount = 0;
        private DateTime current_time = new DateTime();
        private static VideoCapture capture;
        Form2 f = new Form2();
        bool Form2Visible = false;
        String SlidesPath;
        List<String> SlidesPathList = new List<String>();
        int DisplayProgress = 0;
        bool timetoIncertBlackFrame = false;

        double Kp = 0.05, Ki = 1, Kd = 1;


        private controllerState[] State = new controllerState[4];


        public Form1()
        {
            InitializeComponent();
        }

        private void SerialMessageSend(String s)
        {
            byte[] temp = new byte[1];
            //serialPort1.Write(s);
            this.Invoke((EventHandler)(delegate
            {
                if (checkBox1.Checked)
                {
                    current_time = DateTime.Now;
                    textBox1.AppendText(current_time.ToString("HH:mm:ss") + " ");
                }
                textBox1.AppendText("Send:     ");
                if (radioButton4.Checked)
                {
                    string pattern = @"[^[0-9a-fA-F]]*";
                    string replacement = "";
                    Regex rgx = new Regex(pattern);
                    string send_data = rgx.Replace(s, replacement);
                    long num = (send_data.Length - send_data.Length % 2) / 2;
                    for (int i = 0; i < num; i++)
                    {
                        temp[0] = Convert.ToByte(send_data.Substring(i * 2, 2), 16);
                        textBox1.AppendText(send_data.Substring(i * 2, 2) + " ");
                        serialPort1.Write(temp, 0, 1);  //循环发送

                    }
                    SendCount += num;
                    textBox1.AppendText("\r\n");

                }
                else if (radioButton5.Checked)
                {
                    string pattern = @"\b(1[0-1][0-9])\b|\b(12[0-7])\b|\b([0-9][0-9])\b|\b([1-9])\b";
                    Regex rgx = new Regex(pattern);
                    foreach (Match match in rgx.Matches(s))
                    {
                        temp[0] = Convert.ToByte(match.Value, 10);
                        textBox1.AppendText(temp[0].ToString("X2") + " ");
                        serialPort1.Write(temp, 0, 1);
                    }
                    SendCount += rgx.Matches(s).Count;
                    textBox1.AppendText("\r\n");
                }
                else
                {
                    serialPort1.Write(s);
                    textBox1.AppendText(s + "\r\n");
                    SendCount += s.Length;
                }
                if (checkBox2.Checked)
                {
                    SendCount += 2;
                    serialPort1.Write("\r\n");
                }
                label8.Text = "S:" + Convert.ToString(SendCount);
            }));
        }

        private void Exception_Process(Exception ex)
        {
            serialPort1 = new SerialPort();
            //刷新COM口选项
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            //响铃并显示异常给用户
            System.Media.SystemSounds.Beep.Play();
            button4.Text = "Open the Port";
            button4.BackColor = Color.ForestGreen;
            MessageBox.Show(ex.Message);
            comboBox1.Enabled = true;
            comboBox2.Enabled = true;
            comboBox3.Enabled = true;
            comboBox4.Enabled = true;
            comboBox5.Enabled = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            comboBox1.Text = "COM1";
            comboBox2.Text = "115200";
            comboBox3.Text = "8";
            comboBox4.Text = "None";
            comboBox5.Text = "1";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                    button1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = false;
                    label6.Text = "Serial Port is Closed";
                    label6.ForeColor = Color.Red;
                    button4.Text = "Open the Port";
                    button4.BackColor = Color.ForestGreen;
                    comboBox1.Enabled = true;
                    comboBox2.Enabled = true;
                    comboBox3.Enabled = true;
                    comboBox4.Enabled = true;
                    comboBox5.Enabled = true;
                }
                else
                {
                    comboBox1.Enabled = false;
                    comboBox2.Enabled = false;
                    comboBox3.Enabled = false;
                    comboBox4.Enabled = false;
                    comboBox5.Enabled = false;
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);
                    serialPort1.DataBits = Convert.ToInt16(comboBox3.Text);


                    if (comboBox4.Text.Equals("None"))
                        serialPort1.Parity = Parity.None;
                    else if (comboBox4.Text.Equals("Odd"))
                        serialPort1.Parity = Parity.Odd;
                    else if (comboBox4.Text.Equals("Even"))
                        serialPort1.Parity = Parity.Even;
                    else if (comboBox4.Text.Equals("Mark"))
                        serialPort1.Parity = Parity.Mark;
                    else if (comboBox4.Text.Equals("Space"))
                        serialPort1.Parity = Parity.Space;

                    if (comboBox5.Text.Equals("1"))
                        serialPort1.StopBits = StopBits.One;
                    else if (comboBox5.Text.Equals("1.5"))
                        serialPort1.StopBits = StopBits.OnePointFive;
                    else if (comboBox5.Text.Equals("2"))
                        serialPort1.StopBits = StopBits.Two;

                    serialPort1.Open();
                    label6.Text = "Serial Port is Open";
                    label6.ForeColor = Color.Green;
                    button4.Text = "Close the Port";
                    button4.BackColor = Color.Firebrick;

                    button1.Enabled = true;
                    button2.Enabled = true;
                    button3.Enabled = true;

                }
            }
            catch (Exception ex)
            {
                Exception_Process(ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    SerialMessageSend(textBox2.Text);
                }
            }
            catch (Exception ex)
            {
                Exception_Process(ex);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    SerialMessageSend(textBox3.Text);
                }
            }
            catch (Exception ex)
            {
                Exception_Process(ex);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    SerialMessageSend(textBox4.Text);
                }
            }
            catch (Exception ex)
            {
                Exception_Process(ex);
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int num = serialPort1.BytesToRead;
                byte[] ReceiveBuf = new byte[num];
                ReceiveCount += num;
                serialPort1.Read(ReceiveBuf, 0, num);
                this.Invoke((EventHandler)(delegate
                    {
                        if (checkBox1.Checked)
                        {
                            current_time = DateTime.Now;
                            textBox1.AppendText(current_time.ToString("HH:mm:ss") + " ");
                        }
                        textBox1.AppendText("Receive:  ");
                        if (radioButton2.Checked)
                        {
                            textBox1.AppendText("HEX:  ");
                            foreach (byte b in ReceiveBuf)
                            {
                                textBox1.AppendText(b.ToString("X2") + " ");
                            }
                            textBox1.AppendText("\r\n");
                        }
                        else
                        {
                            textBox1.AppendText(Encoding.UTF8.GetString(ReceiveBuf) + "\r\n");
                        }
                        label7.Text = "R:" + Convert.ToString(ReceiveCount);
                    }));
            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show(ex.Message);
            }
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            ReceiveCount = 0;
            label7.Text = "R:0";
            label8.Text = "S:0";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            button1_Click(button1, new EventArgs());
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBox3.Checked)
            {
                //自动发送功能选中,开始自动发送
                numericUpDown1.Enabled = false;     //失能时间选择
                timer1.Interval = (int)numericUpDown1.Value;     //定时器赋初值
                timer1.Start();     //启动定时器
                //label6.Text = "串口已打开" + " 自动发送中...";
            }
            else
            {
                //自动发送功能未选中,停止自动发送
                numericUpDown1.Enabled = true;     //使能时间选择
                timer1.Stop();     //停止定时器
                //label6.Text = "串口已打开";
            }
        }

        private void CamController_Enter(object sender, EventArgs e)
        {

        }

        private void openCamera()
        {
            capture = new VideoCapture(0);
            //capture.Open(0, VideoCaptureAPIs.ANY);
            if (!capture.IsOpened())
            {
                Close();
                MessageBox.Show("打开摄像头失败");
                return;
            }
            button7.Text = "Close the Camera";
            cameraWorker.RunWorkerAsync();
        }

        private void closeCamera()
        {
            button7.Text = "Open the Camera";
            cameraWorker.CancelAsync();
            capture.Release();
            pictureBox1.Image = null;
        }


        private void button7_Click(object sender, EventArgs e)
        {
            if (cameraWorker.IsBusy)
            {
                closeCamera();
            }
            else
            {
                openCamera();
            }
        }

        private void cameraWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var bgWorker = (BackgroundWorker)sender;

            while (!bgWorker.CancellationPending)
            {
                using (var frameMat = capture.RetrieveMat())
                {
                    var frameBitmap = BitmapConverter.ToBitmap(frameMat);
                    bgWorker.ReportProgress(0, frameBitmap);
                }
                Thread.Sleep(100);
            }
        }

        private void cameraWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var frameBitmap = (Bitmap)e.UserState;
            pictureBox1.Image?.Dispose();
            pictureBox1.Height = frameBitmap.Height;
            pictureBox1.Width = frameBitmap.Width;
            pictureBox1.Image = frameBitmap;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (cameraWorker.IsBusy)
            {
                Bitmap image = new Bitmap(pictureBox1.Image);
                image.Save(System.DateTime.Now.ToString("s").Replace(":", "-") + "-capture.jpg");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (!Form2Visible)
            {
                Form2Visible = true;
                f = new Form2();                  
                f.Show();
                this.Activate();
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {

        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (Form2Visible)
                {
                    f.Close();
                    Form2Visible = false;
                }
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofdlg = new OpenFileDialog();
            ofdlg.Filter = "BMP File(*.bmp)|*.bmp";
            if (ofdlg.ShowDialog() == DialogResult.OK)
            {
                Bitmap image = new Bitmap(ofdlg.FileName);
                f.pictureBox1.Image = image;
            }
            Rectangle r = new Rectangle(10, 10, 300, 200);//是创建画矩形的区域  
            f.g.DrawRectangle(Pens.Red, r);//g对象提供了画图形的方法，我们只需调用即可
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Screen[] sc;
            sc = Screen.AllScreens;
            Screen scr = sc[1];
            Rectangle rc = scr.Bounds;
            int iWidth = rc.Width;
            int iHeight = rc.Height;
            System.Drawing.Image myImage = new Bitmap(iWidth, iHeight);
            Graphics g = Graphics.FromImage(myImage);
            g.CopyFromScreen(new System.Drawing.Point(sc[1].Bounds.Left, sc[1].Bounds.Top), new System.Drawing.Point(0, 0), new System.Drawing.Size(iWidth, iHeight));
            myImage.Save(System.DateTime.Now.ToString("s").Replace(":", "-") + "-frame.jpg");
        }
        void Director(string dir)
        {
            DirectoryInfo d = new DirectoryInfo(dir);
            FileSystemInfo[] fsinfos = d.GetFileSystemInfos();
            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                if (fsinfo is DirectoryInfo)     //判断是否为文件夹
                {
                    Director(fsinfo.FullName);//递归调用
                }
                else
                {
                    //Console.WriteLine(fsinfo.FullName);//输出文件的全部路径
                    if (fsinfo.FullName.Contains(".png") || fsinfo.FullName.Contains(".jpg") || fsinfo.FullName.Contains(".jpeg"))
                    {
                        SlidesPathList.Add(fsinfo.FullName);
                    }
                }
            }
            SlidesPathList.Sort();
            foreach (String SlidePath in SlidesPathList)
            {
                Console.WriteLine(SlidePath);
            }
            label10.Text = "progres: 0/" + SlidesPathList.Count;
        }

        private void button10_Click_1(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                SlidesPathList.Clear();
                DisplayProgress = 0;
                SlidesPath = dialog.SelectedPath;
                label9.Text = "Current Path:\n" + SlidesPath;
                Director(SlidesPath);
            }
        }

        private void button14_Click(object sender, EventArgs e)
        {
            DisplayProgress = 0;
            numericUpDown2.Enabled = false;
            timer2.Interval = (int)numericUpDown2.Value;
            timer2.Start();
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (timetoIncertBlackFrame && checkBox4.Checked)
            {
                System.Drawing.Image lastPic = f.pictureBox1.Image;
                f.pictureBox1.Image = null;
                lastPic.Dispose();
                timer2.Interval = (int)numericUpDown3.Value;
                timetoIncertBlackFrame = false;
                return;
            }
            else
            {
                timer2.Interval = (int)numericUpDown2.Value;
                timetoIncertBlackFrame = true;
            }
            if (f.pictureBox1.Image != null)
            {
                System.Drawing.Image lastPic = f.pictureBox1.Image;
                f.pictureBox1.Image = System.Drawing.Image.FromFile(SlidesPathList[DisplayProgress++]);
                lastPic.Dispose();
            }
            else
            {
                f.pictureBox1.Image = System.Drawing.Image.FromFile(SlidesPathList[DisplayProgress++]);
            }
            if (DisplayProgress >= SlidesPathList.Count)
            {
                System.Drawing.Image lastPic = f.pictureBox1.Image;
                f.pictureBox1.Image = null;
                lastPic.Dispose();
                timer2.Stop();
                numericUpDown2.Enabled = true;
                DisplayProgress = 0;
            }
            label10.Text = "progres: " + DisplayProgress + "/" + SlidesPathList.Count;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            f.pictureBox1.Image = null;
            timer2.Stop();
            numericUpDown2.Enabled = true;
            DisplayProgress = 0;
        }

        private void button15_Click(object sender, EventArgs e)
        {
            SolidBrush redBrush = new SolidBrush(Color.Red);
            int x = 40;
            int y = 70;
            int width = 30;
            int height = 30;
            f.g.FillEllipse(redBrush, x, y, width, height);
            State[3].changeState((double)numericUpDown4.Value,
                                 (double)numericUpDown5.Value,
                                 40.0,
                                 70.0,
                                 40.0,
                                 70.0);
        }

        private void ControllerTimer_Tick(object sender, EventArgs e)
        {
            State[0].copyState(State[1]);
            State[1].copyState(State[2]);
            State[2].copyState(State[3]);
            State[3].changeState(State[3].rx,
                State[3].ry,
                State[3].ux,
                State[3].uy,
                1.6 * State[3].ux - 0.63 * State[2].ux + 0.03 * State[2].rx ,
                1.6 * State[3].uy - 0.63 * State[2].uy + 0.03 * State[2].ry );
            SolidBrush blackBrush = new SolidBrush(Color.Black);
            f.g.FillRectangle(blackBrush, 0, 0, f.pictureBox1.Width, f.pictureBox1.Height);
            SolidBrush redBrush = new SolidBrush(Color.Red);
            int x = (int)State[3].ux;
            int y = (int)State[3].uy;
            Console.WriteLine(x.ToString() + " " + y.ToString());
            int width = 30;
            int height = 30;
            f.g.FillEllipse(redBrush, x, y, width, height);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            ControllerTimer.Interval = 50;
            State[3].changeState((double)numericUpDown4.Value,
                (double)numericUpDown5.Value,
                State[3].cx,
                State[3].cy,
                State[3].ux,
                State[3].uy);
            ControllerTimer.Start();
        } 
    }
}
