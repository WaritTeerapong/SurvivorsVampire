using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class SelectConnectionUI : MonoBehaviour
{
    [SerializeField] private GameObject _connectionCanvas;
    [SerializeField] private Button _hostBtn;
    [SerializeField] private Button _clientBtn;

    private void Start()
    {
        _hostBtn.onClick.AddListener(OnHostClicked);
        _clientBtn.onClick.AddListener(OnClientClicked);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
        }

        _connectionCanvas.SetActive(true);
    }

    public void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
        }
    }

    void OnConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            _connectionCanvas.SetActive(false);
        }
    }

    private void OnHostClicked()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void OnClientClicked()
    {
        NetworkManager.Singleton.StartClient();
    }
}