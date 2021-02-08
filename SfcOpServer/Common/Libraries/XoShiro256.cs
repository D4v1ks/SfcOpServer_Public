using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace SfcOpServer
{
    public class XoShiro256
    {
        private static readonly long[][] j = new long[][]
        {
            new long[] { 1733541517147835066L, -3051731464161248980L, -6244198995065845334L, 4155657270789760540L },
            new long[] { 8566230491382795199L, -4251311993797857357L, 8606660816089834049L, 4111957640723818037L }
        };

        // initial seed

        private long k;

        // current seeds

        private ulong k0, k1, k2, k3;

        public XoShiro256()
        {

#if DEBUG
            RunInternalTest();
#endif

            Seed();
        }

        public XoShiro256(long value)
        {

#if DEBUG
            RunInternalTest();
#endif

            Seed(value);
        }

        public void Seed()
        {
            k = (Environment.TickCount + 2305843008139952128L) * 2685821657736338717L;

            k0 = SplitMix64();
            k1 = SplitMix64();
            k2 = SplitMix64();
            k3 = SplitMix64();
        }

        public void Seed(long value)
        {
            k = value;

            k0 = SplitMix64();
            k1 = SplitMix64();
            k2 = SplitMix64();
            k3 = SplitMix64();
        }

        public void Jump(int interval)
        {
            ulong s0 = 0UL;
            ulong s1 = 0UL;
            ulong s2 = 0UL;
            ulong s3 = 0UL;

            for (int a = 0; a < 4; a++)
            {
                for (int b = 0; b < 64; b++)
                {
                    if ((j[interval][a] & (1L << b)) != 0L)
                    {
                        s0 ^= k0;
                        s1 ^= k1;
                        s2 ^= k2;
                        s3 ^= k3;
                    }

                    Next(out ulong _);
                }
            }

            k0 = s0;
            k1 = s1;
            k2 = s2;
            k3 = s3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double NextDouble()
        {
            const double F = 1.0 / (1UL << 52);

            Next(out ulong r);

            return (r >> 12) * F;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float NextSingle()
        {
            const double F = 1.0 / (1UL << 23);

            Next(out ulong r);

            return (float)((r >> 41) * F);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong NextUInt64()
        {
            Next(out ulong r);

            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long NextInt64()
        {
            Next(out ulong r);

            return (long)r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long NextInt64(long count)
        {
            Next((ulong)(count - 1L), out ulong r);

            return (long)r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long NextInt64(long minValue, long maxValue)
        {
            Next((ulong)(maxValue - minValue), out ulong r);

            return minValue + (long)r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint NextUInt32()
        {
            Next(out ulong r);

            return (uint)(r >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt32()
        {
            Next(out ulong r);

            return (int)(r >> 32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt32(int count)
        {
            Next((ulong)(count - 1), out ulong r);

            return (int)r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NextInt32(int minValue, int maxValue)
        {
            Next((ulong)(maxValue - minValue), out ulong r);

            return minValue + (int)r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort NextUInt16()
        {
            Next(out ulong r);

            return (ushort)(r >> 48);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short NextInt16()
        {
            Next(out ulong r);

            return (short)(r >> 48);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte NextByte()
        {
            Next(out ulong r);

            return (byte)(r >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte NextSByte()
        {
            Next(out ulong r);

            return (sbyte)(r >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool NextBoolean()
        {
            Next(out ulong r);

            return (r >> 63) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public unsafe void NextBytes(byte[] b, int c)
        {
            Contract.Requires(b != null);

            fixed (byte* b0 = b)
            {
                byte* b1 = b0;
                byte* b2 = b0 + c;
                byte* b3 = b0 + (c & -8);

                ulong r;

                while (b1 < b3)
                {
                    Next(out r);

                    *(ulong*)b1 = r;

                    b1 += 8;
                }

                if (b1 < b2)
                {
                    Next(out r);

                    b3 = (byte*)&r;

                    do
                    {
                        *b1 = *b3;

                        b1++;
                        b3++;
                    }
                    while (b1 < b2);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Next(ulong maxValue, out ulong r)
        {
            ulong m = maxValue;

            m |= m >> 1;
            m |= m >> 2;
            m |= m >> 4;
            m |= m >> 8;
            m |= m >> 16;
            m |= m >> 32;

            do
            {
                Next(out r);

                r &= m;
            }
            while (r > maxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void Next(out ulong r)
        {
            ulong s0 = k0;
            ulong s1 = k1;
            ulong s2 = k2;
            ulong s3 = k3;

            r = s0 + s3;

            s2 ^= s0;
            s3 ^= s1;

            r = ((r << 23) | (r >> 41)) + s0;

            k0 = s0 ^ s3;
            k1 = s1 ^ s2;
            k2 = s2 ^ (s1 << 17);
            k3 = (s3 << 45) | (s3 >> 19);
        }

#if DEBUG
        private void RunInternalTest()
        {
            k0 = 15471220080371608766UL;
            k1 = 3280365575269808125UL;
            k2 = 18274576499568730556UL;
            k3 = 906047308095828067UL;

            ulong[][] r = new ulong[][]
            {
                new ulong[] { 14727167842594214044UL, 6004944704269006089UL, 12510381582720191771UL },
                new ulong[] { 5006134394253252361UL, 14701479888315032639UL, 1906288635787975297UL },
                new ulong[] { 1322796781459574627UL, 2435732478456631749UL, 13512522216230553622UL }
            };

            Contract.Assert(NextUInt64() == r[0][0]);
            Contract.Assert(NextUInt64() == r[0][1]);
            Contract.Assert(NextUInt64() == r[0][2]);

            Jump(0);

            Contract.Assert(NextUInt64() == r[1][0]);
            Contract.Assert(NextUInt64() == r[1][1]);
            Contract.Assert(NextUInt64() == r[1][2]);

            Jump(1);

            Contract.Assert(NextUInt64() == r[2][0]);
            Contract.Assert(NextUInt64() == r[2][1]);
            Contract.Assert(NextUInt64() == r[2][2]);

            Seed(0);

            byte[] b = new byte[12];

            Stopwatch w = new Stopwatch();

            w.Start();

            for (int i = 0; i < 100000; i++)
            {
                NextDouble();
                NextSingle();

                NextUInt64();
                NextInt64();

                int i64 = NextInt32(-32, 31);

                Contract.Assert(i64 >= -32 && i64 <= 31);

                NextUInt32();
                NextInt32();

                int i32 = NextInt32(-8, 7);

                Contract.Assert(i32 >= -8 && i32 <= 7);

                NextUInt16();
                NextInt16();

                NextByte();
                NextSByte();

                NextBoolean();

                NextBytes(b, b.Length);
            }

            w.Stop();

            // Debug.WriteLine(w.ElapsedMilliseconds);
        }
#endif

        private ulong SplitMix64()
        {
            long r = k;

            r -= 7046029254386353131L;

            k = r;

            r = (r ^ (r >> 30)) * -4658895280553007687L;
            r = (r ^ (r >> 27)) * -7723592293110705685L;

            return (ulong)(r ^ (r >> 31));
        }
    }
}
