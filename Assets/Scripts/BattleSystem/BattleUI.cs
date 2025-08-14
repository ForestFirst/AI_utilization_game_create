using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem
{
    public class BattleUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas battleCanvas;
        
        [Header("Player Info UI")]
        [SerializeField] private Slider playerHPSlider;
        [SerializeField] private TextMeshProUGUI playerHPText;
        [SerializeField] private TextMeshProUGUI playerStatusText;
        
        [Header("Turn Info UI")]
        [SerializeField] private TextMeshProUGUI turnText;
        
        [Header("Enemy Info UI")]
        [SerializeField] private Slider enemyHPSlider;
        [SerializeField] private TextMeshProUGUI enemyHPText;
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private TextMeshProUGUI enemyStatusText;
        
        [Header("Weapon Selection UI")]
        [SerializeField] private Button[] weaponButtons = new Button[4];
        [SerializeField] private Image[] weaponIcons = new Image[4];
        [SerializeField] private TextMeshProUGUI[] weaponPowerTexts = new TextMeshProUGUI[4];
        [SerializeField] private TextMeshProUGUI[] weaponCritTexts = new TextMeshProUGUI[4];
        [SerializeField] private GameObject weaponDetailPanel;
        [SerializeField] private TextMeshProUGUI selectedWeaponNameText;
        [SerializeField] private TextMeshProUGUI selectedWeaponDetailsText;
        
        [Header("Combo UI")]
        [SerializeField] private Transform comboListParent;
        [SerializeField] private GameObject comboPrefab;
        [SerializeField] private List<ComboUIItem> comboItems = new List<ComboUIItem>();
        
        [Header("Damage Display")]
        [SerializeField] private Transform damageNumberParent;
        [SerializeField] private GameObject damageNumberPrefab;
        
        [Header("Menu UI")]
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject pauseMenu;
        
        // Internal state
        private BattleManager battleManager;
        private int selectedWeaponIndex = -1;
        private List<DamageNumber> activeDamageNumbers = new List<DamageNumber>();
        
        [System.Serializable]
        public class ComboUIItem
        {
            public GameObject root;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI effectText;
            public Transform weaponSequenceParent;
            public Slider progressSlider;
        }
        
        [System.Serializable]
        public class DamageNumber
        {
            public GameObject gameObject;
            public TextMeshProUGUI text;
            public float lifetime;
            public Vector3 velocity;
        }
        
        private void Awake()
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                Debug.LogError("BattleManager not found! Please add BattleManager to the scene.");
                return;
            }
            
            InitializeUI();
            SubscribeToEvents();
        }
        
        private void InitializeUI()
        {
            // Weapon buttons initialization
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                int weaponIndex = i; // Capture for closure
                weaponButtons[i].onClick.AddListener(() => OnWeaponButtonClicked(weaponIndex));
            }
            
            // Menu button initialization
            if (menuButton != null)
                menuButton.onClick.AddListener(OnMenuButtonClicked);
            
            // Hide weapon detail panel initially
            if (weaponDetailPanel != null)
                weaponDetailPanel.SetActive(false);
                
            // Hide pause menu initially
            if (pauseMenu != null)
                pauseMenu.SetActive(false);
        }
        
        private void SubscribeToEvents()
        {
            if (battleManager != null)
            {
                battleManager.OnGameStateChanged += OnGameStateChanged;
                battleManager.OnTurnChanged += OnTurnChanged;
                battleManager.OnPlayerDataChanged += OnPlayerDataChanged;
                battleManager.OnBattleEnded += OnBattleEnded;
            }
        }
        
        private void Update()
        {
            UpdateDamageNumbers();
        }
        
        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.PlayerTurn:
                    EnablePlayerInput(true);
                    break;
                case GameState.EnemyTurn:
                    EnablePlayerInput(false);
                    break;
                case GameState.Victory:
                    ShowVictoryScreen();
                    break;
                case GameState.Defeat:
                    ShowDefeatScreen();
                    break;
            }
        }
        
        private void OnTurnChanged(int newTurn)
        {
            if (turnText != null)
                turnText.text = $"Turn {newTurn}";
        }
        
        private void OnPlayerDataChanged(PlayerData playerData)
        {
            UpdatePlayerUI(playerData);
            UpdateWeaponUI(playerData);
        }
        
        private void OnBattleEnded(BattleResult result)
        {
            if (result.isVictory)
            {
                ShowAttachmentSelectionScreen();
            }
            else
            {
                ShowGameOverScreen();
            }
        }
        
        private void UpdatePlayerUI(PlayerData playerData)
        {
            if (playerHPSlider != null)
            {
                playerHPSlider.value = (float)playerData.currentHp / playerData.maxHp;
            }
            
            if (playerHPText != null)
            {
                playerHPText.text = $"{playerData.currentHp:N0}/{playerData.maxHp:N0}";
            }
            
            if (playerStatusText != null)
            {
                playerStatusText.text = playerData.IsAlive() ? "機能正常" : "機能停止";
            }
        }
        
        private void UpdateWeaponUI(PlayerData playerData)
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                if (i < playerData.equippedWeapons.Length && playerData.equippedWeapons[i] != null)
                {
                    WeaponData weapon = playerData.equippedWeapons[i];
                    
                    // Update weapon button appearance
                    weaponButtons[i].interactable = playerData.CanUseWeapon(i);
                    
                    // Update weapon power text
                    if (weaponPowerTexts[i] != null)
                        weaponPowerTexts[i].text = weapon.basePower.ToString();
                    
                    // Update weapon crit text
                    if (weaponCritTexts[i] != null)
                        weaponCritTexts[i].text = $"{weapon.criticalRate}%";
                    
                    // Update weapon icon based on type/attribute (color coding)
                    if (weaponIcons[i] != null)
                    {
                        weaponIcons[i].color = GetWeaponColor(weapon.attackAttribute);
                    }
                }
                else
                {
                    weaponButtons[i].interactable = false;
                    if (weaponPowerTexts[i] != null) weaponPowerTexts[i].text = "-";
                    if (weaponCritTexts[i] != null) weaponCritTexts[i].text = "-";
                }
            }
        }
        
        private void OnWeaponButtonClicked(int weaponIndex)
        {
            selectedWeaponIndex = weaponIndex;
            UpdateSelectedWeaponDisplay();
            
            // For now, automatically use weapon on front enemy
            GridPosition targetPosition = new GridPosition(0, 0); // Front position
            bool success = battleManager.UseWeapon(weaponIndex, targetPosition);
            
            if (success)
            {
                ShowDamageNumber(UnityEngine.Random.Range(800, 2500), DamageDisplayType.Normal);
                battleManager.EndPlayerTurn(TurnEndReason.ActionCompleted);
            }
        }
        
        private void UpdateSelectedWeaponDisplay()
        {
            if (selectedWeaponIndex >= 0 && selectedWeaponIndex < battleManager.PlayerData.equippedWeapons.Length)
            {
                WeaponData selectedWeapon = battleManager.PlayerData.equippedWeapons[selectedWeaponIndex];
                
                if (selectedWeapon != null && weaponDetailPanel != null)
                {
                    weaponDetailPanel.SetActive(true);
                    
                    if (selectedWeaponNameText != null)
                        selectedWeaponNameText.text = selectedWeapon.weaponName;
                    
                    if (selectedWeaponDetailsText != null)
                    {
                        selectedWeaponDetailsText.text = 
                            $"攻撃力: {selectedWeapon.basePower} | クリティカル: {selectedWeapon.criticalRate}%\n" +
                            $"属性: {GetAttributeDisplayName(selectedWeapon.attackAttribute)}";
                    }
                }
            }
            else
            {
                if (weaponDetailPanel != null)
                    weaponDetailPanel.SetActive(false);
            }
        }
        
        private void EnablePlayerInput(bool enable)
        {
            for (int i = 0; i < weaponButtons.Length; i++)
            {
                weaponButtons[i].interactable = enable && battleManager.PlayerData.CanUseWeapon(i);
            }
        }
        
        private void OnMenuButtonClicked()
        {
            if (pauseMenu != null)
            {
                bool isActive = pauseMenu.activeSelf;
                pauseMenu.SetActive(!isActive);
                
                if (!isActive)
                {
                    // Pause the game
                    Time.timeScale = 0f;
                }
                else
                {
                    // Resume the game
                    Time.timeScale = 1f;
                }
            }
        }
        
        public void ShowDamageNumber(int damage, DamageDisplayType type)
        {
            if (damageNumberPrefab == null || damageNumberParent == null)
                return;
            
            GameObject damageObj = Instantiate(damageNumberPrefab, damageNumberParent);
            TextMeshProUGUI damageText = damageObj.GetComponent<TextMeshProUGUI>();
            
            if (damageText != null)
            {
                damageText.text = damage.ToString("N0");
                
                // Set color based on damage type
                switch (type)
                {
                    case DamageDisplayType.Normal:
                        damageText.color = Color.white;
                        break;
                    case DamageDisplayType.Critical:
                        damageText.color = Color.red;
                        damageText.text += "!";
                        break;
                    case DamageDisplayType.Combo:
                        damageText.color = Color.magenta;
                        damageText.text += " COMBO";
                        break;
                }
            }
            
            DamageNumber damageNumber = new DamageNumber
            {
                gameObject = damageObj,
                text = damageText,
                lifetime = 2f,
                velocity = new Vector3(UnityEngine.Random.Range(-50f, 50f), 100f, 0f)
            };
            
            activeDamageNumbers.Add(damageNumber);
        }
        
        private void UpdateDamageNumbers()
        {
            for (int i = activeDamageNumbers.Count - 1; i >= 0; i--)
            {
                DamageNumber damageNum = activeDamageNumbers[i];
                damageNum.lifetime -= Time.deltaTime;
                
                if (damageNum.lifetime <= 0f)
                {
                    if (damageNum.gameObject != null)
                        Destroy(damageNum.gameObject);
                    activeDamageNumbers.RemoveAt(i);
                }
                else
                {
                    // Animate damage number
                    if (damageNum.gameObject != null)
                    {
                        Vector3 pos = damageNum.gameObject.transform.position;
                        pos += damageNum.velocity * Time.deltaTime;
                        damageNum.velocity.y -= 200f * Time.deltaTime; // Gravity
                        damageNum.gameObject.transform.position = pos;
                        
                        // Fade out
                        if (damageNum.text != null)
                        {
                            Color color = damageNum.text.color;
                            color.a = damageNum.lifetime / 2f;
                            damageNum.text.color = color;
                        }
                    }
                }
            }
        }
        
        private void ShowVictoryScreen()
        {
            Debug.Log("Victory! Battle completed successfully.");
            ShowAttachmentSelectionScreen();
        }
        
        private void ShowDefeatScreen()
        {
            Debug.Log("Defeat! Battle failed.");
        }
        
        private void ShowAttachmentSelectionScreen()
        {
            Debug.Log("Showing attachment selection screen...");
            
            // アタッチメント選択UIを探す
            AttachmentSelectionUI selectionUI = FindObjectOfType<AttachmentSelectionUI>();
            if (selectionUI != null)
            {
                Debug.Log("AttachmentSelectionUI found! 選択画面を表示します");
                selectionUI.ShowSelectionScreen();
                Debug.Log("アタッチメント選択画面を表示しました");
            }
            else
            {
                Debug.LogWarning("AttachmentSelectionUI not found! 動的にUI要素を作成します");
                CreateAttachmentSelectionUI();
            }
        }

        /// <summary>
        /// アタッチメント選択UIを動的に作成します
        /// </summary>
        private void CreateAttachmentSelectionUI()
        {
            Debug.Log("Creating AttachmentSelectionUI dynamically...");
            
            // AttachmentSystemの確認・作成
            AttachmentSystem attachmentSystem = AttachmentUIBuilder.EnsureAttachmentSystem();
            if (attachmentSystem == null)
            {
                Debug.LogError("Failed to create AttachmentSystem");
                return;
            }

            // Canvasを探す
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Canvas not found! Cannot create AttachmentSelectionUI");
                return;
            }

            // UIBuilderを使用してUIを作成
            AttachmentSelectionUI selectionUI = AttachmentUIBuilder.CreateAttachmentSelectionUI(canvas);
            if (selectionUI == null)
            {
                Debug.LogError("Failed to create AttachmentSelectionUI");
                return;
            }
            
            // 作成したUIで選択画面を表示
            selectionUI.ShowSelectionScreen();
        }

        private void CreateBasicSelectionUI(GameObject parentGO, AttachmentSelectionUI selectionUI)
        {
            // Selection Panel作成
            GameObject selectionPanel = new GameObject("SelectionPanel");
            selectionPanel.transform.SetParent(parentGO.transform, false);
            
            UnityEngine.UI.Image panelImage = selectionPanel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f); // 半透明黒背景
            
            RectTransform panelRect = selectionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Options Container作成
            GameObject optionsContainer = new GameObject("OptionsContainer");
            optionsContainer.transform.SetParent(selectionPanel.transform, false);
            
            UnityEngine.UI.GridLayoutGroup gridLayout = optionsContainer.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(300, 100);
            gridLayout.spacing = new Vector2(20, 20);
            gridLayout.startCorner = UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            
            RectTransform containerRect = optionsContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.1f, 0.3f);
            containerRect.anchorMax = new Vector2(0.9f, 0.7f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Option Button Prefab作成
            GameObject optionButtonPrefab = CreateOptionButtonPrefab(selectionPanel);

            // Skip Button作成
            GameObject skipButton = new GameObject("SkipButton");
            skipButton.transform.SetParent(selectionPanel.transform, false);
            
            UnityEngine.UI.Button skipBtn = skipButton.AddComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image skipBtnImage = skipButton.AddComponent<UnityEngine.UI.Image>();
            skipBtnImage.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            
            GameObject skipBtnText = new GameObject("Text");
            skipBtnText.transform.SetParent(skipButton.transform, false);
            TMPro.TextMeshProUGUI skipText = skipBtnText.AddComponent<TMPro.TextMeshProUGUI>();
            skipText.text = "スキップ";
            skipText.color = Color.white;
            skipText.fontSize = 18;
            skipText.alignment = TMPro.TextAlignmentOptions.Center;
            
            RectTransform skipBtnRect = skipButton.GetComponent<RectTransform>();
            skipBtnRect.anchorMin = new Vector2(0.4f, 0.1f);
            skipBtnRect.anchorMax = new Vector2(0.6f, 0.2f);
            skipBtnRect.offsetMin = Vector2.zero;
            skipBtnRect.offsetMax = Vector2.zero;

            RectTransform skipTextRect = skipBtnText.GetComponent<RectTransform>();
            skipTextRect.anchorMin = Vector2.zero;
            skipTextRect.anchorMax = Vector2.one;
            skipTextRect.offsetMin = Vector2.zero;
            skipTextRect.offsetMax = Vector2.zero;

            // Title Text作成
            GameObject titleText = new GameObject("TitleText");
            titleText.transform.SetParent(selectionPanel.transform, false);
            TMPro.TextMeshProUGUI titleTMP = titleText.AddComponent<TMPro.TextMeshProUGUI>();
            titleTMP.text = "アタッチメント選択";
            titleTMP.color = Color.white;
            titleTMP.fontSize = 24;
            titleTMP.alignment = TMPro.TextAlignmentOptions.Center;
            
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.8f);
            titleRect.anchorMax = new Vector2(1f, 0.9f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Instruction Text作成
            GameObject instructionText = new GameObject("InstructionText");
            instructionText.transform.SetParent(selectionPanel.transform, false);
            TMPro.TextMeshProUGUI instructionTMP = instructionText.AddComponent<TMPro.TextMeshProUGUI>();
            instructionTMP.text = "装備するアタッチメントを選択してください";
            instructionTMP.color = Color.white;
            instructionTMP.fontSize = 16;
            instructionTMP.alignment = TMPro.TextAlignmentOptions.Center;
            
            RectTransform instructionRect = instructionText.GetComponent<RectTransform>();
            instructionRect.anchorMin = new Vector2(0f, 0.75f);
            instructionRect.anchorMax = new Vector2(1f, 0.8f);
            instructionRect.offsetMin = Vector2.zero;
            instructionRect.offsetMax = Vector2.zero;

            // AttachmentSelectionUIのフィールドを設定
            SetAttachmentSelectionUIFields(selectionUI, selectionPanel, optionsContainer.transform, optionButtonPrefab, skipBtn, titleTMP, instructionTMP);
            
            Debug.Log("Basic AttachmentSelectionUI structure created");
        }

        private GameObject CreateOptionButtonPrefab(GameObject parent)
        {
            GameObject buttonPrefab = new GameObject("OptionButtonPrefab");
            buttonPrefab.transform.SetParent(parent.transform, false);
            buttonPrefab.SetActive(false); // プレハブなので非アクティブ
            
            UnityEngine.UI.Button button = buttonPrefab.AddComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image buttonImage = buttonPrefab.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // Main Text
            GameObject mainText = new GameObject("MainText");
            mainText.transform.SetParent(buttonPrefab.transform, false);
            TMPro.TextMeshProUGUI mainTMP = mainText.AddComponent<TMPro.TextMeshProUGUI>();
            mainTMP.text = "アタッチメント名";
            mainTMP.color = Color.white;
            mainTMP.fontSize = 16;
            mainTMP.alignment = TMPro.TextAlignmentOptions.Center;
            
            RectTransform mainTextRect = mainText.GetComponent<RectTransform>();
            mainTextRect.anchorMin = new Vector2(0f, 0.6f);
            mainTextRect.anchorMax = new Vector2(1f, 1f);
            mainTextRect.offsetMin = Vector2.zero;
            mainTextRect.offsetMax = Vector2.zero;
            
            // Sub Text
            GameObject subText = new GameObject("SubText");
            subText.transform.SetParent(buttonPrefab.transform, false);
            TMPro.TextMeshProUGUI subTMP = subText.AddComponent<TMPro.TextMeshProUGUI>();
            subTMP.text = "説明";
            subTMP.color = Color.gray;
            subTMP.fontSize = 12;
            subTMP.alignment = TMPro.TextAlignmentOptions.Center;
            
            RectTransform subTextRect = subText.GetComponent<RectTransform>();
            subTextRect.anchorMin = new Vector2(0f, 0.3f);
            subTextRect.anchorMax = new Vector2(1f, 0.6f);
            subTextRect.offsetMin = Vector2.zero;
            subTextRect.offsetMax = Vector2.zero;
            
            // Rarity Text
            GameObject rarityText = new GameObject("RarityText");
            rarityText.transform.SetParent(buttonPrefab.transform, false);
            TMPro.TextMeshProUGUI rarityTMP = rarityText.AddComponent<TMPro.TextMeshProUGUI>();
            rarityTMP.text = "[Common]";
            rarityTMP.color = Color.white;
            rarityTMP.fontSize = 10;
            rarityTMP.alignment = TMPro.TextAlignmentOptions.Center;
            
            RectTransform rarityTextRect = rarityText.GetComponent<RectTransform>();
            rarityTextRect.anchorMin = new Vector2(0f, 0f);
            rarityTextRect.anchorMax = new Vector2(1f, 0.3f);
            rarityTextRect.offsetMin = Vector2.zero;
            rarityTextRect.offsetMax = Vector2.zero;
            
            return buttonPrefab;
        }

        private void SetAttachmentSelectionUIFields(AttachmentSelectionUI selectionUI, GameObject selectionPanel, Transform optionsContainer, GameObject optionButtonPrefab, UnityEngine.UI.Button skipButton, TMPro.TextMeshProUGUI titleText, TMPro.TextMeshProUGUI instructionText)
        {
            // リフレクションを使用してprivateフィールドを設定
            var selectionPanelField = typeof(AttachmentSelectionUI).GetField("selectionPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectionPanelField?.SetValue(selectionUI, selectionPanel);

            var optionsContainerField = typeof(AttachmentSelectionUI).GetField("optionsContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            optionsContainerField?.SetValue(selectionUI, optionsContainer);

            var optionButtonPrefabField = typeof(AttachmentSelectionUI).GetField("optionButtonPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            optionButtonPrefabField?.SetValue(selectionUI, optionButtonPrefab);

            var skipButtonField = typeof(AttachmentSelectionUI).GetField("skipButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            skipButtonField?.SetValue(selectionUI, skipButton);

            var titleTextField = typeof(AttachmentSelectionUI).GetField("titleText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            titleTextField?.SetValue(selectionUI, titleText);

            var instructionTextField = typeof(AttachmentSelectionUI).GetField("instructionText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            instructionTextField?.SetValue(selectionUI, instructionText);

            Debug.Log("AttachmentSelectionUI fields set via reflection");
        }
        
        private void ShowGameOverScreen()
        {
            Debug.Log("Game Over!");
        }
        
        private Color GetWeaponColor(AttackAttribute attribute)
        {
            switch (attribute)
            {
                case AttackAttribute.Fire: return Color.red;
                case AttackAttribute.Ice: return Color.cyan;
                case AttackAttribute.Thunder: return Color.yellow;
                case AttackAttribute.Wind: return Color.green;
                case AttackAttribute.Earth: return new Color(0.65f, 0.16f, 0.16f); // Brown
                case AttackAttribute.Light: return Color.white;
                case AttackAttribute.Dark: return new Color(0.2f, 0.2f, 0.2f); // Dark gray
                default: return Color.gray;
            }
        }
        
        private string GetAttributeDisplayName(AttackAttribute attribute)
        {
            switch (attribute)
            {
                case AttackAttribute.Fire: return "炎";
                case AttackAttribute.Ice: return "氷";
                case AttackAttribute.Thunder: return "雷";
                case AttackAttribute.Wind: return "風";
                case AttackAttribute.Earth: return "土";
                case AttackAttribute.Light: return "光";
                case AttackAttribute.Dark: return "闇";
                default: return "無";
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (battleManager != null)
            {
                battleManager.OnGameStateChanged -= OnGameStateChanged;
                battleManager.OnTurnChanged -= OnTurnChanged;
                battleManager.OnPlayerDataChanged -= OnPlayerDataChanged;
                battleManager.OnBattleEnded -= OnBattleEnded;
            }
        }
    }
    
    public enum DamageDisplayType
    {
        Normal,
        Critical,
        Combo
    }
}