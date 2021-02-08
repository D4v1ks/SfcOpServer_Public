using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public enum MissileTypes
    {
        TypeI,
        TypeIV,
        TypeM,
        TypeFighterI,
        TypeFighterIV,
        TypeDogfight,
        TypeD
    }

    public enum MissileDriveSystems
    {
        Slow,
        Medium,
        Fast
    }

    public enum ScriptTransportItems
    {
        kTransNothing,

        kTransGornEgg,
        kTransSpareParts,
        kTransCaptain,
        kTransBlueStar,
        kTransDilithiumCrystals,
        kTransGravTank,
        kTransMarineDude,
        kTransHarryMudd,
        kTransInfiltrator,
        kTransNovaMine,
        kTransRomulanAle,
        kTransTribbles,
        kTransAlienArtifact,
        kTransCaptainDude,
        kTransWeaponSchematic,
        kTransMedicalSupplies,
        kTransAwayTeamItem,
        kTransDiplomat,
        kTransInjuredDude,
        kTransMedicineJar,
        kTransScientists,
        kTransLifePodDude,
        kTransDeathPlague,
        kTransBlackBox,
        kTransPsionicDisruptor,
        kTransIonicProjector,
        kTransEngineers,
        kTransPrisoner,
    };

    public struct MissileHardpoint
    {
        public short MissilesReady;
        public short MissilesStored;
        public short TubesCount;
        public short TubesCapacity;
    }

    public struct TransportItem
    {
        public ScriptTransportItems Item;
        public int Count;
    }

    public struct FighterBay
    {
        public byte FightersCount;
        public byte FightersLoaded;
        public byte FightersMax;
        public byte Unknown;

        public string FighterType;
        public string FighterSubType;
    }

    public class ShipStores
    {
        public const int Offset_MissileHardpoints = 14;
        public const int Size_MissileHardpoints = 8;

        public const int Size_Unknown3 = 9;

        public const int Offset_TransportItems = 226;
        public const int Size_TransportItems = 8;

        public const int Size_Unknown4 = 100;

        public const int Size_Section4 = 9;

        public bool ContainsFighters => FighterBays[0].FighterType.Length + FighterBays[1].FighterType.Length + FighterBays[2].FighterType.Length + FighterBays[3].FighterType.Length != 0;

        // 1st section

        public byte Unknown1;
        public byte Unknown2;

        public MissileTypes MissilesType; // byte
        public MissileDriveSystems MissilesDriveSystem; // byte
        public short MissilesReloads;

        public short TotalTubesCount;
        public short TotalMissilesReadyAndStored;
        public short TotalMissilesReady;
        public short TotalMissilesStored;

        public MissileHardpoint[] MissileHardpoints;

        public byte GeneralMax;
        public byte GeneralBase;
        public byte General;

        public byte[] Unknown3;

        // 2nd section

        public TransportItem[] TransportItems;

        // 3rd section

        public byte[] Unknown4;

        // 4th section

        public byte BoardingPartiesMax;
        public byte BoardingPartiesBase;
        public byte BoardingParties;

        public byte TBombsMax;
        public byte TBombsBase;
        public byte TBombs;

        public byte DamageControlMax;
        public byte DamageControlBase;
        public byte DamageControl;

        // 5th section

        public FighterBay[] FighterBays;

        public ShipStores(byte[] buffer, int index, int count)
        {
            using MemoryStream m = new MemoryStream(buffer, index, count);
            using BinaryReader r = new BinaryReader(m);

            ReadFrom(r);

            Contract.Assert(r.BaseStream.Position == count);
        }

        public ShipStores(BinaryReader r)
        {
            ReadFrom(r);
        }

        public void ReadFrom(BinaryReader r)
        {
            // 1st section

            Unknown1 = r.ReadByte();
            Unknown2 = r.ReadByte();

            MissilesType = (MissileTypes)r.ReadByte();
            MissilesDriveSystem = (MissileDriveSystems)r.ReadByte();
            MissilesReloads = r.ReadInt16();

            TotalTubesCount = r.ReadInt16();
            TotalMissilesReadyAndStored = r.ReadInt16();
            TotalMissilesReady = r.ReadInt16();
            TotalMissilesStored = r.ReadInt16();

            MissileHardpoints = new MissileHardpoint[25];

            for (int i = 0; i < 25; i++)
            {
                MissileHardpoints[i].MissilesReady = r.ReadInt16();
                MissileHardpoints[i].MissilesStored = r.ReadInt16();
                MissileHardpoints[i].TubesCount = r.ReadInt16();
                MissileHardpoints[i].TubesCapacity = r.ReadInt16();
            }

            GeneralMax = r.ReadByte();
            GeneralBase = r.ReadByte();
            General = r.ReadByte();

            Unknown3 = r.ReadBytes(Size_Unknown3);

            // 2nd section

            int c = r.ReadInt32();

            if (c == 0)
                TransportItems = null;
            else
            {
                TransportItems = new TransportItem[c];

                for (int i = 0; i < c; i++)
                {
                    TransportItems[i].Item = (ScriptTransportItems)r.ReadInt32();
                    TransportItems[i].Count = r.ReadInt32();
                }
            }

            // 3rd section

            Unknown4 = r.ReadBytes(Size_Unknown4);

            // 4th section

            BoardingPartiesMax = r.ReadByte();
            BoardingPartiesBase = r.ReadByte();
            BoardingParties = r.ReadByte();

            TBombsMax = r.ReadByte();
            TBombsBase = r.ReadByte();
            TBombs = r.ReadByte();

            DamageControlMax = r.ReadByte();
            DamageControlBase = r.ReadByte();
            DamageControl = r.ReadByte();

            // 5th section

            FighterBays = new FighterBay[4];

            for (int i = 0; i < 4; i++)
            {
                FighterBays[i].FightersCount = r.ReadByte();
                FighterBays[i].FightersLoaded = r.ReadByte();
                FighterBays[i].FightersMax = r.ReadByte();
                FighterBays[i].Unknown = r.ReadByte();

                Utils.ReadString(r, out FighterBays[i].FighterType);
                Utils.ReadString(r, out FighterBays[i].FighterSubType);
            }
        }

        public void WriteTo(BinaryWriter w)
        {
            // 1st section

            w.Write(Unknown1);
            w.Write(Unknown2);

            w.Write((byte)MissilesType);
            w.Write((byte)MissilesDriveSystem);
            w.Write(MissilesReloads);

            w.Write(TotalTubesCount);
            w.Write(TotalMissilesReadyAndStored);
            w.Write(TotalMissilesReady);
            w.Write(TotalMissilesStored);

            for (int i = 0; i < 25; i++)
            {
                w.Write(MissileHardpoints[i].MissilesReady);
                w.Write(MissileHardpoints[i].MissilesStored);
                w.Write(MissileHardpoints[i].TubesCount);
                w.Write(MissileHardpoints[i].TubesCapacity);
            }

            w.Write(GeneralMax);
            w.Write(GeneralBase);
            w.Write(General);

            w.Write(Unknown3);

            // 2nd section

            if (TransportItems == null)
                w.Write(0);
            else
            {
                int c = TransportItems.Length;

                w.Write(c);

                for (int i = 0; i < c; i++)
                {
                    w.Write((int)TransportItems[i].Item);
                    w.Write(TransportItems[i].Count);
                }
            }

            // 3rd section

            w.Write(Unknown4);

            // 4th section

            w.Write(BoardingPartiesMax);
            w.Write(BoardingPartiesBase);
            w.Write(BoardingParties);

            w.Write(TBombsMax);
            w.Write(TBombsBase);
            w.Write(TBombs);

            w.Write(DamageControlMax);
            w.Write(DamageControlBase);
            w.Write(DamageControl);

            // 5th section

            for (int i = 0; i < 4; i++)
            {
                w.Write(FighterBays[i].FightersCount);
                w.Write(FighterBays[i].FightersLoaded);
                w.Write(FighterBays[i].FightersMax);
                w.Write(FighterBays[i].Unknown);

                Utils.WriteString(w, FighterBays[i].FighterType);
                Utils.WriteString(w, FighterBays[i].FighterSubType);
            }
        }
    }
}
