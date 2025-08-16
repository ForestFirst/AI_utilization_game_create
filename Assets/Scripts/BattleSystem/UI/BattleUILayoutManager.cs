using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem.UI
{
    /// <summary>
    /// 戦闘UIのレイアウト管理を担当するクラス
    /// UIの作成、配置、サイズ調整などを管理
    /// </summary>
    public class BattleUILayoutManager : MonoBehaviour
    {
        [Header("Layout Settings")]
        [SerializeField] private Font defaultFont;
        [SerializeField] private TMP_FontAsset japaneseFontAsset;
        [SerializeField] private Vector2 defaultButtonSize = new Vector2(120, 30);
        [SerializeField] private Vector2 defaultTextSize = new Vector2(200, 30);

        private Canvas canvas;

        #region Initialization

        /// <summary>
        /// レイアウトマネージャーの初期化
        /// </summary>
        public void Initialize()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            LoadJapaneseFont();
        }

        /// <summary>
        /// 日本語フォントの読み込み
        /// </summary>
        private void LoadJapaneseFont()
        {
            if (japaneseFontAsset == null)
            {
                japaneseFontAsset = Resources.Load<TMP_FontAsset>("Fonts/NotoSansCJK-Regular SDF");
                if (japaneseFontAsset == null)
                {
                    Debug.LogWarning("[BattleUILayoutManager] Japanese font not found, using default font");
                }
            }
        }

        #endregion

        #region UI Creation Methods

        /// <summary>
        /// テキストUIを作成
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="text">表示テキスト</param>
        /// <param name="parent">親Transform</param>
        /// <param name="position">位置</param>
        /// <param name="size">サイズ</param>
        /// <returns>作成されたTextMeshProUGUIコンポーネント</returns>
        public TextMeshProUGUI CreateUIText(string name, string text, Transform parent = null, 
            Vector2? position = null, Vector2? size = null)
        {
            var textObj = new GameObject(name);
            var rectTransform = textObj.AddComponent<RectTransform>();
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();

            // 親設定
            SetParent(textObj, parent);

            // テキスト設定
            textComponent.text = text;
            textComponent.font = japaneseFontAsset;
            textComponent.fontSize = 14;
            textComponent.alignment = TextAlignmentOptions.Center;

            // サイズと位置設定
            SetRectTransform(rectTransform, position ?? Vector2.zero, size ?? defaultTextSize);

            return textComponent;
        }

        /// <summary>
        /// ボタンUIを作成
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="buttonText">ボタンテキスト</param>
        /// <param name="parent">親Transform</param>
        /// <param name="position">位置</param>
        /// <param name="size">サイズ</param>
        /// <returns>作成されたButtonコンポーネント</returns>
        public Button CreateUIButton(string name, string buttonText, Transform parent = null,
            Vector2? position = null, Vector2? size = null)
        {
            var buttonObj = new GameObject(name);
            var rectTransform = buttonObj.AddComponent<RectTransform>();
            var image = buttonObj.AddComponent<Image>();
            var button = buttonObj.AddComponent<Button>();

            // 親設定
            SetParent(buttonObj, parent);

            // ボタン画像設定
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // ボタンテキスト作成
            var textObj = new GameObject("Text");
            var textRect = textObj.AddComponent<RectTransform>();
            var textComponent = textObj.AddComponent<TextMeshProUGUI>();

            textObj.transform.SetParent(buttonObj.transform, false);
            textComponent.text = buttonText;
            textComponent.font = japaneseFontAsset;
            textComponent.fontSize = 12;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;

            // テキストをボタン全体に設定
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // サイズと位置設定
            SetRectTransform(rectTransform, position ?? Vector2.zero, size ?? defaultButtonSize);

            return button;
        }

        /// <summary>
        /// パネルUIを作成
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="parent">親Transform</param>
        /// <param name="position">位置</param>
        /// <param name="size">サイズ</param>
        /// <param name="backgroundColor">背景色</param>
        /// <returns>作成されたGameObject</returns>
        public GameObject CreateUIPanel(string name, Transform parent = null,
            Vector2? position = null, Vector2? size = null, Color? backgroundColor = null)
        {
            var panelObj = new GameObject(name);
            var rectTransform = panelObj.AddComponent<RectTransform>();
            var image = panelObj.AddComponent<Image>();

            // 親設定
            SetParent(panelObj, parent);

            // 背景設定
            image.color = backgroundColor ?? new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // サイズと位置設定
            SetRectTransform(rectTransform, position ?? Vector2.zero, size ?? new Vector2(300, 200));

            return panelObj;
        }

        /// <summary>
        /// スライダーUIを作成
        /// </summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="parent">親Transform</param>
        /// <param name="position">位置</param>
        /// <param name="size">サイズ</param>
        /// <returns>作成されたSliderコンポーネント</returns>
        public Slider CreateUISlider(string name, Transform parent = null,
            Vector2? position = null, Vector2? size = null)
        {
            var sliderObj = new GameObject(name);
            var rectTransform = sliderObj.AddComponent<RectTransform>();
            var slider = sliderObj.AddComponent<Slider>();

            // 親設定
            SetParent(sliderObj, parent);

            // バックグラウンド作成
            var background = CreateSliderBackground(sliderObj.transform);
            slider.targetGraphic = background;

            // フィル作成
            var fillArea = CreateSliderFillArea(sliderObj.transform);
            var fill = CreateSliderFill(fillArea.transform);
            slider.fillRect = fill.rectTransform;

            // ハンドル作成
            var handleArea = CreateSliderHandleArea(sliderObj.transform);
            var handle = CreateSliderHandle(handleArea.transform);
            slider.handleRect = handle.rectTransform;

            // サイズと位置設定
            SetRectTransform(rectTransform, position ?? Vector2.zero, size ?? new Vector2(200, 20));

            return slider;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 親オブジェクトを設定
        /// </summary>
        /// <param name="obj">設定対象のGameObject</param>
        /// <param name="parent">親Transform</param>
        private void SetParent(GameObject obj, Transform parent)
        {
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            else if (canvas != null)
            {
                obj.transform.SetParent(canvas.transform, false);
            }
        }

        /// <summary>
        /// RectTransformの設定
        /// </summary>
        /// <param name="rectTransform">設定対象のRectTransform</param>
        /// <param name="position">位置</param>
        /// <param name="size">サイズ</param>
        private void SetRectTransform(RectTransform rectTransform, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        /// <summary>
        /// スライダーのバックグラウンドを作成
        /// </summary>
        private Image CreateSliderBackground(Transform parent)
        {
            var backgroundObj = new GameObject("Background");
            var rect = backgroundObj.AddComponent<RectTransform>();
            var image = backgroundObj.AddComponent<Image>();

            backgroundObj.transform.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            return image;
        }

        /// <summary>
        /// スライダーのフィルエリアを作成
        /// </summary>
        private RectTransform CreateSliderFillArea(Transform parent)
        {
            var fillAreaObj = new GameObject("Fill Area");
            var rect = fillAreaObj.AddComponent<RectTransform>();

            fillAreaObj.transform.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        /// <summary>
        /// スライダーのフィルを作成
        /// </summary>
        private Image CreateSliderFill(Transform parent)
        {
            var fillObj = new GameObject("Fill");
            var rect = fillObj.AddComponent<RectTransform>();
            var image = fillObj.AddComponent<Image>();

            fillObj.transform.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            image.color = new Color(0.3f, 0.8f, 0.3f, 1f);

            return image;
        }

        /// <summary>
        /// スライダーのハンドルエリアを作成
        /// </summary>
        private RectTransform CreateSliderHandleArea(Transform parent)
        {
            var handleAreaObj = new GameObject("Handle Slide Area");
            var rect = handleAreaObj.AddComponent<RectTransform>();

            handleAreaObj.transform.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return rect;
        }

        /// <summary>
        /// スライダーのハンドルを作成
        /// </summary>
        private Image CreateSliderHandle(Transform parent)
        {
            var handleObj = new GameObject("Handle");
            var rect = handleObj.AddComponent<RectTransform>();
            var image = handleObj.AddComponent<Image>();

            handleObj.transform.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(20, 20);

            image.color = Color.white;

            return image;
        }

        #endregion
    }
}