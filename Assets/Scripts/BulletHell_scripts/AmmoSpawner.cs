using UnityEngine;

public class AmmoSpawner : MonoBehaviour
{
    [SerializeField] private AmmoPickup ammoPickup;
    [SerializeField] private float minDelay = 3f;
    [SerializeField] private float maxDelay = 5f;
    [SerializeField] private float edgePadding = 2f; // prevents spawns too close to the border
    [SerializeField, Range(0.1f, 1f)] private float ammoSpawnHalf = 0.5f;
    [SerializeField] private PlayerShoot shooter;

    private AmmoPickup current;
    private float timer;
    void Start()
    {
        if (shooter == null)
            shooter = FindAnyObjectByType<PlayerShoot>();
        ResetTimer();
    }

    // Update is called once per frame
    void Update()
    {
        if (BattleManager.BattleDone)
        {
            if (current != null)
            {
                Destroy(current.gameObject); // remove a pickup left lying around
                current = null;
            }
            return;
        }
        if (current != null || (shooter != null && shooter.HasAmmo))
            return;
        timer -= Time.deltaTime;
        if (timer <= 0f)
            SpawnPickup();
    }

    private void ResetTimer()
    {
        timer = Random.Range(minDelay, maxDelay);
    }

    private void SpawnPickup()
    {
        if (ammoPickup == null || PlayArea.Instance == null) 
            return;

        PlayArea area = PlayArea.Instance;
        float zoneTop = area.Min.y + area.Size.y * ammoSpawnHalf;

        float x = Random.Range(area.Min.x + edgePadding, area.Max.x - edgePadding);
        float y = Random.Range(area.Min.y + edgePadding, zoneTop - edgePadding);

        current = Instantiate(ammoPickup, new Vector3(x, y, 0f), Quaternion.identity);
        current.SetSpawner(this);
    }

    public void OnPickupCollected()
    {
        current = null;
        ResetTimer();
    }
}
