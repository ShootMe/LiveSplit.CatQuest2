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
        public void Update(int currentSplit) {
            if (currentSplit != CurrentSplit) {
                CurrentSplit = currentSplit;
                if (CurrentSplit > 0) {
                    Running = true;
                }
                InitializeSplit();
            }
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
                case SplitType.GameEnd:
                    CheckGameEnd();
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
        private void CheckGameEnd() {
            bool gameEnd = Memory.FinalQuestCompleted();
            ShouldSplit = gameEnd && !lastBoolValue;
            lastBoolValue = gameEnd;
        }
        private void CheckRoyalArt(Split split) {
            SplitRoyalArt royalArt = Utility.GetEnumValue<SplitRoyalArt>(split.Value);
            switch (royalArt) {
                case SplitRoyalArt.RollAttack: CheckRoyalArt(RoyalArts.RollAttack); break;
                case SplitRoyalArt.PawerSmash: CheckRoyalArt(RoyalArts.RoyalSmash); break;
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
                case SplitChest.BarkingCaveNormal: CheckChest("a5b512a5b9ff9e0438b343a8c2d30c3b", "22b6d823c9f8cd24288a3141335f4008"); break;
                case SplitChest.BarkingCaveRed: CheckChest("bd1f6de32a518c14097da1a66ecc8414"); break;
                case SplitChest.BarkingCaveLocked: CheckChest("232c4246fe585c348b8311e296a93e30"); break;
                case SplitChest.BatCoveRed: CheckChest("b724bba34aa71ff4fbbb5c63a0382a7c", "d8d3c59c5f8e3e64db7341c44f30bb9d", "82303c859343c2c46a3c023d7bc5bcb5"); break;
                case SplitChest.BayCaveWood: CheckChest("264448010caf40148ba319d225a80634"); break;
                case SplitChest.BayCaveNormal: CheckChest("ad83758ae9d8ead43a8ca5224a1b9fb5"); break;
                case SplitChest.BayCaveLocked: CheckChest("3d1df079d367ddb48b6f75664897db64"); break;
                case SplitChest.BraveCaveWood: CheckChest("769af5d9fb8919e43b5399b840842d5a", "0a039b88ce7979a42bf54d9730b02285"); break;
                case SplitChest.BraveCaveNormal: CheckChest("860602c55fdf54cfdb988308d5dc9245", "3d327315bdc8f394ea178366681a2587"); break;
                case SplitChest.BulletHeavenNormal: CheckChest("5ce41ee661771a84ba0d08afbae7dfb9"); break;
                case SplitChest.BulletHeavenLocked: CheckChest("a55da110457105340adbfaf2369afb1e"); break;
                case SplitChest.CactusCaveGold: CheckChest("9fe4d2eea1c58694aa64c23784177dc2"); break;
                case SplitChest.CatpitalCaveWood: CheckChest("ff0d9c83517b99848a31515190e46dfd"); break;
                case SplitChest.CatpitalCaveNormal: CheckChest("22508efd4530e6a4995aeb2406886a7d"); break;
                case SplitChest.CatpitalCaveLocked: CheckChest("65fd088605adf464bbd162b4ecafb62e"); break;
                case SplitChest.CaveGrottoNormal: CheckChest("2a386c8407b959c43b0f8ab180204cf8", "4146c61d22bf44f4383d31f4bf62e8f6"); break;
                case SplitChest.CaveMercyNormal: CheckChest("8261a62e02a3fa9489ec4ea57eab9035"); break;
                case SplitChest.CaveOfTheLionRed: CheckChest("091779f6a8319da45bc22032890a3130", "24771a6637ffd4644a1dbb6453177ab3"); break;
                case SplitChest.CaveOfTheLionLocked: CheckChest("0be5dd6252325244695d445cf7b55edf"); break;
                case SplitChest.CavePeasyWood: CheckChest("8ba097ecd47b8bc4b83a467c452505ed"); break;
                case SplitChest.CavePeasyNormal: CheckChest("32a74d0cc576c0f44961b0416e6237d5"); break;
                case SplitChest.CavePeasyLocked: CheckChest("52f4486660d0fde41bf8082f42ae0fc1"); break;
                case SplitChest.CaveValorWood: CheckChest("4a5defb0f10bd9043863c910dd89b4c5"); break;
                case SplitChest.CaveValorNormal: CheckChest("0b8ea93c54b507647a8c23c1236a7f4f"); break;
                case SplitChest.CaveVirtueNormal: CheckChest("a88e3c2067fb9d040846e604355d8a35"); break;
                case SplitChest.CursedRuinsRed: CheckChest("8c11f57d7cfd62f41a78891715ee052e"); break;
                case SplitChest.DecemRuinsRed: CheckChest("c0513ac7468734c4eba25eca1d33bac7"); break;
                case SplitChest.DevigniRuinsRed: CheckChest("ecae2a670e00ba64d83740f858534800"); break;
                case SplitChest.DrakothsVaultRed: CheckChest("1fbcb23c15f5e3e468cd871f5606ed4f", "4d332e00b77a36a43b1d8589ee50343b"); break;
                case SplitChest.DrakothsVaultLocked: CheckChest("799b0f2ba82bf904db937d50cb7a9ad4"); break;
                case SplitChest.DuodecimRuinsRed: CheckChest("eb95beaa3d09e1d428c1222851c00cef"); break;
                case SplitChest.DuosRuinsRed: CheckChest("1d55003af8dd99f4eaae161afb323da7"); break;
                case SplitChest.EmptyHoleWood: CheckChest("75a1c34a8d522a846a27ff280bd55b1b"); break;
                case SplitChest.EmptyHoleNormal: CheckChest("389160862c0dbac49a1b1a7a64fba2c3"); break;
                case SplitChest.EmptyHoleLocked: CheckChest("c57f3bf417c494b4d92d9b02437d8e57"); break;
                case SplitChest.FarfetchedCaveNormal: CheckChest("dd3108d08f5fc704c9db5acca210d6e9", "cab6ab104b3beae41b5a81a9e6503afb"); break;
                case SplitChest.FarfetchedCaveLocked: CheckChest("f670f8e7fe2db5f41b897b8f719add66"); break;
                case SplitChest.FurrestCaveWood: CheckChest("30f56ca35b31a6049af7ba04cfff6096", "40775876df715834080cae00d1eccfc1"); break;
                case SplitChest.FurrestCaveNormal: CheckChest("310fc77e07ed4ac4481ae92c23c3f435"); break;
                case SplitChest.FurrestCaveLocked: CheckChest("f97424a31d729a5489a1ddb82cfe29b5"); break;
                case SplitChest.FurriblePurrisonWood: CheckChest("7c3de93469dbc4a4daf91424ef98bc32", "20dc75bf87fb6e5488531f962fcc71cd", "2a6c1abe0541eb2498b4008d4c2e49ec", "96f7b29780fa4f3478f0ba4d779cd48d"); break;
                case SplitChest.FurriblePurrisonNormal: CheckChest("93ef209cc7102f743a1647220c0fe13e", "5d948b914b919094189a457b4152d755"); break;
                case SplitChest.FurriblePurrisonRed: CheckChest("801b89117e969a248b6a26592c721466"); break;
                case SplitChest.FurriblePurrisonLocked: CheckChest("f2c3a202f930c0645b86bbb564f46d74"); break;
                case SplitChest.FursakenCaveWood: CheckChest("9a2ad71b53496f547ad4af0635199cd4"); break;
                case SplitChest.FursakenCaveNormal: CheckChest("9645b61eca57207498d3863b11053104", "89a9b3dc80974c54095c87679b5bcd52"); break;
                case SplitChest.FursakenCaveLocked: CheckChest("a5472f5c2b0ebbf4785ed54de487da68"); break;
                case SplitChest.HiddenCaveGold: CheckChest("016caaf1a47ebbb49ba4f2216690eb38"); break;
                case SplitChest.HiddenStashWood: CheckChest("1463eb0456dedc64dbde09d7d520c26e", "52bdf86b2f7e8ca4bac3493d892412b4", "98f7c7b64ded22e4f9eef57588189b34"); break;
                case SplitChest.HiddenStashLocked: CheckChest("47546b2b5545cec408c899e216de7a56"); break;
                case SplitChest.HowlingMazeWood: CheckChest("0bcc34d9ad0c91d449b4ec216af8b29b"); break;
                case SplitChest.HowlingMazeNormal: CheckChest("b68eacc6e1c046b4ab20979dea17e739"); break;
                case SplitChest.HowlingMazeLocked: CheckChest("c91cb7a3da36fbc45b7782e5cc792cb6"); break;
                case SplitChest.KingLionardoTrialNormal: CheckChest("b4df01a6c473bca4abc1ac55b035c097", "8fdfd5e4034240845af98f4983ee2d69"); break;
                case SplitChest.KingLionardoTrialRed: CheckChest("85bd28267adeeb64ba8e89d102e6b063"); break;
                case SplitChest.KingSigilNormal: CheckChest("c63431d4f98d5644ea7b41ebe53f6969", "ec1200ce45611b54aaeed39661104ebf"); break;
                case SplitChest.KingWoofhauserTrialWood: CheckChest("fddbc59c18fdb1343b1c23b84700320a"); break;
                case SplitChest.KingWoofhauserTrialNormal: CheckChest("4dbdf68dec0b6c345bbeea88ad1bbf73"); break;
                case SplitChest.KingWoofhauserTrialRed: CheckChest("b63fe8787c7b00944985e5157b1f17dc", "fa14b162e84bf104d97f465a84c29063"); break;
                case SplitChest.KingWoofhauserTrialLocked: CheckChest("4e76777f17f1e7344a466e4102654e4c"); break;
                case SplitChest.KitsTrialNormal: CheckChest("dfe9c3f08ae45cb4dac761894e9df127", "7554f2abd0c00c141b86e7e6750312bf"); break;
                case SplitChest.LonelyCaveRed: CheckChest("2a24d3a5d74e15843a1e1eae29a22799", "b570f022195c97c4d9824117c3127011"); break;
                case SplitChest.LonelyCaveLocked: CheckChest("4d7ad0d1d9b93a14d87f5a58c493eb3c"); break;
                case SplitChest.MountainCaveWood: CheckChest("4bd76ebc31cfb6f4d87388172590e50e"); break;
                case SplitChest.MountainCaveNormal: CheckChest("686aa1d25734f8945abd0f42441a40f7"); break;
                case SplitChest.MountainCaveLocked: CheckChest("0af63589477f82945aa2b877c00644df"); break;
                case SplitChest.NovemRuinsRed: CheckChest("15059423ac280e34c9c4207bd723c348"); break;
                case SplitChest.OctoRuinsRed: CheckChest("9277eb480428d7348844065abebf916c"); break;
                case SplitChest.OverworldWood: CheckChest("a5f97bb3ac46ad94195acc4d27315a45", "c2ba8ab2967fd7a4aa9b496694905b66", "0cb76bf96be5d7041a8c8e3dee1f1c6a", "32e10476d83bfc14e87dbe532982c057"); break;
                case SplitChest.OverworldNormal: CheckChest("168f647f9b55e6f44b4b468a423097ee", "c07ea8ca3d2f17e4e96fe000cc7ffcaa", "ec25c0a6f6c96a248991a9d0d6564c8d", "9704f3b46fb5902468652d3e8bc352ba"); break;
                case SplitChest.OverworldGold:
                    CheckChest("2db77a7f0dfc74f4cbf566322e4c9af7", "1f0f329d03b01784790c66c030e904e8", "33186026b1b8bbe479525b1f734b3156", "6475bf47d06d6ef46a0f2b68fde3d115",
                        "0f7df55f8324e2b478a2801072f55a4f", "cd887538430b0c64bb407c5df18255ae", "006600b4d83b6bf4f9d1b682535cbb18", "32cdc08836889924084e74e34bba50dd",
                        "251831bcce2ecb6459eb20fac1ad911b", "9bb2ed0d13474cf42b64eb70fdf07766", "6d9d8999ecfbb4c49b9a81c19a59e0b5", "1939c80b928b3d149a74619a5f59d04a",
                        "4ab69478de7a44546a35e619258cc84f", "d800d45da6a23ea448ada42a8b95405f", "80e03f1b9e9a19b449975ffc5983e709", "6fad584068801324ca857d8c892ee125");
                    break;
                case SplitChest.PathToLupusTombWood: CheckChest("bf83c3b2b2a51bb40aa5cc4813037ab9"); break;
                case SplitChest.PathToLupusTombNormal: CheckChest("d99133cee5351db47802d3887a5fb604"); break;
                case SplitChest.PathToLupusTombLocked: CheckChest("61bd67d2c8ae93e48b65df222d28f159"); break;
                case SplitChest.PawasisCaveWood: CheckChest("580eeb74dc6f63a4aa29816a90f21675"); break;
                case SplitChest.PawasisCaveLocked: CheckChest("3915629c808a3ed42aa80d7470e28aea"); break;
                case SplitChest.PawfulCaveWood: CheckChest("f0aee4feedf337843b849fb388071edf"); break;
                case SplitChest.PawfulCaveNormal: CheckChest("0a0fa10e774447a4dadf2407b67f9498", "03f70b5e899ff244c820eee9b9632eab"); break;
                case SplitChest.PawfulCaveLocked: CheckChest("7fd103dff9394dd438c90ca85628ef02"); break;
                case SplitChest.PawreignCaveNormal: CheckChest("3fcd0888ff22bf746af78443530bcdac"); break;
                case SplitChest.PawreignCaveLocked: CheckChest("e09ce3f99f6daa741af064d79dc93da3"); break;
                case SplitChest.PawsCaveNormal: CheckChest("d91c0e77f21ddc748a44f2b8f979e150"); break;
                case SplitChest.PawsCaveLocked: CheckChest("7dd459dda4ffaae4f9bee921ab1c34ed"); break;
                case SplitChest.PawtCaveWood: CheckChest("041598a8d17cc964fb73627d449e669d", "b773e8438adf320458ba81522f866e9e", "e12a101e1ad355b49be6583209699d6a", "5b66bd0bcaa602141955e1b3e4a8c580", "42f01fe3882f35747a59028eb08fe269", "c084966449a1b824997e065bce4867e6", "833989369d5834a4886c251a9dfc48c7"); break;
                case SplitChest.PawtCaveNormal: CheckChest("337f249c57fc997499df73dfe58f69aa"); break;
                case SplitChest.PawtCaveLocked: CheckChest("242f74b26e551794d8849afcb522e3da"); break;
                case SplitChest.PuggerMazeWood: CheckChest("fae43e290fb504e4394c07227a9f0c4c", "70343f01c43f336489a6ed2f87a8bda6", "aec9bc05f16a2314fb17130b1449a641", "ba49a9a18a154a146a77ad900da0ea74", "bdfc92347fb22df4b82004fab1808bce"); break;
                case SplitChest.PuggerMazeRed: CheckChest("1b155456a3dde554fba2865ce82fa01a"); break;
                case SplitChest.PurrcludedCaveWood: CheckChest("7e36399f52661e742b33cf0cd24dffb6"); break;
                case SplitChest.PurrcludedCaveNormal: CheckChest("5842ebc0f1d07344f819535af273dc27"); break;
                case SplitChest.PurrcludedCaveRed: CheckChest("b943ac934fd55ba4a9b3f3f87e11e50c"); break;
                case SplitChest.PurrcludedCaveLocked: CheckChest("dde373c035825d24a9f2823def041a2b"); break;
                case SplitChest.PurrnCaveWood: CheckChest("8b7d0b8df5e45b047a2eda35c2cb641c"); break;
                case SplitChest.PurrnCaveNormal: CheckChest("6b2a87d7d580c5e4b88dec8d699d453f"); break;
                case SplitChest.PurrnCaveRed: CheckChest("18d629a7f1100ed468756fe6985873f2"); break;
                case SplitChest.PurrnCaveLocked: CheckChest("0b898adb599fcfa4f852b88b8cce43f2"); break;
                case SplitChest.PussCaveWood: CheckChest("283881cfffb372745a319ed840e30650"); break;
                case SplitChest.PussCaveNormal: CheckChest("81a2b57ca1b34fd4a87837f50c27cd29", "5c703b352a282524799a21a532dc3f5a"); break;
                case SplitChest.PussCaveLocked: CheckChest("c32ccfce02d41924e9115d9fff8afa7b"); break;
                case SplitChest.QuadecimRuinsRed: CheckChest("b7af8be1191e81d4ea10bbf75f3e5ee4"); break;
                case SplitChest.QuattorRuinsRed: CheckChest("a9a05ed84fcf8814d961a1e9fc5327b4"); break;
                case SplitChest.QuindecimRuinsRed: CheckChest("563d96b12b6fa944e8d9ea16e9d585c1"); break;
                case SplitChest.QuinqueRuinsRed: CheckChest("ef4af4a22f150364d83b136f1531e365"); break;
                case SplitChest.RiverHoleNormal: CheckChest("acecd3d57514bad42a0da7473d6dcbf2"); break;
                case SplitChest.RiverHoleLocked: CheckChest("2ed9560b288e71446ad728c1be4fdc53"); break;
                case SplitChest.RiversideCoveWood: CheckChest("ad702f6c47f39c94f9c7b5de181675c5", "ae4261ff09467f847916d032792ae52a"); break;
                case SplitChest.RiversideCoveNormal: CheckChest("3239845313d27804ca2eaf12ce68cfd6"); break;
                case SplitChest.RiversideCoveRed: CheckChest("faad3af1941071445948012f9d175b7c"); break;
                case SplitChest.RiversideCoveLocked: CheckChest("067f770ae4a62de4097bb45b6b475a2c"); break;
                case SplitChest.RoverdoseMazeNormal: CheckChest("7916f371cb03df24697ee7584dde61e0"); break;
                case SplitChest.RoverdoseMazeLocked: CheckChest("f435ed3b8cf71cb48834c2b109df10a7"); break;
                case SplitChest.RuffCoveWood: CheckChest("9598a1705f14d744f94fd55277130a70"); break;
                case SplitChest.RuffCoveNormal: CheckChest("076f5fba1049fbd41a2177f1e83c1c75", "9e908ce23a7df1743982a674510672fe"); break;
                case SplitChest.RuffCoveLocked: CheckChest("f5a94893dff0d8a438b77f589fdeb728"); break;
                case SplitChest.SandyPitWood: CheckChest("ed0114fc607babb419856f7d8104addb"); break;
                case SplitChest.SandyPitNormal: CheckChest("4d15f00c922025740ac7bd82f26e7e40"); break;
                case SplitChest.SandyPitLocked: CheckChest("dcd6cc056c4c10040b68140c4b6c7927"); break;
                case SplitChest.SaximRuinsRed: CheckChest("e81c2ef381c5a4341bde3282a204b036"); break;
                case SplitChest.SeasideCoveWood: CheckChest("befac82108769ce468a5b31018167604"); break;
                case SplitChest.SeasideCoveNormal: CheckChest("326206b46b413614dbc436f5e5f58606", "05e27c72b5d7ded4eb6123728f595f63"); break;
                case SplitChest.SedecimRuinsRed: CheckChest("d559d7ef5e2530048bec8eed363fc6a9"); break;
                case SplitChest.SeptemRuinsRed: CheckChest("d098e64e1d7eb10449ba72c0407d443d"); break;
                case SplitChest.SeptencinRuinsRed: CheckChest("60077b3cc753273488bae4c0f95ca698"); break;
                case SplitChest.SeptencinRuinsLocked: CheckChest("4b146bf11e9400e4988f8d5ff6f14663"); break;
                case SplitChest.TerrierfyingTombWood: CheckChest("4a9484932242155449e1815f66fa2548"); break;
                case SplitChest.TerrierfyingTombNormal: CheckChest("980b77b0669d8694cb2be2417e62171d", "245b8fe93ee530a459715f7703fc84fb"); break;
                case SplitChest.TerrierfyingTombLocked: CheckChest("6d5b809c430c79c4380697569de7e4d0"); break;
                case SplitChest.TombOfTheFollowerNormal: CheckChest("15208abe219b2624bb2c17c2e43dbb24"); break;
                case SplitChest.TombOfTheFollowerLocked: CheckChest("0229ebe536e1dac45970c41df387ec41"); break;
                case SplitChest.TombOfTheWolfRed: CheckChest("bee79b0a2b6895149892d5afa4dff8b8", "643e7cc2b48aa2b4cb0ede540bea1da4"); break;
                case SplitChest.TombOfTheWolfLocked: CheckChest("35cc9b6ee5ac6d24ea45fa1c24f3d884"); break;
                case SplitChest.TombstoneCaveRed: CheckChest("13bd744975bf541418358e7b65b4a03a"); break;
                case SplitChest.TombstoneCaveLocked: CheckChest("20587626fd053f84687496f9372bbdee"); break;
                case SplitChest.TresRuinsRed: CheckChest("b8603737fa3072a4fb6515d2009815b7"); break;
                case SplitChest.TrigintaRuinsRed: CheckChest("308749d1335eeaf409e5427c7175f876"); break;
                case SplitChest.TrollCaveGold: CheckChest("1a7dfe5372385a542b56c9475361306c", "5809a7e2b47f48e4e8bbb093c7057a44", "85011c24351cdb1499c004bda909653c"); break;
                case SplitChest.UltimuttStashWood: CheckChest("235eaa45b8b764b4d800982c1ffa7362", "487a0fd8f442af0418f6e6c8f4ea8b9a", "84968bf22989e6046a3318424865420e", "d6919900c2f502d4381ee6a4cb6775bd"); break;
                case SplitChest.UndecimRuinsRed: CheckChest("c60921c05dc8c2243a6675025de7ca38"); break;
                case SplitChest.UndevintiRuinsRed: CheckChest("0e1ba6c5151192c4da55caea47b2e753"); break;
                case SplitChest.UnusRuinsRed: CheckChest("98c475d02e995524ab9ed686e4d2ca46"); break;
                case SplitChest.WhiskCoveNormal: CheckChest("124537d805f6b724ebecf02a8d08ecdf"); break;
                case SplitChest.WhiskCoveLocked: CheckChest("d872decf091ebdd4d8df0bd4e28c783b"); break;
                case SplitChest.WindingCoveWood: CheckChest("abbd36fb56aff8e43ba948cc48d17c0e"); break;
                case SplitChest.WindingCoveNormal: CheckChest("32fb8cf774f07db4083b6d74c8b121f9"); break;
                case SplitChest.WindingCoveLocked: CheckChest("b51c97d398d4d564f9a64f72a9e63906"); break;
                case SplitChest.WyvernNestWood: CheckChest("252c93b7ef9a8184b87d66a7db5b9cb6", "20c52ac0644a2b543b653e678c282727"); break;
                case SplitChest.WyvernNestRed: CheckChest("b8628b497811b134e87daf946687d11b"); break;
                case SplitChest.WyvernNestLocked: CheckChest("4eaf2daa42eeef14cbdf3e7d555d8858"); break;
            }
        }
        private void CheckChest(params string[] guids) {
            bool value = false;
            for (int i = 0; i < guids.Length; i++) {
                if (Memory.HasChest(guids[i])) {
                    value = true;
                    break;
                }
            }
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
                case SplitSpell.Astropaw: CheckSpell("1a5d253e686ea2e4a95e204586a4015a"); break;
                case SplitSpell.Cattrap: CheckSpell("7f1c726a50e26064695ab2032dfb86ac"); break;
                case SplitSpell.Celestail: CheckSpell("e3be896b95e4db7429a64a7ab28fe173"); break;
                case SplitSpell.Flamepurr: CheckSpell("f40361ad748bef44e8df76b76af05a80"); break;
                case SplitSpell.ForceWoofer: CheckSpell("e5fc04dd6f6f7ac4daab9f80d3ee91ff"); break;
                case SplitSpell.Freezepaw: CheckSpell("347e2dd12ccbd254a9bdc587cee9daa5"); break;
                case SplitSpell.FurryShield: CheckSpell("3357e1ce56e242a40a9d37bb37d02b7a"); break;
                case SplitSpell.Graviruff: CheckSpell("5dbdacdb41d8fcd41bc8977b301750aa"); break;
                case SplitSpell.HealPaw: CheckSpell("204b183d154bcbf4fa8f5fc1662fbb1c"); break;
                case SplitSpell.Lightnyan: CheckSpell("825bafad02550144bb83fb1f836e6403"); break;
                case SplitSpell.Manapaw: CheckSpell("68e1f2f9c179423458d5661b61eea42b"); break;
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
                case SplitKey.ArcaneHeadpawters: CheckKey("5a40932a9be236f46882840732c6ee25"); break;
                case SplitKey.DrakothsFirstKey: CheckKey("bb93da0e6149494418ab1530af1213ee"); break;
                case SplitKey.DrakothsSecondKey: CheckKey("2006b5dc3956cf441adedc0ff605d742"); break;
                case SplitKey.GoldenKey: CheckKey("45d4223829520402db18086c5cbe2338"); break;
                case SplitKey.HiddenCave: CheckKey("7b4a483bcde12ba47abb49d255df3ed5"); break;
                case SplitKey.HiddenStash: CheckKey("1fd8a888481269c469fcdf60e398d2a9"); break;
                case SplitKey.HottoDoggo: CheckKey("e4cbedfd820c2464b804015fd69e222c"); break;
                case SplitKey.KeyOfMercy: CheckKey("436215c03fe251d4faf0d8cc720b2e09"); break;
                case SplitKey.KeyOfValor: CheckKey("488cac877060b6a43b70ddb41f792cfc"); break;
                case SplitKey.KeyOfVirtue: CheckKey("c1f0c66ff5f951844b4ed02bcc335d0a"); break;
                case SplitKey.KingFelingardsTomb: CheckKey("3454e751e15750a4ab25deb153c0e8ff"); break;
                case SplitKey.KingLionardoTrial: CheckKey("18c38d642116c05448a4d3cdbb341519"); break;
                case SplitKey.KingWoofhausersTrial: CheckKey("bcaa342a1af3dba49adeb24e09b7cd03"); break;
                case SplitKey.KingWoofhausersTrialKey1: CheckKey("dfe82a302eb1f6849bf8b651fb0b1338"); break;
                case SplitKey.KingWoofhausersTrialKey2: CheckKey("e4e54b60b7401034987e3bacf2ea8c7a"); break;
                case SplitKey.KingWoofhausersTrialKey3: CheckKey("7817996717709b249a92377ab6b5e4c3"); break;
                case SplitKey.Kingsmarker2: CheckKey("8999191aaf53a437ba2c892d404343de"); break;
                case SplitKey.Kingsmarker3: CheckKey("31d65870769db4d43b71a47497f5e15d"); break;
                case SplitKey.Kingsmarker4: CheckKey("82f6db3dc8b224a2996051e6f841d0b7"); break;
                case SplitKey.Kingsmarker5: CheckKey("44b9b72018187504294296055d3659db"); break;
                case SplitKey.Kingsmarker6: CheckKey("6a7144106fbc96244acb96f841c1571d"); break;
                case SplitKey.Kingsmarker7: CheckKey("7aa28cf0bacd19047825ccf658f7ed55"); break;
                case SplitKey.Kingsmarker8: CheckKey("18fd323e03c2eee4383ad74711b6ef82"); break;
                case SplitKey.KitCat: CheckKey("d861b9836ddc3304480ba17284dfda5c"); break;
                case SplitKey.FirstBridge: CheckKey("69c8b1b70a322fd4696bef92065500f9"); break;
                case SplitKey.TrialOfTheFirstKingsKey: CheckKey("79d22c05d24556045b0d9cb6cd69766f"); break;
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
                case SplitQuest.ThisIsIt: CheckQuest("3c292b0f3c18c124e98779a60d7f3b3e", complete); break;
                case SplitQuest.TrackingPurrrivateMewan: CheckQuest("c06e2b12b5e6d274295b4b36c3b5ce8a", complete); break;
                case SplitQuest.TrialOfLionardo: CheckQuest("e123b3ec60d9de94da9fd04e0e6d8551", complete); break;
                case SplitQuest.TrialOfTheFirstKings: CheckQuest("8f2dc042404d89244881a69b06078aa5", complete); break;
                case SplitQuest.TrialOfWoofhauser: CheckQuest("34f8fd72c4c08404aaebd934d1c8418c", complete); break;
                case SplitQuest.TrueWeapon: CheckQuest("a54d620348c1b4249bcb6f3cc981e6ef", complete); break;
                case SplitQuest.Tutorial: CheckQuest("01dcb2d755fd9c345bfa2ac2cfd66788", complete); break;
                case SplitQuest.TwinCaves: CheckQuest("045578b86c7be014bb54e01a71ff813f", complete); break;
                case SplitQuest.TwinCavesAgain: CheckQuest("f129fb1ae760da645bd6a57ed3a63bcf", complete); break;
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
                case SplitDungeon.BatCove: sceneToCheck = "Tomb_batcave"; break;
                case SplitDungeon.BayCave: sceneToCheck = "Cave_baycave"; break;
                case SplitDungeon.BlueCave: sceneToCheck = "Cave_bluecave"; break;
                case SplitDungeon.BraveCave: sceneToCheck = "Cave_bravecave"; break;
                case SplitDungeon.BulletHeaven: sceneToCheck = "Tomb_bulletheaven"; break;
                case SplitDungeon.CactusCave: sceneToCheck = "Tomb_cactuscave"; break;
                case SplitDungeon.CatpitalCave: sceneToCheck = "Cave_catpitalcave"; break;
                case SplitDungeon.CaveGrotto: sceneToCheck = "Cave_cavegrotto"; break;
                case SplitDungeon.CaveMercy: sceneToCheck = "Cave_cavemercy"; break;
                case SplitDungeon.CaveOfTheLion: sceneToCheck = "Cave_lioncave"; break;
                case SplitDungeon.CavePeasy: sceneToCheck = "Cave_cavepeasy"; break;
                case SplitDungeon.CaveValor: sceneToCheck = "Cave_cavevalor"; break;
                case SplitDungeon.CaveVirtue: sceneToCheck = "Cave_cavevirtue"; break;
                case SplitDungeon.CursedRuins: sceneToCheck = "Ruins_Cursed"; break;
                case SplitDungeon.DecemRuins: sceneToCheck = "Ruins_Decem"; break;
                case SplitDungeon.DevigniRuins: sceneToCheck = "Ruins_Devigni"; break;
                case SplitDungeon.DrakothsVault: sceneToCheck = "Ruins_DrakothVault"; break;
                case SplitDungeon.DuodecimRuins: sceneToCheck = "Ruins_Duodecim"; break;
                case SplitDungeon.DuosRuins: sceneToCheck = "Ruins_Duos"; break;
                case SplitDungeon.EmptyHole: sceneToCheck = "Tomb_emptyhole"; break;
                case SplitDungeon.FarfetchedCave: sceneToCheck = "Tomb_farfetchedcave"; break;
                case SplitDungeon.FurrestCave: sceneToCheck = "Cave_furrestcave"; break;
                case SplitDungeon.FurriblePurrison: sceneToCheck = "Cave_furriblecave"; break;
                case SplitDungeon.FursakenCave: sceneToCheck = "Cave_fursakencave"; break;
                case SplitDungeon.HiddenCave: sceneToCheck = "Cave_hiddencave"; break;
                case SplitDungeon.HiddenStash: sceneToCheck = "Tomb_hiddenstash"; break;
                case SplitDungeon.HowlingMaze: sceneToCheck = "Tomb_howlingmaze"; break;
                case SplitDungeon.KingLionardoTrial: sceneToCheck = "Ruins_Lionardo"; break;
                case SplitDungeon.KingSigil: sceneToCheck = "Ruins_KingSigil"; break;
                case SplitDungeon.KingWoofhausersTrial: sceneToCheck = "Ruins_Woofhauser"; break;
                case SplitDungeon.LonelyCave: sceneToCheck = "Cave_lonelycave"; break;
                case SplitDungeon.MountainCave: sceneToCheck = "Cave_mountaincave"; break;
                case SplitDungeon.NovemRuins: sceneToCheck = "Ruins_Novem"; break;
                case SplitDungeon.OctoRuins: sceneToCheck = "Ruins_Octo"; break;
                case SplitDungeon.PathToLupusTomb: sceneToCheck = "Tomb_barkaracave"; break;
                case SplitDungeon.PawasisCave: sceneToCheck = "Tomb_pawasiscave"; break;
                case SplitDungeon.PawfulCave: sceneToCheck = "Tomb_pawfulcave"; break;
                case SplitDungeon.PawreignCave: sceneToCheck = "Cave_pawreigncave"; break;
                case SplitDungeon.PawsCave: sceneToCheck = "Cave_pawscave"; break;
                case SplitDungeon.PawtCave: sceneToCheck = "Cave_pawtcave"; break;
                case SplitDungeon.PuggerMaze: sceneToCheck = "Tomb_puggermaze"; break;
                case SplitDungeon.PurrcludedCave: sceneToCheck = "Cave_purrcludedcave"; break;
                case SplitDungeon.PurrnCave: sceneToCheck = "Cave_purrncave"; break;
                case SplitDungeon.PussCave: sceneToCheck = "Cave_pusscave"; break;
                case SplitDungeon.QuadecimRuins: sceneToCheck = "Ruins_Quadecim"; break;
                case SplitDungeon.QuattorRuins: sceneToCheck = "Ruins_Quattor"; break;
                case SplitDungeon.QuindecimRuins: sceneToCheck = "Ruins_Quindecim"; break;
                case SplitDungeon.QuinqueRuins: sceneToCheck = "Ruins_Quinque"; break;
                case SplitDungeon.RiverHole: sceneToCheck = "Cave_riverhole"; break;
                case SplitDungeon.RiversideCove: sceneToCheck = "Cave_riversidecove"; break;
                case SplitDungeon.RoverdoseMaze: sceneToCheck = "Tomb_roverdosemaze"; break;
                case SplitDungeon.RuffCove: sceneToCheck = "Tomb_ruffcave"; break;
                case SplitDungeon.SandyPit: sceneToCheck = "Tomb_sandypit"; break;
                case SplitDungeon.SaximRuins: sceneToCheck = "Ruins_Saxim"; break;
                case SplitDungeon.SeasideCove: sceneToCheck = "Cave_seasidecove"; break;
                case SplitDungeon.SedecimRuins: sceneToCheck = "Ruins_Sedecim"; break;
                case SplitDungeon.SeptemRuins: sceneToCheck = "Ruins_Septem"; break;
                case SplitDungeon.SeptencinRuins: sceneToCheck = "Ruins_Septencin"; break;
                case SplitDungeon.TerrierfyingTomb: sceneToCheck = "Tomb_terrierfying_tomb"; break;
                case SplitDungeon.TombOfTheFollower: sceneToCheck = "Tomb_cavefollower"; break;
                case SplitDungeon.TombOfTheWolf: sceneToCheck = "Tomb_wolfcave"; break;
                case SplitDungeon.TombstoneCave: sceneToCheck = "Tomb_tombstonecave"; break;
                case SplitDungeon.TresRuins: sceneToCheck = "Ruins_Tres"; break;
                case SplitDungeon.TrigintaRuins: sceneToCheck = "Ruins_Triginta"; break;
                case SplitDungeon.TrollCave: sceneToCheck = "Tomb_trollcave"; break;
                case SplitDungeon.UltimuttStash: sceneToCheck = "Tomb_ultimuttstash"; break;
                case SplitDungeon.UndecimRuins: sceneToCheck = "Ruins_Undecim"; break;
                case SplitDungeon.UndevintiRuins: sceneToCheck = "Ruins_Undevinti"; break;
                case SplitDungeon.UnusRuins: sceneToCheck = "Ruins_Unus"; break;
                case SplitDungeon.WhiskCove: sceneToCheck = "Cave_whiskcove"; break;
                case SplitDungeon.WindingCove: sceneToCheck = "Tomb_windingcave"; break;
                case SplitDungeon.WyvernNest: sceneToCheck = "Tomb_wyvernnest"; break;
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
                case SplitArea.BatCove: CheckScene(enter, "Tomb_batcave"); break;
                case SplitArea.BayCave: CheckScene(enter, "Cave_baycave"); break;
                case SplitArea.BlueCave: CheckScene(enter, "Cave_bluecave"); break;
                case SplitArea.BraveCave: CheckScene(enter, "Cave_bravecave"); break;
                case SplitArea.BulletHeaven: CheckScene(enter, "Tomb_bulletheaven"); break;
                case SplitArea.CactusCave: CheckScene(enter, "Tomb_cactuscave"); break;
                case SplitArea.CatpitalCave: CheckScene(enter, "Cave_catpitalcave"); break;
                case SplitArea.CaveGrotto: CheckScene(enter, "Cave_cavegrotto"); break;
                case SplitArea.CaveMercy: CheckScene(enter, "Cave_cavemercy"); break;
                case SplitArea.CaveOfTheLion: CheckScene(enter, "Cave_lioncave"); break;
                case SplitArea.CavePeasy: CheckScene(enter, "Cave_cavepeasy"); break;
                case SplitArea.CaveValor: CheckScene(enter, "Cave_cavevalor"); break;
                case SplitArea.CaveVirtue: CheckScene(enter, "Cave_cavevirtue"); break;
                case SplitArea.CursedRuins: CheckScene(enter, "Ruins_Cursed"); break;
                case SplitArea.DecemRuins: CheckScene(enter, "Ruins_Decem"); break;
                case SplitArea.DevigniRuins: CheckScene(enter, "Ruins_Devigni"); break;
                case SplitArea.DrakothsFirstKey: CheckScene(enter, "Ruins_DrakothFirstKey"); break;
                case SplitArea.DrakothsSecondKey: CheckScene(enter, "Ruins_DrakothSecondKey"); break;
                case SplitArea.DrakothsVault: CheckScene(enter, "Ruins_DrakothVault"); break;
                case SplitArea.DuodecimRuins: CheckScene(enter, "Ruins_Duodecim"); break;
                case SplitArea.DuosRuins: CheckScene(enter, "Ruins_Duos"); break;
                case SplitArea.EmptyHole: CheckScene(enter, "Tomb_emptyhole"); break;
                case SplitArea.FarfetchedCave: CheckScene(enter, "Tomb_farfetchedcave"); break;
                case SplitArea.FirstBridge: CheckScene(enter, "Ruins_KingStatues"); break;
                case SplitArea.FurrestCave: CheckScene(enter, "Cave_furrestcave"); break;
                case SplitArea.FurriblePurrison: CheckScene(enter, "Cave_furriblecave"); break;
                case SplitArea.FursakenCave: CheckScene(enter, "Cave_fursakencave"); break;
                case SplitArea.HiddenCave: CheckScene(enter, "Cave_hiddencave"); break;
                case SplitArea.HiddenStash: CheckScene(enter, "Tomb_hiddenstash"); break;
                case SplitArea.HowlingMaze: CheckScene(enter, "Tomb_howlingmaze"); break;
                case SplitArea.KingFelingardsTomb: CheckScene(enter, "BlackRuins_Felingard"); break;
                case SplitArea.KingLionardoTrial: CheckScene(enter, "Ruins_Lionardo"); break;
                case SplitArea.KingLupusTomb: CheckScene(enter, "BlackRuins_Lupus"); break;
                case SplitArea.KingSigil: CheckScene(enter, "Ruins_KingSigil"); break;
                case SplitArea.KingWoofhausersTrial: CheckScene(enter, "Ruins_Woofhauser"); break;
                case SplitArea.Kingsmarker: CheckScene(enter, "Ruins_Kingsmarker"); break;
                case SplitArea.KitCat: CheckScene(enter, "Interior_KitCat"); break;
                case SplitArea.KitsTrial: CheckScene(enter, "KitCat_01_Subscene"); break;
                case SplitArea.LonelyCave: CheckScene(enter, "Cave_lonelycave"); break;
                case SplitArea.MountainCave: CheckScene(enter, "Cave_mountaincave"); break;
                case SplitArea.NovemRuins: CheckScene(enter, "Ruins_Novem"); break;
                case SplitArea.OctoRuins: CheckScene(enter, "Ruins_Octo"); break;
                case SplitArea.Overworld: CheckScene(enter, "MainOverworld"); break;
                case SplitArea.PathToLupusTomb: CheckScene(enter, "Tomb_barkaracave"); break;
                case SplitArea.PawasisCave: CheckScene(enter, "Tomb_pawasiscave"); break;
                case SplitArea.PawfulCave: CheckScene(enter, "Tomb_pawfulcave"); break;
                case SplitArea.PawreignCave: CheckScene(enter, "Cave_pawreigncave"); break;
                case SplitArea.PawsCave: CheckScene(enter, "Cave_pawscave"); break;
                case SplitArea.PawtCave: CheckScene(enter, "Cave_pawtcave"); break;
                case SplitArea.PuggerMaze: CheckScene(enter, "Tomb_puggermaze"); break;
                case SplitArea.PurrcludedCave: CheckScene(enter, "Cave_purrcludedcave"); break;
                case SplitArea.PurrnCave: CheckScene(enter, "Cave_purrncave"); break;
                case SplitArea.PussCave: CheckScene(enter, "Cave_pusscave"); break;
                case SplitArea.QuadecimRuins: CheckScene(enter, "Ruins_Quadecim"); break;
                case SplitArea.QuattorRuins: CheckScene(enter, "Ruins_Quattor"); break;
                case SplitArea.QuindecimRuins: CheckScene(enter, "Ruins_Quindecim"); break;
                case SplitArea.QuinqueRuins: CheckScene(enter, "Ruins_Quinque"); break;
                case SplitArea.RiverHole: CheckScene(enter, "Cave_riverhole"); break;
                case SplitArea.RiversideCove: CheckScene(enter, "Cave_riversidecove"); break;
                case SplitArea.RoverdoseMaze: CheckScene(enter, "Tomb_roverdosemaze"); break;
                case SplitArea.RuffCove: CheckScene(enter, "Tomb_ruffcave"); break;
                case SplitArea.SandyPit: CheckScene(enter, "Tomb_sandypit"); break;
                case SplitArea.SaximRuins: CheckScene(enter, "Ruins_Saxim"); break;
                case SplitArea.SeasideCove: CheckScene(enter, "Cave_seasidecove"); break;
                case SplitArea.SedecimRuins: CheckScene(enter, "Ruins_Sedecim"); break;
                case SplitArea.SeptemRuins: CheckScene(enter, "Ruins_Septem"); break;
                case SplitArea.SeptencinRuins: CheckScene(enter, "Ruins_Septencin"); break;
                case SplitArea.TerrierfyingTomb: CheckScene(enter, "Tomb_terrierfying_tomb"); break;
                case SplitArea.TombOfTheFollower: CheckScene(enter, "Tomb_cavefollower"); break;
                case SplitArea.TombOfTheWolf: CheckScene(enter, "Tomb_wolfcave"); break;
                case SplitArea.TombstoneCave: CheckScene(enter, "Tomb_tombstonecave"); break;
                case SplitArea.TresRuins: CheckScene(enter, "Ruins_Tres"); break;
                case SplitArea.TrialOfTheFirstKings: CheckScene(enter, "Ruins_Forgehammer"); break;
                case SplitArea.TrigintaRuins: CheckScene(enter, "Ruins_Triginta"); break;
                case SplitArea.TrollCave: CheckScene(enter, "Tomb_trollcave"); break;
                case SplitArea.UltimuttStash: CheckScene(enter, "Tomb_ultimuttstash"); break;
                case SplitArea.UndecimRuins: CheckScene(enter, "Ruins_Undecim"); break;
                case SplitArea.UndevintiRuins: CheckScene(enter, "Ruins_Undevinti"); break;
                case SplitArea.UnusRuins: CheckScene(enter, "Ruins_Unus"); break;
                case SplitArea.WhiskCove: CheckScene(enter, "Cave_whiskcove"); break;
                case SplitArea.WindingCove: CheckScene(enter, "Tomb_windingcave"); break;
                case SplitArea.WyvernNest: CheckScene(enter, "Tomb_wyvernnest"); break;
                case SplitArea.ZeroDimension: CheckScene(enter, "ZeroDimension", Memory.GameSceneType().ToString()); break;
            }
        }
        private void CheckScene(bool enter, string sceneToCheck, string overrideScene = null) {
            string scene = string.IsNullOrEmpty(overrideScene) ? Memory.SceneName() : overrideScene;
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