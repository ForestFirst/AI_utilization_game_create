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
    /// 実際の敵インスタンス（戦闘中の敵オブジェクト）
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
        
        [Header("バフ・デバフ管理")]
        public Dictionary<string, float> activeBuffs;      // アクティブバフ
        public Dictionary<string, int> buffDurations;      // バフ持続時間

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
            activeBuffs = new Dictionary<string, float>();
            buffDurations = new Dictionary<string, int>();
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
            int actualDamage = Mathf.Max(1, damage - currentDefense);
            currentHp = Mathf.Max(0, currentHp - actualDamage);
            
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
        /// バフを適用
        /// </summary>
        /// <param name="buffName">バフ名</param>
        /// <param name="multiplier">効果倍率</param>
        /// <param name="duration">持続時間（ターン）</param>
        public void ApplyBuff(string buffName, float multiplier, int duration = -1)
        {
            activeBuffs[buffName] = multiplier;
            if (duration > 0)
            {
                buffDurations[buffName] = duration;
            }
            
            Debug.Log($"{EnemyName} gains buff: {buffName} ({multiplier:F1}x)");
        }

        /// <summary>
        /// バフを削除
        /// </summary>
        /// <param name="buffName">バフ名</param>
        public void RemoveBuff(string buffName)
        {
            if (activeBuffs.ContainsKey(buffName))
            {
                activeBuffs.Remove(buffName);
                buffDurations.Remove(buffName);
                Debug.Log($"{EnemyName} loses buff: {buffName}");
            }
        }

        /// <summary>
        /// 指定バフを持っているかチェック
        /// </summary>
        /// <param name="buffName">バフ名</param>
        /// <returns>持っている場合true</returns>
        public bool HasBuff(string buffName)
        {
            return activeBuffs.ContainsKey(buffName);
        }

        /// <summary>
        /// バフ効果を取得
        /// </summary>
        /// <param name="buffName">バフ名</param>
        /// <returns>効果倍率（持っていない場合1.0）</returns>
        public float GetBuffMultiplier(string buffName)
        {
            return activeBuffs.GetValueOrDefault(buffName, 1.0f);
        }

        /// <summary>
        /// ターン終了処理（バフ持続時間管理）
        /// </summary>
        public void OnTurnEnd()
        {
            turnsSinceSpawned++;
            
            // クールダウン減少
            if (actionCooldownRemaining > 0)
                actionCooldownRemaining--;
            
            // バフ持続時間減少
            var expiredBuffs = new List<string>();
            foreach (var kvp in buffDurations.ToList())
            {
                buffDurations[kvp.Key]--;
                if (buffDurations[kvp.Key] <= 0)
                {
                    expiredBuffs.Add(kvp.Key);
                }
            }
            
            // 期限切れバフを削除
            foreach (string buffName in expiredBuffs)
            {
                RemoveBuff(buffName);
            }
        }

        /// <summary>
        /// 現在の実効攻撃力を取得（バフ効果込み）
        /// </summary>
        /// <returns>実効攻撃力</returns>
        public int GetEffectiveAttackPower()
        {
            float multiplier = 1.0f;
            
            // 攻撃力バフを適用
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
            
            // 防御力バフを適用
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
                info += $"\nBuffs: {string.Join(", ", activeBuffs.Keys)}";
            }
            
            return info;
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
    /// 敵データベース - 敵データの管理を行うScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "BattleSystem/EnemyDatabase")]
    public class EnemyDatabase : ScriptableObject
    {
        [Header("敵データ配列")]
        [SerializeField] private EnemyData[] enemies;

        /// <summary>
        /// 敵データ配列のプロパティ
        /// </summary>
        public EnemyData[] Enemies => enemies;

        /// <summary>
        /// IDで敵データを取得
        /// </summary>
        /// <param name="enemyId">敵ID</param>
        /// <returns>敵データ（見つからない場合はnull）</returns>
        public EnemyData GetEnemy(int enemyId)
        {
            if (enemies == null || enemyId < 0 || enemyId >= enemies.Length)
                return null;
                
            return enemies[enemyId];
        }

        /// <summary>
        /// 敵名で敵データを取得
        /// </summary>
        /// <param name="enemyName">敵名</param>
        /// <returns>敵データ（見つからない場合はnull）</returns>
        public EnemyData GetEnemyByName(string enemyName)
        {
            if (enemies == null || string.IsNullOrEmpty(enemyName))
                return null;

            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.enemyName == enemyName)
                    return enemy;
            }
            return null;
        }

        /// <summary>
        /// ランダムな敵データを取得
        /// </summary>
        /// <returns>ランダムな敵データ</returns>
        public EnemyData GetRandomEnemy()
        {
            if (enemies == null || enemies.Length == 0)
                return null;
                
            int randomIndex = UnityEngine.Random.Range(0, enemies.Length);
            return enemies[randomIndex];
        }

        /// <summary>
        /// カテゴリで敵データを絞り込み
        /// </summary>
        /// <param name="category">敵カテゴリ</param>
        /// <returns>該当する敵データ配列</returns>
        public EnemyData[] GetEnemiesByCategory(EnemyCategory category)
        {
            if (enemies == null)
                return new EnemyData[0];

            var result = new System.Collections.Generic.List<EnemyData>();
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.category == category)
                    result.Add(enemy);
            }
            return result.ToArray();
        }

        /// <summary>
        /// 敵データ数を取得
        /// </summary>
        public int Count => enemies?.Length ?? 0;

        /// <summary>
        /// 有効な敵データが存在するかチェック
        /// </summary>
        public bool HasEnemies => enemies != null && enemies.Length > 0;
    }
}