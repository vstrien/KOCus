using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator
{
    class ConfigurationData
    {
        public ConfigurationData()
        {
            string PIDCodes = GetResourceTextFile("PIDCodes.xml");
            // Verwerk XML. 
            // Er komen soms dubbele waarden in voor i.v.m. uitgebreide beschrijving / uitleg. 
            // Kies in dat geval slechts de eerste waarde!
            string RequestCodes = GetResourceTextFile("RequestCodes.xml");
            string ResponseCodes = GetResourceTextFile("ResponseCodes.xml");

        }

        private string GetResourceTextFile(string filename)
        {
            string result = string.Empty;

            using (Stream stream = this.GetType().Assembly.
                       GetManifestResourceStream("Koos__OBD_Communicator." + filename))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }
    }

    class PIDCodes
    {
        public byte[] mode = new byte[2];
        public byte[] PID = new byte[2];
        public int dataLength = 0;
        public string description = "";
        public int ID;
    }

    class RequestCodes
    {
        public byte[] testmode = new byte[2];
        public string description;
    }

    class ResponseCodes
    {
        public string response;
        public string description;
    }
}
