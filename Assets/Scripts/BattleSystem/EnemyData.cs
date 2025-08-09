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

    // 実際の敵インスタンス（戦闘中の敵オブジェクト）
    [Serializable]
    public class EnemyInstance
    {
        public EnemyData enemyData;
        public int currentHp;
        public int currentAttackPower;
        public int gridX, gridY;           // グリッド座標
        public int actionCooldownRemaining; // 残りクールダウン
        public bool[] statusEffects;       // 状態異常配列
        public int turnsSinceSpawned;      // 召喚からの経過ターン

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
    }
}