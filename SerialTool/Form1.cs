using System.IO.Ports;
using System.Diagnostics;
using System.Configuration;

namespace SerialTool
{
    
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //获取电脑当前可用串口并添加到选项列表中
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            comboBox1.Text = "COM1";
            comboBox2.Text = "115200";
            comboBox3.Text = "8";
            comboBox4.Text = "None";
            comboBox5.Text = "1";

            Trace.Listeners.Add(new TextWriterTraceListener(DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fff") + ".log"));
            Trace.Flush();
        }

        private void BtnDetectPort_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
        }

        private void BtnOpenPort_Click(object sender, EventArgs e)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Close();
                    BtnOpenPort.Text = "Open the Port";
                    BtnOpenPort.BackColor = Color.ForestGreen;
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
                    sp.PortName = comboBox1.Text;
                    sp.BaudRate = Convert.ToInt32(comboBox2.Text);
                    sp.DataBits = Convert.ToInt16(comboBox3.Text);

                    if (comboBox4.Text.Equals("None")) sp.Parity = Parity.None;
                    else if (comboBox4.Text.Equals("Odd")) sp.Parity = Parity.Odd;
                    else if (comboBox4.Text.Equals("Even")) sp.Parity = Parity.Even;
                    else if (comboBox4.Text.Equals("Mark")) sp.Parity = Parity.Mark;
                    else if (comboBox4.Text.Equals("Space")) sp.Parity = Parity.Space;

                    if (comboBox5.Text.Equals("1")) sp.StopBits = StopBits.One;
                    else if (comboBox5.Text.Equals("1.5")) sp.StopBits = StopBits.OnePointFive;
                    else if (comboBox5.Text.Equals("2")) sp.StopBits = StopBits.Two;

                    sp.Open();
                    BtnOpenPort.Text = "Close the Port";
                    BtnOpenPort.BackColor = Color.Firebrick;

                }
            }
            catch (Exception ex)
            {
                sp = new SerialPort();
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(SerialPort.GetPortNames());
                BtnOpenPort.Text = "Open the Port";
                BtnOpenPort.BackColor = Color.ForestGreen;
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }
        }

        private void BtnSend1_Click(object sender, EventArgs e)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Write(textBox2.Text);
                    Trace.WriteLine("    Send:" + textBox2.Text);
                    Trace.Flush();
                }
            }
            catch (Exception ex)
            {
                sp = new SerialPort();
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(SerialPort.GetPortNames());
                BtnOpenPort.Text = "Open the Port";
                BtnOpenPort.BackColor = Color.ForestGreen;
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }
        }

        private void BtnSend2_Click(object sender, EventArgs e)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Write(textBox4.Text);
                    Trace.WriteLine("    Send:" + textBox4.Text);
                    Trace.Flush();
                }
            }
            catch (Exception ex)
            {
                sp = new SerialPort();
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(SerialPort.GetPortNames());
                BtnOpenPort.Text = "Open the Port";
                BtnOpenPort.BackColor = Color.ForestGreen;
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }
        }

        private void BtnSend3_Click(object sender, EventArgs e)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Write(textBox6.Text);
                    Trace.WriteLine("    Send:" + textBox6.Text);
                    Trace.Flush();
                }
            }
            catch (Exception ex)
            {
                sp = new SerialPort();
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(SerialPort.GetPortNames());
                BtnOpenPort.Text = "Open the Port";
                BtnOpenPort.BackColor = Color.ForestGreen;
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }
        }

        private void BtnSend4_Click(object sender, EventArgs e)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Write(textBox8.Text);
                    Trace.WriteLine("    Send:"+textBox8.Text);
                    Trace.Flush();
                }
            }
            catch (Exception ex)
            {
                sp = new SerialPort();
                comboBox1.Items.Clear();
                comboBox1.Items.AddRange(SerialPort.GetPortNames());
                BtnOpenPort.Text = "Open the Port";
                BtnOpenPort.BackColor = Color.ForestGreen;
                MessageBox.Show(ex.Message);
                comboBox1.Enabled = true;
                comboBox2.Enabled = true;
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox5.Enabled = true;
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] ReData = new byte[sp.BytesToRead];
            sp.Read(ReData, 0, ReData.Length);
            String ReString = System.Text.Encoding.UTF8.GetString(ReData);
            Trace.WriteLine("   Receive:" + ReString);
            Trace.Flush();
            this.Invoke(new EventHandler(delegate
            {
                textBox1.Text += ReString + "\r\n";
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}