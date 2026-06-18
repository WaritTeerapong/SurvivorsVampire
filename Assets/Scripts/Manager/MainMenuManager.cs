using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject SettingsPanel; // ลากหน้าต่าง Settings มาใส่
    public GameObject MainMenuPanel;

    public void OnHostButtonClicked()
    {
        Debug.Log("Starting Host...");
        // 1. สั่งเปิดห้อง
        NetworkManager.Singleton.StartHost();

        // 2. สั่งโหลดฉาก (ต้องใช้ SceneManager ของ Netcode เพื่อให้ทุกคนในห้องโหลดตาม)
        NetworkManager.Singleton.SceneManager.LoadScene("Bob_Test_Scene", LoadSceneMode.Single);
    }

    public void OnJoinButtonClicked()
    {
        Debug.Log("Starting Client...");
        // 1. สั่ง Join (เมื่อต่อสำเร็จ Netcode จะดูดเราไปหน้า Gameplay ตาม Host ทันที)
        NetworkManager.Singleton.StartClient();
    }

    public void OnSettingsButtonClicked()
    {
        if (SettingsPanel != null) SettingsPanel.SetActive(true);
        if (MainMenuPanel != null) MainMenuPanel.SetActive(false);
    }

    public void OnCloseSettingClicked()
    {
        if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
    }

    public void OnQuitButtonClicked()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

        // บรรทัดนี้ช่วยให้หยุดรันในหน้า Editor ได้ด้วย
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}