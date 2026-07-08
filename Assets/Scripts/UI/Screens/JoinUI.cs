using System.Collections;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class JoinUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private TMP_InputField _ipInputField;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _backButton;

    [Header("Connection Settings")]
    [SerializeField] private float _connectionTimeoutSeconds = 10f;

    private const int PORT = 7777;

    private bool _isConnecting = false;
    private Coroutine _timeoutCoroutine;

    // --- Lifecycle (called explicitly by MainMenuManager, not tied to SetActive) ---

    public void Activate()
    {
        _errorText.gameObject.SetActive(true);
        _errorText.text = string.Empty;
        SetButtonsInteractable(true);

        _joinButton.onClick.RemoveAllListeners();
        _joinButton.onClick.AddListener(OnJoinButtonClicked);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public void Deactivate()
    {
        _errorText.gameObject.SetActive(false);
        _joinButton.onClick.RemoveListener(OnJoinButtonClicked);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        CancelConnectionAttempt();
    }

    private void OnDestroy() => CancelConnectionAttempt();

    // --- Join flow ---

    private void OnJoinButtonClicked()
    {
        string ip = _ipInputField.text.Trim();
        if (ip.Split('.').Length != 4 || !IPAddress.TryParse(ip, out IPAddress parsedIp))
        {
            ShowError("Invalid IP Format");
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            ShowError("NetworkManager Transport Unavailable");
            return;
        }

        transport.SetConnectionData(parsedIp.ToString(), PORT);
        StartCoroutine(ConnectRoutine());
    }

    private IEnumerator ConnectRoutine()
    {
        SetButtonsInteractable(false);

        // A previous session's socket may still be releasing after Shutdown() -
        // wait for that before attempting a new connection.
        if (NetworkManager.Singleton != null)
        {
            while (NetworkManager.Singleton.ShutdownInProgress)
                yield return null;
        }

        ShowStatus("Try Connecting Server...");

        _isConnecting = true;
        NetworkManager.Singleton.StartClient();

        // Netcode/UTP doesn't reliably report "server not found" on its own,
        // so we enforce our own timeout.
        _timeoutCoroutine = StartCoroutine(TimeoutRoutine());
    }

    private IEnumerator TimeoutRoutine()
    {
        yield return new WaitForSeconds(_connectionTimeoutSeconds);

        if (_isConnecting)
        {
            NetworkManager.Singleton?.Shutdown();
            EndConnectionAttempt("Server Not Found (Timed Out)");
        }
    }

    // --- Netcode callbacks ---

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer) return;
        if (clientId != NetworkManager.Singleton.LocalClientId) return;

        StopTimeout();
        _isConnecting = false;
        // Buttons stay disabled - we're transitioning scenes away from this panel.

        GameSessionData.PlayerSelections.Clear();
        GameSessionData.SpawnedDummies.Clear();

        SceneController.Instance
            .NewTransition()
            .Unload(Slots.MAIN_MENU)
            .WithOverlay()
            .WithClearUnusedAssets()
            .Perform();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            EndConnectionAttempt("Connection Failed or Timeout");
        }
    }

    // --- Helpers ---

    private void CancelConnectionAttempt()
    {
        StopTimeout();

        if (_isConnecting)
        {
            _isConnecting = false;
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
                NetworkManager.Singleton.Shutdown();
        }
    }

    private void EndConnectionAttempt(string errorMessage)
    {
        StopTimeout();
        _isConnecting = false;
        SetButtonsInteractable(true);
        ShowError(errorMessage);
    }

    private void StopTimeout()
    {
        if (_timeoutCoroutine != null)
        {
            StopCoroutine(_timeoutCoroutine);
            _timeoutCoroutine = null;
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_joinButton != null) _joinButton.interactable = interactable;
        if (_backButton != null) _backButton.interactable = interactable;
    }

    private void ShowError(string message)
    {
        _errorText.text = message;
        _errorText.color = Color.darkRed;
    }

    private void ShowStatus(string message)
    {
        _errorText.text = message;
        _errorText.color = Color.white;
    }
}