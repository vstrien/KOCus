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
        DateTime lastSeen = DateTime.MinValue;

#if DEBUG
        // benodigd voor unit tests
        public Dictionary<int, TextBlock> DisplayValues = new Dictionary<int,TextBlock>();
#endif

        // Constructor - voor lokaal testen met een Python-backend: eigen IP-adres gebruiken (opvragen!)
        // Voor gebruik in de auto: zet op 'Release'.
#if DEBUG
        public MainPage()
            : this(IPAddress.Parse("192.168.1.46"), Int32.Parse("35000"))
        {
            
        } 
#else
        public MainPage()
            : this(IPAddress.Parse("192.168.0.10"), Int32.Parse("35000"))
        {

        }
#endif
        public MainPage(IPAddress IP, int port)
        {
            InitializeComponent();
            configData = new ConfigurationData();
            obd = new OBDDeviceCommunicatorAsync(new CommunicationProviders.SocketAsync(IP, port), configData);
            //obd = new OBDDeviceCommunicatorAsync(new CommunicationProviders.SocketAsync(IPAddress.Parse("192.168.40.138"), Int32.Parse("35000")), configData);

            // Every second, request new sensor values or re-initialize (if no response for 10 seconds)
            DispatcherTimer requestNewPIDs = new DispatcherTimer();
            requestNewPIDs.Interval = TimeSpan.FromSeconds(1);
            requestNewPIDs.Tick += requestNewPIDs_Tick;
            requestNewPIDs.Start();

            this.obd.getAndHandleResponseJobAsync(); // starts response job

            // subscribe to events
            this.obd.RaisePIDResponse += obd_updateTimer;

            AddPIDSensorDisplay(this.configData.availableSensors());
        }

        private void AddPIDSensorDisplay(List<PIDSensor> sensors)
        {
            foreach (PIDSensor sensor in sensors)
            {
                if (sensor.PIDSensors.Count > 0)
                {
                    this.AddPIDSensorDisplay(sensor.PIDSensors);
                }
                else
                {
                    
                    TextBlock sensorDescription = new TextBlock()
                    {
                        //Name = "PIDDesc " + sensor.PID.ToString(),
                        FontSize = 30,
                        Text = sensor.description
                    };

                    TextBlock sensorValue = new TextBlock()
                    {
                        Name = "PIDValue " + sensor.PID.ToString(),
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
                        Content = sensorStack
                    };
                    // this.DisplayItems[sensor.PID] = sensorItem;
                    
                    ControlsDisplay.Items.Add(sensorItem);
                    
                    sensor.RaiseOBDSensorData += (object sender, OBDSensorDataEventArgs s) =>
                    {
                        this.Dispatcher.BeginInvoke(delegate()
                        {
                            sensorValue.Text = s.value;
                        });
                    };
                }
            }
        }

        void requestNewPIDs_Tick(object sender, EventArgs e)
        {
            requestNewValues();
        }

        private void requestNewValues()
        {
            BackgroundWorker worker = new BackgroundWorker();

            if (lastSeen.AddSeconds(10) < DateTime.Now)
            {
                // 10 seconds without response. Re-init.
                updateStatus_async("Initializing..");
                lastSeen = DateTime.Now;

                worker.DoWork += (s, eventArgs) =>
                {
                    obd.init_communication();
                };
            }
            else
            {
                updateStatus_async("Requesting new sensors..");

                worker.DoWork += (s, eventArgs) =>
                {
                    obd.checkSensorsAsync();
                };
            }
            //For future:
            //worker.DoWork += (s, eventArgs) =>
            //{
            //    obd.getAndHandleResponse();
            //};
            worker.RunWorkerAsync();
        }


        void updateStatus_async(string newStatus)
        {
            //this.Dispatcher.BeginInvoke(delegate()
            //{
            //    var maxLength = 1000;
            //    var newText = newStatus + Environment.NewLine + StatusDisplay.Text;

            //    var newText_capped = newText.Substring(0, Math.Min(newText.Length, maxLength));

            //    // insert on top
            //    StatusDisplay.Text = newText_capped;
            //});
        }

        void PIDRequestButton_Tap(object s, System.Windows.Input.GestureEventArgs e)
        {
            updateStatus_async("Requesting PIDs");

            this.obd.getAndHandleResponseJobAsync();
        }

        private void obd_updateTimer(object sender, ResponseEventArgs e)
        {
            this.lastSeen = DateTime.Now;
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
    }
}