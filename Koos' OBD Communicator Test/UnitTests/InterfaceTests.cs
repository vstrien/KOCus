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
            MPage.obd_newOBDSensorData(this, new Koos__OBD_Communicator.OBDSensorDataEventArgs(01, 01, 4, "0007E100"));
        }

        [TestMethod]
        [Description("Check if PID with formula is parsed correctly")]
        public void FilledFormulaeTest()
        {
            //<PIDSensor PID="0C" bytes="01" Description="Engine RPM" Formula="((A*256)+B)/4" />
            Koos__OBD_Communicator.MainPage MPage = new Koos__OBD_Communicator.MainPage();
            MPage.obd_newOBDSensorData(this, new Koos__OBD_Communicator.OBDSensorDataEventArgs(01, 0x0C, 2, "DD30"));
        }
    }
}
