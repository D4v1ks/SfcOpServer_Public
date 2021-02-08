#pragma warning disable CA1051

using System.IO;

namespace SfcOpServer
{
    public class Location
    {
        public int X;
        public int Y;
        public int Z;

        public Location(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Location(BinaryReader r)
        {
            X = r.ReadInt32();
            Y = r.ReadInt32();
            Z = r.ReadInt32();
        }

        public void WriteTo(BinaryWriter w)
        {
            w.Write(X);
            w.Write(Y);
            w.Write(Z);
        }
    }
}
