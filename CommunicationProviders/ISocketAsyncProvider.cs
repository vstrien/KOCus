using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CommunicationProviders
{
    interface ISocketAsyncProvider : ISocketProvider
    {
        void ConnectAsync(EventHandler<SocketAsyncEventArgs> onCompletion);

        void ReceiveAsync(EventHandler<SocketAsyncEventArgs> handleResponse, bool ensureConnection = true);

        void SendAsync(string data, EventHandler<SocketAsyncEventArgs> handleResponse);
    }
}
