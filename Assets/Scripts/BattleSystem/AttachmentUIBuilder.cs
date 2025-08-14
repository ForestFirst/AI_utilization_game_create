using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem
{
    /// <summary>
    /// アタッチメント選択UIの動的構築を担当するクラス
    /// </summary>
    public static class AttachmentUIBuilder
    {
        /// <summary>
        /// アタッチメント選択UIを動的に作成します
        /// </summary>
        /// <param name="canvas">UIを配置するキャンバス</param>
        /// <returns>作成されたAttachmentSelectionUIコンポーネント</returns>
        public static AttachmentSelectionUI CreateAttachmentSelectionUI(Canvas canvas)
        {
            if (canvas == null)
            {
                Debug.LogError("Canvas not found! Cannot create AttachmentSelectionUI");
                return null;
            }

            // AttachmentSelectionUIオブジェクトを作成
            GameObject selectionUIGameObject = new GameObject("AttachmentSelectionUI");
            selectionUIGameObject.transform.SetParent(canvas.transform, false);
            
            AttachmentSelectionUI selectionUI = selectionUIGameObject.AddComponent<AttachmentSelectionUI>();

            // 基本的なUI構造を作成
            BuildUIStructure(selectionUIGameObject, selectionUI);

            Debug.Log("AttachmentSelectionUI created successfully!");
            return selectionUI;
        }

        /// <summary>
        /// AttachmentSystemが存在しない場合に作成します
        /// </summary>
        /// <returns>作成または既存のAttachmentSystem</returns>
        public static AttachmentSystem EnsureAttachmentSystem()
        {
            AttachmentSystem attachmentSystem = Object.FindObjectOfType<AttachmentSystem>();
            
            if (attachmentSystem == null)
            {
                Debug.Log("AttachmentSystem not found, creating it...");
                BattleManager battleManager = Object.FindObjectOfType<BattleManager>();
                
                if (battleManager != null)
                {
                    attachmentSystem = battleManager.gameObject.AddComponent<AttachmentSystem>();
                    Debug.Log("AttachmentSystem created on BattleManager");
                }
                else
                {
                    Debug.LogError("BattleManager not found! Cannot create AttachmentSystem");
                    return null;
                }
            }

            return attachmentSystem;
        }

        /// <summary>
        /// 基本的なUI構造を構築します
        /// </summary>
        /// <param name="parentGameObject">親GameObject</param>
        /// <param name="selectionUI">AttachmentSelectionUIコンポーネント</param>
        private static void BuildUIStructure(GameObject parentGameObject, AttachmentSelectionUI selectionUI)
        {
            // Selection Panel作成
            GameObject selectionPanel = CreateSelectionPanel(parentGameObject);

            // UI要素を作成
            Transform optionsContainer = CreateOptionsContainer(selectionPanel);
            GameObject optionButtonPrefab = CreateOptionButtonPrefab(selectionPanel);
            Button skipButton = CreateSkipButton(selectionPanel);
            TextMeshProUGUI titleText = CreateTitleText(selectionPanel);
            TextMeshProUGUI instructionText = CreateInstructionText(selectionPanel);

            // AttachmentSelectionUIのフィールドを設定
            SetUIFields(selectionUI, selectionPanel, optionsContainer, optionButtonPrefab, skipButton, titleText, instructionText);
            
            Debug.Log("Basic AttachmentSelectionUI structure created");
        }

        /// <summary>
        /// 選択パネルを作成します
        /// </summary>
        private static GameObject CreateSelectionPanel(GameObject parent)
        {
            GameObject selectionPanel = new GameObject("SelectionPanel");
            selectionPanel.transform.SetParent(parent.transform, false);
            
            Image panelImage = selectionPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f); // 半透明黒背景
            
            RectTransform panelRect = selectionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            return selectionPanel;
        }

        /// <summary>
        /// オプションコンテナを作成します
        /// </summary>
        private static Transform CreateOptionsContainer(GameObject parent)
        {
            GameObject optionsContainer = new GameObject("OptionsContainer");
            optionsContainer.transform.SetParent(parent.transform, false);
            
            GridLayoutGroup gridLayout = optionsContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(350, 120); // ボタンサイズを拡大
            gridLayout.spacing = new Vector2(20, 20);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2; // 2列で表示
            
            RectTransform containerRect = optionsContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.3f);
            containerRect.anchorMax = new Vector2(0.9f, 0.7f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            return optionsContainer.transform;
        }

        /// <summary>
        /// オプションボタンのプレハブを作成します
        /// </summary>
        private static GameObject CreateOptionButtonPrefab(GameObject parent)
        {
            GameObject buttonPrefab = new GameObject("OptionButtonPrefab");
            buttonPrefab.transform.SetParent(parent.transform, false);
            buttonPrefab.SetActive(false); // プレハブなので非アクティブ
            
            Button button = buttonPrefab.AddComponent<Button>();
            Image buttonImage = buttonPrefab.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // テキスト要素を作成
            CreateButtonTextElements(buttonPrefab);
            
            return buttonPrefab;
        }

        /// <summary>
        /// ボタン用のテキスト要素を作成します
        /// </summary>
        private static void CreateButtonTextElements(GameObject button)
        {
            // メインテキスト（アタッチメント名）- より大きく表示
            TextMeshProUGUI mainText = CreateTextElement(button, "MainText", "アタッチメント名", Color.white, 18, 
                new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.95f));
            mainText.fontStyle = TMPro.FontStyles.Bold;
            mainText.alignment = TextAlignmentOptions.Center;
            mainText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
            
            // サブテキスト（説明）- 適度なサイズで表示
            TextMeshProUGUI subText = CreateTextElement(button, "SubText", "説明", Color.gray, 14, 
                new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.65f));
            subText.alignment = TextAlignmentOptions.Center;
            subText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
            subText.enableWordWrapping = true;
            
            // レアリティテキスト - 下部に表示
            TextMeshProUGUI rarityText = CreateTextElement(button, "RarityText", "[Common]", Color.white, 12, 
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.35f));
            rarityText.fontStyle = TMPro.FontStyles.Italic;
            rarityText.alignment = TextAlignmentOptions.Center;
            rarityText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        }

        /// <summary>
        /// テキスト要素を作成します
        /// </summary>
        private static TextMeshProUGUI CreateTextElement(GameObject parent, string name, string text, 
            Color color, float fontSize, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent.transform, false);
            
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.color = color;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            // テキスト表示の問題を修正する設定
            textComponent.enableWordWrapping = true;
            textComponent.overflowMode = TMPro.TextOverflowModes.Truncate;
            textComponent.enableAutoSizing = false;
            textComponent.autoSizeTextContainer = false;
            
            // より詳細なテキスト設定
            textComponent.textWrappingMode = TMPro.TextWrappingModes.Normal;
            textComponent.parseCtrlCharacters = false;
            textComponent.isOverlay = false;
            textComponent.richText = true;
            
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // テキストコンテナのサイズを明示的に設定
            // タイトルやインストラクション用の場合は画面幅を使用
            float containerWidth = name.Contains("Title") || name.Contains("Instruction") ? 1920f : 350f;
            float containerHeight = name.Contains("Title") || name.Contains("Instruction") ? 1080f : 120f;
            
            textComponent.rectTransform.sizeDelta = new Vector2(
                (anchorMax.x - anchorMin.x) * containerWidth,
                (anchorMax.y - anchorMin.y) * containerHeight
            );

            return textComponent;
        }

        /// <summary>
        /// スキップボタンを作成します
        /// </summary>
        private static Button CreateSkipButton(GameObject parent)
        {
            GameObject skipButton = new GameObject("SkipButton");
            skipButton.transform.SetParent(parent.transform, false);
            
            Button skipButtonComponent = skipButton.AddComponent<Button>();
            Image skipButtonImage = skipButton.AddComponent<Image>();
            skipButtonImage.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            
            CreateTextElement(skipButton, "Text", "スキップ", Color.white, 18,
                Vector2.zero, Vector2.one);
            
            RectTransform skipButtonRect = skipButton.GetComponent<RectTransform>();
            skipButtonRect.anchorMin = new Vector2(0.4f, 0.1f);
            skipButtonRect.anchorMax = new Vector2(0.6f, 0.2f);
            skipButtonRect.offsetMin = Vector2.zero;
            skipButtonRect.offsetMax = Vector2.zero;

            return skipButtonComponent;
        }

        /// <summary>
        /// タイトルテキストを作成します
        /// </summary>
        private static TextMeshProUGUI CreateTitleText(GameObject parent)
        {
            TextMeshProUGUI titleText = CreateTextElement(parent, "TitleText", "アタッチメント選択", Color.white, 24,
                new Vector2(0f, 0.85f), new Vector2(1f, 0.95f));
            
            // タイトルテキスト専用設定
            titleText.enableWordWrapping = false;
            titleText.overflowMode = TMPro.TextOverflowModes.Overflow;
            titleText.fontStyle = TMPro.FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
            
            return titleText;
        }

        /// <summary>
        /// 説明テキストを作成します
        /// </summary>
        private static TextMeshProUGUI CreateInstructionText(GameObject parent)
        {
            TextMeshProUGUI instructionText = CreateTextElement(parent, "InstructionText", "装備するアタッチメントを選択してください", 
                Color.white, 16, new Vector2(0f, 0.78f), new Vector2(1f, 0.85f));
            
            // 説明テキスト専用設定
            instructionText.enableWordWrapping = false;
            instructionText.overflowMode = TMPro.TextOverflowModes.Overflow;
            instructionText.alignment = TextAlignmentOptions.Center;
            instructionText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
            
            return instructionText;
        }

        /// <summary>
        /// AttachmentSelectionUIのフィールドをリフレクションで設定します
        /// </summary>
        private static void SetUIFields(AttachmentSelectionUI selectionUI, GameObject selectionPanel, 
            Transform optionsContainer, GameObject optionButtonPrefab, Button skipButton, 
            TextMeshProUGUI titleText, TextMeshProUGUI instructionText)
        {
            BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            
            // プライベートフィールドにリフレクションでアクセス
            SetFieldValue(selectionUI, "selectionPanel", selectionPanel, flags);
            SetFieldValue(selectionUI, "optionsContainer", optionsContainer, flags);
            SetFieldValue(selectionUI, "optionButtonPrefab", optionButtonPrefab, flags);
            SetFieldValue(selectionUI, "skipButton", skipButton, flags);
            SetFieldValue(selectionUI, "titleText", titleText, flags);
            SetFieldValue(selectionUI, "instructionText", instructionText, flags);

            Debug.Log("AttachmentSelectionUI fields set via reflection");
        }

        /// <summary>
        /// リフレクションでフィールド値を設定します
        /// </summary>
        private static void SetFieldValue(object target, string fieldName, object value, BindingFlags flags)
        {
            FieldInfo field = target.GetType().GetField(fieldName, flags);
            field?.SetValue(target, value);
        }
    }
}