using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class BattleManager: MonoBehaviour
{
    [SerializeField] private string battleId = "battle_1";
    [SerializeField] private PlayerHPSystem player;
    [SerializeField] private EnemyHP enemy;

    [Header("Scenes to load (names must be in the build's scene list)")]
    [SerializeField] private string winScene = "";
    [SerializeField] private string loseScene = "";
    [SerializeField] private float delayBeforeLoad = 1.5f;

    //private bool battleDone;
    public static bool BattleDone { get; private set; }
    private float startTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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

        GameData.RecordBattle(new GameData.BattleResult
        {
            battleId = battleId,
            win = win,
            playerHP = player != null ? player.CurrentHP : 0,
            playerMaxHP = player != null ? player.MaxHP : 0,
            duration = Time.time - startTime
        });
        if (win && enemy != null) 
            enemy.gameObject.SetActive(false);
        if (!win && player != null) 
            player.gameObject.SetActive(false);

        StartCoroutine(LoadAfterDelay(win ? winScene : loseScene));

    }

    private IEnumerator LoadAfterDelay(string sceneName)
    {
        yield return new WaitForSeconds(delayBeforeLoad);

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.Log("BattleManager: battle ended, but no scene is set to load.");
            yield break;
        }

        SceneManager.LoadScene(sceneName);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
