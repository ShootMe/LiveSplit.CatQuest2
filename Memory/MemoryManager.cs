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
        private Dictionary<IntPtr, CacheItem<List<IntPtr>>> groupCache = new Dictionary<IntPtr, CacheItem<List<IntPtr>>>();
        private Dictionary<string, GuidItem> currentChests = new Dictionary<string, GuidItem>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Equipment> currentEquipment = new Dictionary<string, Equipment>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, GuidItem> currentKeys = new Dictionary<string, GuidItem>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Quest> currentQuests = new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Spell> currentSpells = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);

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
            IntPtr cache = GetCache(Context.Framework, (int)FrameworkContext.TransitionExitStarted);
            return GroupSingleComponent(cache, (int)FrameworkContext.TransitionExitStarted) != IntPtr.Zero
                || GroupSingleComponent(cache, (int)FrameworkContext.TransitionEnterStarted) != IntPtr.Zero;
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
            IntPtr cache = GetCache(Context.Framework, (int)FrameworkContext.SceneType);
            IntPtr item = GroupSingleComponent(cache, (int)FrameworkContext.SceneType);
            if (item == IntPtr.Zero) {
                return SceneType.None;
            }
            return Program.Read<SceneType>(item, 0x8);
        }
        public int Gold() {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.Gold);
            IntPtr entity = GroupSingleComponent(cache, (int)GameStateContext.Gold);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<int>(entity, 0x8);
        }
        public int Experience() {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.Experience);
            IntPtr entity = GroupSingleComponent(cache, (int)GameStateContext.Experience);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<int>(entity, 0x8);
        }
        public int Level() {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.Level);
            IntPtr entity = GroupSingleComponent(cache, (int)GameStateContext.Level);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<int>(entity, 0x8);
        }
        public IntPtr SavedGame() {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.SavedGame);
            return GroupSingleComponent(cache, (int)GameStateContext.SavedGame);
        }
        public int DungeonsCleared() {
            IntPtr savedGame = SavedGame();
            return Program.Read<int>(savedGame, 0x8, 0xc0);
        }
        public TimeSpan TotalPlayTime() {
            IntPtr savedGame = SavedGame();
            return Program.Read<TimeSpan>(savedGame, 0x8, 0x28);
        }
        public bool FinalQuestCompleted() {
            IntPtr savedGame = SavedGame();
            return Program.Read<bool>(savedGame, 0x8, 0xc8);
        }
        public RoyalArts PlayerRoyalArts() {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.RoyalArts);
            IntPtr entity = GroupSingleComponent(cache, (int)GameStateContext.RoyalArts);
            if (entity == IntPtr.Zero) {
                return 0;
            }
            return Program.Read<RoyalArts>(entity, 0x8);
        }
        public bool HasKey(string guid) {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.KeyID);
            List<IntPtr> entities = GetGroupCache(cache);

            int count = entities.Count;
            for (int i = 0; i < count; i++) {
                IntPtr entity = entities[i];

                bool obtained = Program.Read<IntPtr>(entity, 0x10 + ((int)GameStateContext.IsObtained * 0x4)) != IntPtr.Zero;
                if (!obtained) { continue; }

                string id = Program.ReadString(entity, 0x10 + ((int)GameStateContext.KeyID * 0x4), 0x8, 0xc, 0x0);
                if (id == guid) {
                    return true;
                }
            }
            return false;
        }
        public Dictionary<string, GuidItem> Keys() {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.KeyID);
            List<IntPtr> entities = GetGroupCache(cache);
            currentKeys.Clear();

            int count = entities.Count;
            for (int i = 0; i < count; i++) {
                IntPtr entity = entities[i];

                bool obtained = Program.Read<IntPtr>(entity, 0x10 + ((int)GameStateContext.IsObtained * 0x4)) != IntPtr.Zero;
                if (!obtained) { continue; }

                string guid = Program.ReadString(entity, 0x10 + ((int)GameStateContext.KeyID * 0x4), 0x8, 0xc, 0x0);
                currentKeys.Add(guid, new GuidItem() { Guid = guid });
            }
            return currentKeys;
        }
        public int HasChests(string[] guids) {
            int found = 0;
            IntPtr savedGame = SavedGame();
            IntPtr chests = Program.Read<IntPtr>(savedGame, 0x8, 0x58);

            int count = Program.Read<int>(chests, 0xc);
            chests = Program.Read<IntPtr>(chests, 0x8);
            byte[] data = Program.Read(chests + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                string id = Program.ReadString((IntPtr)BitConverter.ToUInt32(data, i * 0x4), 0xc, 0x0);
                for (int j = 0; j < guids.Length; j++) {
                    if (id == guids[j]) {
                        found++;
                        break;
                    }
                }
            }

            return found;
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
        public bool HasSpell(string guid) {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.SpellsAttainedList);
            IntPtr spells = GroupSingleComponent(cache, (int)GameStateContext.SpellsAttainedList);

            int count = Program.Read<int>(spells, 0x8, 0xc);
            spells = Program.Read<IntPtr>(spells, 0x8, 0x8);
            byte[] data = Program.Read(spells + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                IntPtr entity = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                string id = Program.ReadString(entity, 0x8, 0xc, 0x0);

                if (guid.Equals(id, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }
        public Dictionary<string, Spell> Spells() {
            IntPtr cache = GetCache(Context.GameState, (int)GameStateContext.SpellsAttainedList);
            IntPtr spells = GroupSingleComponent(cache, (int)GameStateContext.SpellsAttainedList);

            currentSpells.Clear();
            int count = Program.Read<int>(spells, 0x8, 0xc);
            spells = Program.Read<IntPtr>(spells, 0x8, 0x8);
            byte[] data = Program.Read(spells + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                IntPtr entity = (IntPtr)BitConverter.ToUInt32(data, i * 0x4);
                int level = Program.Read<int>(entity, 0x10);
                Spell spell = Program.Read<SpellData>(entity, 0x8, 0xc).Create(Program);
                spell.Level = level;
                currentSpells.Add(spell.Guid, spell);
            }

            return currentSpells;
        }
        public Quest Quest(string guid) {
            IntPtr cache = GetCache(Context.Quest, (int)QuestContext.QuestID);
            List<IntPtr> entities = GetGroupCache(cache);

            int count = entities.Count;
            for (int i = 0; i < count; i++) {
                IntPtr entity = entities[i];
                //entity._components[QuestStarted]
                bool started = Program.Read<uint>(entity, 0x10 + ((int)QuestContext.QuestStarted * 0x4)) != 0;
                //entity._components[QuestCompleted]
                bool completed = Program.Read<uint>(entity, 0x10 + ((int)QuestContext.QuestCompleted * 0x4)) != 0;
                //entity._components[QuestID].value
                entity = (IntPtr)Program.Read<uint>(entity, 0x10 + ((int)QuestContext.QuestID * 0x4), 0x8);

                Quest obj = Program.Read<QuestData>(entity, 0xc).Create(Program);
                obj.Started = started;
                obj.Completed = completed;
                if (obj.Guid == guid) { return obj; }
            }
            return null;
        }
        public Dictionary<string, Quest> Quests() {
            IntPtr cache = GetCache(Context.Quest, (int)QuestContext.QuestID);
            List<IntPtr> entities = GetGroupCache(cache);
            currentQuests.Clear();

            int count = entities.Count;
            for (int i = 0; i < count; i++) {
                IntPtr entity = entities[i];
                //entity._components[QuestStarted]
                bool started = Program.Read<uint>(entity, 0x10 + ((int)QuestContext.QuestStarted * 0x4)) != 0;
                //entity._components[QuestCompleted]
                bool completed = Program.Read<uint>(entity, 0x10 + ((int)QuestContext.QuestCompleted * 0x4)) != 0;
                if (!started && !completed) { continue; }

                //entity._components[QuestID].value
                entity = (IntPtr)Program.Read<uint>(entity, 0x10 + ((int)QuestContext.QuestID * 0x4), 0x8);

                Quest obj = Program.Read<QuestData>(entity, 0xc).Create(Program);
                obj.Started = started;
                obj.Completed = completed;
                currentQuests.Add(obj.Guid, obj);
            }
            return currentQuests;
        }
        private IntPtr GetCache(Context context, int componentID) {
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
                cacheItem.RefreshDate = DateTime.Now.AddSeconds(2);
            }
            return cacheItem.Item;
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
                returnVal.Add(Program.Read<IntPtr>((IntPtr)BitConverter.ToUInt32(data, i * 0x4), 0x24));
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
        private List<IntPtr> GetGroupCache(IntPtr group) {
            CacheItem<List<IntPtr>> cache = null;
            if (!groupCache.TryGetValue(group, out cache) || DateTime.Now > cache.RefreshDate) {
                if (cache == null) {
                    cache = new CacheItem<List<IntPtr>>();
                    groupCache.Add(group, cache);
                }
                cache.Item = GroupComponents(group);
                cache.RefreshDate = DateTime.Now.AddMilliseconds(500);
            }
            return cache.Item;
        }
        private List<IntPtr> GroupComponents(IntPtr group) {
            List<IntPtr> entities = new List<IntPtr>();

            //group._entities.Count
            int count = Program.Read<int>(group, 0x24);
            //group._entities.Items
            group = Program.Read<IntPtr>(group, 0x10);
            byte[] data = Program.Read(group + 0x10, count * 0x4);
            for (int i = 0; i < count; i++) {
                //.Items[i]._components
                group = Program.Read<IntPtr>((IntPtr)BitConverter.ToUInt32(data, i * 0x4), 0x24);
                entities.Add(group);
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