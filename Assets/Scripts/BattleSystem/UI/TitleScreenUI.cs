using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace BattleSystem.UI
{
    /// <summary>
    /// サイバーパンク風タイトル画面UI（UIモックアップ準拠）
    /// Guardian Protocol - 仮題
    /// </summary>
    public class TitleScreenUI : MonoBehaviour
    {
        [Header("UI設定")]
        [SerializeField] private bool autoCreateUI = true;
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private string settingsSceneName = "SettingsScene";

        // UI要素
        private Canvas mainCanvas;
        private GameObject titleContainer;
        private Button[] menuButtons;

        // アニメーション用
        private float glowTime = 0f;
        private ParticleSystem[] particles;
        
        // システム参照
        private SceneTransitionManager sceneTransition;
        private GameEventManager eventManager;

        #region Unity Lifecycle

        private void Start()
        {
            // システム参照の初期化
            InitializeReferences();
            
            if (autoCreateUI)
            {
                CreateTitleUI();
            }
            SetupEventSystem();
        }
        
        /// <summary>
        /// システム参照の初期化
        /// </summary>
        private void InitializeReferences()
        {
            sceneTransition = SceneTransitionManager.Instance;
            eventManager = GameEventManager.Instance;
            
            if (sceneTransition == null)
            {
                Debug.LogWarning("[TitleScreenUI] SceneTransitionManager not found!");
            }
        }

        private void Update()
        {
            UpdateTitleGlow();
            HandleKeyboardNavigation();
        }

        #endregion

        #region UI Creation

        /// <summary>
        /// タイトルUI作成
        /// </summary>
        private void CreateTitleUI()
        {
            CreateMainCanvas();
            CreateCyberBackground();
            CreateTitleContent();
            CreateMenuButtons();
            CreateParticleEffects();
        }

        /// <summary>
        /// メインCanvas作成
        /// </summary>
        private void CreateMainCanvas()
        {
            var canvasObj = new GameObject("TitleCanvas");
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
        /// サイバーパンク風背景作成
        /// </summary>
        private void CreateCyberBackground()
        {
            // メイン背景
            var backgroundObj = new GameObject("CyberBackground");
            backgroundObj.transform.SetParent(mainCanvas.transform, false);

            var backgroundRect = backgroundObj.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.sizeDelta = Vector2.zero;
            backgroundRect.anchoredPosition = Vector2.zero;

            var backgroundImg = backgroundObj.AddComponent<Image>();
            // グラデーション背景色（ダークブルー系）
            backgroundImg.color = new Color(0.04f, 0.04f, 0.08f, 1f);

            // グリッドオーバーレイ
            CreateGridOverlay(backgroundObj.transform);
        }

        /// <summary>
        /// グリッドオーバーレイ作成
        /// </summary>
        private void CreateGridOverlay(Transform parent)
        {
            var gridObj = new GameObject("GridOverlay");
            gridObj.transform.SetParent(parent, false);

            var gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.sizeDelta = Vector2.zero;
            gridRect.anchoredPosition = Vector2.zero;

            var gridImg = gridObj.AddComponent<Image>();
            gridImg.color = new Color(0f, 1f, 1f, 0.1f); // サイアン色、低透明度

            // TODO: 実際のグリッドテクスチャがあればそれを使用
            // 現在は単色で代用
        }

        /// <summary>
        /// タイトルコンテンツ作成
        /// </summary>
        private void CreateTitleContent()
        {
            titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(mainCanvas.transform, false);

            var titleRect = titleContainer.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(800, 600);
            titleRect.anchoredPosition = Vector2.zero;

            // メインタイトル
            CreateMainTitle(titleContainer.transform);

            // サブタイトル
            CreateSubTitle(titleContainer.transform);
        }

        /// <summary>
        /// メインタイトル作成
        /// </summary>
        private void CreateMainTitle(Transform parent)
        {
            var titleObj = new GameObject("GameTitle");
            titleObj.transform.SetParent(parent, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.6f);
            titleRect.anchorMax = new Vector2(1, 0.9f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "GUARDIAN PROTOCOL";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 64;
            titleText.color = new Color(0f, 1f, 1f, 1f); // サイアン色
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;

            // グロー効果はUpdateで処理
        }

        /// <summary>
        /// サブタイトル作成
        /// </summary>
        private void CreateSubTitle(Transform parent)
        {
            var subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(parent, false);

            var subtitleRect = subtitleObj.AddComponent<RectTransform>();
            subtitleRect.anchorMin = new Vector2(0, 0.45f);
            subtitleRect.anchorMax = new Vector2(1, 0.55f);
            subtitleRect.sizeDelta = Vector2.zero;
            subtitleRect.anchoredPosition = Vector2.zero;

            var subtitleText = subtitleObj.AddComponent<Text>();
            subtitleText.text = "仮題";
            subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtitleText.fontSize = 24;
            subtitleText.color = new Color(0.4f, 0.8f, 1f, 0.8f);
            subtitleText.alignment = TextAnchor.MiddleCenter;
            subtitleText.fontStyle = FontStyle.Normal;
        }

        /// <summary>
        /// メニューボタン作成
        /// </summary>
        private void CreateMenuButtons()
        {
            var menuContainer = new GameObject("MenuContainer");
            menuContainer.transform.SetParent(titleContainer.transform, false);

            var menuRect = menuContainer.AddComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.3f, 0.1f);
            menuRect.anchorMax = new Vector2(0.7f, 0.4f);
            menuRect.sizeDelta = Vector2.zero;
            menuRect.anchoredPosition = Vector2.zero;

            // 縦方向レイアウト
            var layoutGroup = menuContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 15f;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            // メニューボタン作成
            menuButtons = new Button[3];
            menuButtons[0] = CreateMenuButton(menuContainer.transform, "ゲーム開始", OnGameStart);
            menuButtons[1] = CreateMenuButton(menuContainer.transform, "設定", OnSettings);
            menuButtons[2] = CreateMenuButton(menuContainer.transform, "終了", OnExit);

            // 最初のボタンを選択状態に
            if (menuButtons.Length > 0)
            {
                menuButtons[0].Select();
            }
        }

        /// <summary>
        /// 個別メニューボタン作成
        /// </summary>
        private Button CreateMenuButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            var buttonObj = new GameObject($"MenuButton_{text}");
            buttonObj.transform.SetParent(parent, false);

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 60;
            layoutElement.minHeight = 60;

            var button = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();

            // サイバーパンク風ボタンスタイル
            image.color = new Color(0f, 1f, 1f, 0.1f); // 透明なサイアン

            // ボタンテキスト
            var textObj = new GameObject("ButtonText");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = text.ToUpper();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 24;
            buttonText.color = new Color(0f, 1f, 1f, 1f);
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontStyle = FontStyle.Bold;

            button.targetGraphic = image;
            button.onClick.AddListener(action);

            // ホバー効果
            var colors = button.colors;
            colors.normalColor = new Color(0f, 1f, 1f, 0.1f);
            colors.highlightedColor = new Color(0f, 1f, 1f, 0.3f);
            colors.pressedColor = new Color(0f, 1f, 1f, 0.5f);
            colors.selectedColor = new Color(0f, 1f, 1f, 0.2f);
            colors.fadeDuration = 0.3f;
            button.colors = colors;

            return button;
        }

        /// <summary>
        /// パーティクルエフェクト作成
        /// </summary>
        private void CreateParticleEffects()
        {
            // 簡易パーティクル効果（GameObject+移動）
            CreateFloatingParticles();
        }

        /// <summary>
        /// 浮遊パーティクル作成
        /// </summary>
        private void CreateFloatingParticles()
        {
            var particlesContainer = new GameObject("FloatingParticles");
            particlesContainer.transform.SetParent(mainCanvas.transform, false);

            var particlesRect = particlesContainer.AddComponent<RectTransform>();
            particlesRect.anchorMin = Vector2.zero;
            particlesRect.anchorMax = Vector2.one;
            particlesRect.sizeDelta = Vector2.zero;
            particlesRect.anchoredPosition = Vector2.zero;

            // 複数の小さなパーティクル作成
            for (int i = 0; i < 20; i++)
            {
                CreateSingleParticle(particlesContainer.transform, i);
            }
        }

        /// <summary>
        /// 単一パーティクル作成
        /// </summary>
        private void CreateSingleParticle(Transform parent, int index)
        {
            var particleObj = new GameObject($"Particle_{index}");
            particleObj.transform.SetParent(parent, false);

            var particleRect = particleObj.AddComponent<RectTransform>();
            particleRect.sizeDelta = new Vector2(4, 4);
            particleRect.anchoredPosition = new Vector2(
                UnityEngine.Random.Range(-960, 960),
                UnityEngine.Random.Range(-540, 540)
            );

            var particleImg = particleObj.AddComponent<Image>();
            particleImg.color = new Color(0f, 1f, 1f, 0.7f);

            // 簡易アニメーション（TODO: より洗練されたエフェクトに置き換え可能）
            StartCoroutine(AnimateParticle(particleObj, UnityEngine.Random.Range(2f, 6f)));
        }

        /// <summary>
        /// パーティクルアニメーション
        /// </summary>
        private IEnumerator AnimateParticle(GameObject particle, float duration)
        {
            if (particle == null) yield break;
            
            var rectTransform = particle.GetComponent<RectTransform>();
            var image = particle.GetComponent<Image>();
            
            if (rectTransform == null || image == null) yield break;
            
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(UnityEngine.Random.Range(-100, 100), UnityEngine.Random.Range(-100, 100));
            Color startColor = image.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
            
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                if (particle == null) yield break;
                
                float progress = elapsedTime / duration;
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
                image.color = Color.Lerp(startColor, endColor, progress);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            if (particle != null)
                Destroy(particle);
        }

        #endregion

        #region Event System

        /// <summary>
        /// EventSystem確認・作成
        /// </summary>
        private void SetupEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// タイトルグロー効果更新
        /// </summary>
        private void UpdateTitleGlow()
        {
            glowTime += Time.deltaTime;
            
            var titleText = titleContainer?.GetComponentInChildren<Text>();
            if (titleText != null)
            {
                float intensity = 0.8f + 0.2f * Mathf.Sin(glowTime * 2f);
                titleText.color = new Color(0f, intensity, intensity, 1f);
            }
        }

        /// <summary>
        /// キーボードナビゲーション処理
        /// </summary>
        private void HandleKeyboardNavigation()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                NavigateButtons(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                NavigateButtons(1);
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                var currentButton = EventSystem.current.currentSelectedGameObject?.GetComponent<Button>();
                currentButton?.onClick.Invoke();
            }
        }

        /// <summary>
        /// ボタンナビゲーション
        /// </summary>
        private void NavigateButtons(int direction)
        {
            if (menuButtons == null || menuButtons.Length == 0) return;

            var currentSelected = EventSystem.current.currentSelectedGameObject;
            int currentIndex = -1;

            // 現在選択されているボタンのインデックスを取得
            for (int i = 0; i < menuButtons.Length; i++)
            {
                if (menuButtons[i].gameObject == currentSelected)
                {
                    currentIndex = i;
                    break;
                }
            }

            // 次のボタンを選択
            int nextIndex = (currentIndex + direction + menuButtons.Length) % menuButtons.Length;
            menuButtons[nextIndex].Select();
        }

        #endregion

        #region Button Events

        /// <summary>
        /// ゲーム開始ボタン
        /// </summary>
        private void OnGameStart()
        {
            Debug.Log("ゲーム開始");
            
            // SceneTransitionManagerを使用してシーン遷移
            if (sceneTransition != null)
            {
                GameEventManager.TriggerUIScreenShow("StageSelection");
                sceneTransition.TransitionToScene("StageSelectionScene");
            }
            else if (!string.IsNullOrEmpty(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                // ステージ選択UIを直接表示
                ShowStageSelection();
            }
        }

        /// <summary>
        /// 設定ボタン
        /// </summary>
        private void OnSettings()
        {
            Debug.Log("設定画面を開く");
            
            if (!string.IsNullOrEmpty(settingsSceneName))
            {
                SceneManager.LoadScene(settingsSceneName);
            }
            else
            {
                ShowSettings();
            }
        }

        /// <summary>
        /// 終了ボタン
        /// </summary>
        private void OnExit()
        {
            Debug.Log("ゲーム終了");
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// ステージ選択画面表示
        /// </summary>
        private void ShowStageSelection()
        {
            // ステージ選択UIコンポーネントがあれば表示
            var stageSelectionUI = FindObjectOfType<StageSelectionUI>();
            if (stageSelectionUI != null)
            {
                gameObject.SetActive(false);
                stageSelectionUI.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("ステージ選択UIが見つかりません");
            }
        }

        /// <summary>
        /// 設定画面表示
        /// </summary>
        private void ShowSettings()
        {
            Debug.Log("設定UIの表示（未実装）");
            // TODO: 設定UI実装
        }

        #endregion
    }

    /// <summary>
    /// 簡易パーティクルアニメーター
    /// </summary>
    public class SimpleParticleAnimator : MonoBehaviour
    {
        private float duration;
        private float currentTime;
        private Vector2 startPos;
        private Vector2 endPos;
        private RectTransform rectTransform;
        private Image image;

        public void Initialize(float animDuration)
        {
            duration = animDuration;
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            
            startPos = rectTransform.anchoredPosition;
            endPos = startPos + new Vector2(0, UnityEngine.Random.Range(50f, 200f));
            
            currentTime = 0f;
        }

        private void Update()
        {
            if (rectTransform == null) return;

            currentTime += Time.deltaTime;
            float progress = currentTime / duration;

            if (progress >= 1f)
            {
                // リセット
                currentTime = 0f;
                startPos = new Vector2(
                    UnityEngine.Random.Range(-960, 960),
                    -540
                );
                endPos = startPos + new Vector2(0, UnityEngine.Random.Range(50f, 200f));
                progress = 0f;
            }

            // 位置更新
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, progress);
            
            // 透明度更新（フェードイン・フェードアウト）
            float alpha = Mathf.Sin(progress * Mathf.PI) * 0.7f;
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}