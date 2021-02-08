using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Text;
using System.Threading;

namespace SfcOpServer
{
    public static class IrcService
    {
        private class Message
        {
            public int Id;

            public byte[] Buffer;
            public int Size;

            public Message(int id, byte[] buffer, int size)
            {
                Id = id;

                Buffer = ArrayPool<byte>.Shared.Rent((size - 1 >> 10) + 1 << 10);
                Size = size;

                Array.Copy(buffer, 0, Buffer, 0, size);
            }

            public void Clear()
            {
                Contract.Assert(Buffer != null);

                ArrayPool<byte>.Shared.Return(Buffer);

                Buffer = null;
                Size = 0;
            }
        }

        private const int CHANNELLEN = 64;
        private const char CHANTYPES = '#';
        private const int NICKLEN = 31;

        private const string CRLF = "\r\n";

        private const string defaultChannelModes = "nt";
        private const string defaultPingMessage = ":SfcRulz";

        private static string[] _motd;
        private static string[] _image;

        private static ConcurrentDictionary<int, IrcClient> _clients;
        private static ConcurrentDictionary<string, int> _nicks;
        private static ConcurrentDictionary<string, int> _users;

        private static Dictionary<string, Dictionary<int, int>> _channels; // channelName, { client.Id, flags }
        private static Queue<string> _channelsToBeRemoved;

        private static Dictionary<int, object> _whitelist; // list of users that can receive messages from a channel that start with '!' (sfc2op server commands)

        private static string _hostname;
        private static ConcurrentQueue<Message> _queue;
        private static Server6667 _server;

        private static Thread _thread;

        public static void InitializeAndStart(string[] motd, IPAddress localIP)
        {

#if DEBUG
            if (!defaultPingMessage.StartsWith(":", StringComparison.Ordinal))
                throw new NotSupportedException();
#endif

            _motd = motd[1].Split(new string[] { "\r", "\n" }, StringSplitOptions.None);

            _image = new string[]
            {
            "",
            " __________________          _-_",
            " \\________________|)____.---'---`---.____",
            "              ||    \\----._________.----/",
            "              ||     / ,'   `---'",
            "           ___||_,--'  -._",
            "          /___          ||(-",
            "              `---._____-'"
            };

            _clients = new ConcurrentDictionary<int, IrcClient>();
            _nicks = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _users = new ConcurrentDictionary<string, int>();
            _channels = new Dictionary<string, Dictionary<int, int>>();

            _channelsToBeRemoved = new Queue<string>();
            _whitelist = new Dictionary<int, object>();

            _hostname = localIP.ToString();
            _queue = new ConcurrentQueue<Message>();
            _server = new Server6667();

            _thread = new Thread(new ThreadStart(ThreadFunction))
            {

#if DEBUG
                Name = "IrcService"
#endif

            };

            try
            {
                _server.Start(localIP);
                _thread.Start();
            }
            catch (Exception)
            { }
        }

        public static void Close()
        {
            Contract.Assert(_server != null);

            _server.Dispose();

            Contract.Assert(_thread != null);

            try
            {
                if (_thread.IsAlive)
                    _thread.Interrupt();
            }
            catch (Exception)
            { }
        }

        private static void ThreadFunction()
        {
            try
            {
                const double pingInterval = 90_000.0; // ms

                double t1 = 0.0;

                Message msg = null;
                StringBuilder t = new StringBuilder(1024);
                List<string> a = new List<string>();
                SortedList<string, object> n = new SortedList<string, object>();

                Stopwatch clock = new Stopwatch();

                clock.Start();

                while (true)
                {
                    double t0 = clock.Elapsed.TotalMilliseconds;

                    // processes all the client messages

                    int msgs = 0;

                    for (; _queue.TryDequeue(out msg); msg.Clear())
                    {
                        if (!_clients.TryGetValue(msg.Id, out IrcClient client))
                            continue;

                        // tries to parse the current message

                        int c = GetArguments(msg, a);

                        msgs++;

                        // checks if we found any arguments

                        if (c == 0)
                            continue;

                        // clears some stuff before processing the message

                        if (_channelsToBeRemoved.Count > 0)
                        {
                            while (_channelsToBeRemoved.TryDequeue(out string channelName))
                                _channels.Remove(channelName);
                        }

                        // records the last time this client was active

                        client.LastActivity = t0;

                        // processes the current message

                        if (client.Nick == null)
                        {
                            if (a[0].Equals("NICK", StringComparison.Ordinal))
                            {
                                // NICK D4v1ks3074930439

                                if (c == 2 && a[1].Length <= NICKLEN)
                                {
                                    if (_nicks.TryAdd(a[1], client.Id))
                                    {
                                        client.Nick = a[1];

                                        continue;
                                    }
                                }

                                TryClose(client);
                            }
                        }
                        else if (client.User == null)
                        {
                            if (a[0].Equals("USER", StringComparison.Ordinal))
                            {
                                /*
                                    USER d4v1ks@hotmail.com 127.0.0.1 192.168.1.71 :D4v1ks
                                        -> D4v1ks3074930439!d4v1ks@192.168.1.71
                                */

                                if (c == 5 && a[4].StartsWith(":", StringComparison.Ordinal))
                                {
                                    string name = a[4][1..].ToLowerInvariant();
                                    string user = client.Nick + "!" + name + "@" + client.LocalIP;

                                    if (_users.TryAdd(user, client.Id))
                                    {
                                        client.Name = name;
                                        client.User = user;

                                        // :d4v1ks.ddns.net 001 D4v1ks3074930439 :Welcome to the REDE-SANTOS IRC Network D4v1ks3074930439!d4v1ks@192.168.1.71

                                        t.Clear();
                                        t.Append(':');
                                        t.Append(_hostname);
                                        t.Append(" 001 ");
                                        t.Append(client.Nick);
                                        t.Append(" :Welcome to the universe of Star Fleet Command!");
                                        t.Append(CRLF);

                                        Write(client, t);

                                        // :d4v1ks.ddns.net 002 Test :Your host is d4v1ks.ddns.net, running version InspIRCd-3

                                        t.Clear();
                                        t.Append(':');
                                        t.Append(_hostname);
                                        t.Append(" 002 ");
                                        t.Append(client.Nick);
                                        t.Append(" :Your communications host is ");
                                        t.Append(_hostname);
                                        t.Append(CRLF);

                                        Write(client, t);

                                        // :d4v1ks.ddns.net 005 John AWAYLEN=200 CASEMAPPING=rfc1459 CHANLIMIT=#:20 CHANMODES=Ybw,k,l,Oimnpst CHANNELLEN=64 CHANTYPES=# ELIST=CMNTU EXTBAN=,O HOSTLEN=64 KEYLEN=32 KICKLEN=255 LINELEN=512 :are supported by this server
                                        // :d4v1ks.ddns.net 005 John MAXLIST=bw:100 MAXTARGETS=20 MODES=20 NETWORK=RedeCasa NICKLEN=31 OPERLOG OVERRIDE PREFIX=(yov)!@+ SAFELIST STATUSMSG=!@+ TOPICLEN=307 USERLEN=11 WHOX :are supported by this server

                                        t.Clear();
                                        t.Append(':');
                                        t.Append(_hostname);
                                        t.Append(" 005 ");
                                        t.Append(client.Nick);

                                        t.Append(" CHANMODES=");
                                        t.Append(defaultChannelModes);

                                        t.Append(" CHANNELLEN=");
                                        t.Append(CHANNELLEN);

                                        t.Append(" CHANTYPES=");
                                        t.Append(CHANTYPES);

                                        t.Append(" LINELEN=");
                                        t.Append(IrcClient.MaximumBufferSize);

                                        t.Append(" NICKLEN=");
                                        t.Append(NICKLEN);

                                        t.Append(" :are supported");
                                        t.Append(CRLF);

                                        Write(client, t);

                                        // :d4v1ks.ddns.net 375 Test :d4v1ks.ddns.net message of the day

                                        t.Clear();
                                        t.Append(':');
                                        t.Append(_hostname);
                                        t.Append(" 375 ");
                                        t.Append(client.Nick);
                                        t.Append(" :- System Message");
                                        t.Append(CRLF);

                                        Write(client, t);

                                        // :d4v1ks.ddns.net 372 Test :- Welcome!

                                        for (int i = 0; i < _motd.Length; i++)
                                        {
                                            t.Clear();
                                            t.Append(':');
                                            t.Append(_hostname);
                                            t.Append(" 372 ");
                                            t.Append(client.Nick);
                                            t.Append(" :-    ");
                                            t.Append(_motd[i]);
                                            t.Append(CRLF);

                                            Write(client, t);
                                        }

                                        for (int i = 0; i < _image.Length; i++)
                                        {
                                            t.Clear();
                                            t.Append(':');
                                            t.Append(_hostname);
                                            t.Append(" 372 ");
                                            t.Append(client.Nick);
                                            t.Append(" :- ");
                                            t.Append(_image[i]);
                                            t.Append(CRLF);

                                            Write(client, t);
                                        }

                                        // :d4v1ks.ddns.net 376 Test :End of message of the day.

                                        t.Clear();
                                        t.Append(':');
                                        t.Append(_hostname);
                                        t.Append(" 376 ");
                                        t.Append(client.Nick);
                                        t.Append(" :End of /MOTD command.");
                                        t.Append(CRLF);

                                        Write(client, t);

                                        Debug.WriteLine(client.Nick + " joined the IRC service");

                                        continue;
                                    }
                                }

                                TryClose(client);
                            }
                        }
                        else
                        {
                            if (a[0].Equals("PRIVMSG", StringComparison.Ordinal))
                            {
                                // PRIVMSG #help :Hello World

                                if (c == 3 && a[2].StartsWith(":", StringComparison.Ordinal))
                                {
                                    t.Clear();
                                    t.Append(':');
                                    t.Append(client.User);
                                    t.Append(" PRIVMSG ");
                                    t.Append(a[1]);
                                    t.Append(' ');
                                    t.Append(a[2]);
                                    t.Append(CRLF);

                                    if (_channels.ContainsKey(a[1]))
                                    {
                                        if ((_whitelist.Count == 0) || (!a[2].StartsWith(":!", StringComparison.Ordinal)))
                                            Broadcast(_channels[a[1]], client.Id, t.ToString());
                                        else
                                            Broadcast(t.ToString());

                                        continue;
                                    }
                                    else if (_nicks.TryGetValue(a[1], out int id) && id != client.Id && _clients.TryGetValue(id, out IrcClient destination))
                                    {
                                        Write(destination, t);

                                        continue;
                                    }
                                }
                            }
                            else if (a[0].Equals("NOTICE", StringComparison.Ordinal))
                            {
                                // NOTICE #help :Hello World

                                if (c == 3 && a[2].StartsWith(":", StringComparison.Ordinal))
                                {
                                    t.Clear();
                                    t.Append(':');
                                    t.Append(client.User);
                                    t.Append(" NOTICE ");
                                    t.Append(a[1]);
                                    t.Append(' ');
                                    t.Append(a[2]);
                                    t.Append(CRLF);

                                    if (_channels.ContainsKey(a[1]))
                                    {
                                        Broadcast(_channels[a[1]], client.Id, t.ToString());

                                        continue;
                                    }
                                    else if (_nicks.TryGetValue(a[1], out int id) && id != client.Id && _clients.TryGetValue(id, out IrcClient destination))
                                    {
                                        Write(destination, t);

                                        continue;
                                    }
                                }
                            }
                            else if (a[0].Equals("JOIN", StringComparison.Ordinal))
                            {
                                // JOIN #help

                                if (c >= 2)
                                {
                                    for (int i = 1; i < c; i++)
                                    {
                                        if (a[i].Length <= CHANNELLEN && a[i].StartsWith(CHANTYPES))
                                        {
                                            Dictionary<int, int> channel;

                                            if (_channels.ContainsKey(a[i]))
                                            {
                                                channel = _channels[a[i]];

                                                // checks if the current client already joined the channel

                                                if (channel.ContainsKey(client.Id))
                                                    continue;
                                            }
                                            else
                                            {
                                                // creates and adds the new channel

                                                channel = new Dictionary<int, int>();

                                                _channels.Add(a[i], channel);
                                            }

                                            // adds the current client to the channel, and sets the defaults flags

                                            channel.Add(client.Id, 0);

                                            // :D4v1ks3074930439!d4v1ks@192.168.1.71 JOIN :#General@Standard

                                            t.Clear();
                                            t.Append(':');
                                            t.Append(client.User);
                                            t.Append(" JOIN :");
                                            t.Append(a[i]);
                                            t.Append(CRLF);

                                            Broadcast(channel, 0, t.ToString());

                                            // lists the names currently in the channel

                                            ListNames(t, n, client, a[i], channel);
                                        }
                                    }

                                    continue;
                                }
                            }
                            else if (a[0].Equals("PART", StringComparison.Ordinal))
                            {
                                // <Client> PART #General@Standard

                                if (c >= 2)
                                {
                                    for (int i = 1; i < c; i++)
                                    {
                                        if (a[i].StartsWith(CHANTYPES))
                                            TryPart(a[i], client);
                                    }

                                    continue;
                                }
                            }
                            else if (a[0].Equals("MODE", StringComparison.Ordinal))
                            {
                                switch (c)
                                {
                                    case 2:
                                        {
                                            if (_channels.ContainsKey(a[1]))
                                            {
                                                /*
                                                    <Client> MODE #General@Standard
                                                    <Server> :d4v1ks.ddns.net 324 D4v1ks3074930439 #General@Standard +nt
                                                */

                                                t.Clear();
                                                t.Append(':');
                                                t.Append(_hostname);
                                                t.Append(" 324 ");
                                                t.Append(client.Nick);
                                                t.Append(' ');
                                                t.Append(a[1]);
                                                t.Append(" +");
                                                t.Append(defaultChannelModes);
                                                t.Append(CRLF);

                                                Write(client, t);

                                                continue;
                                            }
                                            else if (_nicks.TryGetValue(a[1], out int id))
                                            {
                                                if (client.Id == id && client.Modes.Length > 0)
                                                {
                                                    /*
                                                        <Client> MODE John
                                                        <Server> :d4v1ks.ddns.net 221 D4v1ks3074930439 +i
                                                    */

                                                    t.Clear();
                                                    t.Append(':');
                                                    t.Append(_hostname);
                                                    t.Append(" 221 ");
                                                    t.Append(client.Nick);
                                                    t.Append(" +");
                                                    t.Append(client.Modes);
                                                    t.Append(CRLF);

                                                    Write(client, t);

                                                    continue;
                                                }
                                            }

                                            break;
                                        }
                                    case 3:
                                        {
                                            if (_channels.ContainsKey(a[1]))
                                            {
                                                /*
                                                    <Client> MODE #General@Standard -t
                                                    <Server> :D4v1ks3074930439!d4v1ks@192.168.1.71 MODE #General@Standard -t

                                                    <Client> MODE #General@Standard +t
                                                    <Server> :D4v1ks3074930439!d4v1ks@192.168.1.71 MODE #General@Standard +t
                                                */
                                            }
                                            else if (_nicks.TryGetValue(a[1], out int id))
                                            {
                                                if (client.Id == id)
                                                {
                                                    /*
                                                        <Client> MODE D4v1ks3074930439 -i
                                                        <Server> :D4v1ks3074930439 MODE D4v1ks3074930439 :-i

                                                        <Client> MODE D4v1ks3074930439 +i
                                                        <Server> :D4v1ks3074930439 MODE D4v1ks3074930439 :+i
                                                    */

                                                    char[] flags = a[2].ToCharArray();

                                                    StringBuilder changes = new StringBuilder(1024);
                                                    string modes = client.Modes;

                                                    if (flags[0] == '+')
                                                    {
                                                        changes.Append('+');

                                                        for (int i = 1; i < flags.Length; i++)
                                                        {
                                                            if (!modes.Contains(flags[i], StringComparison.Ordinal))
                                                            {
                                                                modes += flags[i];

                                                                changes.Append(flags[i]);

                                                                switch (flags[i])
                                                                {
                                                                    case 'w':
                                                                        {
                                                                            _whitelist.Add(client.Id, null);

                                                                            break;
                                                                        }
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else if (flags[0] == '-')
                                                    {
                                                        changes.Append('-');

                                                        for (int i = 1; i < flags.Length; i++)
                                                        {
                                                            if (modes.Contains(flags[i], StringComparison.Ordinal))
                                                            {
                                                                modes = modes.Replace(flags[i].ToString(), "", StringComparison.Ordinal);

                                                                changes.Append(flags[i]);

                                                                switch (flags[i])
                                                                {
                                                                    case 'w':
                                                                        {
                                                                            if (_whitelist.ContainsKey(client.Id))
                                                                                _whitelist.Remove(client.Id);

                                                                            break;
                                                                        }
                                                                }
                                                            }
                                                        }
                                                    }

                                                    if (changes.Length > 1)
                                                    {
                                                        client.Modes = modes;

                                                        t.Clear();
                                                        t.Append(':');
                                                        t.Append(client.Nick);
                                                        t.Append(" MODE ");
                                                        t.Append(a[1]);
                                                        t.Append(" :");
                                                        t.Append(changes);
                                                        t.Append(CRLF);

                                                        Write(client, t);

                                                        continue;
                                                    }
                                                }
                                            }

                                            break;
                                        }
                                }
                            }
                            else if (a[0].Equals("NAMES", StringComparison.Ordinal))
                            {
                                // NAMES #channel1 [, #channel2]

                                if (c >= 2)
                                {
                                    for (int i = 1; i < c; i++)
                                    {
                                        if (a[i].StartsWith(CHANTYPES))
                                        {
                                            if (_channels.TryGetValue(a[i], out Dictionary<int, int> channel))
                                                ListNames(t, n, client, a[i], channel);
                                        }
                                    }

                                    continue;
                                }
                            }
                            else if (a[0].Equals("PING", StringComparison.Ordinal))
                            {
                                if (c == 2 && a[1].StartsWith(":", StringComparison.Ordinal))
                                {
                                    t.Clear();
                                    t.Append("PONG ");
                                    t.Append(a[1]);
                                    t.Append(CRLF);

                                    Write(client, t);

                                    continue;
                                }
                            }
                            else if (a[0].Equals("QUIT", StringComparison.Ordinal))
                            {
                                TryClose(client);

                                continue;
                            }
                        }

                        if (a[0].Equals("PONG", StringComparison.Ordinal))
                        {
                            if (c != 2 || !a[1].Equals(defaultPingMessage, StringComparison.Ordinal))
                                TryClose(client);

                            continue;
                        }

#if DEBUG
                        else if (a[0].Equals("CAP", StringComparison.Ordinal))
                        {
                            // CAP LS 302

                            continue;
                        }
                        else if (a[0].Equals("USERHOST", StringComparison.Ordinal))
                        {
                            // USERHOST d4v1ks

                            continue;
                        }

                        // displays the messages that are not ignored, supported, or implemented yet

                        //Debugger.Break();
#endif

                    }

                    // tries to process the server pings

                    if (t0 - t1 >= 1_000.0)
                    {
                        msgs++;
                        t1 = t0;

                        foreach (KeyValuePair<int, IrcClient> p in _clients)
                        {
                            IrcClient client = p.Value;

                            if (t0 - client.LastActivity >= pingInterval)
                            {
                                client.LastActivity = t0;

                                t.Clear();
                                t.Append("PING ");
                                t.Append(defaultPingMessage);
                                t.Append(CRLF);

                                Write(client, t);
                            }
                        }
                    }

                    // does a small pause if nothing was processed during the loop

                    if (msgs == 0)
                        TimerHelper.SleepForNoMoreThanCurrentResolution();
                }
            }
            catch (Exception)
            { }
        }

        public static void Enqueue(int id, byte[] buffer, int size)
        {
            _queue.Enqueue(new Message(id, buffer, size));
        }

        public static bool TryAddClient(int id, IrcClient client)
        {
            return _clients.TryAdd(id, client);
        }

        public static bool TryGetClient(int id, out IrcClient client)
        {
            return _clients.TryGetValue(id, out client);
        }

        public static bool TryGetClient(string nick, out IrcClient client)
        {
            if (_nicks.TryGetValue(nick, out int id) && _clients.TryGetValue(id, out client))
                return true;

            client = null;

            return false;
        }

        public static void TryQuitClient(IrcClient client)
        {
            Contract.Requires(client != null);

            const string msg = "QUIT :" + CRLF;

            Enqueue(client.Id, Encoding.UTF8.GetBytes(msg), msg.Length);
        }

        public static void Write(IrcClient client, StringBuilder msg)
        {
            client.Write(_server, msg.ToString());
        }

        private static int GetArguments(Message msg, List<string> a)
        {
            // skips the control chars at the end of the line

            int c = msg.Size;

            while (c > 0 && msg.Buffer[c - 1] <= 32)
                c--;

            // checks if we are processing an empty line

            if (c == 0)
                return 0;

            // tries to read all the arguments

            a.Clear();

            int i = 0; // first position
            int j = 0; // last position

            while (j < c)
            {
                if (msg.Buffer[j] == 58)
                {
                    // skips lines starting with ":"

                    if (j == 0)
                        return 0;

                    // assumes we are processing the last argument, so we add it and exit the loop

                    a.Add(Encoding.UTF8.GetString(msg.Buffer, j, c - j));

                    break;
                }

                // skips everything till we find any " " or ","

                while (j < c && msg.Buffer[j] != 32 && msg.Buffer[j] != 44)
                    j++;

                // assumes we found an argument so we add it

                a.Add(Encoding.UTF8.GetString(msg.Buffer, i, j - i));

                // skips the following " " and ","

                do
                    j++;
                while (j < c && (msg.Buffer[j] == 32 || msg.Buffer[j] == 44));

                // sets the new starting position

                i = j;
            }

            return a.Count;
        }

        private static void Broadcast(Dictionary<int, int> channel, int id, string message)
        {
            foreach (KeyValuePair<int, int> p in channel)
            {
                IrcClient client = _clients[p.Key];

                if (client.Id != id)
                    client.Write(_server, message);
            }
        }

        private static void Broadcast(string whiteListedMsg)
        {
            foreach (KeyValuePair<int, object> p in _whitelist)
            {
                if (_clients.TryGetValue(p.Key, out IrcClient client))
                    client.Write(_server, whiteListedMsg);
            }
        }

        private static void ListNames(StringBuilder t, SortedList<string, object> n, IrcClient client, string channelName, Dictionary<int, int> channel)
        {
            const int LINE_LENGTH = IrcClient.MaximumBufferSize - (NICKLEN + 2);

            // lists the nicks currently in the channel

            Contract.Assert(n.Count == 0);

            foreach (KeyValuePair<int, int> p in channel)
                n.Add(_clients[p.Key].Nick, null);

            // :d4v1ks.ddns.net 353 Test = #help :Test

            IEnumerator<KeyValuePair<string, object>> e = n.GetEnumerator();

            if (!e.MoveNext())
                throw new NotSupportedException();

            startLine:

            t.Clear();
            t.Append(':');
            t.Append(_hostname);
            t.Append(" 353 ");
            t.Append(client.Nick);
            t.Append(" = ");
            t.Append(channelName);
            t.Append(" :");
            t.Append(e.Current.Key);

            do
            {
                if (!e.MoveNext())
                    goto finishLine;

                t.Append(' ');
                t.Append(e.Current.Key);
            }
            while (t.Length <= LINE_LENGTH);

            if (!e.MoveNext())
                goto finishLine;

            goto startLine;

        finishLine:

            t.Append(CRLF);

            Write(client, t);

            // :d4v1ks.ddns.net 366 Test #help :End of /NAMES list.

            t.Clear();
            t.Append(':');
            t.Append(_hostname);
            t.Append(" 366 ");
            t.Append(client.Nick);
            t.Append(' ');
            t.Append(channelName);
            t.Append(" :End of /NAMES list.");
            t.Append(CRLF);

            Write(client, t);

            // clears the list

            n.Clear();
        }

        private static void TryPart(string channelName, IrcClient client)
        {
            if (_channels.TryGetValue(channelName, out Dictionary<int, int> channel) && channel.ContainsKey(client.Id))
            {
                // <Server> :D4v1ks3074930439!d4v1ks@192.168.1.71 PART #General@Standard

                string msg = ":" + client.User + " PART " + channelName + " :" + CRLF;

                Broadcast(channel, 0, msg);

                channel.Remove(client.Id);

                if (channel.Count == 0)
                    _channelsToBeRemoved.Enqueue(channelName);
            }
        }

        private static void TryClose(IrcClient client)
        {
            Contract.Requires(client != null);

            foreach (KeyValuePair<string, Dictionary<int, int>> p in _channels)
            {
                Dictionary<int, int> channel = p.Value;

                if (channel.ContainsKey(client.Id))
                {
                    channel.Remove(client.Id);

                    if (channel.Count == 0)
                        _channelsToBeRemoved.Enqueue(p.Key);
                }
            }

            if (_whitelist.ContainsKey(client.Id))
                _whitelist.Remove(client.Id);

            bool clientRemoved = _clients.TryRemove(client.Id, out _);

            Debug.WriteLineIf(clientRemoved, client.Nick + " left the IRC service");

            _nicks.TryRemove(client.Nick, out _);

            if (client.User != null && _users.TryRemove(client.User, out _))
            {
                // broadcasts a message to all the remaining users in the server

                if (!_users.IsEmpty)
                {
                    string msg = ":" + client.User + " QUIT :" + CRLF;

                    foreach (KeyValuePair<string, int> p in _users)
                    {
                        if (_clients.TryGetValue(p.Value, out IrcClient other))
                            other.Write(_server, msg);
                    }
                }
            }

            client.EndCloseUser();
        }
    }
}
