using UnityEngine;
using UnityEngine.UI;

public class LeaveRoomUI : MonoBehaviour
{
    [Header("=== UI References ===")]
    public Button LeaveBtn;

    private void Start()
    {
        if (LeaveBtn != null)
        {
            LeaveBtn.onClick.AddListener(OnLeaveButtonClicked);
        }
        else
        {
            // Debug.LogWarning("LeaveBtn reference is missing in the Inspector.", this);
        }
    }

    private void OnDestroy()
    {
        if (LeaveBtn != null)
        {
            LeaveBtn.onClick.RemoveListener(OnLeaveButtonClicked);
        }
    }

    private void OnLeaveButtonClicked()
    {
        // Disable button to prevent spam clicking
        if (LeaveBtn != null) LeaveBtn.interactable = false;

        // Call the graceful disconnect routine which handles both Host and Client
        // and eventually loads the CoreScene for a full state reset.
        NetworkDisconnectHandler.ReturnToMainMenu();
    }
}