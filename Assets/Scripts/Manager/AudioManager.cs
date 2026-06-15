using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioDatabase_SO AudioDatabase;
    public GameObject AudioObjectPrefab;

    public AudioSource BGMSource;
    public AudioSource UISource;

    public AudioMixerGroup SFXGroup;
    public AudioMixerGroup BGMGroup;
    public AudioMixerGroup UIGroup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (BGMSource != null)
        {
            BGMSource.spatialBlend = 0f;
            if (BGMGroup != null) BGMSource.outputAudioMixerGroup = BGMGroup;
        }
        if (UISource != null)
        {
            UISource.spatialBlend = 0f;
            if (UIGroup != null) UISource.outputAudioMixerGroup = UIGroup;
        }

    }

    public void PlaySFX(string soundName, Vector3 position)
    {
        if (AudioDatabase == null) return;

        SoundData? data = AudioDatabase.GetSFX(soundName);
        if (data.HasValue)
        {
            GameObject auidoObj = ObjectPoolManager.Instance.SpawnObject(AudioObjectPrefab, position, Quaternion.identity, PoolCategory.Audio);

            if (auidoObj != null)
            {
                AudioObject aObj = auidoObj.GetComponent<AudioObject>();
                aObj.PlaySound(data.Value, SFXGroup);
            }
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Mai Found Audio {soundName} !!");
        }
    }

    public void PlayUI(string soundName)
    {
        if (AudioDatabase == null || UISource == null) return;

        SoundData? data = AudioDatabase.GetUI(soundName);
        if (data.HasValue) UISource.PlayOneShot(data.Value.Clip, data.Value.Volume);
    }

    public void PlayBGM(string soundName, float fadeDuration = 1.5f)
    {
        if (AudioDatabase == null || BGMSource == null) return;

        SoundData? data = AudioDatabase.GetBGM(soundName);
        if (data.HasValue)
        {
            if (BGMSource.isPlaying)
            {
                BGMSource.DOFade(0f, fadeDuration / 2f).OnComplete(() => ChangeBGMAndFadeIn(data.Value, fadeDuration / 2f));
            }
            else
            {
                ChangeBGMAndFadeIn(data.Value, fadeDuration);
            }
        }
    }

    private void ChangeBGMAndFadeIn(SoundData data, float fadeDuration)
    {
        BGMSource.clip = data.Clip;
        BGMSource.volume = 0f;
        BGMSource.Play();
        BGMSource.DOFade(data.Volume, fadeDuration);
    }
}