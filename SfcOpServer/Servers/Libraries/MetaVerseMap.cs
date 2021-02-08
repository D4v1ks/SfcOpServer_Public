#pragma warning disable CA1031, CA1034, CA1815, IDE1006

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Text;

namespace SfcOpServer
{
    public class MetaVerseMap
    {
        public enum eVersion : int
        {
            None,

            Sfc2Eaw,
            Sfc2Op,
            Sfc3,

            Total
        }
        private enum eClass : int
        {
            Regions,
            CartelRegions,
            Terrain,
            Planets,
            Bases,

            Total
        }

        public struct tCell
        {
            public int Economic { get; set; }
            public float Impedence { get; set; }
            public int Strength { get; set; }
            public int Region { get; set; }
            public int CartelRegion { get; set; }
            public int Terrain { get; set; }
            public int Planet { get; set; }
            public int Base { get; set; }

            public tCell(eVersion version, BinaryReader r)
            {
                Contract.Requires(r != null);

                Economic = r.ReadInt32();
                Impedence = r.ReadSingle();
                Strength = r.ReadInt32();

                Region = r.ReadInt32();

                if (version >= eVersion.Sfc2Op)
                    CartelRegion = r.ReadInt32();
                else
                    CartelRegion = -1;

                Terrain = r.ReadInt32();
                Planet = r.ReadInt32();
                Base = r.ReadInt32();
            }
        }

        private static eVersion _version;
        private static string[] _classes;
        private static string[][] _values;

        public eVersion Version { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<tCell> Cells { get; }

        private static void Initialize(eVersion version)
        {

#if DEBUG
            Contract.Assert(version > eVersion.None && version < eVersion.Total);
#endif
            if (_version == version)
                return;

            _version = version;

            _classes = new string[] {
                "Regions", "CartelRegions", "Terrain", "Planets", "Bases"
            };

            switch (version)
            {
                case eVersion.Sfc2Eaw:
                    {
                        throw new NotImplementedException();
                    }

                case eVersion.Sfc2Op:
                    {
                        _values = new string[][] {
                            new string[] { "Neutral", "Federation", "Klingon", "Romulan", "Lyran", "Hydran", "Gorn", "ISC", "Mirak" },
                            new string[] { "Neutral", "OrionOrion", "OrionKorgath", "OrionPrime", "OrionTigerHeart", "OrionBeastRaiders", "OrionSyndicate", "OrionWyldeFire", "OrionCamboro" },
                            new string[] { "Space 1", "Space 2", "Space 3", "Space 4", "Space 5", "Space 6", "Asteroid 1", "Asteroid 2", "Asteroid 3", "Asteroid 4", "Asteroid 5", "Asteroid 6", "Nebula 1", "Nebula 2", "Nebula 3", "Nebula 4", "Nebula 5", "Nebula 6", "Blackhole1", "Blackhole2", "Blackhole3", "Blackhole4", "Blackhole5", "Blackhole6" },
                            new string[] { "(none)", "Homeworld 1", "Homeworld 2", "Homeworld 3", "Core World 1", "Core World 2", "Core World 3", "Colony 1", "Colony 2", "Colony 3", "Asteroid Base 1", "Asteroid Base 2", "Asteroid Base 3" },
                            new string[] { "(none)", "Starbase", "Battle Station", "Base Station", "Weapons Platform", "Listening Post" }
                        };

                        break;
                    }

                case eVersion.Sfc3:
                    {
                        throw new NotImplementedException();
                    }
            }
        }

        private static int GetNormalizedIndex(GameFile h, eClass c, int i)
        {
            Contract.Requires(h != null);

            string k = "Classes/" + _classes[(int)c];

            if (h.TryGetValue(k, i.ToString(CultureInfo.InvariantCulture), out string v, out _))
                return TryGetIndex(c, v);

            return 0;
        }

        private static int TryGetIndex(eClass key, string value)
        {
            int k = (int)key;

            for (int i = 0; i < _values[k].Length; i++)
            {
                if (_values[k][i].Equals(value, StringComparison.Ordinal))
                    return i;
            }

            return 0;
        }

        public MetaVerseMap()
        {
            Cells = new List<tCell>();

            Clear();
        }

        public bool Load(string filename)
        {
            GameFile h = new GameFile();

            if (!h.Load(filename))
                return false;

            FileStream f = null;
            byte[] d = null;

            try
            {
                f = new FileStream(filename, FileMode.Open, FileAccess.Read);

                int c = (int)f.Length;

                if (c > 0)
                {
                    d = new byte[c];

                    f.Read(d, 0, c);
                }
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                f?.Close();
            }

            if (d == null)
                return false;

            try
            {
                const string ObjectReference = "[Objects]\r\n";

                byte[] b = Encoding.ASCII.GetBytes(ObjectReference);

                int i = Utils.Contains(d, d.Length, b);

                if (i == 0)
                    return false;

                i += ObjectReference.Length;

                while (d[i] == 0)
                    i++;

                int width = BitConverter.ToInt32(d, i);

                i += 4;

                if (width < 8 || width > 1000)
                    return false;

                int height = BitConverter.ToInt32(d, i);

                i += 4;

                if (height < 8 || height > 1000)
                    return false;

                int size = BitConverter.ToInt32(d, i);

                i += 4;

                eVersion version;

                if (d.Length - i == size * 32)
                    version = h.ContainsKey("Classes/CartelRegions", "1") ? eVersion.Sfc2Op : eVersion.Sfc3;
                else if (d.Length - i == size * 28)
                    version = eVersion.Sfc2Eaw;
                else
                    return false;

                Initialize(version);

                using MemoryStream m = new MemoryStream(d);
                using BinaryReader r = new BinaryReader(m, Encoding.UTF8, true);

                m.Seek(i, SeekOrigin.Begin);

                for (i = 0; i < size; i++)
                {
                    tCell cell = new tCell(version, r);

                    if (cell.Economic > 100)
                        cell.Economic = 100;

                    if (cell.Impedence > 2f)
                        cell.Impedence = 2f;

                    if (cell.Strength > 200)
                        cell.Strength = 200;

                    cell.Region = GetNormalizedIndex(h, eClass.Regions, cell.Region);
                    cell.CartelRegion = GetNormalizedIndex(h, eClass.CartelRegions, cell.CartelRegion);

                    cell.Terrain = GetNormalizedIndex(h, eClass.Terrain, cell.Terrain);
                    cell.Planet = GetNormalizedIndex(h, eClass.Planets, cell.Planet);
                    cell.Base = GetNormalizedIndex(h, eClass.Bases, cell.Base);

                    Cells.Add(cell);
                }

                Version = version;

                Width = width;
                Height = height;

                return true;
            }
            catch (Exception)
            { }

            return false;
        }

        public void Clear()
        {
            // static

            _version = eVersion.None;

            _classes = null;
            _values = null;

            // public

            Version = eVersion.None;

            Width = -1;
            Height = -1;

            Cells.Clear();
        }
    }
}
