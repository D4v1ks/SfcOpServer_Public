#pragma warning disable CA5351

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SfcOpServer
{
    public class Server29900 : AsyncServer
    {
        private static byte[][] _data;
        private static string _serverChallenge;

        public static void Initialize()
        {
            _data = new byte[][]
            {
                // \final\
                new byte[] { 92, 102, 105, 110, 97, 108, 92 },

                // \lc\1\challenge\0000000000\id\1\final\
                new byte[] { 92, 108, 99, 92, 49, 92, 99, 104, 97, 108, 108, 101, 110, 103, 101, 92, 48, 48, 48, 48, 48, 48, 48, 48, 48, 48, 92, 105, 100, 92, 49, 92, 102, 105, 110, 97, 108, 92 },

                // \error\\err\100\fatal\\errmsg\A profile with this email already exists.\id\1\final\
                new byte[] { 92, 101, 114, 114, 111, 114, 92, 92, 101, 114, 114, 92, 49, 48, 48, 92, 102, 97, 116, 97, 108, 92, 92, 101, 114, 114, 109, 115, 103, 92, 65, 32, 112, 114, 111, 102, 105, 108, 101, 32, 119, 105, 116, 104, 32, 116, 104, 105, 115, 32, 101, 109, 97, 105, 108, 32, 97, 108, 114, 101, 97, 100, 121, 32, 101, 120, 105, 115, 116, 115, 46, 92, 105, 100, 92, 49, 92, 102, 105, 110, 97, 108, 92 },

                // \error\\err\516\fatal\\errmsg\A profile with this nick already exists.\id\1\final\
                new byte[] { 92, 101, 114, 114, 111, 114, 92, 92, 101, 114, 114, 92, 53, 49, 54, 92, 102, 97, 116, 97, 108, 92, 92, 101, 114, 114, 109, 115, 103, 92, 65, 32, 112, 114, 111, 102, 105, 108, 101, 32, 119, 105, 116, 104, 32, 116, 104, 105, 115, 32, 110, 105, 99, 107, 32, 97, 108, 114, 101, 97, 100, 121, 32, 101, 120, 105, 115, 116, 115, 46, 92, 105, 100, 92, 49, 92, 102, 105, 110, 97, 108, 92 },

                // \error\\err\515\fatal\\errmsg\The profile provided is incorrect.\id\1\final\
                new byte[] { 92, 101, 114, 114, 111, 114, 92, 92, 101, 114, 114, 92, 53, 49, 53, 92, 102, 97, 116, 97, 108, 92, 92, 101, 114, 114, 109, 115, 103, 92, 84, 104, 101, 32, 112, 114, 111, 102, 105, 108, 101, 32, 112, 114, 111, 118, 105, 100, 101, 100, 32, 105, 115, 32, 105, 110, 99, 111, 114, 114, 101, 99, 116, 46, 92, 105, 100, 92, 49, 92, 102, 105, 110, 97, 108, 92 }
            };

            _serverChallenge = Utils.GetRandomASCII(10);

            Utils.ReplacePattern(_data, 48, 10, Encoding.ASCII.GetBytes(_serverChallenge));
        }

        public Server29900()
        {
            Initialize(GameServer.MaxNumPlayers, 0, 512);
        }

        public void Start(IPAddress privateIP)
        {
            Start(new IPEndPoint(privateIP, 29900));
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
        {
            Write(user, _data[1], 0, _data[1].Length);
        }

        public override int Process(AsyncUser user, byte[] buffer, int size)
        {
            Contract.Requires(user != null);

            Dictionary<string, string> d = new Dictionary<string, string>();

            Utils.GetArguments(buffer, size, ref d);

            // \newuser\\email\johndoe2@hotmail.com\nick\JohnDoe2\password\sfcrulz\id\1\final\

            if (d.ContainsKey("newuser") && d.ContainsKey("email") && d.ContainsKey("nick") && d.ContainsKey("password"))
            {
                if (GsService.ContainsEmail(d["email"]))
                    Write(user, _data[2], 0, _data[2].Length);
                else if (GsService.ContainsNick(d["nick"]))
                    Write(user, _data[3], 0, _data[3].Length);
                else
                {
                    GsService.AddProfile(d["email"], d["nick"], d["password"], out GsProfile profile);

                    // \nur\\userid\12617\profileid\19465\id\1\final\

                    StringBuilder s = new StringBuilder(1024);

                    s.Append("\\nur\\\\userid\\");
                    s.Append(profile.Id);
                    s.Append("\\profileid\\");
                    s.Append(profile.Id);
                    s.Append("\\id\\1\\final\\");

                    byte[] b = Encoding.ASCII.GetBytes(s.ToString());

                    Write(user, b, 0, b.Length);
                }

                return 1;
            }

            /*
                \login\\challenge\IoWIbSnjMf2pv9iUioRgF9ySYLV2r72p\user\JohnDoe2@johndoe2@hotmail.com\userid\12617\profileid\19465\response\08eeb76f1241ac0777a18aac726c03d1\firewall\1\port\0\id\1\final\
                \login\\challenge\PKPNiLeCH1D71pCwSndbT36zrkQLaG2w\user\JohnDoe2@johndoe2@hotmail.com\response\a7d6e62a66f96565966f96ab475a44cf\firewall\1\port\0\id\1\final\
            */

            if (d.ContainsKey("login") && d.ContainsKey("challenge") && d.ContainsKey("user") && d.ContainsKey("response"))
            {
                GsService.GetProfile(d["user"], out GsProfile profile);

                if (profile != null)
                {
                    using MD5 md5 = MD5.Create();

                    string password = GetHash(md5, profile.Password);
                    string userData = password + "                                                " + profile.Username;

                    string clientChallenge = d["challenge"];

                    string clientResponse = GetHash(md5, userData + clientChallenge + _serverChallenge + password);
                    string serviceResponse = GetHash(md5, userData + _serverChallenge + clientChallenge + password);

                    if (clientResponse.Equals(d["response"], StringComparison.Ordinal))
                    {
                        // \lc\2\sesskey\239289\proof\8f1295968d534a3c6816d24dc172d92e\userid\12617\profileid\19465\uniquenick\JohnDoe2@johndoe2@hotmail.com\lt\x4TGX3[SbkAk]NIJt3Hc4N__\id\1\final\

                        StringBuilder s = new StringBuilder(1024);

                        s.Append("\\lc\\2\\sesskey\\");
                        s.Append(user.Id);
                        s.Append("\\proof\\");
                        s.Append(serviceResponse);
                        s.Append("\\userid\\");
                        s.Append(profile.Id);
                        s.Append("\\profileid\\");
                        s.Append(profile.Id);
                        s.Append("\\uniquenick\\");
                        s.Append(profile.Username);
                        s.Append("\\lt\\x4TGX3[SbkAk]NIJt3Hc4N__\\id\\1\\final\\");

                        byte[] b = Encoding.ASCII.GetBytes(s.ToString());

                        Write(user, b, 0, b.Length);

                        if (d.ContainsKey("userid") && d.ContainsKey("profileid"))
                            return 1;
                        else
                            return 0;
                    }
                }

                Write(user, _data[4], 0, _data[4].Length);

                return 1;
            }

            // \logout\\sesskey\239288\final\

            if (d.ContainsKey("logout"))
                return 1;

            return 0;
        }

        private static string GetHash(MD5 md5, string text)
        {
            return Utils.GetHex(md5.ComputeHash(Encoding.ASCII.GetBytes(text)), 16);
        }
    }
}
