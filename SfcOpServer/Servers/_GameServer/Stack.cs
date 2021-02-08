using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace SfcOpServer
{
    public partial class GameServer
    {
        // structures

        private void Push(Character character, BidItem item, int version)
        {
            Contract.Assert(version >= 0 && version <= 1);

            switch (version)
            {
                case 0:
                    Push(0x00);
                    Push(0x00);
                    Push(0x00);
                    Push(item.CurrentBid);
                    Push(0x00);
                    Push(0x00);
                    Push(item.AuctionRate);
                    Push(item.AuctionValue);
                    Push(item.ShipBPV);
                    Push(item.ShipId);
                    Push(item.ShipClassName);
                    Push((byte)0x00);
                    Push(0x00);
                    Push(0x00);

                    break;

                case 1:
                    if (character.Id == item.BidOwnerID)
                        Push(item.BidMaximum);
                    else
                        Push(0x00);

                    Push(item.TurnBidMade);
                    Push(item.BidOwnerID);
                    Push(item.CurrentBid);
                    Push(item.TurnToClose);
                    Push(item.TurnOpened);
                    Push(item.AuctionRate);
                    Push(item.AuctionValue);
                    Push(item.ShipBPV);
                    Push(item.ShipId);
                    Push(item.ShipClassName);
                    Push(item.BiddingHasBegun);
                    Push(item.LockID);
                    Push(item.Id);

                    break;
            }
        }

        private void Push(Character character)
        {
            if (character.ShipCache == null)
                Push(0x00);
            else
                Push(character.ShipCache);

            Push(character.ShipCount);
            Push(character.MoveDestinationY);
            Push(character.MoveDestinationX);
            Push(character.HomeWorldLocationY);
            Push(character.HomeWorldLocationX);
            Push(character.CharacterLocationY);
            Push(character.CharacterLocationX);
            Push(character.Unknown);
            Push(character.CharacterLifetimePrestige);
            Push(character.CharacterCurrentPrestige);
            Push(character.CharacterRating);
            Push((int)character.CharacterRank);
            Push((int)character.CharacterPoliticalControl);
            Push((int)character.CharacterRace);
            Push(character.CharacterName);
            Push(character.Id);
            Push(character.WONLogon);
            Push(character.IPAddress);
        }

        private void Push(MapHex hex, int version)
        {
            Contract.Assert(version >= 0 && version <= 1);

            switch (version)
            {
                case 0:
                    Push((byte)(hex.CurrentSpeedPoints * 100.0));

                    Push((byte)hex.CurrentEconomicPoints);

                    Push((byte)hex.CartelCurrentVictoryPoints);
                    Push((byte)hex.EmpireCurrentVictoryPoints);

                    Push((byte)hex.Base);
                    Push((byte)hex.Planet);
                    Push((int)hex.TerrainType);

                    Push((byte)hex.CartelControl);
                    Push((byte)hex.EmpireControl);

                    break;

                case 1:
                    Push(hex.CurrentSpeedPoints);
                    Push(hex.BaseSpeedPoints);

                    Push(hex.CartelCurrentVictoryPoints);
                    Push(0x01);
                    Push(hex.EmpireCurrentVictoryPoints);
                    Push(0x00);
                    Push(0x02);

                    Push(hex.CartelBaseVictoryPoints);
                    Push(0x01);
                    Push(hex.EmpireBaseVictoryPoints);
                    Push(0x00);
                    Push(0x02);

                    Push(hex.CurrentEconomicPoints);
                    Push(hex.BaseEconomicPoints);

                    Push((int)hex.BaseType);
                    Push((int)hex.PlanetType);
                    Push((int)hex.TerrainType);

                    Push((int)hex.CartelControl);
                    Push(0x01);
                    Push((int)hex.EmpireControl);
                    Push(0x00);
                    Push(0x02);

                    Push(hex.Y);
                    Push(hex.X);

                    break;
            }
        }

        private void Push(Ship ship)
        {
            Push(ship.Flags);

            // officers

            for (int i = (int)OfficerRoles.kMaxOfficers - 1; i >= 0; i--)
            {
                ref Officer officer = ref ship.Officers.Items[i];

                Push(officer.Unknown2);
                Push(officer.Unknown1);
                Push((int)officer.Rank);
                Push(officer.Name);
            }

            // stores

            ShipStores stores = ship.Stores;

            for (int i = 3; i >= 0; i--)
            {
                ref FighterBay fighterBay = ref stores.FighterBays[i];

                Push(fighterBay.FighterSubType);
                Push(fighterBay.FighterType);

                Push(fighterBay.Unknown);
                Push(fighterBay.FightersMax);
                Push(fighterBay.FightersLoaded);
                Push(fighterBay.FightersCount);
            }

            Push(stores.DamageControl);
            Push(stores.DamageControlBase);
            Push(stores.DamageControlMax);

            Push(stores.TBombs);
            Push(stores.TBombsBase);
            Push(stores.TBombsMax);

            Push(stores.BoardingParties);
            Push(stores.BoardingPartiesBase);
            Push(stores.BoardingPartiesMax);

            Push(stores.Unknown4);

            if (stores.TransportItems == null)
                Push(0);
            else
            {
                for (int i = stores.TransportItems.Length - 1; i >= 0; i--)
                {
                    ref TransportItem transportItem = ref stores.TransportItems[i];

                    Push(transportItem.Count);
                    Push((int)transportItem.Item);
                }

                Push(stores.TransportItems.Length);
            }

            Push(stores.Unknown3);

            Push(stores.General);
            Push(stores.GeneralBase);
            Push(stores.GeneralMax);

            for (int i = 24; i >= 0; i--)
            {
                ref MissileHardpoint missileHardpoint = ref stores.MissileHardpoints[i];

                Push(missileHardpoint.TubesCapacity);
                Push(missileHardpoint.TubesCount);
                Push(missileHardpoint.MissilesStored);
                Push(missileHardpoint.MissilesReady);
            }

            Push(stores.TotalMissilesStored);
            Push(stores.TotalMissilesReady);
            Push(stores.TotalMissilesReadyAndStored);
            Push(stores.TotalTubesCount);

            Push(stores.MissilesReloads);
            Push((byte)stores.MissilesDriveSystem);
            Push((byte)stores.MissilesType);

            Push(stores.Unknown2);
            Push(stores.Unknown1);

            // damage

            Push(ship.Damage.Items);

            Push(ship.TurnCreated);
            Push(ship.Name);
            Push(ship.ShipClassName);
            Push(ship.EPV);
            Push(ship.BPV);
            Push((int)ship.ClassType);
            Push((int)ship.Race);
            Push(ship.IsInAuction);
            Push(ship.OwnerID);
            Push(ship.LockID);
            Push(ship.Id);
        }

        // simple operations

        private void Clear()
        {
            _position = Client27000.MaximumBufferSize;
        }

        private void Push(byte value)
        {
            _position--;

            _buffer[_position] = value;
        }

        private unsafe void Push(short value)
        {
            _position -= 2;

            fixed (byte* b0 = &_buffer[_position])
            {
                *(short*)b0 = value;
            }
        }

        private unsafe void Push(int value)
        {
            _position -= 4;

            fixed (byte* b0 = &_buffer[_position])
            {
                *(int*)b0 = value;
            }
        }

        private unsafe void Push(double value)
        {
            _position -= 8;

            fixed (byte* b0 = &_buffer[_position])
            {
                *(double*)b0 = value;
            }
        }

        private void Push(byte[] value)
        {
            Contract.Assert(value.Length > 0);

            _position -= value.Length;

            Buffer.BlockCopy(value, 0, _buffer, _position, value.Length);
        }

        private void Push(string value)
        {
            int c = value.Length;

            if (c != 0)
            {
                _position -= c;

                Encoding.UTF8.GetBytes(value, 0, c, _buffer, _position);
            }

            Push(c);
        }

        private unsafe void Push(int i1, int i2, int i3)
        {
            int c = Client27000.MaximumBufferSize - _position;

            Contract.Assert(c >= 0);

            _position -= 21;

            fixed (byte* b0 = &_buffer[_position])
            {
                *(int*)b0 = c + 21;

                b0[4] = 0;

                *(int*)(b0 + 5) = i1;
                *(int*)(b0 + 9) = i2;
                *(int*)(b0 + 13) = i3;

                *(int*)(b0 + 17) = c;
            }
        }

        private unsafe void Push(int i1, int i2, int i3, int opcode, byte flag, int info)
        {
            _position -= 30;

            fixed (byte* b0 = &_buffer[_position])
            {
                *(int*)b0 = 30;

                b0[4] = 0;

                *(int*)(b0 + 5) = i1;
                *(int*)(b0 + 9) = i2;
                *(int*)(b0 + 13) = i3;

                *(int*)(b0 + 17) = 9;

                *(int*)(b0 + 21) = opcode;

                b0[25] = flag;

                *(int*)(b0 + 26) = info;
            }
        }

        private byte[] GetStack()
        {
            int c = Client27000.MaximumBufferSize - _position;

#if DEBUG
            Contract.Assert(c > 0);
#endif

            byte[] b = new byte[c];

            Array.Copy(_buffer, _position, b, 0, c);

            return b;
        }

        private void Write(Client27000 client)
        {
            _server27000.Write(client, _buffer, _position, Client27000.MaximumBufferSize - _position);
        }

        // complex operations

        private void BroadcastHex(int mapIndex)
        {
            Contract.Assert(mapIndex >= 0 && mapIndex < _map.Length);

            foreach (KeyValuePair<int, Client27000> p in _clients)
            {
                Client27000 client = p.Value;

                if (client.HexRequest == -1)
                    client.HexRequest = mapIndex;
            }
        }

        private void BroadcastIcons()
        {
            foreach (KeyValuePair<int, Client27000> p in _clients)
            {
                Client27000 client = p.Value;

                if (client.IconsRequest == 0)
                    client.IconsRequest = 1;
            }
        }

        private void Write(Client27000 client, Relays relay, int i3, int opcode, byte flag, int info)
        {
            Contract.Assert(client.Relay[(int)relay] != -1);

            Clear();

            Push(client.Id, client.Relay[(int)relay], i3, opcode, flag, info);

            Write(client);
        }

        private void ProcessRequests(double elapsedMilliseconds)
        {
            foreach (KeyValuePair<int, Client27000> p in _clients)
            {
                Client27000 client = p.Value;
                Character character = client.Character;

                if (character == null)
                    continue;

                if ((character.State & Character.States.IsBusy) != Character.States.IsBusy)
                {
                    if (client.LastTurn != _turn)
                    {
                        client.LastTurn = _turn;

                        M_Turn(client);
                    }
                    else if (client.LastActivity - elapsedMilliseconds <= -60_000.0)
                    {
                        client.LastActivity = elapsedMilliseconds;

                        M_Ping(client);
                    }

                    if (client.HexRequest >= 0)
                    {
                        int mapIndex = client.HexRequest;

                        client.HexRequest = -2;

                        Write(client, Relays.MetaViewPortHandlerNameC, 0x03, 0x02, 0x00, mapIndex); // D_3
                    }
                    else if (client.IconsRequest == 1)
                    {
                        client.IconsRequest = 2;

                        Write(client, Relays.MetaViewPortHandlerNameC, 0x06, 0x00, 0x0f, 0x00); // 15_F
                    }

                    // tries to inform about the clients that logged in and out

                    Dictionary<int, int> idList = client.IdList;

                    if (idList.Count == _clients.Count)
                        continue;

                    foreach (KeyValuePair<int, Client27000> q in _clients)
                    {
                        Client27000 otherClient = q.Value;

                        if (otherClient.Id != client.Id)
                        {
                            Character otherCharacter = otherClient.Character;

                            if (otherCharacter != null && (otherCharacter.State & Character.States.IsHumanOnline) == Character.States.IsHumanOnline && idList.TryAdd(otherClient.Id, otherCharacter.Id))
                            {
                                //if (isFirstTime)
                                //    Write(target, Relays.MetaClientNewsPanel, 0x03, 0x03, 0x00, 0x00); // 10_2

                                Write(client, Relays.MetaClientPlayerListPanel, 0x02, 0x00, 0x00, otherCharacter.Id); // 15_8
                            }
                        }
                    }

                    foreach (KeyValuePair<int, int> q in idList)
                    {
                        int otherClientId = q.Key;

                        if (otherClientId != client.Id && !_clients.ContainsKey(otherClientId))
                        {
                            int otherCharacterId = idList[otherClientId];

                            idList.Remove(otherClientId);

                            Write(client, Relays.MetaClientPlayerListPanel, 0x03, 0x00, 0x01, otherCharacterId); // nothing
                        }
                    }
                }
                else if ((character.State & Character.States.IsAfk) == Character.States.IsAfk)
                {
                    if (client.LastActivity - elapsedMilliseconds <= -60_000.0)
                    {
                        client.LastActivity = elapsedMilliseconds;

                        M_Ping(client);
                    }
                }
            }
        }

        private bool TryPushCharacterAndHex(int characterId)
        {
            if (_characters.TryGetValue(characterId, out Character character) && (character.State & Character.States.IsHumanOnline) == Character.States.IsHumanOnline)
            {
                PushCharacterAndHex(character);

                return true;
            }

            return false;
        }

        private void PushCharacterAndHex(Character character)
        {
            MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

            Push((byte)0x00);
            Push(0x01);

            Push(hex, 1);
            Push(0x00);
            Push(hex.Id);

            Push(character);
        }

        private static void FilterIcons(int bestId, int bestBPV, int[] id, int[] bpv, int offset)
        {
            int i = offset;

            offset += 3;

            while (i < offset)
            {
                if (bpv[i] < bestBPV)
                {
                    id[i + 2] = id[i + 1];
                    bpv[i + 2] = bpv[i + 1];

                    id[i + 1] = id[i];
                    bpv[i + 1] = bpv[i];

                    id[i] = bestId;
                    bpv[i] = bestBPV;

                    break;
                }

                i++;
            }
        }

        private void PushIcons(int[] id, int offset, int count)
        {
            Contract.Assert(count > 0);

            do
            {
                Contract.Assert(_ships.ContainsKey(id[offset]));

                Ship ship = _ships[id[offset]];

                offset++;
                count--;

                Character target = _characters[ship.OwnerID];

                PushIcon(target, ship, 0x00); // right top
            }
            while (count > 0);
        }

        private void PushIcon(Character character, Ship ship, byte location)
        {
            int icon = _classTypeIcons[(int)ship.ClassType];

            Contract.Assert(icon != -1);

            Push((int)character.CharacterRace);
            Push(location);
            Push(icon);
            Push(character.CharacterLocationY);
            Push(character.CharacterLocationX);
            Push(ship.Id);
            Push(character.Id);
        }

        private void PushStardate()
        {
            int earlyYears;
            int baseYear;

            if (_earlyYears >= 0)
            {
                earlyYears = _earlyYears;
                baseYear = referenceYear;
            }
            else
            {
                earlyYears = 0;
                baseYear = _earlyYears + referenceYear;
            }

            Push(_advancedYears);
            Push(_lateYears);
            Push(_middleYears);
            Push(earlyYears);
            Push(baseYear);

            Push(_millisecondsPerTurn);
            Push(_turnsPerYear);
            Push(0x00); // ?

            Push(_turn);
        }
    }
}
