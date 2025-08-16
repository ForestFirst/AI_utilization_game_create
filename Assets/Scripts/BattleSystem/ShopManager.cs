using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// ショップ管理システム
    /// アイテムの販売、購入処理、通貨管理を行う
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        [Header("ショップ設定")]
        [SerializeField] private ShopConfig shopConfig;
        [SerializeField] private List<ShopItemData> allShopItems = new List<ShopItemData>();
        [SerializeField] private bool autoLoadItemsFromResources = true;
        [SerializeField] private string itemResourcesPath = "ShopItems";
        
        [Header("デバッグ設定")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool enableTestCurrency = true;
        
        // 通貨管理
        private Dictionary<CurrencyType, int> playerCurrencies;
        
        // 購入制限管理
        private Dictionary<string, int> totalPurchaseCounts;    // 総購入回数
        private Dictionary<string, int> dailyPurchaseCounts;    // 日次購入回数
        private Dictionary<string, int> weeklyPurchaseCounts;   // 週次購入回数
        private Dictionary<string, DateTime> lastPurchaseDates; // 最終購入日
        
        // 購入履歴
        private List<PurchaseHistory> purchaseHistory;
        
        // ウィッシュリスト
        private List<string> wishlistItemIds;
        
        // セール管理
        private Dictionary<string, DateTime> saleEndTimes;
        
        // セーブデータキー
        private const string CURRENCY_SAVE_KEY = "PlayerCurrencies";
        private const string PURCHASE_COUNT_SAVE_KEY = "PurchaseCounts";
        private const string PURCHASE_HISTORY_SAVE_KEY = "PurchaseHistory";
        private const string WISHLIST_SAVE_KEY = "Wishlist";
        private const string SALE_SAVE_KEY = "SaleData";
        
        // イベント定義
        public event Action<ShopItemData, bool> OnPurchaseAttempt;      // 購入試行時（成功/失敗）
        public event Action<CurrencyType, int, int> OnCurrencyChanged;  // 通貨変更時（種類、変更前、変更後）
        public event Action<string> OnWishlistChanged;                  // ウィッシュリスト変更時
        public event Action OnShopRefreshed;                            // ショップ更新時

        // シングルトンパターン
        public static ShopManager Instance { get; private set; }

        // プロパティ
        public ShopConfig Config => shopConfig;
        public List<ShopItemData> AllShopItems => allShopItems.ToList();
        public List<PurchaseHistory> PurchaseHistory => purchaseHistory.ToList();
        public List<string> WishlistItems => wishlistItemIds.ToList();

        #region Unity Lifecycle

        private void Awake()
        {
            // シングルトン設定
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeShopManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // アイテムデータの自動読み込み
            if (autoLoadItemsFromResources)
            {
                LoadShopItemsFromResources();
            }
            
            // セーブデータの読み込み
            LoadAllData();
            
            // テスト用通貨の設定
            if (enableTestCurrency && debugMode)
            {
                SetupTestCurrency();
            }
            
            // 定期更新の開始
            if (shopConfig?.enableAutoRefresh == true)
            {
                InvokeRepeating(nameof(RefreshShop), shopConfig.autoRefreshInterval * 3600f, 
                               shopConfig.autoRefreshInterval * 3600f);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ショップマネージャーの初期化
        /// </summary>
        private void InitializeShopManager()
        {
            playerCurrencies = new Dictionary<CurrencyType, int>();
            totalPurchaseCounts = new Dictionary<string, int>();
            dailyPurchaseCounts = new Dictionary<string, int>();
            weeklyPurchaseCounts = new Dictionary<string, int>();
            lastPurchaseDates = new Dictionary<string, DateTime>();
            purchaseHistory = new List<PurchaseHistory>();
            wishlistItemIds = new List<string>();
            saleEndTimes = new Dictionary<string, DateTime>();
            
            LogDebug("ShopManager initialized");
        }

        /// <summary>
        /// Resourcesフォルダからショップアイテムを読み込み
        /// </summary>
        private void LoadShopItemsFromResources()
        {
            try
            {
                var itemAssets = Resources.LoadAll<ShopItemData>(itemResourcesPath);
                
                foreach (var item in itemAssets)
                {
                    if (!allShopItems.Contains(item))
                    {
                        allShopItems.Add(item);
                    }
                }
                
                // 表示順序でソート
                allShopItems.Sort((a, b) => a.displayOrder.CompareTo(b.displayOrder));
                
                LogDebug($"Loaded {itemAssets.Length} shop items from Resources/{itemResourcesPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load shop items from Resources: {ex.Message}");
            }
        }

        /// <summary>
        /// テスト用通貨を設定
        /// </summary>
        private void SetupTestCurrency()
        {
            SetCurrency(CurrencyType.Gold, 10000);
            SetCurrency(CurrencyType.Gem, 500);
            SetCurrency(CurrencyType.BattlePoint, 1000);
            SetCurrency(CurrencyType.EventToken, 100);
            SetCurrency(CurrencyType.SpecialCoin, 50);
            
            LogDebug("Test currency setup completed");
        }

        #endregion

        #region Currency Management

        /// <summary>
        /// 通貨を取得
        /// </summary>
        /// <param name="currencyType">通貨タイプ</param>
        /// <returns>所持量</returns>
        public int GetCurrency(CurrencyType currencyType)
        {
            return playerCurrencies.ContainsKey(currencyType) ? playerCurrencies[currencyType] : 0;
        }

        /// <summary>
        /// 通貨を設定
        /// </summary>
        /// <param name="currencyType">通貨タイプ</param>
        /// <param name="amount">設定量</param>
        public void SetCurrency(CurrencyType currencyType, int amount)
        {
            int oldAmount = GetCurrency(currencyType);
            playerCurrencies[currencyType] = Mathf.Max(0, amount);
            
            OnCurrencyChanged?.Invoke(currencyType, oldAmount, playerCurrencies[currencyType]);
            SaveCurrencyData();
            
            LogDebug($"Currency set: {currencyType} = {playerCurrencies[currencyType]} (was {oldAmount})");
        }

        /// <summary>
        /// 通貨を追加
        /// </summary>
        /// <param name="currencyType">通貨タイプ</param>
        /// <param name="amount">追加量</param>
        public void AddCurrency(CurrencyType currencyType, int amount)
        {
            if (amount <= 0) return;
            
            int newAmount = GetCurrency(currencyType) + amount;
            SetCurrency(currencyType, newAmount);
        }

        /// <summary>
        /// 通貨を消費
        /// </summary>
        /// <param name="currencyType">通貨タイプ</param>
        /// <param name="amount">消費量</param>
        /// <returns>消費成功の場合true</returns>
        public bool SpendCurrency(CurrencyType currencyType, int amount)
        {
            if (amount <= 0) return false;
            
            int currentAmount = GetCurrency(currencyType);
            if (currentAmount < amount)
            {
                LogDebug($"Insufficient currency: {currencyType} (need {amount}, have {currentAmount})");
                return false;
            }
            
            SetCurrency(currencyType, currentAmount - amount);
            return true;
        }

        /// <summary>
        /// 指定通貨が足りているかチェック
        /// </summary>
        /// <param name="currencyType">通貨タイプ</param>
        /// <param name="amount">必要量</param>
        /// <returns>足りている場合true</returns>
        public bool HasEnoughCurrency(CurrencyType currencyType, int amount)
        {
            return GetCurrency(currencyType) >= amount;
        }

        #endregion

        #region Item Management

        /// <summary>
        /// アイテムIDからショップアイテムを取得
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <returns>ショップアイテム（見つからない場合null）</returns>
        public ShopItemData GetShopItemById(string itemId)
        {
            return allShopItems.FirstOrDefault(item => item.itemId == itemId);
        }

        /// <summary>
        /// タブに表示するアイテムリストを取得
        /// </summary>
        /// <param name="tabId">タブID</param>
        /// <returns>表示アイテムリスト</returns>
        public List<ShopItemData> GetItemsForTab(string tabId)
        {
            var tab = shopConfig?.GetTabById(tabId);
            if (tab == null) return new List<ShopItemData>();
            
            var playerLevel = GetCurrentPlayerLevel();
            var ownedItems = GetOwnedItemIds();
            var clearedStages = GetClearedStageIds();
            
            return allShopItems
                .Where(item => tab.CanDisplayItem(item) && 
                              item.CanPurchase(playerLevel, ownedItems, clearedStages))
                .OrderBy(item => item.displayOrder)
                .ToList();
        }

        /// <summary>
        /// セール中のアイテムリストを取得
        /// </summary>
        /// <returns>セール中アイテムリスト</returns>
        public List<ShopItemData> GetSaleItems()
        {
            return allShopItems.Where(item => item.price.isOnSale).ToList();
        }

        /// <summary>
        /// おすすめアイテムリストを取得
        /// </summary>
        /// <returns>おすすめアイテムリスト</returns>
        public List<ShopItemData> GetRecommendedItems()
        {
            return allShopItems.Where(item => item.isRecommended).ToList();
        }

        /// <summary>
        /// 新商品アイテムリストを取得
        /// </summary>
        /// <returns>新商品アイテムリスト</returns>
        public List<ShopItemData> GetNewItems()
        {
            return allShopItems.Where(item => item.isNew).ToList();
        }

        #endregion

        #region Purchase System

        /// <summary>
        /// アイテムを購入
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <param name="quantity">購入数量</param>
        /// <returns>購入成功の場合true</returns>
        public bool PurchaseItem(string itemId, int quantity = 1)
        {
            var item = GetShopItemById(itemId);
            if (item == null)
            {
                LogDebug($"Item not found: {itemId}");
                OnPurchaseAttempt?.Invoke(null, false);
                return false;
            }

            // 購入可能性チェック
            if (!CanPurchaseItem(item, quantity))
            {
                OnPurchaseAttempt?.Invoke(item, false);
                return false;
            }

            // 通貨消費
            int totalCost = item.price.GetActualPrice() * quantity;
            if (!SpendCurrency(item.price.currencyType, totalCost))
            {
                LogDebug($"Failed to spend currency for purchase: {itemId}");
                OnPurchaseAttempt?.Invoke(item, false);
                return false;
            }

            // 購入処理実行
            ExecutePurchase(item, quantity, totalCost);
            
            // 購入回数更新
            UpdatePurchaseCounts(itemId, quantity);
            
            // 購入履歴追加
            var purchase = new PurchaseHistory(itemId, quantity, item.price.currencyType, 
                                             totalCost, item.price.isOnSale);
            purchaseHistory.Add(purchase);
            
            // データ保存
            SaveAllData();
            
            OnPurchaseAttempt?.Invoke(item, true);
            LogDebug($"Purchase successful: {item.itemName} x{quantity} for {totalCost} {item.price.currencyType}");
            
            return true;
        }

        /// <summary>
        /// アイテムの購入可能性をチェック
        /// </summary>
        /// <param name="item">ショップアイテム</param>
        /// <param name="quantity">購入数量</param>
        /// <returns>購入可能な場合true</returns>
        public bool CanPurchaseItem(ShopItemData item, int quantity = 1)
        {
            if (item == null) return false;
            
            // 基本購入条件チェック
            var playerLevel = GetCurrentPlayerLevel();
            var ownedItems = GetOwnedItemIds();
            var clearedStages = GetClearedStageIds();
            
            if (!item.CanPurchase(playerLevel, ownedItems, clearedStages))
            {
                return false;
            }
            
            // 購入制限チェック
            if (!CheckPurchaseLimits(item.itemId, quantity))
            {
                return false;
            }
            
            // 通貨チェック
            int totalCost = item.price.GetActualPrice() * quantity;
            if (!HasEnoughCurrency(item.price.currencyType, totalCost))
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 購入制限をチェック
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <param name="quantity">購入数量</param>
        /// <returns>制限内の場合true</returns>
        private bool CheckPurchaseLimits(string itemId, int quantity)
        {
            var item = GetShopItemById(itemId);
            if (item == null) return false;
            
            // 最大購入数チェック
            if (item.maxPurchaseCount > 0)
            {
                int totalPurchased = GetTotalPurchaseCount(itemId);
                if (totalPurchased + quantity > item.maxPurchaseCount)
                {
                    LogDebug($"Max purchase limit exceeded: {itemId} (limit: {item.maxPurchaseCount})");
                    return false;
                }
            }
            
            // 日次制限チェック
            if (item.dailyPurchaseLimit > 0)
            {
                int dailyPurchased = GetDailyPurchaseCount(itemId);
                if (dailyPurchased + quantity > item.dailyPurchaseLimit)
                {
                    LogDebug($"Daily purchase limit exceeded: {itemId} (limit: {item.dailyPurchaseLimit})");
                    return false;
                }
            }
            
            // 週次制限チェック
            if (item.weeklyPurchaseLimit > 0)
            {
                int weeklyPurchased = GetWeeklyPurchaseCount(itemId);
                if (weeklyPurchased + quantity > item.weeklyPurchaseLimit)
                {
                    LogDebug($"Weekly purchase limit exceeded: {itemId} (limit: {item.weeklyPurchaseLimit})");
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 購入処理を実行
        /// </summary>
        /// <param name="item">ショップアイテム</param>
        /// <param name="quantity">購入数量</param>
        /// <param name="totalCost">総費用</param>
        private void ExecutePurchase(ShopItemData item, int quantity, int totalCost)
        {
            switch (item.itemType)
            {
                case ShopItemType.Weapon:
                    GiveWeapon(item.targetItemId, quantity);
                    break;
                    
                case ShopItemType.Consumable:
                    GiveConsumable(item.targetItemId, quantity);
                    break;
                    
                case ShopItemType.Equipment:
                    GiveEquipment(item.targetItemId, quantity);
                    break;
                    
                case ShopItemType.Material:
                    GiveMaterial(item.targetItemId, quantity);
                    break;
                    
                case ShopItemType.Bundle:
                    GiveBundle(item);
                    break;
                    
                case ShopItemType.Special:
                    ProcessSpecialItem(item.targetItemId, quantity);
                    break;
            }
        }

        #endregion

        #region Purchase Count Management

        /// <summary>
        /// 購入回数を更新
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <param name="quantity">購入数量</param>
        private void UpdatePurchaseCounts(string itemId, int quantity)
        {
            // 総購入回数更新
            if (!totalPurchaseCounts.ContainsKey(itemId))
                totalPurchaseCounts[itemId] = 0;
            totalPurchaseCounts[itemId] += quantity;
            
            // 日次購入回数更新
            ResetDailyCountIfNeeded(itemId);
            if (!dailyPurchaseCounts.ContainsKey(itemId))
                dailyPurchaseCounts[itemId] = 0;
            dailyPurchaseCounts[itemId] += quantity;
            
            // 週次購入回数更新
            ResetWeeklyCountIfNeeded(itemId);
            if (!weeklyPurchaseCounts.ContainsKey(itemId))
                weeklyPurchaseCounts[itemId] = 0;
            weeklyPurchaseCounts[itemId] += quantity;
            
            // 最終購入日更新
            lastPurchaseDates[itemId] = DateTime.Now;
        }

        /// <summary>
        /// 総購入回数を取得
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <returns>総購入回数</returns>
        public int GetTotalPurchaseCount(string itemId)
        {
            return totalPurchaseCounts.ContainsKey(itemId) ? totalPurchaseCounts[itemId] : 0;
        }

        /// <summary>
        /// 日次購入回数を取得
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <returns>日次購入回数</returns>
        public int GetDailyPurchaseCount(string itemId)
        {
            ResetDailyCountIfNeeded(itemId);
            return dailyPurchaseCounts.ContainsKey(itemId) ? dailyPurchaseCounts[itemId] : 0;
        }

        /// <summary>
        /// 週次購入回数を取得
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <returns>週次購入回数</returns>
        public int GetWeeklyPurchaseCount(string itemId)
        {
            ResetWeeklyCountIfNeeded(itemId);
            return weeklyPurchaseCounts.ContainsKey(itemId) ? weeklyPurchaseCounts[itemId] : 0;
        }

        /// <summary>
        /// 必要に応じて日次カウントをリセット
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        private void ResetDailyCountIfNeeded(string itemId)
        {
            if (lastPurchaseDates.ContainsKey(itemId))
            {
                var lastPurchase = lastPurchaseDates[itemId];
                if (lastPurchase.Date < DateTime.Now.Date)
                {
                    dailyPurchaseCounts[itemId] = 0;
                }
            }
        }

        /// <summary>
        /// 必要に応じて週次カウントをリセット
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        private void ResetWeeklyCountIfNeeded(string itemId)
        {
            if (lastPurchaseDates.ContainsKey(itemId))
            {
                var lastPurchase = lastPurchaseDates[itemId];
                var weeksDiff = (DateTime.Now - lastPurchase).Days / 7;
                if (weeksDiff >= 1)
                {
                    weeklyPurchaseCounts[itemId] = 0;
                }
            }
        }

        #endregion

        #region Wishlist Management

        /// <summary>
        /// ウィッシュリストにアイテムを追加
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        public void AddToWishlist(string itemId)
        {
            if (!wishlistItemIds.Contains(itemId))
            {
                wishlistItemIds.Add(itemId);
                OnWishlistChanged?.Invoke(itemId);
                SaveWishlistData();
                LogDebug($"Added to wishlist: {itemId}");
            }
        }

        /// <summary>
        /// ウィッシュリストからアイテムを削除
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        public void RemoveFromWishlist(string itemId)
        {
            if (wishlistItemIds.Remove(itemId))
            {
                OnWishlistChanged?.Invoke(itemId);
                SaveWishlistData();
                LogDebug($"Removed from wishlist: {itemId}");
            }
        }

        /// <summary>
        /// アイテムがウィッシュリストに入っているかチェック
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        /// <returns>ウィッシュリストに入っている場合true</returns>
        public bool IsInWishlist(string itemId)
        {
            return wishlistItemIds.Contains(itemId);
        }

        #endregion

        #region Shop Refresh

        /// <summary>
        /// ショップを更新
        /// </summary>
        public void RefreshShop()
        {
            // ランダムセールの処理
            if (shopConfig?.enableRandomSales == true)
            {
                ProcessRandomSales();
            }
            
            // 期間限定アイテムの更新
            UpdateTimeLimitedItems();
            
            OnShopRefreshed?.Invoke();
            LogDebug("Shop refreshed");
        }

        /// <summary>
        /// ランダムセールの処理
        /// </summary>
        private void ProcessRandomSales()
        {
            // 既存セールの終了チェック
            var expiredSales = saleEndTimes.Where(kvp => DateTime.Now > kvp.Value).ToList();
            foreach (var expiredSale in expiredSales)
            {
                var item = GetShopItemById(expiredSale.Key);
                if (item != null)
                {
                    item.price.isOnSale = false;
                    LogDebug($"Sale ended: {expiredSale.Key}");
                }
                saleEndTimes.Remove(expiredSale.Key);
            }
            
            // 新しいセールの開始
            var eligibleItems = allShopItems
                .Where(item => !item.price.isOnSale && UnityEngine.Random.value <= shopConfig.saleChance)
                .Take(shopConfig.maxSaleItems)
                .ToList();
            
            foreach (var item in eligibleItems)
            {
                float discountRate = UnityEngine.Random.Range(shopConfig.minDiscountRate, shopConfig.maxDiscountRate);
                item.price.originalAmount = item.price.amount;
                item.price.discountRate = discountRate;
                item.price.isOnSale = true;
                
                // セール終了時間を設定（1-7日後）
                var saleEndTime = DateTime.Now.AddDays(UnityEngine.Random.Range(1, 8));
                saleEndTimes[item.itemId] = saleEndTime;
                
                LogDebug($"Sale started: {item.itemId} ({discountRate:P0} off until {saleEndTime:MM/dd})");
            }
        }

        /// <summary>
        /// 期間限定アイテムの更新
        /// </summary>
        private void UpdateTimeLimitedItems()
        {
            var now = DateTime.Now;
            foreach (var item in allShopItems.Where(i => i.isLimitedTime))
            {
                // 販売期間外のアイテムを非表示にする処理
                // （実際の実装では、表示フィルタリングで対応）
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
        /// 所持アイテムIDリストを取得
        /// </summary>
        /// <returns>所持アイテムIDリスト</returns>
        private List<string> GetOwnedItemIds()
        {
            // TODO: インベントリシステムと連携
            return new List<string>(); // 仮実装
        }

        /// <summary>
        /// クリア済みステージIDリストを取得
        /// </summary>
        /// <returns>クリア済みステージIDリスト</returns>
        private List<string> GetClearedStageIds()
        {
            // TODO: StageManagerと連携
            return new List<string>(); // 仮実装
        }

        /// <summary>
        /// 武器を付与
        /// </summary>
        /// <param name="weaponId">武器ID</param>
        /// <param name="quantity">数量</param>
        private void GiveWeapon(string weaponId, int quantity)
        {
            // TODO: 武器システムと連携
            LogDebug($"Weapon given: {weaponId} x{quantity}");
        }

        /// <summary>
        /// 消耗品を付与
        /// </summary>
        /// <param name="consumableId">消耗品ID</param>
        /// <param name="quantity">数量</param>
        private void GiveConsumable(string consumableId, int quantity)
        {
            // TODO: インベントリシステムと連携
            LogDebug($"Consumable given: {consumableId} x{quantity}");
        }

        /// <summary>
        /// 装備品を付与
        /// </summary>
        /// <param name="equipmentId">装備品ID</param>
        /// <param name="quantity">数量</param>
        private void GiveEquipment(string equipmentId, int quantity)
        {
            // TODO: 装備システムと連携
            LogDebug($"Equipment given: {equipmentId} x{quantity}");
        }

        /// <summary>
        /// 素材を付与
        /// </summary>
        /// <param name="materialId">素材ID</param>
        /// <param name="quantity">数量</param>
        private void GiveMaterial(string materialId, int quantity)
        {
            // TODO: 素材システムと連携
            LogDebug($"Material given: {materialId} x{quantity}");
        }

        /// <summary>
        /// バンドルを付与
        /// </summary>
        /// <param name="bundleItem">バンドルアイテム</param>
        private void GiveBundle(ShopItemData bundleItem)
        {
            for (int i = 0; i < bundleItem.bundleItemIds.Count && i < bundleItem.bundleQuantities.Count; i++)
            {
                string itemId = bundleItem.bundleItemIds[i];
                int quantity = bundleItem.bundleQuantities[i];
                
                // アイテムタイプに応じて処理（簡易実装）
                LogDebug($"Bundle item given: {itemId} x{quantity}");
            }
        }

        /// <summary>
        /// 特別アイテムを処理
        /// </summary>
        /// <param name="specialItemId">特別アイテムID</param>
        /// <param name="quantity">数量</param>
        private void ProcessSpecialItem(string specialItemId, int quantity)
        {
            // TODO: 特別アイテムシステムと連携
            LogDebug($"Special item processed: {specialItemId} x{quantity}");
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// すべてのデータを保存
        /// </summary>
        private void SaveAllData()
        {
            SaveCurrencyData();
            SavePurchaseCountData();
            SavePurchaseHistoryData();
            SaveWishlistData();
            SaveSaleData();
        }

        /// <summary>
        /// すべてのデータを読み込み
        /// </summary>
        private void LoadAllData()
        {
            LoadCurrencyData();
            LoadPurchaseCountData();
            LoadPurchaseHistoryData();
            LoadWishlistData();
            LoadSaleData();
        }

        /// <summary>
        /// 通貨データを保存
        /// </summary>
        private void SaveCurrencyData()
        {
            try
            {
                string json = JsonUtility.ToJson(new CurrencySaveData { currencies = playerCurrencies }, true);
                PlayerPrefs.SetString(CURRENCY_SAVE_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save currency data: {ex.Message}");
            }
        }

        /// <summary>
        /// 通貨データを読み込み
        /// </summary>
        private void LoadCurrencyData()
        {
            try
            {
                if (PlayerPrefs.HasKey(CURRENCY_SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(CURRENCY_SAVE_KEY);
                    var saveData = JsonUtility.FromJson<CurrencySaveData>(json);
                    playerCurrencies = saveData.currencies ?? new Dictionary<CurrencyType, int>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load currency data: {ex.Message}");
                playerCurrencies = new Dictionary<CurrencyType, int>();
            }
        }

        /// <summary>
        /// 購入回数データを保存
        /// </summary>
        private void SavePurchaseCountData()
        {
            try
            {
                var saveData = new PurchaseCountSaveData
                {
                    totalCounts = totalPurchaseCounts,
                    dailyCounts = dailyPurchaseCounts,
                    weeklyCounts = weeklyPurchaseCounts,
                    lastDates = lastPurchaseDates
                };
                string json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(PURCHASE_COUNT_SAVE_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save purchase count data: {ex.Message}");
            }
        }

        /// <summary>
        /// 購入回数データを読み込み
        /// </summary>
        private void LoadPurchaseCountData()
        {
            try
            {
                if (PlayerPrefs.HasKey(PURCHASE_COUNT_SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(PURCHASE_COUNT_SAVE_KEY);
                    var saveData = JsonUtility.FromJson<PurchaseCountSaveData>(json);
                    totalPurchaseCounts = saveData.totalCounts ?? new Dictionary<string, int>();
                    dailyPurchaseCounts = saveData.dailyCounts ?? new Dictionary<string, int>();
                    weeklyPurchaseCounts = saveData.weeklyCounts ?? new Dictionary<string, int>();
                    lastPurchaseDates = saveData.lastDates ?? new Dictionary<string, DateTime>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load purchase count data: {ex.Message}");
            }
        }

        /// <summary>
        /// 購入履歴データを保存
        /// </summary>
        private void SavePurchaseHistoryData()
        {
            try
            {
                var saveData = new PurchaseHistorySaveData { history = purchaseHistory };
                string json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(PURCHASE_HISTORY_SAVE_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save purchase history data: {ex.Message}");
            }
        }

        /// <summary>
        /// 購入履歴データを読み込み
        /// </summary>
        private void LoadPurchaseHistoryData()
        {
            try
            {
                if (PlayerPrefs.HasKey(PURCHASE_HISTORY_SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(PURCHASE_HISTORY_SAVE_KEY);
                    var saveData = JsonUtility.FromJson<PurchaseHistorySaveData>(json);
                    purchaseHistory = saveData.history ?? new List<PurchaseHistory>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load purchase history data: {ex.Message}");
                purchaseHistory = new List<PurchaseHistory>();
            }
        }

        /// <summary>
        /// ウィッシュリストデータを保存
        /// </summary>
        private void SaveWishlistData()
        {
            try
            {
                var saveData = new WishlistSaveData { itemIds = wishlistItemIds };
                string json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(WISHLIST_SAVE_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save wishlist data: {ex.Message}");
            }
        }

        /// <summary>
        /// ウィッシュリストデータを読み込み
        /// </summary>
        private void LoadWishlistData()
        {
            try
            {
                if (PlayerPrefs.HasKey(WISHLIST_SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(WISHLIST_SAVE_KEY);
                    var saveData = JsonUtility.FromJson<WishlistSaveData>(json);
                    wishlistItemIds = saveData.itemIds ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load wishlist data: {ex.Message}");
                wishlistItemIds = new List<string>();
            }
        }

        /// <summary>
        /// セールデータを保存
        /// </summary>
        private void SaveSaleData()
        {
            try
            {
                var saveData = new SaleSaveData { saleEndTimes = saleEndTimes };
                string json = JsonUtility.ToJson(saveData, true);
                PlayerPrefs.SetString(SALE_SAVE_KEY, json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save sale data: {ex.Message}");
            }
        }

        /// <summary>
        /// セールデータを読み込み
        /// </summary>
        private void LoadSaleData()
        {
            try
            {
                if (PlayerPrefs.HasKey(SALE_SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(SALE_SAVE_KEY);
                    var saveData = JsonUtility.FromJson<SaleSaveData>(json);
                    saleEndTimes = saveData.saleEndTimes ?? new Dictionary<string, DateTime>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load sale data: {ex.Message}");
                saleEndTimes = new Dictionary<string, DateTime>();
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
                Debug.Log($"[ShopManager] {message}");
            }
        }

        /// <summary>
        /// 全通貨を最大に設定（デバッグ用）
        /// </summary>
        [ContextMenu("Max All Currency (Debug)")]
        public void MaxAllCurrencyDebug()
        {
            foreach (CurrencyType currency in Enum.GetValues(typeof(CurrencyType)))
            {
                SetCurrency(currency, 999999);
            }
            LogDebug("All currencies set to maximum (Debug)");
        }

        /// <summary>
        /// 購入履歴をクリア（デバッグ用）
        /// </summary>
        [ContextMenu("Clear Purchase History (Debug)")]
        public void ClearPurchaseHistoryDebug()
        {
            purchaseHistory.Clear();
            totalPurchaseCounts.Clear();
            dailyPurchaseCounts.Clear();
            weeklyPurchaseCounts.Clear();
            lastPurchaseDates.Clear();
            SaveAllData();
            LogDebug("Purchase history cleared (Debug)");
        }

        #endregion
    }

    // セーブデータ構造体
    [Serializable] public class CurrencySaveData { public Dictionary<CurrencyType, int> currencies; }
    [Serializable] public class PurchaseCountSaveData { public Dictionary<string, int> totalCounts; public Dictionary<string, int> dailyCounts; public Dictionary<string, int> weeklyCounts; public Dictionary<string, DateTime> lastDates; }
    [Serializable] public class PurchaseHistorySaveData { public List<PurchaseHistory> history; }
    [Serializable] public class WishlistSaveData { public List<string> itemIds; }
    [Serializable] public class SaleSaveData { public Dictionary<string, DateTime> saleEndTimes; }
}