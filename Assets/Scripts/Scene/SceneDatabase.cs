using UnityEngine;


// Using class to hold data like Scriptable object
// But better Performance and Readability when access data
public static class SceneDatabase
{
    public class Slots
    {
        public const string MAIN_MENU = "MainMenu";
        public const string SESSION = "Session";                // for main game scene
        public const string SESSION_CONTENT = "SessionContent"; // for event game scene (upgrade, special event)
    }
    
    public class Scenes
    {
        //Main Menu
        public const string MAIN_MENU = "MainMenuScene";
        // Session
        public const string WAITING_ROOM = "WaitingRoomScene";
        public const string SESSION = "SessionScene";
        // Session Content
        public const string UPGRADE = "UpgradeScene";
    }

    // TODO: Add static string GetSlotForScene(string sceneName) helper to map Scenes to Slots.
}
