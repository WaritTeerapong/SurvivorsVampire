using UnityEngine;
using UnityEngine.UI;

public class ButtonSFXUI : MonoBehaviour
{
    private Button _btn;

    void Awake()
    {
        _btn = GetComponent<Button>();
    }

    void Start()
    {
        _btn.onClick.RemoveListener(PlaySFXOnClick);
        _btn.onClick.AddListener(PlaySFXOnClick);
    }

    void OnDisable()
    {
        _btn.onClick.RemoveListener(PlaySFXOnClick);
    }

    private void PlaySFXOnClick()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUI("Click");
        }
    }
}
