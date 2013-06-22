using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CommunicationProviders
{
    public class SocketSync : SocketAsync , ISocketSyncProvider
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        int MAX_BUFFER_SIZE = 4096;

        public SocketSync(IPAddress hostaddress, int port) : base(hostaddress, port) { }


        public bool ConnectSync()
        {
            ManualResetEvent connectionDone = new ManualResetEvent(false);
            bool connected = false;

            if (socket.Connected) // already connected
            {
                connected = true;
            }
            else
            {
                base.ConnectAsync((socketObject, eventArgs) =>
                {
                    connected = eventArgs.SocketError == SocketError.Success;
                    connectionDone.Set();
                });

                connectionDone.Reset();
                connectionDone.WaitOne();
            }

            return connected;
        }

        public string ReceiveSync()
        {
            string response = "";
            ManualResetEvent receiveDone = new ManualResetEvent(false);

            EventHandler<SocketAsyncEventArgs> read_complete_buffer = new EventHandler<SocketAsyncEventArgs>(delegate(object socketObject, SocketAsyncEventArgs eventArgs)
            {
                if (eventArgs.SocketError == SocketError.Success)
                {
                    response += Encoding.UTF8.GetString(eventArgs.Buffer, eventArgs.Offset, eventArgs.BytesTransferred);
                    response = response.Trim('\0', '\n', '\r');
                }

                receiveDone.Set();
            });

            base.ReceiveAsync(read_complete_buffer);
            
            receiveDone.Reset();
            receiveDone.WaitOne();

            return response;
        }

        public SocketError SendSync(string message, bool ensureConnection = true)
        {
            SocketError result = SocketError.NotConnected;

            if(ensureConnection)
                this.ConnectSync();

            ManualResetEvent sendDone = new ManualResetEvent(false);

            base.SendAsync(message, (s, e) =>
            {
                result = e.SocketError;

                sendDone.Set();
            });

            sendDone.Reset();
            sendDone.WaitOne();

            return result;
        }


        public string EmptyPipelineUntilNoResponseFor(int timeoutPerRead_ms = 100)
        {

            bool receivedNewData = false;
            string response = "";

            // receive all data from buffer.
            // This achieved through polling the buffer until no response comes back
            do
            {
                receivedNewData = false;

                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = socket.RemoteEndPoint,
                    UserToken = null
                };

                socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);

                base.ReceiveAsync((s, e) =>
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        response += Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                        response = response.Trim('\0');
                        receivedNewData = true;
                    }
                });

                Thread.Sleep(timeoutPerRead_ms);

            } while (receivedNewData);

            return response;
        }

        
        public string ReceiveUntilLastCharacterIs(char targetCharacter)
        {
            string response = "";

            // receive all data from buffer.
            // This achieved through polling the buffer until no response comes back
            do
            {
                string newResponse = this.ReceiveSync();
                response += newResponse;

            } while (response.Length > 0 && response.Substring(response.Length - 1) != targetCharacter.ToString());

            return response;
        }

    }
}
