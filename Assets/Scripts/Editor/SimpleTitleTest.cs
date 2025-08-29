using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// シンプルなタイトル画面テスト用スクリプト
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
    /// テストボタン作成
    /// </summary>
    private void CreateTestButton()
    {
        var buttonObj = new GameObject("TestButton");
        buttonObj.transform.SetParent(titleCanvas.transform, false);
        
        var buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.4f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
        buttonRect.sizeDelta = new Vector2(200, 60);
        buttonRect.anchoredPosition = Vector2.zero;
        
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
        buttonText.text = "GAME START";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 18;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => {
            Debug.Log("[SimpleTitleTest] ゲーム開始ボタンが押されました！");
        });
        
        // 最初のボタンを選択
        button.Select();
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