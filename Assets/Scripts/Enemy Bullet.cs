using UnityEngine;
using UnityEngine.Pool;


[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    [SerializeField] private int dmg;
    [SerializeField] private float speed;
    [SerializeField] private float lifetime = 6f;

    public int Damage => dmg;
    public float Speed => speed;

    private IObjectPool<EnemyBullet> bulletPool;
    private float timeAlive;
    private bool released;
    private int originalDamage;
    private float originalSpeed;

    void Awake()
    {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        originalDamage = dmg;
        originalSpeed = speed;
    }

    void onEnable()
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

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime);
        timeAlive += Time.deltaTime;
        if (timeAlive >= lifetime)
            Despawn();
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
        if (released) return; // stops a double-release if it hits AND times out on the same frame
        released = true;

        if (bulletPool != null)
            bulletPool.Release(this);    // back to the pool for reuse
        else
            Destroy(gameObject);   // fallback if placed in the scene by hand without a pool
    }
}
