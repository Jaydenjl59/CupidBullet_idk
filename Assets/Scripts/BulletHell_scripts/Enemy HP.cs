using UnityEngine;
using UnityEngine.Events;

public class EnemyHP : MonoBehaviour
{
    [SerializeField] private int maxHP = 10;

    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public bool IsAlive => CurrentHP > 0;
    public float HPPercent => maxHP > 0 ? (float)CurrentHP / maxHP : 0f; //1 = full, 0 = dead

    public UnityEvent<int, int> onHPChange;
    public UnityEvent onDeath;

    void Awake()
    {
        CurrentHP = maxHP;
    }

    public void TakeDamage(int dmg)
    {
        if (!IsAlive)
            return;

        CurrentHP = Mathf.Max(CurrentHP - dmg, 0);
        onHPChange?.Invoke(CurrentHP, maxHP);

        if (CurrentHP == 0)
            onDeath?.Invoke();
    }

    //// Update is called once per frame
    //void Update()
    //{
        
    //}
}
