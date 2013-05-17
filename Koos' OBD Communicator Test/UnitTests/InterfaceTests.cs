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
    }
}
