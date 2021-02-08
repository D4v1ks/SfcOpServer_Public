#pragma warning disable CA1001, CA1031, CA1303

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SfcOpServer
{
    public class ManInTheMiddle
    {
        private const int CentralSwitchPort = 27000;

        private class Client
        {
            private const int MinimumBufferSize = 5;
            private const int MaximumBufferSize = 65536;

            private const int LocalDelay = 0; // ms
            private const int RemoteDelay = 0; // ms

            public bool IsRunning => _closing == 0;

            private static int _counter;

            private readonly int _id;
            private int _closing;

            private readonly IPEndPoint _localEP;

            private readonly TcpClient _localClient;
            private readonly NetworkStream _localStream;
            private readonly Thread _localThread;

            private readonly TcpClient _remoteClient;
            private readonly NetworkStream _remoteStream;
            private readonly Thread _remoteThread;

            public static void Initialize()
            {
                _counter = 0;
            }

            public Client(TcpClient tcpClient)
            {
                _id = Interlocked.Increment(ref _counter);
                _closing = 0;

                try
                {
                    _localEP = (IPEndPoint)tcpClient.Client.LocalEndPoint;

                    // local client

                    _localClient = tcpClient;
                    _localStream = _localClient.GetStream();

                    _localClient.NoDelay = true;
                    _localClient.ReceiveBufferSize = MaximumBufferSize;
                    _localClient.SendBufferSize = MaximumBufferSize;
                    _localClient.ReceiveTimeout = 0;
                    _localClient.SendTimeout = 0;

                    _localThread = new Thread(new ThreadStart(LocalFunction))
                    {
                        Name = "mitmLocalThread" + _localEP.Port + " -- " + _localClient.Client.RemoteEndPoint.ToString()
                    };

                    // remote client

                    _remoteClient = new TcpClient(_localEP.Address.ToString(), CentralSwitchPort + 1);
                    _remoteStream = _remoteClient.GetStream();

                    _remoteClient.NoDelay = true;
                    _remoteClient.ReceiveBufferSize = MaximumBufferSize;
                    _remoteClient.SendBufferSize = MaximumBufferSize;
                    _remoteClient.ReceiveTimeout = 0;
                    _remoteClient.SendTimeout = 0;

                    _remoteThread = new Thread(new ThreadStart(RemoteFunction))
                    {
                        Name = "mitmRemoteThread" + _localEP.Port + " -- " + _localClient.Client.RemoteEndPoint.ToString()
                    };

                    // threads

                    _localThread.Start();
                    _remoteThread.Start();

                    Console.WriteLine("The man-in-the-middle was created with success!");
                }
                catch (Exception)
                {
                    Close();
                }
            }

            private void LocalFunction()
            {
                try
                {
                    byte[] buffer = new byte[MaximumBufferSize];

                    while (true)
                    {
                        int offset = 0;

                        // reads the header of the message (4 bytes)

                        do
                        {
                            int bytesRead = _localStream.Read(buffer, offset, 4 - offset);

                            if (bytesRead == 0)
                                return;

                            offset += bytesRead;
                        }
                        while (offset < 4);

                        Contract.Assert(offset == 4);

                        // checks the size of the message

                        int size = BitConverter.ToInt32(buffer, 0);

                        if (size < MinimumBufferSize || size > MaximumBufferSize)
                            break;

                        // reads the rest of the message

                        do
                        {
                            int bytesRead = _localStream.Read(buffer, offset, size - offset);

                            if (bytesRead == 0)
                                return;

                            offset += bytesRead;
                        }
                        while (offset < size);

                        Contract.Assert(offset == size);

                        // processes the messages

                        Thread.Sleep(LocalDelay);

                        Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Client" + _id + "] " + Utils.GetHex(buffer, size));

                        _remoteStream.Write(buffer, 0, size);
                    }
                }
                catch (Exception)
                { }
                finally
                {
                    Close();
                }
            }

            private void RemoteFunction()
            {
                try
                {
                    byte[] buffer = new byte[MaximumBufferSize];

                    while (true)
                    {
                        int offset = 0;

                        // reads the header of the message (4 bytes)

                        do
                        {
                            int bytesRead = _remoteStream.Read(buffer, offset, 4 - offset);

                            if (bytesRead == 0)
                                return;

                            offset += bytesRead;
                        }
                        while (offset < 4);

                        Contract.Assert(offset == 4);

                        // checks the size of the message

                        int size = BitConverter.ToInt32(buffer, 0);

                        if (size < MinimumBufferSize || size > MaximumBufferSize)
                            break;

                        // reads the rest of the message

                        do
                        {
                            int bytesRead = _remoteStream.Read(buffer, offset, size - offset);

                            if (bytesRead == 0)
                                return;

                            offset += bytesRead;
                        }
                        while (offset < size);

                        Contract.Assert(offset == size);

                        // processes the messages

                        Thread.Sleep(RemoteDelay);

                        Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Server" + _id + "] " + Utils.GetHex(buffer, size));

                        _localStream.Write(buffer, 0, size);
                    }
                }
                catch (Exception)
                { }
                finally
                {
                    Close();
                }
            }

            private void Close()
            {
                if (Interlocked.Exchange(ref _closing, 1) == 0)
                {
                    _localStream?.Close();
                    _localClient?.Close();

                    _remoteStream?.Close();
                    _remoteClient?.Close();
                }
            }
        }

        private readonly IPAddress _localIP;

        private readonly TcpListener _listener;
        private readonly Thread _listenerThread;

        private readonly Thread _udpThread;

        public ManInTheMiddle(IPAddress privateIP)
        {
            try
            {
                Contract.Assert(CentralSwitchPort == 27000);

                // static stuff

                Client.Initialize();

                // variables

                _localIP = privateIP;

                // listener

                _listener = new TcpListener(_localIP, CentralSwitchPort);

                _listenerThread = new Thread(new ThreadStart(ListenerFunction))
                {
                    IsBackground = true,
                    Name = "mitmListener" + CentralSwitchPort
                };

                // udp

                _udpThread = new Thread(new ThreadStart(UdpFunction))
                {
                    IsBackground = true,
                    Name = "mitmUdp" + CentralSwitchPort
                };

                // threads

                _listenerThread.Start();

                _udpThread.Start();
            }
            catch (Exception)
            { }
        }

        private void ListenerFunction()
        {
            try
            {
                _listener.Start();

                IPEndPoint localEP = (IPEndPoint)_listener.LocalEndpoint;

                Console.WriteLine("The man-in-the-middle is listenning on port " + localEP.Port + ".");

                while (true)
                {
                    TcpClient tcpClient = _listener.AcceptTcpClient();

                    _ = new Client(tcpClient);
                }
            }
            catch (Exception)
            { }
            finally
            {
                _listener.Stop();
            }
        }

        private void UdpFunction()
        {
            IPEndPoint localEP = null;
            UdpClient client = null;

            try
            {
                localEP = new IPEndPoint(_localIP, CentralSwitchPort);
                client = new UdpClient(localEP);

                // adds the server profile and sets its default status message

                GsService.AddServer(localEP);

                byte[] serverStatus = Encoding.UTF8.GetBytes("\\gamename\\sfc2op\\gamever\\1.6\\location\\0\\serverver\\2.5.6.4\\validclientver\\2.5.6.4\\hostname\\Standard\\hostport\\" + CentralSwitchPort + "\\mapname\\StandardMap.mvm StandardMap.mvm StandardMap.mvm\\gametype\\Man-in-the-middle\\maxnumplayers\\3000\\numplayers\\0\\maxnumloggedonplayers\\64\\numloggedonplayers\\0\\gamemode\\Open\\racelist\\0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 \\password\\\\final\\\\queryid\\1.1");

                // starts receiving

                Dictionary<string, string> d = new Dictionary<string, string>();

                while (true)
                {
                    try
                    {
                        IPEndPoint dataEP = null;

                        byte[] datagram = client.Receive(ref dataEP);

                        Utils.GetArguments(datagram, datagram.Length, ref d);

                        if (d.ContainsKey("status"))
                            client.Send(serverStatus, serverStatus.Length, dataEP);

                        d.Clear();
                    }
                    catch (Exception)
                    { }
                }
            }
            catch (Exception)
            { }
            finally
            {
                GsService.TryRemoveServer(localEP);

                client?.Close();
            }
        }
    }
}
