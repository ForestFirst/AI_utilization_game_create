using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem.UI
{
    /// <summary>
    /// サイバーパンク風戦闘結果画面UI
    /// 戦闘結果の表示、報酬獲得、次のアクションへの遷移
    /// </summary>
    public class ResultUI : MonoBehaviour
    {
        [Header("UI設定")]
        [SerializeField] private bool autoCreateUI = true;
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private float displayDelay = 0.2f;
        
        [Header("サイバーパンク色設定")]
        [SerializeField] private Color primaryGlowColor = new Color(0f, 1f, 1f, 1f); // シアン
        [SerializeField] private Color secondaryGlowColor = new Color(1f, 0f, 1f, 1f); // マゼンタ
        [SerializeField] private Color victoryColor = new Color(0f, 1f, 0f, 1f); // 緑
        [SerializeField] private Color defeatColor = new Color(1f, 0f, 0f, 1f); // 赤
        [SerializeField] private Color rewardColor = new Color(1f, 1f, 0f, 1f); // 黄
        
        // UI要素
        private Canvas mainCanvas;
        private GameObject resultContainer;
        private GameObject rewardContainer;
        private GameObject statisticsContainer;
        private GameObject buttonContainer;
        
        // テキスト要素
        private TextMeshProUGUI resultTitleText;
        private TextMeshProUGUI stageNameText;
        private TextMeshProUGUI resultStatusText;
        private TextMeshProUGUI statisticsText;
        private TextMeshProUGUI rewardText;
        
        // ボタン要素
        private Button continueButton;
        private Button retryButton;
        private Button stageSelectButton;
        
        // データ
        private BattleResult battleResult;
        private StageResult stageResult;
        private bool isVictory;
        
        // システム参照
        private PlayerDataManager playerDataManager;
        private StageManager stageManager;
        private SceneTransitionManager sceneTransition;
        private GameEventManager eventManager;
        
        // アニメーション状態
        private bool isAnimating = false;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeReferences();
            
            if (autoCreateUI)
            {
                CreateResultUI();
            }
            
            SetupEventSubscriptions();
            
            // 初期状態では非表示
            gameObject.SetActive(false);
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
            playerDataManager = PlayerDataManager.Instance;
            stageManager = StageManager.Instance;
            sceneTransition = SceneTransitionManager.Instance;
            eventManager = GameEventManager.Instance;
            
            if (playerDataManager == null)
            {
                Debug.LogError("[ResultUI] PlayerDataManager not found!");
            }
            
            if (stageManager == null)
            {
                Debug.LogError("[ResultUI] StageManager not found!");
            }
        }

        /// <summary>
        /// イベント購読設定
        /// </summary>
        private void SetupEventSubscriptions()
        {
            // GameEventManagerのイベント購読
            GameEventManager.OnBattleCompleted += OnBattleCompleted;
            GameEventManager.OnStageCompleted += OnStageCompleted;
        }

        /// <summary>
        /// イベント購読解除
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            GameEventManager.OnBattleCompleted -= OnBattleCompleted;
            GameEventManager.OnStageCompleted -= OnStageCompleted;
        }

        #endregion

        #region UI Creation

        /// <summary>
        /// 結果UI作成
        /// </summary>
        private void CreateResultUI()
        {
            CreateMainCanvas();
            CreateBackground();
            CreateResultSection();
            CreateStatisticsSection();
            CreateRewardSection();
            CreateButtonSection();
            
            Debug.Log("[ResultUI] Result UI created successfully");
        }

        /// <summary>
        /// メインCanvas作成
        /// </summary>
        private void CreateMainCanvas()
        {
            var canvasObj = new GameObject("ResultCanvas");
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
            backgroundImg.color = new Color(0.02f, 0.02f, 0.05f, 0.98f);
        }

        /// <summary>
        /// 結果セクション作成
        /// </summary>
        private void CreateResultSection()
        {
            resultContainer = new GameObject("ResultContainer");
            resultContainer.transform.SetParent(mainCanvas.transform, false);
            
            var resultRect = resultContainer.AddComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.1f, 0.7f);
            resultRect.anchorMax = new Vector2(0.9f, 0.95f);
            resultRect.sizeDelta = Vector2.zero;
            resultRect.anchoredPosition = Vector2.zero;
            
            // 結果タイトル
            resultTitleText = CreateCyberpunkText(resultContainer.transform, "戦闘結果", 48);
            var titleRect = resultTitleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.6f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;
            
            // ステージ名
            stageNameText = CreateCyberpunkText(resultContainer.transform, "ステージ名", 24);
            var stageRect = stageNameText.GetComponent<RectTransform>();
            stageRect.anchorMin = new Vector2(0, 0.3f);
            stageRect.anchorMax = new Vector2(1, 0.6f);
            stageRect.sizeDelta = Vector2.zero;
            stageRect.anchoredPosition = Vector2.zero;
            
            // 結果ステータス
            resultStatusText = CreateCyberpunkText(resultContainer.transform, "勝利", 36);
            var statusRect = resultStatusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0f);
            statusRect.anchorMax = new Vector2(1, 0.3f);
            statusRect.sizeDelta = Vector2.zero;
            statusRect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// 統計セクション作成
        /// </summary>
        private void CreateStatisticsSection()
        {
            statisticsContainer = new GameObject("StatisticsContainer");
            statisticsContainer.transform.SetParent(mainCanvas.transform, false);
            
            var statsRect = statisticsContainer.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.05f, 0.4f);
            statsRect.anchorMax = new Vector2(0.45f, 0.65f);
            statsRect.sizeDelta = Vector2.zero;
            statsRect.anchoredPosition = Vector2.zero;
            
            // 統計パネル背景
            var statsBg = statisticsContainer.AddComponent<Image>();
            statsBg.color = new Color(0.1f, 0.15f, 0.3f, 0.9f);
            
            var statsOutline = statisticsContainer.AddComponent<Outline>();
            statsOutline.effectColor = primaryGlowColor;
            statsOutline.effectDistance = new Vector2(2, 2);
            
            // 統計タイトル
            var statsTitle = CreateCyberpunkText(statisticsContainer.transform, "戦闘統計", 20);
            var titleRect = statsTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;
            
            // 統計詳細テキスト
            statisticsText = CreateCyberpunkText(statisticsContainer.transform, "", 16);
            var detailRect = statisticsText.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.05f, 0.05f);
            detailRect.anchorMax = new Vector2(0.95f, 0.8f);
            detailRect.sizeDelta = Vector2.zero;
            detailRect.anchoredPosition = Vector2.zero;
            statisticsText.alignment = TextAlignmentOptions.TopLeft;
        }

        /// <summary>
        /// 報酬セクション作成
        /// </summary>
        private void CreateRewardSection()
        {
            rewardContainer = new GameObject("RewardContainer");
            rewardContainer.transform.SetParent(mainCanvas.transform, false);
            
            var rewardRect = rewardContainer.AddComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(0.55f, 0.4f);
            rewardRect.anchorMax = new Vector2(0.95f, 0.65f);
            rewardRect.sizeDelta = Vector2.zero;
            rewardRect.anchoredPosition = Vector2.zero;
            
            // 報酬パネル背景
            var rewardBg = rewardContainer.AddComponent<Image>();
            rewardBg.color = new Color(0.15f, 0.3f, 0.15f, 0.9f);
            
            var rewardOutline = rewardContainer.AddComponent<Outline>();
            rewardOutline.effectColor = rewardColor;
            rewardOutline.effectDistance = new Vector2(2, 2);
            
            // 報酬タイトル
            var rewardTitle = CreateCyberpunkText(rewardContainer.transform, "獲得報酬", 20);
            var titleRect = rewardTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;
            rewardTitle.color = rewardColor;
            
            // 報酬詳細テキスト
            rewardText = CreateCyberpunkText(rewardContainer.transform, "", 16);
            var detailRect = rewardText.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.05f, 0.05f);
            detailRect.anchorMax = new Vector2(0.95f, 0.8f);
            detailRect.sizeDelta = Vector2.zero;
            detailRect.anchoredPosition = Vector2.zero;
            rewardText.alignment = TextAlignmentOptions.TopLeft;
            rewardText.color = Color.white;
        }

        /// <summary>
        /// ボタンセクション作成
        /// </summary>
        private void CreateButtonSection()
        {
            buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(mainCanvas.transform, false);
            
            var buttonRect = buttonContainer.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.2f, 0.1f);
            buttonRect.anchorMax = new Vector2(0.8f, 0.3f);
            buttonRect.sizeDelta = Vector2.zero;
            buttonRect.anchoredPosition = Vector2.zero;
            
            // ボタン配置用レイアウト
            var layoutGroup = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 50f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;
            
            // 継続ボタン（次のステージまたはステージ選択）
            continueButton = CreateCyberpunkButton(buttonContainer.transform,
                Vector2.zero, new Vector2(180, 60), "次のステージ", OnContinueClicked);
            
            // リトライボタン
            retryButton = CreateCyberpunkButton(buttonContainer.transform,
                Vector2.zero, new Vector2(180, 60), "リトライ", OnRetryClicked);
            
            // ステージ選択ボタン
            stageSelectButton = CreateCyberpunkButton(buttonContainer.transform,
                Vector2.zero, new Vector2(180, 60), "ステージ選択", OnStageSelectClicked);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 結果画面を表示
        /// </summary>
        /// <param name="battleResult">戦闘結果</param>
        /// <param name="stageResult">ステージ結果</param>
        public void ShowResult(BattleResult battleResult, StageResult stageResult = null)
        {
            this.battleResult = battleResult;
            this.stageResult = stageResult;
            this.isVictory = battleResult.victory;
            
            gameObject.SetActive(true);
            StartCoroutine(AnimateResultDisplay());
            
            Debug.Log($"[ResultUI] Showing result: Victory={isVictory}");
        }

        /// <summary>
        /// 結果画面を非表示
        /// </summary>
        public void HideResult()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Animation

        /// <summary>
        /// 結果表示アニメーション
        /// </summary>
        private IEnumerator AnimateResultDisplay()
        {
            isAnimating = true;
            
            // 初期状態設定（透明）
            SetContainerAlpha(resultContainer, 0f);
            SetContainerAlpha(statisticsContainer, 0f);
            SetContainerAlpha(rewardContainer, 0f);
            SetContainerAlpha(buttonContainer, 0f);
            
            // データ更新
            UpdateResultDisplay();
            
            // 順次フェードイン
            yield return StartCoroutine(FadeInContainer(resultContainer, animationDuration));
            yield return new WaitForSeconds(displayDelay);
            
            yield return StartCoroutine(FadeInContainer(statisticsContainer, animationDuration));
            yield return new WaitForSeconds(displayDelay);
            
            // 勝利時のみ報酬表示
            if (isVictory)
            {
                yield return StartCoroutine(FadeInContainer(rewardContainer, animationDuration));
                yield return new WaitForSeconds(displayDelay);
            }
            
            yield return StartCoroutine(FadeInContainer(buttonContainer, animationDuration));
            
            isAnimating = false;
        }

        /// <summary>
        /// コンテナのフェードインアニメーション
        /// </summary>
        private IEnumerator FadeInContainer(GameObject container, float duration)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                float alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
                SetContainerAlpha(container, alpha);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            SetContainerAlpha(container, 1f);
        }

        /// <summary>
        /// コンテナのアルファ値設定
        /// </summary>
        private void SetContainerAlpha(GameObject container, float alpha)
        {
            if (container == null) return;
            
            var canvasGroup = container.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = container.AddComponent<CanvasGroup>();
            }
            
            canvasGroup.alpha = alpha;
        }

        #endregion

        #region Display Update

        /// <summary>
        /// 結果表示を更新
        /// </summary>
        private void UpdateResultDisplay()
        {
            if (battleResult == null) return;
            
            // 結果タイトル
            resultTitleText.text = isVictory ? "戦闘勝利!" : "戦闘敗北";
            resultTitleText.color = isVictory ? victoryColor : defeatColor;
            
            // ステージ名（ステージ結果がある場合）
            if (stageResult != null)
            {
                stageNameText.text = $"ステージ: {stageResult.stageId}";
            }
            else
            {
                stageNameText.text = "バトルテスト";
            }
            
            // 結果ステータス
            if (isVictory)
            {
                resultStatusText.text = "VICTORY";
                resultStatusText.color = victoryColor;
            }
            else
            {
                resultStatusText.text = "DEFEAT";
                resultStatusText.color = defeatColor;
            }
            
            // 統計情報
            UpdateStatisticsDisplay();
            
            // 報酬情報（勝利時のみ）
            if (isVictory)
            {
                UpdateRewardDisplay();
                rewardContainer.SetActive(true);
            }
            else
            {
                rewardContainer.SetActive(false);
            }
            
            // ボタン状態
            UpdateButtonStates();
        }

        /// <summary>
        /// 統計表示を更新
        /// </summary>
        private void UpdateStatisticsDisplay()
        {
            if (statisticsText == null || battleResult == null) return;
            
            string statsText = "";
            statsText += $"<color=#FFFF00>経過ターン数:</color> {battleResult.turnsTaken}\n";
            statsText += $"<color=#FFFF00>戦闘時間:</color> {battleResult.timeTaken:F1}秒\n";
            statsText += $"<color=#FFFF00>与えたダメージ:</color> {battleResult.damageDealt:N0}\n";
            statsText += $"<color=#FFFF00>受けたダメージ:</color> {battleResult.damageTaken:N0}\n";
            
            if (battleResult.damageDealt > 0 && battleResult.turnsTaken > 0)
            {
                float dps = battleResult.damageDealt / battleResult.timeTaken;
                float avgDamagePerTurn = battleResult.damageDealt / (float)battleResult.turnsTaken;
                statsText += $"<color=#00FFFF>DPS:</color> {dps:F1}\n";
                statsText += $"<color=#00FFFF>ターン平均ダメージ:</color> {avgDamagePerTurn:F0}\n";
            }
            
            // スタッツスコア
            if (stageResult != null)
            {
                statsText += $"\n<color=#FF00FF>スコア:</color> {stageResult.score:N0}";
                if (stageResult.stars > 0)
                {
                    statsText += $"\n<color=#FFD700>評価:</color> ";
                    for (int i = 0; i < stageResult.stars; i++)
                    {
                        statsText += "★";
                    }
                }
            }
            
            statisticsText.text = statsText;
        }

        /// <summary>
        /// 報酬表示を更新
        /// </summary>
        private void UpdateRewardDisplay()
        {
            if (rewardText == null || battleResult == null) return;
            
            string rewardStr = "";
            
            if (battleResult.experienceGained > 0)
            {
                rewardStr += $"<color=#00FF00>経験値:</color> +{battleResult.experienceGained}\n";
            }
            
            if (battleResult.goldGained > 0)
            {
                rewardStr += $"<color=#FFD700>ゴールド:</color> +{battleResult.goldGained}\n";
            }
            
            if (battleResult.itemsObtained != null && battleResult.itemsObtained.Count > 0)
            {
                rewardStr += "\n<color=#FF00FF>獲得アイテム:</color>\n";
                foreach (var itemId in battleResult.itemsObtained)
                {
                    rewardStr += $"• {itemId}\n";
                }
            }
            
            if (string.IsNullOrEmpty(rewardStr))
            {
                rewardStr = "報酬なし";
            }
            
            rewardText.text = rewardStr;
        }

        /// <summary>
        /// ボタン状態を更新
        /// </summary>
        private void UpdateButtonStates()
        {
            // 継続ボタン（勝利時のみ有効）
            continueButton.interactable = isVictory;
            
            // リトライボタン（常に有効）
            retryButton.interactable = true;
            
            // ステージ選択ボタン（常に有効）
            stageSelectButton.interactable = true;
            
            // ボタンテキストの更新
            if (isVictory && stageResult != null)
            {
                continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "次のステージ";
            }
            else
            {
                continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "継続";
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 戦闘完了イベント処理
        /// </summary>
        private void OnBattleCompleted(bool victory, BattleResult result)
        {
            ShowResult(result);
        }

        /// <summary>
        /// ステージ完了イベント処理
        /// </summary>
        private void OnStageCompleted(StageData stageData, bool success, StageResult result)
        {
            if (battleResult != null)
            {
                ShowResult(battleResult, result);
            }
        }

        /// <summary>
        /// 継続ボタンクリック処理
        /// </summary>
        private void OnContinueClicked()
        {
            if (isAnimating) return;
            
            // 次のステージまたはステージ選択へ
            if (sceneTransition != null)
            {
                sceneTransition.TransitionToScene("StageSelectionScene");
            }
        }

        /// <summary>
        /// リトライボタンクリック処理
        /// </summary>
        private void OnRetryClicked()
        {
            if (isAnimating) return;
            
            // 同じステージをリトライ
            if (stageResult != null && sceneTransition != null)
            {
                var retryData = new BattleStartData
                {
                    stageId = stageResult.stageId,
                    stageName = stageResult.stageId,
                    playerData = playerDataManager?.PlayerData,
                    startTime = DateTime.Now
                };
                
                sceneTransition.TransitionToScene("GameScene", retryData);
            }
        }

        /// <summary>
        /// ステージ選択ボタンクリック処理
        /// </summary>
        private void OnStageSelectClicked()
        {
            if (isAnimating) return;
            
            if (sceneTransition != null)
            {
                sceneTransition.TransitionToScene("StageSelectionScene");
            }
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
            buttonRect.sizeDelta = size;
            
            var button = buttonObj.AddComponent<Button>();
            var buttonImg = buttonObj.AddComponent<Image>();
            
            // レイアウト要素として設定
            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
            
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
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            button.colors = colors;
            
            button.onClick.AddListener(onClick);
            
            return button;
        }

        #endregion

        #region Debug

        /// <summary>
        /// テスト用結果表示
        /// </summary>
        [ContextMenu("Show Test Victory")]
        public void ShowTestVictory()
        {
            var testResult = new BattleResult
            {
                victory = true,
                turnsTaken = 12,
                timeTaken = 145.5f,
                damageDealt = 8500,
                damageTaken = 2300,
                experienceGained = 250,
                goldGained = 150,
                itemsObtained = new List<string> { "ポーション", "魔法石" },
                completionTime = DateTime.Now
            };
            
            var testStageResult = new StageResult
            {
                stageId = "test_stage",
                cleared = true,
                score = 15000,
                stars = 3,
                battleResult = testResult,
                isFirstClear = true
            };
            
            ShowResult(testResult, testStageResult);
        }

        /// <summary>
        /// テスト用敗北結果表示
        /// </summary>
        [ContextMenu("Show Test Defeat")]
        public void ShowTestDefeat()
        {
            var testResult = new BattleResult
            {
                victory = false,
                turnsTaken = 8,
                timeTaken = 95.2f,
                damageDealt = 3200,
                damageTaken = 15000,
                experienceGained = 0,
                goldGained = 0,
                itemsObtained = new List<string>(),
                completionTime = DateTime.Now
            };
            
            ShowResult(testResult);
        }

        #endregion
    }
}