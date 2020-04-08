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
                    case SplitType.Chest:
                        CheckChest(split);
                        break;
                    case SplitType.DungeonComplete:
                        CheckDungeon(split);
                        break;
                    case SplitType.Level:
                        CheckLevel(split);
                        break;
                    case SplitType.QuestStart:
                        CheckQuest(split, false);
                        break;
                    case SplitType.QuestComplete:
                        CheckQuest(split, true);
                        break;
                    case SplitType.RoyalArt:
                        CheckRoyalArt(split);
                        break;
                    case SplitType.Spell:
                        CheckSpell(split);
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
        private void CheckRoyalArt(Split split) {
            SplitRoyalArt royalArt = Utility.GetEnumValue<SplitRoyalArt>(split.Value);
            switch (royalArt) {
                case SplitRoyalArt.RollAttack: CheckRoyalArt(RoyalArts.RollAttack); break;
                case SplitRoyalArt.RoyalSmash: CheckRoyalArt(RoyalArts.RoyalSmash); break;
                case SplitRoyalArt.SpellSlot: CheckRoyalArt(RoyalArts.SpellSlot); break;
                case SplitRoyalArt.WaterWalk: CheckRoyalArt(RoyalArts.WaterWalk); break;
            }
        }
        private void CheckRoyalArt(RoyalArts art) {
            RoyalArts royalArts = Memory.PlayerRoyalArts();
            ShouldSplit = (royalArts & art) != RoyalArts.None && ((RoyalArts)lastIntValue & art) == RoyalArts.None;
            lastIntValue = (int)royalArts;
        }
        private void CheckChest(Split split) {
            SplitChest chest = Utility.GetEnumValue<SplitChest>(split.Value);
            switch (chest) {
                case SplitChest.BraveCaveNormal1: CheckChest("860602c55fdf54cfdb988308d5dc9245"); break;
                case SplitChest.BraveCaveNormal2: CheckChest("3d327315bdc8f394ea178366681a2587"); break;
                case SplitChest.BraveCaveWood1: CheckChest("769af5d9fb8919e43b5399b840842d5a"); break;
                case SplitChest.BraveCaveWood2: CheckChest("0a039b88ce7979a42bf54d9730b02285"); break;
                case SplitChest.FursakenCaveWood: CheckChest("9a2ad71b53496f547ad4af0635199cd4"); break;
                case SplitChest.FursakenCaveNormal1: CheckChest("9645b61eca57207498d3863b11053104"); break;
                case SplitChest.FursakenCaveNormal2: CheckChest("89a9b3dc80974c54095c87679b5bcd52"); break;
                case SplitChest.FursakenCaveBoss: CheckChest(""); break;
                case SplitChest.SeasideCoveNormal1: CheckChest("326206b46b413614dbc436f5e5f58606"); break;
                case SplitChest.SeasideCoveNormal2: CheckChest("05e27c72b5d7ded4eb6123728f595f63"); break;
                case SplitChest.SeasideCoveWood: CheckChest("befac82108769ce468a5b31018167604"); break;
            }
        }
        private void CheckChest(string guid) {
            bool value = Memory.HasChest(guid);
            ShouldSplit = value && !lastBoolValue;
            lastBoolValue = value;
        }
        private void CheckLevel(Split split) {
            int level = Memory.Level();
            int splitLevel = -1;
            int.TryParse(split.Value, out splitLevel);
            ShouldSplit = lastIntValue != level && level == splitLevel;
            lastIntValue = level;
        }
        private void CheckSpell(Split split) {
            SplitSpell spell = Utility.GetEnumValue<SplitSpell>(split.Value);
            bool value = Memory.Spell(spell.ToString()) != null;
            ShouldSplit = value && !lastBoolValue;
            lastBoolValue = value;
        }
        private void CheckQuest(Split split, bool complete) {
            SplitQuest quest = Utility.GetEnumValue<SplitQuest>(split.Value);
            switch (quest) {
                case SplitQuest.BlacksmithKit: CheckQuest(Memory.Quest("2033db891553b5044ba159b2c585ad5b"), complete); break;
                case SplitQuest.Tutorial: CheckQuest(Memory.Quest("01dcb2d755fd9c345bfa2ac2cfd66788"), complete); break;
            }
        }
        private void CheckQuest(Quest quest, bool complete) {
            bool value = false;
            if (quest != null) {
                value = complete ? quest.Completed : quest.Started;
                ShouldSplit = value && !lastBoolValue;
            }
            lastBoolValue = value;
        }
        private void CheckDungeon(Split split) {
            SplitDungeon dungeon = Utility.GetEnumValue<SplitDungeon>(split.Value);
            string sceneToCheck = string.Empty;
            switch (dungeon) {
                case SplitDungeon.BraveCave: sceneToCheck = "Cave_bravecave"; break;
                case SplitDungeon.CaveGrotto: sceneToCheck = "Cave_cavegrotto"; break;
                case SplitDungeon.FursakenCave: sceneToCheck = "Cave_fursakencave"; break;
                case SplitDungeon.SeasideCove: sceneToCheck = "Cave_seasidecove"; break;
            }

            int dungeonsComplete = Memory.DungeonsCleared();
            ShouldSplit = dungeonsComplete > lastIntValue && Memory.SceneName() == sceneToCheck;
            lastIntValue = dungeonsComplete;
        }
        private void CheckArea(Split split, bool enter) {
            SplitArea area = Utility.GetEnumValue<SplitArea>(split.Value);
            switch (area) {
                case SplitArea.BraveCave: CheckScene(enter, "Cave_bravecave"); break;
                case SplitArea.CaveGrotto: CheckScene(enter, "Cave_cavegrotto"); break;
                case SplitArea.FursakenCave: CheckScene(enter, "Cave_fursakencave"); break;
                case SplitArea.Kingsmarker: CheckScene(enter, "Ruins_Kingsmarker"); break;
                case SplitArea.KitCat: CheckScene(enter, "Interior_KitCat"); break;
                case SplitArea.Overworld: CheckScene(enter, "MainOverworld"); break;
                case SplitArea.SeasideCove: CheckScene(enter, "Cave_seasidecove"); break;
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