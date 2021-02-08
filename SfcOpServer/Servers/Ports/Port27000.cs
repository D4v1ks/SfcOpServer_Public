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
    public class Port27000
    {
        private readonly byte[][] _data;

        private readonly GameServer _server;
        private readonly IPEndPoint _localEP;

        private readonly ConcurrentQueue<byte[]> _outgoingQueue;
        private readonly ManualResetEventSlim _outgoingEvent;

        private readonly Thread _incomingThread;
        private readonly Thread _outgoingThread;

        private UdpClient _incomingClient;
        private UdpClient _outgoingClient;

        private int _state;

        public Port27000(int serverId, IPAddress localIP, int localPort)
        {
            _data = new byte[][] {
                // \status\
                new byte[] { 92, 115, 116, 97, 116, 117, 115, 92 },
                // \id\
                new byte[] { 92, 105, 100, 92 }
            };

            if (!GameServer.TryGetServer(serverId, out GameServer server))
                throw new NotSupportedException();

            _server = server;
            _localEP = new IPEndPoint(localIP, localPort);

            _outgoingQueue = new ConcurrentQueue<byte[]>();
            _outgoingEvent = new ManualResetEventSlim(false);

            _incomingThread = new Thread(new ThreadStart(IncomingFunction))
            {

#if DEBUG
                Name = "Port <- " + localPort
#endif

            };

            _outgoingThread = new Thread(new ThreadStart(OutgoingFunction))
            {
                IsBackground = true,

#if DEBUG
                Name = "Port -> " + localPort
#endif

            };

            _incomingClient = null;
            _outgoingClient = null;

            _state = 0;
        }

        public void Start()
        {
            try
            {
                _incomingThread.Start();
                _outgoingThread.Start();
            }
            catch (Exception)
            {
                Close();
            }
        }

        public void Close()
        {
            if (Interlocked.Exchange(ref _state, -1) == 0)
            {
                try
                {
                    _incomingClient?.Close();
                    _outgoingClient?.Close();

                    _outgoingEvent.Dispose();

                    while (_outgoingQueue.TryDequeue(out byte[] buffer))
                        GameServer.Return(buffer);
                }
                catch (Exception)
                { }
            }
        }

        public unsafe void Enqueue(long address, int port, byte[] msg)
        {
            Contract.Assert(address > 0 && port > 0 && msg.Length > 0);

            int msgLength = msg.Length;

            GameServer.Rent(msgLength + 16, out byte[] buffer);

            fixed (byte* b = buffer)
            {
                *(long*)b = address;
                *(int*)(b + 8) = port;
                *(int*)(b + 12) = msgLength;
            }

            Buffer.BlockCopy(msg, 0, buffer, 16, msgLength);

            _outgoingQueue.Enqueue(buffer);
            _outgoingEvent.Set();
        }

        private void InitializeClient(ref UdpClient client)
        {
            client = new UdpClient()
            {
                ExclusiveAddressUse = false
            };

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(_localEP);
        }

        private void IncomingFunction()
        {
            try
            {
                InitializeClient(ref _incomingClient);

                GsService.AddServer(_localEP);

                while (_state == 0)
                {
                    try
                    {
                        IPEndPoint remoteEP = null;
                        byte[] buffer = _incomingClient.Receive(ref remoteEP);

#if VERBOSE
                        Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [UdpClient] " + Encoding.ASCII.GetString(buffer, 0, buffer.Length));
#endif

                        if (Utils.EqualsTo(buffer, buffer.Length, _data[0]))
                        {
                            buffer = remoteEP.Address.GetAddressBytes();

                            Contract.Assert(buffer.Length == 4);

                            long address = BitConverter.ToUInt32(buffer, 0);
                            int port = remoteEP.Port;
                            byte[] msg = _server.Status;

                            Enqueue(address, port, msg);
                        }
                        else if (Utils.StartsWith(buffer, buffer.Length, _data[1]))
                        {
                            string id = Encoding.UTF8.GetString(buffer, _data[1].Length, buffer.Length - _data[1].Length);

                            if (int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out int clientId))
                                _server.CheckClient(clientId);
                        }
                    }
                    catch (Exception)
                    { }
                }
            }
            catch (Exception)
            { }
            finally
            {
                Close();

                GsService.TryRemoveServer(_localEP);
            }
        }

        private unsafe void OutgoingFunction()
        {
            try
            {
                InitializeClient(ref _outgoingClient);

                byte[] buffer = null;
                byte[] msg = null;

                while (_state == 0)
                {
                    _outgoingEvent.Wait();

                    while (_outgoingQueue.TryDequeue(out buffer))
                    {
                        try
                        {
                            Contract.Assert(buffer.Length > 16);

                            // address (8 bytes)
                            // port (4 bytes)
                            // msg length (4 bytes)

                            long address;
                            int port;
                            int msgLength;

                            fixed (byte* b = buffer)
                            {
                                address = *(long*)b;
                                port = *(int*)(b + 8);
                                msgLength = *(int*)(b + 12);
                            }

                            Contract.Assert(address > 0 && port > 0 && msgLength > 0);

                            GameServer.Rent(msgLength, out msg);

                            Buffer.BlockCopy(buffer, 16, msg, 0, msgLength);

                            int bytesRemaining = msgLength;

                            IPEndPoint remoteEP = new IPEndPoint(address, port);
                            
                            while (true)
                            {
                                int bytesSent = _outgoingClient.Send(msg, bytesRemaining, remoteEP);

                                if (bytesRemaining == bytesSent)
                                    break;

                                bytesRemaining -= bytesSent;

                                Buffer.BlockCopy(msg, bytesSent, msg, 0, bytesRemaining); 
                            }

#if VERBOSE
                            Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [UdpServer] " + msgLength.ToString(CultureInfo.InvariantCulture) + " bytes sent");
#endif

                        }
                        catch (Exception)
                        { }
                        finally
                        {
                            if (buffer != null)
                            {
                                GameServer.Return(buffer);

                                buffer = null;
                            }

                            if (msg != null)
                            {
                                GameServer.Return(msg);

                                msg = null;
                            }
                        }
                    }

                    _outgoingEvent.Reset();
                }
            }
            catch (Exception)
            { }
            finally
            {
                Close();
            }
        }
    }
}
