using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator
{
    public class ResponseEventArgs : EventArgs
    {
        public ResponseEventArgs(string message)
        {
            this.message = message;
        }

        public string message { get; private set; }
    }
}
