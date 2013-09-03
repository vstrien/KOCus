using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator
{
    /// <summary>
    /// Class to store a PID Sensor
    /// Because of the ELM configuration, even the 'which sensors are available' PIDs are PIDs themselves.
    /// Everything can therefor be covered beneath one root node, and a PID sensor can contain other PID sensors.
    /// This class is meant to store this tree.
    /// </summary>
    public class PIDSensor
    {
        public int mode, PID, bytes;
        public string description, formula;
        public PIDSensor parent;
        public char? highestFormulaCharacter;
        public int highestFormulaCharacterNumber;
        public static char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
        public int firstPID, lastPID;
        public bool isAvailable = false;
        public List<PIDSensor> PIDSensors = new List<PIDSensor>();
        public event EventHandler<OBDSensorDataEventArgs> RaiseOBDSensorData;
        FormulaEvaluation.Eval expressionParser = new FormulaEvaluation.Eval();

        protected virtual void OnRaiseOBDSensorData(OBDSensorDataEventArgs eventArgs)
        {
            EventHandler<OBDSensorDataEventArgs> handler = RaiseOBDSensorData;

            // Only execute if there are any subscribers
            if (handler != null)
            {
                handler(this, eventArgs);
            }
        }


        public PIDSensor(int mode, int PID, int bytes, PIDSensor parent, string description, string formula = "")
        {
            this.mode = mode;
            this.PID = PID;
            this.bytes = bytes;
            this.description = description;
            this.formula = formula.ToUpper();
            this.parent = parent;

            // The 'formula' field in the XML file can be filled with characters and calculation symbols
            // To determine the number of arguments needed, the highest used character in the formula will be stored.
            this.highestFormulaCharacter = GetHighestFormulaCharacterFrom(formula);
            if (this.highestFormulaCharacter == null)
                this.highestFormulaCharacterNumber = 0;
            else if (this.highestFormulaCharacter != null)
            {
                char character = (char)highestFormulaCharacter;
                this.highestFormulaCharacterNumber = Array.IndexOf(alphabet, character, 0, 26);
            }
        }

        public List<PIDSensor> availableSensors()
        {
            var availableSensors = new List<PIDSensor>();

            if (this.isAvailable)
                availableSensors.Add(this);

            if (this.isAvailable && this.PIDSensors.Count > 0)
            {
                // add all available children to the availableSensors-list.
                foreach (PIDSensor child in PIDSensors)
                    availableSensors.AddRange(child.availableSensors());

            }

            return availableSensors;
        }

        public PIDSensor GetPID(int searchForPID)
        {
            if (searchForPID == this.PID)
            {
                return this;
            }
            // Performance can be better by using the 'firstPID' attribute
            else if (PIDSensors.Count > 0)
            {
                foreach (PIDSensor child in PIDSensors)
                {
                    var childQueryResult = child.GetPID(searchForPID);
                    if (childQueryResult != null)
                        return childQueryResult;
                }
            }
            return null;
        }

        /// <summary>
        /// Updates availability based on a response from the car's OBD system
        /// </summary>
        /// <param name="response">the uncleaned, unfiltered response from the car</param>
        public void parseResponse(string response)
        {

            string cleanedResponse = Message.cleanReponse(response);

            // get PID from message
            int msgPID = Message.getPIDOfMessage(cleanedResponse);
            string OBDMessageContents = Message.getMessageContents(response);

            this.parseResponse(msgPID, OBDMessageContents);
        }

        /// <summary>
        /// Parses response from the car's OBD system
        /// </summary>
        /// <param name="msgPID">The PID number for which the returned message is meant</param>
        /// <param name="OBDMessageContents">The message, as returned by the Message helper class</param>
        private void parseResponse(int msgPID, string OBDMessageContents)
        {
            if (msgPID == this.PID)
            {
                // The message is meant for the current PID. 

                // Check the type of PID this is:
                if (this.PIDSensors.Count > 0) // availability PID
                    updateAvailability(OBDMessageContents);
                else if (this.highestFormulaCharacterNumber > 0) // formula PID
                {
                    this.OnRaiseOBDSensorData(new OBDSensorDataEventArgs(
                            this.mode,
                            PID,
                            this.bytes,
                            OBDMessageContents,
                            parseFormula(OBDMessageContents).ToString())
                    );
                }
                else
                {
                    // Nothing yet. Bit-encoded sensor is available, but we don't have the means to parse it (yet).
                } 
            }
            else
            {
                foreach (var child in this.PIDSensors)
                {
                    // Would be better to tighten the scope here, only addressing possible targets.
                    child.parseResponse(msgPID, OBDMessageContents);
                }
            }
        }

        /// <summary>
        /// Parses the formula of the current PID, filling in the arguments as provided by the car's OBD system
        /// </summary>
        /// <param name="OBDMessageContents">OBD response containing the arguments for the formula</param>
        /// <returns></returns>
        private double parseFormula(string OBDMessageContents)
        {
            int bytesInMessage = Message.getBytesInMessage(OBDMessageContents);

            if (this.formula != "" && this.highestFormulaCharacterNumber > bytesInMessage)
                throw new Exception("Not all values are filled in the formula");
            else if (this.formula != "" && this.highestFormulaCharacterNumber <= bytesInMessage)
            {
                // vul formule in, A = byte 0, B = byte 1, C = byte 2, etc.
                return this.TranslateSensorValues(OBDMessageContents);
            }
            else
            {
                throw new Exception("No formula present");
            }
        }

        /// <summary>
        /// Helper code to execute a string formula. Replaces characters in the formula with values, then calculates the outcome.
        /// </summary>
        /// <param name="OBDMessageContents"></param>
        /// <returns></returns>
        private double TranslateSensorValues(string OBDMessageContents)
        {
            string completedFormula = this.formula;

            for (int c = 0; c <= this.highestFormulaCharacterNumber; c++)
            {
                completedFormula = completedFormula.Replace(PIDSensor.alphabet[c].ToString(), int.Parse(OBDMessageContents.Substring(c * 2, 2), NumberStyles.HexNumber).ToString());
            }

            return this.expressionParser.Execute(completedFormula);
        }

        /// <summary>
        /// Updates availability based on a clean response from the car's OBD system, as well as a PID number where this information should be stored.
        /// </summary>
        /// <param name="supportedSensors"></param>
        private void updateAvailability(string supportedSensors)
        {
            // If this PID is an 'availability' PID, it should have a 'start' number of the first PID available here.
            ulong u_startPID = (ulong)this.firstPID;
            ulong u_endPID = u_startPID + 31;

            UInt64 nHex = UInt64.Parse(supportedSensors, NumberStyles.HexNumber);
            for (ulong currentPID_absolute = u_startPID; currentPID_absolute <= u_endPID; currentPID_absolute++)
            {
                ulong currentPID_relative = u_endPID - currentPID_absolute;
                ulong currentPID_bit = (ulong)Math.Pow(2, currentPID_relative);

                var result = this.PIDSensors.Where(sensor => ((ulong)sensor.PID == currentPID_absolute));
                if ((nHex & currentPID_bit) == currentPID_bit && result.Count() == 1)
                {
                    result.First().isAvailable = true;
                }
                else if (result.Count() == 1)
                {
                    result.First().isAvailable = false;
                }
            }
        }

        /// <summary>
        /// Looks into a formula to determine how many arguments are inside.
        /// The highest character should represent the number of arguments.
        /// </summary>
        /// <param name="formula">Formula to inspect</param>
        /// <returns>the 'highest' character, A = lowest, Z is highest)</returns>
        private char? GetHighestFormulaCharacterFrom(string formula)
        {
            char? highestChar = null;
            foreach (char character in alphabet)
            {
                if (formula.Contains(character))
                    highestChar = character;
            }
            return highestChar;
        }
    }

}
