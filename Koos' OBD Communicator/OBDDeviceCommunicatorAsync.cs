using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using CommunicationProviders;

namespace Koos__OBD_Communicator
{
    class OBDDeviceCommunicatorAsync
    {
        public PID PIDInformation { get; private set; }

        public event EventHandler<ResponseEventArgs> RaisePIDResponse;
        public ConfigurationData configuration { get; set; }
        ISocketAsyncProvider socket;

        public OBDDeviceCommunicatorAsync(ISocketAsyncProvider socket, ConfigurationData currentConfiguration)
        {
            this.socket = socket;
            this.configuration = currentConfiguration;
        }

        public PID.SupportedStatus isSupported(int mode, int nPID)
        {
            return this.PIDInformation.isSupported(mode, nPID);
        }

        public void init_communication()
        {
            this.PIDInformation = new PID();

            // connect
            this.socket.ConnectAsync((s, e) =>
            {
                // reset device
                //this.socket.SendSync(vocabulary["RESET"]);
                this.socket.SendAsync("AT Z\r", (s1, e1) =>
                {
                    // set line feed off
                    //this.socket.SendSync(vocabulary["LF OFF"]);
                    //this.socket.ReceiveUntilLastCharacterIs('>');
                    this.socket.SendAsync("AT L0\r", null);
                    
                    // set headers on
                    //this.socket.SendSync(vocabulary["HEADERS ON"]);
                    //this.socket.ReceiveUntilLastCharacterIs('>');
                    this.socket.SendAsync("AT H1\r", null);
                    
                    // set echo off
                    //this.socket.SendSync(vocabulary["ECHO OFF"]);
                    //this.socket.ReceiveUntilLastCharacterIs('>');
                    this.socket.SendAsync("AT E0\r", null);
                });
            });
        }
        
        /// <summary>
        /// For all available sensors (all sensors that we can handle, and are indicated by the car as 'available'):
        /// Send out a request for new values
        /// </summary>
        public void checkSensorsAsync()
        {    
            var availableSensors = this.configuration.availableSensors();
            foreach (PIDSensor SensorsToCheck in availableSensors)
            {
                string message = SensorsToCheck.mode.ToString("D2") + " " + SensorsToCheck.PID.ToString("D2") + "\r";

                this.socket.SendAsync(message, null);
            }
        }

        /// <summary>
        /// Receives async messages from the OBD system, sends them to the configuration's parser
        /// </summary>
        public void getAndHandleResponse()
        {
            this.socket.ReceiveAsync((s, eventArgs) =>
            {
                if (eventArgs.SocketError != SocketError.Success)
                    return;
                
                string response = Encoding.UTF8.GetString(eventArgs.Buffer, eventArgs.Offset, eventArgs.BytesTransferred);
                response = response.Trim('\0', '\n', '\r');

                if (Message.isValid(response) != Message.ResponseValidity.Valid)
                    return;

                this.configuration.parseOBDResponse(response);

                //OnRaiseOBDSensorData(
                //        new OBDSensorDataEventArgs(
                //            Message.getModeOfMessage(cleanedResponse), 
                //            PID, 
                //            Message.getBytesInMessage(cleanedResponse), 
                //            Message.getMessageContents(cleanedResponse))
                //    );
            });
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
