using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using GxIAPINET;
using static System.Net.Mime.MediaTypeNames;
using VideoMode;

struct ControllerState
{
    public double rx, ry;//控制器输入
    public double cx, cy;//测量量
    public double ux, uy;//控制器输出

    public void ChangeState(double rx, double ry, double cx, double cy, double ux, double uy)
    {
        this.rx = rx;
        this.ry = ry;
        this.cx = cx;
        this.cy = cy;
        this.ux = ux;
        this.uy = uy;
    }
    public void CopyState(ControllerState a)
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
        Form2 f = new Form2();

        private const string HexPattern = @"[^[0-9a-fA-F]]*";
        private const string DECtoHEXPattern =
            @"\b(1[0-1][0-9])\b|\b(12[0-7])\b|\b([0-9][0-9])\b|\b([1-9])\b";
        private DateTime current_time = new DateTime();
        private long ReceiveCount = 0;
        private long SendCount = 0;

        private static VideoCapture capture;
        private bool Form2Visible = false;
        private string SlidesPath;
        private List<String> SlidesPathList = new List<String>();
        private int DisplayProgress = 0;
        private bool timetoIncertBlackFrame = false;

        private int MessageCount = 0;

        private ControllerState[] State = new ControllerState[4];

        int m_CamNum = 0;
        DhCamera m_Cam = new DhCamera();

        public Form1()
        {
            InitializeComponent();




        }

        private void SerialMessageSend(string s)
        {
            byte[] temp = new byte[1];
            this.Invoke((EventHandler)delegate
            {
                // 收发窗显示发送时间
                if (checkBox1.Checked)
                {
                    current_time = DateTime.Now;
                    textBox1.AppendText(current_time.ToString("HH:mm:ss") + " ");
                }
                textBox1.AppendText("Send:     ");

                // 以十六进制发送
                if (radioButton4.Checked)
                {
                    string replacement = "";
                    Regex rgx = new Regex(HexPattern);
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
                // 接收十进制，但是以十进制发送
                else if (radioButton5.Checked)
                {
                    Regex rgx = new Regex(DECtoHEXPattern);
                    foreach (Match match in rgx.Matches(s))
                    {
                        temp[0] = Convert.ToByte(match.Value, 10);
                        textBox1.AppendText(temp[0].ToString("X2") + " ");
                        serialPort1.Write(temp, 0, 1);
                    }
                    SendCount += rgx.Matches(s).Count;
                    textBox1.AppendText("\r\n");
                }
                // 正常发送ASCII字符
                else
                {
                    serialPort1.Write(s);
                    textBox1.AppendText(s + "\r\n");
                    SendCount += s.Length;
                }
                // 发送换行符
                if (checkBox2.Checked)
                {
                    SendCount += 2;
                    serialPort1.Write("\r\n");
                }
                label8.Text = "S:" + Convert.ToString(SendCount);
            });
        }

        //这个异常调用程序会在完成的是关闭串口的工作，并输出异常原因。
        private void Exception_Process(Exception ex)
        {
            serialPort1 = new SerialPort();
            //刷新COM口选项
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            //修改按钮外观
            button4.Text = "Open the Port";
            button4.BackColor = Color.ForestGreen;
            MessageBox.Show(ex.Message);
            //重新使能串口功能选择框
            comboBox1.Enabled = true;
            comboBox2.Enabled = true;
            comboBox3.Enabled = true;
            comboBox4.Enabled = true;
            comboBox5.Enabled = true;

            checkBox5.Checked = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            comboBox1.Text = "COM1";
            comboBox2.Text = "115200";
            comboBox3.Text = "8";
            comboBox4.Text = "None";
            comboBox5.Text = "1";


            // 大恒相机
            DhCamera.initlib();
            m_CamNum = DhCamera.GetCamNumFunc(); //获取识别到的相机数量
            if (m_CamNum > 0)
                textBox1.Text = m_Cam.GetInfo(0); //第一个相机的id
            else
                textBox1.Text = "未识别到相机";

        }

        // 检测可以连接的串口
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
                    // 关闭串口
                    checkBox5.Checked = false;
                    label6.Text = "Serial Port is Closed";
                    button1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = false;
                    label6.ForeColor = Color.Red;
                    button4.Text = "Open the Port";
                    serialPort1.Dispose();
                    //serialPort1.Close();
                    button4.BackColor = Color.ForestGreen;
                    comboBox1.Enabled = true;
                    comboBox2.Enabled = true;
                    comboBox3.Enabled = true;
                    comboBox4.Enabled = true;
                    comboBox5.Enabled = true;
                }
                else
                {
                    // 打开串口
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

        byte[] ReceiveBuf = new byte[40960];

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            MessageCount++;
            try
            {
                int num = serialPort1.BytesToRead;
                Array.Clear(ReceiveBuf, 0, ReceiveBuf.Length);
                ReceiveCount += num;
                serialPort1.Read(ReceiveBuf, 0, num);

                this.Invoke((EventHandler)delegate
                    {
                        textBox1.Text = "";
                        if (checkBox1.Checked)
                        {

                            current_time = DateTime.Now;
                            textBox1.AppendText(current_time.ToString("HH:mm:ss") + " ");
                        }
                        textBox1.AppendText("\nReceive:  ");
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
                         if (checkBox5.Checked)
                        {
                            string status = Encoding.UTF8.GetString(ReceiveBuf);
                            UpdateWorkingStatusTable(status);
                        }
                        label7.Text = "R:" + Convert.ToString(ReceiveCount);
                    });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        public static readonly string[] StatusPatterns = new string[] {
            "a(-?\\d*\\.?\\d*)",
            "b(-?\\d*\\.?\\d*)",
            "c(-?\\d*)",
            "d(-?\\d*)",
            "e(-?\\d*)",
            "f(-?\\d*)",
            "g(\\d*)",
            "h(-?\\d*\\.?\\d*)",
            "i(-?\\d*\\.?\\d*)",
            "j(-?\\d*)",
            "k(-?\\d*)",
            "l(-?\\d*)",
            "m(\\d)(\\d)(\\d)(\\d)"
        };

        public static readonly string[] statusStringHeads = new string[]
        {
            "Longitude:                  ",
            "Latitude:                   ",
            "Machine State:              ",
            "Bx:                         ",
            "By:                         ",
            "Bz:                         ",
            "B:                          ",
            "Brigheness:                 ",
            "Longitude Motor Angle:      ",
            "Latitude Motor Angle:       ",
            "xPosition:                  ",
            "yPosition:                  ",
            "Longitude Motor Status:     ",
            "Latitude Motor Status:      ",
            "xMotor State:               ",
            "yMotor State:               ",
            "Message Count:              "
        };

        private string ExtractInfo(string status, string pattern, int index)
        {

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            Match m = regex.Match(status);
            if (m.Success)
            {
                try
                {
                    return m.Groups[index].Value.Remove(0,1);
                }
                finally
                {
                    
                }
            }
            else return "0000";
        }
        private void UpdateWorkingStatusTable(string status)
        {
            label11.Text = statusStringHeads[0] + ExtractInfo(status, StatusPatterns[0], 0);
            label12.Text = statusStringHeads[1] + ExtractInfo(status, StatusPatterns[1], 0);
            label13.Text = statusStringHeads[2] + ExtractInfo(status, StatusPatterns[2], 0);
            double Bx = double.Parse(ExtractInfo(status, StatusPatterns[3], 0)) * 0.13;
            double By = double.Parse(ExtractInfo(status, StatusPatterns[4], 0)) * 0.13;
            double Bz = double.Parse(ExtractInfo(status, StatusPatterns[5], 0)) * 0.13;
            int RealMessageCount = int.Parse(ExtractInfo(status, StatusPatterns[11], 0));
            double B = Math.Sqrt(Bx * Bx + By * By + Bz * Bz);
            //Console.WriteLine(statusStringHeads[6] + B.ToString("0.000"));
            label14.Text = statusStringHeads[3] + Bx.ToString();
            label15.Text = statusStringHeads[4] + By.ToString();
            label16.Text = statusStringHeads[5] + Bz.ToString();
            label17.Text = statusStringHeads[6] + B.ToString("0.000");
            label18.Text = statusStringHeads[7] + ExtractInfo(status, StatusPatterns[6], 0);
            label19.Text = statusStringHeads[8] + ExtractInfo(status, StatusPatterns[7], 0);
            label20.Text = statusStringHeads[9] + ExtractInfo(status, StatusPatterns[8], 0);
            label21.Text = statusStringHeads[10] + ExtractInfo(status, StatusPatterns[9], 0);
            label22.Text = statusStringHeads[11] + ExtractInfo(status, StatusPatterns[10], 0);
            label23.Text = statusStringHeads[12] + ExtractInfo(status, StatusPatterns[12], 0)[0];
            label24.Text = statusStringHeads[13] + ExtractInfo(status, StatusPatterns[12], 0)[1];
            label25.Text = statusStringHeads[14] + ExtractInfo(status, StatusPatterns[12], 0)[2];
            label26.Text = statusStringHeads[15] + ExtractInfo(status, StatusPatterns[12], 0)[3];
            label27.Text = statusStringHeads[16] + (RealMessageCount-MessageCount).ToString();
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

        private void openUSBCamera()
        {
            // USB摄像头
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

        private void closeUSBCamera()
        {
            // USB摄像头
            button7.Text = "Open the Camera";
            cameraWorker.CancelAsync();
            capture.Release();
            pictureBox1.Image = null;
        }


        private void button7_Click(object sender, EventArgs e)
        {
            // 大恒摄像头
            if (m_Cam.CamOpen())
            {
                closeDahengCamera();
            }
            else
            {
                openDahengCamera();
            }

            //USB摄像头
            //if (cameraWorker.IsBusy)
            //{
            //    closeUSBCamera();
            //}
            //else
            //{
            //    openUSBCamera();
            //}
        }
        public void ShowImage(int index, Bitmap objdata)
        {
            if (!m_Cam.m_bIsColor)
            {
                //添加调色板
                ColorPalette palette;
                palette = objdata.Palette;
                int i = 0;
                for (i = 0; i <= 255; i++)
                {
                    palette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }
                objdata.Palette = palette;

            }
            //BitmapImage objdataImage = BitmapToBitmapImage(objdata);       
            Action action1 = () =>
            {
                pictureBox1.Image = objdata;
            };
            pictureBox1.BeginInvoke(action1);
        }
        private void openDahengCamera()
        {
            if (m_CamNum > 0)
                if (!m_Cam.CamOpen())
                    m_Cam.OpenCameraFunc(0, ShowImage);
            if (m_Cam.CamOpen())
                m_Cam.StartAcqFunc();
        }

        private void closeDahengCamera()
        {
            if (m_Cam.CamOpen())
                m_Cam.CloseCameraFunc();
            if (m_Cam.CamOpen())
                m_Cam.StopAcqFunc();
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
            int NumberofScreens = sc.Length - 1;
            Screen scr = sc[NumberofScreens];
            Rectangle rc = scr.Bounds;
            int iWidth = rc.Width;
            int iHeight = rc.Height;
            System.Drawing.Image myImage = new Bitmap(iWidth, iHeight);
            Graphics g = Graphics.FromImage(myImage);
            g.CopyFromScreen(new System.Drawing.Point(sc[NumberofScreens].Bounds.Left, sc[NumberofScreens].Bounds.Top), new System.Drawing.Point(0, 0), new System.Drawing.Size(iWidth, iHeight));
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
                    if (fsinfo.FullName.Contains(".png") || fsinfo.FullName.Contains(".jpg") || fsinfo.FullName.Contains(".jpeg")|| fsinfo.FullName.Contains(".bmp"))
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
            if (DisplayProgress >= SlidesPathList.Count)
            {
                System.Drawing.Image lastPic = f.pictureBox1.Image;
                f.pictureBox1.Image = null;
                lastPic.Dispose();
                timer2.Stop();
                numericUpDown2.Enabled = true;
                DisplayProgress = 0;
                label10.Text = "progres: " + DisplayProgress + "/" + SlidesPathList.Count;
                return;
            }
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
            label10.Text = "progres: " + DisplayProgress + "/" + SlidesPathList.Count;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            f.pictureBox1.Image = null;
            timer2.Stop();
            numericUpDown2.Enabled = true;
            DisplayProgress = 0;
        }


        /// <summary>
        /// 图像明暗调整
        /// </summary>
        /// <param name="b">原始图</param>
        /// <param name="degree">亮度[-255, 255]</param>
        /// <returns></returns>
        public static Bitmap KiLighten(Bitmap b, int degree)
        {
            if (b == null)
            {
                return null;
            }

            if (degree < -255) degree = -255;
            if (degree > 255) degree = 255;

            try
            {

                int width = b.Width;
                int height = b.Height;

                int pix = 0;

                BitmapData data = b.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                unsafe
                {
                    byte* p = (byte*)data.Scan0;
                    int offset = data.Stride - width * 3;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            // 处理指定位置像素的亮度
                            for (int i = 0; i < 3; i++)
                            {
                                p[i] = (byte)(p[i] > 0 ? degree : 0);

                                //if (degree < 0) p[i] = (byte)Math.Max(0, pix);
                                //if (degree > 0) p[i] = (byte)Math.Min(255, pix);

                            } // i
                            p += 3;
                        } // x
                        p += offset;
                    } // y
                }

                b.UnlockBits(data);

                return b;
            }
            catch
            {
                return null;
            }

        } // end of Lighten

        private void button17_Click(object sender, EventArgs e)
        {
            Bitmap image = new Bitmap(f.pictureBox1.Image);
            Bitmap a = KiLighten(image, (int)numericUpDown6.Value);
            f.pictureBox1.Image = a;
            return;
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            Bitmap image = new Bitmap(f.pictureBox1.Image);
            Bitmap a = KiLighten(image, (int)numericUpDown6.Value);
            f.pictureBox1.Image = a;
            return;
        }

        private void ControllerTimer_Tick(object sender, EventArgs e)
        {
            State[0].CopyState(State[1]);
            State[1].CopyState(State[2]);
            State[2].CopyState(State[3]);
            State[3].ChangeState(State[3].rx,
                State[3].ry,
                State[3].ux,
                State[3].uy,
                1.6 * State[3].ux - 0.63 * State[2].ux + 0.03 * State[2].rx,
                1.6 * State[3].uy - 0.63 * State[2].uy + 0.03 * State[2].ry);
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


        private void Form1_Resize(object sender, EventArgs e)
        {
        }

        private void Form1_ClientSizeChanged(object sender, EventArgs e)
        {
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button15_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    SerialMessageSend("b1");
                }
            }
            catch (Exception ex)
            {
                Exception_Process(ex);
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    SerialMessageSend("b8000");
                }
            }
            catch (Exception ex)
            {
                Exception_Process(ex);
            }
        }

        private void button19_Click(object sender, EventArgs e)
        {
            // 获取文本框的值
            string Longitude = textBox5.Text;
            string Latitude = textBox6.Text;
            string formattedLongitude, formattedLatitude;

            // 将文本框的值转换为数字
            if (int.TryParse(Longitude, out int LongitudeNumber))
            {
                // 将数字格式化为3位数，并在前面用0填充
                formattedLongitude = LongitudeNumber.ToString("D3");
            }
            else
            {
                Console.WriteLine("无效的经度输入");
                return;
            }

            // 将文本框的值转换为数字
            if (int.TryParse(Latitude, out int LatitudeNumber))
            {
                // 将数字格式化为2位数，并在前面用0填充
                formattedLatitude = Math.Abs(LatitudeNumber).ToString("D2");
                formattedLatitude = LatitudeNumber > 0 ? "1" + formattedLatitude : "0" + formattedLatitude;
            }
            else
            {
                Console.WriteLine("无效的经度输入");
                return;
            }
            try
            {
                if (serialPort1.IsOpen)
                {
                    Console.WriteLine("y" + formattedLongitude + formattedLatitude);
                    SerialMessageSend("y" + formattedLongitude + formattedLatitude);
                }
            }
            catch (Exception ex)
            {
                Exception_Process(ex);
            }
        }

        private void button18_Click(object sender, EventArgs e)
        {
            string longitudeMaxBound = textBox7.Text;
            try
            {
                if (serialPort1.IsOpen)
                {
                    SerialMessageSend("h" + longitudeMaxBound + " j");
                }
            }
            catch (Exception ex)
            {
                Exception_Process(ex);
            }
        }


        private string MagnetizationSlidesPath;
        private List<string> MagnetizationSlidesList = new List<string>();
        private int MagnetizationDisplayProgress = 0;
        private int mode = 0;

        void MagnetizationDirector(string dir)
        {
            DirectoryInfo d = new DirectoryInfo(dir);
            FileSystemInfo[] fsinfos = d.GetFileSystemInfos();
            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                if (fsinfo is DirectoryInfo)     //判断是否为文件夹
                {
                    MagnetizationDirector(fsinfo.FullName);//递归调用
                }
                else
                {
                    //Console.WriteLine(fsinfo.FullName);//输出文件的全部路径
                    if (fsinfo.FullName.Contains(".png") || fsinfo.FullName.Contains(".jpg") || fsinfo.FullName.Contains(".jpeg") || fsinfo.FullName.Contains(".bmp"))
                    {
                        MagnetizationSlidesList.Add(fsinfo.FullName);
                    }
                }
            }
            MagnetizationSlidesList.Sort();
            foreach (String SlidePath in MagnetizationSlidesList)
            {
                Console.WriteLine(SlidePath);
            }
            label10.Text = "progres: 0/" + MagnetizationSlidesList.Count;
        }
        private void button20_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new FolderBrowserDialog();
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                MagnetizationSlidesList.Clear();
                MagnetizationDisplayProgress = 0;
                MagnetizationSlidesPath = dialog.SelectedPath;
                label9.Text = "Current Path:\n" + dialog.SelectedPath;
                MagnetizationDirector(MagnetizationSlidesPath);
            }
        }

        private void mainTimer_Tick(object sender, EventArgs e)
        {
            // 发送转向指令
            if(mode == 0)
            {
                string command = MagnetizationSlidesList[MagnetizationDisplayProgress];
                string commandExtract = command.Substring(command.Length - 11, 7);
                try
                {
                    if (serialPort1.IsOpen)
                    {
                        Console.WriteLine(commandExtract);
                        SerialMessageSend(commandExtract);
                    }
                }
                catch (Exception ex)
                {
                    Exception_Process(ex);
                }
                mainTimer.Interval = 30000;
                mode = 1;
                return;
            }

            // 显示投影图案
            if(mode == 1)
            {
                if (f.pictureBox1.Image != null)
                {
                    System.Drawing.Image lastPic = f.pictureBox1.Image;
                    f.pictureBox1.Image = System.Drawing.Image.FromFile(MagnetizationSlidesList[MagnetizationDisplayProgress]);
                    lastPic.Dispose();
                    Console.WriteLine("Picture changed");
                }
                else
                {
                    f.pictureBox1.Image = System.Drawing.Image.FromFile(MagnetizationSlidesList[MagnetizationDisplayProgress]);
                    Console.WriteLine("Picture changed");
                }
                mainTimer.Interval = (int)numericUpDown2.Value;
                mode = 2;
                return;
            }

            // 关闭投影图案
            if(mode == 2)
            {
                System.Drawing.Image lastPictoDisPose = f.pictureBox1.Image;
                f.pictureBox1.Image = null;
                lastPictoDisPose.Dispose();
                label10.Text = "progres: " + MagnetizationDisplayProgress + "/" + MagnetizationSlidesList.Count;
                mode = 0;
                mainTimer.Interval = 20;

                // 判断是否完成固化流程
                MagnetizationDisplayProgress++;
                if (MagnetizationDisplayProgress >= MagnetizationSlidesList.Count)
                {
                    Console.WriteLine("All finished.");
                    mainTimer.Stop();
                    numericUpDown2.Enabled = true;
                    MagnetizationDisplayProgress = 0;
                    label10.Text = "progres: " + MagnetizationDisplayProgress + "/" + MagnetizationSlidesList.Count;
                }
                return;
            }
        }

        private void button21_Click(object sender, EventArgs e)
        {
            MagnetizationDisplayProgress = 0;
            numericUpDown2.Enabled = false;
            mainTimer.Interval = 30;
            mode = 0;
            mainTimer.Start();
        }

        private Screen[] scforSnap;
        private int NumberofScreensforSnap;
        private Screen scrforSnap;
        private Rectangle rcSnap;
        private int iWidth;
        private int iHeight;

        private void ScreenSnapTimer_Tick(object sender, EventArgs e)
        {
            System.Drawing.Image myImage = new Bitmap(iWidth, iHeight);
            Graphics g = Graphics.FromImage(myImage);
            g.CopyFromScreen(new System.Drawing.Point(scforSnap[NumberofScreensforSnap].Bounds.Left, scforSnap[NumberofScreensforSnap].Bounds.Top), new System.Drawing.Point(0, 0), new System.Drawing.Size(iWidth, iHeight));
            if(pictureBox2.Image != null)
            {
                System.Drawing.Image PictoDispose = pictureBox2.Image;
                pictureBox2.Image = myImage;
                PictoDispose.Dispose();
            }
            else
            {
                pictureBox2.Image = myImage;
            }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            scforSnap = Screen.AllScreens;
            NumberofScreensforSnap = scforSnap.Length - 1;
            scrforSnap = scforSnap[NumberofScreensforSnap];
            rcSnap = scrforSnap.Bounds;
            iWidth = rcSnap.Width;
            iHeight = rcSnap.Height;

            if (checkBox6.Checked)
            {
                ScreenSnapTimer.Interval = 1000;
                ScreenSnapTimer.Start();
            }
            else
            {
                System.Drawing.Image PictoDispose = pictureBox2.Image;
                pictureBox2.Image = null;
                PictoDispose.Dispose();
                ScreenSnapTimer.Stop();
            }
        }

        private void cameraTimer_Tick(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //大恒相机
            if (m_Cam.CamOpen())
                m_Cam.CloseCameraFunc();
        }
    }
}
