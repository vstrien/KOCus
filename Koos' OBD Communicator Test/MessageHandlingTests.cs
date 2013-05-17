using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator_Test
{
    [TestClass]
    public class MessageHandlingTests
    {
        [TestMethod]
        [Description("Test if PID communication class initially has no information about the availability of PIDs")]
        public void TestInitPIDClass()
        {
            Koos__OBD_Communicator.PID testPID = new Koos__OBD_Communicator.PID();
            for (int mode = 0; mode < Koos__OBD_Communicator.PID.defaultNumberOfModes; mode++)
            {
                for (int nPID = 0; nPID < Koos__OBD_Communicator.PID.defaultNumberOfPIDsPerMode; nPID++)
                {
                    Assert.IsTrue(testPID.isSupported(mode + 1, nPID) == Koos__OBD_Communicator.PID.SupportedStatus.Unknown);
                }
            }
        }

        [TestMethod]
        [Description("Test if return message cleansed the right way")]
        public void TestCleansingOfReturnMessage()
        {
            string supportedPIDs0_20_return = "7E8 06 41 20 A0 07 B0 11 \r\r>";
            string returnMessage_cleansed = "7E8064120A007B011";
            Assert.IsTrue(Koos__OBD_Communicator.ByteEncodedMessages.cleanReponse(supportedPIDs0_20_return) == returnMessage_cleansed);
        }

        [TestMethod]
        [Description("Test if return message is recognized with the right number of bytes")]
        public void TestRecognitionOfReturnMessage()
        {
            string returnMessage_cleansed = "7E8064120A007B011";

            Assert.IsTrue(Koos__OBD_Communicator.ByteEncodedMessages.getBytesInMessage(returnMessage_cleansed) == 6);
        }


        [TestMethod]
        [Description("Test if valid return message is not mentioned with errors")]
        public void TestValidationOfReturnMessage()
        {
            string returnMessage_cleansed = "7E8064120A007B011";

            Assert.IsTrue(Koos__OBD_Communicator.ByteEncodedMessages.validHeader(returnMessage_cleansed));
            Assert.IsTrue(Koos__OBD_Communicator.ByteEncodedMessages.validSize(returnMessage_cleansed));
        }


        [TestMethod]
        [Description("Test if valid return message with PID sensors is decoded correctly")]
        public void TestDecodingOfReturnMessage()
        {
            string returnMessage_cleansed = "7E8064120A007B011";

            bool[] supported_shouldbe = new bool[] { true, false, true, false, false, false, false, false, false, false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, false, false, false, true, false, false, false, true };
            int firstPID = 0x21;
            int lastPID = 0x40;
            int mode = 1;

            Koos__OBD_Communicator.PID testPID = new Koos__OBD_Communicator.PID();
            Assert.IsTrue(testPID.parseSupportedPIDs(mode, firstPID, lastPID, returnMessage_cleansed));

            for(int nPID = firstPID; nPID <= lastPID; nPID++)
            {
                if(supported_shouldbe[nPID - firstPID])
                {
                    Assert.IsTrue(testPID.isSupported(mode, nPID) == Koos__OBD_Communicator.PID.SupportedStatus.Supported);
                }
                else
                {
                    Assert.IsTrue(testPID.isSupported(mode, nPID) == Koos__OBD_Communicator.PID.SupportedStatus.Unsupported);
                }
            }
        }
    }
}
