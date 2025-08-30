using UnityEngine;
using BattleSystem;

/// <summary>
/// ゲーム状態管理システム
/// タイトル画面、戦闘画面、結果画面などの切り替えを管理
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public enum GameState
    {
        TitleScreen,    // タイトル画面
        BattleScreen,   // 戦闘画面
        ResultScreen,   // 結果画面
        InventoryScreen // インベントリ画面
    }

    [Header("現在のゲーム状態")]
    [SerializeField] private GameState currentState = GameState.TitleScreen;

    [Header("UI参照")]
    [SerializeField] private GameObject titleScreenUI;
    [SerializeField] private GameObject battleScreenUI;
    [SerializeField] private GameObject resultScreenUI;
    [SerializeField] private GameObject inventoryScreenUI;

    [Header("戦闘システム参照")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleFlowManager battleFlowManager;

    public GameState CurrentState => currentState;
    public System.Action<GameState> OnStateChanged;

    private void Awake()
    {
        // シングルトンパターン
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 自動でタイトル画面に遷移
        ChangeState(GameState.TitleScreen);
    }

    /// <summary>
    /// ゲーム状態の変更
    /// </summary>
    public void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"[GameStateManager] 状態変更: {currentState} -> {newState}");

        // 現在の状態を終了
        ExitCurrentState();

        // 新しい状態に変更
        currentState = newState;
        EnterNewState();

        // 状態変更イベント発火
        OnStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// 現在の状態を終了
    /// </summary>
    private void ExitCurrentState()
    {
        switch (currentState)
        {
            case GameState.TitleScreen:
                if (titleScreenUI != null) titleScreenUI.SetActive(false);
                break;
            case GameState.BattleScreen:
                if (battleScreenUI != null) battleScreenUI.SetActive(false);
                EndBattle();
                break;
            case GameState.ResultScreen:
                if (resultScreenUI != null) resultScreenUI.SetActive(false);
                break;
            case GameState.InventoryScreen:
                if (inventoryScreenUI != null) inventoryScreenUI.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// 新しい状態を開始
    /// </summary>
    private void EnterNewState()
    {
        switch (currentState)
        {
            case GameState.TitleScreen:
                ShowTitleScreen();
                break;
            case GameState.BattleScreen:
                ShowBattleScreen();
                StartBattleTest();
                break;
            case GameState.ResultScreen:
                ShowResultScreen();
                break;
            case GameState.InventoryScreen:
                ShowInventoryScreen();
                break;
        }
    }

    /// <summary>
    /// タイトル画面表示
    /// </summary>
    private void ShowTitleScreen()
    {
        if (titleScreenUI != null)
        {
            titleScreenUI.SetActive(true);
        }
        else
        {
            // SimpleTitleTestがない場合は作成
            CreateTitleScreen();
        }
    }

    /// <summary>
    /// タイトル画面作成
    /// </summary>
    private void CreateTitleScreen()
    {
        var simpleTitleTest = FindObjectOfType<SimpleTitleTest>();
        if (simpleTitleTest == null)
        {
            var titleObject = new GameObject("SimpleTitleTest");
            simpleTitleTest = titleObject.AddComponent<SimpleTitleTest>();
            Debug.Log("[GameStateManager] SimpleTitleTestを作成しました");
        }
    }

    /// <summary>
    /// 戦闘画面表示
    /// </summary>
    private void ShowBattleScreen()
    {
        if (battleScreenUI != null)
        {
            battleScreenUI.SetActive(true);
        }
        else
        {
            // 戦闘UIがない場合は作成
            CreateBattleScreen();
        }
    }

    /// <summary>
    /// 戦闘画面作成
    /// </summary>
    private void CreateBattleScreen()
    {
        var battleTestUI = FindObjectOfType<BattleTestUI>();
        if (battleTestUI == null)
        {
            var battleObject = new GameObject("BattleTestUI");
            battleTestUI = battleObject.AddComponent<BattleTestUI>();
            Debug.Log("[GameStateManager] BattleTestUIを作成しました");
        }
    }

    /// <summary>
    /// 結果画面表示
    /// </summary>
    private void ShowResultScreen()
    {
        if (resultScreenUI != null)
        {
            resultScreenUI.SetActive(true);
        }
        Debug.Log("[GameStateManager] 結果画面を表示");
    }

    /// <summary>
    /// インベントリ画面表示
    /// </summary>
    private void ShowInventoryScreen()
    {
        if (inventoryScreenUI != null)
        {
            inventoryScreenUI.SetActive(true);
        }
        Debug.Log("[GameStateManager] インベントリ画面を表示");
    }

    /// <summary>
    /// 戦闘テスト開始
    /// </summary>
    private void StartBattleTest()
    {
        Debug.Log("[GameStateManager] 戦闘テスト開始");
        
        // BattleManagerとBattleFlowManagerを取得・初期化
        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();
            
        if (battleFlowManager == null)
            battleFlowManager = FindObjectOfType<BattleFlowManager>();

        // 戦闘システムの初期化と開始
        if (battleManager != null && battleFlowManager != null)
        {
            // テスト用の戦闘開始処理
            Debug.Log("[GameStateManager] 戦闘システム初期化完了");
        }
        else
        {
            Debug.LogWarning("[GameStateManager] 戦闘システムが見つかりません");
        }
    }

    /// <summary>
    /// 戦闘終了
    /// </summary>
    private void EndBattle()
    {
        Debug.Log("[GameStateManager] 戦闘終了");
    }

    // 外部から呼び出される状態遷移メソッド
    public void GoToTitleScreen() => ChangeState(GameState.TitleScreen);
    public void GoToBattleScreen() => ChangeState(GameState.BattleScreen);
    public void GoToResultScreen() => ChangeState(GameState.ResultScreen);
    public void GoToInventoryScreen() => ChangeState(GameState.InventoryScreen);

    // デバッグ用：エディターから状態変更
    [ContextMenu("Go To Title Screen")]
    public void Debug_GoToTitleScreen() => GoToTitleScreen();
    
    [ContextMenu("Go To Battle Screen")]
    public void Debug_GoToBattleScreen() => GoToBattleScreen();
    
    [ContextMenu("Go To Result Screen")]
    public void Debug_GoToResultScreen() => GoToResultScreen();
    
    [ContextMenu("Go To Inventory Screen")]  
    public void Debug_GoToInventoryScreen() => GoToInventoryScreen();
}