#pragma warning disable CA1051, CA1717

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public enum TerrainTypes
    {
        kTerrainNone = 0,

        kTerrainSpace1 = 1 << 0,
        kTerrainSpace2 = 1 << 1,
        kTerrainSpace3 = 1 << 2,
        kTerrainSpace4 = 1 << 3,
        kTerrainSpace5 = 1 << 4,
        kTerrainSpace6 = 1 << 5, // cell.Terrain == 0

        kTerrainAsteroids1 = 1 << 6,
        kTerrainAsteroids2 = 1 << 7,
        kTerrainAsteroids3 = 1 << 8,
        kTerrainAsteroids4 = 1 << 9,
        kTerrainAsteroids5 = 1 << 10,
        kTerrainAsteroids6 = 1 << 11,

        kTerrainNebula1 = 1 << 12,
        kTerrainNebula2 = 1 << 13,
        kTerrainNebula3 = 1 << 14,
        kTerrainNebula4 = 1 << 15,
        kTerrainNebula5 = 1 << 16,
        kTerrainNebula6 = 1 << 17,

        kTerrainBlackHole1 = 1 << 18,
        kTerrainBlackHole2 = 1 << 19,
        kTerrainBlackHole3 = 1 << 20,
        kTerrainBlackHole4 = 1 << 21,
        kTerrainBlackHole5 = 1 << 22,
        kTerrainBlackHole6 = 1 << 23,

        kTerrainDustclouds = 1 << 24,
        kTerrainShippingLane = 1 << 25,

        kAnySpace = kTerrainSpace1 | kTerrainSpace2 | kTerrainSpace3 | kTerrainSpace4 | kTerrainSpace5 | kTerrainSpace6,
        kAnyAsteroids = kTerrainAsteroids1 | kTerrainAsteroids2 | kTerrainAsteroids3 | kTerrainAsteroids4 | kTerrainAsteroids5 | kTerrainAsteroids6,
        kAnyNebula = kTerrainNebula1 | kTerrainNebula2 | kTerrainNebula3 | kTerrainNebula4 | kTerrainNebula5 | kTerrainNebula6,
        kAnyBlackHole = kTerrainBlackHole1 | kTerrainBlackHole2 | kTerrainBlackHole3 | kTerrainBlackHole4 | kTerrainBlackHole5 | kTerrainBlackHole6
    }

    public enum PlanetTypes
    {
        kPlanetNone = 0,

        kPlanetHomeWorld1 = 1 << 0,
        kPlanetHomeWorld2 = 1 << 1,
        kPlanetHomeWorld3 = 1 << 2,
        kPlanetCoreWorld1 = 1 << 3,
        kPlanetCoreWorld2 = 1 << 4,
        kPlanetCoreWorld3 = 1 << 5,
        kPlanetColony1 = 1 << 6,
        kPlanetColony2 = 1 << 7,
        kPlanetColony3 = 1 << 8,
        kPlanetAsteroidBase1 = 1 << 9,
        kPlanetAsteroidBase2 = 1 << 10,
        kPlanetAsteroidBase3 = 1 << 11,
    }

    public enum BaseTypes
    {
        kBaseNone = 0,

        kBaseStarbase = 1 << 0,
        kBaseBattleStation = 1 << 1,
        kBaseBaseStation = 1 << 2,
        kBaseWeaponsPlatform = 1 << 3,
        kBaseListeningPost = 1 << 4,
    }

    public class MapHex
    {
        // data

        public int Id;

        public int X;
        public int Y;

        public Races EmpireControl;
        public Races CartelControl;

        public int Terrain;
        public int Planet;
        public int Base;

        public TerrainTypes TerrainType;
        public PlanetTypes PlanetType;
        public BaseTypes BaseType;

        public int BaseEconomicPoints;
        public int CurrentEconomicPoints;

        public int EmpireBaseVictoryPoints;
        public int EmpireCurrentVictoryPoints;

        public int CartelBaseVictoryPoints;
        public int CartelCurrentVictoryPoints;

        public double BaseSpeedPoints;
        public double CurrentSpeedPoints;

        // helpers

        public double[] ControlPoints;
        public long Mission;
        public Dictionary<int, object> Population;
        public int[] PopulationCount;

        public MapHex()
        {
            // helpers

            ControlPoints = new double[(int)Races.kNumberOfRaces];

            Population = new Dictionary<int, object>();
            PopulationCount = new int[(int)Races.kNumberOfRaces];
        }

        public MapHex(BinaryReader r)
        {
            Contract.Requires(r != null);

            // data

            Id = r.ReadInt32();

            X = r.ReadInt32();
            Y = r.ReadInt32();

            EmpireControl = (Races)r.ReadInt32();
            CartelControl = (Races)r.ReadInt32();

            Terrain = r.ReadInt32();
            Planet = r.ReadInt32();
            Base = r.ReadInt32();

            TerrainType = (TerrainTypes)r.ReadInt32();
            PlanetType = (PlanetTypes)r.ReadInt32();
            BaseType = (BaseTypes)r.ReadInt32();

            BaseEconomicPoints = r.ReadInt32();
            CurrentEconomicPoints = r.ReadInt32();

            EmpireBaseVictoryPoints = r.ReadInt32();
            EmpireCurrentVictoryPoints = r.ReadInt32();

            CartelBaseVictoryPoints = r.ReadInt32();
            CartelCurrentVictoryPoints = r.ReadInt32();

            BaseSpeedPoints = r.ReadDouble();
            CurrentSpeedPoints = r.ReadDouble();

            // helpers

            ControlPoints = new double[(int)Races.kNumberOfRaces];

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                ControlPoints[i] = r.ReadDouble();

            Mission = r.ReadInt64();
            Population = new Dictionary<int, object>();

            while (true)
            {
                int characterId = r.ReadInt32();

                if (characterId == 0)
                    break;

                Population.Add(characterId, null);
            }

            PopulationCount = new int[(int)Races.kNumberOfRaces];

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                PopulationCount[i] = r.ReadInt32();
        }

        public void WriteTo(BinaryWriter w)
        {
            Contract.Requires(w != null);

            // data

            w.Write(Id);

            w.Write(X);
            w.Write(Y);

            w.Write((int)EmpireControl);
            w.Write((int)CartelControl);

            w.Write(Terrain);
            w.Write(Planet);
            w.Write(Base);

            w.Write((int)TerrainType);
            w.Write((int)PlanetType);
            w.Write((int)BaseType);

            w.Write(BaseEconomicPoints);
            w.Write(CurrentEconomicPoints);

            w.Write(EmpireBaseVictoryPoints);
            w.Write(EmpireCurrentVictoryPoints);

            w.Write(CartelBaseVictoryPoints);
            w.Write(CartelCurrentVictoryPoints);

            w.Write(BaseSpeedPoints);
            w.Write(CurrentSpeedPoints);

            // helpers

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(ControlPoints[i]);

            w.Write(Mission);

            foreach (KeyValuePair<int, object> p in Population)
                w.Write(p.Key);

            w.Write(0);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(PopulationCount[i]);
        }
    }
}
