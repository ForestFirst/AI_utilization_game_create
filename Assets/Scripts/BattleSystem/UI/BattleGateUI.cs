using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BattleSystem.UI
{
    /// <summary>
    /// 戦闘ゲートUIシステム - 戦闘フィールドの特殊エリア管理
    /// </summary>
    public class BattleGateUI : MonoBehaviour
    {
        [Header("ゲート管理")]
        [SerializeField] private List<GateInfo> activeGates = new List<GateInfo>();
        [SerializeField] private GateInfo selectedGate;
        [SerializeField] private bool isGateUIActive = false;

        [Header("UI設定")]
        [SerializeField] private float animationDuration = 0.3f;
        [SerializeField] private bool debugMode = false;

        // UI要素
        private Canvas gateCanvas;
        private GameObject gatePanel;
        private GameObject gateListContainer;
        private GameObject gateDetailPanel;
        private Text gateCountText;
        private Button activateButton;
        private Button closeButton;

        // ゲート管理
        private Dictionary<string, GameObject> gateButtons;
        private Coroutine currentAnimation;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeGateSystem();
            CreateGateUI();
            CreateSampleGates();
            UpdateGateDisplay();
            
            if (debugMode)
                Debug.Log("BattleGateUI initialized with " + activeGates.Count + " gates");
        }

        private void Update()
        {
            // キーボードショートカット
            if (Input.GetKeyDown(KeyCode.G))
            {
                ToggleGateUI();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape) && isGateUIActive)
            {
                HideGateUI();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ゲートシステム初期化
        /// </summary>
        private void InitializeGateSystem()
        {
            gateButtons = new Dictionary<string, GameObject>();
            
            // EventSystem確認
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(eventSystemObj);
            }
        }

        /// <summary>
        /// ゲートUI作成
        /// </summary>
        private void CreateGateUI()
        {
            // メインCanvas作成
            CreateGateCanvas();
            
            // ゲートパネル作成
            CreateGatePanel();
            
            // ヘッダー作成
            CreateGateHeader();
            
            // ゲートリスト作成
            CreateGateList();
            
            // 詳細パネル作成
            CreateGateDetailPanel();
            
            // アクションボタン作成
            CreateActionButtons();
            
            // 初期状態は非表示
            gatePanel.SetActive(false);
        }

        /// <summary>
        /// ゲートCanvas作成
        /// </summary>
        private void CreateGateCanvas()
        {
            var canvasObj = new GameObject("GateCanvas");
            canvasObj.transform.SetParent(transform, false);

            gateCanvas = canvasObj.AddComponent<Canvas>();
            gateCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gateCanvas.sortingOrder = 2000; // インベントリより上位

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// ゲートパネル作成
        /// </summary>
        private void CreateGatePanel()
        {
            gatePanel = new GameObject("GatePanel");
            gatePanel.transform.SetParent(gateCanvas.transform, false);

            var panelRect = gatePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0.1f);
            panelRect.anchorMax = new Vector2(0.8f, 0.9f);
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            // パネル背景
            var panelBg = gatePanel.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f); // ダークブルー

            // サイバーパンク風ボーダー
            CreatePanelBorder(gatePanel.transform);
        }

        /// <summary>
        /// パネルボーダー作成
        /// </summary>
        private void CreatePanelBorder(Transform parent)
        {
            var borderObj = new GameObject("PanelBorder");
            borderObj.transform.SetParent(parent, false);

            var borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            borderRect.anchoredPosition = Vector2.zero;

            var borderImage = borderObj.AddComponent<Image>();
            borderImage.color = new Color(0.2f, 0.8f, 1f, 0.6f); // ネオンブルー
            
            // ボーダーを外側のみに表示（簡易実装）
            borderRect.sizeDelta = new Vector2(4, 4);
        }

        /// <summary>
        /// ゲートヘッダー作成
        /// </summary>
        private void CreateGateHeader()
        {
            var headerObj = new GameObject("GateHeader");
            headerObj.transform.SetParent(gatePanel.transform, false);

            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = new Vector2(1, 1f);
            headerRect.sizeDelta = Vector2.zero;
            headerRect.anchoredPosition = Vector2.zero;

            // ヘッダー背景
            var headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.15f, 0.2f, 0.35f, 0.8f);

            // タイトル
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(headerObj.transform, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0);
            titleRect.anchorMax = new Vector2(0.6f, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "▲ BATTLE GATES ▲";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 28;
            titleText.color = new Color(0.2f, 0.8f, 1f, 1f);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;

            // ゲート数表示
            var countObj = new GameObject("GateCount");
            countObj.transform.SetParent(headerObj.transform, false);

            var countRect = countObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.65f, 0.2f);
            countRect.anchorMax = new Vector2(0.9f, 0.8f);
            countRect.sizeDelta = Vector2.zero;
            countRect.anchoredPosition = Vector2.zero;

            gateCountText = countObj.AddComponent<Text>();
            gateCountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gateCountText.fontSize = 18;
            gateCountText.color = new Color(0.8f, 1f, 0.8f, 1f);
            gateCountText.alignment = TextAnchor.MiddleCenter;
            gateCountText.fontStyle = FontStyle.Bold;

            // 閉じるボタン
            closeButton = CreateHeaderButton(headerObj.transform, "×", new Vector2(0.92f, 0.1f), new Vector2(0.98f, 0.9f));
            closeButton.onClick.AddListener(HideGateUI);
        }

        /// <summary>
        /// ヘッダーボタン作成
        /// </summary>
        private Button CreateHeaderButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            var buttonObj = new GameObject("HeaderButton_" + text);
            buttonObj.transform.SetParent(parent, false);

            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;

            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.7f);
            button.targetGraphic = buttonImage;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 24;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontStyle = FontStyle.Bold;

            return button;
        }

        #endregion

        #region Gate List UI

        /// <summary>
        /// ゲートリスト作成
        /// </summary>
        private void CreateGateList()
        {
            gateListContainer = new GameObject("GateListContainer");
            gateListContainer.transform.SetParent(gatePanel.transform, false);

            var listRect = gateListContainer.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.05f, 0.55f);
            listRect.anchorMax = new Vector2(0.95f, 0.85f);
            listRect.sizeDelta = Vector2.zero;
            listRect.anchoredPosition = Vector2.zero;

            // 背景
            var listBg = gateListContainer.AddComponent<Image>();
            listBg.color = new Color(0.1f, 0.15f, 0.2f, 0.8f);

            // スクロールビュー作成
            CreateGateScrollView(gateListContainer.transform);
        }

        /// <summary>
        /// ゲートスクロールビュー作成
        /// </summary>
        private void CreateGateScrollView(Transform parent)
        {
            var scrollObj = new GameObject("GateScrollView");
            scrollObj.transform.SetParent(parent, false);

            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.02f, 0.05f);
            scrollRect.anchorMax = new Vector2(0.98f, 0.95f);
            scrollRect.sizeDelta = Vector2.zero;
            scrollRect.anchoredPosition = Vector2.zero;

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true;
            scroll.horizontal = false;

            // Content作成
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            var layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 8f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            var sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;
        }

        /// <summary>
        /// ゲートリスト更新
        /// </summary>
        private void UpdateGateList()
        {
            // 既存のゲートボタンをクリア
            var contentTransform = gateListContainer.transform.GetChild(0).GetChild(0);
            foreach (Transform child in contentTransform)
            {
                DestroyImmediate(child.gameObject);
            }
            gateButtons.Clear();

            // 各ゲートのボタン作成
            foreach (var gate in activeGates)
            {
                CreateGateButton(gate, contentTransform);
            }
        }

        /// <summary>
        /// ゲートボタン作成
        /// </summary>
        private void CreateGateButton(GateInfo gate, Transform parent)
        {
            var buttonObj = new GameObject($"GateButton_{gate.gateId}");
            buttonObj.transform.SetParent(parent, false);

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 80;
            layoutElement.minHeight = 80;

            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = GetGateTypeColor(gate.gateType);
            button.targetGraphic = buttonImage;

            // ゲート名
            var nameObj = new GameObject("GateName");
            nameObj.transform.SetParent(buttonObj.transform, false);

            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.6f);
            nameRect.anchorMax = new Vector2(0.7f, 0.9f);
            nameRect.sizeDelta = Vector2.zero;
            nameRect.anchoredPosition = Vector2.zero;

            var nameText = nameObj.AddComponent<Text>();
            nameText.text = gate.gateName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 18;
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.fontStyle = FontStyle.Bold;

            // ゲート状態
            var statusObj = new GameObject("GateStatus");
            statusObj.transform.SetParent(buttonObj.transform, false);

            var statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.72f, 0.6f);
            statusRect.anchorMax = new Vector2(0.95f, 0.9f);
            statusRect.sizeDelta = Vector2.zero;
            statusRect.anchoredPosition = Vector2.zero;

            var statusText = statusObj.AddComponent<Text>();
            statusText.text = gate.isActive ? "ACTIVE" : "INACTIVE";
            statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusText.fontSize = 12;
            statusText.color = gate.isActive ? Color.green : Color.gray;
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.fontStyle = FontStyle.Bold;

            // ゲート効果
            var effectObj = new GameObject("GateEffect");
            effectObj.transform.SetParent(buttonObj.transform, false);

            var effectRect = effectObj.AddComponent<RectTransform>();
            effectRect.anchorMin = new Vector2(0.05f, 0.1f);
            effectRect.anchorMax = new Vector2(0.95f, 0.5f);
            effectRect.sizeDelta = Vector2.zero;
            effectRect.anchoredPosition = Vector2.zero;

            var effectText = effectObj.AddComponent<Text>();
            effectText.text = gate.description;
            effectText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            effectText.fontSize = 10;
            effectText.color = new Color(0.8f, 0.9f, 1f, 0.8f);
            effectText.alignment = TextAnchor.UpperLeft;

            // ボタンイベント
            button.onClick.AddListener(() => OnGateSelected(gate));

            gateButtons[gate.gateId] = buttonObj;
        }

        /// <summary>
        /// ゲートタイプ別色取得
        /// </summary>
        private Color GetGateTypeColor(GateType gateType)
        {
            switch (gateType)
            {
                case GateType.Entrance: return new Color(0.2f, 0.8f, 0.3f, 0.8f); // 緑
                case GateType.Exit: return new Color(0.8f, 0.3f, 0.2f, 0.8f); // 赤
                case GateType.Teleport: return new Color(0.3f, 0.2f, 0.8f, 0.8f); // 青
                case GateType.Buff: return new Color(0.8f, 0.8f, 0.2f, 0.8f); // 黄
                case GateType.Trap: return new Color(0.8f, 0.2f, 0.8f, 0.8f); // 紫
                default: return new Color(0.5f, 0.5f, 0.5f, 0.8f); // グレー
            }
        }

        #endregion

        #region Gate Detail Panel

        /// <summary>
        /// ゲート詳細パネル作成
        /// </summary>
        private void CreateGateDetailPanel()
        {
            gateDetailPanel = new GameObject("GateDetailPanel");
            gateDetailPanel.transform.SetParent(gatePanel.transform, false);

            var detailRect = gateDetailPanel.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.05f, 0.15f);
            detailRect.anchorMax = new Vector2(0.95f, 0.5f);
            detailRect.sizeDelta = Vector2.zero;
            detailRect.anchoredPosition = Vector2.zero;

            // 背景
            var detailBg = gateDetailPanel.AddComponent<Image>();
            detailBg.color = new Color(0.12f, 0.18f, 0.25f, 0.9f);

            // プレースホルダー
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(gateDetailPanel.transform, false);

            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;

            var placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = "ゲートを選択してください";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 20;
            placeholderText.color = Color.gray;
            placeholderText.alignment = TextAnchor.MiddleCenter;
        }

        #endregion

        #region Action Buttons

        /// <summary>
        /// アクションボタン作成
        /// </summary>
        private void CreateActionButtons()
        {
            var buttonContainer = new GameObject("ActionButtonContainer");
            buttonContainer.transform.SetParent(gatePanel.transform, false);

            var containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.2f, 0.05f);
            containerRect.anchorMax = new Vector2(0.8f, 0.12f);
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;

            // Activate/Deactivate ボタン
            activateButton = CreateActionButton(buttonContainer.transform, "ACTIVATE GATE", 
                new Vector2(0, 0), new Vector2(0.45f, 1f), new Color(0.2f, 0.8f, 0.3f, 0.8f));
            activateButton.onClick.AddListener(ActivateSelectedGate);
            activateButton.interactable = false;

            // 詳細情報ボタン
            var infoButton = CreateActionButton(buttonContainer.transform, "DETAILED INFO", 
                new Vector2(0.55f, 0), new Vector2(1f, 1f), new Color(0.2f, 0.6f, 0.8f, 0.8f));
            infoButton.onClick.AddListener(ShowGateInfo);
            infoButton.interactable = false;
        }

        /// <summary>
        /// アクションボタン作成
        /// </summary>
        private Button CreateActionButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var buttonObj = new GameObject($"ActionButton_{text}");
            buttonObj.transform.SetParent(parent, false);

            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;

            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = color;
            button.targetGraphic = buttonImage;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontStyle = FontStyle.Bold;

            return button;
        }

        #endregion

        #region Sample Gates

        /// <summary>
        /// サンプルゲート作成
        /// </summary>
        private void CreateSampleGates()
        {
            activeGates = new List<GateInfo>
            {
                new GateInfo
                {
                    gateId = "gate_01",
                    gateName = "戦術展開ゲート",
                    gateType = GateType.Entrance,
                    position = new Vector3(2, 0, 3),
                    isActive = true,
                    description = "味方ユニットの配置エリア。戦闘開始時の初期配置を決定。",
                    effect = "InitialDeployment"
                },
                new GateInfo
                {
                    gateId = "gate_02",
                    gateName = "敵襲撃ゲート",
                    gateType = GateType.Entrance,
                    position = new Vector3(6, 0, 7),
                    isActive = true,
                    description = "敵ユニットが出現するエリア。警戒が必要。",
                    effect = "EnemySpawn"
                },
                new GateInfo
                {
                    gateId = "gate_03",
                    gateName = "テレポートゲート",
                    gateType = GateType.Teleport,
                    position = new Vector3(1, 0, 8),
                    isActive = false,
                    description = "特定条件で別エリアへの瞬間移動が可能。",
                    effect = "Teleport"
                },
                new GateInfo
                {
                    gateId = "gate_04",
                    gateName = "強化ゲート",
                    gateType = GateType.Buff,
                    position = new Vector3(4, 0, 2),
                    isActive = true,
                    description = "通過すると一時的に能力値が向上する。",
                    effect = "BuffZone"
                },
                new GateInfo
                {
                    gateId = "gate_05",
                    gateName = "トラップゲート",
                    gateType = GateType.Trap,
                    position = new Vector3(8, 0, 4),
                    isActive = true,
                    description = "隠された罠。回避するか無効化する必要がある。",
                    effect = "TrapDamage"
                },
                new GateInfo
                {
                    gateId = "gate_06",
                    gateName = "脱出ゲート",
                    gateType = GateType.Exit,
                    position = new Vector3(9, 0, 9),
                    isActive = false,
                    description = "戦闘エリアからの脱出口。勝利条件達成で開放。",
                    effect = "Victory"
                }
            };
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ゲート選択時の処理
        /// </summary>
        private void OnGateSelected(GateInfo gate)
        {
            selectedGate = gate;
            activateButton.interactable = true;
            
            UpdateGateDetailDisplay(gate);
            
            if (debugMode)
                Debug.Log($"Gate selected: {gate.gateName} ({gate.gateType})");
        }

        /// <summary>
        /// ゲート詳細表示更新
        /// </summary>
        private void UpdateGateDetailDisplay(GateInfo gate)
        {
            // 既存のプレースホルダーを削除
            var placeholder = gateDetailPanel.transform.Find("Placeholder");
            if (placeholder != null)
            {
                DestroyImmediate(placeholder.gameObject);
            }

            // 詳細情報表示作成
            CreateDetailContent(gate, gateDetailPanel.transform);
        }

        /// <summary>
        /// 詳細コンテンツ作成
        /// </summary>
        private void CreateDetailContent(GateInfo gate, Transform parent)
        {
            var detailObj = new GameObject("DetailContent");
            detailObj.transform.SetParent(parent, false);

            var detailRect = detailObj.AddComponent<RectTransform>();
            detailRect.anchorMin = Vector2.zero;
            detailRect.anchorMax = Vector2.one;
            detailRect.sizeDelta = Vector2.zero;
            detailRect.anchoredPosition = Vector2.zero;

            // レイアウトグループ
            var layoutGroup = detailObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(20, 20, 20, 20);
            layoutGroup.childAlignment = TextAnchor.UpperLeft;

            // ゲート名
            CreateDetailText(detailObj.transform, $"▲ {gate.gateName}", 24, Color.white, FontStyle.Bold);
            
            // ゲートタイプ
            CreateDetailText(detailObj.transform, $"Type: {gate.gateType}", 16, GetGateTypeColor(gate.gateType));
            
            // ポジション
            CreateDetailText(detailObj.transform, $"Position: ({gate.position.x}, {gate.position.z})", 16, Color.gray);
            
            // ステータス
            var statusColor = gate.isActive ? Color.green : Color.red;
            var statusText = gate.isActive ? "ACTIVE" : "INACTIVE";
            CreateDetailText(detailObj.transform, $"Status: {statusText}", 16, statusColor);
            
            // 説明
            CreateDetailText(detailObj.transform, gate.description, 14, new Color(0.8f, 0.9f, 1f));
        }

        /// <summary>
        /// 詳細テキスト作成
        /// </summary>
        private void CreateDetailText(Transform parent, string text, int fontSize, Color color, FontStyle fontStyle = FontStyle.Normal)
        {
            var textObj = new GameObject("DetailText");
            textObj.transform.SetParent(parent, false);

            var layoutElement = textObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = fontSize + 5;

            var textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.color = color;
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.fontStyle = fontStyle;
        }

        /// <summary>
        /// 選択されたゲートのアクティベート
        /// </summary>
        private void ActivateSelectedGate()
        {
            if (selectedGate != null)
            {
                selectedGate.isActive = !selectedGate.isActive;
                
                var statusText = selectedGate.isActive ? "activated" : "deactivated";
                Debug.Log($"Gate {selectedGate.gateName} {statusText}");
                
                UpdateGateDisplay();
                UpdateGateDetailDisplay(selectedGate);
            }
        }

        /// <summary>
        /// ゲート情報表示
        /// </summary>
        private void ShowGateInfo()
        {
            if (selectedGate != null && debugMode)
            {
                Debug.Log($"Gate Info: ID={selectedGate.gateId}, Name={selectedGate.gateName}, " +
                         $"Type={selectedGate.gateType}, Position={selectedGate.position}, " +
                         $"Active={selectedGate.isActive}, Effect={selectedGate.effect}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ゲートUI表示切り替え
        /// </summary>
        public void ToggleGateUI()
        {
            if (isGateUIActive)
                HideGateUI();
            else
                ShowGateUI();
        }

        /// <summary>
        /// ゲートUI表示
        /// </summary>
        public void ShowGateUI()
        {
            if (!isGateUIActive)
            {
                isGateUIActive = true;
                gatePanel.SetActive(true);
                
                UpdateGateDisplay();
                
                if (currentAnimation != null)
                    StopCoroutine(currentAnimation);
                currentAnimation = StartCoroutine(AnimateGateUI(true));
                
                if (debugMode)
                    Debug.Log("Gate UI shown");
            }
        }

        /// <summary>
        /// ゲートUI非表示
        /// </summary>
        public void HideGateUI()
        {
            if (isGateUIActive)
            {
                isGateUIActive = false;
                
                if (currentAnimation != null)
                    StopCoroutine(currentAnimation);
                currentAnimation = StartCoroutine(AnimateGateUI(false));
                
                if (debugMode)
                    Debug.Log("Gate UI hidden");
            }
        }

        /// <summary>
        /// ゲート表示更新
        /// </summary>
        public void UpdateGateDisplay()
        {
            UpdateGateList();
            
            if (gateCountText != null)
            {
                var activeCount = activeGates.FindAll(g => g.isActive).Count;
                gateCountText.text = $"{activeCount}/{activeGates.Count} Gates Active";
            }
        }

        /// <summary>
        /// ゲート追加
        /// </summary>
        public void AddGate(GateInfo newGate)
        {
            if (!activeGates.Exists(g => g.gateId == newGate.gateId))
            {
                activeGates.Add(newGate);
                UpdateGateDisplay();
                
                if (debugMode)
                    Debug.Log($"Gate added: {newGate.gateName}");
            }
        }

        /// <summary>
        /// ゲート削除
        /// </summary>
        public void RemoveGate(string gateId)
        {
            var gate = activeGates.Find(g => g.gateId == gateId);
            if (gate != null)
            {
                activeGates.Remove(gate);
                
                if (selectedGate != null && selectedGate.gateId == gateId)
                {
                    selectedGate = null;
                    activateButton.interactable = false;
                }
                
                UpdateGateDisplay();
                
                if (debugMode)
                    Debug.Log($"Gate removed: {gateId}");
            }
        }

        /// <summary>
        /// 全ゲート取得
        /// </summary>
        public List<GateInfo> GetAllGates()
        {
            return new List<GateInfo>(activeGates);
        }

        /// <summary>
        /// アクティブゲート取得
        /// </summary>
        public List<GateInfo> GetActiveGates()
        {
            return activeGates.FindAll(g => g.isActive);
        }

        /// <summary>
        /// ゲート取得
        /// </summary>
        public GateInfo GetGate(string gateId)
        {
            return activeGates.Find(g => g.gateId == gateId);
        }

        #endregion

        #region Animation

        /// <summary>
        /// ゲートUIアニメーション
        /// </summary>
        private IEnumerator AnimateGateUI(bool show)
        {
            var startScale = show ? Vector3.zero : Vector3.one;
            var endScale = show ? Vector3.one : Vector3.zero;
            var elapsedTime = 0f;

            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                var progress = elapsedTime / animationDuration;
                var currentScale = Vector3.Lerp(startScale, endScale, progress);
                gatePanel.transform.localScale = currentScale;
                yield return null;
            }

            gatePanel.transform.localScale = endScale;
            
            if (!show)
                gatePanel.SetActive(false);
                
            currentAnimation = null;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// ゲート情報
    /// </summary>
    [System.Serializable]
    public class GateInfo
    {
        public string gateId;
        public string gateName;
        public GateType gateType;
        public Vector3 position;
        public bool isActive;
        public string description;
        public string effect;
    }

    /// <summary>
    /// ゲートタイプ列挙
    /// </summary>
    public enum GateType
    {
        Entrance,   // 入口
        Exit,       // 出口
        Teleport,   // テレポート
        Buff,       // 強化
        Trap        // 罠
    }

    #endregion
}