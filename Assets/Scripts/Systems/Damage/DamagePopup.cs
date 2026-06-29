using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamagePopup : MonoBehaviour
{
    public float FloatHeight = 1.5f;
    public float Duration = 0.8f;
    public Vector2 RandomOffset = new Vector2(0.5f, 0.5f);

    private TMP_Text _dmgText;

    private void Awake()
    {
        _dmgText = GetComponent<TMP_Text>();
    }

    private void OnDisable()
    {
        transform.DOKill();
        if (_dmgText != null) _dmgText.DOKill();
    }

    public void Setup(int damage, Color color)
    {
        _dmgText.text = damage.ToString();
        _dmgText.color = color;
        _dmgText.alpha = 1f;

        float randomX = Random.Range(-RandomOffset.x, RandomOffset.x);
        float randomY = Random.Range(RandomOffset.y, -RandomOffset.y);
        transform.position += new Vector3(randomX, randomY, 0);

        transform.DOMoveY(transform.position.y + FloatHeight, Duration).SetEase(Ease.OutCirc);

        _dmgText.DOFade(0f, Duration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            if (ObjectPoolManager.Instance != null) ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
            else Destroy(gameObject);
        }
        );
    }
}
