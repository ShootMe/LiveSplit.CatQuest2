using System;
using System.Threading;
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
                CheckSplit(Settings.Autosplits[CurrentSplit], !Running);
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
            int savedGame = (int)Memory.SavedGame();
            bool hasSavedGame = savedGame != 0;

            if (!updateValues && !hasSavedGame) {
                return;
            }

            switch (split.Type) {
                case SplitType.ManualSplit:
                    break;
                case SplitType.GameStart:
                    CheckGameStart(savedGame);
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
                case SplitType.Key:
                    CheckKey(split);
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

            if (!hasSavedGame && Running) {
                ShouldSplit = false;
            } else if (DateTime.Now > splitLate) {
                ShouldSplit = true;
                splitLate = DateTime.MaxValue;
            }
        }
        private void CheckGameStart(int savedGame) {
            string scene = Memory.SceneName();
            ShouldSplit = scene == "TitleScene" && savedGame != 0 && lastIntValue == 0;
            if (ShouldSplit) {
                Thread.Sleep(5);
                ShouldSplit = Memory.TotalPlayTime().Ticks == 0;
            }
            lastIntValue = savedGame;
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
                case SplitChest.BraveCaveWood1: CheckChest("769af5d9fb8919e43b5399b840842d5a"); break;
                case SplitChest.BraveCaveWood2: CheckChest("0a039b88ce7979a42bf54d9730b02285"); break;
                case SplitChest.BraveCaveNormal1: CheckChest("860602c55fdf54cfdb988308d5dc9245"); break;
                case SplitChest.BraveCaveNormal2: CheckChest("3d327315bdc8f394ea178366681a2587"); break;
                case SplitChest.CatpitalCaveWood: CheckChest("ff0d9c83517b99848a31515190e46dfd"); break;
                case SplitChest.CatpitalCaveNormal: CheckChest("22508efd4530e6a4995aeb2406886a7d"); break;
                case SplitChest.CatpitalCaveLocked: CheckChest("65fd088605adf464bbd162b4ecafb62e"); break;
                case SplitChest.CaveGrottoNormal1: CheckChest("2a386c8407b959c43b0f8ab180204cf8"); break;
                case SplitChest.CaveGrottoNormal2: CheckChest("4146c61d22bf44f4383d31f4bf62e8f6"); break;
                case SplitChest.DecemRuinsTrial: CheckChest("c0513ac7468734c4eba25eca1d33bac7"); break;
                case SplitChest.FurrestCaveWood1: CheckChest("30f56ca35b31a6049af7ba04cfff6096"); break;
                case SplitChest.FurrestCaveWood2: CheckChest("40775876df715834080cae00d1eccfc1"); break;
                case SplitChest.FurrestCaveNormal: CheckChest("310fc77e07ed4ac4481ae92c23c3f435"); break;
                case SplitChest.FurrestCaveLocked: CheckChest("f97424a31d729a5489a1ddb82cfe29b5"); break;
                case SplitChest.FursakenCaveWood: CheckChest("9a2ad71b53496f547ad4af0635199cd4"); break;
                case SplitChest.FursakenCaveNormal1: CheckChest("9645b61eca57207498d3863b11053104"); break;
                case SplitChest.FursakenCaveNormal2: CheckChest("89a9b3dc80974c54095c87679b5bcd52"); break;
                case SplitChest.FursakenCaveLocked: CheckChest(""); break;
                case SplitChest.KingSigilRuinsNormal1: CheckChest("c63431d4f98d5644ea7b41ebe53f6969"); break;
                case SplitChest.KingSigilRuinsNormal2: CheckChest("ec1200ce45611b54aaeed39661104ebf"); break;
                case SplitChest.KitsTrialNormal1: CheckChest("dfe9c3f08ae45cb4dac761894e9df127"); break;
                case SplitChest.KitsTrialNormal2: CheckChest("7554f2abd0c00c141b86e7e6750312bf"); break;
                case SplitChest.NovemRuinsTrial: CheckChest("15059423ac280e34c9c4207bd723c348"); break;
                case SplitChest.OctoRuinsTrial: CheckChest("9277eb480428d7348844065abebf916c"); break;
                case SplitChest.PawsCaveNormal: CheckChest("d91c0e77f21ddc748a44f2b8f979e150"); break;
                case SplitChest.PawsCaveLocked: CheckChest("7dd459dda4ffaae4f9bee921ab1c34ed"); break;
                case SplitChest.PussCaveWood: CheckChest("283881cfffb372745a319ed840e30650"); break;
                case SplitChest.PussCaveNormal: CheckChest("81a2b57ca1b34fd4a87837f50c27cd29"); break;
                case SplitChest.PussCaveLocked: CheckChest("c32ccfce02d41924e9115d9fff8afa7b"); break;
                case SplitChest.RiverHoleNormal: CheckChest("acecd3d57514bad42a0da7473d6dcbf2"); break;
                case SplitChest.RiverHoleLocked: CheckChest("2ed9560b288e71446ad728c1be4fdc53"); break;
                case SplitChest.SeasideCoveWood: CheckChest("befac82108769ce468a5b31018167604"); break;
                case SplitChest.SeasideCoveNormal1: CheckChest("326206b46b413614dbc436f5e5f58606"); break;
                case SplitChest.SeasideCoveNormal2: CheckChest("05e27c72b5d7ded4eb6123728f595f63"); break;
                case SplitChest.SeptemRuinsTrial: CheckChest("d098e64e1d7eb10449ba72c0407d443d"); break;
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
            switch (spell) {
                case SplitSpell.Flamepurr: CheckSpell("f40361ad748bef44e8df76b76af05a80"); break;
                case SplitSpell.Freezepaw: CheckSpell("347e2dd12ccbd254a9bdc587cee9daa5"); break;
                case SplitSpell.FurryShield: CheckSpell("3357e1ce56e242a40a9d37bb37d02b7a"); break;
                case SplitSpell.HealPaw: CheckSpell("204b183d154bcbf4fa8f5fc1662fbb1c"); break;
            }
        }
        private void CheckSpell(string guid) {
            bool value = Memory.HasSpell(guid);
            ShouldSplit = value && !lastBoolValue;
            lastBoolValue = value;
        }
        private void CheckKey(Split split) {
            SplitKey quest = Utility.GetEnumValue<SplitKey>(split.Value);
            switch (quest) {
                //CatpitalCave 3bd16a6967c679e4f81ece15a1197ef7
                //DecemRuins 7f2e61a385842124d995c52b44fa667f
                //FurrestCave c621211c245b8e248bf84bec25c5bdbf
                //PawsCave 54da0da6a4589754c80370337f3b72e2
                //PussCave 1a0e7969190456f4fa50a0f2b094b274
                //NovemRuins cfc6c2c022c117c49bebd2ee23afa801
                //OctoRuins 37fb6c998616c8843838f8f7eca83fc2
                //RiverHole d74ac0cc3d4d62e41a0ec3fc70026b34
                //SeptemRuins b2d4d50737b248242b76f05ac17c2e5f
                case SplitKey.ArcaneHeadpawters: CheckKey("5a40932a9be236f46882840732c6ee25"); break;
                case SplitKey.KingLionardoRuins: CheckKey("18c38d642116c05448a4d3cdbb341519"); break;
                case SplitKey.Kingsmarker2: CheckKey("8999191aaf53a437ba2c892d404343de"); break;
                case SplitKey.Kingsmarker3: CheckKey("31d65870769db4d43b71a47497f5e15d"); break;
                case SplitKey.Kingsmarker4: CheckKey("82f6db3dc8b224a2996051e6f841d0b7"); break;
                case SplitKey.KitCat: CheckKey("d861b9836ddc3304480ba17284dfda5c"); break;
                case SplitKey.FirstBridge: CheckKey("69c8b1b70a322fd4696bef92065500f9"); break;
            }
        }
        private void CheckKey(string guid) {
            bool value = Memory.HasKey(guid);
            ShouldSplit = value && !lastBoolValue;
            lastBoolValue = value;
        }
        private void CheckQuest(Split split, bool complete) {
            SplitQuest quest = Utility.GetEnumValue<SplitQuest>(split.Value);
            switch (quest) {
                case SplitQuest.ArcaneKitties: CheckQuest("4aa539578917a56448165bc3ca4940fe", complete); break;
                case SplitQuest.BlacksmithKit: CheckQuest("2033db891553b5044ba159b2c585ad5b", complete); break;
                case SplitQuest.CaseOfTheMissingBlueprint: CheckQuest("6a7a0e224825f53438991677673bf8f2", complete); break;
                case SplitQuest.FirstKings: CheckQuest("4e1df48ecbf1f0940843cf6a758922ff", complete); break;
                case SplitQuest.Kingsblade: CheckQuest("2fcf4c7428d1e83428570caf34d8b530", complete); break;
                case SplitQuest.Purrsecutor: CheckQuest("98e574a60ffec244ba7a416ca2ae2bf7", complete); break;
                case SplitQuest.RockPaperScissors: CheckQuest("6c49b3bc9d49ae441b8b95897a58be26", complete); break;
                case SplitQuest.TrialOfLionardo: CheckQuest("e123b3ec60d9de94da9fd04e0e6d8551", complete); break;
                case SplitQuest.Tutorial: CheckQuest("01dcb2d755fd9c345bfa2ac2cfd66788", complete); break;
            }
        }
        private void CheckQuest(string guid, bool complete) {
            Quest quest = Memory.Quest(guid);
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
                case SplitDungeon.BayCave: sceneToCheck = "Cave_baycave"; break;
                case SplitDungeon.BlueCave: sceneToCheck = "Cave_bluecave"; break;
                case SplitDungeon.BraveCave: sceneToCheck = "Cave_bravecave"; break;
                case SplitDungeon.CatpitalCave: sceneToCheck = "Cave_catpitalcave"; break;
                case SplitDungeon.CaveGrotto: sceneToCheck = "Cave_cavegrotto"; break;
                case SplitDungeon.CavePeasy: sceneToCheck = "Cave_cavepeasy"; break;
                case SplitDungeon.CursedRuins: sceneToCheck = "Ruins_Cursed"; break;
                case SplitDungeon.DecemRuins: sceneToCheck = "Ruins_Decem"; break;
                case SplitDungeon.FurrestCave: sceneToCheck = "Cave_furrestcave"; break;
                case SplitDungeon.FurriblePurrison: sceneToCheck = "Cave_furriblecave"; break;
                case SplitDungeon.FursakenCave: sceneToCheck = "Cave_fursakencave"; break;
                case SplitDungeon.KingDuosRuins: sceneToCheck = "Ruins_Duos"; break;
                case SplitDungeon.KingLionardoRuins: sceneToCheck = "Ruins_Lionardo"; break;
                case SplitDungeon.KingSigilRuins: sceneToCheck = "Ruins_KingSigil"; break;
                case SplitDungeon.MountainCave: sceneToCheck = "Cave_mountaincave"; break;
                case SplitDungeon.NovemRuins: sceneToCheck = "Ruins_Novem"; break;
                case SplitDungeon.OctoRuins: sceneToCheck = "Ruins_Octo"; break;
                case SplitDungeon.PawreignCave: sceneToCheck = "Cave_pawreigncave"; break;
                case SplitDungeon.PawsCave: sceneToCheck = "Cave_pawscave"; break;
                case SplitDungeon.PawtCave: sceneToCheck = "Cave_pawtcave"; break;
                case SplitDungeon.PurrcludedCave: sceneToCheck = "Cave_purrcludedcave"; break;
                case SplitDungeon.PurrnCave: sceneToCheck = "Cave_purrncave"; break;
                case SplitDungeon.PussCave: sceneToCheck = "Cave_pusscave"; break;
                case SplitDungeon.QuattorRuins: sceneToCheck = "Ruins_Quattor"; break;
                case SplitDungeon.RiverHole: sceneToCheck = "Cave_riverhole"; break;
                case SplitDungeon.RiversideCove: sceneToCheck = "Cave_riversidecove"; break;
                case SplitDungeon.SaximRuins: sceneToCheck = "Ruins_Saxim"; break;
                case SplitDungeon.SeasideCove: sceneToCheck = "Cave_seasidecove"; break;
                case SplitDungeon.SeptemRuins: sceneToCheck = "Ruins_Septem"; break;
                case SplitDungeon.TresRuins: sceneToCheck = "Ruins_Tres"; break;
                case SplitDungeon.UnusRuins: sceneToCheck = "Ruins_Unus"; break;
                case SplitDungeon.WhiskCove: sceneToCheck = "Cave_whiskcove"; break;
            }

            int dungeonsComplete = Memory.DungeonsCleared();
            ShouldSplit = dungeonsComplete > lastIntValue && Memory.SceneName() == sceneToCheck;
            lastIntValue = dungeonsComplete;
        }
        private void CheckArea(Split split, bool enter) {
            SplitArea area = Utility.GetEnumValue<SplitArea>(split.Value);
            switch (area) {
                case SplitArea.ArcaneHeadpawters: CheckScene(enter, "Interior_ArcaneUniversity"); break;
                case SplitArea.BayCave: CheckScene(enter, "Cave_baycave"); break;
                case SplitArea.BlueCave: CheckScene(enter, "Cave_bluecave"); break;
                case SplitArea.BraveCave: CheckScene(enter, "Cave_bravecave"); break;
                case SplitArea.CatpitalCave: CheckScene(enter, "Cave_catpitalcave"); break;
                case SplitArea.CaveGrotto: CheckScene(enter, "Cave_cavegrotto"); break;
                case SplitArea.CavePeasy: CheckScene(enter, "Cave_cavepeasy"); break;
                case SplitArea.CursedRuins: CheckScene(enter, "Ruins_Cursed"); break;
                case SplitArea.DecemRuins: CheckScene(enter, "Ruins_Decem"); break;
                case SplitArea.FirstBridge: CheckScene(enter, "Ruins_KingStatues"); break;
                case SplitArea.FurrestCave: CheckScene(enter, "Cave_furrestcave"); break;
                case SplitArea.FurriblePurrison: CheckScene(enter, "Cave_furriblecave"); break;
                case SplitArea.FursakenCave: CheckScene(enter, "Cave_fursakencave"); break;
                case SplitArea.HiddenCave: CheckScene(enter, "Cave_hiddencave"); break;
                case SplitArea.KingDuosRuins: CheckScene(enter, "Ruins_Duos"); break;
                case SplitArea.KingLionardoRuins: CheckScene(enter, "Ruins_Lionardo"); break;
                case SplitArea.KingSigilRuins: CheckScene(enter, "Ruins_KingSigil"); break;
                case SplitArea.Kingsmarker: CheckScene(enter, "Ruins_Kingsmarker"); break;
                case SplitArea.KitCat: CheckScene(enter, "Interior_KitCat"); break;
                case SplitArea.KitsTrial: CheckScene(enter, "KitCat_01_Subscene"); break;
                case SplitArea.MountainCave: CheckScene(enter, "Cave_mountaincave"); break;
                case SplitArea.NovemRuins: CheckScene(enter, "Ruins_Novem"); break;
                case SplitArea.OctoRuins: CheckScene(enter, "Ruins_Octo"); break;
                case SplitArea.Overworld: CheckScene(enter, "MainOverworld"); break;
                case SplitArea.PawreignCave: CheckScene(enter, "Cave_pawreigncave"); break;
                case SplitArea.PawsCave: CheckScene(enter, "Cave_pawscave"); break;
                case SplitArea.PawtCave: CheckScene(enter, "Cave_pawtcave"); break;
                case SplitArea.PurrcludedCave: CheckScene(enter, "Cave_purrcludedcave"); break;
                case SplitArea.PurrnCave: CheckScene(enter, "Cave_purrncave"); break;
                case SplitArea.PussCave: CheckScene(enter, "Cave_pusscave"); break;
                case SplitArea.QuattorRuins: CheckScene(enter, "Ruins_Quattor"); break;
                case SplitArea.RiverHole: CheckScene(enter, "Cave_riverhole"); break;
                case SplitArea.RiversideCove: CheckScene(enter, "Cave_riversidecove"); break;
                case SplitArea.SaximRuins: CheckScene(enter, "Ruins_Saxim"); break;
                case SplitArea.SeasideCove: CheckScene(enter, "Cave_seasidecove"); break;
                case SplitArea.SeptemRuins: CheckScene(enter, "Ruins_Septem"); break;
                case SplitArea.TresRuins: CheckScene(enter, "Ruins_Tres"); break;
                case SplitArea.UnusRuins: CheckScene(enter, "Ruins_Unus"); break;
                case SplitArea.WhiskCove: CheckScene(enter, "Cave_whiskcove"); break;
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