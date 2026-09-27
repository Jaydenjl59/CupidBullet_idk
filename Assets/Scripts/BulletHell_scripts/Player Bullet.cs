using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBullet : MonoBehaviour
{
    [SerializeField] private int dmg = 2;
    [SerializeField] private float speed = 25;
    [SerializeField] private float offscreenDespawn = 1f;

    private void Awake()
    {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector2.up * speed * Time.deltaTime);

        //on miss
        if (PlayArea.Instance != null && !PlayArea.Instance.Contains(transform.position, offscreenDespawn))
        {
            Debug.Log($"PlayerBullet left the play area at {transform.position}", this);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out EnemyHP enemy))
        {
            enemy.TakeDamage(dmg);
            Destroy(gameObject);
        }
    }
}
