using System;
using System.Diagnostics.Contracts;
using System.Net;

namespace SfcOpServer
{
    public abstract class IrcClient : AsyncUser
    {
        public const int MaximumBufferSize = 1024;

        public string LocalIP { get; set; }
        public string Name { get; set; }
        public string Nick { get; set; }
        public string User { get; set; }
        public string Modes { get; set; }

        public double LastActivity { get; set; }

        public abstract void Write(AsyncServer server, string msg);

        protected void InitializeThis(IPAddress localIP)
        {
            Contract.Assert(Id != 0);

            if (!IrcService.TryAddClient(Id, this))
                throw new NotSupportedException();

            Contract.Requires(localIP != null);

            LocalIP = localIP.ToString();
            Modes = string.Empty;
        }
    }
}
