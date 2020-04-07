using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace LiveSplit.CatQuest2 {
    [StructLayout(LayoutKind.Explicit, Size = 36, Pack = 1)]
    public struct QuestData {
        [FieldOffset(0)]
        public uint Guid;
        [FieldOffset(4)]
        public uint title;
        [FieldOffset(8)]
        public uint titleTerm;
        [FieldOffset(12)]
        public uint prerequisites;
        [FieldOffset(16)]
        public uint nextQuest;
        [FieldOffset(20)]
        public bool isMainQuest;
        [FieldOffset(21)]
        public bool isSideQuest;
        [FieldOffset(24)]
        public int requiredLevel;
        [FieldOffset(28)]
        public int gold;
        [FieldOffset(32)]
        public int exp;
        public Quest Create(Process program) {
            string guid = program.ReadString((IntPtr)Guid, 0x0);
            string name = program.ReadString((IntPtr)title, 0x0);
            return new Quest() { Guid = guid, Name = name, MainQuest = isMainQuest, SideQuest = isSideQuest, Exp = exp, Gold = gold, Level = requiredLevel };
        }
    }
    public class Quest {
        public string Guid;
        public string Name;
        public bool MainQuest;
        public bool SideQuest;
        public int Level;
        public int Gold;
        public int Exp;

        public Quest Clone() {
            return new Quest() { Guid = Guid, Exp = Exp, Gold = Gold, Level = Level, MainQuest = MainQuest, SideQuest = SideQuest, Name = Name };
        }
        public override string ToString() {
            return $"{Name} (Guid={Guid})(Main={MainQuest})(Gold={Gold})(Exp={Exp})";
        }
    }
}