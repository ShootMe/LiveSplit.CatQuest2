using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace LiveSplit.CatQuest2.UI {
    public partial class Manager : Form {
#if Manager
        public static void Main(string[] args) {
            try {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Manager());
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }
#endif
        public MemoryManager Memory { get; set; }
        private bool isRunning;
        public Manager() {
            this.DoubleBuffered = true;
            InitializeComponent();
            Memory = new MemoryManager();
            StartUpdateLoop();
            Text = "Cat Quest 2 v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        }

        private void StartUpdateLoop() {
            if (isRunning) { return; }
            isRunning = true;

            Task.Factory.StartNew(delegate () {
                try {
                    bool lastHooked = false;
                    while (isRunning) {
                        try {
                            bool hooked = Memory.HookProcess();
                            if (hooked) {
                                UpdateValues();
                            }
                            if (lastHooked != hooked) {
                                lastHooked = hooked;
                                this.Invoke((Action)delegate () { lblNote.Visible = !hooked; });
                            }
                        } catch { }
                        Thread.Sleep(10);
                    }
                } catch { }
            }, TaskCreationOptions.LongRunning);
        }
        public void UpdateValues() {
            if (this.InvokeRequired) { this.Invoke((Action)UpdateValues); return; }

            UpdateText<int>(StatsValue.Attack, txtCurrentAttack);
            UpdateText<int>(StatsValue.IncreasedAttackSpeed, txtCurrentAttackSpeed);
            UpdateText<int>(StatsValue.Defence, txtCurrentDefense);
            UpdateText<int>(StatsValue.Health, txtCurrentHealth);
            UpdateText<int>(StatsValue.Magic, txtCurrentMagic);
            UpdateText<int>(StatsValue.Mana, txtCurrentMana);
            UpdateText<float>(StatsValue.MoveSpeed, txtCurrentMoveSpeed);
            UpdateText<float>(StatsValue.RollDistance, txtCurrentRollDistance);

            RoyalArts royalArts = Memory.PlayerRoyalArts();
            chkCurrentWaterWalking.Checked = (royalArts & RoyalArts.WaterWalk) != RoyalArts.None;
            chkCurrentRollAttack.Checked = (royalArts & RoyalArts.RollAttack) != RoyalArts.None;
            chkCurrentPawerSmash.Checked = (royalArts & RoyalArts.RoyalSmash) != RoyalArts.None;

            if (chkAutoApply.Checked) {
                ApplyChanges(royalArts);
            }
        }
        private void ApplyChanges(RoyalArts royalArts = RoyalArts.All) {
            SetValue(StatsValue.Attack, txtAttack, txtCurrentAttack);
            SetValue(StatsValue.IncreasedAttackSpeed, txtAttackSpeed, txtCurrentAttackSpeed);
            SetValue(StatsValue.Defence, txtDefense, txtCurrentDefense);
            SetValue(StatsValue.Health, txtHealth, txtCurrentHealth);
            SetValue(StatsValue.Magic, txtMagic, txtCurrentMagic);
            SetValue(StatsValue.Mana, txtMana, txtCurrentMana);
            SetValue(StatsValue.MoveSpeed, txtMoveSpeed, txtCurrentMoveSpeed);
            SetValue(StatsValue.RollDistance, txtRollDistance, txtCurrentRollDistance);

            if (royalArts == RoyalArts.All) {
                royalArts = Memory.PlayerRoyalArts();
            }
            RoyalArts arts = (chkWaterWalking.Checked ? RoyalArts.WaterWalk : RoyalArts.None) | (chkRollAttack.Checked ? RoyalArts.RollAttack : RoyalArts.None) | (chkPawerSmash.Checked ? RoyalArts.RoyalSmash : RoyalArts.None);
            if (arts != royalArts) {
                Memory.SetPlayerRoyalArts(arts);
            }
        }
        private void UpdateText<T>(StatsValue type, TextBox textBox) where T : unmanaged {
            string statValue = null;
            if (typeof(T) == typeof(float)) {
                statValue = Memory.PlayerStat<float>(type).ToString("0.######");
            } else {
                statValue = Memory.PlayerStat<T>(type).ToString();
            }
            if (textBox.Text != statValue) { textBox.Tag = true; }
            if (textBox.Tag == null) { textBox.Tag = false; }
            textBox.Text = statValue;
        }
        private void SetValue(StatsValue type, TextBox changeTo, TextBox current) {
            float value;
            if (!string.IsNullOrEmpty(changeTo.Text) && current.Text != changeTo.Text && (bool)current.Tag && float.TryParse(changeTo.Text, out value)) {
                Memory.SetPlayerStats(type, value);
                current.Tag = false;
            }
        }
        private void btnApplyChanges_Click(object sender, EventArgs e) {
            ApplyChanges();
        }
    }
}