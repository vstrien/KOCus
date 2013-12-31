using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CommunicationProviders
{
    public interface ISocketAsyncProvider : ISocketProvider
    {
        bool ConnectAsync(EventHandler<SocketAsyncEventArgs> onCompletion);

        bool ReceiveAsync(EventHandler<SocketAsyncEventArgs> handleResponse, bool ensureConnection = true);

        bool SendAsync(string data, EventHandler<SocketAsyncEventArgs> handleResponse);
    }
}
