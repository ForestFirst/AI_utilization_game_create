using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// アタッチメントデータベース - アタッチメントの定義と管理を担当
    /// </summary>
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

        /// <summary>
        /// プリセットアタッチメントの初期化
        /// </summary>
        private void InitializePresetAttachments()
        {
            if (presetAttachments == null || presetAttachments.Length == 0)
            {
                CreateDefaultAttachments();
            }
        }

        /// <summary>
        /// デフォルトアタッチメントの作成（コンボ対応付きで15個）
        /// </summary>
        private void CreateDefaultAttachments()
        {
            List<AttachmentData> attachments = new List<AttachmentData>();

            // コモン（4個）- 基本コンボ対応
            attachments.Add(CreateAttachment(1, "パワーモジュール", AttachmentRarity.Common, AttachmentCategory.Attack,
                "攻撃力+15%", "基本的な攻撃力強化モジュール",
                AttachmentEffectType.AttackPowerBoost, 0.15f, 0, "フレイムスラッシュ"));

            attachments.Add(CreateAttachment(2, "アーマープレート", AttachmentRarity.Common, AttachmentCategory.Defense,
                "被ダメージ-10%", "軽量な防護プレート",
                AttachmentEffectType.DamageReduction, 0.10f, 0, "アイスブレイカー"));

            attachments.Add(CreateAttachment(3, "エナジーコア", AttachmentRarity.Common, AttachmentCategory.Defense,
                "HP最大値+20%", "エネルギー容量を増加させる",
                AttachmentEffectType.MaxHpBoost, 0.20f, 0, "サンダーストライク"));

            attachments.Add(CreateAttachment(4, "プレシジョンサイト", AttachmentRarity.Common, AttachmentCategory.Attack,
                "クリティカル率+10%", "照準精度を向上させる",
                AttachmentEffectType.CriticalRateBoost, 0.10f, 0, "ガンスリンガー"));

            // レア（5個）- 中級コンボ対応
            attachments.Add(CreateAttachment(5, "ヘビーバレル", AttachmentRarity.Rare, AttachmentCategory.Attack,
                "武器攻撃力+30%", "重装型の攻撃強化バレル",
                AttachmentEffectType.WeaponPowerBoost, 0.30f, 0, "炎氷爆発"));

            attachments.Add(CreateAttachment(6, "コンボチップ", AttachmentRarity.Rare, AttachmentCategory.Combo,
                "コンボダメージ倍率+25%", "コンボ効果を増幅する",
                AttachmentEffectType.ComboDamageBoost, 0.25f, 0, "雷撃連打"));

            attachments.Add(CreateAttachment(7, "シールドジェネレーター", AttachmentRarity.Rare, AttachmentCategory.Defense,
                "盾の反射率+25%", "防御反射能力を強化",
                AttachmentEffectType.ShieldReflection, 0.25f, 0, "シールドバッシュ"));

            attachments.Add(CreateAttachment(8, "クイックローダー", AttachmentRarity.Rare, AttachmentCategory.Utility,
                "クールダウン-1ターン", "武器の再装填速度向上",
                AttachmentEffectType.CooldownReduction, 0f, 1, "ウィンドスパイラル"));

            attachments.Add(CreateAttachment(9, "リフレクターコア", AttachmentRarity.Rare, AttachmentCategory.Defense,
                "カウンターダメージ+30%", "反撃能力を大幅強化",
                AttachmentEffectType.CounterDamageBoost, 0.30f, 0, "アーチャーボレー"));

            // エピック（3個）- 上級コンボ対応
            attachments.Add(CreateAttachment(10, "オーバークロッカー", AttachmentRarity.Epic, AttachmentCategory.Attack,
                "クリティカル倍率+50%（2倍→3倍）", "限界突破型の攻撃増幅器",
                AttachmentEffectType.CriticalDamageBoost, 0.50f, 0, "アースクラッシュ"));

            attachments.Add(CreateAttachment(11, "コンボアクセラレーター", AttachmentRarity.Epic, AttachmentCategory.Combo,
                "コンボ成功時、次ターン2回行動可能", "コンボ連携を加速させる",
                AttachmentEffectType.ComboActionGrant, 0f, 1, "属性循環"));

            attachments.Add(CreateAttachment(12, "バリアフィールド", AttachmentRarity.Epic, AttachmentCategory.Utility,
                "ターン開始時HP5%回復", "自動回復バリアを展開",
                AttachmentEffectType.TurnStartHeal, 0.05f, 0, "ホーリーレイジ"));

            // レジェンダリー（3個）- 超上級コンボ対応
            attachments.Add(CreateAttachment(13, "デストロイヤーモード", AttachmentRarity.Legendary, AttachmentCategory.Attack,
                "HP50%以下時、全ダメージ+100%", "危機状態で真の力を発揮",
                AttachmentEffectType.ConditionalPowerBoost, 1.00f, 0, "ダークネスボイド"));

            attachments.Add(CreateAttachment(14, "フェニックスコア", AttachmentRarity.Legendary, AttachmentCategory.Utility,
                "戦闘中1回だけ、HP0時に50%で復活", "不死鳥の力を宿すコア",
                AttachmentEffectType.SpecialAbility, 0.50f, 1, "エレメンタルマスタリー"));

            attachments.Add(CreateAttachment(15, "オムニコンボ", AttachmentRarity.Legendary, AttachmentCategory.Combo,
                "任意の装備でコンボ継続可能", "全ての武器を統合制御",
                AttachmentEffectType.ComboSimplification, 1.00f, 0, "オールウェポンアサルト"));

            presetAttachments = attachments.ToArray();
        }

        /// <summary>
        /// アタッチメント作成ヘルパー（コンボ名付き）
        /// </summary>
        private AttachmentData CreateAttachment(int id, string name, AttachmentRarity rarity, 
            AttachmentCategory category, string description, string flavorText,
            AttachmentEffectType effectType, float effectValue, int flatValue, string comboName)
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
                associatedComboName = comboName, // 対応コンボ名を追加
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

        /// <summary>
        /// レアリティ別アタッチメント取得
        /// </summary>
        public AttachmentData[] GetAttachmentsByRarity(AttachmentRarity rarity)
        {
            return presetAttachments.Where(a => a.rarity == rarity).ToArray();
        }

        /// <summary>
        /// カテゴリ別アタッチメント取得
        /// </summary>
        public AttachmentData[] GetAttachmentsByCategory(AttachmentCategory category)
        {
            return presetAttachments.Where(a => a.category == category).ToArray();
        }

        /// <summary>
        /// コンボ名でアタッチメント取得
        /// </summary>
        public AttachmentData[] GetAttachmentsByCombo(string comboName)
        {
            return presetAttachments.Where(a => a.associatedComboName == comboName).ToArray();
        }

        /// <summary>
        /// アタッチメント取得
        /// </summary>
        public AttachmentData GetAttachment(int attachmentId)
        {
            return presetAttachments.FirstOrDefault(a => a.attachmentId == attachmentId);
        }

        /// <summary>
        /// ランダムレアリティ決定
        /// </summary>
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

        /// <summary>
        /// ランダムアタッチメント生成
        /// </summary>
        public AttachmentData GetRandomAttachment(AttachmentRarity? forceRarity = null)
        {
            AttachmentRarity rarity = forceRarity ?? GetRandomRarity();
            AttachmentData[] candidateAttachments = GetAttachmentsByRarity(rarity);
            
            if (candidateAttachments.Length == 0)
                return null;
            
            return candidateAttachments[UnityEngine.Random.Range(0, candidateAttachments.Length)];
        }

        /// <summary>
        /// 全アタッチメントとコンボの対応表を取得（デバッグ用）
        /// </summary>
        public Dictionary<string, string> GetAttachmentComboMapping()
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>();
            foreach (AttachmentData attachment in presetAttachments)
            {
                mapping[attachment.attachmentName] = attachment.associatedComboName ?? "未割り当て";
            }
            return mapping;
        }
    }
}