using UnityEngine;


// Using class to hold data like Scriptable object
// But better Performance and Readability when access data
public static class SceneDatabase
{
    public class Slots
    {
        public const string MAIN_MENU = "MainMenu";
        public const string SESSION = "Session";
        public const string SESSION_CONTENT = "SessionContent";
    }
    
    public class Scenes
    {
        public const string MAIN_MENU = "MainMenuScene";
        public const string WAITING_ROOM = "WaitingRoomScene";
        public const string SESSION = "SessionScene";
        public const string UPGRADE = "UpgradeScene";
    }
}
