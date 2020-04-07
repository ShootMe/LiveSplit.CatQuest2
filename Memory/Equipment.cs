using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace LiveSplit.CatQuest2 {
    [StructLayout(LayoutKind.Explicit, Size = 80, Pack = 1)]
    public struct EquipmentData {
        [FieldOffset(0)]
        public uint guid;
        [FieldOffset(4)]
        public uint icon;
        [FieldOffset(8)]
        public uint setName;
        [FieldOffset(12)]
        public uint itemName;
        [FieldOffset(16)]
        public uint skinData;
        [FieldOffset(20)]
        public uint projectileFiringPattern;
        [FieldOffset(24)]
        public uint projectileLaunchParams;
        [FieldOffset(28)]
        public uint itemNameTerm;
        [FieldOffset(32)]
        public EquipmentPartType partType;
        [FieldOffset(36)]
        public WeaponType weaponType;
        [FieldOffset(40)]
        public bool isLongWeapon;
        [FieldOffset(44)]
        public int health;
        [FieldOffset(48)]
        public int attack;
        [FieldOffset(52)]
        public int magic;
        [FieldOffset(56)]
        public int defence;
        [FieldOffset(60)]
        public PassiveType passiveType;
        [FieldOffset(64)]
        public int passiveInt;
        [FieldOffset(68)]
        public float passiveFloat;
        [FieldOffset(72)]
        public ElementalType elementalType;
        [FieldOffset(76)]
        public float attackRange;

        public Equipment Create(Process program) {
            string set = program.ReadString((IntPtr)setName, 0x0);
            string name = program.ReadString((IntPtr)itemName, 0x0);
            string id = program.ReadString((IntPtr)guid, 0x0);
            return new Equipment() { SetName = set, ItemName = name, Guid = id, PartType = partType, WeaponType = weaponType, ElementalType = elementalType, PassiveType = passiveType, Attack = attack, AttackRange = attackRange, Magic = magic, Defence = defence, Health = health };
        }
    }
    public class Equipment {
        public string Guid;
        public string SetName;
        public string ItemName;
        public int Level;
        public EquipmentPartType PartType;
        public WeaponType WeaponType;
        public int Health;
        public int Attack;
        public int Magic;
        public int Defence;
        public PassiveType PassiveType;
        public ElementalType ElementalType;
        public float AttackRange;

        public override string ToString() {
            return $"{SetName}-{ItemName} (Guid={Guid})(Level={Level})(Part={PartType})(Weapon={WeaponType})(Passive={PassiveType})(Element={ElementalType})(Range={AttackRange})";
        }
    }
    public enum EquipmentPartType {
        Head,
        Body,
        Weapon
    }
    public enum WeaponType {
        Melee,
        Ranged
    }
    public enum PassiveType {
        None,
        FireElementResist,
        IceElementResist,
        LightningResist,
        ArcaneResist,
        PhysicalResist,
        FireBuff,
        IceBuff,
        LightningBuff,
        ArcaneBuff,
        PhysicalBuff,
        HealpawBuff,
        CritChanceBuff,
        ReduceHP,
        ReduceMana,
        IncreaseMana,
        IncreaseRollDistance,
        IncreaseManaRegen,
        LifeSteal,
        IncreaseExpCollected,
        IncreaseGoldCollected,
        GoldSteal,
        IncreaseHP,
        IncreaseMagicBuff,
        IncreaseAttackSpeed,
        ReduceSpellCost
    }
}