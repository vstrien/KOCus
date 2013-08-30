using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Koos__OBD_Communicator;
using System.Xml;
using System.Xml.Linq;
namespace Koos__OBD_Communicator_Test.UnitTests
{
    [TestClass]
    public class ConfigurationInitTests
    {
        [TestMethod]
        [Description("Test if PID configuration is loaded at all")]
        public void TestInitPIDClass()
        {
            string xmlSource = "<PIDList>\r\n  <SensorAvailability Mode=\"01\">\r\n    <PIDSensor PID=\"00\" bytes=\"04\" firstPID=\"01\" Description=\"PIDs supported [01 - 20]\">\r\n    </PIDSensor>\r\n  </SensorAvailability>\r\n</PIDList>";
            XElement dSource = XElement.Parse(xmlSource, LoadOptions.None);


            Koos__OBD_Communicator.ConfigurationData testConfig = new Koos__OBD_Communicator.ConfigurationData(dSource);
            
            // There's only one availability mode in this example, so there is only one 'root' PID sensor
            Assert.IsTrue(testConfig.possibleSensors.Count == 1); 
            
            // The mode should be written to the key of the dict:
            Assert.IsTrue(testConfig.possibleSensors.First().Key == 1);

            var sensor = testConfig.possibleSensors.First().Value;
            // The properties should have been recorded correctly:
            Assert.IsTrue(sensor.PID == 0);
            Assert.IsTrue(sensor.bytes == 4);
            // Assert.IsTrue(sensor.firstPID == 1); --> this is filtered in the config loading: whithout children, mentioning the range of children is useless.
            Assert.IsTrue(sensor.description == "PIDs supported [01 - 20]");
            
            // The sensor shouldn't have any descendants
            Assert.IsTrue(sensor.PIDSensors.Count == 0);
        }

        [TestMethod]
        [Description("Test if PID configuration is loaded correctly")]
        public void TestInitPIDClassWithChildren()
        {
            string PID10 = "      <PIDSensor PID=\"10\" bytes=\"02\" Description=\"MAF air flow rate (grams / sec)\" Formula=\"((A*256)+B) / 100\" />\r\n";
            string PID11 = "      <PIDSensor PID=\"11\" bytes=\"01\" Description=\"Throttle position (%)\" Formula=\"A*100/255\" />\r\n";
            string PID00 = "    <PIDSensor PID=\"00\" bytes=\"04\" firstPID=\"01\" Description=\"PIDs supported [01 - 20]\">\r\n" + PID10 + PID11 + "    </PIDSensor>\r\n";
            string xmlSource = "<PIDList>\r\n  <SensorAvailability Mode=\"01\">\r\n" + PID00 + "  </SensorAvailability>\r\n</PIDList>";
            XElement dSource = XElement.Parse(xmlSource, LoadOptions.None);
            Koos__OBD_Communicator.ConfigurationData testConfig = new Koos__OBD_Communicator.ConfigurationData(dSource);

            // There's only one availability mode in this example, so there is only one 'root' PID sensor
            Assert.IsTrue(testConfig.possibleSensors.Count == 1);

            // The mode should be written to the key of the dict:
            Assert.IsTrue(testConfig.possibleSensors.First().Key == 1);

            var sensor = testConfig.possibleSensors.First().Value;
            // The properties of PID 0 should have been recorded correctly:
            Assert.IsTrue(sensor.PID == 0);
            Assert.IsTrue(sensor.bytes == 4);
            Assert.IsTrue(sensor.firstPID == 1);
            Assert.IsTrue(sensor.description == "PIDs supported [01 - 20]");

            // The sensor should have two descendants
            Assert.IsTrue(sensor.PIDSensors.Count == 2);
            
            // The descendants should have been loaded correctly
            var PIDSensor10 = sensor.PIDSensors.First();
            var PIDSensor11 = sensor.PIDSensors.Last();
            Assert.IsTrue(PIDSensor10.PID == 16); // 16 = 10 hex
            Assert.IsTrue(PIDSensor11.PID == 17); // 17 = 11 hex
            Assert.IsTrue(PIDSensor10.bytes == 2);
            Assert.IsTrue(PIDSensor11.bytes == 1);
            Assert.IsTrue(PIDSensor10.description == "MAF air flow rate (grams / sec)");
            Assert.IsTrue(PIDSensor11.description == "Throttle position (%)");
            Assert.IsTrue(PIDSensor10.formula == "((A*256)+B) / 100");
            Assert.IsTrue(PIDSensor11.formula == "A*100/255");
            Assert.IsTrue(PIDSensor10.highestFormulaCharacter == 'B');
            Assert.IsTrue(PIDSensor11.highestFormulaCharacter == 'A');
            Assert.IsTrue(PIDSensor10.highestFormulaCharacterNumber == 1);
            Assert.IsTrue(PIDSensor11.highestFormulaCharacterNumber == 0);
        }


        [TestMethod]
        [Description("Test if configured PIDs can be found correctly")]
        public void testPIDSearch()
        {
            string PID10 = "      <PIDSensor PID=\"10\" bytes=\"02\" Description=\"MAF air flow rate (grams / sec)\" Formula=\"((A*256)+B) / 100\" />\r\n";
            string PID11 = "      <PIDSensor PID=\"11\" bytes=\"01\" Description=\"Throttle position (%)\" Formula=\"A*100/255\" />\r\n";
            string PID00 = "    <PIDSensor PID=\"00\" bytes=\"04\" firstPID=\"01\" Description=\"PIDs supported [01 - 20]\">\r\n" + PID10 + PID11 + "    </PIDSensor>\r\n";
            string xmlSource = "<PIDList>\r\n  <SensorAvailability Mode=\"01\">\r\n" + PID00 + "  </SensorAvailability>\r\n</PIDList>";
            XElement dSource = XElement.Parse(xmlSource, LoadOptions.None);
            Koos__OBD_Communicator.ConfigurationData testConfig = new Koos__OBD_Communicator.ConfigurationData(dSource);

            var sensor = testConfig.possibleSensors.First().Value;
            var PIDSensor10 = sensor.PIDSensors.First();
            var PIDSensor11 = sensor.PIDSensors.Last();

            Assert.IsTrue(testConfig.GetSensor(1, 0) == sensor);
            Assert.IsTrue(testConfig.GetSensor(1, 16) == PIDSensor10);
            Assert.IsTrue(testConfig.GetSensor(1, 17) == PIDSensor11);
        }

        [TestMethod]
        [Description("Test behavior when a configured PID can't be found")]
        [ExpectedException(typeof(ArgumentOutOfRangeException), "PID")]
        public void testPIDNotFound()
        {
            string PID00 = "    <PIDSensor PID=\"00\" bytes=\"04\" firstPID=\"01\" Description=\"PIDs supported [01 - 20]\">\r\n    </PIDSensor>\r\n";
            string xmlSource = "<PIDList>\r\n  <SensorAvailability Mode=\"01\">\r\n" + PID00 + "  </SensorAvailability>\r\n</PIDList>";
            XElement dSource = XElement.Parse(xmlSource, LoadOptions.None);
            Koos__OBD_Communicator.ConfigurationData testConfig = new Koos__OBD_Communicator.ConfigurationData(dSource);

            testConfig.GetSensor(1, 16);
        }

        [TestMethod]
        [Description("Test behavior when a configured PID can't be found")]
        [ExpectedException(typeof(ArgumentOutOfRangeException), "Mode")]
        public void testModeNotFound()
        {
            string PID00 = "    <PIDSensor PID=\"00\" bytes=\"04\" firstPID=\"01\" Description=\"PIDs supported [01 - 20]\">\r\n    </PIDSensor>\r\n";
            string xmlSource = "<PIDList>\r\n  <SensorAvailability Mode=\"01\">\r\n" + PID00 + "  </SensorAvailability>\r\n</PIDList>";
            XElement dSource = XElement.Parse(xmlSource, LoadOptions.None);
            Koos__OBD_Communicator.ConfigurationData testConfig = new Koos__OBD_Communicator.ConfigurationData(dSource);

            testConfig.GetSensor(15, 16);
        }
    }
}
