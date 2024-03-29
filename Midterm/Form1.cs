﻿using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
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

            //To handle live data easily, in this case we built a specialized type
            //the MeasureModel class, it only contains 2 properties
            //DateTime and Value
            //We need to configure LiveCharts to handle MeasureModel class
            //The next code configures MEasureModel  globally, this means
            //that livecharts learns to plot MeasureModel and will use this config every time
            //a ChartValues instance uses this type.
            //this code ideally should only run once, when application starts is reccomended.
            //you can configure series in many ways, learn more at http://lvcharts.net/App/examples/v1/wpf/Types%20and%20Configuration

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)   //use DateTime.Ticks as X
                .Y(model => model.Value);           //use the value property as Y

            //lets save the mapper globally.
            Charting.For<MeasureModel>(mapper);

            //the ChartValues property will store our values array
            ChartValues = new ChartValues<MeasureModel>();
            cartesianChart1.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Values = ChartValues,
                    PointGeometrySize = 18,
                    StrokeThickness = 4
                }
            };
            cartesianChart1.AxisX.Add(new Axis
            {
                DisableAnimations = true,
                LabelFormatter = value => new System.DateTime((long)value).ToString("mm:ss"),
                Separator = new Separator
                {
                    Step = TimeSpan.FromSeconds(1).Ticks
                }
            });

            SetAxisLimits(System.DateTime.Now);

            ////The next code simulates data changes every 500 ms
            //Timer = new Timer
            //{
            //    Interval = 500
            //};
            //Timer.Tick += TimerOnTick;
            //Timer.Start();
        }

        public ChartValues<MeasureModel> ChartValues { get; set; }
        //public Timer Timer { get; set; }

        private void SetAxisLimits(System.DateTime now)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<System.DateTime>(SetAxisLimits), new object[] { now });
                return;
            }

            cartesianChart1.AxisX[0].MaxValue = now.Ticks + TimeSpan.FromSeconds(1).Ticks; // lets force the axis to be 100ms ahead
            cartesianChart1.AxisX[0].MinValue = now.Ticks - TimeSpan.FromSeconds(8).Ticks; //we only care about the last 8 seconds
        }

        //private void TimerOnTick(object sender, EventArgs eventArgs)
        //{
        //    var now = System.DateTime.Now;

        //    ChartValues.Add(new MeasureModel
        //    {
        //        DateTime = now,
        //        Value = //Value to Plot
        //    });

        //    SetAxisLimits(now);

        //    //lets only use the last 30 values
        //    if (ChartValues.Count > 30) ChartValues.RemoveAt(0);
        //}

        #endregion

        #region Bluetooth Event Handlers

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.Serial.Close();
            connectedflag = false;
            BlutoothStatusLabel.Text = string.Format("Bluetooth Connection: {0}", connectedflag);
            BlutoothStatusLabel.ForeColor = System.Drawing.Color.FromArgb(255, 0, 0);
        }

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

        private bool SendPeriod(UInt16 Period)
        {
            byte[] TempData = BitConverter.GetBytes( Globals.Period);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x04;
            TempFreq.Data = new List<byte>(TempData);
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        private bool SendPeriod_LPF(UInt16 Period)
        {
            byte[] TempData = BitConverter.GetBytes(Period);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x07;
            TempFreq.Data = new List<byte>(TempData);
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        private bool SendPeriod_BPF(UInt16 Period)
        {
            byte[] TempData = BitConverter.GetBytes(Period);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x08;
            TempFreq.Data = new List<byte>(TempData);
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        private bool SendOffset()
        {
            byte[] TempData = BitConverter.GetBytes(Globals.offset);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x05;
            TempFreq.Data = new List<byte>(TempData);
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        private bool SendScale(Byte s)
        {
            byte[] TempData = BitConverter.GetBytes(s);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0x06;
            TempFreq.Data = new List<byte>(TempData);
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        private bool Send_Write_to_GUI(byte flag)
        {
            byte[] TempData = BitConverter.GetBytes(flag);
            HDLC_tx TempFreq = new HDLC_tx();
            TempFreq.cmd = 0xFF;
            TempFreq.Data = new List<byte>(TempData);
            TempFreq.CreateHDLC();
            Globals.Serial.Write(TempFreq.Buffer, 0, TempFreq.Buffer.Length);
            return Aknowledged();
        }

        private void button_download_Click(object sender, EventArgs e)
        {
            Globals.Serial.ReadTimeout = -1;
            int FirstPara = 0;
            int SecondPara = 0;
            byte FailedCounter = 0;
            byte[] coeff_values = null;
            String coef = null;
            UInt16 tempperiod = 0;
            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            char[] buffer;
            if (comboBox_FilterSelection.SelectedItem == "Custom")
            {
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
                                    while (buffer[FirstPara] != '{')
                                    {
                                        FirstPara++;
                                    }
                                    while (buffer[SecondPara] != '}')
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
            else //Everting thats not custom
            {
                switch(comboBox_FilterSelection.SelectedItem)
                {
                    case "LPF":
                        switch (comboBox_CutoffSelection.SelectedItem)
                        {
                            case "100Hz":
                                tempperiod = Globals.LPF_100_Period;
                                //coeff_values = CreateValueArray(Globals.LPF_100);
                                break;
                            case "200Hz":
                                tempperiod = Globals.LPF_200_Period;
                                //coeff_values = CreateValueArray(Globals.LPF_200);
                                break;
                            case "300Hz":
                                tempperiod = Globals.LPF_300_Period;
                                //coeff_values = CreateValueArray(Globals.LPF_300);
                                break;
                        }
                        if (Globals.Serial.IsOpen)
                        {
                            OutputLabel.Text = ">> Setting up LPF...\n";
                            while (!SendPeriod_LPF(tempperiod))
                            {
                                FailedCounter++;
                                if (FailedCounter >= 3)
                                {
                                    OutputLabel.Text += ">> SSetting Up LPF Failed\n";
                                    return;
                                }
                            }
                            OutputLabel.Text += ">> LPF Set up.\n";
                        }
                        else
                        {
                            OutputLabel.Text += ">> Please Connect Bluetooth\n";
                        }
                        break;

                    case "BPF":
                        switch (comboBox_CutoffSelection.SelectedItem)
                        {
                            case "100Hz":
                                tempperiod = Globals.BPF_100_Period;
                                //coeff_values = CreateValueArray(Globals.BPF_100);
                                break;
                            case "200Hz":
                                tempperiod = Globals.BPF_200_Period;
                                //coeff_values = CreateValueArray(Globals.BPF_200);
                                break;
                            case "300Hz":
                                tempperiod = Globals.BPF_300_Period;
                                //coeff_values = CreateValueArray(Globals.BPF_300);
                                break;
                        }
                        if (Globals.Serial.IsOpen)
                        {
                            OutputLabel.Text = ">> Setting up BPF...\n";
                            while (!SendPeriod_BPF(tempperiod))
                            {
                                FailedCounter++;
                                if (FailedCounter >= 3)
                                {
                                    OutputLabel.Text += ">> Setting Up BPF Failed\n";
                                    return;
                                }
                            }
                            OutputLabel.Text += ">> BPF Set up.\n";
                        }
                        else
                        {
                            OutputLabel.Text += ">> Please Connect Bluetooth\n";
                        }
                        break;
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

        private void button_offset_Click(object sender, EventArgs e)
        {
            int FailedCounter = 0;
            OutputLabel.Text = ">> Sending Offset Command...\n";
            while (!SendOffset())
            {
                FailedCounter++;
                if (FailedCounter >= 3)
                {
                    OutputLabel.Text += ">> Sending Offset Signal Failed\n";
                    return;
                }
            }
        }
        #endregion


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

        #region Offset Functions


        private void textBox_Offset_TextChanged(object sender, EventArgs e)
        {
            if (Byte.TryParse(textBox_Period.Text, out Byte temp))
            {
                Globals.offset = temp;
            }
            else
            {
                Globals.offset = 133;
            }
        }

        #endregion

        #region Period Functions
        private void textBox_Period_TextChanged(object sender, EventArgs e)
        {
            if (UInt16.TryParse(textBox_Period.Text, out UInt16 temp))
            {
                Globals.Period = temp;
            }
            else
            {
                Globals.Period = 10000;
            }
        }
        private void button_period_Click(object sender, EventArgs e)
        {
            int FailedCounter = 0;
            OutputLabel.Text = ">> Sending Period Command...\n";
            while (!SendPeriod(Globals.Period))
            {
                FailedCounter++;
                if (FailedCounter >= 3)
                {
                    OutputLabel.Text += ">> Sending Period Signal Failed\n";
                    return;
                }
            }
            OutputLabel.Text = ">> Period Sent \n";
        }
        #endregion

        #region Scale Functions
        private void textBox_Scale_TextChanged(object sender, EventArgs e)
        {
            if (Byte.TryParse(textBox_Period.Text, out Byte temp))
            {
                Globals.scale = temp;
            }
            else
            {
                Globals.scale = 22;
            }
        }

        private void button_scale_Click(object sender, EventArgs e)
        {
            int FailedCounter = 0;
            OutputLabel.Text = ">> Sending Scale Command...\n";
            while (!SendScale(Globals.scale))
            {
                FailedCounter++;
                if (FailedCounter >= 3)
                {
                    OutputLabel.Text += ">> Sending Scale Signal Failed\n";
                    return;
                }
            }
        }
        #endregion

        private void comboBox_FilterSelection_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if(comboBox_FilterSelection.SelectedItem == "Custom")
            {
                textBox1.Enabled = true;
                textBox2.Enabled = true;
                textBox_Period.Enabled = true;
                comboBox_CutoffSelection.Enabled = false;
                checkBox_manual.Enabled = true;
            }
            else
            {
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                textBox_Period.Enabled = false;
                comboBox_CutoffSelection.Enabled = true;
                checkBox_manual.Enabled = false;
            }
        }

        private void checkBox_manual_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_manual.Checked)
            {
                button_offset.Enabled = true;
                button_period.Enabled = true;
                button_scale.Enabled = true;
            }
            else
            {
                button_offset.Enabled = false;
                button_period.Enabled = false;
                button_scale.Enabled = false;
            }
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            while (true)
            {

            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {

        }

        private void checkBox_graph_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox_graph.Checked)
            {
                int FailedCounter = 0;
                OutputLabel.Text = ">> Sending Write to GUI Command...\n";
                while (!Send_Write_to_GUI(1))
                {
                    FailedCounter++;
                    if (FailedCounter >= 3)
                    {
                        OutputLabel.Text += ">> Sending Write to GUi Signal Failed\n";
                        return;
                    }
                }
                OutputLabel.Text += ">> Plotting is Enabled\n";
                //Globals.Serial.DiscardInBuffer();
                Globals.Serial.DataReceived += DataRecieved;
                Globals.Serial.ReceivedBytesThreshold = 7;
               // backgroundWorker1.RunWorkerAsync();
            }
            else
            {
                //backgroundWorker1.CancelAsync();
                Globals.Serial.DataReceived -= DataRecieved;
                int FailedCounter = 0;
                OutputLabel.Text = ">> Sending Write to GUI Command...\n";
                while (!Send_Write_to_GUI(0))
                {
                    FailedCounter++;
                    if (FailedCounter >= 3)
                    {
                        OutputLabel.Text += ">> Sending Write to GUi Signal Failed\n";
                        return;
                    }
                }
                OutputLabel.Text += ">> Plotting is disabled\n";
            }
        }

        public void Append_Output(string value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(Append_Output), new object[] { value });
                return;
            }
            OutputLabel.Text += value;
        }

        public void Plot_Point(byte value1, byte value2)
        {
            if(InvokeRequired)
            {
                this.Invoke(new Action<byte,byte>(Plot_Point), new object[] { value1, value2 });
                return;
            }
            if(chart1.Series[0].Points.Count == 20)
            {
                chart1.Series[0].Points.Clear();
                chart1.Series[1].Points.Clear();
            }
            chart1.Series[0].Points.AddY(value1);
            chart1.Series[1].Points.AddY(value2);
            chart1.Update();
        }

        private void DataRecieved(object sender, EventArgs eventArgs)
        {
            HDLC_Rx DataRx = new HDLC_Rx();
            HDLC_Rx.DPA_RX_STATE state = new HDLC_Rx.DPA_RX_STATE();
            var now = System.DateTime.Now;
            DataRx = new HDLC_Rx();
            state = new HDLC_Rx.DPA_RX_STATE();

            while (state == HDLC_Rx.DPA_RX_STATE.DPA_RX_NOERR)
            {
                try
                {
                    state = DataRx.DPA_RX_Parse((byte)Globals.Serial.ReadByte());
                }
                catch (Exception)
                {
                    Append_Output(">> Test failed to parse data\n");
                    return;
                }
            }

            switch (state)
            {
                case HDLC_Rx.DPA_RX_STATE.DPA_RX_OK:
                    ChartValues.Add(new MeasureModel
                    {
                        DateTime = now,
                        Value = DataRx.Data[0]//Value to Plot
                    });

                    SetAxisLimits(now);

                    //lets only use the last 30 values
                    if (ChartValues.Count > 30) ChartValues.RemoveAt(0);
                    break;

                case HDLC_Rx.DPA_RX_STATE.DPA_RX_FE:
                    Append_Output(">> An error in the calibration data frame.\n"); return;
                case HDLC_Rx.DPA_RX_STATE.DPA_RX_CRCERR:
                    //success = false;
                    break;
            }
        }
    }
}
