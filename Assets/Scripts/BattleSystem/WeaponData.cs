using System;
using UnityEngine;

namespace BattleSystem
{
    // 攻撃属性の定義
    public enum AttackAttribute
    {
        Fire,    // 炎：継続ダメージ系
        Ice,     // 氷：行動阻害系
        Thunder, // 雷：連鎖・麻痺系
        Wind,    // 風：範囲・移動系
        Earth,   // 土：爆発・物理系
        Light,   // 光：回復・支援系
        Dark,    // 闇：デバフ・妨害系
        None     // 無：属性なし・汎用系
    }

    // 武器属性の定義
    public enum WeaponType
    {
        Sword,   // 剣：近接バランス型
        Axe,     // 斧：近接高威力型
        Spear,   // 槍：中距離貫通型
        Bow,     // 弓：遠距離精密型
        Gun,     // 銃：遠距離火力型
        Shield,  // 盾：防御・カウンター型
        Magic,   // 魔法：特殊効果型
        Tool     // 道具：ユーティリティ型
    }

    // 攻撃範囲の種類
    public enum AttackRange
    {
        SingleFront,    // 一番前の敵
        SingleTarget,   // 任意の単体
        Row1,           // 1列目全体
        Row2,           // 2列目全体
        Column,         // 縦列貫通
        All,            // 全体攻撃
        Self            // 自分
    }

    // 武器データの基本構造
    [Serializable]
    public class WeaponData
    {
        [Header("基本情報")]
        public string weaponName;
        public AttackAttribute attackAttribute;
        public WeaponType weaponType;
        
        [Header("戦闘パラメータ")]
        public int basePower;           // 基本攻撃力（0-200の範囲）
        public AttackRange attackRange;
        public int criticalRate;        // クリティカル率（%）
        public int cooldownTurns;       // クールダウンターン数
        
        [Header("特殊効果")]
        public string specialEffect;   // 特殊効果の説明
        public int effectValue;        // 効果の数値
        public int effectDuration;     // 効果継続ターン数
        
        [Header("使用制限")]
        public bool canUseConsecutively; // 連続使用可能フラグ

        public WeaponData()
        {
            weaponName = "";
            attackAttribute = AttackAttribute.None;
            weaponType = WeaponType.Sword;
            basePower = 100;
            attackRange = AttackRange.SingleFront;
            criticalRate = 5;
            cooldownTurns = 0;
            specialEffect = "";
            effectValue = 0;
            effectDuration = 0;
            canUseConsecutively = true;
        }

        public WeaponData(string name, AttackAttribute attack, WeaponType weapon, int power, AttackRange range)
        {
            weaponName = name;
            attackAttribute = attack;
            weaponType = weapon;
            basePower = Mathf.Clamp(power, 0, 200);
            attackRange = range;
            criticalRate = 5;
            cooldownTurns = 0;
            specialEffect = "";
            effectValue = 0;
            effectDuration = 0;
            canUseConsecutively = true;
        }
    }

    // 武器データベース管理クラス
    [CreateAssetMenu(fileName = "WeaponDatabase", menuName = "BattleSystem/WeaponDatabase")]
    public class WeaponDatabase : ScriptableObject
    {
        [SerializeField] private WeaponData[] weapons;
        
        public WeaponData[] Weapons => weapons;
        
        public WeaponData GetWeapon(int index)
        {
            if (index >= 0 && index < weapons.Length)
                return weapons[index];
            return null;
        }
        
        public WeaponData[] GetWeaponsByAttribute(AttackAttribute attribute)
        {
            return System.Array.FindAll(weapons, weapon => weapon.attackAttribute == attribute);
        }
        
        public WeaponData[] GetWeaponsByType(WeaponType type)
        {
            return System.Array.FindAll(weapons, weapon => weapon.weaponType == type);
        }
    }
}