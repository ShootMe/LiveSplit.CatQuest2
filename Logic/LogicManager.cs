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
                case SplitChest.BarkingCaveWood: CheckChest("ed066952e80120b4fb90e7b632bb9c26"); break;
                case SplitChest.BarkingCaveNormal1: CheckChest("a5b512a5b9ff9e0438b343a8c2d30c3b"); break;
                case SplitChest.BarkingCaveNormal2: CheckChest("22b6d823c9f8cd24288a3141335f4008"); break;
                case SplitChest.BarkingCaveRed: CheckChest("bd1f6de32a518c14097da1a66ecc8414"); break;
                case SplitChest.BarkingCaveLocked: CheckChest("232c4246fe585c348b8311e296a93e30"); break;
                case SplitChest.BraveCaveWood1: CheckChest("769af5d9fb8919e43b5399b840842d5a"); break;
                case SplitChest.BraveCaveWood2: CheckChest("0a039b88ce7979a42bf54d9730b02285"); break;
                case SplitChest.BraveCaveNormal1: CheckChest("860602c55fdf54cfdb988308d5dc9245"); break;
                case SplitChest.BraveCaveNormal2: CheckChest("3d327315bdc8f394ea178366681a2587"); break;
                case SplitChest.CatpitalCaveWood: CheckChest("ff0d9c83517b99848a31515190e46dfd"); break;
                case SplitChest.CatpitalCaveNormal: CheckChest("22508efd4530e6a4995aeb2406886a7d"); break;
                case SplitChest.CatpitalCaveLocked: CheckChest("65fd088605adf464bbd162b4ecafb62e"); break;
                case SplitChest.CaveGrottoNormal1: CheckChest("2a386c8407b959c43b0f8ab180204cf8"); break;
                case SplitChest.CaveGrottoNormal2: CheckChest("4146c61d22bf44f4383d31f4bf62e8f6"); break;
                case SplitChest.CavePeasyWood: CheckChest("8ba097ecd47b8bc4b83a467c452505ed"); break;
                case SplitChest.CavePeasyNormal: CheckChest("32a74d0cc576c0f44961b0416e6237d5"); break;
                case SplitChest.CavePeasyLocked: CheckChest("52f4486660d0fde41bf8082f42ae0fc1"); break;
                case SplitChest.DecemRuinsRed: CheckChest("c0513ac7468734c4eba25eca1d33bac7"); break;
                case SplitChest.DevigniRuinsRed: CheckChest("ecae2a670e00ba64d83740f858534800"); break;
                case SplitChest.EmptyHoleWood: CheckChest("75a1c34a8d522a846a27ff280bd55b1b"); break;
                case SplitChest.EmptyHoleNormal: CheckChest("389160862c0dbac49a1b1a7a64fba2c3"); break;
                case SplitChest.EmptyHoleLocked: CheckChest("c57f3bf417c494b4d92d9b02437d8e57"); break;
                case SplitChest.FurrestCaveWood1: CheckChest("30f56ca35b31a6049af7ba04cfff6096"); break;
                case SplitChest.FurrestCaveWood2: CheckChest("40775876df715834080cae00d1eccfc1"); break;
                case SplitChest.FurrestCaveNormal: CheckChest("310fc77e07ed4ac4481ae92c23c3f435"); break;
                case SplitChest.FurrestCaveLocked: CheckChest("f97424a31d729a5489a1ddb82cfe29b5"); break;
                case SplitChest.FursakenCaveWood: CheckChest("9a2ad71b53496f547ad4af0635199cd4"); break;
                case SplitChest.FursakenCaveNormal1: CheckChest("9645b61eca57207498d3863b11053104"); break;
                case SplitChest.FursakenCaveNormal2: CheckChest("89a9b3dc80974c54095c87679b5bcd52"); break;
                case SplitChest.FursakenCaveLocked: CheckChest(""); break;
                case SplitChest.KingLionardoRuinsNormal1: CheckChest("b4df01a6c473bca4abc1ac55b035c097"); break;
                case SplitChest.KingLionardoRuinsNormal2: CheckChest("8fdfd5e4034240845af98f4983ee2d69"); break;
                case SplitChest.KingSigilRuinsNormal1: CheckChest("c63431d4f98d5644ea7b41ebe53f6969"); break;
                case SplitChest.KingSigilRuinsNormal2: CheckChest("ec1200ce45611b54aaeed39661104ebf"); break;
                case SplitChest.KitsTrialNormal1: CheckChest("dfe9c3f08ae45cb4dac761894e9df127"); break;
                case SplitChest.KitsTrialNormal2: CheckChest("7554f2abd0c00c141b86e7e6750312bf"); break;
                case SplitChest.NovemRuinsRed: CheckChest("15059423ac280e34c9c4207bd723c348"); break;
                case SplitChest.OctoRuinsRed: CheckChest("9277eb480428d7348844065abebf916c"); break;
                case SplitChest.PawfulCaveWood: CheckChest("f0aee4feedf337843b849fb388071edf"); break;
                case SplitChest.PawfulCaveNormal: CheckChest("0a0fa10e774447a4dadf2407b67f9498"); break;
                case SplitChest.PawfulCaveLocked: CheckChest("7fd103dff9394dd438c90ca85628ef02"); break;
                case SplitChest.PawreignCaveNormal: CheckChest("3fcd0888ff22bf746af78443530bcdac"); break;
                case SplitChest.PawreignCaveLocked: CheckChest("e09ce3f99f6daa741af064d79dc93da3"); break;
                case SplitChest.PawsCaveNormal: CheckChest("d91c0e77f21ddc748a44f2b8f979e150"); break;
                case SplitChest.PawsCaveLocked: CheckChest("7dd459dda4ffaae4f9bee921ab1c34ed"); break;
                case SplitChest.PurrcludedCaveWood: CheckChest("7e36399f52661e742b33cf0cd24dffb6"); break;
                case SplitChest.PurrcludedCaveNormal: CheckChest("5842ebc0f1d07344f819535af273dc27"); break;
                case SplitChest.PurrcludedCaveLocked: CheckChest("dde373c035825d24a9f2823def041a2b"); break;
                case SplitChest.PussCaveWood: CheckChest("283881cfffb372745a319ed840e30650"); break;
                case SplitChest.PussCaveNormal: CheckChest("81a2b57ca1b34fd4a87837f50c27cd29"); break;
                case SplitChest.PussCaveLocked: CheckChest("c32ccfce02d41924e9115d9fff8afa7b"); break;
                case SplitChest.QuadecimRuinsRed: CheckChest("b7af8be1191e81d4ea10bbf75f3e5ee4"); break;
                case SplitChest.RiverHoleNormal: CheckChest("acecd3d57514bad42a0da7473d6dcbf2"); break;
                case SplitChest.RiverHoleLocked: CheckChest("2ed9560b288e71446ad728c1be4fdc53"); break;
                case SplitChest.RuffCoveWood: CheckChest("9598a1705f14d744f94fd55277130a70"); break;
                case SplitChest.RuffCoveNormal1: CheckChest("076f5fba1049fbd41a2177f1e83c1c75"); break;
                case SplitChest.RuffCoveNormal2: CheckChest("9e908ce23a7df1743982a674510672fe"); break;
                case SplitChest.RuffCoveLocked: CheckChest("f5a94893dff0d8a438b77f589fdeb728"); break;
                case SplitChest.SeasideCoveWood: CheckChest("befac82108769ce468a5b31018167604"); break;
                case SplitChest.SeasideCoveNormal1: CheckChest("326206b46b413614dbc436f5e5f58606"); break;
                case SplitChest.SeasideCoveNormal2: CheckChest("05e27c72b5d7ded4eb6123728f595f63"); break;
                case SplitChest.SeptemRuinsRed: CheckChest("d098e64e1d7eb10449ba72c0407d443d"); break;
                case SplitChest.SeptencinRuinsRed: CheckChest("60077b3cc753273488bae4c0f95ca698"); break;
                case SplitChest.SeptencinRuinsLocked: CheckChest("4b146bf11e9400e4988f8d5ff6f14663"); break;
                case SplitChest.UndecimRuinsRed: CheckChest("c60921c05dc8c2243a6675025de7ca38"); break;
                case SplitChest.WhiskCoveNormal: CheckChest("124537d805f6b724ebecf02a8d08ecdf"); break;
                case SplitChest.WhiskCoveLocked: CheckChest("d872decf091ebdd4d8df0bd4e28c783b"); break;
                case SplitChest.WindingCoveNormal: CheckChest("32fb8cf774f07db4083b6d74c8b121f9"); break;
                case SplitChest.WindingCoveLocked: CheckChest("b51c97d398d4d564f9a64f72a9e63906"); break;
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
                case SplitSpell.Lightnyan: CheckSpell("825bafad02550144bb83fb1f836e6403"); break;
                case SplitSpell.Purrserk: CheckSpell("dfbf3f18e758cc240b1c458d0c5d16f6"); break;
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
                //BarkingCave d2be1b8b9c1837a4bb1b04767f61100e
                //CatpitalCave 3bd16a6967c679e4f81ece15a1197ef7
                //CavePeasy 0cc910dd93badaa4499bdf8cd6a100f6
                //DecemRuins 7f2e61a385842124d995c52b44fa667f
                //DevigniRuins 8f4cd90ad2e3f244d957f542cfbee0f7
                //EmptyHole 43a92655bc926b541b2c6933fce1ce36
                //FurrestCave c621211c245b8e248bf84bec25c5bdbf
                //PawfulCave 50c6e95c6f9a6b946bb12a5cdce7ae39
                //PawreignCave 8e4a0860886f4084db8dcbe0e5a3ee65
                //PawsCave 54da0da6a4589754c80370337f3b72e2
                //PurrcludedCave 4ab4792868ce91e4694c957d1a3749bd
                //PussCave 1a0e7969190456f4fa50a0f2b094b274
                //NovemRuins cfc6c2c022c117c49bebd2ee23afa801
                //OctoRuins 37fb6c998616c8843838f8f7eca83fc2
                //QuadecimRuins 5c112596c4bcd634495b2736ccbd7583
                //RiverHole d74ac0cc3d4d62e41a0ec3fc70026b34
                //RuffCove cc59cbbdbe1d6412f9f84db08922ea2a
                //SeptemRuins b2d4d50737b248242b76f05ac17c2e5f
                //SeptencinRuins 1a5736c141d319543bb173afe9e1d12d
                //UndecimRuins 53be2b68af9f8a34a8571f325f66699e
                //WhiskCove 524ad8edcf842ed40979c69a53665206
                //WindingCove 855e6c49e81396c4a9bf3d1e273a2817
                case SplitKey.ArcaneHeadpawters: CheckKey("5a40932a9be236f46882840732c6ee25"); break;
                case SplitKey.KingLionardoRuins: CheckKey("18c38d642116c05448a4d3cdbb341519"); break;
                case SplitKey.Kingsmarker2: CheckKey("8999191aaf53a437ba2c892d404343de"); break;
                case SplitKey.Kingsmarker3: CheckKey("31d65870769db4d43b71a47497f5e15d"); break;
                case SplitKey.Kingsmarker4: CheckKey("82f6db3dc8b224a2996051e6f841d0b7"); break;
                case SplitKey.Kingsmarker5: CheckKey("44b9b72018187504294296055d3659db"); break;
                case SplitKey.Kingsmarker6: CheckKey("6a7144106fbc96244acb96f841c1571d"); break;
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
                case SplitQuest.AcceptThisNow: CheckQuest("12c427bffe7d851419791efb2992d926", complete); break;
                case SplitQuest.AncientRealm: CheckQuest("f0235f2618b3e124a9f6017bab0be39a", complete); break;
                case SplitQuest.ArcaneKitties: CheckQuest("4aa539578917a56448165bc3ca4940fe", complete); break;
                case SplitQuest.ArtefactOfWaterWalking: CheckQuest("799f459d74406f844b58d0dcd640898e", complete); break;
                case SplitQuest.BadConsequences: CheckQuest("226f85811e0301d41bad269f8638a577", complete); break;
                case SplitQuest.BadDoggos: CheckQuest("177ee814e1258814c8f587e24290b7da", complete); break;
                case SplitQuest.BadPropawganda: CheckQuest("f398a3e2d1d4a724fad3a8ea37fc83e7", complete); break;
                case SplitQuest.BlacksmithKit: CheckQuest("2033db891553b5044ba159b2c585ad5b", complete); break;
                case SplitQuest.BookOfWaterWalking: CheckQuest("311e5037a5177b84ba470353fb896738", complete); break;
                case SplitQuest.CaseOfTheCodedNote: CheckQuest("595bcf670107dc040bbd1cd3408a0725", complete); break;
                case SplitQuest.CaseOfTheHiddenGrudge: CheckQuest("8910d1fffe64aa84ab139cae1cbd4160", complete); break;
                case SplitQuest.CaseOfTheMissingBlueprint: CheckQuest("6a7a0e224825f53438991677673bf8f2", complete); break;
                case SplitQuest.CaseOfTheMissingPiece: CheckQuest("c792c2548f9384c4d91f1e177b20e5b6", complete); break;
                case SplitQuest.CaseOfTheSecretPassword: CheckQuest("af533c1f401ddbd4fbcb0534ae7b2b02", complete); break;
                case SplitQuest.Coda: CheckQuest("a9d92dcc5b8d8774e9cf42e6d102b8b8", complete); break;
                case SplitQuest.ConfurrsationOfSecrets: CheckQuest("9ccd001333d82ec4b850ac6c8764b0aa", complete); break;
                case SplitQuest.CurseOfTheDogs: CheckQuest("855fd0c3ccfa0034696cf30a6cfdec67", complete); break;
                case SplitQuest.DeletingDataNow: CheckQuest("1a4c920d0e230024689d0bf3e6c4561c", complete); break;
                case SplitQuest.DogeKnight: CheckQuest("94ae6c5c46f536242b3b1de5e3898f82", complete); break;
                case SplitQuest.Dragonblood: CheckQuest("0ebb95be47dcda247892e1b0494f67a7", complete); break;
                case SplitQuest.EpicStaredown: CheckQuest("1c144a7d0689b2e42b19c5e7aabe5830", complete); break;
                case SplitQuest.Epilogue: CheckQuest("061eee52668350242b85e584e87f6050", complete); break;
                case SplitQuest.EyeOfEternity: CheckQuest("1f3a23f5eac051d48ace4ebefa2aa2d3", complete); break;
                case SplitQuest.FakingPurrrivateMewan: CheckQuest("0a80760586bc17f4695b363f12f5c294", complete); break;
                case SplitQuest.FinalPurreperations: CheckQuest("5fc5c861ad68fd94c9b917c87832baf7", complete); break;
                case SplitQuest.FinalShard: CheckQuest("f5fb355c99570574bb46ca53c5134201", complete); break;
                case SplitQuest.FindingPurrrivateMewan: CheckQuest("e45490684afec9f42b055b11ed42fac7", complete); break;
                case SplitQuest.FirstKings: CheckQuest("4e1df48ecbf1f0940843cf6a758922ff", complete); break;
                case SplitQuest.FursACrowd: CheckQuest("3f0853ce5421717458ae8abb4467a9bb", complete); break;
                case SplitQuest.FurTheFurmily: CheckQuest("18dc7d46c3ba2164d94fecd4ffff8a4f", complete); break;
                case SplitQuest.FurTheGreaterGood: CheckQuest("d5eb0f580b334d8439768f0846830f1d", complete); break;
                case SplitQuest.FurTheMother: CheckQuest("8428350affea4674c9542ffa872ec2d2", complete); break;
                case SplitQuest.FurTheSon: CheckQuest("4794a5e722a78cd44980e83bde8d3357", complete); break;
                case SplitQuest.GiftOfWaterWalking: CheckQuest("02ed90ff49761af4e8bc438e528493ed", complete); break;
                case SplitQuest.GiveToThePoor: CheckQuest("20d10d866618b82439c4f95125638135", complete); break;
                case SplitQuest.GoodIntentions: CheckQuest("9c178c2f2d54dc54685643ccfe3d45a5", complete); break;
                case SplitQuest.KeiNein: CheckQuest("4e54ec43c21570e49910658a8ba0e330", complete); break;
                case SplitQuest.KingLupusI: CheckQuest("4b0ad8eab847f4b41bc4389778e0a1eb", complete); break;
                case SplitQuest.Kingsblade: CheckQuest("2fcf4c7428d1e83428570caf34d8b530", complete); break;
                case SplitQuest.KingsOfOld: CheckQuest("b36f0bc9b8d20fe4794d67dc02f3fd80", complete); break;
                case SplitQuest.Labrathor: CheckQuest("6a9a4b0b85e3f8942aa70aa447cced8d", complete); break;
                case SplitQuest.LionerAndWolfen: CheckQuest("ffbec5a34209edd47a8c3dce0a139a4f", complete); break;
                case SplitQuest.LostTreasure: CheckQuest("4c632637c910065429618569d4a806a3", complete); break;
                case SplitQuest.MagicUser: CheckQuest("5612bd65b1ecc814cbc9bd608559db47", complete); break;
                case SplitQuest.Meat: CheckQuest("ccbf3d788d8275b429f31861c5690c16", complete); break;
                case SplitQuest.MeatMeat: CheckQuest("4ce34bb985dffdd4b8c80da2821b6084", complete); break;
                case SplitQuest.MeatMeatMeeeaaat: CheckQuest("a094a3f1648a1844884d24a352d2e54d", complete); break;
                case SplitQuest.OfMetalsAndMagic: CheckQuest("f0bac905e280981408680b76e24b6907", complete); break;
                case SplitQuest.OfSwordsAndTheWorthy: CheckQuest("33076b70be297ea4dbfcfd753e8f39e8", complete); break;
                case SplitQuest.OhYouGonnaGetItNow: CheckQuest("9d080f3b53898d24a9dbd77f80b0afce", complete); break;
                case SplitQuest.OrderOfTheEmeowrald: CheckQuest("ad2757c65d99f8647a6c4552cdc7ffa7", complete); break;
                case SplitQuest.OverTheLimit: CheckQuest("4a6e45c142344724f9b5830b2764fc3e", complete); break;
                case SplitQuest.PawlatinumChef: CheckQuest("e516ebe387f735b46ba6e33ded7a2e29", complete); break;
                case SplitQuest.PawsitivelySecretContact: CheckQuest("f6d1c1dde10c6d545b5962100a1a5411", complete); break;
                case SplitQuest.PawsTogether: CheckQuest("fc11103c9c75c9c45897bf3480fc1d8d", complete); break;
                case SplitQuest.PowerfulOne: CheckQuest("3fa9425b2bf9cc546812ee0176dd9dc4", complete); break;
                case SplitQuest.PowerOfTheHorde: CheckQuest("05a2474a84796c741983cc4213785c2b", complete); break;
                case SplitQuest.Prelude: CheckQuest("9840685e69e1c4c4b9914d1589c23913", complete); break;
                case SplitQuest.PundorasBox: CheckQuest("37721b39a7a009148babd6cfc1766577", complete); break;
                case SplitQuest.PurrotectedMaze: CheckQuest("2b132ae2877b0bc41a5666edeaa91c2d", complete); break;
                case SplitQuest.Purrsecutor: CheckQuest("98e574a60ffec244ba7a416ca2ae2bf7", complete); break;
                case SplitQuest.ReclaimFelingard: CheckQuest("01ffb3fce7801b148ba5504728b09e9c", complete); break;
                case SplitQuest.ReclaimTheEmpire: CheckQuest("c98575f4318ba954c8181ba9c73d5b0d", complete); break;
                case SplitQuest.RescuingPurrrivateMewan: CheckQuest("2718778759f7ce34199dda5f6feb026f", complete); break;
                case SplitQuest.ReturnOfTheCats: CheckQuest("6d0eae2d0e72359428c74178db20725f", complete); break;
                case SplitQuest.RevengeOfFurindelmeow: CheckQuest("fce297f7753d23f45b9de879c8a7fb1b", complete); break;
                case SplitQuest.RivalsOfAKind: CheckQuest("deef2183d78d42b4ab79a2a2f9b5c410", complete); break;
                case SplitQuest.RockPaperScissors: CheckQuest("6c49b3bc9d49ae441b8b95897a58be26", complete); break;
                case SplitQuest.SecondMovement: CheckQuest("c6cfd3c6003dad545902c62ed25ed9ab", complete); break;
                case SplitQuest.SkullOfAWarrior: CheckQuest("2e5779b0fb4518b45b89bc28e2255e94", complete); break;
                case SplitQuest.SpellOfTheCats: CheckQuest("fa563330402243e4c97a57649134cdfa", complete); break;
                case SplitQuest.SpiritInTheLake: CheckQuest("68c10979b8cc3ee47956a0b2a179f732", complete); break;
                case SplitQuest.StealFromTheRich: CheckQuest("c9388e46c368f2741baa5d4c10286c19", complete); break;
                case SplitQuest.TalentedSoldier: CheckQuest("37a90349d9fb9f7428480af40f500723", complete); break;
                case SplitQuest.TaleOfACatAndADog: CheckQuest("d6a144fc243723c4ca5be86d9d6c1dc6", complete); break;
                case SplitQuest.ThisIsIt1: CheckQuest("eb3760c19266d31459bd9b4dbcc566f0", complete); break;
                case SplitQuest.ThisIsIt2: CheckQuest("3c292b0f3c18c124e98779a60d7f3b3e", complete); break;
                case SplitQuest.TrackingPurrrivateMewan: CheckQuest("c06e2b12b5e6d274295b4b36c3b5ce8a", complete); break;
                case SplitQuest.TrialOfLionardo: CheckQuest("e123b3ec60d9de94da9fd04e0e6d8551", complete); break;
                case SplitQuest.TrialOfTheFirstKings: CheckQuest("8f2dc042404d89244881a69b06078aa5", complete); break;
                case SplitQuest.TrialOfWoofhauser: CheckQuest("34f8fd72c4c08404aaebd934d1c8418c", complete); break;
                case SplitQuest.TrueWeapon: CheckQuest("a54d620348c1b4249bcb6f3cc981e6ef", complete); break;
                case SplitQuest.Tutorial: CheckQuest("01dcb2d755fd9c345bfa2ac2cfd66788", complete); break;
                case SplitQuest.TwinCaves1: CheckQuest("045578b86c7be014bb54e01a71ff813f", complete); break;//might be switched
                case SplitQuest.TwinCaves2: CheckQuest("e5beee3b4f1a82144b01fa501693709e", complete); break;//might be switched
                case SplitQuest.TwinCavesAgain1: CheckQuest("cdf2ee2cd33170b48a331d178c7fab97", complete); break;
                case SplitQuest.TwinCavesAgain2: CheckQuest("f129fb1ae760da645bd6a57ed3a63bcf", complete); break;
                case SplitQuest.TwinDelivery: CheckQuest("da15597c2b93f5743a9b050bff9404b9", complete); break;
                case SplitQuest.TwinDeliveryAgain: CheckQuest("a764ee1d3112c4c4fb9b415022813cff", complete); break;
                case SplitQuest.WeaponsmithDoggo: CheckQuest("ce48d499ebee9cb4b8fa87e56107491f", complete); break;
                case SplitQuest.Wooftopia: CheckQuest("f0939a9e7baa3a741a4e312665555556", complete); break;
                case SplitQuest.YoungPup: CheckQuest("6bb6f0d7ade62064d823819010d74e4e", complete); break;
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
                case SplitDungeon.BarkingCave: sceneToCheck = "Tomb_barkingcave"; break;
                case SplitDungeon.BayCave: sceneToCheck = "Cave_baycave"; break;
                case SplitDungeon.BlueCave: sceneToCheck = "Cave_bluecave"; break;
                case SplitDungeon.BraveCave: sceneToCheck = "Cave_bravecave"; break;
                case SplitDungeon.CatpitalCave: sceneToCheck = "Cave_catpitalcave"; break;
                case SplitDungeon.CaveGrotto: sceneToCheck = "Cave_cavegrotto"; break;
                case SplitDungeon.CavePeasy: sceneToCheck = "Cave_cavepeasy"; break;
                case SplitDungeon.CursedRuins: sceneToCheck = "Ruins_Cursed"; break;
                case SplitDungeon.DecemRuins: sceneToCheck = "Ruins_Decem"; break;
                case SplitDungeon.DevigniRuins: sceneToCheck = "Ruins_Devigni"; break;
                case SplitDungeon.EmptyHole: sceneToCheck = "Tomb_emptyhole"; break;
                case SplitDungeon.FurrestCave: sceneToCheck = "Cave_furrestcave"; break;
                case SplitDungeon.FurriblePurrison: sceneToCheck = "Cave_furriblecave"; break;
                case SplitDungeon.FursakenCave: sceneToCheck = "Cave_fursakencave"; break;
                case SplitDungeon.KingDuosRuins: sceneToCheck = "Ruins_Duos"; break;
                case SplitDungeon.KingLionardoRuins: sceneToCheck = "Ruins_Lionardo"; break;
                case SplitDungeon.KingSigilRuins: sceneToCheck = "Ruins_KingSigil"; break;
                case SplitDungeon.MountainCave: sceneToCheck = "Cave_mountaincave"; break;
                case SplitDungeon.NovemRuins: sceneToCheck = "Ruins_Novem"; break;
                case SplitDungeon.OctoRuins: sceneToCheck = "Ruins_Octo"; break;
                case SplitDungeon.PawfulCave: sceneToCheck = "Tomb_pawfulcave"; break;
                case SplitDungeon.PawreignCave: sceneToCheck = "Cave_pawreigncave"; break;
                case SplitDungeon.PawsCave: sceneToCheck = "Cave_pawscave"; break;
                case SplitDungeon.PawtCave: sceneToCheck = "Cave_pawtcave"; break;
                case SplitDungeon.PurrcludedCave: sceneToCheck = "Cave_purrcludedcave"; break;
                case SplitDungeon.PurrnCave: sceneToCheck = "Cave_purrncave"; break;
                case SplitDungeon.PussCave: sceneToCheck = "Cave_pusscave"; break;
                case SplitDungeon.QuadecimRuins: sceneToCheck = "Ruins_Quadecim"; break;
                case SplitDungeon.QuattorRuins: sceneToCheck = "Ruins_Quattor"; break;
                case SplitDungeon.RiverHole: sceneToCheck = "Cave_riverhole"; break;
                case SplitDungeon.RiversideCove: sceneToCheck = "Cave_riversidecove"; break;
                case SplitDungeon.RuffCove: sceneToCheck = "Tomb_ruffcave"; break;
                case SplitDungeon.SaximRuins: sceneToCheck = "Ruins_Saxim"; break;
                case SplitDungeon.SeasideCove: sceneToCheck = "Cave_seasidecove"; break;
                case SplitDungeon.SeptemRuins: sceneToCheck = "Ruins_Septem"; break;
                case SplitDungeon.SeptencinRuins: sceneToCheck = "Ruins_Septencin"; break;
                case SplitDungeon.TresRuins: sceneToCheck = "Ruins_Tres"; break;
                case SplitDungeon.UndecimRuins: sceneToCheck = "Ruins_Undecim"; break;
                case SplitDungeon.UnusRuins: sceneToCheck = "Ruins_Unus"; break;
                case SplitDungeon.WhiskCove: sceneToCheck = "Cave_whiskcove"; break;
                case SplitDungeon.WindingCove: sceneToCheck = "Tomb_windingcave"; break;
            }

            int dungeonsComplete = Memory.DungeonsCleared();
            ShouldSplit = dungeonsComplete > lastIntValue && Memory.SceneName() == sceneToCheck;
            lastIntValue = dungeonsComplete;
        }
        private void CheckArea(Split split, bool enter) {
            SplitArea area = Utility.GetEnumValue<SplitArea>(split.Value);
            switch (area) {
                case SplitArea.ArcaneHeadpawters: CheckScene(enter, "Interior_ArcaneUniversity"); break;
                case SplitArea.BarkingCave: CheckScene(enter, "Tomb_barkingcave"); break;
                case SplitArea.BayCave: CheckScene(enter, "Cave_baycave"); break;
                case SplitArea.BlueCave: CheckScene(enter, "Cave_bluecave"); break;
                case SplitArea.BraveCave: CheckScene(enter, "Cave_bravecave"); break;
                case SplitArea.CatpitalCave: CheckScene(enter, "Cave_catpitalcave"); break;
                case SplitArea.CaveGrotto: CheckScene(enter, "Cave_cavegrotto"); break;
                case SplitArea.CavePeasy: CheckScene(enter, "Cave_cavepeasy"); break;
                case SplitArea.CursedRuins: CheckScene(enter, "Ruins_Cursed"); break;
                case SplitArea.DecemRuins: CheckScene(enter, "Ruins_Decem"); break;
                case SplitArea.DevigniRuins: CheckScene(enter, "Ruins_Devigni"); break;
                case SplitArea.EmptyHole: CheckScene(enter, "Tomb_emptyhole"); break;
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
                case SplitArea.PawfulCave: CheckScene(enter, "Tomb_pawfulcave"); break;
                case SplitArea.PawreignCave: CheckScene(enter, "Cave_pawreigncave"); break;
                case SplitArea.PawsCave: CheckScene(enter, "Cave_pawscave"); break;
                case SplitArea.PawtCave: CheckScene(enter, "Cave_pawtcave"); break;
                case SplitArea.PurrcludedCave: CheckScene(enter, "Cave_purrcludedcave"); break;
                case SplitArea.PurrnCave: CheckScene(enter, "Cave_purrncave"); break;
                case SplitArea.PussCave: CheckScene(enter, "Cave_pusscave"); break;
                case SplitArea.QuadecimRuins: CheckScene(enter, "Ruins_Quadecim"); break;
                case SplitArea.QuattorRuins: CheckScene(enter, "Ruins_Quattor"); break;
                case SplitArea.RiverHole: CheckScene(enter, "Cave_riverhole"); break;
                case SplitArea.RiversideCove: CheckScene(enter, "Cave_riversidecove"); break;
                case SplitArea.RuffCove: CheckScene(enter, "Tomb_ruffcave"); break;
                case SplitArea.SaximRuins: CheckScene(enter, "Ruins_Saxim"); break;
                case SplitArea.SeasideCove: CheckScene(enter, "Cave_seasidecove"); break;
                case SplitArea.SeptemRuins: CheckScene(enter, "Ruins_Septem"); break;
                case SplitArea.SeptencinRuins: CheckScene(enter, "Ruins_Septencin"); break;
                case SplitArea.TresRuins: CheckScene(enter, "Ruins_Tres"); break;
                case SplitArea.UndecimRuins: CheckScene(enter, "Ruins_Undecim"); break;
                case SplitArea.UnusRuins: CheckScene(enter, "Ruins_Unus"); break;
                case SplitArea.WhiskCove: CheckScene(enter, "Cave_whiskcove"); break;
                case SplitArea.WindingCove: CheckScene(enter, "Tomb_windingcave"); break;
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