using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum PauseUIState { Closed, PauseMenu, SettingMenu, Overlay }

public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    public bool IsLevelUpActive = false;

    [Header("Audio Mixer")]
    public AudioMixer MainMixer;

    [Header("UI Panels")]
    public GameObject PauseMenuPanel;
    public GameObject SettingsPanel;
    public GameObject OverlayPanel;
    public TMP_Text OverlayText;

    [Header("Background Settings")]
    public GameObject BGPanel;
    public CanvasGroup BGCanvasGroup;

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

    [Header("Animation Settings")]
    public float OverlayTextBobAmount = 15f;
    public float OverlayTextBobDuration = 1f;
    public float BGFadeDuration = 0.2f;

    private PauseUIState _currentState = PauseUIState.Closed;
    private bool _isTransitioning = false;
    private Tween _overlayTextTween;
    private Vector3 _overlayTextOriginalPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (OverlayText != null)
        {
            _overlayTextOriginalPos = OverlayText.rectTransform.localPosition;
        }

        // Ensure BG is properly disabled at start
        if (BGPanel != null) BGPanel.SetActive(false);

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
        PauseManager.Instance.PlayersSelectingUpgrade.OnListChanged += HandleNetworkListChanged;
    }

    private void OnDestroy()
    {
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.IsGamePaused.OnValueChanged -= HandleNetworkPauseState;
            PauseManager.Instance.PlayersInPause.OnListChanged -= HandleNetworkListChanged;
            PauseManager.Instance.PlayersSelectingUpgrade.OnListChanged -= HandleNetworkListChanged;
        }
    }

    private void Update()
    {
        if (IsLevelUpActive || _isTransitioning) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            switch (_currentState)
            {
                case PauseUIState.Closed: OpenPauseMenu(); break;
                case PauseUIState.Overlay: OpenPauseMenu(); break;
                case PauseUIState.PauseMenu: ResumeGame(); break;
                case PauseUIState.SettingMenu: BackToPauseMenu(); break;
            }
        }
    }

    private void ChangeState(PauseUIState newState, Action onTransitionComplete = null)
    {
        if (_currentState == newState)
        {
            onTransitionComplete?.Invoke();
            return;
        }

        _isTransitioning = true;
        PauseUIState previousState = _currentState;
        _currentState = newState;

        // === Background Animation Logic ===
        if (newState == PauseUIState.Closed)
        {
            // Fade out when closing everything
            if (BGCanvasGroup != null && BGPanel.activeInHierarchy)
            {
                BGCanvasGroup.DOKill();
                BGCanvasGroup.DOFade(0f, BGFadeDuration).SetUpdate(true).OnComplete(() => BGPanel.SetActive(false));
            }
            else if (BGPanel != null)
            {
                BGPanel.SetActive(false);
            }
        }
        else // Moving to PauseMenu, SettingMenu, or Overlay
        {
            // Only fade in if we were previously completely closed
            if (previousState == PauseUIState.Closed && BGPanel != null)
            {
                BGPanel.SetActive(true);
                if (BGCanvasGroup != null)
                {
                    BGCanvasGroup.DOKill();
                    BGCanvasGroup.alpha = 0f;
                    BGCanvasGroup.DOFade(1f, BGFadeDuration).SetUpdate(true);
                }
            }
        }

        // Callback function to execute after the closing animation finishes
        Action openNewState = () =>
        {
            PauseMenuPanel.SetActive(_currentState == PauseUIState.PauseMenu);
            SettingsPanel.SetActive(_currentState == PauseUIState.SettingMenu);
            OverlayPanel.SetActive(_currentState == PauseUIState.Overlay);

            HandleOverlayText();

            _isTransitioning = false;
            onTransitionComplete?.Invoke();
        };

        // Find the currently active panel to close it smoothly
        GameObject activePanel = GetActivePanel(previousState);

        if (activePanel != null && activePanel.activeInHierarchy && activePanel.TryGetComponent(out PopupUI popup))
        {
            popup.ClosePopup(openNewState);
        }
        else
        {
            if (activePanel != null) activePanel.SetActive(false);
            openNewState();
        }
    }

    private GameObject GetActivePanel(PauseUIState state)
    {
        switch (state)
        {
            case PauseUIState.PauseMenu: return PauseMenuPanel;
            case PauseUIState.SettingMenu: return SettingsPanel;
            case PauseUIState.Overlay: return OverlayPanel;
            default: return null;
        }
    }

    private void HandleOverlayText()
    {
        _overlayTextTween?.Kill();
        if (OverlayText != null)
        {
            OverlayText.rectTransform.localPosition = _overlayTextOriginalPos;
        }

        if (_currentState == PauseUIState.Overlay && PauseManager.Instance != null)
        {
            if (PauseManager.Instance.PlayersSelectingUpgrade.Count > 0)
            {
                OverlayText.text = "Waiting for other player to select upgrade...";
            }
            else
            {
                OverlayText.text = "Waiting for other player...\n(Press ESC to open menu)";
            }

            if (OverlayText != null)
            {
                _overlayTextTween = OverlayText.rectTransform
                    .DOLocalMoveY(_overlayTextOriginalPos.y + OverlayTextBobAmount, OverlayTextBobDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }
    }

    public void ForceCloseMenu()
    {
        if (_currentState != PauseUIState.Closed && _currentState != PauseUIState.Overlay)
        {
            ChangeState(PauseUIState.Closed, () =>
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
                {
                    PauseManager.Instance.ToggleSettingServerRpc(NetworkManager.Singleton.LocalClientId, false);
                }
            });
        }
    }

    public void OpenPauseMenu()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        ChangeState(PauseUIState.PauseMenu, () =>
        {
            PauseManager.Instance.ToggleSettingServerRpc(NetworkManager.Singleton.LocalClientId, true);
        });
    }

    public void ResumeGame()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        ChangeState(PauseUIState.Overlay, () =>
        {
            PauseManager.Instance.ToggleSettingServerRpc(NetworkManager.Singleton.LocalClientId, false);
        });
    }

    public void OpenSettingsMenu() => ChangeState(PauseUIState.SettingMenu);
    public void BackToPauseMenu() => ChangeState(PauseUIState.PauseMenu);

    public void QuitGame()
    {
        ChangeState(PauseUIState.Closed, () =>
        {
            NetworkDisconnectHandler.ReturnToMainMenu();
        });
    }

    private void HandleNetworkPauseState(bool previousValue, bool isPaused)
    {
        if (!isPaused)
        {
            ChangeState(PauseUIState.Closed);
        }
        else
        {
            if (_currentState == PauseUIState.Closed && !IsLevelUpActive)
            {
                ChangeState(PauseUIState.Overlay);
            }
        }
    }

    private void HandleNetworkListChanged(NetworkListEvent<ulong> changeEvent)
    {
        if (PauseManager.Instance.PlayersInPause.Count == 0 &&
            PauseManager.Instance.PlayersSelectingUpgrade.Count == 0 &&
            _currentState == PauseUIState.Overlay)
        {
            ChangeState(PauseUIState.Closed);
        }
        else if (_currentState == PauseUIState.Overlay)
        {
            HandleOverlayText();
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