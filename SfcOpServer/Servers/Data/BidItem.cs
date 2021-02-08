#pragma warning disable CA1051

using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public class BidItem
    {
        public int Id;
        public int LockID;

        public byte BiddingHasBegun;

        public string ShipClassName;
        public int ShipId;
        public int ShipBPV;

        public int AuctionValue;
        public double AuctionRate;

        public int TurnOpened;
        public int TurnToClose;
        public int CurrentBid;

        public int BidOwnerID;
        public int TurnBidMade;
        public int BidMaximum;

        public BidItem()
        { }

        public BidItem(BinaryReader r)
        {
            Contract.Requires(r != null);

            Id = r.ReadInt32();
            LockID = r.ReadInt32();

            BiddingHasBegun = r.ReadByte();

            ShipClassName = r.ReadString();
            ShipId = r.ReadInt32();
            ShipBPV = r.ReadInt32();

            AuctionValue = r.ReadInt32();
            AuctionRate = r.ReadDouble();

            TurnOpened = r.ReadInt32();
            TurnToClose = r.ReadInt32();
            CurrentBid = r.ReadInt32();

            BidOwnerID = r.ReadInt32();
            TurnBidMade = r.ReadInt32();
            BidMaximum = r.ReadInt32();
        }

        public void WriteTo(BinaryWriter w)
        {
            Contract.Requires(w != null);

            w.Write(Id);
            w.Write(LockID);

            w.Write(BiddingHasBegun);

            w.Write(ShipClassName);
            w.Write(ShipId);
            w.Write(ShipBPV);

            w.Write(AuctionValue);
            w.Write(AuctionRate);

            w.Write(TurnOpened);
            w.Write(TurnToClose);
            w.Write(CurrentBid);

            w.Write(BidOwnerID);
            w.Write(TurnBidMade);
            w.Write(BidMaximum);
        }
    }
}
