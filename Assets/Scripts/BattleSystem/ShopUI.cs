using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    /// <summary>
    /// ショップUI管理クラス
    /// ショップアイテムの表示、購入処理、通貨表示を管理
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Header("メインUI")]
        [SerializeField] private Transform shopItemParent;      // ショップアイテムの親オブジェクト
        [SerializeField] private GameObject shopItemPrefab;     // ショップアイテムプレハブ
        [SerializeField] private ScrollRect shopScrollRect;     // ショップスクロール
        
        [Header("タブUI")]
        [SerializeField] private Transform tabParent;           // タブの親オブジェクト
        [SerializeField] private GameObject tabButtonPrefab;    // タブボタンプレハブ
        [SerializeField] private ToggleGroup tabToggleGroup;    // タブトグルグループ
        
        [Header("通貨表示UI")]
        [SerializeField] private Text goldText;                 // ゴールド表示
        [SerializeField] private Text gemText;                  // ジェム表示
        [SerializeField] private Text battlePointText;          // バトルポイント表示
        [SerializeField] private Text eventTokenText;           // イベントトークン表示
        [SerializeField] private Text specialCoinText;          // 特別コイン表示
        
        [Header("購入確認UI")]
        [SerializeField] private GameObject purchaseConfirmPanel; // 購入確認パネル
        [SerializeField] private Text confirmItemNameText;      // 確認アイテム名
        [SerializeField] private Text confirmPriceText;         // 確認価格
        [SerializeField] private Text confirmQuantityText;      // 確認数量
        [SerializeField] private Button confirmPurchaseButton;  // 購入確定ボタン
        [SerializeField] private Button cancelPurchaseButton;   // 購入キャンセルボタン
        
        [Header("ウィッシュリストUI")]
        [SerializeField] private GameObject wishlistPanel;      // ウィッシュリストパネル
        [SerializeField] private Transform wishlistParent;      // ウィッシュリストアイテム親
        [SerializeField] private Button wishlistButton;         // ウィッシュリスト表示ボタン
        [SerializeField] private Button closeWishlistButton;    // ウィッシュリスト閉じるボタン
        
        [Header("通知UI")]
        [SerializeField] private GameObject notificationPanel;  // 通知パネル
        [SerializeField] private Text notificationText;         // 通知テキスト
        [SerializeField] private Button closeNotificationButton; // 通知閉じるボタン
        
        [Header("デバッグ設定")]
        [SerializeField] private bool debugMode = false;
        
        // 現在の状態
        private string currentTabId = "";
        private ShopItemData pendingPurchaseItem;
        private int pendingPurchaseQuantity = 1;
        private Dictionary<string, GameObject> shopItemObjects;
        private Dictionary<string, Toggle> tabToggleObjects;
        
        // イベント定義
        public event Action<ShopItemData> OnItemPurchased;      // アイテム購入時
        public event Action<string> OnTabChanged;               // タブ変更時
        public event Action<ShopItemData> OnWishlistToggled;    // ウィッシュリスト変更時

        #region Unity Lifecycle

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            RefreshShop();
        }

        private void OnEnable()
        {
            // ShopManagerのイベントを購読
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnPurchaseAttempt += HandlePurchaseAttempt;
                ShopManager.Instance.OnCurrencyChanged += HandleCurrencyChanged;
                ShopManager.Instance.OnWishlistChanged += HandleWishlistChanged;
                ShopManager.Instance.OnShopRefreshed += HandleShopRefreshed;
            }
        }

        private void OnDisable()
        {
            // ShopManagerのイベント購読を解除
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnPurchaseAttempt -= HandlePurchaseAttempt;
                ShopManager.Instance.OnCurrencyChanged -= HandleCurrencyChanged;
                ShopManager.Instance.OnWishlistChanged -= HandleWishlistChanged;
                ShopManager.Instance.OnShopRefreshed -= HandleShopRefreshed;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// UIの初期化
        /// </summary>
        private void InitializeUI()
        {
            shopItemObjects = new Dictionary<string, GameObject>();
            tabToggleObjects = new Dictionary<string, Toggle>();
            
            // パネルの初期状態設定
            if (purchaseConfirmPanel != null)
                purchaseConfirmPanel.SetActive(false);
            
            if (wishlistPanel != null)
                wishlistPanel.SetActive(false);
            
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
            
            // タブの生成
            CreateShopTabs();
            
            LogDebug("ShopUI initialized");
        }

        /// <summary>
        /// イベントリスナーの設定
        /// </summary>
        private void SetupEventListeners()
        {
            // 購入確認ボタン
            if (confirmPurchaseButton != null)
                confirmPurchaseButton.onClick.AddListener(OnConfirmPurchaseClicked);
            
            if (cancelPurchaseButton != null)
                cancelPurchaseButton.onClick.AddListener(OnCancelPurchaseClicked);
            
            // ウィッシュリストボタン
            if (wishlistButton != null)
                wishlistButton.onClick.AddListener(ShowWishlist);
            
            if (closeWishlistButton != null)
                closeWishlistButton.onClick.AddListener(HideWishlist);
            
            // 通知ボタン
            if (closeNotificationButton != null)
                closeNotificationButton.onClick.AddListener(HideNotification);
        }

        /// <summary>
        /// ショップタブを作成
        /// </summary>
        private void CreateShopTabs()
        {
            if (ShopManager.Instance?.Config == null || tabButtonPrefab == null || tabParent == null)
                return;
            
            var shopTabs = ShopManager.Instance.Config.shopTabs;
            if (shopTabs == null || shopTabs.Count == 0)
                return;
            
            // タブボタンを生成
            foreach (var tab in shopTabs.OrderBy(t => t.displayOrder))
            {
                var tabObject = Instantiate(tabButtonPrefab, tabParent);
                var toggle = tabObject.GetComponent<Toggle>();
                var tabComponent = tabObject.GetComponent<ShopTabButton>();
                
                if (toggle != null && tabComponent != null)
                {
                    // トグルグループに追加
                    if (tabToggleGroup != null)
                        toggle.group = tabToggleGroup;
                    
                    // タブ設定
                    tabComponent.SetupTab(tab);
                    tabComponent.OnTabClicked += HandleTabClicked;
                    
                    tabToggleObjects[tab.tabId] = toggle;
                    
                    // デフォルトタブを選択
                    if (tab.isDefault && string.IsNullOrEmpty(currentTabId))
                    {
                        toggle.isOn = true;
                        currentTabId = tab.tabId;
                    }
                }
            }
        }

        #endregion

        #region Shop Display

        /// <summary>
        /// ショップを更新
        /// </summary>
        public void RefreshShop()
        {
            UpdateCurrencyDisplay();
            RefreshCurrentTab();
            LogDebug("Shop refreshed");
        }

        /// <summary>
        /// 現在のタブを更新
        /// </summary>
        private void RefreshCurrentTab()
        {
            if (string.IsNullOrEmpty(currentTabId) || ShopManager.Instance == null)
                return;
            
            // タブのアイテムを取得
            var tabItems = ShopManager.Instance.GetItemsForTab(currentTabId);
            
            // ショップアイテムUIを更新
            UpdateShopItemsUI(tabItems);
        }

        /// <summary>
        /// ショップアイテムUIを更新
        /// </summary>
        /// <param name="items">表示するアイテムリスト</param>
        private void UpdateShopItemsUI(List<ShopItemData> items)
        {
            // 既存のアイテムをクリア
            ClearShopItems();
            
            // 新しいアイテムを生成
            foreach (var item in items)
            {
                CreateShopItemUI(item);
            }
        }

        /// <summary>
        /// ショップアイテムをクリア
        /// </summary>
        private void ClearShopItems()
        {
            foreach (var item in shopItemObjects.Values)
            {
                if (item != null)
                    Destroy(item);
            }
            shopItemObjects.Clear();
        }

        /// <summary>
        /// ショップアイテムUIを作成
        /// </summary>
        /// <param name="item">ショップアイテム</param>
        private void CreateShopItemUI(ShopItemData item)
        {
            if (shopItemPrefab == null || shopItemParent == null)
                return;
            
            var itemObject = Instantiate(shopItemPrefab, shopItemParent);
            var itemComponent = itemObject.GetComponent<ShopItemUI>();
            
            if (itemComponent != null)
            {
                // アイテムを設定
                itemComponent.SetupShopItem(item);
                itemComponent.OnPurchaseRequested += HandleItemPurchaseRequested;
                itemComponent.OnWishlistRequested += HandleWishlistRequested;
                
                shopItemObjects[item.itemId] = itemObject;
            }
        }

        /// <summary>
        /// 通貨表示を更新
        /// </summary>
        private void UpdateCurrencyDisplay()
        {
            if (ShopManager.Instance == null) return;
            
            if (goldText != null)
                goldText.text = ShopManager.Instance.GetCurrency(CurrencyType.Gold).ToString("#,0");
            
            if (gemText != null)
                gemText.text = ShopManager.Instance.GetCurrency(CurrencyType.Gem).ToString("#,0");
            
            if (battlePointText != null)
                battlePointText.text = ShopManager.Instance.GetCurrency(CurrencyType.BattlePoint).ToString("#,0");
            
            if (eventTokenText != null)
                eventTokenText.text = ShopManager.Instance.GetCurrency(CurrencyType.EventToken).ToString("#,0");
            
            if (specialCoinText != null)
                specialCoinText.text = ShopManager.Instance.GetCurrency(CurrencyType.SpecialCoin).ToString("#,0");
        }

        #endregion

        #region Purchase System

        /// <summary>
        /// 購入確認を表示
        /// </summary>
        /// <param name="item">購入アイテム</param>
        /// <param name="quantity">購入数量</param>
        private void ShowPurchaseConfirmation(ShopItemData item, int quantity)
        {
            if (purchaseConfirmPanel == null || ShopManager.Instance == null)
                return;
            
            pendingPurchaseItem = item;
            pendingPurchaseQuantity = quantity;
            
            // 確認情報を設定
            if (confirmItemNameText != null)
                confirmItemNameText.text = item.itemName;
            
            if (confirmQuantityText != null)
                confirmQuantityText.text = $"数量: {quantity}";
            
            if (confirmPriceText != null)
            {
                int totalPrice = item.price.GetActualPrice() * quantity;
                confirmPriceText.text = $"価格: {totalPrice:N0} {item.price.currencyType}";
                
                // セール情報も表示
                if (item.price.isOnSale)
                {
                    int savings = item.price.GetSavings() * quantity;
                    confirmPriceText.text += $"\n(通常: {item.price.originalAmount * quantity:N0}, {savings:N0} お得!)";
                }
            }
            
            // 購入可能性をチェック
            bool canPurchase = ShopManager.Instance.CanPurchaseItem(item, quantity);
            if (confirmPurchaseButton != null)
                confirmPurchaseButton.interactable = canPurchase;
            
            purchaseConfirmPanel.SetActive(true);
        }

        /// <summary>
        /// 購入確認を非表示
        /// </summary>
        private void HidePurchaseConfirmation()
        {
            if (purchaseConfirmPanel != null)
                purchaseConfirmPanel.SetActive(false);
            
            pendingPurchaseItem = null;
            pendingPurchaseQuantity = 1;
        }

        #endregion

        #region Wishlist Management

        /// <summary>
        /// ウィッシュリストを表示
        /// </summary>
        public void ShowWishlist()
        {
            if (wishlistPanel == null || ShopManager.Instance == null)
                return;
            
            var wishlistItems = ShopManager.Instance.WishlistItems;
            var items = new List<ShopItemData>();
            
            foreach (string itemId in wishlistItems)
            {
                var item = ShopManager.Instance.GetShopItemById(itemId);
                if (item != null)
                    items.Add(item);
            }
            
            // ウィッシュリストアイテムを表示
            UpdateWishlistUI(items);
            
            wishlistPanel.SetActive(true);
        }

        /// <summary>
        /// ウィッシュリストを非表示
        /// </summary>
        public void HideWishlist()
        {
            if (wishlistPanel != null)
                wishlistPanel.SetActive(false);
        }

        /// <summary>
        /// ウィッシュリストUIを更新
        /// </summary>
        /// <param name="items">ウィッシュリストアイテム</param>
        private void UpdateWishlistUI(List<ShopItemData> items)
        {
            if (wishlistParent == null) return;
            
            // 既存のウィッシュリストアイテムをクリア
            foreach (Transform child in wishlistParent)
            {
                Destroy(child.gameObject);
            }
            
            // ウィッシュリストアイテムを生成
            foreach (var item in items)
            {
                var itemObject = Instantiate(shopItemPrefab, wishlistParent);
                var itemComponent = itemObject.GetComponent<ShopItemUI>();
                
                if (itemComponent != null)
                {
                    itemComponent.SetupShopItem(item, true); // ウィッシュリストモード
                    itemComponent.OnPurchaseRequested += HandleItemPurchaseRequested;
                    itemComponent.OnWishlistRequested += HandleWishlistRequested;
                }
            }
        }

        #endregion

        #region Notification System

        /// <summary>
        /// 通知を表示
        /// </summary>
        /// <param name="message">通知メッセージ</param>
        /// <param name="duration">表示時間（秒）</param>
        public void ShowNotification(string message, float duration = 3f)
        {
            if (notificationPanel == null || notificationText == null)
                return;
            
            notificationText.text = message;
            notificationPanel.SetActive(true);
            
            // 自動で非表示にする
            if (duration > 0f)
            {
                Invoke(nameof(HideNotification), duration);
            }
        }

        /// <summary>
        /// 通知を非表示
        /// </summary>
        public void HideNotification()
        {
            if (notificationPanel != null)
                notificationPanel.SetActive(false);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// タブクリック時の処理
        /// </summary>
        /// <param name="tabId">タブID</param>
        private void HandleTabClicked(string tabId)
        {
            if (currentTabId != tabId)
            {
                currentTabId = tabId;
                RefreshCurrentTab();
                OnTabChanged?.Invoke(tabId);
                LogDebug($"Tab changed to: {tabId}");
            }
        }

        /// <summary>
        /// アイテム購入要求時の処理
        /// </summary>
        /// <param name="item">購入要求アイテム</param>
        /// <param name="quantity">購入数量</param>
        private void HandleItemPurchaseRequested(ShopItemData item, int quantity)
        {
            if (ShopManager.Instance?.Config?.showPurchaseConfirmation == true)
            {
                ShowPurchaseConfirmation(item, quantity);
            }
            else
            {
                // 確認なしで直接購入
                if (ShopManager.Instance.PurchaseItem(item.itemId, quantity))
                {
                    ShowNotification($"{item.itemName} を購入しました！");
                }
            }
        }

        /// <summary>
        /// ウィッシュリスト要求時の処理
        /// </summary>
        /// <param name="item">対象アイテム</param>
        private void HandleWishlistRequested(ShopItemData item)
        {
            if (ShopManager.Instance == null) return;
            
            if (ShopManager.Instance.IsInWishlist(item.itemId))
            {
                ShopManager.Instance.RemoveFromWishlist(item.itemId);
                ShowNotification($"{item.itemName} をウィッシュリストから削除しました");
            }
            else
            {
                ShopManager.Instance.AddToWishlist(item.itemId);
                ShowNotification($"{item.itemName} をウィッシュリストに追加しました");
            }
        }

        /// <summary>
        /// 購入確定ボタンクリック時の処理
        /// </summary>
        private void OnConfirmPurchaseClicked()
        {
            if (pendingPurchaseItem != null && ShopManager.Instance != null)
            {
                if (ShopManager.Instance.PurchaseItem(pendingPurchaseItem.itemId, pendingPurchaseQuantity))
                {
                    ShowNotification($"{pendingPurchaseItem.itemName} を購入しました！");
                    OnItemPurchased?.Invoke(pendingPurchaseItem);
                }
            }
            
            HidePurchaseConfirmation();
        }

        /// <summary>
        /// 購入キャンセルボタンクリック時の処理
        /// </summary>
        private void OnCancelPurchaseClicked()
        {
            HidePurchaseConfirmation();
        }

        /// <summary>
        /// 購入試行時の処理
        /// </summary>
        /// <param name="item">購入アイテム</param>
        /// <param name="success">成功フラグ</param>
        private void HandlePurchaseAttempt(ShopItemData item, bool success)
        {
            if (!success && item != null)
            {
                ShowNotification($"{item.itemName} の購入に失敗しました");
            }
            
            // UI更新
            RefreshShop();
        }

        /// <summary>
        /// 通貨変更時の処理
        /// </summary>
        /// <param name="currencyType">通貨タイプ</param>
        /// <param name="oldAmount">変更前の量</param>
        /// <param name="newAmount">変更後の量</param>
        private void HandleCurrencyChanged(CurrencyType currencyType, int oldAmount, int newAmount)
        {
            UpdateCurrencyDisplay();
        }

        /// <summary>
        /// ウィッシュリスト変更時の処理
        /// </summary>
        /// <param name="itemId">変更されたアイテムID</param>
        private void HandleWishlistChanged(string itemId)
        {
            // ウィッシュリストが表示中の場合は更新
            if (wishlistPanel != null && wishlistPanel.activeSelf)
            {
                ShowWishlist();
            }
            
            OnWishlistToggled?.Invoke(ShopManager.Instance?.GetShopItemById(itemId));
        }

        /// <summary>
        /// ショップ更新時の処理
        /// </summary>
        private void HandleShopRefreshed()
        {
            RefreshShop();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        /// <param name="message">メッセージ</param>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[ShopUI] {message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 特定のタブを選択
        /// </summary>
        /// <param name="tabId">タブID</param>
        public void SelectTab(string tabId)
        {
            if (tabToggleObjects.ContainsKey(tabId))
            {
                tabToggleObjects[tabId].isOn = true;
            }
        }

        /// <summary>
        /// 特定のアイテムにスクロール
        /// </summary>
        /// <param name="itemId">アイテムID</param>
        public void ScrollToItem(string itemId)
        {
            if (shopItemObjects.ContainsKey(itemId) && shopScrollRect != null)
            {
                var itemTransform = shopItemObjects[itemId].transform;
                // スクロール位置計算（実装時に詳細化）
                LogDebug($"Scrolling to item: {itemId}");
            }
        }

        #endregion
    }

    /// <summary>
    /// ショップタブボタンコンポーネント
    /// </summary>
    public class ShopTabButton : MonoBehaviour
    {
        [Header("UI要素")]
        [SerializeField] private Text tabNameText;      // タブ名
        [SerializeField] private Image tabIcon;         // タブアイコン
        [SerializeField] private GameObject newBadge;   // 新着バッジ
        
        private ShopTab tabData;
        
        // イベント定義
        public event Action<string> OnTabClicked;

        /// <summary>
        /// タブを設定
        /// </summary>
        /// <param name="tab">タブデータ</param>
        public void SetupTab(ShopTab tab)
        {
            tabData = tab;
            
            if (tabNameText != null)
                tabNameText.text = tab.tabName;
            
            if (tabIcon != null && tab.tabIcon != null)
                tabIcon.sprite = tab.tabIcon;
            
            if (newBadge != null)
                newBadge.SetActive(false); // 新着表示の実装は後で
            
            // Toggle コンポーネントのイベントをフック
            var toggle = GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }

        /// <summary>
        /// トグル値変更時の処理
        /// </summary>
        /// <param name="isOn">選択状態</param>
        private void OnToggleValueChanged(bool isOn)
        {
            if (isOn && tabData != null)
            {
                OnTabClicked?.Invoke(tabData.tabId);
            }
        }
    }

    /// <summary>
    /// ショップアイテムUIコンポーネント
    /// </summary>
    public class ShopItemUI : MonoBehaviour
    {
        [Header("UI要素")]
        [SerializeField] private Text itemNameText;         // アイテム名
        [SerializeField] private Text priceText;            // 価格
        [SerializeField] private Text originalPriceText;    // 元価格（セール時）
        [SerializeField] private Image itemIcon;            // アイテムアイコン
        [SerializeField] private Image rarityBorder;        // 希少度枠
        [SerializeField] private Button purchaseButton;     // 購入ボタン
        [SerializeField] private Button wishlistButton;     // ウィッシュリストボタン
        [SerializeField] private GameObject saleIcon;       // セールアイコン
        [SerializeField] private GameObject newIcon;        // 新商品アイコン
        [SerializeField] private GameObject recommendedIcon; // おすすめアイコン
        
        private ShopItemData itemData;
        private bool isWishlistMode = false;
        
        // イベント定義
        public event Action<ShopItemData, int> OnPurchaseRequested;
        public event Action<ShopItemData> OnWishlistRequested;

        /// <summary>
        /// ショップアイテムを設定
        /// </summary>
        /// <param name="item">アイテムデータ</param>
        /// <param name="wishlistMode">ウィッシュリストモードか</param>
        public void SetupShopItem(ShopItemData item, bool wishlistMode = false)
        {
            itemData = item;
            isWishlistMode = wishlistMode;
            
            UpdateItemUI();
            SetupButtonEvents();
        }

        /// <summary>
        /// アイテムUIを更新
        /// </summary>
        private void UpdateItemUI()
        {
            if (itemData == null) return;
            
            // アイテム名
            if (itemNameText != null)
                itemNameText.text = itemData.itemName;
            
            // 価格表示
            UpdatePriceDisplay();
            
            // アイテムアイコン
            if (itemIcon != null && itemData.itemIcon != null)
                itemIcon.sprite = itemData.itemIcon;
            
            // 希少度枠
            if (rarityBorder != null)
                rarityBorder.color = itemData.GetRarityColor();
            
            // 各種アイコン
            if (saleIcon != null)
                saleIcon.SetActive(itemData.price.isOnSale);
            
            if (newIcon != null)
                newIcon.SetActive(itemData.isNew);
            
            if (recommendedIcon != null)
                recommendedIcon.SetActive(itemData.isRecommended);
            
            // ボタン状態
            UpdateButtonStates();
        }

        /// <summary>
        /// 価格表示を更新
        /// </summary>
        private void UpdatePriceDisplay()
        {
            if (priceText != null)
            {
                int actualPrice = itemData.price.GetActualPrice();
                priceText.text = $"{actualPrice:N0} {itemData.price.currencyType}";
                
                if (itemData.price.isOnSale)
                {
                    priceText.color = Color.red; // セール価格は赤
                }
            }
            
            // 元価格表示（セール時）
            if (originalPriceText != null)
            {
                if (itemData.price.isOnSale && itemData.price.originalAmount > 0)
                {
                    originalPriceText.text = $"{itemData.price.originalAmount:N0}";
                    originalPriceText.gameObject.SetActive(true);
                }
                else
                {
                    originalPriceText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// ボタン状態を更新
        /// </summary>
        private void UpdateButtonStates()
        {
            // 購入ボタン
            if (purchaseButton != null && ShopManager.Instance != null)
            {
                bool canPurchase = ShopManager.Instance.CanPurchaseItem(itemData);
                purchaseButton.interactable = canPurchase;
                
                var buttonText = purchaseButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    if (isWishlistMode)
                        buttonText.text = "購入";
                    else if (!canPurchase)
                        buttonText.text = "購入不可";
                    else
                        buttonText.text = $"{itemData.price.GetActualPrice():N0}";
                }
            }
            
            // ウィッシュリストボタン
            if (wishlistButton != null && ShopManager.Instance != null)
            {
                bool isInWishlist = ShopManager.Instance.IsInWishlist(itemData.itemId);
                var wishlistIcon = wishlistButton.GetComponent<Image>();
                if (wishlistIcon != null)
                {
                    wishlistIcon.color = isInWishlist ? Color.yellow : Color.white;
                }
            }
        }

        /// <summary>
        /// ボタンイベントを設定
        /// </summary>
        private void SetupButtonEvents()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
            }
            
            if (wishlistButton != null)
            {
                wishlistButton.onClick.RemoveAllListeners();
                wishlistButton.onClick.AddListener(OnWishlistButtonClicked);
            }
        }

        /// <summary>
        /// 購入ボタンクリック時の処理
        /// </summary>
        private void OnPurchaseButtonClicked()
        {
            OnPurchaseRequested?.Invoke(itemData, 1); // 数量は1固定（将来的に拡張可能）
        }

        /// <summary>
        /// ウィッシュリストボタンクリック時の処理
        /// </summary>
        private void OnWishlistButtonClicked()
        {
            OnWishlistRequested?.Invoke(itemData);
        }
    }
}