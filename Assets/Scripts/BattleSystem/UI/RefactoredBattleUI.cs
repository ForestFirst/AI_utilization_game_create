using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem.UI
{
    /// <summary>
    /// リファクタリング済み戦闘UIコントローラー
    /// 各UI管理クラスを統合し、全体の制御を行う
    /// </summary>
    public class RefactoredBattleUI : MonoBehaviour
    {
        [Header("UI Manager Settings")]
        [SerializeField] private bool autoCreateUI = true;
        [SerializeField] private bool useStaticReferences = false;

        [Header("Static UI References")]
        [SerializeField] private GameObject staticTurnText;
        [SerializeField] private GameObject staticHpText;
        [SerializeField] private GameObject staticStateText;
        [SerializeField] private GameObject staticNextTurnButton;
        [SerializeField] private GameObject staticResetButton;

        // UI管理クラス
        private BattleUILayoutManager layoutManager;
        private BattleComboUIManager comboUIManager;
        private BattleEnemyInfoUI enemyInfoUI;

        // システム参照
        private Canvas canvas;
        private BattleManager battleManager;
        private HandSystem handSystem;
        private ComboSystem comboSystem;

        // 基本UI要素
        private TextMeshProUGUI turnText;
        private TextMeshProUGUI hpText;
        private TextMeshProUGUI stateText;
        private TextMeshProUGUI pendingDamageText;
        private Button nextTurnButton;
        private Button resetButton;
        private Button comboTestButton;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeCanvas();
            InitializeUIManagers();
            FindSystemReferences();
        }

        private void Start()
        {
            if (autoCreateUI)
            {
                CreateMainUI();
            }
            else
            {
                SetupStaticReferences();
            }

            InitializeUIManagers();
            SubscribeToBattleEvents();
        }

        private void Update()
        {
            UpdateUI();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBattleEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Canvasの初期化
        /// </summary>
        private void InitializeCanvas()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogError("[RefactoredBattleUI] Canvas not found!");
            }
        }

        /// <summary>
        /// UI管理クラスの初期化
        /// </summary>
        private void InitializeUIManagers()
        {
            // レイアウトマネージャー
            if (layoutManager == null)
            {
                layoutManager = gameObject.AddComponent<BattleUILayoutManager>();
                layoutManager.Initialize();
            }

            // コンボUIマネージャー
            if (comboUIManager == null)
            {
                comboUIManager = gameObject.AddComponent<BattleComboUIManager>();
            }

            // 敵情報UI
            if (enemyInfoUI == null)
            {
                enemyInfoUI = gameObject.AddComponent<BattleEnemyInfoUI>();
            }
        }

        /// <summary>
        /// システム参照の取得
        /// </summary>
        private void FindSystemReferences()
        {
            if (battleManager == null)
            {
                battleManager = FindObjectOfType<BattleManager>();
            }

            if (handSystem == null)
            {
                handSystem = FindObjectOfType<HandSystem>();
            }

            if (comboSystem == null)
            {
                comboSystem = FindObjectOfType<ComboSystem>();
            }
        }

        /// <summary>
        /// UI管理クラスの初期化（システム参照取得後）
        /// </summary>
        private void InitializeUIManagersWithReferences()
        {
            if (comboUIManager != null && comboSystem != null)
            {
                comboUIManager.Initialize(layoutManager, comboSystem);
            }

            if (enemyInfoUI != null && battleManager != null)
            {
                enemyInfoUI.Initialize(layoutManager, battleManager);
            }
        }

        #endregion

        #region UI Creation

        /// <summary>
        /// メインUIの作成
        /// </summary>
        private void CreateMainUI()
        {
            CreateBasicInfoTexts();
            CreateControlButtons();
            CreateComboTestButton();

            // システム参照が取得できたらUI管理クラスを初期化
            if (battleManager != null || comboSystem != null)
            {
                InitializeUIManagersWithReferences();
            }
        }

        /// <summary>
        /// 基本情報テキストの作成
        /// </summary>
        private void CreateBasicInfoTexts()
        {
            // ターン表示
            turnText = layoutManager.CreateUIText(
                "Turn Text",
                "ターン: 0",
                null,
                new Vector2(-300, 200),
                new Vector2(150, 30)
            );

            // HP表示
            hpText = layoutManager.CreateUIText(
                "HP Text",
                "HP: 100/100",
                null,
                new Vector2(-300, 160),
                new Vector2(150, 30)
            );

            // 状態表示
            stateText = layoutManager.CreateUIText(
                "State Text",
                "状態: 準備完了",
                null,
                new Vector2(-300, 120),
                new Vector2(150, 30)
            );

            // 保留ダメージ表示
            pendingDamageText = layoutManager.CreateUIText(
                "Pending Damage Text",
                "保留ダメージ: 0",
                null,
                new Vector2(-300, 80),
                new Vector2(150, 30)
            );
        }

        /// <summary>
        /// 制御ボタンの作成
        /// </summary>
        private void CreateControlButtons()
        {
            // 次ターンボタン
            nextTurnButton = layoutManager.CreateUIButton(
                "Next Turn Button",
                "次のターン",
                null,
                new Vector2(-300, -150),
                new Vector2(120, 40)
            );
            nextTurnButton.onClick.AddListener(OnNextTurnClicked);

            // リセットボタン
            resetButton = layoutManager.CreateUIButton(
                "Reset Button",
                "リセット",
                null,
                new Vector2(-300, -200),
                new Vector2(120, 40)
            );
            resetButton.onClick.AddListener(OnResetClicked);
        }

        /// <summary>
        /// コンボテストボタンの作成
        /// </summary>
        private void CreateComboTestButton()
        {
            comboTestButton = layoutManager.CreateUIButton(
                "Combo Test Button",
                "コンボテスト",
                null,
                new Vector2(-150, -150),
                new Vector2(120, 40)
            );
            comboTestButton.onClick.AddListener(OnComboTestClicked);
        }

        /// <summary>
        /// 静的参照の設定
        /// </summary>
        private void SetupStaticReferences()
        {
            if (staticTurnText != null)
                turnText = staticTurnText.GetComponent<TextMeshProUGUI>();

            if (staticHpText != null)
                hpText = staticHpText.GetComponent<TextMeshProUGUI>();

            if (staticStateText != null)
                stateText = staticStateText.GetComponent<TextMeshProUGUI>();

            if (staticNextTurnButton != null)
            {
                nextTurnButton = staticNextTurnButton.GetComponent<Button>();
                nextTurnButton.onClick.AddListener(OnNextTurnClicked);
            }

            if (staticResetButton != null)
            {
                resetButton = staticResetButton.GetComponent<Button>();
                resetButton.onClick.AddListener(OnResetClicked);
            }
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// バトルイベントの購読
        /// </summary>
        private void SubscribeToBattleEvents()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged += HandleTurnChanged;
                battleManager.OnPlayerHealthChanged += HandlePlayerHealthChanged;
                battleManager.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        /// <summary>
        /// バトルイベントの購読解除
        /// </summary>
        private void UnsubscribeFromBattleEvents()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged -= HandleTurnChanged;
                battleManager.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
                battleManager.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        /// <summary>
        /// ターン変更時のハンドラー
        /// </summary>
        /// <param name="newTurn">新しいターン</param>
        private void HandleTurnChanged(int newTurn)
        {
            if (turnText != null)
            {
                turnText.text = $"ターン: {newTurn}";
            }
        }

        /// <summary>
        /// プレイヤーHP変更時のハンドラー
        /// </summary>
        /// <param name="currentHp">現在のHP</param>
        /// <param name="maxHp">最大HP</param>
        private void HandlePlayerHealthChanged(int currentHp, int maxHp)
        {
            if (hpText != null)
            {
                hpText.text = $"HP: {currentHp}/{maxHp}";
            }
        }

        /// <summary>
        /// ゲーム状態変更時のハンドラー
        /// </summary>
        /// <param name="newState">新しい状態</param>
        private void HandleGameStateChanged(GameState newState)
        {
            if (stateText != null)
            {
                stateText.text = $"状態: {GetGameStateText(newState)}";
            }
        }

        /// <summary>
        /// ゲーム状態のテキスト変換
        /// </summary>
        /// <param name="state">ゲーム状態</param>
        /// <returns>状態テキスト</returns>
        private string GetGameStateText(GameState state)
        {
            return state switch
            {
                GameState.Initializing => "初期化中",
                GameState.Playing => "戦闘中",
                GameState.Paused => "一時停止",
                GameState.Victory => "勝利",
                GameState.Defeat => "敗北",
                GameState.GameOver => "ゲーム終了",
                _ => "不明"
            };
        }

        #endregion

        #region Button Event Handlers

        /// <summary>
        /// 次ターンボタンクリック時の処理
        /// </summary>
        private void OnNextTurnClicked()
        {
            if (battleManager != null)
            {
                battleManager.AdvanceTurn();
            }
        }

        /// <summary>
        /// リセットボタンクリック時の処理
        /// </summary>
        private void OnResetClicked()
        {
            if (battleManager != null)
            {
                battleManager.ResetBattle();
            }

            // UI管理クラスもリセット
            if (comboUIManager != null)
            {
                comboUIManager.ResetAllComboContainers();
            }

            if (enemyInfoUI != null)
            {
                enemyInfoUI.ClearEnemyInfo();
            }
        }

        /// <summary>
        /// コンボテストボタンクリック時の処理
        /// </summary>
        private void OnComboTestClicked()
        {
            if (comboUIManager != null)
            {
                comboUIManager.ExecuteComboTest();
            }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// UIの更新
        /// </summary>
        private void UpdateUI()
        {
            UpdatePendingDamageDisplay();
        }

        /// <summary>
        /// 保留ダメージ表示の更新
        /// </summary>
        private void UpdatePendingDamageDisplay()
        {
            if (pendingDamageText != null && battleManager != null)
            {
                var pendingDamage = GetPendingDamage();
                pendingDamageText.text = $"保留ダメージ: {pendingDamage}";
            }
        }

        /// <summary>
        /// 保留ダメージの取得
        /// </summary>
        /// <returns>保留ダメージ総量</returns>
        private int GetPendingDamage()
        {
            // バトルマネージャーから保留ダメージを取得
            // 実装は具体的なバトルシステムに依存
            return 0; // 仮の値
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// UIの表示/非表示切り替え
        /// </summary>
        /// <param name="visible">表示するかどうか</param>
        public void SetUIVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 強制的にUIを更新
        /// </summary>
        public void ForceUpdateUI()
        {
            if (battleManager != null)
            {
                HandleTurnChanged(battleManager.CurrentTurn);
                HandleGameStateChanged(battleManager.CurrentState);
                
                // プレイヤーデータがある場合
                if (battleManager.PlayerData != null)
                {
                    HandlePlayerHealthChanged(
                        battleManager.PlayerData.currentHp,
                        battleManager.PlayerData.maxHp
                    );
                }
            }

            if (enemyInfoUI != null)
            {
                enemyInfoUI.UpdateAllEnemyInfo();
            }
        }

        #endregion
    }
}