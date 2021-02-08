using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SfcOpServer
{
    public partial class GameServer
    {
        private void RunCampaign()
        {
            _clock.Start();

            _ts = 0.0;
            _t1 = 0.0;
            _t60 = 0.0;
            _tt = 0.0;

#if DEBUG
            // tries to load a savegame for debugging

            string savegameFile = null; // "New_Server_96";

            if (savegameFile != null && File.Exists(_root + savegameFile + savegameExtension))
            {
                _lastSavegame = _root + savegameFile;
                _locked = 2011;
            }
#endif

            while (true)
            {
                int msgs = 0;

                // processes the client messages

                double t = _clock.Elapsed.TotalMilliseconds;

                if (!_clients.IsEmpty)
                {
                    // gets the oldest timestamp value

                    long minTimeStamp = long.MaxValue;

                    foreach (KeyValuePair<int, Client27000> p in _clients)
                    {
                        if (p.Value.Messages.TryPeek(out ClientMessage msg) && minTimeStamp > msg.TimeStamp)
                            minTimeStamp = msg.TimeStamp;
                    }

                    // processes the client messages

                    if (minTimeStamp != long.MaxValue)
                    {
                        long maxTimeStamp = minTimeStamp + 2; // ticks

                        while (true)
                        {
                            long curTimeStamp = long.MaxValue;

                            foreach (KeyValuePair<int, Client27000> p in _clients)
                            {
                                Client27000 client = p.Value;

                                if (client.Messages.TryPeek(out ClientMessage msg))
                                {
                                    if (msg.TimeStamp == minTimeStamp)
                                    {
                                        if (!client.Messages.TryDequeue(out msg))
                                            throw new NotSupportedException();

                                        msgs++;

                                        try
                                        {
                                            // makes sure we don't ping a client that is currently sending messages

                                            client.LastActivity = t;

                                            // process the incoming message

                                            Process(client, msg.Buffer, msg.Size);
                                        }
                                        catch (Exception e)
                                        {
                                            LogError("Process()", e);
                                        }
                                        finally
                                        {
                                            Return(msg.Buffer);
                                        }
                                    }
                                    else if (msg.TimeStamp < curTimeStamp)
                                    {
                                        curTimeStamp = msg.TimeStamp;
                                    }
                                }
                            }

                            if (maxTimeStamp <= curTimeStamp)
                                break;

                            minTimeStamp = curTimeStamp;
                        }
                    }
                }

                // processes the irc messages

                t = _clock.Elapsed.TotalMilliseconds;

                while (_stream6667.TryRead(out string line))
                {
                    msgs++;

#if DEBUG
                    ProcessLine(line, t);
#else
                    try {
                        ProcessLine(line, t);
                    }
                    catch (Exception e)
                    {
                        LogError("ProcessLine()", e);
                    }
#endif
                }

                // processes the ticks

                t = _clock.Elapsed.TotalMilliseconds;

                if (t - _ts >= smallTick)
                {
                    msgs++;
                    _ts = t;

                    // system

                    _smallTicks++;

#if DEBUG
                    ProcessRequests(t);
#else
                    try
                    {
                        ProcessRequests(t);
                    }
                    catch (Exception e)
                    {
                        LogError("ProcessRequests()", e);
                    }
#endif

                    // general

#if DEBUG
                    ProcessHumanMovements();
#else
                    try
                    {
                        ProcessHumanMovements();
                    }
                    catch (Exception e)
                    {
                        LogError("ProcessHumanMovements()", e);
                    }
#endif

#if DEBUG
                    ProcessDrafts();
#else
                    try
                    {
                        ProcessDrafts();
                    }
                    catch (Exception e)
                    {
                        LogError("ProcessDrafts()", e);
                    }
#endif
                }

                t = _clock.Elapsed.TotalMilliseconds;

                if (t - _t1 >= 1_000.0)
                {
                    msgs++;
                    _t1 = t;

                    // system

                    _seconds++;

#if DEBUG
                    ProcessLoginsAndLogouts();
#else
                    try
                    {
                        ProcessLoginsAndLogouts();
                    }
                    catch (Exception e)
                    {
                        LogError("ProcessLoginsAndLogouts()", e);
                    }
#endif

                    // general

#if DEBUG
                    ProcessCpuMovements();
#else
                    try
                    {
                        ProcessCpuMovements();
                    }
                    catch (Exception e)
                    {
                        LogError("ProcessCpuMovements()", e);
                    }
#endif

                    // system

                    if (_locked > 0)
                    {
                        _locked--;

                        switch (_locked)
                        {
                            case 1000:
                                _locked = 0;
                                break;
                            case 1010:
                                SaveCampaign(t);
                                break;
                            case 1040:
                                CloseForMaintenance();
                                break;

                            case 2000:
                                _locked = 0;
                                break;
                            case 2010:
                                LoadCampaign();
                                break;
                            case 2020:
                                CloseForMaintenance();
                                break;
                        }
                    }
                }

                t = _clock.Elapsed.TotalMilliseconds;

                if (t - _t60 >= 60_000.0)
                {
                    msgs++;
                    _t60 = t;

                    // system

                    // general

#if DEBUG
                    UpdateHexOwnership();
#else
                    try
                    {
                        UpdateHexOwnership();
                    }
                    catch (Exception e)
                    {
                        LogError("UpdateHexOwnership()", e);
                    }
#endif

                    ResetHomeLocations();

#if DEBUG
                    UpdateHomeLocations();
#else
                    try
                    {
                        UpdateHomeLocations();
                    }
                    catch (Exception e)
                    {
                        LogError("UpdateHomeLocations()", e);
                    }
#endif

#if DEBUG
                    UpdateStatus();
#else
                    try
                    {
                        UpdateStatus();
                    }
                    catch (Exception e)
                    {
                        LogError("UpdateStatus()", e);
                    }
#endif
                }

                t = _clock.Elapsed.TotalMilliseconds;

                if (t - _tt >= _millisecondsPerTurn)
                {
                    msgs++;
                    _tt = t;

                    // system

                    _turn++;

                    // general

#if DEBUG
                    ProcessBids();
#else
                    try
                    {
                        ProcessBids();
                    }
                    catch (Exception e)
                    {
                        LogError("ProcessBids()", e);
                    }
#endif

#if DEBUG
                    CalculateMaintenance();
#else
                    try
                    {
                        CalculateMaintenance();
                    }
                    catch (Exception e)
                    {
                        LogError("CalculateMaintenance()", e);
                    }
#endif

#if DEBUG
                    CalculateProduction();
#else
                    try
                    {
                        CalculateProduction();
                    }
                    catch (Exception e)
                    {
                        LogError("CalculateProduction()", e);
                    }
#endif

                    if (_turn % _turnsPerYear == 0)
                    {
#if DEBUG
                        ClearShipyard();
#else
                        try
                        {
                            ClearShipyard();
                        }
                        catch (Exception e)
                        {
                            LogError("ClearShipyard()", e);
                        }
#endif

#if DEBUG
                        CreateShipyard();
#else
                        try
                        {
                            CreateShipyard();
                        }
                        catch (Exception e)
                        {
                            LogError("CreateShipyard()", e);
                        }
#endif

#if DEBUG
                        CalculateBudget();
#else
                        try
                        {
                            CalculateBudget();
                        }
                        catch (Exception e)
                        {
                            LogError("CalculateBudget()", e);
                        }
#endif
                    }
                }

                // does a small pause if nothing was processed during the loop

                if (msgs == 0)
                    TimerHelper.SleepForNoMoreThanCurrentResolution();
            }
        }

        private static void LogError(string source, Exception e)
        {
            StringBuilder t = new StringBuilder(2048);

            t.Append("ERROR: ");
            t.Append(source);
            t.Append(" -> ");
            t.Append(e.Message);
            t.AppendLine();
            t.Append(e.StackTrace);
            t.AppendLine();

            Console.WriteLine(t.ToString());
        }
    }
}
