using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BattleSystem
{
    /// <summary>
    /// インベントリUIシステム（UIモックアップ準拠）
    /// 武器、アタッチメント、スキルツリーの3タブ構成
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("リソース情報")]
        [SerializeField] private int skillPoints = 12;
        [SerializeField] private int gold = 850;

        [Header("現在の選択")]
        [SerializeField] private InventoryTab currentTab = InventoryTab.Weapons;

        // UI要素
        private Canvas mainCanvas;
        private GameObject tabContainer;
        private GameObject characterDisplayArea;
        private GameObject contentArea;
        private Text goldText;
        private Text skillPointsText;

        // タブボタン
        private Dictionary<InventoryTab, Button> tabButtons;
        private Dictionary<InventoryTab, GameObject> tabContents;

        // 武器システム
        private List<WeaponData> equippedWeapons;
        private WeaponData selectedWeapon;

        // アタッチメントシステム
        private List<AttachmentData> attachmentInventory;
        private AttachmentData selectedAttachment;
        private List<ComboProgress> activeCombos;

        // UI状態
        private Dictionary<string, GameObject> weaponButtons;
        private Dictionary<string, GameObject> attachmentButtons;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeData();
            InitializeUI();
            CreateTabSystem();
            CreateCharacterDisplay();
            CreateTabContents();
            SwitchTab(currentTab);
            UpdateResourceDisplay();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// データ初期化
        /// </summary>
        private void InitializeData()
        {
            // 装備中武器（4つ固定）
            equippedWeapons = new List<WeaponData>
            {
                CreateSampleWeapon("ソードブレード", 125, 15, WeaponAttribute.Sword, AttackAttribute.None),
                CreateSampleWeapon("フレイムスロアー", 80, 9, WeaponAttribute.Gun, AttackAttribute.Fire),
                CreateSampleWeapon("サンダーボルト", 90, 10, WeaponAttribute.Magic, AttackAttribute.Thunder),
                CreateSampleWeapon("アイスランス", 85, 8, WeaponAttribute.Spear, AttackAttribute.Ice)
            };

            // アタッチメントインベントリ
            attachmentInventory = new List<AttachmentData>();
            CreateSampleAttachments();

            // アクティブコンボ
            activeCombos = new List<ComboProgress>();
            CreateSampleActiveCombos();

            weaponButtons = new Dictionary<string, GameObject>();
            attachmentButtons = new Dictionary<string, GameObject>();
        }

        /// <summary>
        /// サンプル武器作成
        /// </summary>
        private WeaponData CreateSampleWeapon(string name, int power, int crit, WeaponAttribute weaponAttr, AttackAttribute attackAttr)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponData>();
            weapon.weaponName = name;
            weapon.attackPower = power;
            weapon.criticalRate = crit;
            weapon.weaponAttribute = weaponAttr;
            weapon.attackAttribute = attackAttr;
            weapon.cooldownTime = 0;
            return weapon;
        }

        /// <summary>
        /// サンプルアタッチメント作成
        /// </summary>
        private void CreateSampleAttachments()
        {
            var attachments = new[]
            {
                ("炎の加護", AttachmentRarity.Common, "炎属性武器", "ダメージ+130% + 炎上付与"),
                ("氷の加護", AttachmentRarity.Common, "氷属性武器", "ダメージ+130% + 凍結付与"),
                ("雷の加護", AttachmentRarity.Common, "雷属性武器", "ダメージ+130% + 麻痺付与"),
                ("貫通射撃", AttachmentRarity.Rare, "弓属性武器", "ダメージ+154% + 縦列全体攻撃"),
                ("連射制御", AttachmentRarity.Rare, "銃属性武器", "ダメージ+143% + 2回攻撃"),
                ("守護の誓い", AttachmentRarity.Epic, "盾属性武器", "ダメージ+195% + 被ダメージ-50%"),
                ("魔力増幅", AttachmentRarity.Epic, "魔法属性武器", "ダメージ+225% + 次の武器強化"),
                ("万能調整", AttachmentRarity.Legendary, "道具属性武器", "ダメージ+300% + 全能力値+25%"),
                ("雷撃連打", AttachmentRarity.Common, "雷→斧属性武器", "2回攻撃 + 麻痺付与"),
                ("炎氷融合", AttachmentRarity.Rare, "炎→氷属性武器", "ダメージ+165% + 蒸気爆発"),
                ("光闇螺旋", AttachmentRarity.Epic, "光→闇属性武器", "ダメージ+225% + HP50%回復"),
                ("森羅万象", AttachmentRarity.Legendary, "炎→氷→雷→風→土", "ダメージ+500% + 全能力最大化"),
                ("剣聖の心得", AttachmentRarity.Common, "剣属性武器", "ダメージ+140% + クリティカル率+20%"),
                ("重撃の型", AttachmentRarity.Common, "斧属性武器", "ダメージ+160% + 次ターン攻撃力-20%"),
                ("風雷激流", AttachmentRarity.Rare, "風→雷属性武器", "ダメージ+154% + 連鎖攻撃"),
                ("土炎溶岩", AttachmentRarity.Rare, "土→炎属性武器", "ダメージ+165% + 炎上3ターン"),
                ("光闇輪廻", AttachmentRarity.Legendary, "光→闇→光→闇武器", "ダメージ+400% + 撃破時HP全回復"),
                ("完全兵装", AttachmentRarity.Legendary, "剣→斧→槍→弓→銃", "ダメージ+450% + 5ターン全武器自動使用")
            };

            foreach (var (name, rarity, combo, effect) in attachments)
            {
                var attachment = new AttachmentData
                {
                    id = attachmentInventory.Count + 1,
                    name = name,
                    rarity = rarity,
                    comboRequirement = combo,
                    effect = effect
                };
                attachmentInventory.Add(attachment);
            }
        }

        /// <summary>
        /// サンプルアクティブコンボ作成
        /// </summary>
        private void CreateSampleActiveCombos()
        {
            activeCombos.Add(CreateSampleComboProgress("炎の加護", "ダメージ+130% + 炎上付与", 1, 1));
            activeCombos.Add(CreateSampleComboProgress("雷撃連打", "2回攻撃 + 麻痺付与", 1, 2));
            activeCombos.Add(CreateSampleComboProgress("炎氷爆発", "ダメージ+165% + 凍結付与", 0, 3));
            activeCombos.Add(CreateSampleComboProgress("属性循環", "全体攻撃 + 状態異常無効", 2, 3));
            activeCombos.Add(CreateSampleComboProgress("風雷激流", "ダメージ+154% + 連鎖攻撃", 0, 2));
            activeCombos.Add(CreateSampleComboProgress("光闇螺旋", "ダメージ+225% + HP50%回復", 1, 2));
        }
        
        /// <summary>
        /// サンプルComboProgress作成ヘルパー
        /// </summary>
        private ComboProgress CreateSampleComboProgress(string name, string effect, int currentSteps, int maxSteps)
        {
            var comboData = new ComboData
            {
                comboName = name,
                comboDescription = effect,
                requiredWeaponCount = maxSteps,
                effects = new ComboEffect[]
                {
                    new ComboEffect
                    {
                        effectType = ComboEffectType.DamageMultiplier,
                        effectName = effect,
                        damageMultiplier = 1.3f,
                        effectDescription = effect
                    }
                }
            };
            
            var progress = new ComboProgress
            {
                comboData = comboData,
                usedWeaponIndices = new List<int>(),
                usedAttackAttributes = new List<AttackAttribute>(),
                usedWeaponTypes = new List<WeaponType>()
            };
            
            // 進行状況をシミュレート
            for (int i = 0; i < currentSteps; i++)
            {
                progress.usedWeaponIndices.Add(i);
                progress.usedAttackAttributes.Add(AttackAttribute.Fire);
                progress.usedWeaponTypes.Add(WeaponType.Sword);
            }
            
            return progress;
        }

        /// <summary>
        /// UI初期化
        /// </summary>
        private void InitializeUI()
        {
            // EventSystem確認
            EnsureEventSystem();

            // メインCanvas作成
            CreateMainCanvas();

            // ヘッダー作成
            CreateHeader();

            tabButtons = new Dictionary<InventoryTab, Button>();
            tabContents = new Dictionary<InventoryTab, GameObject>();
        }

        /// <summary>
        /// EventSystem確認・作成
        /// </summary>
        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(eventSystemObj);
            }
        }

        /// <summary>
        /// メインCanvas作成
        /// </summary>
        private void CreateMainCanvas()
        {
            var canvasObj = new GameObject("InventoryCanvas");
            canvasObj.transform.SetParent(transform, false);

            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 1000;

            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 背景作成
            CreateBackground(canvasObj.transform);
        }

        /// <summary>
        /// 背景作成
        /// </summary>
        private void CreateBackground(Transform parent)
        {
            var backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(parent, false);

            var rect = backgroundObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var background = backgroundObj.AddComponent<Image>();
            background.color = new Color(0.05f, 0.07f, 0.1f, 1f); // ダークブルーグレー
        }

        /// <summary>
        /// ヘッダー作成
        /// </summary>
        private void CreateHeader()
        {
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(mainCanvas.transform, false);

            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = new Vector2(1, 1f);
            headerRect.sizeDelta = Vector2.zero;
            headerRect.anchoredPosition = Vector2.zero;

            // ヘッダー背景
            var headerBg = headerObj.AddComponent<Image>();
            headerBg.color = new Color(0.1f, 0.12f, 0.15f, 0.9f);

            // タイトル
            CreateTitle(headerObj.transform);

            // リソース表示
            CreateResourceDisplay(headerObj.transform);
        }

        /// <summary>
        /// タイトル作成
        /// </summary>
        private void CreateTitle(Transform parent)
        {
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0);
            titleRect.anchorMax = new Vector2(0.4f, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "Guardian Equipment";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 32;
            titleText.color = new Color(0.8f, 0.85f, 0.9f, 1f);
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
        }

        /// <summary>
        /// リソース表示作成
        /// </summary>
        private void CreateResourceDisplay(Transform parent)
        {
            // ゴールド表示
            var goldContainer = CreateResourceContainer(parent, "GoldContainer", new Vector2(0.6f, 0.2f), new Vector2(0.75f, 0.8f), new Color(0.2f, 0.4f, 0.2f, 0.8f));
            goldText = CreateResourceText(goldContainer, $"{gold}G", Color.green);

            // スキルポイント表示
            var spContainer = CreateResourceContainer(parent, "SPContainer", new Vector2(0.76f, 0.2f), new Vector2(0.9f, 0.8f), new Color(0.4f, 0.3f, 0.1f, 0.8f));
            skillPointsText = CreateResourceText(spContainer, $"{skillPoints} SP", Color.yellow);
        }

        /// <summary>
        /// リソースコンテナ作成
        /// </summary>
        private Transform CreateResourceContainer(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color backgroundColor)
        {
            var container = new GameObject(name);
            container.transform.SetParent(parent, false);

            var rect = container.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var bg = container.AddComponent<Image>();
            bg.color = backgroundColor;

            return container.transform;
        }

        /// <summary>
        /// リソーステキスト作成
        /// </summary>
        private Text CreateResourceText(Transform parent, string text, Color color)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);

            var rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            var textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = 18;
            textComponent.color = color;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.fontStyle = FontStyle.Bold;

            return textComponent;
        }

        #endregion

        #region Tab System

        /// <summary>
        /// タブシステム作成
        /// </summary>
        private void CreateTabSystem()
        {
            tabContainer = new GameObject("TabContainer");
            tabContainer.transform.SetParent(mainCanvas.transform, false);

            var tabRect = tabContainer.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0.02f, 0.15f);
            tabRect.anchorMax = new Vector2(0.25f, 0.85f);
            tabRect.sizeDelta = Vector2.zero;
            tabRect.anchoredPosition = Vector2.zero;

            // 縦方向レイアウト
            var layoutGroup = tabContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            // 各タブボタン作成
            CreateTabButton(InventoryTab.Skills, "スキルツリー", "★");
            CreateTabButton(InventoryTab.Weapons, "武器一覧", "⚔");
            CreateTabButton(InventoryTab.Attachments, "アタッチメント", "⚙");
        }

        /// <summary>
        /// タブボタン作成
        /// </summary>
        private void CreateTabButton(InventoryTab tab, string text, string icon)
        {
            var buttonObj = new GameObject($"TabButton_{tab}");
            buttonObj.transform.SetParent(tabContainer.transform, false);

            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 80;
            layoutElement.minHeight = 80;

            var button = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();
            button.targetGraphic = image;

            // ボタンテキスト作成
            var textObj = new GameObject("TabText");
            textObj.transform.SetParent(buttonObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = $"{icon} {text}";
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 18;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontStyle = FontStyle.Bold;

            // ボタンイベント設定
            button.onClick.AddListener(() => SwitchTab(tab));

            tabButtons[tab] = button;
            UpdateTabButtonAppearance(tab, currentTab == tab);
        }

        /// <summary>
        /// タブボタンの外観更新
        /// </summary>
        private void UpdateTabButtonAppearance(InventoryTab tab, bool isActive)
        {
            var button = tabButtons[tab];
            var image = button.GetComponent<Image>();
            var text = button.GetComponentInChildren<Text>();

            if (isActive)
            {
                image.color = new Color(0.2f, 0.4f, 0.8f, 0.8f); // 青色
                text.color = new Color(0.8f, 0.9f, 1f, 1f);
            }
            else
            {
                image.color = new Color(0.3f, 0.3f, 0.3f, 0.8f); // グレー
                text.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }
        }

        /// <summary>
        /// タブ切り替え
        /// </summary>
        private void SwitchTab(InventoryTab newTab)
        {
            // 前のタブを非アクティブに
            UpdateTabButtonAppearance(currentTab, false);
            if (tabContents.ContainsKey(currentTab))
                tabContents[currentTab].SetActive(false);

            // 新しいタブをアクティブに
            currentTab = newTab;
            UpdateTabButtonAppearance(currentTab, true);
            if (tabContents.ContainsKey(currentTab))
                tabContents[currentTab].SetActive(true);

            Debug.Log($"Switched to tab: {newTab}");
        }

        #endregion

        #region Character Display

        /// <summary>
        /// キャラクター表示エリア作成
        /// </summary>
        private void CreateCharacterDisplay()
        {
            characterDisplayArea = new GameObject("CharacterDisplay");
            characterDisplayArea.transform.SetParent(mainCanvas.transform, false);

            var charRect = characterDisplayArea.AddComponent<RectTransform>();
            charRect.anchorMin = new Vector2(0.27f, 0.5f);
            charRect.anchorMax = new Vector2(0.55f, 0.85f);
            charRect.sizeDelta = Vector2.zero;
            charRect.anchoredPosition = Vector2.zero;

            // 背景
            var bg = characterDisplayArea.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.5f);

            // プレースホルダーテキスト
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(characterDisplayArea.transform, false);

            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;

            var placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = "Guardian Model\n3D表示エリア";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 18;
            placeholderText.color = Color.gray;
            placeholderText.alignment = TextAnchor.MiddleCenter;
        }

        #endregion

        #region Tab Contents

        /// <summary>
        /// タブコンテンツ作成
        /// </summary>
        private void CreateTabContents()
        {
            CreateWeaponsContent();
            CreateAttachmentsContent();
            CreateSkillsContent();
        }

        /// <summary>
        /// 武器タブコンテンツ作成
        /// </summary>
        private void CreateWeaponsContent()
        {
            var weaponsContent = new GameObject("WeaponsContent");
            weaponsContent.transform.SetParent(mainCanvas.transform, false);

            var contentRect = weaponsContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.57f, 0.15f);
            contentRect.anchorMax = new Vector2(0.98f, 0.85f);
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;

            // 装備中武器ステータス
            CreateEquippedWeaponsDisplay(weaponsContent.transform);

            // 武器詳細情報
            CreateWeaponDetailDisplay(weaponsContent.transform);

            // 武器管理説明
            CreateWeaponManagementInfo(weaponsContent.transform);

            tabContents[InventoryTab.Weapons] = weaponsContent;
        }

        /// <summary>
        /// 装備中武器表示作成
        /// </summary>
        private void CreateEquippedWeaponsDisplay(Transform parent)
        {
            var displayObj = new GameObject("EquippedWeaponsDisplay");
            displayObj.transform.SetParent(parent, false);

            var displayRect = displayObj.AddComponent<RectTransform>();
            displayRect.anchorMin = new Vector2(0, 0.6f);
            displayRect.anchorMax = new Vector2(1, 1f);
            displayRect.sizeDelta = Vector2.zero;
            displayRect.anchoredPosition = Vector2.zero;

            // 背景
            var bg = displayObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.8f);

            // タイトル
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(displayObj.transform, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.8f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "装備中武器 (4/4)";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;

            // 武器グリッド
            var gridObj = new GameObject("WeaponGrid");
            gridObj.transform.SetParent(displayObj.transform, false);

            var gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.05f, 0.1f);
            gridRect.anchorMax = new Vector2(0.95f, 0.75f);
            gridRect.sizeDelta = Vector2.zero;
            gridRect.anchoredPosition = Vector2.zero;

            var gridLayout = gridObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(180, 80);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;

            // 各武器ボタン作成
            for (int i = 0; i < equippedWeapons.Count; i++)
            {
                CreateWeaponButton(equippedWeapons[i], gridObj.transform);
            }
        }

        /// <summary>
        /// 武器ボタン作成
        /// </summary>
        private void CreateWeaponButton(WeaponData weapon, Transform parent)
        {
            var buttonObj = new GameObject($"WeaponButton_{weapon.weaponName}");
            buttonObj.transform.SetParent(parent, false);

            var button = buttonObj.AddComponent<Button>();
            var image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
            button.targetGraphic = image;

            // 武器アイコン（簡易実装）
            var iconObj = new GameObject("WeaponIcon");
            iconObj.transform.SetParent(buttonObj.transform, false);

            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.05f, 0.2f);
            iconRect.anchorMax = new Vector2(0.35f, 0.8f);
            iconRect.sizeDelta = Vector2.zero;
            iconRect.anchoredPosition = Vector2.zero;

            var iconImage = iconObj.AddComponent<Image>();
            iconImage.color = GetWeaponColor(weapon.weaponType);

            // 武器情報テキスト
            var infoObj = new GameObject("WeaponInfo");
            infoObj.transform.SetParent(buttonObj.transform, false);

            var infoRect = infoObj.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.4f, 0);
            infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.sizeDelta = Vector2.zero;
            infoRect.anchoredPosition = Vector2.zero;

            var infoText = infoObj.AddComponent<Text>();
            infoText.text = $"{weapon.basePower}\n{weapon.criticalRate}%";
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 14;
            infoText.color = Color.white;
            infoText.alignment = TextAnchor.MiddleLeft;

            // ボタンイベント
            button.onClick.AddListener(() => OnWeaponSelected(weapon));

            weaponButtons[weapon.weaponName] = buttonObj;
        }

        /// <summary>
        /// 武器タイプ別色取得
        /// </summary>
        private Color GetWeaponColor(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword: return Color.white;
                case WeaponType.Axe: return Color.gray;
                case WeaponType.Spear: return Color.cyan;
                case WeaponType.Bow: return Color.green;
                case WeaponType.Gun: return Color.yellow;
                case WeaponType.Shield: return Color.blue;
                case WeaponType.Magic: return Color.magenta;
                case WeaponType.Tool: return new Color(0.8f, 0.6f, 0.2f);
                default: return Color.white;
            }
        }

        /// <summary>
        /// 武器詳細表示作成
        /// </summary>
        private void CreateWeaponDetailDisplay(Transform parent)
        {
            var detailObj = new GameObject("WeaponDetailDisplay");
            detailObj.transform.SetParent(parent, false);

            var detailRect = detailObj.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0, 0.3f);
            detailRect.anchorMax = new Vector2(1, 0.55f);
            detailRect.sizeDelta = Vector2.zero;
            detailRect.anchoredPosition = Vector2.zero;

            // 背景
            var bg = detailObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.8f);

            // プレースホルダー
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(detailObj.transform, false);

            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;

            var placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = "武器を選択してください";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 16;
            placeholderText.color = Color.gray;
            placeholderText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 武器管理情報作成
        /// </summary>
        private void CreateWeaponManagementInfo(Transform parent)
        {
            var infoObj = new GameObject("WeaponManagementInfo");
            infoObj.transform.SetParent(parent, false);

            var infoRect = infoObj.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 0);
            infoRect.anchorMax = new Vector2(1, 0.25f);
            infoRect.sizeDelta = Vector2.zero;
            infoRect.anchoredPosition = Vector2.zero;

            // 背景
            var bg = infoObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.8f);

            // 情報テキスト
            var textObj = new GameObject("InfoText");
            textObj.transform.SetParent(infoObj.transform, false);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            var infoText = textObj.AddComponent<Text>();
            infoText.text = "武器管理システム\n常に4つの武器を装備。新しい武器入手時のみ入れ替えが可能です。";
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 14;
            infoText.color = Color.white;
            infoText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// アタッチメントタブコンテンツ作成
        /// </summary>
        private void CreateAttachmentsContent()
        {
            var attachmentsContent = new GameObject("AttachmentsContent");
            attachmentsContent.transform.SetParent(mainCanvas.transform, false);

            var contentRect = attachmentsContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.57f, 0.15f);
            contentRect.anchorMax = new Vector2(0.98f, 0.85f);
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;

            // アクティブコンボ情報
            CreateActiveComboDisplay(attachmentsContent.transform);

            // アタッチメント詳細情報
            CreateAttachmentDetailDisplay(attachmentsContent.transform);

            // アタッチメントインベントリ
            CreateAttachmentInventory(attachmentsContent.transform);

            tabContents[InventoryTab.Attachments] = attachmentsContent;
        }

        /// <summary>
        /// アクティブコンボ表示作成
        /// </summary>
        private void CreateActiveComboDisplay(Transform parent)
        {
            var comboObj = new GameObject("ActiveComboDisplay");
            comboObj.transform.SetParent(parent, false);

            var comboRect = comboObj.AddComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0, 0.5f);
            comboRect.anchorMax = new Vector2(1, 1f);
            comboRect.sizeDelta = Vector2.zero;
            comboRect.anchoredPosition = Vector2.zero;

            // 背景
            var bg = comboObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.8f);

            // タイトル
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(comboObj.transform, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.text = "アクティブコンボ";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;

            // スクロールエリア
            CreateComboScrollArea(comboObj.transform);
        }

        /// <summary>
        /// コンボスクロールエリア作成
        /// </summary>
        private void CreateComboScrollArea(Transform parent)
        {
            var scrollObj = new GameObject("ComboScrollArea");
            scrollObj.transform.SetParent(parent, false);

            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.05f);
            scrollRect.anchorMax = new Vector2(0.95f, 0.8f);
            scrollRect.sizeDelta = Vector2.zero;
            scrollRect.anchoredPosition = Vector2.zero;

            // ScrollRect設定
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true;
            scroll.horizontal = false;

            // Content作成
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            contentRect.anchoredPosition = Vector2.zero;

            var layoutGroup = contentObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;

            var sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;

            // 各コンボアイテム作成
            foreach (var combo in activeCombos)
            {
                CreateComboItem(combo, contentObj.transform);
            }
        }

        /// <summary>
        /// コンボアイテム作成
        /// </summary>
        private void CreateComboItem(ComboProgress combo, Transform parent)
        {
            var itemObj = new GameObject($"ComboItem_{combo.comboData.comboName}");
            itemObj.transform.SetParent(parent, false);

            var layoutElement = itemObj.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 80;
            layoutElement.minHeight = 80;

            // 背景
            var bg = itemObj.AddComponent<Image>();
            bg.color = new Color(0.4f, 0.2f, 0.6f, 0.3f); // 紫色

            // コンボ名
            var nameObj = new GameObject("ComboName");
            nameObj.transform.SetParent(itemObj.transform, false);

            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.6f);
            nameRect.anchorMax = new Vector2(0.95f, 0.9f);
            nameRect.sizeDelta = Vector2.zero;
            nameRect.anchoredPosition = Vector2.zero;

            var nameText = nameObj.AddComponent<Text>();
            nameText.text = combo.comboData.comboName;
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 14;
            nameText.color = new Color(0.8f, 0.6f, 1f);
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.fontStyle = FontStyle.Bold;

            // 効果テキスト
            var effectObj = new GameObject("ComboEffect");
            effectObj.transform.SetParent(itemObj.transform, false);

            var effectRect = effectObj.AddComponent<RectTransform>();
            effectRect.anchorMin = new Vector2(0.05f, 0.35f);
            effectRect.anchorMax = new Vector2(0.95f, 0.55f);
            effectRect.sizeDelta = Vector2.zero;
            effectRect.anchoredPosition = Vector2.zero;

            var effectText = effectObj.AddComponent<Text>();
            effectText.text = combo.comboData.comboDescription;
            effectText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            effectText.fontSize = 10;
            effectText.color = new Color(0.7f, 0.5f, 0.9f);
            effectText.alignment = TextAnchor.MiddleLeft;

            // 進行バー
            CreateProgressBar(combo, itemObj.transform);
        }

        /// <summary>
        /// 進行バー作成
        /// </summary>
        private void CreateProgressBar(ComboProgress combo, Transform parent)
        {
            var barBgObj = new GameObject("ProgressBarBG");
            barBgObj.transform.SetParent(parent, false);

            var barBgRect = barBgObj.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.05f, 0.1f);
            barBgRect.anchorMax = new Vector2(0.75f, 0.25f);
            barBgRect.sizeDelta = Vector2.zero;
            barBgRect.anchoredPosition = Vector2.zero;

            var barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = new Color(0.2f, 0.1f, 0.3f);

            // 進行部分
            var barFillObj = new GameObject("ProgressBarFill");
            barFillObj.transform.SetParent(barBgObj.transform, false);

            var barFillRect = barFillObj.AddComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            int currentProgress = combo.usedWeaponIndices.Count;
            int maxSteps = combo.comboData.requiredWeaponCount;
            float fillRatio = maxSteps > 0 ? (float)currentProgress / maxSteps : 0f;
            barFillRect.anchorMax = new Vector2(fillRatio, 1f);
            barFillRect.sizeDelta = Vector2.zero;
            barFillRect.anchoredPosition = Vector2.zero;

            var barFillImage = barFillObj.AddComponent<Image>();
            barFillImage.color = new Color(0.6f, 0.4f, 0.8f);

            // 進行テキスト
            var progressTextObj = new GameObject("ProgressText");
            progressTextObj.transform.SetParent(parent, false);

            var progressTextRect = progressTextObj.AddComponent<RectTransform>();
            progressTextRect.anchorMin = new Vector2(0.8f, 0.05f);
            progressTextRect.anchorMax = new Vector2(0.95f, 0.3f);
            progressTextRect.sizeDelta = Vector2.zero;
            progressTextRect.anchoredPosition = Vector2.zero;

            var progressText = progressTextObj.AddComponent<Text>();
            progressText.text = $"{currentProgress}/{maxSteps}";
            progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            progressText.fontSize = 10;
            progressText.color = new Color(0.7f, 0.5f, 0.9f);
            progressText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// アタッチメント詳細表示作成
        /// </summary>
        private void CreateAttachmentDetailDisplay(Transform parent)
        {
            var detailObj = new GameObject("AttachmentDetailDisplay");
            detailObj.transform.SetParent(parent, false);

            var detailRect = detailObj.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0, 0.25f);
            detailRect.anchorMax = new Vector2(1, 0.45f);
            detailRect.sizeDelta = Vector2.zero;
            detailRect.anchoredPosition = Vector2.zero;

            // 背景
            var bg = detailObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.8f);

            // プレースホルダー
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(detailObj.transform, false);

            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;

            var placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = "アタッチメントを選択してください";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 16;
            placeholderText.color = Color.gray;
            placeholderText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// アタッチメントインベントリ作成
        /// </summary>
        private void CreateAttachmentInventory(Transform parent)
        {
            var inventoryObj = new GameObject("AttachmentInventory");
            inventoryObj.transform.SetParent(parent, false);

            var inventoryRect = inventoryObj.AddComponent<RectTransform>();
            inventoryRect.anchorMin = new Vector2(0, 0);
            inventoryRect.anchorMax = new Vector2(1, 0.2f);
            inventoryRect.sizeDelta = Vector2.zero;
            inventoryRect.anchoredPosition = Vector2.zero;

            // 背景
            var bg = inventoryObj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.8f);

            // タイトル
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(inventoryObj.transform, false);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.7f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.sizeDelta = Vector2.zero;
            titleRect.anchoredPosition = Vector2.zero;

            var titleText = titleObj.AddComponent<Text>();
            titleText.text = $"アタッチメントインベントリ ({attachmentInventory.Count}個所持)";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 16;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.fontStyle = FontStyle.Bold;

            // スクロールビュー作成
            CreateAttachmentScrollView(inventoryObj.transform);
        }

        /// <summary>
        /// アタッチメントスクロールビュー作成
        /// </summary>
        private void CreateAttachmentScrollView(Transform parent)
        {
            var scrollObj = new GameObject("AttachmentScrollView");
            scrollObj.transform.SetParent(parent, false);

            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.02f, 0.1f);
            scrollRect.anchorMax = new Vector2(0.98f, 0.65f);
            scrollRect.sizeDelta = Vector2.zero;
            scrollRect.anchoredPosition = Vector2.zero;

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;

            // Content作成
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(scrollObj.transform, false);

            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.sizeDelta = new Vector2(attachmentInventory.Count * 70, 0);
            contentRect.anchoredPosition = Vector2.zero;

            var layoutGroup = contentObj.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 5f;
            layoutGroup.padding = new RectOffset(5, 5, 5, 5);
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlHeight = true;
            layoutGroup.childControlWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = false;

            scroll.content = contentRect;

            // 各アタッチメントアイテム作成
            foreach (var attachment in attachmentInventory)
            {
                CreateAttachmentItem(attachment, contentObj.transform);
            }
        }

        /// <summary>
        /// アタッチメントアイテム作成
        /// </summary>
        private void CreateAttachmentItem(AttachmentData attachment, Transform parent)
        {
            var itemObj = new GameObject($"AttachmentItem_{attachment.attachmentId}");
            itemObj.transform.SetParent(parent, false);

            var layoutElement = itemObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 60;
            layoutElement.minWidth = 60;

            var button = itemObj.AddComponent<Button>();
            var image = itemObj.AddComponent<Image>();
            image.color = GetRarityColor(attachment.rarity);
            button.targetGraphic = image;

            // アタッチメントアイコン（簡易実装）
            var iconObj = new GameObject("AttachmentIcon");
            iconObj.transform.SetParent(itemObj.transform, false);

            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.sizeDelta = Vector2.zero;
            iconRect.anchoredPosition = Vector2.zero;

            var iconText = iconObj.AddComponent<Text>();
            iconText.text = GetRarityIcon(attachment.rarity);
            iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            iconText.fontSize = 16;
            iconText.color = Color.white;
            iconText.alignment = TextAnchor.MiddleCenter;
            iconText.fontStyle = FontStyle.Bold;

            // ボタンイベント
            button.onClick.AddListener(() => OnAttachmentSelected(attachment));

            attachmentButtons[attachment.attachmentName] = itemObj;
        }

        /// <summary>
        /// レアリティ別色取得
        /// </summary>
        private Color GetRarityColor(AttachmentRarity rarity)
        {
            switch (rarity)
            {
                case AttachmentRarity.Common: return new Color(0.5f, 0.5f, 0.5f, 0.8f);
                case AttachmentRarity.Rare: return new Color(0.2f, 0.4f, 0.8f, 0.8f);
                case AttachmentRarity.Epic: return new Color(0.6f, 0.2f, 0.8f, 0.8f);
                case AttachmentRarity.Legendary: return new Color(0.8f, 0.6f, 0.2f, 0.8f);
                default: return Color.gray;
            }
        }

        /// <summary>
        /// レアリティ別アイコン取得
        /// </summary>
        private string GetRarityIcon(AttachmentRarity rarity)
        {
            switch (rarity)
            {
                case AttachmentRarity.Common: return "○";
                case AttachmentRarity.Rare: return "◆";
                case AttachmentRarity.Epic: return "★";
                case AttachmentRarity.Legendary: return "♔";
                default: return "?";
            }
        }

        /// <summary>
        /// スキルタブコンテンツ作成
        /// </summary>
        private void CreateSkillsContent()
        {
            var skillsContent = new GameObject("SkillsContent");
            skillsContent.transform.SetParent(mainCanvas.transform, false);

            var contentRect = skillsContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.57f, 0.15f);
            contentRect.anchorMax = new Vector2(0.98f, 0.85f);
            contentRect.sizeDelta = Vector2.zero;
            contentRect.anchoredPosition = Vector2.zero;

            // 背景
            var bg = skillsContent.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.15f, 0.5f);

            // プレースホルダー
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(skillsContent.transform, false);

            var placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;

            var placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.text = "★ スキルツリー画面\n既存のスキルツリーUIをここに表示";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 18;
            placeholderText.color = Color.gray;
            placeholderText.alignment = TextAnchor.MiddleCenter;

            tabContents[InventoryTab.Skills] = skillsContent;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 武器選択時の処理
        /// </summary>
        private void OnWeaponSelected(WeaponData weapon)
        {
            selectedWeapon = weapon;
            Debug.Log($"Selected weapon: {weapon.weaponName}");
            // 武器詳細UI更新（実装省略）
        }

        /// <summary>
        /// アタッチメント選択時の処理
        /// </summary>
        private void OnAttachmentSelected(AttachmentData attachment)
        {
            selectedAttachment = attachment;
            Debug.Log($"Selected attachment: {attachment.attachmentName}");
            // アタッチメント詳細UI更新（実装省略）
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// リソース表示更新
        /// </summary>
        private void UpdateResourceDisplay()
        {
            if (goldText != null)
                goldText.text = $"{gold}G";
            
            if (skillPointsText != null)
                skillPointsText.text = $"{skillPoints} SP";
        }

        #endregion

        #region Public Methods (for Demo)

        /// <summary>
        /// 外部からタブ切り替え（デモ用）
        /// </summary>
        public void SwitchToTab(InventoryTab tab)
        {
            currentTab = tab;
            Debug.Log($"Tab switched to: {tab}");
            
            // タブボタンの表示状態更新
            foreach (var kvp in tabButtons)
            {
                var button = kvp.Value;
                var isSelected = kvp.Key == currentTab;
                
                var colors = button.colors;
                colors.normalColor = isSelected ? new Color(0.3f, 0.6f, 1.0f, 0.8f) : new Color(0.2f, 0.3f, 0.4f, 0.8f);
                button.colors = colors;
            }
            
            // コンテンツの表示/非表示
            foreach (var kvp in tabContents)
            {
                kvp.Value.SetActive(kvp.Key == currentTab);
            }
        }

        /// <summary>
        /// 現在のタブ取得
        /// </summary>
        public InventoryTab GetCurrentTab()
        {
            return currentTab;
        }

        /// <summary>
        /// リソース更新（デモ用）
        /// </summary>
        public void UpdateResources(int newGold, int newSkillPoints)
        {
            gold = newGold;
            skillPoints = newSkillPoints;
            UpdateResourceDisplay();
            Debug.Log($"Resources updated: {gold}G, {skillPoints}SP");
        }

        /// <summary>
        /// 武器追加（デモ用）
        /// </summary>
        public void AddTestWeapon(WeaponData weapon)
        {
            if (equippedWeapons.Count < 4)
            {
                equippedWeapons.Add(weapon);
                Debug.Log($"Test weapon added: {weapon.weaponName}");
                
                // 武器タブが現在表示されている場合は更新
                if (currentTab == InventoryTab.Weapons)
                {
                    RefreshWeaponsTab();
                }
            }
        }

        /// <summary>
        /// アタッチメント追加（デモ用）
        /// </summary>
        public void AddTestAttachment(AttachmentData attachment)
        {
            attachmentInventory.Add(attachment);
            Debug.Log($"Test attachment added: {attachment.name}");
            
            // アタッチメントタブが現在表示されている場合は更新
            if (currentTab == InventoryTab.Attachments)
            {
                RefreshAttachmentsTab();
            }
        }

        /// <summary>
        /// インベントリリセット（デモ用）
        /// </summary>
        public void ResetInventory()
        {
            equippedWeapons.Clear();
            attachmentInventory.Clear();
            activeCombos.Clear();
            selectedWeapon = null;
            selectedAttachment = null;
            
            Debug.Log("Inventory reset");
            
            // 現在のタブを再描画
            RefreshCurrentTab();
        }

        /// <summary>
        /// 武器タブ再描画
        /// </summary>
        private void RefreshWeaponsTab()
        {
            if (tabContents.ContainsKey(InventoryTab.Weapons))
            {
                Destroy(tabContents[InventoryTab.Weapons]);
                CreateWeaponsContent();
                SwitchToTab(InventoryTab.Weapons);
            }
        }

        /// <summary>
        /// アタッチメントタブ再描画
        /// </summary>
        private void RefreshAttachmentsTab()
        {
            if (tabContents.ContainsKey(InventoryTab.Attachments))
            {
                Destroy(tabContents[InventoryTab.Attachments]);
                CreateAttachmentsContent();
                SwitchToTab(InventoryTab.Attachments);
            }
        }

        /// <summary>
        /// 現在のタブ再描画
        /// </summary>
        private void RefreshCurrentTab()
        {
            switch (currentTab)
            {
                case InventoryTab.Weapons:
                    RefreshWeaponsTab();
                    break;
                case InventoryTab.Attachments:
                    RefreshAttachmentsTab();
                    break;
                case InventoryTab.Skills:
                    // スキルタブは再描画不要（静的コンテンツ）
                    break;
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// インベントリタブ列挙
    /// </summary>
    public enum InventoryTab
    {
        Skills,
        Weapons,
        Attachments
    }

    // 既存のAttachmentSystemのAttachmentDataとAttachmentRarity、ComboSystemのComboProgressを使用

    #endregion
}