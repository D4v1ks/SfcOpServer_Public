using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SfcOpServer
{
    public class Client6667 : IrcClient
    {
        public Client6667(Socket sock)
        {
            Contract.Requires(sock != null);

            InitializeUser(sock);
            InitializeThis(((IPEndPoint)sock.RemoteEndPoint).Address);
        }

        public override void Write(AsyncServer server, string msg)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(msg);

            server.Write(this, buffer, 0, buffer.Length);
        }
    }
}
