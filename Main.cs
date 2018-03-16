using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;


namespace BT
{
    public partial class mainWin : Form
    {
        private SerialPort inPort;
        private bool receiveStatus = false;
        private Thread refreshData;
        TimeSpan RemainTime;
        TimeSpan SubTime = new TimeSpan(0, 0, 0, 0, 100);
        bool isStr = true;

        public mainWin()
        {
            InitializeComponent();
            SerialPortSearch();
            
        }

        private void SerialPortSearch()
        {
            string[] ports = SerialPort.GetPortNames();
            for (int i = 0; i < ports.Length; i++)
                comList.Items.Add(ports[i]);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            try
            {
                String COM = comList.SelectedItem.ToString();
                inPort = new SerialPort(COM, 9600);
                inPort.Open();
                if (inPort.IsOpen == true)
                {
                    btStatus.Text = "已連接成功!";
                    btStatus.ForeColor = Color.Blue;
                    btnStart.Enabled = true;
                    btnStop.Enabled = true;
                    btnExport.Enabled = true;
                    btnSend.Enabled = true;
                    btn_cmd.Enabled = true;
                    refreshData = new Thread(dataRefrsh);
                    refreshData.Start();
                }
            }
            catch (System.IO.IOException)
            {
                btStatus.Text = "連接失敗! 請重新選擇其他COM使用。";
                btStatus.ForeColor = Color.Red;
                btnConnect.Enabled = true;
            }
            catch (System.NullReferenceException)
            {
                btStatus.Text = "選項不得為空，請重新選擇其他COM使用。";
                btStatus.ForeColor = Color.Red;
                btnConnect.Enabled = true;
            }
            catch (System.UnauthorizedAccessException)
            {
                btStatus.Text = "存取被拒絕! 請重新選擇其他COM使用。";
                btStatus.ForeColor = Color.Red;
                btnConnect.Enabled = true;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            //inPort.Write("1");
            receiveStatus = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            
           
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
           // inPort.Write("0");
            receiveStatus = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;

        }

        private void btnTimer_Click(object sender, EventArgs e)
        {
            int t = (int)time.Value;
            RemainTime = new TimeSpan(0, 0, t);
            timerTest.Start();
            try
            {
                //inPort.Write("1");
                receiveStatus = true;
                btnStart.Enabled = false;
                btnStop.Enabled = false;
                btnTimer.Enabled = false;
            }
            catch (Exception) { }
            
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            /*try
            {*/
                String path = fileName.Text +".txt";
                FileInfo file = new FileInfo(path);
                StreamWriter sw = file.CreateText();
                sw.WriteLine(data.Text);
                sw.Flush();
                sw.Close();
           /* }
            catch (Exception){}*/

        }
        public static string ToHexString(byte[] bytes) // 0xae00cf => "AE00CF "
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();
            }
            return hexString;
        }

        delegate void SetNumCallback(int newText);
        private void receiveNumData(int str)
        {
            if (this.InvokeRequired)
            {
                SetNumCallback update = new SetNumCallback(receiveNumData);
                this.Invoke(update, new object[] { str });
            }
            else
            {
                data.Text += str +" ";
                data.SelectionStart = data.Text.Length;
                data.ScrollToCaret();
            }
        }


        delegate void SetTextCallback(string newText);
        private void receiveData(String str)
        {
            if (this.InvokeRequired)
            {
                SetTextCallback update = new SetTextCallback(receiveData);
                this.Invoke(update, new object[] { str });
            }
            else
            {
                data.Text += str + "\n";
                data.SelectionStart = data.Text.Length;
                data.ScrollToCaret();
            }
        }

        private void dataRefrsh()
        {
            while (true)
            {
                if (receiveStatus == true)
                {
                    try
                    {
                        if (isStr)
                        {
                            string str = inPort.ReadLine();
                            receiveData(str);
                        }
                        else
                        {
                            int tmp = inPort.ReadByte();
                            if ( tmp == 0x80)
                            {
                                Byte[] buffer = new Byte[18];
                                buffer[0] = 0x80;
                                int len = inPort.Read(buffer, 1, buffer.Length-1);
                                String str = ToHexString(buffer);
                                if (buffer[1] == 0x00 && buffer[2] == 0x00 && buffer[3] == 0x7f && buffer[4]==0x00)
                                    receiveData(str);
                            }
                        }
                    }
                    catch (TimeoutException) { }
                }
                else
                {
                    try
                    {
                        string str = inPort.ReadLine();
                    }
                    catch (TimeoutException) { }
                }
            }
        }

        private void btnClean_Click(object sender, EventArgs e)
        {
            data.Text = "";
        }

        private void timerTest_Tick(object sender, EventArgs e)
        {
            RemainTime = RemainTime.Subtract(SubTime);
            lblTime.Text = "" + RemainTime.Seconds;
            if (RemainTime.TotalMilliseconds == 0)
            {
                inPort.Write("0");
                receiveStatus = false;
                timerTest.Stop();
                btnTimer.Enabled = true;
                btnStart.Enabled = true;
                btnStop.Enabled = true;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (refreshData != null)
                refreshData.Abort();
            if (inPort != null)
                inPort.Close();
            Application.Exit();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            inPort.Write(txtSend.Text);
            txtSend.Text = "";
        }

        private void btn_cmd_Click(object sender, EventArgs e)
        {
            try
            {
                String cmd_str = txtCmd.Text;
                byte[] Command = Encoding.ASCII.GetBytes(cmd_str);
                byte[] cmd = new byte[Command.Length + 2];
                for (int i = 0; i < Command.Length; i++)
                    cmd[i] = Command[i];
                cmd[Command.Length] = 0x0d;
                cmd[Command.Length + 1] = 0x0a;

                inPort.Write(cmd, 0, cmd.Length);
                txtCmd.Text = "";
            }
            catch (Exception) { }
        }

        private void btnTypeOfOutput_Click(object sender, EventArgs e)
        {
            isStr = (isStr == true) ? false : true;
            btnTypeOfOutput.Text = (isStr == true) ? "String" : "Hex";
        }
    }
}
