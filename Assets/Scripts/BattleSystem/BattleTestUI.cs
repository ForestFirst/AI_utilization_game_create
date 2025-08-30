using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BattleSystem;

/// <summary>
/// 戦闘画面テスト用UI
/// 戦闘システムのテストと基本的な戦闘インターフェースを提供
/// </summary>
public class BattleTestUI : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private bool autoCreateUI = true;
    
    // UI要素
    private Canvas battleCanvas;
    private Text playerHPText;
    private Text turnCounterText;
    private Text gameStatusText;
    private Button[] weaponButtons;
    private Button backToTitleButton;
    
    // 戦闘システム参照
    private BattleManager battleManager;
    private GameStateManager gameStateManager;

    private void Start()
    {
        // システム参照の初期化
        InitializeReferences();
        
        if (autoCreateUI)
        {
            CreateBattleUI();
        }
        
        Debug.Log("[BattleTestUI] 戦闘画面UI初期化完了");
    }

    /// <summary>
    /// システム参照の初期化
    /// </summary>
    private void InitializeReferences()
    {
        battleManager = FindObjectOfType<BattleManager>();
        gameStateManager = GameStateManager.Instance;
        
        if (battleManager == null)
        {
            Debug.LogWarning("[BattleTestUI] BattleManagerが見つかりません");
        }
    }

    /// <summary>
    /// 戦闘UI作成
    /// </summary>
    private void CreateBattleUI()
    {
        CreateBattleCanvas();
        CreatePlayerHPDisplay();
        CreateTurnCounter();
        CreateGameStatus();
        CreateWeaponButtons();
        CreateBackToTitleButton();
        CreateBackground();
    }

    /// <summary>
    /// メイン戦闘Canvas作成
    /// </summary>
    private void CreateBattleCanvas()
    {
        var canvasObj = new GameObject("BattleCanvas");
        canvasObj.transform.SetParent(transform, false);

        battleCanvas = canvasObj.AddComponent<Canvas>();
        battleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        battleCanvas.sortingOrder = 50;

        var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// 背景作成
    /// </summary>
    private void CreateBackground()
    {
        var backgroundObj = new GameObject("BattleBackground");
        backgroundObj.transform.SetParent(battleCanvas.transform, false);

        var backgroundRect = backgroundObj.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        backgroundRect.anchoredPosition = Vector2.zero;

        var backgroundImg = backgroundObj.AddComponent<Image>();
        backgroundImg.color = new Color(0.05f, 0.05f, 0.1f, 1f); // ダークブルー背景
    }

    /// <summary>
    /// プレイヤーHP表示作成
    /// </summary>
    private void CreatePlayerHPDisplay()
    {
        var hpObj = new GameObject("PlayerHPDisplay");
        hpObj.transform.SetParent(battleCanvas.transform, false);

        var hpRect = hpObj.AddComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(1f, 1f);
        hpRect.anchorMax = new Vector2(1f, 1f);
        hpRect.sizeDelta = new Vector2(300, 80);
        hpRect.anchoredPosition = new Vector2(-20, -20);

        // HP背景パネル
        var hpBg = hpObj.AddComponent<Image>();
        hpBg.color = new Color(0f, 0f, 0f, 0.8f);

        // HPテキスト
        playerHPText = hpObj.AddComponent<Text>();
        playerHPText.text = "Player HP: 150/150";
        playerHPText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playerHPText.fontSize = 20;
        playerHPText.color = Color.green;
        playerHPText.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// ターンカウンター作成
    /// </summary>
    private void CreateTurnCounter()
    {
        var turnObj = new GameObject("TurnCounter");
        turnObj.transform.SetParent(battleCanvas.transform, false);

        var turnRect = turnObj.AddComponent<RectTransform>();
        turnRect.anchorMin = new Vector2(1f, 1f);
        turnRect.anchorMax = new Vector2(1f, 1f);
        turnRect.sizeDelta = new Vector2(200, 60);
        turnRect.anchoredPosition = new Vector2(-20, -120);

        // ターン背景パネル
        var turnBg = turnObj.AddComponent<Image>();
        turnBg.color = new Color(0f, 0f, 0f, 0.8f);

        // ターンテキスト
        turnCounterText = turnObj.AddComponent<Text>();
        turnCounterText.text = "Turn: 1";
        turnCounterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        turnCounterText.fontSize = 18;
        turnCounterText.color = Color.cyan;
        turnCounterText.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// ゲームステータス表示作成
    /// </summary>
    private void CreateGameStatus()
    {
        var statusObj = new GameObject("GameStatus");
        statusObj.transform.SetParent(battleCanvas.transform, false);

        var statusRect = statusObj.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 0.8f);
        statusRect.anchorMax = new Vector2(0.5f, 0.8f);
        statusRect.sizeDelta = new Vector2(600, 100);
        statusRect.anchoredPosition = Vector2.zero;

        // ステータス背景パネル
        var statusBg = statusObj.AddComponent<Image>();
        statusBg.color = new Color(0f, 0f, 0f, 0.7f);

        // ステータステキスト
        gameStatusText = statusObj.AddComponent<Text>();
        gameStatusText.text = "BATTLE TEST MODE\nPress weapon buttons to test combat system";
        gameStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameStatusText.fontSize = 16;
        gameStatusText.color = Color.yellow;
        gameStatusText.alignment = TextAnchor.MiddleCenter;
    }

    /// <summary>
    /// 武器ボタン作成
    /// </summary>
    private void CreateWeaponButtons()
    {
        var weaponPanelObj = new GameObject("WeaponPanel");
        weaponPanelObj.transform.SetParent(battleCanvas.transform, false);

        var weaponPanelRect = weaponPanelObj.AddComponent<RectTransform>();
        weaponPanelRect.anchorMin = new Vector2(0.5f, 0f);
        weaponPanelRect.anchorMax = new Vector2(0.5f, 0f);
        weaponPanelRect.sizeDelta = new Vector2(800, 120);
        weaponPanelRect.anchoredPosition = new Vector2(0, 80);

        // 横方向レイアウト
        var layoutGroup = weaponPanelObj.AddComponent<HorizontalLayoutGroup>();
        layoutGroup.spacing = 20f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;

        // 武器ボタン作成（4個）
        weaponButtons = new Button[4];
        string[] weaponNames = { "炎の剣", "氷の斧", "雷槍", "大剣" };
        int[] weaponAttacks = { 95, 110, 85, 120 };

        for (int i = 0; i < 4; i++)
        {
            weaponButtons[i] = CreateWeaponButton(weaponPanelObj.transform, weaponNames[i], weaponAttacks[i], i);
        }
    }

    /// <summary>
    /// 個別武器ボタン作成
    /// </summary>
    private Button CreateWeaponButton(Transform parent, string weaponName, int attack, int index)
    {
        var buttonObj = new GameObject($"WeaponButton_{index}");
        buttonObj.transform.SetParent(parent, false);

        var layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 100;
        layoutElement.preferredWidth = 180;

        var button = buttonObj.AddComponent<Button>();
        var image = buttonObj.AddComponent<Image>();

        // サイバーパンク風ボタンスタイル
        image.color = new Color(0f, 0.8f, 0.8f, 0.3f);

        // ボタンテキスト
        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(buttonObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var buttonText = textObj.AddComponent<Text>();
        buttonText.text = $"{weaponName}\nATK: {attack}";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;

        button.targetGraphic = image;
        button.onClick.AddListener(() => OnWeaponButtonClick(weaponName, attack, index));

        // ホバー効果
        var colors = button.colors;
        colors.normalColor = new Color(0f, 0.8f, 0.8f, 0.3f);
        colors.highlightedColor = new Color(0f, 1f, 1f, 0.5f);
        colors.pressedColor = new Color(0f, 1f, 1f, 0.7f);
        colors.fadeDuration = 0.3f;
        button.colors = colors;

        return button;
    }

    /// <summary>
    /// タイトルに戻るボタン作成
    /// </summary>
    private void CreateBackToTitleButton()
    {
        var buttonObj = new GameObject("BackToTitleButton");
        buttonObj.transform.SetParent(battleCanvas.transform, false);

        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 0f);
        buttonRect.anchorMax = new Vector2(0f, 0f);
        buttonRect.sizeDelta = new Vector2(200, 50);
        buttonRect.anchoredPosition = new Vector2(20, 20);

        backToTitleButton = buttonObj.AddComponent<Button>();
        var image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);

        // ボタンテキスト
        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(buttonObj.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        var buttonText = textObj.AddComponent<Text>();
        buttonText.text = "BACK TO TITLE";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;

        backToTitleButton.targetGraphic = image;
        backToTitleButton.onClick.AddListener(OnBackToTitleClick);
    }

    /// <summary>
    /// 武器ボタンクリック処理
    /// </summary>
    private void OnWeaponButtonClick(string weaponName, int attack, int index)
    {
        Debug.Log($"[BattleTestUI] {weaponName} (ATK: {attack}) を使用！");
        
        // テスト用ダメージ計算
        int testDamage = Random.Range(attack - 20, attack + 50);
        
        // ゲームステータス更新
        UpdateGameStatus($"{weaponName} used!\nDamage: {testDamage}\nTesting combat system...");
        
        // 戦闘システムとの連携テスト
        if (battleManager != null)
        {
            Debug.Log($"[BattleTestUI] BattleManagerに武器使用を通知: {weaponName}");
            // TODO: BattleManager.UseWeapon(index) のような実際の戦闘処理呼び出し
        }
    }

    /// <summary>
    /// タイトルに戻るボタンクリック処理
    /// </summary>
    private void OnBackToTitleClick()
    {
        Debug.Log("[BattleTestUI] タイトル画面に戻る");
        
        if (gameStateManager != null)
        {
            gameStateManager.GoToTitleScreen();
        }
        else
        {
            // GameStateManagerがない場合の代替処理
            Destroy(battleCanvas.gameObject);
            var gameInitializer = FindObjectOfType<GameInitializer>();
            if (gameInitializer != null)
            {
                gameInitializer.InitializeGame();
            }
        }
    }

    /// <summary>
    /// ゲームステータステキスト更新
    /// </summary>
    private void UpdateGameStatus(string message)
    {
        if (gameStatusText != null)
        {
            gameStatusText.text = message;
        }
    }

    /// <summary>
    /// プレイヤーHP更新
    /// </summary>
    public void UpdatePlayerHP(int currentHP, int maxHP)
    {
        if (playerHPText != null)
        {
            playerHPText.text = $"Player HP: {currentHP}/{maxHP}";
            
            // HP色変更
            float hpRatio = (float)currentHP / maxHP;
            if (hpRatio > 0.6f)
                playerHPText.color = Color.green;
            else if (hpRatio > 0.3f)
                playerHPText.color = Color.yellow;
            else
                playerHPText.color = Color.red;
        }
    }

    /// <summary>
    /// ターンカウンター更新
    /// </summary>
    public void UpdateTurnCounter(int turnNumber)
    {
        if (turnCounterText != null)
        {
            turnCounterText.text = $"Turn: {turnNumber}";
        }
    }
}