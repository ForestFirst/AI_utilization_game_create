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
            }

            // 選択肢UIを作成
            CreateOptionButtons();
            
            Debug.Log($"アタッチメント選択画面表示: {currentOptions.Length}個の選択肢");
        }

        // 選択肢ボタンの作成
        private void CreateOptionButtons()
        {
            // 既存のボタンをクリア
            ClearOptionButtons();

            if (optionsContainer == null || optionButtonPrefab == null)
            {
                Debug.LogError("Options container or button prefab not assigned!");
                return;
            }

            // 各選択肢のボタンを作成
            for (int i = 0; i < currentOptions.Length; i++)
            {
                AttachmentData option = currentOptions[i];
                CreateOptionButton(option, i);
            }
        }

        // 個別選択肢ボタンの作成
        private void CreateOptionButton(AttachmentData attachment, int index)
        {
            GameObject buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
            
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
            
            Debug.Log($"選択肢ボタン作成: {attachment.attachmentName} ({attachment.rarity})");
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