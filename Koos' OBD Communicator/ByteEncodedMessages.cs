using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Koos__OBD_Communicator
{
    public static class ByteEncodedMessages : Message
    {

        public static string getSupportedSensorsFromByteEncodedMessage(string checkedResponse)
        {
            string cleanedResponse = cleanReponse(checkedResponse);

            return cleanedResponse.Substring((cleanedResponse.Length - (getBytesInMessage(cleanedResponse) * 2)) + 4);
        }

    }
}
