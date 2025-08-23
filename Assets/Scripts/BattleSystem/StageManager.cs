using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// ステージ管理システム
    /// ステージデータの管理、進行状況の保存、ステージ選択機能を提供
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [Header("ステージデータベース")]
        [SerializeField] private List<StageData> allStages = new List<StageData>();
        [SerializeField] private bool autoLoadStagesFromResources = true;
        [SerializeField] private string stageResourcesPath = "Stages";
        
        [Header("現在のステージ")]
        [SerializeField] private StageData currentStage;
        [SerializeField] private bool debugMode = false;
        
        // ステージ進行状況管理
        private Dictionary<string, StageProgress> stageProgressMap;
        private string saveDataKey = "StageProgressData";
        
        // イベント定義
        public event Action<StageData> OnStageSelected;         // ステージ選択時
        public event Action<StageData> OnStageStarted;          // ステージ開始時
        public event Action<StageData, bool> OnStageCompleted;  // ステージ完了時（成功/失敗）
        public event Action<StageData> OnStageUnlocked;         // ステージ解放時
        public event Action OnStageProgressChanged;             // 進行状況変更時

        // シングルトンパターン
        public static StageManager Instance { get; private set; }

        // プロパティ
        public StageData CurrentStage => currentStage;
        public List<StageData> AllStages => allStages.ToList(); // コピーを返す
        public int TotalStageCount => allStages.Count;
        public int ClearedStageCount => stageProgressMap?.Values.Count(p => p.isCleared) ?? 0;

        #region Unity Lifecycle

        private void Awake()
        {
            // シングルトン設定
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeStageManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // ステージデータの自動読み込み
            if (autoLoadStagesFromResources)
            {
                LoadStagesFromResources();
            }
            
            // 進行状況データの読み込み
            LoadStageProgress();
            
            // 初期ステージの解放チェック
            CheckInitialStageUnlocks();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ステージマネージャーの初期化
        /// </summary>
        private void InitializeStageManager()
        {
            stageProgressMap = new Dictionary<string, StageProgress>();
            LogDebug("StageManager initialized");
        }

        /// <summary>
        /// Resourcesフォルダからステージデータを読み込み
        /// </summary>
        private void LoadStagesFromResources()
        {
            try
            {
                var stageAssets = Resources.LoadAll<StageData>(stageResourcesPath);
                
                foreach (var stage in stageAssets)
                {
                    if (!allStages.Contains(stage))
                    {
                        allStages.Add(stage);
                    }
                }
                
                // ステージIDでソート
                allStages.Sort((a, b) => string.Compare(a.stageId, b.stageId, StringComparison.Ordinal));
                
                LogDebug($"Loaded {stageAssets.Length} stages from Resources/{stageResourcesPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load stages from Resources: {ex.Message}");
            }
        }

        /// <summary>
        /// 初期ステージの解放チェック
        /// </summary>
        private void CheckInitialStageUnlocks()
        {
            var playerLevel = GetCurrentPlayerLevel();
            var clearedStageIds = GetClearedStageIds();
            
            foreach (var stage in allStages)
            {
                if (!stage.isUnlocked && stage.CanUnlock(playerLevel, clearedStageIds))
                {
                    UnlockStage(stage.stageId);
                }
            }
        }

        #endregion

        #region Stage Management

        /// <summary>
        /// ステージを選択
        /// </summary>
        /// <param name="stageId">ステージID</param>
        /// <returns>選択成功の場合true</returns>
        public bool SelectStage(string stageId)
        {
            var stage = GetStageById(stageId);
            if (stage == null)
            {
                Debug.LogError($"Stage not found: {stageId}");
                return false;
            }

            if (!stage.isUnlocked)
            {
                Debug.LogWarning($"Stage is locked: {stageId}");
                return false;
            }

            currentStage = stage;
            OnStageSelected?.Invoke(stage);
            
            LogDebug($"Stage selected: {stage.stageName} ({stageId})");
            return true;
        }

        /// <summary>
        /// 現在のステージを開始
        /// </summary>
        /// <returns>開始成功の場合true</returns>
        public bool StartCurrentStage()
        {
            if (currentStage == null)
            {
                Debug.LogError("No stage selected");
                return false;
            }

            // スタミナチェック（実装時に追加）
            if (!HasEnoughStamina(currentStage.staminaCost))
            {
                Debug.LogWarning($"Not enough stamina for stage: {currentStage.stageId}");
                return false;
            }

            OnStageStarted?.Invoke(currentStage);
            LogDebug($"Stage started: {currentStage.stageName}");
            return true;
        }

        /// <summary>
        /// ステージ完了処理
        /// </summary>
        /// <param name="success">成功フラグ</param>
        /// <param name="clearTurns">クリアターン数</param>
        /// <param name="clearTime">クリア時間</param>
        public void CompleteStage(bool success, int clearTurns = 0, float clearTime = 0f)
        {
            if (currentStage == null)
            {
                Debug.LogError("No current stage to complete");
                return;
            }

            if (success)
            {
                // 進行状況更新
                var progress = GetOrCreateStageProgress(currentStage.stageId);
                bool isFirstClear = !progress.isCleared;
                
                progress.UpdateClearRecord(clearTurns, clearTime);
                
                // 報酬処理
                ProcessStageRewards(currentStage, isFirstClear);
                
                // 新しいステージの解放チェック
                CheckNewStageUnlocks();
                
                LogDebug($"Stage cleared: {currentStage.stageName} (Turns: {clearTurns}, Time: {clearTime:F1}s)");
            }
            else
            {
                LogDebug($"Stage failed: {currentStage.stageName}");
            }

            OnStageCompleted?.Invoke(currentStage, success);
            SaveStageProgress();
        }

        /// <summary>
        /// ステージを解放
        /// </summary>
        /// <param name="stageId">ステージID</param>
        public void UnlockStage(string stageId)
        {
            var stage = GetStageById(stageId);
            if (stage == null)
            {
                Debug.LogError($"Stage not found for unlock: {stageId}");
                return;
            }

            if (stage.isUnlocked)
            {
                LogDebug($"Stage already unlocked: {stageId}");
                return;
            }

            stage.isUnlocked = true;
            OnStageUnlocked?.Invoke(stage);
            
            LogDebug($"Stage unlocked: {stage.stageName}");
        }

        #endregion

        #region Data Access

        /// <summary>
        /// ステージIDからステージデータを取得
        /// </summary>
        /// <param name="stageId">ステージID</param>
        /// <returns>ステージデータ（見つからない場合null）</returns>
        public StageData GetStageById(string stageId)
        {
            return allStages.FirstOrDefault(stage => stage.stageId == stageId);
        }

        /// <summary>
        /// 難易度別ステージリストを取得
        /// </summary>
        /// <param name="difficulty">難易度</param>
        /// <returns>該当するステージリスト</returns>
        public List<StageData> GetStagesByDifficulty(StageDifficulty difficulty)
        {
            return allStages.Where(stage => stage.difficulty == difficulty).ToList();
        }

        /// <summary>
        /// ステージタイプ別ステージリストを取得
        /// </summary>
        /// <param name="stageType">ステージタイプ</param>
        /// <returns>該当するステージリスト</returns>
        public List<StageData> GetStagesByType(StageType stageType)
        {
            return allStages.Where(stage => stage.stageType == stageType).ToList();
        }

        /// <summary>
        /// 解放済みステージリストを取得
        /// </summary>
        /// <returns>解放済みステージリスト</returns>
        public List<StageData> GetUnlockedStages()
        {
            return allStages.Where(stage => stage.isUnlocked).ToList();
        }

        /// <summary>
        /// ステージ進行状況を取得
        /// </summary>
        /// <param name="stageId">ステージID</param>
        /// <returns>進行状況（見つからない場合新規作成）</returns>
        public StageProgress GetStageProgress(string stageId)
        {
            return GetOrCreateStageProgress(stageId);
        }

        #endregion

        #region Progress Management

        /// <summary>
        /// ステージ進行状況を取得または作成
        /// </summary>
        /// <param name="stageId">ステージID</param>
        /// <returns>進行状況</returns>
        private StageProgress GetOrCreateStageProgress(string stageId)
        {
            if (!stageProgressMap.ContainsKey(stageId))
            {
                stageProgressMap[stageId] = new StageProgress(stageId);
            }
            return stageProgressMap[stageId];
        }

        /// <summary>
        /// クリア済みステージIDリストを取得
        /// </summary>
        /// <returns>クリア済みステージIDリスト</returns>
        private List<string> GetClearedStageIds()
        {
            return stageProgressMap.Values
                .Where(progress => progress.isCleared)
                .Select(progress => progress.stageId)
                .ToList();
        }

        /// <summary>
        /// 新しいステージの解放をチェック
        /// </summary>
        private void CheckNewStageUnlocks()
        {
            var playerLevel = GetCurrentPlayerLevel();
            var clearedStageIds = GetClearedStageIds();
            
            foreach (var stage in allStages)
            {
                if (!stage.isUnlocked && stage.CanUnlock(playerLevel, clearedStageIds))
                {
                    UnlockStage(stage.stageId);
                }
            }
        }

        #endregion

        #region Reward Processing

        /// <summary>
        /// ステージ報酬を処理
        /// </summary>
        /// <param name="stage">クリアしたステージ</param>
        /// <param name="isFirstClear">初回クリアか</param>
        private void ProcessStageRewards(StageData stage, bool isFirstClear)
        {
            // 通常報酬の処理
            ProcessReward(stage.clearReward, "クリア報酬");
            
            // 初回クリアボーナスの処理
            if (isFirstClear && stage.firstClearBonus != null)
            {
                var progress = GetOrCreateStageProgress(stage.stageId);
                if (!progress.firstClearRewarded)
                {
                    ProcessReward(stage.firstClearBonus, "初回クリアボーナス");
                    progress.firstClearRewarded = true;
                }
            }
        }

        /// <summary>
        /// 報酬を処理
        /// </summary>
        /// <param name="reward">報酬データ</param>
        /// <param name="rewardType">報酬タイプ名</param>
        private void ProcessReward(StageReward reward, string rewardType)
        {
            if (reward == null) return;
            
            LogDebug($"{rewardType}を処理中:");
            
            // ゴールド報酬
            if (reward.goldReward > 0)
            {
                AddGold(reward.goldReward);
                LogDebug($"  ゴールド: +{reward.goldReward}");
            }
            
            // 経験値報酬
            if (reward.experienceReward > 0)
            {
                AddExperience(reward.experienceReward);
                LogDebug($"  経験値: +{reward.experienceReward}");
            }
            
            // アイテム報酬
            for (int i = 0; i < reward.itemRewards.Count && i < reward.itemQuantities.Count; i++)
            {
                AddItem(reward.itemRewards[i], reward.itemQuantities[i]);
                LogDebug($"  アイテム: {reward.itemRewards[i]} x{reward.itemQuantities[i]}");
            }
            
            // 武器報酬（確率）
            foreach (string weaponId in reward.weaponRewards)
            {
                if (UnityEngine.Random.value <= reward.weaponDropRate)
                {
                    AddWeapon(weaponId);
                    LogDebug($"  武器獲得: {weaponId}");
                }
            }
            
            // 特別報酬
            if (reward.hasSpecialReward && !string.IsNullOrEmpty(reward.specialRewardId))
            {
                ProcessSpecialReward(reward.specialRewardId, reward.specialRewardName);
                LogDebug($"  特別報酬: {reward.specialRewardName}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 現在のプレイヤーレベルを取得
        /// </summary>
        /// <returns>プレイヤーレベル</returns>
        private int GetCurrentPlayerLevel()
        {
            // TODO: PlayerDataシステムと連携
            return 1; // 仮実装
        }

        /// <summary>
        /// スタミナが十分かチェック
        /// </summary>
        /// <param name="requiredStamina">必要スタミナ</param>
        /// <returns>十分な場合true</returns>
        private bool HasEnoughStamina(int requiredStamina)
        {
            // TODO: スタミナシステムと連携
            return true; // 仮実装
        }

        /// <summary>
        /// ゴールドを追加
        /// </summary>
        /// <param name="amount">追加量</param>
        private void AddGold(int amount)
        {
            // TODO: 通貨システムと連携
            LogDebug($"ゴールド追加: {amount}");
        }

        /// <summary>
        /// 経験値を追加
        /// </summary>
        /// <param name="amount">追加量</param>
        private void AddExperience(int amount)
        {
            // TODO: 経験値システムと連携
            LogDebug($"経験値追加: {amount}");
        }

        /// <summary>
        /// アイテムを追加
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <param name="quantity">数量</param>
        private void AddItem(string itemId, int quantity)
        {
            // TODO: インベントリシステムと連携
            LogDebug($"アイテム追加: {itemId} x{quantity}");
        }

        /// <summary>
        /// 武器を追加
        /// </summary>
        /// <param name="weaponId">武器ID</param>
        private void AddWeapon(string weaponId)
        {
            // TODO: 武器システムと連携
            LogDebug($"武器追加: {weaponId}");
        }

        /// <summary>
        /// 特別報酬を処理
        /// </summary>
        /// <param name="rewardId">報酬ID</param>
        /// <param name="rewardName">報酬名</param>
        private void ProcessSpecialReward(string rewardId, string rewardName)
        {
            // TODO: 特別報酬システムと連携
            LogDebug($"特別報酬処理: {rewardName} ({rewardId})");
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// ステージ進行状況を保存
        /// </summary>
        private void SaveStageProgress()
        {
            try
            {
                var saveData = new StageProgressSaveData
                {
                    progressMap = stageProgressMap.Values.ToList(),
                    lastSaveTime = DateTime.Now
                };
                
                string json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(saveDataKey, json);
                PlayerPrefs.Save();
                
                OnStageProgressChanged?.Invoke();
                LogDebug("Stage progress saved");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save stage progress: {ex.Message}");
            }
        }

        /// <summary>
        /// ステージ進行状況を読み込み
        /// </summary>
        private void LoadStageProgress()
        {
            try
            {
                if (PlayerPrefs.HasKey(saveDataKey))
                {
                    string json = PlayerPrefs.GetString(saveDataKey);
                    var saveData = JsonUtility.FromJson<StageProgressSaveData>(json);
                    
                    stageProgressMap.Clear();
                    foreach (var progress in saveData.progressMap)
                    {
                        stageProgressMap[progress.stageId] = progress;
                    }
                    
                    LogDebug($"Stage progress loaded: {stageProgressMap.Count} entries");
                }
                else
                {
                    LogDebug("No saved stage progress found");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load stage progress: {ex.Message}");
                stageProgressMap.Clear();
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        /// <param name="message">メッセージ</param>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[StageManager] {message}");
            }
        }

        /// <summary>
        /// 全ステージを強制解放（デバッグ用）
        /// </summary>
        [ContextMenu("Unlock All Stages (Debug)")]
        public void UnlockAllStagesDebug()
        {
            foreach (var stage in allStages)
            {
                if (!stage.isUnlocked)
                {
                    UnlockStage(stage.stageId);
                }
            }
            LogDebug("All stages unlocked (Debug)");
        }

        /// <summary>
        /// 進行状況をリセット（デバッグ用）
        /// </summary>
        [ContextMenu("Reset Progress (Debug)")]
        public void ResetProgressDebug()
        {
            stageProgressMap.Clear();
            PlayerPrefs.DeleteKey(saveDataKey);
            
            foreach (var stage in allStages)
            {
                stage.isUnlocked = false;
            }
            
            CheckInitialStageUnlocks();
            LogDebug("Stage progress reset (Debug)");
        }

        #endregion
    }

    /// <summary>
    /// ステージ進行状況セーブデータ
    /// </summary>
    [Serializable]
    public class StageProgressSaveData
    {
        public List<StageProgress> progressMap;
        public DateTime lastSaveTime;
    }
}