using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CommunicationProviders
{
    public class SocketAsync : ISocketAsyncProvider
    {
        IPAddress hostaddress;
        int port;
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        int MAX_BUFFER_SIZE = 4096;

        public SocketAsync(IPAddress hostaddress, int port)
        {
            this.hostaddress = hostaddress;
            this.port = port;
        }

        public bool ConnectAsync(EventHandler<SocketAsyncEventArgs> onCompletion)
        {
            if (socket.Connected)
            {
                // already connected, just invoke the 'on completion' event.
                onCompletion.DynamicInvoke(null, null);
                return false; // operation not pending - return synchronously
            }
            else
            {

                SocketAsyncEventArgs socketEventArgs = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = new IPEndPoint(this.hostaddress, this.port)
                };

                socketEventArgs.Completed += onCompletion;

                return socket.ConnectAsync(socketEventArgs);
            }
        }

        public bool ReceiveAsync(EventHandler<SocketAsyncEventArgs> handleResponse, bool ensureConnection = true)
        {
            if (ensureConnection)
            {
                if (this.ConnectAsync((s, e) =>
                {
                    this.ReceiveAsync(handleResponse, false);
                }))
                {
                    return true; // operation pending - return asynchronously
                }
                else
                {
                    // connection returned immediately - connection was executed synchronously
                    return this.ReceiveAsync(handleResponse, false);
                }
                
            }

            // We are re-using the _socket object initialized in the Connect method
            if (socket != null)
            {
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = socket.RemoteEndPoint,
                    UserToken = null
                };

                socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
                socketEventArg.Completed += handleResponse;

                return socket.ReceiveAsync(socketEventArg);
            }
            else
            {
                throw new InvalidOperationException("socket not initialized!");
            }
        }




        public bool SendAsync(string data, EventHandler<SocketAsyncEventArgs> handleResponse)
        {
            // We are re-using the _socket object initialized in the Connect method
            if (socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = socket.RemoteEndPoint,
                    UserToken = null
                };

                // Event handler for the Completed event.
                socketEventArg.Completed += handleResponse;

                // Add the data to be sent into the buffer
                byte[] payload = Encoding.UTF8.GetBytes(data);
                socketEventArg.SetBuffer(payload, 0, payload.Length);

                // Make an asynchronous Send request over the socket
                return socket.SendAsync(socketEventArg);
            }
            else
            {
                throw new InvalidOperationException("socket not initialized!");
            }
        }
    }
}
