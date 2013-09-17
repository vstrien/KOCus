﻿using System;
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
            PIDAnswerButton.Tap += PIDAnswerButton_Tap;
            //DisplayItems.Add(0x1D, VehicleSpeed);
            //DisplayValues.Add(0x1D, SpeedValue);

            // Every second, request new sensor values or re-initialize (if no response for 10 seconds)
            DispatcherTimer requestNewPIDs = new DispatcherTimer();
            requestNewPIDs.Interval = TimeSpan.FromSeconds(2);
            requestNewPIDs.Tick += requestNewPIDs_Tick;
            requestNewPIDs.Start();

            this.obd.getAndHandleResponseJobAsync(); // starts response job

            // subscribe to events
            configData.RaiseOBDSensorData += obd_newOBDSensorData;
            this.obd.RaisePIDResponse += obd_RaiseResponse;
            this.obd.RaisePIDResponse += obd_updateTimer;

            foreach (PIDSensor sensor in this.configData.availableSensors())
            {
                //<ListBoxItem x:Name="PIDRequestButton">
                //    <StackPanel Orientation="Vertical">
                //        <TextBlock x:Name="PIDRequestText" Text="Read results" TextWrapping="Wrap" FontSize="36" />
                //    </StackPanel>
                //</ListBoxItem>
                TextBlock sensorDescription = new TextBlock() {
                    Name = "PIDDesc " + sensor.PID.ToString(),
                    FontSize = 30,
                    Text = sensor.description
                };

                TextBlock sensorValue = new TextBlock()
                {
                    Name = "PIDValue " + sensor.PID.ToString(),
                    Text = ""
                };

                StackPanel sensorStack = new StackPanel()
                {
                    Orientation = System.Windows.Controls.Orientation.Vertical
                };
                sensorStack.Children.Add(sensorDescription);
                sensorStack.Children.Add(sensorValue);
                
                ListBoxItem sensorItem = new ListBoxItem();
                sensorItem.Name = "sensor " + sensor.PID.ToString();
                sensorItem.Content = sensorStack;

                ControlsDisplay.Items.Add(sensorItem);

                sensor.RaiseOBDSensorData += (object sender, OBDSensorDataEventArgs s) =>
                {
                    sensorValue.Text = s.value;
                };

            }
        }

        void PIDAnswerButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            requestNewValues();
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

            this.obd.getAndHandleResponseJobAsync();
        }



        public void obd_newOBDSensorData(object sender, OBDSensorDataEventArgs e)
        {
            updateStatus_async("New sensor data!");
            // what to do when new sensor data arrives
            if (this.DisplayValues.Keys.Contains(e.PIDCode) && this.DisplayValues[e.PIDCode] != null)
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