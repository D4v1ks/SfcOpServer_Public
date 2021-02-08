#pragma warning disable CA1031, CA1051, CA1819

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace SfcOpServer
{
    public partial class GameServer : IDisposable
    {
        public const int MaxNumPlayers = 100;

        // private static variables

        private static int _serverCounter;
        private static ConcurrentDictionary<int, GameServer> _servers;

        // public variables

        public int Id { get; }
        public byte[] Status { get; set; }

        // private variables

        private readonly IPEndPoint _localEP;
        private readonly string _root;
        private readonly string _clientVersion;

        private readonly ConcurrentDictionary<int, Client27000> _clients;

        private readonly Port27000 _port27000;
        private readonly Server27000 _server27000;
        private readonly Stream6667 _stream6667;

        private readonly Thread _thread;

        protected bool _disposed;

        public static void Initialize()
        {
            _serverCounter = 0;
            _servers = new ConcurrentDictionary<int, GameServer>();
        }

        public static bool TryGetServer(int id, out GameServer server)
        {
            if (_servers.TryGetValue(id, out server))
                return true;

            server = null;

            return false;
        }

        public GameServer(IPAddress localIP, int localPort, string root, string version)
        {
            // private static variables

            int serverId = Interlocked.Increment(ref _serverCounter);

            if (!_servers.TryAdd(serverId, this))
                throw new NotSupportedException();

            // public variables

            Id = serverId;
            Status = Array.Empty<byte>();

            // private variables

            root = Path.GetFullPath(root).Replace('\\', '/');

            if (!root.EndsWith('/'))
                root += "/";

            _localEP = new IPEndPoint(localIP, localPort);
            _root = root;
            _clientVersion = version;

            _clients = new ConcurrentDictionary<int, Client27000>();

            _port27000 = new Port27000(serverId, localIP, localPort);
            _server27000 = new Server27000(serverId);
            _stream6667 = new Stream6667(localIP);

            _thread = new Thread(new ThreadStart(ThreadFunction))
            {

#if DEBUG
                Name = "Server" + localPort
#endif

            };
        }

        // byte pool

        public static void Rent(int size, out byte[] b)
        {
            b = ArrayPool<byte>.Shared.Rent((size - 1 >> 10) + 1 << 10); // the memory is not set to zero!
        }

        private static void Rent(int size, out byte[] b, out MemoryStream m, out BinaryWriter w)
        {
            Rent(size, out b);

            m = new MemoryStream(b);
            w = new BinaryWriter(m, Encoding.UTF8, true);
        }

        private static void Rent(int size, out byte[] b, out MemoryStream m, out BinaryWriter w, out BinaryReader r)
        {
            Rent(size, out b);

            m = new MemoryStream(b);
            w = new BinaryWriter(m, Encoding.UTF8, true);
            r = new BinaryReader(m, Encoding.UTF8, true);
        }

        public static void Return(byte[] b)
        {
            ArrayPool<byte>.Shared.Return(b);
        }

        private static void Return(byte[] b, MemoryStream m, BinaryWriter w)
        {
            Contract.Requires(m != null && w != null);

            w.Close();
            m.Close();

            Return(b);
        }

        private static void Return(byte[] b, MemoryStream m, BinaryWriter w, BinaryReader r)
        {
            Contract.Requires(m != null && w != null);

            r.Close();
            w.Close();
            m.Close();

            Return(b);
        }

        // clients

        public void AddClient(Client27000 client)
        {
            if (!_clients.TryAdd(client.Id, client))
                throw new NotSupportedException();
        }

        public void CheckClient(int clientId)
        {
            if (_clients.TryGetValue(clientId, out Client27000 client))
            {
                Contract.Assert(client.Address == 0);

                client.Address = GetAddress(client);
            }
        }

        private static long GetAddress(Client27000 client)
        {
            long result;

            try
            {
                result = BitConverter.ToUInt32(((IPEndPoint)client.Socket.RemoteEndPoint).Address.GetAddressBytes(), 0);
            }
            catch (Exception)
            {
                result = 0;
            }

            return result;
        }

        public void EnqueueMessage(Client27000 client, byte[] buffer, int size)
        {
            Contract.Assert(size > 0);

            Rent(size, out byte[] msg);

            Buffer.BlockCopy(buffer, 0, msg, 0, size);

            ClientMessage clientMsg = new ClientMessage()
            {
                Buffer = msg,
                Size = size,

                TimeStamp = _smallTicks
            };

            client.Messages.Enqueue(clientMsg);
        }

        // main thread

        public void Start()
        {
            try
            {
                _thread.Start();
            }
            catch (Exception)
            {
                Close();
            }
        }

        public void Close()
        {
            try
            {
                if (_thread.IsAlive)
                    _thread.Interrupt();

                _port27000.Close();
                _server27000.Dispose();

                IrcService.TryQuitClient(_stream6667);
            }
            catch (Exception)
            { }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void ThreadFunction()
        {
            try
            {
                _server27000.Start(_localEP);
                _port27000.Start();

                InitializeData();

                CreateShipyard();

                CreateInitialPlanetsAndBases();
                CreateInitialPopulation();

                UpdateHomeLocations();

                CalculateInitialProduction();
                CalculateBudget();

                JoinIrcServer();
                UpdateStatus();

                // confirms if we have a group of settings, and a map, that will work for every playable race

                for (int i = (int)Races.kFirstEmpire; i < (int)Races.kLastCartel; i++)
                {
                    CreateShip((Races)i, _minClassType, _maxClassType, _minBPV, _maxBPV, _validRoles, _invalidRoles, CurrentYear, out _);

                    if (_homeLocations[i].X == -1 || _homeLocations[i].Y == -1)
                        throw new NotSupportedException();
                }

                // starts the campaign

                RunCampaign();
            }
            catch (Exception)
            {
                throw; // !?
            }
            finally
            {
                Close();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                Close();
            }
        }
    }
}
