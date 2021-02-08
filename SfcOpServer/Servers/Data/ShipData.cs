#pragma warning disable CA1051, CA1717

namespace SfcOpServer
{
    public enum Races
    {
        kNoRace = -1,

        // empires

        kFederation,
        kKlingon,
        kRomulan,
        kLyran,
        kHydran,
        kGorn,
        kISC,
        kMirak,

        // cartels

        kOrionOrion,
        kOrionKorgath,
        kOrionPrime,
        kOrionTigerHeart,
        kOrionBeastRaiders,
        kOrionSyndicate,
        kOrionWyldeFire,
        kOrionCamboro,

        // NPC

        kOrion,
        kMonster,

        kTholian,
        kLDR,
        kWYN,
        kJindarian,
        kAndro,

        kNeutralRace,

        kMirror,

        // total

        kNumberOfRaces,

        // helpers

        kFirstEmpire = kFederation,
        kLastEmpire = kMirak,

        kFirstCartel = kOrionOrion,
        kLastCartel = kOrionCamboro,

        kFirstNPC = kOrion,
        kLastNPC = kMonster
    };

    public enum ClassTypes
    {
        kNoClassType = -1, // used for stars (NONE)

        // ships

        kClassShuttle,
        kClassPseudoFighter,
        kClassFreighter,
        kClassFrigate,
        kClassDestroyer,
        kClassWarDestroyer,
        kClassLightCruiser,
        kClassHeavyCruiser,
        kClassNewHeavyCruiser,
        kClassHeavyBattlecruiser,
        kClassCarrier,
        kClassDreadnought,
        kClassBattleship,

        // bases

        kClassListeningPost,
        kClassBaseStation,
        kClassBattleStation,
        kClassStarBase,

        // other

        kClassMonster,
        kClassPlanets,
        kClassSpecial,

        // total

        kMaxClasses
    };

    public enum SpecialRoles
    {
        None,

        NotSpecified = 1 << 0,

        A = 1 << 1,
        B = 1 << 2,
        C = 1 << 3,
        D = 1 << 4,
        E = 1 << 5,
        F = 1 << 6,
        G = 1 << 7,
        H = 1 << 8,
        I = 1 << 9,
        J = 1 << 10,
        K = 1 << 11,
        L = 1 << 12,
        M = 1 << 13,
        N = 1 << 14,
        O = 1 << 15,
        P = 1 << 16,
        Q = 1 << 17,
        R = 1 << 18,
        S = 1 << 19,
        T = 1 << 20,
        U = 1 << 21,
        V = 1 << 22,
        W = 1 << 23,
        X = 1 << 24,
        Y = 1 << 25,
        Z = 1 << 26,

        All = NotSpecified | A | B | C | D | E | F | G | H | I | J | K | L | M | N | O | P | Q | R | S | T | U | V | W | X | Y | Z
    }

    public enum WeaponTypes
    {
        None = -1,

        Phot,
        PhoF,
        PhoH,

        Dis1,
        Dis2,
        Dis3,
        Dis4,
        DisF,
        DisH,

        Fus,
        FusF,

        HB,

        Hellf,

        PlaR,
        PlaS,
        PlaG,
        PlaF,
        PlaI,
        PlaD,
        PlaX,
        PlaE,

        ESGL,

        MLR,

        PhA,
        PhB,

        TRBL,
        TRBH,

        DroA,
        DroB,
        DroC,
        DroD,
        DroE,
        DroF,
        DroG,
        DroH,
        DroI,
        DroVI,
        DroM,

        Opt,
        OptW,

        Ph1,
        Ph2,
        Ph3,
        PhG,
        PhG2,
        Ph4,
        PhX,

        ADD6,
        ADD12,
        ADD30,

        ESG,
        PPD,

        Total
    }

    public class ShipData
    {
        public Races Race;
        public string HullType;
        public string ClassName;
        public ClassTypes ClassType;
        public int BPV;
        public SpecialRoles SpecialRole;
        public int YearFirstAvailable;
        public int YearLastAvailable;
        public int SizeClass;
        public string TurnMode;
        public float MoveCost;
        public int HetAndNimble;
        public int HetBreakdown;
        public int StealthOrECM;
        public float RegularCrew;
        public int BoardingPartiesBase;
        public int BoardingPartiesMax;
        public int DeckCrews;
        public float TotalCrew;
        public int MinCrew;
        public int Shield1;
        public int Shield2And6;
        public int Shield3And5;
        public int Shield4;
        public int ShieldTotal;
        public int Cloak;
        public int Num1;
        public WeaponTypes HeavyWeapon1;
        public string Arc1;
        public int Num2;
        public WeaponTypes HeavyWeapon2;
        public string Arc2;
        public int Num3;
        public WeaponTypes HeavyWeapon3;
        public string Arc3;
        public int Num4;
        public WeaponTypes HeavyWeapon4;
        public string Arc4;
        public int Num5;
        public WeaponTypes HeavyWeapon5;
        public string Arc5;
        public int Num6;
        public WeaponTypes HeavyWeapon6;
        public string Arc6;
        public int Num7;
        public WeaponTypes HeavyWeapon7;
        public string Arc7;
        public int Num8;
        public WeaponTypes HeavyWeapon8;
        public string Arc8;
        public int Num9;
        public WeaponTypes HeavyWeapon9;
        public string Arc9;
        public int Num10;
        public WeaponTypes HeavyWeapon10;
        public string Arc10;
        public int Num11;
        public WeaponTypes Weapon11;
        public string Arc11;
        public int Num12;
        public WeaponTypes Weapon12;
        public string Arc12;
        public int Num13;
        public WeaponTypes Weapon13;
        public string Arc13;
        public int Num14;
        public WeaponTypes Weapon14;
        public string Arc14;
        public int Num15;
        public WeaponTypes Weapon15;
        public string Arc15;
        public int Num16;
        public WeaponTypes Weapon16;
        public string Arc16;
        public int Num17;
        public WeaponTypes Weapon17;
        public string Arc17;
        public int Num18;
        public WeaponTypes Weapon18;
        public string Arc18;
        public int Num19;
        public WeaponTypes Weapon19;
        public string Arc19;
        public int Num20;
        public WeaponTypes Weapon20;
        public string Arc20;
        public int Num21;
        public WeaponTypes Weapon21;
        public string Arc21;
        public int Num22;
        public WeaponTypes Weapon22;
        public string Arc22;
        public int Num23;
        public WeaponTypes Weapon23;
        public string Arc23;
        public int Num24;
        public WeaponTypes Weapon24;
        public string Arc24;
        public int Num25;
        public WeaponTypes Weapon25;
        public string Arc25;
        public int Probes;
        public int T_BombsBase;
        public int T_BombsMax;
        public int NuclearMineBase;
        public int NuclearMineMax;
        public int DroneControl;
        public int ADD_6;
        public int ADD_12;
        public int ShuttlesSize;
        public int LaunchRate;
        public int GeneralBase;
        public int GeneralMax;
        public int FighterBay1;
        public string FighterType1;
        public int FighterBay2;
        public string FighterType2;
        public int FighterBay3;
        public string FighterType3;
        public int FighterBay4;
        public string FighterType4;
        public int Armor;
        public int ForwardHull;
        public int CenterHull;
        public int AftHull;
        public int Cargo;
        public int Barracks;
        public int Repair;
        public int R_L_Warp;
        public int C_Warp;
        public int Impulse;
        public int Apr;
        public int Battery;
        public int Bridge;
        public int Security;
        public int Lab;
        public int Transporters;
        public int Tractors;
        public int MechTractors;
        public int SpecialSensors;
        public int Sensors;
        public int Scanners;
        public int ExplosionStrength;
        public int Acceleration;
        public int DamageControl;
        public int ExtraDamage;
        public int ShipCost;
        public string RefitBaseClass;
        public string Geometry;
        public string UI;
        public string FullName;
        public string Refits;
        public int Balance;
    }

    public class FighterData
    {
        public Races Race;
        public string HullType;
        public int Speed;
        public int Num1;
        public string Weapon1;
        public string Arc1;
        public int Shots1;
        public int Num2;
        public string Weapon2;
        public string Arc2;
        public int Shots2;
        public int Num3;
        public string Weapon3;
        public string Arc3;
        public int Shots3;
        public int Num4;
        public string Weapon4;
        public string Arc4;
        public int Shots4;
        public int Num5;
        public string Weapon5;
        public string Arc5;
        public int Shots5;
        public int Damage;
        public int ADD_6;
        public int GroundAttackBonus;
        public int ECM;
        public int ECCM;
        public int BPV;
        public int CarrierSizeClass;
        public int FirstYearAvailable;
        public int LastYearAvailable;
        public int Size;
        public string UI;
        public string Geometry;
        public string Name;
    }
}
