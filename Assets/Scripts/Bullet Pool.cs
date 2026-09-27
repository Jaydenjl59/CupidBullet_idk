using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour
{
    [SerializeField] private EnemyBullet bulletPreFab;
    [SerializeField] private int defaultCapacity = 300;
    [SerializeField] private int maxSize = 1000;

    private ObjectPool<EnemyBullet> bulletPool;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bulletPool = new ObjectPool<EnemyBullet>
        (
            createFunc: CreateBullet,
            actionOnGet: bullet => bullet.gameObject.SetActive(true),
            actionOnRelease: bullet => bullet.gameObject.SetActive(false),
            actionOnDestroy: bullet => Destroy(bullet.gameObject),
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );
    }

    private EnemyBullet CreateBullet() 
    {
        EnemyBullet bullet = Instantiate(bulletPreFab, transform);
        bullet.SetPool(bulletPool);
        return bullet;
    }

    public EnemyBullet SpawnBullet(Vector3 pos, Quaternion rotate)
    {
        EnemyBullet bullet = bulletPool.Get();
        bullet.transform.SetPositionAndRotation(pos, rotate);
        return bullet;
    }
}
