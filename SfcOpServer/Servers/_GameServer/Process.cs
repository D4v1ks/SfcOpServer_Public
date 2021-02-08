#pragma warning disable IDE1006

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Text;

namespace SfcOpServer
{
    public partial class GameServer
    {
        // private enumerations

        private enum Data
        {
            // 01x00

            CharacterLogOnRelayNameC,
            MessengerRelayNameC,
            MetaViewPortHandlerNameC,
            MissionRelayNameC,
            PlayerRelayC,

            MedalsPanel,
            MetaClientChatPanel,
            MetaClientHelpListPanel,
            MetaClientMissionPanel,
            MetaClientNewsPanel,
            MetaClientPlayerListPanel,
            MetaClientShipPanel,
            MetaClientSupplyDockPanel,
            PlayerInfoPanel,

            // strings

            AVtCharacterRelayS,
            AVtChatRelayS,
            AVtClockRelayS,
            AVtDataValidatorS,
            AVtEconomyRelayS,
            // AVtInfoRelayS
            AVtMapRelayS,
            // AVtMessengerRelayS
            AVtMissionMatcherRelayS,
            AVtNewsRelayS,
            AVtNotifyRelayS,
            AVtSecurityRelayS,
            AVtShipRelayS,

            SuccessfulSecurityCheck,

            // other

            master5_0,
            master5_1,
            master6,

            pingRequest,
            shipyardFiller,
            bidItemNull,

            Total
        }

        // private static variables

        private static readonly byte[][] _data = new byte[][]
        {
            new byte[] { 67, 104, 97, 114, 97, 99, 116, 101, 114, 76, 111, 103, 79, 110, 82, 101, 108, 97, 121, 78, 97, 109, 101, 67 },
            new byte[] { 77, 101, 115, 115, 101, 110, 103, 101, 114, 82, 101, 108, 97, 121, 78, 97, 109, 101, 67 },
            new byte[] { 77, 101, 116, 97, 86, 105, 101, 119, 80, 111, 114, 116, 72, 97, 110, 100, 108, 101, 114, 78, 97, 109, 101, 67 },
            new byte[] { 77, 105, 115, 115, 105, 111, 110, 82, 101, 108, 97, 121, 78, 97, 109, 101, 67 },
            new byte[] { 80, 108, 97, 121, 101, 114, 82, 101, 108, 97, 121, 67 },
            new byte[] { 77, 101, 100, 97, 108, 115, 80, 97, 110, 101, 108 },
            new byte[] { 77, 101, 116, 97, 67, 108, 105, 101, 110, 116, 67, 104, 97, 116, 80, 97, 110, 101, 108 },
            new byte[] { 77, 101, 116, 97, 67, 108, 105, 101, 110, 116, 72, 101, 108, 112, 76, 105, 115, 116, 80, 97, 110, 101, 108 },
            new byte[] { 77, 101, 116, 97, 67, 108, 105, 101, 110, 116, 77, 105, 115, 115, 105, 111, 110, 80, 97, 110, 101, 108 },
            new byte[] { 77, 101, 116, 97, 67, 108, 105, 101, 110, 116, 78, 101, 119, 115, 80, 97, 110, 101, 108 },
            new byte[] { 77, 101, 116, 97, 67, 108, 105, 101, 110, 116, 80, 108, 97, 121, 101, 114, 76, 105, 115, 116, 80, 97, 110, 101, 108 },
            new byte[] { 77, 101, 116, 97, 67, 108, 105, 101, 110, 116, 83, 104, 105, 112, 80, 97, 110, 101, 108 },
            new byte[] { 77, 101, 116, 97, 67, 108, 105, 101, 110, 116, 83, 117, 112, 112, 108, 121, 68, 111, 99, 107, 80, 97, 110, 101, 108 },
            new byte[] { 80, 108, 97, 121, 101, 114, 73, 110, 102, 111, 80, 97, 110, 101, 108 },
            new byte[] { 34, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 67, 104, 97, 114, 97, 99, 116, 101, 114, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 29, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 67, 104, 97, 116, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 30, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 67, 108, 111, 99, 107, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 33, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 68, 97, 116, 97, 86, 97, 108, 105, 100, 97, 116, 111, 114, 83, 64, 64 },
            new byte[] { 32, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 69, 99, 111, 110, 111, 109, 121, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 28, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 77, 97, 112, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 39, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 77, 105, 115, 115, 105, 111, 110, 77, 97, 116, 99, 104, 101, 114, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 29, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 78, 101, 119, 115, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 31, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 78, 111, 116, 105, 102, 121, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 33, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 83, 101, 99, 117, 114, 105, 116, 121, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 29, 0, 0, 0, 32, 42, 126, 83, 101, 114, 118, 101, 114, 126, 42, 32, 46, 63, 65, 86, 116, 83, 104, 105, 112, 82, 101, 108, 97, 121, 83, 64, 64 },
            new byte[] { 25, 0, 0, 0, 83, 117, 99, 99, 101, 115, 115, 102, 117, 108, 32, 115, 101, 99, 117, 114, 105, 116, 121, 32, 99, 104, 101, 99, 107 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 220, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 220, 5, 0, 0, 50, 0, 0, 0, 50, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0 },
            new byte[] { 29, 0, 0, 0, 0, 255, 255, 255, 255, 0, 0, 0, 0, 4, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 78, 47, 65, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 255, 255, 255, 127, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 240, 63, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        };

        private static readonly byte[] i2 = new byte[]
        {
            255, 0, 255, 255, 1, 2, 3, 255,
            255, 255, 4, 255, 155, 5, 255, 6,
            7, 255, 255, 8, 255, 9, 10, 255,
            11, 255, 255, 255, 255, 255, 255, 255
        };

        private static readonly byte[] i3 = new byte[]
        {
            0, 1, 2, 3, 4, 5, 6, 7,
            8, 255, 255, 255, 9, 255, 255, 10,
            11, 255, 12, 13, 14, 15, 255, 255
        };

        private static readonly byte[] i23 = new byte[]
        {
            0, 1, 2, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 3, 255, 255, 4, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 5, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 6, 7, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 255, 255, 8, 9, 10, 11, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 12, 13, 255, 255, 14, 255, 255, 255, 255, 255, 15, 255, 255, 255,
            255, 255, 255, 16, 17, 255, 255, 18, 255, 19, 255, 255, 255, 255, 255, 255,
            255, 255, 20, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 21, 255, 22, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 23, 24, 255, 255, 255, 255, 25, 255, 26, 27, 28, 29, 30, 31,
            255, 255, 32, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
            255, 255, 33, 34, 35, 255, 255, 255
        };

        private unsafe int Process(Client27000 client, byte[] buffer, int size)
        {
            fixed (byte* b0 = buffer)
            {
                if (size >= 34)
                {
                    int i4 = *(int*)(b0 + 22);
                    int i5 = *(int*)(b0 + 26);
                    int i6 = *(int*)(b0 + 30);

                    switch (i23[(i2[buffer[9]] << 4) + i3[buffer[13]]])
                    {
                        case 0:
                            Q_1_0(client, buffer, size);
                            break;
                        case 1:
                            Q_1_1();
                            break;
                        case 2:
                            Q_1_2(client, i4, i5, i6, buffer, size);
                            break;

                        case 3:
                            Q_4_2(client, i4, i5, i6, buffer, size);
                            break;
                        case 4:
                            Q_4_5();
                            break;

                        case 5:
                            Q_5_2(client, buffer);
                            break;

                        case 6:
                            Q_6_2(client, i4, i5, i6);
                            break;
                        case 7:
                            Q_6_3(client, i4, i5, i6, buffer);
                            break;

                        case 8:
                            Q_A_4(client, buffer);
                            break;
                        case 9:
                            Q_A_5(client, i4, i5, i6);
                            break;
                        case 10:
                            Q_A_6(client, i4, i5, i6, buffer);
                            break;
                        case 11:
                            Q_A_7(client, i4, i5, i6, buffer);
                            break;

                        case 12:
                            Q_D_2(client, i4, i5, i6);
                            break;
                        case 13:
                            Q_D_3(client, i4, i5, i6, buffer);
                            break;
                        case 14:
                            Q_D_6(client, buffer);
                            break;
                        case 15:
                            Q_D_12(client, i4, i5, i6, buffer);
                            break;

                        case 16:
                            Q_F_3(client, buffer);
                            break;
                        case 17:
                            Q_F_4();
                            break;
                        case 18:
                            Q_F_7(client, buffer);
                            break;
                        case 19:
                            Q_F_C(client, buffer);
                            break;

                        case 20:
                            Q_10_2(client, i4, i5, i6, buffer);
                            break;

                        case 21:
                            Q_13_2(client, i4, i5, i6);
                            break;
                        case 22:
                            Q_13_4(client, i4, i5, i6);
                            break;

                        case 23:
                            Q_15_2(client, i4, i5, i6);
                            break;
                        case 24:
                            Q_15_3(client, i4, i5, i6, buffer);
                            break;
                        case 25:
                            Q_15_8(client, i4, i5, i6, buffer, size);
                            break;
                        case 26:
                            Q_15_F(client, i4, i5, i6);
                            break;
                        case 27:
                            Q_15_10(client, i4, i5, i6);
                            break;
                        case 28:
                            Q_15_12(client, i4, i5, i6);
                            break;
                        case 29:
                            Q_15_13(client, i4, i5, i6);
                            break;
                        case 30:
                            Q_15_14(client, i4, i5, i6);
                            break;
                        case 31:
                            Q_15_15(client, i4, i5, i6);
                            break;

                        case 32:
                            Q_16_2();
                            break;

                        case 33:
                            Q_18_2(client, i4, i5, i6, buffer);
                            break;
                        case 34:
                            Q_18_3(client, i4, i5, i6);
                            break;
                        case 35:
                            Q_18_4();
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
                else
                {
                    long i32;

                    switch (size)
                    {
                        case 30:
                            i32 = *(long*)(b0 + 9);

                            if (i32 == 0x_00000003_00000016)
                                Q_16_3(client, buffer);
                            else
                                throw new NotImplementedException();

                            break;
                        case 21:
                            i32 = *(long*)(b0 + 9);

                            if (i32 == 0x_00000005_00000000)
                            {
                                // ping received
                            }
                            else
                                throw new NotImplementedException();

                            break;
                        case 5:
                            if (buffer[4] == 1)
                                Q(client);
                            else
                                throw new NotImplementedException();

                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }

            return 1;
        }

        // login

        private void Q(Client27000 client)
        {
            client.Event[(int)Events.Q]++;

            if (client.Event[(int)Events.Q] == 1)
            {
                client.LastTurn = _turn;

                // [Q] 05000000 01
                // [R] 19000000 00 ffffffff0000000000000000 04000000 01000000

                Clear();

                Push(client.Id);
                Push(-1, 0x00, 0x00);

                Write(client);

                // [R] 21000000 00 ffffffff0000000001000000 0c000000 00000000 00000000 01000000

                Clear();

                Push(0x01);
                Push(0x00);
                Push(0x00);
                Push(-1, 0x00, 0x01);

                Write(client);

                // [R] 05000000 02

                Clear();

                Push((byte)0x02);
                Push(0x05);

                Write(client);
            }
        }

        // AVtInfoRelayS

        private void Q_1_0(Client27000 client, byte[] buffer, int size)
        {
            // [Q] 4b000000 00 000000000100000000000000 36000000 2a000000 643476316b7340686f746d61696c2e636f6d4368617261637465724c6f674f6e52656c61794e616d6543 03000000 04000000

            int i;

            for (i = (int)Data.CharacterLogOnRelayNameC; i <= (int)Data.PlayerInfoPanel; i++)
            {
                if (Utils.EndsWith(buffer, size - 8, _data[i]))
                {
                    Contract.Assert(BitConverter.ToInt32(buffer, size - 8) == client.Id && BitConverter.ToInt32(buffer, size - 4) != -1);

                    client.Relay[i] = BitConverter.ToInt32(buffer, size - 4);

                    break;
                }
            }

            // checks if we are reconnecting

            if (i == (int)Data.CharacterLogOnRelayNameC)
            {
                Character character = client.Character;

                if ((character.State & Character.States.IsHumanBusyReconnecting) == Character.States.IsHumanBusyReconnecting)
                {
                    character.State = Character.States.IsHumanBusyConnecting;

                    // full character

                    Contract.Assert(client.Relay[(int)Data.CharacterLogOnRelayNameC] == 4);

                    _logins.Enqueue(client.Id);
                }

                Contract.Assert(character.State == Character.States.IsHumanBusyConnecting);
            }

#if DEBUG
            else if (i > (int)Data.PlayerInfoPanel)
            {
                Debugger.Break(); // !?
            }
#endif

        }

        private static void Q_1_1()
        {
            // 51000000 00 000000000100000001000000 3c000000 28000000643476316b7340686f746d61696c2e636f6d4d657461436c69656e744d697373696f6e50616e656c 4f000000 11000000 4f000000 01000000
        }

        private void Q_1_2(Client27000 client, int i4, int i5, int i6, byte[] buffer, int size)
        {
            // [Q] 47000000 00 000000000100000002000000 32000000 01 010000000100000003000000 21000000202a7e5365727665727e2a202e3f415674536563757269747952656c6179534040

            Data data;
            int opcode;

            if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtCharacterRelayS]))
            {
                // [R] 43000000000100000001000000030000002e000000 22000000202a7e5365727665727e2a202e3f41567443686172616374657252656c6179534040 00000000 15000000

                data = Data.AVtCharacterRelayS;
                opcode = 0x15;
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtChatRelayS]))
            {
                // [R] 3e0000000001000000010000000300000029000000 1d000000202a7e5365727665727e2a202e3f4156744368617452656c6179534040 00000000 13000000

                data = Data.AVtChatRelayS;
                opcode = 0x13;
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtClockRelayS]))
            {
                // [R] 3f000000000100000001000000030000002a000000 1e000000202a7e5365727665727e2a202e3f415674436c6f636b52656c6179534040 00000000 04000000

                data = Data.AVtClockRelayS;
                opcode = 0x04;
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtDataValidatorS]))
            {
                // [R] 42000000000100000001000000030000002d000000 21000000202a7e5365727665727e2a202e3f4156744461746156616c696461746f72534040 00000000 05000000

                data = Data.AVtDataValidatorS;
                opcode = 0x05;
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtEconomyRelayS]))
            {
                // [R] 41000000000100000001000000030000002c000000 20000000202a7e5365727665727e2a202e3f41567445636f6e6f6d7952656c6179534040 00000000 06000000

                data = Data.AVtEconomyRelayS;
                opcode = 0x06;
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtMapRelayS]))
            {
                // [R] 3d0000000001000000010000000300000028000000 1c000000202a7e5365727665727e2a202e3f4156744d617052656c6179534040 00000000 0d000000

                data = Data.AVtMapRelayS;
                opcode = 0x0d;
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtMissionMatcherRelayS]))
            {
                // [R] 48000000000100000001000000030000003300000027000000202a7e5365727665727e2a202e3f4156744d697373696f6e4d61746368657252656c6179534040 00000000 0e000000

                data = Data.AVtMissionMatcherRelayS;
                opcode = 0x0f; // was 0x0e
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtNewsRelayS]))
            {
                // [R] 3e0000000001000000010000000300000029000000 1d000000202a7e5365727665727e2a202e3f4156744e65777352656c6179534040 00000000 0f000000

                data = Data.AVtNewsRelayS;
                opcode = 0x10; // was 0x0f
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtNotifyRelayS]))
            {
                // [R] 40000000000100000001000000030000002b000000 1f000000202a7e5365727665727e2a202e3f4156744e6f7469667952656c6179534040 00000000 16000000

                data = Data.AVtNotifyRelayS;
                opcode = 0x16;
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtSecurityRelayS]))
            {
                // [R] 42000000000100000001000000030000002d000000 21000000202a7e5365727665727e2a202e3f415674536563757269747952656c6179534040 00000000 18000000

                data = Data.AVtSecurityRelayS;
                opcode = 0x18;
            }
            else if (Utils.EndsWith(buffer, size, _data[(int)Data.AVtShipRelayS]))
            {
                // [R] 3e0000000001000000010000000300000029000000 1d000000202a7e5365727665727e2a202e3f4156745368697052656c6179534040 00000000 0a000000

                data = Data.AVtShipRelayS;
                opcode = 0x0a;
            }
            else
            {
                throw new NotSupportedException();
            }

            Clear();

            Push(opcode);
            Push(0x00);
            Push(_data[(int)data]);
            Push(i4, i5, i6);

            Write(client);
        }

        // AVtClockRelayS

        private void Q_4_2(Client27000 client, int i4, int i5, int i6, byte[] buffer, int size)
        {
            if (Utils.EndsWith(buffer, size - 4, _data[(int)Data.MetaViewPortHandlerNameC]) || Utils.EndsWith(buffer, size - 4, _data[(int)Data.PlayerInfoPanel]))
            {
                Clear();

                PushStardate();

                Push(0x00);
                Push(i4, i5, i6);

                Write(client);
            }
        }

        private static void Q_4_5()
        { }

        // AVtDataValidatorS

        private void Q_5_2(Client27000 client, byte[] buffer)
        {
            Character character = client.Character;

            Contract.Assert(character.Mission != 0);

            // skips the character id

            Contract.Assert(BitConverter.ToInt32(buffer, 21) == character.Id);

            // skips the character name

            int c = BitConverter.ToInt32(buffer, 25);

            Contract.Assert(Encoding.UTF8.GetString(buffer, 29, c).Equals(character.CharacterName, StringComparison.Ordinal));

            int p = 29 + c;

            // checks if the character was the host

            bool IsHost = buffer[p] != 0;

            p++;

            // gets the X coordinate

            int locationX = BitConverter.ToInt32(buffer, p);

            Contract.Assert(locationX == character.CharacterLocationX);

            p += 4;

            // gets the Y coordinate

            int locationY = BitConverter.ToInt32(buffer, p);

            Contract.Assert(locationY == character.CharacterLocationY);

            p += 4;

            // gets the number of teams reported

            int teamsReported = BitConverter.ToInt32(buffer, p);

            p += 4;

            // gets the current draft 

            c = locationX + locationY * _mapWidth;

            Draft draft = _drafts[c];

            // reports the character

            draft.Reported.Add(character.Id, null);

            // updates the mission host if it changed during the mission

            if (IsHost)
            {
                Contract.Assert(draft.Mission.Host == null);

                draft.Mission.Host = character;
            }

            // gets the current mission

            MapHex hex = _map[c];

            Contract.Assert(hex.Mission != 0);

            // checks which updates will be triggered (1 - prestige, 2 - medals)

            int updates = 0;

            // processes the reported teams

            for (int i = 0; i < teamsReported; i++)
            {
                // skips the team's id

                Contract.Assert((IsHost && BitConverter.ToInt32(buffer, p) == i) || (!IsHost && teamsReported == 1));

                p += 4;

                // gets the owner and his team

                Character owner = _characters[BitConverter.ToInt32(buffer, p)];
                Team team = draft.Mission.Teams[owner.Id];

                Contract.Assert(owner.CharacterLocationX == locationX && owner.CharacterLocationY == locationY);

                p += 4;

                // gets the number of ships reported

                int shipsReported = BitConverter.ToInt32(buffer, p);

                p += 4;

                // defines some flags

                bool IsCpu = (owner.State & Character.States.IsCpu) == Character.States.IsCpu;
                bool IsUpdatable = (owner.Id == character.Id || IsCpu) && owner.Mission == (hex.Mission & MissionFilter);

                // processes the reported ships

                for (int j = 0; j < shipsReported; j++)
                {
                    // tries to get the ship

                    bool shipCanBeUpdated = _ships.TryGetValue(BitConverter.ToInt32(buffer, p), out Ship ship) && IsUpdatable;

                    // skips the header

                    c = Ship.GetHeaderSize(buffer, p);
                    p += c;

                    // tries to update the damage chunk

                    if (shipCanBeUpdated)
                    {
                        UpdateShipDamage(ship, buffer, p);
                    }

                    p += Ship.DamageSize;

                    // tries to update the stores chunk

                    c = Ship.GetStoresSize(buffer, p);

                    if (shipCanBeUpdated)
                    {
                        UpdateShipStores(ship, buffer, p, c);
                    }

                    p += c;

                    // tries to update the officers chunk

                    c = Ship.GetOfficersSize(buffer, p);

                    if (shipCanBeUpdated)
                    {
                        UpdateShipOfficers(ship, buffer, p, c);
                    }

                    p += c;

                    // skips the ship flag

                    Contract.Assert(BitConverter.ToInt32(buffer, p) == 0);

                    p += 4;
                }

                // gets the VictoryLevel

                c = BitConverter.ToInt32(buffer, p);

                Contract.Assert(Enum.IsDefined(typeof(VictoryLevels), (VictoryLevels)c));

                p += 4;

                // gets the Prestige

                c = BitConverter.ToInt32(buffer, p);

                if (IsUpdatable)
                {
                    if (UpdateCharacter(owner, c) && !IsCpu)
                        updates |= 1;
                }

                p += 4;

                // gets the BonusPrestige

                c = BitConverter.ToInt32(buffer, p);

                if (IsUpdatable)
                {
                    if (UpdateCharacter(owner, c) && !IsCpu)
                        updates |= 1;
                }

                p += 4;

                // gets the length of the NextMissionTitle

                c = BitConverter.ToInt32(buffer, p);

                Contract.Assert(c >= 8);

                p += 4;

                // tries to process the NextMissionTitle (the list of the ships reported)

                const int originalBPV = 32;
                const int capturedBPV = 47;

                Contract.Assert(_sortLongInt.Count == 0);

                while (c >= 8)
                {
                    c -= 8;

                    int shipId = (int)Utils.HexToUInt(buffer, p);

                    p += 8;

                    if (shipId == 0)
                        break;

                    if (IsUpdatable)
                    {
                        if (_ships.ContainsKey(shipId))
                        {
                            Ship ship = _ships[shipId];

                            Contract.Assert(ship.Damage.Items[(int)DamageType.ExtraDamageMax] != 0 && ship.Damage.Items[(int)DamageType.ExtraDamage] > 0);

                            if (team.Ships.ContainsKey(shipId))
                                _sortLongInt.Add((long)ship.BPV << originalBPV | (long)ship.Id, shipId);
                            else
                                _sortLongInt.Add((long)ship.BPV << capturedBPV | (long)ship.Id, shipId);
                        }
                    }
                }

                // gets the NextMissionScore

                c = BitConverter.ToInt32(buffer, p);

                Contract.Assert(c == 0);

                p += 4;

                // gets the Medal

                c = BitConverter.ToInt32(buffer, p);

                if (IsUpdatable)
                {
                    if (UpdateCharacter(owner, (Medals)c) && !IsCpu)
                        updates |= 2;
                }

                p += 4;

                // gets the CampaignEvent

                c = BitConverter.ToInt32(buffer, p);

                Contract.Assert(Enum.IsDefined(typeof(CampaignEvents), (CampaignEvents)c));

                p += 4;

                // -------------------------------------------------------------------------------

                if (IsUpdatable)
                {
                    // checks if the owner, while playing, received any ship from the shipyard
                    // and adds it to the current list

                    if (!IsCpu)
                    {
                        for (int j = 0; j < owner.ShipCount; j++)
                        {
                            if (!team.Ships.ContainsKey(owner.Ships[j]))
                            {
                                Ship ship = _ships[owner.Ships[j]];

                                Contract.Assert(ship.OwnerID == owner.Id);

                                _sortLongInt.Add((long)ship.BPV << originalBPV | (long)ship.Id, ship.Id);
                            }
                        }
                    }

                    // clears the owner's fleet

                    DeleteFleet(owner);

                    // checks if the owner has any ships to rebuild his fleet

                    if (_sortLongInt.Count == 0)
                    {
                        // as we can't have any character without ships
                        // we must do something about it

                        if (IsCpu)
                        {
                            // deletes the AI permanently

                            _characters.Remove(owner.Id);
                            _cpuMovements.Remove(owner.Id);

                            RemoveFromHexPopulation(hex, owner);
                        }
                        else
                        {
                            // creates a temporary ship for the players

                            CreateTemporaryShip(owner);
                        }
                    }
                    else
                    {
                        // checks if the owner's fleet surpassed its cap and needs to sell any 'extra' ships

                        if (IsCpu)
                            c = Character.MaxFleetSize;
                        else
                        {
                            // a human character can have up to 3 ships in his fleet
                            // but, here, we need to give room for any bids that are still pending

                            Contract.Assert(owner.Bids >= 0 && owner.Bids <= 2); 

                            c = 3 - owner.Bids;
                        }

                        if (_sortLongInt.Count > c)
                        {
                            Contract.Assert(_queueInt.Count == 0);

                            foreach (KeyValuePair<long, int> q in _sortLongInt)
                            {
                                Ship ship = _ships[q.Value];

                                if (c > 0)
                                {
                                    // keeps the 'best' ships

                                    _queueInt.Enqueue(ship.Id);

                                }
                                else
                                {
                                    // sells the ships and makes a profit on it

                                    int profit = GetShipTradeInValue(ship);

                                    if (UpdateCharacter(owner, profit) && !IsCpu)
                                        updates |= 1;

                                    // deletes the ship permanently

                                    _ships.Remove(ship.Id);
                                }

                                c--;
                            }

                            _sortLongInt.Clear();

                            c = _queueInt.Count;

                            while (c > 0)
                            {
                                _sortLongInt.Add(c, _queueInt.Dequeue());

                                c--;
                            }

                            Contract.Assert(_queueInt.Count == 0);
                        }

                        // rebuilds the owner's fleet

                        foreach (KeyValuePair<long, int> q in _sortLongInt)
                        {
                            Ship ship = _ships[q.Value];

                            // checks if the ship was captured and modifies it if necessary

                            if (ship.Race != owner.CharacterRace)
                            {
                                Contract.Assert(ship.OwnerID != owner.Id);

                                ModifyShip(ship, owner.CharacterRace);
                            }

                            // does the automatic repairs and resupplies
                            // and decides what to do with the ship

                            if (IsCpu)
                            {
                                int expenses = 0;

                                // the automatic repair is mostly done by the crew
                                // but the AI 'supports' any differences

                                int oldCost = GetShipRepairCost(ship);

                                AutomaticRepair(ship, _cpuAutomaticRepairMultiplier);

                                int newCost = GetShipRepairCost(ship);

                                Contract.Assert(newCost <= oldCost);

                                expenses += (int)Math.Round((newCost - oldCost) * (_cpuAutomaticRepairMultiplier - _humanAutomaticRepairMultiplier), MidpointRounding.AwayFromZero);

                                // the automatic resupply is entirely 'supported' by the AI

                                oldCost = GetStoresCost(ship.ClassType, ship.Stores);

                                AutomaticResupply(ship, _cpuAutomaticResupplyMultiplier);

                                newCost = GetStoresCost(ship.ClassType, ship.Stores);

                                Contract.Assert(oldCost <= newCost);

                                expenses += (int)Math.Round((oldCost - newCost) * (_cpuAutomaticResupplyMultiplier - _humanAutomaticResupplyMultiplier), MidpointRounding.AwayFromZero);

                                // registers the expense

                                Contract.Assert(expenses <= 0);

                                UpdateCharacter(owner, expenses);

                                _curExpenses[(int)owner.CharacterRace] -= expenses;

                                // adds the ship to the fleet

                                UpdateCharacter(owner, ship);
                            }
                            else
                            {
                                // the automatic repair and resupply is done by the crew (free labor)

                                AutomaticRepair(ship, _humanAutomaticRepairMultiplier);
                                AutomaticResupply(ship, _humanAutomaticResupplyMultiplier);

                                // checks if the ship is a base or planet that we brought or was captured

                                if (ship.ClassType >= ClassTypes.kClassListeningPost && ship.ClassType <= ClassTypes.kClassStarBase || ship.ClassType == ClassTypes.kClassPlanets)
                                {
                                    // creates an AI character to take care of it

                                    CreateCharacter(owner.CharacterRace, locationX, locationY, ship, out _);
                                }
                                else
                                {
                                    // adds the ship to the fleet

                                    UpdateCharacter(owner, ship);
                                }
                            }
                        }

                        _sortLongInt.Clear();

                        RefreshCharacter(owner);
                    }
                }
            }

            // ----------------------------------------------------------------------------------------------------------------------------------------------

            if ((updates & 1) != 0)
                Write(client, Relays.PlayerRelayC, 0x05, 0x00, 0x07, character.Id); // 15_12

            if ((updates & 2) != 0)
                Write(client, Relays.PlayerRelayC, 0x08, 0x00, 0x0c, character.Id); // 15_15

            Write(client, Relays.PlayerRelayC, 0x04, 0x00, 0x06, character.Id); // A_5

            if (client.IconsRequest == 0)
                client.IconsRequest = 1; // 15_F

            // ----------------------------------------------------------------------------------------------------------------------------------------------

            // tries to leave the current mission

            Contract.Assert((character.State & Character.States.IsBusy) == Character.States.IsBusy);

            character.State &= ~Character.States.IsBusy;

            TryLeaveMission(character, hex);
        }

        // AVtEconomyRelayS

        private void Q_6_2(Client27000 client, int i4, int i5, int i6)
        {
            // [Q] 2e000000000000000006000000020000001900000001210000001600000002000000e20100000000803f00000000

            Character character = client.Character;

            Clear();

            int c = 0;

            if (character.CharacterRace == character.CharacterPoliticalControl)
            {
                foreach (KeyValuePair<int, BidItem> p in _bidItems[(int)character.CharacterRace])
                {
                    BidItem item = p.Value;

                    Push(character, item, item.BiddingHasBegun);
                    Push(item.ShipId);

                    c++;

                    if (c == 40)
                        break;
                }
            }

            if (c == 0)
            {
                Push(_data[(int)Data.shipyardFiller]);

                c++;
            }

            Push(c);
            Push(0x01);

            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_6_3(Client27000 client, int i4, int i5, int i6, byte[] buffer)
        {
            int shipId = BitConverter.ToInt32(buffer, 34);

            if (_ships[shipId].IsInAuction != 1)
                return;

            Character character = client.Character;

            Contract.Assert(BitConverter.ToInt32(buffer, 38) == character.Id && BitConverter.ToInt32(buffer, 42) == (int)character.CharacterRace);

            BidItem item = _bidItems[(int)character.CharacterRace][shipId];
            int bidType = BitConverter.ToInt32(buffer, 46);

            Clear();

            if (TryUpdateBidItem(character, item, bidType))
            {
                Push(0x02);
                Push(0x00);
                Push(character.Id);

                Push(character, item, 1);

                Push(0x00);
            }
            else
            {
                Push(0x00);
                Push(0x00);
                Push(character.Id);

                Push(_data[(int)Data.bidItemNull]);

                Push(0x05);
            }

            Push(i4, i5, i6);

            Write(client);
        }

        // AVtShipRelayS

        private void Q_A_4(Client27000 client, byte[] buffer)
        {
            Character character = client.Character;

            Contract.Assert(BitConverter.ToInt32(buffer, 34) == -1);

            int shipId = BitConverter.ToInt32(buffer, 38);

            Contract.Assert(BitConverter.ToInt32(buffer, 42) == character.Id);

            int c = character.ShipCount;

            Contract.Assert(c > 0);

            for (int i = 0; i < c; i++)
            {
                if (character.Ships[i] == shipId)
                {
                    Ship ship = _ships[shipId];

                    // updates the character

                    int tradeInValue = GetShipTradeInValue(ship);

                    UpdateCharacter(character, tradeInValue);

                    // repairs the ship because the tradeInValue already includes the repair cost value

                    RepairShip(ship);

                    // removes the current ship from the character's ship list

                    for (int j = i + 1; j < c; j++)
                        character.Ships[j - 1] = character.Ships[j];

                    character.ShipCount--;

                    character.Ships[character.ShipCount] = 0;

                    RefreshCharacter(character);

                    // decides what to do with the ship

                    int option = 2;

                    switch (option)
                    {
                        case 0:
                            {
                                // gives the ship to a new character

                                CreateCharacter(character.CharacterRace, character.CharacterLocationX, character.CharacterLocationY, ship, out _);

                                break;
                            }
                        case 1:
                            {
                                // puts the ship in the shipyard

                                ship.OwnerID = 0;

                                AddBidItem(ship);

                                Contract.Assert(ship.Race == character.CharacterRace);

                                TrySortBidItems((int)ship.Race);

                                break;
                            }
                        case 2:
                            {
                                // removes the ship from the game

                                _ships.Remove(ship.Id);

                                break;
                            }
                    }

                    break;
                }
            }

            Write(client, Relays.PlayerRelayC, 0x04, 0x00, 0x06, character.Id); // A_5
            Write(client, Relays.PlayerRelayC, 0x05, 0x00, 0x07, character.Id); // 15_12
        }

        private void Q_A_5(Client27000 client, int i4, int i5, int i6)
        {
            Character character = client.Character;

            Clear();

            int c = character.ShipCount;

            Contract.Assert(c > 0);

            // third

            for (int i = c - 1; i >= 0; i--)
            {
                Ship ship = _ships[character.Ships[i]];

                if (ship.Stores.ContainsFighters)
                    Push(_supplyFtrCache[(int)character.CharacterRace]);
                else
                    Push(0x00);

                Push(_costSpareParts[(int)ship.ClassType]);
                Push(_costMines);
                Push(_costMarines);
                Push(_costShuttles);
                Push(_costFighters);
                Push(_costMissiles);
                Push(_costUnknown);

                Push(ship.Id);
            }

            Push(c);

            // second

            for (int i = c - 1; i >= 0; i--)
            {
                Push(_costTradeIn);

                Push(character.Ships[i]);
            }

            Push(c);

            // first

            for (int i = c - 1; i >= 0; i--)
            {
                Ship ship = _ships[character.Ships[i]];

                Push(_costRepair[(int)ship.ClassType]);

                Push(ship.Id);
            }

            Push(c);

            // ships

            for (int i = c - 1; i >= 0; i--)
            {
                Ship ship = _ships[character.Ships[i]];

                Push(ship);
            }

            Push(c);

            // header

            Push(0x01);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_A_6(Client27000 client, int i4, int i5, int i6, byte[] buffer)
        {
            Character character = client.Character;

            Contract.Assert(BitConverter.ToInt32(buffer, 34) == character.Id);
            Contract.Assert(BitConverter.ToInt32(buffer, 38) == 1);

            int shipId = BitConverter.ToInt32(buffer, 42);

            Ship ship = _ships[shipId];

            int repairCost = GetShipRepairCost(ship);

            if (character.CharacterCurrentPrestige >= repairCost)
            {
                UpdateCharacter(character, -repairCost);

                RepairShip(ship);
            }

            Write(client, Relays.PlayerRelayC, 0x05, 0x00, 0x07, character.Id); // 15_12

            // 19000000 00 010000001400000002000000 04000000 00000000

            Clear();

            Push(0x00);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_A_7(Client27000 client, int i4, int i5, int i6, byte[] buffer)
        {
            Character character = client.Character;

            Contract.Assert(BitConverter.ToInt32(buffer, 34) == character.Id);

            // updates the ship count, and ship list

            int c = BitConverter.ToInt32(buffer, 38);
            int p = 42;

            character.ShipCount = c;

            for (int i = 0; i < c; i++)
            {
                character.Ships[i] = BitConverter.ToInt32(buffer, p);

                p += 4;
            }

            // updates the names of the ships

            Contract.Assert(BitConverter.ToInt32(buffer, p) == c);

            p += 4;

            for (int i = 0; i < c; i++)
            {
                Ship ship = _ships[character.Ships[i]];

                Contract.Assert(BitConverter.ToInt32(buffer, p) == ship.Id);

                p += 4;

                int count = BitConverter.ToInt32(buffer, p);

                p += 4;

                ship.Name = Encoding.UTF8.GetString(buffer, p, count);

                p += count;
            }

            // tries to update the stores' chunks of the ships

            Contract.Assert(BitConverter.ToInt32(buffer, p) == c);

            p += 4;

            for (int i = 0; i < c; i++)
            {
                Ship ship = _ships[character.Ships[i]];

                Contract.Assert(BitConverter.ToInt32(buffer, p) == ship.Id);

                p += 4;

                // calculates the cost of updating the current stores

                int size = Ship.GetStoresSize(buffer, p);
                ShipStores newStores = new ShipStores(buffer, p, size);

                int currentValue = GetStoresCost(ship.ClassType, ship.Stores);
                int newValue = GetStoresCost(ship.ClassType, newStores);

                // checks if we have enough prestige to make the update

                int cost = newValue - currentValue;

                if (cost < 0)
                    cost = 0;

                //Debug.WriteLine("Total: " + cost);

                if (character.CharacterCurrentPrestige >= cost)
                {
                    UpdateCharacter(character, -cost);

                    // updates the ship's stores chunk

                    ship.Stores = newStores;
                }

                p += size;

                // updates the ship, in case we modified anything

                RefreshShip(ship);
            }

            // updates the character, in case we modified anything

            RefreshCharacter(character);

            // 1e00000000020000000600000005000000 09000000 00000000 07 73021234

            Write(client, Relays.PlayerRelayC, 0x05, 0x00, 0x07, character.Id); //  15_12

            /*
                3e050000 00 020000001400000003000000 29050000 01000000

                02000000

                e9011234 e901123473021234000000000007000000810000008100000005000000462d434152080000005553532056656761020000000f0f0f0f000004040000060606060606060600000c0c040400000202060603030303040408080000000000000606020200000202020200000000000000000000000000000000020202020202000000000202000000000000000000000000000000000000 0101000001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004040400000000000000000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff140a140804041e06060000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000010000000000000000000000000000000100000000000000000000000000000001000000000000000000000000000000010000000000000000000000000000000100000000000000000000000000000001000000000000000000000000000000         
                74021234 7402123473021234000000000003000000470000004700000004000000..462d46460800000055535320566567610200000006060606000003030000040405050505040400000000000006060202040402020202020202020000000000000404010100000101010100000000000000000000000000000000010101010101010101010000000000000000000000000000000000000000 0101000001000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002020200000000000000000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff0c06080402031404040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001000000000000000000000000000000010000000000000000000000000000000100000000000000000000000000000001000000000000000000000000000000010000000000000000000000000000000100000000000000000000000000000001000000000000000000000000000000
            */

            Clear();

            for (int i = c - 1; i >= 0; i--)
            {
                Ship ship = _ships[character.Ships[i]];

                Push(ship);
                Push(ship.Id);
            }

            Push(c);
            Push(0x01);

            // header

            Push(0x01);
            Push(i4, i5, i6);

            Write(client);
        }

        // AVtMapRelayS

        private void Q_D_2(Client27000 client, int i4, int i5, int i6)
        {
            Clear();

            Push(_mapHeight);
            Push(_mapWidth);

            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_D_3(Client27000 client, int i4, int i5, int i6, byte[] buffer)
        {
            /*
                [Q] 24000000 00 000000000d00000003000000 0f000000 01 010000000e00000002000000 ffff
                [Q] 24000000 00 000000000d00000003000000 0f000000 01 010000000e00000002000000 5000
            */

            Clear();

            int c = BitConverter.ToInt16(buffer, 34);

            if (c == -1)
            {
                Push((byte)0x01);

                c = _map.Length;

                for (int i = c - 1; i >= 0; i--)
                {
                    MapHex hex = _map[i];

                    Push(hex, 0);
                }
            }
            else
            {
                int x = c & 255;
                int y = c >> 8;

                MapHex hex = _map[x + y * _mapWidth];

                Push((byte)y);
                Push((byte)x);
                Push((byte)0x00);

                Push(hex, 0);

                c = 1;

                // resets the flag

                client.HexRequest = -1;
            }

            Push(c);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_D_6(Client27000 client, byte[] buffer)
        {
            // [Q] 32000000 00 000000000d00000006000000 1d000000 01 010000000e00000000000000 55010000 09000000 03000000 00000000

            Character character = client.Character;

            Contract.Assert(BitConverter.ToInt32(buffer, 34) == character.Id);

            int moveDestinationX = BitConverter.ToInt32(buffer, 38);
            int moveDestinationY = BitConverter.ToInt32(buffer, 42);

            int moveDelay; // seconds
            int moveOpcode;

            if ((character.Mission & IsMustPlayMask) == IsMustPlayMask || (character.State & Character.States.IsBusy) == Character.States.IsBusy)
            {
                // [R] 2d0000000 00 10000000e00000004000000 18000000 05000000 55010000 08000000 03000000 00000000 00000000

                moveDelay = 0x00;
                moveOpcode = 0x05;
            }
            else if (character.MoveDestinationX != -1 || character.MoveDestinationY != -1 || !MovementValid(character.CharacterLocationX, character.CharacterLocationY, moveDestinationX, moveDestinationY))
            {
                // [R] 2d0000000 00 10000000e00000004000000 18000000 06000000 55010000 0a000000 04000000 00000000 00000000

                moveDelay = 0x00;
                moveOpcode = 0x06;
            }
            else
            {
                // [R] 2d000000 00 010000000e00000004000000 18000000 02000000 55010000 09000000 03000000 03000000 00000000

                moveDelay = 0x03;
                moveOpcode = 0x02;

                Contract.Assert((character.State & Character.States.IsBusy) != Character.States.IsBusy);

                character.State |= Character.States.IsBusy;

                character.MoveDestinationX = moveDestinationX;
                character.MoveDestinationY = moveDestinationY;

                MapHex origin = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];
                MapHex destination = _map[moveDestinationX + moveDestinationY * _mapWidth];

                AddHumanMovement(character, origin, destination);

                if (client.Address > 0)
                    TryGetMission(character, destination);

                Write(client, Relays.PlayerRelayC, 0x03, 0x00, 0x04, character.Id); // 15_10
            }

            // begin movement

            Clear();

            Push(0x00);
            Push(moveDelay);
            Push(moveDestinationY);
            Push(moveDestinationX);
            Push(character.Id);
            Push(moveOpcode);
            Push(client.Id, client.Relay[(int)Relays.MetaViewPortHandlerNameC], 0x04);

            Write(client);
        }

        private void Q_D_12(Client27000 client, int i4, int i5, int i6, byte[] buffer)
        {
            // [Q] 2a000000 00 000000000d00000012000000 15000000 01 010000000e00000009000200 00000000 08000000

            int race1 = BitConverter.ToInt32(buffer, 34);
            int race2 = BitConverter.ToInt32(buffer, 38);

            StringBuilder status = new StringBuilder(1024);

            status.Append('(');
            status.Append(_races[race1]);
            status.Append(" is at ");

            if ((_alliances[race1] & (Alliances)(1 << race2)) != 0)
                status.Append("peace");
            else
                status.Append("war");

            status.Append(" with ");
            status.Append(_races[race2]);
            status.Append(')');

            // [R] 39000000 00 010000000e00000009000200 24000000 01000000 1c000000576520646973747275737420746865204f72696f6e2043617274656c

            Clear();

            Push(status.ToString());
            Push(0x01);
            Push(i4, i5, i6);

            Write(client);
        }

        // AVtMissionMatcherRelayS

        private void Q_F_3(Client27000 client, byte[] buffer)
        {
            Character character = client.Character;

            Contract.Assert(BitConverter.ToInt32(buffer, 21) == 1);

            int missionIndex = BitConverter.ToInt32(buffer, 25);

            Contract.Assert(BitConverter.ToInt32(buffer, 29) == character.Id);

            int mapIndex = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;

            MapHex hex = _map[mapIndex];

            if (buffer[33] == 0)
            {
                if (missionIndex == -1)
                {
                    /*
                        // the character moved or abandoned his missions

                        22000000 00 000000000f00000003000000 0d000000 01000000 ffffffff 42050000 00
                    */

                    Contract.Assert(character.Mission == 0 || (character.Mission & IsMustPlayMask) != IsMustPlayMask);

                    TryLeaveMission(character, hex);
                }
                else
                {
                    /*
                        // the character forfeited a mission

                        22000000 00 000000000f00000003000000 0d000000 01000000 00000000 42050000 00
                    */

                    Contract.Assert((character.Mission & IsMustPlayMask) == IsMustPlayMask);

                    TryLeaveMission(character, hex);
                }

                return;
            }

            /*
                // the character accepted a mission

                22000000 00 000000000f00000003000000 0d000000 01000000 00000000 42050000 01
            */

            Contract.Assert((character.State & Character.States.IsBusy) != Character.States.IsBusy);

            character.State |= Character.States.IsBusy;

            Draft draft = new Draft();

            if (!_drafts.TryAdd(mapIndex, draft))
                throw new NotSupportedException();

            draft.Expected.Add(character.Id, null);
            draft.Accepted.Add(character.Id, null);

            foreach (KeyValuePair<int, object> p in hex.Population)
            {
                Character target = _characters[p.Key];

                if (target.Id == character.Id)
                    continue;

                if (target.State == Character.States.IsCpuOnline)
                {
                    // AI character

                    Contract.Assert(target.Mission == 0);

                    target.Mission = character.Mission;
                    target.State = Character.States.IsCpuAfkBusyOnline;
                }
                else if (target.State == Character.States.IsHumanOnline)
                {
                    Client27000 targetClient = target.Client;

                    if (targetClient.Address > 0)
                    {
                        // human character with a valid address

                        if (target.Mission == 0)
                        {
                            target.Mission = character.Mission;

                            hex.Mission += (1L << CounterShift);
                        }

                        Contract.Assert(target.Mission == character.Mission);

                        target.State |= Character.States.IsBusy;

                        SendMissionToGuest(target.Client);

                        draft.Expected.Add(target.Id, null);
                    }
                }
            }
        }

        private static void Q_F_4()
        {
            /*
                // a mission failed to start (ex: draft with 7 human players)

                21000000 00 000000000f00000004000000 0c000000 48050000 ffffffff ffffffff
            */
        }

        private void Q_F_7(Client27000 client, byte[] buffer)
        {
            Character character = client.Character;

            Contract.Assert(character.Mission != 0);

            int draftId = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;

            Draft draft = _drafts[draftId];

            if (buffer[33] == 0)
            {
                /*
                    // the character forfeited a mission

                    22000000 00 000000000f00000007000000 0d000000 01000000 00000000 42050000 00
                */

                draft.Forfeited.Add(character.Id, null);

                Contract.Assert((character.State & Character.States.IsBusy) == Character.States.IsBusy);

                character.State &= ~Character.States.IsBusy;

                MapHex hex = _map[draftId];

                TryLeaveMission(character, hex);

                return;
            }

            /*
                // the character accepted a mission

                22000000 00 000000000f00000007000000 0d000000 01000000 00000000 42050000 01
            */

            draft.Accepted.Add(character.Id, null);
        }

        private void Q_F_C(Client27000 client, byte[] buffer)
        {
            /*
                // a mission was received with sucess

                26000000 00 000000000e0000000c000000 11000000 01 060000000400000010000000 42050000
            */

            Character character = client.Character;

            Contract.Assert(BitConverter.ToInt32(buffer, 34) == character.Id);

            Character host = _characters[(int)((character.Mission & HostMask) >> HostShift)];
            Draft draft = _drafts[host.CharacterLocationX + host.CharacterLocationY * _mapWidth];

            draft.Received.Add(client.Character.Id, null);
        }

        // AVtNewsRelayS

        private void Q_10_2(Client27000 client, int i4, int i5, int i6, byte[] buffer)
        {
            /*
                //********************************************************************************************************************************************************
                // requests news by id

                [Client1] 26000000 00 000000001100000002000000 11000000 01 010000001200000002000000 9a040000
                [Server1] 5a000000 00 010000001200000002000000 45000000

                01000000 // news count

                9a040000 // news id
                00000000
                04
                04       // urgency
                01       // category (0 - universal, 1 - empire, 2 - personal)
                26000000 4e657720676f7665726e6f7220656c656374656420746f2073797374656d202831302c33292e
                2f27e95e
                e9c4c2cc
                ae200a00 // rgb color

                //********************************************************************************************************************************************************
                // requests all news

                [Client1] 26000000 00 000000000f00000002000000 11000000 01 010000001200000002000000 ffffffff
                [Server1] 280a0000 00 010000001200000002000000 130a0000

                1c000000

                e7090000
                00000000
                03
                03
                01
                3f000000 546865204d6972616b2053746172204c65616775652072616e6b732061742032382077697468203020746f74616c2065636f6e6f6d696320706f696e74732e
                032ce95e
                0e4dadad
                bd3a1700

                e6090000
                00000000
                03
                03
                01
                45000000 54686520496e7465727374656c6c617220436f6e636f726469756d2072616e6b732061742032372077697468203020746f74616c2065636f6e6f6d696320706f696e74732e
                032ce95e
                c147adad
                bd3a1700

                (...)

            */

            Character character = client.Character;

            int newsTime = GetCurrentTime();
            int newsId = GetNextDataId();
            int newsColor;
            string newsText;

            switch (BitConverter.ToInt32(buffer, 34))
            {
                case -1:
                    newsColor = 0x7f7f7f;
                    newsText = "Welcome " + Enum.GetName(typeof(Ranks), character.CharacterRank) + " " + character.CharacterName + "!"; break;
                case 0:
                    newsColor = 0x4fa300;
                    newsText = "You have received a new ship!"; break;
                default:
                    throw new NotImplementedException();
            }

            Clear();

            Push(newsColor);
            Push(0x00);       // TimeDetail
            Push(newsTime);
            Push(newsText);
            Push((byte)0x00); // Category
            Push((byte)0x00); // UrgencyLevel
            Push((byte)0x00); // PersistenceLevel
            Push(0x00);       // LockID
            Push(newsId);

            // count

            Push(0x01);

            Push(i4, i5, i6);

            Write(client);
        }

        // AVtChatRelayS

        private void Q_13_2(Client27000 client, int i4, int i5, int i6)
        {
            Contract.Assert(_channels.Length == 20);

            Clear();

            // @Standard

            Push(_channels[0]);

            // #General, #ServerBroadcast

            for (int i = 3; i >= 2; i--)
            {
                Push(_channels[0]);
                Push(_channels[i]);
            }

            // #SystemBroadcast

            Push(0x00);
            Push(_channels[1]);

            // # empires and cartels

            for (int i = 19; i >= 4; i--)
            {
                Push(_channels[0]);
                Push(_channels[i]);
            }

            Push(19);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_13_4(Client27000 client, int i4, int i5, int i6)
        {
            Clear();

            Push(0x00);

            Push(defaultIrcChannel);           // default IRC server channel
            Push(_serverNick);                 // VerboseName
            Push(_serverNick);                 // Name
            Push(_serverNick);                 // NickName
            Push(defaultIrcPort);              // default IRC server port
            Push(_localEP.Address.ToString()); // default IRC server address

            Push(0x01);
            Push(i4, i5, i6);

            Write(client);
        }

        // AVtCharacterRelayS

        private void Q_15_2(Client27000 client, int i4, int i5, int i6)
        {
            /*
                0x00000002 - reconnecting with existing char
                0x00000003 - server full
                0x00000005 - name already exists
                0x00000008 - creates a new char
                0x00000010 - bad password
            */

            // checks if we are creating a new character
            // or reconnecting with an existing one

            Character character = client.Character;

            if (character.CharacterName.Length == 0)
            {
                M_NullCharacter(client, i4, i5, i6, 0x08);

                return;
            }

            Contract.Assert(character.State == Character.States.IsHumanBusyConnecting);

            character.State = Character.States.IsHumanBusyReconnecting;

            UpdatePoliticalControl(client.Character);

            M_FullCharacter(client, i4, i5, i6, 0x02);
        }

        private void Q_15_3(Client27000 client, int i4, int i5, int i6, byte[] buffer)
        {
            /*
                [Q] cd00000000000000001500000003000000b8000000010c000000040000000300000000000000
                13000000 643476316b733740686f746d61696c2e636f6d
                00000000
                07000000 443476316b7336
                06000000 
                00000000ffffffffdc0500000000000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
            */

            // checks if the server is currently locked

            if (_locked > 0)
            {
                M_NullCharacter(client, i4, i5, i6, 0x03);

                return;
            }

            // skips the email

            int c = BitConverter.ToInt32(buffer, 38);
            int p = 42 + c;

            // skips the id

            Contract.Assert(BitConverter.ToInt32(buffer, p) == 0);

            p += 4;

            // reads the name

            c = BitConverter.ToInt32(buffer, p);
            p += 4;

            string name = Encoding.UTF8.GetString(buffer, p, c);

            p += c;

            // reads the race

            Races race = (Races)BitConverter.ToInt32(buffer, p);

            // checks if the name already exists

            Character character = client.Character;

            if (TryUpdateCharacter(character, name, race))
            {
                // partial character

                Clear();

                Push((byte)0x00);
                Push(0x00);
                Push(_data[(int)Data.master5_1]);
                Push(_data[(int)Data.master6]);
                Push((int)character.CharacterPoliticalControl);
                Push((int)character.CharacterRace);
                Push(character.CharacterName);
                Push(character.Id);
                Push(character.WONLogon);
                Push(0x00);
                Push(0x00);
                Push(i4, i5, i6);

                Write(client);

                // full character

                Contract.Assert(i4 == client.Id && i5 == client.Relay[(int)Data.CharacterLogOnRelayNameC] && (i6 - 1) == 0x02);

                _logins.Enqueue(client.Id);
            }
            else
            {
                M_NullCharacter(client, i4, i5, i6, 0x05);
            }
        }

        private void Q_15_8(Client27000 client, int i4, int i5, int i6, byte[] buffer, int size)
        {
            /*
                [Q] 2b000000 00 000000001500000008000000 16000000 01 010000000600000003000100 01000000 d2000000 00
                [Q] 27000000 00 000000001500000008000000 12000000 01 010000000b00000001000100 00000000 01

                [Q] 2b000000 00 000000001500000008000000 16000000 01 020000000b00000003000100 01000000 000000d6 01
            */

            Clear();

            int c = 0;

            if (size == 39)
            {
                Contract.Assert(client.IdList.Count == 0 && BitConverter.ToInt32(buffer, 34) == 0 && buffer[38] == 1);

                // adds the current character

                Character character = client.Character;

                Contract.Assert(character.State == Character.States.IsHumanBusyConnecting);

                client.IdList.TryAdd(client.Id, character.Id);

                PushCharacterAndHex(character);

                c++;

                // adds the others characters

                foreach (KeyValuePair<string, int> p in _characterNames)
                {
                    int characterId = p.Value;

                    if (_characters.TryGetValue(characterId, out character) && (character.State & Character.States.IsHumanOnline) == Character.States.IsHumanOnline)
                    {
                        client.IdList.Add(character.Client.Id, character.Id);

                        PushCharacterAndHex(character);

                        c++;
                    }
                }

                Contract.Assert(c != 0);
            }
            else
            {
                Contract.Assert(BitConverter.ToInt32(buffer, 34) == 1);

                int id = BitConverter.ToInt32(buffer, 38);

                if (id != 0)
                {
                    if (TryPushCharacterAndHex(id))
                    {
                        // the character id was in little indian notation

                        c++;
                    }
                    else if (TryPushCharacterAndHex(buffer[41] + (buffer[40] << 8) + (buffer[39] << 16) + (buffer[38] << 24)))
                    {
                        // the character id was in big indian notation (very rare)

                        c++;
                    }
                }

                if (c == 0)
                {
                    PushCharacterAndHex(client.Character);

                    c++;
                }
            }

            Push(c);
            Push(0x00);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_15_F(Client27000 client, int i4, int i5, int i6)
        {
            /*
                [Q] 26000000 00 00000000150000000f000000 11000000 01 010000000e00000007000200 55010000
                [R] 36000000 00 010000000e00000007000200 21000000 00000000
                    01000000
                    55010000 56010000 03000000 0a000000 04000000 01 0100000
            */

            Character character = client.Character;

            Clear();

            Contract.Assert(character.ShipsBestId != 0);

            PushIcon(character, _ships[character.ShipsBestId], 0x01);

            int teamMask = 1 << (int)character.CharacterRace;
            int allyMask = (int)_alliances[(int)character.CharacterRace];
            int neutralMask = (int)_alliances[(int)Races.kNeutralRace];

            int c = 1;

            int i1 = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;

            for (int i = 0; i < 7; i++)
            {
                int i2 = i1 + _directions[character.CharacterLocationX & 1][i];

                if (i2 < 0 || i2 >= _map.Length || Math.Abs((i1 % _mapWidth) - (i2 % _mapWidth)) > 2)
                    continue;

                Dictionary<int, object> hexPopulation = _map[i2].Population;

                if (hexPopulation.Count == 0)
                    continue;

                Array.Clear(_arrayInt1, 0, 32); // ship.Id
                Array.Clear(_arrayInt2, 0, 32); // ship.BPV

                foreach (KeyValuePair<int, object> p in hexPopulation)
                {
                    Character target = _characters[p.Key];

                    if (target.Id != character.Id && target.ShipsBestId != 0)
                    {
                        int mask = 1 << (int)target.CharacterRace;

                        if (mask == teamMask)
                            FilterIcons(target.ShipsBestId, target.ShipsBestBPV, _arrayInt1, _arrayInt2, 0);
                        else if ((mask & allyMask) != 0)
                            FilterIcons(target.ShipsBestId, target.ShipsBestBPV, _arrayInt1, _arrayInt2, 8);
                        else if ((mask & neutralMask) != 0)
                            FilterIcons(target.ShipsBestId, target.ShipsBestBPV, _arrayInt1, _arrayInt2, 16);
                        else // enemy mask
                            FilterIcons(target.ShipsBestId, target.ShipsBestBPV, _arrayInt1, _arrayInt2, 24);
                    }
                }

                int teammates = 0;
                int allies = 0;
                int neutrals = 0;
                int enemies = 0;

                for (int j = 0; j < 3; j++)
                {
                    if (_arrayInt1[j] != 0) teammates++;
                    if (_arrayInt1[j + 8] != 0) allies++;
                    if (_arrayInt1[j + 16] != 0) neutrals++;
                    if (_arrayInt1[j + 24] != 0) enemies++;
                }

                while (teammates + allies + neutrals + enemies > 3)
                {
                    if (neutrals > 1)
                    {
                        neutrals--;

                        continue;
                    }

                    if (enemies > 1)
                    {
                        enemies--;

                        continue;
                    }

                    if (allies > 1)
                    {
                        allies--;

                        continue;
                    }

                    if (teammates > 1)
                    {
                        teammates--;

                        continue;
                    }

                    Contract.Assert(allies > 0);

                    allies--;
                }

                if (teammates > 0)
                {
                    c += teammates;

                    PushIcons(_arrayInt1, 0, teammates);
                }

                if (allies > 0)
                {
                    c += allies;

                    PushIcons(_arrayInt1, 8, allies);
                }

                if (enemies > 0)
                {
                    c += enemies;

                    PushIcons(_arrayInt1, 24, enemies);
                }

                if (neutrals > 0)
                {
                    c += neutrals;

                    PushIcons(_arrayInt1, 16, neutrals);
                }
            }

            Push(c);
            Push(0x00);
            Push(i4, i5, i6);

            Write(client);

            // resets the flag

            client.IconsRequest = 0;
        }

        private void Q_15_10(Client27000 client, int i4, int i5, int i6)
        {
            Character character = client.Character;

            // updates the character details, supply and shipyard availability, in the UI

            Clear();

            Push(character.MoveDestinationY);
            Push(character.MoveDestinationX);

            MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

            Push(hex, 1);
            Push(0x01);
            Push(hex.Id);

            Push(0x00);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_15_12(Client27000 client, int i4, int i5, int i6)
        {
            Clear();

            Push(client.Character.CharacterCurrentPrestige);
            Push(0x00);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_15_13(Client27000 client, int i4, int i5, int i6)
        {
            /*
                Q: 26000000 00 000000001500000013000000 11000000 01 020000000600000007000200 73021234

                // no bids

                R: 1d000000 00 020000000600000007000200 08000000 00000000
                   00000000 // bid count

                // one bid

                R: 21000000 00 020000000600000007000200 0c000000 00000000
                   01000000 // bid count
                   9e1f1234 // bid id
            */

            Clear();

            Push(0x00);
            Push(0x00);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_15_14(Client27000 client, int i4, int i5, int i6)
        {
            Clear();

            Push((int)client.Character.CharacterRank);
            Push(0x00);
            Push(i4, i5, i6);

            Write(client);
        }

        private void Q_15_15(Client27000 client, int i4, int i5, int i6)
        {
            Clear();

            Push((int)client.Character.Awards);
            Push(0x00);
            Push(i4, i5, i6);

            Write(client);
        }

        // AVtNotifyRelayS

        private static void Q_16_2()
        {
            // 5a000000 00 000000001600000002000000 45000000 00000000 00000000 00000000 0d e2010000 2b000000643476316b7340686f746d61696c2e636f6d4d657461436c69656e74537570706c79446f636b50616e656c 04000000 00
        }

        private void Q_16_3(Client27000 client, byte[] buffer)
        {
            Character character = client.Character;

            switch (buffer[25])
            {
                case 0x03:
                    {
                        Write(client, Relays.PlayerRelayC, 0x03, 0x00, 0x03, character.Id); // 15_10

                        break;
                    }
                case 0x0d:
                    {
                        client.Event[(int)Events.Q_16_3_D]++;

                        if (client.Event[(int)Events.Q_16_3_D] >= 3)
                            Write(client, Relays.MetaViewPortHandlerNameC, 0x07, 0x00, 0x0d, character.Id); // nothing

                        Write(client, Relays.MetaClientSupplyDockPanel, 0x04, 0x00, 0x0d, character.Id); // nothing
                        Write(client, Relays.MetaClientShipPanel, 0x05, 0x00, 0x0d, character.Id); // nothing

                        // checks if we are in the end of the login phase

                        if (client.Event[(int)Events.Q_16_3_D] == 3)
                        {
                            Debug.WriteLine(character.CharacterName + " joined the game server");

                            // tries to send an UDP message to the client's launcher to confirm his local ip address

                            long address = GetAddress(client);

                            if (address > 0)
                            {
                                Clear();

                                Push(client.Id);
                                Push((byte)1);
                                Push(9);

                                byte[] msg = GetStack();

                                _port27000.Enqueue(address, 27001, msg);
                            }

                            // updates the character

                            Contract.Assert(character.State == Character.States.IsHumanBusyConnecting);

                            character.State = Character.States.IsHumanOnline;
                        }

                        break;
                    }
                case 0x0e:
                    {
                        Write(client, Relays.MetaClientSupplyDockPanel, 0x05, 0x00, 0x0e, character.Id); // nothing

                        break;
                    }

#if DEBUG
                default:
                    {
                        Debugger.Break();

                        break;
                    }
#endif

            }
        }

        // AVtSecurityRelayS

        private void Q_18_2(Client27000 client, int i4, int i5, int i6, byte[] buffer)
        {
            /*
                [Q] 630a0000000000000018000000020000004e0a0000010100000002000000020001005c0000000e000000626f6e5f6865617465722e736372e83bc09a0b000000626f6e5f6869742e736372196b775713000000626f6e5f7069656365616374696f6e2e7363727ddfabd30b0000006674726c6973742e7478744d3265951000000067656e5f706c61796261636b2e736372a0a2b5cc100000006d65745f3130706174726f6c2e7363729e260a1e140000006d65745f3131636f6e766f79726169642e736372c0dce4fd160000006d65745f3132636f6e766f796573636f72742e736372b52b9168110000006d65745f31336d6f6e737465722e736372b521cc4f100000006d65745f3134656e69676d612e736372b23d3c76150000006d65745f313562617365646566656e73652e736372f8c26743150000006d65745f313673686970646566656e73652e7363724ae988b6100000006d65745f3137706174726f6c2e736372bdd893041a0000006d65745f3138686f6d65776f726c6461737361756c742e736372200dacf90e0000006d65745f31397363616e2e7363727498a0910e0000006d65745f3173636f75742e736372dd990f59120000006d65745f323073757072697365722e736372024fea8d160000006d65745f3231646973747265737363616c6c2e7363724e2e1641120000006d
                65745f32326469706c6f6d61742e736372980a1c0d120000006d65745f323371756172746572732e73637279a2f2ca110000006d65745f3234616e6f6d616c792e73637208705068140000006d65745f32357375706572666c6565742e73637211702d6c190000006d65745f323661737465726f696461737375616c742e73637287c77cd4190000006d65745f323761737465726f6964646566656e73652e73637268c3d5ac150000006d65745f32386e65676f74696174696f6e2e7363723bc805e0120000006d65745f323972656368617267652e73637251b1aa26160000006d65745f32686f6c64696e67616374696f6e2e7363720de77e23110000006d65745f333073616c766167652e736372d3b9283c130000006d65745f333165706963656e7465722e736372d35a0b6a110000006d65745f33616d6275736865652e73637206a18259110000006d65745f34616d6275736865722e736372875dc415140000006d65745f35666c656574616374696f6e2e7363728ca218c90f0000006d65745f36706174726f6c2e7363727fcbd8e1140000006d65745f376261736561737361756c742e736372356ae6d8140000006d65745f387368697061737361756c742e7363724db299f5190000006d65745f39706c616e657461727961737361756c742e73637213843b960e0000006d65745f636f6d6d6f6e2e736372556
                bed861c0000006d65745f7374617262617365636f6e737472756374696f6e2e73637231bb6ee8100000006d756c5f3130686f636b65792e7363720a325a0d120000006d756c5f313174696e79666573742e736372a97df698120000006d756c5f3132736c7567666573742e73637281d8832a160000006d756c5f313372616e646f6d626174746c652e7363724a144fa4150000006d756c5f313474696d6564626174746c652e7363729df35bdd140000006d756c5f31367375706572666c6565742e7363724a659b25110000006d756c5f31376d6f6e737465722e7363723d062bcc150000006d756c5f31386d6f6e737465726d6173682e736372638abdd4130000006d756c5f31397363616e68617070792e73637257fa404e110000006d756c5f316672656534616c6c2e7363725ece7273120000006d756c5f323073746172626173652e736372d538fce8140000006d756c5f326261736561737361756c742e736372ea1e6e38140000006d756c5f33626174746c6566657374732e73637242b3ba66140000006d756c5f34746f75726e6579666573742e736372be41b943170000006d756c5f35626174746c65666573746c6974652e736372d946f470180000006d756c5f36746f75726e6579666573746c6974652e736372912d1ddc100000006d756c5f37746f75726e65792e7363728f1204f8140000006d756c5f
                397465616d61737361756c742e736372962bd709100000006d756c5f696e7472756465722e7363722f001a800c0000006d756c5f74776f6b2e736372790c5af60c000000736869706c6973742e747874250bd91211000000736b695f316672656534616c6c2e736372629bc32512000000736b695f677265656e6e676f6c642e736372944711570c000000736b695f686f6f642e7363722a04186d12000000736b695f7375706572666c6565742e7363720ed362c917000000736b695f7375707269736572657665727365642e7363725a3d8e5d0e000000736b695f7473686f6f742e7363729794f6940c000000736b695f74776f6b2e736372eac0928a12000000736b695f7761726f66726f7365732e7363723adca6580d000000736b695f777265636b2e73637220389df90f00000073746172666c6565746f702e657865a095ca290d0000007475745f7832355f312e736372a7c445df0d0000007475745f7832355f322e7363721de79be60d0000007475745f7832355f332e7363724e228cfd0d0000007475745f7832355f342e736372dc73c82b160000007475745f7832355f636f6d6d616e643139302e736372c896b06e160000007475745f7832355f636f6d6d616e643239302e7363726c86468d160000007475745f7832355f636f6d6d616e643539302e736372d1be6059160000007475745f7832355f73636
                9656e63653331302e7363725dc1d2ee160000007475745f7832355f776561706f6e733338302e736372bb321300160000007475745f7832355f776561706f6e733438302e7363723ef547000f0000007832355f62756768756e742e7363720e28e7e2120000007832355f636174636874686965662e73637203414f790f0000007832355f64656164656e642e736372ddd6260a0c0000007832355f666163652e736372893d0d37100000007832355f68656c6c6261636b2e736372cb788cba0c0000007832355f686972652e7363729af482d30d0000007832355f686f6e6f722e7363723cb7518c110000007832355f6c61776e6f726465722e736372d2a82e110e0000007832355f6c6567656e642e736372ad571ac9110000007832355f6c6567656e646172792e7363726bb660830c0000007832355f727573652e736372c8241a630f0000007832355f746865746573742e7363727d08d5750e0000007832355f74726176656c2e73637260cc0214480000003338316261396338313661353765623265333531346562333562396432663235306363353333643030373835653230396632373730303064396438343766376630663066323339310c0000003139322e3136382e312e373112000000643476316b7340686f746d61696c2e636f6d00000000000000000000000000000000ffffffffdc0500000000000000
                00000040fc3503ffffffffffffffffffffffffffffffffffffffffffffffff0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
            */

            string warning;

            // checks if the server is currently locked

            if (_locked > 0)
            {
                warning = "The server is currently closed for maintenance. Please try again later.";

                goto sendWarning;
            }

            // gets the number of files

            int c = BitConverter.ToInt32(buffer, 34);
            int p = 38;

            // gets the names and CRCs of the files

            Contract.Assert(_dictStringUInt.Count == 0);

            for (int i = c; i > 0; i--)
            {
                c = BitConverter.ToInt32(buffer, p);
                p += 4;

                string file = Encoding.UTF8.GetString(buffer, p, c);

                p += c;

                uint crc = BitConverter.ToUInt32(buffer, p);

                p += 4;

                _dictStringUInt.Add(file, crc);
            }

            // skips the hash key

            c = BitConverter.ToInt32(buffer, p);
            p += 4 + c;

            // gets the ip

            c = BitConverter.ToInt32(buffer, p);
            p += 4;

            string ipAddress = Encoding.UTF8.GetString(buffer, p, c);

            p += c;

            // gets the email

            c = BitConverter.ToInt32(buffer, p);
            p += 4;

            string wonLogon = Encoding.UTF8.GetString(buffer, p, c);

            p += c;

            // gets the unknown value

            p += 32;

            int unknown = BitConverter.ToInt32(buffer, p);

            Contract.Assert(unknown != 0);

            // checks if the security check was successful

            if (TryValidateClientFiles(_dictStringUInt, out warning))
            {
                AddOrUpdateCharacter(client, ipAddress, wonLogon);

                // [R] 3a0000000001000000020000000200010025000000 0100000000000000 190000005375636365737366756c20736563757269747920636865636b

                Clear();

                Push(_data[(int)Data.SuccessfulSecurityCheck]);
                Push(0x00);
                Push(0x01);
                Push(i4, i5, i6);

                Write(client);

                goto afterWarning;
            }

        sendWarning:

            // [R] d300000000020000000200000002000100be000000 0100000001000000 b20000005468697320736572766572206861732064657465637465642074686174206f6e65206f72206d6f7265206f6620746865206e65636573736172792066696c657320726571756972656420746f20636f6e6e656374206f7220656974686572206d697373696e67206f7220696e636f6d70617469626c652e20204c697374206f66206f6666656e64696e672066696c65733a206d65745f78706174726f6c2e7363727c204d697373696e672046696c65207c20

            Clear();

            Push(warning);
            Push(0x01);
            Push(0x01);
            Push(i4, i5, i6);

            Write(client);

        afterWarning:

            _dictStringUInt.Clear();
        }

        private void Q_18_3(Client27000 client, int i4, int i5, int i6)
        {
            // [Q] 22000000 00 000000001800000003000000 0d000000 01 010000000200000001000100
            // [R] 19000000 00 010000000200000001000100 04000000 01000000

            Clear();

            Push(0x01);
            Push(i4, i5, i6);

            Write(client);
        }

        private static void Q_18_4()
        {
            // 25000000 00 000000001800000004000000 10000000 0c0000003139322e3136382e312e3731
        }

        // Messages

        private void M_FullCharacter(Client27000 client, int i4, int i5, int i6, int opcode)
        {
            Clear();

            Push((byte)0x00);
            Push(0x01);
            Push(_data[(int)Data.master5_1]);
            Push(client.Character);
            Push(opcode);
            Push(i4, i5, i6);

            Write(client);
        }

        private void M_NullCharacter(Client27000 client, int i4, int i5, int i6, int opcode)
        {
            Clear();

            Push((byte)0x00);
            Push(0x00);
            Push(_data[(int)Data.master5_1]);
            Push(_data[(int)Data.master5_0]);
            Push(opcode);
            Push(i4, i5, i6);

            Write(client);
        }

        private void M_EndMovement(Character character, Client27000 client)
        {
            Clear();

            Push(0x00);
            Push(0x00);
            Push(character.MoveDestinationY);
            Push(character.MoveDestinationX);
            Push(character.Id);
            Push(0x00);
            Push(client.Id, client.Relay[(int)Relays.MetaViewPortHandlerNameC], 0x04);

            Write(client);
        }

        private void M_Ping(Client27000 client)
        {
            Clear();

            Push(_data[(int)Data.pingRequest]);

            Write(client);
        }

        private void M_Turn(Client27000 client)
        {
            // 3a000000 00 020000000e00000001000000 25000000 1200000038f8af036d010000c0270900d7080000000000000a000000140000002800000000

            Clear();

            Push((byte)0x00);
            PushStardate();
            Push(client.Id, client.Relay[(int)Relays.MetaViewPortHandlerNameC], 0x01);

            Write(client);

            // 3a000000 00 020000000900000001000000 25000000 1200000038f8af036d010000c0270900d7080000000000000a000000140000002800000000

            Clear();

            Push((byte)0x00);
            PushStardate();
            Push(client.Id, client.Relay[(int)Relays.PlayerInfoPanel], 0x01);

            Write(client);
        }

        // Requests

        private void AvailableRequests(Client27000 client, Character character)
        {
            // single use

            Write(client, Relays.MetaViewPortHandlerNameC, 0x07, 0x00, 0x0d, character.Id); // nothing (Q_16_3)

            Write(client, Relays.PlayerRelayC, 0x03, 0x00, 0x03, character.Id); // 15_10 (Q_16_3)
            Write(client, Relays.PlayerRelayC, 0x04, 0x00, 0x05, character.Id); // A_5 (bid)

            Write(client, Relays.MetaClientPlayerListPanel, 0x02, 0x00, 0x00, character.Id); // 15_8 (login)
            Write(client, Relays.MetaClientPlayerListPanel, 0x03, 0x00, 0x01, character.Id); // nothing (logout)

            Write(client, Relays.MetaClientShipPanel, 0x05, 0x00, 0x0d, character.Id); // nothing (Q_16_3)

            Write(client, Relays.MetaClientSupplyDockPanel, 0x04, 0x00, 0x0d, character.Id); // nothing (Q_16_3)
            Write(client, Relays.MetaClientSupplyDockPanel, 0x05, 0x00, 0x0e, character.Id); // nothing (Q_16_3)

            // general use

            // hex data (partial or complete)
            Write(client, Relays.MetaViewPortHandlerNameC, 0x03, 0x02, 0x00, (character.CharacterLocationX << 16) + character.CharacterLocationY); // D_3 (or -1)
            // hex icons
            Write(client, Relays.MetaViewPortHandlerNameC, 0x06, 0x00, 0x0f, 0x00); // 15_F

            // character data
            Write(client, Relays.PlayerRelayC, 0x02, 0x00, 0x02, character.Id); // 15_8 (not used)
            // hex data + character destination
            Write(client, Relays.PlayerRelayC, 0x03, 0x00, 0x04, character.Id); // 15_10
            // ship data (entire fleet)
            Write(client, Relays.PlayerRelayC, 0x04, 0x00, 0x06, character.Id); // A_5
            // character.CharacterCurrentPrestige
            Write(client, Relays.PlayerRelayC, 0x05, 0x00, 0x07, character.Id); // 15_12
            // bid stuff
            Write(client, Relays.PlayerRelayC, 0x06, 0x00, 0x08, character.Id); // 15_13
            // character.CharacterRank  
            Write(client, Relays.PlayerRelayC, 0x07, 0x00, 0x0b, character.Id); // 15_14
            // character.Awards 
            Write(client, Relays.PlayerRelayC, 0x08, 0x00, 0x0c, character.Id); // 15_15 (not used)

            // news query
            Write(client, Relays.MetaClientNewsPanel, 0x03, 0x03, 0x00, -1); // 10_2 (or anything >= 0)
        }
    }
}
