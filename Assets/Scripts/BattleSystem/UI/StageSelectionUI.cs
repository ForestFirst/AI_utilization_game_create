using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem.UI
{
    /// <summary>
    /// サイバーパンク風ステージ選択UI
    /// StageManagerとの連携によるステージ選択機能
    /// </summary>
    public class StageSelectionUI : MonoBehaviour
    {
        [Header("UI設定")]
        [SerializeField] private bool autoCreateUI = true;
        [SerializeField] private int maxStagesPerRow = 4;
        [SerializeField] private float stageButtonSize = 120f;
        [SerializeField] private float stageButtonSpacing = 20f;
        
        [Header("サイバーパンク色設定")]
        [SerializeField] private Color primaryGlowColor = new Color(0f, 1f, 1f, 1f); // シアン
        [SerializeField] private Color secondaryGlowColor = new Color(1f, 0f, 1f, 1f); // マゼンタ
        [SerializeField] private Color lockedStageColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // グレー
        [SerializeField] private Color unlockedStageColor = new Color(0f, 1f, 1f, 0.8f); // シアン
        [SerializeField] private Color clearedStageColor = new Color(0f, 1f, 0f, 0.8f); // 緑
        
        // UI要素
        private Canvas mainCanvas;
        private GameObject stageContainer;
        private GameObject detailPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI stageInfoText;
        private Button backButton;
        private Button startBattleButton;
        private ScrollRect stageScrollRect;
        
        // ステージボタン管理
        private List<StageButtonData> stageButtons = new List<StageButtonData>();
        private StageData selectedStage;
        
        // システム参照
        private StageManager stageManager;
        private PlayerDataManager playerDataManager;
        private SceneTransitionManager sceneTransition;
        private GameEventManager eventManager;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeReferences();
            
            if (autoCreateUI)
            {
                CreateStageSelectionUI();
            }
            
            SetupEventSubscriptions();
            LoadStageData();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// システム参照の初期化
        /// </summary>
        private void InitializeReferences()
        {
            stageManager = StageManager.Instance;
            playerDataManager = PlayerDataManager.Instance;
            sceneTransition = SceneTransitionManager.Instance;
            eventManager = GameEventManager.Instance;
            
            if (stageManager == null)
            {
                Debug.LogError("[StageSelectionUI] StageManager not found!");
            }
            
            if (playerDataManager == null)
            {
                Debug.LogError("[StageSelectionUI] PlayerDataManager not found!");
            }
        }

        /// <summary>
        /// イベント購読設定
        /// </summary>
        private void SetupEventSubscriptions()
        {
            if (stageManager != null)
            {
                stageManager.OnStageUnlocked += OnStageUnlocked;
                stageManager.OnStageProgressChanged += OnStageProgressChanged;
            }
            
            if (playerDataManager != null)
            {
                PlayerDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
            }
            
            // GameEventManagerのイベント購読
            GameEventManager.OnStageSelected += OnStageSelectedEvent;
        }

        /// <summary>
        /// イベント購読解除
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (stageManager != null)
            {
                stageManager.OnStageUnlocked -= OnStageUnlocked;
                stageManager.OnStageProgressChanged -= OnStageProgressChanged;
            }
            
            if (playerDataManager != null)
            {
                PlayerDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;
            }
            
            GameEventManager.OnStageSelected -= OnStageSelectedEvent;
        }

        #endregion

        #region UI Creation

        /// <summary>
        /// ステージ選択UI作成
        /// </summary>
        private void CreateStageSelectionUI()
        {
            CreateMainCanvas();
            CreateBackground();
            CreateTitleSection();
            CreateStageGrid();
            CreateDetailPanel();
            CreateNavigationButtons();
            
            Debug.Log("[StageSelectionUI] UI created successfully");
        }

        /// <summary>
        /// メインCanvas作成
        /// </summary>
        private void CreateMainCanvas()
        {
            var canvasObj = new GameObject("StageSelectionCanvas");
            canvasObj.transform.SetParent(transform, false);
            
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 50;
            
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// 背景作成
        /// </summary>
        private void CreateBackground()
        {
            var backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(mainCanvas.transform, false);
            
            var backgroundRect = backgroundObj.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;
            backgroundRect.anchoredPosition = Vector2.zero;
            
            var backgroundImg = backgroundObj.AddComponent<Image>();
            backgroundImg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
        }

        /// <summary>
        /// タイトルセクション作成
        /// </summary>
        private void CreateTitleSection()
        {
            var titleObj = new GameObject("TitleSection");
            titleObj.transform.SetParent(mainCanvas.transform, false);
            
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;
            
            titleText = CreateCyberpunkText(titleObj.transform, "ステージ選択", 48);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = primaryGlowColor;
        }

        /// <summary>
        /// ステージグリッド作成
        /// </summary>
        private void CreateStageGrid()
        {
            // スクロールビュー作成
            var scrollViewObj = new GameObject("StageScrollView");
            scrollViewObj.transform.SetParent(mainCanvas.transform, false);
            
            var scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.1f, 0.25f);
            scrollRect.anchorMax = new Vector2(0.6f, 0.8f);
            scrollRect.sizeDelta = Vector2.zero;
            scrollRect.anchoredPosition = Vector2.zero;
            
            stageScrollRect = scrollViewObj.AddComponent<ScrollRect>();
            stageScrollRect.horizontal = false;
            stageScrollRect.vertical = true;
            stageScrollRect.scrollSensitivity = 20f;
            
            // スクロール背景
            var scrollBackground = scrollViewObj.AddComponent<Image>();
            scrollBackground.color = new Color(0f, 0f, 0f, 0.3f);
            
            // ビューポート作成
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;
            
            var mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            viewportObj.AddComponent<Image>(); // Mask requires Image component
            
            stageScrollRect.viewport = viewportRect;
            
            // コンテンツ作成
            stageContainer = new GameObject("StageContainer");
            stageContainer.transform.SetParent(viewportObj.transform, false);
            
            var contentRect = stageContainer.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            
            stageScrollRect.content = contentRect;
            
            // グリッドレイアウト設定
            var gridLayout = stageContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(stageButtonSize, stageButtonSize + 40);
            gridLayout.spacing = new Vector2(stageButtonSpacing, stageButtonSpacing);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = maxStagesPerRow;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            
            var contentSizeFitter = stageContainer.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// 詳細パネル作成
        /// </summary>
        private void CreateDetailPanel()
        {
            detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(mainCanvas.transform, false);
            
            var detailRect = detailPanel.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.65f, 0.25f);
            detailRect.anchorMax = new Vector2(0.9f, 0.8f);
            detailRect.sizeDelta = Vector2.zero;
            detailRect.anchoredPosition = Vector2.zero;
            
            // 詳細パネル背景
            var panelBg = detailPanel.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.15f, 0.3f, 0.9f);
            
            // グロー効果
            var outline = detailPanel.AddComponent<Outline>();
            outline.effectColor = primaryGlowColor;
            outline.effectDistance = new Vector2(2, 2);
            
            // ステージ情報テキスト
            var infoTextObj = new GameObject("StageInfoText");
            infoTextObj.transform.SetParent(detailPanel.transform, false);
            
            var infoRect = infoTextObj.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.1f, 0.3f);
            infoRect.anchorMax = new Vector2(0.9f, 0.9f);
            infoRect.sizeDelta = Vector2.zero;
            infoRect.anchoredPosition = Vector2.zero;
            
            stageInfoText = infoTextObj.AddComponent<TextMeshProUGUI>();
            stageInfoText.text = "ステージを選択してください";
            stageInfoText.fontSize = 16;
            stageInfoText.color = Color.white;
            stageInfoText.alignment = TextAlignmentOptions.TopLeft;
            
            // 戦闘開始ボタン
            startBattleButton = CreateCyberpunkButton(detailPanel.transform, 
                new Vector2(0, -0.35f), new Vector2(200, 60), "戦闘開始", OnStartBattleClicked);
            startBattleButton.interactable = false;
        }

        /// <summary>
        /// ナビゲーションボタン作成
        /// </summary>
        private void CreateNavigationButtons()
        {
            // 戻るボタン
            backButton = CreateCyberpunkButton(mainCanvas.transform,
                new Vector2(-0.4f, -0.4f), new Vector2(150, 50), "戻る", OnBackClicked);
        }

        #endregion

        #region Stage Data Management

        /// <summary>
        /// ステージデータ読み込み
        /// </summary>
        private void LoadStageData()
        {
            if (stageManager == null) return;
            
            var allStages = stageManager.AllStages;
            CreateStageButtons(allStages);
            
            Debug.Log($"[StageSelectionUI] Loaded {allStages.Count} stages");
        }

        /// <summary>
        /// ステージボタン作成
        /// </summary>
        private void CreateStageButtons(List<StageData> stages)
        {
            // 既存のボタンをクリア
            ClearStageButtons();
            
            foreach (var stage in stages)
            {
                CreateStageButton(stage);
            }
        }

        /// <summary>
        /// 個別ステージボタン作成
        /// </summary>
        private void CreateStageButton(StageData stageData)
        {
            var buttonObj = new GameObject($"StageButton_{stageData.stageId}");
            buttonObj.transform.SetParent(stageContainer.transform, false);
            
            // ボタンコンポーネント
            var button = buttonObj.AddComponent<Button>();
            var buttonRect = buttonObj.AddComponent<RectTransform>();
            var buttonImg = buttonObj.AddComponent<Image>();
            
            // ボタン外観設定
            bool isUnlocked = stageData.isUnlocked;
            bool isCleared = IsStageCleared(stageData.stageId);
            
            Color buttonColor = lockedStageColor;
            if (isCleared)
                buttonColor = clearedStageColor;
            else if (isUnlocked)
                buttonColor = unlockedStageColor;
            
            buttonImg.color = buttonColor;
            
            // ボタンクリック処理
            button.onClick.AddListener(() => OnStageButtonClicked(stageData));
            button.interactable = isUnlocked;
            
            // ステージ名テキスト
            var textObj = new GameObject("StageText");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var stageText = textObj.AddComponent<TextMeshProUGUI>();
            stageText.text = stageData.stageName;
            stageText.fontSize = 14;
            stageText.color = isUnlocked ? Color.white : Color.gray;
            stageText.alignment = TextAlignmentOptions.Center;
            stageText.raycastTarget = false;
            
            // ロック状態表示
            if (!isUnlocked)
            {
                var lockIcon = new GameObject("LockIcon");
                lockIcon.transform.SetParent(buttonObj.transform, false);
                
                var lockRect = lockIcon.AddComponent<RectTransform>();
                lockRect.anchorMin = new Vector2(0.7f, 0.7f);
                lockRect.anchorMax = new Vector2(1f, 1f);
                lockRect.sizeDelta = Vector2.zero;
                lockRect.anchoredPosition = Vector2.zero;
                
                var lockImg = lockIcon.AddComponent<Image>();
                lockImg.color = Color.red;
                // TODO: 実際のロックアイコンスプライトがあれば設定
            }
            
            // データ保存
            var buttonData = new StageButtonData
            {
                stageData = stageData,
                button = button,
                buttonImage = buttonImg,
                stageText = stageText
            };
            
            stageButtons.Add(buttonData);
        }

        /// <summary>
        /// ステージボタンをクリア
        /// </summary>
        private void ClearStageButtons()
        {
            foreach (var buttonData in stageButtons)
            {
                if (buttonData.button != null)
                {
                    DestroyImmediate(buttonData.button.gameObject);
                }
            }
            stageButtons.Clear();
        }

        /// <summary>
        /// ステージクリア状況をチェック
        /// </summary>
        private bool IsStageCleared(string stageId)
        {
            if (stageManager == null) return false;
            
            var progress = stageManager.GetStageProgress(stageId);
            return progress != null && progress.isCleared;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ステージボタンクリック処理
        /// </summary>
        private void OnStageButtonClicked(StageData stageData)
        {
            selectedStage = stageData;
            UpdateStageDetail(stageData);
            
            // ステージ選択イベント発火
            GameEventManager.TriggerStageSelection(stageData);
            
            Debug.Log($"[StageSelectionUI] Stage selected: {stageData.stageName}");
        }

        /// <summary>
        /// 戦闘開始ボタンクリック処理
        /// </summary>
        private void OnStartBattleClicked()
        {
            if (selectedStage == null) return;
            
            // ステージマネージャーでステージを選択
            if (stageManager != null && stageManager.SelectStage(selectedStage.stageId))
            {
                // 戦闘シーンに遷移
                var battleData = new BattleStartData
                {
                    stageId = selectedStage.stageId,
                    stageName = selectedStage.stageName,
                    stageData = selectedStage,
                    playerData = playerDataManager?.PlayerData,
                    startTime = DateTime.Now
                };
                
                GameEventManager.TriggerBattleStart(battleData);
                
                if (sceneTransition != null)
                {
                    sceneTransition.TransitionToScene("GameScene", battleData);
                }
                
                Debug.Log($"[StageSelectionUI] Starting battle: {selectedStage.stageName}");
            }
        }

        /// <summary>
        /// 戻るボタンクリック処理
        /// </summary>
        private void OnBackClicked()
        {
            if (sceneTransition != null)
            {
                sceneTransition.TransitionToScene("TitleScene");
            }
        }

        /// <summary>
        /// ステージ解放イベント処理
        /// </summary>
        private void OnStageUnlocked(StageData stageData)
        {
            RefreshStageButtons();
            Debug.Log($"[StageSelectionUI] Stage unlocked, refreshing UI: {stageData.stageName}");
        }

        /// <summary>
        /// ステージ進行状況変更イベント処理
        /// </summary>
        private void OnStageProgressChanged()
        {
            RefreshStageButtons();
        }

        /// <summary>
        /// プレイヤーデータ変更イベント処理
        /// </summary>
        private void OnPlayerDataChanged(PlayerData playerData)
        {
            RefreshStageButtons();
        }

        /// <summary>
        /// ステージ選択イベント処理
        /// </summary>
        private void OnStageSelectedEvent(StageData stageData)
        {
            // 他のシステムからのステージ選択に対応
            if (selectedStage != stageData)
            {
                selectedStage = stageData;
                UpdateStageDetail(stageData);
            }
        }

        #endregion

        #region UI Update

        /// <summary>
        /// ステージボタンを更新
        /// </summary>
        private void RefreshStageButtons()
        {
            foreach (var buttonData in stageButtons)
            {
                var stageData = buttonData.stageData;
                bool isUnlocked = stageData.isUnlocked;
                bool isCleared = IsStageCleared(stageData.stageId);
                
                // ボタン色更新
                Color buttonColor = lockedStageColor;
                if (isCleared)
                    buttonColor = clearedStageColor;
                else if (isUnlocked)
                    buttonColor = unlockedStageColor;
                
                buttonData.buttonImage.color = buttonColor;
                buttonData.button.interactable = isUnlocked;
                buttonData.stageText.color = isUnlocked ? Color.white : Color.gray;
            }
        }

        /// <summary>
        /// ステージ詳細表示を更新
        /// </summary>
        private void UpdateStageDetail(StageData stageData)
        {
            if (stageInfoText == null) return;
            
            var progress = stageManager?.GetStageProgress(stageData.stageId);
            string clearStatus = progress?.isCleared == true ? "クリア済み" : "未クリア";
            
            string detailText = $"<color=#00FFFF><size=20>{stageData.stageName}</size></color>\n\n";
            detailText += $"<color=#FFFF00>難易度:</color> {stageData.difficulty}\n";
            detailText += $"<color=#FFFF00>タイプ:</color> {stageData.stageType}\n";
            detailText += $"<color=#FFFF00>ステータス:</color> {clearStatus}\n";
            detailText += $"<color=#FFFF00>スタミナ:</color> {stageData.staminaCost}\n\n";
            
            if (!string.IsNullOrEmpty(stageData.description))
            {
                detailText += $"<color=#FFFFFF>{stageData.description}</color>\n\n";
            }
            
            // 報酬情報
            if (stageData.clearReward != null)
            {
                detailText += "<color=#00FF00>クリア報酬:</color>\n";
                if (stageData.clearReward.goldReward > 0)
                    detailText += $"• ゴールド: {stageData.clearReward.goldReward}\n";
                if (stageData.clearReward.experienceReward > 0)
                    detailText += $"• 経験値: {stageData.clearReward.experienceReward}\n";
            }
            
            stageInfoText.text = detailText;
            startBattleButton.interactable = stageData.isUnlocked;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// サイバーパンク風テキスト作成
        /// </summary>
        private TextMeshProUGUI CreateCyberpunkText(Transform parent, string text, int fontSize)
        {
            var textObj = new GameObject("CyberpunkText");
            textObj.transform.SetParent(parent, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            // グロー効果
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = primaryGlowColor;
            outline.effectDistance = new Vector2(1, 1);
            
            return textComponent;
        }

        /// <summary>
        /// サイバーパンク風ボタン作成
        /// </summary>
        private Button CreateCyberpunkButton(Transform parent, Vector2 anchorPosition, Vector2 size, string text, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObj = new GameObject($"CyberpunkButton_{text}");
            buttonObj.transform.SetParent(parent, false);
            
            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = size;
            buttonRect.anchoredPosition = anchorPosition * 540f; // 相対位置をピクセルに変換
            
            var button = buttonObj.AddComponent<Button>();
            var buttonImg = buttonObj.AddComponent<Image>();
            
            // サイバーパンクスタイル
            buttonImg.color = new Color(0.1f, 0.3f, 0.5f, 0.8f);
            
            // ボタンテキスト
            var textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            var buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 18;
            buttonText.color = primaryGlowColor;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.raycastTarget = false;
            
            // ボタン効果
            var outline = buttonObj.AddComponent<Outline>();
            outline.effectColor = secondaryGlowColor;
            outline.effectDistance = new Vector2(2, 2);
            
            // ホバー効果
            var colors = button.colors;
            colors.normalColor = new Color(0.1f, 0.3f, 0.5f, 0.8f);
            colors.highlightedColor = new Color(0.2f, 0.5f, 0.8f, 1f);
            colors.pressedColor = new Color(0.05f, 0.2f, 0.4f, 0.9f);
            button.colors = colors;
            
            button.onClick.AddListener(onClick);
            
            return button;
        }

        #endregion

        #region Debug

        /// <summary>
        /// デバッグ情報表示
        /// </summary>
        [ContextMenu("Debug Stage Selection")]
        public void DebugStageSelection()
        {
            Debug.Log($"=== Stage Selection Debug ===");
            Debug.Log($"Total Stages: {stageButtons.Count}");
            Debug.Log($"Selected Stage: {selectedStage?.stageName ?? "None"}");
            Debug.Log($"Stage Manager: {(stageManager != null ? "Found" : "Missing")}");
            Debug.Log($"Player Data Manager: {(playerDataManager != null ? "Found" : "Missing")}");
        }

        #endregion
    }

    /// <summary>
    /// ステージボタンデータクラス
    /// </summary>
    [Serializable]
    public class StageButtonData
    {
        public StageData stageData;
        public Button button;
        public Image buttonImage;
        public TextMeshProUGUI stageText;
    }
}