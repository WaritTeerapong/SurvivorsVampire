using System;
using System.Collections;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("=== UI Panels ===")]
    public GameObject GameOverPanel;
    public GameObject GameClearPanel;

    public NetworkVariable<float> ReturnToLobbyTimer = new NetworkVariable<float>(3f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (GameOverPanel != null) GameOverPanel.SetActive(false);
        if (GameClearPanel != null) GameClearPanel.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        AudioManager.Instance?.PlayBGM("Gameplay");
        if (IsServer)
        {
            if (PlayerManager.Instance != null)
            {
                PlayerManager.Instance.OnWipeout += HandleWipeout;
            }
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

    private void Update()
    {
        // Debug tool: Fast Forward / Normal Speed (Host only triggers)
        if (IsServer)
        {
#if UNITY_EDITOR
            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                SetTimeScaleRpc(3f);
            }
            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                SetTimeScaleRpc(1f);
            }
#endif
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SetTimeScaleRpc(float timeScale)
    {
        Time.timeScale = timeScale;
        // Debug.Log($"[GameManager] TimeScale synced to {timeScale} across all clients.");
    }

    public void HandleWipeout()
    {
        ShowGameOverClientRpc();
    }

    public void HandleGameClear()
    {
        if (!IsServer) return;
        ShowGameClearClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ShowGameOverClientRpc()
    {
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

    [Rpc(SendTo.Everyone)]
    private void ShowGameClearClientRpc()
    {
        DOVirtual.DelayedCall(1.5f, () =>
        {
            if (GameClearPanel != null)
            {
                GameClearPanel.SetActive(true);
                GameClearPanel.transform.localScale = Vector3.zero;
                GameClearPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
            }

            if (IsServer)
            {
                DOVirtual.DelayedCall(5f, () => StartReTimer());
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
        StopAllCoroutines();
        ReturnToLobbyTimer.Value = 0f;
        ReturnToLobbyRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReturnToLobbyRpc()
    {
        SceneController.Instance
            .NewTransition()
            .Load(Slots.SESSION, Scenes.WAITING_ROOM, setActive: true)
            .Unload(Slots.SESSION)
            .WithClearUnusedAssets()
            .WithOverlay()
            .Perform();
    }

    public void ReturnToMenu()
    {
        NetworkDisconnectHandler.ReturnToMainMenu();
    }
}