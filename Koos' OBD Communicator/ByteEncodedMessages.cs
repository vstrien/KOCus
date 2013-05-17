using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator
{
    public static class ByteEncodedMessages
    {

        public enum ResponseValidity { Valid, InvalidHeader, InvalidSize, InvalidContents };

        public static ResponseValidity checkByteEncodedMessage(string response)
        {
            string voorbeeldResponse = "7E8 06 41 20 A0 07 B0 11 \r\r>";
            // betekenis:
            // 7E8: start bericht
            // 06: zes bytes
            // 41: mode 01 (+ 40)
            // 20: ?? (PID supported?)
            // A0 07 B0 11
            // 1010 0000 0000 0111 1011 0000 0001 0001
            
            string cleanedResponse = cleanReponse(response);
            // cleaned: "7E8064120A007B011"
            // Eerst controle: start het bericht met 7E8?
            if (!validHeader(cleanedResponse))
                return ResponseValidity.InvalidHeader;
            //cleanedResponse = cleanedResponse.Substring(3);

            // Volgende controle: hoeveel bytes komen er terug?
            // Klopt dat met het aantal bytes dat nog resteert?
            if(!validSize(cleanedResponse))
                return ResponseValidity.InvalidSize;

            return ResponseValidity.Valid;

        }

        public static string getSupportedSensorsFromByteEncodedMessage(string checkedResponse)
        {
            string cleanedResponse = cleanReponse(checkedResponse);

            return cleanedResponse.Substring((cleanedResponse.Length - (getBytesInMessage(cleanedResponse) * 2)) + 4);
        }

        public static bool validSize(string cleanedResponse)
        {
            if(cleanedResponse.Length <= 9)
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
            return response.Trim(new Char[] { ' ', '\r', '>' }).Replace(" ", string.Empty);
        }
    }
}
