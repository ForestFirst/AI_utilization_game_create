using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// プレイヤーの武器データ管理クラス
    /// </summary>
    [Serializable]
    public class PlayerWeaponData
    {
        [Header("武器リスト")]
        [SerializeField] public List<WeaponData> weapons = new List<WeaponData>();
        
        [Header("装備中武器")]
        [SerializeField] public int currentWeaponIndex = 0;
        
        [Header("プレイヤー修正値")]
        [SerializeField] public float damageMultiplier = 1.0f;
        [SerializeField] public float criticalChance = 0.05f;
        [SerializeField] public float criticalDamageModifier = 0.5f;
        [SerializeField] public float luckModifier = 0.0f;

        /// <summary>
        /// 現在装備中の武器を取得
        /// </summary>
        public WeaponData CurrentWeapon
        {
            get
            {
                if (weapons != null && currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
                {
                    return weapons[currentWeaponIndex];
                }
                return null;
            }
        }

        /// <summary>
        /// 武器を追加
        /// </summary>
        /// <param name="weapon">追加する武器</param>
        public void AddWeapon(WeaponData weapon)
        {
            if (weapon != null && weapons != null)
            {
                weapons.Add(weapon);
            }
        }

        /// <summary>
        /// 武器を削除
        /// </summary>
        /// <param name="index">削除する武器のインデックス</param>
        /// <returns>削除に成功したかどうか</returns>
        public bool RemoveWeapon(int index)
        {
            if (weapons != null && index >= 0 && index < weapons.Count)
            {
                weapons.RemoveAt(index);
                
                // 現在装備中の武器が削除された場合の調整
                if (currentWeaponIndex >= weapons.Count)
                {
                    currentWeaponIndex = Math.Max(0, weapons.Count - 1);
                }
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// 装備武器を変更
        /// </summary>
        /// <param name="index">装備する武器のインデックス</param>
        /// <returns>変更に成功したかどうか</returns>
        public bool EquipWeapon(int index)
        {
            if (weapons != null && index >= 0 && index < weapons.Count)
            {
                currentWeaponIndex = index;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 指定した武器タイプの武器を取得
        /// </summary>
        /// <param name="weaponType">武器タイプ</param>
        /// <returns>該当する武器のリスト</returns>
        public List<WeaponData> GetWeaponsByType(string weaponType)
        {
            var result = new List<WeaponData>();
            
            if (weapons != null)
            {
                foreach (var weapon in weapons)
                {
                    if (weapon != null && weapon.weaponType == weaponType)
                    {
                        result.Add(weapon);
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 武器数を取得
        /// </summary>
        public int WeaponCount => weapons?.Count ?? 0;

        /// <summary>
        /// 有効な武器があるかチェック
        /// </summary>
        /// <returns>有効な武器があるかどうか</returns>
        public bool HasValidWeapons()
        {
            return weapons != null && weapons.Count > 0 && CurrentWeapon != null;
        }
    }

    /// <summary>
    /// プレイヤーの基本データ
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [Header("基本ステータス")]
        [SerializeField] public int maxHp = 15000;
        [SerializeField] public int currentHp = 15000;
        [SerializeField] public int maxMana = 50;
        [SerializeField] public int currentMana = 50;
        [SerializeField] public int baseAttackPower = 100;

        [Header("戦闘修正値")]
        [SerializeField] public float damageMultiplier = 1.0f;
        [SerializeField] public float defenseMultiplier = 1.0f;
        [SerializeField] public float criticalChance = 0.05f;
        [SerializeField] public float criticalDamageModifier = 0.5f;
        [SerializeField] public float luckModifier = 0.0f;

        [Header("武器データ")]
        [SerializeField] public PlayerWeaponData weaponData;
        [SerializeField] public WeaponData[] equippedWeapons = new WeaponData[4];
        [SerializeField] public int[] weaponCooldowns = new int[4];

        /// <summary>
        /// HPの割合を取得
        /// </summary>
        public float HpRatio => maxHp > 0 ? (float)currentHp / maxHp : 0f;

        /// <summary>
        /// マナの割合を取得
        /// </summary>
        public float ManaRatio => maxMana > 0 ? (float)currentMana / maxMana : 0f;

        /// <summary>
        /// 生存しているかチェック
        /// </summary>
        public bool IsAlive => currentHp > 0;

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <returns>実際に受けたダメージ</returns>
        public int TakeDamage(int damage)
        {
            var actualDamage = Mathf.RoundToInt(damage * defenseMultiplier);
            var oldHp = currentHp;
            currentHp = Mathf.Max(0, currentHp - actualDamage);
            return oldHp - currentHp;
        }

        /// <summary>
        /// 回復する
        /// </summary>
        /// <param name="healAmount">回復量</param>
        /// <returns>実際に回復した量</returns>
        public int Heal(int healAmount)
        {
            var oldHp = currentHp;
            currentHp = Mathf.Min(maxHp, currentHp + healAmount);
            return currentHp - oldHp;
        }

        /// <summary>
        /// マナを消費する
        /// </summary>
        /// <param name="manaCost">消費マナ</param>
        /// <returns>消費に成功したかどうか</returns>
        public bool ConsumeMana(int manaCost)
        {
            if (currentMana >= manaCost)
            {
                currentMana -= manaCost;
                return true;
            }
            return false;
        }

        /// <summary>
        /// マナを回復する
        /// </summary>
        /// <param name="manaAmount">回復量</param>
        /// <returns>実際に回復した量</returns>
        public int RestoreMana(int manaAmount)
        {
            var oldMana = currentMana;
            currentMana = Mathf.Min(maxMana, currentMana + manaAmount);
            return currentMana - oldMana;
        }

        /// <summary>
        /// 武器使用可能性をチェック
        /// </summary>
        /// <param name="weaponIndex">武器インデックス</param>
        /// <returns>使用可能かどうか</returns>
        public bool CanUseWeapon(int weaponIndex)
        {
            return weaponIndex >= 0 && weaponIndex < 4 && 
                   equippedWeapons[weaponIndex] != null && 
                   weaponCooldowns[weaponIndex] <= 0;
        }

        /// <summary>
        /// プレイヤーデータのリセット
        /// </summary>
        public void Reset()
        {
            currentHp = maxHp;
            currentMana = maxMana;
            // 武器クールダウンをリセット
            for (int i = 0; i < weaponCooldowns.Length; i++)
            {
                weaponCooldowns[i] = 0;
            }
        }
    }
}