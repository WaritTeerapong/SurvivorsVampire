using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct SoundData
{
    public string SoundName;
    public AudioClip Clip;

    [Range(0f, 1f)] public float Volume;

    [Header("Pitch Settings")]
    public bool UseRandomPitch;
    [Range(0.1f, 3f)] public float MinPitch;
    [Range(0.1f, 3f)] public float MaxPitch;

    [Header("3D SFX")]
    public float MaxDistance;
}

[CreateAssetMenu(fileName = "AudioDatabase_SO", menuName = "DataSO/AudioDatabase", order = 0)]
public class AudioDatabase_SO : ScriptableObject
{
    public List<SoundData> SFXList;
    public List<SoundData> UIList;
    public List<SoundData> BGMList;

    // Helper for find sound name
    public SoundData? GetSFX(string name)
    {
        int index = SFXList.FindIndex(x => x.SoundName == name);
        if (index != -1) return SFXList[index];
        return null;
    }

    public SoundData? GetUI(string name)
    {
        int index = UIList.FindIndex(x => x.SoundName == name);
        if (index != -1) return UIList[index];
        return null;
    }

    public SoundData? GetBGM(string name)
    {
        int index = BGMList.FindIndex(x => x.SoundName == name);
        if (index != -1) return BGMList[index];
        return null;
    }
}