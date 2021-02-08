using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace SfcOpServer
{
    public partial class GameServer
    {
        private const int referenceYear = 2263;

        private const string defaultIrcChannel = "General";
        private const int defaultIrcPort = 6667;

        private const double smallTick = 200.0; // ms

        private const int cpuMovementInterval = 10; // s
        private const int cpuMovementMinRest = 120; // s
        private const int cpuMovementMaxRest = 300; // s

        private const double cpuPressureMultiplier = 0.01;
        private const double humanPressureMultiplier = 0.001;

        private const int draftCooldown = 60; // s
        private const int draftRest = 5; // s

        private const string savegameExtension = ".bin";

        // general

        private int _locked;

        private XoShiro256 _rand;

        private int[] _arrayInt1;
        private int[] _arrayInt2;

        private Dictionary<string, uint> _dictStringUInt;

        private List<int> _listInt; // HexMap id

        private Queue<int> _queueInt;

        private SortedDictionary<long, int> _sortLongInt;
        private SortedDictionary<long, BidItem> _sortLongBidItem;

        // clock

        private Stopwatch _clock;

        private double _ts;  // small tick
        private double _t1;  // 1 second
        private double _t60; // 60 seconds
        private double _tt;  // turn tick

        private long _smallTicks;
        private long _seconds;

        // stack

        private byte[] _buffer;
        private int _position;

        // server status

        private string _administrator;

        private string _hostId;
        private string _hostName;

        private string _gameType;
        private int _numPlayers;
        private int _maxNumLoggedOnPlayers;
        private int _numLoggedOnPlayers;

        private int _difficultyLevel;
        private int _startingEra;

        // server files

        private Dictionary<string, uint> _serverFiles;

        // data counter

        private int _dataCounter;

        // characters

        private Queue<int> _logins;
        private Queue<int> _logouts;

        private long _lastLogin;

        private Dictionary<string, int> _wonLogons;
        private SortedDictionary<string, int> _characterNames;
        private Dictionary<int, Character> _characters;

        private Ranks _rank;
        private int _prestige;

        private readonly int[][] _directions = new int[][]
        {
            new int[]{ 0, -20, -19, -18, -1, 1, 19 },
            new int[]{ 0, -19, -1, 1, 18, 19, 20 }
        };

        private Dictionary<int, int> _cpuMovements; // character id, movement ticks
        private Dictionary<int, int> _humanMovements; // character id, movement ticks

        // map

        private string _mapName;

        private int _mapWidth;
        private int _mapHeight;
        private MapHex[] _map;

        private readonly int[] _classTypeIcons = new int[]
        {
            -1, // kClassShuttle
            -1, // kClassPseudoFighter

            2,  // kClassFreighter
            3,  // kClassFrigate
            14, // kClassDestroyer
            14, // kClassWarDestroyer
            4,  // kClassLightCruiser
            7,  // kClassHeavyCruiser
            7,  // kClassNewHeavyCruiser
            15, // kClassHeavyBattlecruiser
            10, // kClassCarrier
            11, // kClassDreadnought
            11, // kClassBattleship
                
            -1, // kClassListeningPost
            -1, // kClassBaseStation
            -1, // kClassBattleStation
            -1, // kClassStarBase
                
            1,  // kClassMonster
                
            -1, // kClassPlanets
            -1, // kClassSpecial
        };

        private Location[] _homeLocations;

        // economy

        private double _expensesMultiplier;
        private double _maintenanceMultiplier;
        private double _productionMultiplier;

        private double[] _curBudget;
        private double[] _curExpenses;
        private double[] _curMaintenance;
        private double[] _curProduction;

        private int[] _curPopulation;
        private int[] _curSize;

        private List<double>[] _logBudget;
        private List<double>[] _logExpenses;
        private List<double>[] _logMaintenance;
        private List<double>[] _logProduction;

        private List<int>[] _logPopulation;
        private List<int>[] _logSize;

        // stardate

        private int _turn;
        private int _turnsPerYear;
        private int _millisecondsPerTurn;

        private int _earlyYears;
        private int _middleYears;
        private int _lateYears;
        private int _advancedYears;

        private int _mediumMissileSpeedDate; // in which turn they are available
        private int _fastMissileSpeedDate; // in which turn they are available

        private int CurrentYear => _turn / _turnsPerYear;

        // specs

        private Dictionary<string, ShipData> _shiplist;
        private Dictionary<string, FighterData> _ftrlist;

        private byte[][] _supplyFtrCache;

        private readonly string[] _classTypes = new string[]
        {
            "SHUTTLE", "PF",
            "FREIGHTER", "FRIGATE", "DESTROYER", "WAR_DESTROYER", "LIGHT_CRUISER", "HEAVY_CRUISER", "NEW_HEAVY_CRUISER", "HEAVY_BATTLECRUISER", "CARRIER", "DREADNOUGHT", "BATTLESHIP",
            "LISTENING_POST", "BASE_STATION", "BATTLE_STATION", "STARBASE",
            "MONSTER",
            "PLANET",
            "SPECIAL"
        };
        private readonly string[] _weaponTypes = new string[] {
            "Phot", "PhoF", "PhoH",
            "Dis1", "Dis2", "Dis3", "Dis4", "DisF", "DisH",
            "Fus", "FusF",
            "HB",
            "Hellf",
            "PlaR", "PlaS", "PlaG", "PlaF", "PlaI", "PlaD", "PlaX", "PlaE", "ESGL",
            "MLR",
            "PhA", "PhB",
            "TRBL", "TRBH",
            "DroA", "DroB", "DroC", "DroD", "DroE", "DroF", "DroG", "DroH", "DroI", "DroVI", "DroM",
            "Opt", "OptW",
            "Ph1", "Ph2", "Ph3", "PhG", "PhG2", "Ph4", "PhX",
            "ADD6", "ADD12", "ADD30",

            "PPD", "ESG"
        };

        private readonly Dictionary<WeaponTypes, int> _droneCapacities = new Dictionary<WeaponTypes, int>
        {
            { WeaponTypes.DroA,  4 },
            { WeaponTypes.DroB,  6 },
            { WeaponTypes.DroC,  4 },
            { WeaponTypes.DroD,  4 },
            { WeaponTypes.DroE,  8 },
            { WeaponTypes.DroF,  4 },
            { WeaponTypes.DroG,  4 },
            { WeaponTypes.DroH,  4 },

            { WeaponTypes.DroI,  4 }, // 1?  ( default 2x; Arc2 = 3x, Arc3 = 4x
            { WeaponTypes.DroVI, 4 }, // 1?

            { WeaponTypes.DroM,  4 },
        };

        private int[] _missileSizes;

        private readonly int[] _officerDefaults = new int[]
        {
            0x00,
            0x00, // kRookie
            0x05, // kJunior
            0x14, // kSenior
            0x32, // kVeteran
            0xc8, // kLegendary
        };

        private readonly string[] _races = new string[]
        {
            "Federation", "Klingon", "Romulan", "Lyran", "Hydran", "Gorn", "ISC", "Mirak",
            "OrionOrion", "OrionKorgath", "OrionPrime", "OrionTigerHeart", "OrionBeastRaiders", "OrionSyndicate", "OrionWyldeFire", "OrionCamboro",
            "Orion", "Monster",
            "Tholian", "LDR", "WYN", "Jindarian", "Andro", "Neutral", "Mirror"
        };
        private readonly string[] _raceAbbreviations = new string[]
        {
            "F", "K", "R", "L", "H", "G", "I", "Z",
            "X", "Y", "P", "T", "B", "S", "W", "C",
            "O", "M",
            null, null, null, null, null, "", null
        };

        private double _sparePartsMultiplier;

        private int _initialPopulation;
        private Alliances[] _alliances;

        private readonly int[] _planetTypes = new int[]
        {
            2, // 0 - Moon
            2, // 1 - Earth
            2, // 2 - Ringed Earth
            1, // 3 - Mars
            0, // 4 - Jupiter
            1, // 5 - Black Planet
            0, // 6 - Saturn
            2, // 7 - Night
            0, // 8 - Planet Fire
            1, // 9 - Ice
            0, // 10 - Gas Giant
            1, // 11 - Annwn
            1, // 12 - Forbidden Planet
            0, // 13 - Mostly Harmless
            1, // 14 - Niflheim
            2, // 15 - Urzuli
            2, // 16 - Water
            0, // 17 - Saturn II
        };

        private List<ShipData>[] _homeWorlds;
        private List<ShipData>[] _coreWorlds;
        private List<ShipData>[] _colonies;
        private List<ShipData>[] _asteroidBases;

        private List<ShipData>[] _starbases;
        private List<ShipData>[] _battleStations;
        private List<ShipData>[] _baseStations;
        private List<ShipData>[] _weaponPlatforms;
        private List<ShipData>[] _listeningPosts;

        // ships

        private Dictionary<int, Ship> _ships;

        private ClassTypes _minClassType;
        private ClassTypes _maxClassType;
        private int _minBPV;
        private int _maxBPV;
        private SpecialRoles _invalidRoles;
        private SpecialRoles _validRoles;

        private OfficerRanks _cpuOfficerRank;
        private double _cpuPowerBoost;

        private double[] _costClassType; // it is multiplied by the ship's BPV

        private double[] _costRepair;
        private double _costTradeIn;

        private double _costUnknown;
        private double _costMissiles;
        private double _costFighters;
        private double _costShuttles;
        private double _costMarines;
        private double _costMines;
        private double[] _costSpareParts;

        private double _cpuAutomaticRepairMultiplier;
        private double _humanAutomaticRepairMultiplier;

        private double _cpuAutomaticResupplyMultiplier;
        private double _humanAutomaticResupplyMultiplier;

        // shipyard

        private Dictionary<int, BidItem>[] _bidItems;
        private Dictionary<string, int> _bidReplacements; // shipClassName, count

        private int[] _turnsToClose;

        // chat

        private string[] _channels;
        private string _serverNick;

        // drafts

        private readonly string[] _missionNames = new string[]
        {
            "The Cage",
            "Where No Man Has Gone Before",
            "The Corbomite Maneuver",
            "Mudd's Women",
            "The Enemy Within",
            "The Man Trap",
            "The Naked Time",
            "Charlie X",
            "Balance of Terror",
            "What Are Little Girls Made Of?",
            "Dagger of the Mind",
            "Miri",
            "The Conscience of the King",
            "The Galileo Seven",
            "Court Martial",
            "The Menagerie, Parts I and II",
            "Shore Leave",
            "The Squire of Gothos",
            "Arena",
            "The Alternative Factor",
            "Tomorrow Is Yesterday",
            "The Return of the Archons",
            "A Taste of Armageddon",
            "Space Seed",
            "This Side of Paradise",
            "The Devil in the Dark",
            "Errand of Mercy",
            "The City on the Edge of Forever",
            "Operation: Annihilate!",
            "Catspaw",
            "Metamorphosis",
            "Friday's Child",
            "Who Mourns for Adonais?",
            "Amok Time",
            "The Doomsday Machine",
            "Wolf in the Fold",
            "The Changeling",
            "The Apple",
            "Mirror, Mirror",
            "The Deadly Years",
            "I, Mudd",
            "The Trouble with Tribbles",
            "Bread and Circuses",
            "Journey to Babel",
            "A Private Little War",
            "The Gamesters of Triskelion",
            "Obsession",
            "The Immunity Syndrome",
            "A Piece of the Action",
            "By Any Other Name",
            "Return to Tomorrow",
            "Patterns of Force",
            "The Ultimate Computer",
            "The Omega Glory",
            "Assignment: Earth",
            "Spectre of the Gun",
            "Elaan of Troyius",
            "The Paradise Syndrome",
            "The Enterprise Incident",
            "And the Children Shall Lead",
            "Spock's Brain",
            "Is There in Truth No Beauty?",
            "The Empath",
            "The Tholian Web",
            "For the World Is Hollow and I've Touched the Sky",
            "Day of the Dove",
            "Plato's Stepchildren",
            "Wink of an Eye",
            "That Which Survives",
            "Let That Be Your Last Battlefield",
            "Whom Gods Destroy",
            "The Mark of Gideon",
            "The Lights of Zetar",
            "The Cloud Minders",
            "The Way to Eden",
            "Requiem for Methuselah",
            "The Savage Curtain",
            "All Our Yesterdays",
            "Turnabout Intruder",
            "Encounter at Farpoint",
            "The Naked Now",
            "Code of Honor",
            "The Last Outpost",
            "Where No One Has Gone Before",
            "Lonely Among Us",
            "Justice",
            "The Battle",
            "Hide and Q",
            "Haven",
            "The Big Goodbye",
            "Datalore",
            "Angel One",
            "11001001",
            "Too Short a Season",
            "When the Bough Breaks",
            "Home Soil",
            "Coming of Age",
            "Heart of Glory",
            "The Arsenal of Freedom",
            "Symbiosis",
            "Skin of Evil",
            "We'll Always Have Paris",
            "Conspiracy",
            "The Neutral Zone",
            "The Child",
            "Where Silence Has Lease",
            "Elementary, Dear Data",
            "The Outrageous Okona",
            "Loud as a Whisper",
            "The Schizoid Man",
            "Unnatural Selection",
            "A Matter of Honor",
            "The Measure of a Man",
            "The Dauphin",
            "Contagion",
            "The Royale",
            "Time Squared",
            "The Icarus Factor",
            "Pen Pals",
            "Q Who",
            "Samaritan Snare",
            "Up the Long Ladder",
            "Manhunt",
            "The Emissary",
            "Peak Performance",
            "Shades of Gray",
            "Evolution",
            "The Ensigns of Command",
            "The Survivors",
            "Who Watches the Watchers",
            "The Bonding",
            "Booby Trap",
            "The Enemy",
            "The Price",
            "The Vengeance Factor",
            "The Defector",
            "The Hunted",
            "The High Ground",
            "Deja Q",
            "A Matter of Perspective",
            "Yesterday's Enterprise",
            "The Offspring",
            "Sins of the Father",
            "Allegiance",
            "Captain's Holiday",
            "Tin Man",
            "Hollow Pursuits",
            "The Most Toys",
            "Sarek",
            "Menage a Troi",
            "Transfigurations",
            "The Best of Both Worlds",
            "Family",
            "Brothers",
            "Suddenly Human",
            "Remember Me",
            "Legacy",
            "Reunion",
            "Future Imperfect",
            "Final Mission",
            "The Loss",
            "Data's Day",
            "The Wounded",
            "Devil's Due",
            "Clues",
            "First Contact",
            "Galaxy's Child",
            "Night Terrors",
            "Identity Crisis",
            "The Nth Degree",
            "Qpid",
            "The Drumhead",
            "Half a Life",
            "The Host",
            "The Mind's Eye",
            "In Theory",
            "Redemption",
            "Darmok",
            "Ensign Ro",
            "Silicon Avatar",
            "Disaster",
            "The Game",
            "Unification",
            "A Matter of Time",
            "New Ground",
            "Hero Worship",
            "Violations",
            "The Masterpiece Society",
            "Conundrum",
            "Power Play",
            "Ethics",
            "The Outcast",
            "Cause and Effect",
            "The First Duty",
            "Cost of Living",
            "The Perfect Mate",
            "Imaginary Friend",
            "I, Borg",
            "The Next Phase",
            "The Inner Light",
            "Time's Arrow",
            "Realm of Fear",
            "Man of the People",
            "Relics",
            "Schisms",
            "True Q",
            "Rascals",
            "A Fistful of Datas",
            "The Quality of Life",
            "Chain of Command",
            "Ship in a Bottle",
            "Aquiel",
            "Face of the Enemy",
            "Tapestry",
            "Birthright",
            "Starship Mine",
            "Lessons",
            "The Chase",
            "Frame of Mind",
            "Suspicions",
            "Rightful Heir",
            "Second Chances",
            "Timescape",
            "Descent",
            "Liaisons",
            "Interface",
            "Gambit",
            "Phantasms",
            "Dark Page",
            "Attached",
            "Force of Nature",
            "Inheritance",
            "Parallels",
            "The Pegasus",
            "Homeward",
            "Sub Rosa",
            "Lower Decks",
            "Thine Own Self",
            "Masks",
            "Eye of the Beholder",
            "Genesis",
            "Journey's End",
            "Firstborn",
            "Bloodlines",
            "Emergence",
            "Preemptive Strike",
            "All Good Things..."
        };

        private List<int> _availableMissions;

        private Dictionary<int, Draft> _drafts; // hex index, countdown (s)

        // maintenance

        private string _lastSavegame;

        // initialization

        private void InitializeData()
        {
            Contract.Assert(smallTick < 1000.0);

            // initialize gf

            GameFile gf = new GameFile();
            string filename = _root + "SfcOpServer.gf";

            gf.Load(filename);

            // general

            _locked = 0;

            _rand = new XoShiro256();

            _arrayInt1 = new int[32];
            _arrayInt2 = new int[32];

            _dictStringUInt = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

            _listInt = new List<int>();

            _queueInt = new Queue<int>();

            _sortLongInt = new SortedDictionary<long, int>();
            _sortLongBidItem = new SortedDictionary<long, BidItem>();

            // clock

            _clock = new Stopwatch();

            _smallTicks = 0;
            _seconds = 0;

            // stack

            _buffer = new byte[Client27000.MaximumBufferSize];

            Clear();

            // server status

            _administrator = null;

            _hostId = null;
            _hostName = gf.GetValue("", "Name", "New_Server");

            _gameType = gf.GetValue("", "Description", "This is the new server campaign.");
            _numPlayers = 0;
            _maxNumLoggedOnPlayers = 0;
            _numLoggedOnPlayers = 0;

            _difficultyLevel = gf.GetValue("", "DifficultyLevel", 2); // 0 - 2
            _startingEra = gf.GetValue("", "Era", 0); // 0 - 3

            // server files

            _serverFiles = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

            // data counter

            _dataCounter = 0;

            // characters

            _logins = new Queue<int>();
            _logouts = new Queue<int>();

            _lastLogin = 0;

            _wonLogons = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _characterNames = new SortedDictionary<string, int>();
            _characters = new Dictionary<int, Character>();

            _rank = Ranks.Ensign;
            _prestige = gf.GetValue("Character", "Prestige", 200);

            _cpuMovements = new Dictionary<int, int>();
            _humanMovements = new Dictionary<int, int>();

            // map

            _mapName = gf.GetValue("", "MapName", "StandardMap.mvm");

            _homeLocations = new Location[(int)Races.kNumberOfRaces];

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _homeLocations[i] = new Location(-1, -1, 0);

            // economy

            _expensesMultiplier = gf.GetValue("Economy", "ExpensesMultiplier", 1.0f);
            _maintenanceMultiplier = gf.GetValue("Economy", "MaintenanceMultiplier", 0.05f); // 5%
            _productionMultiplier = gf.GetValue("Economy", "ProductionMultiplier", 2.0f); // map dependent

            _curBudget = new double[(int)Races.kNumberOfRaces];
            _curExpenses = new double[(int)Races.kNumberOfRaces];
            _curMaintenance = new double[(int)Races.kNumberOfRaces];
            _curProduction = new double[(int)Races.kNumberOfRaces];

            _curPopulation = new int[(int)Races.kNumberOfRaces];
            _curSize = new int[(int)Races.kNumberOfRaces];

            _logBudget = new List<double>[(int)Races.kNumberOfRaces];
            _logExpenses = new List<double>[(int)Races.kNumberOfRaces];
            _logMaintenance = new List<double>[(int)Races.kNumberOfRaces];
            _logProduction = new List<double>[(int)Races.kNumberOfRaces];

            _logPopulation = new List<int>[(int)Races.kNumberOfRaces];
            _logSize = new List<int>[(int)Races.kNumberOfRaces];

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                _logBudget[i] = new List<double>();
                _logExpenses[i] = new List<double>();
                _logMaintenance[i] = new List<double>();
                _logProduction[i] = new List<double>();

                _logPopulation[i] = new List<int>();
                _logSize[i] = new List<int>();
            }

            // stardate

            _turn = 0;
            _turnsPerYear = gf.GetValue("Clock", "TurnsPerYear", 52); // 1 turn per week
            _millisecondsPerTurn = gf.GetValue("Clock", "MilliSecondsPerTurn", 600_000); // 10 minuts

            _earlyYears = gf.GetValue("Clock/StartingDate", "EarlyYears", 2263) - referenceYear; // +0
            _middleYears = gf.GetValue("Clock/StartingDate", "MiddleYears", 2273) - referenceYear; // +10
            _lateYears = gf.GetValue("Clock/StartingDate", "LateYears", 2283) - referenceYear; // +20
            _advancedYears = gf.GetValue("Clock/StartingDate", "AdvancedYears", 2303) - referenceYear; // +50

            Contract.Assert(_earlyYears < _middleYears && _middleYears < _lateYears && _lateYears < _advancedYears);

            switch (_startingEra)
            {
                case 0:
                    break;
                case 1:
                    _turn += _middleYears * _turnsPerYear; break;
                case 2:
                    _turn += _lateYears * _turnsPerYear; break;
                case 3:
                    _turn += _advancedYears * _turnsPerYear; break;
            }

            _mediumMissileSpeedDate = (gf.GetValue("Clock/MissileSpeedDate", "Medium", 2267) - referenceYear) * _turnsPerYear; // need to match the client (+4)
            _fastMissileSpeedDate = (gf.GetValue("Clock/MissileSpeedDate", "Fast", 2280) - referenceYear) * _turnsPerYear; // need to match the client (+17)

            // specs

            _shiplist = new Dictionary<string, ShipData>(StringComparer.OrdinalIgnoreCase);
            _ftrlist = new Dictionary<string, FighterData>(StringComparer.OrdinalIgnoreCase);

            _supplyFtrCache = new byte[(int)Races.kNumberOfRaces][];

            _missileSizes = new int[]
            {
                1, // Type1
                1, // Type2 (old TypeIV)
                1, // Type3
                1,
                1,
                1,
                1,
            };

            _sparePartsMultiplier = 0.8; // used to be 5.0 in stock

            _initialPopulation = gf.GetValue("", "Population", 24);

            const Alliances theGood = Alliances.kFederation | Alliances.kHydran | Alliances.kGorn | Alliances.kMirak | Alliances.kOrionOrion | Alliances.kOrionBeastRaiders | Alliances.kOrionSyndicate | Alliances.kOrionCamboro;
            const Alliances theBad = Alliances.kKlingon | Alliances.kRomulan | Alliances.kLyran | Alliances.kISC | Alliances.kOrionKorgath | Alliances.kOrionPrime | Alliances.kOrionTigerHeart | Alliances.kOrionWyldeFire;

            Contract.Assert((theGood & theBad) == Alliances.None);

            const Alliances theNeutral = Alliances.All ^ theGood ^ theBad;

            _alliances = new Alliances[]
            {
                theGood,    // kFederation
                theBad,     // kKlingon
                theBad,     // kRomulan
                theBad,     // kLyran
                theGood,    // kHydran
                theGood,    // kGorn
                theBad,     // kISC
                theGood,    // kMirak

                theGood,    // kOrionOrion
                theBad,     // kOrionKorgath
                theBad,     // kOrionPrime
                theGood,    // kOrionTigerHeart
                theBad,     // kOrionBeastRaiders
                theGood,    // kOrionSyndicate
                theGood,    // kOrionWyldeFire
                theBad,     // kOrionCamboro

                theNeutral, // kOrion
                theNeutral, // kMonster

                theNeutral, // kTholian
                theNeutral, // kLDR
                theNeutral, // kWYN
                theNeutral, // kJindarian
                theNeutral, // kAndro

                theNeutral, // kNeutralRace

                theNeutral  // kMirror
            };

            Contract.Assert(_alliances.Length == (int)Races.kNumberOfRaces);

            // each cartel needs to be allied with its corresponding empire
            // so we can track their home locations later more easily

            for (int i = (int)Races.kFirstCartel; i <= (int)Races.kLastCartel; i++)
            {
                if ((1 << i & (int)_alliances[i - 8]) == 0)
                    throw new NotSupportedException();
            }

            _homeWorlds = new List<ShipData>[(int)Races.kNumberOfRaces];
            _coreWorlds = new List<ShipData>[(int)Races.kNumberOfRaces];
            _colonies = new List<ShipData>[(int)Races.kNumberOfRaces];
            _asteroidBases = new List<ShipData>[(int)Races.kNumberOfRaces];

            _starbases = new List<ShipData>[(int)Races.kNumberOfRaces];
            _battleStations = new List<ShipData>[(int)Races.kNumberOfRaces];
            _baseStations = new List<ShipData>[(int)Races.kNumberOfRaces];
            _weaponPlatforms = new List<ShipData>[(int)Races.kNumberOfRaces];
            _listeningPosts = new List<ShipData>[(int)Races.kNumberOfRaces];

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                _homeWorlds[i] = new List<ShipData>();
                _coreWorlds[i] = new List<ShipData>();
                _colonies[i] = new List<ShipData>();
                _asteroidBases[i] = new List<ShipData>();

                _starbases[i] = new List<ShipData>();
                _battleStations[i] = new List<ShipData>();
                _baseStations[i] = new List<ShipData>();
                _weaponPlatforms[i] = new List<ShipData>();
                _listeningPosts[i] = new List<ShipData>();
            }

            // ships

            _ships = new Dictionary<int, Ship>();

            _minClassType = ClassTypes.kClassFrigate + _startingEra;
            _maxClassType = ClassTypes.kClassBattleship;

            _minBPV = gf.GetValue("Character", "MinBPV", 80);
            _maxBPV = gf.GetValue("Character", "MaxBPV", 110);

            _invalidRoles = SpecialRoles.R | SpecialRoles.T;
            _validRoles = SpecialRoles.All ^ _invalidRoles;

            _cpuOfficerRank = OfficerRanks.kSenior + _difficultyLevel;
            _cpuPowerBoost = gf.GetValue("AI", "PowerBoost", 0.12f) * (_difficultyLevel + 1); // 12-36 %

            if (_cpuPowerBoost < 0.0 || _cpuPowerBoost > 1.0)
                throw new NotSupportedException();

            _costClassType = new double[]
            {
                gf.GetValue("Cost/Class", "SHUTTLE", 1.00f),
                gf.GetValue("Cost/Class", "PF", 1.00f),

                gf.GetValue("Cost/Class", "FREIGHTER", 1.00f),
                gf.GetValue("Cost/Class", "FRIGATE", 1.25f),
                gf.GetValue("Cost/Class", "DESTROYER", 1.50f),
                gf.GetValue("Cost/Class", "WAR_DESTROYER", 1.75f),
                gf.GetValue("Cost/Class", "LIGHT_CRUISER", 2.00f),
                gf.GetValue("Cost/Class", "HEAVY_CRUISER", 2.25f),
                gf.GetValue("Cost/Class", "NEW_HEAVY_CRUISER", 2.50f),
                gf.GetValue("Cost/Class", "HEAVY_BATTLECRUISER", 2.75f),
                gf.GetValue("Cost/Class", "CARRIER", 3.00f),
                gf.GetValue("Cost/Class", "DREADNOUGHT", 3.25f),
                gf.GetValue("Cost/Class", "BATTLESHIP", 3.50f),

                gf.GetValue("Cost/Class", "LISTENING_POST", 1.00f),
                gf.GetValue("Cost/Class", "BASE_STATION", 1.00f),
                gf.GetValue("Cost/Class", "BATTLE_STATION", 1.00f),
                gf.GetValue("Cost/Class", "STARBASE", 1.00f),

                gf.GetValue("Cost/Class", "MONSTER", 1.00f),

                gf.GetValue("Cost/Class", "PLANET", 1.00f),
                gf.GetValue("Cost/Class", "SPECIAL", 1.00f),
            };

            Contract.Assert(_costClassType.Length == (int)ClassTypes.kMaxClasses);

            // ... sets all the costs

            _costRepair = new double[]
            {
                gf.GetValue("Cost/Repair", "SHUTTLE", 3.0f),
                gf.GetValue("Cost/Repair", "PF", 3.0f),

                gf.GetValue("Cost/Repair", "FREIGHTER", 3.0f),
                gf.GetValue("Cost/Repair", "FRIGATE", 3.0f),
                gf.GetValue("Cost/Repair", "DESTROYER", 3.0f),
                gf.GetValue("Cost/Repair", "WAR_DESTROYER", 3.0f),
                gf.GetValue("Cost/Repair", "LIGHT_CRUISER", 3.0f),
                gf.GetValue("Cost/Repair", "HEAVY_CRUISER", 3.0f),
                gf.GetValue("Cost/Repair", "NEW_HEAVY_CRUISER", 3.0f),
                gf.GetValue("Cost/Repair", "HEAVY_BATTLECRUISER", 3.0f),
                gf.GetValue("Cost/Repair", "CARRIER", 3.0f),
                gf.GetValue("Cost/Repair", "DREADNOUGHT", 3.0f),
                gf.GetValue("Cost/Repair", "BATTLESHIP", 3.0f),

                gf.GetValue("Cost/Repair", "LISTENING_POST", 3.0f),
                gf.GetValue("Cost/Repair", "BASE_STATION", 3.0f),
                gf.GetValue("Cost/Repair", "BATTLE_STATION", 3.0f),
                gf.GetValue("Cost/Repair", "STARBASE", 3.0f),

                gf.GetValue("Cost/Repair", "MONSTER", 3.0f),

                gf.GetValue("Cost/Repair", "PLANET", 3.0f),
                gf.GetValue("Cost/Repair", "SPECIAL", 3.0f),
            };

            Contract.Assert(_costRepair.Length == (int)ClassTypes.kMaxClasses);

            _costTradeIn = gf.GetValue("Cost", "TradeIn", 3.0f);

            _costUnknown = 1.0;
            _costMissiles = gf.GetValue("Cost/Supply", "Missiles", 9.0f);
            _costFighters = gf.GetValue("Cost/Supply", "Fighters", 6.0f);
            _costShuttles = gf.GetValue("Cost/Supply", "Shuttles", 12.0f);
            _costMarines = gf.GetValue("Cost/Supply", "Marines", 15.0f);
            _costMines = gf.GetValue("Cost/Supply", "Mines", 18.0f);

            _costSpareParts = new double[]
            {
                gf.GetValue("Cost/SpareParts", "SHUTTLE", 30.0f),
                gf.GetValue("Cost/SpareParts", "PF", 30.0f),

                gf.GetValue("Cost/SpareParts", "FREIGHTER", 30.0f),
                gf.GetValue("Cost/SpareParts", "FRIGATE", 30.0f),
                gf.GetValue("Cost/SpareParts", "DESTROYER", 30.0f),
                gf.GetValue("Cost/SpareParts", "WAR_DESTROYER", 30.0f),
                gf.GetValue("Cost/SpareParts", "LIGHT_CRUISER", 30.0f),
                gf.GetValue("Cost/SpareParts", "HEAVY_CRUISER", 30.0f),
                gf.GetValue("Cost/SpareParts", "NEW_HEAVY_CRUISER", 30.0f),
                gf.GetValue("Cost/SpareParts", "HEAVY_BATTLECRUISER", 30.0f),
                gf.GetValue("Cost/SpareParts", "CARRIER", 30.0f),
                gf.GetValue("Cost/SpareParts", "DREADNOUGHT", 30.0f),
                gf.GetValue("Cost/SpareParts", "BATTLESHIP", 30.0f),

                gf.GetValue("Cost/SpareParts", "LISTENING_POST", 30.0f),
                gf.GetValue("Cost/SpareParts", "BASE_STATION", 30.0f),
                gf.GetValue("Cost/SpareParts", "BATTLE_STATION", 30.0f),
                gf.GetValue("Cost/SpareParts", "STARBASE", 30.0f),

                gf.GetValue("Cost/SpareParts", "MONSTER", 30.0f),

                gf.GetValue("Cost/SpareParts", "PLANET", 30.0f),
                gf.GetValue("Cost/SpareParts", "SPECIAL", 30.0f)
            };

            Contract.Assert(_costSpareParts.Length == (int)ClassTypes.kMaxClasses);

            // ... makes sure we loss at least 10% when trading ships (to avoid shipyard exploitation)

            if (_costTradeIn < 1.1)
                throw new NotSupportedException();

            for (int i = 0; i < (int)ClassTypes.kMaxClasses; i++)
            {
                // ... makes sure ship prices are not bellow 'production cost' (to avoid shipyard explotation)

                if (_costClassType[i] < 1.0)
                    throw new NotSupportedException();

                _costClassType[i] *= _costTradeIn;
            }

            _cpuAutomaticRepairMultiplier = gf.GetValue("Bonus", "CpuAutomaticRepair", 0.25f); // +25%
            _humanAutomaticRepairMultiplier = gf.GetValue("Bonus", "HumanAutomaticRepair", 0.10f); // +10%

            if (_cpuAutomaticRepairMultiplier < _humanAutomaticRepairMultiplier)
                throw new NotSupportedException();

            _cpuAutomaticResupplyMultiplier = gf.GetValue("Bonus", "CpuAutomaticResupply", 0.25f); // +25%
            _humanAutomaticResupplyMultiplier = gf.GetValue("Bonus", "HumanAutomaticResupply", 0.10f); // +25%

            if (_cpuAutomaticResupplyMultiplier < _humanAutomaticResupplyMultiplier)
                throw new NotSupportedException();

            // shipyard

            _bidItems = new Dictionary<int, BidItem>[(int)Races.kNumberOfRaces];

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _bidItems[i] = new Dictionary<int, BidItem>();

            _bidReplacements = new Dictionary<string, int>();

            _turnsToClose = new int[]
            {
                gf.GetValue("Shipyard/TurnsForConstruction", "SizeClass1", 32),
                gf.GetValue("Shipyard/TurnsForConstruction", "SizeClass2", 16),
                gf.GetValue("Shipyard/TurnsForConstruction", "SizeClass3", 8),
                gf.GetValue("Shipyard/TurnsForConstruction", "SizeClass4", 4),
                gf.GetValue("Shipyard/TurnsForConstruction", "SizeClass5", 2), // pseudo-fighters
                gf.GetValue("Shipyard/TurnsForConstruction", "SizeClass6", 1)  // shuttles, fighters
            };

            for (int i = 0; i < 6; i++)
            {
                if (_turnsToClose[i] < 1)
                    throw new NotSupportedException();
            }

            // chat

            _channels = new string[]
            {
                "@" + _hostName,
                "#SystemBroadcast", "#ServerBroadcast", "#General",
                "#Federation", "#Klingon", "#Romulan", "#Lyran", "#Hydran", "#Gorn", "#ISC", "#Mirak",
                "#Orion", "#Korgath", "#Prime", "#TigerHeart", "#BeastRaiders", "#Syndicate", "#Wyldefire", "#Camboro"
            };

            _serverNick = "A" + Id;

            // draft

#if DEBUG
            // checks if all mission names are unique

            Dictionary<string, object> d = new Dictionary<string, object>();

            for (int i = 1; i < _missionNames.Length; i++)
                d.Add(_missionNames[i], null);
#endif

            _availableMissions = new List<int>();

            _drafts = new Dictionary<int, Draft>();

            // maintenance

            _lastSavegame = null;

            // functions

            LoadValidatedClientFiles();

            LoadMap();
            LoadShiplist();

            ClassifyPlanetsAndBases();

            ResetAvailableMissions();

            // finalize gf

            gf.Save(filename, -1, -1);
        }

        // server status

        private void UpdateStatus()
        {
            StringBuilder s = new StringBuilder(1024);

            s.Append("\\gamename\\sfc2op\\gamever\\1.6\\location\\0\\serverver\\");
            s.Append(_clientVersion);
            s.Append("\\validclientver\\");
            s.Append(_clientVersion);
            s.Append("\\hostname\\");
            s.Append(_hostName);

            s.Append("\\hostport\\");
            s.Append(_localEP.Port);

            s.Append("\\mapname\\");
            s.Append(_mapName);
            s.Append(' ');
            s.Append(_mapName);
            s.Append(' ');
            s.Append(_mapName);

            s.Append("\\gametype\\");
            s.Append(_gameType);

            s.Append("\\maxnumplayers\\");
            s.Append(MaxNumPlayers);

            s.Append("\\numplayers\\");
            s.Append(_numPlayers);

            s.Append("\\maxnumloggedonplayers\\");
            s.Append(_maxNumLoggedOnPlayers);

            s.Append("\\numloggedonplayers\\");
            s.Append(_numLoggedOnPlayers);

            s.Append("\\gamemode\\Open\\racelist\\0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15\\password\\\\final\\\\queryid\\1.1");

            // checks if we need to resize the buffer

            int c = s.Length;

            if (Status.Length != c)
                Status = new byte[c];

            Encoding.UTF8.GetBytes(s.ToString(), 0, c, Status, 0);
        }

        // server files

        private void LoadValidatedClientFiles()
        {
            IEnumerable<string> e = Directory.EnumerateFiles(_root + "Assets/ValidatedClientFiles/", "*", SearchOption.TopDirectoryOnly);

            foreach (string f in e)
                _serverFiles.Add(Path.GetFileName(f), 0);
        }

        private bool TryValidateClientFiles(Dictionary<string, uint> clientFiles, out string badOrMissing)
        {
            Contract.Requires(_serverFiles != null && clientFiles != null);

            if (_serverFiles.Count > 0)
            {
                List<string> warnings = new List<string>();

                foreach (KeyValuePair<string, uint> p in _serverFiles)
                {
                    string filename = p.Key;
                    uint serverCRC = p.Value;

                    if (clientFiles.ContainsKey(filename))
                    {
                        uint clientCRC = clientFiles[filename];

                        if (_serverFiles[filename] == 0)
                        {
                            _serverFiles[filename] = clientCRC;

                            Console.WriteLine("SUCCESS: A new file was added to the security check {" + filename + ", " + clientCRC.ToString("X8", CultureInfo.InvariantCulture) + "}");
                        }
                        else if (clientCRC != serverCRC)
                            warnings.Add(filename);
                    }
                    else
                    {
                        warnings.Add(filename);
                    }
                }

                int c = warnings.Count;

                if (c > 0)
                {
                    StringBuilder b = new StringBuilder(1024);

                    b.Append("Warning! The following files are either missing or incompatible:");
                    b.Append(warnings[0]);

                    for (int i = 1; i < c; i++)
                    {
                        b.Append(", ");
                        b.Append(warnings[i]);
                    }

                    b.Append('.');

                    badOrMissing = b.ToString();

                    return false;
                }
            }

            badOrMissing = null;

            return true;
        }

        // data counter

        private int GetNextDataId()
        {
            return Interlocked.Increment(ref _dataCounter);
        }

        // characters

        private void ProcessLoginsAndLogouts()
        {
            if (_seconds - _lastLogin >= 6)
            {
                if (_logins.TryDequeue(out int clientId))
                {
                    _lastLogin = _seconds;

                    // continues the process

                    Client27000 client = _clients[clientId];

                    M_FullCharacter(client, client.Id, client.Relay[(int)Data.CharacterLogOnRelayNameC], 0x02, 0x00);

                    // informs the other players

                    foreach (KeyValuePair<int, Client27000> p in _clients)
                    {
                        Client27000 target = p.Value;

                        if (target.Id != clientId && target.IconsRequest == 0)
                            target.IconsRequest = 1;
                    }
                }
            }

            while (_logouts.TryDequeue(out int clientId))
            {
                Contract.Assert(_clients.ContainsKey(clientId));

                LogoutClient(clientId);
            }
        }

        private void AddOrUpdateCharacter(Client27000 client, string ipAddress, string wonLogon)
        {
            Character character;

            if (_wonLogons.TryGetValue(wonLogon, out int id))
            {
                character = _characters[id];

                Contract.Assert(character.State == Character.States.IsHuman && character.Client == null);

                // we need to check the character's fleet at this point
                // because he may have ALT+F4 during a mission
                // and the server destroyed all his ships

                int c = 0;

                for (int i = 0; i < Character.MaxFleetSize; i++)
                {
                    if (_ships.ContainsKey(character.Ships[i]))
                    {
                        if (c != i)
                            character.Ships[c] = character.Ships[i];

                        c++;
                    }
                }

                Array.Clear(character.Ships, c, Character.MaxFleetSize - c);

                if (c == 0)
                {
                    character.ShipCount = 0;

                    CreateTemporaryShip(character);
                }
                else if (character.ShipCount != c)
                {
                    RefreshCharacter(character);
                }
            }
            else
            {
                id = GetNextDataId();

                _wonLogons.Add(wonLogon, id);

                character = new Character()
                {
                    // data

                    IPAddress = ipAddress,
                    WONLogon = wonLogon,
                    Id = id,

                    CharacterRank = _rank,

                    CharacterCurrentPrestige = _prestige,
                    CharacterLifetimePrestige = _prestige * 10,
                };

                _characters.Add(id, character);

                // updates the stats

                _numPlayers++;
            }

            // updates the character

            character.State = Character.States.IsHumanBusyConnecting;

            Contract.Assert(character.Client == null);

            character.Client = client;

            // updates the client

            Contract.Assert(client.Character == null);

            client.Character = character;

            // updates the stats

            _numLoggedOnPlayers++;

            if (_maxNumLoggedOnPlayers < _numLoggedOnPlayers)
                _maxNumLoggedOnPlayers = _numLoggedOnPlayers;
        }

        private void CreateTemporaryShip(Character character)
        {
            Contract.Assert(character.ShipCount == 0 && (character.State & Character.States.IsHuman) == Character.States.IsHuman);

            CreateShip(character.CharacterRace, _minClassType, _minClassType, 0, 32767, _validRoles, _invalidRoles, CurrentYear, out Ship ship);

            RefreshShip(ship);

            UpdateCharacter(character, ship);
            RefreshCharacter(character);

            // the new ship is free for the player, but not for the empire
            // so we account it in the overall expenses

            _curExpenses[(int)character.CharacterRace] += GetShipCost(ship);
        }

        private bool TryUpdateCharacter(Character character, string characterName, Races race)
        {
            Contract.Assert(character.State == Character.States.IsHumanBusyConnecting);

            if (_characterNames.TryAdd(characterName, character.Id))
            {
                // gets the initial location

                Location location = _homeLocations[(int)race];

                Contract.Assert(location.X != 1 && location.Y != -1);

                // updates the character

                character.CharacterName = characterName;
                character.CharacterRace = race;
                character.CharacterPoliticalControl = race;

                character.CharacterLocationX = location.X;
                character.CharacterLocationY = location.Y;

                character.HomeWorldLocationX = location.X;
                character.HomeWorldLocationY = location.Y;

                // ... finalize

                CreateShip(race, _minClassType, _maxClassType, _minBPV, _maxBPV, _validRoles, _invalidRoles, CurrentYear, out Ship ship);

                RefreshShip(ship);

                UpdateCharacter(character, ship);
                RefreshCharacter(character);

                AddToHexPopulation(character);
                UpdatePoliticalControl(character);

                return true;
            }

            return false;
        }

        private static bool MovementValid(int x1, int y1, int x2, int y2)
        {
            int x = Math.Abs(x1 - x2);
            int y = Math.Abs(y1 - y2);

            return (x + y == 1) | (((x | y) == 1) & (((x1 & 1) == 0 & y1 > y2) | ((x1 & 1) != 0 & y1 < y2)));
        }

        private bool UpdateCharacter(Character character, int prestigeIncrement)
        {
            if (prestigeIncrement != 0)
            {
                int prestige = character.CharacterCurrentPrestige + prestigeIncrement;

                if (prestige < 0)
                    character.CharacterCurrentPrestige = 0;
                else
                {
                    character.CharacterCurrentPrestige = prestige;

                    if (character.CharacterLifetimePrestige < prestige)
                        character.CharacterLifetimePrestige = prestige;
                }

                // every time a player spends his credit

                if (prestigeIncrement < 0)
                    _curExpenses[(int)character.CharacterRace] -= prestigeIncrement;

                return true;
            }

            return false;
        }

        private static bool UpdateCharacter(Character character, Medals medals)
        {
            if (medals != 0 && (character.Awards & medals) != medals)
            {
                character.Awards |= medals;

                return true;
            }

            return false;
        }

        private void UpdateCharacter(Character character, Ship shipAddition)
        {
            //Contract.Assert(shipAddition.OwnerID == 0);

            shipAddition.OwnerID = character.Id;

            if ((character.State & Character.States.IsCpu) == Character.States.IsCpu)
                AutomaticResupply(shipAddition, 1.0);

            character.Ships[character.ShipCount] = shipAddition.Id;
            character.ShipCount++;
        }

        private void UpdatePoliticalControl(Character character)
        {
            MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

            if (character.CharacterRace <= Races.kLastEmpire)
            {
                if ((_alliances[(int)character.CharacterRace] & (Alliances)(1 << (int)hex.EmpireControl)) != 0)
                    character.CharacterPoliticalControl = hex.EmpireControl;
                else
                    character.CharacterPoliticalControl = character.CharacterRace;
            }
            else if (character.CharacterRace <= Races.kLastCartel)
            {
                if ((_alliances[(int)character.CharacterRace] & (Alliances)(1 << (int)hex.CartelControl)) != 0)
                    character.CharacterPoliticalControl = hex.CartelControl;
                else
                    character.CharacterPoliticalControl = character.CharacterRace;
            }
        }

        private void RefreshCharacter(Character character)
        {
            Contract.Assert(character.ShipCount > 0);

            Rent(2048, out byte[] b, out MemoryStream m, out BinaryWriter w);

            // updates the data

            int bestId = 0;
            int bestBPV = 0;

            int shipsBPV = 0;

            for (int i = 0; i < character.ShipCount; i++)
            {
                Ship ship = _ships[character.Ships[i]];

                w.Write(ship.ShipCache, 0, ship.ShipCache.Length);

                if (_classTypeIcons[(int)ship.ClassType] != -1 && bestBPV < ship.BPV)
                {
                    bestId = ship.Id;
                    bestBPV = ship.BPV;
                }

                shipsBPV += ship.BPV;
            }

            m.SetLength(m.Position);

            character.ShipCache = m.ToArray();

            // updates the helpers

            character.ShipsBestId = bestId;
            character.ShipsBestBPV = bestBPV;

            character.ShipsBPV = shipsBPV;

            // closes everything

            Return(b, m, w);
        }

        public void RemoveClient(int clientId)
        {
            _logouts.Enqueue(clientId);
        }

        private void LogoutClient(int clientId)
        {
            if (!_clients.TryRemove(clientId, out Client27000 client))
                throw new NotSupportedException();

            ResetClient(client);

            Character character = client.Character;

            if (character == null)
                return;

            client.Character = null;

            if (character.CharacterName.Length == 0)
            {
                Contract.Assert(character.State == Character.States.IsHumanBusyConnecting);

                _wonLogons.Remove(character.WONLogon);
                _characters.Remove(character.Id);

                _numPlayers--;

                Contract.Assert(_numPlayers >= 0);
            }
            else
            {
                Contract.Assert((character.State & Character.States.IsHumanOnline) == Character.States.IsHumanOnline);

                // the political control defines the access to the shipyard and supplies in the client interface
                // (by default empires are restricted to their planets and bases, but cartels can have access to any planet or base in the map)

                character.CharacterPoliticalControl = character.CharacterRace;

                character.MoveDestinationX = -1;
                character.MoveDestinationY = -1;

                character.Client = null;

                _humanMovements.Remove(character.Id);

                MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

                RemoveFromHexPopulation(hex, character);

                // deletes all the ships of the character if he left the server during a draft process

                if (character.Mission != 0 && character.Mission == (hex.Mission & MissionFilter) && _drafts.TryGetValue(hex.X + hex.Y * _mapWidth, out Draft draft))
                {
                    draft.LeftEarly.TryAdd(character.Id, null);

                    for (int i = 0; i < character.ShipCount; i++)
                        _ships.Remove(character.Ships[i]);

                    DeleteFleet(character);
                }

                TryLeaveMission(character, hex);

                BroadcastIcons();
            }

            // updates the character

            character.State = Character.States.IsHuman;

            Debug.WriteLine(character.CharacterName + " left the game server");

            // updates the stats

            _numLoggedOnPlayers--;

            Contract.Assert(_numLoggedOnPlayers >= 0);
        }

        private static void ResetClient(Client27000 client)
        {
            client.HexRequest = -1;
            client.IconsRequest = 0;

            while (client.Messages.TryDequeue(out ClientMessage msg))
                Return(msg.Buffer);
        }

        private static void DeleteFleet(Character character)
        {
            character.ShipCount = 0;
            character.ShipCache = null;

            Array.Clear(character.Ships, 0, Character.MaxFleetSize);

            character.ShipsBestId = 0;
            character.ShipsBestBPV = 0;

            character.ShipsBPV = 0;
        }

        // ... IA

        private void CreateCharacter(Races race, int x, int y, ShipData shipData)
        {
            CreateShip(shipData, out Ship ship);

            CreateCharacter(race, x, y, ship, out _);
        }

        private void CreateCharacter(Races race, int x, int y, Ship ship, out Character character)
        {
            int id = GetNextDataId();

            character = new Character()
            {
                Id = id,
                CharacterName = _raceAbbreviations[(int)race] + (id & 0xffff).ToString("X4", CultureInfo.InvariantCulture) + "C",
                CharacterRace = race,
                CharacterPoliticalControl = race,
                CharacterRank = _rank,

                CharacterCurrentPrestige = _prestige,
                CharacterLifetimePrestige = _prestige * 10,

                CharacterLocationX = x,
                CharacterLocationY = y,

                State = Character.States.IsCpuOnline
            };

            _characters.Add(id, character);

            RefreshShip(ship);

            UpdateCharacter(character, ship);
            RefreshCharacter(character);

            AddToHexPopulation(character);

            AddOrUpdateCpuMovement(character);
        }

        private void AddOrUpdateCpuMovement(Character character)
        {
            int sleepDelay = _rand.NextInt32(cpuMovementMinRest, cpuMovementMaxRest); // 2-5 minutes

            Contract.Assert(sleepDelay > 0);

            if (_cpuMovements.ContainsKey(character.Id))
                _cpuMovements[character.Id] = sleepDelay;
            else
                _cpuMovements.Add(character.Id, sleepDelay);
        }

        private void AddHumanMovement(Character character, MapHex origin, MapHex destination)
        {
            const double originSpeedMultiplier = 0.25;
            const double destinationSpeedMultiplier = 1.0 - originSpeedMultiplier;
            const double ticksPerSecond = 1000.0 / smallTick;

            int delay = (int)Math.Round((origin.CurrentSpeedPoints * originSpeedMultiplier + destination.CurrentSpeedPoints * destinationSpeedMultiplier) * ticksPerSecond, MidpointRounding.AwayFromZero);

            Contract.Assert(delay > 0);

            if (!_humanMovements.TryAdd(character.Id, delay))
                throw new NotSupportedException();
        }

        private void AddToHexPopulation(Character character)
        {
            Contract.Assert(character.CharacterLocationX != -1 && character.CharacterLocationY != -1);
            Contract.Assert(character.MoveDestinationX == -1 && character.MoveDestinationY == -1);

            MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

            AddToHexPopulation(hex, character);
        }

        private static void AddToHexPopulation(MapHex hex, Character character)
        {
            hex.Population.Add(character.Id, null);
            hex.PopulationCount[(int)character.CharacterRace]++;
        }

        private void RemoveFromHexPopulation(Character character)
        {
            Contract.Assert(character.CharacterLocationX != -1 && character.CharacterLocationY != -1);
            Contract.Assert(character.MoveDestinationX != -1 && character.MoveDestinationY != -1);

            MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

            RemoveFromHexPopulation(hex, character);
        }

        private static void RemoveFromHexPopulation(MapHex hex, Character character)
        {
            if (hex.Population.Remove(character.Id))
                hex.PopulationCount[(int)character.CharacterRace]--;
        }

        private void ProcessHumanMovements()
        {
            if (_humanMovements.Count == 0)
                return;

            Contract.Assert(_queueInt.Count == 0);

            foreach (KeyValuePair<int, int> p in _humanMovements)
            {
                int tick = p.Value;

                if (tick > 0)
                {
                    _humanMovements[p.Key] = tick - 1;
                }
                else
                {
                    Character character = _characters[p.Key];
                    Client27000 client = character.Client;

                    // updates the character

                    RemoveFromHexPopulation(character);

                    character.CharacterLocationX = character.MoveDestinationX;
                    character.CharacterLocationY = character.MoveDestinationY;

                    character.MoveDestinationX = -1;
                    character.MoveDestinationY = -1;

                    AddToHexPopulation(character);

                    // ... updates the political control

                    Races lastPoliticalControl = character.CharacterPoliticalControl;

                    ApplyPressure(character);
                    UpdatePoliticalControl(character);

                    // updates the client

                    if (lastPoliticalControl != character.CharacterPoliticalControl)
                        Write(client, Relays.PlayerRelayC, 0x02, 0x00, 0x02, character.Id); // 15_8 (not used)

                    //Write(client, Relays.MetaViewPortHandlerNameC, 0x06, 0x00, 0x0f, 0x00); // 15_F
                    Write(client, Relays.PlayerRelayC, 0x03, 0x00, 0x04, character.Id); // 15_10

                    M_EndMovement(character, client);

                    // checks if we have any mission to send

                    if (character.Mission != 0)
                    {
                        if ((character.Mission & HostMask) >> HostShift == character.Id)
                            SendMissionToHost(client);
                        else
                        {
                            int draftId = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;

                            if (_drafts.TryGetValue(draftId, out Draft draft))
                            {
                                if (draft.Mission == null)
                                {
                                    SendMissionToGuest(client);

                                    draft.Expected.Add(character.Id, null);
                                }
                                /*
                                    else if (draft.Expected.Count == 0)
                                    {
                                        // can we even join an ongoing mission?

                                        character.State |= Character.States.IsBusy;

                                        AddTeam(draft.Mission, character, TeamTag.kTagA, ref tick);

                                        draft.Mission.Configuration = draft.Mission.Configuration.Replace("AlliedHuman = 1", "AlliedHuman = 2");
                                        
                                        R_SendConfig(character, draft.Mission);

                                        Thread.Sleep(3000);

                                        R_F_3(draft.Mission);
                                        R_SendMission(client, draft.Mission);

                                        Thread.Sleep(3000);

                                        ResetClient(client);
                                        R_StartMission(client);
                                    }
                                */
                            }
                        }
                    }

                    // releases the character

                    Contract.Assert((character.State & Character.States.IsBusy) == Character.States.IsBusy);

                    character.State &= ~Character.States.IsBusy;

                    // finalizes

                    _queueInt.Enqueue(character.Id);
                }
            }

            int c = _queueInt.Count;

            if (c != 0)
            {
                do
                {
                    _humanMovements.Remove(_queueInt.Dequeue());

                    c--;
                }
                while (c != 0);

                BroadcastIcons();
            }
        }

        private void ProcessCpuMovements()
        {
            bool isDirty = false;
            bool isSync = _seconds % cpuMovementInterval == 0;

            Contract.Assert(_listInt.Count == 0);

            foreach (KeyValuePair<int, int> p in _cpuMovements)
            {
                Character character = _characters[p.Key];

                if (character.State == Character.States.IsCpuAfkBusyOnline)
                {
                    Contract.Assert(character.Mission != 0);

                    continue;
                }

                Contract.Assert(character.State == Character.States.IsCpuOnline);

                int tick = p.Value;

                if (tick > 0)
                {
                    _cpuMovements[p.Key] = tick - 1;

                    continue;
                }

                if (isSync)
                {
                    Contract.Assert(character.ShipCount > 0);

                    Ship ship = _ships[character.Ships[0]];

                    if ((ship.ClassType >= ClassTypes.kClassFreighter && ship.ClassType <= ClassTypes.kClassBattleship) || ship.ClassType == ClassTypes.kClassMonster)
                    {
                        int i1 = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;

                        MapHex origin = _map[i1];

                        for (int i = 0; i < 7; i++)
                        {
                            int i2 = i1 + _directions[character.CharacterLocationX & 1][i];

                            if (i2 < 0 || i2 >= _map.Length || Math.Abs((i1 % _mapWidth) - (i2 % _mapWidth)) > 2)
                                continue;

                            MapHex destination = _map[i2];

                            if ((destination.Mission & IsClosedMask) == IsClosedMask)
                                continue;

                            if (origin.PopulationCount[(int)character.CharacterRace] - destination.PopulationCount[(int)character.CharacterRace] >= -1)
                            {
                                int j = 0;

                                if (origin.EmpireControl == character.CharacterRace) j += 1;
                                if (origin.CartelControl == character.CharacterRace) j += 2;

                                if (destination.EmpireControl == character.CharacterRace) j += 4;
                                if (destination.CartelControl == character.CharacterRace) j += 8;

                                if (
                                    (character.CharacterRace >= Races.kFirstNPC && j != 0) ||
                                    (character.CharacterRace >= Races.kFirstCartel && (j & 10) != 0) ||
                                    (character.CharacterRace <= Races.kLastEmpire && (j & 5) != 0)
                                )
                                    _listInt.Add(i2);
                            }
                        }

                        int c = _listInt.Count;

                        if (c > 0)
                        {
                            MapHex destination = _map[_listInt[_rand.NextInt32(c)]];

                            RemoveFromHexPopulation(origin, character);

                            character.CharacterLocationX = destination.X;
                            character.CharacterLocationY = destination.Y;

                            AddToHexPopulation(destination, character);

                            _listInt.Clear();

                            isDirty = true;
                        }
                    }
                    else
                    {
                        int i1 = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;

                        // we need to check if our hex, or at least one of the adjacent hexes, is owned by us or not
                        // we do this to prevent the exploitation of remote bases\planets in order to advance our empire

                        for (int i = 0; i < 7; i++)
                        {
                            int i2 = i1 + _directions[character.CharacterLocationX & 1][i];

                            if (i2 < 0 || i2 >= _map.Length || Math.Abs((i1 % _mapWidth) - (i2 % _mapWidth)) > 2)
                                continue;

                            MapHex destination = _map[i2];

                            if (destination.EmpireControl == character.CharacterRace || destination.CartelControl == character.CharacterRace)
                                goto applyPressure;
                        }

                        goto updateCpuMovement;
                    }

                applyPressure:

                    ApplyPressure(character);

                updateCpuMovement:

                    AddOrUpdateCpuMovement(character);
                }
            }

            if (isDirty)
                BroadcastIcons();
        }

        private void ApplyPressure(Character character)
        {
            int race = (int)character.CharacterRace;
            MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

            // gets the current score

            double score = character.ShipsBPV;

            if ((character.State & Character.States.IsCpu) == Character.States.IsCpu)
                score *= cpuPressureMultiplier;
            else
                score *= humanPressureMultiplier;

            score += hex.ControlPoints[race];

            Contract.Assert(score > 0.0);

            // gets the max score

            double max;

            if (race <= (int)Races.kLastEmpire)
                max = hex.EmpireBaseVictoryPoints;
            else if (race <= (int)Races.kLastCartel)
                max = hex.CartelBaseVictoryPoints;
            else
                max = (hex.EmpireBaseVictoryPoints + hex.CartelBaseVictoryPoints) * 0.5;

            if (score <= max)
                hex.ControlPoints[race] = score;
            else
            {
                hex.ControlPoints[race] = max;

                int allies = (int)_alliances[race] ^ (1 << race);

                double half = max * 0.5;
                double surplus = score - max;

                for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                {
                    if ((allies & 1 << i) != 0)
                    {
                        // ally (down to 50%)

                        if (hex.ControlPoints[i] > half)
                        {
                            hex.ControlPoints[i] -= surplus;

                            if (hex.ControlPoints[i] < half)
                                hex.ControlPoints[i] = half;
                        }
                    }
                    else
                    {
                        // enemy (down to 0%)

                        if (hex.ControlPoints[i] > surplus)
                        {
                            hex.ControlPoints[i] -= surplus;

                            if (hex.ControlPoints[i] < 0.0)
                                hex.ControlPoints[i] = 0.0;
                        }
                    }
                }
            }
        }

        private void UpdateHexOwnership()
        {
            bool isDirty = false;

            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                int empire = (int)hex.EmpireControl;
                int cartel = (int)hex.CartelControl;

                double empirePoints = hex.ControlPoints[empire];
                double cartelPoints = hex.ControlPoints[cartel];

                for (int j = (int)Races.kFirstEmpire; j <= (int)Races.kLastEmpire; j++)
                {
                    if (empirePoints < hex.ControlPoints[j])
                    {
                        isDirty = true;

                        empire = j;
                        empirePoints = hex.ControlPoints[j];
                    }

                    int k = j + (int)Races.kFirstCartel;

                    if (cartelPoints < hex.ControlPoints[k])
                    {
                        isDirty = true;

                        cartel = k;
                        cartelPoints = hex.ControlPoints[k];
                    }
                }

                for (int j = (int)Races.kOrion; j <= (int)Races.kMonster; j++)
                {
                    if (empirePoints < hex.ControlPoints[j])
                    {
                        isDirty = true;

                        empire = (int)Races.kNeutralRace;
                        empirePoints = hex.ControlPoints[j];
                    }

                    if (cartelPoints < hex.ControlPoints[j])
                    {
                        isDirty = true;

                        cartel = (int)Races.kNeutralRace;
                        cartelPoints = hex.ControlPoints[j];
                    }
                }

                if (hex.EmpireControl != (Races)empire)
                {
                    hex.EmpireControl = (Races)empire;

                    Contract.Assert(hex.EmpireControl == Races.kNeutralRace || hex.EmpireControl >= Races.kFirstEmpire && hex.EmpireControl <= Races.kLastEmpire);

                    if (empire == (int)Races.kNeutralRace)
                        hex.EmpireCurrentVictoryPoints = 0;
                    else
                    {
                        hex.EmpireCurrentVictoryPoints = (int)Math.Round(hex.ControlPoints[empire], MidpointRounding.AwayFromZero);

                        if (hex.EmpireCurrentVictoryPoints > hex.EmpireBaseVictoryPoints)
                            hex.EmpireCurrentVictoryPoints = hex.EmpireBaseVictoryPoints;
                    }
                }

                if (hex.CartelControl != (Races)cartel)
                {
                    hex.CartelControl = (Races)cartel;

                    Contract.Assert(hex.CartelControl == Races.kNeutralRace || hex.CartelControl >= Races.kFirstCartel && hex.CartelControl <= Races.kLastCartel);

                    if (cartel == (int)Races.kNeutralRace)
                        hex.CartelCurrentVictoryPoints = 0;
                    else
                    {
                        hex.CartelCurrentVictoryPoints = (int)Math.Round(hex.ControlPoints[cartel], MidpointRounding.AwayFromZero);

                        if (hex.CartelCurrentVictoryPoints > hex.CartelBaseVictoryPoints)
                            hex.CartelCurrentVictoryPoints = hex.CartelBaseVictoryPoints;
                    }
                }
            }

            if (isDirty)
            {
                foreach (KeyValuePair<int, Client27000> p in _clients)
                {
                    Client27000 client = p.Value;
                    Character character = client.Character;

                    if (character != null && (character.State & Character.States.IsBusy) != Character.States.IsBusy)
                        Write(client, Relays.MetaViewPortHandlerNameC, 0x03, 0x02, 0x00, -1); // D_3
                }
            }
        }

        private bool UpdateHexTerrain(MapHex hex)
        {
            int hexBase = 0;

            foreach (KeyValuePair<int, object> p in hex.Population)
            {
                if (_characters.TryGetValue(p.Key, out Character character) && character.ShipCount == 1 && _ships.TryGetValue(character.Ships[0], out Ship ship))
                {
                    switch (ship.ClassType)
                    {
                        case ClassTypes.kClassListeningPost:
                            hexBase = 5;
                            break;
                        case ClassTypes.kClassBaseStation:
                            hexBase = 3;
                            break;
                        case ClassTypes.kClassBattleStation:
                            hexBase = 2;
                            break;
                        case ClassTypes.kClassStarBase:
                            hexBase = 1;
                            break;
                    }
                }
            }

            if (hex.Base != hexBase)
            {
                hex.Base = hexBase;
                hex.BaseType = (BaseTypes)(1 << (hexBase - 1));

                return true;
            }

            return false;
        }

        // map

        private void LoadMap()
        {
            MetaVerseMap map = new MetaVerseMap();

            if (!map.Load(_root + "Assets/Maps/" + _mapName))
                throw new NotSupportedException();

            // initializes the hex map

            _mapWidth = map.Width;
            _mapHeight = map.Height;
            _map = new MapHex[map.Cells.Count];

            // loads the hex map

            int i = 0;

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    MetaVerseMap.tCell cell = map.Cells[i];

                    // creates a hex

                    MapHex hex = new MapHex()
                    {
                        Id = GetNextDataId(),

                        X = x,
                        Y = y
                    };

                    if (cell.Region == 0)
                        hex.EmpireControl = Races.kNeutralRace;
                    else
                        hex.EmpireControl = (Races)(cell.Region - 1);

                    if (cell.CartelRegion == 0)
                        hex.CartelControl = Races.kNeutralRace;
                    else
                        hex.CartelControl = (Races)(cell.CartelRegion + 7);

                    hex.Terrain = cell.Terrain;
                    hex.Planet = cell.Planet;
                    hex.Base = cell.Base;

                    hex.TerrainType = (TerrainTypes)(1 << cell.Terrain);

                    if (cell.Planet == 0)
                        hex.PlanetType = PlanetTypes.kPlanetNone;
                    else
                        hex.PlanetType = (PlanetTypes)(1 << (cell.Planet - 1));

                    if (cell.Base == 0)
                        hex.BaseType = BaseTypes.kBaseNone;
                    else
                        hex.BaseType = (BaseTypes)(1 << (cell.Base - 1));

                    hex.BaseEconomicPoints = cell.Economic;
                    hex.CurrentEconomicPoints = cell.Economic;

                    hex.EmpireBaseVictoryPoints = cell.Strength;
                    hex.EmpireCurrentVictoryPoints = cell.Strength;

                    hex.CartelBaseVictoryPoints = cell.Strength;
                    hex.CartelCurrentVictoryPoints = cell.Strength;

                    hex.BaseSpeedPoints = cell.Impedence;
                    hex.CurrentSpeedPoints = cell.Impedence;

                    // helpers

                    hex.ControlPoints[(int)hex.EmpireControl] = cell.Strength;
                    hex.ControlPoints[(int)hex.CartelControl] = cell.Strength;

                    // adds the hex to the map

                    _map[i] = hex;

                    i++;
                }
            }
        }

        private void ResetHomeLocations()
        {
            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                Location location = _homeLocations[i];

                location.X = -1;
                location.Y = -1;
                location.Z = 0;
            }
        }

        private void UpdateHomeLocations()
        {
            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                if (hex.EmpireControl != Races.kNeutralRace)
                    TryUpdateHomeLocation(hex, hex.EmpireControl);

                if (hex.CartelControl != Races.kNeutralRace)
                    TryUpdateHomeLocation(hex, hex.CartelControl);
            }
        }

        private void TryUpdateHomeLocation(MapHex hex, Races race)
        {
            int score;

            if (hex.Planet > 0 && ContainsHomeLocation(hex, race, ClassTypes.kClassPlanets, ClassTypes.kClassPlanets))
                score = 65536 - hex.Planet;
            else if (hex.Base > 0 && ContainsHomeLocation(hex, race, ClassTypes.kClassListeningPost, ClassTypes.kClassStarBase))
                score = 256 - hex.Base;
            else
                return;

            Location location = _homeLocations[(int)race];

            if (location.Z < score)
            {
                location.X = hex.X;
                location.Y = hex.Y;

                location.Z = score;
            }
        }

        private bool ContainsHomeLocation(MapHex hex, Races race, ClassTypes firstClass, ClassTypes lastClass)
        {
            Contract.Assert(race >= Races.kFirstEmpire && race <= Races.kLastCartel);

            Races ally;

            if (race <= Races.kLastEmpire)
                ally = race + 8;
            else
                ally = race - 8;

            foreach (KeyValuePair<int, object> p in hex.Population)
            {
                Character character = _characters[p.Key];

                if (character.CharacterRace == race || character.CharacterRace == ally)
                {
                    for (int i = 0; i < character.ShipCount; i++)
                    {
                        Ship ship = _ships[character.Ships[i]];

                        if (ship.ClassType >= firstClass && ship.ClassType <= lastClass)
                            return true;
                    }
                }
            }

            return false;
        }

        // economy

        private void CalculateInitialProduction()
        {
            // values of 1 turn

#if DEBUG
            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                Contract.Assert
                (
                    _curBudget[i] == 0.0 &&
                    _curExpenses[i] == 0.0 &&
                    _curMaintenance[i] == 0.0 &&
                    _curProduction[i] == 0.0 &&
                    
                    _curPopulation[i] == 0 &&
                    _curSize[i] == 0
                );
            }
#endif

            CalculateMaintenance();
            CalculateProduction();

            // multiplied by an year

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                _curMaintenance[i] *= _turnsPerYear;
                _curProduction[i] *= _turnsPerYear;

                _curPopulation[i] *= _turnsPerYear;
                _curSize[i] *= _turnsPerYear;
            }
        }

        private void CalculateMaintenance()
        {
            // sums the BPV of all ships

            foreach (KeyValuePair<int, Character> p in _characters)
            {
                Character character = p.Value;

                _curMaintenance[(int)character.CharacterRace] += character.ShipsBPV;

                _curPopulation[(int)character.CharacterRace]++;
            }
        }

        private void CalculateProduction()
        {
            // sums the economic points of all hexes

            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                bool neutralPresence = false;

                if (hex.EmpireControl == Races.kNeutralRace)
                    neutralPresence = true;
                else
                {
                    _curProduction[(int)hex.EmpireControl] += hex.CurrentEconomicPoints;

                    _curSize[(int)hex.EmpireControl]++;
                }

                if (hex.CartelControl == Races.kNeutralRace)
                    neutralPresence = true;
                else
                {
                    _curProduction[(int)hex.CartelControl] += hex.CurrentEconomicPoints;

                    _curSize[(int)hex.CartelControl]++;
                }

                if (neutralPresence)
                {
                    _curProduction[(int)Races.kNeutralRace] += hex.CurrentEconomicPoints;

                    _curSize[(int)Races.kNeutralRace]++;
                }
            }
        }

        private void CalculateBudget()
        {
            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                // logs the values and averages

                _logBudget[i].Add(_curBudget[i]);

                _logExpenses[i].Add(_curExpenses[i]);
                _logMaintenance[i].Add(_curMaintenance[i]);
                _logProduction[i].Add(_curProduction[i]);

                _logPopulation[i].Add((int)Math.Round((double)_curPopulation[i] / _turnsPerYear, MidpointRounding.AwayFromZero));
                _logSize[i].Add((int)Math.Round((double)_curSize[i] / _turnsPerYear, MidpointRounding.AwayFromZero));

                // calculates the totals from last year

                double totalExpenses = _curExpenses[i] * _expensesMultiplier;
                double totalMaintenance = _curMaintenance[i] * _maintenanceMultiplier;
                double totalProduction = _curProduction[i] * _productionMultiplier;

                // calculates the budget for the new year

                double budgetFromLastYear = _curBudget[i];
                double budgetForTheNewYear = totalProduction - totalMaintenance - totalExpenses;

                _curBudget[i] = Math.Round(budgetFromLastYear * 0.25 + budgetForTheNewYear * 0.75, MidpointRounding.AwayFromZero);

                // resets the other values

                _curExpenses[i] = 0.0;
                _curMaintenance[i] = 0.0;
                _curProduction[i] = 0.0;

                _curPopulation[i] = 0;
                _curSize[i] = 0;
            }

            // merges the neutral budgets

            double neutralBudget = 0.0;

            for (int i = (int)Races.kFirstNPC; i < (int)Races.kNumberOfRaces; i++)
            {
                neutralBudget += _curBudget[i];

                _curBudget[i] = 0.0;
            }

            _curBudget[(int)Races.kNeutralRace] = Math.Round(neutralBudget, MidpointRounding.AwayFromZero);
        }

        // stardate

        private static int GetCurrentTime()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

            return (int)ts.TotalSeconds;
        }

        // specs

        private void LoadShiplist()
        {
            const string specs = "Assets/Specs/";

            // reads the shiplist

            SortedDictionary<string, object> d = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            StreamReader r = new StreamReader(_root + specs + "shiplist.txt", Encoding.ASCII);

            while (!r.EndOfStream)
            {
                string t = r.ReadLine();

                if (t.Length == 0 || t.StartsWith("\t", StringComparison.Ordinal) || t.StartsWith("Race", StringComparison.Ordinal))
                    continue;

                string[] a = t.Split('\t', StringSplitOptions.None);

                ShipData data = new ShipData()
                {
                    Race = (Races)GetIndex(a[0], _races, StringComparison.OrdinalIgnoreCase),
                    HullType = a[1],
                    ClassName = a[2],
                    ClassType = (ClassTypes)GetIndex(a[3], _classTypes, StringComparison.OrdinalIgnoreCase),
                    BPV = GetInteger(a[4]),
                    SpecialRole = GetSpecialRole(a[5]),
                    YearFirstAvailable = GetInteger(a[6]),
                    YearLastAvailable = GetInteger(a[7]),
                    SizeClass = GetInteger(a[8]),
                    TurnMode = a[9],

                    MoveCost = GetFloat(a[10]),
                    HetAndNimble = GetInteger(a[11]),
                    HetBreakdown = GetInteger(a[12]),
                    StealthOrECM = GetInteger(a[13]),
                    RegularCrew = GetFloat(a[14]),
                    BoardingPartiesBase = GetInteger(a[15]),
                    BoardingPartiesMax = GetInteger(a[16]),
                    DeckCrews = GetInteger(a[17]),
                    TotalCrew = GetFloat(a[18]),
                    MinCrew = GetInteger(a[19]),

                    Shield1 = GetInteger(a[20]),
                    Shield2And6 = GetInteger(a[21]),
                    Shield3And5 = GetInteger(a[22]),
                    Shield4 = GetInteger(a[23]),
                    ShieldTotal = GetInteger(a[24]),
                    Cloak = GetInteger(a[25]),
                    Num1 = GetInteger(a[26]),
                    HeavyWeapon1 = (WeaponTypes)GetIndex(a[27], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc1 = a[28],
                    Num2 = GetInteger(a[29]),

                    HeavyWeapon2 = (WeaponTypes)GetIndex(a[30], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc2 = a[31],
                    Num3 = GetInteger(a[32]),
                    HeavyWeapon3 = (WeaponTypes)GetIndex(a[33], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc3 = a[34],
                    Num4 = GetInteger(a[35]),
                    HeavyWeapon4 = (WeaponTypes)GetIndex(a[36], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc4 = a[37],
                    Num5 = GetInteger(a[38]),
                    HeavyWeapon5 = (WeaponTypes)GetIndex(a[39], _weaponTypes, StringComparison.OrdinalIgnoreCase),

                    Arc5 = a[40],
                    Num6 = GetInteger(a[41]),
                    HeavyWeapon6 = (WeaponTypes)GetIndex(a[42], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc6 = a[43],
                    Num7 = GetInteger(a[44]),
                    HeavyWeapon7 = (WeaponTypes)GetIndex(a[45], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc7 = a[46],
                    Num8 = GetInteger(a[47]),
                    HeavyWeapon8 = (WeaponTypes)GetIndex(a[48], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc8 = a[49],

                    Num9 = GetInteger(a[50]),
                    HeavyWeapon9 = (WeaponTypes)GetIndex(a[51], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc9 = a[52],
                    Num10 = GetInteger(a[53]),
                    HeavyWeapon10 = (WeaponTypes)GetIndex(a[54], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc10 = a[55],
                    Num11 = GetInteger(a[56]),
                    Weapon11 = (WeaponTypes)GetIndex(a[57], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc11 = a[58],
                    Num12 = GetInteger(a[59]),

                    Weapon12 = (WeaponTypes)GetIndex(a[60], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc12 = a[61],
                    Num13 = GetInteger(a[62]),
                    Weapon13 = (WeaponTypes)GetIndex(a[63], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc13 = a[64],
                    Num14 = GetInteger(a[65]),
                    Weapon14 = (WeaponTypes)GetIndex(a[66], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc14 = a[67],
                    Num15 = GetInteger(a[68]),
                    Weapon15 = (WeaponTypes)GetIndex(a[69], _weaponTypes, StringComparison.OrdinalIgnoreCase),

                    Arc15 = a[70],
                    Num16 = GetInteger(a[71]),
                    Weapon16 = (WeaponTypes)GetIndex(a[72], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc16 = a[73],
                    Num17 = GetInteger(a[74]),
                    Weapon17 = (WeaponTypes)GetIndex(a[75], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc17 = a[76],
                    Num18 = GetInteger(a[77]),
                    Weapon18 = (WeaponTypes)GetIndex(a[78], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc18 = a[79],

                    Num19 = GetInteger(a[80]),
                    Weapon19 = (WeaponTypes)GetIndex(a[81], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc19 = a[82],
                    Num20 = GetInteger(a[83]),
                    Weapon20 = (WeaponTypes)GetIndex(a[84], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc20 = a[85],
                    Num21 = GetInteger(a[86]),
                    Weapon21 = (WeaponTypes)GetIndex(a[87], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc21 = a[88],
                    Num22 = GetInteger(a[89]),

                    Weapon22 = (WeaponTypes)GetIndex(a[90], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc22 = a[91],
                    Num23 = GetInteger(a[92]),
                    Weapon23 = (WeaponTypes)GetIndex(a[93], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc23 = a[94],
                    Num24 = GetInteger(a[95]),
                    Weapon24 = (WeaponTypes)GetIndex(a[96], _weaponTypes, StringComparison.OrdinalIgnoreCase),
                    Arc24 = a[97],
                    Num25 = GetInteger(a[98]),
                    Weapon25 = (WeaponTypes)GetIndex(a[99], _weaponTypes, StringComparison.OrdinalIgnoreCase),

                    Arc25 = a[100],
                    Probes = GetInteger(a[101]),
                    T_BombsBase = GetInteger(a[102]),
                    T_BombsMax = GetInteger(a[103]),
                    NuclearMineBase = GetInteger(a[104]),
                    NuclearMineMax = GetInteger(a[105]),
                    DroneControl = GetInteger(a[106]),
                    ADD_6 = GetInteger(a[107]),
                    ADD_12 = GetInteger(a[108]),
                    ShuttlesSize = GetInteger(a[109]),

                    LaunchRate = GetInteger(a[110]),
                    GeneralBase = GetInteger(a[111]),
                    GeneralMax = GetInteger(a[112]),
                    FighterBay1 = GetInteger(a[113]),
                    FighterType1 = a[114],
                    FighterBay2 = GetInteger(a[115]),
                    FighterType2 = a[116],
                    FighterBay3 = GetInteger(a[117]),
                    FighterType3 = a[118],
                    FighterBay4 = GetInteger(a[119]),

                    FighterType4 = a[120],
                    Armor = GetInteger(a[121]),
                    ForwardHull = GetInteger(a[122]),
                    CenterHull = GetInteger(a[123]),
                    AftHull = GetInteger(a[124]),
                    Cargo = GetInteger(a[125]),
                    Barracks = GetInteger(a[126]),
                    Repair = GetInteger(a[127]),
                    R_L_Warp = GetInteger(a[128]),
                    C_Warp = GetInteger(a[129]),

                    Impulse = GetInteger(a[130]),
                    Apr = GetInteger(a[131]),
                    Battery = GetInteger(a[132]),
                    Bridge = GetInteger(a[133]),
                    Security = GetInteger(a[134]),
                    Lab = GetInteger(a[135]),
                    Transporters = GetInteger(a[136]),
                    Tractors = GetInteger(a[137]),
                    MechTractors = GetInteger(a[138]),
                    SpecialSensors = GetInteger(a[139]),

                    Sensors = GetInteger(a[140]),
                    Scanners = GetInteger(a[141]),
                    ExplosionStrength = GetInteger(a[142]),
                    Acceleration = GetInteger(a[143]),
                    DamageControl = GetInteger(a[144]),
                    ExtraDamage = GetInteger(a[145]),
                    ShipCost = GetInteger(a[146]),
                    RefitBaseClass = a[147],
                    Geometry = a[148],
                    UI = a[149],

                    FullName = a[150],
                    Refits = a[151],
                    Balance = GetInteger(a[152]),
                };

                Contract.Assert(data.Race != Races.kNoRace && data.SizeClass >= 1 && data.SizeClass <= 6 && data.YearFirstAvailable <= data.YearLastAvailable);

                // skips unusable entries (like the stars)

                if (data.ClassType == ClassTypes.kNoClassType)
                    continue;

                // sorts the ships by <Race>, <ClassType>, <BPV> and <ClassName>

                t = ((int)data.Race).ToString("D2", CultureInfo.InvariantCulture) +
                    ((int)data.ClassType).ToString("D2", CultureInfo.InvariantCulture) +
                    data.BPV.ToString("D5", CultureInfo.InvariantCulture) +
                    data.ClassName;

                d.Add(t, data);
            }

            r.Close();

            // creates the ship list, using <ClassName> as key

            Contract.Assert(_shiplist.Count == 0);

            foreach (KeyValuePair<string, object> p in d)
            {
                ShipData data = (ShipData)p.Value;

                _shiplist.Add(data.ClassName, data);
            }

            // reads the ftrlist

            d.Clear();

            r = new StreamReader(_root + specs + "ftrlist.txt", Encoding.ASCII);

            while (!r.EndOfStream)
            {
                string t = r.ReadLine();

                if (t.Length == 0 || t.StartsWith("\t", StringComparison.Ordinal) || t.StartsWith("Race", StringComparison.Ordinal))
                    continue;

                string[] a = t.Split('\t', StringSplitOptions.None);

                FighterData data = new FighterData()
                {
                    Race = (Races)GetIndex(a[0], _races, StringComparison.OrdinalIgnoreCase),
                    HullType = a[1],
                    Speed = GetInteger(a[2]),

                    Num1 = GetInteger(a[3]),
                    Weapon1 = a[4],
                    Arc1 = a[5],
                    Shots1 = GetInteger(a[6]),

                    Num2 = GetInteger(a[7]),
                    Weapon2 = a[8],
                    Arc2 = a[9],
                    Shots2 = GetInteger(a[10]),

                    Num3 = GetInteger(a[11]),
                    Weapon3 = a[12],
                    Arc3 = a[13],
                    Shots3 = GetInteger(a[14]),

                    Num4 = GetInteger(a[15]),
                    Weapon4 = a[16],
                    Arc4 = a[17],
                    Shots4 = GetInteger(a[18]),

                    Num5 = GetInteger(a[19]),
                    Weapon5 = a[20],
                    Arc5 = a[21],
                    Shots5 = GetInteger(a[22]),

                    Damage = GetInteger(a[23]),
                    ADD_6 = GetInteger(a[24]),
                    GroundAttackBonus = GetInteger(a[25]),
                    ECM = GetInteger(a[26]),
                    ECCM = GetInteger(a[27]),
                    BPV = GetInteger(a[28]),
                    CarrierSizeClass = GetInteger(a[29]),
                    FirstYearAvailable = GetInteger(a[30]),
                    LastYearAvailable = GetInteger(a[31]),
                    Size = GetInteger(a[32]),
                    UI = a[33],
                    Geometry = a[34],
                    Name = a[35]
                };

                Contract.Assert(data.Race != Races.kNoRace && data.UI.Length > 0);

                // sorts the fighters by <Race>, the <BPV> reversed and <HullType>

                t = ((int)data.Race).ToString("D2", CultureInfo.InvariantCulture) +
                    (99999 - data.BPV).ToString("D5", CultureInfo.InvariantCulture) +
                    data.HullType;

                d.Add(t, data);
            }

            r.Close();

            // creates the fighter list, using <HullType> as key

            Contract.Assert(_ftrlist.Count == 0);

            foreach (KeyValuePair<string, object> p in d)
            {
                FighterData data = (FighterData)p.Value;

                _ftrlist.Add(data.HullType, data);
            }

            // creates the pseudo\fighter list, for each race, as it is used in 'AVtShipRelay.cs'

            Rent(1024, out byte[] b, out MemoryStream m, out BinaryWriter w);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                int c = 0;

                w.Seek(0, SeekOrigin.Begin);
                w.Write(c);

                foreach (KeyValuePair<string, FighterData> p in _ftrlist)
                {
                    FighterData fighter = p.Value;

                    if (fighter.Race == (Races)i)
                    {
                        w.Write(fighter.HullType.Length);
                        w.Write(Encoding.UTF8.GetBytes(fighter.HullType));

                        w.Write(fighter.BPV);

                        c++;
                    }
                }

                foreach (KeyValuePair<string, ShipData> p in _shiplist)
                {
                    ShipData ship = p.Value;

                    if (ship.Race == (Races)i && ship.ClassType == ClassTypes.kClassPseudoFighter)
                    {
                        w.Write(ship.ClassName.Length);
                        w.Write(Encoding.UTF8.GetBytes(ship.ClassName));

                        w.Write(ship.BPV);

                        c++;
                    }
                }

                long lastPosition = m.Position;

                w.Seek(0, SeekOrigin.Begin);
                w.Write(c);

                m.SetLength(lastPosition);

                _supplyFtrCache[i] = m.ToArray();
            }

            Return(b, m, w);
        }

        private static int GetIndex(string key, string[] keys, StringComparison comparisonType)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].Equals(key, comparisonType))
                    return i;
            }

            if (key.Length == 0 || key.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                return -1;

            throw new NotSupportedException();
        }

        private static float GetFloat(string t)
        {
            if (t.Length == 0)
                return 0f;

            if (float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;

            throw new NotSupportedException();
        }

        private static int GetInteger(string t)
        {
            if (t.Length == 0)
                return 0;

            if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                return result;

            throw new NotSupportedException();
        }

        private static SpecialRoles GetSpecialRole(string t)
        {
            if (t.Length == 0)
                return SpecialRoles.NotSpecified;

            byte[] b = Encoding.UTF8.GetBytes(t);
            int r = 0;

            for (int i = 0; i < b.Length; i++)
            {
                if (b[i] >= 65 && b[i] <= 90)
                    r |= 1 << b[i] - 65; // A - Z
                else if (b[i] >= 97 && b[i] <= 122)
                    r |= 1 << b[i] - 97; // a - z
            }

            if (r != 0)
                return (SpecialRoles)r;

            throw new NotSupportedException();
        }

        private bool GetShipData(Races race, ClassTypes minClassType, ClassTypes maxClassType, int minBPV, int maxBPV, SpecialRoles validRoles, SpecialRoles invalidRoles, int yearAvailable, out ShipData shipData)
        {
            List<ShipData> list = new List<ShipData>();

            foreach (KeyValuePair<string, ShipData> p in _shiplist)
            {
                ShipData data = p.Value;

                if (
                    data.Race == race &&
                    data.ClassType >= minClassType && data.ClassType <= maxClassType &&
                    data.BPV >= minBPV && data.BPV <= maxBPV &&
                    (data.SpecialRole & validRoles) != SpecialRoles.None && (data.SpecialRole & invalidRoles) == SpecialRoles.None &&
                    data.YearFirstAvailable <= yearAvailable && data.YearLastAvailable >= yearAvailable
                )
                    list.Add(data);
            }

            int c = list.Count;

            if (c > 0)
            {
                shipData = list[_rand.NextInt32(c)];

                return true;
            }

            shipData = null;

            return false;
        }

        private void CopyShipData(ShipData data, out Ship ship)
        {
            Rent(2048, out byte[] b, out MemoryStream m, out BinaryWriter w, out BinaryReader r);

            // header

            ship = new Ship
            {
                Race = data.Race,
                ClassType = data.ClassType,
                BPV = data.BPV,
                EPV = data.BPV,
                ShipClassName = data.ClassName
            };

            // damage chunk

            WriteShipData(w, data.R_L_Warp);
            WriteShipData(w, data.R_L_Warp);
            WriteShipData(w, data.C_Warp);
            WriteShipData(w, data.Impulse);
            WriteShipData(w, data.Apr);
            WriteShipData(w, data.Bridge);
            WriteShipData(w, data.Sensors);
            WriteShipData(w, data.Scanners);
            WriteShipData(w, data.DamageControl);
            WriteShipData(w, data.Repair);
            WriteShipData(w, data.ForwardHull);
            WriteShipData(w, data.AftHull);
            WriteShipData(w, data.CenterHull);
            WriteShipData(w, data.Tractors);
            WriteShipData(w, data.ExtraDamage);
            WriteShipData(w, data.Transporters);
            WriteShipData(w, data.Transporters);
            WriteShipData(w, data.Battery);
            WriteShipData(w, data.Lab);
            WriteShipData(w, data.Cargo);
            WriteShipData(w, data.Armor);
            WriteShipData(w, data.Cloak);
            WriteShipData(w, data.DamageControl);
            WriteShipData(w, data.Probes);
            WriteShipData(w, data.Barracks);
            WriteShipData(w, data.Num1);
            WriteShipData(w, data.Num2);
            WriteShipData(w, data.Num3);
            WriteShipData(w, data.Num4);
            WriteShipData(w, data.Num5);
            WriteShipData(w, data.Num6);
            WriteShipData(w, data.Num7);
            WriteShipData(w, data.Num8);
            WriteShipData(w, data.Num9);
            WriteShipData(w, data.Num10);
            WriteShipData(w, data.Num11);
            WriteShipData(w, data.Num12);
            WriteShipData(w, data.Num13);
            WriteShipData(w, data.Num14);
            WriteShipData(w, data.Num15);
            WriteShipData(w, data.Num16);
            WriteShipData(w, data.Num17);
            WriteShipData(w, data.Num18);
            WriteShipData(w, data.Num19);
            WriteShipData(w, data.Num20);
            WriteShipData(w, data.Num21);
            WriteShipData(w, data.Num22);
            WriteShipData(w, data.Num23);
            WriteShipData(w, data.Num24);
            WriteShipData(w, data.Num25);

            // saves the chunk

            m.Seek(0L, SeekOrigin.Begin);

            ship.Damage = new ShipDamage(r);

            /*
                Stores chunk

                                   hps  ammo           DroA             DroB             DroC             DroD             DroE             DroF             DroG             DroH             DroI             DroM             DroVI            
                1) 0101 00 00 0100 0B00 6400 0000 6400 0000080001000400 00000C0001000600 0000080001000400 0000080001000400 0000100001000800 0000080001000400 0000080001000400 0000080001000400 0000080001000400 0000080001000400 0000080001000400
                2) 0101 00 00 0100 1600 C800 0000 C800 0000100002000400 0000180002000600 0000100002000400 0000100002000400 0000200002000800 0000100002000400 0000100002000400 0000100002000400 0000100002000400 0000100002000400 0000100002000400
                3) 0101 00 00 0100 2100 2C01 0000 2C01 0000180003000400 0000240003000600 0000180003000400 0000180003000400 0000300003000800 0000180003000400 0000180003000400 0000180003000400 0000180003000400 0000180003000400 0000180003000400
                4) 
                   // ?

                   01
                   01

                   // missiles data

                   00   // missiles type         (type 1, type 4)
                   00   // missiles drive system (slow, medium, fast)
                   0100 // missiles reloads      (one, two, three, four)

                   2C00 // missiles tubes

                   9001 // missiles count    (missiles on tubes + missiles on silos)
                   0000 // missiles on tubes
                   9001 // missiles on silos

                   // weapon 1

                   0000 // ?
                   2000 // number of ammo
                   0400 // number of hardpoints
                   0400 // drone type

                   // weapon 2 ...

                   0000
                   3000
                   0400
                   0600

                   // weapon 3, 4, 5, ...

                   0000200004000400 0000200004000400 0000400004000800 0000200004000400 0000200004000400 0000200004000400 0000200004000400 0000200004000400 0000200004000400                
            */

            m.Seek(0L, SeekOrigin.Begin);

            // 0

            w.Write((ushort)0x0101);

            // 2

            w.Write(0x00);

            // 6

            w.Write((ulong)0x00);

            // 12

            int droneCount = 0;
            int droneAmmo = 0;

            WriteShipData(w, data.Num1, data.HeavyWeapon1, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num2, data.HeavyWeapon2, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num3, data.HeavyWeapon3, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num4, data.HeavyWeapon4, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num5, data.HeavyWeapon5, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num6, data.HeavyWeapon6, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num7, data.HeavyWeapon7, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num8, data.HeavyWeapon8, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num9, data.HeavyWeapon9, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num10, data.HeavyWeapon10, ref droneCount, ref droneAmmo);

            WriteShipData(w, data.Num11, data.Weapon11, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num12, data.Weapon12, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num13, data.Weapon13, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num14, data.Weapon14, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num15, data.Weapon15, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num16, data.Weapon16, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num17, data.Weapon17, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num18, data.Weapon18, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num19, data.Weapon19, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num20, data.Weapon20, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num21, data.Weapon21, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num22, data.Weapon22, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num23, data.Weapon23, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num24, data.Weapon24, ref droneCount, ref droneAmmo);
            WriteShipData(w, data.Num25, data.Weapon25, ref droneCount, ref droneAmmo);

            Contract.Assert(m.Position == 214L);

            if (droneCount > 0)
            {
                w.Seek(2, SeekOrigin.Begin);

                // 2

                w.Write(0x_0001_00_00);      // type 1, slow, one

                w.Write((ushort)droneCount); // total hardpoints
                w.Write(droneAmmo);          // total ammo loaded and used
                w.Write((ushort)droneAmmo);  // total ammo remaining

                w.Seek(214, SeekOrigin.Begin);
            }

            // 214

            WriteShipData(w, data.ShuttlesSize, data.ShuttlesSize);
            WriteShipData(w, 13, (ulong)0x00);

            // 230

            Contract.Assert(m.Position == 230L);

            WriteShipData(w, 100, 0xffffffffffffffff);

            // 330

            WriteShipData(w, data.BoardingPartiesMax, data.BoardingPartiesBase);
            WriteShipData(w, data.T_BombsMax, data.T_BombsBase);

            int defaultSpareParts = (int)Math.Round(data.DamageControl * _sparePartsMultiplier, MidpointRounding.AwayFromZero);

            WriteShipData(w, defaultSpareParts, defaultSpareParts);

            // 339

            WriteShipData(w, data.FighterBay1, data.FighterType1);
            WriteShipData(w, data.FighterBay2, data.FighterType2);
            WriteShipData(w, data.FighterBay3, data.FighterType3);
            WriteShipData(w, data.FighterBay4, data.FighterType4);

            // saves the chunk

            m.Seek(0L, SeekOrigin.Begin);

            ship.Stores = new ShipStores(r);

            // Officers chunk

            m.Seek(0L, SeekOrigin.Begin);

            for (int i = 0; i < (int)OfficerRoles.kMaxOfficers; i++)
            {
                const int rank = (int)OfficerRanks.kSenior;

                string name = Enum.GetName(typeof(OfficerRoles), i);

                Utils.WriteString(w, name[1..]);

                w.Write(rank);
                w.Write(0x00);
                w.Write(_officerDefaults[rank]);
            }

            // saves the chunk

            m.Seek(0L, SeekOrigin.Begin);

            ship.Officers = new ShipOfficers(r);

            // closes everything

            Return(b, m, w, r);
        }

        private void WriteShipData(BinaryWriter w, int droneCount, WeaponTypes droneType, ref int totalTubes, ref int totalMissiles)
        {
            if (droneCount > 0 && _droneCapacities.ContainsKey(droneType))
            {
                // default AI values

                const int defaultReloads = 1;
                const int defaultMissilesSize = 1;

                int droneCapacity = _droneCapacities[droneType];
                int missilesStored = droneCapacity * droneCount * (1 + defaultReloads) / defaultMissilesSize;

                totalTubes += droneCount;
                totalMissiles += missilesStored;

                // 0000 0800 0100 0400

                w.Write((ushort)0x00); // missilesReady
                w.Write((ushort)missilesStored);
                w.Write((ushort)droneCount);
                w.Write((ushort)droneCapacity);
            }
            else if ((droneCount > 0 && droneType != WeaponTypes.None) || (droneCount == 0 && droneType == WeaponTypes.None))
                w.Write((ulong)0x00);
            else
                throw new NotSupportedException();
        }

        private static void WriteShipData(BinaryWriter w, int value)
        {
            if (value >= 0 && value <= 127)
                w.Write((ushort)(value | value << 8));
            else
                throw new NotSupportedException();
        }

        private static void WriteShipData(BinaryWriter w, int count, ulong value)
        {
            Contract.Assert(count >= 0);

            while (count >= 8)
            {
                w.Write(value);

                count -= 8;
            }

            if (count >= 4)
            {
                w.Write((uint)value);

                count -= 4;
            }

            if (count >= 2)
            {
                w.Write((ushort)value);

                count -= 2;
            }

            if (count >= 1)
            {
                w.Write((byte)value);

                Contract.Assert(count == 1);
            }
        }

        private static void WriteShipData(BinaryWriter w, int maxValue, int baseValue)
        {
            if (maxValue <= 127 && baseValue >= 0 && maxValue >= baseValue)
            {
                w.Write((byte)maxValue);
                w.Write((ushort)(baseValue | (baseValue << 8)));
            }
            else
                throw new NotSupportedException();
        }

        private void WriteShipData(BinaryWriter w, int fighterBay, string fighterType)
        {
            if (fighterBay == 0 && fighterType.Length == 0)
            {
                w.Write(0);
                w.Write((ulong)0);
            }
            else if (fighterBay > 0 && (_shiplist.ContainsKey(fighterType) || _ftrlist.ContainsKey(fighterType)))
            {
                w.Write(fighterBay | (fighterBay << 8) | (fighterBay << 16));

                // type

                byte[] b = Encoding.UTF8.GetBytes(fighterType);

                w.Write(b.Length);
                w.Write(b);

                // sub type

                w.Write(0);
            }
            else
                throw new NotSupportedException();
        }

        private void ClassifyPlanetsAndBases()
        {
            /*
                planet type classification:

                0 - asteroid base
                1 - colony
                2 - core world
            */

            foreach (KeyValuePair<string, ShipData> p in _shiplist)
            {
                ShipData data = p.Value;

                switch (data.ClassType)
                {
                    case ClassTypes.kClassListeningPost:
                        throw new NotSupportedException();

                    case ClassTypes.kClassBaseStation:
                        _baseStations[(int)data.Race].Add(data); break;

                    case ClassTypes.kClassBattleStation:
                        _battleStations[(int)data.Race].Add(data); break;

                    case ClassTypes.kClassStarBase:
                        _starbases[(int)data.Race].Add(data); break;

                    case ClassTypes.kClassPlanets:
                        {
                            string name;

                            if (data.Race == Races.kNeutralRace)
                                name = data.ClassName;
                            else
                                name = data.ClassName[2..];

                            if (name.StartsWith("PLPh", StringComparison.Ordinal) && int.TryParse(name[4..], NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                                switch (_planetTypes[number])
                                {
                                    case 0:
                                        _asteroidBases[(int)data.Race].Add(data); break;

                                    case 1:
                                        _colonies[(int)data.Race].Add(data); break;

                                    case 2:
                                        _coreWorlds[(int)data.Race].Add(data); break;
                                }

                            else if (name.StartsWith("PL", StringComparison.Ordinal) && int.TryParse(name[2..], NumberStyles.None, CultureInfo.InvariantCulture, out number))
                                switch (_planetTypes[number])
                                {
                                    case 0:
                                        _asteroidBases[(int)data.Race].Add(data); break;

                                    case 1:
                                        _colonies[(int)data.Race].Add(data); break;

                                    case 2:
                                        _coreWorlds[(int)data.Race].Add(data); break;
                                }

                            else if (!name.Equals("PE", StringComparison.Ordinal) || data.Race == Races.kFederation)
                                _homeWorlds[(int)data.Race].Add(data);

                            break;
                        }
                    case ClassTypes.kClassSpecial:
                        {
                            string name;

                            if (data.Race == Races.kNeutralRace)
                                name = data.ClassName;
                            else
                                name = data.ClassName[2..];

                            if (name.Equals("LP", StringComparison.Ordinal))
                                _listeningPosts[(int)data.Race].Add(data);
                            else if (name.Equals("DEF", StringComparison.Ordinal))
                                _weaponPlatforms[(int)data.Race].Add(data);

                            break;
                        }
                }
            }
        }

        // ships

        private void CreateInitialPlanetsAndBases()
        {
            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                if (hex.Planet > 0 || hex.Base > 0)
                {
                    Races race = hex.EmpireControl;

                    if (race > Races.kLastEmpire && race != Races.kNeutralRace)
                        throw new NotSupportedException();

                    if (hex.Planet > 0)
                    {
                        ShipData data;

                        if (hex.PlanetType == PlanetTypes.kPlanetHomeWorld1)
                            data = _homeWorlds[(int)race][0];

                        else if (hex.PlanetType == PlanetTypes.kPlanetHomeWorld2)
                            data = _homeWorlds[(int)race][1];

                        else if (hex.PlanetType == PlanetTypes.kPlanetHomeWorld3)
                            data = _homeWorlds[(int)race][2];

                        else if (hex.PlanetType <= PlanetTypes.kPlanetCoreWorld3)
                            data = _coreWorlds[(int)race][_rand.NextInt32(_coreWorlds[(int)race].Count)];

                        else if (hex.PlanetType <= PlanetTypes.kPlanetColony3)
                            data = _colonies[(int)race][_rand.NextInt32(_colonies[(int)race].Count)];

                        else if (hex.PlanetType <= PlanetTypes.kPlanetAsteroidBase3)
                            data = _asteroidBases[(int)race][_rand.NextInt32(_asteroidBases[(int)race].Count)];

                        else
                            throw new NotSupportedException();

                        CreateCharacter(race, hex.X, hex.Y, data);
                    }

                    if (hex.Base > 0)
                    {
                        ShipData data;

                        if (hex.BaseType == BaseTypes.kBaseStarbase)
                            data = _starbases[(int)race][_rand.NextInt32(_starbases[(int)race].Count)];

                        else if (hex.BaseType == BaseTypes.kBaseBattleStation)
                            data = _battleStations[(int)race][_rand.NextInt32(_battleStations[(int)race].Count)];

                        else if (hex.BaseType == BaseTypes.kBaseBaseStation)
                            data = _baseStations[(int)race][_rand.NextInt32(_baseStations[(int)race].Count)];

                        else if (hex.BaseType == BaseTypes.kBaseWeaponsPlatform)
                            data = _weaponPlatforms[(int)race][_rand.NextInt32(_weaponPlatforms[(int)race].Count)];

                        else if (hex.BaseType == BaseTypes.kBaseListeningPost)
                            data = _listeningPosts[(int)race][_rand.NextInt32(_listeningPosts[(int)race].Count)];

                        else
                            throw new NotSupportedException();

                        CreateCharacter(race, hex.X, hex.Y, data);
                    }
                }
            }
        }

        private void CreateInitialPopulation()
        {
            int currentYear = CurrentYear;

            List<Location> locations = new List<Location>();
            List<ShipData> ships = new List<ShipData>();

            for (int i = (int)Races.kFirstEmpire; i <= (int)Races.kLastNPC; i++)
            {
                Races race = (Races)i;

                // gets all the available locations

                for (int j = 0; j < _map.Length; j++)
                {
                    MapHex hex = _map[j];

                    if (race <= Races.kLastEmpire)
                    {
                        if (hex.EmpireControl != race)
                            continue;
                    }
                    else if (race <= Races.kLastCartel)
                    {
                        if (hex.CartelControl != race)
                            continue;
                    }
                    else if (race <= Races.kLastNPC)
                    {
                        if (hex.EmpireControl != Races.kNeutralRace && hex.CartelControl != Races.kNeutralRace)
                            continue;
                    }

                    locations.Add(new Location(hex.X, hex.Y, 0));
                }

                Contract.Assert(locations.Count > 0);

                // gets all the available ships

                foreach (KeyValuePair<string, ShipData> p in _shiplist)
                {
                    ShipData data = p.Value;

                    if (
                        data.Race != race ||
                        data.ClassType < ClassTypes.kClassFrigate || (i <= (int)Races.kOrion && data.ClassType > ClassTypes.kClassBattleship) || (i == (int)Races.kMonster && data.ClassType != ClassTypes.kClassMonster) ||
                        (data.SpecialRole & _validRoles) == SpecialRoles.None || (data.SpecialRole & _invalidRoles) != SpecialRoles.None ||
                        data.YearLastAvailable < currentYear || data.YearFirstAvailable > currentYear
                    )
                        continue;

                    ships.Add(data);
                }

                Contract.Assert(ships.Count > 0);

                // creates the initial population

                for (int j = 0; j < _initialPopulation; j++)
                {
                    // tries to get a random location, not overcrowded, and crashes if it is not sucessful

                    double t = _clock.Elapsed.TotalMilliseconds;
                    Location location;

                    do
                    {
                        location = locations[_rand.NextInt32(locations.Count)];

                        if (_clock.Elapsed.TotalMilliseconds - t > 1000.0)
                            throw new NotSupportedException("Map too small?");
                    }
                    while (location.Z >= 3);

                    location.Z++;

                    // gets a random ship data

                    ShipData data = ships[_rand.NextInt32(ships.Count)];

                    // creates the character

                    CreateCharacter(race, location.X, location.Y, data);
                }

                ships.Clear();
                locations.Clear();
            }
        }

        private void CreateShip(Races race, ClassTypes minClassType, ClassTypes maxClassType, int minBPV, int maxBPV, SpecialRoles validRoles, SpecialRoles invalidRoles, int yearAvailable, out Ship ship)
        {
            if (!GetShipData(race, minClassType, maxClassType, minBPV, maxBPV, validRoles, invalidRoles, yearAvailable, out ShipData data))
                throw new NotSupportedException();

            CreateShip(data, out ship);
        }

        private void CreateShip(ShipData data, out Ship ship)
        {
            CopyShipData(data, out ship);

            ship.Id = GetNextDataId();
            ship.LockID = 0;
            ship.OwnerID = 0;
            ship.IsInAuction = 0;

            ship.Name = _raceAbbreviations[(int)ship.Race] + (ship.Id & 0xffff).ToString("X4", CultureInfo.InvariantCulture) + "S";
            ship.TurnCreated = _turn;

            ship.Flags = 0;

            _ships.Add(ship.Id, ship);
        }

        private void ModifyShip(Ship ship, Races race)
        {
            ship.Race = race;

            byte[] ftrCache = _supplyFtrCache[(int)race];
            string firstFtr = Encoding.UTF8.GetString(ftrCache, 8, BitConverter.ToInt32(ftrCache, 4));

            for (int i = 0; i < 4; i++)
            {
                if (ship.Stores.FighterBays[i].FightersMax > 0)
                {
                    ship.Stores.FighterBays[i].FightersCount = 0;
                    ship.Stores.FighterBays[i].FightersLoaded = 0;

                    ship.Stores.FighterBays[i].FighterType = firstFtr;
                    ship.Stores.FighterBays[i].FighterSubType = string.Empty;
                }
            }
        }

        private static void RefreshShip(Ship ship)
        {
            Rent(1024, out byte[] b, out MemoryStream m, out BinaryWriter w);

            // updates the ship cache

            w.Write(ship.Id);
            w.Write(ship.BPV);
            w.Write((int)ship.ClassType);

            w.Write(ship.ShipClassName.Length);
            w.Write(Encoding.UTF8.GetBytes(ship.ShipClassName));

            w.Write(0x3f800000); // unknown (float?)  

            w.Write(ship.Name.Length);
            w.Write(Encoding.UTF8.GetBytes(ship.Name));

            w.Write(ship.Flags);

            m.SetLength(m.Position);

            ship.ShipCache = m.ToArray();

            // closes everything

            Return(b, m, w);
        }

        private static double GetShipDamagePercentage(Ship ship)
        {
            int total = 0;

            int affected = 0;
            int damaged = 0;

            for (int i = 0; i < Ship.DamageSize; i += 2)
            {
                int max = ship.Damage.Items[i];
                int min = ship.Damage.Items[i + 1];

                total += max;

                if (i >= 16 && i <= 19)
                    continue;

                if (max > min)
                {
                    affected += max;
                    damaged += (max - min);
                }
            }

            if (damaged == 0)
                return 0.0;

            return (double)damaged / affected * ((double)affected / total);
        }

        private int GetShipRepairCost(Ship ship)
        {
            double damagePercentage = GetShipDamagePercentage(ship);

            if (damagePercentage == 0.0)
                return 0;

            return (int)Math.Truncate(ship.BPV * _costRepair[(int)ship.ClassType] * damagePercentage);
        }

        private int GetShipTradeInValue(Ship ship)
        {
            int repairCost = GetShipRepairCost(ship);

            return (int)Math.Truncate(ship.BPV * _costTradeIn) - repairCost;
        }

        private int GetStoresCost(ClassTypes t, ShipStores s)
        {
            // missiles

            double v = _missileSizes[(int)s.MissilesType] * (int)s.MissilesDriveSystem * s.TotalMissilesReadyAndStored * 0.25 * _costMissiles;

            // shuttles

            v += s.General * _costShuttles;

            // supplies

            v += s.BoardingParties * _costMarines;
            v += s.TBombs * _costMines;
            v += s.DamageControl * _costSpareParts[(int)t];

            // fighters

            for (int i = 0; i < 4; i++)
            {
                int c = s.FighterBays[i].FightersCount;

                // cost

                if (c > 0)
                {
                    string k = s.FighterBays[i].FighterType;

                    if (_ftrlist.ContainsKey(k))
                        c *= _ftrlist[k].BPV;
                    else if (_shiplist.ContainsKey(k))
                        c *= _shiplist[k].BPV;
                    else
                        throw new NotImplementedException();

                    v += c * _costFighters;
                }
            }

            return (int)Math.Truncate(v);
        }

        private static void RepairShip(Ship ship)
        {
            ref byte[] d = ref ship.Damage.Items;

            for (int i = 0; i < Ship.DamageSize; i += 2)
                d[i + 1] = d[i];
        }

        private static void AutomaticRepair(Ship ship, double percentage)
        {
            ref byte[] d = ref ship.Damage.Items;

            for (int i = 0; i < Ship.DamageSize; i += 2)
            {
                double max = d[i];

                if (max > 0.0)
                    d[i + 1] = (byte)GetWithPercentageOrMinimum(d[i + 1], max, percentage, 1.0);
            }
        }

        private void AutomaticResupply(Ship ship, double percentage)
        {
            ShipStores s = ship.Stores;

            // upgrades the missiles

            if (_turn >= _fastMissileSpeedDate)
                s.MissilesDriveSystem = MissileDriveSystems.Fast;
            else if (_turn >= _mediumMissileSpeedDate)
                s.MissilesDriveSystem = MissileDriveSystems.Medium;
            else
                Contract.Assert(s.MissilesDriveSystem == MissileDriveSystems.Slow);

            s.MissilesReloads = 4;

            // resupplies the missiles

            int c = 0;

            for (int i = 0; i < 25; i++)
            {
                int j = s.MissileHardpoints[i].TubesCount;

                if (j > 0)
                {
                    j *= s.MissileHardpoints[i].TubesCapacity;
                    j *= (s.MissilesReloads + 1);
                    j /= _missileSizes[(int)s.MissilesType];

                    Contract.Assert(s.MissileHardpoints[i].MissilesReady == 0 && s.MissileHardpoints[i].MissilesStored <= j);

                    s.MissileHardpoints[i].MissilesStored = (short)GetWithPercentageOrMinimum(s.MissileHardpoints[i].MissilesStored, j, percentage, 2.0);

                    c += s.MissileHardpoints[i].MissilesStored;
                }
            }

            s.TotalMissilesReadyAndStored = (short)c;

            Contract.Assert(s.TotalMissilesReady == 0);

            s.TotalMissilesStored = (short)c;

            // resuplies the shuttles, boarding parties, mines and spare parts

            s.General = (byte)GetWithPercentageOrMinimum(s.General, s.GeneralMax, percentage, 1.0);

            s.BoardingParties = (byte)GetWithPercentageOrMinimum(s.BoardingParties, s.BoardingPartiesMax, percentage, 1.0);
            s.TBombs = (byte)GetWithPercentageOrMinimum(s.TBombs, s.TBombsMax, percentage, 1.0);
            s.DamageControl = (byte)GetWithPercentageOrMinimum(s.DamageControl, s.DamageControlMax, percentage, 1.0);

            // resuplies the fighters

            for (int i = 0; i < 4; i++)
            {
                int j = s.FighterBays[i].FightersMax;

                if (j > 0)
                {
                    c = (int)GetWithPercentageOrMinimum(s.FighterBays[i].FightersCount, j, percentage, 1.0);

                    s.FighterBays[i].FightersCount = (byte)c;
                    s.FighterBays[i].FightersLoaded = (byte)c;
                }
            }
        }

        private static double GetWithPercentageOrMinimum(double cur, double max, double percentage, double minimum)
        {
            Contract.Assert(percentage <= 1.0);

            percentage *= max;

            if (percentage < minimum)
                percentage = minimum;

            cur = Math.Round(cur + percentage, MidpointRounding.AwayFromZero);

            if (cur > max)
                cur = max;

            return cur;
        }

        // ... report phase

        private void UpdateShipDamage(Ship ship, byte[] buffer, int index)
        {
            // copies the data

            ship.Damage = new ShipDamage(buffer, index);

            // and does some validation

            ref byte[] d = ref ship.Damage.Items;

            for (int i = 0; i < Ship.DamageSize; i += 2)
            {
                if (d[i] > 0)
                {
                    if (d[i] > 127)
                        d[i] = 127;

                    if (d[i + 1] > d[i])
                        d[i + 1] = d[i];
                }
            }

            // normalizes the data

            ShipData data = _shiplist[ship.ShipClassName];

            NormalizeDamage(d, DamageType.RightWarpMax, data.R_L_Warp);
            NormalizeDamage(d, DamageType.LeftWarpMax, data.R_L_Warp);
            NormalizeDamage(d, DamageType.CenterWarpMax, data.C_Warp);
            NormalizeDamage(d, DamageType.ImpulseMax, data.Impulse);
            NormalizeDamage(d, DamageType.AprMax, data.Apr);
        }

        private static void NormalizeDamage(byte[] d, DamageType type, int maxValue)
        {
            if (maxValue > 0)
            {
                if (maxValue > 127)
                    maxValue = 127;

                int i = (int)type;

                Contract.Assert((i & 1) == 0);

                if (d[i] != maxValue)
                {
                    int curValue = (int)Math.Round((double)maxValue / d[i] * d[i + 1], MidpointRounding.AwayFromZero);

                    if (curValue > maxValue)
                        curValue = maxValue;

                    d[i] = (byte)maxValue;
                    d[i + 1] = (byte)curValue;
                }
            }
        }

        private void UpdateShipStores(Ship ship, byte[] buffer, int index, int size)
        {
            ShipStores a = ship.Stores;
            ShipStores b = new ShipStores(buffer, index, size);

            // missile hardpoints

            int c = 0;

            for (int i = 0; i < 25; i++)
            {
                int max = b.MissileHardpoints[i].TubesCount;

                if (max > 0)
                {
                    max = max * b.MissileHardpoints[i].TubesCapacity * (b.MissilesReloads + 1) / _missileSizes[(int)b.MissilesType];

                    int cur = b.MissileHardpoints[i].MissilesReady + b.MissileHardpoints[i].MissilesStored;

                    if (cur > max)
                        cur = max;

                    Contract.Assert(a.MissileHardpoints[i].MissilesReady == 0);

                    a.MissileHardpoints[i].MissilesStored = (short)cur;

                    c += cur;
                }
            }

            // missiles (totals)

            a.TotalMissilesReadyAndStored = (short)c;

            Contract.Assert(a.TotalMissilesReady == 0);

            a.TotalMissilesStored = (short)c;

            // shuttles

            a.General = b.General;

            if (a.General > a.GeneralMax)
                a.General = a.GeneralMax;

            // transport items

            if (b.TransportItems == null)
                a.TransportItems = null;
            else
            {
                c = b.TransportItems.Length;

                Contract.Assert(c >= 1 && b.TransportItems[0].Item == ScriptTransportItems.kTransSpareParts);

                if (c == 1)
                    a.TransportItems = null;
                else
                {
                    a.TransportItems = new TransportItem[c - 1];

                    for (int i = 1; i < c; i++)
                    {
                        a.TransportItems[i - 1].Item = b.TransportItems[i].Item;
                        a.TransportItems[i - 1].Count = b.TransportItems[i].Count;
                    }
                }
            }

            // marines

            a.BoardingParties = b.BoardingParties;

            if (a.BoardingParties > a.BoardingPartiesMax)
                a.BoardingParties = a.BoardingPartiesMax;

            // mines

            a.TBombs = b.TBombs;

            if (a.TBombs > a.TBombsMax)
                a.TBombs = a.TBombsMax;

            // spare parts

            a.DamageControl = b.DamageControl;

            if (a.DamageControl > a.DamageControlMax)
                a.DamageControl = a.DamageControlMax;

            // fighter bays

            for (int i = 0; i < 4; i++)
            {
                byte cur = b.FighterBays[i].FightersCount;
                byte max = b.FighterBays[i].FightersMax;

                if (cur > max)
                    cur = max;

                a.FighterBays[i].FightersCount = cur;
                a.FighterBays[i].FightersLoaded = cur;
            }
        }

        private void UpdateShipOfficers(Ship ship, byte[] buffer, int index, int size)
        {
            // copies the data

            ship.Officers = new ShipOfficers(buffer, index, size);

            // normalizes the data

            UpgradeShipOfficers(ship, OfficerRanks.kSenior);
        }

        // ... draft phase

        private static void UpgradeShipDamage(Ship ship, double powerBoost)
        {
            Contract.Assert(ship.ClassType >= ClassTypes.kClassFreighter);

            // adjusts the power boost

            if (ship.Race >= Races.kFirstCartel && ship.Race <= Races.kLastEmpire)
                powerBoost = Math.Round(powerBoost / 3.0, MidpointRounding.AwayFromZero);

            Contract.Assert(powerBoost > 0.0 && powerBoost <= 1.0);

            powerBoost += 1.0;

            // applies the power boost

            UpgradeDamage(ship, DamageType.RightWarpMax, powerBoost);
            UpgradeDamage(ship, DamageType.LeftWarpMax, powerBoost);
            UpgradeDamage(ship, DamageType.CenterWarpMax, powerBoost);
            UpgradeDamage(ship, DamageType.ImpulseMax, powerBoost);
            UpgradeDamage(ship, DamageType.AprMax, powerBoost);
        }

        private static void UpgradeDamage(Ship ship, DamageType type, double powerBoost)
        {
            int damageType = (int)type;

            Contract.Assert((damageType & 1) == 0);

            UpgradeDamage(ship, damageType, powerBoost);
            UpgradeDamage(ship, damageType + 1, powerBoost);
        }

        private static void UpgradeDamage(Ship ship, int damageType, double powerBoost)
        {
            double value = Math.Round(ship.Damage.Items[damageType] * powerBoost, MidpointRounding.AwayFromZero);

            if (value > 127.0)
                value = 127.0;

            ship.Damage.Items[damageType] = (byte)value;
        }

        private void UpgradeShipOfficers(Ship ship, OfficerRanks rank)
        {
            for (int i = 0; i < (int)OfficerRoles.kMaxOfficers; i++)
            {
                ref Officer officer = ref ship.Officers.Items[i];

                officer.Rank = rank;
                officer.Unknown1 = 0;
                officer.Unknown2 = _officerDefaults[(int)rank];
            }
        }

        // shipyard

        private void CreateShipyard()
        {
            int currentYear = CurrentYear;

            foreach (KeyValuePair<string, ShipData> p in _shiplist)
            {
                ShipData data = p.Value;

                if (
                    data.Race >= Races.kFirstEmpire && data.Race <= Races.kLastCartel &&
                    data.ClassType >= ClassTypes.kClassFrigate && data.ClassType <= ClassTypes.kClassBattleship &&
                    (data.SpecialRole & _validRoles) != SpecialRoles.None && (data.SpecialRole & _invalidRoles) == SpecialRoles.None &&
                    data.YearLastAvailable >= currentYear && data.YearFirstAvailable <= currentYear
                )
                {
                    CreateShip(data, out Ship ship);

                    RefreshShip(ship);

                    AddBidItem(ship);
                }
            }

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                TrySortBidItems(i);
        }

        private void AddBidItem(Ship ship)
        {
            // updates the ship

            Contract.Assert(ship.OwnerID == 0 && ship.IsInAuction == 0);

            ship.IsInAuction = 1;

            // initializes the item

            int turnsToEndOfYear = _turnsPerYear - (_turn % _turnsPerYear);

            BidItem item = new BidItem()
            {
                Id = GetNextDataId(),
                LockID = 0,

                BiddingHasBegun = 0,

                ShipClassName = ship.ShipClassName,
                ShipId = ship.Id,
                ShipBPV = ship.BPV,

                AuctionValue = ship.BPV, // not used ?
                AuctionRate = 1.0,       // not used ?

                TurnOpened = _turn,
                TurnToClose = _turn + turnsToEndOfYear, // we want to renew the shipyard every year
                CurrentBid = GetShipCost(ship),

                BidOwnerID = 0,
                TurnBidMade = 0,
                BidMaximum = 0
            };

            _bidItems[(int)ship.Race].Add(ship.Id, item);
        }

        private int GetShipCost(Ship ship)
        {
            return (int)Math.Round(ship.BPV * _costClassType[(int)ship.ClassType], MidpointRounding.AwayFromZero);
        }

        private void TrySortBidItems(int race)
        {
            Dictionary<int, BidItem> items = _bidItems[race];

            if (items.Count <= 1)
                return;

            Contract.Assert(_sortLongBidItem.Count == 0);

            foreach (KeyValuePair<int, BidItem> p in items)
            {
                Ship ship = _ships[p.Key];

                long key = ((long)(ClassTypes.kMaxClasses - ship.ClassType) << 48) + ((long)(32767 - ship.BPV) << 32) + ship.Id;

                _sortLongBidItem.Add(key, p.Value);
            }

            items.Clear();

            foreach (KeyValuePair<long, BidItem> p in _sortLongBidItem)
            {
                BidItem item = p.Value;

                items.Add(item.ShipId, item);
            }

            _sortLongBidItem.Clear();
        }

        private void ClearShipyard()
        {
            Dictionary<int, BidItem> d = new Dictionary<int, BidItem>();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                Dictionary<int, BidItem> items = _bidItems[i];

                if (items.Count == 0)
                    continue;

                // we want to preserve the bids that are still going on

                foreach (KeyValuePair<int, BidItem> p in items)
                {
                    BidItem item = p.Value;

                    if (item.BiddingHasBegun == 1)
                        d.Add(item.ShipId, item);
                    else if (!_ships.Remove(item.ShipId))
                        throw new NotImplementedException();
                }

                items.Clear();

                foreach (KeyValuePair<int, BidItem> p in d)
                {
                    BidItem item = p.Value;

                    items.Add(item.ShipId, item);
                }

                d.Clear();
            }
        }

        private bool TryUpdateBidItem(Character character, BidItem item, int bidType)
        {
            bool isFirstBid = item.BiddingHasBegun == 0;

            // updates the bid value

            int bidValue;

            if (isFirstBid)
                bidValue = item.CurrentBid;
            else
                bidValue = item.BidMaximum;

            switch (bidType)
            {
                case 2: // +5 prestige
                    bidValue += 5;
                    break;

                case 3: // +10 prestige
                    bidValue += 10;
                    break;

                case 5: // +5 %
                    bidValue = (int)Math.Round(bidValue * 1.05, MidpointRounding.AwayFromZero);
                    break;

                case 6: // +10%
                    bidValue = (int)Math.Round(bidValue * 1.10, MidpointRounding.AwayFromZero);
                    break;

#if DEBUG
                default:
                    throw new NotImplementedException();
#endif

            }

            // updates the character(s) and bid

            if (isFirstBid)
            {
                if (character.CharacterCurrentPrestige < bidValue || character.ShipCount + character.Bids > 2)
                    return false;

                character.Bids++;

                // charges the full bid

                UpdateCharacter(character, -bidValue);

                ShipData data = _shiplist[item.ShipClassName];

                item.BiddingHasBegun = 1;
                item.TurnToClose = _turn + _turnsToClose[data.SizeClass - 1];
                item.BidOwnerID = character.Id;
            }
            else if (character.Id == item.BidOwnerID)
            {
                int inc = bidValue - item.BidMaximum;

                if (character.CharacterCurrentPrestige < inc)
                    return false;

                // charges the difference between the last bid and the current one

                UpdateCharacter(character, -inc);
            }
            else
            {
                if (character.CharacterCurrentPrestige < bidValue || character.ShipCount + character.Bids > 2)
                    return false;

                // returns the last bid to its last owner

                Character lastOwner = _characters[item.BidOwnerID];

                lastOwner.Bids--;

                UpdateCharacter(lastOwner, item.BidMaximum);

                // charges the full bid to the new owner

                character.Bids++;

                UpdateCharacter(character, -bidValue);

                item.BidOwnerID = character.Id;

                // updates the last owner UI

                Write(lastOwner.Client, Relays.PlayerRelayC, 0x05, 0x00, 0x07, lastOwner.Id); // 15_12
            }

            // finalizes the bid

            item.CurrentBid = bidValue;

            item.TurnBidMade = _turn;
            item.BidMaximum = bidValue;

            // updates the current owner UI

            Write(character.Client, Relays.PlayerRelayC, 0x05, 0x00, 0x07, character.Id); // 15_12

            return true;
        }

        private void ProcessBids()
        {
            int mask = 0;

            if (_bidReplacements.Count > 0)
            {
                // there is no point in adding new replacements at the start of a new year
                // so we should check it before doing it

                if (_turn % _turnsPerYear != 0)
                {
                    foreach (KeyValuePair<string, int> p in _bidReplacements)
                    {
                        Ship ship = null;

                        for (int i = 0; i < p.Value; i++)
                        {
                            ShipData data = _shiplist[p.Key];

                            CreateShip(data, out ship);

                            RefreshShip(ship);

                            AddBidItem(ship);
                        }

                        mask |= 1 << (int)ship.Race;
                    }
                }

                // the list is always cleared at the end

                _bidReplacements.Clear();
            }

            Contract.Assert(_queueInt.Count == 0);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                Dictionary<int, BidItem> items = _bidItems[i];

                if (items.Count > 0)
                {
                    int c;

                    foreach (KeyValuePair<int, BidItem> p in items)
                    {
                        BidItem item = p.Value;

                        if (item.TurnToClose <= _turn)
                        {
                            _queueInt.Enqueue(item.ShipId);

                            if (item.BiddingHasBegun == 0)
                            {
                                // deletes the ship

                                _ships.Remove(item.ShipId);
                            }
                            else
                            {
                                // updates the ship

                                Ship ship = _ships[item.ShipId];

                                Contract.Assert(ship.OwnerID == 0 && ship.IsInAuction == 1);

                                ship.OwnerID = item.BidOwnerID;
                                ship.IsInAuction = 0;

                                // updates the character

                                Character character = _characters[item.BidOwnerID];

                                c = character.ShipCount;

                                character.ShipCount = c + 1;
                                character.Ships[c] = ship.Id;

                                character.Bids--;

                                RefreshCharacter(character);

                                if ((character.State & Character.States.IsHumanOnline) == Character.States.IsHumanOnline)
                                {
                                    Write(character.Client, Relays.PlayerRelayC, 0x04, 0x00, 0x05, character.Id); // A_5
                                    Write(character.Client, Relays.PlayerRelayC, 0x06, 0x00, 0x08, character.Id); // 15_13
                                }

                                // queues a replacement for the next turn

                                if (_bidReplacements.ContainsKey(ship.ShipClassName))
                                    _bidReplacements[ship.ShipClassName] += 1;
                                else
                                    _bidReplacements.TryAdd(ship.ShipClassName, 1);
                            }
                        }
                    }

                    c = _queueInt.Count;

                    while (c != 0)
                    {
                        items.Remove(_queueInt.Dequeue());

                        c--;
                    }

                    if ((mask & 1 << i) != 0)
                        TrySortBidItems(i);
                }
            }
        }

        // maintenance

        private void CloseForMaintenance()
        {
            foreach (KeyValuePair<int, Client27000> p in _clients)
            {
                LogoutClient(p.Key);

                p.Value.EndCloseUser();
            }

            Contract.Assert(_clients.IsEmpty);
        }

        private void SaveCampaign(double t)
        {
            Contract.Assert(_lastSavegame != null);

            // tries to create a new file and writer

            FileStream f = null;

            try
            {
                f = new FileStream(_lastSavegame + savegameExtension, FileMode.CreateNew, FileAccess.Write);
            }
            catch (Exception e)
            {
                _lastSavegame = null;

                f?.Close();

                LogError("SaveCampaign()", e);

                return;
            }

            BinaryWriter w = new BinaryWriter(f, Encoding.UTF8, true);

            // general

            w.Write(_locked);

            w.Write(_dictStringUInt.Count);

            w.Write(_listInt.Count);

            w.Write(_queueInt.Count);

            w.Write(_sortLongInt.Count);
            w.Write(_sortLongBidItem.Count);

            // clock

            Contract.Assert(_ts <= t && _t1 <= t && _t60 <= t && _tt <= t);

            w.Write(_ts - t);
            w.Write(_t1 - t);
            w.Write(_t60 - t);
            w.Write(_tt - t);

            w.Write(_smallTicks);
            w.Write(_seconds);

            // stack

            w.Write(_position);

            // server status

            w.Write(_hostId);
            w.Write(_hostName);

            w.Write(_gameType);
            w.Write(_numPlayers);
            w.Write(_maxNumLoggedOnPlayers);
            w.Write(_numLoggedOnPlayers);

            w.Write(_difficultyLevel);
            w.Write(_startingEra);

            // server files

            w.Write(_serverFiles.Count);

            foreach (KeyValuePair<string, uint> p in _serverFiles)
            {
                w.Write(p.Key);
                w.Write(p.Value);
            }

            // data counter

            w.Write(_dataCounter);

            // characters

            w.Write(_logins.Count);
            w.Write(_logouts.Count);

            w.Write(_lastLogin);

            w.Write(_wonLogons.Count);

            foreach (KeyValuePair<string, int> p in _wonLogons)
            {
                w.Write(p.Key);
                w.Write(p.Value);
            }

            w.Write(_characterNames.Count);

            foreach (KeyValuePair<string, int> p in _characterNames)
            {
                w.Write(p.Key);
                w.Write(p.Value);
            }

            w.Write(_characters.Count);

            foreach (KeyValuePair<int, Character> p in _characters)
            {
                w.Write(p.Key);
                p.Value.WriteTo(w);
            }

            w.Write((int)_rank);
            w.Write(_prestige);

            w.Write(_cpuMovements.Count);

            foreach (KeyValuePair<int, int> p in _cpuMovements)
            {
                w.Write(p.Key);
                w.Write(p.Value);
            }

            w.Write(_humanMovements.Count);

            foreach (KeyValuePair<int, int> p in _humanMovements)
            {
                w.Write(p.Key);
                w.Write(p.Value);
            }

            // map

            w.Write(_mapName);

            w.Write(_mapWidth);
            w.Write(_mapHeight);

            w.Write(_map.Length);

            for (int i = 0; i < _map.Length; i++)
                _map[i].WriteTo(w);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _homeLocations[i].WriteTo(w);

            // economy

            w.Write(_expensesMultiplier);
            w.Write(_maintenanceMultiplier);
            w.Write(_productionMultiplier);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(_curBudget[i]);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(_curExpenses[i]);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(_curMaintenance[i]);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(_curProduction[i]);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(_curPopulation[i]);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(_curSize[i]);

            int c;

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                c = _logBudget[i].Count;

                w.Write(c);

                for (int j = 0; j < c; j++)
                    w.Write(_logBudget[i][j]);

                c = _logExpenses[i].Count;

                w.Write(c);

                for (int j = 0; j < c; j++)
                    w.Write(_logExpenses[i][j]);

                c = _logMaintenance[i].Count;

                w.Write(c);

                for (int j = 0; j < c; j++)
                    w.Write(_logMaintenance[i][j]);

                c = _logProduction[i].Count;

                w.Write(c);

                for (int j = 0; j < c; j++)
                    w.Write(_logProduction[i][j]);

                c = _logPopulation[i].Count;

                w.Write(c);

                for (int j = 0; j < c; j++)
                    w.Write(_logPopulation[i][j]);

                c = _logSize[i].Count;

                w.Write(c);

                for (int j = 0; j < c; j++)
                    w.Write(_logSize[i][j]);
            }

            // stardate

            w.Write(_turn);
            w.Write(_turnsPerYear);
            w.Write(_millisecondsPerTurn);

            w.Write(_earlyYears);
            w.Write(_middleYears);
            w.Write(_lateYears);
            w.Write(_advancedYears);

            w.Write(_mediumMissileSpeedDate);
            w.Write(_fastMissileSpeedDate);

            // specs

            Contract.Assert(_missileSizes.Length == 7);

            for (int i = 0; i < 7; i++)
                w.Write(_missileSizes[i]);

            w.Write(_sparePartsMultiplier);

            w.Write(_initialPopulation);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write((int)_alliances[i]);

            // ships

            w.Write(_ships.Count);

            foreach (KeyValuePair<int, Ship> p in _ships)
            {
                w.Write(p.Key);
                p.Value.WriteTo(w);
            }

            w.Write((int)_minClassType);
            w.Write((int)_maxClassType);
            w.Write(_minBPV);
            w.Write(_maxBPV);
            w.Write((int)_invalidRoles);
            w.Write((int)_validRoles);

            w.Write((int)_cpuOfficerRank);
            w.Write(_cpuPowerBoost);

            for (int i = 0; i < (int)ClassTypes.kMaxClasses; i++)
                w.Write(_costClassType[i]);

            for (int i = 0; i < (int)ClassTypes.kMaxClasses; i++)
                w.Write(_costRepair[i]);

            w.Write(_costTradeIn);

            w.Write(_costUnknown);
            w.Write(_costMissiles);
            w.Write(_costFighters);
            w.Write(_costShuttles);
            w.Write(_costMarines);
            w.Write(_costMines);

            for (int i = 0; i < (int)ClassTypes.kMaxClasses; i++)
                w.Write(_costSpareParts[i]);

            w.Write(_cpuAutomaticRepairMultiplier);
            w.Write(_humanAutomaticRepairMultiplier);

            w.Write(_cpuAutomaticResupplyMultiplier);
            w.Write(_humanAutomaticResupplyMultiplier);

            // shipyard

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                w.Write(_bidItems[i].Count);

                foreach (KeyValuePair<int, BidItem> p in _bidItems[i])
                {
                    w.Write(p.Key);
                    p.Value.WriteTo(w);
                }
            }

            w.Write(_bidReplacements.Count);

            foreach (KeyValuePair<string, int> p in _bidReplacements)
            {
                w.Write(p.Key);
                w.Write(p.Value);
            }

            for (int i = 0; i < 6; i++)
                w.Write(_turnsToClose[i]);

            // chat

            w.Write(_serverNick);

            // drafts

            c = _availableMissions.Count;

            w.Write(c);

            for (int i = 0; i < c; i++)
                w.Write(_availableMissions[i]);

            w.Write(_drafts.Count);

            foreach (KeyValuePair<int, Draft> p in _drafts)
            {
                w.Write(p.Key);
                p.Value.WriteTo(w);
            }

            // gs service

            GsService.WriteTo(w);

            // eol

            w.Write(0x12345678);

            // closes the writer and file

            w.Close();

            f.Flush();
            f.Close();
        }

        private void LoadCampaign()
        {
            Contract.Assert(_lastSavegame != null);

            // tries to open the file and creates a reader

            FileStream f = null;

            try
            {
                f = new FileStream(_lastSavegame + savegameExtension, FileMode.Open, FileAccess.Read);
            }
            catch (Exception e)
            {
                f?.Close();

                LogError("LoadCampaign()", e);

                return;
            }

            BinaryReader r = new BinaryReader(f, Encoding.UTF8, true);

            // general

            _locked = r.ReadInt32();

            Contract.Assert(_locked == 1010);

            _dictStringUInt.Clear();

            int c = r.ReadInt32();

            Contract.Assert(c == 0);

            _listInt.Clear();

            c = r.ReadInt32();

            Contract.Assert(c == 0);

            _queueInt.Clear();

            c = r.ReadInt32();

            Contract.Assert(c == 0);

            _sortLongInt.Clear();

            c = r.ReadInt32();

            Contract.Assert(c == 0);

            _sortLongBidItem.Clear();

            c = r.ReadInt32();

            Contract.Assert(c == 0);

            // clock

            _clock.Restart();

            _ts = r.ReadDouble();
            _t1 = r.ReadDouble();
            _t60 = r.ReadDouble();
            _tt = r.ReadDouble();

            _smallTicks = r.ReadInt64();
            _seconds = r.ReadInt64();

            // stack

            _position = r.ReadInt32();

            Contract.Assert(_position >= 0 && _position < _buffer.Length);

            // server status

            _hostId = r.ReadString();
            _hostName = r.ReadString();

            _gameType = r.ReadString();
            _numPlayers = r.ReadInt32();
            _maxNumLoggedOnPlayers = r.ReadInt32();
            _numLoggedOnPlayers = r.ReadInt32();

            _difficultyLevel = r.ReadInt32();
            _startingEra = r.ReadInt32();

            // server files

            _serverFiles.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _serverFiles.Add(r.ReadString(), r.ReadUInt32());

            // data counter

            _dataCounter = r.ReadInt32();

            // characters

            c = r.ReadInt32(); // _logins.Count

            Contract.Assert(c == 0);

            c = r.ReadInt32(); // _logouts.Count

            Contract.Assert(c == 0);

            _lastLogin = r.ReadInt64();

            _wonLogons.Clear();

            c = r.ReadInt32(); ;

            for (int i = 0; i < c; i++)
                _wonLogons.Add(r.ReadString(), r.ReadInt32());

            _characterNames.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _characterNames.Add(r.ReadString(), r.ReadInt32());

            Contract.Assert(_wonLogons.Count == _characterNames.Count);

            _characters.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _characters.Add(r.ReadInt32(), new Character(r));

            _rank = (Ranks)r.ReadInt32();
            _prestige = r.ReadInt32();

            _cpuMovements.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _cpuMovements.Add(r.ReadInt32(), r.ReadInt32());

            _humanMovements.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _humanMovements.Add(r.ReadInt32(), r.ReadInt32());

            Contract.Assert(_humanMovements.Count == 0);

            // map

            _mapName = r.ReadString();

            _mapWidth = r.ReadInt32();
            _mapHeight = r.ReadInt32();

            c = r.ReadInt32();

            _map = new MapHex[c];

            for (int i = 0; i < c; i++)
                _map[i] = new MapHex(r);

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _homeLocations[i] = new Location(r);

            // economy

            _expensesMultiplier = r.ReadDouble();
            _maintenanceMultiplier = r.ReadDouble();
            _productionMultiplier = r.ReadDouble();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _curBudget[i] = r.ReadDouble();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _curExpenses[i] = r.ReadDouble();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _curMaintenance[i] = r.ReadDouble();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _curProduction[i] = r.ReadDouble();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _curPopulation[i] = r.ReadInt32();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _curSize[i] = r.ReadInt32();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                _logBudget[i].Clear();

                c = r.ReadInt32();

                for (int j = 0; j < c; j++)
                    _logBudget[i].Add(r.ReadDouble());

                _logExpenses[i].Clear();

                c = r.ReadInt32();

                for (int j = 0; j < c; j++)
                    _logExpenses[i].Add(r.ReadDouble());

                _logMaintenance[i].Clear();

                c = r.ReadInt32();

                for (int j = 0; j < c; j++)
                    _logMaintenance[i].Add(r.ReadDouble());

                _logProduction[i].Clear();

                c = r.ReadInt32();

                for (int j = 0; j < c; j++)
                    _logProduction[i].Add(r.ReadDouble());

                _logPopulation[i].Clear();

                c = r.ReadInt32();

                for (int j = 0; j < c; j++)
                    _logPopulation[i].Add(r.ReadInt32());

                _logSize[i].Clear();

                c = r.ReadInt32();

                for (int j = 0; j < c; j++)
                    _logSize[i].Add(r.ReadInt32());
            }

            // stardate

            _turn = r.ReadInt32();
            _turnsPerYear = r.ReadInt32();
            _millisecondsPerTurn = r.ReadInt32();

            _earlyYears = r.ReadInt32();
            _middleYears = r.ReadInt32();
            _lateYears = r.ReadInt32();
            _advancedYears = r.ReadInt32();

            _mediumMissileSpeedDate = r.ReadInt32();
            _fastMissileSpeedDate = r.ReadInt32();

            // specs

            _shiplist.Clear();
            _ftrlist.Clear();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _supplyFtrCache[i] = null;

            LoadShiplist();

            Contract.Assert(_missileSizes.Length == 7);

            for (int i = 0; i < 7; i++)
                _missileSizes[i] = r.ReadInt32();

            _sparePartsMultiplier = r.ReadDouble();

            _initialPopulation = r.ReadInt32();

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                _alliances[i] = (Alliances)r.ReadInt32();

            // ships

            _ships.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _ships.Add(r.ReadInt32(), new Ship(r));

            _minClassType = (ClassTypes)r.ReadInt32();
            _maxClassType = (ClassTypes)r.ReadInt32();
            _minBPV = r.ReadInt32();
            _maxBPV = r.ReadInt32();
            _invalidRoles = (SpecialRoles)r.ReadInt32();
            _validRoles = (SpecialRoles)r.ReadInt32();

            _cpuOfficerRank = (OfficerRanks)r.ReadInt32();
            _cpuPowerBoost = r.ReadDouble();

            for (int i = 0; i < (int)ClassTypes.kMaxClasses; i++)
                _costClassType[i] = r.ReadDouble();

            for (int i = 0; i < (int)ClassTypes.kMaxClasses; i++)
                _costRepair[i] = r.ReadDouble();

            _costTradeIn = r.ReadDouble();

            _costUnknown = r.ReadDouble();
            _costMissiles = r.ReadDouble();
            _costFighters = r.ReadDouble();
            _costShuttles = r.ReadDouble();
            _costMarines = r.ReadDouble();
            _costMines = r.ReadDouble();

            for (int i = 0; i < (int)ClassTypes.kMaxClasses; i++)
                _costSpareParts[i] = r.ReadDouble();

            _cpuAutomaticRepairMultiplier = r.ReadDouble();
            _humanAutomaticRepairMultiplier = r.ReadDouble();

            _cpuAutomaticResupplyMultiplier = r.ReadDouble();
            _humanAutomaticResupplyMultiplier = r.ReadDouble();

            // shipyard

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                _bidItems[i].Clear();

                c = r.ReadInt32();

                for (int j = 0; j < c; j++)
                    _bidItems[i].Add(r.ReadInt32(), new BidItem(r));
            }

            _bidReplacements.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _bidReplacements.Add(r.ReadString(), r.ReadInt32());

            for (int i = 0; i < 6; i++)
                _turnsToClose[i] = r.ReadInt32();

            // chat

            _serverNick = r.ReadString();

            // drafts

            _availableMissions.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _availableMissions.Add(r.ReadInt32());

            _drafts.Clear();

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                _drafts.Add(r.ReadInt32(), new Draft(r));

            // gs service

            GsService.ReadFrom(r);

            // eol

            if (r.ReadInt32() != 0x12345678)
                throw new NotSupportedException();

            // closes the reader and file

            r.Close();

            f.Flush();
            f.Close();

#if DEBUG
            // it is a really bad sign if there is any draft still going on
            // because no players are meant to be online at this point

            Contract.Assert(_clients.IsEmpty);

            if (_drafts.Count > 0)
            {
                DebugDrafts();
                DebugMapCharactersAndShips();
            }
#endif

        }

#if DEBUG
        private void DebugDrafts()
        {
            // tries to restore the indexed data for debugging reasons

            foreach (KeyValuePair<int, Draft> d in _drafts)
            {
                Draft draft = d.Value;

                if (_characters.ContainsKey(draft.Mission.Host.Id))
                {
                    draft.Mission.Host = _characters[draft.Mission.Host.Id];

                    Debug.WriteLine("draft.Mission.Host.Id = " + draft.Mission.Host.Id);
                }
                else
                    Debug.WriteLine("draft.Mission.Host.Id = " + draft.Mission.Host.Id + " (missing)");

                foreach (KeyValuePair<int, Team> t in draft.Mission.Teams)
                {
                    Team team = t.Value;

                    if (_characters.ContainsKey(team.Owner.Id))
                    {
                        team.Owner = _characters[team.Owner.Id];

                        Debug.WriteLine("\tteam.Owner.Id = " + team.Owner.Id);
                    }
                    else
                        Debug.WriteLine("\tteam.Owner.Id = " + team.Owner.Id + " (missing)");

                    foreach (KeyValuePair<int, Ship> s in team.Ships)
                    {
                        Ship ship = s.Value;

                        if (_ships.ContainsKey(ship.Id))
                        {
                            team.Ships[s.Key] = _ships[ship.Id];

                            Debug.WriteLine("\t\tship.Id = " + ship.Id);
                        }
                        else
                            Debug.WriteLine("\t\tship.Id = " + ship.Id + " (missing)");
                    }
                }
            }
        }

        private void DebugMapCharactersAndShips()
        {
            int bugs = 0;

            // checks if any character is still linked to a mission

            foreach (KeyValuePair<int, Character> p in _characters)
            {
                Character character = p.Value;

                if (character.Mission != 0)
                {
                    bugs++;

                    Debug.WriteLine("_characters[" + character.Id + "].Mission = " + (character.Mission & MissionFilter));
                }

                //if (character.State != Character.States.IsHumanOnline && character.State != Character.States.IsCpuOnline)
                //    Debug.WriteLine("_characters[" + character.Id + "].State = " + Enum.GetName(typeof(Character.States), character.State));
            }

            // checks if any hex is still linked to a mission

            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                if (hex.Mission != 0)
                {
                    bugs++;

                    Debug.WriteLine("_map[" + i + "].Mission = " + (hex.Mission & MissionFilter));
                }
            }

            // checks if all the ships in each character's fleet still exist, and if they belong to them

            foreach (KeyValuePair<int, Character> p in _characters)
            {
                Character character = p.Value;

                for (int i = 0; i < character.ShipCount; i++)
                {
                    if (_ships.ContainsKey(character.Ships[i]))
                    {
                        Ship ship = _ships[character.Ships[i]];

                        if (ship.OwnerID == character.Id)
                        {
                            if (ship.Damage.Items[(int)DamageType.ExtraDamage] == 0)
                            {
                                bugs++;

                                Contract.Assert(ship.Damage.Items[(int)DamageType.ExtraDamageMax] != 0);

                                Debug.WriteLine("_characters[" + character.Id + "].Ships[" + i + "] is invalid (was already destroyed)");
                            }

                            ship.Flags = character.Id;
                        }
                        else
                        {
                            bugs++;

                            Debug.WriteLine("_ships[" + ship.Id + "].OwnerID == " + ship.OwnerID + "; // should be " + character.Id);
                        }
                    }
                    else
                    {
                        bugs++;

                        Debug.WriteLine("_characters[" + character.Id + "].Ships[" + i + "] doesn't exist");
                    }
                }
            }

            // checks if all the ships are owned by someone

            foreach (KeyValuePair<int, Ship> p in _ships)
            {
                Ship ship = p.Value;

                if (ship.Flags != 0)
                {
                    ship.Flags = 0;
                }
                else if (ship.IsInAuction == 0)
                {
                    Debug.Write("_ships[" + ship.Id + "] of type " + Enum.GetName(typeof(ClassTypes), ship.ClassType) + " doesn't belong to any fleet. ");

                    if (_characters.ContainsKey(ship.OwnerID))
                        Debug.Write("The owner is");
                    else
                        Debug.Write("Last owner was");

                    Debug.WriteLine(" " + ship.OwnerID + ". And the ship has " + ship.Damage.Items[(int)DamageType.ExtraDamage] + " of " + ship.Damage.Items[(int)DamageType.ExtraDamageMax] + " health remaining.");
                }
            }

            // outputs the number of bugs found

            Debug.WriteLine("bugs found: " + bugs);
        }
#endif

    }
}
