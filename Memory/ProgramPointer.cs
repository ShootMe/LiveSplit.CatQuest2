﻿using System;
using System.Diagnostics;
namespace LiveSplit.CatQuest2 {
    public enum PointerVersion {
        All
    }
    public enum AutoDeref {
        None,
        Single,
        Double
    }
    public class ProgramPointer {
        private int lastID;
        private DateTime lastTry;
        public IntPtr Pointer { get; private set; }
        public IFindPointer[] Finders { get; private set; }
        public string AsmName { get; private set; }

        public ProgramPointer(params IFindPointer[] finders) : this(string.Empty, finders) { }
        public ProgramPointer(string asmName, params IFindPointer[] finders) {
            AsmName = asmName;
            Finders = finders;
            lastID = -1;
            lastTry = DateTime.MinValue;
        }

        public T Read<T>(Process program, params int[] offsets) where T : unmanaged {
            GetPointer(program);
            return program.Read<T>(Pointer, offsets);
        }
        public string Read(Process program, params int[] offsets) {
            GetPointer(program);
            return program.ReadString(Pointer, offsets);
        }
        public byte[] ReadBytes(Process program, int length, params int[] offsets) {
            GetPointer(program);
            return program.Read(Pointer, length, offsets);
        }
        public void Write<T>(Process program, T value, params int[] offsets) where T : unmanaged {
            GetPointer(program);
            program.Write<T>(Pointer, value, offsets);
        }
        public void Write(Process program, byte[] value, params int[] offsets) {
            GetPointer(program);
            program.Write(Pointer, value, offsets);
        }
        public IntPtr GetPointer(Process program) {
            if (program == null) {
                Pointer = IntPtr.Zero;
                lastID = -1;
                return Pointer;
            } else if (program.Id != lastID) {
                Pointer = IntPtr.Zero;
                lastID = program.Id;
            }

            if (Pointer == IntPtr.Zero && DateTime.Now > lastTry) {
                lastTry = DateTime.Now.AddSeconds(1);

                for (int i = 0; i < Finders.Length; i++) {
                    IFindPointer finder = Finders[i];
                    if (finder.Version == PointerVersion.All || finder.Version == MemoryManager.Version) {
                        try {
                            Pointer = finder.FindPointer(program, AsmName);
                            if (Pointer != IntPtr.Zero || finder.FoundBaseAddress()) {
                                break;
                            }
                        } catch { }
                    }
                }
            }
            return Pointer;
        }
        public static IntPtr DerefPointer(Process program, IntPtr pointer, AutoDeref autoDeref) {
            if (pointer != IntPtr.Zero) {
                if (autoDeref != AutoDeref.None) {
                    pointer = program.Read<IntPtr>(pointer);
                    
                    if (autoDeref == AutoDeref.Double) {
                        pointer = program.Read<IntPtr>(pointer);
                    }
                }
            }
            return pointer;
        }
        public static Tuple<IntPtr, IntPtr> GetAddressRange(Process program, string asmName) {
            Module64 module = program.Module64(asmName);
            if (module != null) {
                return new Tuple<IntPtr, IntPtr>(module.BaseAddress, module.BaseAddress + module.MemorySize);
            }
            return new Tuple<IntPtr, IntPtr>(IntPtr.Zero, IntPtr.Zero);
        }
    }
    public interface IFindPointer {
        IntPtr FindPointer(Process program, string asmName);
        bool FoundBaseAddress();
        PointerVersion Version { get; }
    }
    public class FindPointerSignature : IFindPointer {
        private int lastPID;
        public PointerVersion Version { get; private set; }
        private AutoDeref AutoDeref;
        private string Signature;
        private IntPtr BasePtr;
        private MemorySearcher Searcher;
        private DateTime LastVerified;
        private int[] Relative;

        public FindPointerSignature(PointerVersion version, AutoDeref autoDeref, string signature, params int[] relative) {
            Version = version;
            AutoDeref = autoDeref;
            Signature = signature;
            BasePtr = IntPtr.Zero;
            Searcher = new MemorySearcher();
            LastVerified = DateTime.MaxValue;
            Relative = relative;
        }

        public bool FoundBaseAddress() {
            return BasePtr != IntPtr.Zero;
        }
        public IntPtr FindPointer(Process program, string asmName) {
            return ProgramPointer.DerefPointer(program, GetPointer(program, asmName), AutoDeref);
        }
        private IntPtr GetPointer(Process program, string asmName) {
            if (lastPID != program.Id) {
                lastPID = program.Id;
                BasePtr = IntPtr.Zero;
            }

            if (BasePtr != IntPtr.Zero) {
                DateTime now = DateTime.Now;
                if (now > LastVerified) {
                    bool isValid = Searcher.VerifySignature(program, BasePtr, Signature);
                    LastVerified = now.AddSeconds(5);
                    if (!isValid) {
                        BasePtr = IntPtr.Zero;
                    }
                }

                if (BasePtr != IntPtr.Zero) {
                    int offset = CalculateRelative(program);
                    return BasePtr + offset;
                }
            }

            if (string.IsNullOrEmpty(asmName)) {
                Searcher.MemoryFilter = delegate (MemInfo info) {
                    return (info.State & 0x1000) != 0 && (info.Protect & 0x40) != 0 && (info.Protect & 0x100) == 0;
                };
            } else {
                Tuple<IntPtr, IntPtr> range = ProgramPointer.GetAddressRange(program, asmName);
                Searcher.MemoryFilter = delegate (MemInfo info) {
                    return (ulong)info.BaseAddress >= (ulong)range.Item1 && (ulong)info.BaseAddress <= (ulong)range.Item2 && (info.State & 0x1000) != 0 && (info.Protect & 0x20) != 0 && (info.Protect & 0x100) == 0;
                };
            }

            IntPtr ptr = Searcher.FindSignature(program, Signature);
            if (ptr != IntPtr.Zero) {
                BasePtr = ptr;
                LastVerified = DateTime.Now.AddSeconds(5);
                int offset = CalculateRelative(program);
                return BasePtr + offset;
            }
            return IntPtr.Zero;
        }
        private int CalculateRelative(Process program) {
            int maxIndex = Relative.Length - 1;
            if (Relative == null || maxIndex < 0) { return 0; }

            int offset = 0;
            for (int i = 0; i < maxIndex; i++) {
                offset += Relative[i];
                offset += program.Read<int>(BasePtr + offset) + 4;
            }
            return offset + Relative[maxIndex];
        }
    }
    public class FindOffset : IFindPointer {
        private int lastPID;
        private int[] Offsets;
        private IntPtr BasePtr;
        public PointerVersion Version { get; private set; }

        public FindOffset(PointerVersion version, params int[] offsets) {
            Version = version;
            Offsets = offsets;
        }

        public bool FoundBaseAddress() {
            return BasePtr != IntPtr.Zero;
        }
        public IntPtr FindPointer(Process program, string asmName) {
            if (lastPID != program.Id) {
                lastPID = program.Id;

                if (string.IsNullOrEmpty(asmName)) {
                    BasePtr = program.MainModule.BaseAddress;
                } else {
                    Tuple<IntPtr, IntPtr> range = ProgramPointer.GetAddressRange(program, asmName);
                    BasePtr = range.Item1;
                }
            }

            return program.Read<IntPtr>(BasePtr, Offsets);
        }
    }
}