﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Koos__OBD_Communicator
{
    class ConfigurationData
    {
        public SensorAvailability[] sensorLists;

        public ConfigurationData()
        {
            // Verwerk XML PID codes
            //using (Stream stream = this.GetType().Assembly.
            //           GetManifestResourceStream("Koos__OBD_Communicator.PIDCodes.xml"))
            //{
            
            var PIDList = XElement.Load(this.GetType().Assembly.GetManifestResourceStream("Koos__OBD_Communicator.PIDCodes.xml"));
            this.sensorLists = new SensorAvailability[PIDList.Elements().Count()];
            int currentSensor = 0;

            foreach (var availableSensors in PIDList.Elements())
            {
                var Mode = availableSensors.Attribute("Mode").Value.ToString();
                var PID = availableSensors.Attribute("PID").Value.ToString();
                var bytes = availableSensors.Attribute("bytes").Value.ToString();
                var firstPID = availableSensors.Attribute("firstPID").Value.ToString();
                var lastPID = availableSensors.Attribute("lastPID").Value.ToString();
                var description = availableSensors.Attribute("Description").Value.ToString();
                sensorLists[currentSensor] = new SensorAvailability(Mode, PID, bytes, firstPID, lastPID, description);

                var AvailablePIDSensors = availableSensors.Elements().First();
                foreach (var PIDSensor in AvailablePIDSensors.Elements())
                {
                    var s_PID = PIDSensor.Attribute("PID").Value.ToString();
                    var s_bytes = PIDSensor.Attribute("bytes").Value.ToString();
                    var s_description = PIDSensor.Attribute("Description").Value.ToString();
                    
                    var s_formulaAttribute = PIDSensor.Attribute("Formula");
                    if (s_formulaAttribute == null)
                    {
                        sensorLists[currentSensor].addPID(s_PID, s_bytes, s_description);
                    }
                    else
                    {
                        var s_formula = s_formulaAttribute.Value.ToString();
                        sensorLists[currentSensor].addPID(s_PID, s_bytes, s_description, s_formula);
                    }
                }

                currentSensor += 1;
            }

        }
    }

    public class SensorAvailability
    {
        public int mode, PID, bytes, firstPID, lastPID;
        public string description;
        public PIDSensor[] PIDSensors;

        public SensorAvailability(int mode, int PID, int bytes, int firstPID, int lastPID, string description)
        {
            this.mode = mode;
            this.PID = PID;
            this.bytes = bytes;
            this.description = description;
            this.firstPID = firstPID;
            this.lastPID = lastPID;
            this.PIDSensors = new PIDSensor[lastPID - firstPID];
        }

        public SensorAvailability(string mode, string PID, string bytes, string firstPID, string lastPID, string description) :
            this(int.Parse(mode, NumberStyles.HexNumber),
                int.Parse(PID, NumberStyles.HexNumber),
                int.Parse(bytes, NumberStyles.HexNumber),
                int.Parse(firstPID, NumberStyles.HexNumber),
                int.Parse(lastPID, NumberStyles.HexNumber),
                description) { }

        public void addPID(int PID, int bytes, string description, string formula = "")
        {
            if (PID > lastPID || PID < firstPID)
            {
                throw new ArgumentOutOfRangeException("PID is not in range of this SensorClass");
            }

            this.PIDSensors[PID - firstPID] = new PIDSensor(mode, PID, bytes, this, description, formula);
        }

        internal void addPID(string s_PID, string s_bytes, string s_description, string s_formula = "")
        {
            this.addPID(int.Parse(s_PID, NumberStyles.HexNumber),
                int.Parse(s_bytes, NumberStyles.HexNumber),
                s_description,
                s_formula);
        }
    }

    public class PIDSensor
    {
        public int mode, PID, bytes;
        public string description, formula;
        public SensorAvailability parent;

        public PIDSensor(int mode, int PID, int bytes, SensorAvailability parent, string description, string formula = "")
        {
            this.mode = mode;
            this.PID = PID;
            this.bytes = bytes;
            this.description = description;
            this.formula = formula;
            this.parent = parent;
        }
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
