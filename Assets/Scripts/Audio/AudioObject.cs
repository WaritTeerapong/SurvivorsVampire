using UnityEngine;
using UnityEngine.Audio;

public class AudioObject : MonoBehaviour
{
    private AudioSource _audioSource;
    private float _lifeTimer;
    private bool _isPlaying;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    public void PlaySound(SoundData data, AudioMixerGroup mixerGroup)
    {
        _audioSource.clip = data.Clip;
        _audioSource.volume = data.Volume;
        _audioSource.maxDistance = data.MaxDistance > 0 ? data.MaxDistance : 15f;

        _audioSource.outputAudioMixerGroup = mixerGroup;

        if (data.UseRandomPitch) _audioSource.pitch = Random.Range(data.MinPitch, data.MaxPitch);
        else _audioSource.pitch = 1f;

        _audioSource.Play();

        _lifeTimer = data.Clip.length;
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _lifeTimer -= Time.deltaTime;
        if (_lifeTimer <= 0f)
        {
            _isPlaying = false;
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
    }
}