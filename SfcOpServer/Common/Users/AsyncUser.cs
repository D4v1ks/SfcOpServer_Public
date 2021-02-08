#pragma warning disable CA1031, CA1051, CA2211

using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Threading;

namespace SfcOpServer
{
    public class AsyncUser
    {
        private static int _nextId;

        public int Id;

        public Socket Socket;

        public int ActiveArgs;
        public int Closing;

        public void InitializeUser(Socket sock)
        {
            Contract.Assert(Id == 0);

            Id = Interlocked.Increment(ref _nextId);

            Socket = sock;

            ActiveArgs = 0;
            Closing = 0; // 0 connected, 1 closing, 2 closed
        }

        public void EndCloseUser()
        {
            if (Interlocked.Exchange(ref Closing, 2) < 2)
            {
                try
                {
                    Socket.Shutdown(SocketShutdown.Send);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                Socket.Close();
            }
        }
    }
}
