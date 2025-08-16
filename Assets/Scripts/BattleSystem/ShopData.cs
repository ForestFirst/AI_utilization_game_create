using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 通貨タイプ
    /// </summary>
    public enum CurrencyType
    {
        Gold,           // ゴールド（基本通貨）
        Gem,            // ジェム（プレミアム通貨）
        BattlePoint,    // バトルポイント（戦闘で獲得）
        EventToken,     // イベントトークン（期間限定）
        SpecialCoin     // 特別コイン（特定条件で獲得）
    }

    /// <summary>
    /// ショップアイテムタイプ
    /// </summary>
    public enum ShopItemType
    {
        Weapon,         // 武器
        Consumable,     // 消耗品
        Equipment,      // 装備品
        Material,       // 素材
        Special,        // 特別アイテム
        Bundle,         // セット商品
        Subscription    // サブスクリプション
    }

    /// <summary>
    /// アイテム希少度
    /// </summary>
    public enum ItemRarity
    {
        Common = 1,     // コモン（白）
        Uncommon = 2,   // アンコモン（緑）
        Rare = 3,       // レア（青）
        Epic = 4,       // エピック（紫）
        Legendary = 5   // レジェンダリー（金）
    }

    /// <summary>
    /// ショップアイテムの価格データ
    /// </summary>
    [Serializable]
    public class ItemPrice
    {
        [Header("価格設定")]
        public CurrencyType currencyType;   // 通貨タイプ
        public int amount;                  // 価格
        public int originalAmount;          // 元価格（セール時用）
        public bool isOnSale;              // セール中か
        public float discountRate;         // 割引率（0.0-1.0）

        [Header("特別価格設定")]
        public bool hasSpecialPrice;       // 特別価格があるか
        public List<CurrencyType> alternativeCurrencies; // 代替通貨タイプ
        public List<int> alternativeAmounts;             // 代替通貨での価格

        public ItemPrice()
        {
            currencyType = CurrencyType.Gold;
            amount = 0;
            originalAmount = 0;
            isOnSale = false;
            discountRate = 0f;
            hasSpecialPrice = false;
            alternativeCurrencies = new List<CurrencyType>();
            alternativeAmounts = new List<int>();
        }

        /// <summary>
        /// 実際の価格を取得（セール考慮）
        /// </summary>
        /// <returns>実際の価格</returns>
        public int GetActualPrice()
        {
            if (isOnSale && originalAmount > 0)
            {
                return Mathf.RoundToInt(originalAmount * (1f - discountRate));
            }
            return amount;
        }

        /// <summary>
        /// 節約額を取得
        /// </summary>
        /// <returns>節約額</returns>
        public int GetSavings()
        {
            if (isOnSale && originalAmount > 0)
            {
                return originalAmount - GetActualPrice();
            }
            return 0;
        }
    }

    /// <summary>
    /// ショップアイテムデータ
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NewShopItem", menuName = "BattleSystem/Shop Item")]
    public class ShopItemData : ScriptableObject
    {
        [Header("アイテム基本情報")]
        public string itemId;               // アイテム固有ID
        public string itemName;             // アイテム名
        public string description;          // アイテム説明
        public ShopItemType itemType;       // アイテムタイプ
        public ItemRarity rarity;           // 希少度
        
        [Header("価格設定")]
        public ItemPrice price;             // 価格データ
        
        [Header("購入制限")]
        public int maxPurchaseCount;        // 最大購入可能数（0=無制限）
        public int dailyPurchaseLimit;      // 日次購入制限（0=無制限）
        public int weeklyPurchaseLimit;     // 週次購入制限（0=無制限）
        public bool isLimitedTime;          // 期間限定か
        public DateTime saleStartTime;      // 販売開始時間
        public DateTime saleEndTime;        // 販売終了時間
        
        [Header("購入条件")]
        public int requiredPlayerLevel;     // 必要プレイヤーレベル
        public List<string> prerequisiteItems; // 前提アイテムIDリスト
        public List<string> prerequisiteStages; // 前提ステージIDリスト
        
        [Header("アイテム内容")]
        public string targetItemId;         // 対象アイテムID（武器ID、消耗品IDなど）
        public int quantity;                // 数量
        public List<string> bundleItemIds;  // バンドル内容（セット商品用）
        public List<int> bundleQuantities;  // バンドル数量
        
        [Header("表示設定")]
        public Sprite itemIcon;             // アイテムアイコン
        public bool showInMainShop;         // メインショップに表示するか
        public bool showInEventShop;        // イベントショップに表示するか
        public int displayOrder;            // 表示順序
        public bool isRecommended;          // おすすめ商品か
        public bool isNew;                  // 新商品か

        /// <summary>
        /// 現在購入可能かチェック
        /// </summary>
        /// <param name="playerLevel">プレイヤーレベル</param>
        /// <param name="ownedItems">所持アイテムリスト</param>
        /// <param name="clearedStages">クリア済みステージリスト</param>
        /// <returns>購入可能な場合true</returns>
        public bool CanPurchase(int playerLevel, List<string> ownedItems, List<string> clearedStages)
        {
            // レベル制限チェック
            if (playerLevel < requiredPlayerLevel)
                return false;
            
            // 期間限定チェック
            if (isLimitedTime)
            {
                var now = DateTime.Now;
                if (now < saleStartTime || now > saleEndTime)
                    return false;
            }
            
            // 前提アイテムチェック
            if (prerequisiteItems != null && prerequisiteItems.Count > 0)
            {
                foreach (string prerequisiteItem in prerequisiteItems)
                {
                    if (!ownedItems.Contains(prerequisiteItem))
                        return false;
                }
            }
            
            // 前提ステージチェック
            if (prerequisiteStages != null && prerequisiteStages.Count > 0)
            {
                foreach (string prerequisiteStage in prerequisiteStages)
                {
                    if (!clearedStages.Contains(prerequisiteStage))
                        return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 希少度に基づく色を取得
        /// </summary>
        /// <returns>希少度色</returns>
        public Color GetRarityColor()
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return Color.white;
                case ItemRarity.Uncommon:
                    return Color.green;
                case ItemRarity.Rare:
                    return Color.blue;
                case ItemRarity.Epic:
                    return Color.magenta;
                case ItemRarity.Legendary:
                    return Color.yellow;
                default:
                    return Color.gray;
            }
        }

        /// <summary>
        /// 通貨タイプのアイコン名を取得
        /// </summary>
        /// <returns>通貨アイコン名</returns>
        public string GetCurrencyIcon()
        {
            switch (price.currencyType)
            {
                case CurrencyType.Gold:
                    return "icon_gold";
                case CurrencyType.Gem:
                    return "icon_gem";
                case CurrencyType.BattlePoint:
                    return "icon_battle_point";
                case CurrencyType.EventToken:
                    return "icon_event_token";
                case CurrencyType.SpecialCoin:
                    return "icon_special_coin";
                default:
                    return "icon_unknown";
            }
        }

        /// <summary>
        /// デバッグ情報の取得
        /// </summary>
        /// <returns>デバッグ情報文字列</returns>
        public string GetDebugInfo()
        {
            return $"ShopItem[{itemId}]: {itemName} | Type: {itemType} | Rarity: {rarity} | Price: {price.GetActualPrice()} {price.currencyType}";
        }
    }

    /// <summary>
    /// 購入履歴データ
    /// </summary>
    [Serializable]
    public class PurchaseHistory
    {
        public string itemId;               // 購入アイテムID
        public DateTime purchaseTime;       // 購入時刻
        public int quantity;                // 購入数量
        public CurrencyType currencyUsed;   // 使用通貨
        public int amountPaid;              // 支払い額
        public bool wasOnSale;              // セール時購入か

        public PurchaseHistory(string id, int qty, CurrencyType currency, int amount, bool sale)
        {
            itemId = id;
            purchaseTime = DateTime.Now;
            quantity = qty;
            currencyUsed = currency;
            amountPaid = amount;
            wasOnSale = sale;
        }
    }

    /// <summary>
    /// ショップタブ定義
    /// </summary>
    [Serializable]
    public class ShopTab
    {
        [Header("タブ情報")]
        public string tabId;                // タブID
        public string tabName;              // タブ名
        public string tabDescription;       // タブ説明
        public Sprite tabIcon;              // タブアイコン
        
        [Header("フィルター設定")]
        public List<ShopItemType> allowedItemTypes;    // 表示可能アイテムタイプ
        public List<ItemRarity> allowedRarities;       // 表示可能希少度
        public bool showOnSaleOnly;                     // セール商品のみ表示
        public bool showNewOnly;                        // 新商品のみ表示
        
        [Header("表示設定")]
        public int displayOrder;            // 表示順序
        public bool isDefault;              // デフォルトタブか
        public bool isEventTab;             // イベントタブか

        public ShopTab()
        {
            tabId = "";
            tabName = "";
            tabDescription = "";
            allowedItemTypes = new List<ShopItemType>();
            allowedRarities = new List<ItemRarity>();
            showOnSaleOnly = false;
            showNewOnly = false;
            displayOrder = 0;
            isDefault = false;
            isEventTab = false;
        }

        /// <summary>
        /// アイテムがこのタブに表示可能かチェック
        /// </summary>
        /// <param name="item">ショップアイテム</param>
        /// <returns>表示可能な場合true</returns>
        public bool CanDisplayItem(ShopItemData item)
        {
            // アイテムタイプチェック
            if (allowedItemTypes.Count > 0 && !allowedItemTypes.Contains(item.itemType))
                return false;
            
            // 希少度チェック
            if (allowedRarities.Count > 0 && !allowedRarities.Contains(item.rarity))
                return false;
            
            // セール限定チェック
            if (showOnSaleOnly && !item.price.isOnSale)
                return false;
            
            // 新商品限定チェック
            if (showNewOnly && !item.isNew)
                return false;
            
            return true;
        }
    }

    /// <summary>
    /// ショップ設定データ
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "BattleSystem/Shop Config")]
    public class ShopConfig : ScriptableObject
    {
        [Header("ショップ基本設定")]
        public List<ShopTab> shopTabs;              // ショップタブ一覧
        public int itemsPerPage;                    // 1ページあたりのアイテム数
        public bool enableAutoRefresh;              // 自動更新を有効にするか
        public float autoRefreshInterval;           // 自動更新間隔（時間）
        
        [Header("セール設定")]
        public bool enableRandomSales;              // ランダムセールを有効にするか
        public int maxSaleItems;                    // 同時セール最大アイテム数
        public float saleChance;                    // セール発生確率（0.0-1.0）
        public float minDiscountRate;               // 最小割引率
        public float maxDiscountRate;               // 最大割引率
        
        [Header("通貨設定")]
        public List<CurrencyType> supportedCurrencies; // サポート通貨一覧
        public Dictionary<CurrencyType, int> startingCurrency; // 初期通貨
        
        [Header("UI設定")]
        public bool showPurchaseConfirmation;       // 購入確認を表示するか
        public bool enableWishlist;                 // ウィッシュリスト機能を有効にするか
        public bool showPurchaseHistory;            // 購入履歴を表示するか
        public bool enableItemPreview;              // アイテムプレビューを有効にするか

        /// <summary>
        /// デフォルトタブを取得
        /// </summary>
        /// <returns>デフォルトタブ（見つからない場合null）</returns>
        public ShopTab GetDefaultTab()
        {
            foreach (var tab in shopTabs)
            {
                if (tab.isDefault)
                    return tab;
            }
            return shopTabs.Count > 0 ? shopTabs[0] : null;
        }

        /// <summary>
        /// タブIDからタブを取得
        /// </summary>
        /// <param name="tabId">タブID</param>
        /// <returns>タブ（見つからない場合null）</returns>
        public ShopTab GetTabById(string tabId)
        {
            foreach (var tab in shopTabs)
            {
                if (tab.tabId == tabId)
                    return tab;
            }
            return null;
        }
    }
}