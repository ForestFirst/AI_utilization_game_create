using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem.UI
{
    /// <summary>
    /// 敵情報UIの管理を担当するクラス
    /// 敵のHP、状態、位置などの情報表示を管理
    /// </summary>
    public class BattleEnemyInfoUI : MonoBehaviour
    {
        [Header("Enemy Info Settings")]
        [SerializeField] private Vector2 enemyInfoPanelSize = new Vector2(300, 400);
        [SerializeField] private Vector2 enemyInfoPanelPosition = new Vector2(300, 0);

        private BattleUILayoutManager layoutManager;
        private BattleManager battleManager;
        private GameObject enemyInfoPanel;
        private ScrollRect enemyScrollView;
        private Transform enemyContentContainer;
        private Dictionary<int, EnemyInfoContainer> enemyInfoContainers;

        #region Initialization

        /// <summary>
        /// 敵情報UIの初期化
        /// </summary>
        /// <param name="layout">レイアウトマネージャー</param>
        /// <param name="battle">バトルマネージャー</param>
        public void Initialize(BattleUILayoutManager layout, BattleManager battle)
        {
            layoutManager = layout;
            battleManager = battle;
            enemyInfoContainers = new Dictionary<int, EnemyInfoContainer>();

            CreateEnemyInfoPanel();
            SubscribeToBattleEvents();
        }

        /// <summary>
        /// 敵情報パネルを作成
        /// </summary>
        private void CreateEnemyInfoPanel()
        {
            // メインパネル作成
            enemyInfoPanel = layoutManager.CreateUIPanel(
                "Enemy Info Panel",
                null,
                enemyInfoPanelPosition,
                enemyInfoPanelSize,
                new Color(0.1f, 0.1f, 0.1f, 0.9f)
            );

            // タイトルテキスト作成
            var titleText = layoutManager.CreateUIText(
                "Enemy Info Title",
                "敵情報",
                enemyInfoPanel.transform,
                new Vector2(0, 180),
                new Vector2(200, 30)
            );
            titleText.fontSize = 16;
            titleText.color = Color.white;

            // スクロールビュー作成
            CreateEnemyScrollView();
        }

        /// <summary>
        /// 敵情報用スクロールビューを作成
        /// </summary>
        private void CreateEnemyScrollView()
        {
            // スクロールビューのGameObject作成
            var scrollViewObj = new GameObject("Enemy Scroll View");
            var scrollRect = scrollViewObj.AddComponent<ScrollRect>();
            var scrollImage = scrollViewObj.AddComponent<Image>();

            scrollViewObj.transform.SetParent(enemyInfoPanel.transform, false);

            // スクロールビューの設定
            var scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
            scrollRectTransform.anchoredPosition = new Vector2(0, -20);
            scrollRectTransform.sizeDelta = new Vector2(280, 320);

            scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

            // ビューポート作成
            var viewport = CreateScrollViewport(scrollViewObj.transform);
            scrollRect.viewport = viewport;

            // コンテンツ作成
            var content = CreateScrollContent(viewport.transform);
            scrollRect.content = content;
            enemyContentContainer = content.transform;

            // スクロール設定
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            enemyScrollView = scrollRect;
        }

        /// <summary>
        /// スクロールビューのビューポートを作成
        /// </summary>
        private RectTransform CreateScrollViewport(Transform parent)
        {
            var viewportObj = new GameObject("Viewport");
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            var viewportImage = viewportObj.AddComponent<Image>();
            var mask = viewportObj.AddComponent<Mask>();

            viewportObj.transform.SetParent(parent, false);

            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            viewportImage.color = Color.clear;
            mask.showMaskGraphic = false;

            return viewportRect;
        }

        /// <summary>
        /// スクロールビューのコンテンツを作成
        /// </summary>
        private RectTransform CreateScrollContent(Transform parent)
        {
            var contentObj = new GameObject("Content");
            var contentRect = contentObj.AddComponent<RectTransform>();
            var layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            var contentSizeFitter = contentObj.AddComponent<ContentSizeFitter>();

            contentObj.transform.SetParent(parent, false);

            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.spacing = 5;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);

            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return contentRect;
        }

        /// <summary>
        /// バトルマネージャーのイベント購読
        /// </summary>
        private void SubscribeToBattleEvents()
        {
            if (battleManager != null)
            {
                // 利用可能なイベントのみ購読
                battleManager.OnTurnChanged += HandleTurnChanged;
                battleManager.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        /// <summary>
        /// バトルマネージャーのイベント購読解除
        /// </summary>
        private void UnsubscribeFromBattleEvents()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged -= HandleTurnChanged;
                battleManager.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromBattleEvents();
        }

        #endregion

        #region Enemy Info Container Management

        /// <summary>
        /// 敵情報コンテナを作成
        /// </summary>
        /// <param name="enemyInstance">敵インスタンス</param>
        /// <returns>作成された敵情報コンテナ</returns>
        public EnemyInfoContainer CreateEnemyInfoContainer(EnemyInstance enemyInstance)
        {
            var container = new EnemyInfoContainer
            {
                enemyId = enemyInstance.instanceId,
                enemyInstance = enemyInstance
            };

            // 敵情報パネル作成
            container.containerPanel = layoutManager.CreateUIPanel(
                $"Enemy_{enemyInstance.instanceId}",
                enemyContentContainer,
                Vector2.zero,
                new Vector2(260, 80),
                new Color(0.2f, 0.2f, 0.2f, 0.8f)
            );

            // 敵名テキスト
            container.nameText = layoutManager.CreateUIText(
                "EnemyName",
                enemyInstance.EnemyName,
                container.containerPanel.transform,
                new Vector2(-80, 25),
                new Vector2(160, 20)
            );
            container.nameText.fontSize = 12;

            // HPバー
            container.hpSlider = layoutManager.CreateUISlider(
                "HPBar",
                container.containerPanel.transform,
                new Vector2(-50, 0),
                new Vector2(180, 15)
            );
            container.hpSlider.maxValue = enemyInstance.MaxHp;
            container.hpSlider.value = enemyInstance.currentHp;

            // HPテキスト
            container.hpText = layoutManager.CreateUIText(
                "HPText",
                $"{enemyInstance.currentHp}/{enemyInstance.MaxHp}",
                container.containerPanel.transform,
                new Vector2(100, 0),
                new Vector2(60, 15)
            );
            container.hpText.fontSize = 10;

            // 位置テキスト
            container.positionText = layoutManager.CreateUIText(
                "PositionText",
                $"({enemyInstance.gridX}, {enemyInstance.gridY})",
                container.containerPanel.transform,
                new Vector2(-80, -25),
                new Vector2(80, 15)
            );
            container.positionText.fontSize = 10;

            // ステータステキスト
            container.statusText = layoutManager.CreateUIText(
                "StatusText",
                GetEnemyStatusText(enemyInstance),
                container.containerPanel.transform,
                new Vector2(80, -25),
                new Vector2(100, 15)
            );
            container.statusText.fontSize = 10;

            enemyInfoContainers[enemyInstance.instanceId] = container;
            return container;
        }

        /// <summary>
        /// 敵のステータステキストを取得
        /// </summary>
        /// <param name="enemy">敵インスタンス</param>
        /// <returns>ステータステキスト</returns>
        private string GetEnemyStatusText(EnemyInstance enemy)
        {
            if (!enemy.IsAlive())
                return "撃破";
            
            if (enemy.activeBuffs.Count > 0)
                return "バフあり";
            
            return "通常";
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ターン変更時のハンドラー
        /// </summary>
        /// <param name="newTurn">新しいターン</param>
        private void HandleTurnChanged(int newTurn)
        {
            // ターンが変わった時に敵情報を更新
            UpdateAllEnemyInfo();
        }

        /// <summary>
        /// ゲーム状態変更時のハンドラー
        /// </summary>
        /// <param name="newState">新しいゲーム状態</param>
        private void HandleGameStateChanged(GameState newState)
        {
            // ゲーム状態に応じてUI表示を調整
            if (newState == GameState.GameOver || newState == GameState.Victory || newState == GameState.Defeat)
            {
                // 戦闘終了時は敵情報をクリア
                ClearEnemyInfo();
            }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// 敵情報を更新
        /// </summary>
        /// <param name="enemy">更新対象の敵</param>
        public void UpdateEnemyInfo(EnemyInstance enemy)
        {
            if (enemyInfoContainers.TryGetValue(enemy.instanceId, out var container))
            {
                // HPの更新
                container.hpSlider.value = enemy.currentHp;
                container.hpText.text = $"{enemy.currentHp}/{enemy.MaxHp}";

                // ステータスの更新
                container.statusText.text = GetEnemyStatusText(enemy);
                
                // 撃破時の色変更
                if (!enemy.IsAlive())
                {
                    container.statusText.color = Color.red;
                    container.nameText.color = Color.gray;
                }
            }
        }

        /// <summary>
        /// すべての敵情報を更新
        /// </summary>
        public void UpdateAllEnemyInfo()
        {
            foreach (var container in enemyInfoContainers.Values)
            {
                if (container.enemyInstance != null)
                {
                    UpdateEnemyInfo(container.enemyInstance);
                }
            }
        }

        /// <summary>
        /// 敵情報をクリア
        /// </summary>
        public void ClearEnemyInfo()
        {
            foreach (var container in enemyInfoContainers.Values)
            {
                if (container.containerPanel != null)
                {
                    Destroy(container.containerPanel);
                }
            }
            enemyInfoContainers.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 敵情報UIコンテナ
    /// </summary>
    [System.Serializable]
    public class EnemyInfoContainer
    {
        public int enemyId;
        public EnemyInstance enemyInstance;
        
        [Header("UI要素")]
        public GameObject containerPanel;
        public TextMeshProUGUI nameText;
        public Slider hpSlider;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI positionText;
        public TextMeshProUGUI statusText;
    }
}