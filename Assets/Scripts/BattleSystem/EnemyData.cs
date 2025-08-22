using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    // 敵の種類カテゴリ
    public enum EnemyCategory
    {
        Vanguard,  // 前衛型
        Attacker,  // 攻撃型
        Support,   // 支援型
        Special    // 特殊型
    }

    // 敵の行動タイプ
    public enum EnemyActionType
    {
        Attack,           // 通常攻撃
        DefendAlly,       // 味方を守る
        BuffAlly,         // 味方を強化
        DebuffPlayer,     // プレイヤーにデバフ
        Heal,             // 回復
        Summon,           // 召喚
        SelfDestruct,     // 自爆
        Counter,          // カウンター
        NoAction          // 行動しない（パッシブ系）
    }

    // 敵データの基本構造
    [Serializable]
    public class EnemyData
    {
        [Header("基本情報")]
        public string enemyName;
        public EnemyCategory category;
        public int enemyId;
        
        [Header("戦闘パラメータ")]
        public int baseHp;              // 基本HP（初期値5,000）
        public int attackPower;         // 攻撃力（初期値1,500程度）
        public int defense;             // 防御力
        public int actionPriority;      // 行動優先度
        
        [Header("行動パターン")]
        public EnemyActionType primaryAction;   // 主要行動
        public EnemyActionType secondaryAction; // 副次行動
        public int actionCooldown;              // 行動クールダウン
        
        [Header("特殊能力")]
        public string specialAbility;      // 特殊能力の説明
        public int abilityValue;           // 能力の効果値
        public int abilityDuration;        // 能力継続ターン数
        public bool isPassiveAbility;      // パッシブ能力フラグ
        
        [Header("召喚システム")]
        public bool canBeSummoned;         // 召喚可能フラグ
        public int summonWeight;           // 召喚重み（確率計算用）

        public EnemyData()
        {
            enemyName = "";
            category = EnemyCategory.Attacker;
            enemyId = 0;
            baseHp = 5000;
            attackPower = 1500;
            defense = 0;
            actionPriority = 1;
            primaryAction = EnemyActionType.Attack;
            secondaryAction = EnemyActionType.NoAction;
            actionCooldown = 0;
            specialAbility = "";
            abilityValue = 0;
            abilityDuration = 0;
            isPassiveAbility = false;
            canBeSummoned = true;
            summonWeight = 100;
        }
    }

    /// <summary>
    /// バフ/デバフ効果の管理
    /// </summary>
    [Serializable]
    public class BuffEffect
    {
        public string buffName;
        public float effectValue;        // 効果倍率（1.0が基準値）
        public int remainingTurns;       // 残りターン数（-1で永続）
        public bool isDebuff;            // デバフかどうか
        
        public BuffEffect(string name, float value, int turns, bool debuff = false)
        {
            buffName = name;
            effectValue = value;
            remainingTurns = turns;
            isDebuff = debuff;
        }
        
        public bool IsActive()
        {
            return remainingTurns != 0;
        }
        
        public void DecrementTurn()
        {
            if (remainingTurns > 0)
                remainingTurns--;
        }
    }

    // 敵データベース管理クラス
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "BattleSystem/EnemyDatabase")]
    public class EnemyDatabase : ScriptableObject
    {
        [SerializeField] private EnemyData[] enemies;
        [SerializeField] private GateSummonPatternData[] summonPatterns;
        
        public EnemyData[] Enemies => enemies;
        
        public EnemyData GetEnemy(int enemyId)
        {
            return System.Array.Find(enemies, enemy => enemy.enemyId == enemyId);
        }
        
        public EnemyData[] GetEnemiesByCategory(EnemyCategory category)
        {
            return System.Array.FindAll(enemies, enemy => enemy.category == category);
        }
        
        public GateSummonPatternData GetSummonPattern(string patternName)
        {
            return System.Array.Find(summonPatterns, pattern => pattern.patternName == patternName);
        }
    }

    /// <summary>
    /// 実際の敵インスタンス（戦闘中の敵オブジェクト）- 拡張版
    /// ゲートシステム対応版
    /// </summary>
    [Serializable]
    public class EnemyInstance
    {
        [Header("基本データ")]
        public EnemyData enemyData;
        public int currentHp;
        public int currentAttackPower;
        public int currentDefense;
        
        [Header("位置・所属情報")]
        public int gridX, gridY;           // グリッド座標
        public int assignedGateId;         // 所属ゲートID（ゲートシステム対応）
        
        [Header("戦闘状態")]
        public int actionCooldownRemaining; // 残りクールダウン
        public bool[] statusEffects;       // 状態異常配列
        public int turnsSinceSpawned;      // 召喚からの経過ターン
        
        [Header("バフ/デバフシステム")]
        public System.Collections.Generic.List<BuffEffect> activeBuffs; // アクティブなバフ/デバフ（詳細版）
        public Dictionary<string, float> buffMultipliers;      // バフ効果倍率（互換性用）
        public Dictionary<string, int> buffDurations;          // バフ持続時間（互換性用）
        
        [Header("戦闘統計")]
        public int damageDealt;            // 与えたダメージ累計
        public int damageTaken;           // 受けたダメージ累計
        public int turnsAlive;            // 生存ターン数

        // プロパティ
        public string EnemyName => enemyData?.enemyName ?? "Unknown";
        public int MaxHp => enemyData?.baseHp ?? 1;
        public EnemyCategory Category => enemyData?.category ?? EnemyCategory.Attacker;
        public GridPosition GridPosition => new GridPosition(gridX, gridY);

        public EnemyInstance(EnemyData data, int x, int y, int gateId = -1)
        {
            enemyData = data;
            currentHp = data.baseHp;
            currentAttackPower = data.attackPower;
            currentDefense = data.defense;
            gridX = x;
            gridY = y;
            assignedGateId = gateId;
            actionCooldownRemaining = 0;
            statusEffects = new bool[10]; // 状態異常の種類数に合わせて調整
            turnsSinceSpawned = 0;
            
            // バフシステム初期化（詳細版と互換性版の両方をサポート）
            activeBuffs = new System.Collections.Generic.List<BuffEffect>();
            buffMultipliers = new Dictionary<string, float>();
            buffDurations = new Dictionary<string, int>();
            
            // 統計初期化
            damageDealt = 0;
            damageTaken = 0;
            turnsAlive = 0;
        }

        /// <summary>
        /// 敵が生存しているかチェック
        /// </summary>
        /// <returns>生存している場合true</returns>
        public bool IsAlive()
        {
            return currentHp > 0;
        }

        /// <summary>
        /// ダメージを受ける
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        public void TakeDamage(int damage)
        {
            // 防御力を考慮したダメージ計算
            int actualDamage = Mathf.Max(1, damage - GetEffectiveDefense());
            int oldHp = currentHp;
            currentHp = Mathf.Max(0, currentHp - actualDamage);
            damageTaken += actualDamage;
            
            Debug.Log($"{EnemyName} takes {actualDamage} damage (HP: {currentHp}/{MaxHp})");
        }

        /// <summary>
        /// HPを回復
        /// </summary>
        /// <param name="amount">回復量</param>
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            
            int oldHp = currentHp;
            currentHp = Mathf.Min(MaxHp, currentHp + amount);
            
            Debug.Log($"{EnemyName} healed {currentHp - oldHp} HP (HP: {currentHp}/{MaxHp})");
        }

        /// <summary>
        /// 行動可能かチェック
        /// </summary>
        /// <returns>行動可能な場合true</returns>
        public bool CanAct()
        {
            return IsAlive() && actionCooldownRemaining <= 0;
        }
        
        /// <summary>
        /// バフ/デバフの適用（詳細版）
        /// </summary>
        public void ApplyBuff(string buffName, float effectValue, int duration, bool isDebuff = false)
        {
            // 既存の同名バフを削除（重複回避）
            activeBuffs.RemoveAll(buff => buff.buffName == buffName);
            
            // 新しいバフを追加
            BuffEffect newBuff = new BuffEffect(buffName, effectValue, duration, isDebuff);
            activeBuffs.Add(newBuff);
            
            // 互換性用辞書も更新
            buffMultipliers[buffName] = effectValue;
            if (duration > 0)
                buffDurations[buffName] = duration;
            
            Debug.Log($"{enemyData.enemyName}に{buffName}を適用: 効果値{effectValue}, 持続{duration}ターン");
        }

        /// <summary>
        /// バフを適用（簡易版・互換性用）
        /// </summary>
        /// <param name="buffName">バフ名</param>
        /// <param name="multiplier">効果倍率</param>
        /// <param name="duration">持続時間（ターン）</param>
        public void ApplyBuff(string buffName, float multiplier, int duration = -1)
        {
            ApplyBuff(buffName, multiplier, duration, false);
        }

        /// <summary>
        /// バフを削除
        /// </summary>
        /// <param name="buffName">バフ名</param>
        public void RemoveBuff(string buffName)
        {
            activeBuffs.RemoveAll(buff => buff.buffName == buffName);
            buffMultipliers.Remove(buffName);
            buffDurations.Remove(buffName);
            
            Debug.Log($"{EnemyName} loses buff: {buffName}");
        }

        /// <summary>
        /// 指定バフを持っているかチェック
        /// </summary>
        /// <param name="buffName">バフ名</param>
        /// <returns>持っている場合true</returns>
        public bool HasBuff(string buffName)
        {
            return activeBuffs.Any(buff => buff.buffName == buffName && buff.IsActive()) ||
                   buffMultipliers.ContainsKey(buffName);
        }

        /// <summary>
        /// バフ効果を取得
        /// </summary>
        /// <param name="buffName">バフ名</param>
        /// <returns>効果倍率（持っていない場合1.0）</returns>
        public float GetBuffMultiplier(string buffName)
        {
            var buff = activeBuffs.FirstOrDefault(b => b.buffName == buffName && b.IsActive());
            if (buff != null)
                return buff.effectValue;
                
            return buffMultipliers.ContainsKey(buffName) ? buffMultipliers[buffName] : 1.0f;
        }

        /// <summary>
        /// ターン終了処理（バフ持続時間管理）
        /// </summary>
        public void OnTurnEnd()
        {
            turnsSinceSpawned++;
            turnsAlive++;
            
            // クールダウン減少
            if (actionCooldownRemaining > 0)
                actionCooldownRemaining--;
            
            // 詳細バフシステムの持続時間減少と期限切れ処理
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i].DecrementTurn();
                if (!activeBuffs[i].IsActive())
                {
                    Debug.Log($"{enemyData.enemyName}の{activeBuffs[i].buffName}が期限切れ");
                    activeBuffs.RemoveAt(i);
                }
            }
            
            // 互換性用辞書も更新
            var expiredBuffs = new List<string>();
            foreach (var kvp in buffDurations.ToList())
            {
                buffDurations[kvp.Key]--;
                if (buffDurations[kvp.Key] <= 0)
                {
                    expiredBuffs.Add(kvp.Key);
                }
            }
            
            foreach (string buffName in expiredBuffs)
            {
                buffMultipliers.Remove(buffName);
                buffDurations.Remove(buffName);
            }
        }

        /// <summary>
        /// 現在の実効攻撃力を取得（バフ効果込み）
        /// </summary>
        /// <returns>実効攻撃力</returns>
        public int GetEffectiveAttackPower()
        {
            float multiplier = 1.0f;
            
            // 詳細バフシステムからの効果適用
            foreach (BuffEffect buff in activeBuffs)
            {
                if (buff.IsActive() && (buff.buffName.Contains("Attack") || buff.buffName.Contains("GateBoost")))
                {
                    if (buff.isDebuff)
                        multiplier *= (2.0f - buff.effectValue); // デバフは減算効果
                    else
                        multiplier *= buff.effectValue; // バフは乗算効果
                }
            }
            
            // 互換性用バフもチェック
            if (HasBuff("AttackBoost"))
                multiplier *= GetBuffMultiplier("AttackBoost");
            
            return Mathf.RoundToInt(currentAttackPower * multiplier);
        }

        /// <summary>
        /// 現在の実効防御力を取得（バフ効果込み）
        /// </summary>
        /// <returns>実効防御力</returns>
        public int GetEffectiveDefense()
        {
            float multiplier = 1.0f;
            
            // 詳細バフシステムからの効果適用
            foreach (BuffEffect buff in activeBuffs)
            {
                if (buff.IsActive() && buff.buffName.Contains("Defense"))
                {
                    if (buff.isDebuff)
                        multiplier *= (2.0f - buff.effectValue);
                    else
                        multiplier *= buff.effectValue;
                }
            }
            
            // 互換性用バフもチェック
            if (HasBuff("DefenseBoost"))
                multiplier *= GetBuffMultiplier("DefenseBoost");
            
            return Mathf.RoundToInt(currentDefense * multiplier);
        }

        /// <summary>
        /// 敵の詳細情報を取得
        /// </summary>
        /// <returns>詳細情報文字列</returns>
        public string GetDetailInfo()
        {
            var info = $"{EnemyName} ({Category})\n";
            info += $"HP: {currentHp}/{MaxHp}\n";
            info += $"Attack: {GetEffectiveAttackPower()}";
            if (currentAttackPower != GetEffectiveAttackPower())
                info += $" (base: {currentAttackPower})";
            info += $"\nDefense: {GetEffectiveDefense()}";
            if (currentDefense != GetEffectiveDefense())
                info += $" (base: {currentDefense})";
            info += $"\nPosition: ({gridX}, {gridY})";
            if (assignedGateId >= 0)
                info += $"\nGate: {assignedGateId}";
            
            if (activeBuffs.Count > 0)
            {
                var buffNames = activeBuffs.Where(b => b.IsActive()).Select(b => b.buffName);
                info += $"\nBuffs: {string.Join(", ", buffNames)}";
            }
            
            return info;
        }
        
        /// <summary>
        /// 敵の詳細状態をデバッグログに出力
        /// </summary>
        public void LogStatus()
        {
            string buffList = "";
            foreach (BuffEffect buff in activeBuffs)
            {
                if (buff.IsActive())
                    buffList += $"{buff.buffName}({buff.effectValue}x{buff.remainingTurns}) ";
            }
            
            Debug.Log($"[{enemyData.enemyName}] HP:{currentHp}/{enemyData.baseHp}, 攻撃力:{GetEffectiveAttackPower()}, 防御力:{GetEffectiveDefense()}, バフ:[{buffList}], ゲート:{assignedGateId}");
        }

        /// <summary>
        /// デバッグ用文字列表現
        /// </summary>
        /// <returns>デバッグ情報</returns>
        public override string ToString()
        {
            return $"{EnemyName}[{gridX},{gridY}] HP:{currentHp}/{MaxHp} Gate:{assignedGateId}";
        }
    }
    
    /// <summary>
    /// ゲート召喚パターンデータ（名前変更で重複回避）
    /// </summary>
    [Serializable]
    public class GateSummonPatternData
    {
        public string patternName;
        public int summonInterval;      // 召喚間隔（ターン）
        public int summonCount;         // 一回の召喚数
        public int[] allowedEnemyIds;   // 召喚可能敵ID配列
        
        public GateSummonPatternData()
        {
            patternName = "DefaultPattern";
            summonInterval = 3;
            summonCount = 1;
            allowedEnemyIds = new int[] { 0 };
        }
    }
}