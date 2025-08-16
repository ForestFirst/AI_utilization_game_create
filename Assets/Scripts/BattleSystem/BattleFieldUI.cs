using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    /// <summary>
    /// 戦闘フィールドUI管理クラス
    /// ゲート選択、グリッド表示、戦略情報の表示を管理
    /// </summary>
    public class BattleFieldUI : MonoBehaviour
    {
        [Header("グリッド表示UI")]
        [SerializeField] private Transform gridParent;           // グリッドの親オブジェクト
        [SerializeField] private GameObject gridCellPrefab;      // グリッドセルプレハブ
        [SerializeField] private GameObject gatePrefab;          // ゲートプレハブ
        [SerializeField] private float cellSize = 100f;         // セルサイズ
        [SerializeField] private float cellSpacing = 10f;       // セル間隔
        
        [Header("ゲート選択UI")]
        [SerializeField] private Transform gateButtonsParent;    // ゲートボタンの親
        [SerializeField] private GameObject gateButtonPrefab;    // ゲートボタンプレハブ
        [SerializeField] private Button autoSelectButton;       // 自動選択ボタン
        
        [Header("情報表示UI")]
        [SerializeField] private Text fieldInfoText;            // フィールド情報テキスト
        [SerializeField] private Text strategicInfoText;        // 戦略情報テキスト
        [SerializeField] private Text turnText;                 // ターン表示
        [SerializeField] private GameObject strategicPanel;     // 戦略パネル
        [SerializeField] private Button toggleStrategyButton;   // 戦略表示切り替え
        
        [Header("ゲート詳細UI")]
        [SerializeField] private GameObject gateDetailPanel;    // ゲート詳細パネル
        [SerializeField] private Text gateDetailText;           // ゲート詳細テキスト
        [SerializeField] private Slider gateHpSlider;           // ゲートHPスライダー
        [SerializeField] private Button attackGateButton;       // ゲート攻撃ボタン
        [SerializeField] private Button closeDetailButton;      // 詳細閉じるボタン
        
        [Header("デバッグ設定")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool showGridCoordinates = true;
        
        // 内部状態
        private BattleField battleField;
        private Dictionary<GridPosition, GameObject> gridCells;
        private Dictionary<int, GameObject> gateObjects;
        private Dictionary<int, Button> gateButtons;
        private GateData selectedGate;
        private bool isStrategyPanelVisible = false;
        
        // イベント定義
        public event Action<GateData> OnGateUISelected;         // ゲートUI選択時
        public event Action<GateData> OnGateAttackRequested;    // ゲート攻撃要求時
        public event Action OnAutoSelectRequested;             // 自動選択要求時

        #region Unity Lifecycle

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
        }

        private void OnEnable()
        {
            // BattleFieldのイベントを購読
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            // BattleFieldのイベント購読を解除
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// UIの初期化
        /// </summary>
        private void InitializeUI()
        {
            gridCells = new Dictionary<GridPosition, GameObject>();
            gateObjects = new Dictionary<int, GameObject>();
            gateButtons = new Dictionary<int, Button>();
            
            // パネル初期状態設定
            if (gateDetailPanel != null)
                gateDetailPanel.SetActive(false);
            
            if (strategicPanel != null)
                strategicPanel.SetActive(isStrategyPanelVisible);
            
            LogDebug("BattleFieldUI initialized");
        }

        /// <summary>
        /// イベントリスナーの設定
        /// </summary>
        private void SetupEventListeners()
        {
            if (autoSelectButton != null)
                autoSelectButton.onClick.AddListener(OnAutoSelectButtonClicked);
            
            if (toggleStrategyButton != null)
                toggleStrategyButton.onClick.AddListener(ToggleStrategyPanel);
            
            if (attackGateButton != null)
                attackGateButton.onClick.AddListener(OnAttackGateButtonClicked);
            
            if (closeDetailButton != null)
                closeDetailButton.onClick.AddListener(CloseGateDetail);
        }

        /// <summary>
        /// BattleFieldイベントの購読
        /// </summary>
        private void SubscribeToEvents()
        {
            if (battleField != null)
            {
                battleField.OnGateDestroyed += HandleGateDestroyed;
                battleField.OnGateSelected += HandleGateSelected;
                battleField.OnStrategicEffectApplied += HandleStrategicEffectApplied;
            }
        }

        /// <summary>
        /// BattleFieldイベントの購読解除
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (battleField != null)
            {
                battleField.OnGateDestroyed -= HandleGateDestroyed;
                battleField.OnGateSelected -= HandleGateSelected;
                battleField.OnStrategicEffectApplied -= HandleStrategicEffectApplied;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// BattleFieldを設定
        /// </summary>
        /// <param name="field">戦闘フィールド</param>
        public void SetBattleField(BattleField field)
        {
            // 既存のイベント購読を解除
            UnsubscribeFromEvents();
            
            battleField = field;
            
            // 新しいイベント購読
            SubscribeToEvents();
            
            // UIを更新
            RefreshUI();
            
            LogDebug($"BattleField set: {field.Columns}x{field.Rows} grid");
        }

        /// <summary>
        /// UIを更新
        /// </summary>
        public void RefreshUI()
        {
            if (battleField == null) return;
            
            CreateGrid();
            CreateGateButtons();
            UpdateFieldInfo();
            UpdateTurnDisplay();
            
            if (isStrategyPanelVisible)
            {
                UpdateStrategicInfo();
            }
        }

        /// <summary>
        /// ターン表示を更新
        /// </summary>
        public void UpdateTurnDisplay()
        {
            if (turnText != null && battleField != null)
            {
                turnText.text = $"Turn: {battleField.CurrentTurn}";
            }
        }

        #endregion

        #region Grid Management

        /// <summary>
        /// グリッドを作成
        /// </summary>
        private void CreateGrid()
        {
            if (battleField == null || gridParent == null || gridCellPrefab == null)
                return;
            
            // 既存のグリッドをクリア
            ClearGrid();
            
            // グリッドレイアウト設定
            var gridLayout = gridParent.GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = battleField.Columns;
                gridLayout.cellSize = new Vector2(cellSize, cellSize);
                gridLayout.spacing = new Vector2(cellSpacing, cellSpacing);
            }
            
            // ゲート行を作成（上部）
            CreateGateRow();
            
            // 敵配置行を作成（下部2行）
            for (int row = 0; row < battleField.Rows; row++)
            {
                CreateEnemyRow(row);
            }
            
            LogDebug($"Grid created: {battleField.Columns}x{battleField.Rows + 1}");
        }

        /// <summary>
        /// ゲート行を作成
        /// </summary>
        private void CreateGateRow()
        {
            for (int col = 0; col < battleField.Columns; col++)
            {
                var gateObj = Instantiate(gatePrefab, gridParent);
                var gateComponent = gateObj.GetComponent<GateGridCell>();
                
                if (gateComponent != null)
                {
                    var gate = battleField.Gates[col];
                    gateComponent.SetupGate(gate);
                    gateComponent.OnGateClicked += HandleGateClicked;
                    
                    gateObjects[col] = gateObj;
                }
                
                LogDebug($"Gate {col} created at grid position");
            }
        }

        /// <summary>
        /// 敵配置行を作成
        /// </summary>
        /// <param name="row">行番号</param>
        private void CreateEnemyRow(int row)
        {
            for (int col = 0; col < battleField.Columns; col++)
            {
                var cellObj = Instantiate(gridCellPrefab, gridParent);
                var cellComponent = cellObj.GetComponent<BattleGridCell>();
                
                if (cellComponent != null)
                {
                    var position = new GridPosition(col, row);
                    cellComponent.SetupCell(position, showGridCoordinates);
                    cellComponent.OnCellClicked += HandleCellClicked;
                    
                    gridCells[position] = cellObj;
                }
                
                LogDebug($"Cell created at ({col}, {row})");
            }
        }

        /// <summary>
        /// グリッドをクリア
        /// </summary>
        private void ClearGrid()
        {
            foreach (var cell in gridCells.Values)
            {
                if (cell != null) Destroy(cell);
            }
            gridCells.Clear();
            
            foreach (var gate in gateObjects.Values)
            {
                if (gate != null) Destroy(gate);
            }
            gateObjects.Clear();
        }

        #endregion

        #region Gate Management

        /// <summary>
        /// ゲートボタンを作成
        /// </summary>
        private void CreateGateButtons()
        {
            if (battleField == null || gateButtonsParent == null || gateButtonPrefab == null)
                return;
            
            // 既存ボタンをクリア
            ClearGateButtons();
            
            foreach (var gate in battleField.Gates)
            {
                var buttonObj = Instantiate(gateButtonPrefab, gateButtonsParent);
                var button = buttonObj.GetComponent<Button>();
                var gateButtonComponent = buttonObj.GetComponent<GateSelectionButton>();
                
                if (button != null && gateButtonComponent != null)
                {
                    gateButtonComponent.SetupGateButton(gate);
                    gateButtonComponent.OnGateButtonClicked += HandleGateButtonClicked;
                    
                    gateButtons[gate.gateId] = button;
                }
            }
            
            LogDebug($"Created {battleField.Gates.Count} gate buttons");
        }

        /// <summary>
        /// ゲートボタンをクリア
        /// </summary>
        private void ClearGateButtons()
        {
            foreach (Transform child in gateButtonsParent)
            {
                Destroy(child.gameObject);
            }
            gateButtons.Clear();
        }

        /// <summary>
        /// ゲート詳細を表示
        /// </summary>
        /// <param name="gate">表示するゲート</param>
        private void ShowGateDetail(GateData gate)
        {
            if (gateDetailPanel == null || gate == null) return;
            
            selectedGate = gate;
            
            // 詳細情報を設定
            if (gateDetailText != null)
                gateDetailText.text = gate.GetDetailInfo();
            
            if (gateHpSlider != null)
            {
                gateHpSlider.maxValue = gate.maxHp;
                gateHpSlider.value = gate.currentHp;
            }
            
            // 攻撃ボタンの有効性
            if (attackGateButton != null)
            {
                bool canAttack = !gate.IsDestroyed() && battleField.CanAttackGate(gate.gateId);
                attackGateButton.interactable = canAttack;
                
                var buttonText = attackGateButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    if (gate.IsDestroyed())
                        buttonText.text = "破壊済み";
                    else if (!canAttack)
                        buttonText.text = "敵がブロック中";
                    else
                        buttonText.text = "ゲート攻撃";
                }
            }
            
            gateDetailPanel.SetActive(true);
            LogDebug($"Gate detail shown: {gate.gateName}");
        }

        /// <summary>
        /// ゲート詳細を閉じる
        /// </summary>
        private void CloseGateDetail()
        {
            if (gateDetailPanel != null)
                gateDetailPanel.SetActive(false);
            
            selectedGate = null;
            LogDebug("Gate detail closed");
        }

        #endregion

        #region Information Display

        /// <summary>
        /// フィールド情報を更新
        /// </summary>
        private void UpdateFieldInfo()
        {
            if (fieldInfoText != null && battleField != null)
            {
                fieldInfoText.text = battleField.GetFieldInfo();
            }
        }

        /// <summary>
        /// 戦略情報を更新
        /// </summary>
        private void UpdateStrategicInfo()
        {
            if (strategicInfoText != null && battleField != null)
            {
                strategicInfoText.text = battleField.AnalyzeStrategicSituation();
            }
        }

        /// <summary>
        /// 戦略パネルの表示切り替え
        /// </summary>
        private void ToggleStrategyPanel()
        {
            isStrategyPanelVisible = !isStrategyPanelVisible;
            
            if (strategicPanel != null)
                strategicPanel.SetActive(isStrategyPanelVisible);
            
            if (isStrategyPanelVisible)
                UpdateStrategicInfo();
            
            LogDebug($"Strategy panel toggled: {isStrategyPanelVisible}");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// グリッドセルクリック時の処理
        /// </summary>
        /// <param name="position">クリックされた位置</param>
        private void HandleCellClicked(GridPosition position)
        {
            LogDebug($"Grid cell clicked: ({position.x}, {position.y})");
            
            var enemy = battleField?.GetEnemyAt(position);
            if (enemy != null)
            {
                LogDebug($"Enemy at position: {enemy.EnemyName}");
                // 敵クリック処理（必要に応じて実装）
            }
        }

        /// <summary>
        /// ゲートクリック時の処理
        /// </summary>
        /// <param name="gate">クリックされたゲート</param>
        private void HandleGateClicked(GateData gate)
        {
            LogDebug($"Gate clicked: {gate.gateName}");
            ShowGateDetail(gate);
        }

        /// <summary>
        /// ゲートボタンクリック時の処理
        /// </summary>
        /// <param name="gate">選択されたゲート</param>
        private void HandleGateButtonClicked(GateData gate)
        {
            if (battleField != null && battleField.SelectTargetGate(gate.gateId))
            {
                OnGateUISelected?.Invoke(gate);
                UpdateGateButtonStates();
                LogDebug($"Gate selected via button: {gate.gateName}");
            }
        }

        /// <summary>
        /// 自動選択ボタンクリック時の処理
        /// </summary>
        private void OnAutoSelectButtonClicked()
        {
            if (battleField != null)
            {
                var priorityGate = battleField.GetStrategicPriorityGate();
                if (priorityGate != null && battleField.SelectTargetGate(priorityGate.gateId))
                {
                    OnGateUISelected?.Invoke(priorityGate);
                    UpdateGateButtonStates();
                    ShowGateDetail(priorityGate);
                    LogDebug($"Auto-selected gate: {priorityGate.gateName}");
                }
            }
            
            OnAutoSelectRequested?.Invoke();
        }

        /// <summary>
        /// ゲート攻撃ボタンクリック時の処理
        /// </summary>
        private void OnAttackGateButtonClicked()
        {
            if (selectedGate != null)
            {
                OnGateAttackRequested?.Invoke(selectedGate);
                LogDebug($"Gate attack requested: {selectedGate.gateName}");
            }
        }

        /// <summary>
        /// ゲート破壊時の処理
        /// </summary>
        /// <param name="gate">破壊されたゲート</param>
        private void HandleGateDestroyed(GateData gate)
        {
            // ゲートオブジェクトの視覚的更新
            if (gateObjects.ContainsKey(gate.gateId))
            {
                var gateObj = gateObjects[gate.gateId];
                var gateComponent = gateObj.GetComponent<GateGridCell>();
                gateComponent?.SetDestroyedState();
            }
            
            // ボタン状態更新
            UpdateGateButtonStates();
            
            // 詳細パネルが該当ゲートの場合は閉じる
            if (selectedGate?.gateId == gate.gateId)
            {
                CloseGateDetail();
            }
            
            LogDebug($"UI updated for destroyed gate: {gate.gateName}");
        }

        /// <summary>
        /// ゲート選択時の処理
        /// </summary>
        /// <param name="gate">選択されたゲート</param>
        private void HandleGateSelected(GateData gate)
        {
            UpdateGateButtonStates();
            LogDebug($"UI updated for selected gate: {gate.gateName}");
        }

        /// <summary>
        /// 戦略効果適用時の処理
        /// </summary>
        /// <param name="gate">効果を適用したゲート</param>
        /// <param name="effect">適用された効果</param>
        private void HandleStrategicEffectApplied(GateData gate, GateStrategicEffect effect)
        {
            LogDebug($"Strategic effect applied: {gate.gateName} -> {effect}");
            
            if (isStrategyPanelVisible)
            {
                UpdateStrategicInfo();
            }
        }

        /// <summary>
        /// ゲートボタン状態を更新
        /// </summary>
        private void UpdateGateButtonStates()
        {
            if (battleField == null) return;
            
            var selectedGateId = battleField.SelectedTargetGate?.gateId ?? -1;
            
            foreach (var kvp in gateButtons)
            {
                int gateId = kvp.Key;
                var button = kvp.Value;
                var gate = battleField.Gates.FirstOrDefault(g => g.gateId == gateId);
                
                if (gate != null && button != null)
                {
                    // 選択状態の視覚的表現
                    var colors = button.colors;
                    if (gateId == selectedGateId)
                    {
                        colors.normalColor = Color.yellow;
                    }
                    else if (gate.IsDestroyed())
                    {
                        colors.normalColor = Color.gray;
                    }
                    else
                    {
                        colors.normalColor = Color.white;
                    }
                    button.colors = colors;
                    
                    // ボタンの有効/無効
                    button.interactable = !gate.IsDestroyed();
                }
            }
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
                Debug.Log($"[BattleFieldUI] {message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// グリッドセルコンポーネント
    /// </summary>
    public class BattleGridCell : MonoBehaviour
    {
        [Header("UI要素")]
        [SerializeField] private Button cellButton;
        [SerializeField] private Text coordinateText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image enemyImage;
        
        private GridPosition position;
        
        public event Action<GridPosition> OnCellClicked;

        /// <summary>
        /// セルを設定
        /// </summary>
        /// <param name="pos">グリッド位置</param>
        /// <param name="showCoordinates">座標表示フラグ</param>
        public void SetupCell(GridPosition pos, bool showCoordinates = true)
        {
            position = pos;
            
            if (coordinateText != null)
            {
                coordinateText.text = showCoordinates ? $"({pos.x},{pos.y})" : "";
                coordinateText.gameObject.SetActive(showCoordinates);
            }
            
            if (cellButton != null)
            {
                cellButton.onClick.RemoveAllListeners();
                cellButton.onClick.AddListener(() => OnCellClicked?.Invoke(position));
            }
        }

        /// <summary>
        /// 敵を配置
        /// </summary>
        /// <param name="enemy">配置する敵</param>
        public void PlaceEnemy(EnemyInstance enemy)
        {
            if (enemyImage != null)
            {
                enemyImage.gameObject.SetActive(true);
                // TODO: 敵の画像を設定
                enemyImage.color = Color.red; // 仮の色設定
            }
        }

        /// <summary>
        /// 敵を削除
        /// </summary>
        public void RemoveEnemy()
        {
            if (enemyImage != null)
            {
                enemyImage.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// ゲートグリッドセルコンポーネント
    /// </summary>
    public class GateGridCell : MonoBehaviour
    {
        [Header("UI要素")]
        [SerializeField] private Button gateButton;
        [SerializeField] private Text gateNameText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image gateTypeIcon;
        [SerializeField] private GameObject destroyedOverlay;
        
        private GateData gateData;
        
        public event Action<GateData> OnGateClicked;

        /// <summary>
        /// ゲートを設定
        /// </summary>
        /// <param name="gate">ゲートデータ</param>
        public void SetupGate(GateData gate)
        {
            gateData = gate;
            
            UpdateGateDisplay();
            
            if (gateButton != null)
            {
                gateButton.onClick.RemoveAllListeners();
                gateButton.onClick.AddListener(() => OnGateClicked?.Invoke(gateData));
            }
        }

        /// <summary>
        /// ゲート表示を更新
        /// </summary>
        private void UpdateGateDisplay()
        {
            if (gateData == null) return;
            
            if (gateNameText != null)
                gateNameText.text = gateData.gateName;
            
            if (hpSlider != null)
            {
                hpSlider.maxValue = gateData.maxHp;
                hpSlider.value = gateData.currentHp;
            }
            
            if (gateTypeIcon != null)
            {
                gateTypeIcon.color = GetGateTypeColor(gateData.gateType);
            }
            
            if (destroyedOverlay != null)
                destroyedOverlay.SetActive(gateData.IsDestroyed());
        }

        /// <summary>
        /// 破壊状態を設定
        /// </summary>
        public void SetDestroyedState()
        {
            UpdateGateDisplay();
        }

        /// <summary>
        /// ゲートタイプに基づく色を取得
        /// </summary>
        /// <param name="gateType">ゲートタイプ</param>
        /// <returns>色</returns>
        private Color GetGateTypeColor(GateType gateType)
        {
            switch (gateType)
            {
                case GateType.Support: return Color.green;
                case GateType.Summoner: return Color.blue;
                case GateType.Elite: return Color.red;
                case GateType.Fortress: return Color.gray;
                default: return Color.white;
            }
        }
    }

    /// <summary>
    /// ゲート選択ボタンコンポーネント
    /// </summary>
    public class GateSelectionButton : MonoBehaviour
    {
        [Header("UI要素")]
        [SerializeField] private Text gateInfoText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image typeIcon;
        
        private GateData gateData;
        
        public event Action<GateData> OnGateButtonClicked;

        /// <summary>
        /// ゲートボタンを設定
        /// </summary>
        /// <param name="gate">ゲートデータ</param>
        public void SetupGateButton(GateData gate)
        {
            gateData = gate;
            
            UpdateButtonDisplay();
            
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnGateButtonClicked?.Invoke(gateData));
            }
        }

        /// <summary>
        /// ボタン表示を更新
        /// </summary>
        private void UpdateButtonDisplay()
        {
            if (gateData == null) return;
            
            if (gateInfoText != null)
                gateInfoText.text = $"{gateData.gateName}\n{gateData.gateType}";
            
            if (hpSlider != null)
            {
                hpSlider.maxValue = gateData.maxHp;
                hpSlider.value = gateData.currentHp;
            }
            
            if (typeIcon != null)
            {
                typeIcon.color = GetGateTypeColor(gateData.gateType);
            }
        }

        /// <summary>
        /// ゲートタイプに基づく色を取得
        /// </summary>
        /// <param name="gateType">ゲートタイプ</param>
        /// <returns>色</returns>
        private Color GetGateTypeColor(GateType gateType)
        {
            switch (gateType)
            {
                case GateType.Support: return Color.green;
                case GateType.Summoner: return Color.blue;
                case GateType.Elite: return Color.red;
                case GateType.Fortress: return Color.gray;
                default: return Color.white;
            }
        }
    }
}