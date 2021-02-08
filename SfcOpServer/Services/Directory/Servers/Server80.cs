using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SfcOpServer
{
    public class Server80 : AsyncServer
    {
        private const int defaultMessage = 3;

        private const string opcode200 = "200 OK";
        private const string opcode404 = "404 Not Found";

        private static byte[][] _data;
        private static string[] _message;

        public static void Initialize(string appName, string[] motd)
        {
            Contract.Requires(motd != null);

            _data = new byte[][]
            {
                // GET / HTTP/1.1
                new byte[] { 71, 69, 84, 32, 47, 32, 72, 84, 84, 80, 47, 49, 46, 49 },

                // GET /motd/sys/motd.txt HTTP/1.1
                new byte[] { 71, 69, 84, 32, 47, 109, 111, 116, 100, 47, 115, 121, 115, 47, 109, 111, 116, 100, 46, 116, 120, 116, 32, 72, 84, 84, 80, 47, 49, 46, 49 },

                // GET /motd/starfleetcommand2/motd.txt HTTP/1.1
                new byte[] { 71, 69, 84, 32, 47, 109, 111, 116, 100, 47, 115, 116, 97, 114, 102, 108, 101, 101, 116, 99, 111, 109, 109, 97, 110, 100, 50, 47, 109, 111, 116, 100, 46, 116, 120, 116, 32, 72, 84, 84, 80, 47, 49, 46, 49 },
            };

            _message = new string[]
            {
                // gamespy index
                motd[0],

                // system message
                motd[1],

                // game message
                motd[3], 

                // default message
                "<!DOCTYPE HTML><html><head><title>404 Not Found</title></head><body><h1>Not Found</h1><p>The requested URL was not found on this server.</p></body></html>",

                // server name
                appName,

                // last modified

                GetGMT()
            };
        }

        private static string GetGMT()
        {
            return DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " GMT";
        }

        private static byte[] GetHttpHtml(string opcode, string content)
        {
            StringBuilder t = new StringBuilder(1024);

            string date = GetGMT();

            t.Append("HTTP/1.1 ");
            t.AppendLine(opcode);

            t.Append("Date: ");
            t.AppendLine(date);

            t.Append("Server: ");
            t.AppendLine(_message[defaultMessage + 1]);

            t.Append("Last-Modified: ");
            t.AppendLine(_message[defaultMessage + 2]);

            t.AppendLine("Accept-Ranges: bytes");

            t.Append("Content-Length: ");
            t.AppendLine(content.Length.ToString(CultureInfo.InvariantCulture));

            t.AppendLine("Content-Type: text/html");
            t.AppendLine();

            t.Append(content);

            return Encoding.ASCII.GetBytes(t.ToString());
        }

        private static byte[] GetHttpText(string opcode, string content)
        {
            StringBuilder t = new StringBuilder(1024);

            string date = GetGMT();

            t.Append("HTTP/1.1 ");
            t.AppendLine(opcode);

            t.Append("Date: ");
            t.AppendLine(date);

            t.Append("Server: ");
            t.AppendLine(_message[defaultMessage + 1]);

            t.Append("Last-Modified: ");
            t.AppendLine(_message[defaultMessage + 2]);

            t.AppendLine("Accept-Ranges: bytes");

            t.Append("Content-Length: ");
            t.AppendLine((content.Length + 1).ToString(CultureInfo.InvariantCulture));

            t.AppendLine("Connection: close");
            t.AppendLine("Content-Type: text/plain");
            t.AppendLine();

            t.Append(' ');
            t.Append(content);

            return Encoding.ASCII.GetBytes(t.ToString());
        }

        public Server80()
        {
            Initialize(GameServer.MaxNumPlayers, 0, 512);
        }

        public void Start(IPAddress privateIP)
        {
            Start(new IPEndPoint(privateIP, 80));
        }

        public override void BeginCloseUser(AsyncUser user)
        { }

        public override AsyncUser CreateUser(Socket sock)
        {
            AsyncUser user = new AsyncUser();

            user.InitializeUser(sock);

            return user;
        }

        public unsafe override int GetSize(byte[] buffer, int size)
        {
            Contract.Requires(buffer != null);

            if (size >= 4)
            {
                fixed (byte* b = buffer)
                {
                    byte* b0 = b;
                    byte* b1 = b + size - 4;

                    do
                    {
                        if (*(int*)b0 == 0x0a0d0a0d)
                            return (int)(b0 - b) + 4;

                        b0++;
                    }
                    while (b0 <= b1);
                }
            }

            return buffer.Length;
        }

        public override void Handshake(AsyncUser user)
        { }

        public override int Process(AsyncUser user, byte[] buffer, int size)
        {
            int r = defaultMessage;

            for (int i = 0; i < defaultMessage; i++)
            {
                if (Utils.StartsWith(buffer, size, _data[i]))
                {
                    r = i;

                    break;
                }
            }

            byte[] http = r switch
            {
                0 => GetHttpHtml(opcode200, _message[0]),
                1 => GetHttpText(opcode200, _message[1]),
                2 => GetHttpText(opcode200, _message[2]),
                _ => GetHttpHtml(opcode404, _message[defaultMessage])
            };

            Write(user, http, 0, http.Length);

            return 1;
        }
    }
}
