using System.Collections.Generic;

namespace BattleSystem.Combat
{
    /// <summary>
    /// ダメージ計算機能を提供するインターフェース
    /// </summary>
    public interface IDamageCalculator
    {
        /// <summary>
        /// カードのダメージプレビューを計算します
        /// </summary>
        /// <param name="card">対象カード</param>
        /// <returns>ダメージプレビュー情報</returns>
        DamagePreviewInfo CalculatePreviewDamage(CardData card);
        
        /// <summary>
        /// 実際のダメージを計算します
        /// </summary>
        /// <param name="card">使用カード</param>
        /// <returns>ダメージ計算結果</returns>
        DamageCalculationResult CalculateDamage(CardData card);
        
        /// <summary>
        /// コンボ効果を含むダメージを計算します
        /// </summary>
        /// <param name="card">使用カード</param>
        /// <param name="comboMultiplier">コンボ倍率</param>
        /// <returns>コンボ効果込みダメージ計算結果</returns>
        DamageCalculationResult CalculateDamageWithCombo(CardData card, float comboMultiplier);
        
        /// <summary>
        /// 基本ダメージを取得します
        /// </summary>
        /// <param name="weaponData">武器データ</param>
        /// <returns>基本ダメージ</returns>
        int GetBaseDamage(WeaponData weaponData);
    }
    
    /// <summary>
    /// ダメージプレビュー情報
    /// </summary>
    public class DamagePreviewInfo
    {
        public CardData UsedCard { get; set; }
        public int PrimaryDamage { get; set; }
        public int ComboDamage { get; set; }
        public int TotalDamage { get; set; }
        public List<EnemyInstance> TargetEnemies { get; set; }
        public List<GateData> TargetGates { get; set; }
        public string Description { get; set; }
        public bool HasComboEffect { get; set; }
        public float ComboMultiplier { get; set; }
        
        public DamagePreviewInfo()
        {
            TargetEnemies = new List<EnemyInstance>();
            TargetGates = new List<GateData>();
        }
    }
    
    /// <summary>
    /// ダメージ計算結果
    /// </summary>
    public class DamageCalculationResult
    {
        public bool IsSuccess { get; set; }
        public int TotalDamage { get; set; }
        public List<EnemyInstance> HitEnemies { get; set; }
        public List<GateData> HitGates { get; set; }
        public string ErrorMessage { get; set; }
        public bool HasValidTargets { get; set; }
        
        public DamageCalculationResult()
        {
            HitEnemies = new List<EnemyInstance>();
            HitGates = new List<GateData>();
        }
    }
}