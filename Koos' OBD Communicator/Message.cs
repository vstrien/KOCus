using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator
{
    public static class Message
    {
        public enum ResponseValidity { Valid, InvalidHeader, InvalidSize, InvalidContents };

        public static ResponseValidity isValid(string response)
        {
            string cleanedResponse = cleanReponse(response);

            // cleaned: "7E8064120A007B011"
            // Start het bericht met 7E8?
            if (!validHeader(cleanedResponse))
                return ResponseValidity.InvalidHeader;

            // Volgende controle: Hoeveel bytes komen er terug?
            // Klopt dat met het aantal bytes dat nog resteert?
            if (!validSize(cleanedResponse))
                return ResponseValidity.InvalidSize;

            return ResponseValidity.Valid;
        }

        public static bool validSize(string cleanedResponse)
        {
            if (cleanedResponse.Length <= 9)
                return false;

            int numberOfBytesInMessage = getBytesInMessage(cleanedResponse);

            return (numberOfBytesInMessage * 2 == cleanedResponse.Substring(5).Length);
        }

        public static int getBytesInMessage(string cleanedResponse)
        {
            return Convert.ToInt32(cleanedResponse.Substring(3, 2), 16);
        }

        public static bool validHeader(string cleanedResponse)
        {
            return (cleanedResponse.Length >= 3 && cleanedResponse.Substring(0, 3) == "7E8");
        }

        public static string cleanReponse(string response)
        {
            return response.Trim(new Char[] { ' ', '\r', '>', '\n' }).Replace(" ", string.Empty);
        }

        public static string getMessageContents(string checkedResponse)
        {
            string cleanedResponse = cleanReponse(checkedResponse);

            return cleanedResponse.Substring((cleanedResponse.Length - (getBytesInMessage(cleanedResponse) * 2)) + 4);
        }

        public static ResponseDetails getMessageContentDetails(string checkedResponse)
        {
            string cleanedResponse = cleanReponse(checkedResponse);

            ResponseDetails response = new ResponseDetails() {
                bytes = getBytesInMessage(cleanedResponse),
                mode = getModeOfMessage(cleanedResponse),
                PID = getPIDOfMessage(cleanedResponse),
                message = getMessageContents(cleanedResponse)
            };

            return response;
        }

        public static int getPIDOfMessage(string cleanedResponse)
        {
            // cleaned: "7E8064120A007B011"
            // PID = byte 7, 2 tekens 20.
            return int.Parse(cleanedResponse.Substring(7, 2), NumberStyles.HexNumber);
        }

        public static int getModeOfMessage(string cleanedResponse)
        {
            // cleaned: "7E8064120A007B011"
            // Mode = byte 5, 2 tekens (41) - 40.
            return int.Parse(cleanedResponse.Substring(5, 2), NumberStyles.HexNumber) - 0x40;
        }
    }

    public struct ResponseDetails
    {
        public int bytes;
        public int mode;
        public int PID;
        public string message;
    }
}
