#pragma warning disable CA1051, CA1069, CA1707, CA1717, CA1815

using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public class Ship
    {
        public const int DamageSize = 100;
        public const int MinStoresSize = 387;
        public const int MinOfficersSize = 112;

        // data

        public int Id;
        public int LockID;
        public int OwnerID;
        public byte IsInAuction;
        public Races Race;
        public ClassTypes ClassType;
        public int BPV;
        public int EPV;
        public string ShipClassName;
        public string Name;
        public int TurnCreated;
        public ShipDamage Damage;
        public ShipStores Stores;
        public ShipOfficers Officers;
        public int Flags;

        // helpers

        public byte[] ShipCache;

        public Ship()
        { }

        public Ship(int id)
        {
            Id = id;
        }

        public Ship(BinaryReader r)
        {
            // data

            Id = r.ReadInt32();
            LockID = r.ReadInt32();
            OwnerID = r.ReadInt32();
            IsInAuction = r.ReadByte();
            Race = (Races)r.ReadInt32();
            ClassType = (ClassTypes)r.ReadInt32();
            BPV = r.ReadInt32();
            EPV = r.ReadInt32();

            Utils.ReadString(r, out ShipClassName);
            Utils.ReadString(r, out Name);

            TurnCreated = r.ReadInt32();

            Damage = new ShipDamage(r);
            Stores = new ShipStores(r);
            Officers = new ShipOfficers(r);

            Flags = r.ReadInt32();

            // helpers

            int c = r.ReadInt32();

            if (c == 0)
                ShipCache = null;
            else
                ShipCache = r.ReadBytes(c);
        }

        public void WriteTo(BinaryWriter w)
        {
            w.Write(Id);
            w.Write(LockID);
            w.Write(OwnerID);
            w.Write(IsInAuction);
            w.Write((int)Race);
            w.Write((int)ClassType);
            w.Write(BPV);
            w.Write(EPV);

            Utils.WriteString(w, ShipClassName);
            Utils.WriteString(w, Name);

            w.Write(TurnCreated);

            Damage.WriteTo(w);
            Stores.WriteTo(w);
            Officers.WriteTo(w);

            w.Write(Flags);

            // helpers

            if (ShipCache == null)
                w.Write(0);
            else
            {
                w.Write(ShipCache.Length);
                w.Write(ShipCache);
            }
        }

        public static int GetShipSize(byte[] buffer, int index)
        {
            int p = index;

            // header

            int c = GetHeaderSize(buffer, p);

            p += c;

            // damage

            p += DamageSize;

            // stores

            c = GetStoresSize(buffer, p);
            p += c;

            // officers

            c = GetOfficersSize(buffer, p);
            p += c;

            // flags

            p += 4;

            return p - index;
        }

        public static int GetHeaderSize(byte[] buffer, int index)
        {
            int p = index;

            Contract.Assert((buffer.Length - p) >= DamageSize);

            // first part

            p += 29;

            // ship class name

            int c = BitConverter.ToInt32(buffer, p);

            p += 4;
            p += c;

            // name

            c = BitConverter.ToInt32(buffer, p);

            p += 4;
            p += c;

            // last part

            p += 4;

            return p - index;
        }

        public static int GetStoresSize(byte[] buffer, int index)
        {
            int p = index;

            Contract.Assert((buffer.Length - p) >= MinStoresSize);

            // 2nd section

            p += ShipStores.Offset_TransportItems;

            int c = BitConverter.ToInt32(buffer, p);

            p += 4;
            p += c * ShipStores.Size_TransportItems;

            // 3rd section

            p += ShipStores.Size_Unknown4;

            // 4th section

            p += ShipStores.Size_Section4;

            // 5th section

            for (int i = 0; i < 4; i++)
            {
                p += 4;

                c = BitConverter.ToInt32(buffer, p);

                p += 4;
                p += c;

                c = BitConverter.ToInt32(buffer, p);

                p += 4;
                p += c;
            }

            return p - index;
        }

        public static int GetOfficersSize(byte[] buffer, int index)
        {
            int p = index;

            Contract.Assert((buffer.Length - p) >= MinOfficersSize);

            for (int i = 0; i < 7; i++)
            {
                // officer name

                int c = BitConverter.ToInt32(buffer, p);

                p += 4;
                p += c;

                // last part

                p += 12;
            }

            return p - index;
        }
    }
}
