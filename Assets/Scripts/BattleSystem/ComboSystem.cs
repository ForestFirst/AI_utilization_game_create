using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    // コンボの種類
    public enum ComboType
    {
        AttributeCombo,     // 属性コンボ（攻撃属性ベース）
        WeaponCombo,        // 武器コンボ（武器属性ベース）
        MixedCombo,         // 複合コンボ（両方の組み合わせ）
        SequenceCombo,      // シーケンスコンボ（指定順序）
        PowerCombo          // パワーコンボ（攻撃力閾値）
    }

    // コンボ効果の種類
    public enum ComboEffectType
    {
        DamageMultiplier,   // ダメージ倍率
        AdditionalAction,   // 追加行動
        StatusEffect,       // 状態異常付与
        Healing,            // 回復
        BuffPlayer,         // プレイヤー強化
        DebuffEnemy,        // 敵弱体化
        SpecialAttack       // 特殊攻撃
    }

    // コンボ条件データ
    [Serializable]
    public class ComboCondition
    {
        public ComboType comboType;
        public AttackAttribute[] requiredAttackAttributes;
        public WeaponType[] requiredWeaponTypes;
        public int[] requiredWeaponIndices;     // 特定武器インデックス指定
        public int minAttackPower;              // 最小攻撃力要求
        public bool requiresSequence;          // 順序指定の必要性
        public int maxTurnInterval;             // 最大ターン間隔
        public float successRate;               // 成功率
    }

    // コンボステップデータ
    [Serializable]
    public class ComboStep
    {
        public string stepName;                 // ステップ名
        public string requiredWeaponType;       // 必要武器タイプ
        public WeaponType weaponType;           // 武器タイプ（enum版）
        public AttackAttribute requiredAttribute; // 必要属性
        public int minDamage;                   // 最小ダメージ要求
        public bool isOptional;                // オプションステップか
        public string stepDescription;         // ステップ説明
    }

    // コンボ効果データ
    [Serializable]
    public class ComboEffect
    {
        public ComboEffectType effectType;
        public string effectName;               // 効果名
        public float damageMultiplier;          // ダメージ倍率（1.5 = 150%）
        public int additionalActions;           // 追加行動回数
        public AttackAttribute statusAttribute; // 状態異常の属性
        public int statusDuration;              // 状態異常継続ターン
        public int healingAmount;               // 回復量
        public int buffValue;                   // バフ効果値
        public int effectDuration;              // 効果継続ターン
        public string effectDescription;        // 効果説明
    }

    // コンボデータ（ScriptableObject）
    [Serializable]
    public class ComboData
    {
        public string comboName;
        public ComboCondition condition;
        public ComboEffect[] effects;
        public int requiredWeaponCount;         // 必要武器使用数
        public string[] requiredWeapons;        // 必要武器リスト
        public string comboDescription;
        public bool canInterrupt;               // 中断可能フラグ
        public float interruptResistance;       // 中断耐性
        public int priority;                    // 発動優先度
        
        // UI用の追加フィールド
        public List<ComboStep> steps;           // コンボステップリスト
        public float timeLimit;                 // 制限時間
        public ComboEffect comboEffect;         // 単一コンボ効果（UI用）
    }

    // コンボ進行状況
    [Serializable]
    public class ComboProgress
    {
        public ComboData comboData;
        public List<int> usedWeaponIndices;
        public List<AttackAttribute> usedAttackAttributes;
        public List<WeaponType> usedWeaponTypes;
        public int currentStep;
        public int totalSteps;                   // 総ステップ数
        public int startTurn;
        public float startTime;                  // 開始時間
        public bool isActive;
        public bool isCompleted;
        public float progressPercentage;
    }

    // コンボ実行結果
    public struct ComboExecutionResult
    {
        public bool wasExecuted;
        public ComboData executedCombo;
        public ComboEffect[] appliedEffects;
        public int additionalActionsGranted;
        public float totalDamageMultiplier;
        public string resultMessage;
    }

    // コンボシステム管理クラス
    [CreateAssetMenu(fileName = "ComboDatabase", menuName = "BattleSystem/ComboDatabase")]
    public class ComboDatabase : ScriptableObject
    {
        [SerializeField] private ComboData[] availableCombos;
        
        public ComboData[] AvailableCombos => availableCombos;
        
        public ComboData[] GetCombosByType(ComboType type)
        {
            return availableCombos?.Where(combo => combo.condition.comboType == type).ToArray() ?? new ComboData[0];
        }
        
        public ComboData GetCombo(string comboName)
        {
            return availableCombos?.FirstOrDefault(combo => combo.comboName == comboName);
        }

        /// <summary>
        /// エディタースクリプト用のコンボデータベース設定
        /// </summary>
        public void SetCombos(ComboData[] combos)
        {
            availableCombos = combos;
        }

        /// <summary>
        /// データベースが空かどうかを確認
        /// </summary>
        public bool IsEmpty()
        {
            return availableCombos == null || availableCombos.Length == 0;
        }
    }

    // コンボシステムメインクラス
    public class ComboSystem : MonoBehaviour
    {
        [Header("コンボシステム設定")]
        [SerializeField] private ComboDatabase comboDatabase;
        [SerializeField] private int maxActiveComboCount = 5;
        [SerializeField] private bool allowComboInterruption = true;
        [SerializeField] private float baseInterruptChance = 0.1f;
        [SerializeField] private bool enableComboChaining = true;

        [Header("デバッグ表示")]
        [SerializeField] private bool showComboProgress = true;
        [SerializeField] private bool showComboEffects = true;

        private BattleManager battleManager;
        private List<ComboProgress> activeComboProgresses;
        private Dictionary<ComboData, int> comboFailureCount;
        private int additionalActionsRemaining;

        // イベント定義
        public event Action<ComboData> OnComboStarted;
        public event Action<ComboProgress> OnComboProgressUpdated;
        public event Action<ComboExecutionResult> OnComboCompleted;
        public event Action<ComboData, string> OnComboFailed;
        public event Action<ComboData> OnComboInterrupted;

        // プロパティ
        public int AdditionalActionsRemaining => additionalActionsRemaining;
        public List<ComboProgress> ActiveCombos => activeComboProgresses;
        public ComboDatabase ComboDatabase => comboDatabase;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            activeComboProgresses = new List<ComboProgress>();
            comboFailureCount = new Dictionary<ComboData, int>();
            additionalActionsRemaining = 0;
        }

        private void Start()
        {
            // ComboDatabaseが設定されていない場合、動的に作成
            if (comboDatabase == null)
            {
                CreateDefaultComboDatabase();
            }
            else if (comboDatabase.IsEmpty())
            {
                Debug.LogWarning("ComboDatabase is empty. Please assign combo data.");
            }
        }

        /// <summary>
        /// デフォルトのComboDatabaseを動的作成
        /// </summary>
        private void CreateDefaultComboDatabase()
        {
            Debug.LogWarning("ComboDatabase not assigned. Please assign a ComboDatabase asset in the inspector or create one using Tools > Battle System > Create Combo Database");
            
            // 空のデータベースを作成（エラー回避のため）
            comboDatabase = ScriptableObject.CreateInstance<ComboDatabase>();
        }

        private void OnEnable()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged += HandleTurnChanged;
                battleManager.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged -= HandleTurnChanged;
                battleManager.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        // ターン変更時の処理
        private void HandleTurnChanged(int turn)
        {
            UpdateComboProgresses();
            CheckExpiredCombos();
        }

        // ゲーム状態変更時の処理
        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.PlayerTurn)
            {
                // プレイヤーターン開始時の処理
                ResetTurnSpecificData();
            }
            else if (newState == GameState.EnemyTurn)
            {
                // 敵ターン開始時にコンボ中断判定
                ProcessComboInterruptions();
            }
        }

        // 武器使用時のコンボチェック
        public ComboExecutionResult ProcessWeaponUse(int weaponIndex, GridPosition target)
        {
            WeaponData weapon = battleManager.PlayerData.equippedWeapons[weaponIndex];
            if (weapon == null)
            {
                return new ComboExecutionResult { wasExecuted = false };
            }

            // 現在のコンボ進行状況を更新
            UpdateComboProgressWithWeapon(weaponIndex, weapon);

            // 完成したコンボをチェック
            ComboExecutionResult result = CheckAndExecuteCompletedCombos();

            // 新しいコンボの開始をチェック
            if (!result.wasExecuted)
            {
                CheckForNewComboStart(weaponIndex, weapon);
            }

            return result;
        }

        // コンボ進行状況を武器使用で更新
        private void UpdateComboProgressWithWeapon(int weaponIndex, WeaponData weapon)
        {
            for (int i = activeComboProgresses.Count - 1; i >= 0; i--)
            {
                ComboProgress progress = activeComboProgresses[i];
                
                if (CanUpdateComboWithWeapon(progress, weaponIndex, weapon))
                {
                    progress.usedWeaponIndices.Add(weaponIndex);
                    progress.usedAttackAttributes.Add(weapon.attackAttribute);
                    progress.usedWeaponTypes.Add(weapon.weaponType);
                    progress.currentStep++;
                    
                    UpdateComboProgressPercentage(progress);
                    OnComboProgressUpdated?.Invoke(progress);

                    if (showComboProgress)
                    {
                        Debug.Log($"コンボ進行: {progress.comboData.comboName} - {progress.progressPercentage:P0}");
                    }
                }
            }
        }

        // 武器がコンボ条件を満たすかチェック
        private bool CanUpdateComboWithWeapon(ComboProgress progress, int weaponIndex, WeaponData weapon)
        {
            ComboCondition condition = progress.comboData.condition;

            // 攻撃属性チェック
            if (condition.requiredAttackAttributes != null && condition.requiredAttackAttributes.Length > 0)
            {
                if (!condition.requiredAttackAttributes.Contains(weapon.attackAttribute))
                    return false;
            }

            // 武器属性チェック
            if (condition.requiredWeaponTypes != null && condition.requiredWeaponTypes.Length > 0)
            {
                if (!condition.requiredWeaponTypes.Contains(weapon.weaponType))
                    return false;
            }

            // 特定武器インデックスチェック
            if (condition.requiredWeaponIndices != null && condition.requiredWeaponIndices.Length > 0)
            {
                if (!condition.requiredWeaponIndices.Contains(weaponIndex))
                    return false;
            }

            // 攻撃力閾値チェック
            if (condition.minAttackPower > 0)
            {
                int totalPower = battleManager.PlayerData.baseAttackPower + weapon.basePower;
                if (totalPower < condition.minAttackPower)
                    return false;
            }

            // ターン間隔チェック
            if (condition.maxTurnInterval > 0)
            {
                int turnsSinceStart = battleManager.CurrentTurn - progress.startTurn;
                if (turnsSinceStart > condition.maxTurnInterval)
                {
                    // コンボ失敗
                    OnComboFailed?.Invoke(progress.comboData, "制限時間超過");
                    activeComboProgresses.Remove(progress);
                    return false;
                }
            }

            // シーケンス順序チェック（必要に応じて実装）
            if (condition.requiresSequence)
            {
                return CheckSequenceOrder(progress, weaponIndex, weapon);
            }

            return true;
        }

        // 完成したコンボをチェックして実行
        private ComboExecutionResult CheckAndExecuteCompletedCombos()
        {
            for (int i = activeComboProgresses.Count - 1; i >= 0; i--)
            {
                ComboProgress progress = activeComboProgresses[i];
                
                if (IsComboCompleted(progress))
                {
                    ComboExecutionResult result = ExecuteCombo(progress);
                    activeComboProgresses.RemoveAt(i);
                    return result;
                }
            }

            return new ComboExecutionResult { wasExecuted = false };
        }

        // コンボ完成判定
        private bool IsComboCompleted(ComboProgress progress)
        {
            return progress.currentStep >= progress.comboData.requiredWeaponCount;
        }

        // コンボ実行
        private ComboExecutionResult ExecuteCombo(ComboProgress progress)
        {
            ComboExecutionResult result = new ComboExecutionResult();
            result.wasExecuted = true;
            result.executedCombo = progress.comboData;
            result.appliedEffects = progress.comboData.effects;
            result.additionalActionsGranted = 0;
            result.totalDamageMultiplier = 1.0f;

            List<string> effectMessages = new List<string>();

            // 各効果を適用
            foreach (ComboEffect effect in progress.comboData.effects)
            {
                ApplyComboEffect(effect, result, effectMessages);
            }

            result.resultMessage = string.Join(", ", effectMessages);
            
            progress.isCompleted = true;
            OnComboCompleted?.Invoke(result);

            if (showComboEffects)
            {
                Debug.Log($"コンボ完成: {progress.comboData.comboName} - {result.resultMessage}");
            }

            return result;
        }

        // コンボ効果適用
        private void ApplyComboEffect(ComboEffect effect, ComboExecutionResult result, List<string> messages)
        {
            switch (effect.effectType)
            {
                case ComboEffectType.DamageMultiplier:
                    result.totalDamageMultiplier *= effect.damageMultiplier;
                    messages.Add($"ダメージ {effect.damageMultiplier:P0}");
                    break;

                case ComboEffectType.AdditionalAction:
                    additionalActionsRemaining += effect.additionalActions;
                    result.additionalActionsGranted += effect.additionalActions;
                    messages.Add($"追加行動 +{effect.additionalActions}");
                    break;

                case ComboEffectType.StatusEffect:
                    ApplyStatusEffect(effect);
                    messages.Add($"{GetAttributeName(effect.statusAttribute)}付与");
                    break;

                case ComboEffectType.Healing:
                    battleManager.PlayerData.Heal(effect.healingAmount);
                    messages.Add($"HP回復 +{effect.healingAmount}");
                    break;

                case ComboEffectType.BuffPlayer:
                    ApplyPlayerBuff(effect);
                    messages.Add($"プレイヤー強化");
                    break;

                case ComboEffectType.DebuffEnemy:
                    ApplyEnemyDebuff(effect);
                    messages.Add($"敵弱体化");
                    break;

                case ComboEffectType.SpecialAttack:
                    ExecuteSpecialAttack(effect);
                    messages.Add($"特殊攻撃");
                    break;
            }
        }

        // 新しいコンボの開始をチェック
        private void CheckForNewComboStart(int weaponIndex, WeaponData weapon)
        {
            if (comboDatabase == null || activeComboProgresses.Count >= maxActiveComboCount)
                return;

            foreach (ComboData combo in comboDatabase.AvailableCombos)
            {
                if (CanStartCombo(combo, weaponIndex, weapon))
                {
                    StartNewCombo(combo, weaponIndex, weapon);
                    break; // 一度に1つのコンボのみ開始
                }
            }
        }

        // コンボ開始可能性チェック
        private bool CanStartCombo(ComboData combo, int weaponIndex, WeaponData weapon)
        {
            // 既に同じコンボが進行中でないかチェック
            if (activeComboProgresses.Any(p => p.comboData == combo))
                return false;

            // 武器がコンボの初期条件を満たすかチェック
            ComboCondition condition = combo.condition;

            if (condition.requiredAttackAttributes != null && condition.requiredAttackAttributes.Length > 0)
            {
                if (!condition.requiredAttackAttributes.Contains(weapon.attackAttribute))
                    return false;
            }

            if (condition.requiredWeaponTypes != null && condition.requiredWeaponTypes.Length > 0)
            {
                if (!condition.requiredWeaponTypes.Contains(weapon.weaponType))
                    return false;
            }

            return true;
        }

        // 新しいコンボ開始
        private void StartNewCombo(ComboData combo, int weaponIndex, WeaponData weapon)
        {
            ComboProgress progress = new ComboProgress
            {
                comboData = combo,
                usedWeaponIndices = new List<int> { weaponIndex },
                usedAttackAttributes = new List<AttackAttribute> { weapon.attackAttribute },
                usedWeaponTypes = new List<WeaponType> { weapon.weaponType },
                currentStep = 1,
                totalSteps = combo.requiredWeaponCount,
                startTurn = battleManager.CurrentTurn,
                startTime = Time.time,
                isActive = true,
                isCompleted = false,
                progressPercentage = 1.0f / combo.requiredWeaponCount
            };

            activeComboProgresses.Add(progress);
            OnComboStarted?.Invoke(combo);

            if (showComboProgress)
            {
                Debug.Log($"コンボ開始: {combo.comboName}");
            }
        }

        // コンボ進行率更新
        private void UpdateComboProgressPercentage(ComboProgress progress)
        {
            progress.progressPercentage = (float)progress.currentStep / progress.comboData.requiredWeaponCount;
        }

        // コンボ進行状況更新
        private void UpdateComboProgresses()
        {
            foreach (ComboProgress progress in activeComboProgresses)
            {
                // ターン経過による自然消滅チェック
                int turnsSinceStart = battleManager.CurrentTurn - progress.startTurn;
                if (progress.comboData.condition.maxTurnInterval > 0 && 
                    turnsSinceStart > progress.comboData.condition.maxTurnInterval)
                {
                    OnComboFailed?.Invoke(progress.comboData, "制限時間超過");
                }
            }
        }

        // 期限切れコンボの除去
        private void CheckExpiredCombos()
        {
            for (int i = activeComboProgresses.Count - 1; i >= 0; i--)
            {
                ComboProgress progress = activeComboProgresses[i];
                int turnsSinceStart = battleManager.CurrentTurn - progress.startTurn;
                
                if (progress.comboData.condition.maxTurnInterval > 0 && 
                    turnsSinceStart > progress.comboData.condition.maxTurnInterval)
                {
                    activeComboProgresses.RemoveAt(i);
                }
            }
        }

        // ターン固有データリセット
        private void ResetTurnSpecificData()
        {
            // 追加行動の消費処理は別途実装
        }

        // コンボ中断処理
        private void ProcessComboInterruptions()
        {
            if (!allowComboInterruption)
                return;

            for (int i = activeComboProgresses.Count - 1; i >= 0; i--)
            {
                ComboProgress progress = activeComboProgresses[i];
                
                if (ShouldInterruptCombo(progress))
                {
                    OnComboInterrupted?.Invoke(progress.comboData);
                    activeComboProgresses.RemoveAt(i);
                    
                    if (showComboProgress)
                    {
                        Debug.Log($"コンボ中断: {progress.comboData.comboName}");
                    }
                }
            }
        }

        // コンボ中断判定
        private bool ShouldInterruptCombo(ComboProgress progress)
        {
            if (!progress.comboData.canInterrupt)
                return false;

            float interruptChance = baseInterruptChance * (1.0f - progress.comboData.interruptResistance);
            return UnityEngine.Random.value < interruptChance;
        }

        // 追加行動の消費
        public bool ConsumeAdditionalAction()
        {
            if (additionalActionsRemaining > 0)
            {
                additionalActionsRemaining--;
                return true;
            }
            return false;
        }

        // シーケンス順序チェック
        private bool CheckSequenceOrder(ComboProgress progress, int weaponIndex, WeaponData weapon)
        {
            // 簡易実装：必要に応じて詳細なシーケンス判定を実装
            return true;
        }

        // 状態異常適用
        private void ApplyStatusEffect(ComboEffect effect)
        {
            // 状態異常システムとの連携（後のフェーズで詳細実装）
            Debug.Log($"状態異常付与: {GetAttributeName(effect.statusAttribute)} ({effect.statusDuration}ターン)");
        }

        // プレイヤーバフ適用
        private void ApplyPlayerBuff(ComboEffect effect)
        {
            // バフシステムとの連携（後のフェーズで詳細実装）
            Debug.Log($"プレイヤーバフ: +{effect.buffValue} ({effect.effectDuration}ターン)");
        }

        // 敵デバフ適用
        private void ApplyEnemyDebuff(ComboEffect effect)
        {
            // デバフシステムとの連携（後のフェーズで詳細実装）
            Debug.Log($"敵デバフ: -{effect.buffValue} ({effect.effectDuration}ターン)");
        }

        // 特殊攻撃実行
        private void ExecuteSpecialAttack(ComboEffect effect)
        {
            // 特殊攻撃システムとの連携（後のフェーズで詳細実装）
            Debug.Log($"特殊攻撃実行: {effect.effectDescription}");
        }

        // 属性名取得
        private string GetAttributeName(AttackAttribute attribute)
        {
            switch (attribute)
            {
                case AttackAttribute.Fire: return "炎上";
                case AttackAttribute.Ice: return "凍結";
                case AttackAttribute.Thunder: return "麻痺";
                case AttackAttribute.Wind: return "風圧";
                case AttackAttribute.Earth: return "震動";
                case AttackAttribute.Light: return "盲目";
                case AttackAttribute.Dark: return "呪縛";
                default: return "状態異常";
            }
        }

        // デバッグ用：全コンボリセット
        [ContextMenu("Reset All Combos")]
        public void ResetAllCombos()
        {
            activeComboProgresses.Clear();
            additionalActionsRemaining = 0;
            Debug.Log("全コンボリセット完了");
        }
    }
}