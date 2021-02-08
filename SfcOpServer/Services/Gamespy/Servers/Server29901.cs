using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace SfcOpServer
{
    public class Server29901 : AsyncServer
    {
        private static byte[][] _data;

        public static void Initialize()
        {
            _data = new byte[][]
            {
                // \final\
                new byte[] { 92, 102, 105, 110, 97, 108, 92 },

                // \vr\1\final\
                new byte[] { 92, 118, 114, 92, 49, 92, 102, 105, 110, 97, 108, 92 },

                // \vr\0\final\
                new byte[] { 92, 118, 114, 92, 48, 92, 102, 105, 110, 97, 108, 92 },
            };
        }

        public Server29901()
        {
            Initialize(GameServer.MaxNumPlayers, 0, 512);
        }

        public void Start(IPAddress privateIP)
        {
            Start(new IPEndPoint(privateIP, 29901));
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
            Contract.Requires(buffer != null);

            if (Utils.EndsWith(buffer, size, _data[0]))
                return size;

            return buffer.Length;
        }

        public override void Handshake(AsyncUser user)
        { }

        public override int Process(AsyncUser user, byte[] buffer, int size)
        {
            // \valid\\email\d4v1ks@hotmail.com\final\

            Dictionary<string, string> d = new Dictionary<string, string>();

            Utils.GetArguments(buffer, size, ref d);

            if (d.ContainsKey("valid") && d.ContainsKey("email"))
            {
                if (GsService.ContainsEmail(d["email"]))
                    Write(user, _data[1], 0, _data[1].Length);
                else
                    Write(user, _data[2], 0, _data[2].Length);

                return 1;
            }

            return 0;
        }
    }
}
