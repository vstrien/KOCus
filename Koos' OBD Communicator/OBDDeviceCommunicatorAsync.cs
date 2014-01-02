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
using System.Windows.Threading;
using System.Diagnostics;

namespace Koos__OBD_Communicator
{
    public static class extensionMethods
    {
        public static void Restart(this Stopwatch stwatch)
        {
            stwatch.Reset();
            stwatch.Start();
        }
    }

    public class OBDDeviceCommunicatorAsync
    {

        public enum MessageState { Unsent, Sent, Confirmed };
        public event EventHandler<ResponseEventArgs> RaisePIDResponse;

        public ConfigurationData configuration { get; set; }
        
        ISocketAsyncProvider socket;
        public bool isConnected = false;
        short callNumber = 0;
        const int availabilityRatio = 5;
        const int timeoutForResetSecs =
#if DEBUG
 60;
#else
        5;
#endif
        const int timeoutForReinvokeSecs = 5;

        public volatile MessageState resetStatus = MessageState.Unsent;
        public volatile MessageState headerStatus = MessageState.Unsent;
        public volatile MessageState linefeedStatus = MessageState.Unsent;
        public volatile MessageState echoStatus = MessageState.Unsent;
        private volatile List<PIDSensor> availableSensors;
        private volatile int availableSensor_next;
        volatile Stopwatch lastSeen = new Stopwatch();
        

        /// <summary>
        /// Wrapper for this.socket.sendAsync 
        /// Difference is in the setting of 'ready for command'
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool SendAsync(string data, EventHandler<SocketAsyncEventArgs> handler)
        {
            return this.socket.SendAsync(data, handler);
        }

        public OBDDeviceCommunicatorAsync(ISocketAsyncProvider socket, ConfigurationData currentConfiguration)
        {
            this.socket = socket;
            this.configuration = currentConfiguration;
            this.RaisePIDResponse += this.obd_updateTimer;
            lastSeen.Start();

            DispatcherTimer tenSecondCheckup = new DispatcherTimer();
            tenSecondCheckup.Interval = TimeSpan.FromSeconds(timeoutForReinvokeSecs);
            tenSecondCheckup.Tick += (object sender, EventArgs e) => {
                if (lastSeen.ElapsedMilliseconds > (timeoutForReinvokeSecs * 1000))
                {
                    Logger.WriteLine("No activity for " + timeoutForReinvokeSecs.ToString() + " - forced reinvoke");
                    lastSeen.Restart();
                    requestNewValues();
                }
            };
            tenSecondCheckup.Start();


            
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
            
            this.SendAsync("AT Z" + Environment.NewLine, (s, e) =>
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
                this.SendAsync("AT E1" + Environment.NewLine, (s, e) => { this.echoStatus = MessageState.Sent; });
            }
            else
            {
                Logger.WriteLine("Sending " + "AT E0 (ECHO OFF)");
                this.SendAsync("AT E0" + Environment.NewLine, (s, e) => { this.echoStatus = MessageState.Sent; });
            }
        }

        // set headers on
        public void sendHeaders(bool enable = true)
        {
            if (enable)
            {
                Logger.WriteLine("Sending " + "AT H1 (HEADERS ON)");
                this.SendAsync("AT H1" + Environment.NewLine, (s, e) => { this.headerStatus = MessageState.Sent; });
            }
            else
            {
                Logger.WriteLine("Sending " + "AT H0 (HEADERS OFF)");
                this.SendAsync("AT H0" + Environment.NewLine, (s, e) => { this.headerStatus = MessageState.Sent; });
            }
        }

        // set line feed off
        public void sendLinefeed(bool enable = false)
        {
            if (enable)
            {
                Logger.WriteLine("Sending " + "AT L1 (LF ON)");
                this.SendAsync("AT L1" + Environment.NewLine, (s, e) => { this.linefeedStatus = MessageState.Sent; });
            }
            else
            {
                Logger.WriteLine("Sending " + "AT L0 (LF OFF)");
                this.SendAsync("AT L0" + Environment.NewLine, (s, e) => { this.linefeedStatus = MessageState.Sent; });
            }
        }
        
        /// <summary>
        /// For all available sensors (all sensors that we can handle, and are indicated by the car as 'available'):
        /// Send out a request for new values
        /// </summary>
        public void checkSensorsAsync(bool checkAvailabilityPIDs = true)
        {
            if (this.availableSensors == null || this.availableSensors.Count <= this.availableSensor_next)
            {
                this.availableSensors = this.configuration.availableSensors();
                this.availableSensor_next = 0;
            }
            PIDSensor sensorToQuery = this.availableSensors[this.availableSensor_next];
            this.availableSensor_next += 1;

            if (sensorToQuery.firstPID > 0 && !checkAvailabilityPIDs)
            {
                if(this.availableSensors.Count > this.availableSensor_next)
                    this.checkSensorsAsync(checkAvailabilityPIDs);
            }
            else
            {
                string message = sensorToQuery.mode.ToString("D2") + " " + sensorToQuery.PID.ToString("D2") + '\r';
                this.SendAsync(message, null);
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
                    responses = responses.Trim('\0', '\n', '\r');
                    cleanAndHandleResponses(responses);


                    // If the character '>' is inside the response, the OBD can handle new responses:
                    if (responses.Contains('>'))
                    {
                        this.requestNewValues();
                    }
                        
                }
                getAndHandleResponseJobAsync();
            });
        }

        /// <summary>
        /// Handles messages that are sent to the application by the OBD.
        /// </summary>
        /// <param name="responses">Message that the OBD sent to us</param>
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
                else if (this.echoStatus == MessageState.Sent
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

        /// <summary>
        /// Handles everything in order to query sensors:
        /// - The initialization process
        /// - Querying the sensors
        /// </summary>
        private void requestNewValues()
        {
            /**
             * Querying should only be done when the initialization process is finished.
             * Whenever the OBD doesn't reply any message that makes sense within 10 seconds, the init process should be called again.
             */
            if (lastSeen.ElapsedMilliseconds <= (timeoutForResetSecs * 1000)
                && isConnected
                && resetStatus == OBDDeviceCommunicatorAsync.MessageState.Confirmed
                && headerStatus == OBDDeviceCommunicatorAsync.MessageState.Confirmed
                && linefeedStatus == OBDDeviceCommunicatorAsync.MessageState.Confirmed
                && echoStatus == OBDDeviceCommunicatorAsync.MessageState.Confirmed
                )
            {
                QuerySensors();
            }
            else
            {
                initOBD();
            }
        }

        /// <summary>
        /// Handles the initialization process:
        /// - Reset all settings
        /// - Enable headers ('7E8')
        /// - Disable linefeeds ('\n')
        /// - Disable echo-messages (OBD confirming the arrival of messages by returning them)
        /// </summary>
        private void initOBD()
        {
            /**
             * Initializing can be subdivided in four steps:
             * 1. Reset all settings
             * 2. Enable headers ('7E8')
             * 3. Disable linefeeds ('\n')
             * 4. Disable echo-messages (OBD confirming the arrival of messages by returning them)
             * 
             * Steps 2, 3 and 4 should not be executed before step 1 is finished.
             */
            if (lastSeen.ElapsedMilliseconds >= (timeoutForResetSecs * 1000)
                || resetStatus == OBDDeviceCommunicatorAsync.MessageState.Unsent)
            {
                /* 1. Reset all settings */
                Logger.WriteLine("(Re-)Initializing..");
                lastSeen.Restart();
                this.init_communication();
            }
            else if (this.resetStatus == OBDDeviceCommunicatorAsync.MessageState.Confirmed
                && this.headerStatus == OBDDeviceCommunicatorAsync.MessageState.Unsent)
            {
                /* 2. Enable headers */
                lastSeen.Restart();
                this.sendHeaders(true);
            }
            else if (this.resetStatus == OBDDeviceCommunicatorAsync.MessageState.Confirmed
                && this.linefeedStatus == OBDDeviceCommunicatorAsync.MessageState.Unsent)
            {
                /* 3. Disable linefeeds */
                lastSeen.Restart();
                this.sendLinefeed(false);
            }
            else if (this.resetStatus == OBDDeviceCommunicatorAsync.MessageState.Confirmed
                && this.echoStatus == OBDDeviceCommunicatorAsync.MessageState.Unsent)
            {
                /* 4. Disable echo-messages */
                lastSeen.Restart();
                this.sendEcho(false);
            }
        }

        /// <summary>
        /// Queries all sensors that fulfill the following requirements:
        /// - we know how to handle them (i.e. 'formula sensors')
        /// - the OBD has shown them to be available
        /// </summary>
        private void QuerySensors()
        {
            // Only one in every n calls (where 'n' is the availability ratio) needs to ask for sensor availability
            if (callNumber++ % availabilityRatio == 0)
            {
                callNumber = 0;
                this.checkSensorsAsync(true);
            }
            else
            {
                this.checkSensorsAsync(false);
            }
            
        }

        private void obd_updateTimer(object sender, ResponseEventArgs e)
        {
            lastSeen.Restart();
        }
    }

}
