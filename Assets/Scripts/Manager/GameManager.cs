using System;
using System.Collections;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject GameOverPanel;

    public NetworkVariable<float> ReturnToLobbyTimer = new NetworkVariable<float>(3f);

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

    public void StartReTimer()
    {
        if (!IsServer) return;
        StartCoroutine(StartReToLobbyTimer());
    }

    private IEnumerator StartReToLobbyTimer()
    {
        ReturnToLobbyTimer.Value = 3f;

        while (ReturnToLobbyTimer.Value > 0)
        {
            ReturnToLobbyTimer.Value -= Time.deltaTime;
            yield return null;
        }

        ReturnToLobbyTimer.Value = 0f;

        yield return new WaitForSeconds(1f);

        RequestReturnToLobby();
    }

    public void RequestReturnToLobby()
    {
        Debug.Log("[Game Manager] Return to Lobby!!!");

        StopAllCoroutines();

        ReturnToLobbyTimer.Value = 0f;

        ReturnToLobbyRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReturnToLobbyRpc()
    {
        // NETWORK OPERATION: Initiating transition to the Waiting Room scene on the server
        SceneController.Instance.NewTransition()
            .Load(Slots.SESSION, Scenes.WAITING_ROOM, setActive: true)
            .WithOverlay()
            .Perform();
    }

    public void ReturnToMenu()
    {
        Debug.Log("Back To Main Menu");
        NetworkDisconnectHandler.ReturnToMainMenu();
    }
}