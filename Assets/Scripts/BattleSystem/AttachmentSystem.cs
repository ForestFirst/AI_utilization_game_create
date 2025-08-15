using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    // アタッチメントのレアリティ
    public enum AttachmentRarity
    {
        Common = 1,     // コモン（灰色）
        Rare = 2,       // レア（青色）
        Epic = 3,       // エピック（紫色）
        Legendary = 4   // レジェンダリー（金色）
    }

    // アタッチメントカテゴリ
    public enum AttachmentCategory
    {
        Attack,     // 攻撃系
        Defense,    // 防御系
        Combo,      // コンボ系
        Utility     // ユーティリティ系
    }

    // アタッチメント効果の種類
    public enum AttachmentEffectType
    {
        // 攻撃系
        AttackPowerBoost,       // 攻撃力増加
        CriticalRateBoost,      // クリティカル率増加
        CriticalDamageBoost,    // クリティカル倍率増加
        WeaponPowerBoost,       // 武器攻撃力増加

        // 防御系
        MaxHpBoost,             // 最大HP増加
        DamageReduction,        // 被ダメージ軽減
        CounterDamageBoost,     // カウンターダメージ増加
        ShieldReflection,       // 盾反射率増加

        // コンボ系
        ComboDamageBoost,       // コンボダメージ倍率増加
        ComboActionGrant,       // コンボ成功時追加行動
        ComboSimplification,    // コンボ必要数減少
        ComboAutoTrigger,       // 常時発動コンボ

        // ユーティリティ系
        CooldownReduction,      // クールダウン短縮
        TurnStartHeal,          // ターン開始時HP回復
        ConditionalPowerBoost,  // 条件付き攻撃力増加
        SpecialAbility          // 特殊能力
    }

    // アタッチメント効果データ
    [Serializable]
    public class AttachmentEffect
    {
        public AttachmentEffectType effectType;
        public float effectValue;           // 効果値（%の場合は0.1=10%）
        public int flatValue;               // 固定値効果
        public string conditionDescription; // 発動条件説明
        public bool isPercentage;           // パーセント効果かどうか
        public bool stackable;              // スタック可能かどうか
    }

    // アタッチメントデータ
    [Serializable]
    public class AttachmentData
    {
        public int attachmentId;
        public string attachmentName;
        public AttachmentRarity rarity;
        public AttachmentCategory category;
        public AttachmentEffect[] effects;
        public string description;
        public string flavorText;           // フレーバーテキスト
        public bool isUnique;               // ユニーク（1つまで装着可能）
        public Sprite attachmentIcon;
        public string associatedComboName;  // 対応するコンボ名
        
        // レアリティに応じた効果強化
        public float GetRarityMultiplier()
        {
            switch (rarity)
            {
                case AttachmentRarity.Common: return 1.0f;
                case AttachmentRarity.Rare: return 1.3f;
                case AttachmentRarity.Epic: return 1.6f;
                case AttachmentRarity.Legendary: return 2.0f;
                default: return 1.0f;
            }
        }
    }

    // アタッチメント装着状況
    [Serializable]
    public class AttachmentSlot
    {
        public AttachmentData attachedData;
        public int acquisitionTurn;         // 取得ターン
        public int enhancementLevel;        // 強化レベル
        public bool isActive;               // アクティブ状態
        
        public bool IsEmpty => attachedData == null;
        
        public void AttachAttachment(AttachmentData attachment)
        {
            attachedData = attachment;
            isActive = true;
            enhancementLevel = 0;
        }
        
        public void DetachAttachment()
        {
            attachedData = null;
            isActive = false;
            enhancementLevel = 0;
        }
    }

    // アタッチメント選択オプション
    public struct AttachmentOption
    {
        public AttachmentData attachment;
        public float selectionWeight;
        public string selectionReason;
    }


    // アタッチメント管理システム
    public class AttachmentSystem : MonoBehaviour
    {
        [Header("アタッチメント設定")]
        [SerializeField] private AttachmentDatabase attachmentDatabase;
        [SerializeField] private int initialAttachmentSlots = 10;  // 初期スロット数（動的拡張可能）
        [SerializeField] private int selectionOptionsCount = 4; // 選択肢数
        [SerializeField] private bool allowDuplicates = true;    // 重複許可（PlayMode開始時の自動装備のため）
        [SerializeField] private bool allowUnlimitedSlots = true; // 無制限スロット許可
        
        [Header("装備武器設定")]
        [SerializeField] private WeaponDatabase weaponDatabase;
        [SerializeField] private int maxEquippedWeapons = 5;  // 最大装備武器数（手札の枚数）
        [SerializeField] private bool autoEquipWeaponsOnStart = true;  // PlayMode開始時の自動武器装備
        
        [Header("強化設定")]
        [SerializeField] private bool allowEnhancement = true;
        [SerializeField] private int maxEnhancementLevel = 5;

        private BattleManager battleManager;
        private List<AttachmentSlot> attachmentSlots;
        private List<AttachmentData> availableAttachments;
        
        // 装備武器管理
        private List<WeaponData> equippedWeapons;
        private List<CardData> weaponCards;  // 武器カード（ランダム列割り振り済み）

        // イベント定義
        public event Action<AttachmentData[]> OnAttachmentOptionsPresented;
        public event Action<AttachmentData> OnAttachmentSelected;
        public event Action<AttachmentData, int> OnAttachmentEnhanced;
        public event Action<AttachmentData> OnAttachmentRemoved;
        public event Action<List<AttachmentData>> OnPlayModeAttachmentsDisplayRequested;
        
        // 武器カード関連イベント
        public event Action<List<CardData>> OnWeaponCardsGenerated;
        public event Action<WeaponData> OnWeaponEquipped;
        public event Action<WeaponData> OnWeaponUnequipped;

        // プロパティ
        public List<AttachmentSlot> AttachmentSlots => attachmentSlots;
        public AttachmentDatabase Database => attachmentDatabase;
        public List<WeaponData> EquippedWeapons => equippedWeapons;
        public List<CardData> WeaponCards => weaponCards;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            attachmentSlots = new List<AttachmentSlot>();
            availableAttachments = new List<AttachmentData>();
            equippedWeapons = new List<WeaponData>();
            weaponCards = new List<CardData>();
            
            InitializeAttachmentSlots();
        }

        private void Start()
        {
            if (attachmentDatabase != null)
            {
                availableAttachments.AddRange(attachmentDatabase.PresetAttachments);
            }
            else
            {
                // AttachmentDatabaseが設定されていない場合、動的に作成
                CreateDefaultAttachmentDatabase();
            }
            
            // PlayMode開始時に3つのランダムアタッチメントを装備
            EquipRandomAttachmentsOnStart(3);
            
            // PlayMode開始時に武器を自動装備
            if (autoEquipWeaponsOnStart)
            {
                EquipRandomWeaponsOnStart(maxEquippedWeapons);
            }
            
            // PlayMode開始時にアタッチメント情報を表示
            DisplayEquippedAttachmentsOnPlayModeStart();
            
            // HandSystemの状態を確実に初期化
            InitializeHandSystemForPlay();
        }

        // デフォルトのAttachmentDatabaseを動的作成
        private void CreateDefaultAttachmentDatabase()
        {
            Debug.Log("Creating default AttachmentDatabase...");
            
            attachmentDatabase = ScriptableObject.CreateInstance<AttachmentDatabase>();
            attachmentDatabase.hideFlags = HideFlags.DontSaveInEditor;
            
            // 手動初期化を実行
            attachmentDatabase.ForceInitialize();
            
            // 利用可能なアタッチメントを追加
            if (attachmentDatabase.PresetAttachments != null)
            {
                availableAttachments.AddRange(attachmentDatabase.PresetAttachments);
                Debug.Log($"Default AttachmentDatabase created with {attachmentDatabase.PresetAttachments.Length} attachments");
            }
        }

        // アタッチメントスロット初期化
        private void InitializeAttachmentSlots()
        {
            for (int i = 0; i < initialAttachmentSlots; i++)
            {
                attachmentSlots.Add(new AttachmentSlot());
            }
        }
        
        // 動的スロット拡張
        private void ExpandSlotsIfNeeded()
        {
            if (!allowUnlimitedSlots) return;
            
            // 空きスロットがない場合、新しいスロットを追加
            if (!attachmentSlots.Any(slot => slot.IsEmpty))
            {
                int slotsToAdd = 5; // 一度に5個追加
                for (int i = 0; i < slotsToAdd; i++)
                {
                    attachmentSlots.Add(new AttachmentSlot());
                }
                Debug.Log($"📈 アタッチメントスロットを{slotsToAdd}個追加。総スロット数: {attachmentSlots.Count}");
            }
        }

        // アタッチメント選択肢生成
        public AttachmentData[] GenerateAttachmentOptions()
        {
            List<AttachmentData> options = new List<AttachmentData>();
            List<AttachmentData> candidatePool = new List<AttachmentData>(availableAttachments);

            // 既に装着済みのアタッチメントを除外（重複不許可の場合）
            if (!allowDuplicates)
            {
                foreach (AttachmentSlot slot in attachmentSlots)
                {
                    if (!slot.IsEmpty)
                    {
                        candidatePool.RemoveAll(a => a.attachmentId == slot.attachedData.attachmentId);
                    }
                }
            }

            // 選択肢数分だけランダム選択
            for (int i = 0; i < selectionOptionsCount && candidatePool.Count > 0; i++)
            {
                AttachmentRarity randomRarity = attachmentDatabase.GetRandomRarity();
                AttachmentData[] rarityPool = candidatePool.Where(a => a.rarity == randomRarity).ToArray();
                
                if (rarityPool.Length == 0)
                {
                    // 該当レアリティがない場合、全体からランダム選択
                    rarityPool = candidatePool.ToArray();
                }

                if (rarityPool.Length > 0)
                {
                    AttachmentData selected = rarityPool[UnityEngine.Random.Range(0, rarityPool.Length)];
                    options.Add(selected);
                    candidatePool.Remove(selected);
                }
            }

            OnAttachmentOptionsPresented?.Invoke(options.ToArray());
            return options.ToArray();
        }

        // アタッチメント装着
        public bool AttachAttachment(AttachmentData attachment)
        {
            if (attachment == null)
                return false;

            // 動的スロット拡張チェック
            ExpandSlotsIfNeeded();

            // 空きスロットを探す
            AttachmentSlot emptySlot = attachmentSlots.FirstOrDefault(slot => slot.IsEmpty);
            if (emptySlot == null)
            {
                // まだ空きがない場合は強制的にスロットを追加
                if (allowUnlimitedSlots)
                {
                    attachmentSlots.Add(new AttachmentSlot());
                    emptySlot = attachmentSlots.Last();
                    Debug.Log($"📈 新しいスロットを追加してアタッチメントを装備。総スロット数: {attachmentSlots.Count}");
                }
                else
                {
                    // 無制限許可されていない場合は最初のスロットを上書き
                    emptySlot = attachmentSlots[0];
                    emptySlot.DetachAttachment();
                }
            }

            emptySlot.AttachAttachment(attachment);
            emptySlot.acquisitionTurn = battleManager != null ? battleManager.CurrentTurn : 0;

            OnAttachmentSelected?.Invoke(attachment);
            ApplyAttachmentEffects(attachment);

            Debug.Log($"アタッチメント装着: {attachment.attachmentName}");
            return true;
        }

        // アタッチメント取り外し
        public bool DetachAttachment(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= attachmentSlots.Count)
                return false;

            AttachmentSlot slot = attachmentSlots[slotIndex];
            if (slot.IsEmpty)
                return false;

            AttachmentData detachedAttachment = slot.attachedData;
            RemoveAttachmentEffects(detachedAttachment);
            slot.DetachAttachment();

            OnAttachmentRemoved?.Invoke(detachedAttachment);
            Debug.Log($"アタッチメント取り外し: {detachedAttachment.attachmentName}");
            return true;
        }

        // アタッチメント強化
        public bool EnhanceAttachment(int slotIndex)
        {
            if (!allowEnhancement || slotIndex < 0 || slotIndex >= attachmentSlots.Count)
                return false;

            AttachmentSlot slot = attachmentSlots[slotIndex];
            if (slot.IsEmpty || slot.enhancementLevel >= maxEnhancementLevel)
                return false;

            slot.enhancementLevel++;
            OnAttachmentEnhanced?.Invoke(slot.attachedData, slot.enhancementLevel);

            Debug.Log($"アタッチメント強化: {slot.attachedData.attachmentName} Lv.{slot.enhancementLevel}");
            return true;
        }

        // アタッチメント効果適用
        private void ApplyAttachmentEffects(AttachmentData attachment)
        {
            if (battleManager == null)
                return;

            PlayerData player = battleManager.PlayerData;

            foreach (AttachmentEffect effect in attachment.effects)
            {
                ApplyAttachmentEffect(effect, attachment, player);
            }
        }

        // 個別効果適用
        private void ApplyAttachmentEffect(AttachmentEffect effect, AttachmentData attachment, PlayerData player)
        {
            float rarityMultiplier = attachment.GetRarityMultiplier();
            float finalEffectValue = effect.effectValue * rarityMultiplier;
            int finalFlatValue = Mathf.RoundToInt(effect.flatValue * rarityMultiplier);

            switch (effect.effectType)
            {
                case AttachmentEffectType.AttackPowerBoost:
                    // プレイヤーの基本攻撃力を永続的に強化
                    player.baseAttackPower += Mathf.RoundToInt(player.baseAttackPower * finalEffectValue);
                    break;

                case AttachmentEffectType.MaxHpBoost:
                    // 最大HPを永続的に増加
                    int hpIncrease = Mathf.RoundToInt(player.maxHp * finalEffectValue);
                    player.maxHp += hpIncrease;
                    player.currentHp += hpIncrease; // 増加分は即座に回復
                    break;

                case AttachmentEffectType.CriticalRateBoost:
                    // 全武器のクリティカル率増加（武器データに直接適用）
                    ApplyWeaponCriticalBoost(finalEffectValue);
                    break;

                case AttachmentEffectType.WeaponPowerBoost:
                    // 全武器の攻撃力増加
                    ApplyWeaponPowerBoost(finalEffectValue);
                    break;

                case AttachmentEffectType.CooldownReduction:
                    // 全武器のクールダウン減少
                    ApplyWeaponCooldownReduction(finalFlatValue);
                    break;

                // その他の効果は実装時に詳細化
                default:
                    Debug.Log($"アタッチメント効果適用: {effect.effectType} - {finalEffectValue}");
                    break;
            }
        }

        // 武器クリティカル率強化
        private void ApplyWeaponCriticalBoost(float boostPercentage)
        {
            PlayerData player = battleManager.PlayerData;
            for (int i = 0; i < player.equippedWeapons.Length; i++)
            {
                if (player.equippedWeapons[i] != null)
                {
                    player.equippedWeapons[i].criticalRate += Mathf.RoundToInt(boostPercentage * 100);
                }
            }
        }

        // 武器攻撃力強化
        private void ApplyWeaponPowerBoost(float boostPercentage)
        {
            PlayerData player = battleManager.PlayerData;
            for (int i = 0; i < player.equippedWeapons.Length; i++)
            {
                if (player.equippedWeapons[i] != null)
                {
                    player.equippedWeapons[i].basePower += Mathf.RoundToInt(player.equippedWeapons[i].basePower * boostPercentage);
                }
            }
        }

        // 武器クールダウン短縮
        private void ApplyWeaponCooldownReduction(int reduction)
        {
            PlayerData player = battleManager.PlayerData;
            for (int i = 0; i < player.equippedWeapons.Length; i++)
            {
                if (player.equippedWeapons[i] != null)
                {
                    player.equippedWeapons[i].cooldownTurns = Mathf.Max(0, player.equippedWeapons[i].cooldownTurns - reduction);
                }
            }
        }

        // アタッチメント効果除去
        private void RemoveAttachmentEffects(AttachmentData attachment)
        {
            // 効果の逆転処理（詳細実装は後のフェーズで行う）
            Debug.Log($"アタッチメント効果除去: {attachment.attachmentName}");
        }

        // 特定効果のアタッチメントが装着されているかチェック
        public bool HasAttachmentWithEffect(AttachmentEffectType effectType)
        {
            return attachmentSlots.Any(slot => !slot.IsEmpty && 
                                      slot.attachedData.effects.Any(effect => effect.effectType == effectType));
        }

        // 装着中のアタッチメント取得
        public List<AttachmentData> GetAttachedAttachments()
        {
            return attachmentSlots.Where(slot => !slot.IsEmpty)
                                 .Select(slot => slot.attachedData)
                                 .ToList();
        }

        // アタッチメント効果値の取得
        public float GetAttachmentEffectValue(AttachmentEffectType effectType)
        {
            float totalValue = 0f;

            foreach (AttachmentSlot slot in attachmentSlots)
            {
                if (!slot.IsEmpty)
                {
                    foreach (AttachmentEffect effect in slot.attachedData.effects)
                    {
                        if (effect.effectType == effectType)
                        {
                            float rarityMultiplier = slot.attachedData.GetRarityMultiplier();
                            totalValue += effect.effectValue * rarityMultiplier;
                        }
                    }
                }
            }

            return totalValue;
        }

        // デバッグ用：ランダムアタッチメント装着
        [ContextMenu("Attach Random Attachment")]
        public void AttachRandomAttachment()
        {
            if (attachmentDatabase != null)
            {
                AttachmentData randomAttachment = attachmentDatabase.GetRandomAttachment();
                if (randomAttachment != null)
                {
                    AttachAttachment(randomAttachment);
                }
            }
        }

        // デバッグ用：アタッチメント選択肢表示
        [ContextMenu("Generate Attachment Options")]
        public void GenerateOptionsForDebug()
        {
            AttachmentData[] options = GenerateAttachmentOptions();
            Debug.Log($"アタッチメント選択肢生成: {options.Length}個");
            foreach (AttachmentData option in options)
            {
                Debug.Log($"- {option.attachmentName} ({option.rarity})");
            }
        }
        
        /// <summary>
        /// PlayMode開始時に装備中のアタッチメントを表示
        /// </summary>
        private void DisplayEquippedAttachmentsOnPlayModeStart()
        {
            var equippedAttachments = GetAttachedAttachments();
            
            // コンソールに装備情報を表示
            if (equippedAttachments.Count > 0)
            {
                Debug.Log("=== PlayMode開始 - 装備中アタッチメント ===");
                foreach (var attachment in equippedAttachments)
                {
                    string comboInfo = !string.IsNullOrEmpty(attachment.associatedComboName) 
                        ? $" (コンボ: {attachment.associatedComboName})" 
                        : " (コンボ: 未設定)";
                    Debug.Log($"🔗 {attachment.attachmentName} [{GetRarityIcon(attachment.rarity)} {attachment.rarity}]{comboInfo}");
                    
                    // 効果詳細を表示
                    foreach (var effect in attachment.effects)
                    {
                        string effectDescription = GetEffectDescription(effect);
                        Debug.Log($"   └─ {effectDescription}");
                    }
                }
                Debug.Log("=========================================");
            }
            else
            {
                Debug.Log("=== PlayMode開始 - アタッチメント装備なし ===");
            }
            
            // UIイベントを発行してUI側でも表示
            OnPlayModeAttachmentsDisplayRequested?.Invoke(equippedAttachments);
        }
        
        /// <summary>
        /// レアリティアイコンを取得
        /// </summary>
        private string GetRarityIcon(AttachmentRarity rarity)
        {
            return rarity switch
            {
                AttachmentRarity.Common => "⚪",
                AttachmentRarity.Rare => "🔵",
                AttachmentRarity.Epic => "🟣",
                AttachmentRarity.Legendary => "🟡",
                _ => "❔"
            };
        }
        
        /// <summary>
        /// エフェクト説明を生成
        /// </summary>
        private string GetEffectDescription(AttachmentEffect effect)
        {
            string baseDesc = effect.effectType switch
            {
                AttachmentEffectType.AttackPowerBoost => $"攻撃力+{(effect.effectValue * 100):F0}%",
                AttachmentEffectType.MaxHpBoost => $"最大HP+{(effect.effectValue * 100):F0}%",
                AttachmentEffectType.CriticalRateBoost => $"クリティカル率+{(effect.effectValue * 100):F0}%",
                AttachmentEffectType.WeaponPowerBoost => $"武器攻撃力+{(effect.effectValue * 100):F0}%",
                AttachmentEffectType.CooldownReduction => $"クールダウン-{effect.flatValue}ターン",
                _ => effect.effectType.ToString()
            };
            
            return effect.isPercentage 
                ? $"{baseDesc} (倍率効果)" 
                : $"{baseDesc} (固定効果)";
        }
        
        /// <summary>
        /// PlayMode中に手動でアタッチメント情報を表示
        /// </summary>
        [ContextMenu("Show Current Equipped Attachments")]
        public void ShowCurrentEquippedAttachments()
        {
            DisplayEquippedAttachmentsOnPlayModeStart();
        }
        
        /// <summary>
        /// PlayMode開始時に指定数のランダムアタッチメントを装備
        /// </summary>
        private void EquipRandomAttachmentsOnStart(int count)
        {
            if (attachmentDatabase == null || availableAttachments.Count == 0)
            {
                Debug.LogWarning("AttachmentDatabase が利用できません。アタッチメントの自動装備をスキップします。");
                return;
            }

            Debug.Log($"🎲 PlayMode開始時に{count}個のランダムアタッチメントを装備中...");
            
            int equipped = 0;
            int maxAttempts = availableAttachments.Count * 2; // 無限ループ防止
            int attempts = 0;
            
            while (equipped < count && attempts < maxAttempts)
            {
                attempts++;
                AttachmentData randomAttachment = attachmentDatabase.GetRandomAttachment();
                
                if (randomAttachment != null)
                {
                    // 重複チェック（allowDuplicatesがfalseの場合）
                    if (!allowDuplicates && attachmentSlots.Any(slot => !slot.IsEmpty && 
                        slot.attachedData.attachmentId == randomAttachment.attachmentId))
                    {
                        continue; // 既に装備済みの場合はスキップ
                    }
                    
                    if (AttachAttachment(randomAttachment))
                    {
                        equipped++;
                        Debug.Log($"  ✅ 自動装備: {randomAttachment.attachmentName} [{GetRarityIcon(randomAttachment.rarity)} {randomAttachment.rarity}]");
                    }
                }
            }
            
            if (equipped < count)
            {
                Debug.LogWarning($"⚠️ 要求された{count}個のうち{equipped}個のアタッチメントのみ装備できました。");
            }
            else
            {
                Debug.Log($"✅ {equipped}個のランダムアタッチメント装備完了!");
            }
        }

        /// <summary>
        /// テスト用：ランダムアタッチメントを装備して表示テスト
        /// </summary>
        [ContextMenu("Test: Equip Random Attachment and Display")]
        public void TestEquipAndDisplay()
        {
            // ランダムアタッチメントを装備
            AttachRandomAttachment();
            
            // 装備後に表示テスト
            ShowCurrentEquippedAttachments();
        }
        
        /// <summary>
        /// 全アタッチメントを取り外し（テスト用）
        /// </summary>
        [ContextMenu("Clear All Attachments")]
        public void ClearAllAttachments()
        {
            for (int i = 0; i < attachmentSlots.Count; i++)
            {
                if (!attachmentSlots[i].IsEmpty)
                {
                    DetachAttachment(i);
                }
            }
            Debug.Log("🧹 全アタッチメント取り外し完了");
        }
        
        /// <summary>
        /// テスト用：武器カードの列をランダム再生成
        /// </summary>
        [ContextMenu("Test: Regenerate Weapon Cards")]
        public void TestRegenerateWeaponCards()
        {
            Debug.Log("🧪 テスト: 武器・カードランダム再生成を実行中...");
            RegenerateWeaponCardsForNewTurn();
        }
        
        /// <summary>
        /// テスト用：武器のみをランダム再装備
        /// </summary>
        [ContextMenu("Test: Random Reequip Weapons")]
        public void TestRandomReequipWeapons()
        {
            Debug.Log("🧪 テスト: 武器ランダム再装備を実行中...");
            RandomlyReequipWeapons();
        }
        
        /// <summary>
        /// PlayMode開始時に指定数の武器をランダム装備
        /// </summary>
        private void EquipRandomWeaponsOnStart(int count)
        {
            if (weaponDatabase == null)
            {
                CreateDefaultWeaponDatabase();
            }
            
            if (weaponDatabase == null || weaponDatabase.Weapons == null || weaponDatabase.Weapons.Length == 0)
            {
                Debug.LogWarning("WeaponDatabase が利用できません。武器の自動装備をスキップします。");
                return;
            }
            
            Debug.Log($"⚔️ PlayMode開始時に{count}個のランダム武器を装備中...");
            
            equippedWeapons.Clear();
            var weapons = weaponDatabase.Weapons;
            var random = new System.Random();
            
            for (int i = 0; i < count && i < weapons.Length; i++)
            {
                int randomIndex = random.Next(weapons.Length);
                var selectedWeapon = weapons[randomIndex];
                
                equippedWeapons.Add(selectedWeapon);
                Debug.Log($"  ✅ 武器装備: {selectedWeapon.weaponName} (攻撃力: {selectedWeapon.basePower})");
            }
            
            // 装備武器からランダム列でカードを生成
            GenerateWeaponCardsWithRandomColumns();
            
            Debug.Log($"⚔️ {equippedWeapons.Count}個の武器装備完了!");
        }
        
        /// <summary>
        /// 装備武器からランダム列割り振りのカードを生成
        /// </summary>
        private void GenerateWeaponCardsWithRandomColumns()
        {
            weaponCards.Clear();
            
            if (equippedWeapons == null || equippedWeapons.Count == 0)
            {
                Debug.LogWarning("装備武器がありません。カード生成をスキップします。");
                return;
            }
            
            // より確実にランダムになるよう、現在時刻をシードに使用
            var random = new System.Random((int)System.DateTime.Now.Ticks);
            int totalColumns = 3; // 戦場は3列（左、中、右）
            
            Debug.Log($"🎲 {equippedWeapons.Count}個の武器から{totalColumns}列にランダム配置する武器カードを生成中...");
            
            foreach (var weapon in equippedWeapons)
            {
                // ランダムに列を割り振り
                int randomColumn = random.Next(totalColumns);
                var card = new CardData(weapon, randomColumn, totalColumns);
                
                weaponCards.Add(card);
                Debug.Log($"🎴 武器カード生成: {card.displayName} → 攻撃列: {randomColumn} ({card.columnName})");
            }
            
            // HandSystemに武器カードを通知
            OnWeaponCardsGenerated?.Invoke(weaponCards);
            
            Debug.Log($"✅ {weaponCards.Count}枚の武器カード生成完了! 手札が更新されます。");
        }
        
        /// <summary>
        /// デフォルトのWeaponDatabaseを動的作成
        /// </summary>
        private void CreateDefaultWeaponDatabase()
        {
            // WeaponDatabaseが設定されていない場合のデフォルト武器作成
            Debug.Log("WeaponDatabase not found, creating default weapons...");
            
            weaponDatabase = ScriptableObject.CreateInstance<WeaponDatabase>();
            
            var defaultWeapons = new WeaponData[]
            {
                new WeaponData("炎の剣", AttackAttribute.Fire, WeaponType.Sword, 120, AttackRange.SingleFront)
                {
                    criticalRate = 10,
                    cooldownTurns = 0,
                    canUseConsecutively = true,
                    specialEffect = "燃焼ダメージ"
                },
                new WeaponData("氷の槍", AttackAttribute.Ice, WeaponType.Spear, 100, AttackRange.Column)
                {
                    criticalRate = 8,
                    cooldownTurns = 0,
                    canUseConsecutively = true,
                    specialEffect = "凍結効果"
                },
                new WeaponData("雷の弓", AttackAttribute.Thunder, WeaponType.Bow, 90, AttackRange.SingleTarget)
                {
                    criticalRate = 15,
                    cooldownTurns = 0,
                    canUseConsecutively = true,
                    specialEffect = "麻痺効果"
                },
                new WeaponData("風の斧", AttackAttribute.Wind, WeaponType.Axe, 140, AttackRange.Row1)
                {
                    criticalRate = 5,
                    cooldownTurns = 0,
                    canUseConsecutively = true,
                    specialEffect = "ノックバック"
                },
                new WeaponData("光の魔法杖", AttackAttribute.Light, WeaponType.Magic, 80, AttackRange.All)
                {
                    criticalRate = 12,
                    cooldownTurns = 0,
                    canUseConsecutively = true,
                    specialEffect = "回復効果"
                }
            };
            
            // リフレクションを使って武器配列を設定
            var weaponsField = typeof(WeaponDatabase).GetField("weapons", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            weaponsField?.SetValue(weaponDatabase, defaultWeapons);
            
            Debug.Log($"Default WeaponDatabase created with {defaultWeapons.Length} weapons");
        }
        
        /// <summary>
        /// 装備武器リストを取得
        /// </summary>
        public List<WeaponData> GetEquippedWeapons()
        {
            return new List<WeaponData>(equippedWeapons);
        }
        
        /// <summary>
        /// 武器カードリストを取得
        /// </summary>
        public List<CardData> GetWeaponCards()
        {
            return new List<CardData>(weaponCards);
        }
        
        /// <summary>
        /// PlayMode開始時のHandSystem初期化
        /// </summary>
        private void InitializeHandSystemForPlay()
        {
            var handSystem = battleManager?.GetComponent<HandSystem>();
            if (handSystem == null) return;
            
            // 手札と行動回数を強制初期化
            try
            {
                // リフレクションを使ってHandSystemの初期化メソッドを呼び出し
                var initMethod = handSystem.GetType().GetMethod("InitializeActionsForTurn", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                initMethod?.Invoke(handSystem, null);
                
                Debug.Log("✅ HandSystem初期化完了（行動回数・手札状態）");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"HandSystem初期化に失敗: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ターン開始時に武器カードの列をランダムに再生成
        /// </summary>
        public void RegenerateWeaponCardsForNewTurn()
        {
            Debug.Log($"🎲 ターン開始時: 武器の種類と列をランダム再生成中...");
            
            // 1. 装備武器をランダムに再選択
            RandomlyReequipWeapons();
            
            // 2. 新しい装備武器からランダム列でカードを再生成
            GenerateWeaponCardsWithRandomColumns();
            
            Debug.Log($"✅ ターン開始時の武器・カード再生成完了! 新しい手札が利用可能です。");
        }
        
        /// <summary>
        /// 装備武器をランダムに再選択
        /// </summary>
        private void RandomlyReequipWeapons()
        {
            if (weaponDatabase == null)
            {
                CreateDefaultWeaponDatabase();
            }
            
            if (weaponDatabase == null || weaponDatabase.Weapons == null || weaponDatabase.Weapons.Length == 0)
            {
                Debug.LogWarning("WeaponDatabase が利用できません。武器の再選択をスキップします。");
                return;
            }
            
            int currentWeaponCount = equippedWeapons?.Count ?? maxEquippedWeapons;
            
            Debug.Log($"🔄 装備武器をランダム再選択: {currentWeaponCount}個");
            
            equippedWeapons.Clear();
            var weapons = weaponDatabase.Weapons;
            var random = new System.Random((int)System.DateTime.Now.Ticks);
            
            for (int i = 0; i < currentWeaponCount && weapons.Length > 0; i++)
            {
                int randomIndex = random.Next(weapons.Length);
                var selectedWeapon = weapons[randomIndex];
                
                equippedWeapons.Add(selectedWeapon);
                Debug.Log($"  🎯 新装備: {selectedWeapon.weaponName} (攻撃力: {selectedWeapon.basePower})");
            }
            
            Debug.Log($"✅ {equippedWeapons.Count}個の武器をランダム再装備完了!");
        }
    }
}