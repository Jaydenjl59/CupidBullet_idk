using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using AmoraNovel;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private string battleId = "battle_1"; // only used when this scene runs on its own
    [SerializeField] private PlayerHPSystem player;
    [SerializeField] private EnemyHP enemy;

    [Header("Scenes to load when testing this scene on its own")]
    [SerializeField] private string winScene = "";
    [SerializeField] private string loseScene = "";
    [SerializeField] private float delayBeforeLoad = 1.5f;

    public static bool BattleDone { get; private set; }
    private float startTime;

    void Start()
    {
        BattleDone = false;
        startTime = Time.time;
        if (player == null)
            player = FindAnyObjectByType<PlayerHPSystem>();
        if (enemy == null)
            enemy = FindAnyObjectByType<EnemyHP>();

        if (player != null)
            player.onDeath.AddListener(OnPlayerDied);

        if (enemy != null)
            enemy.onDeath.AddListener(OnEnemyDied);
    }

    private void OnDestroy()
    {
        BattleDone = false;
        if (player != null)
            player.onDeath.RemoveListener(OnPlayerDied);
        if (enemy != null)
            enemy.onDeath.RemoveListener(OnEnemyDied);
    }

    private void OnPlayerDied() => EndBattle(false);
    private void OnEnemyDied() => EndBattle(true);

    private void EndBattle(bool win)
    {
        if (BattleDone)
            return; // only the first ending counts (player dies right after winning)

        BattleDone = true;

        // Capture the result now, before anything gets hidden.
        int hp = player != null ? player.CurrentHP : 0;
        int maxHP = player != null ? player.MaxHP : 0;
        float duration = Time.time - startTime;

        if (win && enemy != null)
            enemy.gameObject.SetActive(false);
        if (!win && player != null)
            player.gameObject.SetActive(false);

        StartCoroutine(FinishAfterDelay(win, hp, maxHP, duration));
    }

    private IEnumerator FinishAfterDelay(bool win, int hp, int maxHP, float duration)
    {
        yield return new WaitForSeconds(delayBeforeLoad);

        // Inside the visual novel: let AmoraGame record the result and continue the story.
        if (AmoraBattle.Active != null)
        {
            AmoraBattle.Active.ReportResult(win, hp, maxHP, duration);
            yield break;
        }

        // Running this scene on its own (for testing): record and load the next scene ourselves.
        GameData.RecordBattle(new GameData.BattleResult
        {
            battleId = battleId,
            win = win,
            playerHP = hp,
            playerMaxHP = maxHP,
            duration = duration
        });

        string sceneName = win ? winScene : loseScene;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.Log($"BattleManager: battle ended (won: {win}, HP {hp}/{maxHP}), but no scene is set to load.");
            yield break;
        }

        SceneManager.LoadScene(sceneName);
    }
}