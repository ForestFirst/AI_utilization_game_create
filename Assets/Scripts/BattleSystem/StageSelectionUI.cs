using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BattleSystem
{
    /// <summary>
    /// ステージ選択UI管理クラス
    /// ステージリストの表示、選択、詳細表示を管理
    /// </summary>
    public class StageSelectionUI : MonoBehaviour
    {
        [Header("UI参照")]
        [SerializeField] private Transform stageListParent;     // ステージリストの親オブジェクト
        [SerializeField] private GameObject stageItemPrefab;    // ステージアイテムプレハブ
        [SerializeField] private ScrollRect stageScrollRect;    // ステージリストスクロール
        
        [Header("ステージ詳細UI")]
        [SerializeField] private GameObject stageDetailPanel;   // ステージ詳細パネル
        [SerializeField] private Text stageNameText;            // ステージ名
        [SerializeField] private Text stageDescriptionText;     // ステージ説明
        [SerializeField] private Text difficultyText;           // 難易度
        [SerializeField] private Text recommendedLevelText;     // 推奨レベル
        [SerializeField] private Text staminaCostText;          // スタミナコスト
        [SerializeField] private Image stageTypeIcon;           // ステージタイプアイコン
        [SerializeField] private Button startStageButton;       // ステージ開始ボタン
        [SerializeField] private Button closeDetailButton;      // 詳細閉じるボタン
        
        [Header("フィルター/ソートUI")]
        [SerializeField] private Dropdown difficultyFilter;     // 難易度フィルター
        [SerializeField] private Dropdown stageTypeFilter;      // ステージタイプフィルター
        [SerializeField] private Toggle unlockedOnlyToggle;     // 解放済みのみ表示
        [SerializeField] private Toggle clearedOnlyToggle;      // クリア済みのみ表示
        [SerializeField] private Button refreshButton;          // 更新ボタン
        
        [Header("進行状況UI")]
        [SerializeField] private Text totalProgressText;        // 総進行状況
        [SerializeField] private Slider progressSlider;         // 進行状況スライダー
        
        [Header("デバッグ設定")]
        [SerializeField] private bool debugMode = false;
        
        // 現在の選択状態
        private StageData selectedStage;
        private List<StageData> filteredStages;
        private Dictionary<string, GameObject> stageItemObjects;
        
        // フィルター状態
        private StageDifficulty currentDifficultyFilter = StageDifficulty.Easy;
        private StageType currentTypeFilter = StageType.Normal;
        private bool showUnlockedOnly = false;
        private bool showClearedOnly = false;
        
        // イベント定義
        public event Action<StageData> OnStageItemClicked;       // ステージアイテムクリック時
        public event Action<StageData> OnStageStartRequested;    // ステージ開始要求時

        #region Unity Lifecycle

        private void Start()
        {
            InitializeUI();
            SetupEventListeners();
            RefreshStageList();
        }

        private void OnEnable()
        {
            // StageManagerのイベントを購読
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageUnlocked += HandleStageUnlocked;
                StageManager.Instance.OnStageCompleted += HandleStageCompleted;
                StageManager.Instance.OnStageProgressChanged += HandleProgressChanged;
            }
        }

        private void OnDisable()
        {
            // StageManagerのイベント購読を解除
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageUnlocked -= HandleStageUnlocked;
                StageManager.Instance.OnStageCompleted -= HandleStageCompleted;
                StageManager.Instance.OnStageProgressChanged -= HandleProgressChanged;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// UIの初期化
        /// </summary>
        private void InitializeUI()
        {
            filteredStages = new List<StageData>();
            stageItemObjects = new Dictionary<string, GameObject>();
            
            // 詳細パネルを初期状態で非表示
            if (stageDetailPanel != null)
                stageDetailPanel.SetActive(false);
            
            // フィルター/ソートUIの初期化
            SetupFilterUI();
            
            LogDebug("StageSelectionUI initialized");
        }

        /// <summary>
        /// フィルターUIの設定
        /// </summary>
        private void SetupFilterUI()
        {
            // 難易度フィルターの設定
            if (difficultyFilter != null)
            {
                difficultyFilter.ClearOptions();
                var difficultyOptions = new List<string> { "全ての難易度" };
                difficultyOptions.AddRange(Enum.GetNames(typeof(StageDifficulty)));
                difficultyFilter.AddOptions(difficultyOptions);
                difficultyFilter.value = 0;
            }
            
            // ステージタイプフィルターの設定
            if (stageTypeFilter != null)
            {
                stageTypeFilter.ClearOptions();
                var typeOptions = new List<string> { "全てのタイプ" };
                typeOptions.AddRange(Enum.GetNames(typeof(StageType)));
                stageTypeFilter.AddOptions(typeOptions);
                stageTypeFilter.value = 0;
            }
            
            // トグルの初期状態設定
            if (unlockedOnlyToggle != null)
                unlockedOnlyToggle.isOn = false;
            
            if (clearedOnlyToggle != null)
                clearedOnlyToggle.isOn = false;
        }

        /// <summary>
        /// イベントリスナーの設定
        /// </summary>
        private void SetupEventListeners()
        {
            // ボタンイベント
            if (startStageButton != null)
                startStageButton.onClick.AddListener(OnStartStageButtonClicked);
            
            if (closeDetailButton != null)
                closeDetailButton.onClick.AddListener(OnCloseDetailButtonClicked);
            
            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshStageList);
            
            // フィルターイベント
            if (difficultyFilter != null)
                difficultyFilter.onValueChanged.AddListener(OnDifficultyFilterChanged);
            
            if (stageTypeFilter != null)
                stageTypeFilter.onValueChanged.AddListener(OnTypeFilterChanged);
            
            if (unlockedOnlyToggle != null)
                unlockedOnlyToggle.onValueChanged.AddListener(OnUnlockedOnlyToggleChanged);
            
            if (clearedOnlyToggle != null)
                clearedOnlyToggle.onValueChanged.AddListener(OnClearedOnlyToggleChanged);
        }

        #endregion

        #region Stage List Management

        /// <summary>
        /// ステージリストを更新
        /// </summary>
        public void RefreshStageList()
        {
            if (StageManager.Instance == null)
            {
                LogDebug("StageManager not found");
                return;
            }
            
            // 全ステージを取得
            var allStages = StageManager.Instance.AllStages;
            
            // フィルター適用
            filteredStages = ApplyFilters(allStages);
            
            // UIアイテムを生成/更新
            UpdateStageListUI();
            
            // 進行状況を更新
            UpdateProgressUI();
            
            LogDebug($"Stage list refreshed: {filteredStages.Count} stages displayed");
        }

        /// <summary>
        /// フィルターを適用
        /// </summary>
        /// <param name="stages">フィルター対象ステージリスト</param>
        /// <returns>フィルター済みステージリスト</returns>
        private List<StageData> ApplyFilters(List<StageData> stages)
        {
            var filtered = stages.AsEnumerable();
            
            // 難易度フィルター
            if (difficultyFilter != null && difficultyFilter.value > 0)
            {
                var targetDifficulty = (StageDifficulty)(difficultyFilter.value);
                filtered = filtered.Where(stage => stage.difficulty == targetDifficulty);
            }
            
            // ステージタイプフィルター
            if (stageTypeFilter != null && stageTypeFilter.value > 0)
            {
                var targetType = (StageType)(stageTypeFilter.value);
                filtered = filtered.Where(stage => stage.stageType == targetType);
            }
            
            // 解放済みフィルター
            if (showUnlockedOnly)
            {
                filtered = filtered.Where(stage => stage.isUnlocked);
            }
            
            // クリア済みフィルター
            if (showClearedOnly && StageManager.Instance != null)
            {
                filtered = filtered.Where(stage => 
                {
                    var progress = StageManager.Instance.GetStageProgress(stage.stageId);
                    return progress.isCleared;
                });
            }
            
            return filtered.OrderBy(stage => stage.stageId).ToList();
        }

        /// <summary>
        /// ステージリストUIを更新
        /// </summary>
        private void UpdateStageListUI()
        {
            // 既存のアイテムをクリア
            ClearStageListItems();
            
            // 新しいアイテムを生成
            foreach (var stage in filteredStages)
            {
                CreateStageListItem(stage);
            }
        }

        /// <summary>
        /// ステージリストアイテムをクリア
        /// </summary>
        private void ClearStageListItems()
        {
            foreach (var item in stageItemObjects.Values)
            {
                if (item != null)
                    Destroy(item);
            }
            stageItemObjects.Clear();
        }

        /// <summary>
        /// ステージリストアイテムを作成
        /// </summary>
        /// <param name="stage">ステージデータ</param>
        private void CreateStageListItem(StageData stage)
        {
            if (stageItemPrefab == null || stageListParent == null)
                return;
            
            var itemObject = Instantiate(stageItemPrefab, stageListParent);
            var itemComponent = itemObject.GetComponent<StageListItem>();
            
            if (itemComponent != null)
            {
                // ステージ進行状況を取得
                var progress = StageManager.Instance?.GetStageProgress(stage.stageId);
                
                // アイテムを設定
                itemComponent.SetupStageItem(stage, progress);
                itemComponent.OnItemClicked += HandleStageItemClicked;
                
                stageItemObjects[stage.stageId] = itemObject;
            }
            else
            {
                LogDebug($"StageListItem component not found on prefab");
            }
        }

        #endregion

        #region Stage Detail Management

        /// <summary>
        /// ステージ詳細を表示
        /// </summary>
        /// <param name="stage">表示するステージ</param>
        public void ShowStageDetail(StageData stage)
        {
            if (stage == null || stageDetailPanel == null)
                return;
            
            selectedStage = stage;
            
            // 詳細情報を設定
            UpdateStageDetailUI(stage);
            
            // 詳細パネルを表示
            stageDetailPanel.SetActive(true);
            
            LogDebug($"Showing stage detail: {stage.stageName}");
        }

        /// <summary>
        /// ステージ詳細UIを更新
        /// </summary>
        /// <param name="stage">ステージデータ</param>
        private void UpdateStageDetailUI(StageData stage)
        {
            if (stageNameText != null)
                stageNameText.text = stage.stageName;
            
            if (stageDescriptionText != null)
                stageDescriptionText.text = stage.stageDescription;
            
            if (difficultyText != null)
            {
                difficultyText.text = $"難易度: {stage.difficulty}";
                difficultyText.color = stage.GetDifficultyColor();
            }
            
            if (recommendedLevelText != null)
                recommendedLevelText.text = $"推奨レベル: {stage.recommendedLevel}";
            
            if (staminaCostText != null)
                staminaCostText.text = $"スタミナ: {stage.staminaCost}";
            
            if (stageTypeIcon != null)
            {
                // ステージタイプアイコンの設定（アイコン画像があれば）
                // stageTypeIcon.sprite = GetStageTypeSprite(stage.stageType);
            }
            
            // 開始ボタンの有効/無効設定
            if (startStageButton != null)
            {
                bool canStart = stage.isUnlocked && CanStartStage(stage);
                startStageButton.interactable = canStart;
                
                var buttonText = startStageButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    if (!stage.isUnlocked)
                        buttonText.text = "未解放";
                    else if (!canStart)
                        buttonText.text = "開始不可";
                    else
                        buttonText.text = "ステージ開始";
                }
            }
        }

        /// <summary>
        /// ステージ詳細を非表示
        /// </summary>
        public void HideStageDetail()
        {
            if (stageDetailPanel != null)
                stageDetailPanel.SetActive(false);
            
            selectedStage = null;
            LogDebug("Stage detail hidden");
        }

        #endregion

        #region Progress Management

        /// <summary>
        /// 進行状況UIを更新
        /// </summary>
        private void UpdateProgressUI()
        {
            if (StageManager.Instance == null)
                return;
            
            int totalStages = StageManager.Instance.TotalStageCount;
            int clearedStages = StageManager.Instance.ClearedStageCount;
            
            if (totalProgressText != null)
                totalProgressText.text = $"進行状況: {clearedStages}/{totalStages}";
            
            if (progressSlider != null)
            {
                progressSlider.maxValue = totalStages;
                progressSlider.value = clearedStages;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ステージアイテムクリック時の処理
        /// </summary>
        /// <param name="stage">クリックされたステージ</param>
        private void HandleStageItemClicked(StageData stage)
        {
            ShowStageDetail(stage);
            OnStageItemClicked?.Invoke(stage);
        }

        /// <summary>
        /// ステージ開始ボタンクリック時の処理
        /// </summary>
        private void OnStartStageButtonClicked()
        {
            if (selectedStage == null) return;
            
            if (StageManager.Instance != null && StageManager.Instance.SelectStage(selectedStage.stageId))
            {
                OnStageStartRequested?.Invoke(selectedStage);
                HideStageDetail();
                LogDebug($"Stage start requested: {selectedStage.stageName}");
            }
            else
            {
                LogDebug($"Failed to select stage: {selectedStage.stageId}");
            }
        }

        /// <summary>
        /// 詳細閉じるボタンクリック時の処理
        /// </summary>
        private void OnCloseDetailButtonClicked()
        {
            HideStageDetail();
        }

        /// <summary>
        /// 難易度フィルター変更時の処理
        /// </summary>
        /// <param name="value">選択値</param>
        private void OnDifficultyFilterChanged(int value)
        {
            RefreshStageList();
        }

        /// <summary>
        /// ステージタイプフィルター変更時の処理
        /// </summary>
        /// <param name="value">選択値</param>
        private void OnTypeFilterChanged(int value)
        {
            RefreshStageList();
        }

        /// <summary>
        /// 解放済みのみトグル変更時の処理
        /// </summary>
        /// <param name="value">トグル値</param>
        private void OnUnlockedOnlyToggleChanged(bool value)
        {
            showUnlockedOnly = value;
            RefreshStageList();
        }

        /// <summary>
        /// クリア済みのみトグル変更時の処理
        /// </summary>
        /// <param name="value">トグル値</param>
        private void OnClearedOnlyToggleChanged(bool value)
        {
            showClearedOnly = value;
            RefreshStageList();
        }

        /// <summary>
        /// ステージ解放時の処理
        /// </summary>
        /// <param name="stage">解放されたステージ</param>
        private void HandleStageUnlocked(StageData stage)
        {
            RefreshStageList();
            LogDebug($"Stage unlocked: {stage.stageName}");
        }

        /// <summary>
        /// ステージ完了時の処理
        /// </summary>
        /// <param name="stage">完了したステージ</param>
        /// <param name="success">成功フラグ</param>
        private void HandleStageCompleted(StageData stage, bool success)
        {
            RefreshStageList();
            LogDebug($"Stage completed: {stage.stageName} (Success: {success})");
        }

        /// <summary>
        /// 進行状況変更時の処理
        /// </summary>
        private void HandleProgressChanged()
        {
            UpdateProgressUI();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// ステージを開始可能かチェック
        /// </summary>
        /// <param name="stage">ステージデータ</param>
        /// <returns>開始可能な場合true</returns>
        private bool CanStartStage(StageData stage)
        {
            // スタミナチェック（実装時に追加）
            // レベル制限チェック（実装時に追加）
            return true; // 仮実装
        }

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        /// <param name="message">メッセージ</param>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[StageSelectionUI] {message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 特定のステージを選択状態にする
        /// </summary>
        /// <param name="stageId">ステージID</param>
        public void SelectStage(string stageId)
        {
            var stage = filteredStages.FirstOrDefault(s => s.stageId == stageId);
            if (stage != null)
            {
                ShowStageDetail(stage);
            }
        }

        /// <summary>
        /// フィルターをリセット
        /// </summary>
        public void ResetFilters()
        {
            if (difficultyFilter != null)
                difficultyFilter.value = 0;
            
            if (stageTypeFilter != null)
                stageTypeFilter.value = 0;
            
            if (unlockedOnlyToggle != null)
                unlockedOnlyToggle.isOn = false;
            
            if (clearedOnlyToggle != null)
                clearedOnlyToggle.isOn = false;
            
            RefreshStageList();
        }

        #endregion
    }

    /// <summary>
    /// ステージリストアイテムコンポーネント
    /// 個別のステージアイテムの表示とインタラクションを管理
    /// </summary>
    public class StageListItem : MonoBehaviour
    {
        [Header("UI要素")]
        [SerializeField] private Text stageNameText;        // ステージ名
        [SerializeField] private Text difficultyText;       // 難易度
        [SerializeField] private Text progressText;         // 進行状況
        [SerializeField] private Image backgroundImage;     // 背景画像
        [SerializeField] private Image lockIcon;            // ロックアイコン
        [SerializeField] private Image clearIcon;           // クリアアイコン
        [SerializeField] private Button itemButton;         // アイテムボタン
        
        [Header("カラー設定")]
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color clearedColor = Color.green;
        
        // ステージデータ
        private StageData stageData;
        private StageProgress stageProgress;
        
        // イベント定義
        public event Action<StageData> OnItemClicked;

        /// <summary>
        /// ステージアイテムを設定
        /// </summary>
        /// <param name="stage">ステージデータ</param>
        /// <param name="progress">進行状況</param>
        public void SetupStageItem(StageData stage, StageProgress progress)
        {
            stageData = stage;
            stageProgress = progress;
            
            // UI要素を更新
            UpdateItemUI();
            
            // ボタンイベントを設定
            if (itemButton != null)
            {
                itemButton.onClick.RemoveAllListeners();
                itemButton.onClick.AddListener(OnItemButtonClicked);
            }
        }

        /// <summary>
        /// アイテムUIを更新
        /// </summary>
        private void UpdateItemUI()
        {
            if (stageData == null) return;
            
            // ステージ名
            if (stageNameText != null)
                stageNameText.text = stageData.stageName;
            
            // 難易度
            if (difficultyText != null)
            {
                difficultyText.text = stageData.difficulty.ToString();
                difficultyText.color = stageData.GetDifficultyColor();
            }
            
            // 進行状況
            if (progressText != null)
            {
                if (stageProgress != null && stageProgress.isCleared)
                {
                    progressText.text = $"クリア済み ({stageProgress.clearCount}回)";
                }
                else if (stageData.isUnlocked)
                {
                    progressText.text = "未クリア";
                }
                else
                {
                    progressText.text = "未解放";
                }
            }
            
            // 背景色
            if (backgroundImage != null)
            {
                if (!stageData.isUnlocked)
                    backgroundImage.color = lockedColor;
                else if (stageProgress?.isCleared == true)
                    backgroundImage.color = clearedColor;
                else
                    backgroundImage.color = unlockedColor;
            }
            
            // アイコン表示
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!stageData.isUnlocked);
            
            if (clearIcon != null)
                clearIcon.gameObject.SetActive(stageProgress?.isCleared == true);
            
            // ボタンの有効/無効
            if (itemButton != null)
                itemButton.interactable = stageData.isUnlocked;
        }

        /// <summary>
        /// アイテムボタンクリック時の処理
        /// </summary>
        private void OnItemButtonClicked()
        {
            OnItemClicked?.Invoke(stageData);
        }
    }
}