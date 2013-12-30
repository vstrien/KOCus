using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CommunicationProviders;
using System.ComponentModel;
using Logger = CaledosLab.Portable.Logging.Logger;

namespace Koos__OBD_Communicator
{
    public class OBDDeviceCommunicatorAsync
    {

        public event EventHandler<ResponseEventArgs> RaisePIDResponse;
        public ConfigurationData configuration { get; set; }
        ISocketAsyncProvider socket;
        public bool isConnected = false;

        public OBDDeviceCommunicatorAsync(ISocketAsyncProvider socket, ConfigurationData currentConfiguration)
        {
            this.socket = socket;
            this.configuration = currentConfiguration;
        }

        public void init_communication()
        {
            
            // connect
            this.socket.ConnectAsync((s, e) =>
            {
                if (e.SocketError == SocketError.Success)
                {
                    this.isConnected = true;
                    sendReset();
                }
                else
                {
                    this.isConnected = false;
                }
            });
        }

        private void sendReset()
        {
            // reset device
            //this.socket.SendSync(vocabulary["RESET"]);
            Logger.WriteLine("Sending " + "AT Z (RESET)");
            this.socket.SendAsync("AT Z" + Environment.NewLine, (s1, e1) =>
            {
                sendLinefeed();

                sendHeaders();

                sendEcho();
            });
        }

        // set echo off
        private void sendEcho(bool enable = false)
        {
            if (enable)
            {
                Logger.WriteLine("Sending " + "AT E1 (ECHO ON)");
                this.socket.SendAsync("AT E1" + Environment.NewLine, null);
            }
            else
            {
                Logger.WriteLine("Sending " + "AT E0 (ECHO OFF)");
                this.socket.SendAsync("AT E0" + Environment.NewLine, null);
            }
        }

        // set headers on
        private void sendHeaders(bool enable = true)
        {
            if (enable)
            {
                Logger.WriteLine("Sending " + "AT H1 (HEADERS ON)");
                this.socket.SendAsync("AT H1" + Environment.NewLine, null);
            }
            else
            {
                Logger.WriteLine("Sending " + "AT H0 (HEADERS OFF)");
                this.socket.SendAsync("AT H0" + Environment.NewLine, null);
            }
        }

        // set line feed off
        private void sendLinefeed(bool enable = false)
        {
            if (enable)
            {
                Logger.WriteLine("Sending " + "AT L1 (LF ON)");
                this.socket.SendAsync("AT L1" + Environment.NewLine, null);
            }
            else
            {
                Logger.WriteLine("Sending " + "AT L0 (LF OFF)");
                this.socket.SendAsync("AT L0" + Environment.NewLine, null);
            }
        }
        
        /// <summary>
        /// For all available sensors (all sensors that we can handle, and are indicated by the car as 'available'):
        /// Send out a request for new values
        /// </summary>
        public void checkSensorsAsync()
        {    
            var availableSensors = this.configuration.availableSensors();
            Logger.WriteLine("Requesting available sensors");

            foreach (PIDSensor SensorsToCheck in availableSensors)
            {
                string message = SensorsToCheck.mode.ToString("D2") + " " + SensorsToCheck.PID.ToString("D2") + '\r';
                Logger.WriteLine("Sending " + message);
                this.socket.SendAsync(message, null);
            }
        }

        /// <summary>
        /// Receives async messages from the OBD system, sends them to the configuration's parser
        /// </summary>
        public void getAndHandleResponses()
        {
            this.socket.ReceiveAsync((s, eventArgs) =>
            {

                if (eventArgs.SocketError == SocketError.Success)
                {
                    string responses = Encoding.UTF8.GetString(eventArgs.Buffer, eventArgs.Offset, eventArgs.BytesTransferred);
                    responses = responses.Trim('\0', '\n', '\r', '>');

                    cleanAndHandleResponses(responses);
                }
                getAndHandleResponseJobAsync();

                
                //OnRaiseOBDSensorData(
                //        new OBDSensorDataEventArgs(
                //            Message.getModeOfMessage(cleanedResponse), 
                //            PID, 
                //            Message.getBytesInMessage(cleanedResponse), 
                //            Message.getMessageContents(cleanedResponse))
                //    );
            });
        }

        public void cleanAndHandleResponses(string responses)
        {
            string[] responseList = responses.Replace('\n'.ToString(), "").Split('\r');
            foreach (string response in responseList)
            {
                Logger.WriteLine("New PID response");
                Logger.WriteLine(response);
                if (Message.isValid(response) == Message.ResponseValidity.Valid)
                {
                    this.OnRaisePIDResponse(new ResponseEventArgs(response));
                    Logger.WriteLine("Valid");

                    this.configuration.parseOBDResponse(response);
                }
                else
                {
                    switch (Message.isValid(response))
                    {
                        case Message.ResponseValidity.InvalidContents:
                            Logger.WriteLine("Invalid contents");
                            break;
                        case Message.ResponseValidity.InvalidHeader:
                            Logger.WriteLine("Invalid header");
                            break;
                        case Message.ResponseValidity.InvalidSize:
                            Logger.WriteLine("Invalid size");
                            break;
                    }
                }
            }
        }

        public void getAndHandleResponseJobAsync()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (sender, eventArgs) =>
            {
                    this.getAndHandleResponses();
            };
            worker.RunWorkerAsync();
        }

        protected virtual void OnRaisePIDResponse(ResponseEventArgs eventArgs)
        {
            EventHandler<ResponseEventArgs> handler = RaisePIDResponse;

            // Only execute if there are any subscribers
            if (handler != null)
            {
                handler(this, eventArgs);
            }
        }


    }

}
