using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    // ダメージタイプの種類
    public enum DamageType
    {
        Physical,    // 物理ダメージ
        Magical,     // 魔法ダメージ
        True,        // 確定ダメージ（防御無視）
        Healing      // 回復
    }

    // ダメージ計算の詳細結果
    [Serializable]
    public struct DetailedDamageResult
    {
        public int baseDamage;              // 基本ダメージ
        public int weaponDamage;            // 武器ダメージ
        public int playerAttackPower;       // プレイヤー攻撃力
        public float criticalMultiplier;    // クリティカル倍率
        public float attributeMultiplier;   // 属性倍率
        public float specialMultiplier;     // 特殊効果倍率
        public int finalDamage;             // 最終ダメージ
        public bool isCritical;             // クリティカル発生フラグ
        public DamageType damageType;       // ダメージタイプ
        public AttackAttribute attackAttribute; // 攻撃属性
        public List<string> appliedEffects; // 適用された効果のリスト
    }

    // 範囲攻撃の対象情報
    [Serializable]
    public struct AttackTarget
    {
        public GridPosition position;
        public EnemyInstance enemy;
        public GateData gate;
        public bool isEnemy;
        public bool isGate;
    }

    // ダメージ計算システムクラス
    public class DamageCalculationSystem : MonoBehaviour
    {
        [Header("ダメージ計算設定")]
        [SerializeField] private float baseCriticalMultiplier = 2.0f;
        [SerializeField] private int minDamageValue = 1;
        [SerializeField] private bool enableAttributeEffects = true;
        [SerializeField] private bool enableSpecialEffects = true;

        [Header("属性効果倍率")]
        [SerializeField] private float fireEffectMultiplier = 1.2f;
        [SerializeField] private float iceEffectMultiplier = 1.0f;
        [SerializeField] private float thunderEffectMultiplier = 1.1f;
        [SerializeField] private float windEffectMultiplier = 1.0f;
        [SerializeField] private float earthEffectMultiplier = 1.3f;
        [SerializeField] private float lightEffectMultiplier = 1.0f;
        [SerializeField] private float darkEffectMultiplier = 1.1f;

        private BattleManager battleManager;

        // イベント定義
        public event Action<DetailedDamageResult> OnDamageCalculated;
        public event Action<AttackTarget[], DetailedDamageResult[]> OnRangeAttackCalculated;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
        }

        // 単体攻撃のダメージ計算
        public DetailedDamageResult CalculateWeaponDamage(WeaponData weapon, AttackTarget target)
        {
            DetailedDamageResult result = new DetailedDamageResult();
            
            if (weapon == null)
            {
                return GetZeroDamageResult();
            }

            // 基本ダメージ計算
            result.playerAttackPower = battleManager.PlayerData.baseAttackPower;
            result.weaponDamage = weapon.basePower;
            result.baseDamage = result.playerAttackPower + result.weaponDamage;
            result.attackAttribute = weapon.attackAttribute;
            result.damageType = GetDamageTypeFromWeapon(weapon);
            result.appliedEffects = new List<string>();

            // クリティカル判定
            result.isCritical = CalculateCritical(weapon);
            result.criticalMultiplier = result.isCritical ? baseCriticalMultiplier : 1.0f;

            // 属性効果計算
            result.attributeMultiplier = CalculateAttributeMultiplier(weapon, target);

            // 特殊効果計算
            result.specialMultiplier = CalculateSpecialEffects(weapon, target, result);

            // 最終ダメージ計算
            float totalMultiplier = result.criticalMultiplier * result.attributeMultiplier * result.specialMultiplier;
            result.finalDamage = Mathf.Max(minDamageValue, 
                Mathf.RoundToInt(result.baseDamage * totalMultiplier));

            OnDamageCalculated?.Invoke(result);
            return result;
        }

        // 範囲攻撃のダメージ計算
        public DetailedDamageResult[] CalculateRangeAttack(WeaponData weapon, AttackTarget[] targets)
        {
            DetailedDamageResult[] results = new DetailedDamageResult[targets.Length];

            for (int i = 0; i < targets.Length; i++)
            {
                results[i] = CalculateWeaponDamage(weapon, targets[i]);
                
                // 範囲攻撃のダメージ減衰（設定により調整可能）
                if (targets.Length > 1)
                {
                    float rangeReduction = CalculateRangeAttackReduction(weapon, targets.Length);
                    results[i].finalDamage = Mathf.RoundToInt(results[i].finalDamage * rangeReduction);
                    results[i].appliedEffects.Add($"範囲攻撃減衰: {rangeReduction:P0}");
                }
            }

            OnRangeAttackCalculated?.Invoke(targets, results);
            return results;
        }

        // 攻撃対象の取得
        public AttackTarget[] GetAttackTargets(WeaponData weapon, GridPosition targetPosition)
        {
            if (weapon == null)
                return new AttackTarget[0];

            List<AttackTarget> targets = new List<AttackTarget>();
            BattleField field = battleManager.BattleField;

            switch (weapon.attackRange)
            {
                case AttackRange.SingleFront:
                    targets.AddRange(GetSingleFrontTargets(field, targetPosition));
                    break;

                case AttackRange.SingleTarget:
                    targets.AddRange(GetSingleTargets(field, targetPosition));
                    break;

                case AttackRange.Row1:
                    targets.AddRange(GetRowTargets(field, 0));
                    break;

                case AttackRange.Row2:
                    targets.AddRange(GetRowTargets(field, 1));
                    break;

                case AttackRange.Column:
                    targets.AddRange(GetColumnTargets(field, targetPosition.x));
                    break;

                case AttackRange.All:
                    targets.AddRange(GetAllTargets(field));
                    break;

                case AttackRange.Self:
                    // 自己回復等の処理（後のフェーズで詳細実装）
                    break;
            }

            return targets.ToArray();
        }

        // クリティカル判定
        private bool CalculateCritical(WeaponData weapon)
        {
            int criticalChance = weapon.criticalRate;
            
            // プレイヤーのクリティカル率ボーナス（後のフェーズで実装）
            // criticalChance += playerData.criticalRateBonus;

            return UnityEngine.Random.Range(0, 100) < criticalChance;
        }

        // 属性効果倍率の計算
        private float CalculateAttributeMultiplier(WeaponData weapon, AttackTarget target)
        {
            if (!enableAttributeEffects)
                return 1.0f;

            float multiplier = 1.0f;

            switch (weapon.attackAttribute)
            {
                case AttackAttribute.Fire:
                    multiplier = fireEffectMultiplier;
                    if (target.isEnemy) multiplier *= GetFireEffectVsEnemy(target.enemy);
                    break;

                case AttackAttribute.Ice:
                    multiplier = iceEffectMultiplier;
                    if (target.isEnemy) multiplier *= GetIceEffectVsEnemy(target.enemy);
                    break;

                case AttackAttribute.Thunder:
                    multiplier = thunderEffectMultiplier;
                    if (target.isEnemy) multiplier *= GetThunderEffectVsEnemy(target.enemy);
                    break;

                case AttackAttribute.Wind:
                    multiplier = windEffectMultiplier;
                    break;

                case AttackAttribute.Earth:
                    multiplier = earthEffectMultiplier;
                    break;

                case AttackAttribute.Light:
                    multiplier = lightEffectMultiplier;
                    if (target.isEnemy) multiplier *= GetLightEffectVsEnemy(target.enemy);
                    break;

                case AttackAttribute.Dark:
                    multiplier = darkEffectMultiplier;
                    if (target.isEnemy) multiplier *= GetDarkEffectVsEnemy(target.enemy);
                    break;
            }

            return multiplier;
        }

        // 特殊効果の計算
        private float CalculateSpecialEffects(WeaponData weapon, AttackTarget target, DetailedDamageResult result)
        {
            if (!enableSpecialEffects)
                return 1.0f;

            float multiplier = 1.0f;
            
            // 武器固有の特殊効果処理
            if (!string.IsNullOrEmpty(weapon.specialEffect))
            {
                multiplier *= ProcessWeaponSpecialEffect(weapon, target, result);
            }

            // ゲートに対する特殊ダメージ（設計書より）
            if (target.isGate)
            {
                if (weapon.specialEffect.Contains("ゲート追加ダメージ"))
                {
                    multiplier *= 1.5f; // 50%追加ダメージ
                    result.appliedEffects.Add("ゲート特効");
                }
            }

            return multiplier;
        }

        // 武器特殊効果の処理
        private float ProcessWeaponSpecialEffect(WeaponData weapon, AttackTarget target, DetailedDamageResult result)
        {
            float multiplier = 1.0f;
            string effect = weapon.specialEffect.ToLower();

            // クリティカル率アップ効果
            if (effect.Contains("クリティカル") && result.isCritical)
            {
                multiplier *= 1.15f;
                result.appliedEffects.Add("クリティカル強化");
            }

            // 装甲貫通効果
            if (effect.Contains("装甲貫通") || effect.Contains("防御力無視"))
            {
                multiplier *= 1.2f;
                result.appliedEffects.Add("装甲貫通");
            }

            // 機械系特効
            if (effect.Contains("機械系") && target.isEnemy)
            {
                if (IsMechanicalEnemy(target.enemy))
                {
                    multiplier *= 2.0f; // 100%追加ダメージ
                    result.appliedEffects.Add("機械系特効");
                }
            }

            return multiplier;
        }

        // 範囲攻撃の減衰計算
        private float CalculateRangeAttackReduction(WeaponData weapon, int targetCount)
        {
            // 基本的な範囲攻撃減衰
            switch (weapon.attackRange)
            {
                case AttackRange.All:
                    return 0.8f; // 全体攻撃は80%
                case AttackRange.Row1:
                case AttackRange.Row2:
                    return 0.9f; // 行攻撃は90%
                case AttackRange.Column:
                    return 0.95f; // 列攻撃は95%
                default:
                    return 1.0f;
            }
        }

        // 単体前列ターゲット取得
        private List<AttackTarget> GetSingleFrontTargets(BattleField field, GridPosition targetPos)
        {
            List<AttackTarget> targets = new List<AttackTarget>();
            EnemyInstance enemy = field.GetFrontEnemyInColumn(targetPos.x);
            
            if (enemy != null)
            {
                targets.Add(CreateEnemyTarget(enemy));
            }
            
            return targets;
        }

        // 単体ターゲット取得
        private List<AttackTarget> GetSingleTargets(BattleField field, GridPosition targetPos)
        {
            List<AttackTarget> targets = new List<AttackTarget>();
            
            EnemyInstance enemy = field.GetEnemyAt(targetPos);
            if (enemy != null)
            {
                targets.Add(CreateEnemyTarget(enemy));
            }
            else if (field.CanAttackGate(targetPos.x))
            {
                GateData gate = field.Gates.Find(g => g.position.x == targetPos.x);
                if (gate != null)
                {
                    targets.Add(CreateGateTarget(gate));
                }
            }
            
            return targets;
        }

        // 行ターゲット取得
        private List<AttackTarget> GetRowTargets(BattleField field, int row)
        {
            List<AttackTarget> targets = new List<AttackTarget>();
            List<EnemyInstance> enemies = field.GetEnemiesInRow(row);
            
            foreach (EnemyInstance enemy in enemies)
            {
                targets.Add(CreateEnemyTarget(enemy));
            }
            
            return targets;
        }

        // 列ターゲット取得
        private List<AttackTarget> GetColumnTargets(BattleField field, int column)
        {
            List<AttackTarget> targets = new List<AttackTarget>();
            List<EnemyInstance> enemies = field.GetEnemiesInColumn(column);
            
            foreach (EnemyInstance enemy in enemies)
            {
                targets.Add(CreateEnemyTarget(enemy));
            }
            
            // 列に敵がいない場合はゲートを攻撃
            if (enemies.Count == 0 && field.CanAttackGate(column))
            {
                GateData gate = field.Gates.Find(g => g.position.x == column);
                if (gate != null)
                {
                    targets.Add(CreateGateTarget(gate));
                }
            }
            
            return targets;
        }

        // 全体ターゲット取得
        private List<AttackTarget> GetAllTargets(BattleField field)
        {
            List<AttackTarget> targets = new List<AttackTarget>();
            List<EnemyInstance> enemies = field.GetAllEnemies();
            
            foreach (EnemyInstance enemy in enemies)
            {
                targets.Add(CreateEnemyTarget(enemy));
            }
            
            return targets;
        }

        // 敵ターゲット作成
        private AttackTarget CreateEnemyTarget(EnemyInstance enemy)
        {
            return new AttackTarget
            {
                position = new GridPosition(enemy.gridX, enemy.gridY),
                enemy = enemy,
                gate = null,
                isEnemy = true,
                isGate = false
            };
        }

        // ゲートターゲット作成
        private AttackTarget CreateGateTarget(GateData gate)
        {
            return new AttackTarget
            {
                position = gate.position,
                enemy = null,
                gate = gate,
                isEnemy = false,
                isGate = true
            };
        }

        // 属性効果の個別実装
        private float GetFireEffectVsEnemy(EnemyInstance enemy)
        {
            // 機械系敵に対する炎の効果
            if (IsMechanicalEnemy(enemy))
                return 1.2f;
            return 1.0f;
        }

        private float GetIceEffectVsEnemy(EnemyInstance enemy)
        {
            // 移動系敵に対する氷の効果
            return 1.0f;
        }

        private float GetThunderEffectVsEnemy(EnemyInstance enemy)
        {
            // 機械系敵に対する雷の効果
            if (IsMechanicalEnemy(enemy))
                return 1.3f;
            return 1.0f;
        }

        private float GetLightEffectVsEnemy(EnemyInstance enemy)
        {
            // 闇系敵に対する光の効果
            return 1.0f;
        }

        private float GetDarkEffectVsEnemy(EnemyInstance enemy)
        {
            // 光系敵に対する闇の効果
            return 1.0f;
        }

        // 機械系敵判定
        private bool IsMechanicalEnemy(EnemyInstance enemy)
        {
            if (enemy?.enemyData == null)
                return false;
            
            string enemyName = enemy.enemyData.enemyName.ToLower();
            return enemyName.Contains("ドローン") || enemyName.Contains("ロボ") || 
                   enemyName.Contains("ユニット") || enemyName.Contains("ボット");
        }

        // 武器からダメージタイプを取得
        private DamageType GetDamageTypeFromWeapon(WeaponData weapon)
        {
            switch (weapon.weaponType)
            {
                case WeaponType.Magic:
                    return DamageType.Magical;
                case WeaponType.Shield:
                    return weapon.attackAttribute == AttackAttribute.Light ? DamageType.Healing : DamageType.Physical;
                default:
                    return DamageType.Physical;
            }
        }

        // ゼロダメージ結果の生成
        private DetailedDamageResult GetZeroDamageResult()
        {
            return new DetailedDamageResult
            {
                baseDamage = 0,
                weaponDamage = 0,
                playerAttackPower = 0,
                criticalMultiplier = 1.0f,
                attributeMultiplier = 1.0f,
                specialMultiplier = 1.0f,
                finalDamage = 0,
                isCritical = false,
                damageType = DamageType.Physical,
                attackAttribute = AttackAttribute.None,
                appliedEffects = new List<string>()
            };
        }
    }
}