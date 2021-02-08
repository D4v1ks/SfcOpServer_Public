#pragma warning disable IDE0066

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Text;

namespace SfcOpServer
{
    public partial class GameServer
    {
        // shift constants

        private const int HostShift = 0;
        private const int MissionShift = 31;
        private const int IsMustPlayShift = 40;

        private const int CounterShift = 41;
        private const int IsClosedShift = 44;

        // mask constants

        private const long HostMask = 0x7fffffffL << HostShift;
        private const long MissionMask = 0x1ffL << MissionShift;
        private const long IsMustPlayMask = 0x1L << IsMustPlayShift;

        private const long MissionFilter = HostMask | MissionMask | IsMustPlayMask;

        private const long CounterMask = 0x7L << CounterShift;
        private const long IsClosedMask = 0x1L << IsClosedShift;

        // other constants

        private const string defaultMissionTitle = "- Patrol -";

        // enumerations

        private enum Alliances
        {
            None = 0,

            // Empire races

            kFederation = 1 << Races.kFederation,
            kKlingon = 1 << Races.kKlingon,
            kRomulan = 1 << Races.kRomulan,
            kLyran = 1 << Races.kLyran,
            kHydran = 1 << Races.kHydran,
            kGorn = 1 << Races.kGorn,
            kISC = 1 << Races.kISC,
            kMirak = 1 << Races.kMirak,

            // Orion Races

            kOrionOrion = 1 << Races.kOrionOrion,
            kOrionKorgath = 1 << Races.kOrionKorgath,
            kOrionPrime = 1 << Races.kOrionPrime,
            kOrionTigerHeart = 1 << Races.kOrionTigerHeart,
            kOrionBeastRaiders = 1 << Races.kOrionBeastRaiders,
            kOrionSyndicate = 1 << Races.kOrionSyndicate,
            kOrionWyldeFire = 1 << Races.kOrionWyldeFire,
            kOrionCamboro = 1 << Races.kOrionCamboro,

            // NPC

            kOrion = 1 << Races.kOrion,
            kMonster = 1 << Races.kMonster,

            // Non-playable races

            kTholian = 1 << Races.kTholian,
            kLDR = 1 << Races.kLDR,
            kWYN = 1 << Races.kWYN,
            kJindarian = 1 << Races.kJindarian,
            kAndro = 1 << Races.kAndro,

            kNeutralRace = 1 << Races.kNeutralRace,

            kMirror = 1 << Races.kMirror,

            // helpers

            All = (1 << Races.kNumberOfRaces) - 1
        }

        private enum TeamID
        {
            kTeam1,
            kTeam2,
            kTeam3,
            kTeam4,
            kTeam5,
            kTeam6,
            kTeam7,
            kTeam8,
            kTeam9,
            kTeam10,
            kTeam11,
            kTeam12,
            kTeam13,
            kTeam14,
            kTeam15,
            kTeam16,
            kTeam17,
            kTeam18,
            kTeam19,
            kTeam20,

            kMaxTeams
        }

        private enum TeamTag
        {
            kNoTeamTag = -1,

            // playable tags ?

            kTagA,
            kTagB,
            kTagC,
            kTagD,
            kTagE,
            kTagF,

            // NPC tags ?

            kTagG,
            kTagH,
            kTagI,
            kTagJ,
            kTagK,
            kTagL,
            kTagM,
            kTagN,
            kTagO,
            kTagP,
            kTagQ,
            kTagR,
            kTagS,
            kTagT,
            kTagU,
            kTagV,
            kTagW,
            kTagX,
            kTagY,
            kTagZ,
        };

        private enum TeamType
        {
            kNoTeamType = -1,

            // Only run by the AI

            kNPCTeam,

            // Intended for human play, although there is a chance that an AI can fill the slot if the scripter wants it

            kPlayableTeam,

            kPrimaryTeam,
            kPrimaryOpponentTeam,

            // Used to indicate a team that has left the universe and must be removed

            kDeadTeam
        };

        private enum CampaignEvents
        {
            kNone = 0,

            kRetire = 1 << 1,
            kLost = 1 << 2,
        };

        private enum VictoryLevels
        {
            kUnknownVictoryLevel = -1,

            kAstoundingVictory,
            kVictory,
            kDraw,
            kDefeat,
            kDevastatingDefeat,
        };

        // classes

        private class Draft
        {
            public int Countdown;

            public Dictionary<int, object> Expected; // ... to play in the mission
            public Dictionary<int, object> Accepted; // ... the misison
            public Dictionary<int, object> Forfeited; // ... the mission
            public Dictionary<int, object> Received; // ... the mission
            public Dictionary<int, object> Started; // ... the mission
            public Dictionary<int, object> LeftEarly; // ... from the mission
            public Dictionary<int, object> Reported; // ... the mission

            public Mission Mission;

            public Draft()
            {
                const int ticksPerSecond = (int)(1000.0 / smallTick);

                Countdown = draftCooldown * ticksPerSecond - 1;

                Contract.Assert(Countdown > 0 && Countdown < 4096); // up to 60 fps during 65 seconds

                Expected = new Dictionary<int, object>();
                Accepted = new Dictionary<int, object>();
                Forfeited = new Dictionary<int, object>();
                Received = new Dictionary<int, object>();
                Started = new Dictionary<int, object>();
                LeftEarly = new Dictionary<int, object>();
                Reported = new Dictionary<int, object>();

                Mission = null;
            }

            /// <summary>
            /// This function is for debugging
            /// </summary>
            /// <param name="r"></param>
            public Draft(BinaryReader r)
            {
                Countdown = r.ReadInt32();

                Expected = new Dictionary<int, object>();

                int c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    Expected.Add(r.ReadInt32(), null);

                Accepted = new Dictionary<int, object>();

                c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    Accepted.Add(r.ReadInt32(), null);

                Forfeited = new Dictionary<int, object>();

                c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    Forfeited.Add(r.ReadInt32(), null);

                Received = new Dictionary<int, object>();

                c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    Received.Add(r.ReadInt32(), null);

                Started = new Dictionary<int, object>();

                c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    Started.Add(r.ReadInt32(), null);

                LeftEarly = new Dictionary<int, object>();

                c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    LeftEarly.Add(r.ReadInt32(), null);

                Reported = new Dictionary<int, object>();

                c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    Reported.Add(r.ReadInt32(), null);

                Mission = new Mission(r);
            }

            /// <summary>
            /// This function is for debugging
            /// </summary>
            /// <param name="w"></param>
            public void WriteTo(BinaryWriter w)
            {
                // this function is only meaningful for debugging

                w.Write(Countdown);

                w.Write(Expected.Count);

                foreach (KeyValuePair<int, object> p in Expected)
                    w.Write(p.Key);

                w.Write(Accepted.Count);

                foreach (KeyValuePair<int, object> p in Accepted)
                    w.Write(p.Key);

                w.Write(Forfeited.Count);

                foreach (KeyValuePair<int, object> p in Forfeited)
                    w.Write(p.Key);

                w.Write(Received.Count);

                foreach (KeyValuePair<int, object> p in Received)
                    w.Write(p.Key);

                w.Write(Started.Count);

                foreach (KeyValuePair<int, object> p in Started)
                    w.Write(p.Key);

                w.Write(LeftEarly.Count);

                foreach (KeyValuePair<int, object> p in LeftEarly)
                    w.Write(p.Key);

                w.Write(Reported.Count);

                foreach (KeyValuePair<int, object> p in Reported)
                    w.Write(p.Key);

                Mission.WriteTo(w);
            }
        }

        private class Mission
        {
            public Character Host;

            public int Map;
            public string Background;
            public int Speed;

            public Dictionary<int, Team> Teams; // character id, team

            public string Config;
            public byte[] Cache;

            public Mission()
            { }

            /// <summary>
            /// This function is for debugging
            /// </summary>
            /// <param name="r"></param>
            public Mission(BinaryReader r)
            {
                Host = new Character(r.ReadInt32());

                Map = r.ReadInt32();
                Background = r.ReadString();
                Speed = r.ReadInt32();

                Teams = new Dictionary<int, Team>();

                int c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    Teams.Add(r.ReadInt32(), new Team(r));

                Config = r.ReadString();
                Cache = r.ReadBytes(r.ReadInt32());
            }

            /// <summary>
            /// This function is for debugging
            /// </summary>
            /// <param name="w"></param>
            public void WriteTo(BinaryWriter w)
            {
                w.Write(Host.Id);

                w.Write(Map);
                w.Write(Background);
                w.Write(Speed);

                w.Write(Teams.Count);

                foreach (KeyValuePair<int, Team> p in Teams)
                {
                    w.Write(p.Key);
                    p.Value.WriteTo(w);
                }

                w.Write(Config);

                w.Write(Cache.Length);
                w.Write(Cache);
            }
        }

        private class Team
        {
            public Character Owner;

            public TeamID Id;
            public TeamType Type;
            public TeamTag Tag;

            public Dictionary<int, Ship> Ships;

            public Team(Character teamOwner, TeamID teamId, TeamType teamType, TeamTag teamTag)
            {
                Owner = teamOwner;

                Id = teamId;
                Type = teamType;
                Tag = teamTag;

                Ships = new Dictionary<int, Ship>();
            }

            /// <summary>
            /// This function is for debugging
            /// </summary>
            /// <param name="r"></param>
            public Team(BinaryReader r)
            {
                Owner = new Character(r.ReadInt32());

                Id = (TeamID)r.ReadInt32();
                Type = (TeamType)r.ReadInt32();
                Tag = (TeamTag)r.ReadInt32();

                Ships = new Dictionary<int, Ship>();

                int c = r.ReadInt32();

                for (int i = 0; i < c; i++)
                    Ships.Add(r.ReadInt32(), new Ship(r.ReadInt32()));
            }

            /// <summary>
            /// This function is for debugging
            /// </summary>
            /// <param name="w"></param>
            public void WriteTo(BinaryWriter w)
            {
                w.Write(Owner.Id);

                w.Write((int)Id);
                w.Write((int)Type);
                w.Write((int)Tag);

                w.Write(Ships.Count);

                foreach (KeyValuePair<int, Ship> p in Ships)
                {
                    w.Write(p.Key);
                    w.Write(p.Value.Id);
                }
            }
        }

        // functions

        private void ResetAvailableMissions()
        {
            Contract.Assert(_availableMissions.Count == 0);

            for (int i = 0; i < _missionNames.Length; i++)
                _availableMissions.Add(i);
        }

        private void TryGetMission(Character character, MapHex destination)
        {
            Contract.Assert(character.Mission == 0);

            if ((character.State & Character.States.IsAfk) == Character.States.IsAfk)
                return;

            if (destination.Mission == 0)
            {
                // checks if we meet the minimum requirements for a mission

                if (destination.Population.Count == 0)
                    return;

                Contract.Assert(!destination.Population.ContainsKey(character.Id));

                // grabs a mission from the list

                int i = _rand.NextInt32(_availableMissions.Count);

                long hostId = character.Id;
                long missionId = _availableMissions[i];
                long isMustPlay = 0; // _rand.NextInt32(2);

                _availableMissions.RemoveAt(i);

                if (_availableMissions.Count == 0)
                    ResetAvailableMissions();

                long mission = (hostId << HostShift) | (missionId << MissionShift) | (isMustPlay << IsMustPlayShift);

                // updates the hex

                destination.Mission = mission | (1L << CounterShift);

                // updates the character

                character.Mission = mission;
            }
            else if ((destination.Mission & IsClosedMask) != IsClosedMask && ((destination.Mission & CounterMask) >> CounterShift) < 6)
            {
                // updates the hex

                destination.Mission += (1L << CounterShift);

                // updates the character

                character.Mission = destination.Mission & MissionFilter;
            }
        }

        private void TryLeaveMission(Character character, MapHex hex)
        {

#if DEBUG
            bool isDone = false;
#endif

            if (character.Mission != 0)
            {
                Contract.Assert((character.Mission & MissionFilter) == character.Mission);

                if (character.Mission == (hex.Mission & MissionFilter))
                {
                    Contract.Assert((hex.Mission & CounterMask) != 0);

                    hex.Mission -= (1L << CounterShift);

                    Debug.WriteLine((hex.Mission & CounterMask) >> CounterShift);

                    if ((hex.Mission & CounterMask) == 0)
                    {
                        int draftId = hex.X + hex.Y * _mapWidth;

                        if (_drafts.TryGetValue(draftId, out Draft draft))
                        {
                            foreach (KeyValuePair<int, Team> t in draft.Mission.Teams)
                            {
                                Team team = t.Value;
                                Character owner = team.Owner;

                                Contract.Assert((owner.Mission & MissionFilter) == owner.Mission);

                                if (owner.Mission == (hex.Mission & MissionFilter))
                                {
                                    // releases the AI from the mission

                                    if ((owner.State & Character.States.IsCpu) == Character.States.IsCpu)
                                    {
                                        if (_characters.ContainsKey(owner.Id))
                                        {
                                            Contract.Assert(owner.ShipCount > 0 && owner.Mission != 0 && owner.State == Character.States.IsCpuAfkBusyOnline);

                                            owner.Mission = 0;
                                            owner.State = Character.States.IsCpuOnline;
                                        }
                                    }
                                }

                                // deletes all the ships that were destroyed

                                foreach (KeyValuePair<int, Ship> s in team.Ships)
                                {
                                    Ship ship = s.Value;

                                    if (ship.Damage.Items[(int)DamageType.ExtraDamage] == 0)
                                    {
                                        Contract.Assert(ship.Damage.Items[(int)DamageType.ExtraDamageMax] != 0);

                                        if (ship.OwnerID == owner.Id)
                                        {
                                            // the ship was destroyed

                                            _ships.Remove(ship.Id);
                                        }
                                        else
                                        {
                                            // the ship was captured (falsely reported as being destroyed)

                                            ship.Damage.Items[(int)DamageType.ExtraDamage] = 1;
                                        }
                                    }
                                }
                            }

                            // checks if something changed in the hex
                            // and the clients need to be updated

                            if (UpdateHexTerrain(hex))
                                BroadcastHex((hex.X << 16) + hex.Y);

#if DEBUG
                            isDone = true;
#endif

                            _drafts.Remove(draftId);
                        }

                        hex.Mission = 0;
                    }
                }

                character.Mission = 0;
            }

#if DEBUG
            if (isDone)
                DebugMapCharactersAndShips();
#endif

        }

        private void SendMissionToHost(Client27000 client)
        {
            long mission = client.Character.Mission;

            Contract.Assert(mission != 0);

            Clear();

            Push((byte)0x00);
            Push(60005); // cooldown (ms)

            Push((byte)((mission & IsMustPlayMask) >> IsMustPlayShift));
            Push(_missionNames[(int)((mission & MissionMask) >> MissionShift)]);

            Push(0x01);

            Push((byte)0x00);
            Push(client.Id, client.Relay[(int)Relays.MissionRelayNameC], 0x04);

            Write(client);
        }

        private void SendMissionToGuest(Client27000 client)
        {
            long mission = client.Character.Mission;

            Contract.Assert(mission != 0);

            Clear();

            Push((byte)0x01);
            Push(60000); // cooldown (ms)

            Push((byte)0x01);
            Push(_missionNames[(int)((mission & MissionMask) >> MissionShift)]);

            Push(0x01);

            Push(0x07);
            Push(0x0f);
            Push(0x00);

            Push((byte)0x01);
            Push(client.Id, client.Relay[(int)Relays.MissionRelayNameC], 0x04);

            Write(client);
        }

        private void ProcessDrafts()
        {
            const int ticksPerSecond = (int)(1000.0 / smallTick);

            //------------------------------------------------------

            const int a0 = 0;

            const int a1 = ticksPerSecond;
            const int a2 = ticksPerSecond + 1;
            const int a3 = draftCooldown * ticksPerSecond - 1;

            //------------------------------------------------------

            const int b0 = 4096;

            const int b1 = b0 + (draftRest * ticksPerSecond - 1);

            //------------------------------------------------------

            const int c0 = 8192;

            const int c1 = c0 + (2 * ticksPerSecond - 1);
            const int c2 = c0 + (draftRest * ticksPerSecond - 1);

            //------------------------------------------------------

            // checks if there is any draft pending

            //DebugMapCharactersAndShips(); 

            if (_drafts.Count == 0)
                return;

            foreach (KeyValuePair<int, Draft> p in _drafts)
            {
                Draft draft = p.Value;

                draft.Countdown--;

                switch (draft.Countdown)
                {
                    case a0:
                        {
                            // everyone is on board! lets try to create a mission

                            MapHex hex = _map[p.Key];

                            Contract.Assert((hex.Mission & IsClosedMask) == IsClosedMask);

                            if (TryCreateMission(draft, hex))
                            {
                                // sends the initial configuration to everyone

                                foreach (KeyValuePair<int, Team> q in draft.Mission.Teams)
                                {
                                    Character owner = q.Value.Owner;

                                    if (draft.Accepted.ContainsKey(owner.Id))
                                    {
                                        try
                                        {
                                            R_SendConfig(owner, draft.Mission);
                                        }
                                        catch (Exception)
                                        { }
                                    }
                                    else if (draft.Expected.ContainsKey(owner.Id))
                                        draft.Forfeited.TryAdd(owner.Id, null);
                                }

                                draft.Countdown = b1;
                            }

                            break;
                        }
                    case a1:
                        {
                            // lets close the mission first, to prevent other players to join it at this stage

                            MapHex hex = _map[p.Key];

                            hex.Mission |= IsClosedMask;

                            break;
                        }
                    case b0:
                        {
                            PrepareMission(draft.Mission);

                            draft.Expected.Clear();

                            foreach (KeyValuePair<int, Team> q in draft.Mission.Teams)
                            {
                                Character owner = q.Value.Owner;

                                if (draft.Accepted.ContainsKey(owner.Id))
                                {
                                    try
                                    {
                                        R_SendMission(owner.Client, draft.Mission);

                                        draft.Expected.Add(owner.Id, null);
                                    }
                                    catch (Exception)
                                    { }
                                }
                            }

                            draft.Countdown = c2;

                            break;
                        }
                    case c0:
                        {
                            int ownerId = 0;

                            foreach (KeyValuePair<int, object> q in draft.Expected)
                            {
                                ownerId = q.Key;

                                if (draft.Received.ContainsKey(ownerId))
                                {
                                    Client27000 client = _characters[ownerId].Client;

                                    ResetClient(client);

                                    try
                                    {
                                        R_StartMission(client);

                                        draft.Started.Add(ownerId, null);
                                    }
                                    catch (Exception)
                                    { }
                                }

                                break;
                            }

                            if (ownerId == 0)
                            {
                                Contract.Assert(draft.Expected.Count == 0);

                                draft.Countdown = 0;

                                // we reset the mission host at this moment because it can change during a mission
                                // and it can help us track what happened after the mission started if anything went wrong

                                draft.Mission.Host = null;
                            }
                            else
                            {
                                draft.Expected.Remove(ownerId);

                                draft.Countdown = c1;
                            }

                            break;
                        }
                    default:
                        {
                            if (draft.Countdown > a2 && draft.Countdown < b0)
                            {
                                Contract.Assert(draft.Countdown < a3);

                                // lets check if we have everyone on board already!

                                if (draft.Expected.Count == draft.Accepted.Count + draft.Forfeited.Count)
                                    draft.Countdown = a2;
                            }

                            break;
                        }
                }
            }
        }

        private bool TryCreateMission(Draft draft, MapHex hex)
        {
            Character host = _characters[(int)((hex.Mission & HostMask) >> HostShift)];

            Contract.Assert(hex.Population.ContainsKey(host.Id));

            // creates the mission

            Mission mission = new Mission()
            {
                Host = host,

                Map = 0,
                Background = "space" + (hex.Id % 32).ToString("D2", CultureInfo.InvariantCulture) + ".mod",
                Speed = 8,

                Teams = new Dictionary<int, Team>(),

                Config = null,
                Cache = null
            };

            // gets the teams

            uint alliedRaces = (uint)_alliances[(int)host.CharacterRace];
            uint neutralRaces = (uint)_alliances[(int)Races.kNeutralRace];

            Contract.Assert((alliedRaces & neutralRaces) == 0);

            Queue<Character> alliedHuman = new Queue<Character>();
            Queue<Character> alliedAI = new Queue<Character>();
            Queue<Character> enemyHuman = new Queue<Character>();
            Queue<Character> enemyAI = new Queue<Character>();
            Queue<Character> neutralAI = new Queue<Character>();

            alliedHuman.Enqueue(host);

            foreach (KeyValuePair<int, object> p in hex.Population)
            {
                Character character = _characters[p.Key];

                if (character.Id != host.Id && character.Mission == host.Mission)
                {
                    uint mask = 1u << (int)character.CharacterRace;

                    if ((alliedRaces & mask) != 0)
                    {
                        if (draft.Accepted.ContainsKey(character.Id) && (character.State & Character.States.IsHumanBusyOnline) == Character.States.IsHumanBusyOnline)
                            alliedHuman.Enqueue(character);
                        else if (character.State == Character.States.IsCpuAfkBusyOnline)
                            alliedAI.Enqueue(character);
                    }
                    else if ((neutralRaces & mask) != 0)
                    {
                        if (character.State == Character.States.IsCpuAfkBusyOnline)
                            neutralAI.Enqueue(character);
                    }
                    else
                    {
                        if (draft.Accepted.ContainsKey(character.Id) && (character.State & Character.States.IsHumanBusyOnline) == Character.States.IsHumanBusyOnline)
                            enemyHuman.Enqueue(character);
                        else if (character.State == Character.States.IsCpuAfkBusyOnline)
                            enemyAI.Enqueue(character);
                    }
                }
            }

            int totalAlliedHuman = alliedHuman.Count;
            int totalAlliedAI = alliedAI.Count;
            int totalEnemyHuman = enemyHuman.Count;
            int totalEnemyAI = enemyAI.Count;
            int totalNeutralAI = neutralAI.Count;

            // checks the number of enemies

            //if (totalEnemyHuman + totalEnemyAI + totalNeutralAI == 0)
            //    goto notSupported;

            // gets the number of allied and enemy teams

            int allied = 0;
            int enemy = 0;

            // ... human teams

            while (alliedHuman.Count > 0)
                AddTeam(mission, alliedHuman.Dequeue(), TeamTag.kTagA, ref allied);

            while (enemyHuman.Count > 0)
                AddTeam(mission, enemyHuman.Dequeue(), TeamTag.kTagB, ref enemy);

            // ... AI teams

            while (alliedAI.Count > 0)
                AddTeam(mission, alliedAI.Dequeue(), TeamTag.kTagA, ref allied);

            while (enemyAI.Count > 0)
                AddTeam(mission, enemyAI.Dequeue(), TeamTag.kTagB, ref enemy);

            // ... neutral teams

            while (neutralAI.Count > 0)
                AddTeam(mission, neutralAI.Dequeue(), TeamTag.kTagC, ref enemy);

            // checks the number of teams

            if (allied + enemy > (int)TeamID.kMaxTeams)
                goto notSupported;

            // tries to create the map

            string template = GetTemplate();

            int templateWidth = template.IndexOf('\n', StringComparison.Ordinal);

            if (templateWidth < 19) // 19 positions
                throw new NotSupportedException();

            template = template.Replace("\n", "", StringComparison.Ordinal);

            int templateHeight = template.Length / templateWidth;

            if (templateHeight < 1 || templateHeight > 38) // 42 lines
                throw new NotSupportedException();

            // starting positions

            Dictionary<int, char> lc = new Dictionary<int, char>();

            allied = 97; // a
            enemy = 65; // A

            int neutral = int.MinValue;

            int bit = 1; // first bit
            int bases = 0;
            int planets = 0;

            string lastLine = string.Empty;

            foreach (KeyValuePair<int, Team> p in mission.Teams)
            {
                Character owner = _characters[p.Key];
                Team team = p.Value;
                Ship ship = _ships[owner.Ships[0]];

                char c = (char)(lc.Count + 71); // G

                if (team.Tag == TeamTag.kTagA)
                {
                    if (ship.ClassType == ClassTypes.kClassPlanets)
                    {
                        lc.Add('t', c);

                        planets |= bit;
                    }
                    else if (ship.ClassType >= ClassTypes.kClassListeningPost && ship.ClassType <= ClassTypes.kClassStarBase)
                    {
                        lc.Add('u', c);

                        bases |= bit;
                    }
                    else
                    {
                        lc.Add(allied, c);

                        allied++;
                    }
                }
                else if (ship.ClassType == ClassTypes.kClassPlanets)
                {
                    lc.Add('T', c);

                    planets |= bit;
                }
                else if (ship.ClassType >= ClassTypes.kClassListeningPost && ship.ClassType <= ClassTypes.kClassStarBase)
                {
                    lc.Add('U', c);

                    bases |= bit;
                }
                else if (team.Tag == TeamTag.kTagB)
                {
                    lc.Add(enemy, c);

                    enemy++;

                    if ((enemy & 1) == 0)
                        lastLine += char.ToLowerInvariant(c);
                    else
                        lastLine = char.ToLowerInvariant(c) + lastLine;
                }
                else
                {
                    Contract.Assert(team.Tag == TeamTag.kTagC);

                    lc.Add(neutral, c);

                    neutral++;
                }

                bit <<= 1;
            }

            for (; allied <= 122; allied++) // z
            {
                char c = (char)allied;

                if (!lc.ContainsKey(c))
                    template = template.Replace(c, '.');
            }

            for (; enemy <= 90; enemy++) // Z
            {
                char c = (char)enemy;

                if (!lc.ContainsKey(c))
                    template = template.Replace(c, '.');
            }

            foreach (KeyValuePair<int, char> p in lc)
            {
                int k = p.Key;

                if (k > 0)
                    template = template.Replace((char)k, p.Value);
                else
                    PopulateTemplate(ref template, p.Value.ToString(CultureInfo.InvariantCulture), 1);
            }

            // terrain

            GetContent(hex, out int asteroids, out int nebulas, out int blackholes, out int dustClouds, out int ionStorms, out int pulsars);

            PopulateTemplate(ref template, "[]*aA", asteroids);
            PopulateTemplate(ref template, ":,bB", blackholes);

            if (nebulas > 0)
            {
                const string nebulasS = "&?cC";

                // ... the nebula goes in the upper left corner

                Contract.Assert(template[0].Equals(' '));

                template = nebulasS[_rand.NextInt32(nebulasS.Length)] + template[1..];
            }

            PopulateTemplate(ref template, "<>dD", dustClouds);
            PopulateTemplate(ref template, "~", pulsars);
            PopulateTemplate(ref template, "{", ionStorms);

            // empty space

            template = template.Replace(' ', '.');

            // tries to create the configuration

            string ini = GetConfiguration();

            // allied

            ini = ini.Replace("%ah", totalAlliedHuman.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
            ini = ini.Replace("%aa", totalAlliedAI.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

            // enemy

            ini = ini.Replace("%eh", totalEnemyHuman.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
            ini = ini.Replace("%ea", totalEnemyAI.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

            // neutral

            ini = ini.Replace("%na", totalNeutralAI.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

            // bases and planets

            ini = ini.Replace("%b", bases.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
            ini = ini.Replace("%p", planets.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

            // map

            const char top = '_';
            const string topStart = " = +_";
            const string topEnd = "_+\n";

            const char middle = '.';
            const string middleStart = " = |.";
            const string middleEnd = ".|\n";

            const char bottom = '-';
            const string bottomStart = " = +-";
            const string bottomEnd = "-+\n";

            int line = -1;
            StringBuilder map = new StringBuilder();

            for (int i = templateWidth - lastLine.Length; i > 0; i--)
            {
                if ((i & 1) == 0)
                    lastLine += middle;
                else
                    lastLine = middle + lastLine;
            }

            // 0 = +___________________+\n

            line++;

            map.Append(line);
            map.Append(topStart);
            map.Append(top, templateWidth);
            map.Append(topEnd);

            // 1 = |...................|\n

            line++;

            map.Append(line);
            map.Append(middleStart);
            map.Append(middle, templateWidth);
            map.Append(middleEnd);

            // 2..39 = |...................|\n

            for (int i = 0; i < template.Length; i += templateWidth)
            {
                line++;

                map.Append(line);
                map.Append(middleStart);
                map.Append(template, i, templateWidth);
                map.Append(middleEnd);
            }

            // 40 = |...................|\n

            line++;

            map.Append(line);
            map.Append(middleStart);
            map.Append(lastLine);
            map.Append(middleEnd);

            // 41 = +-------------------+\n

            line++;

            map.Append(line);
            map.Append(bottomStart);
            map.Append(bottom, templateWidth);
            map.Append(bottomEnd);

            // null

            ini = ini.Replace("%ml", map.ToString(), StringComparison.Ordinal);

            map.Clear();

            /*
                const string mapToken = "%ml";

                for (int i = 0; i < template.Length; i += templateWidth)
                {
                    int j = ini.IndexOf(mapToken, StringComparison.Ordinal);

                    Contract.Assert(j >= 0);

                    string l = template.Substring(i, templateWidth);

                    ini = ini.Substring(0, j) + l + ini[(j + mapToken.Length)..];
                }
            */

            // messages

            ini = ini.Replace("%mb", "Mission Briefing " + _rand.NextUInt64().ToString("X16", CultureInfo.InvariantCulture), StringComparison.Ordinal);
            ini = ini.Replace("%md", "Mission Description " + _rand.NextUInt64().ToString("X16", CultureInfo.InvariantCulture), StringComparison.Ordinal);

            // rnd

            for (int i = 0; i < 1; i++)
            {
                string k = "%r" + i.ToString(CultureInfo.InvariantCulture);
                string v = (_rand.NextUInt32() & 0x7fffffffu).ToString(CultureInfo.InvariantCulture);

                ini = ini.Replace(k, v, StringComparison.Ordinal);
            }

            // finalizes the configuration

            mission.Config = ini;

            // mission creation sucessful!

            draft.Mission = mission;

            return true;

        notSupported:

            Contract.Assert(draft.Mission == null);

            return false;
        }

        private void AddTeam(Mission mission, Character character, TeamTag teamTag, ref int teamCount)
        {
            // gets the team id

            int teamId = mission.Teams.Count;

            // gets the team type

            TeamType teamType;

            if (teamCount == 0)
            {
                if (teamTag == TeamTag.kTagA)
                    teamType = TeamType.kPrimaryTeam;
                else
                    teamType = TeamType.kPrimaryOpponentTeam;
            }
            else
                teamType = TeamType.kPlayableTeam;

            teamCount++;

            // creates a new team

            Team team = new Team(character, (TeamID)teamId, teamType, teamTag);

            // adds the ships

            for (int i = 0; i < character.ShipCount; i++)
            {
                Ship ship = _ships[character.Ships[i]];

                team.Ships.Add(ship.Id, ship);
            }

            // adds the team

            mission.Teams.Add(character.Id, team);
        }

        private string GetTemplate()
        {
            switch (_rand.NextInt32(3))
            {
                case 0:
                    return
                        " ..................   ..\n" +
                        "..................     .\n" +
                        "..................  T  .\n" +
                        "..................     .\n" +
                        "...................   ..\n" +
                        "..   ...................\n" +
                        ".. U ..QOECABDNP........\n" +
                        "..   .SIG      FHR......\n" +
                        ".....MK..........JL.....\n" +
                        "........................\n" +
                        "........................\n" +
                        "........................\n" +
                        "........................\n" +
                        ".....mk..........jl.....\n" +
                        "......sig      fhr.   ..\n" +
                        "........qoecabdnp.. u ..\n" +
                        "..   ..............   ..\n" +
                        ".     ..................\n" +
                        ".  t  ..................\n" +
                        ".     ..................\n" +
                        "..   ...................\n"
                    ;
                case 1:
                    return
                        " .......................\n" +
                        "..........CAB......   ..\n" +
                        ".......QOE   DNP..     .\n" +
                        "......SIG....FHR..  T  .\n" +
                        ".....MK  .....JL..     .\n" +
                        "...... U ..........   ..\n" +
                        "......   ...............\n" +
                        "........................\n" +
                        "........................\n" +
                        "...............   ......\n" +
                        "..   .......... u ......\n" +
                        ".     ..mk.....  jl.....\n" +
                        ".  t  ..sig....fhr......\n" +
                        ".     ..qoe   dnp.......\n" +
                        "..   ......cab..........\n" +
                        "........................\n"
                    ;
                case 2:
                    return
                        " .........................\n" +
                        ".........QOECABDNP........\n" +
                        ".   ..SIG.........FHR.....\n" +
                        ". U .MK.............JL....\n" +
                        ".   .......   ............\n" +
                        "..........     ...........\n" +
                        "..........  T  ...........\n" +
                        "..........  t  ...........\n" +
                        "..........     ...........\n" +
                        "...........   ........   .\n" +
                        "....mk.............jl. u .\n" +
                        ".....sig.........fhr..   .\n" +
                        "........qoecabdnp.........\n" +
                        "..........................\n"
                    ;
                default:
                    throw new NotImplementedException();
            }
        }

        private void PopulateTemplate(ref string template, string objects, int count)
        {
            char[] t = template.ToCharArray();
            char[] o = objects.ToCharArray();

            for (int i = 0; i < count; i++)
            {
                int j;

                do
                    j = _rand.NextInt32(t.Length);
                while (t[j] != '.');

                int k;

                if (o.Length == 1)
                    k = 0;
                else
                    k = _rand.NextInt32(o.Length);

                t[j] = o[k];
            }

            template = new string(t);
        }

        private static void GetContent(MapHex hex, out int asteroids, out int nebulas, out int blackholes, out int dustClouds, out int ionStorms, out int pulsars)
        {
            asteroids = 0;
            nebulas = 0;
            blackholes = 0;

            dustClouds = 0;
            ionStorms = 0;
            pulsars = 0;

            if ((hex.TerrainType & TerrainTypes.kAnySpace) == TerrainTypes.kTerrainSpace1)
            {
                dustClouds += 2;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnySpace) == TerrainTypes.kTerrainSpace2)
            {
                dustClouds += 3;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnySpace) == TerrainTypes.kTerrainSpace3)
            {
                dustClouds += 5;
                ionStorms += 1;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnySpace) == TerrainTypes.kTerrainSpace4)
            {
                dustClouds += 8;
                ionStorms += 2;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnySpace) == TerrainTypes.kTerrainSpace5)
            {
                dustClouds += 12;
                ionStorms += 3;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnySpace) == TerrainTypes.kTerrainSpace6)
            {
                dustClouds += 20;
                ionStorms += 5;
            }

            if ((hex.TerrainType & TerrainTypes.kAnyAsteroids) == TerrainTypes.kTerrainAsteroids1)
            {
                asteroids += 12;

                dustClouds += 5;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnyAsteroids) == TerrainTypes.kTerrainAsteroids2)
            {
                asteroids += 20;

                dustClouds += 8;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnyAsteroids) == TerrainTypes.kTerrainAsteroids3)
            {
                asteroids += 32;

                dustClouds += 12;
            }

            if ((hex.TerrainType & TerrainTypes.kAnyNebula) == TerrainTypes.kTerrainNebula1)
            {
                nebulas += 1;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnyNebula) == TerrainTypes.kTerrainNebula2)
            {
                asteroids += 2;
                nebulas += 1;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnyNebula) == TerrainTypes.kTerrainNebula3)
            {
                asteroids += 3;
                nebulas += 1;

                ionStorms += 3;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnyNebula) == TerrainTypes.kTerrainNebula4)
            {
                asteroids += 5;
                nebulas += 1;

                ionStorms += 5;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnyNebula) == TerrainTypes.kTerrainNebula5)
            {
                asteroids += 8;
                nebulas += 1;
                blackholes += 1;

                pulsars += 1;
                ionStorms += 8;
            }
            else if ((hex.TerrainType & TerrainTypes.kAnyNebula) == TerrainTypes.kTerrainNebula6)
            {
                asteroids += 12;
                nebulas += 1;
                blackholes += 1;

                pulsars += 2;
                ionStorms += 12;
            }

            if ((hex.TerrainType & TerrainTypes.kAnyBlackHole) == TerrainTypes.kTerrainBlackHole1)
            {
                blackholes += 1;
            }
            if ((hex.TerrainType & TerrainTypes.kAnyBlackHole) == TerrainTypes.kTerrainBlackHole2)
            {
                blackholes += 1;
            }
            if ((hex.TerrainType & TerrainTypes.kAnyBlackHole) == TerrainTypes.kTerrainBlackHole3)
            {
                blackholes += 2;
            }
            if ((hex.TerrainType & TerrainTypes.kAnyBlackHole) == TerrainTypes.kTerrainBlackHole4)
            {
                blackholes += 2;

                pulsars += 3;
            }
            if ((hex.TerrainType & TerrainTypes.kAnyBlackHole) == TerrainTypes.kTerrainBlackHole5)
            {
                blackholes += 3;

                pulsars += 5;
            }
            if ((hex.TerrainType & TerrainTypes.kAnyBlackHole) == TerrainTypes.kTerrainBlackHole6)
            {
                blackholes += 3;

                pulsars += 8;
            }
        }

        private static string GetConfiguration()
        {
            return
                "TeamID = %ti\n" +
                "\n" +
                "[Teams]\n" +
                "AlliedHuman = %ah\n" +
                "AlliedAI = %aa\n" +
                "EnemyHuman = %eh\n" +
                "EnemyAI = %ea\n" +
                "NeutralAI = %na\n" +
                "Bases = %b\n" +
                "Planets = %p\n" +
                "\n" +
                "[Map/Lines]\n" +
                "%ml" +
                "\n" +
                "[Messages]\n" +
                "0 = %mb\n" +
                "1 = %md\n" +
                "\n" +
                "[Prestige]\n" +
                "AstoundingVictory = 300\n" +
                "Victory = 250\n" +
                "Draw = 0\n" +
                "Defeat = 0\n" +
                "DevastatingDefeat = 0\n" +
                "LeftEarly = 0\n" +
                "\n" +
                "[Rnd]\n" +
                "0 = %r0\n"
            ;
        }

        private void R_SendConfig(Character character, Mission mission)
        {
            // gets the text

            string ti = ((int)mission.Teams[character.Id].Id).ToString(CultureInfo.InvariantCulture);
            string fs = mission.Config.Replace("%ti", ti, StringComparison.Ordinal);

            // sends the text

            Clear();

            Push(fs); // data
            Push((byte)0x00); // opcode
            Push(Client27000.MaximumBufferSize + 4 - _position); // size

            _port27000.Enqueue(character.Client.Address, 27001, GetStack());
        }

        private void PrepareMission(Mission mission)
        {
            Character host = mission.Host;

            // initializes some data

            Stack<Team> stack = new Stack<Team>();

            int c = 0;

            foreach (KeyValuePair<int, Team> p in mission.Teams)
            {
                Team team = p.Value;

                stack.Push(team);

                if ((team.Owner.State & Character.States.IsHumanOnline) == Character.States.IsHumanOnline)
                    c++;
            }

            Contract.Assert(c > 0);

            // creates the mission cache

            Clear();

            // ?

            Push(0x00);

            // ?

            Push(0x00);

            // location range

            Push(0x00);

            // number of human players

            Push(c);

            // starting era

            Push(0x00);

            // difficulty level

            Push(0x00);

            // teams

            while (stack.Count > 0)
            {
                // gets the team and its owner

                Team team = stack.Pop();
                Character owner = team.Owner;

                // checks if the team is controlled by the CPU

                bool isHuman = (owner.State & Character.States.IsHumanOnline) == Character.States.IsHumanOnline;

                if (isHuman)
                    Push((byte)0x00);
                else
                    Push((byte)0x01);

                // eol

                Push(0x00);

                // ships

                c = owner.ShipCount;

                if (isHuman)
                {
                    for (int j = c - 1; j >= 0; j--)
                        Push(_ships[owner.Ships[j]]);
                }
                else
                {
                    Rent(4096, out byte[] b, out MemoryStream m, out BinaryWriter w, out BinaryReader r);

                    for (int j = c - 1; j >= 0; j--)
                    {
                        Ship original = _ships[owner.Ships[j]];

                        m.Seek(0, SeekOrigin.Begin);

                        original.WriteTo(w);

                        m.Seek(0, SeekOrigin.Begin);

                        Ship clone = new Ship(r);

                        UpgradeShipDamage(clone, _cpuPowerBoost);
                        UpgradeShipOfficers(clone, _cpuOfficerRank);

                        Push(clone);
                    }

                    Return(b, m, w, r);
                }

                // ship count

                Push(c);

                // team

                Push((int)team.Type);
                Push(owner.Id);
                Push((int)team.Id);
                Push(0x00); // ?
                Push(0x00); // owner.CharacterRating 
                Push((int)owner.CharacterRace);
                Push(owner.CharacterName);
            }

            // team count

            Push(mission.Teams.Count);

            // political tension matrice (primary opponent team)

            for (int j = 0x18; j >= 0; j--)
            {
                Push(0x00);
                Push(0x00);

                Push(j);
            }

            Push(0x19);
            Push(0x00);

            // political tension matrice (primary team)

            for (int j = 0x18; j >= 0; j--)
            {
                Push(0x00);
                Push(0x00);

                Push(j);
            }

            Push(0x19);
            Push(0x00);

            // allied race

            Push(0x00);

            // enemy race

            Push(0x00);

            // game speed

            Push(mission.Speed);

            // host name (shuffled)

            Push("");

            // Metaverse (shuffled)

            Push("");

            // host IP

            Push(host.IPAddress);

            // space background

            Push(mission.Background);

            // mission location (x, y)

            Push(host.CharacterLocationY);
            Push(host.CharacterLocationX);

            // stardate

            for (int i = 0; i < 9; i++)
                Push(0x00);

            // mission map

            Push(mission.Map);

            // mission title

            Push(defaultMissionTitle);

            // current time

            Push(0x00);

            // (...)

            mission.Cache = GetStack();
        }

        private static string Scramble(string text, ref long seed)
        {
            byte[] name = Encoding.UTF8.GetBytes(text + "\0");

            int count = 2;

            while (count < name.Length)
            {
                int r = Rand(ref seed);
                int d = r % count;

                r = (r / count & 0x7f00) | name[count - 1];

                name[count - 1] = name[d];

                count++;

                name[d] = (byte)(r & 255);
            }

            return Encoding.UTF8.GetString(name, 0, name.Length - 1);
        }

        private static int Rand(ref long seed)
        {
            long r = seed * 214013L + 2531011L;

            seed = r;

            return (int)(r >> 16) & 32767;
        }

        private void R_SendMission(Client27000 client, Mission mission)
        {
            /*
                [Q] 22000000 00 00000000_0f000000_03000000 0d000000 01000000 00000000 55010000 01
                [R] 7c070000 00 01000000_10000000_02000000 67070000 01 00000000_0f000000_0c000100 (...)
            */

            Clear();

            // mission cache

            Push(mission.Cache);

            // reply header

            Push(0x0c);
            Push(0x0f);
            Push(0x00);

            Push((byte)0x01);

            // this header

            int i3;

            if (client.Character.Id == mission.Host.Id)
                i3 = 2;
            else
                i3 = 3;

            Push(client.Id, client.Relay[(int)Relays.MissionRelayNameC], i3);

            Write(client);
        }

        private void R_StartMission(Client27000 client)
        {
            /*
                [Q] 26000000 00 00000000_0f000000_0c000100 11000000 01 06000000_01000000_10000000 55010000
                [R] 2a000000 00 01000000_10000000_06000000 15000000 01 00000000_0f000000_0d000100 00000000 00000000
            */

            Clear();

            Push(0x00);
            Push(0x00);

            // reply header

            Push(0x1000d);
            Push(0x0f);
            Push(0x00);

            Push((byte)0x01);

            // msg header

            Push(client.Id, client.Relay[(int)Relays.MissionRelayNameC], 0x06);

            Write(client);
        }
    }
}
