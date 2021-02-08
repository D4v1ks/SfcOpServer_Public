#pragma warning disable CA1051, CA1717

using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public enum Ranks
    {
        None = -1,

        Ensign,
        Lieutenant,
        LieutenantCommander,
        Captain,
        Commodore,
        RearAdmiral,
        Admiral,
        FleetAdmiral,

        Total
    }

    public enum Medals
    {
        kNoMedals = 0,

        kMedalRankOne = 1 << 0,
        kMedalRankTwo = 1 << 1,
        kMedalRankThree = 1 << 2,
        kMedalRankFour = 1 << 3,
        kMedalRankFive = 1 << 4,

        kMedalMissionOne = 1 << 5,
        kMedalMissionTwo = 1 << 6,
        kMedalMissionThree = 1 << 7,
        kMedalMissionFour = 1 << 8,

        kMedalSpecialOne = 1 << 9,
        kMedalSpecialTwo = 1 << 10,
        kMedalSpecialThree = 1 << 11,
        kMedalSpecialFour = 1 << 12,

        kAllMedals = kMedalRankOne | kMedalRankTwo | kMedalRankThree | kMedalRankFour | kMedalRankFive | kMedalMissionOne | kMedalMissionTwo | kMedalMissionThree | kMedalMissionFour | kMedalSpecialOne | kMedalSpecialTwo | kMedalSpecialThree | kMedalSpecialFour
    };

    public class Character
    {
        public const int MaxFleetSize = 6;

        public enum States
        {
            None = 0,

            // basic states

            IsCpu = 1 << 0,
            IsHuman = 1 << 1,

            IsAfk = 1 << 2,
            IsBusy = 1 << 3,
            IsOnline = 1 << 4,

            IsConnecting = 1 << 5,
            IsReconnecting = 1 << 6,

            // cpu valid states

            IsCpuOnline = IsCpu | IsOnline,
            IsCpuBusyOnline = IsCpu | IsBusy | IsOnline,
            IsCpuAfkBusyOnline = IsCpu | IsAfk | IsBusy | IsOnline,

            // human valid states

            IsHumanOnline = IsHuman | IsOnline,
            IsHumanAfkOnline = IsHuman | IsAfk | IsOnline,
            IsHumanBusyOnline = IsHuman | IsBusy | IsOnline,
            IsHumanAfkBusyOnline = IsHuman | IsAfk | IsBusy | IsOnline,

            IsHumanBusyConnecting = IsHuman | IsBusy | IsConnecting,
            IsHumanBusyReconnecting = IsHuman | IsBusy | IsReconnecting,
        }

        // data

        public string IPAddress;
        public string WONLogon;
        public int Id;
        public string CharacterName;
        public Races CharacterRace;
        public Races CharacterPoliticalControl;
        public Ranks CharacterRank;
        public int CharacterRating;
        public int CharacterCurrentPrestige;
        public int CharacterLifetimePrestige;
        public int Unknown;
        public int CharacterLocationX;
        public int CharacterLocationY;
        public int HomeWorldLocationX;
        public int HomeWorldLocationY;
        public int MoveDestinationX;
        public int MoveDestinationY;
        public int ShipCount;
        public byte[] ShipCache;

        // helpers

        public Medals Awards;
        public int Bids;
        public long Mission;
        public int[] Ships;

        public int ShipsBestId; // 0 no ships, 1.. best ship id
        public int ShipsBestBPV;

        public int ShipsBPV;
        public States State;

        // references

        public Client27000 Client;

        public Character()
        {
            // data

            IPAddress = string.Empty;
            WONLogon = string.Empty;

            CharacterName = string.Empty;

            CharacterRank = Ranks.None;
            CharacterRating = 1500;

            CharacterLocationX = -1;
            CharacterLocationY = -1;
            HomeWorldLocationX = -1;
            HomeWorldLocationY = -1;
            MoveDestinationX = -1;
            MoveDestinationY = -1;

            // helpers

            Awards = Medals.kMedalRankOne;
            Ships = new int[MaxFleetSize];
        }

        public Character(int id)
        {
            Id = id;
        }

        public Character(BinaryReader r)
        {
            // data

            Contract.Requires(r != null);

            IPAddress = r.ReadString();
            WONLogon = r.ReadString();
            Id = r.ReadInt32();
            CharacterName = r.ReadString();
            CharacterRace = (Races)r.ReadInt32();
            CharacterPoliticalControl = (Races)r.ReadInt32();
            CharacterRank = (Ranks)r.ReadInt32();
            CharacterRating = r.ReadInt32();
            CharacterCurrentPrestige = r.ReadInt32();
            CharacterLifetimePrestige = r.ReadInt32();
            Unknown = r.ReadInt32();
            CharacterLocationX = r.ReadInt32();
            CharacterLocationY = r.ReadInt32();
            HomeWorldLocationX = r.ReadInt32();
            HomeWorldLocationY = r.ReadInt32();
            MoveDestinationX = r.ReadInt32();
            MoveDestinationY = r.ReadInt32();
            ShipCount = r.ReadInt32();

            int c = r.ReadInt32();

            if (c == 0)
                ShipCache = null;
            else
                ShipCache = r.ReadBytes(c);

            // helpers

            Awards = (Medals)r.ReadInt32();
            Bids = r.ReadInt32();
            Mission = r.ReadInt64();
            Ships = new int[MaxFleetSize];

            for (int i = 0; i < MaxFleetSize; i++)
                Ships[i] = r.ReadInt32();

            ShipsBestId = r.ReadInt32();
            ShipsBestBPV = r.ReadInt32();

            ShipsBPV = r.ReadInt32();
            State = (States)r.ReadInt64();

            // references

            Client = null;
        }

        public void WriteTo(BinaryWriter w)
        {
            Contract.Requires(w != null);

            // data

            w.Write(IPAddress);
            w.Write(WONLogon);
            w.Write(Id);
            w.Write(CharacterName);
            w.Write((int)CharacterRace);
            w.Write((int)CharacterPoliticalControl);
            w.Write((int)CharacterRank);
            w.Write(CharacterRating);
            w.Write(CharacterCurrentPrestige);
            w.Write(CharacterLifetimePrestige);
            w.Write(Unknown);
            w.Write(CharacterLocationX);
            w.Write(CharacterLocationY);
            w.Write(HomeWorldLocationX);
            w.Write(HomeWorldLocationY);
            w.Write(MoveDestinationX);
            w.Write(MoveDestinationY);
            w.Write(ShipCount);

            if (ShipCache == null)
                w.Write(0);
            else
            {
                w.Write(ShipCache.Length);
                w.Write(ShipCache);
            }

            // helpers

            w.Write((int)Awards);
            w.Write(Bids);
            w.Write(Mission);

            for (int i = 0; i < MaxFleetSize; i++)
                w.Write(Ships[i]);

            w.Write(ShipsBestId);
            w.Write(ShipsBestBPV);

            w.Write(ShipsBPV);
            w.Write((long)State);
        }
    }
}
