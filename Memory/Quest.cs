using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace LiveSplit.CatQuest2 {
    [StructLayout(LayoutKind.Explicit, Size = 28, Pack = 1)]
    public unsafe struct QuestData {
        [FieldOffset(0)]
        public uint Title;
        [FieldOffset(4)]
        public uint TitleTerm;
        [FieldOffset(8)]
        public uint Prerequisites;
        [FieldOffset(12)]
        public bool MainQuest;
        [FieldOffset(13)]
        public bool SideQuest;
        [FieldOffset(16)]
        public int Level;
        [FieldOffset(20)]
        public int Gold;
        [FieldOffset(24)]
        public int Exp;
        public Quest Create(Process program) {
            string title = program.ReadString((IntPtr)Title, 0x0);
            return new Quest() { Title = title, MainQuest = MainQuest, SideQuest = SideQuest, Exp = Exp, Gold = Gold, Level = Level };
        }
    }
    public class Quest {
        public string Title;
        public bool MainQuest;
        public bool SideQuest;
        public bool Completed;
        public int Level;
        public int Gold;
        public int Exp;

        public Quest Clone() {
            return new Quest() { Completed = Completed, Exp = Exp, Gold = Gold, Level = Level, MainQuest = MainQuest, SideQuest = SideQuest, Title = Title };
        }
        public override string ToString() {
            return $"{Title} (Completed={Completed})(Main={MainQuest})(Gold={Gold})(Exp={Exp})";
        }
    }
}