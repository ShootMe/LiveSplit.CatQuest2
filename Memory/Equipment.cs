using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace LiveSplit.CatQuest2 {
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 1)]
    public struct EquipmentData {
        [FieldOffset(0)]
        public uint guid;
        [FieldOffset(4)]
        public uint icon;
        [FieldOffset(8)]
        public uint setName;
        [FieldOffset(12)]
        public uint itemName;

        public Equipment Create(Process program) {
            string set = program.ReadString((IntPtr)setName, 0x0);
            string name = program.ReadString((IntPtr)itemName, 0x0);
            string id = program.ReadString((IntPtr)guid, 0x0);
            return new Equipment() { SetName = set, ItemName = name, Guid = id };
        }
    }
    public class Equipment {
        public string Guid;
        public string SetName;
        public string ItemName;
        public int Level;

        public override bool Equals(object obj) {
            return obj is Equipment equipment && equipment.Guid == Guid && equipment.Level == Level;
        }
        public override int GetHashCode() {
            return Guid.GetHashCode();
        }
        public override string ToString() {
            return $"{SetName}({ItemName}) (Guid={Guid})(Level={Level})";
        }
    }
}