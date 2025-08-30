using UnityEngine;
using BattleSystem;

/// <summary>
/// ゲーム初期化用スクリプト（ランタイム版）
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
        
        // GameStateManagerの初期化
        InitializeGameStateManager();
        
        // ゲームシステムの初期化
        InitializeGameSystems();
        
        Debug.Log("[GameInitializer] ゲーム初期化完了");
    }
    
    /// <summary>
    /// GameStateManagerの初期化
    /// </summary>
    private void InitializeGameStateManager()
    {
        // GameStateManagerの確認・作成
        var gameStateManager = GameStateManager.Instance;
        if (gameStateManager == null)
        {
            var gameStateObject = new GameObject("GameStateManager");
            gameStateManager = gameStateObject.AddComponent<GameStateManager>();
            Debug.Log("[GameInitializer] GameStateManagerを作成しました");
        }
        else
        {
            Debug.Log("[GameInitializer] GameStateManagerが既に存在します");
        }
        
        // タイトル画面の表示を指示（GameStateManagerが自動で処理）
        if (showTitleScreen)
        {
            Debug.Log("[GameInitializer] タイトル画面表示を開始");
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