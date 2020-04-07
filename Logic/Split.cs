using System.ComponentModel;
namespace LiveSplit.CatQuest2 {
    public enum SplitType {
        [Description("Manual Split")]
        ManualSplit,
        [Description("Chest")]
        Chest,
        [Description("Dungeon (Enter)")]
        DungeonEnter,
        [Description("Dungeon (Complete)")]
        DungeonComplete,
        [Description("Game Start")]
        GameStart,
        [Description("Game End")]
        GameEnd,
        [Description("Level")]
        Level,
        [Description("Quest")]
        Quest,
        [Description("Royal Art")]
        RoyalArt,
        [Description("Spell")]
        Spell
    }
    public class Split {
        public string Name { get; set; }
        public SplitType Type { get; set; }
        public string Value { get; set; }

        public override string ToString() {
            return $"{Type}|{Value}";
        }
    }
}