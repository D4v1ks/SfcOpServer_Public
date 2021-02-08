using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SfcOpServer
{
    public abstract class AsyncServer : IDisposable
    {
        private const int DefaultBufferSize = 8192;

        private int _numConnections;

        private int _minimumBufferSize;
        private int _maximumBufferSize;

        private ConcurrentQueue<SocketAsyncEventArgs> _args;

        private Socket _listener;

        public abstract void BeginCloseUser(AsyncUser user);
        public abstract AsyncUser CreateUser(Socket sock);
        public abstract int GetSize(byte[] buffer, int size);
        public abstract void Handshake(AsyncUser user);
        public abstract int Process(AsyncUser user, byte[] buffer, int size);

        protected void Initialize(int numConnections, int minimumBufferSize, int maximumBufferSize)
        {
            Contract.Assert(numConnections > 0);

            _numConnections = numConnections;

            Contract.Assert(minimumBufferSize >= 0 && minimumBufferSize < maximumBufferSize);

            if (maximumBufferSize <= DefaultBufferSize)
            {
                Contract.Assert(DefaultBufferSize == (DefaultBufferSize - 1 >> 10) + 1 << 10);

                maximumBufferSize = DefaultBufferSize;
            }
            else
            {
                maximumBufferSize = (maximumBufferSize - 1 >> 10) + 1 << 10;
            }

            _minimumBufferSize = minimumBufferSize;
            _maximumBufferSize = maximumBufferSize;

            Contract.Assert(_args == null);

            _args = new ConcurrentQueue<SocketAsyncEventArgs>();

            CreateArgs(numConnections << 1);
        }

        public void Start(IPEndPoint localEP)
        {
            Contract.Assert(_listener == null);

            _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            _listener.Bind(localEP);
            _listener.Listen(_numConnections);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();

            args.Completed += AcceptCompleted;

            TryAccept(args);
        }

        public void Write(AsyncUser user, byte[] buffer, int offset, int size)
        {
            Contract.Assert(size >= _minimumBufferSize && size <= _maximumBufferSize);

            GetArgs(user, out SocketAsyncEventArgs args);

            if (user.ActiveArgs < _numConnections >> 3)
            {
                Buffer.BlockCopy(buffer, offset, args.Buffer, 0, size);

                args.SetBuffer(0, size);

                TrySend(args);

                return;
            }

#if VERBOSE
            Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " Write() -> CloseArgs(" + user.Id + ")");
#endif

            CloseArgs(args);
        }

        private void CreateArgs(int numArgs)
        {
            Contract.Assert(numArgs > 0);

            do
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();

                args.Completed += ReceiveOrSendCompleted;

                args.SetBuffer(new byte[_maximumBufferSize], 0, _maximumBufferSize);

                _args.Enqueue(args);

                numArgs--;
            }
            while (numArgs > 0);
        }

        private void GetArgs(AsyncUser user, out SocketAsyncEventArgs args)
        {
            while (!_args.TryDequeue(out args))
                CreateArgs(_numConnections >> 2);

            Contract.Assert(args.UserToken == null);

            args.UserToken = user;

            user.ActiveArgs++;
        }

        private void ReturnArgs(AsyncUser user, SocketAsyncEventArgs args)
        {
            user.ActiveArgs--;

            Contract.Assert(args.UserToken == user);

            args.UserToken = null;

            _args.Enqueue(args);
        }

        private void CloseArgs(SocketAsyncEventArgs args)
        {
            AsyncUser user = (AsyncUser)args.UserToken;

            ReturnArgs(user, args);

            if (Interlocked.Exchange(ref user.Closing, 1) < 1)
            {
                BeginCloseUser(user);

                Contract.Assert(user.Socket != null);

                user.EndCloseUser();
            }
        }

        private void TryAccept(SocketAsyncEventArgs args)
        {
            Contract.Assert(args.AcceptSocket == null);

            try
            {
                if (!_listener.AcceptAsync(args))
                    ProcessAccept(args);
            }
            catch (NullReferenceException)
            { }
            catch (ObjectDisposedException)
            { }
            catch (Exception)
            {

#if VERBOSE
                Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " TryAccept() -> TryCloseAndContinueAccepting()");
#endif

                TryCloseAndContinueAccepting(args);
            }
        }

        private void TryCloseAndContinueAccepting(SocketAsyncEventArgs args)
        {
            if (args.AcceptSocket != null)
            {
                args.AcceptSocket.Close();
                args.AcceptSocket = null;
            }

            TryAccept(args);
        }

        private void TryReceive(SocketAsyncEventArgs args)
        {
            AsyncUser user = (AsyncUser)args.UserToken;

            try
            {
                if (!user.Socket.ReceiveAsync(args))
                    ProcessReceive(args);
            }
            catch (Exception)
            {

#if VERBOSE
                Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " TryReceive() -> CloseArgs(" + user.Id + ")");
#endif

                CloseArgs(args);
            }
        }

        private void TrySend(SocketAsyncEventArgs args)
        {
            AsyncUser user = (AsyncUser)args.UserToken;

            try
            {
                if (!user.Socket.SendAsync(args))
                    ProcessSend(args);
            }
            catch (Exception)
            {

#if VERBOSE
                Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " TrySend() -> CloseArgs(" + user.Id + ")");
#endif

                CloseArgs(args);
            }
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            if (args.LastOperation == SocketAsyncOperation.Accept)
            {
                ProcessAccept(args);
            }
            else
            {

#if VERBOSE
                Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " AcceptCompleted() -> TryCloseAndContinueAccepting()");
#endif

                TryCloseAndContinueAccepting(args);
            }
        }

        private void ReceiveOrSendCompleted(object sender, SocketAsyncEventArgs args)
        {
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(args);
                    break;

                case SocketAsyncOperation.Send:
                    ProcessSend(args);
                    break;

                default:

#if VERBOSE
                    Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " ReceiveOrSendCompleted() -> CloseArgs(" + ((AsyncUser)args.UserToken).Id + ")");
#endif

                    CloseArgs(args);

                    break;
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptArgs)
        {
            if (acceptArgs.SocketError == SocketError.Success)
            {
                Socket sock = acceptArgs.AcceptSocket;

                Contract.Requires(sock != null);

                acceptArgs.AcceptSocket = null;

                //----------------------------------------------------------

                //sock.NoDelay = true;

                sock.ReceiveBufferSize = _maximumBufferSize;
                sock.SendBufferSize = _maximumBufferSize;

                sock.ReceiveTimeout = 120_000; // ms
                sock.SendTimeout = 120_000; // ms

                //----------------------------------------------------------

                AsyncUser user = CreateUser(sock);

                GetArgs(user, out SocketAsyncEventArgs receiveArgs);

                try
                {
                    Handshake(user);
                }
                catch (Exception)
                {

#if VERBOSE
                    Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " ProcessAccept() -> CloseArgs(" + user.Id + ")");
#endif

                    CloseArgs(receiveArgs);

                    goto returnArgs;
                }

                receiveArgs.SetBuffer(0, _maximumBufferSize);

                TryReceive(receiveArgs);
            }
            else
            {

#if VERBOSE
                Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " ProcessAccept() -> TryCloseAndContinueAccepting()");
#endif

            }

        returnArgs:

            TryCloseAndContinueAccepting(acceptArgs);
        }

        private void ProcessReceive(SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                int bytesTransferred = args.BytesTransferred;

                if (bytesTransferred > 0)
                {
                    int bytesAccumulated = args.Offset + bytesTransferred;

                    AsyncUser user = (AsyncUser)args.UserToken;

                    while (true)
                    {
                        int bytesExpected = GetSize(args.Buffer, bytesAccumulated);

                        if (bytesExpected < _minimumBufferSize || bytesExpected > _maximumBufferSize)
                            break;

                        if (bytesAccumulated < bytesExpected)
                        {
                            args.SetBuffer(bytesAccumulated, _maximumBufferSize - bytesAccumulated);

                            TryReceive(args);

                            return;
                        }

#if VERBOSE
                        Debug.WriteIf(_minimumBufferSize == 0 && args.Buffer[bytesExpected - 1] == 10, DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Client" + user.Id + "] " + Encoding.ASCII.GetString(args.Buffer, 0, bytesExpected));
                        Debug.WriteLineIf(_minimumBufferSize == 0 && args.Buffer[bytesExpected - 1] != 10, DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Client" + user.Id + "] " + Encoding.ASCII.GetString(args.Buffer, 0, bytesExpected));
                        Debug.WriteLineIf(_minimumBufferSize > 0, DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Client" + user.Id + "] " + Utils.GetHex(args.Buffer, bytesExpected));
#endif

                        if (Process(user, args.Buffer, bytesExpected) == 0)
                            break;

                        if (bytesAccumulated > bytesExpected)
                        {
                            bytesAccumulated -= bytesExpected;

                            Buffer.BlockCopy(args.Buffer, bytesExpected, args.Buffer, 0, bytesAccumulated);

                            continue;
                        }

                        Contract.Assert(bytesAccumulated == bytesExpected);

                        args.SetBuffer(0, _maximumBufferSize);

                        TryReceive(args);

                        return;
                    }
                }
            }

#if VERBOSE
            Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " ProcessReceive() -> CloseArgs(" + ((AsyncUser)args.UserToken).Id + ")");
#endif

            CloseArgs(args);
        }

        private void ProcessSend(SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                int bytesTransferred = args.BytesTransferred;

                if (bytesTransferred != args.Count)
                    throw new NotImplementedException();

                AsyncUser user = (AsyncUser)args.UserToken;

#if VERBOSE
                Debug.WriteIf(_minimumBufferSize == 0 && args.Buffer[args.Count - 1] == 10, DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Server" + user.Id + "] " + Encoding.ASCII.GetString(args.Buffer, 0, args.Count));
                Debug.WriteLineIf(_minimumBufferSize == 0 && args.Buffer[args.Count - 1] != 10, DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Server" + user.Id + "] " + Encoding.ASCII.GetString(args.Buffer, 0, args.Count));
                Debug.WriteLineIf(_minimumBufferSize > 0, DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Server" + user.Id + "] " + Utils.GetHex(args.Buffer, args.Count));
#endif

                ReturnArgs(user, args);
            }
            else
            {

#if VERBOSE
                Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " ProcessSend() -> CloseArgs(" + ((AsyncUser)args.UserToken).Id + ")");
#endif

                CloseArgs(args);
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_listener != null)
            {
                _listener.Close();

                _listener = null;
            }
        }
    }
}
