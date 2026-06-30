public struct Slots
{
    public const string MAIN_MENU = "MainMenu";
    public const string SESSION = "Session"; // for main game scene
    public const string SESSION_CONTENT = "SessionContent"; // for event game scene (upgrade, special event)
}

public struct Scenes
{
    //Main Menu
    public const string MAIN_MENU = "MainMenuScene";
    // Session
    public const string WAITING_ROOM = "WaitingRoomScene";
    public const string SESSION = "SessionScene";
    public const string SESSION_BOB = "SessionScene_Bob_Test";
    // Session Content
    public const string UPGRADE = "UpgradeScene";

    public static string GetSlotForScene(string sceneName)
    {
        switch (sceneName)
        {
            case MAIN_MENU:
                return Slots.MAIN_MENU;
            case WAITING_ROOM:
            case SESSION:
            case SESSION_BOB:
                return Slots.SESSION;
            case UPGRADE:
                return Slots.SESSION_CONTENT;
            default:
                return null;
        }
    }
}



