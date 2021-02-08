using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace SfcOpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Contract.Requires(args != null);

            //KillProcess("StarFleetOP");

            // gets the current list of IPs

            string hostName = Dns.GetHostName();
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            IPAddress[] hostAddressList = hostEntry.AddressList;
            List<string> AddressList = new List<string>();

            Console.WriteLine("Address list:");
            Console.WriteLine();

            string data;

            for (int i = 0; i < hostAddressList.Length; i++)
            {
                if (hostAddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    data = hostAddressList[i].ToString();

                    Console.WriteLine(AddressList.Count.ToString(CultureInfo.InvariantCulture) + ". " + data);

                    AddressList.Add(data);
                }
            }

            Console.WriteLine();
            Console.Write("Local address: ");

            data = Console.ReadLine();

            if (!int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out int AddressIndex) || AddressIndex < 0 || AddressIndex >= AddressList.Count)
                return;

            Console.WriteLine();

            data = AddressList[AddressIndex];

            IPAddress privateIP = IPAddress.Parse(data);
            IPAddress publicIP = IPAddress.Parse(data);

            Contract.Requires(privateIP != null && publicIP != null);

            // starts the services

#if DEBUG
            string appDirectory = "C:/Users/D4v1k/Documents/My Games/Starfleet Command 2 Orion Pirates";
#else
            string appDirectory = AppContext.BaseDirectory;
#endif

            if (!Directory.Exists(appDirectory))
            {
                Console.WriteLine("ERROR: directory not found!");

                return;
            }

            AssemblyName app = Assembly.GetEntryAssembly().GetName();

            string appName = app.Name + " " + app.Version.ToString();

            string[] motd = {
                "<!DOCTYPE html><html><head><title>Index</title></head><body>Under construction...</body></html>",
                appName + " 1.0 (C) D4v1ks",
                "SFC2EAW 2.0.3.7 Patch (C) TarMinyatur, D4v1ks, Adam",
                "SFC2OP 2.5.6.4 Patch (C) D4v1ks, TarMinyatur, Adam, Javora, Darkdrone, Falconer",
                "SFC3 HD/Gamespy Patch (C) D4v1ks, Falconer"
            };

            // ... Directory

            Server80.Initialize(appName, motd);
            Server15101.Initialize(publicIP);
            Server15300.Initialize();

            using Server80 server80 = new Server80();
            using Server15101 server15101 = new Server15101();
            using Server15300 server15300 = new Server15300();

            server80.Start(privateIP);
            server15101.Start(privateIP);
            server15300.Start(privateIP);

            // ... Gamespy

            GsService.Initialize();

            Server28900.Initialize();
            Server29900.Initialize();
            Server29901.Initialize();

            using Server28900 server28900 = new Server28900();
            using Server29900 server29900 = new Server29900();
            using Server29901 server29901 = new Server29901();

            server28900.Start(privateIP);
            server29900.Start(privateIP);
            server29901.Start(privateIP);

            // ... InternetRelayChat

            IrcService.InitializeAndStart(motd, privateIP);

            // selects a server

#if DEBUG
            Console.Write("You want to (r)un the new server or (d)ebug a stock server? ");

            data = Console.ReadLine();
#else
            Console.Write("Starting the new server...");
            
            data = "r";
#endif

            Console.WriteLine();

            // tries to launch the server

            GameServer server = null;

            if (data.Equals("r", StringComparison.Ordinal))
            {
                GameServer.Initialize();

                server = new GameServer(privateIP, 27000, appDirectory, "2.5.6.4");

                server.Start();
            }

#if DEBUG
            else if (data.Equals("d", StringComparison.Ordinal))
            {
                // makes sure no stock server is running in the background

                KillProcess("ServerPlatform");

                // makes sure the stock server is configured with the settings we need

                GameFile gf = new GameFile();

                // ... ServerSetup.gf

                gf.Load(appDirectory + "/Assets/Settings/Dedicated/Standard/ServerSetup.gf");

                if (gf.TryGetValue("CentralSwitchSetup", "CentralSwitchPort", out int port) && port != 27001)
                {
                    gf.AddOrUpdate("CentralSwitchSetup", "CentralSwitchPort", 27001);

                    gf.Save();
                }

                gf.Clear();

                // ... Chat.gf

                gf.Load(appDirectory + "/Assets/Settings/Dedicated/Chat.gf");

                if (gf.TryGetValue("Server", "NickName", out string nick, out bool quotes) && !nick.Equals("A1", StringComparison.Ordinal))
                {
                    gf.AddOrUpdate("Server", "NickName", "A1", true);
                    gf.AddOrUpdate("Server", "Name", "A1", true);
                    gf.AddOrUpdate("Server", "VerboseName", "A1", true);

                    gf.Save();
                }

                gf.Clear();

                Thread.Sleep(500);

                // launches the stock server as a separated process

                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = appDirectory,
                    FileName = appDirectory + "/ServerPlatform.exe",
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                // VERY IMPORTANT: it is assumed here that the stock server has at least run once, previously, using the SFC Launcher                

                ManInTheMiddle mitm = new ManInTheMiddle(privateIP);
            }
#endif

            else
            {
                Console.WriteLine("Invalid option!");
            }

            // waits for input

            Console.WriteLine("Press ENTER, at any time, to exit...");
            Console.ReadLine();

            // closes everything

            server?.Close();
        }

#if DEBUG
        static void KillProcess(string processName)
        {
            Process[] processes;

            try
            {
                processes = Process.GetProcessesByName(processName);
            }
            catch (Exception)
            {
                processes = null;
            }

            if (processes != null)
            {
                for (int i = 0; i < processes.Length; i++)
                {
                    try
                    {
                        processes[i].Kill();
                    }
                    catch (Exception)
                    { }
                }
            }
        }
#endif

    }
}
