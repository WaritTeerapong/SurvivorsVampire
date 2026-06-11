using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class SelectColorUI : MonoBehaviour
{
    [SerializeField] private GameObject _selectionCanvas;
    [SerializeField] private Button _foxBtn;
    [SerializeField] private Button _ratBtn;

    private void Start()
    {
        _foxBtn.onClick.AddListener(() => OnCharacterSelected(0));
        _ratBtn.onClick.AddListener(() => OnCharacterSelected(1));

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

    private void OnCharacterSelected(int characterIndex)
    {
        _selectionCanvas.SetActive(false);

        ulong myLocalId = NetworkManager.Singleton.LocalClientId;
        PlayerSpawnManager.Instance.RequestSpawnPlayerRpc(characterIndex, myLocalId);
    }

}