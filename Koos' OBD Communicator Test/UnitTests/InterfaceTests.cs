using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator_Test.UnitTests
{
    [TestClass]
    public class InterfaceTests
    {
        [TestMethod]
        [Description("Check to see if Mainpage.xaml gets instantiated")]
        public void MainPageTest()
        {
            Koos__OBD_Communicator.MainPage MPage = new Koos__OBD_Communicator.MainPage();
            Assert.IsNotNull(MPage);
        }

        [TestMethod]
        [Description("Check if PID without formula doesn't throw errors")]
        public void EmptyFormulaTest()
        {
            Koos__OBD_Communicator.MainPage MPage = new Koos__OBD_Communicator.MainPage();
            MPage.configData.parseOBDResponse("7E8 01 01 04 00 07 E1 00");
        }

        [TestMethod]
        [Description("Check if PID with formula is displayed correctly on the main screen within one second")]
        public void FilledFormulaeTest()
        {
            string PIDreturn_engineLoad = "7E8 03 41 04 FF";
            // 0x03 bytes, mode (0x41 - 0x40) = 0x01, PID 0x04, message 0xFF = 0d255
            // betekenis: Calculated engine load value (%). Formule: A*100/255 = 255*100/255 = 100%
            string expectedAnswer = "100";
            Koos__OBD_Communicator.MainPage MPage = new Koos__OBD_Communicator.MainPage();
            MPage.configData.parseOBDResponse(PIDreturn_engineLoad);
            
            DateTime startWaiting = DateTime.Now;
            while (DateTime.Now < startWaiting.AddSeconds(1)) ;
            Assert.Equals(MPage.DisplayValues[4].Text, expectedAnswer);
        }


    }
}
