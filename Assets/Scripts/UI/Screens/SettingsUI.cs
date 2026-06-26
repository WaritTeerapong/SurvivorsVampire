using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio; // สำคัญสำหรับ AudioMixer

public class SettingsUI : MonoBehaviour
{
    [Header("Audio Mixer Reference")]
    public AudioMixer MainMixer; // ลาก AudioMixer ของคุณมาใส่

    [Header("Sliders (Set Min 0.0001, Max 1)")]
    public Slider MasterSlider;
    public Slider BGMSlider;
    public Slider SFXSlider;
    public Slider UISlider;

    [Header("Close Settings")]
    public Button CloseButton; // ปุ่ม X สำหรับปิดหน้าตั้งค่า

    private void Start()
    {
        // 1. โหลดค่าเดิมจากเครื่องผู้เล่น (ถ้าเพิ่งเล่นครั้งแรก จะตั้งค่าเริ่มต้นที่ 1f (ดังสุด))
        MasterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        BGMSlider.value = PlayerPrefs.GetFloat("BGMVolume", 1f);
        SFXSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        UISlider.value = PlayerPrefs.GetFloat("UIVolume", 1f);

        // 2. ดักจับเหตุการณ์ว่า "ถ้ามีการเลื่อนหลอด ให้เรียกฟังก์ชันเปลี่ยนเสียง"
        MasterSlider.onValueChanged.AddListener(SetMasterVolume);
        BGMSlider.onValueChanged.AddListener(SetBGMVolume);
        SFXSlider.onValueChanged.AddListener(SetSFXVolume);
        UISlider.onValueChanged.AddListener(SetUIVolume);

        // 3. ผูกปุ่มปิดหน้าต่าง
        if (CloseButton != null)
        {
            CloseButton.onClick.AddListener(CloseSettings);
        }

        // 4. บังคับให้เสียงทำงานตามค่าที่เพิ่งโหลดมาทันทีตอนเริ่มเกม
        ApplyAllVolumes();
    }

    // --- ฟังก์ชันปรับเสียง และ เซฟลง PlayerPrefs ทันที ---
    private void SetMasterVolume(float value)
    {
        // ใช้สมการ Log10 เปลี่ยนค่า 0-1 ให้เป็นเดซิเบล (-80 ถึง 0) สำหรับ Mixer
        MainMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    private void SetBGMVolume(float value)
    {
        MainMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    private void SetSFXVolume(float value)
    {
        MainMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void SetUIVolume(float value)
    {
        MainMixer.SetFloat("UIVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f);
        PlayerPrefs.SetFloat("UIVolume", value);
    }

    private void ApplyAllVolumes()
    {
        SetMasterVolume(MasterSlider.value);
        SetBGMVolume(BGMSlider.value);
        SetSFXVolume(SFXSlider.value);
        SetUIVolume(UISlider.value);
    }

    private void CloseSettings()
    {
        PlayerPrefs.Save(); // สั่งบันทึกไฟล์ลงเครื่องแบบถาวร
    }
}