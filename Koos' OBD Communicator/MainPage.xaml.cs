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
        string ON = "1", 
               OFF = "0", 
               AT_RESET = "Z", 
               AT_LINEFEED = "L", 
               AT_ECHO = "E", 
               AT_HEADERS = "H", 
               AT_VOLTAGE = "RV", 
               AVAILABLE_SENSORS_0_20 = "01 00";


        // Constructor
        public MainPage()
        {
            InitializeComponent();
            ResetIndicator.Tap += ResetButton_Tap;
            PIDRequestButton.Tap += PIDRequestButton_Tap;
            GetRPMButton.Tap += GetRPMButton_Tap;
            InitButton.Tap += InitButton_Tap;
        }

        void GetRPMButton_Tap(object s, System.Windows.Input.GestureEventArgs e)
        {
            updateStatus_async("Getting rpm..");
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, eventArgs) =>
            {
                string result = obd.get_rpm();
                updateStatus_async("RPM: " + result);
            };
            worker.RunWorkerAsync();
        }

        void InitButton_Tap(object s, System.Windows.Input.GestureEventArgs e)
        {
            updateStatus_async("Initializing..");
            
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, eventArgs) =>
            {
                string result = this.obd.init_communication(this.configData);
                if (result == "Success")
                {
                    updateStatus_async("Init successful.");
                    for (int mode = 1; mode <= PID.defaultNumberOfModes; mode++)
                    {
                        for (int nPID = 0; nPID < PID.defaultNumberOfPIDsPerMode; nPID++)
                        {
                            var supported = this.obd.isSupported(mode, nPID);
                            string isSupported;

                            switch (supported)
                            {
                                case PID.SupportedStatus.Supported:
                                    isSupported = "Supported";
                                    break;
                                case PID.SupportedStatus.Unsupported:
                                    isSupported = "Unsupported";
                                    break;
                                default:
                                    isSupported = "Unknown";
                                    break;
                            }
                            updateStatus_async(mode.ToString() + " " + nPID.ToString() + ": " + isSupported);
                        }
                    }
                }
                else
                {
                    updateStatus_async("Init failed.");
                    updateStatus_async(result);
                }
            };
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


        void ResetButton_Tap(object s, System.Windows.Input.GestureEventArgs e)
        {
            updateStatus_async("Resetting");

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, eventArgs) =>
            {
                Thread.Sleep(1000);
                string message = "AT " + AT_RESET + "\r";

                SocketError result = obd.connectAndSendSync(message);
                if (result == SocketError.Success)
                {
                    string response = obd.ReceiveUntilGtSync();
                    if (response.Length == 0)
                    {
                        updateStatus_async("No R_response");
                    }
                    else
                    {
                        updateStatus_async("R_Response: " + response);
                    }
                }
                else
                {
                    updateStatus_async("R_Status: " + result.ToString());
                }
            };
            worker.RunWorkerAsync();
        }

        void PIDRequestButton_Tap(object s, System.Windows.Input.GestureEventArgs e)
        {
            updateStatus_async("Requesting PIDs");

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, eventArgs) =>
            {
                Thread.Sleep(1000);
                string message = AVAILABLE_SENSORS_0_20 + "\r";

                SocketError result = obd.connectAndSendSync(message);
                if (result == SocketError.Success)
                {
                    string response = obd.ReceiveUntilGtSync();
                    if (response.Length == 0)
                    {
                        updateStatus_async("No P_response");
                    }
                    else
                    {
                        updateStatus_async("P_Response: " + response);
                    }
                }
                else
                {
                    updateStatus_async("P_Status: " + result.ToString());
                }
            };
            worker.RunWorkerAsync();
        }

        

    }
}