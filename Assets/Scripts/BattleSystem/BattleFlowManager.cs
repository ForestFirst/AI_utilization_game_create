using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    // 戦闘行動の種類
    public enum BattleActionType
    {
        WeaponAttack,    // 武器攻撃
        ItemUse,         // アイテム使用
        Wait,            // 待機
        Escape           // 逃走（実装予定外）
    }

    // 戦闘行動データ
    [Serializable]
    public class BattleAction
    {
        public BattleActionType actionType;
        public int weaponIndex;           // 使用武器のインデックス
        public GridPosition targetPosition; // 攻撃対象の位置
        public int itemId;                // 使用アイテムID
        public int actionPriority;        // 行動優先度

        public BattleAction(BattleActionType type)
        {
            actionType = type;
            weaponIndex = -1;
            targetPosition = new GridPosition(-1, -1);
            itemId = -1;
            actionPriority = 0;
        }
    }

    // 戦闘フロー管理クラス
    public class BattleFlowManager : MonoBehaviour
    {
        [Header("戦闘フロー設定")]
        [SerializeField] private float actionAnimationTime = 1.5f;
        [SerializeField] private float turnTransitionTime = 1.0f;
        [SerializeField] private bool autoProgressTurns = true;

        private BattleManager battleManager;
        private Queue<BattleAction> playerActionQueue;
        private Queue<BattleAction> enemyActionQueue;
        private bool isProcessingActions;

        // イベント定義
        public event Action<BattleAction> OnActionStarted;
        public event Action<BattleAction> OnActionCompleted;
        public event Action OnPlayerTurnStarted;
        public event Action OnEnemyTurnStarted;
        public event Action OnTurnCompleted;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            playerActionQueue = new Queue<BattleAction>();
            enemyActionQueue = new Queue<BattleAction>();
            isProcessingActions = false;
        }

        private void OnEnable()
        {
            if (battleManager != null)
            {
                battleManager.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (battleManager != null)
            {
                battleManager.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        // ゲーム状態変更時の処理
        private void HandleGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.PlayerTurn:
                    StartPlayerTurnFlow();
                    break;
                case GameState.EnemyTurn:
                    StartEnemyTurnFlow();
                    break;
            }
        }

        // プレイヤーターン開始フロー
        private void StartPlayerTurnFlow()
        {
            OnPlayerTurnStarted?.Invoke();
            playerActionQueue.Clear();
            isProcessingActions = false;

            // プレイヤーの行動待機状態に設定
            Debug.Log($"プレイヤーターン {battleManager.CurrentTurn} 開始");
        }

        // 敵ターン開始フロー
        private void StartEnemyTurnFlow()
        {
            OnEnemyTurnStarted?.Invoke();
            enemyActionQueue.Clear();
            
            // 敵の行動を自動的に決定・実行
            StartCoroutine(ProcessEnemyTurnCoroutine());
        }

        // プレイヤー行動の登録
        public bool RegisterPlayerAction(BattleAction action)
        {
            if (battleManager.CurrentState != GameState.PlayerTurn || isProcessingActions)
                return false;

            // 行動の妥当性チェック
            if (!ValidatePlayerAction(action))
                return false;

            playerActionQueue.Enqueue(action);
            
            // 自動進行が有効な場合、即座に処理開始
            if (autoProgressTurns)
            {
                StartCoroutine(ProcessPlayerActionsCoroutine());
            }

            return true;
        }

        // プレイヤー行動の妥当性チェック
        private bool ValidatePlayerAction(BattleAction action)
        {
            PlayerData player = battleManager.PlayerData;

            switch (action.actionType)
            {
                case BattleActionType.WeaponAttack:
                    // 武器使用可能性チェック
                    if (!player.CanUseWeapon(action.weaponIndex))
                        return false;
                    
                    // 攻撃対象の妥当性チェック
                    return ValidateAttackTarget(action.weaponIndex, action.targetPosition);

                case BattleActionType.ItemUse:
                    // アイテム使用可能性チェック（後のフェーズで詳細実装）
                    return true;

                case BattleActionType.Wait:
                    return true;

                default:
                    return false;
            }
        }

        // 攻撃対象の妥当性チェック
        private bool ValidateAttackTarget(int weaponIndex, GridPosition target)
        {
            WeaponData weapon = battleManager.PlayerData.equippedWeapons[weaponIndex];
            if (weapon == null)
                return false;

            BattleField field = battleManager.BattleField;

            switch (weapon.attackRange)
            {
                case AttackRange.SingleFront:
                    // 一番前の敵への攻撃
                    return field.GetFrontEnemyInColumn(target.x) != null;

                case AttackRange.SingleTarget:
                    // 任意の単体への攻撃
                    return field.GetEnemyAt(target) != null || field.CanAttackGate(target.x);

                case AttackRange.Row1:
                case AttackRange.Row2:
                    // 行攻撃（対象行に敵が存在するかチェック）
                    int targetRow = weapon.attackRange == AttackRange.Row1 ? 0 : 1;
                    return field.GetEnemiesInRow(targetRow).Count > 0;

                case AttackRange.Column:
                    // 縦列攻撃
                    return field.GetEnemiesInColumn(target.x).Count > 0 || field.CanAttackGate(target.x);

                case AttackRange.All:
                    // 全体攻撃
                    return field.GetAllEnemies().Count > 0 || field.GetAliveGateCount() > 0;

                default:
                    return false;
            }
        }

        // プレイヤー行動処理コルーチン
        private IEnumerator ProcessPlayerActionsCoroutine()
        {
            isProcessingActions = true;

            while (playerActionQueue.Count > 0)
            {
                BattleAction action = playerActionQueue.Dequeue();
                
                OnActionStarted?.Invoke(action);
                yield return StartCoroutine(ExecutePlayerAction(action));
                OnActionCompleted?.Invoke(action);

                yield return new WaitForSeconds(actionAnimationTime);
            }

            isProcessingActions = false;
            
            // プレイヤーターン終了
            yield return new WaitForSeconds(turnTransitionTime);
            battleManager.EndPlayerTurn(TurnEndReason.ActionCompleted);
        }

        // プレイヤー行動実行
        private IEnumerator ExecutePlayerAction(BattleAction action)
        {
            switch (action.actionType)
            {
                case BattleActionType.WeaponAttack:
                    yield return StartCoroutine(ExecuteWeaponAttack(action));
                    break;

                case BattleActionType.ItemUse:
                    yield return StartCoroutine(ExecuteItemUse(action));
                    break;

                case BattleActionType.Wait:
                    yield return StartCoroutine(ExecuteWait(action));
                    break;
            }
        }

        // 武器攻撃実行
        private IEnumerator ExecuteWeaponAttack(BattleAction action)
        {
            WeaponData weapon = battleManager.PlayerData.equippedWeapons[action.weaponIndex];
            
            Debug.Log($"プレイヤーが {weapon.weaponName} で攻撃");

            // ダメージ計算システムを呼び出し
            DamageCalculationResult result = CalculateWeaponDamage(weapon, action.targetPosition);
            
            // ターゲットへのダメージ適用
            ApplyDamageToTargets(result, action.targetPosition);

            // 武器クールダウン設定
            battleManager.PlayerData.weaponCooldowns[action.weaponIndex] = weapon.cooldownTurns;

            yield return null;
        }

        // アイテム使用実行
        private IEnumerator ExecuteItemUse(BattleAction action)
        {
            Debug.Log($"プレイヤーがアイテム {action.itemId} を使用");
            // アイテムシステムとの連携（後のフェーズで詳細実装）
            yield return null;
        }

        // 待機実行
        private IEnumerator ExecuteWait(BattleAction action)
        {
            Debug.Log("プレイヤーが待機");
            yield return null;
        }

        // 敵ターン処理コルーチン
        private IEnumerator ProcessEnemyTurnCoroutine()
        {
            Debug.Log($"敵ターン {battleManager.CurrentTurn} 開始");

            // 敵の行動決定
            GenerateEnemyActions();

            // 敵行動の実行
            while (enemyActionQueue.Count > 0)
            {
                BattleAction action = enemyActionQueue.Dequeue();
                
                OnActionStarted?.Invoke(action);
                yield return StartCoroutine(ExecuteEnemyAction(action));
                OnActionCompleted?.Invoke(action);

                yield return new WaitForSeconds(actionAnimationTime);
            }

            // 敵召喚処理
            yield return StartCoroutine(ProcessEnemySummoningCoroutine());

            // 敵ターン終了
            yield return new WaitForSeconds(turnTransitionTime);
            OnTurnCompleted?.Invoke();

            // プレイヤーターンに戻る
            if (battleManager.CurrentState == GameState.EnemyTurn)
            {
                // ゲーム終了チェック後、プレイヤーターンに移行
                yield return new WaitForSeconds(0.1f);
            }
        }

        // 敵行動の生成
        private void GenerateEnemyActions()
        {
            List<EnemyInstance> enemies = battleManager.BattleField.GetAllEnemies();

            foreach (EnemyInstance enemy in enemies)
            {
                if (enemy.CanAct())
                {
                    BattleAction enemyAction = GenerateEnemyAction(enemy);
                    if (enemyAction != null)
                    {
                        enemyActionQueue.Enqueue(enemyAction);
                    }
                }
            }
        }

        // 個別敵の行動生成
        private BattleAction GenerateEnemyAction(EnemyInstance enemy)
        {
            // 簡易AI：基本的に攻撃行動を選択
            BattleAction action = new BattleAction(BattleActionType.WeaponAttack);
            action.actionPriority = enemy.enemyData.actionPriority;
            
            return action;
        }

        // 敵行動実行
        private IEnumerator ExecuteEnemyAction(BattleAction action)
        {
            Debug.Log("敵が攻撃");
            
            // 基本的な敵攻撃処理
            int damage = UnityEngine.Random.Range(1000, 2000); // 仮のダメージ値
            battleManager.PlayerData.TakeDamage(damage);

            Debug.Log($"プレイヤーが {damage} ダメージを受けた");
            yield return null;
        }

        // 敵召喚処理コルーチン
        private IEnumerator ProcessEnemySummoningCoroutine()
        {
            // 基本的な敵召喚ロジック（BattleManagerから移植・簡素化）
            foreach (GateData gate in battleManager.BattleField.Gates)
            {
                if (gate.IsDestroyed() || gate.summonPattern == null)
                    continue;

                int turnsSinceLastSummon = battleManager.CurrentTurn - gate.lastSummonTurn;
                if (turnsSinceLastSummon >= gate.summonPattern.summonInterval)
                {
                    // 敵召喚実行
                    for (int i = 0; i < gate.summonPattern.summonCount; i++)
                    {
                        GridPosition emptyPos = battleManager.BattleField.GetRandomEmptyPosition();
                        if (emptyPos.x == -1)
                            break;

                        // 仮の敵召喚（詳細は後のフェーズで実装）
                        Debug.Log($"ゲート {gate.gateId} から敵召喚：位置 ({emptyPos.x}, {emptyPos.y})");
                    }
                    
                    gate.lastSummonTurn = battleManager.CurrentTurn;
                }
            }

            yield return null;
        }

        // ダメージ計算結果構造体
        public struct DamageCalculationResult
        {
            public int baseDamage;
            public bool isCritical;
            public float damageMultiplier;
            public int finalDamage;
        }

        // 武器ダメージ計算
        private DamageCalculationResult CalculateWeaponDamage(WeaponData weapon, GridPosition target)
        {
            DamageCalculationResult result = new DamageCalculationResult();
            
            // 基本ダメージ計算
            result.baseDamage = battleManager.PlayerData.baseAttackPower + weapon.basePower;
            
            // クリティカル判定
            result.isCritical = UnityEngine.Random.Range(0, 100) < weapon.criticalRate;
            result.damageMultiplier = result.isCritical ? 2.0f : 1.0f;
            
            // 最終ダメージ
            result.finalDamage = Mathf.RoundToInt(result.baseDamage * result.damageMultiplier);
            
            return result;
        }

        // ダメージ適用
        private void ApplyDamageToTargets(DamageCalculationResult damage, GridPosition targetPosition)
        {
            BattleField field = battleManager.BattleField;
            
            // ターゲットへダメージ適用（基本実装）
            EnemyInstance target = field.GetEnemyAt(targetPosition);
            if (target != null)
            {
                target.TakeDamage(damage.finalDamage);
                Debug.Log($"{target.enemyData.enemyName} に {damage.finalDamage} ダメージ" + 
                         (damage.isCritical ? " (クリティカル！)" : ""));

                if (!target.IsAlive())
                {
                    field.RemoveEnemy(targetPosition);
                    Debug.Log($"{target.enemyData.enemyName} を撃破");
                }
            }
            else if (field.CanAttackGate(targetPosition.x))
            {
                // ゲート攻撃
                GateData gate = field.Gates.Find(g => g.position.x == targetPosition.x);
                if (gate != null)
                {
                    gate.TakeDamage(damage.finalDamage);
                    Debug.Log($"ゲート {gate.gateId} に {damage.finalDamage} ダメージ");
                }
            }
        }

        // 手動でプレイヤーターンを終了
        public void ForceEndPlayerTurn()
        {
            if (battleManager.CurrentState == GameState.PlayerTurn && !isProcessingActions)
            {
                battleManager.EndPlayerTurn(TurnEndReason.ForcedEnd);
            }
        }
    }
}