using System;
namespace LiveSplit.CatQuest2 {
    [Flags]
    public enum RoyalArts {
        None = 0,
        RollAttack = 1,
        WaterWalk = 2,
        RoyalSmash = 4,
        SpellSlot = 8,
        All = 15
    }
}