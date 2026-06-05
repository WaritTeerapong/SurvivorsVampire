using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class SelectConnectionUI : MonoBehaviour
{
    [SerializeField] private GameObject _connectionCanvas;
    [SerializeField] private Button _hostBtn;
    [SerializeField] private Button _clientBtn;
    [SerializeField] private Button _disconnectBtn;

    private void Start()
    {
        _hostBtn.onClick.AddListener(OnHostClicked);
        _clientBtn.onClick.AddListener(OnClientClicked);
        _disconnectBtn.onClick.AddListener(OnDisconnectClicked);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
        }

        _connectionCanvas.SetActive(true);
        _disconnectBtn.gameObject.SetActive(false);
    }

    public void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        }
    }

    void OnConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            _connectionCanvas.SetActive(false);
            _disconnectBtn.gameObject.SetActive(true);
        }
    }

    void OnDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            _connectionCanvas.SetActive(true);
            _disconnectBtn.gameObject.SetActive(false);
        }
    }

    private void OnHostClicked() => NetworkManager.Singleton.StartHost();

    private void OnClientClicked() => NetworkManager.Singleton.StartClient();

    private void OnDisconnectClicked() => NetworkManager.Singleton.Shutdown();
}