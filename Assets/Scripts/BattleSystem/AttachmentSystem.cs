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

    // アタッチメントデータベース
    [CreateAssetMenu(fileName = "AttachmentDatabase", menuName = "BattleSystem/AttachmentDatabase")]
    public class AttachmentDatabase : ScriptableObject
    {
        [Header("プリセットアタッチメント")]
        [SerializeField] private AttachmentData[] presetAttachments;
        
        [Header("レアリティ出現率")]
        [SerializeField] private float commonRate = 0.6f;
        [SerializeField] private float rareRate = 0.25f;
        [SerializeField] private float epicRate = 0.12f;
        [SerializeField] private float legendaryRate = 0.03f;

        public AttachmentData[] PresetAttachments => presetAttachments;
        
        private void OnEnable()
        {
            InitializePresetAttachments();
        }

        // プリセットアタッチメントの初期化
        private void InitializePresetAttachments()
        {
            if (presetAttachments == null || presetAttachments.Length == 0)
            {
                CreateDefaultAttachments();
            }
        }

        // デフォルトアタッチメントの作成
        private void CreateDefaultAttachments()
        {
            List<AttachmentData> attachments = new List<AttachmentData>();

            // コモン（4個）
            attachments.Add(CreateAttachment(1, "パワーモジュール", AttachmentRarity.Common, AttachmentCategory.Attack,
                "攻撃力+15%", "基本的な攻撃力強化モジュール",
                AttachmentEffectType.AttackPowerBoost, 0.15f, 0));

            attachments.Add(CreateAttachment(2, "アーマープレート", AttachmentRarity.Common, AttachmentCategory.Defense,
                "被ダメージ-10%", "軽量な防護プレート",
                AttachmentEffectType.DamageReduction, 0.10f, 0));

            attachments.Add(CreateAttachment(3, "エナジーコア", AttachmentRarity.Common, AttachmentCategory.Defense,
                "HP最大値+20%", "エネルギー容量を増加させる",
                AttachmentEffectType.MaxHpBoost, 0.20f, 0));

            attachments.Add(CreateAttachment(4, "プレシジョンサイト", AttachmentRarity.Common, AttachmentCategory.Attack,
                "クリティカル率+10%", "照準精度を向上させる",
                AttachmentEffectType.CriticalRateBoost, 0.10f, 0));

            // レア（5個）
            attachments.Add(CreateAttachment(5, "ヘビーバレル", AttachmentRarity.Rare, AttachmentCategory.Attack,
                "武器攻撃力+30%", "重装型の攻撃強化バレル",
                AttachmentEffectType.WeaponPowerBoost, 0.30f, 0));

            attachments.Add(CreateAttachment(6, "コンボチップ", AttachmentRarity.Rare, AttachmentCategory.Combo,
                "コンボダメージ倍率+25%", "コンボ効果を増幅する",
                AttachmentEffectType.ComboDamageBoost, 0.25f, 0));

            attachments.Add(CreateAttachment(7, "シールドジェネレーター", AttachmentRarity.Rare, AttachmentCategory.Defense,
                "盾の反射率+25%", "防御反射能力を強化",
                AttachmentEffectType.ShieldReflection, 0.25f, 0));

            attachments.Add(CreateAttachment(8, "クイックローダー", AttachmentRarity.Rare, AttachmentCategory.Utility,
                "クールダウン-1ターン", "武器の再装填速度向上",
                AttachmentEffectType.CooldownReduction, 0f, 1));

            attachments.Add(CreateAttachment(9, "リフレクターコア", AttachmentRarity.Rare, AttachmentCategory.Defense,
                "カウンターダメージ+30%", "反撃能力を大幅強化",
                AttachmentEffectType.CounterDamageBoost, 0.30f, 0));

            // エピック（3個）
            attachments.Add(CreateAttachment(10, "オーバークロッカー", AttachmentRarity.Epic, AttachmentCategory.Attack,
                "クリティカル倍率+50%（2倍→3倍）", "限界突破型の攻撃増幅器",
                AttachmentEffectType.CriticalDamageBoost, 0.50f, 0));

            attachments.Add(CreateAttachment(11, "コンボアクセラレーター", AttachmentRarity.Epic, AttachmentCategory.Combo,
                "コンボ成功時、次ターン2回行動可能", "コンボ連携を加速させる",
                AttachmentEffectType.ComboActionGrant, 0f, 1));

            attachments.Add(CreateAttachment(12, "バリアフィールド", AttachmentRarity.Epic, AttachmentCategory.Utility,
                "ターン開始時HP5%回復", "自動回復バリアを展開",
                AttachmentEffectType.TurnStartHeal, 0.05f, 0));

            // レジェンダリー（3個）
            attachments.Add(CreateAttachment(13, "デストロイヤーモード", AttachmentRarity.Legendary, AttachmentCategory.Attack,
                "HP50%以下時、全ダメージ+100%", "危機状態で真の力を発揮",
                AttachmentEffectType.ConditionalPowerBoost, 1.00f, 0));

            attachments.Add(CreateAttachment(14, "フェニックスコア", AttachmentRarity.Legendary, AttachmentCategory.Utility,
                "戦闘中1回だけ、HP0時に50%で復活", "不死鳥の力を宿すコア",
                AttachmentEffectType.SpecialAbility, 0.50f, 1));

            attachments.Add(CreateAttachment(15, "オムニコンボ", AttachmentRarity.Legendary, AttachmentCategory.Combo,
                "任意の装備でコンボ継続可能", "全ての武器を統合制御",
                AttachmentEffectType.ComboSimplification, 1.00f, 0));

            presetAttachments = attachments.ToArray();
        }

        // アタッチメント作成ヘルパー
        private AttachmentData CreateAttachment(int id, string name, AttachmentRarity rarity, 
            AttachmentCategory category, string description, string flavorText,
            AttachmentEffectType effectType, float effectValue, int flatValue)
        {
            AttachmentData attachment = new AttachmentData
            {
                attachmentId = id,
                attachmentName = name,
                rarity = rarity,
                category = category,
                description = description,
                flavorText = flavorText,
                isUnique = rarity == AttachmentRarity.Legendary,
                effects = new AttachmentEffect[]
                {
                    new AttachmentEffect
                    {
                        effectType = effectType,
                        effectValue = effectValue,
                        flatValue = flatValue,
                        isPercentage = effectValue > 0,
                        stackable = rarity != AttachmentRarity.Legendary
                    }
                }
            };

            return attachment;
        }

        // レアリティ別アタッチメント取得
        public AttachmentData[] GetAttachmentsByRarity(AttachmentRarity rarity)
        {
            return presetAttachments.Where(a => a.rarity == rarity).ToArray();
        }

        // カテゴリ別アタッチメント取得
        public AttachmentData[] GetAttachmentsByCategory(AttachmentCategory category)
        {
            return presetAttachments.Where(a => a.category == category).ToArray();
        }

        // アタッチメント取得
        public AttachmentData GetAttachment(int attachmentId)
        {
            return presetAttachments.FirstOrDefault(a => a.attachmentId == attachmentId);
        }

        // ランダムレアリティ決定
        public AttachmentRarity GetRandomRarity()
        {
            float random = UnityEngine.Random.value;
            
            if (random < legendaryRate)
                return AttachmentRarity.Legendary;
            else if (random < legendaryRate + epicRate)
                return AttachmentRarity.Epic;
            else if (random < legendaryRate + epicRate + rareRate)
                return AttachmentRarity.Rare;
            else
                return AttachmentRarity.Common;
        }

        // ランダムアタッチメント生成
        public AttachmentData GetRandomAttachment(AttachmentRarity? forceRarity = null)
        {
            AttachmentRarity rarity = forceRarity ?? GetRandomRarity();
            AttachmentData[] candidateAttachments = GetAttachmentsByRarity(rarity);
            
            if (candidateAttachments.Length == 0)
                return null;
            
            return candidateAttachments[UnityEngine.Random.Range(0, candidateAttachments.Length)];
        }
    }

    // アタッチメント管理システム
    public class AttachmentSystem : MonoBehaviour
    {
        [Header("アタッチメント設定")]
        [SerializeField] private AttachmentDatabase attachmentDatabase;
        [SerializeField] private int maxAttachmentSlots = 1;  // 基本は1個まで
        [SerializeField] private int selectionOptionsCount = 4; // 選択肢数
        [SerializeField] private bool allowDuplicates = false;   // 重複許可
        
        [Header("強化設定")]
        [SerializeField] private bool allowEnhancement = true;
        [SerializeField] private int maxEnhancementLevel = 5;

        private BattleManager battleManager;
        private List<AttachmentSlot> attachmentSlots;
        private List<AttachmentData> availableAttachments;

        // イベント定義
        public event Action<AttachmentData[]> OnAttachmentOptionsPresented;
        public event Action<AttachmentData> OnAttachmentSelected;
        public event Action<AttachmentData, int> OnAttachmentEnhanced;
        public event Action<AttachmentData> OnAttachmentRemoved;

        // プロパティ
        public List<AttachmentSlot> AttachmentSlots => attachmentSlots;
        public AttachmentDatabase Database => attachmentDatabase;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            attachmentSlots = new List<AttachmentSlot>();
            availableAttachments = new List<AttachmentData>();
            
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
        }

        // デフォルトのAttachmentDatabaseを動的作成
        private void CreateDefaultAttachmentDatabase()
        {
            Debug.Log("Creating default AttachmentDatabase...");
            
            attachmentDatabase = ScriptableObject.CreateInstance<AttachmentDatabase>();
            attachmentDatabase.hideFlags = HideFlags.DontSaveInEditor;
            
            // OnEnableを手動で呼び出してプリセットアタッチメントを初期化
            var onEnableMethod = typeof(AttachmentDatabase).GetMethod("OnEnable", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            onEnableMethod?.Invoke(attachmentDatabase, null);
            
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
            for (int i = 0; i < maxAttachmentSlots; i++)
            {
                attachmentSlots.Add(new AttachmentSlot());
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

            // 空きスロットを探す
            AttachmentSlot emptySlot = attachmentSlots.FirstOrDefault(slot => slot.IsEmpty);
            if (emptySlot == null)
            {
                // 空きがない場合、最初のスロットを上書き（実際のゲームでは選択UIが必要）
                emptySlot = attachmentSlots[0];
                emptySlot.DetachAttachment();
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
    }
}