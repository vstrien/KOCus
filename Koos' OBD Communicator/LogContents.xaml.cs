using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Logger = CaledosLab.Portable.Logging.Logger;

namespace Koos__OBD_Communicator
{
    public partial class LogContents : PhoneApplicationPage
    {
        public LogContents()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            logContents.Text = Logger.GetStoredLog();
        }

        private void goBack(object sender, EventArgs e)
        {
            NavigationService.GoBack();

        }
    }
}