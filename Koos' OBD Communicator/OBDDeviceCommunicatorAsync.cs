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
        public enum MessageState { Unsent, Sent, Confirmed };

        public MessageState resetStatus = MessageState.Unsent;
        public MessageState headerStatus = MessageState.Unsent;
        public MessageState linefeedStatus = MessageState.Unsent;
        public MessageState echoStatus = MessageState.Unsent;
        // echo status cannot be confirmed, but can be sent.

        public OBDDeviceCommunicatorAsync(ISocketAsyncProvider socket, ConfigurationData currentConfiguration)
        {
            this.socket = socket;
            this.configuration = currentConfiguration;
        }

        public void init_communication()
        {
            this.socket.ConnectAsync((s, e) =>
            {
                if (e == null // Geen nieuwe verbinding gestart i.v.m. reeds bestaande verbinding
                    || e.SocketError == SocketError.Success)
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

            this.resetStatus = MessageState.Unsent;
            this.socket.SendAsync("AT Z" + Environment.NewLine, (s, e) =>
            {
                this.resetStatus = MessageState.Sent;
                this.linefeedStatus = MessageState.Unsent;
                this.headerStatus = MessageState.Unsent;
                this.echoStatus = MessageState.Unsent;
            });
        }

        // set echo off
        public  void sendEcho(bool enable = false)
        {
            if (enable)
            {
                Logger.WriteLine("Sending " + "AT E1 (ECHO ON)");
                this.socket.SendAsync("AT E1" + Environment.NewLine, (s, e) => { this.echoStatus = MessageState.Sent; });
            }
            else
            {
                Logger.WriteLine("Sending " + "AT E0 (ECHO OFF)");
                this.socket.SendAsync("AT E0" + Environment.NewLine, (s, e) => { this.echoStatus = MessageState.Sent; });
            }
        }

        // set headers on
        public void sendHeaders(bool enable = true)
        {
            if (enable)
            {
                Logger.WriteLine("Sending " + "AT H1 (HEADERS ON)");
                this.socket.SendAsync("AT H1" + Environment.NewLine, (s, e) => { this.headerStatus = MessageState.Sent; });
            }
            else
            {
                Logger.WriteLine("Sending " + "AT H0 (HEADERS OFF)");
                this.socket.SendAsync("AT H0" + Environment.NewLine, (s, e) => { this.headerStatus = MessageState.Sent; });
            }
        }

        // set line feed off
        public void sendLinefeed(bool enable = false)
        {
            if (enable)
            {
                Logger.WriteLine("Sending " + "AT L1 (LF ON)");
                this.socket.SendAsync("AT L1" + Environment.NewLine, (s, e) => { this.linefeedStatus = MessageState.Sent; });
            }
            else
            {
                Logger.WriteLine("Sending " + "AT L0 (LF OFF)");
                this.socket.SendAsync("AT L0" + Environment.NewLine, (s, e) => { this.linefeedStatus = MessageState.Sent; });
            }
        }
        
        /// <summary>
        /// For all available sensors (all sensors that we can handle, and are indicated by the car as 'available'):
        /// Send out a request for new values
        /// </summary>
        public void checkSensorsAsync(bool checkAvailabilityPIDs = true)
        {    
            var availableSensors = this.configuration.availableSensors();
            Logger.WriteLine("Requesting available sensors");

            foreach (PIDSensor SensorsToCheck in availableSensors)
            {
                if (checkAvailabilityPIDs || SensorsToCheck.firstPID == 0)
                {
                    string message = SensorsToCheck.mode.ToString("D2") + " " + SensorsToCheck.PID.ToString("D2") + '\r';
                    this.socket.SendAsync(message, null);
                }
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
                Logger.WriteLine("R: " + response);
                if (this.resetStatus == MessageState.Sent
                        && responses.Length >= 4
                        && responses.Substring(0, 4) == "AT Z")
                {
                    this.resetStatus = MessageState.Confirmed;
                }
                else if (this.headerStatus == MessageState.Sent
                  && responses.Length >= 5
                  && responses.Substring(0, 4) == "AT H")
                {
                    this.headerStatus = MessageState.Confirmed;
                }
                else if (this.linefeedStatus == MessageState.Sent
                    && responses.Length >= 5
                    && responses.Substring(0, 4) == "AT L")
                {
                    this.linefeedStatus = MessageState.Confirmed;
                }
                else if (this.linefeedStatus == MessageState.Sent
                    && responses.Length >= 5
                    && responses.Substring(0, 4) == "AT E")
                {
                    this.echoStatus = MessageState.Confirmed;
                }
                else if (Message.isValid(response) == Message.ResponseValidity.Valid)
                {
                    this.OnRaisePIDResponse(new ResponseEventArgs(response));
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
                            // No logging - just a message we can't handle.
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
