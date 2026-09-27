using UnityEngine;
using UnityEngine.Pool;


[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private int dmg;
    [SerializeField] private float speed;
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float offScreenDespawn = 1f;

    public int Damage => dmg;
    public float Speed => speed;

    private IObjectPool<EnemyBullet> bulletPool;
    private SpriteRenderer sprite;
    private float timeAlive;
    private bool released;
    private int originalDamage;
    private float originalSpeed;

    void Awake()
    {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        sprite = GetComponentInChildren<SpriteRenderer>();
        originalDamage = dmg;
        originalSpeed = speed;
    }

    void OnEnable()
    {
        timeAlive = 0f;
        released = false;
        dmg = originalDamage;
        speed = originalSpeed;
    }

    public void SetPool(IObjectPool<EnemyBullet> hostPool)
    {
        bulletPool = hostPool;
    }

    public void SetColor(Color color)
    {
        if (sprite != null)
        {
            sprite.color = color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime);
        timeAlive += Time.deltaTime;
        if (timeAlive >= lifetime)
        {
            Despawn();
            return;
        }

        if (PlayArea.Instance != null && !PlayArea.Instance.Contains(transform.position, offScreenDespawn))
        {
            Despawn();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerHPSystem playerHP))
        {
            playerHP.TakeDamage(Damage);
            //Despawn();
        }
    }

    public void Setup(int newDamage, float newSpeed)
    {
        dmg = newDamage;
        speed = newSpeed;
    }

    private void Despawn()
    {
        if (released) 
            return; // stops a double-release if it hits AND times out on the same frame
        released = true;

        if (bulletPool != null)
            bulletPool.Release(this);    // back to the pool for reuse
        else
            Destroy(gameObject);   // fallback if placed in the scene by hand without a pool
    }
}
