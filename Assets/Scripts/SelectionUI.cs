using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class SelectionUI : MonoBehaviour
{
    [SerializeField] private GameObject _selectionCanvas;
    [SerializeField] private Button _redBtn;
    [SerializeField] private Button _blueBtn;
    [SerializeField] private Button _greenBtn;

    private void Start()
    {
        _redBtn.onClick.AddListener(() => OnColorSelected(0));
        _blueBtn.onClick.AddListener(() => OnColorSelected(1));
        _greenBtn.onClick.AddListener(() => OnColorSelected(2));

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
        }

        _selectionCanvas.SetActive(false);
    }

    void OnDestroy()
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
            _selectionCanvas.SetActive(true);
        }
    }

    private void OnColorSelected(int colorIndex)
    {
        _selectionCanvas.SetActive(false);

        ulong myLocalId = NetworkManager.Singleton.LocalClientId;
        PlayerSpawnManager.Instance.RequestSpawnPlayerRpc(colorIndex, myLocalId);
    }

}