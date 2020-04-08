using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace LiveSplit.CatQuest2 {
    public class CacheItem<T> {
        public T Item;
        public DateTime RefreshDate;

        public override string ToString() {
            return $"{Item} (Refresh={RefreshDate})";
        }
    }
    public partial class MemoryManager {
        private static ProgramPointer Contexts = new ProgramPointer(
            new FindPointerSignature(PointerVersion.All, AutoDeref.Double, "83C410E8????????8947448BC839098B40108947488B47448BC839098B402489474C8D45??83EC0C50", 0x4, 0x8));
        private static ProgramPointer SceneManager = new ProgramPointer("UnityPlayer.dll",
            new FindPointerSignature(PointerVersion.All, AutoDeref.Double, "558BECE8????????FF75088BC88B10FF52048BC885C9740C8B412885C075078D412C5DC333C05DC3", 0x4, 0x1));
        public static PointerVersion Version { get; set; } = PointerVersion.All;
        public Process Program { get; set; }
        public bool IsHooked { get; set; }
        public DateTime LastHooked { get; set; }
        private Dictionary<Context, Dictionary<int, CacheItem<IntPtr>>> contextCache = new Dictionary<Context, Dictionary<int, CacheItem<IntPtr>>>();
        private Dictionary<string, Spell> currentSpells = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Quest> currentQuests = new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, GuidItem> currentChests = new Dictionary<string, GuidItem>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, GuidItem> currentKeys = new Dictionary<string, GuidItem>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Equipment> currentEquipment = new Dictionary<string, Equipment>(StringComparer.OrdinalIgnoreCase);
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
            IntPtr scene = SceneManager.Read<IntPtr>(Program, 0x28);
            IntPtr name = Program.Read<IntPtr>(scene, 0x28);
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
        public int DungeonsCleared() {
            IntPtr savedGame = SavedGame();
            return Program.Read<int>(savedGame, 0x8, 0xc0);
        }
        public bool FinalQuestCompleted() {
            IntPtr savedGame = SavedGame();
            return Program.Read<bool>(savedGame, 0x8, 0xc8);
        }
        public RoyalArts PlayerRoyalArts() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.RoyalArts);
            IntPtr entity = GroupSingleComponent(cache.Item, (int)GameStateContext.RoyalArts);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<RoyalArts>(entity, 0x8);
        }
        public bool HasKey(string guid) {
            IntPtr savedGame = SavedGame();
            IntPtr keys = Program.Read<IntPtr>(savedGame, 0x8, 0x78);

            int count = Program.Read<int>(keys, 0xc);
            keys = Program.Read<IntPtr>(keys, 0x8);
            byte[] data = Program.Read(keys + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                string id = Program.ReadString((IntPtr)BitConverter.ToUInt32(data, i * 0x4), 0xc, 0x0);
                if (id == guid) {
                    return true;
                }
            }

            return false;
        }
        public Dictionary<string, GuidItem> Keys() {
            IntPtr savedGame = SavedGame();
            IntPtr keys = Program.Read<IntPtr>(savedGame, 0x8, 0x78);
            currentKeys.Clear();

            int count = Program.Read<int>(keys, 0xc);
            keys = Program.Read<IntPtr>(keys, 0x8);
            byte[] data = Program.Read(keys + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                string guid = Program.ReadString((IntPtr)BitConverter.ToUInt32(data, i * 0x4), 0xc, 0x0);
                currentKeys.Add(guid, new GuidItem() { Guid = guid });
            }

            return currentKeys;
        }
        public bool HasChest(string guid) {
            IntPtr savedGame = SavedGame();
            IntPtr chests = Program.Read<IntPtr>(savedGame, 0x8, 0x58);

            int count = Program.Read<int>(chests, 0xc);
            chests = Program.Read<IntPtr>(chests, 0x8);
            byte[] data = Program.Read(chests + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                string id = Program.ReadString((IntPtr)BitConverter.ToUInt32(data, i * 0x4), 0xc, 0x0);
                if (id == guid) {
                    return true;
                }
            }

            return false;
        }
        public Dictionary<string, GuidItem> Chests() {
            IntPtr savedGame = SavedGame();
            IntPtr chests = Program.Read<IntPtr>(savedGame, 0x8, 0x58);
            currentChests.Clear();

            int count = Program.Read<int>(chests, 0xc);
            chests = Program.Read<IntPtr>(chests, 0x8);
            byte[] data = Program.Read(chests + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                string guid = Program.ReadString((IntPtr)BitConverter.ToUInt32(data, i * 0x4), 0xc, 0x0);
                currentChests.Add(guid, new GuidItem() { Guid = guid });
            }

            return currentChests;
        }
        public Dictionary<string, Equipment> Equipment() {
            IntPtr savedGame = SavedGame();
            IntPtr equipment = Program.Read<IntPtr>(savedGame, 0x8, 0x6c);
            currentEquipment.Clear();

            int count = Program.Read<int>(equipment, 0xc);
            equipment = Program.Read<IntPtr>(equipment, 0x8);
            byte[] data = Program.Read(equipment + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                IntPtr entity = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                int level = Program.Read<int>(entity, 0x10);
                Equipment obj = Program.Read<EquipmentData>(entity, 0x8, 0xc).Create(Program);
                obj.Level = level;
                currentEquipment.Add(obj.Guid, obj);
            }

            return currentEquipment;
        }
        public bool HasSpell(string name) {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.SpellsAttainedList);
            IntPtr spells = GroupSingleComponent(cache.Item, (int)GameStateContext.SpellsAttainedList);

            int count = Program.Read<int>(spells, 0x8, 0xc);
            spells = Program.Read<IntPtr>(spells, 0x8, 0x8);
            byte[] data = Program.Read(spells + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                IntPtr entity = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                string item = Program.ReadString(entity, 0x8, 0x10, 0x0);

                if (name.Equals(item, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }
        public Dictionary<string, Spell> Spells() {
            CacheItem<IntPtr> cache = GetCache(Context.GameState, (int)GameStateContext.SpellsAttainedList);
            IntPtr spells = GroupSingleComponent(cache.Item, (int)GameStateContext.SpellsAttainedList);
            if (cache.Item != IntPtr.Zero && spells == IntPtr.Zero) { return currentSpells; }

            currentSpells.Clear();
            int count = Program.Read<int>(spells, 0x8, 0xc);
            spells = Program.Read<IntPtr>(spells, 0x8, 0x8);
            byte[] data = Program.Read(spells + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                IntPtr entity = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                int level = Program.Read<int>(entity, 0xc);
                Spell spell = Program.Read<SpellData>(entity, 0x8, 0xc).Create(Program);
                spell.Level = level;
                currentSpells.Add(spell.Guid, spell);
            }

            return currentSpells;
        }
        public Quest Quest(string guid) {
            List<IntPtr> entities = ContextEntities(Context.Quest);

            int count = entities.Count;
            for (int i = 0; i < count; i++) {
                IntPtr entity = entities[i];
                //entity._components[QuestStarted]
                bool started = Program.Read<uint>(entity, 0x24, 0x10 + ((int)QuestContext.QuestStarted * 0x4)) != 0;
                //entity._components[QuestCompleted]
                bool completed = Program.Read<uint>(entity, 0x24, 0x10 + ((int)QuestContext.QuestCompleted * 0x4)) != 0;
                //entity._components[QuestID].value
                entity = (IntPtr)Program.Read<uint>(entity, 0x24, 0x10 + ((int)QuestContext.QuestID * 0x4), 0x8);
                if (entity == IntPtr.Zero) { continue; }

                Quest obj = Program.Read<QuestData>(entity, 0xc).Create(Program);
                obj.Started = started;
                obj.Completed = completed;
                if (obj.Guid == guid) { return obj; }
            }
            return null;
        }
        public Dictionary<string, Quest> Quests() {
            List<IntPtr> entities = ContextEntities(Context.Quest);
            currentQuests.Clear();

            int count = entities.Count;
            for (int i = 0; i < count; i++) {
                IntPtr entity = entities[i];
                //entity._components[QuestStarted]
                bool started = Program.Read<uint>(entity, 0x24, 0x10 + ((int)QuestContext.QuestStarted * 0x4)) != 0;
                //entity._components[QuestCompleted]
                bool completed = Program.Read<uint>(entity, 0x24, 0x10 + ((int)QuestContext.QuestCompleted * 0x4)) != 0;
                if (!started && !completed) { continue; }

                //entity._components[QuestID].value
                entity = (IntPtr)Program.Read<uint>(entity, 0x24, 0x10 + ((int)QuestContext.QuestID * 0x4), 0x8);
                if (entity == IntPtr.Zero) { continue; }

                Quest obj = Program.Read<QuestData>(entity, 0xc).Create(Program);
                obj.Started = started;
                obj.Completed = completed;
                currentQuests.Add(obj.Guid, obj);
            }
            return currentQuests;
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
            IntPtr groups = Contexts.Read<IntPtr>(Program, (int)context, 0x34);
            //.Count
            int count = Program.Read<int>(groups, 0x20);
            //.Items
            groups = Program.Read<IntPtr>(groups, 0x14);
            byte[] data = Program.Read(groups + 0x10, count * 0x4);
            IntPtr group = IntPtr.Zero;
            for (int i = 0; i < count; i++) {
                //.Items[i]
                group = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                int id = Program.Read<int>(group, 0x14, 0xc, 0x10);
                if (id == componentID) {
                    return Program.Read<IntPtr>(group, 0x18);
                }
            }

            return IntPtr.Zero;
        }
        private List<IntPtr> ContextEntities(Context context) {
            //Contexts.sharedInstance.[context]._entities
            IntPtr entities = Contexts.Read<IntPtr>(Program, (int)context, 0x28);
            //.Count
            int count = Program.Read<int>(entities, 0x24);
            List<IntPtr> returnVal = new List<IntPtr>(count);
            //.Items
            entities = Program.Read<IntPtr>(entities, 0x10);
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
            count--;
            return Program.Read<IntPtr>(group, 0x10, 0x10 + (count * 0x4), 0x24, 0x10 + (componentID * 0x4));
        }
        private List<IntPtr> GroupComponents(IntPtr group, int componentID) {
            List<IntPtr> entities = new List<IntPtr>();
            //group._entities.Count
            int count = Program.Read<int>(group, 0x24);
            //group._entities.Items
            group = Program.Read<IntPtr>(group, 0x10);
            byte[] data = Program.Read(group + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                //.Items[i]
                group = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                //.Items[i]._components[id]
                IntPtr entity = Program.Read<IntPtr>(group, 0x24, 0x10 + (componentID * 0x4));
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