using System;
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

    // ゲート召喚パターンの定義
    [Serializable]
    public class GateSummonPattern
    {
        public string patternName;
        public int summonInterval;      // 召喚間隔（ターン）
        public int summonCount;         // 一回の召喚数
        public bool isInitialSummon;    // 初回召喚フラグ
        public int[] allowedEnemyIds;   // 召喚可能敵ID配列
    }

    // 敵データベース管理クラス
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "BattleSystem/EnemyDatabase")]
    public class EnemyDatabase : ScriptableObject
    {
        [SerializeField] private EnemyData[] enemies;
        [SerializeField] private GateSummonPattern[] summonPatterns;
        
        public EnemyData[] Enemies => enemies;
        public GateSummonPattern[] SummonPatterns => summonPatterns;
        
        public EnemyData GetEnemy(int enemyId)
        {
            return System.Array.Find(enemies, enemy => enemy.enemyId == enemyId);
        }
        
        public EnemyData[] GetEnemiesByCategory(EnemyCategory category)
        {
            return System.Array.FindAll(enemies, enemy => enemy.category == category);
        }
        
        public GateSummonPattern GetSummonPattern(string patternName)
        {
            return System.Array.Find(summonPatterns, pattern => pattern.patternName == patternName);
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
    
    /// <summary>
    /// 実際の敵インスタンス（戦闘中の敵オブジェクト）- 拡張版
    /// </summary>
    [Serializable]
    public class EnemyInstance
    {
        [Header("基本データ")]
        public EnemyData enemyData;
        public int currentHp;
        public int currentAttackPower;
        public int gridX, gridY;           // グリッド座標
        public int actionCooldownRemaining; // 残りクールダウン
        public bool[] statusEffects;       // 状態異常配列
        public int turnsSinceSpawned;      // 召喚からの経過ターン
        
        [Header("ゲート連携")]
        public int assignedGateId = -1;    // 所属ゲートID（-1で未割り当て）
        
        [Header("バフ/デバフシステム")]
        public System.Collections.Generic.List<BuffEffect> activeBuffs; // アクティブなバフ/デバフ
        
        [Header("戦闘統計")]
        public int damageDealt;            // 与えたダメージ累計
        public int damageTaken;           // 受けたダメージ累計
        public int turnsAlive;            // 生存ターン数

        public EnemyInstance(EnemyData data, int x, int y)
        {
            enemyData = data;
            currentHp = data.baseHp;
            currentAttackPower = data.attackPower;
            gridX = x;
            gridY = y;
            actionCooldownRemaining = 0;
            statusEffects = new bool[10]; // 状態異常の種類数に合わせて調整
            turnsSinceSpawned = 0;
            
            // 新規フィールドの初期化
            assignedGateId = -1;
            activeBuffs = new System.Collections.Generic.List<BuffEffect>();
            damageDealt = 0;
            damageTaken = 0;
            turnsAlive = 0;
        }

        public bool IsAlive()
        {
            return currentHp > 0;
        }

        public void TakeDamage(int damage)
        {
            currentHp = Mathf.Max(0, currentHp - damage);
        }

        public bool CanAct()
        {
            return IsAlive() && actionCooldownRemaining <= 0;
        }
        
        /// <summary>
        /// バフ/デバフの適用
        /// </summary>
        public void ApplyBuff(string buffName, float effectValue, int duration, bool isDebuff = false)
        {
            // 既存の同名バフを削除（重複回避）
            activeBuffs.RemoveAll(buff => buff.buffName == buffName);
            
            // 新しいバフを追加
            BuffEffect newBuff = new BuffEffect(buffName, effectValue, duration, isDebuff);
            activeBuffs.Add(newBuff);
            
            Debug.Log($"{enemyData.enemyName}に{buffName}を適用: 効果値{effectValue}, 持続{duration}ターン");
        }
        
        /// <summary>
        /// HP回復処理
        /// </summary>
        public void Heal(int healAmount)
        {
            int actualHeal = Mathf.Min(healAmount, enemyData.baseHp - currentHp);
            currentHp = Mathf.Min(enemyData.baseHp, currentHp + healAmount);
            
            if (actualHeal > 0)
            {
                Debug.Log($"{enemyData.enemyName}が{actualHeal}HP回復（{currentHp}/{enemyData.baseHp}）");
            }
        }
        
        /// <summary>
        /// ダメージ処理の拡張（統計記録付き）
        /// </summary>
        public void TakeDamageWithStats(int damage)
        {
            int actualDamage = Mathf.Min(damage, currentHp);
            currentHp = Mathf.Max(0, currentHp - damage);
            damageTaken += actualDamage;
            
            Debug.Log($"{enemyData.enemyName}が{actualDamage}ダメージを受けた（{currentHp}/{enemyData.baseHp}）");
        }
        
        /// <summary>
        /// バフ/デバフの効果を計算した実攻撃力を取得
        /// </summary>
        public int GetEffectiveAttackPower()
        {
            float multiplier = 1.0f;
            
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
            
            return Mathf.RoundToInt(currentAttackPower * multiplier);
        }
        
        /// <summary>
        /// バフ/デバフの効果を計算した実防御力を取得
        /// </summary>
        public int GetEffectiveDefense()
        {
            float multiplier = 1.0f;
            
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
            
            return Mathf.RoundToInt(enemyData.defense * multiplier);
        }
        
        /// <summary>
        /// ターン終了時のバフ/デバフ更新処理
        /// </summary>
        public void ProcessEndTurn()
        {
            turnsAlive++;
            turnsSinceSpawned++;
            
            if (actionCooldownRemaining > 0)
                actionCooldownRemaining--;
                
            // バフ/デバフのターン数減算と期限切れバフの削除
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                activeBuffs[i].DecrementTurn();
                if (!activeBuffs[i].IsActive())
                {
                    Debug.Log($"{enemyData.enemyName}の{activeBuffs[i].buffName}が期限切れ");
                    activeBuffs.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 敵の詳細状態をデバッグログに出力
        /// </summary>
        public void LogStatus()
        {
            string buffList = "";
            foreach (BuffEffect buff in activeBuffs)
            {
                buffList += $"{buff.buffName}({buff.effectValue}x{buff.remainingTurns}) ";
            }
            
            Debug.Log($"[{enemyData.enemyName}] HP:{currentHp}/{enemyData.baseHp}, 攻撃力:{GetEffectiveAttackPower()}, 防御力:{GetEffectiveDefense()}, バフ:[{buffList}], ゲート:{assignedGateId}");
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