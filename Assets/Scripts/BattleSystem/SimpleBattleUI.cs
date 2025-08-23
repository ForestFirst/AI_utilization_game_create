using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static System.Reflection.BindingFlags;

namespace BattleSystem
{
    /// <summary>
    /// シンプルな戦闘UI表示テスト用コンポーネント
    /// 手札システム対応版（列選択機能は削除済み）
    /// Canvasにアタッチしてプレイモードでテストします
    /// </summary>
    public class SimpleBattleUI : MonoBehaviour
    {
        [Header("UI Creation Settings")]
        [SerializeField] private bool autoCreateUI = true;
        [SerializeField] private bool useStaticUIReferences = false;
        [SerializeField] private Font defaultFont;
        
        [Header("Static UI References - Canvas要素への直接参照")]
        [SerializeField] private GameObject staticTurnText;
        [SerializeField] private GameObject staticHpText;
        [SerializeField] private GameObject staticStateText;
        [SerializeField] private GameObject staticPendingDamageText;
        [SerializeField] private GameObject staticNextTurnButton;
        [SerializeField] private GameObject staticResetButton;
        [SerializeField] private GameObject staticEnemyInfoPanel;
        [SerializeField] private GameObject staticBattleFieldPanel;
        [SerializeField] private GameObject staticComboProgressPanel;
        [SerializeField] private GameObject staticStartScreenPanel;
        [SerializeField] private GameObject staticComboTestButton;
        
        [Header("Cyberpunk UI Style Settings")]
        [SerializeField] private Color primaryGlowColor = new Color(0f, 1f, 1f, 1f); // シアン
        [SerializeField] private Color secondaryGlowColor = new Color(1f, 0f, 1f, 1f); // マゼンタ
        [SerializeField] private Color warningColor = new Color(1f, 0.2f, 0.2f, 1f); // 赤
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.8f); // ダークブルー
        
        [Header("Combo Test Settings")]
        [SerializeField] private bool enableComboTest = true;
        
        // 日本語対応フォントアセット
        private TMP_FontAsset japaneseFont;
        
        private Canvas canvas;
        private BattleManager battleManager;
        private HandSystem handSystem; // HandSystem参照を追加
        private ComboSystem comboSystem; // ComboSystem参照を追加
        
        // システム参照
        private SceneTransitionManager sceneTransition;
        private PlayerDataManager playerDataManager;
        private GameEventManager eventManager;
        
        // UI要素
        private TextMeshProUGUI turnText;
        private TextMeshProUGUI hpText;
        private TextMeshProUGUI stateText;
        private Button nextTurnButton;
        private Button resetButton;
        private Button comboTestButton;
        
        // 予告ダメージ表示UI（HPの下に表示）
        private TextMeshProUGUI pendingDamageText;
        
        // 敵情報表示UI要素（右上）
        private GameObject enemyInfoPanel;
        private TextMeshProUGUI enemyInfoTitle;
        private TextMeshProUGUI[] enemyHpTexts = new TextMeshProUGUI[6]; // 最大6体の敵
        
        // 戦場表示UI要素（手札システム対応版 - 列選択機能削除）
        private GameObject battleFieldPanel;
        private GameObject[,] gridCells = new GameObject[3, 2];  // 3列×2行のグリッド
        private TextMeshProUGUI[] enemyTexts = new TextMeshProUGUI[6]; // 敵表示用テキスト（最大6体）
        
        // コンボUI要素（新規追加）
        private GameObject comboProgressPanel;
        private TextMeshProUGUI comboProgressTitle;
        private GameObject[] comboProgressItems = new GameObject[5]; // 最大5つのアクティブコンボ
        private TextMeshProUGUI[] comboNameTexts = new TextMeshProUGUI[5];
        private Slider[] comboProgressBars = new Slider[5];
        private TextMeshProUGUI[] comboStepTexts = new TextMeshProUGUI[5];
        private TextMeshProUGUI[] comboTimerTexts = new TextMeshProUGUI[5];
        private TextMeshProUGUI[] comboResistanceTexts = new TextMeshProUGUI[5];
        
        // コンボグループ管理用
        private Dictionary<string, ComboGroupContainer> comboGroups = new Dictionary<string, ComboGroupContainer>();
        private GameObject comboGroupContainer; // 全コンボグループの親コンテナ
        
        // コンボ効果表示UI要素（ポップアップ）
        private GameObject comboEffectPopup;
        private TextMeshProUGUI comboEffectTitle;
        private TextMeshProUGUI comboEffectDescription;
        
        // コンボ色分け設定
        private readonly Color[] comboTypeColors = new Color[]
        {
            new Color(1f, 0.5f, 0.5f, 0.8f),   // AttributeCombo - 赤系
            new Color(0.5f, 1f, 0.5f, 0.8f),   // WeaponCombo - 緑系
            new Color(0.5f, 0.5f, 1f, 0.8f),   // MixedCombo - 青系
            new Color(1f, 1f, 0.5f, 0.8f),     // SequenceCombo - 黄系
            new Color(1f, 0.5f, 1f, 0.8f)      // PowerCombo - 紫系
        };
        
        // 戦闘状態管理（新規追加）
        private bool isBattleStarted = false;
        
        // スタート画面UI要素（新規追加）
        private GameObject startScreenPanel;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI instructionText;
        private Button startBattleButton;
        
        // 戦闘UI要素のグループ（新規追加）
        private GameObject battleUIGroup;
        
        private void Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas component not found! Attach this script to a Canvas.");
                return;
            }
        
            // CanvasとCanvasScalerの設定を確実に行う
            SetupCanvasConfiguration();
            
            // 日本語フォントアセットを読み込み
            LoadJapaneseFont();
            
            if (useStaticUIReferences)
            {
                // 静的UI参照を使用する場合の初期化
                InitializeStaticUIReferences();
            }
            else if (autoCreateUI)
            {
                // 最初はスタート画面を表示
                CreateStartScreen();
                
                // 戦闘UIを作成するが非表示状態にしておく
                CreateBattleUIGroup();
                CreateSimpleBattleUI();
                battleUIGroup.SetActive(false); // 初期状態では非表示
            }
            
            // BattleManagerを探す（なければ作成）
            SetupBattleManager();
        }
        
        /// <summary>
        /// 敵情報表示を更新（右上エリア）
        /// </summary>
        private void UpdateEnemyInfoDisplay()
        {
            if (battleManager?.BattleField == null || enemyHpTexts == null) return;
            
            // 全ての敵HP表示をクリア
            for (int i = 0; i < enemyHpTexts.Length; i++)
            {
                if (enemyHpTexts[i] != null)
                    enemyHpTexts[i].text = "";
            }
            
            // 現在の敵を取得して表示
            var enemies = battleManager.BattleField.GetAllEnemies();
            int displayIndex = 0;
            
            foreach (var enemy in enemies)
            {
                if (displayIndex >= enemyHpTexts.Length) break;
                
                if (enemyHpTexts[displayIndex] != null)
                {
                    // 敵の位置情報も含めて表示
                    string locationInfo = $"({enemy.gridX + 1}, {enemy.gridY + 1})";
                    string hpInfo = $"{enemy.currentHp} / {enemy.enemyData.baseHp}";
                    
                    enemyHpTexts[displayIndex].text = $"{enemy.enemyData.enemyName} {locationInfo}\nHP: {hpInfo}";
                    
                    // HPの割合に応じて色を変更
                    float hpRatio = (float)enemy.currentHp / enemy.enemyData.baseHp;
                    if (hpRatio > 0.7f)
                    {
                        enemyHpTexts[displayIndex].color = Color.white; // 健康
                    }
                    else if (hpRatio > 0.3f)
                    {
                        enemyHpTexts[displayIndex].color = Color.yellow; // 負傷
                    }
                    else
                    {
                        enemyHpTexts[displayIndex].color = Color.red; // 重傷
                    }
                }
                
                displayIndex++;
            }
            
            // 敵情報タイトルを更新（敵数も表示）
            if (enemyInfoTitle != null)
            {
                enemyInfoTitle.text = $"=== 敵情報 ({enemies.Count}体) ===";
            }
        }
        
        /// <summary>
        /// コンボ進行状況表示を更新
        /// </summary>
        private void UpdateComboProgressDisplay()
        {
            if (comboProgressItems == null) return;
            
            // 装備アタッチメントのコンボ情報を表示
            UpdateEquippedAttachmentCombosDisplay();
            
            if (comboSystem == null) return;
            
            // 全てのコンボアイテムを一旦非表示
            for (int i = 0; i < comboProgressItems.Length; i++)
            {
                if (comboProgressItems[i] != null)
                    comboProgressItems[i].SetActive(false);
            }
            
            // 新システム: コンボグループの更新
            UpdateComboGroupsDisplay();
            
            // アクティブコンボを取得して表示
            var activeCombos = comboSystem.ActiveCombos;
            int displayIndex = 0;
            
            foreach (var combo in activeCombos)
            {
                if (displayIndex >= comboProgressItems.Length) break;
                if (combo == null) continue;
                
                // コンボアイテムを表示
                comboProgressItems[displayIndex].SetActive(true);
                
                // コンボタイプに応じた色分け
                Color comboColor = GetComboTypeColor(combo.comboData.condition.comboType);
                var panelImage = comboProgressItems[displayIndex].GetComponent<Image>();
                if (panelImage != null)
                {
                    panelImage.color = comboColor;
                }
                
                // コンボ名表示
                if (comboNameTexts[displayIndex] != null)
                {
                    comboNameTexts[displayIndex].text = combo.comboData.comboName;
                }
                
                // 進行率バー更新
                if (comboProgressBars[displayIndex] != null)
                {
                    comboProgressBars[displayIndex].value = combo.progressPercentage;
                    
                    // 進行率に応じてバーの色を変更
                    var fillImage = comboProgressBars[displayIndex].fillRect?.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        if (combo.progressPercentage >= 0.8f)
                        {
                            fillImage.color = Color.red; // 完成間近
                        }
                        else if (combo.progressPercentage >= 0.5f)
                        {
                            fillImage.color = Color.yellow; // 中間進行
                        }
                        else
                        {
                            fillImage.color = Color.green; // 開始段階
                        }
                    }
                }
                
                // 手数と残り手数表示（改良版）
                if (comboStepTexts[displayIndex] != null)
                {
                    int totalSteps = combo.comboData.requiredWeaponCount;
                    int currentSteps = combo.currentStep;
                    int remainingSteps = totalSteps - currentSteps;
                    comboStepTexts[displayIndex].text = $"{currentSteps}/{totalSteps} 残{remainingSteps}";
                    
                    // 残り手数に応じて色変更
                    if (remainingSteps <= 1)
                    {
                        comboStepTexts[displayIndex].color = Color.red; // 完成間近
                    }
                    else if (remainingSteps <= 2)
                    {
                        comboStepTexts[displayIndex].color = Color.yellow; // もうすぐ完成
                    }
                    else
                    {
                        comboStepTexts[displayIndex].color = Color.white; // 通常
                    }
                }
                
                // 次の手表示（改良版）
                if (comboTimerTexts[displayIndex] != null)
                {
                    string nextMoveText = GetNextRequiredMove(combo);
                    comboTimerTexts[displayIndex].text = $"次:{nextMoveText}";
                    comboTimerTexts[displayIndex].color = Color.cyan;
                }
                
                // 必要な残り手順表示（改良版）
                if (comboResistanceTexts[displayIndex] != null)
                {
                    string remainingMovesText = GetRemainingRequiredMoves(combo);
                    comboResistanceTexts[displayIndex].text = remainingMovesText;
                    comboResistanceTexts[displayIndex].color = Color.green;
                }
                
                displayIndex++;
            }
            
            // コンボ進行状況タイトルを更新
            if (comboProgressTitle != null)
            {
                comboProgressTitle.text = $"=== コンボ進行 ({activeCombos.Count}/5) ===";
            }
        }

        
        private void SetupCanvasConfiguration()
        {
            Debug.Log("Setting up Canvas configuration...");
            
            // Canvasの基本設定
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // CanvasScalerの設定
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                Debug.Log("CanvasScaler component added");
            }
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // GraphicRaycasterの確認
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("GraphicRaycaster component added");
            }
            
            // EventSystemの確認と作成（重要！）
            SetupEventSystem();
            
            Debug.Log($"Canvas configuration complete. Screen size will be scaled to fit {scaler.referenceResolution}");
        }
        
        /// <summary>
        /// EventSystemの設定（UIの入力処理に必要）
        /// </summary>
        private void SetupEventSystem()
        {
            // 既存のEventSystemを確認
            var existingEventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                Debug.Log("EventSystem not found, creating new one...");
                
                // EventSystemオブジェクトを作成
                GameObject eventSystemObj = new GameObject("EventSystem");
                
                // EventSystemコンポーネントを追加
                var eventSystem = eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                // StandaloneInputModuleを追加（マウス・キーボード入力用）
                var inputModule = eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                
                Debug.Log("✓ EventSystem created successfully!");
            }
            else
            {
                Debug.Log("✓ EventSystem already exists");
            }
        }
        
        /// <summary>
        /// 日本語対応フォントアセットを読み込む
        /// </summary>
        private void LoadJapaneseFont()
        {
            // DotGothic16-Regular SDFフォントアセットを読み込み
            japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/DotGothic16-Regular SDF");
            
            if (japaneseFont != null)
            {
                Debug.Log($"Japanese font loaded: {japaneseFont.name}");
            }
            else
            {
                Debug.LogWarning("Failed to load Japanese font! UI text may not display correctly.");
                // フォールバック：デフォルトのTextMeshProフォントを使用
                japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
        }
        
        private void Update()
        {
            UpdateUI();
        }
        
        private void OnDestroy()
        {
            // BattleManagerイベントの購読解除（メモリリーク防止）
            if (battleManager != null)
            {
                battleManager.OnPlayerDataChanged -= OnPlayerDataChanged;
            }
            
            // HandSystemイベントの購読解除（メモリリーク防止）
            if (handSystem != null)
            {
                handSystem.OnPendingDamageCalculated -= OnPendingDamageCalculated;
                handSystem.OnPendingDamageApplied -= OnPendingDamageApplied;
                handSystem.OnPendingDamageCleared -= OnPendingDamageCleared;
                handSystem.OnEnemyDataChanged -= OnEnemyDataChanged;
                handSystem.OnBattleFieldChanged -= OnBattleFieldChanged;
            }
            
            // ComboSystemイベントの購読解除（メモリリーク防止）
            if (comboSystem != null)
            {
                comboSystem.OnComboStarted -= OnComboStarted;
                comboSystem.OnComboProgressUpdated -= OnComboProgressUpdated;
                comboSystem.OnComboCompleted -= OnComboCompleted;
                comboSystem.OnComboFailed -= OnComboFailed;
                comboSystem.OnComboInterrupted -= OnComboInterrupted;
            }
        }
        
        /// <summary>
        /// 静的UI参照を初期化
        /// </summary>
        private void InitializeStaticUIReferences()
        {
            Debug.Log("静的UI参照モードで初期化中...");
            
            // 基本UI要素の参照を取得
            if (staticTurnText != null)
                turnText = staticTurnText.GetComponent<TextMeshProUGUI>();
            if (staticHpText != null)
                hpText = staticHpText.GetComponent<TextMeshProUGUI>();
            if (staticStateText != null)
                stateText = staticStateText.GetComponent<TextMeshProUGUI>();
            if (staticPendingDamageText != null)
                pendingDamageText = staticPendingDamageText.GetComponent<TextMeshProUGUI>();
            if (staticNextTurnButton != null)
            {
                nextTurnButton = staticNextTurnButton.GetComponent<Button>();
                if (nextTurnButton != null)
                    nextTurnButton.onClick.AddListener(() => battleManager?.EndPlayerTurn(TurnEndReason.ActionCompleted));
            }
            if (staticResetButton != null)
            {
                resetButton = staticResetButton.GetComponent<Button>();
                if (resetButton != null)
                    resetButton.onClick.AddListener(() => battleManager?.ResetBattle());
            }
            if (staticComboTestButton != null && enableComboTest)
            {
                comboTestButton = staticComboTestButton.GetComponent<Button>();
                if (comboTestButton != null)
                    comboTestButton.onClick.AddListener(TestComboProgress);
            }
            
            // パネル参照
            enemyInfoPanel = staticEnemyInfoPanel;
            battleFieldPanel = staticBattleFieldPanel;
            comboProgressPanel = staticComboProgressPanel;
            startScreenPanel = staticStartScreenPanel;
            
            // 子要素の参照を自動取得
            InitializeStaticChildReferences();
            
            // 初期状態の設定
            if (startScreenPanel != null)
                startScreenPanel.SetActive(true);
        }
        
        /// <summary>
        /// 静的UI要素の子コンポーネント参照を初期化
        /// </summary>
        private void InitializeStaticChildReferences()
        {
            // 敵情報パネルの子要素を取得
            if (enemyInfoPanel != null)
            {
                enemyInfoTitle = enemyInfoPanel.transform.Find("敵情報タイトル")?.GetComponent<TextMeshProUGUI>();
                for (int i = 0; i < enemyHpTexts.Length; i++)
                {
                    Transform child = enemyInfoPanel.transform.Find($"敵HP表示_{i}");
                    if (child != null)
                        enemyHpTexts[i] = child.GetComponent<TextMeshProUGUI>();
                }
            }
            
            // 戦場パネルの子要素を取得
            if (battleFieldPanel != null)
            {
                int enemyIndex = 0;
                for (int col = 0; col < 3; col++)
                {
                    for (int row = 0; row < 2; row++)
                    {
                        Transform gridCell = battleFieldPanel.transform.Find($"グリッド_{col}_{row}");
                        if (gridCell != null)
                        {
                            gridCells[col, row] = gridCell.gameObject;
                            
                            Transform enemyText = gridCell.Find($"敵表示_{col}_{row}");
                            if (enemyText != null && enemyIndex < enemyTexts.Length)
                                enemyTexts[enemyIndex] = enemyText.GetComponent<TextMeshProUGUI>();
                            enemyIndex++;
                        }
                    }
                }
            }
            
            // コンボ進行パネルの子要素を取得
            if (comboProgressPanel != null)
            {
                comboProgressTitle = comboProgressPanel.transform.Find("コンボ進行タイトル")?.GetComponent<TextMeshProUGUI>();
                for (int i = 0; i < comboProgressItems.Length; i++)
                {
                    Transform comboItem = comboProgressPanel.transform.Find($"コンボアイテム_{i}");
                    if (comboItem != null)
                    {
                        comboProgressItems[i] = comboItem.gameObject;
                        comboNameTexts[i] = comboItem.Find($"コンボ名_{i}")?.GetComponent<TextMeshProUGUI>();
                        comboStepTexts[i] = comboItem.Find($"ステップ表示_{i}")?.GetComponent<TextMeshProUGUI>();
                        comboTimerTexts[i] = comboItem.Find($"タイマー表示_{i}")?.GetComponent<TextMeshProUGUI>();
                        comboResistanceTexts[i] = comboItem.Find($"中断耐性表示_{i}")?.GetComponent<TextMeshProUGUI>();
                        
                        // 進行率バーの取得
                        Transform progressBG = comboItem.Find($"進行率バー背景_{i}");
                        if (progressBG != null)
                        {
                            Transform progressBar = progressBG.Find($"進行率バー_{i}");
                            if (progressBar != null)
                            {
                                // スライダーコンポーネントを作成（もし存在しない場合）
                                Slider slider = progressBG.GetComponent<Slider>();
                                if (slider == null)
                                {
                                    slider = progressBG.gameObject.AddComponent<Slider>();
                                    slider.minValue = 0f;
                                    slider.maxValue = 1f;
                                    slider.value = 0f;
                                    slider.interactable = false;
                                }
                                comboProgressBars[i] = slider;
                            }
                        }
                    }
                }
            }
            
            Debug.Log("静的UI参照の初期化完了");
        }
        
        private void CreateSimpleBattleUI()
        {
            Debug.Log("Creating Cyberpunk Battle UI (UI案対応版)...");
            
            // 画面サイズを取得してレスポンシブ対応
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float screenWidth = canvasRect.rect.width;
            float screenHeight = canvasRect.rect.height;
            
            Debug.Log($"Screen size: {screenWidth} x {screenHeight}");
            
            // スケールファクターを計算（1920x1080を基準とする）
            float scaleX = screenWidth / 1920f;
            float scaleY = screenHeight / 1080f;
            float scale = Mathf.Min(scaleX, scaleY);
            
            // サイバーパンク風背景パネル（画面全体）
            GameObject backgroundPanel = CreateCyberpunkPanel("Cyberpunk Background", Vector2.zero, 
                new Vector2(screenWidth, screenHeight), backgroundColor);
            
            // === UI案レイアウト実装 ===
            
            // 1. プレイヤーHP表示（右上）
            hpText = CreateCyberpunkText("Player HP", 
                new Vector2(screenWidth * 0.35f, screenHeight * 0.35f), 
                new Vector2(300 * scale, 80 * scale), "HP: 15000 / 15000", Mathf.RoundToInt(24 * scale));
            hpText.color = primaryGlowColor;
            
            // 2. 予告ダメージ表示（左側中央）
            pendingDamageText = CreateCyberpunkText("Pending Damage", 
                new Vector2(-screenWidth * 0.4f, 0f), 
                new Vector2(350 * scale, 150 * scale), "", Mathf.RoundToInt(18 * scale));
            pendingDamageText.color = warningColor;
            pendingDamageText.alignment = TextAlignmentOptions.TopLeft;
            
            // 3. ターン情報（右下）
            turnText = CreateCyberpunkText("Turn Info", 
                new Vector2(screenWidth * 0.35f, -screenHeight * 0.25f), 
                new Vector2(200 * scale, 60 * scale), "Turn: 1", Mathf.RoundToInt(20 * scale));
            turnText.color = secondaryGlowColor;
            
            // ゲーム状態表示（ターン情報の上）
            stateText = CreateCyberpunkText("Game State", 
                new Vector2(screenWidth * 0.35f, -screenHeight * 0.15f), 
                new Vector2(200 * scale, 50 * scale), "Player Turn", Mathf.RoundToInt(18 * scale));
            stateText.color = primaryGlowColor;

            // 次ターンボタン（右下下）
            nextTurnButton = CreateCyberpunkButton("次のターン", 
                new Vector2(screenWidth * 0.35f, -screenHeight * 0.35f), 
                new Vector2(140 * scale, 60 * scale), OnNextTurnClicked);
            
            // リセットボタン（右下最下）
            resetButton = CreateCyberpunkButton("戦闘リセット", 
                new Vector2(screenWidth * 0.35f, -screenHeight * 0.42f), 
                new Vector2(140 * scale, 50 * scale), OnResetClicked);
            
            // テスト用敵撃破ボタン（右下最下）
            Button killEnemyButton = CreateUIButton("敵を倒す", 
                new Vector2(screenWidth * 0.25f, -screenHeight * 0.3f), 
                new Vector2(140 * scale, 60 * scale), OnKillEnemyClicked);
            killEnemyButton.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 0.8f); // 赤色で目立たせる
            
            // コンボテストボタン（左下）
            if (enableComboTest)
            {
                comboTestButton = CreateUIButton("🎯 コンボテスト", 
                    new Vector2(-screenWidth * 0.25f, -screenHeight * 0.3f), 
                    new Vector2(160 * scale, 60 * scale), TestComboProgress);
                comboTestButton.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.8f, 0.8f); // 青色で目立たせる
            }
            
            // 4. バトルフィールド表示（中央3列）
            CreateCyberpunkBattleFieldDisplay(scale, screenWidth, screenHeight);
            
            // 5. コンボシステム表示（左下）
            CreateCyberpunkComboDisplay(scale, screenWidth, screenHeight);
            
            // 6. 手札システム表示（下部中央）
            CreateCyberpunkHandDisplay(scale, screenWidth, screenHeight);
            
            // コンボ効果ポップアップを作成（中央上部）
            CreateComboEffectDisplay(scale, screenWidth, screenHeight);
            
            // 手札システム用説明テキスト（下部中央）
            CreateUIText("Info", new Vector2(0, -screenHeight * 0.3f), 
                new Vector2(600 * scale, 100 * scale), 
                "手札システム戦闘UI\n画面下部の手札からカードを選択して攻撃\n次のターンでダメージ適用＆ターン終了 / リセットで戦闘リセット", 
                Mathf.RoundToInt(14 * scale));
            
            // テスト用装備入れ替えボタン
            CreateTestEquipmentButtons();
            
            Debug.Log("Simple Battle UI (Hand System Edition) created successfully!");
        }
        
        GameObject CreateUIPanel(string name, Vector2 position, Vector2 size, Color color)
        {
            return CreateUIPanel(name, position, size, color, null);
        }
        
        GameObject CreateUIPanel(string name, Vector2 position, Vector2 size, Color color, GameObject parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent != null ? parent.transform : canvas.transform, false);
            
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = panel.AddComponent<Image>();
            image.color = color;
            
            // 背景パネルはクリックを通すように設定
            if (name.Contains("Background") || name.Contains("背景"))
            {
                image.raycastTarget = false;
                Debug.Log($"✅ Background panel raycastTarget disabled: {name}");
            }
            
            return panel;
        }
        
        TextMeshProUGUI CreateUIText(string name, Vector2 position, Vector2 size, string text, int fontSize)
        {
            return CreateUIText(name, position, size, text, fontSize, null);
        }
        
        TextMeshProUGUI CreateUIText(string name, Vector2 position, Vector2 size, string text, int fontSize, GameObject parent)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent != null ? parent.transform : canvas.transform, false);
            
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
        
        Button CreateUIButton(string name, Vector2 position, Vector2 size, System.Action onClick)
        {
            return CreateUIButton(name, position, size, onClick, null);
        }
        
        Button CreateUIButton(string name, Vector2 position, Vector2 size, System.Action onClick, GameObject parent)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent != null ? parent.transform : canvas.transform, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.6f, 0.8f);
            image.raycastTarget = true; // クリック検知を有効化
            
            Button button = buttonObj.AddComponent<Button>();
            button.interactable = true; // ボタンをインタラクティブに設定
            button.onClick.AddListener(() => {
                Debug.Log($"Button clicked: {name}");
                onClick?.Invoke();
            });
            
            // ボタンテキスト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = name;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.raycastTarget = false; // テキストはクリックをブロックしない
            
            // 日本語フォントを適用
            if (japaneseFont != null)
            {
                textComponent.font = japaneseFont;
            }
            
            Debug.Log($"Button created: {name}, Interactive: {button.interactable}, RaycastTarget: {image.raycastTarget}");
            
            return button;
        }
        
        // === サイバーパンクUIコンポーネント作成メソッド ===
        
        /// <summary>
        /// サイバーパンク風パネルを作成
        /// </summary>
        GameObject CreateCyberpunkPanel(string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = CreateUIPanel(name, position, size, color);
            
            // グロー効果を追加（Outlineコンポーネントを使用）
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = primaryGlowColor;
            outline.effectDistance = new Vector2(2f, 2f);
            
            return panel;
        }
        
        /// <summary>
        /// サイバーパンク風テキストを作成
        /// </summary>
        TextMeshProUGUI CreateCyberpunkText(string name, Vector2 position, Vector2 size, string text, int fontSize)
        {
            TextMeshProUGUI textComponent = CreateUIText(name, position, size, text, fontSize);
            
            // グロー効果を追加
            Outline outline = textComponent.gameObject.AddComponent<Outline>();
            outline.effectColor = primaryGlowColor;
            outline.effectDistance = new Vector2(1f, 1f);
            
            // シャドウ効果を追加
            Shadow shadow = textComponent.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);
            
            return textComponent;
        }
        
        /// <summary>
        /// サイバーパンク風ボタンを作成
        /// </summary>
        Button CreateCyberpunkButton(string name, Vector2 position, Vector2 size, System.Action onClick)
        {
            Button button = CreateUIButton(name, position, size, onClick);
            
            // ボタンの背景色をサイバーパンク風に変更
            Image buttonImage = button.GetComponent<Image>();
            buttonImage.color = new Color(0.1f, 0.3f, 0.5f, 0.8f);
            
            // グロー効果を追加
            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = secondaryGlowColor;
            outline.effectDistance = new Vector2(2f, 2f);
            
            // ホバー時のアニメーション設定
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.1f, 0.3f, 0.5f, 0.8f);
            colors.highlightedColor = new Color(0.2f, 0.5f, 0.8f, 1f);
            colors.pressedColor = new Color(0.05f, 0.2f, 0.4f, 0.9f);
            colors.selectedColor = new Color(0.15f, 0.4f, 0.7f, 0.9f);
            button.colors = colors;
            
            return button;
        }
        
        /// <summary>
        /// UIスライダーを作成
        /// </summary>
        GameObject CreateUISlider(string name, Vector2 position, Vector2 size)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            // スライダーコンポーネント
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false; // 表示用のみ
            
            // 背景イメージ
            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObj.transform, false);
            
            RectTransform bgRect = background.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // フィルエリアイメージ
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.8f, 0.3f, 0.8f); // 緑系のフィル色
            
            // スライダーの設定
            slider.fillRect = fillRect;
            
            return sliderObj;
        }
        
        // === サイバーパンクUIレイアウトメソッド ===
        
        /// <summary>
        /// サイバーパンク風バトルフィールドを作成（中央3列）
        /// </summary>
        private void CreateCyberpunkBattleFieldDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating Cyberpunk BattleField Display...");
            
            // バトルフィールドパネル（中央）
            battleFieldPanel = CreateCyberpunkPanel("バトルフィールド", 
                new Vector2(0f, 0f),
                new Vector2(600 * scale, 300 * scale), 
                new Color(0.1f, 0.15f, 0.3f, 0.9f));
            
            // 3列×2行のグリッドセルを作成
            float cellWidth = 180 * scale;
            float cellHeight = 120 * scale;
            float startX = -cellWidth;
            float startY = cellHeight * 0.5f;
            
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 2; row++)
                {
                    float posX = startX + col * cellWidth;
                    float posY = startY - row * cellHeight;
                    
                    gridCells[col, row] = CreateCyberpunkPanel($"グリッド_{col}_{row}", 
                        new Vector2(posX, posY), 
                        new Vector2(cellWidth - 10, cellHeight - 10),
                        new Color(0.2f, 0.3f, 0.5f, 0.4f));
                    
                    // グリッドラベルを追加
                    CreateCyberpunkText($"グリッドラベル_{col}_{row}",
                        new Vector2(posX, posY + cellHeight * 0.3f),
                        new Vector2(cellWidth - 20, 30 * scale),
                        $"{col + 1}-{row + 1}", Mathf.RoundToInt(14 * scale));
                    
                    gridCells[col, row].transform.SetParent(battleFieldPanel.transform, false);
                }
            }
        }
        
        /// <summary>
        /// サイバーパンク風コンボシステムを作成（左下）
        /// </summary>
        private void CreateCyberpunkComboDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating Cyberpunk Combo Display...");
            
            // コンボパネル（左下）
            comboProgressPanel = CreateCyberpunkPanel("コンボシステム", 
                new Vector2(-screenWidth * 0.35f, -screenHeight * 0.25f),
                new Vector2(300 * scale, 200 * scale), 
                new Color(0.3f, 0.1f, 0.3f, 0.9f));
            
            // コンボタイトル
            comboProgressTitle = CreateCyberpunkText("コンボタイトル",
                new Vector2(-screenWidth * 0.35f, -screenHeight * 0.15f),
                new Vector2(280 * scale, 30 * scale),
                "コンボシステム", Mathf.RoundToInt(16 * scale));
            comboProgressTitle.color = secondaryGlowColor;
            
            // コンボアイテムを作成
            for (int i = 0; i < 3; i++) // 最大3つのアクティブコンボを表示
            {
                float yOffset = -screenHeight * 0.18f - i * 25 * scale;
                
                comboProgressItems[i] = CreateCyberpunkPanel($"コンボアイテム_{i}", 
                    new Vector2(-screenWidth * 0.35f, yOffset), 
                    new Vector2(280 * scale, 20 * scale),
                    new Color(0.4f, 0.2f, 0.4f, 0.7f));
                
                comboNameTexts[i] = CreateCyberpunkText($"コンボ名_{i}",
                    new Vector2(-screenWidth * 0.42f, yOffset),
                    new Vector2(100 * scale, 18 * scale),
                    "", Mathf.RoundToInt(12 * scale));
                
                comboStepTexts[i] = CreateCyberpunkText($"コンボステップ_{i}",
                    new Vector2(-screenWidth * 0.28f, yOffset),
                    new Vector2(80 * scale, 18 * scale),
                    "", Mathf.RoundToInt(10 * scale));
            }
        }
        
        /// <summary>
        /// サイバーパンク風手札システムを作成（下部中央）
        /// </summary>
        private void CreateCyberpunkHandDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating Cyberpunk Hand Display...");
            
            // 手札エリア（下部中央）
            GameObject handPanel = CreateCyberpunkPanel("手札システム", 
                new Vector2(0f, -screenHeight * 0.35f),
                new Vector2(800 * scale, 120 * scale), 
                new Color(0.1f, 0.3f, 0.1f, 0.9f));
            
            // 手札タイトル
            CreateCyberpunkText("手札タイトル",
                new Vector2(0f, -screenHeight * 0.28f),
                new Vector2(200 * scale, 30 * scale),
                "手札システム", Mathf.RoundToInt(16 * scale));
                
            // カードスロットのプレースホルダー
            for (int i = 0; i < 7; i++)
            {
                float cardX = -300 * scale + i * 100 * scale;
                CreateCyberpunkPanel($"カードスロット_{i}",
                    new Vector2(cardX, -screenHeight * 0.38f),
                    new Vector2(80 * scale, 60 * scale),
                    new Color(0.2f, 0.4f, 0.2f, 0.6f));
            }
        }
        
        private void SetupBattleManager()
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                Debug.Log("BattleManager not found, creating new one...");
                GameObject bmObj = new GameObject("BattleManager");
                battleManager = bmObj.AddComponent<BattleManager>();
            }
            else
            {
                Debug.Log("BattleManager found and connected");
            }
            
            // システム参照の初期化
            InitializeSystemReferences();
            
            // BattleManagerのイベントを購読
            battleManager.OnPlayerDataChanged += OnPlayerDataChanged;
            
            // HandSystemを取得してイベントを購読
            SetupHandSystemConnection();
            
            // ComboSystemを取得してイベントを購読
            SetupComboSystemConnection();
            
            // AttachmentSystemを取得してイベントを購読
            SetupAttachmentSystemConnection();
            
            // コンボ表示エリアを初期化
            InitializeComboDisplayArea();
            
            // 確実に動作するようダミーデータを作成
            CreateTestDatabasesForBattleManager();
        }
        
        /// <summary>
        /// HandSystemとの接続を設定
        /// </summary>
        private void SetupHandSystemConnection()
        {
            handSystem = FindObjectOfType<HandSystem>();
            if (handSystem == null)
            {
                Debug.LogWarning("HandSystem not found! Attempting to create one...");
                
                // HandSystemを自動作成
                var battleManager = FindObjectOfType<BattleManager>();
                if (battleManager != null)
                {
                    handSystem = battleManager.gameObject.AddComponent<HandSystem>();
                    Debug.Log("✓ HandSystem created and connected for damage preview");
                }
                else
                {
                    Debug.LogWarning("❌ BattleManager not found! 予告ダメージ表示が動作しません。");
                    return;
                }
            }
            
            // HandSystemのイベントを購読（正しいイベント名を使用）
            handSystem.OnPendingDamageCalculated += OnPendingDamageCalculated;
            handSystem.OnPendingDamageApplied += OnPendingDamageApplied;
            handSystem.OnPendingDamageCleared += OnPendingDamageCleared;
            handSystem.OnEnemyDataChanged += OnEnemyDataChanged;
            handSystem.OnBattleFieldChanged += OnBattleFieldChanged;
            
            Debug.Log("HandSystem found and events subscribed for pending damage display");
        }
        
        /// <summary>
        /// ComboSystemとの接続を設定
        /// </summary>
        private void SetupComboSystemConnection()
        {
            comboSystem = FindObjectOfType<ComboSystem>();
            if (comboSystem == null)
            {
                Debug.Log("ComboSystem not found, creating one on BattleManager...");
                if (battleManager != null)
                {
                    comboSystem = battleManager.gameObject.AddComponent<ComboSystem>();
                    Debug.Log("ComboSystem created and attached to BattleManager");
                }
                else
                {
                    Debug.LogWarning("Cannot create ComboSystem: BattleManager is null");
                    return;
                }
            }
            else
            {
                Debug.Log("ComboSystem found");
            }
            
            // ComboSystemのイベントを購読
            comboSystem.OnComboStarted += OnComboStarted;
            comboSystem.OnComboProgressUpdated += OnComboProgressUpdated;
            comboSystem.OnComboCompleted += OnComboCompleted;
            comboSystem.OnComboFailed += OnComboFailed;
            comboSystem.OnComboInterrupted += OnComboInterrupted;
            
            Debug.Log("ComboSystem events subscribed for combo UI display");
        }
        
        private void CreateTestDatabasesForBattleManager()
        {
            Debug.Log("Creating test databases for BattleManager...");
            
            // テスト用武器データベースを作成
            WeaponDatabase weaponDB = ScriptableObject.CreateInstance<WeaponDatabase>();
            weaponDB.hideFlags = HideFlags.DontSaveInEditor;
            WeaponData[] testWeapons = new WeaponData[4]
            {
                new WeaponData("炎の剣", AttackAttribute.Fire, WeaponType.Sword, 120, AttackRange.SingleFront)
                {
                    criticalRate = 15,
                    cooldownTurns = 0,
                    specialEffect = "炎上効果"
                },
                new WeaponData("氷の斧", AttackAttribute.Ice, WeaponType.Axe, 95, AttackRange.SingleFront)
                {
                    criticalRate = 25,
                    cooldownTurns = 1,
                    specialEffect = "凍結効果"
                },
                new WeaponData("雷槍", AttackAttribute.Thunder, WeaponType.Spear, 110, AttackRange.Column)
                {
                    criticalRate = 20,
                    cooldownTurns = 0,
                    specialEffect = "麻痺効果"
                },
                new WeaponData("大剣", AttackAttribute.None, WeaponType.Sword, 140, AttackRange.SingleFront)
                {
                    criticalRate = 10,
                    cooldownTurns = 2,
                    specialEffect = "高威力攻撃"
                }
            };
            
            // Reflectionでprivateフィールドを設定
            var weaponsField = typeof(WeaponDatabase).GetField("weapons", 
                NonPublic | Instance);
            weaponsField?.SetValue(weaponDB, testWeapons);
            
            // テスト用敵データベースを作成
            EnemyDatabase enemyDB = ScriptableObject.CreateInstance<EnemyDatabase>();
            enemyDB.hideFlags = HideFlags.DontSaveInEditor;
            EnemyData[] testEnemies = new EnemyData[2]
            {
                new EnemyData
                {
                    enemyName = "機械兵士",
                    enemyId = 1,
                    category = EnemyCategory.Attacker,
                    baseHp = 5000,
                    attackPower = 1500,
                    primaryAction = EnemyActionType.Attack,
                    canBeSummoned = true
                },
                new EnemyData
                {
                    enemyName = "機械警備",
                    enemyId = 2,
                    category = EnemyCategory.Vanguard,
                    baseHp = 8000,
                    attackPower = 1200,
                    primaryAction = EnemyActionType.DefendAlly,
                    canBeSummoned = true
                }
            };
            
            var enemiesField = typeof(EnemyDatabase).GetField("enemies", 
                NonPublic | Instance);
            enemiesField?.SetValue(enemyDB, testEnemies);
            
            // BattleManagerにデータベースを設定
            var weaponDBField = typeof(BattleManager).GetField("weaponDatabase", 
                NonPublic | Instance);
            weaponDBField?.SetValue(battleManager, weaponDB);
            
            var enemyDBField = typeof(BattleManager).GetField("enemyDatabase", 
                NonPublic | Instance);
            enemyDBField?.SetValue(battleManager, enemyDB);
            
            Debug.Log("Test databases created and assigned to BattleManager!");
            
            // ComboSystemにテスト用ComboDatabaseを設定
            CreateTestComboDatabase();
            
            // 【重要】プレイヤーに武器を実際に装備させる
            EquipWeaponsToPlayer(testWeapons);
            
            // テスト用の敵を戦場に配置
            CreateTestEnemiesOnBattleField();
        }
        
        /// <summary>
        /// プレイヤーに武器を実際に装備させる（重要！）
        /// </summary>
        private void EquipWeaponsToPlayer(WeaponData[] weapons)
        {
            if (battleManager?.PlayerData == null)
            {
                Debug.LogError("PlayerData is null! Cannot equip weapons");
                return;
            }
            
            Debug.Log("Equipping weapons to player...");
            
            // プレイヤーの装備武器配列が初期化されていない場合は初期化
            if (battleManager.PlayerData.equippedWeapons == null)
            {
                battleManager.PlayerData.equippedWeapons = new WeaponData[4];
            }
            
            if (battleManager.PlayerData.weaponCooldowns == null)
            {
                battleManager.PlayerData.weaponCooldowns = new int[4];
            }
            
            // 武器を装備
            for (int i = 0; i < 4 && i < weapons.Length; i++)
            {
                battleManager.PlayerData.equippedWeapons[i] = weapons[i];
                battleManager.PlayerData.weaponCooldowns[i] = 0;
                Debug.Log($"Equipped weapon {i + 1}: {weapons[i].weaponName}");
            }
            
            Debug.Log("Player weapons equipped successfully!");
        }
        
        /// <summary>
        /// テスト用の敵を戦場に配置
        /// </summary>
        private void CreateTestEnemiesOnBattleField()
        {
            if (battleManager?.BattleField == null)
            {
                Debug.LogWarning("BattleField is null, cannot place test enemies");
                return;
            }
            
            Debug.Log("Placing test enemies on battlefield...");
            
            // テスト用の敵データを取得
            var enemyDBField = typeof(BattleManager).GetField("enemyDatabase", 
                NonPublic | Instance);
            EnemyDatabase enemyDB = enemyDBField?.GetValue(battleManager) as EnemyDatabase;
            
            if (enemyDB?.Enemies == null || enemyDB.Enemies.Length == 0)
            {
                Debug.LogWarning("No enemy data available, creating default enemies...");
                
                // EnemyDatabaseが存在しない場合は自動作成
                if (enemyDB == null)
                {
                    enemyDB = CreateDefaultEnemyDatabase();
                    SetEnemyDatabaseToBattleManager(enemyDB);
                    Debug.Log("Created default EnemyDatabase");
                }
                else if (enemyDB.Enemies == null || enemyDB.Enemies.Length == 0)
                {
                    // EnemyDatabaseは存在するが敵データがない場合
                    Debug.LogWarning("EnemyDatabase exists but has no enemy data, adding default enemies");
                    AddDefaultEnemiesToDatabase(enemyDB);
                }
            }
            
            // 列に敵を配置（テスト用パターン - 最低3体配置）
            Debug.Log("最低3体の敵を配置中...");
            
            // 列0: 機械兵士を前列に配置
            if (enemyDB.Enemies.Length > 0)
            {
                EnemyData soldier = enemyDB.Enemies[0]; // 機械兵士
                EnemyInstance soldierInstance = new EnemyInstance(soldier, 0, 0);
                battleManager.BattleField.PlaceEnemy(soldierInstance, new GridPosition(0, 0));
                Debug.Log($"✅ Placed {soldier.enemyName} at (0, 0)");
            }
            
            // 列1: 機械警備を後列に配置
            if (enemyDB.Enemies.Length > 1)
            {
                EnemyData guard = enemyDB.Enemies[1]; // 機械警備
                EnemyInstance guardInstance = new EnemyInstance(guard, 1, 1);
                battleManager.BattleField.PlaceEnemy(guardInstance, new GridPosition(1, 1));
                Debug.Log($"✅ Placed {guard.enemyName} at (1, 1)");
            }
            
            // 列2: 3体目の敵を配置（前列）
            if (enemyDB.Enemies.Length > 0)
            {
                EnemyData thirdEnemy = enemyDB.Enemies[0]; // 機械兵士を再利用
                EnemyInstance thirdInstance = new EnemyInstance(thirdEnemy, 2, 0);
                battleManager.BattleField.PlaceEnemy(thirdInstance, new GridPosition(2, 0));
                Debug.Log($"✅ Placed {thirdEnemy.enemyName} at (2, 0) - 3rd enemy");
            }
            
            Debug.Log("✅ テスト用敵の配置完了! (3体配置済み)");
            
            // UI表示を更新
            UpdateEnemyDisplay();
            UpdateBattleFieldDisplay();
            
            Debug.Log("UI表示更新完了");
        }
        
        /// <summary>
        /// デフォルトのEnemyDatabaseを作成
        /// </summary>
        EnemyDatabase CreateDefaultEnemyDatabase()
        {
            EnemyDatabase enemyDB = ScriptableObject.CreateInstance<EnemyDatabase>();
            enemyDB.hideFlags = HideFlags.DontSaveInEditor;
            
            // デフォルト敵データを作成
            var defaultEnemies = new List<EnemyData>
            {
                // 機械兵士
                new EnemyData
                {
                    enemyName = "機械兵士",
                    category = EnemyCategory.Attacker,
                    enemyId = 1,
                    baseHp = 3000,
                    attackPower = 1200,
                    defense = 100,
                    actionPriority = 2,
                    primaryAction = EnemyActionType.Attack,
                    secondaryAction = EnemyActionType.NoAction,
                    specialAbility = "突撃攻撃",
                    abilityValue = 150,
                    canBeSummoned = true,
                    summonWeight = 100
                },
                
                // 機械警備
                new EnemyData
                {
                    enemyName = "機械警備",
                    category = EnemyCategory.Vanguard,
                    enemyId = 2,
                    baseHp = 4500,
                    attackPower = 800,
                    defense = 300,
                    actionPriority = 1,
                    primaryAction = EnemyActionType.DefendAlly,
                    secondaryAction = EnemyActionType.Attack,
                    specialAbility = "防御強化",
                    abilityValue = 200,
                    canBeSummoned = true,
                    summonWeight = 80
                },
                
                // 機械砲撃手
                new EnemyData
                {
                    enemyName = "機械砲撃手",
                    category = EnemyCategory.Attacker,
                    enemyId = 3,
                    baseHp = 2500,
                    attackPower = 1800,
                    defense = 50,
                    actionPriority = 3,
                    primaryAction = EnemyActionType.Attack,
                    secondaryAction = EnemyActionType.NoAction,
                    specialAbility = "遠距離砲撃",
                    abilityValue = 300,
                    canBeSummoned = true,
                    summonWeight = 90
                }
            };
            
            // リフレクションでenemiesフィールドにアクセス
            var enemiesField = typeof(EnemyDatabase).GetField("enemies", 
                NonPublic | Instance);
            enemiesField?.SetValue(enemyDB, defaultEnemies.ToArray());
            
            Debug.Log($"Created default EnemyDatabase with {defaultEnemies.Count} enemies");
            return enemyDB;
        }
        
        /// <summary>
        /// BattleManagerにEnemyDatabaseを設定
        /// </summary>
        private void SetEnemyDatabaseToBattleManager(EnemyDatabase enemyDB)
        {
            var enemyDBField = typeof(BattleManager).GetField("enemyDatabase", 
                NonPublic | Instance);
            enemyDBField?.SetValue(battleManager, enemyDB);
            Debug.Log("EnemyDatabase set to BattleManager");
        }
        
        /// <summary>
        /// 既存のEnemyDatabaseにデフォルト敵を追加
        /// </summary>
        private void AddDefaultEnemiesToDatabase(EnemyDatabase enemyDB)
        {
            var defaultEnemies = new EnemyData[]
            {
                new EnemyData
                {
                    enemyName = "機械兵士",
                    category = EnemyCategory.Attacker,
                    enemyId = 1,
                    baseHp = 3000,
                    attackPower = 1200,
                    defense = 100
                },
                new EnemyData
                {
                    enemyName = "機械警備",
                    category = EnemyCategory.Vanguard,
                    enemyId = 2,
                    baseHp = 4500,
                    attackPower = 800,
                    defense = 300
                },
                new EnemyData
                {
                    enemyName = "機械砲撃手",
                    category = EnemyCategory.Attacker,
                    enemyId = 3,
                    baseHp = 2500,
                    attackPower = 1800,
                    defense = 50
                }
            };
            
            // リフレクションで敵データを設定
            var enemiesField = typeof(EnemyDatabase).GetField("enemies", 
                NonPublic | Instance);
            enemiesField?.SetValue(enemyDB, defaultEnemies);
            
            Debug.Log($"Added {defaultEnemies.Length} default enemies to existing EnemyDatabase");
        }
        
        /// <summary>
        /// 敵情報表示を更新
        /// </summary>
        private void UpdateEnemyDisplay()
        {
            Debug.Log("Updating enemy display...");
            
            if (battleManager?.BattleField == null)
            {
                Debug.LogWarning("BattleField is null, cannot update enemy display");
                return;
            }
            
            // BattleFieldから全敵を取得
            var allEnemies = battleManager.BattleField.GetAllEnemies();
            Debug.Log($"Found {allEnemies.Count} enemies on battlefield");
            
            // 敵情報タイトルを更新
            if (enemyInfoTitle != null)
            {
                enemyInfoTitle.text = $"敵情報 ({allEnemies.Count}体)";
                Debug.Log($"Updated enemy info title: {allEnemies.Count} enemies");
            }
            
            // 敵HP表示を更新
            for (int i = 0; i < enemyHpTexts.Length; i++)
            {
                if (enemyHpTexts[i] != null)
                {
                    if (i < allEnemies.Count && allEnemies[i] != null)
                    {
                        var enemy = allEnemies[i];
                        enemyHpTexts[i].text = $"{enemy.enemyData.enemyName}\nHP: {enemy.currentHp}/{enemy.enemyData.baseHp}";
                        enemyHpTexts[i].color = enemy.IsAlive() ? Color.white : Color.gray;
                        Debug.Log($"Updated enemy {i}: {enemy.enemyData.enemyName} HP: {enemy.currentHp}");
                    }
                    else
                    {
                        enemyHpTexts[i].text = "";
                    }
                }
            }
        }
        
        /// <summary>
        /// 戦場グリッド表示を更新
        /// </summary>
        private void UpdateBattleFieldDisplay()
        {
            Debug.Log("Updating battlefield display...");
            
            if (battleManager?.BattleField == null)
            {
                Debug.LogWarning("BattleField is null, cannot update battlefield display");
                return;
            }
            
            // 全グリッド位置をクリア
            for (int i = 0; i < enemyTexts.Length; i++)
            {
                if (enemyTexts[i] != null)
                {
                    enemyTexts[i].text = "";
                }
            }
            
            // BattleFieldから敵位置情報を取得して表示
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 2; row++)
                {
                    var position = new GridPosition(col, row);
                    var enemy = battleManager.BattleField.GetEnemyAt(position);
                    
                    int enemyIndex = col * 2 + row;
                    if (enemyIndex < enemyTexts.Length && enemyTexts[enemyIndex] != null)
                    {
                        if (enemy != null && enemy.IsAlive())
                        {
                            enemyTexts[enemyIndex].text = $"{enemy.enemyData.enemyName}\n{enemy.currentHp}HP";
                            enemyTexts[enemyIndex].color = Color.red;
                            Debug.Log($"Updated grid ({col},{row}): {enemy.enemyData.enemyName}");
                        }
                        else
                        {
                            enemyTexts[enemyIndex].text = "";
                        }
                    }
                }
            }
            
            Debug.Log("Battlefield display update completed");
        }
        
        /// <summary>
        /// 定期的に敵情報表示を更新するコルーチン
        /// </summary>
        System.Collections.IEnumerator UpdateEnemyDisplayPeriodically()
        {
            while (isBattleStarted)
            {
                yield return new WaitForSeconds(1.0f); // 1秒ごとに更新
                
                if (battleManager != null && battleManager.BattleField != null)
                {
                    UpdateEnemyDisplay();
                    UpdateBattleFieldDisplay();
                }
            }
        }
        
        /// <summary>
        /// 戦場表示の作成（手札システム対応版 - 列選択機能削除）
        /// </summary>
        private void CreateBattleFieldDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating BattleField Display (Hand System Edition)...");
            
            // 戦場パネルの作成（中央左寄り）
            battleFieldPanel = CreateUIPanel("戦場パネル", 
                new Vector2(-screenWidth * 0.15f, -screenHeight * 0.05f),
                new Vector2(400 * scale, 280 * scale), 
                new Color(0.1f, 0.1f, 0.2f, 0.8f), battleUIGroup);
            
            // グリッドセルの作成（3列×2行）
            float gridStartX = -screenWidth * 0.15f;
            float gridStartY = -screenHeight * 0.05f;
            float cellWidth = 120 * scale;
            float cellHeight = 80 * scale;
            float cellSpacing = 10 * scale;
            
            // グリッドセルと敵表示を作成（列選択ボタンは削除）
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 2; row++)
                {
                    float posX = gridStartX + (col - 1) * (cellWidth + cellSpacing);
                    float posY = gridStartY + (row - 0.5f) * (cellHeight + cellSpacing);
                    
                    // グリッドセルの作成
                    gridCells[col, row] = CreateUIPanel($"グリッド_{col}_{row}", 
                        new Vector2(posX, posY), 
                        new Vector2(cellWidth, cellHeight),
                        new Color(0.3f, 0.3f, 0.4f, 0.6f));
                    
                    // グリッド枠線の追加
                    Image cellImage = gridCells[col, row].GetComponent<Image>();
                    if (cellImage != null)
                    {
                        cellImage.color = new Color(0.2f, 0.3f, 0.4f, 0.7f);
                        // シンプルな枠線表現（Outlineコンポーネントを使用）
                        Outline outline = gridCells[col, row].AddComponent<Outline>();
                        outline.effectColor = Color.white;
                        outline.effectDistance = new Vector2(2, 2);
                    }
                    
                    // 敵表示用テキストの作成
                    int enemyIndex = col * 2 + row;
                    enemyTexts[enemyIndex] = CreateUIText($"敵表示_{col}_{row}", 
                        new Vector2(posX, posY), 
                        new Vector2(cellWidth - 10 * scale, cellHeight - 10 * scale), 
                        "", Mathf.RoundToInt(12 * scale));
                    enemyTexts[enemyIndex].color = Color.red;
                    enemyTexts[enemyIndex].alignment = TextAlignmentOptions.Center;
                }
            }
            
            Debug.Log("戦場表示作成完了（手札システム対応）!");
        }
        
        /// <summary>
        /// 敵情報表示の作成（右上エリア）
        /// </summary>
        private void CreateEnemyInfoDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating Enemy Info Display...");
            
            // 敵情報パネルの作成（右上）
            float panelWidth = 250 * scale;
            float panelHeight = 200 * scale;
            enemyInfoPanel = CreateUIPanel("敵情報パネル", 
                new Vector2(screenWidth * 0.25f, screenHeight * 0.15f),
                new Vector2(panelWidth, panelHeight), 
                new Color(0.2f, 0.1f, 0.1f, 0.8f));
            
            // 敵情報タイトル
            enemyInfoTitle = CreateUIText("敵情報タイトル", 
                new Vector2(screenWidth * 0.25f, screenHeight * 0.25f), 
                new Vector2(panelWidth - 20 * scale, 30 * scale), 
                "=== 敵情報 ===", 
                Mathf.RoundToInt(16 * scale));
            enemyInfoTitle.color = Color.yellow;
            
            // 敵HP表示テキストを作成（最大6体分）
            for (int i = 0; i < enemyHpTexts.Length; i++)
            {
                float yOffset = screenHeight * 0.2f - (i * 25 * scale);
                
                enemyHpTexts[i] = CreateUIText($"敵HP表示_{i}", 
                    new Vector2(screenWidth * 0.25f, yOffset), 
                    new Vector2(panelWidth - 20 * scale, 20 * scale), 
                    "", 
                    Mathf.RoundToInt(12 * scale));
                enemyHpTexts[i].color = Color.white;
                enemyHpTexts[i].alignment = TextAlignmentOptions.TopLeft;
            }
            
            Debug.Log("敵情報表示作成完了!");
        }
        
        /// <summary>
        /// コンボ進行状況表示の作成（左下エリア）
        /// </summary>
        private void CreateComboProgressDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating Combo Progress Display...");
            
            // コンボ進行状況パネルの作成（左下）
            float panelWidth = 280 * scale;
            float panelHeight = 300 * scale;
            comboProgressPanel = CreateUIPanel("コンボ進行パネル", 
                new Vector2(-screenWidth * 0.32f, -screenHeight * 0.35f),
                new Vector2(panelWidth, panelHeight), 
                new Color(0.1f, 0.1f, 0.3f, 0.8f));
            
            // コンボ進行状況タイトル
            comboProgressTitle = CreateUIText("コンボ進行タイトル", 
                new Vector2(-screenWidth * 0.32f, -screenHeight * 0.18f), 
                new Vector2(panelWidth - 20 * scale, 30 * scale), 
                "=== コンボ進行 ===", 
                Mathf.RoundToInt(16 * scale));
            comboProgressTitle.color = Color.cyan;
            
            // コンボグループコンテナの初期化
            InitializeComboGroupContainer(scale, screenWidth, screenHeight);
            
            // 各コンボ進行アイテムを作成（最大5つ）
            for (int i = 0; i < 5; i++)
            {
                float yOffset = -screenHeight * 0.24f - (i * 50 * scale);
                
                // コンボアイテムパネル
                comboProgressItems[i] = CreateUIPanel($"コンボアイテム_{i}", 
                    new Vector2(-screenWidth * 0.32f, yOffset), 
                    new Vector2(panelWidth - 20 * scale, 45 * scale),
                    new Color(0.2f, 0.2f, 0.4f, 0.7f));
                
                // コンボ名
                comboNameTexts[i] = CreateUIText($"コンボ名_{i}", 
                    new Vector2(-screenWidth * 0.41f, yOffset + 15 * scale), 
                    new Vector2(120 * scale, 15 * scale), 
                    "", 
                    Mathf.RoundToInt(11 * scale));
                comboNameTexts[i].alignment = TextAlignmentOptions.TopLeft;
                comboNameTexts[i].color = Color.white;
                
                // 進行率バー（スライダー）
                GameObject sliderObj = CreateUISlider($"進行率バー_{i}", 
                    new Vector2(-screenWidth * 0.41f, yOffset), 
                    new Vector2(90 * scale, 15 * scale));
                comboProgressBars[i] = sliderObj.GetComponent<Slider>();
                
                // ステップ表示（現在/必要）
                comboStepTexts[i] = CreateUIText($"ステップ表示_{i}", 
                    new Vector2(-screenWidth * 0.18f, yOffset + 15 * scale), 
                    new Vector2(35 * scale, 15 * scale), 
                    "", 
                    Mathf.RoundToInt(10 * scale));
                comboStepTexts[i].alignment = TextAlignmentOptions.TopRight;
                comboStepTexts[i].color = Color.yellow;
                
                // タイマー表示（残りターン）
                comboTimerTexts[i] = CreateUIText($"タイマー表示_{i}", 
                    new Vector2(-screenWidth * 0.18f, yOffset), 
                    new Vector2(35 * scale, 15 * scale), 
                    "", 
                    Mathf.RoundToInt(9 * scale));
                comboTimerTexts[i].alignment = TextAlignmentOptions.TopRight;
                comboTimerTexts[i].color = Color.orange;
                
                // 中断耐性表示
                comboResistanceTexts[i] = CreateUIText($"中断耐性表示_{i}", 
                    new Vector2(-screenWidth * 0.18f, yOffset - 10 * scale), 
                    new Vector2(35 * scale, 10 * scale), 
                    "", 
                    Mathf.RoundToInt(8 * scale));
                comboResistanceTexts[i].alignment = TextAlignmentOptions.TopRight;
                comboResistanceTexts[i].color = Color.green;
                
                // 初期状態では非表示
                comboProgressItems[i].SetActive(false);
            }
            
            Debug.Log("コンボ進行状況表示作成完了!");
        }
        
        /// <summary>
        /// コンボ効果ポップアップ表示の作成（中央上部エリア）
        /// </summary>
        private void CreateComboEffectDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating Combo Effect Display...");
            
            // コンボ効果ポップアップパネルの作成（中央上部）
            float panelWidth = 400 * scale;
            float panelHeight = 120 * scale;
            comboEffectPopup = CreateUIPanel("コンボ効果ポップアップ", 
                new Vector2(0, screenHeight * 0.2f),
                new Vector2(panelWidth, panelHeight), 
                new Color(0.9f, 0.8f, 0.1f, 0.9f)); // 金色系の目立つ色
            
            // コンボ効果タイトル（コンボ名）
            comboEffectTitle = CreateUIText("コンボ効果タイトル", 
                new Vector2(0, screenHeight * 0.22f), 
                new Vector2(panelWidth - 20 * scale, 30 * scale), 
                "", 
                Mathf.RoundToInt(20 * scale));
            comboEffectTitle.color = Color.red;
            comboEffectTitle.fontStyle = FontStyles.Bold;
            
            // コンボ効果説明
            comboEffectDescription = CreateUIText("コンボ効果説明", 
                new Vector2(0, screenHeight * 0.18f), 
                new Vector2(panelWidth - 20 * scale, 50 * scale), 
                "", 
                Mathf.RoundToInt(14 * scale));
            comboEffectDescription.color = Color.black;
            comboEffectDescription.alignment = TextAlignmentOptions.Center;
            
            // 初期状態では非表示
            comboEffectPopup.SetActive(false);
            
            Debug.Log("コンボ効果ポップアップ表示作成完了!");
        }
        
        /// <summary>
        /// テスト用装備入れ替えボタンを作成
        /// </summary>
        private void CreateTestEquipmentButtons()
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float screenWidth = canvasRect.rect.width;
            float screenHeight = canvasRect.rect.height;
            float scale = Mathf.Min(screenWidth / 1920f, screenHeight / 1080f);
            
            // テスト用ボタンパネル作成（右上）
            GameObject testPanel = CreateUIPanel("TestEquipmentPanel", 
                new Vector2(screenWidth * 0.35f, screenHeight * 0.4f), 
                new Vector2(220 * scale, 140 * scale), 
                new Color(0.1f, 0.1f, 0.2f, 0.8f));
            
            // タイトル
            TextMeshProUGUI titleText = CreateUIText("TestTitle", 
                new Vector2(0, 50 * scale), 
                new Vector2(200 * scale, 30 * scale), 
                "=== テスト機能 ===\nF1: 武器 F2: アタッチメント", 
                Mathf.RoundToInt(10 * scale), testPanel);
            titleText.color = Color.yellow;
            titleText.alignment = TextAlignmentOptions.Center;
            
            // 武器入れ替えボタン
            Button weaponSwapButton = CreateUIButton("WeaponSwapButton", 
                new Vector2(0, 10 * scale), 
                new Vector2(180 * scale, 30 * scale), 
                OnWeaponSwapClicked, testPanel);
            
            // ボタンテキスト
            Transform buttonText = weaponSwapButton.transform.Find("Text");
            if (buttonText != null)
            {
                TextMeshProUGUI textComponent = buttonText.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "⚔️ 武器入れ替え";
                    textComponent.fontSize = Mathf.RoundToInt(12 * scale);
                }
            }
            
            // アタッチメント入れ替えボタン
            Button attachmentSwapButton = CreateUIButton("AttachmentSwapButton", 
                new Vector2(0, -30 * scale), 
                new Vector2(180 * scale, 30 * scale), 
                OnAttachmentSwapClicked, testPanel);
            
            // ボタンテキスト
            Transform attachmentButtonText = attachmentSwapButton.transform.Find("Text");
            if (attachmentButtonText != null)
            {
                TextMeshProUGUI textComponent = attachmentButtonText.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "🔧 アタッチメント入れ替え";
                    textComponent.fontSize = Mathf.RoundToInt(12 * scale);
                }
            }
            
            Debug.Log("テスト用装備入れ替えボタン作成完了!");
        }
        
        private void UpdateUI()
        {
            // 戦闘が開始されていない場合はUI更新をスキップ
            if (!isBattleStarted || battleManager == null) return;
            
            // キーボードショートカット（テスト用）
            if (Input.GetKeyDown(KeyCode.F1))
            {
                try
                {
                    OnWeaponSwapClicked();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"武器入れ替えエラー: {ex.Message}");
                }
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                try
                {
                    OnAttachmentSwapClicked();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"アタッチメント入れ替えエラー: {ex.Message}");
                }
            }
            
            // ターン表示更新
            if (turnText != null)
                turnText.text = $"Turn: {battleManager.CurrentTurn}";
            
            // HP表示更新
            if (hpText != null && battleManager.PlayerData != null)
                hpText.text = $"HP: {battleManager.PlayerData.currentHp} / {battleManager.PlayerData.maxHp}";
            
            // ゲーム状態表示更新
            if (stateText != null)
                stateText.text = battleManager.CurrentState.ToString();
            
            // 戦場の敵表示更新
            UpdateBattleFieldDisplay();
            
            // 敵情報表示更新（右上）
            UpdateEnemyInfoDisplay();
            
            // コンボ進行状況表示更新
            UpdateComboProgressDisplay();
        }
        
        
        private void OnNextTurnClicked()
        {
            Debug.Log("=== Next Turn Button Clicked ===");
            
            if (battleManager == null)
            {
                Debug.LogError("BattleManager is null!");
                return;
            }
            
            Debug.Log($"Current State: {battleManager.CurrentState}");
            Debug.Log($"Current Turn: {battleManager.CurrentTurn}");
            
            if (battleManager.CurrentState == GameState.PlayerTurn)
            {
                // 予告ダメージシステム: 予告ダメージを実際に適用
                if (handSystem != null && handSystem.HasPendingDamage)
                {
                    bool damageApplied = handSystem.ApplyPendingDamage();
                    Debug.Log($"✓ Pending damage applied: {damageApplied}");
                }
                
                // ターン終了処理
                battleManager.EndPlayerTurn(TurnEndReason.ActionCompleted);
                Debug.Log("✓ Player turn ended, switching to enemy turn");
            }
            else if (battleManager.CurrentState == GameState.Victory)
            {
                Debug.Log("🏆 Battle already won!");
            }
            else if (battleManager.CurrentState == GameState.Defeat)
            {
                Debug.Log("🖤 Battle already lost!");
            }
            else
            {
                Debug.LogWarning($"Cannot end turn in current state: {battleManager.CurrentState}");
            }
        }
        
        private void OnResetClicked()
        {
            Debug.Log("=== Reset Battle Button Clicked ===");
            
            if (battleManager != null)
            {
                var oldState = battleManager.CurrentState;
                var oldTurn = battleManager.CurrentTurn;
                
                battleManager.ResetBattle();
                
                Debug.Log($"✓ Battle reset! {oldState} (Turn {oldTurn}) -> {battleManager.CurrentState} (Turn {battleManager.CurrentTurn})");
            }
            else
            {
                Debug.LogError("BattleManager is null!");
            }
        }
        
        /// <summary>
        /// 武器入れ替えボタンクリック時の処理
        /// </summary>
        private void OnWeaponSwapClicked()
        {
            Debug.Log("=== 武器入れ替えテスト ===");
            
            try
            {
                AttachmentSystem attachmentSystem = FindObjectOfType<AttachmentSystem>();
                if (attachmentSystem == null)
                {
                    Debug.LogError("AttachmentSystem not found! Cannot swap weapons.");
                    return;
                }
                
                // 装備武器をランダムに入れ替え
                attachmentSystem.RandomlyReequipWeapons();
                
                // 手札を再生成
                attachmentSystem.RegenerateWeaponCardsForNewTurn();
            
            // HandSystemに手札更新を通知
            HandSystem handSystem = FindObjectOfType<HandSystem>();
            if (handSystem != null)
            {
                var weaponCards = attachmentSystem.WeaponCards;
                if (weaponCards != null && weaponCards.Count > 0)
                {
                    // リフレクションでHandSystemの手札を更新
                    try
                    {
                        var field = handSystem.GetType().GetField("currentHand", 
                            NonPublic | Instance);
                        if (field != null)
                        {
                            field.SetValue(handSystem, weaponCards);
                            Debug.Log("✅ HandSystem手札更新完了");
                        }
                        
                        // UI更新を強制実行（代替手段）
                        var updateMethod = handSystem.GetType().GetMethod("ForceUpdateHandDisplay", 
                            NonPublic | Instance);
                        if (updateMethod != null)
                        {
                            updateMethod.Invoke(handSystem, null);
                            Debug.Log("✅ HandSystem UI強制更新完了");
                        }
                        else
                        {
                            // 代替手段：手札状態を変更してUI更新をトリガー
                            var stateField = handSystem.GetType().GetField("currentHandState", 
                                NonPublic | Instance);
                            if (stateField != null)
                            {
                                var currentState = stateField.GetValue(handSystem);
                                stateField.SetValue(handSystem, currentState);
                                Debug.Log("✅ HandSystem 状態更新完了（代替手段）");
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"HandSystem更新エラー: {ex.Message}");
                    }
                }
            }
            
            Debug.Log($"✅ 武器入れ替え完了: {attachmentSystem.EquippedWeapons.Count}個の武器");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"武器入れ替えエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// アタッチメント入れ替えボタンクリック時の処理
        /// </summary>
        private void OnAttachmentSwapClicked()
        {
            Debug.Log("=== アタッチメント入れ替えテスト ===");
            
            try
            {
                AttachmentSystem attachmentSystem = FindObjectOfType<AttachmentSystem>();
                if (attachmentSystem == null)
                {
                    Debug.LogError("AttachmentSystem not found! Cannot swap attachments.");
                    return;
                }
            
            // 全アタッチメントをクリア
            var currentAttachments = attachmentSystem.GetAttachedAttachments();
            Debug.Log($"現在のアタッチメント数: {currentAttachments.Count}");
            
            // 既存アタッチメントを削除
            foreach (var attachment in currentAttachments.ToList())
            {
                attachmentSystem.RemoveAttachment(attachment);
                Debug.Log($"  🗑️ 削除: {attachment.attachmentName}");
            }
            
            // ランダムに新しいアタッチメントを3個装備
            var availableAttachments = attachmentSystem.GenerateAttachmentOptions();
            if (availableAttachments != null && availableAttachments.Length > 0)
            {
                try
                {
                    var random = new System.Random((int)System.DateTime.Now.Ticks);
                    int attachmentsToAdd = Mathf.Min(3, availableAttachments.Length);
                    
                    for (int i = 0; i < attachmentsToAdd; i++)
                    {
                        var randomAttachment = availableAttachments[random.Next(availableAttachments.Length)];
                        if (randomAttachment != null)
                        {
                            bool success = attachmentSystem.AttachAttachment(randomAttachment);
                            if (success)
                            {
                                Debug.Log($"  ✅ 装備: {randomAttachment.attachmentName} ({randomAttachment.rarity})");
                            }
                            else
                            {
                                Debug.LogWarning($"  ❌ 装備失敗: {randomAttachment.attachmentName}");
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"アタッチメント装備エラー: {ex.Message}");
                }
            }
            
            // 新しい装備状況を表示
            var newAttachments = attachmentSystem.GetAttachedAttachments();
            Debug.Log($"✅ アタッチメント入れ替え完了: {newAttachments.Count}個のアタッチメント装備");
            
            // コンボ表示の更新（装備アタッチメントコンボの再表示）
            UpdateEquippedAttachmentCombosDisplay();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"アタッチメント入れ替えエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// テスト用：敵を倒すボタンのクリック処理
        /// </summary>
        private void OnKillEnemyClicked()
        {
            Debug.Log("=== Kill Enemy Button Clicked ===");
            
            if (battleManager?.BattleField == null)
            {
                Debug.LogWarning("BattleManager or BattleField is null!");
                return;
            }
            
            // 生きている敵を取得
            var allEnemies = battleManager.BattleField.GetAllEnemies();
            var aliveEnemies = allEnemies.Where(enemy => enemy != null && enemy.IsAlive()).ToList();
            
            if (aliveEnemies.Count == 0)
            {
                Debug.Log("No alive enemies to kill!");
                return;
            }
            
            // 最初の生きている敵を撃破
            var targetEnemy = aliveEnemies[0];
            Debug.Log($"Killing enemy: {targetEnemy.enemyData.enemyName} at ({targetEnemy.gridX}, {targetEnemy.gridY})");
            
            // 敵のHPを0にして撃破
            targetEnemy.TakeDamage(targetEnemy.currentHp);
            
            // 戦場から敵を削除
            battleManager.BattleField.RemoveEnemy(new GridPosition(targetEnemy.gridX, targetEnemy.gridY));
            
            // UI表示を更新
            UpdateEnemyDisplay();
            UpdateBattleFieldDisplay();
            
            Debug.Log($"✓ Enemy {targetEnemy.enemyData.enemyName} defeated! Remaining: {aliveEnemies.Count - 1}");
            
            // 全敵撃破チェック
            var remainingEnemies = battleManager.BattleField.GetAllEnemies()
                .Where(enemy => enemy != null && enemy.IsAlive()).ToList();
            
            if (remainingEnemies.Count == 0)
            {
                Debug.Log("🎉 All enemies defeated! Victory!");
                if (battleManager != null)
                {
                    // 勝利状態に変更（BattleManagerに勝利処理メソッドがあれば）
                    // battleManager.SetVictory(); // メソッドが存在する場合
                    
                    // アタッチメント選択画面を表示
                    ShowAttachmentSelectionAfterVictory();
                }
            }
        }
        
        // 手動でUIを再作成
        [ContextMenu("Recreate UI")]
        public void RecreateUI()
        {
            // 既存のUI要素を削除
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(canvas.transform.GetChild(i).gameObject);
            }
            
            // UIを再作成（コンボUIも含む）
            CreateSimpleBattleUI();
            
            Debug.Log("UI再作成完了（コンボUI含む）");
        }
        
        /// <summary>
        /// 敵が撃破された時の表示更新
        /// </summary>
        public void OnEnemyDefeated(EnemyInstance defeatedEnemy)
        {
            Debug.Log($"Enemy defeated: {defeatedEnemy.enemyData.enemyName} at ({defeatedEnemy.gridX}, {defeatedEnemy.gridY})");
            // 敵情報表示は自動的に更新される（UpdateEnemyInfoDisplayで）
        }
        
        /// <summary>
        /// コンボタイプに応じた色を取得
        /// </summary>
        Color GetComboTypeColor(ComboType comboType)
        {
            int index = (int)comboType;
            if (index >= 0 && index < comboTypeColors.Length)
            {
                return comboTypeColors[index];
            }
            return comboTypeColors[0]; // デフォルト色
        }
        
        // ===== HandSystem イベントハンドラー =====
        
        /// <summary>
        /// 予告ダメージが計算された時の処理
        /// </summary>
        private void OnPendingDamageCalculated(PendingDamageInfo pendingDamage)
        {
            Debug.Log($"Pending damage calculated: {pendingDamage.description} - {pendingDamage.calculatedDamage} damage");
            
            if (pendingDamageText != null)
            {
                // 予告ダメージ情報を表示
                string displayText = $"予告ダメージ:\n{pendingDamage.description}\nダメージ: {pendingDamage.calculatedDamage}\nターゲット: {pendingDamage.targetEnemies.Count}体";
                pendingDamageText.text = displayText;
                pendingDamageText.color = Color.yellow;
            }
        }
        
        /// <summary>
        /// 予告ダメージが実際に適用された時の処理
        /// </summary>
        private void OnPendingDamageApplied(PendingDamageInfo appliedDamage)
        {
            Debug.Log($"Pending damage applied: {appliedDamage.description} - {appliedDamage.calculatedDamage} damage");
            
            if (pendingDamageText != null)
            {
                // 適用完了を短時間表示
                pendingDamageText.text = $"ダメージ適用完了!\n{appliedDamage.calculatedDamage} ダメージ";
                pendingDamageText.color = Color.green;
                
                // 2秒後にクリア
                StartCoroutine(ClearPendingDamageDisplayAfterDelay(2f));
            }
        }
        
        /// <summary>
        /// 予告ダメージがクリアされた時の処理
        /// </summary>
        private void OnPendingDamageCleared()
        {
            Debug.Log("Pending damage cleared");
            
            if (pendingDamageText != null)
            {
                pendingDamageText.text = "";
            }
        }
        
        /// <summary>
        /// 敵データが変更された時の処理（UI更新）
        /// </summary>
        private void OnEnemyDataChanged()
        {
            Debug.Log("敵データが変更されました - UI更新開始");
            
            // 敵情報表示を強制更新
            UpdateEnemyInfoDisplay();
            
            // 戦場表示も更新
            UpdateBattleFieldDisplay();
        }
        
        /// <summary>
        /// 戦場データが変更された時の処理（UI更新）
        /// </summary>
        private void OnBattleFieldChanged()
        {
            Debug.Log("戦場データが変更されました - UI更新開始");
            
            // 戦場表示を強制更新
            UpdateBattleFieldDisplay();
        }
        
        /// <summary>
        /// プレイヤーデータが変更された時の処理（UI更新）
        /// </summary>
        private void OnPlayerDataChanged(PlayerData playerData)
        {
            Debug.Log($"プレイヤーデータが変更されました - HP: {playerData.currentHp}/{playerData.maxHp}");
            
            // HP表示を更新
            if (hpText != null)
            {
                hpText.text = $"HP: {playerData.currentHp}/{playerData.maxHp}";
            }
        }
        
        // ===== スタート画面関連メソッド =====
        
        /// <summary>
        /// 戦闘UI要素のグループを作成
        /// </summary>
        private void CreateBattleUIGroup()
        {
            battleUIGroup = new GameObject("Battle UI Group");
            battleUIGroup.transform.SetParent(canvas.transform, false);
            
            // RectTransformを追加してキャンバス全体をカバー
            RectTransform rect = battleUIGroup.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            Debug.Log("Battle UI Group created");
        }
        
        /// <summary>
        /// スタート画面を作成
        /// </summary>
        private void CreateStartScreen()
        {
            Debug.Log("Creating Start Screen...");
            
            // 画面サイズを取得
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float screenWidth = canvasRect.rect.width;
            float screenHeight = canvasRect.rect.height;
            
            // スケールファクターを計算
            float scaleX = screenWidth / 1920f;
            float scaleY = screenHeight / 1080f;
            float scale = Mathf.Min(scaleX, scaleY);
            
            // スタート画面パネル
            startScreenPanel = CreateUIPanel("Start Screen Panel", Vector2.zero,
                new Vector2(screenWidth, screenHeight), new Color(0, 0, 0, 0.8f));
            
            // タイトルテキスト
            titleText = CreateUIText("Title Text",
                new Vector2(0, screenHeight * 0.1f),
                new Vector2(800 * scale, 100 * scale),
                "🃏 手札戦闘システム 🃏",
                Mathf.RoundToInt(36 * scale),
                startScreenPanel);
            titleText.color = Color.yellow;
            titleText.fontStyle = FontStyles.Bold;
            
            // 説明テキスト
            instructionText = CreateUIText("Instruction Text",
                new Vector2(0, -screenHeight * 0.05f),
                new Vector2(600 * scale, 150 * scale),
                "コンボシステムで敵を撃破せよ！\n\n操作方法:\n・手札からカードを選択して攻撃\n・同じ属性のカードでコンボを狙え\n・ターン制限内に敵を全滅せよ！",
                Mathf.RoundToInt(18 * scale),
                startScreenPanel);
            instructionText.color = Color.white;
            instructionText.alignment = TextAlignmentOptions.Center;
            
            // 戦闘開始ボタン
            startBattleButton = CreateUIButton("🗺️ 戦闘開始 🗺️",
                new Vector2(0, -screenHeight * 0.25f),
                new Vector2(300 * scale, 80 * scale),
                OnStartBattleClicked,
                startScreenPanel);
            
            // ボタンの色を特別に設定
            var buttonImage = startBattleButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // 赤いボタン
                buttonImage.raycastTarget = true; // 明示的にクリックを有効化
            }
            
            // ボタンテキストのサイズを大きく
            var buttonText = startBattleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.fontSize = Mathf.RoundToInt(20 * scale);
                buttonText.fontStyle = FontStyles.Bold;
                buttonText.raycastTarget = false; // テキストはクリックをブロックしない
            }
            
            // ボタンを最前面に移動（重要！）
            startBattleButton.transform.SetAsLastSibling();
            Debug.Log("✅ Start Battle Button moved to front");
            
            Debug.Log("Start Screen created successfully!");
        }
        
        /// <summary>
        /// 戦闘開始ボタンクリック時の処理
        /// </summary>
        private void OnStartBattleClicked()
        {
            Debug.Log("=== Start Battle Button Clicked ===");
            
            // 戦闘開始処理を実行
            StartBattle();
        }
        
        /// <summary>
        /// 戦闘開始処理（手札表示強化版）
        /// </summary>
        private void StartBattle()
        {
            Debug.Log("=== Starting battle... ===");
            
            // スタート画面を非表示
            if (startScreenPanel != null)
            {
                startScreenPanel.SetActive(false);
                Debug.Log("✓ Start screen hidden");
            }
            
            // 戦闘UIを表示
            if (battleUIGroup != null)
            {
                battleUIGroup.SetActive(true);
                Debug.Log("✓ Battle UI shown");
            }
            
            // 🔧 重要: 手札UIを表示（強化版）
            ShowHandUI();
            
            // 戦闘状態を更新
            isBattleStarted = true;
            Debug.Log("✓ Battle state updated to started");
            
            // 戦闘システムを初期化・準備
            if (battleManager != null)
            {
                // 戦闘をリセットして新しい戦闘を開始
                battleManager.ResetBattle();
                Debug.Log("✓ Battle system initialized");
                
                // 敵を配置してUI表示を更新
                CreateTestEnemiesOnBattleField();
                Debug.Log("✓ Enemies placed and UI updated");
            }
            
            // 🔧 手札生成のタイミングを短縮（即座表示のため）
            StartCoroutine(AutoGenerateHandAfterDelay(0.2f)); // 0.5秒から0.2秒に短縮
            
            // 🔧 追加: 手札UIの再確認処理
            StartCoroutine(VerifyHandUIAfterDelay(1f));
            
            // リアルタイム敵情報更新を開始
            StartCoroutine(UpdateEnemyDisplayPeriodically());
            
            Debug.Log("✓ Battle started successfully!");
        }
        
        /// <summary>
        /// 手札UIを表示（強化版）
        /// </summary>
        private void ShowHandUI()
        {
            Debug.Log("=== ShowHandUI Called ===");
            
            // HandUIコンポーネントを探して表示する
            HandUI handUI = FindObjectOfType<HandUI>();
            if (handUI != null)
            {
                // 手札UIを強制的に表示状態に設定（存在する場合）
                try
                {
                    handUI.GetType().GetMethod("SetHandUIVisible")?.Invoke(handUI, new object[] { true });
                    Debug.Log("✓ Hand UI shown via SetHandUIVisible");
                }
                catch
                {
                    Debug.LogWarning("SetHandUIVisible method not found, using alternative approach");
                }
                
                // 手札UI強制更新
                try
                {
                    handUI.GetType().GetMethod("ForceUpdateHandDisplay")?.Invoke(handUI, null);
                    Debug.Log("✓ Hand UI force updated");
                }
                catch
                {
                    Debug.LogWarning("ForceUpdateHandDisplay method not found");
                }
                
                // 手札UIを継続表示するためのコルーチンを開始
                StartCoroutine(KeepHandUIVisible(handUI));
                Debug.Log("✓ Hand UI force updated");
            }
            else
            {
                Debug.LogWarning("❌ HandUI component not found! Attempting to create...");
                
                // HandUIが見つからない場合、AutoBattleUICreatorで作成を試行
                var creator = FindObjectOfType<AutoBattleUICreator>();
                if (creator != null)
                {
                    creator.CreateBattleUISystem();
                    Debug.Log("✓ Attempted to create HandUI via AutoBattleUICreator");
                    
                    // 再度HandUIを探す
                    handUI = FindObjectOfType<HandUI>();
                    if (handUI != null)
                    {
                        handUI.SetHandUIVisible(true);
                        Debug.Log("✓ Newly created Hand UI shown");
                    }
                }
                else
                {
                    Debug.LogWarning("❌ AutoBattleUICreator not found, creating one manually...");
                    
                    // AutoBattleUICreatorを手動で作成
                    GameObject uiCreatorObj = new GameObject("AutoBattleUICreator");
                    AutoBattleUICreator uiCreator = uiCreatorObj.AddComponent<AutoBattleUICreator>();
                    
                    Debug.Log("✓ AutoBattleUICreator created manually");
                    
                    // 少し待ってからHandUIを再度探す
                    StartCoroutine(DelayedHandUICheck());
                }
            }
            
            // HandSystemの確認と手札生成確認
            var handSystem = FindObjectOfType<HandSystem>();
            if (handSystem != null)
            {
                Debug.Log($"✓ HandSystem found - State: {handSystem.CurrentHandState}, Hand Count: {handSystem.CurrentHand?.Length ?? 0}");
            }
            else
            {
                Debug.LogWarning("❌ HandSystem not found, creating one manually...");
                
                // HandSystemを手動で作成
                var battleManager = FindObjectOfType<BattleManager>();
                if (battleManager != null)
                {
                    HandSystem newHandSystem = battleManager.gameObject.AddComponent<HandSystem>();
                    Debug.Log("✓ HandSystem created manually and attached to BattleManager");
                    
                    // 作成後の手札生成を試行
                    StartCoroutine(DelayedHandGeneration());
                }
                else
                {
                    Debug.LogError("❌ BattleManager not found! Cannot create HandSystem.");
                }
            }
        }
        
        /// <summary>
        /// 指定時間後に自動で手札を補充（強化版）
        /// </summary>
        System.Collections.IEnumerator AutoGenerateHandAfterDelay(float delay)
        {
            Debug.Log($"Starting AutoGenerateHandAfterDelay with {delay} second delay...");
            
            yield return new WaitForSeconds(delay);
            
            Debug.Log("=== Auto Generate Hand Process Started ===");
            
            if (handSystem != null)
            {
                // 現在の手札状態を確認
                Debug.Log($"HandSystem state before generation: {handSystem.CurrentHandState}");
                Debug.Log($"Current hand count: {handSystem.CurrentHand?.Length ?? 0}");
                Debug.Log($"Can take action: {handSystem.CanTakeAction}");
                
                // HandSystemに手札生成を依頼
                handSystem.GenerateHand();
                
                // 手札生成後の成功確認は HandSystem の CurrentHand をチェック
                if (handSystem.CurrentHand != null && handSystem.CurrentHand.Length > 0)
                {
                    Debug.Log($"✓ Hand generated successfully! Cards: {handSystem.CurrentHand.Length}");
                    
                    // 手札UIの強制更新
                    yield return new WaitForSeconds(0.1f); // UI更新のための短い遅延
                    
                    HandUI handUI = FindObjectOfType<HandUI>();
                    if (handUI != null)
                    {
                        handUI.ForceUpdateHandDisplay();
                        Debug.Log("✓ Hand UI display force updated after generation");
                    }
                    
                    // 生成されたカードの詳細をログ出力
                    if (handSystem.CurrentHand != null)
                    {
                        for (int i = 0; i < handSystem.CurrentHand.Length; i++)
                        {
                            var card = handSystem.CurrentHand[i];
                            if (card != null)
                            {
                                Debug.Log($"  Card {i + 1}: {card.displayName} (Power: {card.weaponData.basePower})");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("❌ Failed to generate hand");
                    
                    // 失敗した場合のリトライ処理
                    yield return new WaitForSeconds(1f);
                    Debug.Log("Retrying hand generation...");
                    
                    handSystem.GenerateHand();
                    if (handSystem.CurrentHand != null && handSystem.CurrentHand.Length > 0)
                    {
                        Debug.Log("✓ Hand generation succeeded on retry");
                    }
                    else
                    {
                        Debug.LogError("❌ Hand generation failed on retry");
                    }
                }
            }
            else
            {
                Debug.LogError("❌ HandSystem not found! Cannot auto-generate hand.");
                
                // HandSystemがない場合の応急処置
                Debug.Log("Attempting to find or create HandSystem...");
                
                // BattleManagerからHandSystemを探す
                if (battleManager != null)
                {
                    var battleManagerGameObject = battleManager.gameObject;
                    handSystem = battleManagerGameObject.GetComponent<HandSystem>();
                    
                    if (handSystem == null)
                    {
                        Debug.Log("Adding HandSystem component to BattleManager...");
                        handSystem = battleManagerGameObject.AddComponent<HandSystem>();
                    }
                    
                    if (handSystem != null)
                    {
                        Debug.Log("✓ HandSystem found/created, attempting hand generation...");
                        
                        // 短い遅延で初期化を待つ
                        yield return new WaitForSeconds(0.5f);
                        
                        handSystem.GenerateHand();
                        if (handSystem.CurrentHand != null && handSystem.CurrentHand.Length > 0)
                        {
                            Debug.Log("✓ Hand generation succeeded with newly created HandSystem");
                        }
                        else
                        {
                            Debug.LogError("❌ Hand generation failed with newly created HandSystem");
                        }
                    }
                }
            }
            
            Debug.Log("=== Auto Generate Hand Process Completed ===");
        }
        
        /// <summary>
        /// 手札UIの表示状態を再確認する（新規追加）
        /// </summary>
        System.Collections.IEnumerator VerifyHandUIAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            Debug.Log("=== Verifying Hand UI Display ===");
            
            HandUI handUI = FindObjectOfType<HandUI>();
            if (handUI != null)
            {
                // 手札UIの状態をチェック
                Debug.Log("HandUI found, checking visibility...");
                
                // 強制的に表示状態を更新
                handUI.SetHandUIVisible(true);
                handUI.ForceUpdateHandDisplay();
                handUI.ForceRefreshAll();
                
                Debug.Log("✓ Hand UI visibility and display force refreshed");
            }
            else
            {
                Debug.LogWarning("❌ HandUI still not found after verification delay!");
                
                // 最終手段: AutoBattleUICreatorで再作成を試行
                var creator = FindObjectOfType<AutoBattleUICreator>();
                if (creator != null)
                {
                    Debug.Log("Attempting final HandUI creation via AutoBattleUICreator...");
                    creator.CreateBattleUISystem();
                    
                    yield return new WaitForSeconds(0.2f);
                    
                    handUI = FindObjectOfType<HandUI>();
                    if (handUI != null)
                    {
                        handUI.SetHandUIVisible(true);
                        handUI.ForceUpdateHandDisplay();
                        Debug.Log("✓ HandUI successfully created and displayed on final attempt");
                    }
                    else
                    {
                        Debug.LogError("❌ Final HandUI creation attempt failed!");
                    }
                }
            }
            
            // HandSystemの状態も確認
            if (handSystem != null)
            {
                Debug.Log($"HandSystem state: {handSystem.CurrentHandState}");
                Debug.Log($"Hand count: {handSystem.CurrentHand?.Length ?? 0}");
                Debug.Log($"Can take action: {handSystem.CanTakeAction}");
                
                // 手札が空の場合、再生成を試行
                if (handSystem.CurrentHand == null || handSystem.CurrentHand.Length == 0)
                {
                    Debug.Log("Hand is empty, attempting regeneration...");
                    handSystem.GenerateHand();
                    if (handSystem.CurrentHand != null && handSystem.CurrentHand.Length > 0)
                    {
                        Debug.Log("✓ Hand regenerated successfully during verification");
                    }
                    else
                    {
                        Debug.LogWarning("❌ Hand regeneration failed during verification");
                    }
                }
            }
            else
            {
                Debug.LogWarning("❌ HandSystem not found during verification! Attempting to create...");
                
                // 検証時にもHandSystemがない場合は作成を試行
                var battleManager = FindObjectOfType<BattleManager>();
                if (battleManager != null)
                {
                    HandSystem newHandSystem = battleManager.gameObject.AddComponent<HandSystem>();
                    Debug.Log("✓ HandSystem created during verification");
                }
            }
            
            Debug.Log("=== Hand UI Verification Completed ===");
        }
        
        // ===== ComboSystem イベントハンドラー =====
        
        /// <summary>
        /// コンボ開始時の処理
        /// </summary>
        private void OnComboStarted(ComboData comboData)
        {
            Debug.Log($"Combo started: {comboData.comboName}");
            
            // 新システム：コンボグループ作成・表示
            HandleComboStartedNew(comboData);
            
            // コンボ進行状況表示は自動更新される（UpdateComboProgressDisplayで）
        }
        
        /// <summary>
        /// コンボ進行更新時の処理
        /// </summary>
        private void OnComboProgressUpdated(ComboProgress progress)
        {
            Debug.Log($"Combo progress updated: {progress.comboData.comboName} - {progress.progressPercentage:P0}");
            
            // 新システム：コンボグループ進行更新
            HandleComboProgressUpdatedNew(progress);
            
            // コンボ進行状況表示は自動更新される（UpdateComboProgressDisplayで）
        }
        
        /// <summary>
        /// コンボ完成時の処理
        /// </summary>
        private void OnComboCompleted(ComboExecutionResult result)
        {
            Debug.Log($"Combo completed: {result.executedCombo.comboName} - {result.resultMessage}");
            
            // 新システム：コンボグループ完了処理
            HandleComboCompletedNew(result);
            
            // コンボ効果ポップアップ表示
            ShowComboEffectPopup(result);
        }
        
        /// <summary>
        /// コンボ失敗時の処理
        /// </summary>
        private void OnComboFailed(ComboData comboData, string reason)
        {
            Debug.Log($"Combo failed: {comboData.comboName} - {reason}");
            
            // 新システム：コンボグループ失敗処理
            HandleComboFailedNew(comboData, reason);
            
            // 失敗メッセージを簡易表示
            if (comboEffectPopup != null)
            {
                StartCoroutine(ShowComboFailureMessage(comboData.comboName, reason));
            }
        }
        
        /// <summary>
        /// コンボ中断時の処理
        /// </summary>
        private void OnComboInterrupted(ComboData comboData)
        {
            Debug.Log($"Combo interrupted: {comboData.comboName}");
            
            // 新システム：コンボグループ中断処理
            HandleComboInterruptedNew(comboData);
            
            // 中断メッセージを簡易表示
            if (comboEffectPopup != null)
            {
                StartCoroutine(ShowComboFailureMessage(comboData.comboName, "敵により中断されました"));
            }
        }
        
        /// <summary>
        /// コンボ効果ポップアップ表示
        /// </summary>
        private void ShowComboEffectPopup(ComboExecutionResult result)
        {
            if (comboEffectPopup == null || comboEffectTitle == null || comboEffectDescription == null)
                return;
            
            // ポップアップを表示
            comboEffectPopup.SetActive(true);
            
            // コンボ名表示
            comboEffectTitle.text = $"🎆 {result.executedCombo.comboName} 🎆";
            
            // 効果説明作成
            string effectText = result.resultMessage;
            if (result.totalDamageMultiplier > 1.0f)
            {
                effectText += $"\nダメージ倍率: {result.totalDamageMultiplier:P0}";
            }
            if (result.additionalActionsGranted > 0)
            {
                effectText += $"\n追加行動: +{result.additionalActionsGranted}";
            }
            
            comboEffectDescription.text = effectText;
            
            // 3秒後に自動消滅
            StartCoroutine(HideComboEffectPopupAfterDelay(3f));
        }
        
        /// <summary>
        /// コンボ失敗メッセージ表示
        /// </summary>
        System.Collections.IEnumerator ShowComboFailureMessage(string comboName, string reason)
        {
            if (comboEffectPopup == null || comboEffectTitle == null || comboEffectDescription == null)
                yield break;
            
            // ポップアップを灰色で表示
            comboEffectPopup.SetActive(true);
            var popupImage = comboEffectPopup.GetComponent<Image>();
            if (popupImage != null)
            {
                popupImage.color = new Color(0.5f, 0.5f, 0.5f, 0.9f); // 灰色
            }
            
            comboEffectTitle.text = $"❌ {comboName} ❌";
            comboEffectTitle.color = Color.red;
            
            comboEffectDescription.text = $"コンボ失敗: {reason}";
            comboEffectDescription.color = Color.white;
            
            // 2秒後に消滅
            yield return new WaitForSeconds(2f);
            
            if (comboEffectPopup != null)
            {
                comboEffectPopup.SetActive(false);
                
                // 色を元に戻す
                if (popupImage != null)
                {
                    popupImage.color = new Color(0.9f, 0.8f, 0.1f, 0.9f); // 金色
                }
                
                if (comboEffectTitle != null)
                {
                    comboEffectTitle.color = Color.red;
                }
                
                if (comboEffectDescription != null)
                {
                    comboEffectDescription.color = Color.black;
                }
            }
        }
        
        /// <summary>
        /// 指定時間後にコンボ効果ポップアップを非表示
        /// </summary>
        System.Collections.IEnumerator HideComboEffectPopupAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (comboEffectPopup != null)
            {
                comboEffectPopup.SetActive(false);
            }
        }
        
        /// <summary>
        /// 指定時間後に予告ダメージ表示をクリア
        /// </summary>
        System.Collections.IEnumerator ClearPendingDamageDisplayAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (pendingDamageText != null)
            {
                pendingDamageText.text = "";
            }
        }
        
        /// <summary>
        /// コンボの次に必要な手を取得
        /// </summary>
        string GetNextRequiredMove(ComboProgress combo)
        {
            if (combo?.comboData?.condition == null) return "？";
            
            var condition = combo.comboData.condition;
            
            // 属性コンボの場合
            if (condition.comboType == ComboType.AttributeCombo && condition.requiredAttackAttributes != null)
            {
                // 使用済み属性を除外して次の属性を表示
                var usedAttributes = combo.usedAttackAttributes ?? new List<AttackAttribute>();
                var remainingAttributes = condition.requiredAttackAttributes
                    .Where(attr => !usedAttributes.Contains(attr))
                    .ToArray();
                
                if (remainingAttributes.Length > 0)
                {
                    return GetAttributeShortName(remainingAttributes[0]);
                }
            }
            
            // 武器コンボの場合
            if (condition.comboType == ComboType.WeaponCombo && condition.requiredWeaponTypes != null)
            {
                var usedWeaponTypes = combo.usedWeaponTypes ?? new List<WeaponType>();
                var remainingWeaponTypes = condition.requiredWeaponTypes
                    .Where(type => !usedWeaponTypes.Contains(type))
                    .ToArray();
                
                if (remainingWeaponTypes.Length > 0)
                {
                    return GetWeaponTypeShortName(remainingWeaponTypes[0]);
                }
            }
            
            return "任意";
        }
        
        /// <summary>
        /// コンボに必要な残りの手順を全て取得
        /// </summary>
        string GetRemainingRequiredMoves(ComboProgress combo)
        {
            if (combo?.comboData?.condition == null) return "";
            
            var condition = combo.comboData.condition;
            var result = "";
            
            // 属性コンボの場合
            if (condition.comboType == ComboType.AttributeCombo && condition.requiredAttackAttributes != null)
            {
                var usedAttributes = combo.usedAttackAttributes ?? new List<AttackAttribute>();
                var remainingAttributes = condition.requiredAttackAttributes
                    .Where(attr => !usedAttributes.Contains(attr))
                    .ToArray();
                
                result = string.Join("", remainingAttributes.Select(GetAttributeShortName));
            }
            
            // 武器コンボの場合
            if (condition.comboType == ComboType.WeaponCombo && condition.requiredWeaponTypes != null)
            {
                var usedWeaponTypes = combo.usedWeaponTypes ?? new List<WeaponType>();
                var remainingWeaponTypes = condition.requiredWeaponTypes
                    .Where(type => !usedWeaponTypes.Contains(type))
                    .ToArray();
                
                result = string.Join("", remainingWeaponTypes.Select(GetWeaponTypeShortName));
            }
            
            return result.Length > 0 ? $"要:{result}" : "完成可";
        }
        
        /// <summary>
        /// 属性を一文字で表現
        /// </summary>
        string GetAttributeShortName(AttackAttribute attribute)
        {
            switch (attribute)
            {
                case AttackAttribute.Fire: return "炎";
                case AttackAttribute.Ice: return "氷";
                case AttackAttribute.Thunder: return "雷";
                case AttackAttribute.Wind: return "風";
                case AttackAttribute.Earth: return "土";
                case AttackAttribute.Light: return "光";
                case AttackAttribute.Dark: return "闇";
                case AttackAttribute.None: return "無";
                default: return "？";
            }
        }
        
        /// <summary>
        /// 武器タイプを一文字で表現
        /// </summary>
        string GetWeaponTypeShortName(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword: return "剣";
                case WeaponType.Axe: return "斧";
                case WeaponType.Spear: return "槍";
                case WeaponType.Bow: return "弓";
                case WeaponType.Gun: return "銃";
                case WeaponType.Shield: return "盾";
                case WeaponType.Magic: return "魔";
                case WeaponType.Tool: return "道";
                default: return "？";
            }
        }
        
        /// <summary>
        /// テスト用ComboDatabase作成
        /// </summary>
        private void CreateTestComboDatabase()
        {
            if (comboSystem == null)
            {
                Debug.LogWarning("ComboSystem is null, cannot create combo database");
                return;
            }
            
            Debug.Log("Creating test combo database...");
            
            // テスト用コンボデータベースを作成
            ComboDatabase comboDB = ScriptableObject.CreateInstance<ComboDatabase>();
            comboDB.hideFlags = HideFlags.DontSaveInEditor;
            
            // テスト用コンボデータを作成
            ComboData[] testCombos = new ComboData[]
            {
                new ComboData
                {
                    comboName = "炎氷の共鳴",
                    condition = new ComboCondition
                    {
                        comboType = ComboType.AttributeCombo,
                        requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Fire, AttackAttribute.Ice },
                        maxTurnInterval = 3,
                        successRate = 0.9f
                    },
                    effects = new ComboEffect[]
                    {
                        new ComboEffect
                        {
                            effectType = ComboEffectType.DamageMultiplier,
                            damageMultiplier = 1.5f,
                            effectDescription = "炎と氷の攻撃が共鳴してダメージ1.5倍"
                        }
                    },
                    requiredWeaponCount = 2,
                    comboDescription = "炎と氷属性の武器を組み合わせて威力強化",
                    canInterrupt = false,
                    priority = 10
                },
                new ComboData
                {
                    comboName = "雷光連撃",
                    condition = new ComboCondition
                    {
                        comboType = ComboType.WeaponCombo,
                        requiredWeaponTypes = new WeaponType[] { WeaponType.Spear, WeaponType.Sword },
                        maxTurnInterval = 2,
                        successRate = 0.8f
                    },
                    effects = new ComboEffect[]
                    {
                        new ComboEffect
                        {
                            effectType = ComboEffectType.AdditionalAction,
                            additionalActions = 1,
                            effectDescription = "追加行動1回獲得"
                        }
                    },
                    requiredWeaponCount = 2,
                    comboDescription = "槍と剣の連携で追加攻撃チャンス",
                    canInterrupt = true,
                    interruptResistance = 0.3f,
                    priority = 8
                }
            };
            
            // プライベートフィールドにリフレクションで設定
            var combosField = typeof(ComboDatabase).GetField("availableCombos", 
                NonPublic | Instance);
            combosField?.SetValue(comboDB, testCombos);
            
            // ComboSystemにデータベースを設定
            var comboDBField = typeof(ComboSystem).GetField("comboDatabase", 
                NonPublic | Instance);
            comboDBField?.SetValue(comboSystem, comboDB);
            
            Debug.Log("Test combo database created and assigned to ComboSystem!");
        }
        
        // === UIクリック問題修正用デバッグ機能 ===
        
        /// <summary>
        /// 背景パネルのクリック遮断を修正
        /// </summary>
        [ContextMenu("Fix Background Panel Click Blocking")]
        public void FixBackgroundPanelClickBlocking()
        {
            Debug.Log("=== Fixing Background Panel Click Blocking ===");
            
            // Canvas内の全Imageコンポーネントをチェック
            Image[] allImages = canvas.GetComponentsInChildren<Image>(true);
            
            foreach (Image img in allImages)
            {
                string objName = img.gameObject.name.ToLower();
                
                // 背景関連のパネルを特定
                if (objName.Contains("background") || objName.Contains("背景") || 
                    objName.Contains("panel") && !objName.Contains("button"))
                {
                    // 背景パネルのraycastTargetを無効化
                    img.raycastTarget = false;
                    Debug.Log($"✅ Disabled raycastTarget for: {img.gameObject.name}");
                }
            }
            
            Debug.Log("✅ Background Panel Click Blocking Fixed!");
        }
        
        [ContextMenu("Force Fix UI Click Issues")]
        public void ForceFixUIClickIssues()
        {
            Debug.Log("=== Fixing UI Click Issues ===");
            
            // 🔧 最重要: 非表示UI要素の完全無効化
            ForceDisableAllHiddenUIElements();
            
            // EventSystemの確認・修正
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("EventSystem not found! Creating new one...");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("✅ New EventSystem created");
            }
            else
            {
                eventSystem.enabled = true;
                Debug.Log($"✅ EventSystem found and enabled: {eventSystem.name}");
            }
            
            // Canvasの設定確認
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("✅ GraphicRaycaster added to Canvas");
                }
                else
                {
                    raycaster.enabled = true;
                    Debug.Log("✅ GraphicRaycaster enabled");
                }
                
                Debug.Log($"✅ Canvas configured: RenderMode={canvas.renderMode}, SortingOrder={canvas.sortingOrder}");
            }
            
            // ボタンの状態確認・修正
            FixButtonStates();
            
            Debug.Log("✅ UI Click Issues Fix Complete!");
        }
        
        /// <summary>
        /// 非表示UI要素の完全無効化（クリック遮断防止）
        /// </summary>
        [ContextMenu("Force Disable All Hidden UI Elements")]
        public void ForceDisableAllHiddenUIElements()
        {
            Debug.Log("=== Disabling All Hidden UI Elements ===");
            
            // HandUIを完全無効化
            HandUI handUI = FindObjectOfType<HandUI>();
            if (handUI != null)
            {
                handUI.SetHandUIVisible(false);
                Debug.Log("✅ HandUI completely disabled");
            }
            
            // 手札関連の全UI要素を無効化
            DisableHandUIElements();
            
            // Canvas内の非アクティブ要素のRaycastTargetを無効化
            DisableRaycastOnInactiveElements();
            
            Debug.Log("✅ All hidden UI elements disabled");
        }
        
        /// <summary>
        /// 手札関連UI要素を完全無効化
        /// </summary>
        private void DisableHandUIElements()
        {
            if (canvas == null) return;
            
            // Canvasの全子要素をチェック
            Transform[] allTransforms = canvas.GetComponentsInChildren<Transform>(true);
            
            foreach (Transform t in allTransforms)
            {
                string name = t.name.ToLower();
                
                // 手札関連の名前を持つ要素を特定
                if (name.Contains("hand") || name.Contains("card") || 
                    name.Contains("手札") || name.Contains("カード"))
                {
                    // GameObjectを非アクティブ化
                    t.gameObject.SetActive(false);
                    
                    // ImageコンポーネントのRaycastTargetを無効化
                    Image img = t.GetComponent<Image>();
                    if (img != null)
                    {
                        img.raycastTarget = false;
                    }
                    
                    // Buttonコンポーネントを無効化
                    Button btn = t.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }
                    
                    // TextMeshProコンポーネントのRaycastTargetを無効化
                    TextMeshProUGUI text = t.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.raycastTarget = false;
                    }
                    
                    Debug.Log($"✅ Disabled hand UI element: {t.name}");
                }
            }
        }
        
        /// <summary>
        /// 非アクティブ要素のRaycastTargetを無効化
        /// </summary>
        private void DisableRaycastOnInactiveElements()
        {
            if (canvas == null) return;
            
            // 全てのImageコンポーネントをチェック
            Image[] allImages = canvas.GetComponentsInChildren<Image>(true);
            foreach (Image img in allImages)
            {
                // 非アクティブな要素のRaycastTargetを無効化
                if (!img.gameObject.activeInHierarchy)
                {
                    img.raycastTarget = false;
                }
            }
            
            // 全てのTextMeshProコンポーネントをチェック
            TextMeshProUGUI[] allTexts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in allTexts)
            {
                // 非アクティブな要素のRaycastTargetを無効化
                if (!text.gameObject.activeInHierarchy)
                {
                    text.raycastTarget = false;
                }
            }
            
            Debug.Log("✅ RaycastTargets disabled on inactive elements");
        }
        
        [ContextMenu("Fix Button States")]
        public void FixButtonStates()
        {
            Debug.Log("=== Fixing Button States ===");
            
            // 次のターンボタンの修正
            if (nextTurnButton != null)
            {
                nextTurnButton.interactable = true;
                var image = nextTurnButton.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                    Debug.Log("✅ Next Turn Button fixed");
                }
            }
            
            // リセットボタンの修正
            if (resetButton != null)
            {
                resetButton.interactable = true;
                var image = resetButton.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                    Debug.Log("✅ Reset Button fixed");
                }
            }
            
            // 戦闘開始ボタンの修正（ある場合）
            if (startBattleButton != null)
            {
                startBattleButton.interactable = true;
                var image = startBattleButton.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                    Debug.Log("✅ Start Battle Button fixed");
                }
            }
            
            Debug.Log("✅ Button States Fix Complete!");
        }
        
        [ContextMenu("Debug UI State")]
        public void DebugUIState()
        {
            Debug.Log("=== UI State Debug Info ===");
            
            // EventSystem情報
            var eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            Debug.Log($"EventSystem: {(eventSystem != null ? $"Found ({eventSystem.enabled})" : "Not Found")}");
            
            // Canvas情報
            if (canvas != null)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                Debug.Log($"Canvas: RenderMode={canvas.renderMode}, SortingOrder={canvas.sortingOrder}");
                Debug.Log($"GraphicRaycaster: {(raycaster != null ? $"Found ({raycaster.enabled})" : "Not Found")}");
            }
            
            // ボタン情報
            if (nextTurnButton != null)
            {
                var image = nextTurnButton.GetComponent<Image>();
                Debug.Log($"Next Turn Button: Interactable={nextTurnButton.interactable}, RaycastTarget={image?.raycastTarget}");
            }
            
            if (resetButton != null)
            {
                var image = resetButton.GetComponent<Image>();
                Debug.Log($"Reset Button: Interactable={resetButton.interactable}, RaycastTarget={image?.raycastTarget}");
            }
            
            // 戦闘状態
            Debug.Log($"Battle Started: {isBattleStarted}");
            Debug.Log($"Battle Manager State: {battleManager?.CurrentState}");
            
            // 手札システム情報
            HandUI handUI = FindObjectOfType<HandUI>();
            Debug.Log($"HandUI: {(handUI != null ? "Found" : "Not Found")}");
            
            if (handSystem != null)
            {
                Debug.Log($"HandSystem State: {handSystem.CurrentHandState}");
                Debug.Log($"Hand Count: {handSystem.CurrentHand?.Length ?? 0}");
                Debug.Log($"Can Take Action: {handSystem.CanTakeAction}");
            }
            else
            {
                Debug.Log("HandSystem: Not Found");
            }
            
            Debug.Log("✅ UI State Debug Complete!");
        }
        
        /// <summary>
        /// 手札表示を強制実行（新規追加 - デバッグ用）
        /// </summary>
        [ContextMenu("Force Show Hand UI")]
        public void ForceShowHandUI()
        {
            Debug.Log("=== Force Show Hand UI ===");
            
            ShowHandUI();
            
            // 手札生成も強制実行
            if (handSystem != null)
            {
                handSystem.GenerateHand();
                if (handSystem.CurrentHand != null && handSystem.CurrentHand.Length > 0)
                {
                    Debug.Log($"✓ Hand force generated: {handSystem.CurrentHand.Length} cards");
                }
                else
                {
                    Debug.LogWarning("❌ Hand force generation failed");
                }
            }
            
            // 手札UI強制更新
            HandUI handUI = FindObjectOfType<HandUI>();
            if (handUI != null)
            {
                handUI.SetHandUIVisible(true);
                handUI.ForceUpdateHandDisplay();
                handUI.ForceRefreshAll();
                Debug.Log("✓ Hand UI force refreshed");
            }
            else
            {
                Debug.LogWarning("❌ HandUI not found for force refresh");
            }
            
            Debug.Log("✅ Force Show Hand UI Complete!");
        }

        /// <summary>
        /// 遅延HandUI確認コルーチン
        /// </summary>
        private System.Collections.IEnumerator DelayedHandUICheck()
        {
            yield return new WaitForSeconds(0.5f);
            
            HandUI handUI = FindObjectOfType<HandUI>();
            if (handUI != null)
            {
                handUI.SetHandUIVisible(true);
                Debug.Log("✓ DelayedHandUICheck: HandUI found and shown");
            }
            else
            {
                Debug.LogWarning("❌ DelayedHandUICheck: HandUI still not found");
            }
        }

        /// <summary>
        /// 遅延手札生成コルーチン
        /// </summary>
        private System.Collections.IEnumerator DelayedHandGeneration()
        {
            yield return new WaitForSeconds(1.0f);
            
            HandSystem handSystem = FindObjectOfType<HandSystem>();
            if (handSystem != null)
            {
                Debug.Log("✓ DelayedHandGeneration: Attempting hand generation...");
                handSystem.GenerateHand();
                
                if (handSystem.CurrentHand != null && handSystem.CurrentHand.Length > 0)
                {
                    Debug.Log($"✓ DelayedHandGeneration: Hand generated successfully! Cards: {handSystem.CurrentHand.Length}");
                }
                else
                {
                    Debug.LogWarning("❌ DelayedHandGeneration: Hand generation failed");
                }
            }
            else
            {
                Debug.LogWarning("❌ DelayedHandGeneration: HandSystem still not found");
            }
        }

        /// <summary>
        /// 勝利後にアタッチメント選択画面を表示します
        /// </summary>
        private void ShowAttachmentSelectionAfterVictory()
        {
            Debug.Log("勝利後のアタッチメント選択画面を表示中...");
            
            // アタッチメント選択UIを探す
            AttachmentSelectionUI selectionUI = FindObjectOfType<AttachmentSelectionUI>();
            if (selectionUI != null)
            {
                Debug.Log("AttachmentSelectionUI found! 選択画面を表示します");
                selectionUI.ShowSelectionScreen();
                Debug.Log("アタッチメント選択画面を表示しました");
            }
            else
            {
                Debug.LogWarning("AttachmentSelectionUI not found! 動的にUI要素を作成します");
                CreateAttachmentSelectionUI();
            }
        }

        /// <summary>
        /// アタッチメント選択UIを動的に作成します
        /// </summary>
        private void CreateAttachmentSelectionUI()
        {
            Debug.Log("Creating AttachmentSelectionUI dynamically...");
            
            // AttachmentSystemの確認・作成
            AttachmentSystem attachmentSystem = AttachmentUIBuilder.EnsureAttachmentSystem();
            if (attachmentSystem == null)
            {
                Debug.LogError("Failed to create AttachmentSystem");
                return;
            }

            // 現在のCanvasを使用
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    Debug.LogError("Canvas not found! Cannot create AttachmentSelectionUI");
                    return;
                }
            }

            // UIBuilderを使用してUIを作成
            AttachmentSelectionUI selectionUI = AttachmentUIBuilder.CreateAttachmentSelectionUI(canvas);
            if (selectionUI == null)
            {
                Debug.LogError("Failed to create AttachmentSelectionUI");
                return;
            }
            
            // 作成したUIで選択画面を表示
            selectionUI.ShowSelectionScreen();
        }
        
        /// <summary>
        /// AttachmentSystemとの接続を設定
        /// </summary>
        private void SetupAttachmentSystemConnection()
        {
            var attachmentSystem = battleManager.GetComponent<AttachmentSystem>();
            if (attachmentSystem == null)
            {
                Debug.LogWarning("AttachmentSystem not found on BattleManager");
                return;
            }
            
            // PlayMode開始時のアタッチメント表示イベントを購読
            attachmentSystem.OnPlayModeAttachmentsDisplayRequested += DisplayEquippedAttachmentsInUI;
            
            Debug.Log("✅ AttachmentSystem connected to SimpleBattleUI");
        }
        
        /// <summary>
        /// UI上に装備中のアタッチメントを表示
        /// </summary>
        private void DisplayEquippedAttachmentsInUI(List<AttachmentData> equippedAttachments)
        {
            if (equippedAttachments == null || equippedAttachments.Count == 0)
            {
                Debug.Log("📋 装備中アタッチメント: なし");
                UpdateAttachmentComboDisplay(new List<AttachmentData>());
                return;
            }
            
            Debug.Log($"📋 UI表示 - 装備中アタッチメント: {equippedAttachments.Count}個");
            
            // UI要素を更新
            UpdateAttachmentComboDisplay(equippedAttachments);
            
            // コンソール表示も継続
            foreach (var attachment in equippedAttachments)
            {
                Debug.Log($"🎯 UI: {attachment.attachmentName} ({attachment.rarity})");
            }
        }
        
        /// <summary>
        /// 戦闘画面にアタッチメント・コンボ情報を表示
        /// </summary>
        private void UpdateAttachmentComboDisplay(List<AttachmentData> equippedAttachments)
        {
            // 既存のアタッチメント・コンボ表示エリアを更新または作成
            var comboDisplayArea = battleUIGroup.transform.Find("ComboDisplayArea");
            if (comboDisplayArea == null)
            {
                comboDisplayArea = CreateComboDisplayArea();
            }
            
            var comboText = comboDisplayArea.GetComponentInChildren<UnityEngine.UI.Text>();
            if (comboText == null)
            {
                Debug.LogWarning("ComboDisplayArea内にTextコンポーネントが見つかりません");
                return;
            }
            
            // アタッチメントとコンボ情報を文字列として構築
            System.Text.StringBuilder comboInfo = new System.Text.StringBuilder();
            comboInfo.AppendLine("=== 装備アタッチメント ===");
            
            if (equippedAttachments.Count == 0)
            {
                comboInfo.AppendLine("なし");
            }
            else
            {
                foreach (var attachment in equippedAttachments)
                {
                    string rarityIcon = GetRarityIcon(attachment.rarity);
                    string comboName = !string.IsNullOrEmpty(attachment.associatedComboName) 
                        ? attachment.associatedComboName 
                        : "未設定";
                    
                    comboInfo.AppendLine($"{rarityIcon} {attachment.attachmentName}");
                    comboInfo.AppendLine($"  コンボ: {comboName}");
                    
                    // エフェクト情報も表示
                    if (attachment.effects != null && attachment.effects.Length > 0)
                    {
                        foreach (var effect in attachment.effects)
                        {
                            comboInfo.AppendLine($"  効果: {GetEffectDescription(effect)}");
                        }
                    }
                    comboInfo.AppendLine();
                }
            }
            
            // UIテキストを更新
            comboText.text = comboInfo.ToString();
        }
        
        /// <summary>
        /// コンボ表示エリアを作成
        /// </summary>
        Transform CreateComboDisplayArea()
        {
            // コンボ表示用のパネルを作成
            GameObject comboPanel = new GameObject("ComboDisplayArea");
            comboPanel.transform.SetParent(battleUIGroup.transform, false);
            
            // RectTransformの設定（左上に配置）
            RectTransform rectTransform = comboPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -10);
            rectTransform.sizeDelta = new Vector2(300, 400);
            
            // 背景画像（半透明の黒）
            var image = comboPanel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0, 0, 0, 0.7f);
            
            // テキスト表示用のGameObjectを作成
            GameObject textObj = new GameObject("ComboText");
            textObj.transform.SetParent(comboPanel.transform, false);
            
            // テキストコンポーネントの設定
            var text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.text = "コンボ情報読み込み中...";
            
            // テキストのRectTransform設定
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            Debug.Log("✅ コンボ表示エリアを作成しました");
            return comboPanel.transform;
        }
        
        /// <summary>
        /// レアリティアイコンを取得
        /// </summary>
        string GetRarityIcon(AttachmentRarity rarity)
        {
            return rarity switch
            {
                AttachmentRarity.Common => "⚪",
                AttachmentRarity.Rare => "🔵", 
                AttachmentRarity.Epic => "🟣",
                AttachmentRarity.Legendary => "🟡",
                _ => "❔"
            };
        }
        
        /// <summary>
        /// エフェクト説明を生成
        /// </summary>
        string GetEffectDescription(AttachmentEffect effect)
        {
            string baseDesc = effect.effectType switch
            {
                AttachmentEffectType.AttackPowerBoost => $"攻撃力+{(effect.effectValue * 100):F0}%",
                AttachmentEffectType.MaxHpBoost => $"最大HP+{(effect.effectValue * 100):F0}%",
                AttachmentEffectType.CriticalRateBoost => $"クリティカル率+{(effect.effectValue * 100):F0}%",
                AttachmentEffectType.WeaponPowerBoost => $"武器攻撃力+{(effect.effectValue * 100):F0}%",
                AttachmentEffectType.CooldownReduction => $"クールダウン-{effect.flatValue}ターン",
                _ => effect.effectType.ToString()
            };
            
            return baseDesc;
        }
        
        /// <summary>
        /// コンボ表示エリアの初期化
        /// </summary>
        private void InitializeComboDisplayArea()
        {
            if (battleUIGroup == null)
            {
                Debug.LogWarning("battleUIGroupが見つかりません。コンボ表示エリアの初期化をスキップします。");
                return;
            }
            
            // 既存のコンボ表示エリアをチェック
            var existingComboArea = battleUIGroup.transform.Find("ComboDisplayArea");
            if (existingComboArea == null)
            {
                // 存在しない場合は作成
                CreateComboDisplayArea();
                Debug.Log("🎯 コンボ表示エリアを初期化しました");
            }
            else
            {
                Debug.Log("🎯 既存のコンボ表示エリアを確認しました");
            }
        }
        
        /// <summary>
        /// 装備アタッチメントのコンボ情報を左下コンボ表に表示
        /// </summary>
        private void UpdateEquippedAttachmentCombosDisplay()
        {
            // AttachmentSystemから装備中アタッチメントを取得
            var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
            if (attachmentSystem == null) return;
            
            var equippedAttachments = attachmentSystem.GetAttachedAttachments();
            if (equippedAttachments == null || equippedAttachments.Count == 0) return;
            
            // 装備アタッチメントのコンボ情報を左下コンボ表に表示
            int displayIndex = 0;
            
            for (int i = 0; i < equippedAttachments.Count && displayIndex < comboProgressItems.Length; i++)
            {
                var attachment = equippedAttachments[i];
                
                // コンボが設定されているアタッチメントのみ表示
                if (!string.IsNullOrEmpty(attachment.associatedComboName))
                {
                    if (comboProgressItems[displayIndex] != null)
                    {
                        comboProgressItems[displayIndex].SetActive(true);
                        
                        // コンボ名表示
                        if (comboNameTexts[displayIndex] != null)
                        {
                            string rarityIcon = GetRarityIcon(attachment.rarity);
                            comboNameTexts[displayIndex].text = $"{rarityIcon} {attachment.associatedComboName}";
                            comboNameTexts[displayIndex].color = GetRarityColor(attachment.rarity);
                        }
                        
                        // プログレスバー（装備済みなので100%表示）
                        if (comboProgressBars[displayIndex] != null)
                        {
                            comboProgressBars[displayIndex].value = 1.0f; // 装備済み = 100%
                        }
                        
                        // ステップ表示（装備済み状態）
                        if (comboStepTexts[displayIndex] != null)
                        {
                            comboStepTexts[displayIndex].text = "装備済み";
                        }
                        
                        // タイマー表示（アタッチメント名）
                        if (comboTimerTexts[displayIndex] != null)
                        {
                            comboTimerTexts[displayIndex].text = $"From: {attachment.attachmentName}";
                        }
                        
                        // 抵抗値表示（エフェクト詳細）
                        if (comboResistanceTexts[displayIndex] != null)
                        {
                            string effectsDesc = "";
                            if (attachment.effects != null && attachment.effects.Length > 0)
                            {
                                var effect = attachment.effects[0]; // 最初のエフェクトのみ表示
                                effectsDesc = GetEffectDescription(effect);
                            }
                            comboResistanceTexts[displayIndex].text = effectsDesc;
                        }
                        
                        displayIndex++;
                    }
                }
            }
            
            // 使用していないコンボアイテムを非表示
            for (int i = displayIndex; i < comboProgressItems.Length; i++)
            {
                if (comboProgressItems[i] != null)
                {
                    comboProgressItems[i].SetActive(false);
                }
            }
            
            // コンボ進行状況タイトルを更新
            if (comboProgressTitle != null)
            {
                comboProgressTitle.text = $"=== 装備コンボ ({displayIndex}/5) ===";
            }
        }
        
        /// <summary>
        /// レアリティに応じた色を取得
        /// </summary>
        Color GetRarityColor(AttachmentRarity rarity)
        {
            return rarity switch
            {
                AttachmentRarity.Common => Color.white,
                AttachmentRarity.Rare => Color.cyan,
                AttachmentRarity.Epic => Color.magenta,
                AttachmentRarity.Legendary => Color.yellow,
                _ => Color.gray
            };
        }
        
        /// <summary>
        /// 手札UIの継続表示を保証するコルーチン
        /// </summary>
        System.Collections.IEnumerator KeepHandUIVisible(HandUI handUI)
        {
            while (isBattleStarted && handUI != null)
            {
                // 2秒ごとに手札UIの状態をチェック
                yield return new WaitForSeconds(2.0f);
                
                // 手札UIが非表示になっていた場合、再表示
                try
                {
                    bool isVisible = (bool?)handUI.GetType().GetMethod("IsHandUIVisible")?.Invoke(handUI, null) ?? true;
                    if (!isVisible)
                    {
                        Debug.Log("⚠️ Hand UI became invisible, restoring visibility...");
                        handUI.GetType().GetMethod("SetHandUIVisible")?.Invoke(handUI, new object[] { true });
                        handUI.GetType().GetMethod("ForceUpdateHandDisplay")?.Invoke(handUI, null);
                    }
                }
                catch
                {
                    // メソッドが存在しない場合は、手札生成のみ試行
                    Debug.Log("Hand UI visibility check methods not available, focusing on hand generation");
                }
                
                // 手札が空の場合、再生成を試行
                if (handSystem != null && (handSystem.CurrentHand == null || handSystem.CurrentHand.Length == 0))
                {
                    Debug.Log("⚠️ Hand is empty, attempting regeneration...");
                    handSystem.GenerateHand();
                    if (handSystem.CurrentHand != null && handSystem.CurrentHand.Length > 0)
                    {
                        try
                        {
                            handUI.GetType().GetMethod("ForceUpdateHandDisplay")?.Invoke(handUI, null);
                            Debug.Log("✓ Hand regenerated and UI updated");
                        }
                        catch
                        {
                            Debug.Log("✓ Hand regenerated (UI update method not available)");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// コンボグループコンテナの初期化
        /// </summary>
        private void InitializeComboGroupContainer(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("🎯 コンボグループコンテナを初期化中...");
            
            // 全コンボグループの親コンテナを作成
            comboGroupContainer = CreateUIPanel("ComboGroupsContainer", 
                new Vector2(-screenWidth * 0.4f, -screenHeight * 0.35f),
                new Vector2(350 * scale, 400 * scale), 
                new Color(0.05f, 0.05f, 0.2f, 0.6f));
                
            Debug.Log("✅ コンボグループコンテナ初期化完了");
        }
        
        /// <summary>
        /// コンボグループを取得または作成
        /// </summary>
        private ComboGroupContainer GetOrCreateComboGroup(ComboData combo)
        {
            if (comboGroups.TryGetValue(combo.comboName, out ComboGroupContainer existingGroup))
            {
                return existingGroup;
            }
            
            // 新しいコンボグループを作成
            Vector2 position = CalculateComboGroupPosition(comboGroups.Count);
            Vector2 size = new Vector2(320, 80);
            
            ComboGroupContainer newGroup = new ComboGroupContainer(combo, comboGroupContainer.transform, position, size);
            comboGroups.Add(combo.comboName, newGroup);
            
            Debug.Log($"🎯 新しいコンボグループ作成: {combo.comboName}");
            return newGroup;
        }
        
        /// <summary>
        /// コンボグループの表示位置を計算
        /// </summary>
        private Vector2 CalculateComboGroupPosition(int index)
        {
            float yOffset = -index * 85; // 各グループ間の縦間隔
            return new Vector2(5, yOffset - 10);
        }
        
        /// <summary>
        /// コンボグループ表示の更新（新システム）
        /// </summary>
        private void UpdateComboGroupsDisplay()
        {
            if (comboSystem == null) return;
            
            var activeCombos = comboSystem.ActiveCombos;
            
            // 非アクティブなコンボグループを非表示
            foreach (var kvp in comboGroups.ToList())
            {
                bool isActive = activeCombos.Any(c => c.comboData.comboName == kvp.Key);
                if (!isActive)
                {
                    kvp.Value.SetActive(false);
                }
            }
            
            // アクティブなコンボグループを更新・表示
            for (int i = 0; i < activeCombos.Count; i++)
            {
                var progress = activeCombos[i];
                var group = GetOrCreateComboGroup(progress.comboData);
                
                group.SetActive(true);
                group.UpdateProgress(progress);
                
                // 位置を再計算（動的な並び替え）
                Vector2 newPosition = CalculateComboGroupPosition(i);
                if (group.parentObject != null)
                {
                    RectTransform rect = group.parentObject.GetComponent<RectTransform>();
                    rect.anchoredPosition = newPosition;
                }
            }
            
            Debug.Log($"🎯 コンボグループ表示更新完了: {activeCombos.Count}個のアクティブコンボ");
        }
        
        /// <summary>
        /// アクティブなコンボグループを更新
        /// </summary>
        private void UpdateActiveComboGroups(List<ComboProgress> activeCombos)
        {
            Debug.Log($"🎯 アクティブコンボグループ更新: {activeCombos.Count}個");
            
            // すべてのグループを一旦非表示
            foreach (var group in comboGroups.Values)
            {
                group.SetActive(false);
            }
            
            // アクティブなコンボのグループを表示・更新
            for (int i = 0; i < activeCombos.Count && i < 5; i++)
            {
                var progress = activeCombos[i];
                var group = GetOrCreateComboGroup(progress.comboData);
                
                group.SetActive(true);
                group.UpdateProgress(progress);
                
                // 位置を再計算（動的な並び替え）
                Vector2 newPosition = CalculateComboGroupPosition(i);
                if (group.parentObject != null)
                {
                    RectTransform rect = group.parentObject.GetComponent<RectTransform>();
                    rect.anchoredPosition = newPosition;
                }
            }
            
            // タイトル更新
            if (comboProgressTitle != null)
            {
                comboProgressTitle.text = $"=== コンボ進行 ({activeCombos.Count}/5) ===";
            }
        }
        
        /// <summary>
        /// コンボ開始時の処理（新システム）
        /// </summary>
        private void HandleComboStartedNew(ComboData comboData)
        {
            Debug.Log($"🎯 コンボ開始（新システム）: {comboData.comboName}");
            
            var group = GetOrCreateComboGroup(comboData);
            group.SetActive(true);
            
            // 初期状態設定
            if (group.statusText != null)
                group.statusText.text = "開始!";
                
            if (group.nameText != null)
                group.nameText.color = Color.green;
        }
        
        /// <summary>
        /// コンボ完了時の処理（新システム）
        /// </summary>
        private void HandleComboCompletedNew(ComboExecutionResult result)
        {
            Debug.Log($"🎯 コンボ完了（新システム）: {result.executedCombo.comboName}");
            
            if (comboGroups.TryGetValue(result.executedCombo.comboName, out ComboGroupContainer group))
            {
                if (group.statusText != null)
                {
                    group.statusText.text = "完成!";
                    group.statusText.color = Color.gold;
                }
                
                if (group.nameText != null)
                    group.nameText.color = Color.yellow;
                    
                // 完成エフェクト（一定時間後に非表示）
                StartCoroutine(HideCompletedComboAfterDelay(group, 3f));
            }
        }
        
        /// <summary>
        /// コンボ進行更新時の処理（新システム）
        /// </summary>
        private void HandleComboProgressUpdatedNew(ComboProgress progress)
        {
            Debug.Log($"🎯 コンボ進行更新（新システム）: {progress.comboData.comboName} - {progress.progressPercentage:P0}");
            
            if (comboGroups.TryGetValue(progress.comboData.comboName, out ComboGroupContainer group))
            {
                group.UpdateProgress(progress);
                
                if (group.statusText != null)
                {
                    group.statusText.text = $"{progress.currentStep}/{progress.totalSteps}";
                    group.statusText.color = Color.cyan;
                }
            }
        }
        
        /// <summary>
        /// コンボ失敗時の処理（新システム）
        /// </summary>
        private void HandleComboFailedNew(ComboData comboData, string reason)
        {
            Debug.Log($"🎯 コンボ失敗（新システム）: {comboData.comboName} - {reason}");
            
            if (comboGroups.TryGetValue(comboData.comboName, out ComboGroupContainer group))
            {
                if (group.statusText != null)
                {
                    group.statusText.text = "失敗";
                    group.statusText.color = Color.red;
                }
                
                if (group.nameText != null)
                    group.nameText.color = Color.red;
                    
                // 失敗表示後に非表示
                StartCoroutine(HideCompletedComboAfterDelay(group, 2f));
            }
        }
        
        /// <summary>
        /// コンボ中断時の処理（新システム）
        /// </summary>
        private void HandleComboInterruptedNew(ComboData comboData)
        {
            Debug.Log($"🎯 コンボ中断（新システム）: {comboData.comboName}");
            
            if (comboGroups.TryGetValue(comboData.comboName, out ComboGroupContainer group))
            {
                if (group.statusText != null)
                {
                    group.statusText.text = "中断";
                    group.statusText.color = Color.orange;
                }
                
                if (group.nameText != null)
                    group.nameText.color = Color.orange;
                    
                // 中断表示後に非表示
                StartCoroutine(HideCompletedComboAfterDelay(group, 2f));
            }
        }
        
        /// <summary>
        /// 完了したコンボグループを遅延後に非表示
        /// </summary>
        private System.Collections.IEnumerator HideCompletedComboAfterDelay(ComboGroupContainer group, float delay)
        {
            yield return new WaitForSeconds(delay);
            group.SetActive(false);
        }
        
        // =============================================================================
        // コンボテスト機能
        // =============================================================================
        
        /// <summary>
        /// コンボ進行をテストする（デバッグ用）
        /// </summary>
        public void TestComboProgress()
        {
            if (comboSystem == null)
            {
                Debug.LogWarning("ComboSystemが見つかりません");
                return;
            }
            
            Debug.Log("🎯 コンボテスト開始");
            
            // テスト用コンボデータを作成または既存のコンボを進行
            if (comboSystem.ActiveCombos == null || comboSystem.ActiveCombos.Count == 0)
            {
                CreateTestCombo();
            }
            else
            {
                AdvanceExistingCombo();
            }
        }
        
        /// <summary>
        /// テスト用コンボを作成
        /// </summary>
        private void CreateTestCombo()
        {
            // テスト用のコンボデータを作成
            ComboData testCombo = CreateTestComboData();
            
            // 手動でコンボ進行状況を作成
            ComboProgress testProgress = new ComboProgress
            {
                comboData = testCombo,
                usedWeaponIndices = new List<int> { 0 },
                usedAttackAttributes = new List<AttackAttribute> { AttackAttribute.Fire },
                usedWeaponTypes = new List<WeaponType> { WeaponType.Sword },
                currentStep = 1,
                totalSteps = testCombo.requiredWeaponCount,
                startTurn = battleManager != null ? battleManager.CurrentTurn : 1,
                startTime = Time.time,
                isActive = true,
                isCompleted = false,
                progressPercentage = 1.0f / testCombo.requiredWeaponCount
            };
            
            // ComboSystemの内部リストに直接追加（テスト用）
            if (comboSystem.ActiveCombos != null)
            {
                comboSystem.ActiveCombos.Add(testProgress);
                
                // UI更新を直接呼び出し
                OnComboStarted(testCombo);
                OnComboProgressUpdated(testProgress);
                
                Debug.Log($"テストコンボ作成: {testCombo.comboName} - 進行率: {testProgress.progressPercentage:P0}");
            }
        }
        
        /// <summary>
        /// 既存のコンボを進行させる
        /// </summary>
        private void AdvanceExistingCombo()
        {
            if (comboSystem.ActiveCombos.Count > 0)
            {
                ComboProgress progress = comboSystem.ActiveCombos[0];
                
                if (!progress.isCompleted && progress.currentStep < progress.totalSteps)
                {
                    // コンボを1ステップ進める
                    progress.currentStep++;
                    progress.progressPercentage = (float)progress.currentStep / progress.totalSteps;
                    
                    // 武器使用データを追加（テスト用）
                    progress.usedWeaponIndices.Add(progress.currentStep - 1);
                    progress.usedAttackAttributes.Add(AttackAttribute.Fire);
                    progress.usedWeaponTypes.Add(WeaponType.Sword);
                    
                    Debug.Log($"コンボ進行: {progress.comboData.comboName} - ステップ: {progress.currentStep}/{progress.totalSteps}");
                    
                    // 進行更新UI呼び出し
                    OnComboProgressUpdated(progress);
                    
                    // コンボ完成チェック
                    if (progress.currentStep >= progress.totalSteps)
                    {
                        progress.isCompleted = true;
                        
                        // コンボ完成UI呼び出し
                        ComboExecutionResult result = new ComboExecutionResult
                        {
                            wasExecuted = true,
                            executedCombo = progress.comboData,
                            appliedEffects = progress.comboData.effects,
                            additionalActionsGranted = 1,
                            totalDamageMultiplier = 1.5f,
                            resultMessage = "テストコンボ完成！"
                        };
                        
                        OnComboCompleted(result);
                        comboSystem.ActiveCombos.RemoveAt(0);
                        
                        Debug.Log($"🎉 コンボ完成: {progress.comboData.comboName}");
                    }
                }
                else
                {
                    Debug.Log("既存のコンボはすでに完成しています。新しいコンボを作成します。");
                    CreateTestCombo();
                }
            }
        }
        
        /// <summary>
        /// テスト用コンボデータを作成
        /// </summary>
        private ComboData CreateTestComboData()
        {
            ComboData testCombo = new ComboData
            {
                comboName = "テスト炎コンボ",
                requiredWeaponCount = 3,
                requiredWeapons = new string[] { "炎の剣", "炎の槍", "炎の弓" },
                comboDescription = "炎属性武器による連続攻撃",
                canInterrupt = true,
                interruptResistance = 0.2f,
                priority = 1,
                condition = new ComboCondition
                {
                    comboType = ComboType.AttributeCombo,
                    requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Fire },
                    requiredWeaponTypes = new WeaponType[] { WeaponType.Sword, WeaponType.Spear, WeaponType.Bow },
                    minAttackPower = 0,
                    requiresSequence = false,
                    maxTurnInterval = 5,
                    successRate = 0.8f
                },
                effects = new ComboEffect[]
                {
                    new ComboEffect
                    {
                        effectType = ComboEffectType.DamageMultiplier,
                        damageMultiplier = 1.5f,
                        effectDescription = "ダメージ150%"
                    },
                    new ComboEffect
                    {
                        effectType = ComboEffectType.AdditionalAction,
                        additionalActions = 1,
                        effectDescription = "追加行動+1"
                    }
                }
            };
            
            return testCombo;
        }
    }

    /// <summary>
    /// コンボごとの親オブジェクトとUI要素を管理するコンテナ
    /// </summary>
    [System.Serializable]
    public class ComboGroupContainer
    {
        [Header("コンボ情報")]
        public string comboName;
        public ComboData comboData;
        
        [Header("UI要素")]
        public GameObject parentObject;          // コンボグループの親オブジェクト
        public TextMeshProUGUI nameText;        // コンボ名テキスト
        public Slider progressBar;              // 進行バー
        public TextMeshProUGUI stepText;        // ステップ表示テキスト
        public TextMeshProUGUI timerText;       // タイマーテキスト
        public TextMeshProUGUI statusText;      // ステータステキスト
        public GameObject effectsContainer;     // エフェクト表示コンテナ
        
        [Header("表示設定")]
        public Vector2 position;                // 表示位置
        public Vector2 size;                    // サイズ
        public Color backgroundColor;           // 背景色
        public bool isActive;                   // アクティブ状態
        
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="combo">コンボデータ</param>
        /// <param name="parent">親Transform</param>
        /// <param name="pos">表示位置</param>
        /// <param name="containerSize">コンテナサイズ</param>
        public ComboGroupContainer(ComboData combo, Transform parent, Vector2 pos, Vector2 containerSize)
        {
            comboName = combo.comboName;
            comboData = combo;
            position = pos;
            size = containerSize;
            backgroundColor = GetComboTypeColor(combo);
            isActive = false;
            
            CreateComboGroup(parent);
        }
        
        /// <summary>
        /// コンボグループのUI要素を作成
        /// </summary>
        private void CreateComboGroup(Transform parent)
        {
            // 親オブジェクト作成
            parentObject = new GameObject($"ComboGroup_{comboName}");
            parentObject.transform.SetParent(parent, false);
            
            // RectTransform設定
            RectTransform parentRect = parentObject.AddComponent<RectTransform>();
            parentRect.anchoredPosition = position;
            parentRect.sizeDelta = size;
            
            // 背景パネル
            UnityEngine.UI.Image bgImage = parentObject.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = backgroundColor;
            
            CreateChildUIElements();
        }
        
        /// <summary>
        /// 子UI要素を作成
        /// </summary>
        private void CreateChildUIElements()
        {
            // コンボ名テキスト
            GameObject nameObj = new GameObject("ComboName");
            nameObj.transform.SetParent(parentObject.transform, false);
            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = comboName;
            nameText.fontSize = 14;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.TopLeft;
            
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.7f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(5, 0);
            nameRect.offsetMax = new Vector2(-5, -5);
            
            // 進行バー
            CreateProgressBar();
            
            // ステップテキスト
            CreateStepText();
            
            // タイマーテキスト
            CreateTimerText();
            
            // ステータステキスト
            CreateStatusText();
            
            // エフェクトコンテナ
            CreateEffectsContainer();
        }
        
        /// <summary>
        /// 進行バーを作成
        /// </summary>
        private void CreateProgressBar()
        {
            GameObject sliderObj = new GameObject("ProgressBar");
            sliderObj.transform.SetParent(parentObject.transform, false);
            
            progressBar = sliderObj.AddComponent<Slider>();
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
            
            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            UnityEngine.UI.Image bgImage = bg.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = Color.green;
            
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            // Sliderの設定
            progressBar.fillRect = fillRect;
            
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.4f);
            sliderRect.anchorMax = new Vector2(1, 0.6f);
            sliderRect.offsetMin = new Vector2(5, 0);
            sliderRect.offsetMax = new Vector2(-5, 0);
        }
        
        /// <summary>
        /// ステップテキストを作成
        /// </summary>
        private void CreateStepText()
        {
            GameObject stepObj = new GameObject("StepText");
            stepObj.transform.SetParent(parentObject.transform, false);
            stepText = stepObj.AddComponent<TextMeshProUGUI>();
            stepText.text = "0/0";
            stepText.fontSize = 10;
            stepText.color = Color.cyan;
            stepText.alignment = TextAlignmentOptions.Center;
            
            RectTransform stepRect = stepObj.GetComponent<RectTransform>();
            stepRect.anchorMin = new Vector2(0, 0.2f);
            stepRect.anchorMax = new Vector2(0.5f, 0.4f);
            stepRect.offsetMin = new Vector2(5, 0);
            stepRect.offsetMax = new Vector2(-2, 0);
        }
        
        /// <summary>
        /// タイマーテキストを作成
        /// </summary>
        private void CreateTimerText()
        {
            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(parentObject.transform, false);
            timerText = timerObj.AddComponent<TextMeshProUGUI>();
            timerText.text = "";
            timerText.fontSize = 8;
            timerText.color = Color.yellow;
            timerText.alignment = TextAlignmentOptions.Center;
            
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 0.2f);
            timerRect.anchorMax = new Vector2(1, 0.4f);
            timerRect.offsetMin = new Vector2(2, 0);
            timerRect.offsetMax = new Vector2(-5, 0);
        }
        
        /// <summary>
        /// ステータステキストを作成
        /// </summary>
        private void CreateStatusText()
        {
            GameObject statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(parentObject.transform, false);
            statusText = statusObj.AddComponent<TextMeshProUGUI>();
            statusText.text = "待機中";
            statusText.fontSize = 9;
            statusText.color = Color.gray;
            statusText.alignment = TextAlignmentOptions.Center;
            
            RectTransform statusRect = statusObj.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1, 0.2f);
            statusRect.offsetMin = new Vector2(5, 0);
            statusRect.offsetMax = new Vector2(-5, 0);
        }
        
        /// <summary>
        /// エフェクトコンテナを作成
        /// </summary>
        private void CreateEffectsContainer()
        {
            effectsContainer = new GameObject("EffectsContainer");
            effectsContainer.transform.SetParent(parentObject.transform, false);
            
            RectTransform effectsRect = effectsContainer.AddComponent<RectTransform>();
            effectsRect.anchorMin = Vector2.zero;
            effectsRect.anchorMax = Vector2.one;
            effectsRect.offsetMin = Vector2.zero;
            effectsRect.offsetMax = Vector2.zero;
        }
        
        /// <summary>
        /// コンボタイプに応じた色を取得
        /// </summary>
        private Color GetComboTypeColor(ComboData combo)
        {
            // 武器タイプやエフェクトに応じて色を決定
            if (combo.condition.requiredAttackAttributes != null && combo.condition.requiredAttackAttributes.Length > 0)
            {
                var firstAttribute = combo.condition.requiredAttackAttributes[0];
                switch (firstAttribute)
                {
                    case AttackAttribute.Fire:
                        return new Color(0.8f, 0.2f, 0.2f, 0.7f); // 赤系
                    case AttackAttribute.Ice:
                        return new Color(0.2f, 0.5f, 0.8f, 0.7f); // 青系
                    case AttackAttribute.Thunder:
                        return new Color(0.8f, 0.8f, 0.2f, 0.7f); // 黄系
                    case AttackAttribute.Wind:
                        return new Color(0.2f, 0.8f, 0.5f, 0.7f); // 緑系
                    case AttackAttribute.Light:
                        return new Color(0.9f, 0.9f, 0.9f, 0.7f); // 白系
                    case AttackAttribute.Dark:
                        return new Color(0.4f, 0.2f, 0.6f, 0.7f); // 紫系
                    default:
                        return new Color(0.3f, 0.3f, 0.3f, 0.7f); // グレー系
                }
            }
            return new Color(0.2f, 0.2f, 0.4f, 0.7f); // デフォルト
        }
        
        /// <summary>
        /// コンボの進行状況を更新
        /// </summary>
        public void UpdateProgress(ComboProgress progress)
        {
            if (progressBar != null)
                progressBar.value = progress.progressPercentage;
            
            if (stepText != null)
                stepText.text = $"{progress.currentStep}/{progress.comboData.requiredWeaponCount}";
            
            if (statusText != null)
            {
                if (progress.progressPercentage >= 1.0f)
                    statusText.text = "完成!";
                else if (progress.progressPercentage > 0)
                    statusText.text = "進行中";
                else
                    statusText.text = "待機中";
            }
            
            UpdateTimerDisplay(progress);
        }
        
        /// <summary>
        /// タイマー表示を更新
        /// </summary>
        private void UpdateTimerDisplay(ComboProgress progress)
        {
            if (timerText == null) return;
            
            if (progress.comboData.condition.maxTurnInterval > 0)
            {
                int remainingTurns = progress.comboData.condition.maxTurnInterval - 
                                   (Time.time - progress.startTime > 0 ? 1 : 0);
                if (remainingTurns > 0)
                {
                    timerText.text = $"残り{remainingTurns}T";
                    timerText.color = remainingTurns <= 2 ? Color.red : Color.yellow;
                }
                else
                {
                    timerText.text = "期限切れ";
                    timerText.color = Color.red;
                }
            }
            else
            {
                timerText.text = "";
            }
        }
        
        /// <summary>
        /// コンボグループの表示/非表示を切り替え
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            if (parentObject != null)
                parentObject.SetActive(active);
        }
        
        /// <summary>
        /// コンボグループを破棄
        /// </summary>
        public void Destroy()
        {
            if (parentObject != null)
                UnityEngine.Object.Destroy(parentObject);
        }
    }
    
    // === システム連携メソッド ===
    
    /// <summary>
    /// システム参照の初期化
    /// </summary>
    private void InitializeSystemReferences()
    {
        sceneTransition = SceneTransitionManager.Instance;
        playerDataManager = PlayerDataManager.Instance;
        eventManager = GameEventManager.Instance;
        
        if (sceneTransition == null)
        {
            Debug.LogWarning("[SimpleBattleUI] SceneTransitionManager not found!");
        }
        
        if (playerDataManager == null)
        {
            Debug.LogWarning("[SimpleBattleUI] PlayerDataManager not found!");
        }
        
        // 戦闘終了イベントの購読
        if (eventManager != null)
        {
            GameEventManager.OnBattleCompleted += OnBattleCompleted;
        }
    }
    
    /// <summary>
    /// 戦闘終了処理
    /// </summary>
    /// <param name="victory">勝利フラグ</param>
    public void OnBattleComplete(bool victory)
    {
        Debug.Log($"[SimpleBattleUI] Battle completed: Victory={victory}");
        
        // 戦闘結果データを作成
        var battleResult = new BattleResult
        {
            victory = victory,
            turnsTaken = GetCurrentTurn(),
            timeTaken = Time.time,
            damageDealt = GetTotalDamageDealt(),
            damageTaken = GetTotalDamageTaken(),
            experienceGained = victory ? 100 : 0,
            goldGained = victory ? 50 : 0,
            itemsObtained = victory ? new List<string> { "ポーション" } : new List<string>(),
            completionTime = DateTime.Now
        };
        
        // イベント発火
        GameEventManager.TriggerBattleComplete(victory, battleResult);
    }
    
    /// <summary>
    /// 戦闘終了イベントハンドラー
    /// </summary>
    private void OnBattleCompleted(bool victory, BattleResult result)
    {
        var resultUI = FindObjectOfType<ResultUI>();
        if (resultUI != null)
        {
            resultUI.ShowResult(result);
        }
        else if (sceneTransition != null)
        {
            sceneTransition.TransitionToScene("StageSelectionScene");
        }
    }
    
    // ユーティリティメソッド
    private int GetCurrentTurn() => 1;
    private int GetTotalDamageDealt() => 1000;
    private int GetTotalDamageTaken() => 200;
}
