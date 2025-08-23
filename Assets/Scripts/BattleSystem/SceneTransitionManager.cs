using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BattleSystem
{
    /// <summary>
    /// シーン遷移管理システム
    /// フェードイン・フェードアウト、データ受け渡し管理
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("フェード設定")]
        [SerializeField] private float fadeInDuration = 1f;
        [SerializeField] private float fadeOutDuration = 1f;
        [SerializeField] private Color fadeColor = Color.black;
        
        [Header("UI設定")]
        [SerializeField] private bool autoCreateFadeUI = true;
        [SerializeField] private int fadeUIOrder = 1000;
        
        // フェードUI要素
        private Canvas fadeCanvas;
        private Image fadeImage;
        private bool isTransitioning = false;
        
        // データ受け渡し用
        private static object transitionData;
        
        // イベント
        public static event Action<string> OnSceneTransitionStarted;
        public static event Action<string> OnSceneTransitionCompleted;
        
        // シングルトン
        public static SceneTransitionManager Instance { get; private set; }
        
        // プロパティ
        public bool IsTransitioning => isTransitioning;
        public static object TransitionData => transitionData;

        #region Unity Lifecycle

        private void Awake()
        {
            // シングルトン設定
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeTransitionManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (autoCreateFadeUI)
            {
                CreateFadeUI();
            }
            
            // シーン読み込み完了イベントを購読
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// 遷移マネージャーの初期化
        /// </summary>
        private void InitializeTransitionManager()
        {
            Debug.Log("[SceneTransitionManager] Initialized");
        }

        /// <summary>
        /// フェードUI作成
        /// </summary>
        private void CreateFadeUI()
        {
            // フェード用Canvas作成
            var fadeCanvasObj = new GameObject("FadeCanvas");
            fadeCanvasObj.transform.SetParent(transform, false);
            DontDestroyOnLoad(fadeCanvasObj);
            
            fadeCanvas = fadeCanvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = fadeUIOrder;
            
            var canvasScaler = fadeCanvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            fadeCanvasObj.AddComponent<GraphicRaycaster>();
            
            // フェード用Image作成
            var fadeImageObj = new GameObject("FadeImage");
            fadeImageObj.transform.SetParent(fadeCanvasObj.transform, false);
            
            var fadeRect = fadeImageObj.AddComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.sizeDelta = Vector2.zero;
            fadeRect.anchoredPosition = Vector2.zero;
            
            fadeImage = fadeImageObj.AddComponent<Image>();
            fadeImage.color = fadeColor;
            fadeImage.raycastTarget = false;
            
            // 初期状態では非表示
            fadeCanvas.gameObject.SetActive(false);
            
            Debug.Log("[SceneTransitionManager] Fade UI created");
        }

        #endregion

        #region Scene Transition

        /// <summary>
        /// シーン遷移（フェード付き）
        /// </summary>
        /// <param name="sceneName">遷移先シーン名</param>
        /// <param name="data">受け渡しデータ</param>
        public void TransitionToScene(string sceneName, object data = null)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[SceneTransitionManager] Already transitioning, ignoring request");
                return;
            }
            
            StartCoroutine(TransitionCoroutine(sceneName, data));
        }

        /// <summary>
        /// シーン遷移コルーチン
        /// </summary>
        private IEnumerator TransitionCoroutine(string sceneName, object data)
        {
            isTransitioning = true;
            transitionData = data;
            
            Debug.Log($"[SceneTransitionManager] Starting transition to: {sceneName}");
            OnSceneTransitionStarted?.Invoke(sceneName);
            
            // フェードアウト
            yield return StartCoroutine(FadeOut());
            
            // シーン読み込み
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            // 少し待機（新しいシーンの初期化時間）
            yield return new WaitForSeconds(0.1f);
            
            // フェードイン
            yield return StartCoroutine(FadeIn());
            
            isTransitioning = false;
            OnSceneTransitionCompleted?.Invoke(sceneName);
            Debug.Log($"[SceneTransitionManager] Transition completed to: {sceneName}");
        }

        /// <summary>
        /// 即座にシーン遷移（フェードなし）
        /// </summary>
        /// <param name="sceneName">遷移先シーン名</param>
        /// <param name="data">受け渡しデータ</param>
        public void TransitionToSceneImmediate(string sceneName, object data = null)
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[SceneTransitionManager] Already transitioning, ignoring immediate request");
                return;
            }
            
            transitionData = data;
            OnSceneTransitionStarted?.Invoke(sceneName);
            
            SceneManager.LoadScene(sceneName);
        }

        #endregion

        #region Fade Effects

        /// <summary>
        /// フェードアウト
        /// </summary>
        private IEnumerator FadeOut()
        {
            if (fadeCanvas == null || fadeImage == null)
            {
                yield break;
            }
            
            fadeCanvas.gameObject.SetActive(true);
            
            Color startColor = fadeColor;
            startColor.a = 0f;
            Color endColor = fadeColor;
            endColor.a = 1f;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeOutDuration;
                fadeImage.color = Color.Lerp(startColor, endColor, progress);
                yield return null;
            }
            
            fadeImage.color = endColor;
        }

        /// <summary>
        /// フェードイン
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (fadeCanvas == null || fadeImage == null)
            {
                yield break;
            }
            
            Color startColor = fadeColor;
            startColor.a = 1f;
            Color endColor = fadeColor;
            endColor.a = 0f;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / fadeInDuration;
                fadeImage.color = Color.Lerp(startColor, endColor, progress);
                yield return null;
            }
            
            fadeImage.color = endColor;
            fadeCanvas.gameObject.SetActive(false);
        }

        /// <summary>
        /// 手動フェードアウト（他のスクリプトから呼び出し用）
        /// </summary>
        public void FadeOutManual(Action onComplete = null)
        {
            StartCoroutine(FadeOutCoroutine(onComplete));
        }

        /// <summary>
        /// 手動フェードイン（他のスクリプトから呼び出し用）
        /// </summary>
        public void FadeInManual(Action onComplete = null)
        {
            StartCoroutine(FadeInCoroutine(onComplete));
        }

        private IEnumerator FadeOutCoroutine(Action onComplete)
        {
            yield return StartCoroutine(FadeOut());
            onComplete?.Invoke();
        }

        private IEnumerator FadeInCoroutine(Action onComplete)
        {
            yield return StartCoroutine(FadeIn());
            onComplete?.Invoke();
        }

        #endregion

        #region Data Management

        /// <summary>
        /// 遷移データを取得して消去
        /// </summary>
        /// <typeparam name="T">データ型</typeparam>
        /// <returns>遷移データ</returns>
        public static T GetTransitionData<T>() where T : class
        {
            var data = transitionData as T;
            transitionData = null; // 取得後は消去
            return data;
        }

        /// <summary>
        /// 遷移データを取得（消去しない）
        /// </summary>
        /// <typeparam name="T">データ型</typeparam>
        /// <returns>遷移データ</returns>
        public static T PeekTransitionData<T>() where T : class
        {
            return transitionData as T;
        }

        /// <summary>
        /// 遷移データをクリア
        /// </summary>
        public static void ClearTransitionData()
        {
            transitionData = null;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// シーン読み込み完了時の処理
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[SceneTransitionManager] Scene loaded: {scene.name}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在のシーン名を取得
        /// </summary>
        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// 指定されたシーンが存在するかチェック
        /// </summary>
        public bool IsSceneValid(string sceneName)
        {
            try
            {
                return Application.CanStreamedLevelBeLoaded(sceneName);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// デバッグ用シーン遷移テスト
        /// </summary>
        [ContextMenu("Test Transition")]
        public void TestTransition()
        {
            if (GetCurrentSceneName() != "TitleScene")
            {
                TransitionToScene("TitleScene", "Debug transition test");
            }
            else
            {
                Debug.Log("[SceneTransitionManager] Already in TitleScene");
            }
        }

        #endregion
    }

    /// <summary>
    /// シーン遷移用データクラス
    /// </summary>
    [Serializable]
    public class SceneTransitionData
    {
        public string fromScene;
        public string toScene;
        public DateTime transitionTime;
        public object customData;

        public SceneTransitionData(string from, string to, object data = null)
        {
            fromScene = from;
            toScene = to;
            transitionTime = DateTime.Now;
            customData = data;
        }
    }
}