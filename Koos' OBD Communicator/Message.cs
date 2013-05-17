using System;
using System.Collections.Generic;
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
            return response.Trim(new Char[] { ' ', '\r', '>' }).Replace(" ", string.Empty);
        }

        public static string getMessageContents(string checkedResponse)
        {
            string cleanedResponse = cleanReponse(checkedResponse);

            return cleanedResponse.Substring((cleanedResponse.Length - (getBytesInMessage(cleanedResponse) * 2)) + 4);
        }
    }
}
