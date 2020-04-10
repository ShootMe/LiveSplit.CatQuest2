using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace LiveSplit.CatQuest2 {
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    public struct QuestData {
        [FieldOffset(0)]
        public uint Guid;
        [FieldOffset(4)]
        public uint title;
        public Quest Create(Process program) {
            string guid = program.ReadString((IntPtr)Guid, 0x0);
            string name = program.ReadString((IntPtr)title, 0x0);
            return new Quest() { Guid = guid, Name = name };
        }
    }
    public class Quest {
        public string Guid;
        public string Name;
        public bool Completed;
        public bool Started;

        public override bool Equals(object obj) {
            return obj is Quest quest && quest.Guid == Guid && quest.Completed == Completed && quest.Started == Started;
        }
        public override int GetHashCode() {
            return Guid.GetHashCode();
        }
        public override string ToString() {
            return $"{Name} (Guid={Guid})(Complete={Completed})(Started={Started})";
        }
    }
}