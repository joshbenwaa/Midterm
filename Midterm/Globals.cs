using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Midterm
{

    /// <summary>
    /// Global variables used to store important data and to be useable across the GUI
    /// </summary>
    public class Globals
    {

        /// <summary>
        /// Serial COM Port for GUI
        /// </summary>
        public static string COMPort = "COM4";

        /// <summary>
        /// Serial Port object
        /// </summary>
        public static System.IO.Ports.SerialPort Serial = new System.IO.Ports.SerialPort();

        //Frequency Control//

        /// <summary>
        /// Start Frequency
        /// </summary>
        public static uint StartFreq = 10000;

        /// <summary>
        /// Freq Increment
        /// </summary>
        public static uint FreqInc = 10000;

        /// <summary>
        /// Number of Increments
        /// </summary>
        public static ushort NumOfInc = 2;

        /// <summary>
        /// Settling Time Cycles
        /// </summary>
        public static ushort SettlingCycles = 60;

        /// <summary>
        /// Calculated end freq
        /// </summary>
        public static uint EndFreq; //Not passed just calced


        //Output Voltage Variable//

        /// <summary>
        /// Output Voltage Variable
        /// </summary>
        public static byte OutputVoltage = 0x3;  //This is the outgoing one

        /// <summary>
        /// 2vpp  Const
        /// </summary>
        public const byte OutputVoltage2Vpp = 0x0;

        /// <summary>
        /// 200mvpp const
        /// </summary>
        public const byte OutputVoltage200mVpp = 0x1;

        /// <summary>
        /// 400mvpp const
        /// </summary>
        public const byte OutputVoltage400mVpp = 0x2;

        /// <summary>
        /// 1vpp const
        /// </summary>
        public const byte OutputVoltage1Vpp = 0x3;

        //Switching Variables//

        /// <summary>
        /// Tp switch Variable
        /// </summary>
        public static byte SwitchTp = 1;

        /// <summary>
        /// Tn switch variable
        /// </summary>
        public static byte SwitchTn = 7;

        /// <summary>
        /// A switch variable
        /// </summary>
        public static byte SwitchA = 3;

        /// <summary>
        /// B switch variable
        /// </summary>
        public static byte SwitchB = 5;

        /// <summary>
        /// Temperature variable
        /// </summary>
        public static float Temperature;

        /// <summary>
        /// Increments Completed Variable
        /// </summary>
        public static short IncCompleted = new short();

        /// <summary>
        /// timeout variable
        /// </summary>
        public static int timeout = new byte();

        /// <summary>
        /// Samples variable
        /// </summary>
        public static int Samples = 100;

    }


}
