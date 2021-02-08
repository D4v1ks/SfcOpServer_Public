using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;

namespace SfcOpServer
{
    public class Stream6667 : IrcClient
    {
        private readonly Queue<string> _queue;

        public Stream6667(IPAddress localIP)
        {
            InitializeUser(null);
            InitializeThis(localIP);

            _queue = new Queue<string>();
        }

        public override void Write(AsyncServer server, string msg)
        {
            Contract.Assert(server != null);

            _queue.Enqueue(msg);
        }

        public bool IsEmpty()
        {
            return _queue.Count == 0;
        }

        public bool TryRead(out string msg)
        {
            return _queue.TryDequeue(out msg);
        }
    }
}
