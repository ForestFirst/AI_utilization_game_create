using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// シンプルなタイトル画面テスト用スクリプト（ランタイム版）
/// </summary>
public class SimpleTitleTest : MonoBehaviour
{
    private Canvas titleCanvas;
    private Text titleText;
    
    private void Start()
    {
        Debug.Log("[SimpleTitleTest] タイトル画面テスト開始");
        CreateSimpleTitleScreen();
    }
    
    /// <summary>
    /// シンプルなタイトル画面を作成
    /// </summary>
    private void CreateSimpleTitleScreen()
    {
        // EventSystemの確認・作成
        EnsureEventSystem();
        
        // Canvas作成
        CreateCanvas();
        
        // タイトルテキスト作成
        CreateTitleText();
        
        // テストボタン作成
        CreateTestButton();
        
        Debug.Log("[SimpleTitleTest] タイトル画面作成完了");
    }
    
    /// <summary>
    /// EventSystemの確認・作成
    /// </summary>
    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("[SimpleTitleTest] EventSystemを作成しました");
        }
    }
    
    /// <summary>
    /// Canvas作成
    /// </summary>
    private void CreateCanvas()
    {
        var canvasObj = new GameObject("TitleCanvas");
        titleCanvas = canvasObj.AddComponent<Canvas>();
        titleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        titleCanvas.sortingOrder = 100;
        
        var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // 背景色設定
        var backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(titleCanvas.transform, false);
        
        var backgroundRect = backgroundObj.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        backgroundRect.anchoredPosition = Vector2.zero;
        
        var backgroundImg = backgroundObj.AddComponent<Image>();
        backgroundImg.color = new Color(0.1f, 0.1f, 0.2f, 1f); // ダークブルー
    }
    
    /// <summary>
    /// タイトルテキスト作成
    /// </summary>
    private void CreateTitleText()
    {
        var titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(titleCanvas.transform, false);
        
        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.7f);
        titleRect.anchorMax = new Vector2(0.5f, 0.7f);
        titleRect.sizeDelta = new Vector2(800, 100);
        titleRect.anchoredPosition = Vector2.zero;
        
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "GUARDIAN PROTOCOL";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 48;
        titleText.color = Color.cyan;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
    }
    
    /// <summary>
    /// メニューボタン作成
    /// </summary>
    private void CreateTestButton()
    {
        var buttonPanelObj = new GameObject("ButtonPanel");
        buttonPanelObj.transform.SetParent(titleCanvas.transform, false);
        
        var panelRect = buttonPanelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.3f);
        panelRect.anchorMax = new Vector2(0.5f, 0.3f);
        panelRect.sizeDelta = new Vector2(300, 200);
        panelRect.anchoredPosition = Vector2.zero;
        
        // 縦方向レイアウト
        var layoutGroup = buttonPanelObj.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 15f;
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        
        // 戦闘テストボタン
        CreateMenuButton(buttonPanelObj.transform, "BATTLE TEST", OnBattleTestClick);
        
        // 通常ゲーム開始ボタン
        CreateMenuButton(buttonPanelObj.transform, "GAME START", OnGameStartClick);
        
        // 設定ボタン
        CreateMenuButton(buttonPanelObj.transform, "SETTINGS", OnSettingsClick);
    }
    
    /// <summary>
    /// 個別メニューボタン作成
    /// </summary>
    private Button CreateMenuButton(Transform parent, string text, System.Action action)
    {
        var buttonObj = new GameObject($"MenuButton_{text}");
        buttonObj.transform.SetParent(parent, false);
        
        var layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 50;
        layoutElement.minHeight = 50;
        
        var button = buttonObj.AddComponent<Button>();
        var buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0f, 0.8f, 0.8f, 0.3f);
        
        // ボタンテキスト
        var textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        var buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => action?.Invoke());
        
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
    /// 戦闘テストボタンクリック処理
    /// </summary>
    private void OnBattleTestClick()
    {
        Debug.Log("[SimpleTitleTest] 戦闘テスト開始！");
        
        var gameStateManager = GameStateManager.Instance;
        if (gameStateManager != null)
        {
            // GameStateManagerを使用して戦闘画面に遷移
            gameStateManager.GoToBattleScreen();
        }
        else
        {
            Debug.LogWarning("[SimpleTitleTest] GameStateManagerが見つかりません。直接戦闘テストUIを作成します。");
            CreateBattleTestDirectly();
        }
    }
    
    /// <summary>
    /// ゲーム開始ボタンクリック処理
    /// </summary>
    private void OnGameStartClick()
    {
        Debug.Log("[SimpleTitleTest] 通常ゲーム開始（未実装）");
        // TODO: ステージ選択画面へ遷移する処理を実装
    }
    
    /// <summary>
    /// 設定ボタンクリック処理
    /// </summary>
    private void OnSettingsClick()
    {
        Debug.Log("[SimpleTitleTest] 設定画面（未実装）");
        // TODO: 設定画面への遷移を実装
    }
    
    /// <summary>
    /// 直接戦闘テストUI作成（GameStateManager不使用時の代替処理）
    /// </summary>
    private void CreateBattleTestDirectly()
    {
        // タイトル画面を非表示
        if (titleCanvas != null)
        {
            titleCanvas.gameObject.SetActive(false);
        }
        
        // 戦闘テストUIを作成
        var battleTestObj = new GameObject("BattleTestUI");
        battleTestObj.AddComponent<BattleTestUI>();
    }
    
    private void Update()
    {
        // タイトルテキストの点滅効果
        if (titleText != null)
        {
            float alpha = 0.7f + 0.3f * Mathf.Sin(Time.time * 2f);
            titleText.color = new Color(0f, 1f, 1f, alpha);
        }
    }
}