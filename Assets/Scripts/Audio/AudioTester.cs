using UnityEngine;
using UnityEngine.InputSystem; // ต้องใช้เพราะเกมคุณใช้ New Input System

public class AudioTester : MonoBehaviour
{
    void Update()
    {
        // กดปุ่มเลข 1 บนคีย์บอร์ด (ด้านบนตัวอักษร)
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("กำลังเล่นเพลง BGM01");
            AudioManager.Instance.PlayBGM("BGM01");
        }

        // กดปุ่มเลข 2 บนคีย์บอร์ด
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("กำลังสลับเพลงเป็น BGM02");
            AudioManager.Instance.PlayBGM("BGM02");
        }

        // (แถมแถม) กดปุ่มเลข 3 เพื่อเทสเสียง SFX 
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            // เปลี่ยนชื่อ "PlayerShoot" เป็นชื่อ SFX ที่คุณตั้งไว้ใน SO
            AudioManager.Instance.PlaySFX("PlayerShoot", transform.position);
        }
    }
}