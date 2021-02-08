using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public enum OfficerRoles
    {
        kWeaponsOfficer,
        kEngineeringOfficer,
        kScienceOfficer,
        kCommOfficer,
        kHelmOfficer,
        kSecurityOfficer,
        kCaptainOfficer,

        kMaxOfficers
    };

    public enum OfficerRanks
    {
        kUnknownRank,

        kRookie,
        kJunior,
        kSenior, // default in stock server
        kVeteran,
        kLegendary,

        kMaxOfficerRank
    };

    public struct Officer
    {
        public string Name;
        public OfficerRanks Rank;
        public int Unknown1;
        public int Unknown2;
    }

    public class ShipOfficers
    {
        public Officer[] Items;

        public ShipOfficers(byte[] buffer, int index, int count)
        {
            using MemoryStream m = new MemoryStream(buffer, index, count);
            using BinaryReader r = new BinaryReader(m);

            ReadFrom(r);

            Contract.Assert(r.BaseStream.Position == count);
        }

        public ShipOfficers(BinaryReader r)
        {
            ReadFrom(r);
        }

        public void ReadFrom(BinaryReader r)
        {
            Items = new Officer[(int)OfficerRoles.kMaxOfficers];

            for (int i = 0; i < (int)OfficerRoles.kMaxOfficers; i++)
            {
                Utils.ReadString(r, out Items[i].Name);

                Items[i].Rank = (OfficerRanks)r.ReadInt32();
                Items[i].Unknown1 = r.ReadInt32();
                Items[i].Unknown2 = r.ReadInt32();
            }
        }

        public void WriteTo(BinaryWriter w)
        {
            for (int i = 0; i < (int)OfficerRoles.kMaxOfficers; i++)
            {
                Utils.WriteString(w, Items[i].Name);

                w.Write((int)Items[i].Rank);
                w.Write(Items[i].Unknown1);
                w.Write(Items[i].Unknown2);
            }
        }
    }
}
