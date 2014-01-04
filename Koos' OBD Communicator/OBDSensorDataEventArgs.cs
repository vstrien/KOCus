using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator
{
    public class OBDSensorDataEventArgs : EventArgs
    {
        public OBDSensorDataEventArgs(int mode, int PIDCode, int length, string message, double value)
        {
            this.mode = mode;
            this.PIDCode = PIDCode;
            this.length = length;
            this.message = message;
            this.value = value;
        }

        public int mode { get; private set; }
        public int PIDCode { get; private set; }
        public int length { get; private set; }
        public string message { get; private set; }
        public double value { get; private set; }
    }
}
