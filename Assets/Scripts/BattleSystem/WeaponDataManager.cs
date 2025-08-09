using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    // 武器生成のパラメータ
    [Serializable]
    public class WeaponGenerationParams
    {
        public AttackAttribute attackAttribute;
        public WeaponType weaponType;
        public int minPower;
        public int maxPower;
        public AttackRange primaryRange;
        public AttackRange[] possibleRanges;
        public int baseCriticalRate;
        public int baseCooldown;
        public string[] possibleEffects;
        public int generationWeight;
    }

    // 武器強化データ
    [Serializable]
    public class WeaponEnhancement
    {
        public int enhancementLevel;
        public int powerBonus;
        public int criticalRateBonus;
        public int cooldownReduction;
        public string additionalEffect;
        public float effectPowerMultiplier;
    }

    // 武器コレクション管理
    [Serializable]
    public class WeaponCollection
    {
        public List<WeaponData> ownedWeapons;
        public List<WeaponData> equippedWeapons;
        public Dictionary<int, WeaponEnhancement> weaponEnhancements;
        
        public WeaponCollection()
        {
            ownedWeapons = new List<WeaponData>();
            equippedWeapons = new List<WeaponData>(4);
            weaponEnhancements = new Dictionary<int, WeaponEnhancement>();
            
            // 装備スロット4つ分初期化
            for (int i = 0; i < 4; i++)
            {
                equippedWeapons.Add(null);
            }
        }
    }

    // 武器データ管理システム
    [CreateAssetMenu(fileName = "WeaponDataManager", menuName = "BattleSystem/WeaponDataManager")]
    public class WeaponDataManager : ScriptableObject
    {
        [Header("プリセット武器データ")]
        [SerializeField] private WeaponData[] presetWeapons;
        
        [Header("武器生成パラメータ")]
        [SerializeField] private WeaponGenerationParams[] generationParams;
        
        [Header("武器強化設定")]
        [SerializeField] private WeaponEnhancement[] enhancementLevels;
        
        [Header("バランス設定")]
        [SerializeField] private int maxWeaponPower = 200;
        [SerializeField] private int minWeaponPower = 50;
        [SerializeField] private int maxCriticalRate = 50;
        [SerializeField] private int maxCooldown = 5;

        private static WeaponDataManager instance;
        
        public static WeaponDataManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<WeaponDataManager>("WeaponDataManager");
                }
                return instance;
            }
        }

        public WeaponData[] PresetWeapons => presetWeapons;
        
        private void OnEnable()
        {
            if (instance == null)
                instance = this;
                
            InitializePresetWeapons();
        }

        // プリセット武器の初期化
        private void InitializePresetWeapons()
        {
            if (presetWeapons == null || presetWeapons.Length == 0)
            {
                CreateDefaultPresetWeapons();
            }
        }

        // デフォルトプリセット武器の作成
        private void CreateDefaultPresetWeapons()
        {
            List<WeaponData> weapons = new List<WeaponData>();

            // 1. ソードブレード（無+剣）
            weapons.Add(CreateWeapon(
                "ソードブレード", AttackAttribute.None, WeaponType.Sword,
                120, AttackRange.SingleFront, 15, 0,
                "クリティカル率+15%", 15, 0
            ));

            // 2. スナイパーライフル（無+弓）
            weapons.Add(CreateWeapon(
                "スナイパーライフル", AttackAttribute.None, WeaponType.Bow,
                100, AttackRange.SingleTarget, 10, 1,
                "ゲート追加ダメージ+50%", 50, 0
            ));

            // 3. フレイムスロアー（炎+銃）
            weapons.Add(CreateWeapon(
                "フレイムスロアー", AttackAttribute.Fire, WeaponType.Gun,
                90, AttackRange.Row1, 8, 0,
                "炎上付与（2ターン継続ダメージ）", 0, 2
            ));

            // 4. アイスランス（氷+槍）
            weapons.Add(CreateWeapon(
                "アイスランス", AttackAttribute.Ice, WeaponType.Spear,
                85, AttackRange.Row2, 6, 1,
                "凍結付与（1ターン行動停止）", 0, 1
            ));

            // 5. サンダーボルト（雷+魔法）
            weapons.Add(CreateWeapon(
                "サンダーボルト", AttackAttribute.Thunder, WeaponType.Magic,
                95, AttackRange.Column, 10, 2,
                "麻痺付与（行動順を1つ遅らせる）", 0, 1
            ));

            // 6. レーザーキャノン（光+銃）
            weapons.Add(CreateWeapon(
                "レーザーキャノン", AttackAttribute.Light, WeaponType.Gun,
                110, AttackRange.Column, 5, 1,
                "装甲貫通（防御力無視）", 0, 0
            ));

            // 7. グレネードランチャー（土+銃）
            weapons.Add(CreateWeapon(
                "グレネードランチャー", AttackAttribute.Earth, WeaponType.Gun,
                70, AttackRange.All, 8, 2,
                "爆発ダメージ（残りHP25%以下の敵を即死）", 25, 0
            ));

            // 8. EMPバースト（雷+道具）
            weapons.Add(CreateWeapon(
                "EMPバースト", AttackAttribute.Thunder, WeaponType.Tool,
                60, AttackRange.All, 5, 3,
                "機械系敵に特効ダメージ+100%", 100, 0
            ));

            // 9. リペアドローン（光+道具）
            weapons.Add(CreateWeapon(
                "リペアドローン", AttackAttribute.Light, WeaponType.Tool,
                0, AttackRange.Self, 0, 0,
                "HP回復+次ターンの攻撃力+30%", 30, 1
            ));

            // 10. シールドブレイカー（闇+道具）
            weapons.Add(CreateWeapon(
                "シールドブレイカー", AttackAttribute.Dark, WeaponType.Tool,
                40, AttackRange.SingleFront, 3, 1,
                "敵の防御力を3ターン-50%", 50, 3
            ));

            presetWeapons = weapons.ToArray();
        }

        // 武器作成ヘルパー
        private WeaponData CreateWeapon(string name, AttackAttribute attackAttr, WeaponType weaponType,
            int basePower, AttackRange range, int critRate, int cooldown,
            string effect, int effectValue, int effectDuration)
        {
            WeaponData weapon = new WeaponData
            {
                weaponName = name,
                attackAttribute = attackAttr,
                weaponType = weaponType,
                basePower = basePower,
                attackRange = range,
                criticalRate = critRate,
                cooldownTurns = cooldown,
                specialEffect = effect,
                effectValue = effectValue,
                effectDuration = effectDuration,
                canUseConsecutively = cooldown == 0
            };

            return weapon;
        }

        // ランダム武器生成
        public WeaponData GenerateRandomWeapon()
        {
            if (generationParams == null || generationParams.Length == 0)
                return null;

            // 重み付きランダム選択
            WeaponGenerationParams selectedParam = SelectWeightedRandom(generationParams);
            return GenerateWeaponFromParams(selectedParam);
        }

        // 特定条件での武器生成
        public WeaponData GenerateWeapon(AttackAttribute attackAttr, WeaponType weaponType)
        {
            WeaponGenerationParams param = generationParams.FirstOrDefault(
                p => p.attackAttribute == attackAttr && p.weaponType == weaponType);
            
            if (param != null)
                return GenerateWeaponFromParams(param);
            
            // フォールバック：基本武器生成
            return CreateBasicWeapon(attackAttr, weaponType);
        }

        // パラメータから武器生成
        private WeaponData GenerateWeaponFromParams(WeaponGenerationParams param)
        {
            WeaponData weapon = new WeaponData();
            
            weapon.weaponName = GenerateWeaponName(param.attackAttribute, param.weaponType);
            weapon.attackAttribute = param.attackAttribute;
            weapon.weaponType = param.weaponType;
            weapon.basePower = UnityEngine.Random.Range(param.minPower, param.maxPower + 1);
            weapon.attackRange = SelectRandomRange(param);
            weapon.criticalRate = param.baseCriticalRate + UnityEngine.Random.Range(-2, 5);
            weapon.cooldownTurns = param.baseCooldown;
            
            // 特殊効果の設定
            if (param.possibleEffects != null && param.possibleEffects.Length > 0)
            {
                weapon.specialEffect = param.possibleEffects[UnityEngine.Random.Range(0, param.possibleEffects.Length)];
                weapon.effectValue = UnityEngine.Random.Range(10, 51);
                weapon.effectDuration = UnityEngine.Random.Range(1, 4);
            }

            weapon.canUseConsecutively = weapon.cooldownTurns == 0;
            
            return BalanceWeapon(weapon);
        }

        // 武器名生成
        private string GenerateWeaponName(AttackAttribute attackAttr, WeaponType weaponType)
        {
            string attributePrefix = GetAttributePrefix(attackAttr);
            string weaponBase = GetWeaponBaseName(weaponType);
            string suffix = GetRandomSuffix();
            
            return $"{attributePrefix}{weaponBase}{suffix}";
        }

        private string GetAttributePrefix(AttackAttribute attr)
        {
            switch (attr)
            {
                case AttackAttribute.Fire: return "フレイム";
                case AttackAttribute.Ice: return "アイス";
                case AttackAttribute.Thunder: return "サンダー";
                case AttackAttribute.Wind: return "ウィンド";
                case AttackAttribute.Earth: return "アース";
                case AttackAttribute.Light: return "ライト";
                case AttackAttribute.Dark: return "ダーク";
                default: return "プレーン";
            }
        }

        private string GetWeaponBaseName(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Sword: return "ブレード";
                case WeaponType.Axe: return "アックス";
                case WeaponType.Spear: return "ランス";
                case WeaponType.Bow: return "ボウ";
                case WeaponType.Gun: return "キャノン";
                case WeaponType.Shield: return "シールド";
                case WeaponType.Magic: return "ボルト";
                case WeaponType.Tool: return "デバイス";
                default: return "ウェポン";
            }
        }

        private string GetRandomSuffix()
        {
            string[] suffixes = { "MK1", "MK2", "MK3", "プロト", "改", "EX", "α", "β" };
            return suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
        }

        // 攻撃範囲のランダム選択
        private AttackRange SelectRandomRange(WeaponGenerationParams param)
        {
            if (param.possibleRanges != null && param.possibleRanges.Length > 0)
            {
                return param.possibleRanges[UnityEngine.Random.Range(0, param.possibleRanges.Length)];
            }
            return param.primaryRange;
        }

        // 重み付きランダム選択
        private WeaponGenerationParams SelectWeightedRandom(WeaponGenerationParams[] parameters)
        {
            int totalWeight = parameters.Sum(p => p.generationWeight);
            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            
            int currentWeight = 0;
            foreach (WeaponGenerationParams param in parameters)
            {
                currentWeight += param.generationWeight;
                if (randomValue < currentWeight)
                    return param;
            }
            
            return parameters[0]; // フォールバック
        }

        // 基本武器作成
        private WeaponData CreateBasicWeapon(AttackAttribute attackAttr, WeaponType weaponType)
        {
            WeaponData weapon = new WeaponData();
            weapon.weaponName = GenerateWeaponName(attackAttr, weaponType);
            weapon.attackAttribute = attackAttr;
            weapon.weaponType = weaponType;
            weapon.basePower = UnityEngine.Random.Range(80, 121);
            weapon.attackRange = AttackRange.SingleFront;
            weapon.criticalRate = UnityEngine.Random.Range(3, 8);
            weapon.cooldownTurns = UnityEngine.Random.Range(0, 3);
            weapon.canUseConsecutively = weapon.cooldownTurns == 0;
            
            return BalanceWeapon(weapon);
        }

        // 武器バランス調整
        private WeaponData BalanceWeapon(WeaponData weapon)
        {
            // 攻撃力制限
            weapon.basePower = Mathf.Clamp(weapon.basePower, minWeaponPower, maxWeaponPower);
            
            // クリティカル率制限
            weapon.criticalRate = Mathf.Clamp(weapon.criticalRate, 0, maxCriticalRate);
            
            // クールダウン制限
            weapon.cooldownTurns = Mathf.Clamp(weapon.cooldownTurns, 0, maxCooldown);
            
            // 攻撃範囲に応じた攻撃力調整
            weapon.basePower = AdjustPowerByRange(weapon.basePower, weapon.attackRange);
            
            return weapon;
        }

        // 攻撃範囲に応じた攻撃力調整
        private int AdjustPowerByRange(int basePower, AttackRange range)
        {
            switch (range)
            {
                case AttackRange.SingleFront:
                case AttackRange.SingleTarget:
                    return basePower; // 基本値

                case AttackRange.Row1:
                case AttackRange.Row2:
                    return Mathf.RoundToInt(basePower * 0.85f); // 15%減

                case AttackRange.Column:
                    return Mathf.RoundToInt(basePower * 0.9f); // 10%減

                case AttackRange.All:
                    return Mathf.RoundToInt(basePower * 0.7f); // 30%減

                case AttackRange.Self:
                    return basePower; // 自己対象は調整なし

                default:
                    return basePower;
            }
        }

        // 武器強化
        public WeaponData EnhanceWeapon(WeaponData originalWeapon, int enhancementLevel)
        {
            if (enhancementLevel <= 0 || enhancementLevel >= enhancementLevels.Length)
                return originalWeapon;

            WeaponData enhancedWeapon = CopyWeapon(originalWeapon);
            WeaponEnhancement enhancement = enhancementLevels[enhancementLevel - 1];

            enhancedWeapon.basePower += enhancement.powerBonus;
            enhancedWeapon.criticalRate += enhancement.criticalRateBonus;
            enhancedWeapon.cooldownTurns = Mathf.Max(0, enhancedWeapon.cooldownTurns - enhancement.cooldownReduction);
            
            if (!string.IsNullOrEmpty(enhancement.additionalEffect))
            {
                enhancedWeapon.specialEffect += " + " + enhancement.additionalEffect;
            }

            enhancedWeapon.effectValue = Mathf.RoundToInt(enhancedWeapon.effectValue * enhancement.effectPowerMultiplier);
            enhancedWeapon.weaponName += $" +{enhancementLevel}";

            return BalanceWeapon(enhancedWeapon);
        }

        // 武器コピー
        private WeaponData CopyWeapon(WeaponData original)
        {
            WeaponData copy = new WeaponData();
            copy.weaponName = original.weaponName;
            copy.attackAttribute = original.attackAttribute;
            copy.weaponType = original.weaponType;
            copy.basePower = original.basePower;
            copy.attackRange = original.attackRange;
            copy.criticalRate = original.criticalRate;
            copy.cooldownTurns = original.cooldownTurns;
            copy.specialEffect = original.specialEffect;
            copy.effectValue = original.effectValue;
            copy.effectDuration = original.effectDuration;
            copy.canUseConsecutively = original.canUseConsecutively;
            
            return copy;
        }

        // 特定属性・タイプの武器を取得
        public WeaponData[] GetWeaponsByType(AttackAttribute attribute, WeaponType type)
        {
            return presetWeapons.Where(w => w.attackAttribute == attribute && w.weaponType == type).ToArray();
        }

        // 攻撃範囲別武器取得
        public WeaponData[] GetWeaponsByRange(AttackRange range)
        {
            return presetWeapons.Where(w => w.attackRange == range).ToArray();
        }

        // 攻撃力範囲での武器取得
        public WeaponData[] GetWeaponsByPowerRange(int minPower, int maxPower)
        {
            return presetWeapons.Where(w => w.basePower >= minPower && w.basePower <= maxPower).ToArray();
        }

        // 武器互換性チェック
        public bool IsWeaponCompatible(WeaponData weapon1, WeaponData weapon2)
        {
            // コンボ互換性の基本チェック
            return weapon1.attackAttribute == weapon2.attackAttribute || 
                   weapon1.weaponType == weapon2.weaponType;
        }

        // 武器評価値計算
        public int CalculateWeaponValue(WeaponData weapon)
        {
            int baseValue = weapon.basePower;
            int criticalBonus = weapon.criticalRate * 2;
            int rangeMultiplier = GetRangeValueMultiplier(weapon.attackRange);
            int effectBonus = !string.IsNullOrEmpty(weapon.specialEffect) ? 20 : 0;
            int cooldownPenalty = weapon.cooldownTurns * 5;

            return (baseValue + criticalBonus + effectBonus - cooldownPenalty) * rangeMultiplier / 100;
        }

        private int GetRangeValueMultiplier(AttackRange range)
        {
            switch (range)
            {
                case AttackRange.SingleFront:
                case AttackRange.SingleTarget:
                    return 100;
                case AttackRange.Row1:
                case AttackRange.Row2:
                    return 120;
                case AttackRange.Column:
                    return 115;
                case AttackRange.All:
                    return 150;
                case AttackRange.Self:
                    return 80;
                default:
                    return 100;
            }
        }
    }

    // 武器管理クラス（ランタイム用）
    public class WeaponManager : MonoBehaviour
    {
        [Header("武器管理設定")]
        [SerializeField] private WeaponDataManager weaponDataManager;
        [SerializeField] private int maxOwnedWeapons = 50;
        
        private WeaponCollection playerWeaponCollection;
        private BattleManager battleManager;

        public event Action<WeaponData> OnWeaponAcquired;
        public event Action<WeaponData, int> OnWeaponEquipped;
        public event Action<WeaponData, int> OnWeaponEnhanced;

        public WeaponCollection PlayerWeapons => playerWeaponCollection;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            playerWeaponCollection = new WeaponCollection();
            
            if (weaponDataManager == null)
                weaponDataManager = WeaponDataManager.Instance;
        }

        private void Start()
        {
            InitializePlayerWeapons();
        }

        // プレイヤー武器初期化
        private void InitializePlayerWeapons()
        {
            if (weaponDataManager != null && weaponDataManager.PresetWeapons.Length > 0)
            {
                // 初期武器として基本的な4つを装備
                WeaponData[] initialWeapons = {
                    weaponDataManager.PresetWeapons[0], // ソードブレード
                    weaponDataManager.PresetWeapons[2], // フレイムスロアー
                    weaponDataManager.PresetWeapons[4], // サンダーボルト
                    weaponDataManager.PresetWeapons[8]  // リペアドローン
                };

                for (int i = 0; i < initialWeapons.Length; i++)
                {
                    AcquireWeapon(initialWeapons[i]);
                    EquipWeapon(initialWeapons[i], i);
                }
            }
        }

        // 武器取得
        public bool AcquireWeapon(WeaponData weapon)
        {
            if (weapon == null || playerWeaponCollection.ownedWeapons.Count >= maxOwnedWeapons)
                return false;

            playerWeaponCollection.ownedWeapons.Add(weapon);
            OnWeaponAcquired?.Invoke(weapon);
            
            Debug.Log($"武器取得: {weapon.weaponName}");
            return true;
        }

        // 武器装備
        public bool EquipWeapon(WeaponData weapon, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4 || weapon == null)
                return false;

            if (!playerWeaponCollection.ownedWeapons.Contains(weapon))
                return false;

            playerWeaponCollection.equippedWeapons[slotIndex] = weapon;
            
            // BattleManagerに装備更新を通知
            if (battleManager != null)
            {
                battleManager.PlayerData.equippedWeapons[slotIndex] = weapon;
            }

            OnWeaponEquipped?.Invoke(weapon, slotIndex);
            Debug.Log($"武器装備: {weapon.weaponName} をスロット {slotIndex} に装備");
            return true;
        }

        // 武器強化
        public WeaponData EnhanceWeapon(WeaponData weapon, int enhancementLevel)
        {
            if (weapon == null || !playerWeaponCollection.ownedWeapons.Contains(weapon))
                return null;

            WeaponData enhancedWeapon = weaponDataManager.EnhanceWeapon(weapon, enhancementLevel);
            
            // 元の武器を強化版に置き換え
            int index = playerWeaponCollection.ownedWeapons.IndexOf(weapon);
            playerWeaponCollection.ownedWeapons[index] = enhancedWeapon;

            // 装備中の武器も更新
            for (int i = 0; i < playerWeaponCollection.equippedWeapons.Count; i++)
            {
                if (playerWeaponCollection.equippedWeapons[i] == weapon)
                {
                    playerWeaponCollection.equippedWeapons[i] = enhancedWeapon;
                    if (battleManager != null)
                    {
                        battleManager.PlayerData.equippedWeapons[i] = enhancedWeapon;
                    }
                }
            }

            OnWeaponEnhanced?.Invoke(enhancedWeapon, enhancementLevel);
            Debug.Log($"武器強化: {enhancedWeapon.weaponName}");
            return enhancedWeapon;
        }

        // ランダム武器取得
        public WeaponData GetRandomWeapon()
        {
            return weaponDataManager.GenerateRandomWeapon();
        }

        // デバッグ用：全プリセット武器取得
        [ContextMenu("Acquire All Preset Weapons")]
        public void AcquireAllPresetWeapons()
        {
            if (weaponDataManager != null)
            {
                foreach (WeaponData weapon in weaponDataManager.PresetWeapons)
                {
                    AcquireWeapon(weapon);
                }
                Debug.Log("全プリセット武器取得完了");
            }
        }
    }
}
