using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] private PlayerBullet bullet;
    [SerializeField] private Vector2 bulletSpawnOffset = new Vector2(0f, 0.5f);

    [Header("Colors")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color noAmmoColor = Color.lightGray;
    [SerializeField] private Color hasAmmoColor= Color.white;

    public bool HasAmmo { get; private set; }

    private EnemyHP enemy;

    void Awake()
    {
        if (sprite == null)
            sprite = GetComponent<SpriteRenderer>();
        UpdateColor();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemy = FindAnyObjectByType<EnemyHP>();
    }

    // Update is called once per frame
    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !HasAmmo)
            return;

        if (kb.spaceKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame)
            Shoot();
    }

    public bool TryCollectAmmo()
    {
        if (HasAmmo)
            return false;

        HasAmmo = true;
        UpdateColor();
        return true;
    }

    private void Shoot()
    {
        if (bullet == null)
        {
            Debug.LogError("PlayerShoot prefab slot is empty", this);
            return;
        }

        Vector3 spawnPos = transform.position + (Vector3)bulletSpawnOffset;
        Quaternion rotation = Quaternion.identity; 

        if (enemy != null && enemy.IsAlive)
        {
            Vector2 toEnemy = enemy.transform.position - spawnPos;
            float angle = Mathf.Atan2(-toEnemy.x, toEnemy.y) * Mathf.Rad2Deg;
            rotation = Quaternion.Euler(0f, 0f, angle);
        }

        Instantiate(bullet, spawnPos, rotation);

        HasAmmo = false;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (sprite == null)
            return;
        Color target = HasAmmo ? hasAmmoColor: noAmmoColor;
        target.a = sprite.color.a;
        sprite.color = target;
    }
}
