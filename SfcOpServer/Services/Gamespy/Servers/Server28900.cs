using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SfcOpServer
{
    public class Server28900 : AsyncServer
    {
        private static byte[][] _data;

        public static void Initialize()
        {
            _data = new byte[][]
            {
            // \final\
            new byte[] { 92, 102, 105, 110, 97, 108, 92 },

            // \queryid\1.1\
            new byte[] { 92, 113, 117, 101, 114, 121, 105, 100, 92, 49, 46, 49, 92 },

            // \basic\\secure\000000
            new byte[]{ 92, 98, 97, 115, 105, 99, 92, 92, 115, 101, 99, 117, 114, 101, 92, 48, 48, 48, 48, 48, 48 },
            };

            // challenge

            Utils.ReplacePattern(_data, 48, 6, Encoding.ASCII.GetBytes(Utils.GetRandomASCII(6).ToUpperInvariant()));
        }

        public Server28900()
        {
            Initialize(GameServer.MaxNumPlayers, 0, 512);
        }

        public void Start(IPAddress privateIP)
        {
            Start(new IPEndPoint(privateIP, 28900));
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

            if (Utils.EndsWith(buffer, size, _data[0]) || Utils.EndsWith(buffer, size, _data[1]))
                return size;

            return buffer.Length;
        }

        public override void Handshake(AsyncUser user)
        {
            Write(user, _data[2], 0, _data[2].Length);
        }

        public override int Process(AsyncUser user, byte[] buffer, int size)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();

            Utils.GetArguments(buffer, size, ref d);

            /*
                \gamename\sfc2op\gamever\1.6\location\0\validate\Dvz0jxQz\final\
                \gamename\sfc2op\gamever\1.6\location\0\validate\Dvz0jxQz\final\\queryid\1.1\
                \gamename\sfc2op\gamever\1.6\location\0\validate\Dvz0jxQz\final\\queryid\1.1\\list\cmp\gamename\sfc2op\final\
            */

            if (d.ContainsKey("gamename"))
            {
                if (d.ContainsKey("list"))
                {
                    using MemoryStream m = new MemoryStream();

                    GsService.ListServers(m);

                    m.Write(_data[0], 0, _data[0].Length);

                    // sends the list

                    m.SetLength(m.Position);

                    byte[] b = m.ToArray();

                    Write(user, b, 0, b.Length);
                }

                return 1;
            }

            return 0;
        }
    }
}
