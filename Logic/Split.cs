using System.ComponentModel;
namespace LiveSplit.CatQuest2 {
    public enum SplitType {
        [Description("Manual Split")]
        ManualSplit,
        [Description("Area (Enter)")]
        AreaEnter,
        [Description("Area (Exit)")]
        AreaExit,
        [Description("Chest")]
        Chest,
        [Description("Dungeon")]
        DungeonComplete,
        [Description("Game Start")]
        GameStart,
        [Description("Game End")]
        GameEnd,
        [Description("Level")]
        Level,
        [Description("Quest (Start)")]
        QuestStart,
        [Description("Quest (End)")]
        QuestComplete,
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