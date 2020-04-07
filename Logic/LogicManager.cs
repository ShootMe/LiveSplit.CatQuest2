using System;
namespace LiveSplit.CatQuest2 {
    public class LogicManager {
        public bool ShouldSplit { get; private set; }
        public bool ShouldReset { get; private set; }
        public int CurrentSplit { get; private set; }
        public bool Running { get; private set; }
        public bool Paused { get; private set; }
        public float GameTime { get; private set; }
        public MemoryManager Memory { get; private set; }
        public SplitterSettings Settings { get; private set; }
        private bool lastBoolValue;
        private int lastIntValue;
        private string lastStrValue;
        private DateTime splitLate;

        public LogicManager(SplitterSettings settings) {
            Memory = new MemoryManager();
            Settings = settings;
            splitLate = DateTime.MaxValue;
        }

        public void Reset() {
            splitLate = DateTime.MaxValue;
            Paused = false;
            Running = false;
            CurrentSplit = 0;
            InitializeSplit();
            ShouldSplit = false;
            ShouldReset = false;
        }
        public void Decrement() {
            CurrentSplit--;
            splitLate = DateTime.MaxValue;
            InitializeSplit();
        }
        public void Increment() {
            Running = true;
            splitLate = DateTime.MaxValue;
            CurrentSplit++;
            InitializeSplit();
        }
        private void InitializeSplit() {
            if (CurrentSplit < Settings.Autosplits.Count) {
                bool temp = ShouldSplit;
                CheckSplit(Settings.Autosplits[CurrentSplit], true);
                ShouldSplit = temp;
            }
        }
        public bool IsHooked() {
            bool hooked = Memory.HookProcess();
            Paused = !hooked;
            ShouldSplit = false;
            ShouldReset = false;
            GameTime = -1;
            return hooked;
        }
        public void Update() {
            if (CurrentSplit < Settings.Autosplits.Count) {
                CheckSplit(Settings.Autosplits[CurrentSplit], false);
                if (!Running) {
                    Paused = true;
                    if (ShouldSplit) {
                        Running = true;
                    }
                }

                if (ShouldSplit) {
                    Increment();
                }
            }
        }
        private void CheckSplit(Split split, bool updateValues) {
            ShouldSplit = false;
            Paused = Memory.IsLoading();

            if (split.Type == SplitType.GameStart) {
                string scene = Memory.SceneName();
                int savedGame = (int)Memory.SavedGame();
                ShouldSplit = scene == "TitleScene" && savedGame != 0 && lastIntValue == 0;
                lastIntValue = savedGame;
            } else {
                if (!updateValues && Paused) {
                    return;
                }

                switch (split.Type) {
                    case SplitType.ManualSplit:
                        break;
                    case SplitType.AreaEnter:
                        CheckArea(split, true);
                        break;
                    case SplitType.AreaExit:
                        CheckArea(split, false);
                        break;
                    case SplitType.DungeonComplete:
                        CheckDungeon(split);
                        break;
                    case SplitType.Level:
                        int level = Memory.Level();
                        int splitLevel = -1;
                        int.TryParse(split.Value, out splitLevel);
                        ShouldSplit = lastIntValue != level && level == splitLevel;
                        lastIntValue = level;
                        break;
                }

                if (Paused) {
                    ShouldSplit = false;
                } else if (DateTime.Now > splitLate) {
                    ShouldSplit = true;
                    splitLate = DateTime.MaxValue;
                }
            }
        }
        private void CheckDungeon(Split split) {
            SplitDungeon dungeon = Utility.GetEnumValue<SplitDungeon>(split.Value);
            string sceneToCheck = string.Empty;
            switch (dungeon) {
                case SplitDungeon.BraveCave: sceneToCheck = "Cave_bravecave"; break;
                case SplitDungeon.SeasideCove: sceneToCheck = "Cave_seasidecove"; break;
                case SplitDungeon.CaveGrotto: sceneToCheck = "Cave_cavegrotto"; break;
            }

            int dungeonsComplete = Memory.DungeonsCleared();
            ShouldSplit = dungeonsComplete > lastIntValue && Memory.SceneName() == sceneToCheck;
            lastIntValue = dungeonsComplete;
        }
        private void CheckArea(Split split, bool enter) {
            SplitArea area = Utility.GetEnumValue<SplitArea>(split.Value);
            switch (area) {
                case SplitArea.BraveCave: CheckScene(enter, "Cave_bravecave"); break;
                case SplitArea.SeasideCove: CheckScene(enter, "Cave_seasidecove"); break;
                case SplitArea.CaveGrotto: CheckScene(enter, "Cave_cavegrotto"); break;
            }
        }
        private void CheckScene(bool enter, string sceneToCheck) {
            string scene = Memory.SceneName();
            if (string.IsNullOrEmpty(scene)) { return; }

            bool current = scene.Equals(sceneToCheck, StringComparison.OrdinalIgnoreCase);
            bool previous = sceneToCheck.Equals(lastStrValue, StringComparison.OrdinalIgnoreCase);
            if (enter) {
                ShouldSplit = current && !previous;
            } else {
                ShouldSplit = !current && previous;
            }
            lastStrValue = scene;
        }
    }
}