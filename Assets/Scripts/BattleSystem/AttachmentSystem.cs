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
            // hideFlagsを設定しないことでアサーションエラーを回避
            
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

        /// <summary>
        /// 装備中のアタッチメント情報をコンソールに表示
        /// </summary>
        [ContextMenu("Show Equipped Attachments")]
        public void ShowEquippedAttachments()
        {
            DisplayEquippedAttachments();
        }

        /// <summary>
        /// 装備中のアタッチメント詳細情報を表示
        /// </summary>
        public void DisplayEquippedAttachments()
        {
            Debug.Log("=== 装備中アタッチメント一覧 ===");
            
            if (attachmentSlots == null || attachmentSlots.Count == 0)
            {
                Debug.Log("❌ アタッチメントスロットが存在しません");
                return;
            }

            bool hasAnyAttachment = false;
            
            for (int i = 0; i < attachmentSlots.Count; i++)
            {
                AttachmentSlot slot = attachmentSlots[i];
                
                if (slot.IsEmpty)
                {
                    Debug.Log($"📦 スロット {i + 1}: 空き");
                }
                else
                {
                    hasAnyAttachment = true;
                    AttachmentData attachment = slot.attachedData;
                    
                    // レアリティに応じたアイコン
                    string rarityIcon = GetRarityIcon(attachment.rarity);
                    
                    Debug.Log($"{rarityIcon} スロット {i + 1}: {attachment.attachmentName}");
                    Debug.Log($"   📋 説明: {attachment.description}");
                    Debug.Log($"   🎯 対応コンボ: {GetComboDisplayName(attachment.associatedComboName)}");
                    Debug.Log($"   🌟 レアリティ: {attachment.rarity} (倍率: {attachment.GetRarityMultiplier():F1}x)");
                    Debug.Log($"   📂 カテゴリ: {GetCategoryDisplayName(attachment.category)}");
                    
                    // 効果詳細
                    if (attachment.effects != null && attachment.effects.Length > 0)
                    {
                        Debug.Log($"   ⚡ 効果:");
                        foreach (AttachmentEffect effect in attachment.effects)
                        {
                            string effectDesc = GetEffectDescription(effect, attachment.GetRarityMultiplier());
                            Debug.Log($"      • {effectDesc}");
                        }
                    }
                    
                    // スロット情報
                    Debug.Log($"   📅 取得ターン: {slot.acquisitionTurn}");
                    Debug.Log($"   ⭐ 強化レベル: {slot.enhancementLevel}");
                    Debug.Log($"   🔄 アクティブ: {(slot.isActive ? "有効" : "無効")}");
                    
                    if (attachment.isUnique)
                    {
                        Debug.Log($"   🔒 ユニークアタッチメント");
                    }
                    
                    Debug.Log("   " + new string('-', 30));
                }
            }
            
            if (!hasAnyAttachment)
            {
                Debug.Log("💡 現在アタッチメントは装備されていません");
            }
            else
            {
                // 合計効果値表示
                DisplayTotalEffects();
            }
            
            Debug.Log("================================");
        }

        /// <summary>
        /// 合計効果値を表示
        /// </summary>
        private void DisplayTotalEffects()
        {
            Debug.Log("📊 合計効果値:");
            
            var effectTypes = System.Enum.GetValues(typeof(AttachmentEffectType));
            foreach (AttachmentEffectType effectType in effectTypes)
            {
                float totalValue = GetAttachmentEffectValue(effectType);
                if (totalValue > 0)
                {
                    Debug.Log($"   🔹 {GetEffectTypeDisplayName(effectType)}: +{totalValue:F2}");
                }
            }
        }

        /// <summary>
        /// レアリティ表示用アイコンを取得
        /// </summary>
        private string GetRarityIcon(AttachmentRarity rarity)
        {
            switch (rarity)
            {
                case AttachmentRarity.Common: return "⚪"; // 白
                case AttachmentRarity.Rare: return "🔵"; // 青
                case AttachmentRarity.Epic: return "🟣"; // 紫
                case AttachmentRarity.Legendary: return "🟡"; // 金
                default: return "⚫";
            }
        }

        /// <summary>
        /// コンボ名の表示用文字列を取得
        /// </summary>
        private string GetComboDisplayName(string comboName)
        {
            return string.IsNullOrEmpty(comboName) ? "未設定" : comboName;
        }

        /// <summary>
        /// カテゴリの表示用文字列を取得
        /// </summary>
        private string GetCategoryDisplayName(AttachmentCategory category)
        {
            switch (category)
            {
                case AttachmentCategory.Attack: return "攻撃系";
                case AttachmentCategory.Defense: return "防御系";
                case AttachmentCategory.Combo: return "コンボ系";
                case AttachmentCategory.Utility: return "ユーティリティ系";
                default: return category.ToString();
            }
        }

        /// <summary>
        /// 効果タイプの表示用文字列を取得
        /// </summary>
        private string GetEffectTypeDisplayName(AttachmentEffectType effectType)
        {
            switch (effectType)
            {
                case AttachmentEffectType.AttackPowerBoost: return "攻撃力増加";
                case AttachmentEffectType.CriticalRateBoost: return "クリティカル率増加";
                case AttachmentEffectType.CriticalDamageBoost: return "クリティカルダメージ増加";
                case AttachmentEffectType.WeaponPowerBoost: return "武器攻撃力増加";
                case AttachmentEffectType.MaxHpBoost: return "最大HP増加";
                case AttachmentEffectType.DamageReduction: return "被ダメージ軽減";
                case AttachmentEffectType.CounterDamageBoost: return "カウンターダメージ増加";
                case AttachmentEffectType.ShieldReflection: return "盾反射率増加";
                case AttachmentEffectType.ComboDamageBoost: return "コンボダメージ増加";
                case AttachmentEffectType.ComboActionGrant: return "コンボ追加行動";
                case AttachmentEffectType.ComboSimplification: return "コンボ簡略化";
                case AttachmentEffectType.ComboAutoTrigger: return "コンボ自動発動";
                case AttachmentEffectType.CooldownReduction: return "クールダウン短縮";
                case AttachmentEffectType.TurnStartHeal: return "ターン開始時回復";
                case AttachmentEffectType.ConditionalPowerBoost: return "条件付きパワーブースト";
                case AttachmentEffectType.SpecialAbility: return "特殊能力";
                default: return effectType.ToString();
            }
        }

        /// <summary>
        /// 効果の詳細説明を取得
        /// </summary>
        private string GetEffectDescription(AttachmentEffect effect, float rarityMultiplier)
        {
            float finalEffectValue = effect.effectValue * rarityMultiplier;
            int finalFlatValue = Mathf.RoundToInt(effect.flatValue * rarityMultiplier);

            if (effect.isPercentage && effect.effectValue > 0)
            {
                return $"{GetEffectTypeDisplayName(effect.effectType)}: +{finalEffectValue * 100:F1}%";
            }
            else if (effect.flatValue > 0)
            {
                return $"{GetEffectTypeDisplayName(effect.effectType)}: +{finalFlatValue}";
            }
            else
            {
                return $"{GetEffectTypeDisplayName(effect.effectType)}: 特殊効果";
            }
        }
    }
}