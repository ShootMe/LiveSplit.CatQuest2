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
        Spells
    }
    public class LogManager {
        public List<ILogEntry> LogEntries = new List<ILogEntry>();
        private Dictionary<LogObject, string> currentValues = new Dictionary<LogObject, string>();
        private Dictionary<string, Quest> currentQuests = new Dictionary<string, Quest>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Chest> currentChests = new Dictionary<string, Chest>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Spell> currentSpells = new Dictionary<string, Spell>(StringComparer.OrdinalIgnoreCase);
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
                bool isLoading = logic.Memory.IsLoading();

                foreach (LogObject key in Enum.GetValues(typeof(LogObject))) {
                    string previous = currentValues[key];
                    string current = null;

                    switch (key) {
                        case LogObject.CurrentSplit: current = $"{logic.CurrentSplit} ({GetCurrentSplit(logic, settings)})"; break;
                        case LogObject.Pointers: current = logic.Memory.GamePointers(); break;
                        case LogObject.Version: current = MemoryManager.Version.ToString(); break;
                        case LogObject.Loading: current = isLoading.ToString(); break;
                        case LogObject.Quests: CheckQuests(logic); break;
                        case LogObject.Chests: CheckChests(logic); break;
                        case LogObject.Spells: CheckSpells(logic); break;
                        case LogObject.Gold: current = logic.Memory.Gold().ToString(); break;
                        case LogObject.Level: current = logic.Memory.Level().ToString(); break;
                        case LogObject.Experience: current = logic.Memory.Experience().ToString(); break;
                        case LogObject.Dungeons: current = logic.Memory.DungeonsCleared().ToString(); break;
                        case LogObject.RoyalArts: current = logic.Memory.PlayerRoyalArts().ToString(); break;
                        case LogObject.Scene: current = logic.Memory.SceneName(); break;
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
        private void CheckQuests(LogicManager logic) {
            DateTime date = DateTime.Now;
            Dictionary<string, Quest> quests = logic.Memory.Quests();
            foreach (KeyValuePair<string, Quest> pair in quests) {
                string key = pair.Key;
                Quest state = pair.Value;

                Quest oldState;
                if (currentQuests.TryGetValue(key, out oldState)) {
                    bool value = state.Completed;
                    bool oldValue = oldState.Completed;
                    if (value != oldValue) {
                        AddEntryUnlocked(new ValueLogEntry(date, LogObject.Quests, oldState, state));
                        currentQuests[key] = state;
                    }
                } else {
                    currentQuests[key] = state;
                }
            }
        }
        private void CheckChests(LogicManager logic) {
            DateTime date = DateTime.Now;
            Dictionary<string, Chest> chests = logic.Memory.Chests();
            foreach (KeyValuePair<string, Chest> pair in chests) {
                string key = pair.Key;
                Chest state = pair.Value;

                Chest oldState = null;
                if (!currentChests.TryGetValue(key, out oldState)) {
                    AddEntryUnlocked(new ValueLogEntry(date, LogObject.Chests, oldState, state));
                    currentChests[key] = state;
                }
            }
            List<string> itemsToRemove = new List<string>();
            foreach (KeyValuePair<string, Chest> pair in currentChests) {
                string key = pair.Key;
                Chest state = pair.Value;

                if (!chests.ContainsKey(key)) {
                    AddEntryUnlocked(new ValueLogEntry(date, LogObject.Chests, state, null));
                    itemsToRemove.Add(key);
                }
            }
            for (int i = 0; i < itemsToRemove.Count; i++) {
                currentChests.Remove(itemsToRemove[i]);
            }
        }
        private void CheckSpells(LogicManager logic) {
            DateTime date = DateTime.Now;
            Dictionary<string, Spell> spells = logic.Memory.Spells();
            foreach (KeyValuePair<string, Spell> pair in spells) {
                string key = pair.Key;
                Spell state = pair.Value;

                Spell oldState = null;
                if (!currentSpells.TryGetValue(key, out oldState) || oldState.Level != state.Level) {
                    AddEntryUnlocked(new ValueLogEntry(date, LogObject.Spells, oldState, state));
                    currentSpells[key] = state;
                }
            }
            List<string> itemsToRemove = new List<string>();
            foreach (KeyValuePair<string, Spell> pair in currentSpells) {
                string key = pair.Key;
                Spell state = pair.Value;

                if (!spells.ContainsKey(key)) {
                    AddEntryUnlocked(new ValueLogEntry(date, LogObject.Spells, state, null));
                    itemsToRemove.Add(key);
                }
            }
            for (int i = 0; i < itemsToRemove.Count; i++) {
                currentSpells.Remove(itemsToRemove[i]);
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
