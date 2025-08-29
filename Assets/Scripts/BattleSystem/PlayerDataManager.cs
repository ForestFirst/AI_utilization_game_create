using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// プレイヤーデータ管理システム
    /// レベル、経験値、所持金、インベントリの永続化管理
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        [Header("プレイヤー基本データ")]
        [SerializeField] private PlayerData playerData;
        [SerializeField] private bool autoSave = true;
        [SerializeField] private float autoSaveInterval = 30f; // 30秒間隔
        
        [Header("デバッグ設定")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool resetDataOnStart = false;
        
        // 保存キー
        private const string SAVE_DATA_KEY = "PlayerGameData";
        private const string SETTINGS_KEY = "PlayerSettings";
        
        // 自動保存タイマー
        private float autoSaveTimer = 0f;
        
        // イベント定義
        public static event Action<PlayerData> OnPlayerDataChanged;
        public static event Action<int> OnLevelUp;
        public static event Action<int> OnGoldChanged;
        public static event Action<List<InventoryItem>> OnInventoryChanged;
        public static event Action OnDataSaved;
        public static event Action OnDataLoaded;
        
        // シングルトン
        public static PlayerDataManager Instance { get; private set; }
        
        // プロパティ
        public PlayerData PlayerData => playerData;
        public int Level => playerData?.level ?? 1;
        public int Experience => playerData?.experience ?? 0;
        public int Gold => playerData?.gold ?? 0;
        public int MaxHP => playerData?.maxHP ?? 15000;
        public int CurrentHP => playerData?.currentHP ?? 15000;

        #region Unity Lifecycle

        private void Awake()
        {
            // シングルトン設定
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePlayerDataManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (resetDataOnStart)
            {
                ResetPlayerData();
            }
            else
            {
                LoadPlayerData();
            }
        }

        private void Update()
        {
            // 自動保存処理
            if (autoSave)
            {
                autoSaveTimer += Time.deltaTime;
                if (autoSaveTimer >= autoSaveInterval)
                {
                    SavePlayerData();
                    autoSaveTimer = 0f;
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SavePlayerData();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SavePlayerData();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// プレイヤーデータマネージャーの初期化
        /// </summary>
        private void InitializePlayerDataManager()
        {
            if (playerData == null)
            {
                CreateDefaultPlayerData();
            }
            
            LogDebug("PlayerDataManager initialized");
        }

        /// <summary>
        /// デフォルトプレイヤーデータを作成
        /// </summary>
        private void CreateDefaultPlayerData()
        {
            playerData = new PlayerData
            {
                playerName = "プレイヤー",
                level = 1,
                experience = 0,
                gold = 1000,
                maxHP = 15000,
                currentHP = 15000,
                attackPower = 100,
                inventory = new List<InventoryItem>(),
                unlockedStages = new List<string> { "stage_001" }, // 最初のステージは解放済み
                gameSettings = new PlayerGameSettings(),
                statistics = new PlayerStatistics(),
                lastPlayTime = DateTime.Now
            };
            
            LogDebug("Default player data created");
        }

        #endregion

        #region Data Management

        /// <summary>
        /// プレイヤーデータを保存
        /// </summary>
        public void SavePlayerData()
        {
            try
            {
                if (playerData != null)
                {
                    playerData.lastPlayTime = DateTime.Now;
                    
                    string jsonData = JsonUtility.ToJson(playerData, true);
                    PlayerPrefs.SetString(SAVE_DATA_KEY, jsonData);
                    PlayerPrefs.Save();
                    
                    OnDataSaved?.Invoke();
                    LogDebug("Player data saved successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDataManager] Failed to save player data: {ex.Message}");
            }
        }

        /// <summary>
        /// プレイヤーデータを読み込み
        /// </summary>
        public void LoadPlayerData()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_DATA_KEY))
                {
                    string jsonData = PlayerPrefs.GetString(SAVE_DATA_KEY);
                    var loadedData = JsonUtility.FromJson<PlayerData>(jsonData);
                    
                    if (loadedData != null)
                    {
                        playerData = loadedData;
                        
                        // データ整合性チェック
                        ValidatePlayerData();
                        
                        OnDataLoaded?.Invoke();
                        OnPlayerDataChanged?.Invoke(playerData);
                        
                        LogDebug($"Player data loaded: Level {playerData.level}, Gold {playerData.gold}");
                        return;
                    }
                }
                
                // セーブデータが存在しない場合はデフォルト作成
                CreateDefaultPlayerData();
                OnPlayerDataChanged?.Invoke(playerData);
                LogDebug("No save data found, created default player data");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerDataManager] Failed to load player data: {ex.Message}");
                CreateDefaultPlayerData();
            }
        }

        /// <summary>
        /// プレイヤーデータの整合性をチェック
        /// </summary>
        private void ValidatePlayerData()
        {
            // 必須フィールドの初期化
            if (playerData.inventory == null)
                playerData.inventory = new List<InventoryItem>();
            
            if (playerData.unlockedStages == null)
                playerData.unlockedStages = new List<string> { "stage_001" };
            
            if (playerData.gameSettings == null)
                playerData.gameSettings = new PlayerGameSettings();
            
            if (playerData.statistics == null)
                playerData.statistics = new PlayerStatistics();
            
            // 値の範囲チェック
            if (playerData.currentHP <= 0)
                playerData.currentHP = 1;
            
            if (playerData.currentHP > playerData.maxHP)
                playerData.currentHP = playerData.maxHP;
            
            if (playerData.level <= 0)
                playerData.level = 1;
            
            if (playerData.gold < 0)
                playerData.gold = 0;
        }

        /// <summary>
        /// プレイヤーデータをリセット
        /// </summary>
        public void ResetPlayerData()
        {
            CreateDefaultPlayerData();
            SavePlayerData();
            OnPlayerDataChanged?.Invoke(playerData);
            LogDebug("Player data reset to default");
        }

        #endregion

        #region Experience and Level

        /// <summary>
        /// 経験値を追加
        /// </summary>
        /// <param name="amount">追加する経験値</param>
        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            
            int oldLevel = playerData.level;
            playerData.experience += amount;
            
            // レベルアップ判定
            int newLevel = CalculateLevelFromExperience(playerData.experience);
            if (newLevel > oldLevel)
            {
                LevelUp(newLevel);
            }
            
            OnPlayerDataChanged?.Invoke(playerData);
            LogDebug($"Added {amount} experience (Total: {playerData.experience})");
        }

        /// <summary>
        /// 経験値からレベルを計算
        /// </summary>
        private int CalculateLevelFromExperience(int experience)
        {
            // 必要経験値 = レベル * 100 (簡易計算式)
            int level = 1;
            int totalExp = 0;
            
            while (totalExp < experience)
            {
                totalExp += level * 100;
                if (totalExp <= experience)
                    level++;
            }
            
            return Mathf.Max(1, level - 1);
        }

        /// <summary>
        /// レベルアップ処理
        /// </summary>
        private void LevelUp(int newLevel)
        {
            int oldLevel = playerData.level;
            playerData.level = newLevel;
            
            // レベルアップボーナス
            int levelDiff = newLevel - oldLevel;
            playerData.maxHP += levelDiff * 100; // レベル1につきHP+100
            playerData.currentHP = playerData.maxHP; // HP全回復
            playerData.attackPower += levelDiff * 5; // レベル1につき攻撃力+5
            
            OnLevelUp?.Invoke(newLevel);
            LogDebug($"Level up! {oldLevel} → {newLevel}");
        }

        /// <summary>
        /// 次のレベルまでの必要経験値を取得
        /// </summary>
        public int GetExperienceToNextLevel()
        {
            int nextLevelExp = (playerData.level + 1) * 100;
            int currentLevelExp = playerData.level * 100;
            return nextLevelExp - (playerData.experience - GetExperienceForLevel(playerData.level));
        }

        /// <summary>
        /// 指定レベルまでの合計必要経験値を取得
        /// </summary>
        private int GetExperienceForLevel(int level)
        {
            int totalExp = 0;
            for (int i = 1; i < level; i++)
            {
                totalExp += i * 100;
            }
            return totalExp;
        }

        #endregion

        #region Gold Management

        /// <summary>
        /// ゴールドを追加
        /// </summary>
        /// <param name="amount">追加するゴールド</param>
        public void AddGold(int amount)
        {
            if (amount == 0) return;
            
            playerData.gold = Mathf.Max(0, playerData.gold + amount);
            OnGoldChanged?.Invoke(playerData.gold);
            OnPlayerDataChanged?.Invoke(playerData);
            
            LogDebug($"Gold changed by {amount} (Total: {playerData.gold})");
        }

        /// <summary>
        /// ゴールドを消費
        /// </summary>
        /// <param name="amount">消費するゴールド</param>
        /// <returns>消費に成功した場合true</returns>
        public bool SpendGold(int amount)
        {
            if (amount <= 0 || playerData.gold < amount)
            {
                return false;
            }
            
            AddGold(-amount);
            return true;
        }

        /// <summary>
        /// ゴールドが足りているかチェック
        /// </summary>
        /// <param name="amount">必要なゴールド</param>
        /// <returns>足りている場合true</returns>
        public bool HasEnoughGold(int amount)
        {
            return playerData.gold >= amount;
        }

        #endregion

        #region Inventory Management

        /// <summary>
        /// アイテムを追加
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <param name="quantity">数量</param>
        public void AddItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;
            
            var existingItem = playerData.inventory.Find(item => item.itemId == itemId);
            if (existingItem != null)
            {
                existingItem.quantity += quantity;
            }
            else
            {
                playerData.inventory.Add(new InventoryItem
                {
                    itemId = itemId,
                    quantity = quantity,
                    obtainedDate = DateTime.Now
                });
            }
            
            OnInventoryChanged?.Invoke(playerData.inventory);
            OnPlayerDataChanged?.Invoke(playerData);
            
            LogDebug($"Added item: {itemId} x{quantity}");
        }

        /// <summary>
        /// アイテムを消費
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <param name="quantity">消費数量</param>
        /// <returns>消費に成功した場合true</returns>
        public bool ConsumeItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            
            var existingItem = playerData.inventory.Find(item => item.itemId == itemId);
            if (existingItem == null || existingItem.quantity < quantity)
            {
                return false;
            }
            
            existingItem.quantity -= quantity;
            if (existingItem.quantity <= 0)
            {
                playerData.inventory.Remove(existingItem);
            }
            
            OnInventoryChanged?.Invoke(playerData.inventory);
            OnPlayerDataChanged?.Invoke(playerData);
            
            LogDebug($"Consumed item: {itemId} x{quantity}");
            return true;
        }

        /// <summary>
        /// アイテムの所持数を取得
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <returns>所持数</returns>
        public int GetItemCount(string itemId)
        {
            var item = playerData.inventory.Find(item => item.itemId == itemId);
            return item?.quantity ?? 0;
        }

        #endregion

        #region HP Management

        /// <summary>
        /// HPを回復
        /// </summary>
        /// <param name="amount">回復量</param>
        public void HealHP(int amount)
        {
            if (amount <= 0) return;
            
            playerData.currentHP = Mathf.Min(playerData.maxHP, playerData.currentHP + amount);
            OnPlayerDataChanged?.Invoke(playerData);
            
            LogDebug($"HP healed by {amount} (Current: {playerData.currentHP}/{playerData.maxHP})");
        }

        /// <summary>
        /// HPにダメージを与える
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        public void TakeDamage(int damage)
        {
            if (damage <= 0) return;
            
            playerData.currentHP = Mathf.Max(0, playerData.currentHP - damage);
            OnPlayerDataChanged?.Invoke(playerData);
            
            LogDebug($"Took {damage} damage (Current: {playerData.currentHP}/{playerData.maxHP})");
        }

        /// <summary>
        /// HPを全回復
        /// </summary>
        public void FullHeal()
        {
            playerData.currentHP = playerData.maxHP;
            OnPlayerDataChanged?.Invoke(playerData);
            LogDebug("HP fully restored");
        }

        #endregion

        #region Stage Management

        /// <summary>
        /// ステージを解放
        /// </summary>
        /// <param name="stageId">ステージID</param>
        public void UnlockStage(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return;
            
            if (!playerData.unlockedStages.Contains(stageId))
            {
                playerData.unlockedStages.Add(stageId);
                OnPlayerDataChanged?.Invoke(playerData);
                LogDebug($"Stage unlocked: {stageId}");
            }
        }

        /// <summary>
        /// ステージが解放されているかチェック
        /// </summary>
        /// <param name="stageId">ステージID</param>
        /// <returns>解放されている場合true</returns>
        public bool IsStageUnlocked(string stageId)
        {
            return playerData.unlockedStages.Contains(stageId);
        }

        #endregion

        #region Debug and Utility

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[PlayerDataManager] {message}");
            }
        }

        /// <summary>
        /// プレイヤーデータのデバッグ表示
        /// </summary>
        [ContextMenu("Debug Player Data")]
        public void DebugPlayerData()
        {
            if (playerData != null)
            {
                Debug.Log($"=== Player Data Debug ===");
                Debug.Log($"Name: {playerData.playerName}");
                Debug.Log($"Level: {playerData.level}");
                Debug.Log($"Experience: {playerData.experience}");
                Debug.Log($"Gold: {playerData.gold}");
                Debug.Log($"HP: {playerData.currentHP}/{playerData.maxHP}");
                Debug.Log($"Attack Power: {playerData.attackPower}");
                Debug.Log($"Inventory Items: {playerData.inventory.Count}");
                Debug.Log($"Unlocked Stages: {playerData.unlockedStages.Count}");
                Debug.Log($"Last Play Time: {playerData.lastPlayTime}");
            }
            else
            {
                Debug.Log("Player data is null");
            }
        }

        /// <summary>
        /// テスト用ゴールド追加
        /// </summary>
        [ContextMenu("Add Test Gold (1000)")]
        public void AddTestGold()
        {
            AddGold(1000);
        }

        /// <summary>
        /// テスト用経験値追加
        /// </summary>
        [ContextMenu("Add Test Experience (500)")]
        public void AddTestExperience()
        {
            AddExperience(500);
        }

        #endregion
    }

    /// <summary>
    /// プレイヤーデータクラス
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public string playerName;
        public int level;
        public int experience;
        public int gold;
        public int maxHP;
        public int currentHP;
        public int attackPower;
        public List<InventoryItem> inventory;
        public List<string> unlockedStages;
        public PlayerGameSettings gameSettings;
        public PlayerStatistics statistics;
        public DateTime lastPlayTime;
        
        // SimpleBattleUIとの互換性のためのプロパティ
        public int maxHp 
        { 
            get => maxHP; 
            set => maxHP = value; 
        }
        public int currentHp 
        { 
            get => currentHP; 
            set => currentHP = Mathf.Clamp(value, 0, maxHP); 
        }
        public int baseAttackPower 
        { 
            get => attackPower; 
            set => attackPower = value; 
        }
        
        // 戦闘関連プロパティ
        public float criticalDamageModifier = 1.5f;
        public float luckModifier = 1.0f;
        public bool IsAlive => currentHP > 0;
        
        /// <summary>
        /// 回復処理
        /// </summary>
        public void Heal(int amount)
        {
            currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        }
        
        /// <summary>
        /// ダメージ処理
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);
        }
        
        // 武器関連のプロパティ（既存コードとの互換性維持）
        public WeaponData[] equippedWeapons = new WeaponData[0];
        public int[] weaponCooldowns = new int[0];
        
        /// <summary>
        /// 武器使用可否チェック（互換性のため）
        /// </summary>
        public bool CanUseWeapon(int weaponIndex)
        {
            return weaponIndex >= 0 && weaponIndex < weaponCooldowns.Length && weaponCooldowns[weaponIndex] <= 0;
        }
    }

    /// <summary>
    /// インベントリアイテム
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int quantity;
        public DateTime obtainedDate;
    }

    /// <summary>
    /// ゲーム設定
    /// </summary>
    [Serializable]
    public class PlayerGameSettings
    {
        public float masterVolume = 1.0f;
        public float bgmVolume = 0.8f;
        public float sfxVolume = 0.8f;
        public int graphicsQuality = 2; // 0:Low, 1:Medium, 2:High
        public bool fullScreen = true;
        public string languageCode = "ja";
    }

    /// <summary>
    /// プレイヤー統計情報
    /// </summary>
    [Serializable]
    public class PlayerStatistics
    {
        public int totalBattles = 0;
        public int battlesWon = 0;
        public int totalDamageDealt = 0;
        public int totalDamageTaken = 0;
        public int totalPlayTimeSeconds = 0;
        public int highestLevelReached = 1;
        public int totalGoldEarned = 0;
        public int totalGoldSpent = 0;
        public DateTime firstPlayDate;
        public DateTime lastUpdateDate;
        
        public float WinRate => totalBattles > 0 ? (float)battlesWon / totalBattles * 100f : 0f;
        public TimeSpan TotalPlayTime => TimeSpan.FromSeconds(totalPlayTimeSeconds);
    }
}