using UnityEngine;
using UnityEditor;
using BattleSystem;
using BattleSystem.UI;

/// <summary>
/// ゲーム初期化用エディタースクリプト
/// OutdoorsSceneにゲーム開始に必要なコンポーネントを追加
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("ゲーム初期化設定")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private bool showTitleScreen = true;
    
    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeGame();
        }
    }
    
    /// <summary>
    /// ゲーム初期化処理
    /// </summary>
    public void InitializeGame()
    {
        Debug.Log("[GameInitializer] ゲーム初期化開始");
        
        // タイトル画面の初期化
        if (showTitleScreen)
        {
            InitializeTitleScreen();
        }
        
        // ゲームシステムの初期化
        InitializeGameSystems();
        
        Debug.Log("[GameInitializer] ゲーム初期化完了");
    }
    
    /// <summary>
    /// タイトル画面の初期化
    /// </summary>
    private void InitializeTitleScreen()
    {
        // TitleScreenUIがない場合は作成
        var titleScreenUI = FindObjectOfType<TitleScreenUI>();
        if (titleScreenUI == null)
        {
            var titleObject = new GameObject("TitleScreenManager");
            titleScreenUI = titleObject.AddComponent<TitleScreenUI>();
            Debug.Log("[GameInitializer] TitleScreenUIを作成しました");
        }
    }
    
    /// <summary>
    /// ゲームシステムの初期化
    /// </summary>
    private void InitializeGameSystems()
    {
        // BattleManagerの確認
        var battleManager = FindObjectOfType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogWarning("[GameInitializer] BattleManagerが見つかりません");
        }
        else
        {
            Debug.Log("[GameInitializer] BattleManagerが確認されました");
        }
        
        // AttachmentSystemの確認
        var attachmentSystem = FindObjectOfType<AttachmentSystem>();
        if (attachmentSystem == null)
        {
            Debug.LogWarning("[GameInitializer] AttachmentSystemが見つかりません");
        }
        else
        {
            Debug.Log("[GameInitializer] AttachmentSystemが確認されました");
        }
    }
    
    /// <summary>
    /// エディター用：手動初期化
    /// </summary>
    [ContextMenu("Initialize Game")]
    public void ManualInitialize()
    {
        InitializeGame();
    }
}

#if UNITY_EDITOR
/// <summary>
/// エディター用ヘルパー
/// </summary>
public class GameInitializerEditor
{
    [MenuItem("GameObject/AI Game/Add Game Initializer", false, 0)]
    static void AddGameInitializer()
    {
        var gameObject = new GameObject("GameInitializer");
        gameObject.AddComponent<GameInitializer>();
        Selection.activeGameObject = gameObject;
        
        EditorGUIUtility.PingObject(gameObject);
        Debug.Log("GameInitializerを追加しました");
    }
    
    [MenuItem("AI Game/Initialize Current Scene")]
    static void InitializeCurrentScene()
    {
        var initializer = Object.FindObjectOfType<GameInitializer>();
        if (initializer == null)
        {
            AddGameInitializer();
            initializer = Object.FindObjectOfType<GameInitializer>();
        }
        
        if (initializer != null)
        {
            initializer.InitializeGame();
        }
    }
}
#endif