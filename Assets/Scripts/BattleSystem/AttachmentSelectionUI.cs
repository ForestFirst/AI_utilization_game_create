using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem
{
    // アタッチメント選択UI
    public class AttachmentSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject selectionPanel;
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private GameObject optionButtonPrefab;
        [SerializeField] private Button skipButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI instructionText;

        [Header("Settings")]
        [SerializeField] private Color commonColor = Color.white;
        [SerializeField] private Color rareColor = Color.blue;
        [SerializeField] private Color epicColor = new Color(0.6f, 0.2f, 0.8f); // 紫
        [SerializeField] private Color legendaryColor = Color.yellow;

        private AttachmentSystem attachmentSystem;
        private BattleManager battleManager;
        private AttachmentData[] currentOptions;
        
        // イベント定義
        public event Action<AttachmentData> OnAttachmentSelected;
        public event Action OnSelectionSkipped;
        public event Action OnSelectionClosed;

        private void Awake()
        {
            attachmentSystem = FindObjectOfType<AttachmentSystem>();
            battleManager = FindObjectOfType<BattleManager>();
            
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }
            
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipSelection);
            }
        }

        private void Start()
        {
            // UI初期化
            if (titleText != null)
            {
                titleText.text = "アタッチメント選択";
            }
            
            if (instructionText != null)
            {
                instructionText.text = "装備するアタッチメントを選択してください";
            }

            // テキストのサイズ調整
            if (titleText != null)
            {
                titleText.fontSize = 24;
            }

            // テキストのrectTransformのサイズ調整
            if (titleText != null)
            {
                titleText.rectTransform.sizeDelta = new Vector2(100, 100);
            }
        }

        // アタッチメント選択画面を表示
        public void ShowSelectionScreen()
        {
            Debug.Log("=== ShowSelectionScreen START ===");
            Debug.Log($"attachmentSystem: {(attachmentSystem != null ? "OK" : "NULL")}");
            Debug.Log($"selectionPanel: {(selectionPanel != null ? "OK" : "NULL")}");
            Debug.Log($"optionsContainer: {(optionsContainer != null ? "OK" : "NULL")}");
            Debug.Log($"optionButtonPrefab: {(optionButtonPrefab != null ? "OK" : "NULL")}");
            
            if (attachmentSystem == null)
            {
                Debug.LogError("AttachmentSystem not found!");
                return;
            }

            // アタッチメント選択肢を生成
            currentOptions = attachmentSystem.GenerateAttachmentOptions();
            
            if (currentOptions == null || currentOptions.Length == 0)
            {
                Debug.LogWarning("No attachment options available");
                return;
            }

            // UI表示
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
                Debug.Log("✅ selectionPanel activated");
            }
            else
            {
                Debug.LogError("❌ selectionPanel is null!");
            }

            // 選択肢UIを作成
            CreateOptionButtons();
            
            Debug.Log($"✅ アタッチメント選択画面表示: {currentOptions.Length}個の選択肢");
            Debug.Log("=== ShowSelectionScreen END ===");
        }

        // 選択肢ボタンの作成
        private void CreateOptionButtons()
        {
            Debug.Log("=== CreateOptionButtons START ===");
            
            // 既存のボタンをクリア
            ClearOptionButtons();

            if (optionsContainer == null || optionButtonPrefab == null)
            {
                Debug.LogError($"❌ Options container or button prefab not assigned! Container: {(optionsContainer != null ? "OK" : "NULL")}, Prefab: {(optionButtonPrefab != null ? "OK" : "NULL")}");
                
                // optionsContainerがnullの場合、動的に作成を試みる
                if (optionsContainer == null && selectionPanel != null)
                {
                    Debug.Log("🔧 optionsContainerがnullのため、動的に作成を試みます...");
                    CreateOptionsContainerDynamically();
                }
                
                if (optionsContainer == null)
                {
                    Debug.LogError("❌ optionsContainer作成に失敗しました");
                    return;
                }
            }

            Debug.Log($"✅ optionsContainer準備完了: {optionsContainer.name}");

            // 各選択肢のボタンを作成
            for (int i = 0; i < currentOptions.Length; i++)
            {
                AttachmentData option = currentOptions[i];
                Debug.Log($"🔲 ボタン作成中 [{i}]: {option.attachmentName}");
                CreateOptionButton(option, i);
            }
            
            Debug.Log("=== CreateOptionButtons END ===");
        }
        
        /// <summary>
        /// optionsContainerを動的に作成
        /// </summary>
        private void CreateOptionsContainerDynamically()
        {
            if (selectionPanel == null)
            {
                Debug.LogError("❌ selectionPanelがnullのため、optionsContainerを作成できません");
                return;
            }
            
            Debug.Log("🔧 optionsContainerを動的作成中...");
            
            GameObject containerObj = new GameObject("OptionsContainer_Dynamic");
            containerObj.transform.SetParent(selectionPanel.transform, false);
            
            // GridLayoutGroupを追加
            GridLayoutGroup gridLayout = containerObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(300, 100);
            gridLayout.spacing = new Vector2(20, 20);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // RectTransformの設定
            RectTransform containerRect = containerObj.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.3f);
            containerRect.anchorMax = new Vector2(0.9f, 0.7f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            optionsContainer = containerObj.transform;
            
            Debug.Log($"✅ optionsContainer動的作成完了: {optionsContainer.name}");
        }

        // 個別選択肢ボタンの作成
        private void CreateOptionButton(AttachmentData attachment, int index)
        {
            GameObject buttonObj;
            
            if (optionButtonPrefab != null)
            {
                buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
                Debug.Log($"✅ Prefabからボタン作成: {attachment.attachmentName}");
            }
            else
            {
                // プレハブがない場合は動的に作成
                Debug.Log($"🔧 プレハブがないため動的にボタン作成: {attachment.attachmentName}");
                buttonObj = CreateButtonDynamically(attachment);
            }
            
            // ボタンコンポーネント取得
            Button button = buttonObj.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObj.AddComponent<Button>();
            }

            // テキストコンポーネント設定
            UpdateButtonTexts(buttonObj, attachment);

            // 背景色設定
            Image backgroundImage = buttonObj.GetComponent<Image>();
            if (backgroundImage != null)
            {
                Color rarityColor = GetRarityColor(attachment.rarity);
                rarityColor.a = 0.3f; // 透明度調整
                backgroundImage.color = rarityColor;
            }

            // ボタンクリックイベント設定
            button.onClick.AddListener(() => SelectAttachment(attachment));
            
            Debug.Log($"✅ 選択肢ボタン作成完了: {attachment.attachmentName} ({attachment.rarity})");
        }
        
        /// <summary>
        /// ボタンを動的に作成
        /// </summary>
        private GameObject CreateButtonDynamically(AttachmentData attachment)
        {
            GameObject buttonObj = new GameObject($"AttachmentButton_{attachment.attachmentName}");
            buttonObj.transform.SetParent(optionsContainer, false);
            
            // RectTransformの設定
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(300, 100);
            
            // Imageコンポーネント（背景）
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Buttonコンポーネント
            Button button = buttonObj.AddComponent<Button>();
            
            // テキスト要素を作成
            CreateButtonTextElements(buttonObj, attachment);
            
            Debug.Log($"✅ 動的ボタン作成完了: {buttonObj.name}");
            return buttonObj;
        }
        
        /// <summary>
        /// ボタンのテキスト要素を動的作成
        /// </summary>
        private void CreateButtonTextElements(GameObject buttonObj, AttachmentData attachment)
        {
            // MainText
            GameObject mainTextObj = new GameObject("MainText");
            mainTextObj.transform.SetParent(buttonObj.transform, false);
            
            TextMeshProUGUI mainText = mainTextObj.AddComponent<TextMeshProUGUI>();
            mainText.text = attachment.attachmentName;
            mainText.fontSize = 18;
            mainText.alignment = TextAlignmentOptions.Center;
            mainText.color = GetRarityColor(attachment.rarity);
            
            RectTransform mainTextRect = mainTextObj.GetComponent<RectTransform>();
            mainTextRect.anchorMin = new Vector2(0, 0.6f);
            mainTextRect.anchorMax = new Vector2(1, 1);
            mainTextRect.offsetMin = Vector2.zero;
            mainTextRect.offsetMax = Vector2.zero;
            
            // SubText
            GameObject subTextObj = new GameObject("SubText");
            subTextObj.transform.SetParent(buttonObj.transform, false);
            
            TextMeshProUGUI subText = subTextObj.AddComponent<TextMeshProUGUI>();
            subText.text = attachment.description;
            subText.fontSize = 12;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = Color.white;
            
            RectTransform subTextRect = subTextObj.GetComponent<RectTransform>();
            subTextRect.anchorMin = new Vector2(0, 0.2f);
            subTextRect.anchorMax = new Vector2(1, 0.6f);
            subTextRect.offsetMin = Vector2.zero;
            subTextRect.offsetMax = Vector2.zero;
            
            Debug.Log($"✅ テキスト要素作成完了: {attachment.attachmentName}");
        }

        // ボタンのテキスト要素を更新
        private void UpdateButtonTexts(GameObject buttonObj, AttachmentData attachment)
        {
            // 子オブジェクトからテキストコンポーネントを名前で検索
            Transform mainTextTransform = buttonObj.transform.Find("MainText");
            Transform subTextTransform = buttonObj.transform.Find("SubText");
            Transform comboTextTransform = buttonObj.transform.Find("ComboText");
            Transform rarityTextTransform = buttonObj.transform.Find("RarityText");

            // メインテキスト（アタッチメント名）
            if (mainTextTransform != null)
            {
                TextMeshProUGUI mainText = mainTextTransform.GetComponent<TextMeshProUGUI>();
                if (mainText != null)
                {
                    mainText.text = attachment.attachmentName;
                    mainText.color = GetRarityColor(attachment.rarity);
                }
            }

            // サブテキスト（説明）
            if (subTextTransform != null)
            {
                TextMeshProUGUI subText = subTextTransform.GetComponent<TextMeshProUGUI>();
                if (subText != null)
                {
                    subText.text = attachment.description;
                }
            }

            // コンボテキスト（対応コンボ名）
            if (comboTextTransform != null)
            {
                TextMeshProUGUI comboText = comboTextTransform.GetComponent<TextMeshProUGUI>();
                if (comboText != null)
                {
                    string comboName = !string.IsNullOrEmpty(attachment.associatedComboName) 
                        ? attachment.associatedComboName 
                        : "未設定";
                    comboText.text = $"🎯 {comboName}";
                    comboText.color = !string.IsNullOrEmpty(attachment.associatedComboName) ? Color.cyan : Color.gray;
                }
            }

            // レアリティテキスト
            if (rarityTextTransform != null)
            {
                TextMeshProUGUI rarityText = rarityTextTransform.GetComponent<TextMeshProUGUI>();
                if (rarityText != null)
                {
                    rarityText.text = $"[{attachment.rarity}]";
                    rarityText.color = GetRarityColor(attachment.rarity);
                }
            }
        }

        // レアリティ色取得
        private Color GetRarityColor(AttachmentRarity rarity)
        {
            switch (rarity)
            {
                case AttachmentRarity.Common:
                    return commonColor;
                case AttachmentRarity.Rare:
                    return rareColor;
                case AttachmentRarity.Epic:
                    return epicColor;
                case AttachmentRarity.Legendary:
                    return legendaryColor;
                default:
                    return Color.white;
            }
        }

        // アタッチメント選択処理
        private void SelectAttachment(AttachmentData selectedAttachment)
        {
            if (selectedAttachment == null)
            {
                Debug.LogError("Selected attachment is null!");
                return;
            }

            Debug.Log($"アタッチメント選択: {selectedAttachment.attachmentName}");

            // アタッチメントシステムに装着指示
            if (attachmentSystem != null)
            {
                bool success = attachmentSystem.AttachAttachment(selectedAttachment);
                if (success)
                {
                    Debug.Log($"アタッチメント装着成功: {selectedAttachment.attachmentName}");
                }
                else
                {
                    Debug.LogWarning($"アタッチメント装着失敗: {selectedAttachment.attachmentName}");
                }
            }

            // イベント発火
            OnAttachmentSelected?.Invoke(selectedAttachment);

            // 選択画面を閉じる
            CloseSelectionScreen();
            
            // アタッチメント選択後にPlayModeを終了
            ExitPlayModeAfterDelay();
        }
        
        /// <summary>
        /// アタッチメント選択後に少し遅延してPlayModeを終了
        /// </summary>
        private void ExitPlayModeAfterDelay()
        {
            Debug.Log("🏁 アタッチメント選択完了! 2秒後にPlayModeを終了します...");
            StartCoroutine(ExitPlayModeCoroutine());
        }
        
        /// <summary>
        /// PlayMode終了のコルーチン
        /// </summary>
        private System.Collections.IEnumerator ExitPlayModeCoroutine()
        {
            yield return new WaitForSeconds(2f);
            
            Debug.Log("🏁 PlayMode終了を実行中...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            Debug.Log("✅ PlayMode終了完了");
            #else
            Debug.Log("⚠️ エディター以外の環境では自動終了できません");
            #endif
        }

        // 選択をスキップ
        private void SkipSelection()
        {
            Debug.Log("アタッチメント選択をスキップ");
            
            // イベント発火
            OnSelectionSkipped?.Invoke();
            
            // 選択画面を閉じる
            CloseSelectionScreen();
        }

        // 選択画面を閉じる
        private void CloseSelectionScreen()
        {
            // UI非表示
            if (selectionPanel != null)
            {
                selectionPanel.SetActive(false);
            }

            // 選択肢ボタンをクリア
            ClearOptionButtons();

            // イベント発火
            OnSelectionClosed?.Invoke();
            
            Debug.Log("アタッチメント選択画面を閉じました");
        }

        // 選択肢ボタンのクリア
        private void ClearOptionButtons()
        {
            if (optionsContainer == null) return;

            // 子オブジェクトを全て削除
            foreach (Transform child in optionsContainer)
            {
                if (child.gameObject != optionButtonPrefab) // プレハブ自体は削除しない
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // デバッグ用：強制的に選択画面表示
        [ContextMenu("Show Selection Screen (Debug)")]
        public void ShowSelectionScreenDebug()
        {
            ShowSelectionScreen();
        }

        // デバッグ用：ランダム選択
        [ContextMenu("Random Select (Debug)")]
        public void RandomSelectDebug()
        {
            if (currentOptions != null && currentOptions.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, currentOptions.Length);
                SelectAttachment(currentOptions[randomIndex]);
            }
        }

        private void OnDestroy()
        {
            // イベントのクリーンアップ
            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
            }
        }
    }
}