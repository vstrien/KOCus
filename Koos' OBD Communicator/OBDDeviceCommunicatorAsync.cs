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

        public event EventHandler<OBDSensorDataEventArgs> RaiseOBDSensorData;
        public event EventHandler<ResponseEventArgs> RaiseInitResponse;
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
        
        public void checkSensorsAsync()
        {    
            foreach (SensorAvailability SensorsToCheck in this.configuration.sensorAvailabilityList)
            {
                string message = SensorsToCheck.mode.ToString("D2") + " " + SensorsToCheck.PID.ToString("D2") + "\r";

                this.socket.SendAsync(message, null);
            }
        }

        // Haal voor alle beschikbare sensors waarden op
        public void getSensorValuesAsync()
        {
            /* TODO: Op dit moment is er een lijst met beschikbare sensors. Dat voldoet niet aan de werkelijkheid, waar de lijst een boomstructuur is.
             * Dit moet aangepast worden, zodat het uitvragen van elke sensor identiek gebeurt.
             */
            foreach (SensorAvailability AvailableSensors in this.configuration.sensorAvailabilityList)
            {
                // Wanneer van een sensor-range (van 32 sensors) de beschikbaarheid onbekend is, is uitvragen / loopen zinloos.
                if (this.PIDInformation.isSupported(AvailableSensors.mode, AvailableSensors.firstPID) != PID.SupportedStatus.Unknown)
                {
                    foreach (var currentSensor in AvailableSensors.PIDSensors)
                    {
                        // Alleen wanneer een PID ook daadwerkelijk beschikbaar & ondersteund is, mag deze uitgevraagd worden.
                        if (currentSensor != null && this.PIDInformation.isSupported(currentSensor.mode, currentSensor.PID) == PID.SupportedStatus.Supported)
                        {
                            // PID is beschikbaar. Message opstellen om mee uit te vragen (bijv. "01 02\r")
                            string message = currentSensor.mode.ToString("D2") + " " + currentSensor.PID.ToString("D2") + "\r";
                            this.socket.SendAsync(message, null);
                        }
                    }
                }
            }
        }

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

                string cleanedResponse = Message.cleanReponse(response);

                // get PID from message
                int PID = Message.getPIDOfMessage(cleanedResponse);

                /* TODO: 
                 * This (the split check between a sensor availability message and a sensor value message) is not a nice implementation. 
                 * In essence, every PID message is the same, and should be handled with a class that conforms to an interface.
                 */

                // Check: is this  a sensor availability message?
                var available = this.configuration.sensorAvailabilityList.Where(sensorAvail => (sensorAvail.PID == PID));
                if (available.Count() > 0)
                {
                    // It's a sensor availability message
                    var sensor = available.First();

                    this.PIDInformation.parseSupportedPIDs(sensor.mode, sensor.firstPID, sensor.lastPID, response);
                }
                else
                {
                    // It's a sensor value message
                    
                    // Retrieve mode, PID, bytes:

                    OnRaiseOBDSensorData(
                        new OBDSensorDataEventArgs(
                            Message.getModeOfMessage(cleanedResponse), 
                            PID, 
                            Message.getBytesInMessage(cleanedResponse), 
                            Message.getMessageContents(cleanedResponse))
                    );
                }
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

}
