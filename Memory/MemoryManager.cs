using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace LiveSplit.CatQuest2 {
    public class CacheItem<T> {
        public T Item;
        public DateTime RefreshDate;
    }
    public partial class MemoryManager {
        private static ProgramPointer Contexts = new ProgramPointer(
            new FindPointerSignature(PointerVersion.All, AutoDeref.Double, "83C410E8????????8947448BC839098B40108947488B47448BC839098B402489474C8D45??83EC0C50", 0x4, 0x8));
        private static ProgramPointer SceneManager = new ProgramPointer(
            new FindPointerSignature(PointerVersion.All, AutoDeref.Double, "55565753E8????????83EC0850FF30892083EC04E8????????8BC88B05????????8BF985C0750F8BC78B55DC8B4DE089118B7DF0C9C3E8????????EBEA000000558BEC5683EC048B750883EC08FF750C56", 0x15, 0x1, 0x1));
        public static PointerVersion Version { get; set; } = PointerVersion.All;
        public Process Program { get; set; }
        public bool IsHooked { get; set; }
        public DateTime LastHooked { get; set; }
        private Dictionary<Context, Dictionary<int, CacheItem<IntPtr>>> contextCache = new Dictionary<Context, Dictionary<int, CacheItem<IntPtr>>>();

        public MemoryManager() {
            LastHooked = DateTime.MinValue;
        }
        public string GamePointers() {
            return string.Concat(
                $"CTX: {(uint)Contexts.GetPointer(Program):X} ",
                $"SM: {(uint)SceneManager.GetPointer(Program):X} "
            );
        }
        public bool AllPointersFound() {
            return Contexts.GetPointer(Program) != IntPtr.Zero
                && SceneManager.GetPointer(Program) != IntPtr.Zero;
        }
        public bool IsLoading() {
            CacheItem<IntPtr> cache = GetCache(Context.Framework, (int)FrameworkContext.TransitionExitStarted);
            return GroupSingleComponent(cache.Item, (int)FrameworkContext.TransitionExitStarted) != IntPtr.Zero
                || GroupSingleComponent(cache.Item, (int)FrameworkContext.TransitionEnterStarted) != IntPtr.Zero;
        }
        public string SceneName() {
            IntPtr scene = (IntPtr)SceneManager.Read<uint>(Program, 0x28);
            IntPtr name = (IntPtr)Program.Read<uint>(scene, 0x28);
            if (name == IntPtr.Zero) {
                name = scene + 0x2c;
            }
            return Program.ReadAscii(name);
        }
        public SceneType GameSceneType() {
            CacheItem<IntPtr> cache = GetCache(Context.Framework, (int)FrameworkContext.SceneType);
            IntPtr item = GroupSingleComponent(cache.Item, (int)FrameworkContext.SceneType);
            if (item == IntPtr.Zero) {
                return SceneType.None;
            }
            return Program.Read<SceneType>(item, 0x8);
        }
        public int Gold() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.Gold);
            IntPtr entity = GroupSingleComponent(cache.Item, (int)GameStateContext.Gold);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<int>(entity, 0x8);
        }
        public int Experience() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.Experience);
            IntPtr entity = GroupSingleComponent(cache.Item, (int)GameStateContext.Experience);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<int>(entity, 0x8);
        }
        public int Level() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.Level);
            IntPtr entity = GroupSingleComponent(cache.Item, (int)GameStateContext.Level);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<int>(entity, 0x8);
        }
        public IntPtr SavedGame() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.SavedGame);
            return GroupSingleComponent(cache.Item, (int)GameStateContext.SavedGame);
        }
        public Dictionary<string, Chest> Chests() {
            Dictionary<string, Chest> returnVal = new Dictionary<string, Chest>(StringComparer.OrdinalIgnoreCase);
            IntPtr savedGame = SavedGame();
            IntPtr chests = (IntPtr)Program.Read<uint>(savedGame, 0x8, 0x58);
            int count = Program.Read<int>(chests, 0xc);
            chests = (IntPtr)Program.Read<uint>(chests, 0x8);
            byte[] data = Program.Read(chests + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                string guid = Program.ReadString((IntPtr)BitConverter.ToUInt32(data, i * 0x4), 0xc, 0x0);
                returnVal.Add(guid, new Chest() { Guid = guid, Collected = true });
            }

            return returnVal;
        }
        public Dictionary<string, Spell> Spells() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.SpellsAttainedList);
            IntPtr spells = GroupSingleComponent(cache.Item, (int)GameStateContext.SpellsAttainedList);

            int count = Program.Read<int>(spells, 0x8, 0xc);
            Dictionary<string, Spell> returnVal = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
            spells = (IntPtr)Program.Read<uint>(spells, 0x8, 0x8);
            byte[] data = Program.Read(spells + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                IntPtr entity = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                int level = Program.Read<int>(entity, 0xc);
                Spell spell = Program.Read<SpellData>(entity, 0x8, 0xc).Create(Program);
                spell.Level = level;
                returnVal.Add(spell.Guid, spell);
            }

            return returnVal;
        }
        public int DungeonsCleared() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.DungeonsCleared);
            IntPtr entity = GroupSingleComponent(cache.Item, (int)GameStateContext.DungeonsCleared);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<int>(entity, 0x8);
        }
        public RoyalArts PlayerRoyalArts() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.RoyalArts);
            IntPtr entity = GroupSingleComponent(cache.Item, (int)GameStateContext.RoyalArts);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<RoyalArts>(entity, 0x8);
        }
        public Dictionary<string, Quest> Quests() {
            List<IntPtr> entities = ContextEntities(Context.Quest);
            int count = entities.Count;
            Dictionary<string, Quest> returnVal = new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++) {
                IntPtr entity = entities[i];
                //entity._components[QuestCompleted]
                bool completed = Program.Read<uint>(entity, 0x24, 0x10 + ((int)QuestContext.QuestCompleted * 0x4)) != 0;
                int index = Program.Read<int>(entity, 0x44);
                //entity._components[QuestController].value.questID
                entity = (IntPtr)Program.Read<uint>(entity, 0x24, 0x10 + ((int)QuestContext.QuestController * 0x4), 0x8, 0xc);
                if (entity == IntPtr.Zero) { continue; }

                Quest obj = Program.Read<QuestData>(entity, 0x10).Create(Program);
                if (string.IsNullOrEmpty(obj.Title)) {
                    obj.Title = index.ToString();
                }
                obj.Completed = completed;
                returnVal.Add(obj.Title, obj);
            }
            return returnVal;
        }
        private CacheItem<IntPtr> GetCache(Context context, int componentID) {
            Dictionary<int, CacheItem<IntPtr>> cache = null;
            if (!contextCache.TryGetValue(context, out cache)) {
                cache = new Dictionary<int, CacheItem<IntPtr>>();
                contextCache.Add(context, cache);
            }
            CacheItem<IntPtr> cacheItem = null;
            if (!cache.TryGetValue(componentID, out cacheItem) || DateTime.Now > cacheItem.RefreshDate) {
                if (cacheItem == null) {
                    cacheItem = new CacheItem<IntPtr>();
                    cache.Add(componentID, cacheItem);
                }
                cacheItem.Item = ContextGroup(context, componentID);
                cacheItem.RefreshDate = DateTime.Now.AddSeconds(5);
            }
            return cacheItem;
        }
        private IntPtr ContextGroup(Context context, int componentID) {
            //Contexts.sharedInstance.[context]._groups
            IntPtr groups = (IntPtr)Contexts.Read<uint>(Program, (int)context, 0x34);
            //.Count
            int count = Program.Read<int>(groups, 0x20);
            //.Items
            groups = (IntPtr)Program.Read<uint>(groups, 0x14);
            byte[] data = Program.Read(groups + 0x10, count * 0x4);
            IntPtr group = IntPtr.Zero;
            for (int i = 0; i < count; i++) {
                //.Items[i]
                group = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                int id = Program.Read<int>(group, 0x14, 0xc, 0x10);
                if (id == componentID) {
                    return (IntPtr)Program.Read<uint>(group, 0x18);
                }
            }

            return IntPtr.Zero;
        }
        private List<IntPtr> ContextEntities(Context context) {
            //Contexts.sharedInstance.[context]._entities
            IntPtr entities = (IntPtr)Contexts.Read<uint>(Program, (int)context, 0x28);
            //.Count
            int count = Program.Read<int>(entities, 0x24);
            List<IntPtr> returnVal = new List<IntPtr>(count);
            //.Items
            entities = (IntPtr)Program.Read<uint>(entities, 0x10);
            byte[] data = Program.Read(entities + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                //.Items[i]
                returnVal.Add((IntPtr)BitConverter.ToUInt32(data, i * 0x4));
            }
            return returnVal;
        }
        private IntPtr GroupSingleComponent(IntPtr group, int componentID) {
            //group._entities.Count
            int count = Program.Read<int>(group, 0x24);
            if (count == 0) { return IntPtr.Zero; }
            //group._entities.Items[0]._components[id]
            return (IntPtr)Program.Read<uint>(group, 0x10, 0x10, 0x24, 0x10 + (componentID * 0x4));
        }
        private List<IntPtr> GroupComponents(IntPtr group, int componentID) {
            List<IntPtr> entities = new List<IntPtr>();
            //group._entities.Count
            int count = Program.Read<int>(group, 0x24);
            //group._entities.Items
            group = (IntPtr)Program.Read<uint>(group, 0x10);
            byte[] data = Program.Read(group + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                //.Items[i]
                group = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                //.Items[i]._components[id]
                IntPtr entity = (IntPtr)Program.Read<uint>(group, 0x24, 0x10 + (componentID * 0x4));
                if (entity != IntPtr.Zero) {
                    entities.Add(entity);
                }
            }
            return entities;
        }
        public bool HookProcess() {
            IsHooked = Program != null && !Program.HasExited;
            if (!IsHooked && DateTime.Now > LastHooked.AddSeconds(1)) {
                LastHooked = DateTime.Now;

                Process[] processes = Process.GetProcessesByName("Cat Quest II");
                Program = processes != null && processes.Length > 0 ? processes[0] : null;

                if (Program != null && !Program.HasExited) {
                    MemoryReader.Update64Bit(Program);
                    contextCache.Clear();
                    MemoryManager.Version = PointerVersion.All;
                    //Module64 module = Program.MainModule64();
                    //if (module != null) {
                    //    switch (module.MemorySize) {
                    //        case 77430784: MemoryManager.Version = PointerVersion.V2; break;
                    //    }
                    //}
                    IsHooked = true;
                }
            }

            return IsHooked;
        }
        public void Dispose() {
            if (Program != null) {
                Program.Dispose();
            }
        }
    }
}