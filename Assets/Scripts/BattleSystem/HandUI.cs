using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace BattleSystem
{
    /// <summary>
    /// 手札UI表示・操作システム
    /// トランプゲーム風の手札表示と選択機能を提供
    /// </summary>
    public class HandUI : MonoBehaviour
    {
        [Header("Hand UI Settings")]
        [SerializeField] private bool autoCreateHandUI = true;
        [SerializeField] private Vector2 handPosition = new Vector2(0, -300);
        [SerializeField] private float cardSpacing = 160f;
        [SerializeField] private Vector2 cardSize = new Vector2(140f, 180f);
        
        // 日本語対応フォントアセット
        private TMP_FontAsset japaneseFont;
        
        // UI要素
        private Canvas canvas;
        private BattleManager battleManager;
        private HandSystem handSystem;
        
        // 手札UI要素
        private GameObject handPanel;
        private Button[] cardButtons = new Button[5];
        private TextMeshProUGUI[] cardTexts = new TextMeshProUGUI[5];
        private Image[] cardImages = new Image[5];
        private TextMeshProUGUI handStatusText;
        private TextMeshProUGUI instructionText;
        private TextMeshProUGUI actionsRemainingText;  // 残り行動回数表示
        
        // 状態管理
        private int selectedCardIndex = -1;
        private List<CardData> currentHand = new List<CardData>();
        
        // カード色設定
        private readonly Color[] attributeColors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f, 0.8f),   // Fire - 赤
            new Color(0.3f, 0.7f, 1f, 0.8f),   // Ice - 水色
            new Color(1f, 1f, 0.3f, 0.8f),     // Thunder - 黄色
            new Color(0.6f, 1f, 0.6f, 0.8f),   // Wind - 緑
            new Color(0.8f, 0.5f, 0.3f, 0.8f), // Earth - 茶色
            new Color(1f, 1f, 1f, 0.8f),       // Light - 白
            new Color(0.5f, 0.3f, 0.8f, 0.8f), // Dark - 紫
            new Color(0.7f, 0.7f, 0.7f, 0.8f)  // None - 灰色
        };
        
        void Start()
        {
            SetupComponents();
            LoadJapaneseFont();
            
            if (autoCreateHandUI)
            {
                CreateHandUI();
            }
            
            SetupBattleManagerConnection();
        }
        
        /// <summary>
        /// 必要なコンポーネントを設定
        /// </summary>
        void SetupComponents()
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found! HandUI requires a Canvas in the scene.");
                return;
            }
            
            Debug.Log("HandUI: Canvas found and connected");
        }
        
        /// <summary>
        /// 日本語フォントを読み込み
        /// </summary>
        void LoadJapaneseFont()
        {
            japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/DotGothic16-Regular SDF");
            
            if (japaneseFont != null)
            {
                Debug.Log($"HandUI: Japanese font loaded - {japaneseFont.name}");
            }
            else
            {
                Debug.LogWarning("HandUI: Failed to load Japanese font! Using fallback...");
                japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
        }
        
        /// <summary>
        /// BattleManagerとHandSystemとの接続を設定
        /// </summary>
        void SetupBattleManagerConnection()
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                Debug.LogError("HandUI: BattleManager not found!");
                return;
            }
            
            handSystem = FindObjectOfType<HandSystem>();
            if (handSystem == null)
            {
                Debug.LogError("HandUI: HandSystem not found!");
                return;
            }
            
            // HandSystemのイベントを購読
            SubscribeToHandSystemEvents();
            
            Debug.Log("HandUI: Connected to BattleManager and HandSystem");
        }
        
        /// <summary>
        /// HandSystemのイベントを購読
        /// </summary>
        void SubscribeToHandSystemEvents()
        {
            if (handSystem == null) return;
            
            handSystem.OnHandGenerated += OnHandGenerated;
            handSystem.OnCardPlayed += OnCardPlayed;
            handSystem.OnHandStateChanged += OnHandStateChanged;
            handSystem.OnHandCleared += OnHandCleared;
            handSystem.OnActionsChanged += OnActionsChanged;
            handSystem.OnActionsExhausted += OnActionsExhausted;
            handSystem.OnAutoTurnEnd += OnAutoTurnEnd;
            
            Debug.Log("HandUI: HandSystem events subscribed");
        }
        
        void OnDestroy()
        {
            // イベントの購読解除
            if (handSystem != null)
            {
                handSystem.OnHandGenerated -= OnHandGenerated;
                handSystem.OnCardPlayed -= OnCardPlayed;
                handSystem.OnHandStateChanged -= OnHandStateChanged;
                handSystem.OnHandCleared -= OnHandCleared;
                handSystem.OnActionsChanged -= OnActionsChanged;
                handSystem.OnActionsExhausted -= OnActionsExhausted;
                handSystem.OnAutoTurnEnd -= OnAutoTurnEnd;
            }
        }
        
        /// <summary>
        /// 手札UIを作成
        /// </summary>
        void CreateHandUI()
        {
            if (canvas == null)
            {
                Debug.LogError("Cannot create HandUI: Canvas is null");
                return;
            }
            
            Debug.Log("Creating Hand UI...");
            
            // 画面サイズ取得
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float screenWidth = canvasRect.rect.width;
            float screenHeight = canvasRect.rect.height;
            
            // スケールファクター計算
            float scaleX = screenWidth / 1920f;
            float scaleY = screenHeight / 1080f;
            float scale = Mathf.Min(scaleX, scaleY);
            
            // 手札パネル作成（画面下部中央）
            handPanel = CreateUIPanel("Hand Panel", 
                new Vector2(handPosition.x, handPosition.y * scale),
                new Vector2(cardSpacing * 5.5f * scale, cardSize.y * 1.2f * scale),
                new Color(0.1f, 0.1f, 0.3f, 0.8f));
            
            // 手札状態表示テキスト（手札パネル上部）
            handStatusText = CreateUIText("Hand Status", 
                new Vector2(handPosition.x, (handPosition.y + cardSize.y * 0.7f) * scale),
                new Vector2(400f * scale, 30f * scale),
                "手札: 生成待ち", 16 * scale);
            handStatusText.color = Color.yellow;
            
            // 操作説明テキスト（手札パネル下部）
            instructionText = CreateUIText("Instruction", 
                new Vector2(handPosition.x, (handPosition.y - cardSize.y * 0.7f) * scale),
                new Vector2(600f * scale, 40f * scale),
                "カードをクリックして攻撃を選択してください", 14 * scale);
            instructionText.color = Color.white;
            
            // 行動回数表示テキスト（手札パネル右上）
            actionsRemainingText = CreateUIText("Actions Remaining", 
                new Vector2(handPosition.x + cardSpacing * 2.5f * scale, (handPosition.y + cardSize.y * 0.7f) * scale),
                new Vector2(200f * scale, 30f * scale),
                "行動回数: 1/1", 18 * scale);
            actionsRemainingText.color = Color.cyan;
            actionsRemainingText.alignment = TextAlignmentOptions.Right;
            
            // 5枚のカードボタンを作成
            CreateCardButtons(scale);
            
            // 🔧 重要: 戦闘開始前は手札UIを非表示にする
            SetHandUIVisible(false);
            
            Debug.Log("Hand UI created successfully (initially hidden)!");
        }
        
        /// <summary>
        /// カードボタンを作成
        /// </summary>
        void CreateCardButtons(float scale)
        {
            for (int i = 0; i < 5; i++)
            {
                int cardIndex = i; // クロージャー問題回避
                
                // カードの位置計算（中央から左右に展開）
                float cardX = handPosition.x + (cardIndex - 2) * cardSpacing * scale;
                float cardY = handPosition.y * scale;
                
                Vector2 cardPos = new Vector2(cardX, cardY);
                Vector2 scaledCardSize = cardSize * scale;
                
                // カードボタン作成
                cardButtons[cardIndex] = CreateCardButton($"Card {cardIndex + 1}", 
                    cardPos, scaledCardSize, 
                    () => OnCardClicked(cardIndex));
                
                // カード画像取得
                cardImages[cardIndex] = cardButtons[cardIndex].GetComponent<Image>();
                
                // カードテキスト作成
                cardTexts[cardIndex] = CreateCardText(cardButtons[cardIndex], 
                    "空札", 12 * scale);
                
                // 初期状態では非表示
                cardButtons[cardIndex].gameObject.SetActive(false);
            }
            
            Debug.Log("Card buttons created");
        }
        
        /// <summary>
        /// UIパネルを作成
        /// </summary>
        GameObject CreateUIPanel(string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = panel.AddComponent<Image>();
            image.color = color;
            
            return panel;
        }
        
        /// <summary>
        /// UIテキストを作成
        /// </summary>
        TextMeshProUGUI CreateUIText(string name, Vector2 position, Vector2 size, string text, float fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            // 日本語フォントを適用
            if (japaneseFont != null)
            {
                textComponent.font = japaneseFont;
            }
            
            return textComponent;
        }
        
        /// <summary>
        /// カードボタンを作成
        /// </summary>
        Button CreateCardButton(string name, Vector2 position, Vector2 size, System.Action onClick)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.9f); // デフォルト色
            
            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // カード枠線の追加
            Outline outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);
            
            return button;
        }
        
        /// <summary>
        /// カードテキストを作成
        /// </summary>
        TextMeshProUGUI CreateCardText(Button cardButton, string text, float fontSize)
        {
            GameObject textObj = new GameObject("Card Text");
            textObj.transform.SetParent(cardButton.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Top;
            textComponent.enableWordWrapping = true; // 修正: wordWrapping -> enableWordWrapping
            
            // 日本語フォントを適用
            if (japaneseFont != null)
            {
                textComponent.font = japaneseFont;
            }
            
            return textComponent;
        }
        
        // === HandSystem イベントハンドラー ===
        
        /// <summary>
        /// 手札生成時の処理
        /// </summary>
        void OnHandGenerated(CardData[] newHand) // 修正: List<CardData> -> CardData[]
        {
            Debug.Log($"HandUI: Hand generated with {newHand.Length} cards");
            
            currentHand.Clear();
            foreach(var card in newHand)
            {
                if (card != null)
                    currentHand.Add(card);
            }
            selectedCardIndex = -1;
            
            // 手札表示を強制更新
            UpdateHandDisplay();
            SetHandInteractable(true); // カードを再度選択可能に
            UpdateHandStatusText("手札: 準備完了");
            UpdateInstructionText("カードをクリックして攻撃を選択してください");
            
            Debug.Log($"HandUI: Hand display updated with {currentHand.Count} cards, interactable: true");
        }
        
        /// <summary>
        /// カード使用時の処理
        /// </summary>
        void OnCardPlayed(CardData playedCard) // 修正: CardData playedCard, int handIndex -> CardData playedCard
        {
            Debug.Log($"HandUI: Card played - {playedCard.displayName}");
            
            // 使用したカードを手札から除去（視覚的効果）
            int handIndex = currentHand.FindIndex(card => card != null && card.cardId == playedCard.cardId);
            if (handIndex >= 0 && handIndex < cardButtons.Length)
            {
                StartCoroutine(PlayCardAnimation(handIndex));
            }
            
            selectedCardIndex = -1;
            UpdateHandStatusText("手札: カード使用済み");
            UpdateInstructionText("ターンを終了してください");
        }
        
        /// <summary>
        /// 手札状態変更時の処理
        /// </summary>
        void OnHandStateChanged(HandState newState)
        {
            Debug.Log($"HandUI: Hand state changed to {newState}");
            
            switch (newState)
            {
                case HandState.Empty:
                    UpdateHandStatusText("手札: 空");
                    ClearHandDisplay();
                    UpdateInstructionText("新しいターンを開始してください");
                    break;
                    
                case HandState.Generated:
                    UpdateHandStatusText("手札: 準備完了");
                    UpdateInstructionText("カードをクリックして攻撃を選択してください");
                    break;
                    
                case HandState.CardUsed:
                    UpdateHandStatusText("手札: カード使用済み");
                    UpdateInstructionText("ターンを終了してください");
                    break;
                    
                case HandState.TurnEnded:
                    UpdateHandStatusText("手札: ターン終了");
                    UpdateInstructionText("次のターンを待機中...");
                    break;
            }
        }
        
        /// <summary>
        /// 手札クリア時の処理
        /// </summary>
        void OnHandCleared()
        {
            Debug.Log("HandUI: Hand cleared");
            
            ClearHandDisplay();
            selectedCardIndex = -1;
            UpdateHandStatusText("手札: クリア済み");
            UpdateInstructionText("");
        }
        
        /// <summary>
        /// 行動回数変更時の処理
        /// </summary>
        void OnActionsChanged(int remaining, int max)
        {
            Debug.Log($"HandUI: Actions changed - {remaining}/{max}");
            
            UpdateActionsRemainingText(remaining, max);
            
            // 行動回数に応じたUI色変更
            if (actionsRemainingText != null)
            {
                if (remaining > 0)
                {
                    actionsRemainingText.color = Color.cyan;
                }
                else
                {
                    actionsRemainingText.color = Color.red;
                }
            }
        }
        
        /// <summary>
        /// 行動回数0時の処理
        /// </summary>
        void OnActionsExhausted()
        {
            Debug.Log("HandUI: Actions exhausted");
            
            UpdateInstructionText("行動回数がなくなりました。自動でターンが終了します...");
            
            // 手札を非アクティブに（使用不可状態に）
            SetHandInteractable(false);
        }
        
        /// <summary>
        /// 自動ターン終了時の処理
        /// </summary>
        void OnAutoTurnEnd()
        {
            Debug.Log("HandUI: Auto turn end");
            
            UpdateInstructionText("自動ターン終了 - 敵ターンに移行中...");
            selectedCardIndex = -1;
            UpdateHandDisplay();
        }
        
        // === UI更新メソッド ===
        
        /// <summary>
        /// 手札表示を更新
        /// </summary>
        void UpdateHandDisplay()
        {
            // 全カードを一旦非表示
            for (int i = 0; i < cardButtons.Length; i++)
            {
                cardButtons[i].gameObject.SetActive(false);
            }
            
            // 現在の手札を表示
            for (int i = 0; i < currentHand.Count && i < cardButtons.Length; i++)
            {
                var card = currentHand[i];
                if (card == null) continue;
                
                // カードボタンを表示
                cardButtons[i].gameObject.SetActive(true);
                
                // カード色を属性に応じて設定
                Color cardColor = GetAttributeColor(card.weaponData.attackAttribute);
                cardImages[i].color = cardColor;
                
                // カードテキストを設定
                string cardText = $"{card.displayName}\n\n" + // 修正: DisplayName -> displayName
                                $"威力: {card.weaponData.basePower}\n" + // 修正: WeaponData -> weaponData
                                $"射程: {GetRangeDisplayName(card.weaponData.attackRange)}\n" +
                                $"CT: {card.weaponData.cooldownTurns}";
                
                if (!string.IsNullOrEmpty(card.weaponData.specialEffect))
                {
                    cardText += $"\n{card.weaponData.specialEffect}";
                }
                
                cardTexts[i].text = cardText;
                
                // 選択状態のハイライト
                Outline outline = cardButtons[i].GetComponent<Outline>();
                if (outline != null)
                {
                    if (i == selectedCardIndex)
                    {
                        outline.effectColor = Color.yellow;
                        outline.effectDistance = new Vector2(4, 4);
                    }
                    else
                    {
                        outline.effectColor = Color.white;
                        outline.effectDistance = new Vector2(2, 2);
                    }
                }
            }
        }
        
        /// <summary>
        /// 手札をクリア
        /// </summary>
        void ClearHandDisplay()
        {
            currentHand.Clear();
            selectedCardIndex = -1;
            
            for (int i = 0; i < cardButtons.Length; i++)
            {
                cardButtons[i].gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 手札状態テキストを更新
        /// </summary>
        void UpdateHandStatusText(string status)
        {
            if (handStatusText != null)
            {
                handStatusText.text = status;
            }
        }
        
        /// <summary>
        /// 操作説明テキストを更新
        /// </summary>
        void UpdateInstructionText(string instruction)
        {
            if (instructionText != null)
            {
                instructionText.text = instruction;
            }
        }
        
        /// <summary>
        /// 行動回数表示を更新
        /// </summary>
        void UpdateActionsRemainingText(int remaining, int max)
        {
            if (actionsRemainingText != null)
            {
                actionsRemainingText.text = $"行動回数: {remaining}/{max}";
            }
        }
        
        /// <summary>
        /// 手札のインタラクティブ状態を設定
        /// </summary>
        void SetHandInteractable(bool interactable)
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] != null)
                {
                    cardButtons[i].interactable = interactable;
                    
                    // 非アクティブ時は半透明に
                    if (cardImages[i] != null)
                    {
                        Color color = cardImages[i].color;
                        color.a = interactable ? 1f : 0.5f;
                        cardImages[i].color = color;
                    }
                }
            }
        }
        
        // === ユーティリティメソッド ===
        
        /// <summary>
        /// 属性に応じた色を取得
        /// </summary>
        Color GetAttributeColor(AttackAttribute attribute)
        {
            int index = (int)attribute;
            if (index >= 0 && index < attributeColors.Length)
            {
                return attributeColors[index];
            }
            return attributeColors[attributeColors.Length - 1]; // None
        }
        
        /// <summary>
        /// 攻撃範囲の表示名を取得
        /// </summary>
        string GetRangeDisplayName(AttackRange range)
        {
            switch (range)
            {
                case AttackRange.SingleFront: return "前列単体";
                case AttackRange.SingleTarget: return "任意単体";
                case AttackRange.Row1: return "前列全体";
                case AttackRange.Row2: return "後列全体";
                case AttackRange.Column: return "縦列全体";
                case AttackRange.All: return "全体";
                case AttackRange.Self: return "自分";
                default: return "不明";
            }
        }
        
        // === UI イベントハンドラー ===
        
        /// <summary>
        /// カードクリック時の処理（予告ダメージシステム対応）
        /// </summary>
        void OnCardClicked(int cardIndex)
        {
            Debug.Log($"=== HandUI: Card {cardIndex + 1} clicked (Start Debug) ===");
            Debug.Log($"Current selectedCardIndex: {selectedCardIndex}");
            Debug.Log($"battleManager null check: {battleManager == null}");
            Debug.Log($"handSystem null check: {handSystem == null}");
            
            if (battleManager == null || handSystem == null)
            {
                Debug.LogError("HandUI: BattleManager or HandSystem is null!");
                return;
            }
            
            // 詳細状態ログ
            Debug.Log($"BattleManager CurrentState: {battleManager.CurrentState}");
            Debug.Log($"HandSystem CurrentHandState: {handSystem.CurrentHandState}");
            Debug.Log($"HandSystem RemainingActions: {handSystem.RemainingActions}");
            Debug.Log($"HandSystem CanTakeAction: {handSystem.CanTakeAction}");
            Debug.Log($"currentHand.Count: {currentHand.Count}");
            
            // ターン状態確認（Victory状態でもテスト用に許可）
            if (battleManager.CurrentState != GameState.PlayerTurn && battleManager.CurrentState != GameState.Victory)
            {
                Debug.LogWarning($"HandUI: Cannot use card during {battleManager.CurrentState}");
                return;
            }
            Debug.Log($"✅ Game state check passed: {battleManager.CurrentState} (PlayerTurn or Victory allowed)");
            
            // 手札状態確認
            if (handSystem.CurrentHandState != HandState.Generated)
            {
                Debug.LogWarning($"HandUI: Cannot use card when hand state is {handSystem.CurrentHandState}");
                return;
            }
            
            // カードインデックス確認
            if (cardIndex < 0 || cardIndex >= currentHand.Count)
            {
                Debug.LogError($"HandUI: Invalid card index {cardIndex}, currentHand.Count: {currentHand.Count}");
                return;
            }
            
            // カード選択状態更新
            Debug.Log($"Comparing cardIndex {cardIndex} with selectedCardIndex {selectedCardIndex}");
            if (selectedCardIndex == cardIndex)
            {
                Debug.Log($"=== 2回目クリック検出: Card {cardIndex + 1} → 攻撃実行 ===");
                // 同じカードをクリック（2回目） → カード実行
                UseSelectedCard(cardIndex);
            }
            else
            {
                Debug.Log($"=== 1回目クリック検出: Card {cardIndex + 1} → 選択 & プレビュー ===");
                // 別のカードをクリック（1回目） → 選択変更＆予告ダメージ計算・表示
                selectedCardIndex = cardIndex;
                Debug.Log($"selectedCardIndex updated to: {selectedCardIndex}");
                UpdateHandDisplay();
                
                CardData selectedCard = currentHand[cardIndex];
                Debug.Log($"Selected card: {selectedCard?.displayName ?? "NULL"}");
                
                // 【新機能】1回目クリックで予告ダメージ計算・表示
                CalculateAndDisplayPreviewDamage(selectedCard);
                
                UpdateInstructionText($"選択中: {selectedCard.displayName} (再度クリックで実行)");
                Debug.Log($"HandUI: Card selected with preview damage - {selectedCard.displayName}");
            }
            Debug.Log($"=== HandUI: Card {cardIndex + 1} clicked (End Debug) ===");
        }
        
        /// <summary>
        /// 選択されたカードを使用
        /// </summary>
        void UseSelectedCard(int cardIndex)
        {
            Debug.Log($"=== UseSelectedCard START: Card {cardIndex + 1} ===");
            Debug.Log($"HandSystem check: {handSystem != null}");
            Debug.Log($"HandSystem RemainingActions before PlayCard: {handSystem?.RemainingActions}");
            Debug.Log($"HandSystem CurrentHandState before PlayCard: {handSystem?.CurrentHandState}");
            
            // HandSystemに対してカードの使用を要求
            var result = handSystem.PlayCard(cardIndex);
            
            Debug.Log($"PlayCard result - isSuccess: {result.isSuccess}, message: {result.message}");
            Debug.Log($"PlayCard result - damageDealt: {result.damageDealt}, turnEnded: {result.turnEnded}");
            Debug.Log($"HandSystem RemainingActions after PlayCard: {handSystem?.RemainingActions}");
            Debug.Log($"HandSystem CurrentHandState after PlayCard: {handSystem?.CurrentHandState}");
            
            if (result.isSuccess)
            {
                Debug.Log($"✅ HandUI: Card used successfully! Damage: {result.damageDealt}");
                if (result.turnEnded)
                {
                    Debug.Log($"✅ Turn ended automatically due to actions exhausted");
                }
            }
            else
            {
                Debug.LogWarning($"❌ HandUI: Failed to use card - {result.message}");
                UpdateInstructionText($"エラー: {result.message}");
            }
            Debug.Log($"=== UseSelectedCard END: Card {cardIndex + 1} ===");
        }
        
        /// <summary>
        /// カード使用アニメーション
        /// </summary>
        System.Collections.IEnumerator PlayCardAnimation(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= cardButtons.Length) yield break;
            
            Button cardButton = cardButtons[cardIndex];
            if (cardButton == null) yield break;
            
            // フェードアウトアニメーション
            Image cardImage = cardButton.GetComponent<Image>();
            Color originalColor = cardImage.color;
            
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0.3f, elapsed / duration);
                cardImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
            
            // 最終的に半透明にして使用済みを表現
            cardImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
        }
        
        /// <summary>
        /// 予告ダメージの計算と表示（1回目クリック時）
        /// </summary>
        void CalculateAndDisplayPreviewDamage(CardData card)
        {
            Debug.Log($"=== CalculateAndDisplayPreviewDamage START: {card?.displayName ?? "NULL"} ===");
            Debug.Log($"handSystem null check: {handSystem == null}");
            Debug.Log($"card null check: {card == null}");
            
            if (handSystem == null || card == null)
            {
                Debug.LogWarning("⚠️ handSystem or card is null, cannot calculate preview damage");
                return;
            }
            
            try
            {
                Debug.Log($"Calling handSystem.CalculatePreviewDamage for {card.displayName}");
                // HandSystemの予告ダメージ計算メソッドを呼び出し
                var previewDamage = handSystem.CalculatePreviewDamage(card);
                
                Debug.Log($"CalculatePreviewDamage result: {previewDamage != null}");
                if (previewDamage != null)
                {
                    Debug.Log($"✅ Preview damage calculated successfully:");
                    Debug.Log($"  - Description: {previewDamage.description}");
                    Debug.Log($"  - Damage: {previewDamage.calculatedDamage}");
                    Debug.Log($"  - Target enemies: {previewDamage.targetEnemies?.Count ?? 0}");
                    Debug.Log($"  - Target gates: {previewDamage.targetGates?.Count ?? 0}");
                    
                    // 予告ダメージ表示をSimpleBattleUIに通知
                    // （OnPendingDamageCalculatedイベントは実際の実行時のみ発火させる）
                    Debug.Log($"HandUI: Preview damage calculated - {previewDamage.description}, Damage: {previewDamage.calculatedDamage}");
                    
                    // 【修正】既存のイベントシステムを活用
                    // HandSystemに予告ダメージの表示を依頼（イベント経由でSimpleBattleUIが受け取る）
                    // 注意: HandSystemのCalculatePreviewDamageメソッドが内部でイベントを発火することを期待
                    // もしイベントが発火されない場合は、HandSystem側の実装を確認する必要がある
                    
                    // UI側での簡易表示（フォールバック）
                    string displayText = $"選択中: {card.displayName} - 予告ダメージ: {previewDamage.calculatedDamage} (再度クリックで実行)";
                    Debug.Log($"Updating instruction text: {displayText}");
                    UpdateInstructionText(displayText);
                }
                else
                {
                    Debug.LogWarning($"❌ HandUI: Failed to calculate preview damage for {card.displayName}");
                    UpdateInstructionText($"エラー: {card.displayName}の予告ダメージ計算に失敗");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"HandUI: Error calculating preview damage - {ex.Message}");
                UpdateInstructionText($"エラー: 予告ダメージ計算中にエラーが発生");
            }
        }
        
        /// <summary>
        /// 手札UI全体の表示/非表示を制御
        /// </summary>
        public void SetHandUIVisible(bool visible)
        {
            Debug.Log($"HandUI: Setting visibility to {visible}");
            
            if (handPanel != null)
            {
                handPanel.SetActive(visible);
                // 🔧 重要: RaycastTargetも制御してクリック遮断を防止
                var panelImage = handPanel.GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.raycastTarget = visible;
                }
            }
            
            if (handStatusText != null)
            {
                handStatusText.gameObject.SetActive(visible);
                handStatusText.raycastTarget = visible;
            }
            
            if (instructionText != null)
            {
                instructionText.gameObject.SetActive(visible);
                instructionText.raycastTarget = visible;
            }
            
            if (actionsRemainingText != null)
            {
                actionsRemainingText.gameObject.SetActive(visible);
                actionsRemainingText.raycastTarget = visible;
            }
            
            // カードボタンも制御（RaycastTarget含む）
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] != null)
                {
                    cardButtons[i].gameObject.SetActive(visible);
                    
                    // ボタンのRaycastTargetを制御
                    var buttonImage = cardButtons[i].GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.raycastTarget = visible;
                    }
                    
                    // カードテキストのRaycastTargetも制御
                    if (cardTexts[i] != null)
                    {
                        cardTexts[i].raycastTarget = visible;
                    }
                }
            }
            
            Debug.Log($"HandUI: Visibility and RaycastTargets set to {visible}");
        }
        
        // === デバッグ用メソッド ===
        
        [ContextMenu("Force Update Hand Display")]
        public void ForceUpdateHandDisplay()
        {
            Debug.Log("HandUI: Force updating hand display...");
            
            if (handSystem != null && handSystem.CurrentHand != null)
            {
                currentHand.Clear();
                foreach(var card in handSystem.CurrentHand)
                {
                    if (card != null)
                        currentHand.Add(card);
                }
                
                selectedCardIndex = -1;
                UpdateHandDisplay();
                SetHandInteractable(true);
                
                Debug.Log($"HandUI: Force update completed - {currentHand.Count} cards displayed");
            }
            else
            {
                Debug.LogWarning("HandUI: Cannot force update - HandSystem or hand is null");
            }
        }
        
        [ContextMenu("Debug Hand Status")]
        public void DebugHandStatus()
        {
            Debug.Log($"HandUI Debug Status:");
            Debug.Log($"- Current Hand Size: {currentHand.Count}");
            Debug.Log($"- Selected Card Index: {selectedCardIndex}");
            Debug.Log($"- HandSystem State: {handSystem?.CurrentHandState}"); // 修正: CurrentState -> CurrentHandState
            Debug.Log($"- BattleManager State: {battleManager?.CurrentState}");
            Debug.Log($"- Remaining Actions: {handSystem?.RemainingActions}");
            Debug.Log($"- Can Take Action: {handSystem?.CanTakeAction}");
            
            // カードボタンの状態チェック
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] != null)
                {
                    Debug.Log($"- Card {i + 1}: Active={cardButtons[i].gameObject.activeSelf}, Interactable={cardButtons[i].interactable}");
                }
            }
        }
        
        [ContextMenu("Force Refresh All")]
        public void ForceRefreshAll()
        {
            Debug.Log("HandUI: Force refreshing all UI elements...");
            
            // 強制リフレッシュ
            ForceUpdateHandDisplay();
            
            // 状態テキストをリセット
            if (handSystem != null)
            {
                switch (handSystem.CurrentHandState)
                {
                    case HandState.Generated:
                        UpdateHandStatusText("手札: 準備完了");
                        UpdateInstructionText("カードをクリックして攻撃を選択してください");
                        break;
                    default:
                        UpdateHandStatusText($"手札: {handSystem.CurrentHandState}");
                        break;
                }
                
                UpdateActionsRemainingText(handSystem.RemainingActions, handSystem.MaxActionsPerTurn);
            }
            
            Debug.Log("HandUI: Force refresh completed");
        }
    }
}
