using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem.Combat
{
    /// <summary>
    /// ダメージ計算を担当するクラス
    /// 単一責任原則に従い、ダメージ計算のみを処理
    /// </summary>
    public class DamageCalculator : IDamageCalculator
    {
        private readonly BattleManager battleManager;
        private readonly BattleField battleField;
        private readonly ComboSystem comboSystem;
        
        public DamageCalculator(BattleManager battleManager, BattleField battleField, ComboSystem comboSystem)
        {
            this.battleManager = battleManager ?? throw new ArgumentNullException(nameof(battleManager));
            this.battleField = battleField ?? throw new ArgumentNullException(nameof(battleField));
            this.comboSystem = comboSystem;
        }
        
        /// <summary>
        /// カードのダメージプレビューを計算します
        /// </summary>
        public DamagePreviewInfo CalculatePreviewDamage(CardData card)
        {
            if (card?.weaponData == null)
            {
                return null;
            }
            
            var previewInfo = new DamagePreviewInfo
            {
                UsedCard = card,
                PrimaryDamage = GetBaseDamage(card.weaponData)
            };
            
            // コンボ効果の計算
            if (comboSystem != null)
            {
                var comboMultiplier = CalculateComboMultiplier(card);
                previewInfo.ComboMultiplier = comboMultiplier;
                previewInfo.HasComboEffect = comboMultiplier > 1.0f;
                previewInfo.ComboDamage = Mathf.RoundToInt(previewInfo.PrimaryDamage * comboMultiplier) - previewInfo.PrimaryDamage;
            }
            
            previewInfo.TotalDamage = previewInfo.PrimaryDamage + previewInfo.ComboDamage;
            
            // ターゲットの計算
            CalculateTargets(card, previewInfo);
            
            // 説明文の生成
            GenerateDescription(previewInfo);
            
            return previewInfo;
        }
        
        /// <summary>
        /// 実際のダメージを計算します
        /// </summary>
        public DamageCalculationResult CalculateDamage(CardData card)
        {
            return CalculateDamageWithCombo(card, 1.0f);
        }
        
        /// <summary>
        /// コンボ効果を含むダメージを計算します
        /// </summary>
        public DamageCalculationResult CalculateDamageWithCombo(CardData card, float comboMultiplier)
        {
            var result = new DamageCalculationResult();
            
            if (card?.weaponData == null)
            {
                result.ErrorMessage = "無効なカードまたは武器データです";
                return result;
            }
            
            try
            {
                var baseDamage = GetBaseDamage(card.weaponData);
                var totalDamage = Mathf.RoundToInt(baseDamage * comboMultiplier);
                
                result.TotalDamage = totalDamage;
                result.HasValidTargets = CalculateHitTargets(card, result);
                result.IsSuccess = result.HasValidTargets;
                
                if (!result.IsSuccess)
                {
                    result.ErrorMessage = "有効なターゲットが見つかりません";
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"ダメージ計算エラー: {ex.Message}";
                Debug.LogError($"DamageCalculator Error: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 基本ダメージを取得します
        /// </summary>
        public int GetBaseDamage(WeaponData weaponData)
        {
            if (weaponData == null || battleManager?.PlayerData == null)
            {
                return 0;
            }
            
            return battleManager.PlayerData.baseAttackPower + weaponData.basePower;
        }
        
        /// <summary>
        /// コンボ倍率を計算
        /// </summary>
        private float CalculateComboMultiplier(CardData card)
        {
            if (comboSystem == null || card?.weaponData == null)
            {
                return 1.0f;
            }
            
            try
            {
                // ComboSystemから現在のコンボ効果を取得
                var activeCombo = comboSystem.GetActiveComboForWeapon(card.weaponData.weaponName);
                if (activeCombo != null)
                {
                    return activeCombo.damageMultiplier;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"コンボ倍率計算エラー: {ex.Message}");
            }
            
            return 1.0f;
        }
        
        /// <summary>
        /// プレビュー用ターゲットの計算
        /// </summary>
        private void CalculateTargets(CardData card, DamagePreviewInfo previewInfo)
        {
            var weapon = card.weaponData;
            
            switch (weapon.attackRange)
            {
                case AttackRange.All:
                    CalculateAllTargetsPreview(previewInfo);
                    break;
                    
                case AttackRange.Column:
                    CalculateColumnTargetsPreview(card.targetColumn, previewInfo);
                    break;
                    
                case AttackRange.Row1:
                case AttackRange.Row2:
                    CalculateRowTargetsPreview(weapon, previewInfo);
                    break;
                    
                default:
                    CalculateSingleTargetPreview(card, previewInfo);
                    break;
            }
        }
        
        /// <summary>
        /// 実際の攻撃ターゲットの計算
        /// </summary>
        private bool CalculateHitTargets(CardData card, DamageCalculationResult result)
        {
            var weapon = card.weaponData;
            bool hasTargets = false;
            
            switch (weapon.attackRange)
            {
                case AttackRange.All:
                    hasTargets = CalculateAllTargetsHit(result);
                    break;
                    
                case AttackRange.Column:
                    hasTargets = CalculateColumnTargetsHit(card.targetColumn, result);
                    break;
                    
                case AttackRange.Row1:
                case AttackRange.Row2:
                    hasTargets = CalculateRowTargetsHit(weapon, result);
                    break;
                    
                default:
                    hasTargets = CalculateSingleTargetHit(card, result);
                    break;
            }
            
            return hasTargets;
        }
        
        #region プレビュー用ターゲット計算メソッド
        
        private void CalculateAllTargetsPreview(DamagePreviewInfo previewInfo)
        {
            var allEnemies = GetAllEnemies();
            foreach (var enemy in allEnemies)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    previewInfo.TargetEnemies.Add(enemy);
                }
            }
        }
        
        private void CalculateColumnTargetsPreview(int columnIndex, DamagePreviewInfo previewInfo)
        {
            var enemiesInColumn = GetEnemiesInColumn(columnIndex);
            foreach (var enemy in enemiesInColumn)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    previewInfo.TargetEnemies.Add(enemy);
                }
            }
        }
        
        private void CalculateRowTargetsPreview(WeaponData weapon, DamagePreviewInfo previewInfo)
        {
            int targetRow = (weapon.attackRange == AttackRange.Row1) ? 0 : 1;
            var enemiesInRow = GetEnemiesInRow(targetRow);
            foreach (var enemy in enemiesInRow)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    previewInfo.TargetEnemies.Add(enemy);
                }
            }
        }
        
        private void CalculateSingleTargetPreview(CardData card, DamagePreviewInfo previewInfo)
        {
            var frontEnemy = GetFrontEnemyInColumn(card.targetColumn);
            if (frontEnemy != null && frontEnemy.IsAlive())
            {
                previewInfo.TargetEnemies.Add(frontEnemy);
            }
        }
        
        #endregion
        
        #region 実際の攻撃ターゲット計算メソッド
        
        private bool CalculateAllTargetsHit(DamageCalculationResult result)
        {
            var allEnemies = GetAllEnemies();
            bool hasTargets = false;
            
            foreach (var enemy in allEnemies)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    result.HitEnemies.Add(enemy);
                    hasTargets = true;
                }
            }
            
            return hasTargets;
        }
        
        private bool CalculateColumnTargetsHit(int columnIndex, DamageCalculationResult result)
        {
            var enemiesInColumn = GetEnemiesInColumn(columnIndex);
            bool hasTargets = false;
            
            foreach (var enemy in enemiesInColumn)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    result.HitEnemies.Add(enemy);
                    hasTargets = true;
                }
            }
            
            return hasTargets;
        }
        
        private bool CalculateRowTargetsHit(WeaponData weapon, DamageCalculationResult result)
        {
            int targetRow = (weapon.attackRange == AttackRange.Row1) ? 0 : 1;
            var enemiesInRow = GetEnemiesInRow(targetRow);
            bool hasTargets = false;
            
            foreach (var enemy in enemiesInRow)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    result.HitEnemies.Add(enemy);
                    hasTargets = true;
                }
            }
            
            return hasTargets;
        }
        
        private bool CalculateSingleTargetHit(CardData card, DamageCalculationResult result)
        {
            var frontEnemy = GetFrontEnemyInColumn(card.targetColumn);
            if (frontEnemy != null && frontEnemy.IsAlive())
            {
                result.HitEnemies.Add(frontEnemy);
                return true;
            }
            
            return false;
        }
        
        #endregion
        
        #region ヘルパーメソッド
        
        /// <summary>
        /// 説明文を生成
        /// </summary>
        private void GenerateDescription(DamagePreviewInfo previewInfo)
        {
            var description = $"{previewInfo.UsedCard.displayName}: {previewInfo.PrimaryDamage}ダメージ";
            
            if (previewInfo.HasComboEffect)
            {
                description += $" + コンボ{previewInfo.ComboDamage} = {previewInfo.TotalDamage}ダメージ";
            }
            
            var targetCount = previewInfo.TargetEnemies.Count + previewInfo.TargetGates.Count;
            if (targetCount > 1)
            {
                description += $" ({targetCount}体のターゲット)";
            }
            
            previewInfo.Description = description;
        }
        
        /// <summary>
        /// 全敵の取得
        /// </summary>
        private List<EnemyInstance> GetAllEnemies()
        {
            try
            {
                return battleField?.GetAllEnemies() ?? new List<EnemyInstance>();
            }
            catch
            {
                return new List<EnemyInstance>();
            }
        }
        
        /// <summary>
        /// 列の敵取得
        /// </summary>
        private List<EnemyInstance> GetEnemiesInColumn(int columnIndex)
        {
            try
            {
                return battleField?.GetEnemiesInColumn(columnIndex) ?? new List<EnemyInstance>();
            }
            catch
            {
                return new List<EnemyInstance>();
            }
        }
        
        /// <summary>
        /// 行の敵取得
        /// </summary>
        private List<EnemyInstance> GetEnemiesInRow(int rowIndex)
        {
            try
            {
                return battleField?.GetEnemiesInRow(rowIndex) ?? new List<EnemyInstance>();
            }
            catch
            {
                return new List<EnemyInstance>();
            }
        }
        
        /// <summary>
        /// 列の先頭敵取得
        /// </summary>
        private EnemyInstance GetFrontEnemyInColumn(int columnIndex)
        {
            try
            {
                return battleField?.GetFrontEnemyInColumn(columnIndex);
            }
            catch
            {
                return null;
            }
        }
        
        #endregion
    }
}