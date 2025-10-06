using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // シングルトンインスタンス

    //　ゲーム状態定義
    public enum GameState
    {
        Gameplay,
        Paused,
        GameOver,
        LevelUp,
    }

    public GameState currentState;  // 現在のゲーム状態
    public GameState previousState; // 前回のゲーム状態

    [Header("Damage Text Setting")]
    public Canvas damageTextCanvas;
    public float textFontSize = 20;
    public TMP_FontAsset textFont;
    public Camera referenceCamera;
    public TextMeshProUGUI damageTextPrefab;

    [Header("Screens")]
    public GameObject pauseScreen;  // 一時停止画面
    public GameObject resultsScreen;  // 結果画面
    public GameObject levelUpScreen; // レベルアップ画面
    int stackedLevelUps = 0;

    // リザルト画面
    [Header("Result Screen Displays")]
    public Image chosenCharacterImage;
    public TextMeshProUGUI chosenCharacterName;
    public TextMeshProUGUI levelReachedDisplay;
    public TextMeshProUGUI timeSurvivedDisplay;

    [Header("Stopwatch")]
    public float timeLimit; // タイマーの制限時間
    float stopwatchTime;    // ストップウォッチの時間
    public TextMeshProUGUI stopwatchDisplay;    // ストップウォッチの表示

    [Header("Debug")]
    public TextMeshProUGUI totalObjectCountDisplay;

    PlayerStats[] players;  // プレイヤー

    public bool isGameOver { get { return currentState == GameState.GameOver; } }   // ゲームオーバーフラグ
    public bool choosingUpgrade { get { return currentState == GameState.LevelUp; } } // レベルアップ選択中フラグ

    public float GetElapsedTime() { return stopwatchTime; }

    // Curse（呪い）合計値を計算
    public static float GetCumulativeCurse()
    {
        if (!instance) return 1;    // インスタンスが存在しない場合はデフォルト値1を返す

        float totalCurse = 0;
        foreach (PlayerStats p in instance.players)
        {
            totalCurse += p.Actual.curse;   // 各プレイヤーのCurse値を加算
        }
        return Mathf.Max(1, 1 + totalCurse);    // 1未満にならないようにする(最低でも1を返す)
    }

    // Level合計値を計算
    public static int GetCumulativeLevels()
    {
        if (!instance) return 1;

        int totalLevels = 0;
        foreach (PlayerStats p in instance.players)
        {
            totalLevels += p.level;
        }
        return Mathf.Max(1, totalLevels);
    }

    void Start()
    {
        // ゲームプレイ開始時にBGMを再生
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBGM("MusMus-BGM-125");
        }
    }

    void Awake()
    {
        players = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // 既にインスタンスが存在する場合、重複を防ぐために削除
            Debug.LogWarning("EXTRA " + this + " DELETED");
            ObjectPool.instance.Return(gameObject);
        }

        DisableScreens();
    }

    void Update()
    {
        switch (currentState)
        {
            case GameState.Gameplay:
                // ゲームプレイ中の処理
                CheckForPauseAndResume();
                UpdateStopwatch();
                UpdateDebugDisplay();   // デバッグ情報の更新
                break;
            case GameState.Paused:
                // 一時停止中の処理
                CheckForPauseAndResume();
                break;
            case GameState.GameOver:
            case GameState.LevelUp:
                break;

            default:
                Debug.LogWarning("Unknown GameState");
                break;
        }
    }

    /// <summary>
    /// オブジェクトプールからの情報をデバッグUIに表示します。
    /// </summary>
    private void UpdateDebugDisplay()
    {
        // オブジェクトプールが存在し、かつ表示用のUIが設定されている場合のみ実行
        if (ObjectPool.instance != null && totalObjectCountDisplay != null)
        {
            int totalObjects = ObjectPool.instance.GetTotalPooledObjectCount();
            
            // テキストを動的に更新
            totalObjectCountDisplay.text = $"Objects: {totalObjects}";
            
            // 1000体を超えたら警告色にする（任意）
            if (totalObjects > 1000)
            {
                totalObjectCountDisplay.color = Color.red;
            }
            else
            {
                totalObjectCountDisplay.color = Color.white; // 通常の色に戻す
            }
        }
    }

    // 【エディタ用】指定した時間までゲーム内時間を進める
    public void SkipTo(float timeInSeconds)
    {
        // メインタイマーを指定時間まで進める
        stopwatchTime = timeInSeconds;
        UpdateStopwatchDisplay(); // UIのタイマー表示を更新

        // SpawnManagerの状態を指定時間に同期させる
        SpawnManager spawnManager = FindFirstObjectByType<SpawnManager>();
        if (spawnManager)
        {
            spawnManager.SyncToTime(timeInSeconds);
        }

        // EventManagerの状態も指定時間に同期させる
        EventManager eventManager = FindFirstObjectByType<EventManager>();
        if (eventManager)
        {
            eventManager.SyncToTime(timeInSeconds);
        }
    }

    public void ChangeState(GameState newState)
    {
        previousState = currentState;
        currentState = newState;
    }

    public void PauseGame()
    {
        if (currentState != GameState.Paused)
        {
            ChangeState(GameState.Paused);  // 状態を一時停止に変更
            Time.timeScale = 0f;    // 時間停止
            pauseScreen.SetActive(true);    // 一時停止画面を表示
            Debug.Log("ポーズ中です。");
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            ChangeState(previousState);  // 前回の状態に戻す
            Time.timeScale = 1f;    // 時間停止解除
            pauseScreen.SetActive(false);   // 一時停止画面を非表示
            Debug.Log("ポーズ解除しました。");
        }
    }

    void CheckForPauseAndResume()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GameState.Paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    void DisableScreens()
    {
        pauseScreen.SetActive(false);   // 一時停止画面を非表示
        resultsScreen.SetActive(false); // 結果画面を非表示
        levelUpScreen.SetActive(false); // レベルアップ画面を非表示
    }

    public void GameOver()
    {
        timeSurvivedDisplay.text = stopwatchDisplay.text;
        ChangeState(GameState.GameOver);

        // すでにBGM再生中ならフェードして停止
        if (SoundManager.Instance != null && SoundManager.Instance.bgmSource.isPlaying)
        {
            // 1.5秒かけてフェードアウト
            SoundManager.Instance.StopBGM(1.5f);
        }

        Time.timeScale = 0f;
        DisplayResults();
    }

    void DisplayResults()
    {
        resultsScreen.SetActive(true);  // 結果画面を表示
    }

    public void AssignChosenCharacterUI(CharacterData characterData)
    {
        chosenCharacterImage.sprite = characterData.Icon;
        chosenCharacterName.text = characterData.Name;
    }

    public void AssignLevelReachedUI(int levelReachedData)
    {
        levelReachedDisplay.text = levelReachedData.ToString();
    }

    void UpdateStopwatch()
    {
        stopwatchTime += Time.deltaTime;

        UpdateStopwatchDisplay();

        // 時間制限に達したら
        if (stopwatchTime >= timeLimit)
        {
            // Demon以外のすべての敵を消滅させる
            EnemyMovement[] allEnemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
            foreach (EnemyMovement enemy in allEnemies)
            {
                if (!enemy.gameObject.name.Contains("Demon"))
                {
                    ObjectPool.instance.Return(enemy.gameObject);
                }
            }
        }
    }

    void UpdateStopwatchDisplay()
    {
        // 秒数を分と秒に変換
        int minutes = Mathf.FloorToInt(stopwatchTime / 60);
        int seconds = Mathf.FloorToInt(stopwatchTime % 60);

        // フォーマットして表示
        stopwatchDisplay.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void StartLevelUp()
    {
        ChangeState(GameState.LevelUp);

        if (levelUpScreen.activeSelf) stackedLevelUps++;
        else
        {
            SoundManager.Instance.PlaySFXSingle("levelup");
            Time.timeScale = 0f; // 時間停止
            levelUpScreen.SetActive(true); // レベルアップ画面を表示
            
            foreach (PlayerStats p in players)
                p.SendMessage("RemoveAndApplyUpgrades");
        }
    }

    public void EndLevelUp()
    {
        Time.timeScale = 1f; // 時間停止解除
        levelUpScreen.SetActive(false); // レベルアップ画面を非表示
        ChangeState(GameState.Gameplay);

        // 複数のレベルアップに対する処理
        if (stackedLevelUps > 0)
        {
            stackedLevelUps--;
            StartLevelUp();
        }
    }

    public void GenerateFloationgText(string text, Transform target, float duration = 1f, float speed = 1f)
    {
        StartCoroutine(GenerateFloationgTextCoroutine(text, target, duration, speed));
    }

    IEnumerator GenerateFloationgTextCoroutine(string text, Transform target, float duration = 1f, float speed = 50f)
    {
        if (damageTextPrefab == null || ObjectPool.instance == null)
        {
            Debug.LogError("Damage Text Prefab または ObjectPool が設定されていません！");
            yield break;
        }
        // テキストオブジェクトを作成
        // GameObject textObj = new GameObject("Damage Floationg Text");
        GameObject textObj = ObjectPool.instance.Get(damageTextPrefab.gameObject, Vector3.zero, Quaternion.identity);
        // 取得できなかった場合は処理を中断
        if (!textObj) yield break;

        TextMeshProUGUI tmPro = textObj.GetComponent<TextMeshProUGUI>();
        RectTransform rect = textObj.GetComponent<RectTransform>();

        // 取得したコンポーネントが存在しない場合はエラー
        if (tmPro == null || rect == null)
        {
            Debug.LogError("ダメージテキストPrefabにTextMeshProUGUI または RectTransformがアタッチされていません！");
            ObjectPool.instance.Return(textObj);
            yield break;
        }

        tmPro.text = text;
        tmPro.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmPro.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmPro.fontSize = textFontSize;
        if (textFont) tmPro.font = textFont;

        // 画面座標を設定
        rect.position = referenceCamera.WorldToScreenPoint(target.position);

        // 追記: 生成されたテキストオブジェクトをキャンバスの子として設定
        // ObjectPool.Get()では親がObjectPoolになっているため、Canvasの子に戻す必要があります
        textObj.transform.SetParent(instance.damageTextCanvas.transform);
        textObj.transform.SetSiblingIndex(0);

        // テキストを上方向にパンし、時間経過とともにフェードアウトする
        WaitForEndOfFrame w = new WaitForEndOfFrame();
        float t = 0;
        float yOffset = 0;
        while (t < duration)
        {
            tmPro.color = new Color(tmPro.color.r, tmPro.color.g, tmPro.color.b, 1 - t / duration);

            yOffset += speed * Time.deltaTime;
            if (target != null && rect != null)
            {
                rect.position = referenceCamera.WorldToScreenPoint(target.position + new Vector3(0, yOffset));
            }
            t += Time.deltaTime;
            yield return w;
        }

        // ループ終了後（時間が経過した、またはターゲットが破棄された）にプールに戻す
        if (ObjectPool.instance)
        {
            ObjectPool.instance.Return(textObj);
        }
        else
        {
            textObj.SetActive(false); // フォールバック
        }
    }
}
