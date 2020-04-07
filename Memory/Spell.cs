using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace LiveSplit.CatQuest2 {
    [StructLayout(LayoutKind.Explicit, Size = 76, Pack = 1)]
    public struct SpellData {
        [FieldOffset(0)]
        public uint guid;
        [FieldOffset(4)]
        public uint spellName;
        [FieldOffset(8)]
        public uint vfx;
        [FieldOffset(12)]
        public uint icon;
        [FieldOffset(16)]
        public uint instantEffects;
        [FieldOffset(20)]
        public uint overtimeEffects;
        [FieldOffset(24)]
        public uint spellDescription;
        [FieldOffset(28)]
        public uint statIcon;
        [FieldOffset(32)]
        public uint spellNameTerm;
        [FieldOffset(36)]
        public uint spellDescriptionTerm;
        [FieldOffset(40)]
        public bool isPlayerSpell;
        [FieldOffset(44)]
        public int maxManaIncrease;
        [FieldOffset(48)]
        public bool isActiveAfterDeath;
        [FieldOffset(49)]
        public bool isFriendly;
        [FieldOffset(52)]
        public FriendlyDirectCastType directCastType;
        [FieldOffset(56)]
        public bool isSpawnPositionRandom;
        [FieldOffset(57)]
        public bool toFollowAfterCasted;
        [FieldOffset(58)]
        public bool toCancelUponHit;
        [FieldOffset(59)]
        public bool toCancelPreviousCast;
        [FieldOffset(60)]
        public float randomSpawnRange;
        [FieldOffset(64)]
        public int manaCost;
        [FieldOffset(68)]
        public int baseUpgradeCost;
        [FieldOffset(72)]
        public ElementalType elementalType;

        public Spell Create(Process program) {
            string name = program.ReadString((IntPtr)spellName, 0x0);
            string id = program.ReadString((IntPtr)guid, 0x0);
            return new Spell() { Name = name, Guid = id, ElementalType = elementalType, ManaCost = manaCost };
        }
    }
    public class Spell {
        public string Name;
        public string Guid;
        public int Level;
        public int ManaCost;
        public ElementalType ElementalType;

        public override string ToString() {
            return $"{Name} (Level={Level})(Mana={ManaCost})(Type={ElementalType})(Guid={Guid})";
        }
    }
    [Flags]
    public enum FriendlyDirectCastType {
        None = 0,
        Self = 1,
        Other = 2,
        Both = 3
    }
    public enum ElementalType {
        Physical,
        Fire,
        Ice,
        Lightning,
        Arcane,
        Royal
    }
}