using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    // AI行動の種類
    public enum AIActionType
    {
        BasicAttack,        // 基本攻撃
        SpecialAttack,      // 特殊攻撃
        DefendAlly,         // 味方防御
        HealAlly,           // 味方回復
        BuffAlly,           // 味方強化
        DebuffPlayer,       // プレイヤー弱体化
        Summon,             // 追加召喚
        SelfDestruct,       // 自爆
        Wait,               // 待機
        Passive             // パッシブ（行動なし）
    }

    // AI行動決定の結果
    [Serializable]
    public struct AIActionDecision
    {
        public AIActionType actionType;
        public EnemyInstance actor;
        public GridPosition targetPosition;
        public EnemyInstance targetAlly;
        public int actionPriority;
        public string actionDescription;
        public bool requiresTarget;
        public float successChance;
    }

    // 敵AI管理システム
    public class EnemyAISystem : MonoBehaviour
    {
        [Header("AI設定")]
        [SerializeField] private float aiThinkingTime = 0.5f;
        [SerializeField] private bool enableRandomization = true;
        [SerializeField] private bool showAIDecisions = true;
        [SerializeField] private int maxAIActionsPerTurn = 10;

        [Header("行動確率調整")]
        [SerializeField] private float specialAttackChance = 0.3f;
        [SerializeField] private float healPriority = 0.8f;
        [SerializeField] private float buffPriority = 0.6f;
        [SerializeField] private float aggressionLevel = 0.7f;

        private BattleManager battleManager;
        private DamageCalculationSystem damageSystem;
        private Dictionary<int, IEnemyAI> enemyAIMap;

        public event Action<AIActionDecision> OnAIActionDecided;
        public event Action<EnemyInstance, AIActionType> OnAIActionExecuted;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            damageSystem = GetComponent<DamageCalculationSystem>();
            enemyAIMap = new Dictionary<int, IEnemyAI>();
            
            InitializeEnemyAIs();
        }

        // 敵AI初期化
        private void InitializeEnemyAIs()
        {
            // 前衛型AI（4種類）
            enemyAIMap[1] = new ShieldDroneAI();     // シールドドローン
            enemyAIMap[2] = new TankRobotAI();       // タンクロボ
            enemyAIMap[3] = new RepairBotAI();       // リペアボット
            enemyAIMap[4] = new BarrierUnitAI();     // バリアユニット

            // 攻撃型AI（4種類）
            enemyAIMap[5] = new AssaultDroneAI();    // アサルトドローン
            enemyAIMap[6] = new SniperBotAI();       // スナイパーボット
            enemyAIMap[7] = new RushUnitAI();        // ラッシュユニット
            enemyAIMap[8] = new BomberDroneAI();     // ボマードローン

            // 支援型AI（4種類）
            enemyAIMap[9] = new CommanderRobotAI();  // コマンダーロボ
            enemyAIMap[10] = new HackerDroneAI();    // ハッカードローン
            enemyAIMap[11] = new JammerUnitAI();     // ジャマーユニット
            enemyAIMap[12] = new BoosterBotAI();     // ブースターボット

            // 特殊型AI（3種類）
            enemyAIMap[13] = new NanoSlimeAI();      // ナノスライム
            enemyAIMap[14] = new MimicDroneAI();     // ミミックドローン
            enemyAIMap[15] = new WarpGateAI();       // ワープゲート
        }

        // 全敵の行動決定
        public List<AIActionDecision> DecideAllEnemyActions()
        {
            List<AIActionDecision> decisions = new List<AIActionDecision>();
            List<EnemyInstance> enemies = battleManager.BattleField.GetAllEnemies();

            foreach (EnemyInstance enemy in enemies)
            {
                if (enemy.CanAct())
                {
                    AIActionDecision decision = DecideEnemyAction(enemy);
                    if (decision.actionType != AIActionType.Passive)
                    {
                        decisions.Add(decision);
                        OnAIActionDecided?.Invoke(decision);
                    }
                }
            }

            // 行動優先度でソート
            decisions.Sort((a, b) => b.actionPriority.CompareTo(a.actionPriority));

            return decisions;
        }

        // 個別敵の行動決定
        public AIActionDecision DecideEnemyAction(EnemyInstance enemy)
        {
            if (enemy?.enemyData == null)
                return CreateWaitDecision(enemy);

            // 敵種別に応じたAIを取得
            IEnemyAI enemyAI = GetEnemyAI(enemy.enemyData.enemyId);
            if (enemyAI == null)
                return CreateBasicAttackDecision(enemy);

            // AI判断実行
            AIContext context = CreateAIContext(enemy);
            AIActionDecision decision = enemyAI.DecideAction(context);

            // ランダム要素の適用
            if (enableRandomization)
            {
                decision = ApplyRandomization(decision, enemy);
            }

            return decision;
        }

        // AI実行
        public void ExecuteAIAction(AIActionDecision decision)
        {
            if (decision.actor == null)
                return;

            IEnemyAI enemyAI = GetEnemyAI(decision.actor.enemyData.enemyId);
            if (enemyAI != null)
            {
                AIContext context = CreateAIContext(decision.actor);
                enemyAI.ExecuteAction(decision, context);
            }
            else
            {
                ExecuteBasicAction(decision);
            }

            OnAIActionExecuted?.Invoke(decision.actor, decision.actionType);
            
            if (showAIDecisions)
            {
                Debug.Log($"{decision.actor.enemyData.enemyName}: {decision.actionDescription}");
            }
        }

        // 敵AI取得
        private IEnemyAI GetEnemyAI(int enemyId)
        {
            enemyAIMap.TryGetValue(enemyId, out IEnemyAI ai);
            return ai;
        }

        // AIコンテキスト作成
        private AIContext CreateAIContext(EnemyInstance enemy)
        {
            return new AIContext
            {
                self = enemy,
                battleManager = battleManager,
                damageSystem = damageSystem,
                playerData = battleManager.PlayerData,
                battleField = battleManager.BattleField,
                allEnemies = battleManager.BattleField.GetAllEnemies(),
                currentTurn = battleManager.CurrentTurn,
                aggressionLevel = aggressionLevel,
                specialAttackChance = specialAttackChance,
                healPriority = healPriority,
                buffPriority = buffPriority
            };
        }

        // ランダム化適用
        private AIActionDecision ApplyRandomization(AIActionDecision decision, EnemyInstance enemy)
        {
            // 成功確率のチェック
            if (decision.successChance < 1.0f)
            {
                if (UnityEngine.Random.value > decision.successChance)
                {
                    return CreateWaitDecision(enemy);
                }
            }

            // 一定確率で行動変更
            if (UnityEngine.Random.value < 0.1f) // 10%の確率で基本攻撃に変更
            {
                return CreateBasicAttackDecision(enemy);
            }

            return decision;
        }

        // 基本攻撃決定の作成
        private AIActionDecision CreateBasicAttackDecision(EnemyInstance enemy)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.BasicAttack,
                actor = enemy,
                targetPosition = new GridPosition(0, 0), // プレイヤー位置（仮）
                actionPriority = 1,
                actionDescription = "基本攻撃",
                requiresTarget = true,
                successChance = 0.9f
            };
        }

        // 待機決定の作成
        private AIActionDecision CreateWaitDecision(EnemyInstance enemy)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.Wait,
                actor = enemy,
                actionPriority = 0,
                actionDescription = "待機",
                requiresTarget = false,
                successChance = 1.0f
            };
        }

        // 基本行動実行
        private void ExecuteBasicAction(AIActionDecision decision)
        {
            switch (decision.actionType)
            {
                case AIActionType.BasicAttack:
                    ExecuteBasicAttack(decision.actor);
                    break;

                case AIActionType.Wait:
                    // 何もしない
                    break;

                default:
                    Debug.LogWarning($"未実装のAI行動: {decision.actionType}");
                    break;
            }
        }

        // 基本攻撃実行
        private void ExecuteBasicAttack(EnemyInstance enemy)
        {
            int damage = enemy.currentAttackPower;
            battleManager.PlayerData.TakeDamage(damage);
            Debug.Log($"{enemy.enemyData.enemyName} がプレイヤーに {damage} ダメージ");
        }
    }

    // AIコンテキスト（AI判断に必要な情報）
    public struct AIContext
    {
        public EnemyInstance self;
        public BattleManager battleManager;
        public DamageCalculationSystem damageSystem;
        public PlayerData playerData;
        public BattleField battleField;
        public List<EnemyInstance> allEnemies;
        public int currentTurn;
        public float aggressionLevel;
        public float specialAttackChance;
        public float healPriority;
        public float buffPriority;
    }

    // 敵AIインターフェース
    public interface IEnemyAI
    {
        AIActionDecision DecideAction(AIContext context);
        void ExecuteAction(AIActionDecision decision, AIContext context);
    }

    // 基本敵AIクラス
    public abstract class BaseEnemyAI : IEnemyAI
    {
        public abstract AIActionDecision DecideAction(AIContext context);
        
        public virtual void ExecuteAction(AIActionDecision decision, AIContext context)
        {
            // 基本実行処理
            switch (decision.actionType)
            {
                case AIActionType.BasicAttack:
                    ExecuteBasicAttack(decision, context);
                    break;
                
                case AIActionType.SpecialAttack:
                    ExecuteSpecialAttack(decision, context);
                    break;
            }
        }

        protected virtual void ExecuteBasicAttack(AIActionDecision decision, AIContext context)
        {
            int damage = decision.actor.currentAttackPower;
            context.playerData.TakeDamage(damage);
        }

        protected virtual void ExecuteSpecialAttack(AIActionDecision decision, AIContext context)
        {
            ExecuteBasicAttack(decision, context);
        }

        protected List<EnemyInstance> GetAlliesInRange(AIContext context, int range = 1)
        {
            List<EnemyInstance> allies = new List<EnemyInstance>();
            GridPosition selfPos = new GridPosition(context.self.gridX, context.self.gridY);

            foreach (EnemyInstance enemy in context.allEnemies)
            {
                if (enemy == context.self)
                    continue;

                GridPosition enemyPos = new GridPosition(enemy.gridX, enemy.gridY);
                int distance = Mathf.Abs(selfPos.x - enemyPos.x) + Mathf.Abs(selfPos.y - enemyPos.y);
                
                if (distance <= range)
                {
                    allies.Add(enemy);
                }
            }

            return allies;
        }

        protected EnemyInstance GetLowestHealthAlly(AIContext context)
        {
            EnemyInstance target = null;
            float lowestHealthPercent = 1.0f;

            foreach (EnemyInstance enemy in context.allEnemies)
            {
                if (enemy == context.self)
                    continue;

                float healthPercent = (float)enemy.currentHp / enemy.enemyData.baseHp;
                if (healthPercent < lowestHealthPercent)
                {
                    lowestHealthPercent = healthPercent;
                    target = enemy;
                }
            }

            return target;
        }
    }

    // 個別AI実装例（シールドドローン）
    public class ShieldDroneAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            // 後ろの敵へのダメージを50%軽減する前衛型
            List<EnemyInstance> rearAllies = GetRearAllies(context);
            
            if (rearAllies.Count > 0 && UnityEngine.Random.value < 0.7f)
            {
                return new AIActionDecision
                {
                    actionType = AIActionType.DefendAlly,
                    actor = context.self,
                    targetAlly = rearAllies[0],
                    actionPriority = 3,
                    actionDescription = "後方の味方を防御",
                    requiresTarget = true,
                    successChance = 0.9f
                };
            }

            return new AIActionDecision
            {
                actionType = AIActionType.BasicAttack,
                actor = context.self,
                actionPriority = 2,
                actionDescription = "基本攻撃",
                requiresTarget = true,
                successChance = 0.8f
            };
        }

        private List<EnemyInstance> GetRearAllies(AIContext context)
        {
            List<EnemyInstance> rearAllies = new List<EnemyInstance>();
            
            foreach (EnemyInstance enemy in context.allEnemies)
            {
                if (enemy.gridY > context.self.gridY) // より後方にいる味方
                {
                    rearAllies.Add(enemy);
                }
            }
            
            return rearAllies;
        }
    }

    // アサルトドローンAI（標準的な攻撃型）
    public class AssaultDroneAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.BasicAttack,
                actor = context.self,
                actionPriority = 2,
                actionDescription = "標準攻撃",
                requiresTarget = true,
                successChance = 0.95f
            };
        }
    }

    // その他のAI実装（簡略版）
    public class TankRobotAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.Passive,
                actor = context.self,
                actionPriority = 0,
                actionDescription = "攻撃を2回無効化（パッシブ）",
                requiresTarget = false,
                successChance = 1.0f
            };
        }
    }

    public class RepairBotAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            EnemyInstance target = GetLowestHealthAlly(context);
            if (target != null)
            {
                return new AIActionDecision
                {
                    actionType = AIActionType.HealAlly,
                    actor = context.self,
                    targetAlly = target,
                    actionPriority = 4,
                    actionDescription = "隣接する味方を回復",
                    requiresTarget = true,
                    successChance = 0.9f
                };
            }

            return new AIActionDecision
            {
                actionType = AIActionType.Wait,
                actor = context.self,
                actionPriority = 0,
                actionDescription = "回復対象なし - 待機",
                requiresTarget = false,
                successChance = 1.0f
            };
        }
    }

    public class BarrierUnitAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.BuffAlly,
                actor = context.self,
                actionPriority = 3,
                actionDescription = "縦列全体にバリア展開",
                requiresTarget = false,
                successChance = 0.95f
            };
        }
    }

    public class SniperBotAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            bool canUseSpecial = context.currentTurn % 2 == 0; // 2ターンに1回特殊攻撃
            
            if (canUseSpecial)
            {
                return new AIActionDecision
                {
                    actionType = AIActionType.SpecialAttack,
                    actor = context.self,
                    actionPriority = 3,
                    actionDescription = "確定ダメージ攻撃",
                    requiresTarget = true,
                    successChance = 1.0f
                };
            }

            return new AIActionDecision
            {
                actionType = AIActionType.Wait,
                actor = context.self,
                actionPriority = 1,
                actionDescription = "特殊攻撃の準備",
                requiresTarget = false,
                successChance = 1.0f
            };
        }
    }

    public class RushUnitAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.BasicAttack,
                actor = context.self,
                actionPriority = 5, // 高優先度（召喚と同時ターンに攻撃可能）
                actionDescription = "高速攻撃",
                requiresTarget = true,
                successChance = 0.9f
            };
        }
    }

    public class BomberDroneAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            if (context.self.currentHp <= 0) // 撃破時の自爆処理は別システムで処理
            {
                return new AIActionDecision
                {
                    actionType = AIActionType.SelfDestruct,
                    actor = context.self,
                    actionPriority = 10, // 最高優先度
                    actionDescription = "撃破時爆発",
                    requiresTarget = false,
                    successChance = 1.0f
                };
            }

            return new AIActionDecision
            {
                actionType = AIActionType.BasicAttack,
                actor = context.self,
                actionPriority = 2,
                actionDescription = "通常攻撃",
                requiresTarget = true,
                successChance = 0.85f
            };
        }
    }

    public class CommanderRobotAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.BuffAlly,
                actor = context.self,
                actionPriority = 4,
                actionDescription = "全味方の攻撃力+30%",
                requiresTarget = false,
                successChance = 1.0f
            };
        }
    }

    public class HackerDroneAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.DebuffPlayer,
                actor = context.self,
                actionPriority = 3,
                actionDescription = "武器クールダウン+1ターン",
                requiresTarget = true,
                successChance = 0.8f
            };
        }
    }

    public class JammerUnitAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.DebuffPlayer,
                actor = context.self,
                actionPriority = 3,
                actionDescription = "コンボ中断試行",
                requiresTarget = true,
                successChance = 0.6f
            };
        }
    }

    public class BoosterBotAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.Passive,
                actor = context.self,
                actionPriority = 0,
                actionDescription = "召喚ペース加速（パッシブ）",
                requiresTarget = false,
                successChance = 1.0f
            };
        }
    }

    public class NanoSlimeAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            // 同じマスの味方と合成を試行
            EnemyInstance mergeTarget = FindMergeTarget(context);
            if (mergeTarget != null)
            {
                return new AIActionDecision
                {
                    actionType = AIActionType.SpecialAttack,
                    actor = context.self,
                    targetAlly = mergeTarget,
                    actionPriority = 6,
                    actionDescription = "ナノスライム合成",
                    requiresTarget = true,
                    successChance = 1.0f
                };
            }

            return new AIActionDecision
            {
                actionType = AIActionType.BasicAttack,
                actor = context.self,
                actionPriority = 1,
                actionDescription = "通常攻撃",
                requiresTarget = true,
                successChance = 0.9f
            };
        }

        private EnemyInstance FindMergeTarget(AIContext context)
        {
            foreach (EnemyInstance enemy in context.allEnemies)
            {
                if (enemy != context.self && 
                    enemy.gridX == context.self.gridX && 
                    enemy.gridY == context.self.gridY &&
                    enemy.enemyData.enemyName == "ナノスライム")
                {
                    return enemy;
                }
            }
            return null;
        }
    }

    public class MimicDroneAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            return new AIActionDecision
            {
                actionType = AIActionType.SpecialAttack,
                actor = context.self,
                actionPriority = 2,
                actionDescription = "前回プレイヤー攻撃をコピー",
                requiresTarget = true,
                successChance = 0.9f
            };
        }
    }

    public class WarpGateAI : BaseEnemyAI
    {
        public override AIActionDecision DecideAction(AIContext context)
        {
            bool canSummon = context.currentTurn % 2 == 0; // 2ターンに1回召喚
            
            if (canSummon)
            {
                return new AIActionDecision
                {
                    actionType = AIActionType.Summon,
                    actor = context.self,
                    actionPriority = 4,
                    actionDescription = "敵追加召喚",
                    requiresTarget = false,
                    successChance = 0.8f
                };
            }

            return new AIActionDecision
            {
                actionType = AIActionType.Passive,
                actor = context.self,
                actionPriority = 0,
                actionDescription = "召喚待機",
                requiresTarget = false,
                successChance = 1.0f
            };
        }
    }
}