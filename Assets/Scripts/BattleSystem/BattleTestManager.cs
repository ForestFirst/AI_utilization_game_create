using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem
{
    /// <summary>
    /// 戦闘システムの動作確認用テストマネージャー
    /// シーンに配置してプレイモードで動作をテストします
    /// </summary>
    public class BattleTestManager : MonoBehaviour
    {
        [Header("Test Objects")]
        [SerializeField] private GameObject playerCube;
        [SerializeField] private GameObject[] enemyCubes;
        [SerializeField] private Canvas uiCanvas;
        
        [Header("UI Test Elements")]
        [SerializeField] private Button[] testWeaponButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI turnDisplayText;
        [SerializeField] private TextMeshProUGUI playerHPText;
        [SerializeField] private TextMeshProUGUI gameStateText;
        [SerializeField] private Button nextTurnButton;
        
        [Header("Combat Visual")]
        [SerializeField] private Transform battleFieldParent;
        [SerializeField] private GameObject cubePrefab;
        
        private BattleManager battleManager;
        private BattleUI battleUI;
        private WeaponDatabase testWeaponDatabase;
        private EnemyDatabase testEnemyDatabase;
        
        [System.Serializable]
        public class VisualCube
        {
            public GameObject gameObject;
            public Renderer renderer;
            public GridPosition position;
            public bool isEnemy;
        }
        
        private VisualCube[] visualCubes;
        
        void Start()
        {
            SetupTestEnvironment();
        }
        
        [ContextMenu("Setup Test Environment")]
        public void SetupTestEnvironment()
        {
            Debug.Log("Setting up battle test environment...");
            
            // 1. Create weapon database
            CreateTestWeaponDatabase();
            
            // 2. Create enemy database
            CreateTestEnemyDatabase();
            
            // 3. Setup BattleManager
            SetupBattleManager();
            
            // 4. Setup visual cubes
            SetupVisualCubes();
            
            // 5. Setup UI connections
            SetupUIConnections();
            
            Debug.Log("Battle test environment setup complete!");
        }
        
        private void CreateTestWeaponDatabase()
        {
            testWeaponDatabase = ScriptableObject.CreateInstance<WeaponDatabase>();
            
            WeaponData[] testWeapons = new WeaponData[]
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
                new WeaponData("雷槍", AttackAttribute.Thunder, WeaponType.Spear, 110, AttackRange.SingleTarget)
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
            
            // Use reflection to set private field
            var weaponsField = typeof(WeaponDatabase).GetField("weapons", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            weaponsField.SetValue(testWeaponDatabase, testWeapons);
            
            Debug.Log($"Created test weapon database with {testWeapons.Length} weapons");
        }
        
        private void CreateTestEnemyDatabase()
        {
            testEnemyDatabase = ScriptableObject.CreateInstance<EnemyDatabase>();
            
            EnemyData[] testEnemies = new EnemyData[]
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
                    enemyName = "機械護衛",
                    enemyId = 2,
                    category = EnemyCategory.Vanguard,
                    baseHp = 8000,
                    attackPower = 1200,
                    primaryAction = EnemyActionType.DefendAlly,
                    canBeSummoned = true
                }
            };
            
            // Use reflection to set private field
            var enemiesField = typeof(EnemyDatabase).GetField("enemies", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enemiesField.SetValue(testEnemyDatabase, testEnemies);
            
            Debug.Log($"Created test enemy database with {testEnemies.Length} enemies");
        }
        
        private void SetupBattleManager()
        {
            battleManager = GetComponent<BattleManager>();
            if (battleManager == null)
            {
                battleManager = gameObject.AddComponent<BattleManager>();
            }
            
            // Use reflection to set database references
            var weaponDBField = typeof(BattleManager).GetField("weaponDatabase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            weaponDBField?.SetValue(battleManager, testWeaponDatabase);
            
            var enemyDBField = typeof(BattleManager).GetField("enemyDatabase", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enemyDBField?.SetValue(battleManager, testEnemyDatabase);
            
            Debug.Log("BattleManager setup complete with test databases");
        }
        
        private void SetupVisualCubes()
        {
            if (battleFieldParent == null)
            {
                GameObject battleFieldObj = new GameObject("BattleField");
                battleFieldParent = battleFieldObj.transform;
            }
            
            // Create player cube (blue)
            if (playerCube == null)
            {
                playerCube = CreateVisualCube(Vector3.zero, Color.blue, "Player");
                playerCube.transform.position = new Vector3(-3, 0, 0);
            }
            
            // Create enemy cubes (red)
            if (enemyCubes == null || enemyCubes.Length == 0)
            {
                enemyCubes = new GameObject[4];
                for (int i = 0; i < enemyCubes.Length; i++)
                {
                    enemyCubes[i] = CreateVisualCube(Vector3.zero, Color.red, $"Enemy_{i}");
                    enemyCubes[i].transform.position = new Vector3(2 + (i % 2) * 2, 0, (i / 2) * 2);
                }
            }
            
            Debug.Log($"Created visual cubes: 1 player, {enemyCubes.Length} enemies");
        }
        
        private GameObject CreateVisualCube(Vector3 position, Color color, string name)
        {
            GameObject cube;
            
            if (cubePrefab != null)
            {
                cube = Instantiate(cubePrefab, battleFieldParent);
            }
            else
            {
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(battleFieldParent);
            }
            
            cube.name = name;
            cube.transform.position = position;
            
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = color;
                renderer.material = material;
            }
            
            return cube;
        }
        
        private void SetupUIConnections()
        {
            // Auto-find UI elements if not assigned
            if (uiCanvas == null)
                uiCanvas = FindObjectOfType<Canvas>();
            
            if (turnDisplayText == null)
                turnDisplayText = CreateSimpleText("Turn 1", new Vector2(-300, 200));
            
            if (playerHPText == null)
                playerHPText = CreateSimpleText("HP: 15000/15000", new Vector2(-300, -200));
            
            if (gameStateText == null)
                gameStateText = CreateSimpleText("Player Turn", new Vector2(0, 200));
            
            if (nextTurnButton == null)
                nextTurnButton = CreateSimpleButton("Next Turn", new Vector2(300, -200), OnNextTurnClicked);
            
            // Create weapon buttons
            for (int i = 0; i < testWeaponButtons.Length; i++)
            {
                if (testWeaponButtons[i] == null)
                {
                    int weaponIndex = i;
                    Vector2 buttonPos = new Vector2(200 + (i % 2) * 120, -100 - (i / 2) * 60);
                    testWeaponButtons[i] = CreateSimpleButton($"武器{i + 1}", buttonPos, 
                        () => OnWeaponButtonClicked(weaponIndex));
                }
            }
            
            Debug.Log("UI connections setup complete");
        }
        
        private TextMeshProUGUI CreateSimpleText(string text, Vector2 position)
        {
            if (uiCanvas == null) return null;
            
            GameObject textObj = new GameObject("TestText");
            textObj.transform.SetParent(uiCanvas.transform, false);
            
            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(200, 50);
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 16;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            return textComponent;
        }
        
        private Button CreateSimpleButton(string text, Vector2 position, System.Action onClick)
        {
            if (uiCanvas == null) return null;
            
            GameObject buttonObj = new GameObject("TestButton");
            buttonObj.transform.SetParent(uiCanvas.transform, false);
            
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(100, 40);
            
            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            Button button = buttonObj.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            
            // Add text to button
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            
            return button;
        }
        
        private void OnWeaponButtonClicked(int weaponIndex)
        {
            if (battleManager != null && battleManager.CurrentState == GameState.PlayerTurn)
            {
                GridPosition targetPosition = new GridPosition(0, 0);
                bool success = battleManager.UseWeapon(weaponIndex, targetPosition);
                
                if (success)
                {
                    Debug.Log($"Used weapon {weaponIndex + 1}!");
                    UpdateVisualFeedback();
                }
                else
                {
                    Debug.Log($"Cannot use weapon {weaponIndex + 1}");
                }
            }
        }
        
        private void OnNextTurnClicked()
        {
            if (battleManager != null && battleManager.CurrentState == GameState.PlayerTurn)
            {
                battleManager.EndPlayerTurn(TurnEndReason.ActionCompleted);
            }
        }
        
        private void UpdateVisualFeedback()
        {
            // Simple visual feedback - make enemy cube flash
            if (enemyCubes != null && enemyCubes.Length > 0)
            {
                StartCoroutine(FlashCube(enemyCubes[0]));
            }
        }
        
        private System.Collections.IEnumerator FlashCube(GameObject cube)
        {
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color originalColor = renderer.material.color;
                renderer.material.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                renderer.material.color = originalColor;
            }
        }
        
        void Update()
        {
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (battleManager == null) return;
            
            // Update turn display
            if (turnDisplayText != null)
                turnDisplayText.text = $"Turn {battleManager.CurrentTurn}";
            
            // Update player HP
            if (playerHPText != null && battleManager.PlayerData != null)
                playerHPText.text = $"HP: {battleManager.PlayerData.currentHp}/{battleManager.PlayerData.maxHp}";
            
            // Update game state
            if (gameStateText != null)
                gameStateText.text = battleManager.CurrentState.ToString();
        }
        
        [ContextMenu("Reset Battle")]
        public void ResetBattle()
        {
            if (battleManager != null)
            {
                battleManager.ResetBattle();
                Debug.Log("Battle reset!");
            }
        }
    }
}
