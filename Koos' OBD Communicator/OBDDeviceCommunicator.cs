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
    class OBDDeviceCommunicator
    {
        public PID PIDInformation { get; private set; }

        public event EventHandler<OBDSensorDataEventArgs> RaiseOBDSensorData;
        public event EventHandler<ResponseEventArgs> RaiseInitResponse;
        public event EventHandler<ResponseEventArgs> RaisePIDResponse;
        ISocketSyncProvider socket;

        public OBDDeviceCommunicator(ISocketSyncProvider socket)
        {
            this.socket = socket;
        }

        public PID.SupportedStatus isSupported(int mode, int nPID)
        {
            return this.PIDInformation.isSupported(mode, nPID);
        }

        public string init_communication(ConfigurationData currentConfiguration)
        {
            this.PIDInformation = new PID();

            this.OnRaiseInitResponse(new ResponseEventArgs("Hello from init_communication!"));

            // connect
            this.socket.ConnectSync();
            
            //empty pipeline
            this.OnRaiseInitResponse(new ResponseEventArgs("Initial pipeline: " + this.socket.EmptyPipelineUntilNoResponseFor(500)));

            // reset device
            //this.socket.SendSync(vocabulary["RESET"]);
            this.socket.SendSync("AT Z\r");
            this.OnRaiseInitResponse(new ResponseEventArgs("[RI] AT Z: " + this.socket.ReceiveUntilLastCharacterIs('>')));

            // set line feed off
            //this.socket.SendSync(vocabulary["LF OFF"]);
            //this.socket.ReceiveUntilLastCharacterIs('>');
            this.socket.SendSync("AT L0\r");
            this.OnRaiseInitResponse(new ResponseEventArgs("[RI] AT L0: " + this.socket.ReceiveUntilLastCharacterIs('>')));

            // set headers on
            //this.socket.SendSync(vocabulary["HEADERS ON"]);
            //this.socket.ReceiveUntilLastCharacterIs('>');
            this.socket.SendSync("AT H1\r");
            this.OnRaiseInitResponse(new ResponseEventArgs("[RI] AT H1: " + this.socket.ReceiveUntilLastCharacterIs('>')));

            // set echo off
            //this.socket.SendSync(vocabulary["ECHO OFF"]);
            //this.socket.ReceiveUntilLastCharacterIs('>');
            this.socket.SendSync("AT E0\r");
            this.OnRaiseInitResponse(new ResponseEventArgs("[RI] AT E0: " + this.socket.ReceiveUntilLastCharacterIs('>')));

            foreach (SensorAvailability SensorsToCheck in currentConfiguration.sensorLists)
            {
                string message = SensorsToCheck.mode.ToString("D2") + " " + SensorsToCheck.PID.ToString("D2") + "\r";
                this.OnRaiseInitResponse(new ResponseEventArgs("Sending: " + message));
                this.socket.SendSync(message);
                string supportedPIDs = this.socket.ReceiveUntilLastCharacterIs('>');
                this.OnRaiseInitResponse(new ResponseEventArgs("[RR] " + message + ": " + supportedPIDs));
                if (!this.PIDInformation.parseSupportedPIDs(SensorsToCheck.mode, SensorsToCheck.firstPID, SensorsToCheck.lastPID, supportedPIDs))
                    this.OnRaiseInitResponse(new ResponseEventArgs("Not recognized " + message + ": " + supportedPIDs));
                    ; // well, shit happens. For now, just skip.
            }
            this.OnRaiseInitResponse(new ResponseEventArgs("Finished init."));

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
                        if (currentSensor != null && this.PIDInformation.isSupported(currentSensor.mode, currentSensor.PID) == PID.SupportedStatus.Supported)
                        {
                            // PID is beschikbaar. Message opstellen om mee uit te vragen (bijv. "01 02\r")
                            string message = currentSensor.mode.ToString("D2") + " " + currentSensor.PID.ToString("D2") + "\r";
                            this.socket.SendSync(message);

                            // Wacht de volgende ">" af.
                            string response = this.socket.ReceiveUntilLastCharacterIs('>');
                            if (Message.isValid(response) != Message.ResponseValidity.Valid)
                            {
                                // Als we niets met het teruggekomen bericht aankunnen, kunnen we het voor nu overslaan.
                                OnRaisePIDResponse(new ResponseEventArgs("Not valid: " + response));
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

        protected virtual void OnRaiseOBDSensorData(OBDSensorDataEventArgs eventArgs)
        {
            EventHandler<OBDSensorDataEventArgs> handler = RaiseOBDSensorData;

            // Only execute if there are any subscribers
            if (handler != null)
            {
                handler(this, eventArgs);
            }
        }

        protected virtual void OnRaiseInitResponse(ResponseEventArgs eventArgs)
        {
            EventHandler<ResponseEventArgs> handler = RaiseInitResponse;

            // Only execute if there are any subscribers
            if (handler != null)
            {
                handler(this, eventArgs);
            }
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

    public class OBDSensorDataEventArgs : EventArgs
    {
        public OBDSensorDataEventArgs(int mode, int PIDCode, int length, string message)
        {
            this.mode = mode;
            this.PIDCode = PIDCode;
            this.length = length;
            this.message = message;   
        }

        public int mode { get; private set; }
        public int PIDCode { get; private set; }
        public int length { get; private set; }
        public string message { get; private set; }
    }

    public class ResponseEventArgs : EventArgs
    {
        public ResponseEventArgs(string message)
        {
            this.message = message;
        }

        public string message { get; private set; }
    }
}
