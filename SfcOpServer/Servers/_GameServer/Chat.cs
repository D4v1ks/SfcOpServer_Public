using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace SfcOpServer
{
    public partial class GameServer
    {
        private void JoinIrcServer()
        {
            StringBuilder s = new StringBuilder(1024);

            // does the handshake

            s.Append("NICK ");
            s.Append(_serverNick);

            WriteLine(s);

            s.Append("USER ");
            s.Append(_serverNick.ToLowerInvariant());
            s.Append("@fake.net 127.0.0.1 ");
            s.Append(_localEP.Address.ToString());
            s.Append(" :");
            s.Append(_serverNick);

            WriteLine(s);

            // adds the server to the irc's whitelist

            s.Append("MODE ");
            s.Append(_serverNick);
            s.Append(" +w");

            WriteLine(s);

            // joins the first channel

            s.Append("JOIN ");
            s.Append(_channels[1]);

            WriteLine(s);

            // then joins all the others

            for (int i = 2; i < _channels.Length; i++)
            {
                s.Append("JOIN ");
                s.Append(_channels[i]);
                s.Append(_channels[0]);

                WriteLine(s);
            }
        }

        private void WriteLine(StringBuilder msg)
        {
            msg.AppendLine();

            string t = msg.ToString();

            msg.Clear();

#if VERBOSE
            Debug.Write(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Client" + _stream6667.Id + "] " + t);
#endif

            IrcService.Enqueue(_stream6667.Id, Encoding.UTF8.GetBytes(t), t.Length);
        }

        private void ProcessLine(string line, double t)
        {
            if (line == null)
                return;

#if VERBOSE
            Debug.Write(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " [Server" + _stream6667.Id + "] " + line);
#endif

            if (line.Length >= 36)
            {
                Character source;
                IrcClient client;

                /*
                    :D4v1ks3074930439!d4v1ks@192.168.1.71 JOIN :#ServerBroadcast@New_Server
                    :*0123456789!*@*.*.*.* JOIN :#ServerBroadcast@*
                */

                int i = line.IndexOf(" JOIN :#ServerBroadcast@", 22, StringComparison.Ordinal);

                if (i >= 0)
                {
                    if (_hostId == null && !line.StartsWith(":" + _serverNick + "!", StringComparison.Ordinal))
                    {
                        int j = line.IndexOf('!', StringComparison.Ordinal);

                        if (j <= 0 || j >= i)
                            throw new NotSupportedException();

                        _hostId = line.Substring(j - 10, 10);

                        _administrator = line[1..j];
                    }
                }

                /*
                    :D4v1ks3074930439!c41c@192.168.1.71 NOTICE #ServerBroadcast@New_Server :AfkOn
                    :*0123456789!*@*.*.*.* NOTICE #*@* :AfkOn\r\n

                    :D4v1ks3074930439!c427@192.168.1.71 NOTICE #ServerBroadcast@New_Server :AfkOff
                    :*0123456789!*@*.*.*.* NOTICE #*@* :AfkOff\r\n
                */

                i = line.IndexOf(" NOTICE #ServerBroadcast@", 22, StringComparison.Ordinal);

                if (i >= 0)
                {
                    if (line.IndexOf(":AfkOn", 35, StringComparison.Ordinal) >= 0)
                    {
                        if (TryGetCharacter(line, i, out source, out client))
                            source.State |= Character.States.IsAfk;
                    }
                    else if (line.IndexOf(":AfkOff", 35, StringComparison.Ordinal) >= 0)
                    {
                        if (TryGetCharacter(line, i, out source, out client))
                            source.State &= ~Character.States.IsAfk;
                    }

                    return;
                }

                /*
                    :D4v1ks3074930439!e00e@192.168.1.71 PRIVMSG #General@New_Server :!scan
                    :*0123456789!*@*.*.*.* PRIVMSG #*@* :!*
                */

                i = line.IndexOf(" PRIVMSG #", 22, StringComparison.Ordinal);

                if (i >= 0)
                {
                    int j = line.IndexOf(":!", 36, StringComparison.Ordinal);

                    if (j >= 0 && TryGetCharacter(line, i, out source, out client))
                    {
                        string cmd = line.Substring(j + 2, line.Length - j - 4);

                        if (cmd.Length == 0)
                            return;

                        string[] a = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        if (a.Length < 1)
                            return;

                        // initialize

                        StringBuilder msg = new StringBuilder(1024);

                        // process command

                        if (a[0].Equals('s') || a[0].Equals("scan", StringComparison.Ordinal))
                        {
                            int locationX;
                            int locationY;

                            switch (a.Length)
                            {
                                case 1:
                                    locationX = source.CharacterLocationX;
                                    locationY = source.CharacterLocationY;

                                    break;

                                case 3:
                                    if (!int.TryParse(a[1], NumberStyles.None, CultureInfo.InvariantCulture, out locationX) || !int.TryParse(a[2], NumberStyles.None, CultureInfo.InvariantCulture, out locationY) || locationX < 0 || locationY < 0 || locationX >= _mapWidth || locationY >= _mapHeight || !MovementValid(source.CharacterLocationX, source.CharacterLocationY, locationX, locationY))
                                        return;

                                    break;

                                default:
                                    return;
                            }

                            CmdScan(source.Id, client.Nick, locationX, locationY, msg);
                        }
                        else if (a[0].Equals('t') || a[0].Equals("turn", StringComparison.Ordinal))
                        {
                            if (a.Length == 1)
                            {
                                double timeRemaining = _tt + _millisecondsPerTurn - t;

                                if (timeRemaining > 0.0)
                                    timeRemaining = Math.Round(timeRemaining / 1000.0, MidpointRounding.AwayFromZero);
                                else
                                    timeRemaining = 0.0;

                                msg.Append(":Computer PRIVMSG ");
                                msg.Append(client.Nick);
                                msg.Append(" :");
                                msg.Append(timeRemaining);
                                msg.Append(" seconds until next turn");
                                msg.AppendLine();
                            }
                        }
                        else if (client.Nick.Equals(_administrator, StringComparison.Ordinal))
                        {
                            if (a[0].Equals("add", StringComparison.Ordinal))
                            {
                                ShipData data = null;

                                switch (a.Length)
                                {
                                    case 2:
                                        if (source.ShipCount < 3 && _shiplist.TryGetValue(a[1], out data))
                                        {
                                            CreateShip(data, out Ship ship);

                                            ModifyShip(ship, source.CharacterRace);
                                            RefreshShip(ship);

                                            UpdateCharacter(source, ship);
                                            RefreshCharacter(source);

                                            Write(source.Client, Relays.PlayerRelayC, 0x04, 0x00, 0x06, source.Id); // A_5
                                            Write(source.Client, Relays.PlayerRelayC, 0x06, 0x00, 0x08, source.Id); // 15_13
                                        }
                                        break;

                                    case 4:
                                        if (int.TryParse(a[1], NumberStyles.None, CultureInfo.InvariantCulture, out int race) && race >= (int)Races.kFederation && race <= (int)Races.kOrion && _shiplist.TryGetValue(a[2], out data))
                                        {
                                            bool remove = false;

                                            if (a[3].Equals("-m", StringComparison.Ordinal))
                                                remove = true;
                                            else if (!a[3].Equals("+m", StringComparison.Ordinal))
                                                return;

                                            CreateShip(data, out Ship ship);
                                            CreateCharacter((Races)race, source.CharacterLocationX, source.CharacterLocationY, ship, out Character character);

                                            if (remove)
                                                _cpuMovements.Remove(character.Id);

                                            source.Client.IconsRequest = 1;
                                        }
                                        break;
                                }
                            }
                            else if (a[0].Equals("medals", StringComparison.Ordinal))
                            {
                                if (a.Length == 2 && int.TryParse(a[1], NumberStyles.None, CultureInfo.InvariantCulture, out int medals) && medals >= (int)Medals.kNoMedals && medals <= (int)Medals.kAllMedals)
                                {
                                    source.Awards = (Medals)medals;

                                    Write(source.Client, Relays.PlayerRelayC, 0x08, 0x00, 0x0c, source.Id);
                                }
                            }
                            else if (a[0].Equals("prestige", StringComparison.Ordinal))
                            {
                                if (a.Length == 2 && int.TryParse(a[1], NumberStyles.None, CultureInfo.InvariantCulture, out int prestige) && prestige >= 0 && prestige <= 1_000_000_000)
                                {
                                    source.CharacterCurrentPrestige = prestige;

                                    if (source.CharacterLifetimePrestige < prestige)
                                        source.CharacterLifetimePrestige = prestige;

                                    Write(source.Client, Relays.PlayerRelayC, 0x05, 0x00, 0x07, source.Id);
                                }
                            }
                            else if (a[0].Equals("rank", StringComparison.Ordinal))
                            {
                                if (a.Length == 2 && int.TryParse(a[1], NumberStyles.None, CultureInfo.InvariantCulture, out int rank) && rank > (int)Ranks.None && rank < (int)Ranks.Total)
                                {
                                    source.CharacterRank = (Ranks)rank;

                                    Write(source.Client, Relays.PlayerRelayC, 0x07, 0x00, 0x0b, source.Id);
                                }
                            }
                            else if (a[0].Equals("save", StringComparison.Ordinal))
                            {
                                if (_locked == 0)
                                {
                                    const int lockDelay = 1060;

                                    switch (a.Length)
                                    {
                                        case 1:
                                            _locked = lockDelay;
                                            _lastSavegame = _root + _hostName + "_" + _turn;

                                            MsgMaintenance(msg);

                                            break;
                                    }
                                }
                            }
                            else if (a[0].Equals("load", StringComparison.Ordinal))
                            {
                                if (_locked == 0)
                                {
                                    const int lockDelay = 2030;

                                    switch (a.Length)
                                    {
                                        case 1:
                                            if (File.Exists(_lastSavegame + savegameExtension))
                                            {
                                                _locked = lockDelay;

                                                MsgMaintenance(msg);
                                            }
                                            break;

                                        case 2:
                                            if (File.Exists(_root + a[1] + savegameExtension))
                                            {
                                                _locked = lockDelay;
                                                _lastSavegame = _root + a[1];

                                                MsgMaintenance(msg);
                                            }
                                            break;
                                    }
                                }
                            }
                        }

                        // finalize

                        if (msg.Length > 0)
                            IrcService.Write(client, msg);
                    }

                    return;
                }
            }
            else if (line.StartsWith("PING :", StringComparison.Ordinal))
            {
                StringBuilder msg = new StringBuilder(1024);

                msg.Append("PO");
                msg.Append(line, 2, line.Length - 4);

                WriteLine(msg);
            }
        }

        private bool TryGetCharacter(string line, int limit, out Character character, out IrcClient client)
        {
            // :*0123456789!*@*.*.*.* PRIVMSG #*@* :!*

            int i = line.IndexOf(_hostId, 2, StringComparison.Ordinal);

            if (i >= 0 && i < limit && _characterNames.TryGetValue(line[1..i], out int id) && _characters.TryGetValue(id, out character) && IrcService.TryGetClient(line[1..(i + 10)], out client))
                return true;

            character = null;
            client = null;

            return false;
        }

        private void CmdScan(int sourceId, string nick, int locationX, int locationY, StringBuilder msg)
        {
            StringBuilder arg = new StringBuilder(1024);
            StringBuilder line = new StringBuilder(1024);

            foreach (KeyValuePair<int, object> p in _map[locationX + locationY * _mapWidth].Population)
            {
                Character character = _characters[p.Key];

                if (character.Id != sourceId && (character.State & (Character.States.IsAfk | Character.States.IsBusy)) == Character.States.None)
                {
                    for (int i = 0; i < character.ShipCount; i++)
                    {
                        Ship ship = _ships[character.Ships[i]];

                        arg.Append(ship.ShipClassName);
                        arg.Append('(');
                        arg.Append(ship.BPV);
                        arg.Append(')');

                        if ((character.State & Character.States.IsHuman) == Character.States.IsHuman)
                            arg.Append('*');

                        arg.Append("; ");

                        if (line.Length + arg.Length >= 80)
                        {
                            MsgScan(msg, nick, line);

                            line.Clear();
                        }

                        line.Append(arg);

                        arg.Clear();
                    }
                }
            }

            if (line.Length > 0)
            {
                MsgScan(msg, nick, line);
            }
            else if (msg.Length == 0)
            {
                line.Append("interference detected");

                MsgScan(msg, nick, line);
            }
        }

        private void MsgMaintenance(StringBuilder msg)
        {
            msg.Append("PRIVMSG ");
            msg.Append(_channels[3]);
            msg.Append(_channels[0]);
            msg.Append(" :The server is closing for maintenance. It will only take a few seconds.");

            WriteLine(msg);
        }

        private static void MsgScan(StringBuilder msg, string nick, StringBuilder line)
        {
            msg.Append(":S.R.S PRIVMSG ");
            msg.Append(nick);
            msg.Append(" :");
            msg.Append(line);
            msg.AppendLine();
        }
    }
}
