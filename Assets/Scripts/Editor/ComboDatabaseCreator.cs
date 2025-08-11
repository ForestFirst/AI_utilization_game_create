using UnityEngine;
using UnityEditor;
using BattleSystem;
using System.Reflection;

/// <summary>
/// ComboDatabase.asset作成用のエディタースクリプト
/// Unityエディターのメニューから実行してComboDatabase.assetを生成します
/// </summary>
public class ComboDatabaseCreator : EditorWindow
{
    [MenuItem("Tools/Battle System/Create Combo Database")]
    public static void CreateComboDatabase()
    {
        // ComboDatabase ScriptableObjectのインスタンスを作成
        ComboDatabase database = ScriptableObject.CreateInstance<ComboDatabase>();
        
        // 15種類のコンボデータを作成
        ComboData[] combos = CreateAllCombos();
        
        // リフレクションを使用してprivateフィールドにアクセス
        FieldInfo field = typeof(ComboDatabase).GetField("availableCombos", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(database, combos);
        }
        else
        {
            Debug.LogError("availableCombosフィールドが見つかりません。ComboDatabase.csの実装を確認してください。");
            return;
        }

        // アセットとして保存
        string assetPath = "Assets/Data/MainComboDatabase.asset";
        
        // Dataフォルダが存在しない場合は作成
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        
        AssetDatabase.CreateAsset(database, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 作成されたアセットを選択
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = database;

        Debug.Log($"ComboDatabase created successfully with {combos.Length} combos!");
        Debug.Log($"Asset saved at: {assetPath}");
    }

    /// <summary>
    /// 15種類のコンボデータを作成
    /// </summary>
    private static ComboData[] CreateAllCombos()
    {
        return new ComboData[]
        {
            // === 基本コンボ（2-3手）=== 
            CreateFlameSlashCombo(),
            CreateIceBreakerCombo(),
            CreateThunderStrikeCombo(),
            
            // === 中級コンボ（3-4手）===
            CreateFlameIceExplosionCombo(),
            CreateThunderRushCombo(),
            CreateWindSpiralCombo(),
            CreateEarthCrashCombo(),
            
            // === 上級コンボ（4-5手）===
            CreateElementCycleCombo(),
            CreateHolyRageCombo(),
            CreateDarknessVoidCombo(),
            
            // === 特殊コンボ（武器種特化）===
            CreateGunslingerCombo(),
            CreateShieldBashCombo(),
            CreateArcherVolleyCombo(),
            
            // === 超上級コンボ（最高難易度）===
            CreateElementalMasteryCombo(),
            CreateAllWeaponAssaultCombo()
        };
    }

    // === 基本コンボ作成メソッド ===
    
    private static ComboData CreateFlameSlashCombo()
    {
        return new ComboData
        {
            comboName = "フレイムスラッシュ",
            condition = new ComboCondition
            {
                comboType = ComboType.AttributeCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Fire },
                requiredWeaponTypes = new WeaponType[] { WeaponType.Sword },
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 3,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 1.3f,
                    effectDescription = "火属性ダメージ30%アップ"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.StatusEffect,
                    statusAttribute = AttackAttribute.Fire,
                    statusDuration = 2,
                    effectDescription = "炎上付与（2ターン）"
                }
            },
            requiredWeaponCount = 2,
            comboDescription = "炎属性武器を2回使用で発動。基本的な火属性コンボ。",
            canInterrupt = true,
            interruptResistance = 0.2f,
            priority = 1
        };
    }

    private static ComboData CreateIceBreakerCombo()
    {
        return new ComboData
        {
            comboName = "アイスブレイカー",
            condition = new ComboCondition
            {
                comboType = ComboType.AttributeCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Ice },
                requiredWeaponTypes = new WeaponType[] { WeaponType.Axe, WeaponType.Sword },
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 3,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 1.25f,
                    effectDescription = "氷属性ダメージ25%アップ"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.StatusEffect,
                    statusAttribute = AttackAttribute.Ice,
                    statusDuration = 1,
                    effectDescription = "凍結付与（1ターン）"
                }
            },
            requiredWeaponCount = 2,
            comboDescription = "氷属性の近接武器を2回使用。敵の行動を封じる。",
            canInterrupt = true,
            interruptResistance = 0.2f,
            priority = 1
        };
    }

    private static ComboData CreateThunderStrikeCombo()
    {
        return new ComboData
        {
            comboName = "サンダーストライク",
            condition = new ComboCondition
            {
                comboType = ComboType.AttributeCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Thunder },
                requiredWeaponTypes = null,
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 2,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.AdditionalAction,
                    additionalActions = 1,
                    effectDescription = "追加行動+1"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.StatusEffect,
                    statusAttribute = AttackAttribute.Thunder,
                    statusDuration = 2,
                    effectDescription = "麻痺付与（2ターン）"
                }
            },
            requiredWeaponCount = 2,
            comboDescription = "雷属性武器を素早く2回使用。追加行動を獲得。",
            canInterrupt = true,
            interruptResistance = 0.1f,
            priority = 2
        };
    }

    // === 中級コンボ作成メソッド ===
    
    private static ComboData CreateFlameIceExplosionCombo()
    {
        return new ComboData
        {
            comboName = "炎氷爆発",
            condition = new ComboCondition
            {
                comboType = ComboType.SequenceCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Fire, AttackAttribute.Ice },
                requiredWeaponTypes = new WeaponType[] { WeaponType.Sword, WeaponType.Axe },
                minAttackPower = 0,
                requiresSequence = true,
                maxTurnInterval = 4,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 2.5f,
                    effectDescription = "爆発ダメージ250%"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.StatusEffect,
                    statusAttribute = AttackAttribute.Ice,
                    statusDuration = 2,
                    effectDescription = "凍結付与（2ターン）"
                }
            },
            requiredWeaponCount = 3,
            comboDescription = "炎の剣→氷の斧→任意武器の順序で発動。温度差による爆発攻撃。",
            canInterrupt = true,
            interruptResistance = 0.4f,
            priority = 3
        };
    }

    private static ComboData CreateThunderRushCombo()
    {
        return new ComboData
        {
            comboName = "雷撃連打",
            condition = new ComboCondition
            {
                comboType = ComboType.MixedCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Thunder },
                requiredWeaponTypes = new WeaponType[] { WeaponType.Spear },
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 3,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.AdditionalAction,
                    additionalActions = 2,
                    effectDescription = "追加行動+2"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.StatusEffect,
                    statusAttribute = AttackAttribute.Thunder,
                    statusDuration = 3,
                    effectDescription = "麻痺付与（3ターン）"
                }
            },
            requiredWeaponCount = 2,
            comboDescription = "雷槍→大型武器の順序。電撃による高速連続攻撃。",
            canInterrupt = true,
            interruptResistance = 0.3f,
            priority = 3
        };
    }

    private static ComboData CreateWindSpiralCombo()
    {
        return new ComboData
        {
            comboName = "ウィンドスパイラル",
            condition = new ComboCondition
            {
                comboType = ComboType.AttributeCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Wind },
                requiredWeaponTypes = null,
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 4,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.SpecialAttack,
                    effectDescription = "全体攻撃"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 1.8f,
                    effectDescription = "風圧ダメージ180%"
                }
            },
            requiredWeaponCount = 3,
            comboDescription = "風属性武器を3回使用。竜巻による全体攻撃。",
            canInterrupt = true,
            interruptResistance = 0.3f,
            priority = 2
        };
    }

    private static ComboData CreateEarthCrashCombo()
    {
        return new ComboData
        {
            comboName = "アースクラッシュ",
            condition = new ComboCondition
            {
                comboType = ComboType.PowerCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Earth },
                requiredWeaponTypes = new WeaponType[] { WeaponType.Axe },
                minAttackPower = 300,
                requiresSequence = false,
                maxTurnInterval = 3,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 3.0f,
                    effectDescription = "大地割り300%ダメージ"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.DebuffEnemy,
                    buffValue = 30,
                    effectDuration = 3,
                    effectDescription = "敵防御力-30%（3ターン）"
                }
            },
            requiredWeaponCount = 2,
            comboDescription = "高攻撃力の土属性斧で発動。大地を砕く破壊力。",
            canInterrupt = false,
            interruptResistance = 0.8f,
            priority = 4
        };
    }

    // === 上級コンボ作成メソッド ===
    
    private static ComboData CreateElementCycleCombo()
    {
        return new ComboData
        {
            comboName = "属性循環",
            condition = new ComboCondition
            {
                comboType = ComboType.SequenceCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Fire, AttackAttribute.Thunder, AttackAttribute.Ice },
                requiredWeaponTypes = null,
                minAttackPower = 0,
                requiresSequence = true,
                maxTurnInterval = 5,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.SpecialAttack,
                    effectDescription = "全体属性攻撃"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.BuffPlayer,
                    buffValue = 50,
                    effectDuration = 5,
                    effectDescription = "状態異常無効+攻撃力50%アップ（5ターン）"
                }
            },
            requiredWeaponCount = 3,
            comboDescription = "炎→雷→氷の順序で発動。属性の循環による究極攻撃。",
            canInterrupt = true,
            interruptResistance = 0.6f,
            priority = 5
        };
    }

    private static ComboData CreateHolyRageCombo()
    {
        return new ComboData
        {
            comboName = "ホーリーレイジ",
            condition = new ComboCondition
            {
                comboType = ComboType.AttributeCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Light },
                requiredWeaponTypes = new WeaponType[] { WeaponType.Magic, WeaponType.Sword },
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 4,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 2.2f,
                    effectDescription = "聖なる怒り220%ダメージ"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.Healing,
                    healingAmount = 5000,
                    effectDescription = "HP回復5000"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.BuffPlayer,
                    buffValue = 25,
                    effectDuration = 4,
                    effectDescription = "クリティカル率+25%（4ターン）"
                }
            },
            requiredWeaponCount = 4,
            comboDescription = "光属性魔法と剣を組み合わせた神聖なコンボ。攻撃と回復を同時実行。",
            canInterrupt = true,
            interruptResistance = 0.5f,
            priority = 4
        };
    }

    private static ComboData CreateDarknessVoidCombo()
    {
        return new ComboData
        {
            comboName = "ダークネスボイド",
            condition = new ComboCondition
            {
                comboType = ComboType.MixedCombo,
                requiredAttackAttributes = new AttackAttribute[] { AttackAttribute.Dark },
                requiredWeaponTypes = new WeaponType[] { WeaponType.Magic, WeaponType.Tool },
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 6,
                successRate = 0.9f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.SpecialAttack,
                    effectDescription = "闇の無次元攻撃"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.DebuffEnemy,
                    buffValue = 50,
                    effectDuration = 4,
                    effectDescription = "敵全能力-50%（4ターン）"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.AdditionalAction,
                    additionalActions = 1,
                    effectDescription = "追加行動+1"
                }
            },
            requiredWeaponCount = 4,
            comboDescription = "闇属性魔法と道具の組み合わせ。敵を無力化する禁断のコンボ。",
            canInterrupt = true,
            interruptResistance = 0.7f,
            priority = 5
        };
    }

    // === 特殊コンボ作成メソッド ===
    
    private static ComboData CreateGunslingerCombo()
    {
        return new ComboData
        {
            comboName = "ガンスリンガー",
            condition = new ComboCondition
            {
                comboType = ComboType.WeaponCombo,
                requiredAttackAttributes = null,
                requiredWeaponTypes = new WeaponType[] { WeaponType.Gun },
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 2,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.AdditionalAction,
                    additionalActions = 3,
                    effectDescription = "連続射撃+3"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 1.1f,
                    effectDescription = "精密射撃110%ダメージ"
                }
            },
            requiredWeaponCount = 2,
            comboDescription = "銃器を2回連続使用。西部劇スタイルの連続射撃。",
            canInterrupt = false,
            interruptResistance = 0.9f,
            priority = 3
        };
    }

    private static ComboData CreateShieldBashCombo()
    {
        return new ComboData
        {
            comboName = "シールドバッシュ",
            condition = new ComboCondition
            {
                comboType = ComboType.WeaponCombo,
                requiredAttackAttributes = null,
                requiredWeaponTypes = new WeaponType[] { WeaponType.Shield },
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 3,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 1.5f,
                    effectDescription = "盾攻撃150%ダメージ"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.BuffPlayer,
                    buffValue = 40,
                    effectDuration = 3,
                    effectDescription = "防御力+40%（3ターン）"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.DebuffEnemy,
                    buffValue = 20,
                    effectDuration = 2,
                    effectDescription = "敵攻撃力-20%（2ターン）"
                }
            },
            requiredWeaponCount = 3,
            comboDescription = "盾を3回使用。攻防一体の戦闘スタイル。",
            canInterrupt = false,
            interruptResistance = 0.8f,
            priority = 2
        };
    }

    private static ComboData CreateArcherVolleyCombo()
    {
        return new ComboData
        {
            comboName = "アーチャーボレー",
            condition = new ComboCondition
            {
                comboType = ComboType.WeaponCombo,
                requiredAttackAttributes = null,
                requiredWeaponTypes = new WeaponType[] { WeaponType.Bow },
                minAttackPower = 0,
                requiresSequence = false,
                maxTurnInterval = 4,
                successRate = 1.0f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.SpecialAttack,
                    effectDescription = "精密狙撃（全列攻撃）"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 1.8f,
                    effectDescription = "狙撃ダメージ180%"
                }
            },
            requiredWeaponCount = 3,
            comboDescription = "弓を3回使用。精密な矢の雨による遠距離制圧。",
            canInterrupt = true,
            interruptResistance = 0.3f,
            priority = 3
        };
    }

    // === 超上級コンボ作成メソッド ===
    
    private static ComboData CreateElementalMasteryCombo()
    {
        return new ComboData
        {
            comboName = "エレメンタルマスタリー",
            condition = new ComboCondition
            {
                comboType = ComboType.SequenceCombo,
                requiredAttackAttributes = new AttackAttribute[] 
                { 
                    AttackAttribute.Fire, AttackAttribute.Ice, AttackAttribute.Thunder, 
                    AttackAttribute.Wind, AttackAttribute.Earth 
                },
                requiredWeaponTypes = null,
                minAttackPower = 0,
                requiresSequence = true,
                maxTurnInterval = 8,
                successRate = 0.8f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 4.0f,
                    effectDescription = "全属性融合400%ダメージ"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.SpecialAttack,
                    effectDescription = "エレメンタルストーム（全体攻撃）"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.AdditionalAction,
                    additionalActions = 2,
                    effectDescription = "追加行動+2"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.BuffPlayer,
                    buffValue = 100,
                    effectDuration = 5,
                    effectDescription = "マスタリー状態：全能力+100%（5ターン）"
                }
            },
            requiredWeaponCount = 5,
            comboDescription = "炎→氷→雷→風→土の順序で5属性を制御。元素の真理を極めた究極コンボ。",
            canInterrupt = true,
            interruptResistance = 0.9f,
            priority = 10
        };
    }

    private static ComboData CreateAllWeaponAssaultCombo()
    {
        return new ComboData
        {
            comboName = "オールウェポンアサルト",
            condition = new ComboCondition
            {
                comboType = ComboType.WeaponCombo,
                requiredAttackAttributes = null,
                requiredWeaponTypes = new WeaponType[] 
                { 
                    WeaponType.Sword, WeaponType.Axe, WeaponType.Spear, 
                    WeaponType.Bow, WeaponType.Gun 
                },
                minAttackPower = 500,
                requiresSequence = false,
                maxTurnInterval = 7,
                successRate = 0.7f
            },
            effects = new ComboEffect[]
            {
                new ComboEffect
                {
                    effectType = ComboEffectType.AdditionalAction,
                    additionalActions = 4,
                    effectDescription = "武器熟練+4行動"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.DamageMultiplier,
                    damageMultiplier = 3.5f,
                    effectDescription = "武器マスタリー350%ダメージ"
                },
                new ComboEffect
                {
                    effectType = ComboEffectType.SpecialAttack,
                    effectDescription = "オールレンジアタック"
                }
            },
            requiredWeaponCount = 5,
            comboDescription = "5種類の武器を駆使した武術の極致。全ての武器を完璧に操る証。",
            canInterrupt = false,
            interruptResistance = 1.0f,
            priority = 10
        };
    }
}
