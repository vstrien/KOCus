using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator
{
    public class PID
    {
        public enum SupportedStatus { Supported, Unsupported, Unknown };
        public SupportedStatus[][] supportedPIDs { get; private set; }

        public const int defaultNumberOfModes = 9;
        public const int defaultNumberOfPIDsPerMode = 0xFF;

        public PID(int numberOfModes = defaultNumberOfModes, int PIDsPerMode = defaultNumberOfPIDsPerMode)
        {
            this.supportedPIDs = new SupportedStatus[numberOfModes][];
            for (int i = 0; i < this.supportedPIDs.Count(); i++)
            {
                this.supportedPIDs[i] = new SupportedStatus[PIDsPerMode];
                for (int j = 0; j < PIDsPerMode; j++)
                {
                    this.supportedPIDs[i][j] = SupportedStatus.Unknown;
                }
            }
        }

        public SupportedStatus isSupported(int mode, int PID)
        {
            return supportedPIDs[mode - 1][PID];
        }

        public bool parseSupportedPIDs(int mode, int startPID, int endPID, string response)
        {
            ulong u_startPID = (ulong)startPID;
            ulong u_endPID = (ulong)endPID;

            if (Message.isValid(response) == Message.ResponseValidity.Valid)
            {
                string supportedSensors = Message.getMessageContents(response);

                UInt64 nHex = UInt64.Parse(supportedSensors, NumberStyles.HexNumber);
                for (ulong currentPID_absolute = u_startPID; currentPID_absolute <= u_endPID; currentPID_absolute++)
                {
                    ulong currentPID_relative = u_endPID - currentPID_absolute;
                    ulong currentPID_bit = (ulong)Math.Pow(2, currentPID_relative);

                    if ((nHex & currentPID_bit) == currentPID_bit)
                        this.supportedPIDs[mode - 1][currentPID_absolute] = SupportedStatus.Supported;
                    else
                        this.supportedPIDs[mode - 1][currentPID_absolute] = SupportedStatus.Unsupported;
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
