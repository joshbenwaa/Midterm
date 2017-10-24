using System;
using System.Windows.Forms;
using Excel = Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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

        /// <summary>
        /// Event Handler to change the number of samples
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SamplesText_TextChanged(object sender, EventArgs e)
        {
            Globals.Samples = Convert.ToUInt16(SamplesText.Text);
            if (Globals.Samples >= 1000)
            {
                Globals.Samples = 1000;
            }
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
        /// Tell the PIC chip to test at that frequency
        /// </summary>
        /// <param name="currfreq">the current frequency to test at.</param>
        /// <returns>True if PIC acknowledges</returns>
        public bool SendTest(uint currfreq)
        {
            byte[] tempData = BitConverter.GetBytes(currfreq);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x06;
            TempFreq.Data = new List<byte>(tempData);
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        /// <summary>
        /// Refresh Global variables
        /// </summary>
        public void RefreshFreqGlobals()
        {
            try
            {
                Globals.StartFreq = uint.Parse(StartFreqText.Text);
            }
            catch (Exception)
            {
                OutputLabel.Text = ">> Need to Enter Values.";
                return;
            }
            Globals.FreqInc = uint.Parse(FreqIncText.Text);
            Globals.NumOfInc = ushort.Parse(IncrementsText.Text);
            switch (OutputVoltageDropDown.SelectedItem)
            {
                case "2Vpp": Globals.OutputVoltage = Globals.OutputVoltage2Vpp; break;
                case "1Vpp": Globals.OutputVoltage = Globals.OutputVoltage1Vpp; break;
                case "400mVpp": Globals.OutputVoltage = Globals.OutputVoltage400mVpp; break;
                case "200mVpp": Globals.OutputVoltage = Globals.OutputVoltage200mVpp; break;
            }
            switch (SettlingTimeCombo.SelectedItem)
            {
                case "60": Globals.SettlingCycles = 60; break;
                case "255": Globals.SettlingCycles = 255; break;
                case "510": Globals.SettlingCycles = 510; break;
                case "1022": Globals.SettlingCycles = 1022; break;
                case "2044": Globals.SettlingCycles = 2044; break;

            }
            Globals.Samples = Convert.ToInt32(SamplesText.Text);
            Globals.SwitchA = (byte)aNumber.Value;
            Globals.SwitchB = (byte)bNumber.Value;
            Globals.SwitchTn = (byte)TnNumber.Value;
            Globals.SwitchTp = (byte)TpNumber.Value;
        }

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
        private bool SendSwitches()
        {
            List<byte> TempData = new List<byte> { Globals.SwitchA, Globals.SwitchB, Globals.SwitchTn, Globals.SwitchTp };
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
        private bool SendOutputRange()
        {
            List<byte> TempData = new List<byte> { Globals.OutputVoltage };
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x02;
            TempFreq.Data = TempData;
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        /// <summary>
        /// Send Data Request
        /// </summary>
        /// <returns>Returns true if the PIC acknowedges the command</returns>
        private bool SendDataRequest()
        {
            byte[] tempData = { };
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x07;
            TempFreq.Data = new List<byte>();
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        /// <summary>
        /// Send the Request for Temperature data
        /// </summary>
        /// <returns>returns true if the PIC acknowledges the command</returns>
        private bool SendTempRequest()
        {
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x05;
            TempFreq.Data = new List<byte>();
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        /// <summary>
        /// Send the number of samples to take to the PIC
        /// </summary>
        /// <returns>Returns true if the PIC acknowedges the command</returns>
        private bool SendSamples()
        {
            byte[] tempData = BitConverter.GetBytes(Globals.Samples);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x08;
            TempFreq.Data = new List<byte>(tempData);
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        #endregion

    }
}
