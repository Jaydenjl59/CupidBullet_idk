using UnityEngine;

public class BulletPatternSpawner : MonoBehaviour
{
    public enum Pattern { ring, spiral, Spread }
    public enum FireMode { Timer, AfterOtherPattern }

    [System.Serializable]
    public class PatternSettings
    {
        public string label = "New Pattern"; // also used by other entries to reference this one
        public bool active = true;
        public Pattern pattern = Pattern.ring;
        public BulletPool poolOverride; // for using a different bullet type
        public int bulletCount = 12;

        [Header("Timing")]
        public FireMode fireMode = FireMode.Timer;
        public float shootInterval = 2f; // time from the start of one burst to the start of the next
        public string triggerPatternLabel = ""; // label of the entry to count
        [Min(1)] public int fireEveryN = 3; // fire once per this many bursts of that entry

        [Header("Burst")]
        [Min(1)] public int burstCount = 3;
        public float burstDelay = 0.07f;
        [Min(0f)] public float burstStartDelay = 0f;
        public bool reaimEachShot = false;

        [Header("Spread only")]
        public bool aimAtPlayer = true;
        public float aimAngle = 180f;
        public float spreadAngle = 20f;

        [Header("Spiral only")]
        public float spiralTurn = 12f;

        [HideInInspector] public float timer;
        [HideInInspector] public float spiralOffset;
        [HideInInspector] public int burstShotsLeft;
        [HideInInspector] public float burstTimer;
        [HideInInspector] public float lockedAim;
        [HideInInspector] public int triggerIndex = -1; // which list entry we listen to (found from the label)
        [HideInInspector] public int triggerCounter; // how many times that entry has fired since we last fired
        [HideInInspector] public bool pendingBurst; // set when the count is reached fires on our next Tick
        [HideInInspector] public bool firstShotPending;
    }

    [SerializeField] private BulletPool bulletPool; // default pool for every pattern
    [SerializeField] private Transform player;
    [SerializeField] private float spawnRadius = 0f;

    [Header("Enemy Link")]
    [SerializeField] private EnemyHP enemyHP;
    [SerializeField] private bool colorByEnemyHP = true;
    [SerializeField] private Color fullHPColor = Color.white; // bullet color at full enemy HP
    [SerializeField] private Color lowHPColor = Color.red; // bullet color near 0 enemy HP

    // defaults for new spawners
    [SerializeField]
    private PatternSettings[] patterns =
    {
        new PatternSettings { label = "Ring", pattern = Pattern.ring, shootInterval = 1f, bulletCount = 20, burstCount = 1 },
        new PatternSettings { label = "Aimed Spread", pattern = Pattern.Spread, shootInterval = 3f, bulletCount = 5, spreadAngle = 20f, burstCount = 3, burstDelay = 0.07f },
    };

    [Header("Testing")]
    [SerializeField] private bool cyclePatterns = false;
    [SerializeField] private float secondsPerPattern = 4f;

    private int cycleIndex;
    private float cycleTimer;

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (enemyHP == null)
        {
            enemyHP = GetComponent<EnemyHP>();
        }       
        ResolveTriggers();
    }

    // Turns each "triggerPatternLabel" into a list index once, so we don't search by name every frame.
    private void ResolveTriggers()
    {
        if (patterns == null) return;

        for (int i = 0; i < patterns.Length; i++)
        {
            PatternSettings settings = patterns[i];
            settings.triggerIndex = -1;
            if (settings.fireMode != FireMode.AfterOtherPattern) continue;

            for (int j = 0; j < patterns.Length; j++)
            {
                if (j != i && patterns[j].label == settings.triggerPatternLabel)
                {
                    settings.triggerIndex = j;
                    break;
                }
            }

            if (settings.triggerIndex == -1)
                Debug.LogWarning($"Pattern '{settings.label}' is set to fire after '{settings.triggerPatternLabel}', but no other entry has that label.");
        }
    }

    void Update()
    {
        if (patterns == null || patterns.Length == 0 || (enemyHP != null && !enemyHP.IsAlive)
            || BattleManager.BattleDone)
            return;

        if (cyclePatterns)
        {
            cycleIndex %= patterns.Length;
            cycleTimer += Time.deltaTime;
            if (cycleTimer >= secondsPerPattern)
            {
                cycleTimer -= secondsPerPattern;
                cycleIndex = (cycleIndex + 1) % patterns.Length;
                Debug.Log("Switched to pattern: " + patterns[cycleIndex].label);
            }
            Tick(patterns[cycleIndex]);
        }
        else
        {
            foreach (PatternSettings settings in patterns)
            {
                if (settings.active)
                    Tick(settings);
            }
        }
    }

    // every pattern counts its own time, so they fire independently.
    private void Tick(PatternSettings settings)
    {
        if (settings.fireMode == FireMode.Timer)
        {
            // main timer: starts a new burst every shootInterval.
            settings.timer += Time.deltaTime;
            if (settings.timer >= settings.shootInterval)
            {
                settings.timer -= settings.shootInterval;
                StartBurst(settings);
            }
        }
        else if (settings.pendingBurst)
        {
            // the pattern we listen to has fired enough times.
            settings.pendingBurst = false;
            StartBurst(settings);
        }

        // burst bullet interval timer: fires the remaining shots of the current burst, burstDelay apart.
        if (settings.burstShotsLeft > 0)
        {
            settings.burstTimer += Time.deltaTime;
            if (settings.burstTimer >= settings.burstDelay)
            {
                settings.burstTimer -= settings.burstDelay;
                FireShot(settings);
            }
        }
    }

    private void StartBurst(PatternSettings settings)
    {
        settings.burstShotsLeft = Mathf.Max(1, settings.burstCount);
        settings.firstShotPending = true;
        settings.burstTimer = settings.burstDelay - settings.burstStartDelay;
    }

    private void NotifyListeners(PatternSettings source)
    {
        foreach (PatternSettings listener in patterns)
        {
            if (!listener.active || listener.fireMode != FireMode.AfterOtherPattern) continue;
            if (listener.triggerIndex < 0 || patterns[listener.triggerIndex] != source) continue;

            listener.triggerCounter++;
            if (listener.triggerCounter >= listener.fireEveryN)
            {
                listener.triggerCounter = 0;
                listener.pendingBurst = true; // prevents circular wait
            }
        }
    }

    private void FireShot(PatternSettings settings)
    {
        if (settings.firstShotPending)
        {
            settings.firstShotPending = false;
            settings.lockedAim = GetSpreadCenter(settings);
            NotifyListeners(settings);
        }

        settings.burstShotsLeft--;
        Shoot(settings);
    }

    private float GetSpreadCenter(PatternSettings settings)
    {
        return (settings.aimAtPlayer && player != null)
            ? AngleTo(player.position)
            : settings.aimAngle;
    }

    private void Shoot(PatternSettings settings)
    {
        BulletPool pool = settings.poolOverride != null ? settings.poolOverride : bulletPool;
        if (pool == null || settings.bulletCount <= 0) return;

        switch (settings.pattern)
        {
            case Pattern.ring:
                ShootRing(pool, settings.bulletCount, 0f);
                break;

            case Pattern.spiral:
                ShootRing(pool, settings.bulletCount, settings.spiralOffset);
                settings.spiralOffset = (settings.spiralOffset + settings.spiralTurn) % 360f;
                break;

            case Pattern.Spread:
                float center = settings.reaimEachShot ? GetSpreadCenter(settings) : settings.lockedAim;
                ShootSpread(pool, settings.bulletCount, center, settings.spreadAngle);
                break;
        }
    }

    private void ShootRing(BulletPool pool, int count, float angleOffset)
    {
        float step = 360f / count;
        for (int num = 0; num < count; num++)
        {
            SpawnAtAngle(pool, angleOffset + num * step);
        }
    }

    private void ShootSpread(BulletPool pool, int count, float centerAngle, float width)
    {
        if (count == 1)
        {
            SpawnAtAngle(pool, centerAngle);
            return;
        }

        float start = centerAngle - width / 2f;
        float step = width / (count - 1);
        for (int num = 0; num < count; num++)
        {
            SpawnAtAngle(pool, start + num * step);
        }
    }

    // (0 up, 90 left, 180 down, 270 right) from this spawner toward a target.
    private float AngleTo(Vector3 target)
    {
        Vector2 toTarget = target - transform.position;
        return Mathf.Atan2(-toTarget.x, toTarget.y) * Mathf.Rad2Deg;
    }

    private Color CurrentBulletColor()
    {
        return Color.Lerp(lowHPColor, fullHPColor, enemyHP.HPPercent);
    }
    private void SpawnAtAngle(BulletPool pool, float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(-Mathf.Sin(radian), Mathf.Cos(radian), 0f);
        Vector3 pos = transform.position + direction * spawnRadius;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        EnemyBullet bullet = pool.SpawnBullet(pos, rotation);

        //pool.SpawnBullet(pos, rotation);

        if (colorByEnemyHP && enemyHP != null)
            bullet.SetColor(CurrentBulletColor());
    }
}