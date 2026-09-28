using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace AmoraNovel
{

    // Bridge between AmoraGame (the visual novel) and the bullet hell scenes.
    // AmoraGame creates and uses this exactly like before. Instead of running its own mini-game,
    // it loads a bullet hell scene alongside the story, shows that scene's camera inside the
    // arena frame, and reports the result back to AmoraGame.
    public class AmoraBattle : MonoBehaviour
    {
        // Scene to load for each difficulty AmoraGame passes in (1 = bullethell_1, 2 = start_bullethell_2).
        // Each scene must be in File > Build Profiles > Scene List.
        static readonly string[] BattleScenes = { "", "BattleTest_1", "BattleTest_1" };

        // The battle currently running, so BattleManager can report back to it.
        public static AmoraBattle Active { get; private set; }

        AmoraGame owner;
        RectTransform root, arena, arenaBorder;
        RawImage arenaView;
        RenderTexture arenaTexture;
        Camera battleCamera;
        Text hud, barrierLabel;
        string sceneName;
        Scene loadedScene;
        bool sceneLoaded, stopped, paused;
        float elapsed;
        PlayerHPSystem player;
        EnemyHP enemy;

        // AmoraGame sets this from its pause menu.
        public bool Paused
        {
            get => paused;
            set { paused = value; ApplyTimeScale(); }
        }

        // Kept so any existing code that reads these still compiles.
        public int HP => player != null ? player.CurrentHP : 0;
        public float Elapsed => elapsed;
        public int Barrier => enemy != null ? enemy.CurrentHP : 0;
        public float Duration => 0f; // no countdown in this version

        public void Begin(AmoraGame game, Transform canvas, int difficulty, Texture texture, Texture amora, AudioClip sound)
        {
            owner = game;
            Active = this;
            sceneName = difficulty > 0 && difficulty < BattleScenes.Length ? BattleScenes[difficulty] : BattleScenes[1];

            // Same framing as the original skill check: backdrop, title and instructions on the left,
            // status on the right, and the arena in the middle.
            root = owner.Panel("Bullet hell", canvas, 0, 0, 1280, 720, new Color(.10f, .025f, .12f));
            owner.Label("Title", root, difficulty == 1 ? "CATCH YOUR\nBREATH" : "A RACING\nHEART", -437, 265, 310, 105, 30, new Color(1, .72f, .83f));
            owner.Label("Instructions", root,
                "BREAK HER HEART BARRIER\n\nWASD / arrows · move\nShift · focus\nCollect a heart to load a shot\nSpace · send it back\nEsc · pause\n\nIf your composure runs out,\nthe attempt ends.",
                -437, 45, 315, 310, 21, new Color(.92f, .77f, .87f));

            arenaBorder = owner.Panel("Arena border", root, 0, 0, 564, 664, new Color(.84f, .34f, .57f));
            arena = owner.Panel("Arena", root, 0, 0, 556, 656, Color.black);

            // The battle camera's picture goes here once the scene has loaded.
            var viewObject = new GameObject("Arena view", typeof(RectTransform), typeof(RawImage));
            viewObject.transform.SetParent(arena, false);
            var viewRect = (RectTransform)viewObject.transform;
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = viewRect.offsetMax = Vector2.zero;
            arenaView = viewObject.GetComponent<RawImage>();
            arenaView.raycastTarget = false;
            arenaView.enabled = false;

            owner.Panel("Composure panel", root, 435, -270, 290, 106, new Color(.10f, .025f, .12f, .93f));
            hud = owner.Label("Status", root, "", 435, -270, 295, 110, 25, Color.white);
            barrierLabel = owner.Label("Barrier", root, "", 435, 280, 295, 70, 23, new Color(1, .7f, .84f));

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"AmoraBattle: couldn't load scene '{sceneName}'. Is it in the Build Profiles scene list?");
                return;
            }
            load.completed += _ => OnSceneLoaded();
        }

        void OnSceneLoaded()
        {
            // The scene that just finished loading is always the last one in the list.
            // Keeping this exact handle matters on a retry, when an old copy may still be unloading.
            Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            // The battle was closed (e.g. back to title) while the scene was still loading.
            if (this == null)
            {
                SceneManager.UnloadSceneAsync(scene);
                return;
            }

            loadedScene = scene;
            sceneLoaded = true;

            // The story scene already has these; duplicates cause warnings or double input.
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                foreach (AudioListener listener in rootObject.GetComponentsInChildren<AudioListener>(true))
                    listener.enabled = false;
                foreach (EventSystem eventSystem in rootObject.GetComponentsInChildren<EventSystem>(true))
                    eventSystem.gameObject.SetActive(false);
            }

            player = FindAnyObjectByType<PlayerHPSystem>();
            enemy = FindAnyObjectByType<EnemyHP>();
            PlayArea playArea = FindAnyObjectByType<PlayArea>();

            if (playArea == null)
            {
                Debug.LogError($"AmoraBattle: scene '{sceneName}' has no PlayArea on its camera.");
                return;
            }

            // Fit the arena frame to the play area's shape (it was built for a tall rectangle).
            Vector2 areaSize = playArea.Size;
            float aspect = areaSize.x / areaSize.y;
            float maxWidth = 556f, maxHeight = 656f;
            float width = maxWidth, height = maxWidth / aspect;
            if (height > maxHeight) { height = maxHeight; width = maxHeight * aspect; }
            arena.sizeDelta = new Vector2(width, height);
            arenaBorder.sizeDelta = new Vector2(width + 8f, height + 8f);

            // Render the battle camera into a texture and show it in the arena.
            int textureHeight = 1024;
            int textureWidth = Mathf.RoundToInt(textureHeight * aspect);
            arenaTexture = new RenderTexture(textureWidth, textureHeight, 24) { name = "Bullet hell arena" };
            battleCamera = playArea.GetComponent<Camera>();
            playArea.RenderToTexture(arenaTexture);
            arenaView.texture = arenaTexture;
            arenaView.enabled = true;

            ApplyTimeScale();
        }

        void Update()
        {
            if (!sceneLoaded || stopped || paused) return;

            elapsed += Time.deltaTime;

            if (player != null)
                hud.text = "COMPOSURE  " + player.CurrentHP + " / " + player.MaxHP;
            if (enemy != null)
                barrierLabel.text = "HEART BARRIER\n" + enemy.CurrentHP;
        }

        // Called by BattleManager when the fight ends.
        public void ReportResult(bool won, int hp, int maxHP, float duration)
        {
            if (stopped || owner == null) return;
            owner.FinishBattle(won, hp, duration, maxHP);
        }

        // Kept for AmoraSetup's editor tests, which drove the old mini-game frame by frame.
        // The new battle runs on its own in its scene, so this only advances the timer (and respects pause).
        public void Tick(float dt, Vector2 move, bool fire, bool focus)
        {
            if (stopped || paused) return;
            elapsed += dt;
        }

        // Called by AmoraGame when it shows the result screen. Freezes the arena behind it.
        public void Stop()
        {
            stopped = true;
            ApplyTimeScale();
        }

        void ApplyTimeScale()
        {
            // AmoraGame animates with unscaled time, so this only freezes the bullet hell.
            Time.timeScale = (paused || stopped) ? 0f : 1f;
        }

#if UNITY_EDITOR
        public void TestHit() { if (player != null) player.TakeDamage(1); }
        public void TestSurvive() { ReportResult(true, HP, player != null ? player.MaxHP : 0, elapsed); }
        public void TestLose() { ReportResult(false, 0, player != null ? player.MaxHP : 0, elapsed); }
#endif

        void OnDestroy()
        {
            if (Active == this) Active = null;
            Time.timeScale = 1f;

            // Stop the camera drawing into the texture before releasing it.
            if (battleCamera != null)
            {
                battleCamera.enabled = false;
                battleCamera.targetTexture = null;
            }
            if (arenaTexture != null)
            {
                arenaTexture.Release();
                Destroy(arenaTexture);
            }

            if (sceneLoaded && loadedScene.isLoaded)
                SceneManager.UnloadSceneAsync(loadedScene);

            if (root) Destroy(root.gameObject);
        }
    }
}