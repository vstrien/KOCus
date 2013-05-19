using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Koos__OBD_Communicator
{
    class OBDDeviceCommunicator
    {
        PID PIDInformation;

        public event EventHandler<OBDSensorDataEventArgs> RaiseOBDSensorData;

        #region socket communication
        IPAddress hostaddress;
        int port;
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        int MAX_BUFFER_SIZE = 4096;

        public OBDDeviceCommunicator(IPAddress hostaddress, int port)
        {
            this.hostaddress = hostaddress;
            this.port = port;
        }

        private bool connect()
        {
            ManualResetEvent connectionDone = new ManualResetEvent(false);
            bool connected = false;

            if (socket.Connected) // already connected
                return true;

            
            SocketAsyncEventArgs socketEventArgs = new SocketAsyncEventArgs()
            {
                RemoteEndPoint = new IPEndPoint(this.hostaddress, this.port)
            };

            socketEventArgs.Completed += (socketObject, eventArgs) =>
            {
                connected = eventArgs.SocketError == SocketError.Success;
                connectionDone.Set();
            };

            connectionDone.Reset();
            socket.ConnectAsync(socketEventArgs);
            connectionDone.WaitOne();

            return connected;
        }

        public void connectAndReceiveAsync(EventHandler<SocketAsyncEventArgs> handleResponse)
        {
            connect();

            ReceiveAsync(handleResponse);
        }

        public SocketError connectAndSendSync(string message)
        {
            SocketError result = SocketError.NotConnected;

            connect();
            ManualResetEvent sendDone = new ManualResetEvent(false);

            SendAsync(message, (s, e) =>
            {
                result = e.SocketError;

                sendDone.Set();
            });
            sendDone.Reset();
            sendDone.WaitOne();

            return result;
        }


        public string ReceiveUntilGtSync()
        {
            string response = "";
            bool error = false;
            ManualResetEvent receiveDone = new ManualResetEvent(false);

            // receive all data from buffer.
            // This achieved through polling the buffer until no response comes back
            do
            {
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = socket.RemoteEndPoint,
                    UserToken = null
                };

                socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
                EventHandler<SocketAsyncEventArgs> read_complete_buffer = new EventHandler<SocketAsyncEventArgs>(delegate(object s4, SocketAsyncEventArgs e4)
                {
                    if (e4.SocketError == SocketError.Success)
                    {
                        response += Encoding.UTF8.GetString(e4.Buffer, e4.Offset, e4.BytesTransferred);
                        response = response.Trim('\0', '\n', '\r');
                    }
                    else
                    {
                        error = true;
                    }
                    receiveDone.Set();
                });

                socketEventArg.Completed += read_complete_buffer;
                receiveDone.Reset();

                socket.ReceiveAsync(socketEventArg);
                
                receiveDone.WaitOne();

            } while (!error && response.Length > 0 && response.Substring(response.Length - 1) != ">");

            return response;
        }

        public string ReceiveSync()
        {
            string response = "";
            ManualResetEvent receiveDone = new ManualResetEvent(false);

            // receive all data from buffer.
            // This achieved through polling the buffer until no response comes back
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs()
            {
                RemoteEndPoint = socket.RemoteEndPoint,
                UserToken = null
            };

            socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
            EventHandler<SocketAsyncEventArgs> read_complete_buffer = new EventHandler<SocketAsyncEventArgs>(delegate(object socketObject, SocketAsyncEventArgs eventArgs)
            {
                if (eventArgs.SocketError == SocketError.Success)
                {
                    response += Encoding.UTF8.GetString(eventArgs.Buffer, eventArgs.Offset, eventArgs.BytesTransferred);
                    response = response.Trim('\0', '\n', '\r');
                }

                receiveDone.Set();
            });

            socketEventArg.Completed += read_complete_buffer;
            receiveDone.Reset();

            socket.ReceiveAsync(socketEventArg);

            receiveDone.WaitOne();

            return response;
        }

        public string ReceiveAsync(int timeoutPerRead_ms = 100)
        {

            bool receivedNewData = false;
            string response = "";

            // receive all data from buffer.
            // This achieved through polling the buffer until no response comes back
            do
            {
                receivedNewData = false;

                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = socket.RemoteEndPoint,
                    UserToken = null
                };

                socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
                EventHandler<SocketAsyncEventArgs> read_complete_buffer = new EventHandler<SocketAsyncEventArgs>(delegate(object s4, SocketAsyncEventArgs e4)
                {
                    if (e4.SocketError == SocketError.Success)
                    {
                        response += Encoding.UTF8.GetString(e4.Buffer, e4.Offset, e4.BytesTransferred);
                        response = response.Trim('\0');
                        receivedNewData = true;
                    }
                });

                socketEventArg.Completed += read_complete_buffer;

                socket.ReceiveAsync(socketEventArg);

                Thread.Sleep(timeoutPerRead_ms);

            } while (receivedNewData);

            return response;
        }

        public void ReceiveAsync(EventHandler<SocketAsyncEventArgs> handleResponse)
        {
            // We are re-using the _socket object initialized in the Connect method
            if (socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = socket.RemoteEndPoint,
                    UserToken = null
                };

                socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);

                // Event handler for the Completed event.
                socketEventArg.Completed += handleResponse;

                socket.ReceiveAsync(socketEventArg);
            }
            else
            {
                throw new InvalidOperationException("socket not initialized!");
            }
        }

        /// <summary>
        /// Send the given data to the server using the established connection
        /// </summary>
        /// <param name="data">The data to send to the server</param>
        /// <returns>The result of the Send request</returns>
        public void SendAsync(string data, EventHandler<SocketAsyncEventArgs> handleResponse)
        {
            // We are re-using the _socket object initialized in the Connect method
            if (socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = socket.RemoteEndPoint,
                    UserToken = null
                };

                // Event handler for the Completed event.
                socketEventArg.Completed += handleResponse;

                // Add the data to be sent into the buffer
                byte[] payload = Encoding.UTF8.GetBytes(data);
                socketEventArg.SetBuffer(payload, 0, payload.Length);

                // Make an asynchronous Send request over the socket
                socket.SendAsync(socketEventArg);
            }
            else
            {
                throw new InvalidOperationException("socket not initialized!");
            }
        }
        #endregion socket communication

        #region high-level communication
        public PID.SupportedStatus isSupported(int mode, int nPID)
        {
            return this.PIDInformation.isSupported(mode, nPID);
        }

        public string init_communication(ConfigurationData currentConfiguration)
        {
            this.PIDInformation = new PID();

            // connect
            this.connect();
            
            //empty pipeline
            this.ReceiveAsync(500);

            // reset device
            //this.connectAndSendSync(vocabulary["RESET"]);
            this.connectAndSendSync("AT Z\r");
            this.ReceiveUntilGtSync();

            // set line feed off
            //this.connectAndSendSync(vocabulary["LF OFF"]);
            //this.ReceiveUntilGtSync();
            this.connectAndSendSync("AT L0\r");
            this.ReceiveUntilGtSync();

            // set headers on
            //this.connectAndSendSync(vocabulary["HEADERS ON"]);
            //this.ReceiveUntilGtSync();
            this.connectAndSendSync("AT H1\r");
            this.ReceiveUntilGtSync();

            // set echo off
            //this.connectAndSendSync(vocabulary["ECHO OFF"]);
            //this.ReceiveUntilGtSync();
            this.connectAndSendSync("AT E0\r");
            this.ReceiveUntilGtSync();

            foreach (SensorAvailability SensorsToCheck in currentConfiguration.sensorLists)
            {
                string message = SensorsToCheck.mode.ToString("D2") + " " + SensorsToCheck.PID.ToString("D2") + "\r";
                this.connectAndSendSync(message);
                string supportedPIDs = this.ReceiveUntilGtSync();
                if (!this.PIDInformation.parseSupportedPIDs(SensorsToCheck.mode, SensorsToCheck.firstPID, SensorsToCheck.lastPID, supportedPIDs))
                    ; // well, shit happens. For now, just skip.
            }

            return "Success";
        }

        // Haal voor alle beschikbare sensors waarden op
        public void getSensorValuesSync(ConfigurationData currentConfiguration)
        {
            foreach (SensorAvailability AvailableSensors in currentConfiguration.sensorLists)
            {
                // Wanneer van een sensor-range (van 32 sensors) de beschikbaarheid onbekend is, is uitvragen / loopen zinloos.
                if (this.PIDInformation.isSupported(AvailableSensors.mode, AvailableSensors.firstPID) != PID.SupportedStatus.Unknown)
                {
                    foreach (var currentSensor in AvailableSensors.PIDSensors)
                    {
                        // Alleen wanneer een PID ook daadwerkelijk beschikbaar & ondersteund is, mag deze uitgevraagd worden.
                        if (this.PIDInformation.isSupported(currentSensor.mode, currentSensor.PID) == PID.SupportedStatus.Supported)
                        {
                            // PID is beschikbaar. Message opstellen om mee uit te vragen (bijv. "01 02\r")
                            string message = currentSensor.mode.ToString("D2") + " " + currentSensor.PID.ToString("D2") + "\r";
                            this.connectAndSendSync(message);

                            // Wacht de volgende ">" af.
                            string response = this.ReceiveUntilGtSync();
                            if (Message.isValid(response) != Message.ResponseValidity.Valid)
                            {
                                // Als we niets met het teruggekomen bericht aankunnen, kunnen we het voor nu overslaan.
                            }
                            else
                            {
                                // eigenlijk moet hier natuurlijk ook de mode en PID uit de response afgeleid worden.
                                string data = Message.getMessageContents(response);
                                OnRaiseOBDSensorData(new OBDSensorDataEventArgs(currentSensor.mode, currentSensor.PID, currentSensor.bytes, data));
                            }
                        }
                    }
                }
            }
        }

        #endregion high-level communication

        protected virtual void OnRaiseOBDSensorData(OBDSensorDataEventArgs eventArgs)
        {
            EventHandler<OBDSensorDataEventArgs> handler = RaiseOBDSensorData;

            // Only execute if there are any subscribers
            if (handler != null)
            {
                handler(this, eventArgs);
            }

        }
    }

    public class OBDSensorDataEventArgs : EventArgs
    {
        public OBDSensorDataEventArgs(int mode, int PID, int length, string message)
        {
            this.mode = mode;
            this.PID = PID;
            this.length = length;
            this.message = message;   
        }

        public int mode { get; private set; }
        public int PID { get; private set; }
        public int length { get; private set; }
        public string message { get; private set; }
    }
}
