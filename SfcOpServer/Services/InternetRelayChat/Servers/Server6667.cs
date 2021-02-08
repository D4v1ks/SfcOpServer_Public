using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;

namespace SfcOpServer
{
    public class Server6667 : AsyncServer
    {
        public Server6667()
        {
            Initialize(GameServer.MaxNumPlayers, 0, IrcClient.MaximumBufferSize);
        }

        public void Start(IPAddress privateIP)
        {
            Start(new IPEndPoint(privateIP, 6667));
        }

        public override void BeginCloseUser(AsyncUser user)
        {
            IrcService.TryQuitClient((IrcClient)user);
        }

        public override AsyncUser CreateUser(Socket sock)
        {
            return new Client6667(sock);
        }

        public override int GetSize(byte[] buffer, int size)
        {
            Contract.Requires(buffer != null);

            if (size >= 1)
            {
                for (int i = 0; i < size; i++)
                {
                    if (buffer[i] == 10)
                        return i + 1;
                }
            }

            return IrcClient.MaximumBufferSize;
        }

        public override void Handshake(AsyncUser user)
        { }

        public override int Process(AsyncUser user, byte[] buffer, int size)
        {
            Contract.Requires(user != null);

            IrcService.Enqueue(user.Id, buffer, size);

            return 1;
        }
    }
}
