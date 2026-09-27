using UnityEngine;

// The collectible. Needs a Collider2D with "Is Trigger" checked.
public class AmmoPickup : MonoBehaviour
{
    private AmmoSpawner spawner;

    public void SetSpawner(AmmoSpawner owner)
    {
        spawner = owner;
    }

    void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

    void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        if (other.TryGetComponent(out PlayerShoot shooter) && shooter.TryCollectAmmo())
        {
            if (spawner != null) spawner.OnPickupCollected();
            Destroy(gameObject);
        }
    }
}