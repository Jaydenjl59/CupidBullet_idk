using UnityEngine;
using UnityEngine.Events;
//using UnityEngine.InputSystem;

public class PlayerHPSystem : MonoBehaviour {
    [SerializeField] private int maxHP = 100;
    [SerializeField] private float Iframes = .5f;

    public int CurrentHP { get; private set; }
    public int MaxHP => maxHP;
    public bool IsAlive => CurrentHP > 0;
    public bool IsImmune => Time.time < ImmuneUntil;

    public UnityEvent<int, int> onHPChange;
    public UnityEvent onDeath;

    private float ImmuneUntil;

    void Awake() {
        CurrentHP = maxHP;    
    }
    //void Update()
    //{
    //    if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
    //        TakeDamage(10);
    //    if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
    //        Heal(10);
    //}

    public void TakeDamage(int amount) {
        if (!IsAlive || IsImmune) 
            return;
        CurrentHP = Mathf.Max(CurrentHP - amount, 0);
        ImmuneUntil = Time.time + Iframes;
        onHPChange?.Invoke(CurrentHP, maxHP);

        if (CurrentHP == 0) {
            Death();
        }
    }
    public void Heal(int amount) {
        if (!IsAlive)
            return;
        CurrentHP = Mathf.Min(CurrentHP + amount, maxHP);
        onHPChange?.Invoke(CurrentHP, maxHP);
    }

    private void Death() {
        onDeath?.Invoke();
    }

}
