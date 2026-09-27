using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHPBar : MonoBehaviour
{
    [SerializeField] private PlayerHPSystem player;
    [SerializeField] private Image hpFiller;

    [Header("Name")]
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private string displayName = "Player"; //placeholder

    [Header("0-1 of its width")]
    [SerializeField, Range(0f, 1f)] private float fillStart = 0.168f;
    [SerializeField, Range(0f, 1f)] private float fillEnd = 0.96f;

    [Header("Animation")]
    [SerializeField] private float drainSpeed = 0f; // how fast the bar catches up; 0 = instant

    private float targetPercent = 1f;
    private float shownPercent = 1f;

    void Start()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerHPSystem>();

        if (nameLabel != null)
            nameLabel.text = displayName;

        if (player == null)
        {
            Debug.LogError("no PlayerHPSystem found", this);
            return;
        }

        player.onHPChange.AddListener(OnHPChanged);

        targetPercent = shownPercent = (float)player.CurrentHP / player.MaxHP;
        ApplyFill(shownPercent);
    }

    void OnDestroy()
    {
        if (player != null)
            player.onHPChange.RemoveListener(OnHPChanged);
    }

    // called every time HP changes 
    private void OnHPChanged(int current, int max)
    {
        targetPercent = max > 0 ? (float)current / max : 0f;

        if (drainSpeed <= 0f)
        {
            shownPercent = targetPercent;
            ApplyFill(shownPercent);
        }
    }

    void Update()
    {
        if (drainSpeed <= 0f || Mathf.Approximately(shownPercent, targetPercent)) return;

        shownPercent = Mathf.MoveTowards(shownPercent, targetPercent, drainSpeed * Time.deltaTime);
        ApplyFill(shownPercent);
    }

    private void ApplyFill(float percent)
    {
        if (hpFiller == null) 
            return;
        
        hpFiller.fillAmount = percent <= 0f ? 0f : Mathf.Lerp(fillStart, fillEnd, percent);
    }
}
