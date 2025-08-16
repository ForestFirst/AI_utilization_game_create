using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace BattleSystem
{
    public class BattleUICreator : EditorWindow
    {
        [MenuItem("Tools/Battle System/Create Battle UI on Canvas")]
        public static void ShowWindow()
        {
            GetWindow<BattleUICreator>("Battle UI Creator");
        }

        private Canvas targetCanvas;

        private void OnGUI()
        {
            GUILayout.Label("Battle UI Creator", EditorStyles.boldLabel);
            
            targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);
            
            if (GUILayout.Button("Create Complete Battle UI"))
            {
                if (targetCanvas == null)
                {
                    Debug.LogError("Target Canvas is not assigned!");
                    return;
                }
                
                CreateCompleteBattleUI();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Create Basic UI Elements"))
            {
                if (targetCanvas == null)
                {
                    Debug.LogError("Target Canvas is not assigned!");
                    return;
                }
                
                CreateBasicUIElements();
            }
            
            if (GUILayout.Button("Create Battle Field UI"))
            {
                if (targetCanvas == null)
                {
                    Debug.LogError("Target Canvas is not assigned!");
                    return;
                }
                
                CreateBattleFieldUI();
            }
            
            if (GUILayout.Button("Create Combo Progress UI"))
            {
                if (targetCanvas == null)
                {
                    Debug.LogError("Target Canvas is not assigned!");
                    return;
                }
                
                CreateComboProgressUI();
            }
        }

        private void CreateCompleteBattleUI()
        {
            Undo.RecordObject(targetCanvas, "Create Complete Battle UI");
            
            CreateBasicUIElements();
            CreateBattleFieldUI();
            CreateComboProgressUI();
            CreateEnemyInfoUI();
            CreateStartScreenUI();
            
            Debug.Log("Complete Battle UI created successfully on Canvas: " + targetCanvas.name);
        }

        private void CreateBasicUIElements()
        {
            // 背景パネル
            GameObject backgroundPanel = CreateUIPanel("Background Panel", Vector2.zero, 
                new Vector2(Screen.width, Screen.height), new Color(0.1f, 0.1f, 0.1f, 0.8f));
            
            // ターン表示
            CreateUIText("Turn Display", new Vector2(-Screen.width * 0.4f, Screen.height * 0.4f), 
                new Vector2(200, 50), "ターン: 1", 20);
            
            // HP表示
            CreateUIText("HP Display", new Vector2(-Screen.width * 0.4f, Screen.height * 0.35f), 
                new Vector2(200, 40), "HP: 100/100", 18);
            
            // 予告ダメージ表示
            CreateUIText("Pending Damage Display", new Vector2(-Screen.width * 0.4f, Screen.height * 0.3f), 
                new Vector2(300, 30), "", 16);
            
            // 状態表示
            CreateUIText("State Display", new Vector2(-Screen.width * 0.4f, Screen.height * 0.25f), 
                new Vector2(200, 40), "プレイヤーターン", 16);
            
            // ボタン類
            CreateUIButton("次のターン", new Vector2(-Screen.width * 0.4f, -Screen.height * 0.4f), 
                new Vector2(120, 50));
            
            CreateUIButton("戦闘リセット", new Vector2(-Screen.width * 0.25f, -Screen.height * 0.4f), 
                new Vector2(120, 50));
            
            CreateUIButton("敵を倒す", new Vector2(-Screen.width * 0.1f, -Screen.height * 0.4f), 
                new Vector2(120, 50));
            
            CreateUIButton("🎯 コンボテスト", new Vector2(Screen.width * 0.05f, -Screen.height * 0.4f), 
                new Vector2(140, 50));
        }

        private void CreateBattleFieldUI()
        {
            // 戦場パネル
            GameObject battleFieldPanel = CreateUIPanel("戦場パネル", 
                new Vector2(0, Screen.height * 0.1f), 
                new Vector2(450, 300), 
                new Color(0.2f, 0.3f, 0.4f, 0.8f));
            
            // グリッドセル作成（3列×2行）
            float cellWidth = 140f;
            float cellHeight = 120f;
            float startX = -cellWidth;
            float startY = cellHeight * 0.5f;
            
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 2; row++)
                {
                    Vector2 cellPos = new Vector2(
                        startX + col * cellWidth,
                        startY - row * cellHeight
                    );
                    
                    GameObject gridCell = CreateUIPanel($"グリッド_{col}_{row}", 
                        cellPos, new Vector2(cellWidth - 10f, cellHeight - 10f), 
                        new Color(0.3f, 0.4f, 0.5f, 0.6f), battleFieldPanel);
                    
                    // 敵表示用テキスト
                    CreateUIText($"敵表示_{col}_{row}", 
                        Vector2.zero, new Vector2(cellWidth - 20f, cellHeight - 20f), 
                        "", 14, gridCell);
                }
            }
        }

        private void CreateComboProgressUI()
        {
            // コンボ進行パネル
            GameObject comboProgressPanel = CreateUIPanel("コンボ進行パネル", 
                new Vector2(Screen.width * 0.35f, Screen.height * 0.1f), 
                new Vector2(400, 500), 
                new Color(0.15f, 0.25f, 0.35f, 0.9f));
            
            // タイトル
            CreateUIText("コンボ進行タイトル", 
                new Vector2(0, 220), new Vector2(380, 40), 
                "🎯 アクティブコンボ", 20, comboProgressPanel);
            
            // コンボアイテム（最大5個）
            for (int i = 0; i < 5; i++)
            {
                float yPos = 150 - i * 80;
                
                GameObject comboItem = CreateUIPanel($"コンボアイテム_{i}", 
                    new Vector2(0, yPos), new Vector2(360, 70), 
                    new Color(0.2f, 0.3f, 0.4f, 0.8f), comboProgressPanel);
                
                // コンボ名
                CreateUIText($"コンボ名_{i}", 
                    new Vector2(-120, 20), new Vector2(240, 25), 
                    $"コンボ {i + 1}", 14, comboItem);
                
                // 進行率バー背景
                GameObject progressBG = CreateUIPanel($"進行率バー背景_{i}", 
                    new Vector2(0, -5), new Vector2(300, 15), 
                    new Color(0.1f, 0.1f, 0.1f, 0.8f), comboItem);
                
                // 進行率バー
                CreateUIPanel($"進行率バー_{i}", 
                    new Vector2(-75, 0), new Vector2(150, 15), 
                    new Color(0.2f, 0.8f, 0.2f, 0.8f), progressBG);
                
                // ステップ表示
                CreateUIText($"ステップ表示_{i}", 
                    new Vector2(120, 20), new Vector2(80, 25), 
                    "1/3", 12, comboItem);
                
                // タイマー表示
                CreateUIText($"タイマー表示_{i}", 
                    new Vector2(-120, -15), new Vector2(100, 20), 
                    "5.2s", 10, comboItem);
                
                // 中断耐性表示
                CreateUIText($"中断耐性表示_{i}", 
                    new Vector2(120, -15), new Vector2(80, 20), 
                    "80%", 10, comboItem);
                
                // 初期状態で非表示
                comboItem.SetActive(false);
            }
        }

        private void CreateEnemyInfoUI()
        {
            // 敵情報パネル
            GameObject enemyInfoPanel = CreateUIPanel("敵情報パネル", 
                new Vector2(Screen.width * 0.35f, Screen.height * 0.35f), 
                new Vector2(300, 200), 
                new Color(0.2f, 0.15f, 0.15f, 0.9f));
            
            // タイトル
            CreateUIText("敵情報タイトル", 
                new Vector2(0, 75), new Vector2(280, 30), 
                "👹 敵情報", 18, enemyInfoPanel);
            
            // 敵HP表示（最大6体）
            for (int i = 0; i < 6; i++)
            {
                float yPos = 40 - i * 25;
                CreateUIText($"敵HP表示_{i}", 
                    new Vector2(0, yPos), new Vector2(280, 20), 
                    $"敵 {i + 1}: HP 100/100", 12, enemyInfoPanel);
            }
        }

        private void CreateStartScreenUI()
        {
            // スタート画面パネル
            GameObject startScreenPanel = CreateUIPanel("Start Screen Panel", Vector2.zero,
                new Vector2(Screen.width, Screen.height), new Color(0.05f, 0.05f, 0.1f, 0.95f));
            
            // タイトル
            CreateUIText("Title Text", new Vector2(0, 150),
                new Vector2(600, 80), "⚔️ バトルシステム ⚔️", 32, startScreenPanel);
            
            // 説明文
            CreateUIText("Instruction Text", new Vector2(0, 50),
                new Vector2(800, 60), "戦闘システムのテストプレイです\n手札から武器を選択して敵を攻撃しましょう", 18, startScreenPanel);
            
            // 開始ボタン
            CreateUIButton("🗺️ 戦闘開始 🗺️", new Vector2(0, -50),
                new Vector2(200, 60), startScreenPanel);
        }

        private GameObject CreateUIPanel(string name, Vector2 position, Vector2 size, Color color)
        {
            return CreateUIPanel(name, position, size, color, null);
        }
        
        private GameObject CreateUIPanel(string name, Vector2 position, Vector2 size, Color color, GameObject parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent != null ? parent.transform : targetCanvas.transform, false);
            
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            
            Image image = panel.AddComponent<Image>();
            image.color = color;
            
            return panel;
        }

        private TextMeshProUGUI CreateUIText(string name, Vector2 position, Vector2 size, string text, int fontSize)
        {
            return CreateUIText(name, position, size, text, fontSize, null);
        }
        
        private TextMeshProUGUI CreateUIText(string name, Vector2 position, Vector2 size, string text, int fontSize, GameObject parent)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent != null ? parent.transform : targetCanvas.transform, false);
            
            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;
            
            return textComponent;
        }

        private Button CreateUIButton(string name, Vector2 position, Vector2 size)
        {
            return CreateUIButton(name, position, size, null);
        }
        
        private Button CreateUIButton(string name, Vector2 position, Vector2 size, GameObject parent)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent != null ? parent.transform : targetCanvas.transform, false);
            
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.4f, 0.8f);
            
            Button button = buttonObj.AddComponent<Button>();
            
            // ボタンテキスト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = size;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = name;
            textComponent.fontSize = 14;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.white;
            
            return button;
        }
    }
}