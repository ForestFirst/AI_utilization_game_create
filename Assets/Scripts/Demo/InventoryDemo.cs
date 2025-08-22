using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BattleSystem;

namespace InventorySystem.Demo
{
    public class InventoryDemo : MonoBehaviour
    {
        [Header("Auto-Setup")]
        [SerializeField] private bool autoSetup = true;
        
        [Header("Demo UI Components (Auto-Generated)")]
        [SerializeField] private BattleSystem.InventoryUI inventoryUI;
        [SerializeField] private Canvas mainCanvas;
        
        [Header("Test Data")]
        [SerializeField] private WeaponDatabase weaponDatabase;
        [SerializeField] private AttachmentDatabase attachmentDatabase;
        
        private void Start()
        {
            if (autoSetup)
            {
                SetupDemo();
            }
        }
        
        private void SetupDemo()
        {
            Debug.Log("=== インベントリデモセットアップ開始 ===");
            
            // 既存のCanvasやEventSystemをチェック
            CheckAndCreateEventSystem();
            CreateDemoUI();
            LoadTestData();
            
            Debug.Log("✅ インベントリデモセットアップ完了");
        }
        
        private void CheckAndCreateEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("✅ EventSystem作成完了");
            }
        }
        
        private void CreateDemoUI()
        {
            // メインキャンバス作成
            CreateMainCanvas();
            
            // インベントリUI作成
            CreateInventoryUI();
            
            // デモ用コントロールパネル作成
            CreateControlPanel();
        }
        
        private void CreateMainCanvas()
        {
            GameObject canvasGO = new GameObject("InventoryDemo_Canvas");
            canvasGO.transform.SetParent(transform);
            
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100; // 最前面に表示
            
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            Debug.Log("✅ メインキャンバス作成完了");
        }
        
        private void CreateInventoryUI()
        {
            GameObject inventoryGO = new GameObject("InventoryUI");
            inventoryGO.transform.SetParent(mainCanvas.transform);
            
            // RectTransformの設定
            RectTransform inventoryRect = inventoryGO.AddComponent<RectTransform>();
            inventoryRect.anchorMin = Vector2.zero;
            inventoryRect.anchorMax = Vector2.one;
            inventoryRect.offsetMin = Vector2.zero;
            inventoryRect.offsetMax = Vector2.zero;
            
            // InventoryUIコンポーネントを追加
            inventoryUI = inventoryGO.AddComponent<BattleSystem.InventoryUI>();
            
            Debug.Log("✅ インベントリUI作成完了");
        }
        
        private void CreateControlPanel()
        {
            GameObject controlPanelGO = new GameObject("ControlPanel");
            controlPanelGO.transform.SetParent(mainCanvas.transform);
            
            // RectTransformの設定（右上に配置）
            RectTransform controlRect = controlPanelGO.AddComponent<RectTransform>();
            controlRect.anchorMin = new Vector2(0.8f, 0.8f);
            controlRect.anchorMax = new Vector2(1.0f, 1.0f);
            controlRect.offsetMin = Vector2.zero;
            controlRect.offsetMax = Vector2.zero;
            
            // 背景追加
            Image bgImage = controlPanelGO.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            
            // 垂直レイアウト追加
            VerticalLayoutGroup layout = controlPanelGO.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;
            
            // タイトル追加
            CreateControlLabel(controlPanelGO.transform, "デモコントロール");
            
            // テストボタン追加
            CreateControlButton(controlPanelGO.transform, "武器タブ", () => TestWeaponTab());
            CreateControlButton(controlPanelGO.transform, "アタッチメントタブ", () => TestAttachmentTab());
            CreateControlButton(controlPanelGO.transform, "スキルタブ", () => TestSkillTab());
            CreateControlButton(controlPanelGO.transform, "リソース更新", () => UpdateResources());
            CreateControlButton(controlPanelGO.transform, "リセット", () => ResetInventory());
            
            Debug.Log("✅ コントロールパネル作成完了");
        }
        
        private void CreateControlLabel(Transform parent, string text)
        {
            GameObject labelGO = new GameObject("Label_" + text);
            labelGO.transform.SetParent(parent);
            
            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            
            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 30);
        }
        
        private void CreateControlButton(Transform parent, string text, System.Action onClick)
        {
            GameObject buttonGO = new GameObject("Button_" + text);
            buttonGO.transform.SetParent(parent);
            
            // Button追加
            Button button = buttonGO.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // 背景Image追加
            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1.0f, 0.8f);
            
            // テキスト追加
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            
            TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            // RectTransform設定
            RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(140, 25);
            
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
        
        private void LoadTestData()
        {
            // WeaponDatabase検索
            weaponDatabase = Resources.Load<WeaponDatabase>("WeaponDatabase");
            if (weaponDatabase == null)
            {
                Debug.LogWarning("WeaponDatabase が見つかりません。Resources/WeaponDatabase.asset を確認してください。");
            }
            
            // AttachmentDatabase検索
            attachmentDatabase = Resources.Load<AttachmentDatabase>("AttachmentDatabase");
            if (attachmentDatabase == null)
            {
                Debug.LogWarning("AttachmentDatabase が見つかりません。Resources/AttachmentDatabase.asset を確認してください。");
            }
            
            Debug.Log("✅ テストデータロード完了");
        }
        
        // テスト機能
        private void TestWeaponTab()
        {
            if (inventoryUI != null)
            {
                Debug.Log("武器タブテスト実行");
                inventoryUI.SwitchToTab(InventoryTab.Weapons);
                
                // テスト武器追加
                AddTestWeapons();
            }
        }
        
        private void TestAttachmentTab()
        {
            if (inventoryUI != null)
            {
                Debug.Log("アタッチメントタブテスト実行");
                inventoryUI.SwitchToTab(InventoryTab.Attachments);
                
                // テストアタッチメント追加
                AddTestAttachments();
            }
        }
        
        private void TestSkillTab()
        {
            if (inventoryUI != null)
            {
                Debug.Log("スキルタブテスト実行");
                inventoryUI.SwitchToTab(InventoryTab.Skills);
            }
        }
        
        private void ResetInventory()
        {
            if (inventoryUI != null)
            {
                Debug.Log("インベントリリセット実行");
                inventoryUI.ResetInventory();
            }
        }
        
        private void UpdateResources()
        {
            if (inventoryUI != null)
            {
                // ランダムにリソースを更新
                int newGold = UnityEngine.Random.Range(100, 2000);
                int newSkillPoints = UnityEngine.Random.Range(1, 50);
                
                inventoryUI.UpdateResources(newGold, newSkillPoints);
                Debug.Log($"リソース更新: {newGold}G, {newSkillPoints}SP");
            }
        }
        
        // テストデータ追加機能
        private void AddTestWeapons()
        {
            var testWeapons = new[]
            {
                new WeaponData { weaponName = "テスト剣", basePower = 50, criticalRate = 15, weaponType = WeaponType.Sword },
                new WeaponData { weaponName = "テスト斧", basePower = 70, criticalRate = 10, weaponType = WeaponType.Axe },
                new WeaponData { weaponName = "テスト槍", basePower = 60, criticalRate = 20, weaponType = WeaponType.Spear },
                new WeaponData { weaponName = "テスト弓", basePower = 45, criticalRate = 25, weaponType = WeaponType.Bow }
            };
            
            foreach (var weapon in testWeapons)
            {
                inventoryUI.AddTestWeapon(weapon);
            }
        }
        
        private void AddTestAttachments()
        {
            var testAttachments = new[]
            {
                new AttachmentData { attachmentId = 1, attachmentName = "炎のアタッチメント", rarity = AttachmentRarity.Common, description = "炎ダメージ+20%" },
                new AttachmentData { attachmentId = 2, attachmentName = "氷のアタッチメント", rarity = AttachmentRarity.Rare, description = "氷ダメージ+30%" },
                new AttachmentData { attachmentId = 3, attachmentName = "雷のアタッチメント", rarity = AttachmentRarity.Epic, description = "雷ダメージ+40%" },
                new AttachmentData { attachmentId = 4, attachmentName = "伝説のアタッチメント", rarity = AttachmentRarity.Legendary, description = "全ダメージ+50%" }
            };
            
            foreach (var attachment in testAttachments)
            {
                inventoryUI.AddTestAttachment(attachment);
                Debug.Log($"テストアタッチメント追加: {attachment.attachmentName}");
            }
        }
        
        // デバッグ情報表示
        private void OnGUI()
        {
            if (!autoSetup) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("インベントリデモ状態");
            GUILayout.Label($"InventoryUI: {(inventoryUI != null ? "✅" : "❌")}");
            GUILayout.Label($"MainCanvas: {(mainCanvas != null ? "✅" : "❌")}");
            GUILayout.Label($"WeaponDB: {(weaponDatabase != null ? "✅" : "❌")}");
            GUILayout.Label($"AttachmentDB: {(attachmentDatabase != null ? "✅" : "❌")}");
            
            if (GUILayout.Button("手動セットアップ") && !autoSetup)
            {
                autoSetup = true;
                SetupDemo();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}