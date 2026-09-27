using UnityEngine;


[RequireComponent(typeof(PlayerHPSystem))]
public class PlayerImmuneFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private float flashPerSec = 10f;
    [SerializeField, Range(0f, 1f)] private float fader = 0.2f; 

    private PlayerHPSystem hp;
    private Color baseColor;

    private void Awake()
    {
        hp = GetComponent<PlayerHPSystem>();

        if (sprite == null)
            sprite = GetComponent<SpriteRenderer>();

        baseColor = sprite.color;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Color color = baseColor;

        if (hp.IsImmune)
        {
            bool fadeCond = Mathf.FloorToInt(Time.time * flashPerSec * 2f) % 2 == 0;
            if (fadeCond)
            {
                color.a = baseColor.a * fader;
            }
        }
        sprite.color = color;
    }

    void OnDIsable()
    {
        if (sprite != null)
        {
            sprite.color = baseColor;
        }
    }
}
