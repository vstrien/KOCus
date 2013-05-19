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

namespace Koos__OBD_Communicator
{
    public partial class MainPage : PhoneApplicationPage
    {
        // global settings, to be moved!
        OBDDeviceCommunicator obd = new OBDDeviceCommunicator(IPAddress.Parse("192.168.0.10"), Int32.Parse("35000"));
        ConfigurationData configData = new ConfigurationData();

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            PIDRequestButton.Tap += PIDRequestButton_Tap;
            InitButton.Tap += InitButton_Tap;
        }

        void InitButton_Tap(object s, System.Windows.Input.GestureEventArgs e)
        {
            updateStatus_async("Initializing..");
            
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, eventArgs) =>
            {
                this.obd.RaiseInitResponse += obd_RaiseResponse;
                
                string result = this.obd.init_communication(this.configData);
                if (result == "Success")
                {
                    updateStatus_async("Init successful.");
                    int supported = 0;
                    int mentioned = 0;
                    for (int mode = 0; mode < this.obd.PIDInformation.supportedPIDs.Length; mode++)
                    {
                        for (int PID = 0; PID < this.obd.PIDInformation.supportedPIDs[mode].Length; PID++)
                        {
                            switch(this.obd.PIDInformation.supportedPIDs[mode][PID]) {
                                case Koos__OBD_Communicator.PID.SupportedStatus.Supported:
                                    supported++;
                                    mentioned++;
                                    break;
                                case Koos__OBD_Communicator.PID.SupportedStatus.Unsupported:
                                    mentioned++;
                                    break;
                            }
                        }
                    }
                    updateStatus_async("Mentioned: " + mentioned.ToString());
                    updateStatus_async("Supported: " + supported.ToString());
                }
                else
                {
                    updateStatus_async("Init failed.");
                    updateStatus_async(result);
                }
                this.obd.RaiseInitResponse -= obd_RaiseResponse;
            };
            worker.RunWorkerAsync();
        }

        void obd_RaiseResponse(object sender, ResponseEventArgs e)
        {
            updateStatus_async(e.message);
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
                this.obd.RaiseOBDSensorData += obd_newOBDSensorData;
                this.obd.RaisePIDResponse += obd_RaiseResponse;
                updateStatus_async("Getting values...");
                this.obd.getSensorValuesSync(this.configData);
                updateStatus_async("Finished getting PID values.");
                this.obd.RaiseOBDSensorData -= obd_newOBDSensorData;
                this.obd.RaisePIDResponse -= obd_RaiseResponse;
            };
            worker.RunWorkerAsync();
        }

        private void obd_newOBDSensorData(object sender, OBDSensorDataEventArgs e)
        {
            string message = e.mode.ToString("D2") + " " + e.PIDCode.ToString("D2") + " (" + e.length.ToString() + "): " + e.message;
            updateStatus_async(message);
        }

        

    }
}