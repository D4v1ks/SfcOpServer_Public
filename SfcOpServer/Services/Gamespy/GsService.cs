using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Threading;

namespace SfcOpServer
{
    public static class GsService
    {
        private static int _nextProfileId;
        private static ConcurrentDictionary<int, GsProfile> _profiles;
        private static ConcurrentDictionary<string, int> _emails;
        private static ConcurrentDictionary<string, int> _nicks;

        private static ConcurrentDictionary<string, byte[]> _servers;

        public static void Initialize()
        {
            _nextProfileId = 0;
            _profiles = new ConcurrentDictionary<int, GsProfile>();
            _emails = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _nicks = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            _servers = new ConcurrentDictionary<string, byte[]>();
        }

        public static void AddProfile(string email, string nick, string password, out GsProfile profile)
        {
            int id = Interlocked.Increment(ref _nextProfileId);

            profile = new GsProfile()
            {
                Id = id,

                Email = email,
                Nick = nick,
                Password = password
            };

            if (!_profiles.TryAdd(id, profile) || !_emails.TryAdd(email, id) || !_nicks.TryAdd(nick, id))
                throw new NotSupportedException();
        }

        public static void GetProfile(string username, out GsProfile profile)
        {
            Contract.Requires(username != null);

            int i = username.IndexOf('@', StringComparison.Ordinal);

            if
            (
                i >= 0 &&
                _emails.TryGetValue(username[(i + 1)..], out int id1) &&
                _nicks.TryGetValue(username[0..i], out int id2) &&
                id1 == id2 &&
                _profiles.TryGetValue(id1, out profile)
            )
                return;

            profile = null;
        }

        public static bool ContainsEmail(string email)
        {
            return _emails.ContainsKey(email);
        }

        public static bool ContainsNick(string nick)
        {
            return _nicks.ContainsKey(nick);
        }

        public static void AddServer(IPEndPoint ep)
        {
            Contract.Requires(ep != null);

            byte[] ip = ep.Address.GetAddressBytes();
            byte[] port = BitConverter.GetBytes(ep.Port);

            if (!_servers.TryAdd(ep.ToString(), new byte[] { ip[0], ip[1], ip[2], ip[3], port[1], port[0] }))
                throw new NotSupportedException();
        }

        public static void TryRemoveServer(IPEndPoint ep)
        {
            Contract.Requires(ep != null);

            _servers.TryRemove(ep.ToString(), out _);
        }

        public static void ListServers(MemoryStream m)
        {
            Contract.Requires(m != null);

            foreach (KeyValuePair<string, byte[]> p in _servers)
                m.Write(p.Value, 0, 6);
        }

        public static void WriteTo(BinaryWriter w)
        {
            w.Write(_profiles.Count);

            foreach (KeyValuePair<int, GsProfile> p in _profiles)
            {
                GsProfile profile = p.Value;

                w.Write(profile.Email);
                w.Write(profile.Nick);
                w.Write(profile.Password);
            }
        }

        public static void ReadFrom(BinaryReader r)
        {
            int c = r.ReadInt32();

            for (int i = 0; i < c; i++)
            {
                string email = r.ReadString();
                string nick = r.ReadString();
                string password = r.ReadString();

                if (!ContainsEmail(email) && !ContainsNick(nick))
                    AddProfile(email, nick, password, out _);
            }
        }
    }
}
