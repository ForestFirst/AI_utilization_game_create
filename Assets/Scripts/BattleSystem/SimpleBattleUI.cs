using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem
{
    /// <summary>
    /// シンプルな戦闘UI表示テスト用コンポーネント
    /// Canvasにアタッチしてプレイモードでテストします
    /// </summary>
    public class SimpleBattleUI : MonoBehaviour
    {
        [Header("UI Creation Settings")]
        [SerializeField] private bool autoCreateUI = true;
        [SerializeField] private Font defaultFont;
        
        // 日本語対応フォントアセット
        private TMP_FontAsset japaneseFont;
        
        private Canvas canvas;
        private BattleManager battleManager;
        
        // UI要素
        private TextMeshProUGUI turnText;
        private TextMeshProUGUI hpText;
        private TextMeshProUGUI stateText;
        private Button[] weaponButtons = new Button[4];
        private Button nextTurnButton;
        private Button resetButton;
        
        // 敵情報表示UI要素（右上）
        private GameObject enemyInfoPanel;
        private TextMeshProUGUI enemyInfoTitle;
        private TextMeshProUGUI[] enemyHpTexts = new TextMeshProUGUI[6]; // 最大6体の敵
        
        // 戦場表示UI要素
        private GameObject battleFieldPanel;
        private GameObject[,] gridCells = new GameObject[3, 2];  // 3列×2行のグリッド
        private Button[] columnButtons = new Button[3];          // 列先頭クリック用ボタン
        private TextMeshProUGUI[] enemyTexts = new TextMeshProUGUI[6]; // 敵表示用テキスト（最大6体）
        private TextMeshProUGUI targetSelectionText;             // ターゲット選択状態表示
        private Image[] columnHighlights = new Image[3];         // 列ハイライト用画像
        
        void Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas component not found! Attach this script to a Canvas.");
                return;
            }
        
            // CanvasとCanvasScalerの設定を確実に行う
            SetupCanvasConfiguration();
            
            // 日本語フォントアセットを読み込み
            LoadJapaneseFont();
            
            if (autoCreateUI)
            {
                CreateSimpleBattleUI();
            }
            
            // BattleManagerを探す（なければ作成）
            SetupBattleManager();
        }
        
        /// <summary>
        /// 敵情報表示を更新（右上エリア）
        /// </summary>
        void UpdateEnemyInfoDisplay()
        {
            if (battleManager?.BattleField == null || enemyHpTexts == null) return;
            
            // 全ての敵HP表示をクリア
            for (int i = 0; i < enemyHpTexts.Length; i++)
            {
                if (enemyHpTexts[i] != null)
                    enemyHpTexts[i].text = "";
            }
            
            // 現在の敵を取得して表示
            var enemies = battleManager.BattleField.GetAllEnemies();
            int displayIndex = 0;
            
            foreach (var enemy in enemies)
            {
                if (displayIndex >= enemyHpTexts.Length) break;
                
                if (enemyHpTexts[displayIndex] != null)
                {
                    // 敵の位置情報も含めて表示
                    string locationInfo = $"({enemy.gridX + 1}, {enemy.gridY + 1})";
                    string hpInfo = $"{enemy.currentHp} / {enemy.enemyData.baseHp}";
                    
                    enemyHpTexts[displayIndex].text = $"{enemy.enemyData.enemyName} {locationInfo}\nHP: {hpInfo}";
                    
                    // HPの割合に応じて色を変更
                    float hpRatio = (float)enemy.currentHp / enemy.enemyData.baseHp;
                    if (hpRatio > 0.7f)
                    {
                        enemyHpTexts[displayIndex].color = Color.white; // 健康
                    }
                    else if (hpRatio > 0.3f)
                    {
                        enemyHpTexts[displayIndex].color = Color.yellow; // 負傷
                    }
                    else
                    {
                        enemyHpTexts[displayIndex].color = Color.red; // 重傷
                    }
                }
                
                displayIndex++;
            }
            
            // 敵情報タイトルを更新（敵数も表示）
            if (enemyInfoTitle != null)
            {
                enemyInfoTitle.text = $"=== 敵情報 ({enemies.Count}体) ===";
            }
        }

        
        void SetupCanvasConfiguration()
        {
            Debug.Log("Setting up Canvas configuration...");
            
            // Canvasの基本設定
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // CanvasScalerの設定
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                Debug.Log("CanvasScaler component added");
            }
            
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // GraphicRaycasterの確認
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("GraphicRaycaster component added");
            }
            
            Debug.Log($"Canvas configuration complete. Screen size will be scaled to fit {scaler.referenceResolution}");
        }
        
        /// <summary>
        /// 日本語対応フォントアセットを読み込む
        /// </summary>
        void LoadJapaneseFont()
        {
            // DotGothic16-Regular SDFフォントアセットを読み込み
            japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/DotGothic16-Regular SDF");
            
            if (japaneseFont != null)
            {
                Debug.Log($"Japanese font loaded: {japaneseFont.name}");
            }
            else
            {
                Debug.LogWarning("Failed to load Japanese font! UI text may not display correctly.");
                // フォールバック：デフォルトのTextMeshProフォントを使用
                japaneseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
        }
        
        void Update()
        {
            UpdateUI();
        }
        
        void CreateSimpleBattleUI()
        {
            Debug.Log("Creating Simple Battle UI...");
            
            // 画面サイズを取得してレスポンシブ対応
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float screenWidth = canvasRect.rect.width;
            float screenHeight = canvasRect.rect.height;
            
            Debug.Log($"Screen size: {screenWidth} x {screenHeight}");
            
            // スケールファクターを計算（1920x1080を基準とする）
            float scaleX = screenWidth / 1920f;
            float scaleY = screenHeight / 1080f;
            float scale = Mathf.Min(scaleX, scaleY);
            
            // 背景パネル（画面全体の80%）
            GameObject backgroundPanel = CreateUIPanel("Background Panel", Vector2.zero, 
                new Vector2(screenWidth * 0.8f, screenHeight * 0.7f), new Color(0, 0, 0, 0.3f));
            
            // ターン表示（左上）
            turnText = CreateUIText("Turn Display", 
                new Vector2(-screenWidth * 0.35f, screenHeight * 0.25f), 
                new Vector2(200 * scale, 50 * scale), "Turn: 1", Mathf.RoundToInt(24 * scale));
            
            // HP表示（左上下）
            hpText = CreateUIText("HP Display", 
                new Vector2(-screenWidth * 0.35f, screenHeight * 0.15f), 
                new Vector2(300 * scale, 50 * scale), "HP: 15000 / 15000", Mathf.RoundToInt(20 * scale));
            
            // ゲーム状態表示（中央上）
            stateText = CreateUIText("State Display", 
                new Vector2(0, screenHeight * 0.25f), 
                new Vector2(200 * scale, 50 * scale), "Player Turn", Mathf.RoundToInt(20 * scale));
            
            // 武器ボタン（2x2グリッド、中央）
            for (int i = 0; i < 4; i++)
            {
                int weaponIndex = i; // クロージャー問題を回避するローカル変数
                int row = weaponIndex / 2;
                int col = weaponIndex % 2;
                Vector2 buttonPos = new Vector2(
                    -80 * scale + col * 160 * scale, 
                    50 * scale - row * 80 * scale
                );
                weaponButtons[weaponIndex] = CreateUIButton($"武器 {weaponIndex + 1}", buttonPos, 
                    new Vector2(140 * scale, 60 * scale), () => OnWeaponClicked(weaponIndex));
            }
            
            // 次ターンボタン（右下）
            nextTurnButton = CreateUIButton("次のターン", 
                new Vector2(screenWidth * 0.25f, -screenHeight * 0.1f), 
                new Vector2(140 * scale, 60 * scale), OnNextTurnClicked);
            
            // リセットボタン（右下下）
            resetButton = CreateUIButton("戦闘リセット", 
                new Vector2(screenWidth * 0.25f, -screenHeight * 0.2f), 
                new Vector2(140 * scale, 60 * scale), OnResetClicked);
            
            // 戦場表示を作成（中央左）
            CreateBattleFieldDisplay(scale, screenWidth, screenHeight);
            
            // 敵情報表示を作成（右上）
            CreateEnemyInfoDisplay(scale, screenWidth, screenHeight);
            
            // ターゲット選択状態表示（戦場の上）
            targetSelectionText = CreateUIText("ターゲット選択状態", 
                new Vector2(-screenWidth * 0.15f, screenHeight * 0.1f), 
                new Vector2(300 * scale, 40 * scale), "選択してください", 
                Mathf.RoundToInt(16 * scale));
            targetSelectionText.color = Color.yellow;
            
            // テスト情報表示（下部中央）
            CreateUIText("Info", new Vector2(0, -screenHeight * 0.3f), 
                new Vector2(600 * scale, 100 * scale), 
                "戦闘テストUI\\n列先頭クリックでターゲット選択、武器ボタンで攻撃\\n次のターンでターン終了、リセットで戦闘リセット", 
                Mathf.RoundToInt(14 * scale));
            
            Debug.Log("Simple Battle UI with BattleField Display created successfully!");
        }
        
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
        
        TextMeshProUGUI CreateUIText(string name, Vector2 position, Vector2 size, string text, int fontSize)
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
            textComponent.alignment = TextAlignmentOptions.Center;
            
            // 日本語フォントを適用
            if (japaneseFont != null)
            {
                textComponent.font = japaneseFont;
            }
            
            return textComponent;
        }
        
        Button CreateUIButton(string name, Vector2 position, Vector2 size, System.Action onClick)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.3f, 0.6f, 0.8f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // ボタンテキスト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = name;
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            // 日本語フォントを適用
            if (japaneseFont != null)
            {
                textComponent.font = japaneseFont;
            }
            
            return button;
        }
        
        void SetupBattleManager()
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                Debug.Log("BattleManager not found, creating new one...");
                GameObject bmObj = new GameObject("BattleManager");
                battleManager = bmObj.AddComponent<BattleManager>();
            }
            else
            {
                Debug.Log("BattleManager found and connected");
            }
            
            // BattleManagerのイベントを購読
            SubscribeToBattleManagerEvents();
            
            // 確実に動作するようダミーデータを作成
            CreateTestDatabasesForBattleManager();
        }
        
        /// <summary>
        /// BattleManagerのイベントを購読
        /// </summary>
        void SubscribeToBattleManagerEvents()
        {
            if (battleManager == null) return;
            
            // ターン変更時の処理（前回選択復元）
            battleManager.OnTurnChanged += OnTurnChanged;
            
            Debug.Log("BattleManager events subscribed");
        }
        
        /// <summary>
        /// ターン変更時の処理（新ターン開始時の前回選択復元）
        /// </summary>
        void OnTurnChanged(int newTurn)
        {
            Debug.Log($"=== Turn Changed to {newTurn} ===");
            
            if (battleManager?.LastSelectedTarget != null && battleManager.LastSelectedTarget.isValid)
            {
                Debug.Log($"Restoring last selected target: {battleManager.LastSelectedTarget.targetType} at column {battleManager.LastSelectedTarget.columnIndex}");
                
                // 前回のターゲットを再選択
                bool success = battleManager.ReselectLastTarget();
                
                if (success)
                {
                    Debug.Log("✓ Last target restored successfully!");
                }
                else
                {
                    Debug.LogWarning("✗ Failed to restore last target");
                }
            }
            else
            {
                Debug.Log("No previous target to restore");
            }
        }
        
        void OnDestroy()
        {
            // イベントの購読解除
            if (battleManager != null)
            {
                battleManager.OnTurnChanged -= OnTurnChanged;
            }
        }
        
        void CreateTestDatabasesForBattleManager()
        {
            Debug.Log("Creating test databases for BattleManager...");
            
            // テスト用武器データベースを作成
            WeaponDatabase weaponDB = ScriptableObject.CreateInstance<WeaponDatabase>();
            WeaponData[] testWeapons = new WeaponData[4]
            {
                new WeaponData("炎の剣", AttackAttribute.Fire, WeaponType.Sword, 120, AttackRange.SingleFront)
                {
                    criticalRate = 15,
                    cooldownTurns = 0,
                    specialEffect = "炎上効果"
                },
                new WeaponData("氷の斧", AttackAttribute.Ice, WeaponType.Axe, 95, AttackRange.SingleFront)
                {
                    criticalRate = 25,
                    cooldownTurns = 1,
                    specialEffect = "凍結効果"
                },
                new WeaponData("雷槍", AttackAttribute.Thunder, WeaponType.Spear, 110, AttackRange.Column)
                {
                    criticalRate = 20,
                    cooldownTurns = 0,
                    specialEffect = "麻痺効果"
                },
                new WeaponData("大剣", AttackAttribute.None, WeaponType.Sword, 140, AttackRange.SingleFront)
                {
                    criticalRate = 10,
                    cooldownTurns = 2,
                    specialEffect = "高威力攻撃"
                }
            };
            
            // Reflectionでprivateフィールドを設定
            var weaponsField = typeof(WeaponDatabase).GetField("weapons", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            weaponsField?.SetValue(weaponDB, testWeapons);
            
            // テスト用敵データベースを作成
            EnemyDatabase enemyDB = ScriptableObject.CreateInstance<EnemyDatabase>();
            EnemyData[] testEnemies = new EnemyData[2]
            {
                new EnemyData
                {
                    enemyName = "機械兵士",
                    enemyId = 1,
                    category = EnemyCategory.Attacker,
                    baseHp = 5000,
                    attackPower = 1500,
                    primaryAction = EnemyActionType.Attack,
                    canBeSummoned = true
                },
                new EnemyData
                {
                    enemyName = "機械警備",
                    enemyId = 2,
                    category = EnemyCategory.Vanguard,
                    baseHp = 8000,
                    attackPower = 1200,
                    primaryAction = EnemyActionType.DefendAlly,
                    canBeSummoned = true
                }
            };
            
            var enemiesField = typeof(EnemyDatabase).GetField("enemies", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enemiesField?.SetValue(enemyDB, testEnemies);
            
            // BattleManagerにデータベースを設定
            var weaponDBField = typeof(BattleManager).GetField("weaponDatabase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            weaponDBField?.SetValue(battleManager, weaponDB);
            
            var enemyDBField = typeof(BattleManager).GetField("enemyDatabase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enemyDBField?.SetValue(battleManager, enemyDB);
            
            Debug.Log("Test databases created and assigned to BattleManager!");
            
            // 武器名をUIボタンに反映
            UpdateWeaponButtonNames(testWeapons);
            
            // 【重要】プレイヤーに武器を実際に装備させる
            EquipWeaponsToPlayer(testWeapons);
            
            // テスト用の敵を戦場に配置
            CreateTestEnemiesOnBattleField();
        }
        
        void UpdateWeaponButtonNames(WeaponData[] weapons)
        {
            for (int i = 0; i < weaponButtons.Length && i < weapons.Length; i++)
            {
                if (weaponButtons[i] != null && weapons[i] != null)
                {
                    TextMeshProUGUI buttonText = weaponButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.text = weapons[i].weaponName;
                    }
                }
            }
        }
        
        /// <summary>
        /// プレイヤーに武器を実際に装備させる（重要！）
        /// </summary>
        void EquipWeaponsToPlayer(WeaponData[] weapons)
        {
            if (battleManager?.PlayerData == null)
            {
                Debug.LogError("PlayerData is null! Cannot equip weapons");
                return;
            }
            
            Debug.Log("Equipping weapons to player...");
            
            // プレイヤーの装備武器配列が初期化されていない場合は初期化
            if (battleManager.PlayerData.equippedWeapons == null)
            {
                battleManager.PlayerData.equippedWeapons = new WeaponData[4];
            }
            
            if (battleManager.PlayerData.weaponCooldowns == null)
            {
                battleManager.PlayerData.weaponCooldowns = new int[4];
            }
            
            // 武器を装備
            for (int i = 0; i < 4 && i < weapons.Length; i++)
            {
                battleManager.PlayerData.equippedWeapons[i] = weapons[i];
                battleManager.PlayerData.weaponCooldowns[i] = 0;
                Debug.Log($"Equipped weapon {i + 1}: {weapons[i].weaponName}");
            }
            
            Debug.Log("Player weapons equipped successfully!");
        }
        
        /// <summary>
        /// テスト用の敵を戦場に配置
        /// </summary>
        void CreateTestEnemiesOnBattleField()
        {
            if (battleManager?.BattleField == null)
            {
                Debug.LogWarning("BattleField is null, cannot place test enemies");
                return;
            }
            
            Debug.Log("Placing test enemies on battlefield...");
            
            // テスト用の敵データを取得
            var enemyDBField = typeof(BattleManager).GetField("enemyDatabase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            EnemyDatabase enemyDB = enemyDBField?.GetValue(battleManager) as EnemyDatabase;
            
            if (enemyDB?.Enemies == null || enemyDB.Enemies.Length == 0)
            {
                Debug.LogWarning("No enemy data available");
                return;
            }
            
            // 列に敵を配置（テスト用パターン）
            // 列0: 機械兵士を前列に配置
            if (enemyDB.Enemies.Length > 0)
            {
                EnemyData soldier = enemyDB.Enemies[0]; // 機械兵士
                EnemyInstance soldierInstance = new EnemyInstance(soldier, 0, 0);
                battleManager.BattleField.PlaceEnemy(soldierInstance, new GridPosition(0, 0));
                Debug.Log($"Placed {soldier.enemyName} at (0, 0)");
            }
            
            // 列1: 機械警備を後列に配置
            if (enemyDB.Enemies.Length > 1)
            {
                EnemyData guard = enemyDB.Enemies[1]; // 機械警備
                EnemyInstance guardInstance = new EnemyInstance(guard, 1, 1);
                battleManager.BattleField.PlaceEnemy(guardInstance, new GridPosition(1, 1));
                Debug.Log($"Placed {guard.enemyName} at (1, 1)");
            }
            
            // 列2: 空き（ゲート攻撃テスト用）
            Debug.Log("Column 2 left empty for gate attack testing");
            
            Debug.Log("テスト用敵の配置完了!");
        }
        
        /// <summary>
        /// 戦場表示の作成（崩壊スターレイル風の斥め上からの視点）
        /// </summary>
        void CreateBattleFieldDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating BattleField Display...");
            
            // 戦場パネルの作成（中央左寄り）
            battleFieldPanel = CreateUIPanel("戦場パネル", 
                new Vector2(-screenWidth * 0.15f, -screenHeight * 0.05f),
                new Vector2(400 * scale, 280 * scale), 
                new Color(0.1f, 0.1f, 0.2f, 0.8f));
            
            // グリッドセルの作成（3列×2行）
            float gridStartX = -screenWidth * 0.15f;
            float gridStartY = -screenHeight * 0.05f;
            float cellWidth = 120 * scale;
            float cellHeight = 80 * scale;
            float cellSpacing = 10 * scale;
            
            // グリッドセルと敵表示を作成
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 2; row++)
                {
                    float posX = gridStartX + (col - 1) * (cellWidth + cellSpacing);
                    float posY = gridStartY + (row - 0.5f) * (cellHeight + cellSpacing);
                    
                    // グリッドセルの作成
                    gridCells[col, row] = CreateUIPanel($"グリッド_{col}_{row}", 
                        new Vector2(posX, posY), 
                        new Vector2(cellWidth, cellHeight),
                        new Color(0.3f, 0.3f, 0.4f, 0.6f));
                    
                    // グリッド枠線の追加
                    Image cellImage = gridCells[col, row].GetComponent<Image>();
                    if (cellImage != null)
                    {
                        cellImage.color = new Color(0.2f, 0.3f, 0.4f, 0.7f);
                        // シンプルな枠線表現（Outlineコンポーネントを使用）
                        Outline outline = gridCells[col, row].AddComponent<Outline>();
                        outline.effectColor = Color.white;
                        outline.effectDistance = new Vector2(2, 2);
                    }
                    
                    // 敵表示用テキストの作成
                    int enemyIndex = col * 2 + row;
                    enemyTexts[enemyIndex] = CreateUIText($"敵表示_{col}_{row}", 
                        new Vector2(posX, posY), 
                        new Vector2(cellWidth - 10 * scale, cellHeight - 10 * scale), 
                        "", Mathf.RoundToInt(12 * scale));
                    enemyTexts[enemyIndex].color = Color.red;
                    enemyTexts[enemyIndex].alignment = TextAlignmentOptions.Center;
                }
                
                // 列先頭クリックボタンの作成（グリッドの上）
                int columnIndex = col; // クロージャー問題を回避するローカル変数
                float columnButtonX = gridStartX + (columnIndex - 1) * (cellWidth + cellSpacing);
                float columnButtonY = gridStartY + cellHeight + 30 * scale;
                
                columnButtons[columnIndex] = CreateUIButton($"列{columnIndex + 1}", 
                    new Vector2(columnButtonX, columnButtonY), 
                    new Vector2(cellWidth, 40 * scale), 
                    () => OnColumnClicked(columnIndex));
                
                // 列ボタンのスタイル調整
                Image columnButtonImage = columnButtons[columnIndex].GetComponent<Image>();
                if (columnButtonImage != null)
                {
                    columnButtonImage.color = new Color(0.4f, 0.6f, 0.4f, 0.8f);
                }
                
                // 列ハイライト用画像の作成（初期状態では非表示）
                GameObject highlightObj = CreateUIPanel($"列ハイライト_{columnIndex}", 
                    new Vector2(columnButtonX, gridStartY), 
                    new Vector2(cellWidth + 5 * scale, cellHeight * 2 + cellSpacing + 10 * scale), 
                    new Color(1f, 1f, 0f, 0.3f));
                columnHighlights[columnIndex] = highlightObj.GetComponent<Image>();
                highlightObj.SetActive(false); // 初期状態では非表示
            }
            
            Debug.Log("戦場表示作成完了!");
        }
        
        /// <summary>
        /// 敵情報表示の作成（右上エリア）
        /// </summary>
        void CreateEnemyInfoDisplay(float scale, float screenWidth, float screenHeight)
        {
            Debug.Log("Creating Enemy Info Display...");
            
            // 敵情報パネルの作成（右上）
            float panelWidth = 250 * scale;
            float panelHeight = 200 * scale;
            enemyInfoPanel = CreateUIPanel("敵情報パネル", 
                new Vector2(screenWidth * 0.25f, screenHeight * 0.15f),
                new Vector2(panelWidth, panelHeight), 
                new Color(0.2f, 0.1f, 0.1f, 0.8f));
            
            // 敵情報タイトル
            enemyInfoTitle = CreateUIText("敵情報タイトル", 
                new Vector2(screenWidth * 0.25f, screenHeight * 0.25f), 
                new Vector2(panelWidth - 20 * scale, 30 * scale), 
                "=== 敵情報 ===", 
                Mathf.RoundToInt(16 * scale));
            enemyInfoTitle.color = Color.yellow;
            
            // 敵HP表示テキストを作成（最大6体分）
            for (int i = 0; i < enemyHpTexts.Length; i++)
            {
                float yOffset = screenHeight * 0.2f - (i * 25 * scale);
                
                enemyHpTexts[i] = CreateUIText($"敵HP表示_{i}", 
                    new Vector2(screenWidth * 0.25f, yOffset), 
                    new Vector2(panelWidth - 20 * scale, 20 * scale), 
                    "", 
                    Mathf.RoundToInt(12 * scale));
                enemyHpTexts[i].color = Color.white;
                enemyHpTexts[i].alignment = TextAlignmentOptions.TopLeft;
            }
            
            Debug.Log("敵情報表示作成完了!");
        }
        
        void UpdateUI()
        {
            if (battleManager == null) return;
            
            // ターン表示更新
            if (turnText != null)
                turnText.text = $"Turn: {battleManager.CurrentTurn}";
            
            // HP表示更新
            if (hpText != null && battleManager.PlayerData != null)
                hpText.text = $"HP: {battleManager.PlayerData.currentHp} / {battleManager.PlayerData.maxHp}";
            
            // ゲーム状態表示更新
            if (stateText != null)
                stateText.text = battleManager.CurrentState.ToString();
            
            // 戦場の敵表示更新
            UpdateBattleFieldDisplay();
            
            // 敵情報表示更新（右上）
            UpdateEnemyInfoDisplay();
            
            // ターゲット選択状態更新
            UpdateTargetSelectionDisplay();
        }
        
        /// <summary>
        /// 戦場の敵表示を更新
        /// </summary>
        void UpdateBattleFieldDisplay()
        {
            if (battleManager?.BattleField == null || enemyTexts == null) return;
            
            // 全ての敵表示をクリア
            for (int i = 0; i < enemyTexts.Length; i++)
            {
                if (enemyTexts[i] != null)
                    enemyTexts[i].text = "";
            }
            
            // 現在の敵を表示
            var enemies = battleManager.BattleField.GetAllEnemies();
            foreach (var enemy in enemies)
            {
                if (enemy.gridX >= 0 && enemy.gridX < 3 && enemy.gridY >= 0 && enemy.gridY < 2)
                {
                    int enemyIndex = enemy.gridX * 2 + enemy.gridY;
                    if (enemyIndex < enemyTexts.Length && enemyTexts[enemyIndex] != null)
                    {
                        enemyTexts[enemyIndex].text = $"{enemy.enemyData.enemyName}\nHP:{enemy.currentHp}";
                    }
                }
            }
        }
        
        /// <summary>
        /// ターゲット選択状態を更新
        /// </summary>
        void UpdateTargetSelectionDisplay()
        {
            if (battleManager == null || targetSelectionText == null) return;
            
            TargetSelection currentTarget = battleManager.CurrentTarget;
            
            // 列ハイライトをリセット
            for (int i = 0; i < columnHighlights.Length; i++)
            {
                if (columnHighlights[i] != null)
                    columnHighlights[i].gameObject.SetActive(false);
            }
            
            if (currentTarget != null && currentTarget.isValid)
            {
                if (currentTarget.targetType == TargetType.Column)
                {
                    // 列選択状態を表示
                    targetSelectionText.text = $"選択中: 列{currentTarget.columnIndex + 1}";
                    
                    // 列ハイライトを表示
                    if (currentTarget.columnIndex >= 0 && currentTarget.columnIndex < columnHighlights.Length)
                    {
                        if (columnHighlights[currentTarget.columnIndex] != null)
                            columnHighlights[currentTarget.columnIndex].gameObject.SetActive(true);
                    }
                }
                else if (currentTarget.targetType == TargetType.EnemyPosition)
                {
                    targetSelectionText.text = $"選択中: 敵 ({currentTarget.position.x}, {currentTarget.position.y})";
                }
                else
                {
                    targetSelectionText.text = "不明なターゲット";
                }
                
                // 武器選択待ちの状態を表示
                if (battleManager.IsWaitingForWeaponSelection)
                {
                    targetSelectionText.text += " - 武器を選択してください";
                }
            }
            else
            {
                targetSelectionText.text = "ターゲットを選択してください";
            }
        }
        
        /// <summary>
        /// 列クリック時の処理（ターゲット選択）
        /// </summary>
        void OnColumnClicked(int columnIndex)
        {
            Debug.Log($"=== Column {columnIndex + 1} Clicked ===");
            
            if (battleManager == null)
            {
                Debug.LogError("BattleManager is null!");
                return;
            }
            
            if (battleManager.CurrentState != GameState.PlayerTurn)
            {
                Debug.LogWarning($"Cannot select target during {battleManager.CurrentState}");
                return;
            }
            
            // 列をターゲットとして選択
            bool success = battleManager.SelectColumnTarget(columnIndex);
            
            if (success)
            {
                Debug.Log($"✓ Column {columnIndex + 1} selected as target!");
            }
            else
            {
                Debug.LogWarning($"✗ Failed to select column {columnIndex + 1} as target");
            }
        }
        
        void OnWeaponClicked(int weaponIndex)
        {
            Debug.Log($"=== Weapon {weaponIndex + 1} Clicked ===");
            
            if (battleManager == null)
            {
                Debug.LogError("BattleManager is null!");
                return;
            }
            
            Debug.Log($"Current State: {battleManager.CurrentState}");
            Debug.Log($"Current Turn: {battleManager.CurrentTurn}");
            
            if (battleManager.CurrentState != GameState.PlayerTurn)
            {
                Debug.LogWarning($"Not player turn! Current state: {battleManager.CurrentState}");
                return;
            }
            
            // プレイヤーデータの確認
            if (battleManager.PlayerData == null)
            {
                Debug.LogError("PlayerData is null!");
                return;
            }
            
            // 武器データの確認
            if (battleManager.PlayerData.equippedWeapons == null || 
                weaponIndex >= battleManager.PlayerData.equippedWeapons.Length ||
                battleManager.PlayerData.equippedWeapons[weaponIndex] == null)
            {
                Debug.LogError($"Weapon {weaponIndex + 1} is not equipped or invalid!");
                return;
            }
            
            WeaponData weapon = battleManager.PlayerData.equippedWeapons[weaponIndex];
            Debug.Log($"Using weapon: {weapon.weaponName} (Power: {weapon.basePower})");
            
            // クールダウン確認
            if (!battleManager.PlayerData.CanUseWeapon(weaponIndex))
            {
                int cooldown = battleManager.PlayerData.weaponCooldowns[weaponIndex];
                Debug.LogWarning($"Weapon {weaponIndex + 1} is on cooldown! Remaining: {cooldown} turns");
                return;
            }
            
            // ターゲット選択状態を確認
            if (!battleManager.CurrentTarget.isValid)
            {
                Debug.LogWarning("No target selected! Please select a target first by clicking a column.");
                return;
            }
            
            Debug.Log($"Using weapon against selected target: {battleManager.CurrentTarget.targetType} at column {battleManager.CurrentTarget.columnIndex}");
            
            // 現在選択中のターゲットに対して武器使用
            bool success = battleManager.UseWeaponWithCurrentTarget(weaponIndex);
            
            if (success)
            {
                Debug.Log($"\u2713 Weapon {weaponIndex + 1} ({weapon.weaponName}) used successfully!");
                
                // クールダウン情報表示
                if (weapon.cooldownTurns > 0)
                {
                    Debug.Log($"Weapon {weaponIndex + 1} now on cooldown for {weapon.cooldownTurns} turns");
                }
            }
            else
            {
                Debug.LogError($"\u2717 Failed to use weapon {weaponIndex + 1} ({weapon.weaponName})");
            }
            
            Debug.Log("=== Weapon Action Complete ===");
        }
        
        void OnNextTurnClicked()
        {
            Debug.Log("=== Next Turn Button Clicked ===");
            
            if (battleManager == null)
            {
                Debug.LogError("BattleManager is null!");
                return;
            }
            
            Debug.Log($"Current State: {battleManager.CurrentState}");
            Debug.Log($"Current Turn: {battleManager.CurrentTurn}");
            
            if (battleManager.CurrentState == GameState.PlayerTurn)
            {
                battleManager.EndPlayerTurn(TurnEndReason.ActionCompleted);
                Debug.Log("✓ Player turn ended, switching to enemy turn");
            }
            else if (battleManager.CurrentState == GameState.Victory)
            {
                Debug.Log("🏆 Battle already won!");
            }
            else if (battleManager.CurrentState == GameState.Defeat)
            {
                Debug.Log("🖤 Battle already lost!");
            }
            else
            {
                Debug.LogWarning($"Cannot end turn in current state: {battleManager.CurrentState}");
            }
        }
        
        void OnResetClicked()
        {
            Debug.Log("=== Reset Battle Button Clicked ===");
            
            if (battleManager != null)
            {
                var oldState = battleManager.CurrentState;
                var oldTurn = battleManager.CurrentTurn;
                
                battleManager.ResetBattle();
                
                Debug.Log($"✓ Battle reset! {oldState} (Turn {oldTurn}) -> {battleManager.CurrentState} (Turn {battleManager.CurrentTurn})");
                
                // 武器ボタン名を更新
                if (battleManager.PlayerData?.equippedWeapons != null)
                {
                    UpdateWeaponButtonNames(battleManager.PlayerData.equippedWeapons);
                }
            }
            else
            {
                Debug.LogError("BattleManager is null!");
            }
        }
        
        // 手動でUIを再作成
        [ContextMenu("Recreate UI")]
        public void RecreateUI()
        {
            // 既存のUI要素を削除
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(canvas.transform.GetChild(i).gameObject);
            }
            
            // UIを再作成
            CreateSimpleBattleUI();
        }
        
        /// <summary>
        /// 敵が撃破された時の表示更新
        /// </summary>
        public void OnEnemyDefeated(EnemyInstance defeatedEnemy)
        {
            Debug.Log($"Enemy defeated: {defeatedEnemy.enemyData.enemyName} at ({defeatedEnemy.gridX}, {defeatedEnemy.gridY})");
            // 敵情報表示は自動的に更新される（UpdateEnemyInfoDisplayで）
        }
    }
}
