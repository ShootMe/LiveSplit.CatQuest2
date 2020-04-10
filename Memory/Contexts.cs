namespace LiveSplit.CatQuest2 {
    public enum Context {
        Audio = 0x8,
        Combat = 0xc,
        Config = 0x10,
        Framework = 0x14,
        Game = 0x18,
        GameState = 0x1c,
        GUI = 0x20,
        Input = 0x24,
        Quest = 0x28,
        WorldGrid = 0x2c
    }
    public enum GameStateContext {
        Checkpoint = 0,
        ChestID = 1,
        ChestsCollected = 2,
        DungeonsCleared = 6,
        Equipment = 10,
        EquipmentItemData = 11,
        EquipmentList = 12,
        Experience = 20,
        GameOver = 26,
        Gold = 31,
        Inventory = 34,
        KeyID = 38,
        Level = 39,
        IsObtained = 45,
        RoyalArts = 56,
        SavedGame = 57,
        SpellData = 59,
        SpellsAttainedList = 60
    }
    public enum FrameworkContext {
        SceneCommand = 12,
        SceneManagementService = 13,
        SceneTransition = 14,
        SceneType = 15,
        TimeService = 16,
        TransitionEnterEnded = 17,
        TransitionEnterStarted = 19,
        TransitionExitEnded = 21,
        TransitionExitStarted = 23
    }
    public enum QuestContext {
        FinalQuestCompleted = 0,
        MainQuest = 1,
        QuestCompleted = 3,
        QuestController = 5,
        QuestID = 9,
        QuestParent = 10,
        QuestStarted = 11,
        QuestState = 12,
        SideQuest = 14,
        SubQuest = 15
    }
}