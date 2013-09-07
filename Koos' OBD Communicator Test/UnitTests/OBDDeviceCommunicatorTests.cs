using Koos__OBD_Communicator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator_Test.UnitTests
{
    [TestClass]
    public class OBDDeviceCommunicatorTests
    {

        [TestMethod]
        [Description("Test if messages without body are handled correctly")]
        public void TestPartiallyValidMessage()
        {
            string PIDreturn = "STOPPED\r\r>7E8 03 7F 10 13 \r7E8 03 41 04 1B \r7E8 03 41 11 26 \r\r>STOPPED\r\r>7E8 03 7F 11 13 ";
            OBDDeviceCommunicatorAsync obd = new OBDDeviceCommunicatorAsync(null, new ConfigurationData());

            obd.cleanAndHandleResponses(PIDreturn); // shouldn't raise an error
        }

        [TestMethod]
        [Description("Test if, in case of multiple messages, invalid messages do not obscure valid ones")]
        public void TestMultipleMessagesMixed()
        {
            string PIDreturn = "ELM327 v1.5\r\n\r\n>01 00\r\n41 00 BE 3F A8 11 \r\n\r\n>";
            OBDDeviceCommunicatorAsync obd = new OBDDeviceCommunicatorAsync(null, new ConfigurationData());

            obd.cleanAndHandleResponses(PIDreturn); // shouldn't raise an error
        }

        [TestMethod]
        [Description("Test if the message 'new data arrived' is risen within one second after an event has happened")]
        public void TestNewDataSubscriber()
        {
            ConfigurationData config = new ConfigurationData();
            OBDDeviceCommunicatorAsync obd = new OBDDeviceCommunicatorAsync(null, config);
            

            string PIDreturn_engineLoad = "7E8 03 41 04 1B";
            // 0x03 bytes, mode (0x41 - 0x40) = 0x01, PID 0x04, message 0x1B = 0d27
            // betekenis: Calculated engine load value (%). Formule: A*100/255 = 27*100/255 = 10%
            string expectedAnswer = "10";
            int mode = 0x01;
            int sensor = 0x04;

            bool answered = false;
            string answer = "";
            config.GetSensor(mode, sensor).RaiseOBDSensorData += (object sender, OBDSensorDataEventArgs s) =>
            {
                answer = s.value;
                answered = true;
            };
            obd.cleanAndHandleResponses(PIDreturn_engineLoad);
            DateTime startWaiting = DateTime.Now;
            while (!answered && DateTime.Now > startWaiting.AddSeconds(1)) ;
            Assert.IsTrue(answered, "Event didn't raise within one second");
            Assert.IsTrue(answer == expectedAnswer, "Event value incorrect: is %s, should be %s", answer, expectedAnswer);
          
            

            // string PIDreturn_throttlePosition = "7E8 03 41 04 1B";
            // 0x03 bytes, mode (0x41 - 0x40) = 0x01, PID 0x11 = 0d17, message 0x26 = 0d38
            // betekenis: Throttle position (%). Formule: A*100/255 = 38*100/255 = 7%
            
        }
    }
}
