#pragma warning disable CA1001, CA1031, CA1051, CA1707, CA1717

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace SfcOpServer
{
    public enum Events
    {
        Q,
        Q_16_3_D,

        Total
    }

    public enum Relays
    {
        CharacterLogOnRelayNameC,
        MessengerRelayNameC,
        MetaViewPortHandlerNameC,
        MissionRelayNameC,
        PlayerRelayC,

        MedalsPanel,
        MetaClientChatPanel,
        MetaClientHelpListPanel,
        MetaClientMissionPanel,
        MetaClientNewsPanel,
        MetaClientPlayerListPanel,
        MetaClientShipPanel,
        MetaClientSupplyDockPanel,
        PlayerInfoPanel,

        Total
    }

    public class Client27000 : AsyncUser
    {
        public const int MinimumBufferSize = 4;
        public const int MaximumBufferSize = 65536;

        // references

        public Character Character;

        // status

        public int[] Event;
        public int[] Relay;

        public double LastActivity;
        public int LastTurn;
        public int HexRequest; // -1 not scheduled, >= 0 scheduled, -2 waiting for the client request
        public int IconsRequest; // 0 not scheduled, 1 scheduled, 2 waiting for the client request
        public readonly Dictionary<int, int> IdList; // client.Id, character.Id

        public long Address;
        public readonly Queue<ClientMessage> Messages;

        public Client27000(Socket sock)
        {
            InitializeUser(sock);

            // references

            Character = null;

            // status

            Event = new int[(int)Events.Total];
            Relay = new int[(int)Relays.Total];

            Array.Fill(Relay, -1);

            LastActivity = 0.0;
            LastTurn = 0;
            HexRequest = -1;
            IconsRequest = 0;
            IdList = new Dictionary<int, int>();

            Address = 0;
            Messages = new Queue<ClientMessage>();
        }
    }
}
