using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BattleSystem.UI
{
    /// <summary>
    /// 4x4グリッド型ステージ選択UI（UIモックアップ準拠）
    /// サイバーパンク風デザイン、斜めUI要素
    /// </summary>
    public class StageSelectionUI : MonoBehaviour
    {
        [Header("ゲーム設定")]
        [SerializeField] private int skillPoints = 12;
        [SerializeField] private int gold = 850;
        [SerializeField] private Vector2Int currentPosition = new Vector2Int(1, 2); // スタート位置

        [Header("UI設定")]
        [SerializeField] private bool autoCreateUI = true;

        // UI要素
        private Canvas mainCanvas;
        private GameObject headerContainer;
        private GameObject stageGridContainer;
        private GameObject detailPanelContainer;
        private GameObject bottomControlsContainer;

        // ステージデータ
        private StageData[,] stages;
        private StageData selectedStage;

        // UI コンポーネント
        private Text goldText;
        private Text skillPointsText;
        private Button[,] stageButtons;

        #region Unity Lifecycle

        private void Start()
        {
            if (autoCreateUI)
            {
                InitializeStageData();
                CreateStageSelectionUI();
                UpdateResourceDisplay();
                UpdateStageAccessibility();
            }
            SetupEventSystem();
        }

        private void Update()
        {
            HandleKeyboardNavigation();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ステージデータ初期化
        /// </summary>
        private void InitializeStageData()
        {
            stages = new StageData[4, 4];
            stageButtons = new Button[4, 4];

            // モックアップに基づくステージ配置
            InitializeStageLayout();
        }

        /// <summary>
        /// ステージレイアウト初期化
        /// </summary>
        private void InitializeStageLayout()
        {
            // Row 0 (上)
            stages[0, 0] = CreateStageData(StageType.Tavern, "居酒屋", "ストーリーイベント\n+2 スキルポイント", "+2 SP", StageDifficulty.Easy);
            stages[0, 1] = CreateStageData(StageType.Battle, "戦闘", "通常の敵との戦闘\nアタッチメント獲得", "アタッチメント", StageDifficulty.Normal);
            stages[0, 2] = CreateStageData(StageType.BattleRare, "戦闘(レア)", "エリート敵との戦闘\nレアアタッチメント確定", "レアアタッチメント", StageDifficulty.Hard);
            stages[0, 3] = CreateStageData(StageType.Battle, "戦闘", "通常の敵との戦闘\nアタッチメント獲得", "アタッチメント", StageDifficulty.Normal);

            // Row 1
            stages[1, 0] = CreateStageData(StageType.MapSwap, "マップ\n入れ替え", "ステージ配置を\nシャッフル", "配置変更", StageDifficulty.Neutral);
            stages[1, 1] = CreateStageData(StageType.Shop, "ショップ", "武器・アイテム購入\n強化サービス", "購入機会", StageDifficulty.Easy);
            stages[1, 2] = CreateStageData(StageType.Casino, "カジノ", "ギャンブル\nハイリスク・ハイリターン", "?????", StageDifficulty.Risky);
            stages[1, 3] = CreateStageData(StageType.Battle, "戦闘", "通常の敵との戦闘\nアタッチメント獲得", "アタッチメント", StageDifficulty.Normal);

            // Row 2 (中央行) - スタート位置
            stages[2, 0] = CreateStageData(StageType.Battle, "戦闘", "通常の敵との戦闘\nアタッチメント獲得", "アタッチメント", StageDifficulty.Normal);
            stages[2, 1] = CreateStageData(StageType.Start, "スタート", "現在地\n次の目的地を選択", "", StageDifficulty.Current);
            stages[2, 2] = CreateStageData(StageType.Inn, "宿屋", "HP完全回復\n状態異常解除", "HP回復", StageDifficulty.Easy);
            stages[2, 3] = CreateStageData(StageType.Battle, "戦闘", "通常の敵との戦闘\nアタッチメント獲得", "アタッチメント", StageDifficulty.Normal);

            // Row 3 (下)
            stages[3, 0] = CreateStageData(StageType.Casino, "カジノ", "ギャンブル\nハイリスク・ハイリターン", "?????", StageDifficulty.Risky);
            stages[3, 1] = CreateStageData(StageType.Inn, "宿屋", "HP完全回復\n状態異常解除", "HP回復", StageDifficulty.Easy);
            stages[3, 2] = CreateStageData(StageType.Shop, "ショップ", "武器・アイテム購入\n強化サービス", "購入機会", StageDifficulty.Easy);
            stages[3, 3] = CreateStageData(StageType.Battle, "戦闘", "通常の敵との戦闘\nアタッチメント獲得", "アタッチメント", StageDifficulty.Normal);
        }

        /// <summary>
        /// ステージデータ作成ヘルパー
        /// </summary>
        private StageData CreateStageData(StageType type, string name, string description, string reward, StageDifficulty difficulty)
        {
            return new StageData
            {
                stageType = type,
                stageName = name,
                description = description,
                reward = reward,
                difficulty = difficulty
            };
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
            CreateHeader();
            CreateStageGrid();
            CreateBottomControls();
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
            mainCanvas.sortingOrder = 100;

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
            // サイバーパンク風ダークグラデーション
            backgroundImg.color = new Color(0.05f, 0.05f, 0.1f, 1f);

            // グリッドオーバーレイ
            CreateGridOverlay(backgroundObj.transform);
        }

        /// <summary>
        /// グリッドオーバーレイ作成
        /// </summary>
        private void CreateGridOverlay(Transform parent)
        {
            var gridObj = new GameObject("GridOverlay");
            gridObj.transform.SetParent(parent, false);

            var gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.sizeDelta = Vector2.zero;
            gridRect.anchoredPosition = Vector2.zero;

            var gridImg = gridObj.AddComponent<Image>();
            gridImg.color = new Color(0.2f, 0.3f, 0.4f, 0.1f);
        }

        /// <summary>
        /// ヘッダー作成
        /// </summary>
        private void CreateHeader()
        {
            headerContainer = new GameObject("Header");
            headerContainer.transform.SetParent(mainCanvas.transform, false);

            var headerRect = headerContainer.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = new Vector2(1, 1f);
            headerRect.sizeDelta = Vector2.zero;
            headerRect.anchoredPosition = Vector2.zero;

            var headerBg = headerContainer.AddComponent<Image>();
            headerBg.color = new Color(0.1f, 0.12f, 0.15f, 0.9f);

            // 戻るボタン
            CreateBackButton(headerContainer.transform);

            // タイトル
            CreateHeaderTitle(headerContainer.transform);

            // リソース表示
            CreateResourceDisplay(headerContainer.transform);

            // 設定ボタン
            CreateSettingsButton(headerContainer.transform);
        }

        /// <summary>
        /// 戻るボタン作成
        /// </summary>
        private void CreateBackButton(Transform parent)
        {
            var backButtonObj = new GameObject("BackButton");
            backButtonObj.transform.SetParent(parent, false);

            var backButtonRect = backButtonObj.AddComponent<RectTransform>();
            backButtonRect.anchorMin = new Vector2(0.02f, 0.1f);
            backButtonRect.anchorMax = new Vector2(0.1f, 0.9f);
            backButtonRect.sizeDelta = Vector2.zero;
            backButtonRect.anchoredPosition = Vector2.zero;

            var backButton = backButtonObj.AddComponent<Button>();
            var backImage = backButtonObj.AddComponent<Image>();
            backImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            // 斜めクリッピング風
            var backText = CreateButtonText(backButtonObj.transform, "←", 24);
            backButton.targetGraphic = backImage;
            backButton.onClick.AddListener(OnBackButton);
        }

        /// <summary>
        /// ヘッダータイトル作成
        /// </summary>
        private void CreateHeaderTitle(Transform parent)
        {
            var titleObj = new GameObject("HeaderTitle");
            titleObj.transform.SetParent(parent, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.15f, 0.2f);
            titleRect.anchorMax = new Vector2(0.5f, 0.8f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;

            var titleBg = titleObj.AddComponent<Image>();
            titleBg.color = new Color(0.1f, 0.12f, 0.15f, 0.9f);

            var titleText = CreateButtonText(titleObj.transform, "ステージ選択", 28);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// リソース表示作成
        /// </summary>
        private void CreateResourceDisplay(Transform parent)
        {
            // ゴールド表示
            var goldContainer = CreateResourceContainer(parent, "Gold", new Vector2(0.6f, 0.1f), new Vector2(0.75f, 0.9f), new Color(0.2f, 0.4f, 0.2f, 0.8f));
            goldText = CreateResourceText(goldContainer, $"{gold}G", new Color(0.4f, 1f, 0.4f));

            // スキルポイント表示
            var spContainer = CreateResourceContainer(parent, "SP", new Vector2(0.76f, 0.1f), new Vector2(0.88f, 0.9f), new Color(0.4f, 0.3f, 0.1f, 0.8f));
            skillPointsText = CreateResourceText(spContainer, $"{skillPoints} SP", new Color(1f, 1f, 0.4f));
        }

        /// <summary>
        /// リソースコンテナ作成
        /// </summary>
        private Transform CreateResourceContainer(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var container = new GameObject($"Resource{name}");
            container.transform.SetParent(parent, false);

            var rect = container.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var bg = container.AddComponent<Image>();
            bg.color = bgColor;

            return container.transform;
        }

        /// <summary>
        /// リソーステキスト作成
        /// </summary>
        private Text CreateResourceText(Transform parent, string text, Color color)
        {
            var textObj = new GameObject("ResourceText");
            textObj.transform.SetParent(parent, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 16;
            textComponent.color = color;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.fontStyle = FontStyle.Bold;

            return textComponent;
        }

        /// <summary>
        /// 設定ボタン作成
        /// </summary>
        private void CreateSettingsButton(Transform parent)
        {
            var settingsObj = new GameObject("SettingsButton");
            settingsObj.transform.SetParent(parent, false);

            var settingsRect = settingsObj.AddComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.9f, 0.1f);
            settingsRect.anchorMax = new Vector2(0.98f, 0.9f);
            settingsRect.sizeDelta = Vector2.zero;
            settingsRect.anchoredPosition = Vector2.zero;

            var settingsButton = settingsObj.AddComponent<Button>();
            var settingsImage = settingsObj.AddComponent<Image>();
            settingsImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            var settingsText = CreateButtonText(settingsObj.transform, "⚙", 20);
            settingsButton.targetGraphic = settingsImage;
            settingsButton.onClick.AddListener(OnSettingsButton);
        }

        /// <summary>
        /// ステージグリッド作成
        /// </summary>
        private void CreateStageGrid()
        {
            stageGridContainer = new GameObject("StageGrid");
            stageGridContainer.transform.SetParent(mainCanvas.transform, false);

            var gridRect = stageGridContainer.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.1f, 0.25f);
            gridRect.anchorMax = new Vector2(0.7f, 0.8f);
            gridRect.sizeDelta = Vector2.zero;
            gridRect.anchoredPosition = Vector2.zero;

            var gridLayout = stageGridContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(120, 120);
            gridLayout.spacing = new Vector2(20, 20);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // 各ステージボタン作成
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    CreateStageButton(row, col);
                }
            }
        }

        /// <summary>
        /// ステージボタン作成
        /// </summary>
        private void CreateStageButton(int row, int col)
        {
            var stage = stages[row, col];
            var buttonObj = new GameObject($"StageButton_{row}_{col}");
            buttonObj.transform.SetParent(stageGridContainer.transform, false);

            var button = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();

            // ステージタイプ別色設定
            image.color = GetStageColor(stage);
            button.targetGraphic = image;

            // ステージアイコン・テキスト
            var iconText = CreateButtonText(buttonObj.transform, GetStageIcon(stage.stageType), 16);
            iconText.text += $"\n{stage.stageName}";
            iconText.fontSize = 12;

            // 現在地マーカー
            if (row == currentPosition.y && col == currentPosition.x)
            {
                CreateCurrentPositionMarker(buttonObj.transform);
            }

            // アクセス可能インジケーター
            if (IsAccessible(row, col) && !(row == currentPosition.y && col == currentPosition.x))
            {
                CreateAccessibleIndicator(buttonObj.transform);
            }

            // ボタンイベント
            int capturedRow = row, capturedCol = col;
            button.onClick.AddListener(() => OnStageButtonClicked(capturedRow, capturedCol));

            // ホバーイベント
            var trigger = buttonObj.AddComponent<EventTrigger>();
            var pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            pointerEnter.callback.AddListener((data) => OnStageButtonHover(capturedRow, capturedCol));
            trigger.triggers.Add(pointerEnter);

            stageButtons[row, col] = button;
        }

        /// <summary>
        /// ボタンテキスト作成ヘルパー
        /// </summary>
        private Text CreateButtonText(Transform parent, string text, int fontSize)
        {
            var textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(parent, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.fontStyle = FontStyle.Bold;

            return textComponent;
        }

        /// <summary>
        /// 現在位置マーカー作成
        /// </summary>
        private void CreateCurrentPositionMarker(Transform parent)
        {
            var markerObj = new GameObject("CurrentMarker");
            markerObj.transform.SetParent(parent, false);

            var markerRect = markerObj.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.8f, 0.8f);
            markerRect.anchorMax = new Vector2(1f, 1f);
            markerRect.sizeDelta = Vector2.zero;
            markerRect.anchoredPosition = Vector2.zero;

            var markerImage = markerObj.AddComponent<Image>();
            markerImage.color = new Color(1f, 1f, 0.4f);

            var markerText = CreateButtonText(markerObj.transform, "⚡", 12);
        }

        /// <summary>
        /// アクセス可能インジケーター作成
        /// </summary>
        private void CreateAccessibleIndicator(Transform parent)
        {
            var indicatorObj = new GameObject("AccessibleIndicator");
            indicatorObj.transform.SetParent(parent, false);

            var indicatorRect = indicatorObj.AddComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0.8f, 0f);
            indicatorRect.anchorMax = new Vector2(1f, 0.2f);
            indicatorRect.sizeDelta = Vector2.zero;
            indicatorRect.anchoredPosition = Vector2.zero;

            var indicatorImage = indicatorObj.AddComponent<Image>();
            indicatorImage.color = new Color(0.4f, 1f, 0.4f);

            // アニメーション効果（簡易）
            var animator = indicatorObj.AddComponent<PingAnimator>();
            animator.Initialize();
        }

        /// <summary>
        /// 下部コントロール作成
        /// </summary>
        private void CreateBottomControls()
        {
            bottomControlsContainer = new GameObject("BottomControls");
            bottomControlsContainer.transform.SetParent(mainCanvas.transform, false);

            var bottomRect = bottomControlsContainer.AddComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0f);
            bottomRect.anchorMax = new Vector2(1, 0.2f);
            bottomRect.sizeDelta = Vector2.zero;
            bottomRect.anchoredPosition = Vector2.zero;

            // ステージタイプ凡例
            CreateStageLegend(bottomControlsContainer.transform);

            // エリア移動ボタン
            CreateAreaMoveButton(bottomControlsContainer.transform);
        }

        /// <summary>
        /// ステージタイプ凡例作成
        /// </summary>
        private void CreateStageLegend(Transform parent)
        {
            var legendObj = new GameObject("StageLegend");
            legendObj.transform.SetParent(parent, false);

            var legendRect = legendObj.AddComponent<RectTransform>();
            legendRect.anchorMin = new Vector2(0.02f, 0.3f);
            legendRect.anchorMax = new Vector2(0.6f, 0.9f);
            legendRect.sizeDelta = Vector2.zero;
            legendRect.anchoredPosition = Vector2.zero;

            var legendBg = legendObj.AddComponent<Image>();
            legendBg.color = new Color(0.1f, 0.12f, 0.15f, 0.9f);

            var legendTitle = CreateButtonText(legendObj.transform, "ステージタイプ", 14);
            legendTitle.alignment = TextAnchor.UpperLeft;

            // TODO: 実際の凡例アイテムを追加
        }

        /// <summary>
        /// エリア移動ボタン作成
        /// </summary>
        private void CreateAreaMoveButton(Transform parent)
        {
            var moveButtonObj = new GameObject("AreaMoveButton");
            moveButtonObj.transform.SetParent(parent, false);

            var moveButtonRect = moveButtonObj.AddComponent<RectTransform>();
            moveButtonRect.anchorMin = new Vector2(0.65f, 0.3f);
            moveButtonRect.anchorMax = new Vector2(0.98f, 0.9f);
            moveButtonRect.sizeDelta = Vector2.zero;
            moveButtonRect.anchoredPosition = Vector2.zero;

            var moveButton = moveButtonObj.AddComponent<Button>();
            var moveImage = moveButtonObj.AddComponent<Image>();
            moveImage.color = new Color(0.6f, 0.2f, 0.8f, 0.8f);

            var moveText = CreateButtonText(moveButtonObj.transform, "ステージ完了してエリア移動", 16);
            moveButton.targetGraphic = moveImage;
            moveButton.onClick.AddListener(OnAreaMoveButton);
        }

        #endregion

        #region Event System

        /// <summary>
        /// EventSystem確認・作成
        /// </summary>
        private void SetupEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// キーボードナビゲーション処理
        /// </summary>
        private void HandleKeyboardNavigation()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                NavigateStage(-1, 0);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                NavigateStage(1, 0);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                NavigateStage(0, -1);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                NavigateStage(0, 1);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                var currentButton = EventSystem.current.currentSelectedGameObject?.GetComponent<Button>();
                currentButton?.onClick.Invoke();
            }
        }

        /// <summary>
        /// ステージナビゲーション
        /// </summary>
        private void NavigateStage(int deltaRow, int deltaCol)
        {
            // 現在選択されているボタンから新しい位置を計算
            int newRow = currentPosition.y + deltaRow;
            int newCol = currentPosition.x + deltaCol;

            // 範囲チェック
            if (newRow >= 0 && newRow < 4 && newCol >= 0 && newCol < 4)
            {
                if (IsAccessible(newRow, newCol))
                {
                    stageButtons[newRow, newCol].Select();
                }
            }
        }

        #endregion

        #region Button Events

        /// <summary>
        /// ステージボタンクリック
        /// </summary>
        private void OnStageButtonClicked(int row, int col)
        {
            if (!IsAccessible(row, col)) return;

            var stage = stages[row, col];
            selectedStage = stage;

            Debug.Log($"Stage selected: {stage.stageName} at ({row}, {col})");

            // 詳細パネル表示
            ShowStageDetailPanel(stage, row, col);
        }

        /// <summary>
        /// ステージボタンホバー
        /// </summary>
        private void OnStageButtonHover(int row, int col)
        {
            var stage = stages[row, col];
            // ホバー時の詳細情報表示（簡易版）
            Debug.Log($"Hovering: {stage.stageName} - {stage.description}");
        }

        /// <summary>
        /// 戻るボタン
        /// </summary>
        private void OnBackButton()
        {
            Debug.Log("戻るボタン押下");
            // タイトル画面に戻る
            var titleUI = FindObjectOfType<TitleScreenUI>();
            if (titleUI != null)
            {
                gameObject.SetActive(false);
                titleUI.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 設定ボタン
        /// </summary>
        private void OnSettingsButton()
        {
            Debug.Log("設定ボタン押下");
            // TODO: 設定UI表示
        }

        /// <summary>
        /// エリア移動ボタン
        /// </summary>
        private void OnAreaMoveButton()
        {
            Debug.Log("エリア移動ボタン押下");
            // TODO: エリア移動処理
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// アクセス可能判定
        /// </summary>
        private bool IsAccessible(int row, int col)
        {
            if (row == currentPosition.y && col == currentPosition.x) return true;

            // 隣接マス判定（上下左右のみ）
            int distance = Mathf.Abs(row - currentPosition.y) + Mathf.Abs(col - currentPosition.x);
            return distance == 1;
        }

        /// <summary>
        /// ステージタイプ別色取得
        /// </summary>
        private Color GetStageColor(StageData stage)
        {
            switch (stage.stageType)
            {
                case StageType.Start: return new Color(0.2f, 0.4f, 0.8f, 0.8f);      // 青
                case StageType.Battle: return new Color(0.8f, 0.2f, 0.2f, 0.8f);     // 赤
                case StageType.BattleRare: return new Color(0.6f, 0.2f, 0.8f, 0.8f); // 紫
                case StageType.Shop: return new Color(0.2f, 0.8f, 0.2f, 0.8f);       // 緑
                case StageType.Casino: return new Color(0.8f, 0.6f, 0.2f, 0.8f);     // オレンジ
                case StageType.Inn: return new Color(0.2f, 0.8f, 0.8f, 0.8f);        // シアン
                case StageType.Tavern: return new Color(0.8f, 0.6f, 0.2f, 0.8f);     // アンバー
                case StageType.MapSwap: return new Color(0.4f, 0.2f, 0.8f, 0.8f);    // インディゴ
                default: return new Color(0.5f, 0.5f, 0.5f, 0.8f);                   // グレー
            }
        }

        /// <summary>
        /// ステージタイプ別アイコン取得
        /// </summary>
        private string GetStageIcon(StageType stageType)
        {
            switch (stageType)
            {
                case StageType.Start: return "⚡";
                case StageType.Battle: return "⚔";
                case StageType.BattleRare: return "♔";
                case StageType.Shop: return "🏪";
                case StageType.Casino: return "🎲";
                case StageType.Inn: return "🏠";
                case StageType.Tavern: return "☕";
                case StageType.MapSwap: return "🗺";
                default: return "?";
            }
        }

        /// <summary>
        /// ステージ詳細パネル表示
        /// </summary>
        private void ShowStageDetailPanel(StageData stage, int row, int col)
        {
            // TODO: 詳細パネル実装
            Debug.Log($"Showing detail panel for: {stage.stageName}");
            
            // 現在位置更新（実際にステージに移動する場合）
            if (row != currentPosition.y || col != currentPosition.x)
            {
                currentPosition = new Vector2Int(col, row);
                UpdateStageAccessibility();
            }
        }

        /// <summary>
        /// ステージアクセス可能性更新
        /// </summary>
        private void UpdateStageAccessibility()
        {
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var button = stageButtons[row, col];
                    if (button != null)
                    {
                        button.interactable = IsAccessible(row, col);
                        
                        // 見た目も更新
                        var image = button.GetComponent<Image>();
                        if (!IsAccessible(row, col))
                        {
                            var color = image.color;
                            color.a = 0.5f;
                            image.color = color;
                        }
                        else
                        {
                            var color = GetStageColor(stages[row, col]);
                            image.color = color;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// リソース表示更新
        /// </summary>
        private void UpdateResourceDisplay()
        {
            if (goldText != null)
                goldText.text = $"{gold}G";
            
            if (skillPointsText != null)
                skillPointsText.text = $"{skillPoints} SP";
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// リソース更新（外部用）
        /// </summary>
        public void UpdateResources(int newGold, int newSkillPoints)
        {
            gold = newGold;
            skillPoints = newSkillPoints;
            UpdateResourceDisplay();
        }

        /// <summary>
        /// 現在位置設定（外部用）
        /// </summary>
        public void SetCurrentPosition(int col, int row)
        {
            currentPosition = new Vector2Int(col, row);
            UpdateStageAccessibility();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// ステージタイプ
    /// </summary>
    public enum StageType
    {
        Start,
        Battle,
        BattleRare,
        Shop,
        Casino,
        Inn,
        Tavern,
        MapSwap
    }

    /// <summary>
    /// ステージ難易度
    /// </summary>
    public enum StageDifficulty
    {
        Easy,
        Normal,
        Hard,
        Risky,
        Current,
        Neutral
    }

    /// <summary>
    /// ステージデータ構造
    /// </summary>
    [Serializable]
    public class StageData
    {
        public StageType stageType;
        public string stageName;
        public string description;
        public string reward;
        public StageDifficulty difficulty;
    }

    /// <summary>
    /// ピンアニメーター（簡易）
    /// </summary>
    public class PingAnimator : MonoBehaviour
    {
        private float time = 0f;
        private Image image;

        public void Initialize()
        {
            image = GetComponent<Image>();
        }

        private void Update()
        {
            if (image == null) return;

            time += Time.deltaTime * 3f;
            float alpha = 0.5f + 0.5f * Mathf.Sin(time);
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    #endregion
}