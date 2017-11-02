using System;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
namespace Midterm
{
    public partial class Main : Form
    {
        #region Variables

        /// <summary>
        /// flag for connection between GUI and PIC
        /// </summary>
        public bool connectedflag = false;
        #endregion

        #region Main
        /// <summary>
        /// Main constructor
        /// </summary>
        public Main()
        {
            InitializeComponent();
        }

        #endregion

        #region Bluetooth Event Handlers

        /// <summary>
        /// Event handler to disconnect to close serial port.
        /// </summary>
        /// <param name="sender">object sender</param>
        /// <param name="e">EventArgs</param>
        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.Serial.Close();
            connectedflag = false;
            BlutoothStatusLabel.Text = string.Format("Bluetooth Connection: {0}", connectedflag);
            BlutoothStatusLabel.ForeColor = System.Drawing.Color.FromArgb(255, 0, 0);
        }

        /// <summary>
        /// Event Handler to launch the bluetooth settings form. This is where you connect to the board.
        /// </summary>
        /// <param name="sender">object sender</param>
        /// <param name="e">EventArgs e</param>
        private void bluetoothSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {

            BluetoothSettings frm = new BluetoothSettings();
            frm.Show();
        }

        #endregion

        #region Button Event Handlers

        /// <summary>
        /// Closes the GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Helper Functions
        /// <summary>
        /// Prints the byte array to the serial port
        /// </summary>
        /// <param name="bytes">the byte array to print</param>
        public void PrintByteArray(byte[] bytes)
        {
            var sb = new StringBuilder("new byte[] { ");
            foreach (var b in bytes)
            {
                sb.Append(b + ", ");
            }
            sb.Append("}");
            Console.WriteLine(sb.ToString());
        }

        #endregion

        #region Sending Data

        /// <summary>
        /// Check for the acknowledgement flags
        /// </summary>
        /// <returns>True if the PIC acknowledges the command</returns>
        private bool Aknowledged()
        {
            byte flag = 0;
            while (flag < 2)
            {
                try
                {
                    if ((byte)Globals.Serial.ReadByte() == 0x7E)
                    {
                        flag++;
                    }
                    else
                    {
                        flag = 0;
                    }
                }
                catch (TimeoutException)
                {

                    return false;
                }
            }
            return true;

        }

        /// <summary>
        /// Send Settling Time Cycles
        /// </summary>
        /// <returns>Returns true if the PIC acknowledges the command</returns>
        private bool SendCoef(byte[] coef)
        {
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x01;
            TempFreq.Data = new List<byte>(coef);
            TempFreq.CreateHDLC();
            try
            {
                Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            }
            catch (Exception)
            {
                return false;
            }
            return Aknowledged();
        }

        /// <summary>
        /// Sends the switching variables
        /// </summary>
        /// <returns>Returns true if the PIC acknowledges the command</returns>
        private bool SendStop()
        {
            List<byte> TempData = new List<byte> {};
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x03;
            TempFreq.Data = TempData;
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        /// <summary>
        /// Send Output Voltage Range Variable
        /// </summary>
        /// <returns>Returns true if the PIC Acknowledges</returns>
        private bool SendStart()
        {
            List<byte> TempData = new List<byte> {};
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x02;
            TempFreq.Data = TempData;
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        #endregion

        private void button_download_Click(object sender, EventArgs e)
        {
            Globals.Serial.ReadTimeout = -1;
            int FirstPara = 0;
            int SecondPara = 0;
            byte FailedCounter = 0;
            byte[] coeff_values = null;
            String coef = null;
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            char[] buffer;
            openFileDialog1.InitialDirectory = "D:\\School";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            using (var sr = new StreamReader(myStream))
                            {
                                buffer = new char[(int)sr.BaseStream.Length];
                                sr.Read(buffer, 0, (int)sr.BaseStream.Length);
                                while(buffer[FirstPara] != '{')
                                {
                                    FirstPara++;
                                }
                                while(buffer[SecondPara] != '}')
                                {
                                    SecondPara++;
                                }
                                coef = new string(buffer, FirstPara + 1, SecondPara - FirstPara - 1); //Get string of Coefs
                                coeff_values = CreateValueArray(coef);
                                if (Globals.Serial.IsOpen)
                                {
                                    OutputLabel.Text = ">> Sending Coefs...\n";
                                    while (!SendCoef(coeff_values))
                                    {
                                        FailedCounter++;
                                        if (FailedCounter >= 3)
                                        {
                                            OutputLabel.Text += ">> Sending Coefs Failed\n";
                                            return;
                                        }
                                    }
                                    OutputLabel.Text += ">> Coeficients Sent.\n";
                                }
                                else
                                {
                                    OutputLabel.Text += ">> Please Connect Bluetooth\n";
                                }
                            }
                        }
                    }
            }
                catch (Exception ex)
            {
                MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
            }
        }
        }

        private void button_StartStop_Click(object sender, EventArgs e)
        {
            Globals.Serial.ReadTimeout = 1000;
            if (Globals.Serial.IsOpen)
            {
                byte FailedCounter = 0;
                if (button_StartStop.Text == "Running") //Stop the stuff
                {
                    OutputLabel.Text = ">> Sending Stop Command...\n";
                    while (!SendStop())
                    {
                        FailedCounter++;
                        if (FailedCounter >= 3)
                        {
                            OutputLabel.Text += ">> Sending Stop Signal Failed\n";
                            return;
                        }
                    }
                    button_StartStop.Text = "Stopped";
                    OutputLabel.Text += ">> Filter Stopped\n";
                }
                else //Start the stuff
                {
                    OutputLabel.Text = ">> Sending Start Command...\n";
                    while (!SendStart())
                    {
                        FailedCounter++;
                        if (FailedCounter >= 3)
                        {
                            OutputLabel.Text += ">> Sending Start Siganl Failed\n";
                            return;
                        }
                    }
                    button_StartStop.Text = "Running";
                    OutputLabel.Text += ">> Filter Running...\n";
                }
            }
            else
            {
                OutputLabel.Text += ">> Please Connect to Bluetooth";
            }
        }

        byte[] CreateValueArray(string Values)
        {
            string tempValue;
            byte[] tempInt;
            List<byte> temp = new List<byte>();
            int OldComma = 0;
            Values = Values.Replace(" ", ""); //Remove spaces
            Values = Values.Replace("\r\n", ""); //Remove new line and return
            if (Values[Values.Length - 1] != ',')
            {
                Values += ',';
            }
            for (int i = 0; i < Values.Length; i++)
            {
                if (Values[i] == ',')
                {
                    tempValue = Values.Substring(OldComma, i - OldComma);
                    tempInt = BitConverter.GetBytes(Int16.Parse(tempValue));
                    temp.Add(tempInt[0]);
                    temp.Add(tempInt[1]);
                    OldComma = i + 1;
                }
            }
            return temp.ToArray();
        }
    }
}
