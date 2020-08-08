using System.ComponentModel;
namespace LiveSplit.CatQuest2 {
    public enum SplitType {
        [Description("Manual Split")]
        ManualSplit,
        [Description("Area (Enter)")]
        AreaEnter,
        [Description("Area (Exit)")]
        AreaExit,
        [Description("Chest (Invalid)")]
        Chest,
        [Description("Dungeon (Invalid)")]
        DungeonComplete,
        [Description("Game Start (Invalid)")]
        GameStart,
        [Description("Game End (Invalid)")]
        GameEnd,
        [Description("Key (Invalid)")]
        Key,
        [Description("Level (Invalid)")]
        Level,
        [Description("Quest (Start) (Invalid)")]
        QuestStart,
        [Description("Quest (End) (Invalid)")]
        QuestComplete,
        [Description("Royal Art (Invalid)")]
        RoyalArt,
        [Description("Save Stone (Invalid)")]
        SaveStone,
        [Description("Spell (Invalid)")]
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