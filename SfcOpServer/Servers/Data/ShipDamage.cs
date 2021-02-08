using System;
using System.IO;

namespace SfcOpServer
{
    public enum DamageType
    {
        RightWarpMax,
        RightWarp,

        LeftWarpMax,
        LeftWarp,

        CenterWarpMax,
        CenterWarp,

        ImpulseMax,
        Impulse,

        AprMax,
        Apr,

        BridgeMax,
        Bridge,

        SensorsMax,
        Sensors,

        ScannersMax,
        Scanners,

        RightDamageControlMax,
        RightDamageControl,

        RepairMax,
        Repair,

        ForwardHullMax,
        ForwardHull,

        AfterwardHullMax,
        AfterwardHull,

        CenterHullMax,
        CenterHull,

        TractorsMax,
        Tractors,

        ExtraDamageMax,
        ExtraDamage,

        RightTransportersMax,
        RightTransporters,

        LeftTransportersMax,
        LeftTransporters,

        BatteryMax,
        Battery,

        LabMax,
        Lab,

        CargoMax,
        Cargo,

        ArmorMax,
        Armor,

        CloakMax,
        Cloak,

        LeftDamageControlMax,
        LeftDamageControl,

        ProbesMax,
        Probes,

        BarracksMax,
        Barracks,

        NumHeavyWeaponMax1,
        NumHeavyWeapon1,

        NumHeavyWeaponMax2,
        NumHeavyWeapon2,

        NumHeavyWeaponMax3,
        NumHeavyWeapon3,

        NumHeavyWeaponMax4,
        NumHeavyWeapon4,

        NumHeavyWeaponMax5,
        NumHeavyWeapon5,

        NumHeavyWeaponMax6,
        NumHeavyWeapon6,

        NumHeavyWeaponMax7,
        NumHeavyWeapon7,

        NumHeavyWeaponMax8,
        NumHeavyWeapon8,

        NumHeavyWeaponMax9,
        NumHeavyWeapon9,

        NumHeavyWeaponMax10,
        NumHeavyWeapon10,

        NumWeaponMax11,
        NumWeapon11,

        NumWeaponMax12,
        NumWeapon12,

        NumWeaponMax13,
        NumWeapon13,

        NumWeaponMax14,
        NumWeapon14,

        NumWeaponMax15,
        NumWeapon15,

        NumWeaponMax16,
        NumWeapon16,

        NumWeaponMax17,
        NumWeapon17,

        NumWeaponMax18,
        NumWeapon18,

        NumWeaponMax19,
        NumWeapon19,

        NumWeaponMax20,
        NumWeapon20,

        NumWeaponMax21,
        NumWeapon21,

        NumWeaponMax22,
        NumWeapon22,

        NumWeaponMax23,
        NumWeapon23,

        NumWeaponMax24,
        NumWeapon24,

        NumWeaponMax25,
        NumWeapon25,
    }

    public class ShipDamage
    {
        public byte[] Items;

        public ShipDamage(byte[] buffer, int index)
        {
            if (Items == null)
                Items = new byte[Ship.DamageSize];

            Buffer.BlockCopy(buffer, index, Items, 0, Ship.DamageSize);
        }

        public ShipDamage(BinaryReader r)
        {
            Items = r.ReadBytes(Ship.DamageSize);
        }

        public void WriteTo(BinaryWriter w)
        {
            w.Write(Items);
        }
    }
}
