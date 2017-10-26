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
        private bool SendSettlingTime()
        {
            byte[] tempData = BitConverter.GetBytes(Globals.SettlingCycles);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x01;
            TempFreq.Data = new List<byte>(tempData);
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
            int FirstPara = 0;
            int SecondPara = 0;
            String coef = null;
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            char[] buffer;
            openFileDialog1.InitialDirectory = "c:\\";
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
                                coef = new string(buffer, FirstPara, SecondPara - FirstPara); //Get string of Coefs
                                coef = Regex.Replace(coef, @"\s", String.Empty); //Remove white Space

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
            if (Globals.Serial.IsOpen)
            {
                byte FailedCounter = 0;
                if (button_StartStop.Text == "Start") //Stop the stuff
                {
                    while (!SendStop())
                    {
                        FailedCounter++;
                        if (FailedCounter >= 3)
                        {
                            OutputLabel.Text += ">> Sending Switch Config Failed\n>> Test Failed";
                            return;
                        }
                    }
                    button_StartStop.Text = "Stop";
                }
                else //Start the stuff
                {
                    while (!SendStart())
                    {
                        FailedCounter++;
                        if (FailedCounter >= 3)
                        {
                            OutputLabel.Text += ">> Sending Switch Config Failed\n>> Test Failed";
                            return;
                        }
                    }
                    button_StartStop.Text = "Start";
                }
            }
            else
            {
                OutputLabel.Text += "Please Connect to Bluetooth";
            }
        }
    }
}
