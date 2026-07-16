using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject SettingsPanel;
    public GameObject MainMenuPanel;
    public GameObject JoinPanel;

    [Header("Join UI")]
    [SerializeField] private JoinUI JoinUIScript;

    private void Start()
    {
        if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
        if (JoinPanel != null) JoinPanel.SetActive(false);

        AudioManager.Instance?.PlayBGM("MainMenu");
    }

    public void OnHostButtonClicked()
    {
        StartCoroutine(StartHostRoutine());
    }

    public void OnJoinButtonClicked()
    {
        if (JoinPanel != null) JoinPanel.SetActive(true);
        if (JoinUIScript != null) JoinUIScript.Activate();

        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
    }

    public void OnCloseJoinButtonClicked()
    {
        if (MainMenuPanel != null) MainMenuPanel.SetActive(true);

        if (JoinPanel != null) JoinPanel.SetActive(false);
        if (JoinUIScript != null) JoinUIScript.Deactivate();
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

    private IEnumerator StartHostRoutine()
    {
        // If a previous Join attempt was just cancelled, NetworkManager.Shutdown()
        // may still be releasing its socket. Starting a new Host/Client before that
        // finishes causes "Server failed to bind" (port still held by the old session).
        if (NetworkManager.Singleton == null) yield break;

        while (NetworkManager.Singleton.ShutdownInProgress)
        {
            yield return null;
        }

        Debug.Log("Starting Host...");
        GameSessionData.PlayerSelections.Clear();
        GameSessionData.SpawnedDummies.Clear();

        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
        NetworkManager.Singleton.SceneManager.PostSynchronizationSceneUnloading = false;

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

}