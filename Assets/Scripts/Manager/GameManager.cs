using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject GameOverPanel;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (GameOverPanel != null) GameOverPanel.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            PlayerManager.Instance.OnWipeout += HandleWipeout;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsServer && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnWipeout -= HandleWipeout;
        }
    }

    public void HandleWipeout()
    {
        ShowGameOverClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ShowGameOverClientRpc()
    {
        Debug.Log("Game Over Bros!!");
        DOVirtual.DelayedCall(2f, () =>
        {
            if (GameOverPanel != null)
            {
                GameOverPanel.SetActive(true);

                GameOverPanel.transform.localScale = Vector3.zero;
                GameOverPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            }
        });
    }

    public void RestartGame()
    {
        Debug.Log(" Restart Pressed");
        // TODO: Put Restart Logic Here my bro!!
    }

    public void ReturnToMenu()
    {
        Debug.Log("Back To Main Menu");

        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();

        // TODO: Back to main Menu
    }
}