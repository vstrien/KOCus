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
    }
}
