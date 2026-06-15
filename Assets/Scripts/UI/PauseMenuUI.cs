using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum PauseUIState { Closed, PauseMenu, SettingMenu, Overlay }

public class PauseMenuUI : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer MainMixer;

    [Header("UI Panels")]
    public GameObject PauseMenuPanel;
    public GameObject SettingsPanel;
    public GameObject OverlayPanel;
    public TMP_Text OverlayText;

    [Header("Pause Menu Buttons")]
    public Button ResumeButton;
    public Button SettingsButton;
    public Button QuitButton;

    [Header("Settings Menu")]
    public Button SettingsBackButton;
    public Slider MasterSlider;
    public Slider BGMSlider;
    public Slider SFXSlider;
    public Slider UISlider;

    private PauseUIState _currentState = PauseUIState.Closed;

    private void Start()
    {
        ChangeState(PauseUIState.Closed);

        if (ResumeButton != null) ResumeButton.onClick.AddListener(ResumeGame);
        if (SettingsButton != null) SettingsButton.onClick.AddListener(OpenSettingsMenu);
        if (QuitButton != null) QuitButton.onClick.AddListener(QuitGame);

        if (SettingsBackButton != null) SettingsBackButton.onClick.AddListener(BackToPauseMenu);

        GetVolumeOnStart();
        AddListenerOnStart();
        SetVolumeOnStart();

        StartCoroutine(WaitForPauseManager());
    }

    private IEnumerator WaitForPauseManager()
    {
        while (PauseManager.Instance == null) yield return null;

        PauseManager.Instance.IsGamePaused.OnValueChanged += HandleNetworkPauseState;
        PauseManager.Instance.PlayersInPause.OnListChanged += HandleNetworkListChanged;
    }

    private void OnDestroy()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.IsGamePaused.OnValueChanged -= HandleNetworkPauseState;
            PauseManager.Instance.PlayersInPause.OnListChanged -= HandleNetworkListChanged;
        }
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            switch (_currentState)
            {
                case PauseUIState.Closed: OpenPauseMenu(); break; // Play -> Pause
                case PauseUIState.Overlay: OpenPauseMenu(); break; // Wait other player -> Pasued ( Settings )
                case PauseUIState.PauseMenu: ResumeGame(); break; // Pause -> Play
                case PauseUIState.SettingMenu: BackToPauseMenu(); break; // Settings -> Back ( Pasued )
            }
        }
    }

    private void ChangeState(PauseUIState newState)
    {
        _currentState = newState;

        PauseMenuPanel.SetActive(_currentState == PauseUIState.PauseMenu);
        SettingsPanel.SetActive(_currentState == PauseUIState.SettingMenu);
        OverlayPanel.SetActive(_currentState == PauseUIState.Overlay);

        if (_currentState == PauseUIState.Overlay)
        {
            OverlayText.text = "Waiting for other player...\n(Press ESC to open menu)";
        }
    }

    public void OpenPauseMenu()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        ChangeState(PauseUIState.PauseMenu);
        PauseManager.Instance.ToggleSettingServerRpc(NetworkManager.Singleton.LocalClientId, true);
    }

    public void ResumeGame()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        ChangeState(PauseUIState.Overlay);
        PauseManager.Instance.ToggleSettingServerRpc(NetworkManager.Singleton.LocalClientId, false);
    }

    public void OpenSettingsMenu() => ChangeState(PauseUIState.SettingMenu);
    public void BackToPauseMenu() => ChangeState(PauseUIState.PauseMenu);

    public void QuitGame()
    {
        ChangeState(PauseUIState.Closed);

        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        // TODO : Load to main menu scene na ja
    }

    private void HandleNetworkPauseState(bool previousValue, bool isPaused)
    {
        if (!isPaused)
        {
            ChangeState(PauseUIState.Closed);
        }
        else
        {
            if (_currentState == PauseUIState.Closed)
            {
                ChangeState(PauseUIState.Overlay);
            }
        }
    }

    private void HandleNetworkListChanged(NetworkListEvent<ulong> changeEvent)
    {
        if (PauseManager.Instance.PlayersInPause.Count == 0 && _currentState == PauseUIState.Overlay)
        {
            ChangeState(PauseUIState.Closed);
        }
    }

    // === AUDIO SETTINGS ===
    #region AUDIO SETTINGS
    private void SetVolumeOnStart()
    {
        SetVolume("MasterVolume", MasterSlider.value);
        SetVolume("BGMVolume", BGMSlider.value);
        SetVolume("SFXVolume", SFXSlider.value);
        SetVolume("UIVolume", UISlider.value);
    }

    private void GetVolumeOnStart()
    {
        MasterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        BGMSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        SFXSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        UISlider.value = PlayerPrefs.GetFloat("UIVolume", 1f);
    }

    private void AddListenerOnStart()
    {
        MasterSlider.onValueChanged.AddListener(val => SetVolume("MasterVolume", val));
        BGMSlider.onValueChanged.AddListener(val => SetVolume("BGMVolume", val));
        SFXSlider.onValueChanged.AddListener(val => SetVolume("SFXVolume", val));
        UISlider.onValueChanged.AddListener(val => SetVolume("UIVolume", val));
    }

    private void SetVolume(string exposedParamName, float sliderValue)
    {
        float db = Mathf.Log10(Mathf.Clamp(sliderValue, 0.0001f, 1f)) * 20f;

        MainMixer.SetFloat(exposedParamName, db);

        PlayerPrefs.SetFloat(exposedParamName, sliderValue);
        PlayerPrefs.Save();
    }
    #endregion
}