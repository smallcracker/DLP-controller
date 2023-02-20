using System;
using System.ComponentModel;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace NewSerialTool
{
    public partial class Form1 : Form
    {
        private long ReceiveCount = 0;
        private long SendCount = 0;
        private DateTime current_time = new DateTime();
        private static VideoCapture capture;
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
            Form2 f = new Form2();
            f.Show();
        }

    }
}
