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

namespace Koos__OBD_Communicator
{
    public partial class MainPage : PhoneApplicationPage
    {
        ConfigurationData configData;
        OBDDeviceCommunicatorAsync obd;
        DateTime lastSeen = DateTime.MinValue;
        Dictionary<int, ListBoxItem> DisplayItems = new Dictionary<int,ListBoxItem>();
        Dictionary<int, TextBlock> DisplayValues = new Dictionary<int,TextBlock>();

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            configData = new ConfigurationData();
            obd = new OBDDeviceCommunicatorAsync(new CommunicationProviders.SocketAsync(IPAddress.Parse("192.168.0.10"), Int32.Parse("35000")), configData);
            //obd = new OBDDeviceCommunicatorAsync(new CommunicationProviders.SocketAsync(IPAddress.Parse("192.168.40.138"), Int32.Parse("35000")), configData);
            
            PIDRequestButton.Tap += PIDRequestButton_Tap;
            DisplayItems.Add(0x1D, VehicleSpeed);
            DisplayValues.Add(0x1D, SpeedValue);

            // Every second, request new sensor values or re-initialize (if no response for 10 seconds)
            DispatcherTimer requestNewPIDs = new DispatcherTimer();
            requestNewPIDs.Interval = TimeSpan.FromSeconds(1);
            requestNewPIDs.Tick += requestNewPIDs_Tick;
            requestNewPIDs.Start();

            // subscribe to events
            configData.RaiseOBDSensorData += obd_newOBDSensorData;
            this.obd.RaisePIDResponse += obd_RaiseResponse;
            this.obd.RaisePIDResponse += obd_updateTimer;
        }

        void requestNewPIDs_Tick(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
                
            if (lastSeen.AddSeconds(10) < DateTime.Now)
            {
                // 10 seconds without response. Re-init.
                updateStatus_async("Initializing..");

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
            this.Dispatcher.BeginInvoke(delegate()
            {
                var maxLength = 1000;
                var newText = newStatus + Environment.NewLine + StatusDisplay.Text;

                var newText_capped = newText.Substring(0, Math.Min(newText.Length, maxLength));

                // insert on top
                StatusDisplay.Text = newText_capped;
            });
        }

        void PIDRequestButton_Tap(object s, System.Windows.Input.GestureEventArgs e)
        {
            updateStatus_async("Requesting PIDs");

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, eventArgs) =>
            {
                updateStatus_async("Getting values...");
                this.obd.getAndHandleResponse();
                updateStatus_async("Finished getting PID values.");
            };
            worker.RunWorkerAsync();
        }

        public void obd_newOBDSensorData(object sender, OBDSensorDataEventArgs e)
        {
            updateStatus_async("New sensor data!");
            // what to do when new sensor data arrives
            if (this.DisplayValues[e.PIDCode] != null)
                this.DisplayValues[e.PIDCode].Text = e.message;
            updateStatus_async(e.message);
        }

        void obd_RaiseResponse(object sender, ResponseEventArgs e)
        {
            updateStatus_async(e.message);
        }

        private void obd_updateTimer(object sender, ResponseEventArgs e)
        {
            this.lastSeen = DateTime.Now;
        }
    }
}