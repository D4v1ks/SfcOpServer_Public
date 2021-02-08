using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace SfcOpServer
{
    public class Server15101 : AsyncServer
    {
        private static byte[][] _data;

        public static void Initialize(IPAddress publicIP)
        {
            Contract.Requires(publicIP != null);

            _data = new byte[][]
            {
                new byte[] { 63, 0, 0, 0, 5, 2, 0, 103, 0, 55, 8, 2, 6, 0, 0, 22, 0, 47, 0, 84, 0, 105, 0, 116, 0, 97, 0, 110, 0, 83, 0, 101, 0, 114, 0, 118, 0, 101, 0, 114, 0, 115, 0, 47, 0, 70, 0, 105, 0, 114, 0, 101, 0, 119, 0, 97, 0, 108, 0, 108, 0, 0, 0 },
                new byte[] { 97, 0, 0, 0, 5, 2, 0, 3, 0, 0, 0, 128, 55, 8, 2, 6, 2, 0, 68, 8, 0, 70, 0, 105, 0, 114, 0, 101, 0, 119, 0, 97, 0, 108, 0, 108, 0, 0, 0, 0, 0, 83, 21, 0, 84, 0, 105, 0, 116, 0, 97, 0, 110, 0, 70, 0, 105, 0, 114, 0, 101, 0, 119, 0, 97, 0, 108, 0, 108, 0, 68, 0, 101, 0, 116, 0, 101, 0, 99, 0, 116, 0, 111, 0, 114, 0, 6, 59, 196, 48, 48, 48, 48, 0, 0, 0, 0 },
                new byte[] { 81, 0, 0, 0, 5, 2, 0, 103, 0, 119, 14, 2, 6, 0, 0, 31, 0, 47, 0, 83, 0, 116, 0, 97, 0, 114, 0, 70, 0, 108, 0, 101, 0, 101, 0, 116, 0, 67, 0, 111, 0, 109, 0, 109, 0, 97, 0, 110, 0, 100, 0, 50, 0, 47, 0, 71, 0, 97, 0, 109, 0, 101, 0, 47, 0, 82, 0, 101, 0, 108, 0, 101, 0, 97, 0, 115, 0, 101, 0, 0, 0 },
                new byte[] { 43, 0, 0, 0, 5, 2, 0, 3, 0, 0, 0, 128, 119, 14, 2, 6, 1, 0, 68, 7, 0, 82, 0, 101, 0, 108, 0, 101, 0, 97, 0, 115, 0, 101, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            };

            Utils.ReplacePattern(_data, 48, 4, publicIP.GetAddressBytes());
        }

        public Server15101()
        {
            Initialize(GameServer.MaxNumPlayers, 4, 512);
        }

        public void Start(IPAddress privateIP)
        {
            Start(new IPEndPoint(privateIP, 15101));
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
            return BitConverter.ToInt32(buffer, 0);
        }

        public override void Handshake(AsyncUser user)
        { }

        public override int Process(AsyncUser user, byte[] buffer, int size)
        {
            if (Utils.EqualsTo(buffer, size, _data[0]))
            {
                Write(user, _data[1], 0, _data[1].Length);

                return 1;
            }

            if (Utils.EqualsTo(buffer, size, _data[2]))
            {
                Write(user, _data[3], 0, _data[3].Length);

                return 1;
            }

            return 0;
        }
    }
}
