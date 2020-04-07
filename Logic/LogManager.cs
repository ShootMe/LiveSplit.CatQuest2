using System;
using System.Collections.Generic;
namespace LiveSplit.CatQuest2 {
    public enum LogObject {
        None,
        CurrentSplit,
        Pointers,
        Version,
        Loading,
        SavedGame,
        Quests,
        Gold,
        Level,
        Experience,
        Scene,
        SceneType,
        RoyalArts,
        Chests,
        Dungeons,
        Spells,
        Keys,
        FinalQuest,
        Equipment
    }
    public class LogManager {
        public List<ILogEntry> LogEntries = new List<ILogEntry>();
        private Dictionary<LogObject, string> currentValues = new Dictionary<LogObject, string>();
        private Dictionary<string, Quest> currentQuests = new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, GuidItem> currentChests = new Dictionary<string, GuidItem>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Spell> currentSpells = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, GuidItem> currentKeys = new Dictionary<string, GuidItem>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Equipment> currentEquipment = new Dictionary<string, Equipment>(StringComparer.OrdinalIgnoreCase);
        public bool EnableLogging;

        public LogManager() {
            EnableLogging = false;
            Clear();
            AddEntryUnlocked(new EventLogEntry("Autosplitter Initialized"));
        }
        public void Clear() {
            lock (LogEntries) {
                LogEntries.Clear();
                foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
                    currentValues[key] = null;
                }
            }
        }
        public void AddEntry(ILogEntry entry) {
            lock (LogEntries) {
                AddEntryUnlocked(entry);
            }
        }
        private void AddEntryUnlocked(ILogEntry entry) {
            LogEntries.Add(entry);
            Console.WriteLine(entry.ToString());
        }
        public void Update(LogicManager logic, SplitterSettings settings) {
            if (!EnableLogging) { return; }

            lock (LogEntries) {
                DateTime date = DateTime.Now;
                IntPtr savedGame = logic.Memory.SavedGame();

                foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
                    string previous = currentValues[key];
                    string current = null;

                    switch (key) {
                        case LogObject.CurrentSplit: current = $"{logic.CurrentSplit} ({GetCurrentSplit(logic, settings)})"; break;
                        case LogObject.Pointers: current = logic.Memory.GamePointers(); break;
                        case LogObject.Version: current = MemoryManager.Version.ToString(); break;
                        case LogObject.Loading: current = logic.Memory.IsLoading().ToString(); break;
                        case LogObject.Quests: if (savedGame != IntPtr.Zero) { CheckItems<Quest>(key, currentQuests, logic.Memory.Quests()); } break;
                        case LogObject.Chests: if (savedGame != IntPtr.Zero) { CheckItems<GuidItem>(key, currentChests, logic.Memory.Chests()); } break;
                        case LogObject.Spells: if (savedGame != IntPtr.Zero) { CheckItems<Spell>(key, currentSpells, logic.Memory.Spells()); } break;
                        case LogObject.Keys: if (savedGame != IntPtr.Zero) { CheckItems<GuidItem>(key, currentKeys, logic.Memory.Keys()); } break;
                        case LogObject.Equipment: if (savedGame != IntPtr.Zero) { CheckItems<Equipment>(key, currentEquipment, logic.Memory.Equipment()); } break;
                        case LogObject.Gold: current = logic.Memory.Gold().ToString(); break;
                        case LogObject.Level: current = logic.Memory.Level().ToString(); break;
                        case LogObject.Experience: current = logic.Memory.Experience().ToString(); break;
                        case LogObject.Dungeons: current = logic.Memory.DungeonsCleared().ToString(); break;
                        case LogObject.RoyalArts: current = logic.Memory.PlayerRoyalArts().ToString(); break;
                        case LogObject.Scene: current = logic.Memory.SceneName(); break;
                        case LogObject.FinalQuest: current = logic.Memory.FinalQuestCompleted().ToString(); break;
                        case LogObject.SceneType: current = logic.Memory.GameSceneType().ToString(); break;
                        case LogObject.SavedGame: current = logic.Memory.SavedGame().ToString("X"); break;
                    }

                    if (previous != current) {
                        AddEntryUnlocked(new ValueLogEntry(date, key, previous, current));
                        currentValues[key] = current;
                    }
                }
            }
        }
        private void CheckItems<T>(LogObject type, Dictionary<string, T> currentItems, Dictionary<string, T> newItems) {
            DateTime date = DateTime.Now;
            foreach (KeyValuePair<string, T> pair in newItems) {
                string key = pair.Key;
                T state = pair.Value;

                T oldState;
                if (!currentItems.TryGetValue(key, out oldState)) {
                    AddEntryUnlocked(new ValueLogEntry(date, type, oldState, state));
                    currentItems[key] = state;
                }
            }
            List<string> itemsToRemove = new List<string>();
            foreach (KeyValuePair<string, T> pair in currentItems) {
                string key = pair.Key;
                T state = pair.Value;

                if (!newItems.ContainsKey(key)) {
                    AddEntryUnlocked(new ValueLogEntry(date, type, state, null));
                    itemsToRemove.Add(key);
                }
            }
            for (int i = 0; i < itemsToRemove.Count; i++) {
                currentItems.Remove(itemsToRemove[i]);
            }
        }
        private string GetCurrentSplit(LogicManager logic, SplitterSettings settings) {
            if (logic.CurrentSplit >= settings.Autosplits.Count) { return "N/A"; }
            return settings.Autosplits[logic.CurrentSplit].ToString();
        }
    }
    public interface ILogEntry { }
    public class ValueLogEntry : ILogEntry {
        public DateTime Date;
        public LogObject Type;
        public object PreviousValue;
        public object CurrentValue;

        public ValueLogEntry(DateTime date, LogObject type, object previous, object current) {
            Date = date;
            Type = type;
            PreviousValue = previous;
            CurrentValue = current;
        }

        public override string ToString() {
            return string.Concat(
                Date.ToString(@"HH\:mm\:ss.fff"),
                ": (",
                Type.ToString(),
                ") ",
                PreviousValue,
                " -> ",
                CurrentValue
            );
        }
    }
    public class EventLogEntry : ILogEntry {
        public DateTime Date;
        public string Event;

        public EventLogEntry(string description) {
            Date = DateTime.Now;
            Event = description;
        }
        public EventLogEntry(DateTime date, string description) {
            Date = date;
            Event = description;
        }

        public override string ToString() {
            return string.Concat(
                Date.ToString(@"HH\:mm\:ss.fff"),
                ": ",
                Event
            );
        }
    }
}
