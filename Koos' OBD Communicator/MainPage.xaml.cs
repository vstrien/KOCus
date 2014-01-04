using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using Microsoft.Phone.Controls;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Threading;
using Logger = CaledosLab.Portable.Logging.Logger;
using Microsoft.Phone.Tasks;

namespace Koos__OBD_Communicator
{
    public partial class MainPage : PhoneApplicationPage
    {
        public ConfigurationData configData;
        OBDDeviceCommunicatorAsync obd;
        //DateTime lastSeen = DateTime.MinValue;
        //short callNumber = 0;
//        const int availabilityRatio = 5;
//        const double timerIntervalSecs = 1;
//        const int timeoutForResetSecs = 
//#if DEBUG
//            6000;
//#else
//        10;
//#endif

#if DEBUG
        // benodigd voor unit tests
        public Dictionary<int, TextBlock> DisplayValues = new Dictionary<int,TextBlock>();
#endif

        // Constructor - voor lokaal testen met een Python-backend: eigen IP-adres gebruiken (opvragen!)
        // Voor gebruik in de auto: zet op 'Release'.
        public MainPage()
            : this(IPAddress.Parse(
#if DEBUG
            "192.168.1.46"
#else
            "192.168.0.10"
#endif
            ), Int32.Parse("35000"))
        {
            
        } 

        public MainPage(IPAddress IP, int port)
        {
            InitializeComponent(); // Default WP init
            
configData = new ConfigurationData();
            obd = new OBDDeviceCommunicatorAsync(new CommunicationProviders.SocketAsync(IP, port), configData);
            
            // starts response job
            // The response job runs asynchronously from the send jobs - it continually "drains the pipe" of messages from the OBD.
            // Also, whenever the OBD is ready for new commands, it calls on the 'issue new command' method.
            this.obd.getAndHandleResponseJobAsync(); 
            
            AddPIDSensorDisplay(this.configData.availableSensors());
        }

        private void AddPIDSensorDisplay(List<PIDSensor> sensors)
        {
            foreach (PIDSensor sensor in sensors)
            {
                if (sensor.PIDSensors.Count > 0)
                {
                    // Add 'child' sensors instead of current sensor
                    this.AddPIDSensorDisplay(sensor.PIDSensors);
                }
                // Only formula sensors can be read currently, so all other ones can be ignored.
                else if (sensor.highestFormulaCharacterNumber > -1) 
                {

                    TextBlock sensorDescription = new TextBlock()
                    {
                        //Name = "PIDDesc " + sensor.PID.ToString(),
                        FontSize = 20,
                        Text = sensor.description
                    };

                    TextBlock sensorValue = new TextBlock()
                    {
                        Text = ""
                    };
#if DEBUG
                    this.DisplayValues[sensor.PID] = sensorValue;
#endif           
                    StackPanel sensorStack = new StackPanel()
                    {
                        Orientation = System.Windows.Controls.Orientation.Vertical
                    };
                    sensorStack.Children.Add(sensorDescription);
                    sensorStack.Children.Add(sensorValue);

                    ListBoxItem sensorItem = new ListBoxItem()
                    {
                        //Name = "sensor " + sensor.PID.ToString();
                        Content = sensorStack,
                        Visibility = System.Windows.Visibility.Collapsed
                    };
                    // this.DisplayItems[sensor.PID] = sensorItem;
                    
                    ControlsDisplay.Items.Add(sensorItem);
                    
                    // Whenever sensor finds new data, insert into this textbox:
                    sensor.RaiseOBDSensorData += (object sender, OBDSensorDataEventArgs s) =>
                    {
                        this.Dispatcher.BeginInvoke(delegate()
                        {
                            sensorItem.Visibility = System.Windows.Visibility.Visible;
                            sensorValue.Text = s.value;
                        });
                    };
                }
            }
        }

        private void sendLog(object sender, EventArgs e)
        {
            Logger.WriteLine("Begin Send via email");

            string Subject = "KoCus OBD inspector LOG";

            try
            {
                EmailComposeTask mail = new EmailComposeTask();
                mail.Subject = Subject;
                mail.Body = Logger.GetStoredLog();

                if (mail.Body.Length > 32000) // max 32K 
                {
                    mail.Body = mail.Body.Substring(mail.Body.Length - 32000);
                }

                mail.Show();
            }
            catch
            {
                MessageBox.Show("unable to create the email message");
                Logger.WriteLine("unable to create the email message");
            }

            Logger.WriteLine("End Send via email");
        }

        private void viewLog(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/LogContents.xaml", UriKind.Relative));
        }
    }
}