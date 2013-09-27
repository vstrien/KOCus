using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Data;
using System.Reflection;

namespace Koos__OBD_Communicator
{
    public class ConfigurationData
    {
        public Dictionary<int, PIDSensor> possibleSensors = new Dictionary<int, PIDSensor>();
        public event EventHandler<OBDSensorDataEventArgs> RaiseOBDSensorData;

        private void OnRaiseOBDSensorData(object sender, OBDSensorDataEventArgs e)
        {
            EventHandler<OBDSensorDataEventArgs> handler = RaiseOBDSensorData;

            // Only execute if there are any subscribers
            if (handler != null)
            {
                handler(sender, e);
            }
        }
        
        public ConfigurationData() : this(XElement.Load(Assembly.GetExecutingAssembly().GetManifestResourceStream("Koos__OBD_Communicator.PIDCodes.xml")))
        {   
        }

        /// <summary>
        /// Reads XML file with PID configuration, and loads it into a PIDSensor data structure.
        /// Parse XML PID codes
        /// The XML file can be structured as follows:
        ///  First level: &lt;PIDList&gt; element
        ///   Second level: &lt;SensorAvailability Mode="xx"&gt; element (1 or more)
        ///    Third level: &lt;PIDSensor elements&gt;
        ///     Third+ level: &lt;PIDSensor elements&gt;
        /// In any level n+1 where n &gt; 2, the direct parent PID described the availability of child PIDs.
        /// &lt;PIDList&gt;
        ///   &lt;SensorAvailability Mode="01"&gt;
        ///     &lt;PIDSensor PID="00" bytes="04" firstPID="01" Description="PIDs supported [01 - 20]"&gt;
        ///       &lt;PIDSensor PID="01" bytes="04" Description="Monitor status since DTCs cleared. (Includes malfunction indicator lamp (MIL) status and number of DTCs.)" /&gt;
        ///       &lt;PIDSensor PID="20" bytes="04" firstPID="21" Description="PIDs supported [21 - 40]"&gt;
        ///         &lt;PIDSensor PID="2F" bytes="04" Description="Fuel level input" Formula="A*100/255" /&gt;
        ///         &lt;PIDSensor PID="33" bytes="01" Description="Barometric pressure" Formula="A" /&gt;
        ///         &lt;PIDSensor PID="40" bytes="04" firstPID="41" Description="PIDs supported [41 - 60]"&gt;
        ///           &lt;PIDSensor PID="46" bytes="01" Description="Ambient Air temperature" Formula="A-40" /&gt;
        ///           &lt;PIDSensor PID="5C" bytes="01" Description="Engine Oil temperature" Formula="A-40" /&gt;
        ///           &lt;PIDSensor PID="60" bytes="04" firstPID="61" Description="PIDs supported [61 - 80]"&gt;
        ///             &lt;PIDSensor PID="61" bytes="01" Description="Driver's demand engine - percent torque" Formula="A-125" /&gt;
        ///           &lt;/PIDSensor&gt;
        ///         &lt;/PIDSensor&gt;
        ///       &lt;/PIDSensor&gt;
        ///     &lt;/PIDSensor&gt;
        ///   &lt;/SensorAvailability&gt;
        /// &lt;/PIDList&gt;
        /// </summary>
        /// <param name="PIDList">Contents of XML configuration file</param>
        public ConfigurationData(XElement PIDList)
        {
            // Inside <PIDList> live <SensorAvailability> nodes, so this loop will open all <sensorAvailability> tags one by one.
            foreach (var possibleSensorList in PIDList.Elements())
            {
                // Parse the 'Mode' attribute from the <SensorAvailability> tag
                var Mode = int.Parse(possibleSensorList.Attribute("Mode").Value.ToString());

                // Get the element at the first level (a <PIDList> element which usually contains other elements)
                var SensorAvailability = possibleSensorList.Elements().First();

                // Start parsing the root element and all nodes beneath
                possibleSensors.Add(Mode, this.parsePossibleSensorList(SensorAvailability, Mode));
                // By default, all sensors are set to 'not available'. Set the rood node to 'available'.
                possibleSensors.First().Value.isAvailable = true;
            }
        }

        /// <summary>
        /// Parse all elements inside a &lt;PIDSensor&gt;-element (level 3 or deeper)
        /// </summary>
        /// <param name="xmlNodes">XML element containing the tree of PIDSensor-data</param>
        /// <param name="Mode">ELM mode in which this element resides.</param>
        /// <returns></returns>
        public PIDSensor parsePossibleSensorList(XElement xmlTree, int Mode, PIDSensor parent = null)
        {
            var PID = int.Parse(xmlTree.Attribute("PID").Value.ToString(), NumberStyles.HexNumber);
            var bytes = int.Parse(xmlTree.Attribute("bytes").Value.ToString(), NumberStyles.HexNumber);
            var description = xmlTree.Attribute("Description").Value.ToString();
            
            // Formula attribute can be empty, so we first need to check it before parsing.
            var s_formulaAttribute = xmlTree.Attribute("Formula");
            PIDSensor currentSensor;
            if (s_formulaAttribute == null)
                currentSensor = new PIDSensor(Mode, PID, bytes, parent, description);
            else
                currentSensor = new PIDSensor(Mode, PID, bytes, parent, description, s_formulaAttribute.Value.ToString());

            if (xmlTree.HasElements)
            {
                // The PID node has children, so it should have an indication what the id's of that children are.
                currentSensor.firstPID = int.Parse(xmlTree.Attribute("firstPID").Value.ToString(), NumberStyles.HexNumber);
            }

            foreach (var childNode in xmlTree.Elements())
            {
                currentSensor.PIDSensors.Add(parsePossibleSensorList(childNode, Mode, currentSensor));
            }

            // subscribe to the current sensor
            currentSensor.RaiseOBDSensorData += OnRaiseOBDSensorData;

            return currentSensor;
        }

        public List<PIDSensor> availableSensors()
        {
            List<PIDSensor> returnList = new List<PIDSensor>();
            foreach (KeyValuePair<int, PIDSensor> modePID in this.possibleSensors)
            {
                returnList.AddRange(modePID.Value.availableSensors());
            }
            return returnList;
        }

        public PIDSensor GetSensor(int mode, int PID)
        {
            foreach(KeyValuePair<int, PIDSensor> modePID in this.possibleSensors)
            {
                if (modePID.Key == mode)
                {
                    var queryResult = modePID.Value.GetPID(PID);
                    if(queryResult == null)
                        throw new ArgumentOutOfRangeException("PID");
                    else
                        return queryResult;
                }
            }

            throw new ArgumentOutOfRangeException("mode");
        }

        public void parseOBDResponse(string response)
        {
            var targetSensor = this.possibleSensors.First();
            targetSensor.Value.parseResponse(response);
        }
    
    }
}
