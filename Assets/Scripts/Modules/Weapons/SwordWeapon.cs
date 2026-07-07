using UnityEngine;

public class SwordWeapon : MeleeWeapon
{
    [Header("Sword Settings")]
    public GameObject SwordPrefab;

    private GameObject _swordInstance;
    private Sword _swordScript;

    public override void InitializeWeapon()
    {
        base.InitializeWeapon();


        // Guard clause if Sword Instance already exist
        if (SwordPrefab == null || _swordInstance != null) return;

        // Instantiate and set up the sword hierarchy
        _swordInstance = Instantiate(SwordPrefab, transform.position, Quaternion.identity);

        // Move to session scene first to ensure correct scene placement
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(_swordInstance, gameObject.scene);

        // Parent under the player's Weapons container for correct hierarchy placement
        _swordInstance.transform.SetParent(gameObject.transform, true);

        // Set Layer to Sword Instance
        SetInstanceLayerRecursively(_swordInstance, _inventory.gameObject.layer);

        _swordInstance.SetActive(false);
        _swordScript = _swordInstance.GetComponent<Sword>();
    }

    public override void Attack(Transform target)
    {
        if (WeaponData == null || _swordInstance == null || _swordScript == null) return;

        Vector3 spawnPos = transform.position;
        _swordInstance.transform.position = spawnPos;
        _swordInstance.SetActive(true);

        int totalDamage = GetTotalATKDamage();

        GameObject hitVFX = WeaponData.HitVFXPrefab;

        float playerAtkRange = _playerStats != null ? _playerStats.CurrentStats.Value.ATKRange : 0f;
        float detectorRange = _stat.ATKRange + playerAtkRange;

        _swordScript.Initialize(target, totalDamage, hitVFX, detectorRange, transform);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("PlayerShoot", spawnPos);
        }
    }

    private void OnDestroy()
    {
        if (_swordInstance != null)
        {
            Destroy(_swordInstance);
        }
    }
}
