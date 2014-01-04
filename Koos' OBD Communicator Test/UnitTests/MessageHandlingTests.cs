using Koos__OBD_Communicator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Koos__OBD_Communicator_Test
{
    [TestClass]
    public class MessageHandlingTests
    {
        [TestMethod]
        [Description("Test if return message cleansed the right way")]
        public void TestCleansingOfReturnMessage()
        {
            string supportedPIDs0_20_return = "7E8 06 41 20 A0 07 B0 11 \r\r>";
            string returnMessage_cleansed = "7E8064120A007B011";
            Assert.IsTrue(Koos__OBD_Communicator.Message.cleanReponse(supportedPIDs0_20_return) == returnMessage_cleansed);
        }

        [TestMethod]
        [Description("Test if return message is recognized with the right number of bytes")]
        public void TestRecognitionOfReturnMessage()
        {
            string returnMessage_cleansed = "7E8064120A007B011";
            int numberOfBytes = 6;

            Assert.IsTrue(Koos__OBD_Communicator.Message.getBytesInMessage(returnMessage_cleansed) == numberOfBytes);
        }

        [TestMethod]
        [Description("Test if message body is extracted rightly")]
        public void TestExtractionOfMessageBody()
        {
            string returnMessage_cleansed = "7E8064120A007B011";
            string returnMessage_body = "4120A007B011";

            // test if the message body is defined correctly:
            Assert.IsTrue(Koos__OBD_Communicator.Message.getBytesInMessage(returnMessage_cleansed) * 2 == returnMessage_body.Length);
            // test if the message body is extracted correctly:
            Assert.IsTrue(Koos__OBD_Communicator.Message.getMessageContents(returnMessage_cleansed) == returnMessage_body);
        }

        [TestMethod]
        [Description("Test if valid return message is not mentioned with errors")]
        public void TestValidationOfValidReturnMessage()
        {
            string returnMessage_cleansed = "7E8064120A007B011";

            Assert.IsTrue(Koos__OBD_Communicator.Message.validHeader(returnMessage_cleansed));
            Assert.IsTrue(Koos__OBD_Communicator.Message.validSize(returnMessage_cleansed));
        }

        [TestMethod]
        [Description("Test if return message with invalid number of bytes is recognized as invalid")]
        public void TestValidationOfReturnMessageWrongSize()
        {
            string returnMessage_cleansed = "7E8074120A007B011";

            Assert.IsFalse(Koos__OBD_Communicator.Message.validSize(returnMessage_cleansed));
        }

        [TestMethod]
        [Description("Test if return message with wrong header is recognized as invalid")]
        public void TestValidationOfReturnMessageWrongHeader()
        {
            string returnMessage_cleansed_headerTooShort = "E8064120A007B011";
            string returnMessage_cleansed_headerInvalid = "8E8064120A007B011";

            Assert.IsFalse(Koos__OBD_Communicator.Message.validHeader(returnMessage_cleansed_headerTooShort));
            Assert.IsFalse(Koos__OBD_Communicator.Message.validHeader(returnMessage_cleansed_headerInvalid));
        }

        [TestMethod]
        [Description("Test if valid return message with newlines doesn't throw errors")]
        public void TestNoErrorReturnMessage()
        {
            int mode = 01;
            int pid = 00;
            string returnMessage = "7E8 06 41 00 BE 3F A8 11 \r\n\r\n>";

            PIDSensor testPID = new PIDSensor(mode, pid, 2, null, "no desc");

            testPID.parseResponse(returnMessage);
        }

        [TestMethod]
        [Description("Test if PID sensor number is recognized correctly")]
        public void TestDecodingOfPID()
        {
            string returnMessage_cleansed = "7E8064120A007B011";
            Assert.IsTrue(Message.getPIDOfMessage(returnMessage_cleansed) == 0x20);
        }

        //[TestMethod]
        //[Description("Test if request mode is recognized correctly")]
        //public void TestDecodingOfPID()
        //{
        //    string returnMessage_cleansed = "7E8064120A007B011";
        //    Assert.IsTrue(Message.getModeOfMessage(returnMessage_cleansed) == 0x01);
        //}

        [TestMethod]
        [Description("Test if valid return message with PID sensors is decoded correctly")]
        public void TestDecodingOfReturnMessage()
        {
            bool[] supported_shouldbe = new bool[] { true, false, true, false, false, false, false, false, false, false, false, false, false, true, true, true, true, false, true, true, false, false, false, false, false, false, false, true, false, false, false, true };
            int firstPID = 0x21;
            int lastPID = 0x40;
            int mode = 1;
            int base_pid = 0x20;
            //                               7E8064100BE 3F A8 11 
            string returnMessage_cleansed = "7E8064120A007B011";
            // A0 07 B0 11 = 1010 0000 0000 0111 1011 0000 0001 0001
            
            string PID00 = "    <PIDSensor PID=\"" + base_pid.ToString("X") + "\" bytes=\"04\" firstPID=\"" + firstPID.ToString("X") + "\" Description=\"PIDs supported [01 - 20]\">\r\n";
            for (int nPID = firstPID; nPID <= lastPID; nPID++)
            {
                PID00 += "      <PIDSensor PID=\"" + nPID.ToString("X") + "\" bytes=\"02\" Description=\"Testje\" Formula=\"((A*256)+B) / 100\" />\r\n";
            }
            PID00 += "    </PIDSensor>\r\n";
            string xmlSource = "<PIDList>\r\n  <SensorAvailability Mode=\"01\">\r\n" + PID00 + "  </SensorAvailability>\r\n</PIDList>";

            var configTest = new ConfigurationData(XElement.Parse(xmlSource, LoadOptions.None));

            configTest.parseOBDResponse(returnMessage_cleansed);

            var availableSensors = configTest.availableSensors();

            for(int nPID = firstPID; nPID <= lastPID; nPID++)
            {
                var currentSensor = configTest.GetSensor(mode, nPID);
                if(supported_shouldbe[nPID - firstPID])
                {
                    // First, the availability on the sensor should be set to true
                    Assert.IsTrue(currentSensor.isAvailable);
                    // Secondly, the sensor should show up in the availability list
                    Assert.IsTrue(availableSensors.Contains(currentSensor));
                }
                else
                {
                    // First, the availability on the sensor should be set to false
                    Assert.IsFalse(currentSensor.isAvailable);

                    // Secondly, the sensor should not show up in the availability list
                    Assert.IsFalse(availableSensors.Contains(currentSensor));
                }
            }
        }
    }
}
