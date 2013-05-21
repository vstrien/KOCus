using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace CommunicationProviders
{
    public interface ISocketSyncProvider : ISocketProvider
    {
        bool ConnectSync();

        string ReceiveSync();

        SocketError SendSync(string message, bool ensureConnection = true);

        string EmptyPipelineUntilNoResponseFor(int timeoutPerRead_ms = 100);

        string ReceiveUntilLastCharacterIs(char targetCharacter);
    }
}
