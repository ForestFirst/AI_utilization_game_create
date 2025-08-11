using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace BattleSystem
{
    /// <summary>
    /// コンボUI表示・管理システム
    /// コンボの進行状況、効果プレビュー、完成通知を提供
    /// </summary>
    public class ComboUI : MonoBehaviour
    {
        [Header("コンボUI設定")]
        [SerializeField] private bool autoCreateComboUI = true;
        [SerializeField] private Vector2 comboUIPosition = new Vector2(350, 200);
        [SerializeField] private Vector2 comboPanelSize = new Vector2(300f, 400f);
        [SerializeField] private float comboSpacing = 80f;
        [SerializeField] private int maxDisplayCombos = 5;

        [Header("アニメーション設定")]
        [SerializeField] private float progressAnimationDuration = 0.3f;
        [SerializeField] private float completeEffectDuration = 1.0f;
        [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // 日本語フォント
        private TMP_FontAsset japaneseFont;
        
        // システム参照
        private Canvas canvas;
        private ComboSystem comboSystem;
        private BattleManager battleManager;
        
        // UI要素
        private GameObject comboPanel;
        private TextMeshProUGUI comboTitleText;
        private List<ComboUIEntry> comboEntries;
        private GameObject comboCompleteEffect;
        private AudioSource audioSource;
        
        // 状態管理
        private Dictionary<ComboData, ComboUIEntry> activeComboEntries;
        private Queue<ComboUIEntry> pooledEntries;
        
        void Start()
        {
            SetupComponents();
            LoadJapaneseFont();
            
            if (autoCreateComboUI)
            {
                CreateComboUI();
            }
            
            SetupComboSystemConnection();
        }

        /// <summary>
        /// 必要なコンポーネントを設定
        /// </summary>
        void SetupComponents()
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("ComboUI: Canvas not found!");
                return;
            }

            // AudioSource設定（効果音用）
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            
            Debug.Log("ComboUI: Components setup completed");
        }

        /// <summary>
        /// 日本語フォント読み込み
        /// </summary>
        void LoadJapaneseFont()
        {
            japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/DotGothic16-Regular SDF");
            
            if (japaneseFont == null)
            {
                Debug.LogWarning("ComboUI: Japanese font not found, using fallback");
                japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
        }

        /// <summary>
        /// ComboSystemとの接続設定
        /// </summary>
        void SetupComboSystemConnection()
        {
            comboSystem = FindObjectOfType<ComboSystem>();
            if (comboSystem == null)
            {
                Debug.LogError("ComboUI: ComboSystem not found!");
                return;
            }

            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                Debug.LogError("ComboUI: BattleManager not found!");
                return;
            }

            // ComboSystemのイベントを購読
            SubscribeToComboSystemEvents();
            
            Debug.Log("ComboUI: Connected to ComboSystem and BattleManager");
        }

        /// <summary>
        /// ComboSystemイベント購読
        /// </summary>
        void SubscribeToComboSystemEvents()
        {
            if (comboSystem == null) return;

            comboSystem.OnComboStarted += OnComboStarted;
            comboSystem.OnComboProgressUpdated += OnComboProgressUpdated;
            comboSystem.OnComboCompleted += OnComboCompleted;
            comboSystem.OnComboFailed += OnComboFailed;
            comboSystem.OnComboInterrupted += OnComboInterrupted;
            
            Debug.Log("ComboUI: ComboSystem events subscribed");
        }

        void OnDestroy()
        {
            // イベント購読解除
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
        /// コンボUIを作成
        /// </summary>
        void CreateComboUI()
        {
            if (canvas == null)
            {
                Debug.LogError("Cannot create ComboUI: Canvas is null");
                return;
            }

            Debug.Log("Creating Combo UI...");

            // スケール計算
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float screenWidth = canvasRect.rect.width;
            float screenHeight = canvasRect.rect.height;
            float scaleX = screenWidth / 1920f;
            float scaleY = screenHeight / 1080f;
            float scale = Mathf.Min(scaleX, scaleY);

            // メインコンボパネル作成（画面右上）
            comboPanel = CreateUIPanel("Combo Panel",
                new Vector2(comboUIPosition.x * scale, comboUIPosition.y * scale),
                new Vector2(comboPanelSize.x * scale, comboPanelSize.y * scale),
                new Color(0.1f, 0.2f, 0.4f, 0.9f));

            // コンボタイトルテキスト
            comboTitleText = CreateUIText("Combo Title",
                new Vector2(comboUIPosition.x * scale, (comboUIPosition.y + comboPanelSize.y * 0.4f) * scale),
                new Vector2(comboPanelSize.x * scale, 30f * scale),
                "コンボ進行状況", 18 * scale);
            comboTitleText.color = Color.yellow;
            comboTitleText.alignment = TextAlignmentOptions.Center;

            // コンボエントリー管理初期化
            InitializeComboEntries(scale);

            // コンボ完成エフェクト作成
            CreateComboCompleteEffect(scale);

            // 初期状態では非表示
            SetComboUIVisible(false);

            Debug.Log("ComboUI: UI creation completed");
        }

        /// <summary>
        /// コンボエントリー初期化
        /// </summary>
        void InitializeComboEntries(float scale)
        {
            comboEntries = new List<ComboUIEntry>();
            activeComboEntries = new Dictionary<ComboData, ComboUIEntry>();
            pooledEntries = new Queue<ComboUIEntry>();

            // 最大表示数分のエントリーを事前作成（オブジェクトプール）
            for (int i = 0; i < maxDisplayCombos; i++)
            {
                ComboUIEntry entry = CreateComboEntry(i, scale);
                comboEntries.Add(entry);
                pooledEntries.Enqueue(entry);
            }

            Debug.Log($"ComboUI: Created {maxDisplayCombos} combo entries");
        }

        /// <summary>
        /// コンボエントリー作成
        /// </summary>
        ComboUIEntry CreateComboEntry(int index, float scale)
        {
            float entryY = comboUIPosition.y - (index * comboSpacing) * scale;
            Vector2 entryPosition = new Vector2(comboUIPosition.x * scale, entryY);
            Vector2 entrySize = new Vector2((comboPanelSize.x - 20f) * scale, 70f * scale);

            // エントリーパネル
            GameObject entryPanel = CreateUIPanel($"Combo Entry {index}",
                entryPosition, entrySize, new Color(0.2f, 0.3f, 0.5f, 0.8f));

            ComboUIEntry entry = new ComboUIEntry
            {
                entryPanel = entryPanel,
                index = index
            };

            // コンボ名テキスト
            entry.comboNameText = CreateUIText($"Combo Name {index}",
                new Vector2(entryPosition.x, entryPosition.y + 15f * scale),
                new Vector2(entrySize.x - 10f * scale, 20f * scale),
                "コンボ名", 14 * scale);
            entry.comboNameText.color = Color.white;
            entry.comboNameText.alignment = TextAlignmentOptions.Left;

            // 進行率バー背景
            entry.progressBarBG = CreateUIPanel($"Progress BG {index}",
                new Vector2(entryPosition.x, entryPosition.y - 10f * scale),
                new Vector2(entrySize.x - 20f * scale, 8f * scale),
                new Color(0.3f, 0.3f, 0.3f, 0.8f));

            // 進行率バー
            entry.progressBar = CreateUIPanel($"Progress Bar {index}",
                new Vector2(entryPosition.x - (entrySize.x - 20f) * 0.5f * scale, entryPosition.y - 10f * scale),
                new Vector2(0f, 8f * scale),
                new Color(0.2f, 0.8f, 0.2f, 1f));
            entry.progressBar.transform.SetParent(entry.progressBarBG.transform, false);

            // 進行率テキスト
            entry.progressText = CreateUIText($"Progress Text {index}",
                new Vector2(entryPosition.x, entryPosition.y - 25f * scale),
                new Vector2(entrySize.x - 10f * scale, 15f * scale),
                "0/0", 12 * scale);
            entry.progressText.color = Color.gray;
            entry.progressText.alignment = TextAlignmentOptions.Right;

            // 初期状態では非表示
            entryPanel.SetActive(false);

            return entry;
        }

        /// <summary>
        /// コンボ完成エフェクト作成
        /// </summary>
        void CreateComboCompleteEffect(float scale)
        {
            comboCompleteEffect = CreateUIPanel("Combo Complete Effect",
                Vector2.zero, new Vector2(400f * scale, 100f * scale),
                new Color(1f, 1f, 0f, 0f)); // 初期は透明

            TextMeshProUGUI effectText = CreateUIText("Effect Text",
                Vector2.zero, new Vector2(400f * scale, 100f * scale),
                "コンボ完成!", 24 * scale);
            effectText.color = Color.yellow;
            effectText.alignment = TextAlignmentOptions.Center;
            effectText.transform.SetParent(comboCompleteEffect.transform, false);

            comboCompleteEffect.SetActive(false);
        }

        /// <summary>
        /// UIパネル作成ヘルパー
        /// </summary>
        GameObject CreateUIPanel(string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(canvas.transform, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = panel.AddComponent<Image>();
            image.color = color;

            return panel;
        }

        /// <summary>
        /// UIテキスト作成ヘルパー
        /// </summary>
        TextMeshProUGUI CreateUIText(string name, Vector2 position, Vector2 size, string text, float fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(canvas.transform, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.color = Color.white;

            if (japaneseFont != null)
            {
                textComponent.font = japaneseFont;
            }

            return textComponent;
        }

        // === ComboSystem イベントハンドラー ===

        /// <summary>
        /// コンボ開始時の処理
        /// </summary>
        void OnComboStarted(ComboData combo)
        {
            Debug.Log($"ComboUI: Combo started - {combo.comboName}");

            if (pooledEntries.Count > 0)
            {
                ComboUIEntry entry = pooledEntries.Dequeue();
                activeComboEntries[combo] = entry;

                // エントリー表示設定
                entry.entryPanel.SetActive(true);
                entry.comboNameText.text = combo.comboName;
                entry.progressText.text = $"1/{combo.requiredWeaponCount}";

                // 進行率バー初期化
                UpdateProgressBar(entry, 1f / combo.requiredWeaponCount);

                // 表示アニメーション
                StartCoroutine(AnimateEntryAppearance(entry));

                // UI表示
                SetComboUIVisible(true);
            }
            else
            {
                Debug.LogWarning("ComboUI: No available entries for new combo");
            }
        }

        /// <summary>
        /// コンボ進行更新時の処理
        /// </summary>
        void OnComboProgressUpdated(ComboProgress progress)
        {
            Debug.Log($"ComboUI: Combo progress updated - {progress.comboData.comboName}: {progress.progressPercentage:P0}");

            if (activeComboEntries.TryGetValue(progress.comboData, out ComboUIEntry entry))
            {
                // 進行率バーをアニメーション付きで更新
                StartCoroutine(AnimateProgressUpdate(entry, progress));

                // 進行率テキスト更新
                entry.progressText.text = $"{progress.currentStep}/{progress.comboData.requiredWeaponCount}";

                // 進行に応じて色変更
                UpdateProgressBarColor(entry, progress.progressPercentage);
            }
        }

        /// <summary>
        /// コンボ完成時の処理
        /// </summary>
        void OnComboCompleted(ComboExecutionResult result)
        {
            Debug.Log($"ComboUI: Combo completed - {result.executedCombo.comboName}");

            if (activeComboEntries.TryGetValue(result.executedCombo, out ComboUIEntry entry))
            {
                // 完成エフェクト再生
                StartCoroutine(PlayComboCompleteEffect(result));

                // エントリーを完成状態にしてから削除
                StartCoroutine(AnimateComboCompletion(entry, result));
            }
        }

        /// <summary>
        /// コンボ失敗時の処理
        /// </summary>
        void OnComboFailed(ComboData combo, string reason)
        {
            Debug.Log($"ComboUI: Combo failed - {combo.comboName}: {reason}");

            if (activeComboEntries.TryGetValue(combo, out ComboUIEntry entry))
            {
                // 失敗アニメーション
                StartCoroutine(AnimateComboFailure(entry, reason));
            }
        }

        /// <summary>
        /// コンボ中断時の処理
        /// </summary>
        void OnComboInterrupted(ComboData combo)
        {
            Debug.Log($"ComboUI: Combo interrupted - {combo.comboName}");

            if (activeComboEntries.TryGetValue(combo, out ComboUIEntry entry))
            {
                // 中断アニメーション
                StartCoroutine(AnimateComboInterruption(entry));
            }
        }

        // === UI更新メソッド ===

        /// <summary>
        /// 進行率バー更新
        /// </summary>
        void UpdateProgressBar(ComboUIEntry entry, float progress)
        {
            if (entry.progressBar != null)
            {
                RectTransform barRect = entry.progressBar.GetComponent<RectTransform>();
                RectTransform bgRect = entry.progressBarBG.GetComponent<RectTransform>();
                
                float maxWidth = bgRect.sizeDelta.x - 4f; // パディング考慮
                float newWidth = maxWidth * progress;
                
                barRect.sizeDelta = new Vector2(newWidth, barRect.sizeDelta.y);
                barRect.anchoredPosition = new Vector2(newWidth * 0.5f - maxWidth * 0.5f, 0f);
            }
        }

        /// <summary>
        /// 進行率バーの色更新
        /// </summary>
        void UpdateProgressBarColor(ComboUIEntry entry, float progress)
        {
            if (entry.progressBar != null)
            {
                Image barImage = entry.progressBar.GetComponent<Image>();
                
                if (progress < 0.5f)
                {
                    // 序盤: 緑 → 黄色
                    barImage.color = Color.Lerp(Color.green, Color.yellow, progress * 2f);
                }
                else
                {
                    // 終盤: 黄色 → オレンジ
                    barImage.color = Color.Lerp(Color.yellow, new Color(1f, 0.5f, 0f), (progress - 0.5f) * 2f);
                }
            }
        }

        /// <summary>
        /// エントリー削除
        /// </summary>
        void RemoveComboEntry(ComboData combo)
        {
            if (activeComboEntries.TryGetValue(combo, out ComboUIEntry entry))
            {
                // エントリーをプールに戻す
                entry.entryPanel.SetActive(false);
                pooledEntries.Enqueue(entry);
                activeComboEntries.Remove(combo);

                // アクティブなコンボがなくなったらUI非表示
                if (activeComboEntries.Count == 0)
                {
                    SetComboUIVisible(false);
                }
            }
        }

        /// <summary>
        /// コンボUI全体の表示/非表示
        /// </summary>
        public void SetComboUIVisible(bool visible)
        {
            if (comboPanel != null)
            {
                comboPanel.SetActive(visible);
            }
            
            if (comboTitleText != null)
            {
                comboTitleText.gameObject.SetActive(visible);
            }

            Debug.Log($"ComboUI: Visibility set to {visible}");
        }

        // === アニメーション ===

        /// <summary>
        /// エントリー出現アニメーション
        /// </summary>
        IEnumerator AnimateEntryAppearance(ComboUIEntry entry)
        {
            RectTransform rect = entry.entryPanel.GetComponent<RectTransform>();
            Vector2 originalPos = rect.anchoredPosition;
            Vector2 startPos = originalPos + new Vector2(comboPanelSize.x, 0);
            
            rect.anchoredPosition = startPos;
            
            float elapsed = 0f;
            while (elapsed < progressAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = progressCurve.Evaluate(elapsed / progressAnimationDuration);
                rect.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
                yield return null;
            }
            
            rect.anchoredPosition = originalPos;
        }

        /// <summary>
        /// 進行率更新アニメーション
        /// </summary>
        IEnumerator AnimateProgressUpdate(ComboUIEntry entry, ComboProgress progress)
        {
            RectTransform barRect = entry.progressBar.GetComponent<RectTransform>();
            RectTransform bgRect = entry.progressBarBG.GetComponent<RectTransform>();
            
            float maxWidth = bgRect.sizeDelta.x - 4f;
            float currentWidth = barRect.sizeDelta.x;
            float targetWidth = maxWidth * progress.progressPercentage;
            
            float elapsed = 0f;
            while (elapsed < progressAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = progressCurve.Evaluate(elapsed / progressAnimationDuration);
                float newWidth = Mathf.Lerp(currentWidth, targetWidth, t);
                
                barRect.sizeDelta = new Vector2(newWidth, barRect.sizeDelta.y);
                barRect.anchoredPosition = new Vector2(newWidth * 0.5f - maxWidth * 0.5f, 0f);
                
                yield return null;
            }
            
            UpdateProgressBar(entry, progress.progressPercentage);
            UpdateProgressBarColor(entry, progress.progressPercentage);
        }

        /// <summary>
        /// コンボ完成アニメーション
        /// </summary>
        IEnumerator AnimateComboCompletion(ComboUIEntry entry, ComboExecutionResult result)
        {
            // 完成時の視覚効果
            Image panelImage = entry.entryPanel.GetComponent<Image>();
            Color originalColor = panelImage.color;
            Color highlightColor = new Color(0.8f, 0.8f, 0.2f, 0.9f);
            
            // ハイライト効果
            float elapsed = 0f;
            while (elapsed < completeEffectDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.PingPong(elapsed * 6f, 1f);
                panelImage.color = Color.Lerp(originalColor, highlightColor, t);
                yield return null;
            }
            
            // フェードアウト
            elapsed = 0f;
            while (elapsed < completeEffectDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / (completeEffectDuration * 0.5f));
                Color fadeColor = originalColor;
                fadeColor.a = alpha;
                panelImage.color = fadeColor;
                
                yield return null;
            }
            
            // エントリー削除
            RemoveComboEntry(result.executedCombo);
        }

        /// <summary>
        /// コンボ失敗アニメーション
        /// </summary>
        IEnumerator AnimateComboFailure(ComboUIEntry entry, string reason)
        {
            // 失敗時の赤い点滅効果
            Image panelImage = entry.entryPanel.GetComponent<Image>();
            Color originalColor = panelImage.color;
            Color failureColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            
            for (int i = 0; i < 3; i++)
            {
                panelImage.color = failureColor;
                yield return new WaitForSeconds(0.1f);
                panelImage.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
            
            // フェードアウトして削除
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / 0.5f);
                Color fadeColor = originalColor;
                fadeColor.a = alpha;
                panelImage.color = fadeColor;
                yield return null;
            }
            
            // エントリーから対応するComboDataを取得して削除
            if (entry.comboData != null)
            {
                RemoveComboEntry(entry.comboData);
            }
        }

        /// <summary>
        /// コンボ中断アニメーション
        /// </summary>
        IEnumerator AnimateComboInterruption(ComboUIEntry entry)
        {
            // 中断時のオレンジ点滅効果
            Image panelImage = entry.entryPanel.GetComponent<Image>();
            Color originalColor = panelImage.color;
            Color interruptColor = new Color(1f, 0.5f, 0.2f, 0.9f);
            
            for (int i = 0; i < 2; i++)
            {
                panelImage.color = interruptColor;
                yield return new WaitForSeconds(0.15f);
                panelImage.color = originalColor;
                yield return new WaitForSeconds(0.15f);
            }
            
            // エントリーから対応するComboDataを取得して削除
            if (entry.comboData != null)
            {
                RemoveComboEntry(entry.comboData);
            }
        }

        /// <summary>
        /// コンボ完成エフェクト再生
        /// </summary>
        IEnumerator PlayComboCompleteEffect(ComboExecutionResult result)
        {
            if (comboCompleteEffect == null) yield break;

            // エフェクト位置を画面中央に設定
            RectTransform effectRect = comboCompleteEffect.GetComponent<RectTransform>();
            effectRect.anchoredPosition = Vector2.zero;

            // エフェクトテキスト更新
            TextMeshProUGUI effectText = comboCompleteEffect.GetComponentInChildren<TextMeshProUGUI>();
            if (effectText != null)
            {
                effectText.text = $"コンボ完成!\n{result.executedCombo.comboName}";
            }

            comboCompleteEffect.SetActive(true);

            // フェードイン
            Image effectImage = comboCompleteEffect.GetComponent<Image>();
            float elapsed = 0f;
            while (elapsed < completeEffectDuration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, 0.8f, elapsed / (completeEffectDuration * 0.3f));
                Color color = effectImage.color;
                color.a = alpha;
                effectImage.color = color;
                yield return null;
            }

            // 表示維持
            yield return new WaitForSeconds(completeEffectDuration * 0.4f);

            // フェードアウト
            elapsed = 0f;
            while (elapsed < completeEffectDuration * 0.3f)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.8f, 0f, elapsed / (completeEffectDuration * 0.3f));
                Color color = effectImage.color;
                color.a = alpha;
                effectImage.color = color;
                yield return null;
            }

            comboCompleteEffect.SetActive(false);
        }

        // === デバッグ機能 ===

        [ContextMenu("Test Combo UI")]
        public void TestComboUI()
        {
            Debug.Log("ComboUI: Testing UI with dummy data...");
            
            if (comboSystem != null && comboSystem.comboDatabase != null)
            {
                var testCombo = comboSystem.comboDatabase.AvailableCombos[0];
                OnComboStarted(testCombo);
                
                // 2秒後に進行更新をテスト
                StartCoroutine(TestProgressUpdate(testCombo));
            }
        }

        IEnumerator TestProgressUpdate(ComboData testCombo)
        {
            yield return new WaitForSeconds(2f);
            
            ComboProgress testProgress = new ComboProgress
            {
                comboData = testCombo,
                currentStep = 2,
                progressPercentage = 0.5f
            };
            
            OnComboProgressUpdated(testProgress);
        }
    }

    /// <summary>
    /// コンボUIエントリー（個別コンボ表示用）
    /// </summary>
    [System.Serializable]
    public class ComboUIEntry
    {
        public GameObject entryPanel;
        public TextMeshProUGUI comboNameText;
        public GameObject progressBarBG;
        public GameObject progressBar;
        public TextMeshProUGUI progressText;
        public ComboData comboData;
        public int index;
    }
}