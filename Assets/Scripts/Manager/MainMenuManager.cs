using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject SettingsPanel;
    public GameObject MainMenuPanel;

    public void OnHostButtonClicked()
    {
        Debug.Log("Starting Host...");
        GameSessionData.PlayerSelections.Clear();
        GameSessionData.SpawnedDummies.Clear();

        NetworkManager.Singleton.StartHost();
        if (SceneController.Instance != null)
        {
            SceneController.Instance
                .NewTransition()
                .Load(Slots.SESSION, Scenes.WAITING_ROOM, setActive: true)
                .Unload(Slots.MAIN_MENU)
                .WithOverlay()
                .WithClearUnusedAssets()
                .Perform();
        }
    }

    public void OnJoinButtonClicked()
    {
        Debug.Log("Starting Client...");
        GameSessionData.PlayerSelections.Clear();
        GameSessionData.SpawnedDummies.Clear();

        NetworkManager.Singleton.StartClient(); 
        SceneController.Instance
            .NewTransition()
            .Unload(Slots.MAIN_MENU)
            .WithOverlay()
            .WithClearUnusedAssets()
            .Perform();
    }

    public void OnSettingsButtonClicked()
    {
        if (SettingsPanel != null) SettingsPanel.SetActive(true);
        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
    }

    public void OnCloseSettingClicked()
    {
        if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}