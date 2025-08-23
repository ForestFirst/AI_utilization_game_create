using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem.Combat
{
    /// <summary>
    /// ダメージ計算情報
    /// </summary>
    [Serializable]
    public struct DamageCalculationInfo
    {
        [Header("ダメージ内訳")]
        public int baseDamage;          // 基本ダメージ
        public float comboMultiplier;   // コンボ倍率
        public int comboDamage;         // コンボ追加ダメージ
        public float otherMultiplier;   // その他効果倍率
        public int otherDamage;         // その他追加ダメージ
        public int finalDamage;         // 最終ダメージ

        [Header("詳細情報")]
        public string calculationDetails; // 計算詳細
        public string cardName;           // カード名
        public bool isCritical;           // クリティカルヒット
        public float criticalMultiplier;  // クリティカル倍率

        /// <summary>
        /// 計算詳細の説明文を取得
        /// </summary>
        /// <returns>詳細説明文</returns>
        public readonly string GetDetailDescription()
        {
            if (!string.IsNullOrEmpty(calculationDetails))
                return calculationDetails;

            var parts = new List<string>();
            
            if (baseDamage > 0)
                parts.Add($"基本: {baseDamage}");
            
            if (comboDamage > 0)
                parts.Add($"コンボ: +{comboDamage} (x{comboMultiplier:F1})");
            
            if (otherDamage > 0)
                parts.Add($"装備効果: +{otherDamage} (x{otherMultiplier:F1})");

            if (isCritical)
                parts.Add($"クリティカル: x{criticalMultiplier:F1}");
            
            var detailDescription = $"{cardName}: {string.Join(", ", parts)} = {finalDamage}ダメージ";
            
            if (isCritical)
                detailDescription += " (CRITICAL!)";
                
            return detailDescription;
        }
    }

    /// <summary>
    /// 戦闘ダメージ計算を担当するクラス
    /// カードダメージ、コンボ効果、装備効果などの計算を行う
    /// </summary>
    public class BattleDamageCalculator : MonoBehaviour
    {
        [Header("ダメージ計算設定")]
        [SerializeField] private float baseCriticalChance = 0.05f; // 基本クリティカル率
        [SerializeField] private float baseCriticalMultiplier = 1.5f; // 基本クリティカル倍率
        [SerializeField] private bool enableRandomVariation = true; // ランダム変動
        [SerializeField] private float damageVariationRange = 0.1f; // ダメージ変動幅

        // システム参照
        private ComboSystem comboSystem;
        private AttachmentSystem attachmentSystem;

        #region Initialization

        /// <summary>
        /// ダメージ計算機の初期化
        /// </summary>
        /// <param name="combo">コンボシステム</param>
        /// <param name="attachment">装備システム</param>
        public void Initialize(ComboSystem combo = null, AttachmentSystem attachment = null)
        {
            comboSystem = combo;
            attachmentSystem = attachment;

            Debug.Log("[BattleDamageCalculator] Initialized");
        }

        #endregion

        #region Main Damage Calculation

        /// <summary>
        /// カードダメージを計算
        /// </summary>
        /// <param name="card">使用するカード</param>
        /// <param name="player">プレイヤーデータ</param>
        /// <param name="target">攻撃対象（オプション）</param>
        /// <returns>ダメージ計算情報</returns>
        public DamageCalculationInfo CalculateCardDamage(CardData card, PlayerData player, object target = null)
        {
            if (card == null)
            {
                return CreateEmptyCalculationInfo("無効なカード");
            }

            var calculation = new DamageCalculationInfo
            {
                cardName = card.displayName,
                baseDamage = card.weaponData.basePower
            };

            // コンボ効果の計算
            CalculateComboEffects(ref calculation, card);

            // 装備効果の計算
            CalculateAttachmentEffects(ref calculation, card, player);

            // クリティカル判定
            CalculateCriticalHit(ref calculation, player);

            // ランダム変動の適用
            ApplyRandomVariation(ref calculation);

            // 最終ダメージの計算
            CalculateFinalDamage(ref calculation);

            // 計算詳細の生成
            calculation.calculationDetails = calculation.GetDetailDescription();

            return calculation;
        }

        /// <summary>
        /// 複数敵への範囲ダメージを計算
        /// </summary>
        /// <param name="card">使用するカード</param>
        /// <param name="player">プレイヤーデータ</param>
        /// <param name="targets">攻撃対象リスト</param>
        /// <returns>対象別ダメージ計算情報</returns>
        public Dictionary<object, DamageCalculationInfo> CalculateAreaDamage(
            CardData card, PlayerData player, List<object> targets)
        {
            var results = new Dictionary<object, DamageCalculationInfo>();

            if (targets == null || targets.Count == 0)
                return results;

            // 範囲攻撃の基本ダメージ計算
            var baseDamageInfo = CalculateCardDamage(card, player);

            // 各対象に対してダメージを計算
            foreach (var target in targets)
            {
                var targetDamage = baseDamageInfo;
                
                // 対象固有の効果を適用
                ApplyTargetSpecificEffects(ref targetDamage, target);
                
                results[target] = targetDamage;
            }

            return results;
        }

        #endregion

        #region Effect Calculations

        /// <summary>
        /// コンボ効果の計算
        /// </summary>
        /// <param name="calculation">計算情報</param>
        /// <param name="card">使用カード</param>
        private void CalculateComboEffects(ref DamageCalculationInfo calculation, CardData card)
        {
            if (comboSystem == null)
            {
                calculation.comboMultiplier = 1.0f;
                calculation.comboDamage = 0;
                return;
            }

            // 現在のコンボ進行状況から倍率を取得（仮実装）
            // 実際のComboSystemの実装に応じて修正が必要
            calculation.comboMultiplier = 1.0f;
            calculation.comboDamage = 0;
            
            // コンボシステムが利用可能な場合の処理
            // 実装は具体的なComboSystemのAPIに依存
        }

        /// <summary>
        /// 装備効果の計算
        /// </summary>
        /// <param name="calculation">計算情報</param>
        /// <param name="card">使用カード</param>
        /// <param name="player">プレイヤーデータ</param>
        private void CalculateAttachmentEffects(ref DamageCalculationInfo calculation, CardData card, PlayerData player)
        {
            calculation.otherMultiplier = 1.0f;
            calculation.otherDamage = 0;

            if (attachmentSystem == null || player == null)
                return;

            // 装備からダメージ修正を取得（仮実装）
            // 実際のAttachmentSystemの実装に応じて修正が必要
            // var attachmentEffects = attachmentSystem.GetDamageModifiers(card.weaponType);
            
            // 装備効果による修正（仮の値）
            calculation.otherMultiplier = 1.0f;
            calculation.otherDamage = 0;

            // その他修正の計算
            if (calculation.otherMultiplier != 1.0f)
            {
                var additionalDamage = Mathf.RoundToInt(
                    calculation.baseDamage * (calculation.otherMultiplier - 1.0f)
                );
                calculation.otherDamage += additionalDamage;
            }
        }

        /// <summary>
        /// クリティカルヒットの計算
        /// </summary>
        /// <param name="calculation">計算情報</param>
        /// <param name="player">プレイヤーデータ</param>
        private void CalculateCriticalHit(ref DamageCalculationInfo calculation, PlayerData player)
        {
            // クリティカル率の計算
            float criticalChance = baseCriticalChance;
            
            if (player != null)
            {
                // プレイヤーの幸運値などからクリティカル率を修正
                criticalChance += player.luckModifier * 0.01f;
            }

            // クリティカル判定
            calculation.isCritical = UnityEngine.Random.value < criticalChance;
            
            if (calculation.isCritical)
            {
                calculation.criticalMultiplier = baseCriticalMultiplier;
                
                // プレイヤーのクリティカル倍率修正
                if (player != null)
                {
                    calculation.criticalMultiplier += player.criticalDamageModifier;
                }
            }
            else
            {
                calculation.criticalMultiplier = 1.0f;
            }
        }

        /// <summary>
        /// ランダム変動の適用
        /// </summary>
        /// <param name="calculation">計算情報</param>
        private void ApplyRandomVariation(ref DamageCalculationInfo calculation)
        {
            if (!enableRandomVariation || damageVariationRange <= 0)
                return;

            var variationFactor = UnityEngine.Random.Range(
                1.0f - damageVariationRange,
                1.0f + damageVariationRange
            );

            calculation.baseDamage = Mathf.RoundToInt(calculation.baseDamage * variationFactor);
        }

        /// <summary>
        /// 対象固有効果の適用
        /// </summary>
        /// <param name="calculation">計算情報</param>
        /// <param name="target">攻撃対象</param>
        private void ApplyTargetSpecificEffects(ref DamageCalculationInfo calculation, object target)
        {
            // 敵の防御力や耐性を考慮
            if (target is EnemyInstance enemy)
            {
                ApplyEnemyDefenseEffects(ref calculation, enemy);
            }
            // ゲートへの攻撃の場合
            else if (target is GateData gate)
            {
                ApplyGateDefenseEffects(ref calculation, gate);
            }
        }

        /// <summary>
        /// 敵の防御効果を適用
        /// </summary>
        /// <param name="calculation">計算情報</param>
        /// <param name="enemy">敵インスタンス</param>
        private void ApplyEnemyDefenseEffects(ref DamageCalculationInfo calculation, EnemyInstance enemy)
        {
            // 敵の防御力による軽減
            var defenseReduction = enemy.GetEffectiveDefense();
            calculation.baseDamage = Mathf.Max(1, calculation.baseDamage - defenseReduction);

            // 敵のバフ・デバフ効果
            var damageReduction = enemy.GetBuffMultiplier("defense");
            if (damageReduction != 1.0f)
            {
                calculation.otherMultiplier *= (2.0f - damageReduction); // 防御バフは逆数的に作用
            }
        }

        /// <summary>
        /// ゲートの防御効果を適用
        /// </summary>
        /// <param name="calculation">計算情報</param>
        /// <param name="gate">ゲートデータ</param>
        private void ApplyGateDefenseEffects(ref DamageCalculationInfo calculation, GateData gate)
        {
            // ゲートタイプによる防御効果
            var defenseMultiplier = gate.gateType switch
            {
                GateType.Fortress => 0.5f,  // 要塞型は50%軽減
                GateType.Elite => 0.8f,     // エリート型は20%軽減
                _ => 1.0f
            };

            calculation.otherMultiplier *= defenseMultiplier;
        }

        #endregion

        #region Final Calculation

        /// <summary>
        /// 最終ダメージの計算
        /// </summary>
        /// <param name="calculation">計算情報</param>
        private void CalculateFinalDamage(ref DamageCalculationInfo calculation)
        {
            // 基本ダメージ + コンボダメージ + その他ダメージ
            var totalDamage = calculation.baseDamage + calculation.comboDamage + calculation.otherDamage;

            // 全体的な倍率を適用
            totalDamage = Mathf.RoundToInt(totalDamage * calculation.otherMultiplier);

            // クリティカル倍率を適用
            if (calculation.isCritical)
            {
                totalDamage = Mathf.RoundToInt(totalDamage * calculation.criticalMultiplier);
            }

            calculation.finalDamage = Math.Max(1, totalDamage); // 最低1ダメージ
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 空の計算情報を作成
        /// </summary>
        /// <param name="reason">理由</param>
        /// <returns>空の計算情報</returns>
        private DamageCalculationInfo CreateEmptyCalculationInfo(string reason)
        {
            return new DamageCalculationInfo
            {
                cardName = reason,
                baseDamage = 0,
                comboMultiplier = 1.0f,
                comboDamage = 0,
                otherMultiplier = 1.0f,
                otherDamage = 0,
                finalDamage = 0,
                isCritical = false,
                criticalMultiplier = 1.0f,
                calculationDetails = reason
            };
        }

        /// <summary>
        /// ダメージタイプによる修正を取得
        /// </summary>
        /// <param name="damageType">ダメージタイプ</param>
        /// <param name="target">攻撃対象</param>
        /// <returns>ダメージ修正倍率</returns>
        public float GetDamageTypeModifier(string damageType, object target)
        {
            // 実装例：火炎ダメージは氷敵に効果的など
            if (target is EnemyInstance enemy)
            {
                return GetEnemyTypeEffectiveness(damageType, enemy.Category);
            }

            return 1.0f;
        }

        /// <summary>
        /// 敵タイプに対する効果を取得
        /// </summary>
        /// <param name="damageType">ダメージタイプ</param>
        /// <param name="enemyCategory">敵カテゴリ</param>
        /// <returns>効果倍率</returns>
        private float GetEnemyTypeEffectiveness(string damageType, EnemyCategory enemyCategory)
        {
            // タイプ相性システム（例）
            return (damageType, enemyCategory) switch
            {
                ("fire", EnemyCategory.Support) => 1.5f,     // 炎は支援型に効果的
                ("ice", EnemyCategory.Attacker) => 1.3f,     // 氷は攻撃型に効果的
                ("lightning", EnemyCategory.Special) => 1.8f, // 雷は特殊型に効果的
                _ => 1.0f
            };
        }

        #endregion

        #region Debug

        /// <summary>
        /// ダメージ計算のログ出力
        /// </summary>
        /// <param name="calculation">計算情報</param>
        public void LogDamageCalculation(DamageCalculationInfo calculation)
        {
            Debug.Log($"[BattleDamageCalculator] {calculation.GetDetailDescription()}");
        }

        #endregion
    }
}