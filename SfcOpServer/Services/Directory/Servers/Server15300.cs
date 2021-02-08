using System;
using System.Net;
using System.Net.Sockets;

namespace SfcOpServer
{
    public class Server15300 : AsyncServer
    {
        private static byte[][] _data;

        public static void Initialize()
        {
            _data = new byte[][]
            {
                new byte[] { 15, 0, 5, 3, 0, 1, 0 },
                new byte[] { 1, 32, 78, 0, 0, 0 },
                new byte[] { 9, 0, 5, 3, 0, 2, 0, 0, 0 },
                new byte[] { 9, 0, 5, 3, 0, 2, 0, 255, 255 }
            };
        }

        public Server15300()
        {
            Initialize(GameServer.MaxNumPlayers, 2, 512);
        }

        public void Start(IPAddress privateIP)
        {
            Start(new IPEndPoint(privateIP, 15300));
        }

        public override void BeginCloseUser(AsyncUser user)
        { }

        public override AsyncUser CreateUser(Socket sock)
        {
            AsyncUser user = new AsyncUser();

            user.InitializeUser(sock);

            return user;
        }

        public override int GetSize(byte[] buffer, int size)
        {
            return BitConverter.ToInt16(buffer, 0);
        }

        public override void Handshake(AsyncUser user)
        { }

        public override int Process(AsyncUser user, byte[] buffer, int size)
        {
            if (Utils.StartsWith(buffer, size, _data[0]) && Utils.EndsWith(buffer, size, _data[1]))
                Write(user, _data[2], 0, _data[2].Length);
            else
                Write(user, _data[3], 0, _data[3].Length);

            return 1;
        }
    }
}
