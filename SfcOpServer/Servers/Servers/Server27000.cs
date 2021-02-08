using System;
using System.Net.Sockets;

namespace SfcOpServer
{
    public class Server27000 : AsyncServer
    {
        private readonly GameServer _gameServer;

        public Server27000(int serverId)
        {
            Initialize(GameServer.MaxNumPlayers, Client27000.MinimumBufferSize, Client27000.MaximumBufferSize);

            if (!GameServer.TryGetServer(serverId, out _gameServer))
                throw new NotSupportedException();
        }

        public override void BeginCloseUser(AsyncUser user)
        {
            _gameServer.RemoveClient(user.Id);
        }

        public override AsyncUser CreateUser(Socket sock)
        {
            Client27000 client = new Client27000(sock);

            _gameServer.AddClient(client);

            return client;
        }

        public override int GetSize(byte[] buffer, int size)
        {
            return BitConverter.ToInt32(buffer, 0);
        }

        public override void Handshake(AsyncUser user)
        { }

        public override int Process(AsyncUser user, byte[] buffer, int size)
        {
            _gameServer.EnqueueMessage((Client27000)user, buffer, size);

            return 1;
        }
    }
}
