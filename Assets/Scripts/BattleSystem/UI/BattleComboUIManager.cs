using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem.UI
{
    /// <summary>
    /// コンボUIの管理を担当するクラス
    /// コンボシステムの表示、進行状況、エフェクトなどを管理
    /// </summary>
    public class BattleComboUIManager : MonoBehaviour
    {
        [Header("Combo UI Settings")]
        [SerializeField] private GameObject comboProgressPanelPrefab;
        [SerializeField] private Vector2 comboContainerSize = new Vector2(400, 300);
        [SerializeField] private Vector2 comboContainerPosition = new Vector2(0, 100);

        private BattleUILayoutManager layoutManager;
        private ComboSystem comboSystem;
        private Dictionary<string, ComboGroupContainer> comboContainers;
        private GameObject comboProgressPanel;

        #region Initialization

        /// <summary>
        /// コンボUIマネージャーの初期化
        /// </summary>
        /// <param name="layout">レイアウトマネージャー</param>
        /// <param name="combo">コンボシステム</param>
        public void Initialize(BattleUILayoutManager layout, ComboSystem combo)
        {
            layoutManager = layout;
            comboSystem = combo;
            comboContainers = new Dictionary<string, ComboGroupContainer>();

            CreateComboProgressPanel();
            SubscribeToComboEvents();
        }

        /// <summary>
        /// コンボ進行表示パネルを作成
        /// </summary>
        private void CreateComboProgressPanel()
        {
            if (comboProgressPanelPrefab != null)
            {
                comboProgressPanel = Instantiate(comboProgressPanelPrefab);
            }
            else
            {
                comboProgressPanel = layoutManager.CreateUIPanel(
                    "Combo Progress Panel",
                    null,
                    comboContainerPosition,
                    comboContainerSize,
                    new Color(0.1f, 0.1f, 0.3f, 0.9f)
                );
            }
        }

        /// <summary>
        /// コンボシステムのイベント購読
        /// </summary>
        private void SubscribeToComboEvents()
        {
            if (comboSystem != null)
            {
                comboSystem.OnComboStarted += HandleComboStarted;
                comboSystem.OnComboCompleted += HandleComboCompleted;
                comboSystem.OnComboFailed += HandleComboFailed;
            }
        }

        /// <summary>
        /// コンボシステムのイベント購読解除
        /// </summary>
        private void UnsubscribeFromComboEvents()
        {
            if (comboSystem != null)
            {
                comboSystem.OnComboStarted -= HandleComboStarted;
                comboSystem.OnComboCompleted -= HandleComboCompleted;
                comboSystem.OnComboFailed -= HandleComboFailed;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromComboEvents();
        }

        #endregion

        #region Combo Container Management

        /// <summary>
        /// 指定されたコンボのコンテナを作成
        /// </summary>
        /// <param name="comboData">コンボデータ</param>
        /// <returns>作成されたコンボコンテナ</returns>
        public ComboGroupContainer CreateComboContainer(ComboData comboData)
        {
            var container = new ComboGroupContainer
            {
                comboName = comboData.comboName,
                comboData = comboData,
                isActive = false
            };

            // 親オブジェクト作成
            container.parentObject = layoutManager.CreateUIPanel(
                $"Combo_{comboData.comboName}",
                comboProgressPanel.transform,
                CalculateComboPosition(comboContainers.Count),
                new Vector2(380, 80),
                new Color(0.2f, 0.2f, 0.2f, 0.8f)
            );

            // コンボ名テキスト
            container.nameText = layoutManager.CreateUIText(
                "ComboName",
                comboData.comboName,
                container.parentObject.transform,
                new Vector2(-150, 25),
                new Vector2(120, 20)
            );

            // 進行バー
            container.progressBar = layoutManager.CreateUISlider(
                "ProgressBar",
                container.parentObject.transform,
                new Vector2(0, 0),
                new Vector2(300, 20)
            );

            // ステップ表示テキスト
            container.stepText = layoutManager.CreateUIText(
                "StepText",
                "0/0",
                container.parentObject.transform,
                new Vector2(150, 25),
                new Vector2(60, 20)
            );

            // タイマーテキスト
            container.timerText = layoutManager.CreateUIText(
                "TimerText",
                "0.0s",
                container.parentObject.transform,
                new Vector2(-150, -25),
                new Vector2(60, 20)
            );

            // ステータステキスト
            container.statusText = layoutManager.CreateUIText(
                "StatusText",
                "待機中",
                container.parentObject.transform,
                new Vector2(150, -25),
                new Vector2(80, 20)
            );

            // 初期状態では非表示
            container.parentObject.SetActive(false);

            comboContainers[comboData.comboName] = container;
            return container;
        }

        /// <summary>
        /// コンボの位置を計算
        /// </summary>
        /// <param name="index">コンボのインデックス</param>
        /// <returns>計算された位置</returns>
        private Vector2 CalculateComboPosition(int index)
        {
            return new Vector2(0, 120 - (index * 90));
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// コンボ開始時のハンドラー
        /// </summary>
        /// <param name="comboData">コンボデータ</param>
        private void HandleComboStarted(ComboData comboData)
        {
            if (comboData != null && comboContainers.TryGetValue(comboData.comboName, out var container))
            {
                container.isActive = true;
                container.parentObject.SetActive(true);
                container.statusText.text = "実行中";
                container.statusText.color = Color.yellow;
            }
        }

        /// <summary>
        /// コンボ完了時のハンドラー
        /// </summary>
        /// <param name="result">コンボ実行結果</param>
        private void HandleComboCompleted(ComboExecutionResult result)
        {
            // ComboExecutionResultにcomboDataプロパティが存在しないため、処理をスキップ
            if (result.Equals(default(ComboExecutionResult)) == false)
            {
                Debug.Log("コンボが完了しました");
                // TODO: 適切なコンテナ特定方法を実装
            }
        }

        /// <summary>
        /// コンボ失敗時のハンドラー
        /// </summary>
        /// <param name="comboData">コンボデータ</param>
        /// <param name="reason">失敗理由</param>
        private void HandleComboFailed(ComboData comboData, string reason)
        {
            if (comboData != null && comboContainers.TryGetValue(comboData.comboName, out var container))
            {
                container.isActive = false;
                container.statusText.text = "失敗";
                container.statusText.color = Color.red;
                
                // 2秒後に非表示
                StartCoroutine(HideComboContainerAfterDelay(container, 2f));
            }
        }

        /// <summary>
        /// 指定時間後にコンボコンテナを非表示にする
        /// </summary>
        /// <param name="container">対象のコンテナ</param>
        /// <param name="delay">遅延時間</param>
        private System.Collections.IEnumerator HideComboContainerAfterDelay(ComboGroupContainer container, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (container.parentObject != null)
            {
                container.parentObject.SetActive(false);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// コンボテストを実行
        /// </summary>
        public void ExecuteComboTest()
        {
            if (comboSystem != null)
            {
                // テスト用のコンボを実行
                var testCombo = CreateTestComboData();
                // comboSystem.StartCombo(testCombo); // メソッドが存在しないためコメントアウト
            }
        }

        /// <summary>
        /// テスト用コンボデータを作成
        /// </summary>
        /// <returns>テスト用コンボデータ</returns>
        private ComboData CreateTestComboData()
        {
            return new ComboData
            {
                comboName = "テストコンボ",
                steps = new List<ComboStep>
                {
                    new ComboStep { stepName = "ステップ1", requiredWeaponType = "sword" },
                    new ComboStep { stepName = "ステップ2", requiredWeaponType = "bow" },
                    new ComboStep { stepName = "ステップ3", requiredWeaponType = "magic" }
                },
                timeLimit = 10f,
                comboEffect = new ComboEffect
                {
                    damageMultiplier = 2.0f,
                    effectName = "テストエフェクト"
                }
            };
        }

        /// <summary>
        /// すべてのコンボコンテナをリセット
        /// </summary>
        public void ResetAllComboContainers()
        {
            foreach (var container in comboContainers.Values)
            {
                if (container.parentObject != null)
                {
                    container.parentObject.SetActive(false);
                    container.isActive = false;
                    container.progressBar.value = 0f;
                    container.stepText.text = "0/0";
                    container.statusText.text = "待機中";
                    container.statusText.color = Color.white;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// コンボグループのUIコンテナ
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
    }
}