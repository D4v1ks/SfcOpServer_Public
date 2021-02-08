using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace SfcOpServer
{
    public static class Utils
    {
        public static bool EqualsTo(byte[] buffer, int bufferLength, byte[] data)
        {
            return new ReadOnlySpan<byte>(buffer, 0, bufferLength).SequenceEqual(new ReadOnlySpan<byte>(data));
        }

        public static int Contains(byte[] buffer, int bufferLength, byte[] data)
        {
            return new ReadOnlySpan<byte>(buffer, 0, bufferLength).IndexOf(new ReadOnlySpan<byte>(data));
        }

        public static bool StartsWith(byte[] buffer, int bufferLength, byte[] data)
        {
            return new ReadOnlySpan<byte>(buffer, 0, bufferLength).StartsWith(new ReadOnlySpan<byte>(data));
        }

        public static bool EndsWith(byte[] buffer, int bufferLength, byte[] data)
        {
            return new ReadOnlySpan<byte>(buffer, 0, bufferLength).EndsWith(new ReadOnlySpan<byte>(data));
        }

        public static void GetArguments(byte[] buffer, int size, ref Dictionary<string, string> d)
        {
            Contract.Requires(buffer != null && d != null);

            int i = 0;

            while (i < size && buffer[i] == 92)
            {
                // tries to get a key

                int i1 = i;

                do
                {
                    i++;

                    if (i >= size)
                        return;
                }
                while (buffer[i] != 92);

                // ignores all pairs with empty keys

                if (i - i1 == 1)
                    continue;

                // tries to get a value

                int i2 = i;

                do
                {
                    i++;
                }
                while (i < size && buffer[i] != 92);

                // adds or updates the new pair

                i1++;

                string k = Encoding.UTF8.GetString(buffer, i1, i2 - i1);

                i2++;

                string v = Encoding.UTF8.GetString(buffer, i2, i - i2);

                if (d.ContainsKey(k))
                    d[k] = v;
                else
                    d.Add(k, v);
            }
        }

        public static void ReplacePattern(byte[][] buffer, byte patternValue, int patternSize, byte[] replacement)
        {
            Contract.Requires(buffer != null);

            // pattern

            byte[] pattern = new byte[patternSize];

            Array.Fill(pattern, patternValue);

            // replacement

            Contract.Assert(replacement != null && replacement.Length == patternSize);

            ReadOnlySpan<byte> p = new ReadOnlySpan<byte>(pattern);

            for (int i = 0; i < buffer.Length; i++)
            {
                int j = 0;
                int k = buffer[i].Length;

                do
                {
                    ReadOnlySpan<byte> b = new ReadOnlySpan<byte>(buffer[i], j, k - j);

                    int l = b.IndexOf(p);

                    if (l == -1)
                        break;

                    Array.Copy(replacement, 0, buffer[i], l, patternSize);

                    j = l + patternSize;
                }
                while (j < k);
            }
        }

        public static string GetHex(byte[] buffer, int index, int count)
        {
            Contract.Requires(buffer != null);

            if (count > 0)
            {
                StringBuilder r = new StringBuilder(count << 1);

                for (int i = index; i < index + count; i++)
                    r.Append(buffer[i].ToString("x2", CultureInfo.InvariantCulture));

                return r.ToString();
            }

            return string.Empty;
        }

        public static string GetHex(byte[] buffer, int count)
        {
            Contract.Requires(buffer != null);

            return GetHex(buffer, 0, count);
        }

        public static string GetHex(byte[] buffer)
        {
            Contract.Requires(buffer != null);

            return GetHex(buffer, 0, buffer.Length);
        }

        public static string GetRandomASCII(int length)
        {
            byte[] b = new byte[length];

            RandomizeASCII(ref b);

            return Encoding.ASCII.GetString(b);
        }

        private static void RandomizeASCII(ref byte[] buffer)
        {
            Random r = new Random();

            for (int i = 0; i < buffer.Length; i++)
            {
                const int maxValue = 10 + 26 + 26;

                int j = r.Next(maxValue);

                if (j < 10)
                    buffer[i] = (byte)(j + 48); // 0-9
                else if (j < 36)
                    buffer[i] = (byte)(j + 55); // A-Z
                else
                    buffer[i] = (byte)(j + 61); // a-z
            }
        }

        public static byte[] HexToArray(string h)
        {
            Contract.Requires(h != null);
            Contract.Assert(h.Length > 0 && (h.Length & 1) == 0);

            int c = h.Length >> 1;
            byte[] b = new byte[c];

            for (int i = 0; i < c; i++)
                b[i] = byte.Parse(h.Substring(i << 1, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

            return b;
        }

        public static uint HexToUInt(byte[] buffer, int index)
        {
            Contract.Requires(buffer != null);
            Contract.Assert(buffer.Length - index >= 8);

            uint b0 = HexToByte(buffer, index);
            uint b1 = HexToByte(buffer, index + 2);
            uint b2 = HexToByte(buffer, index + 4);
            uint b3 = HexToByte(buffer, index + 6);

            b0 += (b1 << 8) + (b2 << 16) + (b3 << 24);

            return b0;
        }

        public static uint HexToByte(byte[] buffer, int index)
        {
            Contract.Requires(buffer != null);
            Contract.Assert(buffer.Length - index >= 2);

            uint h1 = buffer[index];
            uint h0 = buffer[index + 1];

            if (h1 >= 97)
                h1 -= 87;
            else if (h1 >= 65)
                h1 -= 55;
            else
                h1 -= 48;

            if (h0 >= 97)
                h0 -= 87;
            else if (h0 >= 65)
                h0 -= 55;
            else
                h0 -= 48;

            h0 += h1 << 4;

            return h0;
        }

        public static void ReadString(BinaryReader r, out string t)
        {
            int c = r.ReadInt32();

            if (c == 0)
                t = string.Empty;
            else
                t = Encoding.UTF8.GetString(r.ReadBytes(c));
        }

        public static void WriteString(BinaryWriter w, string t)
        {
            if (t.Length == 0)
                w.Write(0);
            else
            {
                w.Write(t.Length);
                w.Write(Encoding.UTF8.GetBytes(t));
            }
        }

        // optimized comparison functions

        public unsafe static bool Compare(byte[] arr0, byte[] arr1)
        {
            if (arr0 == arr1)
                return true;

            if (arr0 == null || arr1 == null)
                return false;

            if (arr0.Length != arr1.Length)
                return false;

            if (arr0.Length == 0)
                return true;

            fixed (byte* b0 = arr0, b1 = arr1)
            {
                if (Avx2.IsSupported)
                    return Compare256(b0, b1, arr0.Length);

                if (Sse2.IsSupported)
                    return Compare128(b0, b1, arr0.Length);

                return Compare64(b0, b1, arr0.Length);
            }
        }

        public unsafe static bool Compare256(byte* b0, byte* b1, int length)
        {
            const int mask = -1;

            byte* lastAddr = b0 + length;
            byte* lastAddrMinus128 = lastAddr - 128;

            // unroll the loop so that we are comparing 128 bytes at a time.

            while (b0 < lastAddrMinus128)
            {
                if (Avx2.MoveMask(Avx2.CompareEqual(Avx.LoadVector256(b0), Avx.LoadVector256(b1))) != mask)
                    return false;

                if (Avx2.MoveMask(Avx2.CompareEqual(Avx.LoadVector256(b0 + 32), Avx.LoadVector256(b1 + 32))) != mask)
                    return false;

                if (Avx2.MoveMask(Avx2.CompareEqual(Avx.LoadVector256(b0 + 64), Avx.LoadVector256(b1 + 64))) != mask)
                    return false;

                if (Avx2.MoveMask(Avx2.CompareEqual(Avx.LoadVector256(b0 + 96), Avx.LoadVector256(b1 + 96))) != mask)
                    return false;

                b0 += 128;
                b1 += 128;
            }

            while (b0 < lastAddr)
            {
                if (*b0 != *b1)
                    return false;

                b0++;
                b1++;
            }

            return true;
        }

        public unsafe static bool Compare128(byte* b0, byte* b1, int length)
        {
            const int mask = 0xFFFF;

            byte* lastAddr = b0 + length;
            byte* lastAddrMinus64 = lastAddr - 64;

            // unroll the loop so that we are comparing 64 bytes at a time.

            while (b0 < lastAddrMinus64)
            {
                if (Sse2.MoveMask(Sse2.CompareEqual(Sse2.LoadVector128(b0), Sse2.LoadVector128(b1))) != mask)
                    return false;

                if (Sse2.MoveMask(Sse2.CompareEqual(Sse2.LoadVector128(b0 + 16), Sse2.LoadVector128(b1 + 16))) != mask)
                    return false;

                if (Sse2.MoveMask(Sse2.CompareEqual(Sse2.LoadVector128(b0 + 32), Sse2.LoadVector128(b1 + 32))) != mask)
                    return false;

                if (Sse2.MoveMask(Sse2.CompareEqual(Sse2.LoadVector128(b0 + 48), Sse2.LoadVector128(b1 + 48))) != mask)
                    return false;

                b0 += 64;
                b1 += 64;
            }

            while (b0 < lastAddr)
            {
                if (*b0 != *b1)
                    return false;

                b0++;
                b1++;
            }

            return true;
        }

        public unsafe static bool Compare64(byte* b0, byte* b1, int length)
        {
            byte* lastAddr = b0 + length;
            byte* lastAddrMinus32 = lastAddr - 32;

            // unroll the loop so that we are comparing 32 bytes at a time.

            while (b0 < lastAddrMinus32)
            {
                if (*(ulong*)b0 != *(ulong*)b1)
                    return false;

                if (*(ulong*)(b0 + 8) != *(ulong*)(b1 + 8))
                    return false;

                if (*(ulong*)(b0 + 16) != *(ulong*)(b1 + 16))
                    return false;

                if (*(ulong*)(b0 + 24) != *(ulong*)(b1 + 24))
                    return false;

                b0 += 32;
                b1 += 32;
            }

            while (b0 < lastAddr)
            {
                if (*b0 != *b1)
                    return false;

                b0++;
                b1++;
            }

            return true;
        }
    }
}
