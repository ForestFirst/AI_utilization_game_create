using System;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// ゲーム全体のイベント管理システム
    /// 画面間のイベント通信、データ連携を統括
    /// </summary>
    public class GameEventManager : MonoBehaviour
    {
        [Header("デバッグ設定")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private bool logAllEvents = false;
        
        // シングルトン
        public static GameEventManager Instance { get; private set; }
        
        #region Scene Transition Events
        
        /// <summary>
        /// シーン遷移要求イベント
        /// </summary>
        public static event Action<string, object> OnSceneTransitionRequested;
        
        /// <summary>
        /// シーン読み込み完了イベント
        /// </summary>
        public static event Action<string> OnSceneLoaded;
        
        /// <summary>
        /// シーン遷移開始イベント
        /// </summary>
        public static event Action<string> OnSceneTransitionStarted;
        
        /// <summary>
        /// シーン遷移完了イベント
        /// </summary>
        public static event Action<string> OnSceneTransitionCompleted;
        
        #endregion
        
        #region Player Data Events
        
        /// <summary>
        /// プレイヤーデータ更新イベント
        /// </summary>
        public static event Action<PlayerData> OnPlayerDataUpdated;
        
        /// <summary>
        /// レベルアップイベント
        /// </summary>
        public static event Action<int, int> OnPlayerLevelUp; // oldLevel, newLevel
        
        /// <summary>
        /// ゴールド変更イベント
        /// </summary>
        public static event Action<int, int> OnGoldChanged; // oldGold, newGold
        
        /// <summary>
        /// HP変更イベント
        /// </summary>
        public static event Action<int, int, int> OnHPChanged; // current, max, change
        
        #endregion
        
        #region Stage Events
        
        /// <summary>
        /// ステージ選択イベント
        /// </summary>
        public static event Action<StageData> OnStageSelected;
        
        /// <summary>
        /// ステージ開始イベント
        /// </summary>
        public static event Action<StageData> OnStageStarted;
        
        /// <summary>
        /// ステージ完了イベント
        /// </summary>
        public static event Action<StageData, bool, StageResult> OnStageCompleted; // stage, success, result
        
        /// <summary>
        /// ステージ進行状況更新イベント
        /// </summary>
        public static event Action OnStageProgressUpdated;
        
        /// <summary>
        /// ステージ解放イベント
        /// </summary>
        public static event Action<string> OnStageUnlocked; // stageId
        
        #endregion
        
        #region Battle Events
        
        /// <summary>
        /// 戦闘開始イベント
        /// </summary>
        public static event Action<BattleStartData> OnBattleStarted;
        
        /// <summary>
        /// 戦闘終了イベント
        /// </summary>
        public static event Action<bool, BattleResult> OnBattleCompleted; // victory, result
        
        /// <summary>
        /// ターン開始イベント
        /// </summary>
        public static event Action<int> OnTurnStarted; // turnNumber
        
        /// <summary>
        /// プレイヤーアクションイベント
        /// </summary>
        public static event Action<PlayerAction> OnPlayerAction;
        
        /// <summary>
        /// 敵撃破イベント
        /// </summary>
        public static event Action<EnemyInstance> OnEnemyDefeated;
        
        #endregion
        
        #region UI Events
        
        /// <summary>
        /// UI画面表示イベント
        /// </summary>
        public static event Action<string> OnUIScreenShown; // screenName
        
        /// <summary>
        /// UI画面非表示イベント
        /// </summary>
        public static event Action<string> OnUIScreenHidden; // screenName
        
        /// <summary>
        /// モーダルダイアログ表示要求イベント
        /// </summary>
        public static event Action<ModalDialogData> OnModalDialogRequested;
        
        /// <summary>
        /// 通知メッセージ表示要求イベント
        /// </summary>
        public static event Action<NotificationData> OnNotificationRequested;
        
        #endregion
        
        #region Item and Inventory Events
        
        /// <summary>
        /// アイテム獲得イベント
        /// </summary>
        public static event Action<string, int> OnItemObtained; // itemId, quantity
        
        /// <summary>
        /// アイテム消費イベント
        /// </summary>
        public static event Action<string, int> OnItemConsumed; // itemId, quantity
        
        /// <summary>
        /// インベントリ更新イベント
        /// </summary>
        public static event Action OnInventoryUpdated;
        
        /// <summary>
        /// 装備変更イベント
        /// </summary>
        public static event Action<EquipmentChangeData> OnEquipmentChanged;
        
        #endregion
        
        #region System Events
        
        /// <summary>
        /// ゲーム開始イベント
        /// </summary>
        public static event Action OnGameStarted;
        
        /// <summary>
        /// ゲーム終了イベント
        /// </summary>
        public static event Action OnGameEnded;
        
        /// <summary>
        /// ゲーム一時停止イベント
        /// </summary>
        public static event Action<bool> OnGamePaused; // isPaused
        
        /// <summary>
        /// 設定変更イベント
        /// </summary>
        public static event Action<PlayerGameSettings> OnSettingsChanged;
        
        /// <summary>
        /// エラー発生イベント
        /// </summary>
        public static event Action<string, Exception> OnErrorOccurred; // message, exception
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // シングルトン設定
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEventManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            // すべてのイベントをクリア
            ClearAllEvents();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// イベントマネージャーの初期化
        /// </summary>
        private void InitializeEventManager()
        {
            LogDebug("GameEventManager initialized");
            
            // 基本的なイベント購読の設定
            SetupCoreEventSubscriptions();
        }
        
        /// <summary>
        /// 基本イベント購読の設定
        /// </summary>
        private void SetupCoreEventSubscriptions()
        {
            // デバッグモード時のイベントログ出力
            if (debugMode && logAllEvents)
            {
                SubscribeToAllEventsForLogging();
            }
        }
        
        #endregion
        
        #region Event Trigger Methods
        
        /// <summary>
        /// シーン遷移要求を発火
        /// </summary>
        public static void RequestSceneTransition(string sceneName, object data = null)
        {
            LogEvent($"Scene transition requested: {sceneName}");
            OnSceneTransitionRequested?.Invoke(sceneName, data);
        }
        
        /// <summary>
        /// プレイヤーデータ更新を発火
        /// </summary>
        public static void TriggerPlayerDataUpdate(PlayerData playerData)
        {
            LogEvent($"Player data updated: Level {playerData.level}");
            OnPlayerDataUpdated?.Invoke(playerData);
        }
        
        /// <summary>
        /// ステージ選択を発火
        /// </summary>
        public static void TriggerStageSelection(StageData stageData)
        {
            LogEvent($"Stage selected: {stageData.stageName}");
            OnStageSelected?.Invoke(stageData);
        }
        
        /// <summary>
        /// 戦闘開始を発火
        /// </summary>
        public static void TriggerBattleStart(BattleStartData battleData)
        {
            LogEvent($"Battle started: {battleData.stageName}");
            OnBattleStarted?.Invoke(battleData);
        }
        
        /// <summary>
        /// 戦闘終了を発火
        /// </summary>
        public static void TriggerBattleComplete(bool victory, BattleResult result)
        {
            LogEvent($"Battle completed: Victory={victory}");
            OnBattleCompleted?.Invoke(victory, result);
        }
        
        /// <summary>
        /// UI画面表示を発火
        /// </summary>
        public static void TriggerUIScreenShow(string screenName)
        {
            LogEvent($"UI screen shown: {screenName}");
            OnUIScreenShown?.Invoke(screenName);
        }
        
        /// <summary>
        /// モーダルダイアログ表示要求を発火
        /// </summary>
        public static void RequestModalDialog(string title, string message, Action onConfirm = null, Action onCancel = null)
        {
            var dialogData = new ModalDialogData
            {
                title = title,
                message = message,
                onConfirm = onConfirm,
                onCancel = onCancel
            };
            
            LogEvent($"Modal dialog requested: {title}");
            OnModalDialogRequested?.Invoke(dialogData);
        }
        
        /// <summary>
        /// 通知メッセージ表示要求を発火
        /// </summary>
        public static void RequestNotification(string message, NotificationType type = NotificationType.Info, float duration = 3f)
        {
            var notificationData = new NotificationData
            {
                message = message,
                type = type,
                duration = duration,
                timestamp = DateTime.Now
            };
            
            LogEvent($"Notification requested: {message}");
            OnNotificationRequested?.Invoke(notificationData);
        }
        
        /// <summary>
        /// エラー発生を発火
        /// </summary>
        public static void TriggerError(string message, Exception exception = null)
        {
            LogEvent($"Error occurred: {message}");
            OnErrorOccurred?.Invoke(message, exception);
        }
        
        #endregion
        
        #region Event Management
        
        /// <summary>
        /// すべてのイベントをクリア
        /// </summary>
        private void ClearAllEvents()
        {
            // Scene Transition Events
            OnSceneTransitionRequested = null;
            OnSceneLoaded = null;
            OnSceneTransitionStarted = null;
            OnSceneTransitionCompleted = null;
            
            // Player Data Events
            OnPlayerDataUpdated = null;
            OnPlayerLevelUp = null;
            OnGoldChanged = null;
            OnHPChanged = null;
            
            // Stage Events
            OnStageSelected = null;
            OnStageStarted = null;
            OnStageCompleted = null;
            OnStageProgressUpdated = null;
            OnStageUnlocked = null;
            
            // Battle Events
            OnBattleStarted = null;
            OnBattleCompleted = null;
            OnTurnStarted = null;
            OnPlayerAction = null;
            OnEnemyDefeated = null;
            
            // UI Events
            OnUIScreenShown = null;
            OnUIScreenHidden = null;
            OnModalDialogRequested = null;
            OnNotificationRequested = null;
            
            // Item Events
            OnItemObtained = null;
            OnItemConsumed = null;
            OnInventoryUpdated = null;
            OnEquipmentChanged = null;
            
            // System Events
            OnGameStarted = null;
            OnGameEnded = null;
            OnGamePaused = null;
            OnSettingsChanged = null;
            OnErrorOccurred = null;
            
            LogDebug("All events cleared");
        }
        
        /// <summary>
        /// デバッグ用：すべてのイベントにログ出力を購読
        /// </summary>
        private void SubscribeToAllEventsForLogging()
        {
            OnSceneTransitionRequested += (scene, data) => LogEvent($"[EVENT] Scene Transition: {scene}");
            OnPlayerDataUpdated += (data) => LogEvent($"[EVENT] Player Data Updated: Level {data.level}");
            OnStageSelected += (stage) => LogEvent($"[EVENT] Stage Selected: {stage.stageName}");
            OnBattleCompleted += (victory, result) => LogEvent($"[EVENT] Battle Completed: Victory={victory}");
            OnUIScreenShown += (screen) => LogEvent($"[EVENT] UI Screen Shown: {screen}");
            OnErrorOccurred += (message, ex) => LogEvent($"[EVENT] Error: {message}");
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// デバッグログ出力
        /// </summary>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[GameEventManager] {message}");
            }
        }
        
        /// <summary>
        /// イベントログ出力（静的メソッド）
        /// </summary>
        private static void LogEvent(string message)
        {
            if (Instance != null && Instance.debugMode)
            {
                Debug.Log($"[GameEventManager] {message}");
            }
        }
        
        /// <summary>
        /// イベント購読者数を取得（デバッグ用）
        /// </summary>
        public int GetEventSubscriberCount(string eventName)
        {
            // リフレクションを使用してイベント購読者数を取得
            // 実装は複雑になるため、基本的な例のみ
            switch (eventName)
            {
                case nameof(OnSceneTransitionRequested):
                    return OnSceneTransitionRequested?.GetInvocationList().Length ?? 0;
                case nameof(OnPlayerDataUpdated):
                    return OnPlayerDataUpdated?.GetInvocationList().Length ?? 0;
                default:
                    return 0;
            }
        }
        
        #endregion
        
        #region Debug Methods
        
        /// <summary>
        /// イベント統計情報を表示（デバッグ用）
        /// </summary>
        [ContextMenu("Show Event Statistics")]
        public void ShowEventStatistics()
        {
            Debug.Log("=== Event Statistics ===");
            Debug.Log($"Scene Transition Subscribers: {GetEventSubscriberCount(nameof(OnSceneTransitionRequested))}");
            Debug.Log($"Player Data Subscribers: {GetEventSubscriberCount(nameof(OnPlayerDataUpdated))}");
            Debug.Log($"Debug Mode: {debugMode}");
            Debug.Log($"Log All Events: {logAllEvents}");
        }
        
        /// <summary>
        /// テスト通知を送信
        /// </summary>
        [ContextMenu("Send Test Notification")]
        public void SendTestNotification()
        {
            RequestNotification("テスト通知メッセージ", NotificationType.Info, 3f);
        }
        
        /// <summary>
        /// テストモーダルを表示
        /// </summary>
        [ContextMenu("Show Test Modal")]
        public void ShowTestModal()
        {
            RequestModalDialog(
                "テストダイアログ", 
                "これはテスト用のモーダルダイアログです。",
                () => Debug.Log("Confirm pressed"),
                () => Debug.Log("Cancel pressed")
            );
        }
        
        #endregion
    }
    
    #region Event Data Classes
    
    /// <summary>
    /// 戦闘開始データ
    /// </summary>
    [Serializable]
    public class BattleStartData
    {
        public string stageId;
        public string stageName;
        public StageData stageData;
        public PlayerData playerData;
        public DateTime startTime;
    }
    
    /// <summary>
    /// 戦闘結果データ
    /// </summary>
    [Serializable]
    public class BattleResult
    {
        public bool victory;
        public int turnsTaken;
        public float timeTaken;
        public int damageDealt;
        public int damageTaken;
        public int experienceGained;
        public int goldGained;
        public List<string> itemsObtained;
        public DateTime completionTime;
    }
    
    /// <summary>
    /// ステージ結果データ
    /// </summary>
    [Serializable]
    public class StageResult
    {
        public string stageId;
        public bool cleared;
        public int score;
        public int stars; // 1-3 stars rating
        public BattleResult battleResult;
        public bool isFirstClear;
    }
    
    /// <summary>
    /// プレイヤーアクションデータ
    /// </summary>
    [Serializable]
    public class PlayerAction
    {
        public string actionType; // "attack", "skill", "item"
        public string actionId;
        public EnemyInstance target;
        public int damage;
        public DateTime timestamp;
    }
    
    /// <summary>
    /// 装備変更データ
    /// </summary>
    [Serializable]
    public class EquipmentChangeData
    {
        public string slotType; // "weapon", "armor", "accessory"
        public string oldItemId;
        public string newItemId;
        public DateTime changeTime;
    }
    
    /// <summary>
    /// モーダルダイアログデータ
    /// </summary>
    [Serializable]
    public class ModalDialogData
    {
        public string title;
        public string message;
        public string confirmButtonText = "OK";
        public string cancelButtonText = "キャンセル";
        public Action onConfirm;
        public Action onCancel;
        public bool showCancelButton = true;
    }
    
    /// <summary>
    /// 通知データ
    /// </summary>
    [Serializable]
    public class NotificationData
    {
        public string message;
        public NotificationType type;
        public float duration;
        public DateTime timestamp;
    }
    
    /// <summary>
    /// 通知タイプ
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
    
    #endregion
}