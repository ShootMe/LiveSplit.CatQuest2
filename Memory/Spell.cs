using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace LiveSplit.CatQuest2 {
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    public struct SpellData {
        [FieldOffset(0)]
        public uint guid;
        [FieldOffset(4)]
        public uint spellName;

        public Spell Create(Process program) {
            string name = program.ReadString((IntPtr)spellName, 0x0);
            string id = program.ReadString((IntPtr)guid, 0x0);
            return new Spell() { Name = name, Guid = id };
        }
    }
    public class Spell {
        public string Name;
        public string Guid;
        public int Level;

        public override bool Equals(object obj) {
            return obj is Spell spell && spell.Guid == Guid && spell.Level == Level;
        }
        public override int GetHashCode() {
            return Guid.GetHashCode();
        }
        public override string ToString() {
            return $"{Name} (Guid={Guid})(Level={Level})";
        }
    }
}