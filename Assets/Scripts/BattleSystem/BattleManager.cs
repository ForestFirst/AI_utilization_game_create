using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    // ターゲット選択の種類
    public enum TargetType
    {
        None,           // 選択なし
        Column,         // 列選択（ゲート攻撃）
        EnemyPosition   // 敵位置選択
    }

    // ターゲット選択データ
    [Serializable]
    public class TargetSelection
    {
        public TargetType targetType;
        public int columnIndex;         // 選択された列（ゲート攻撃用）
        public GridPosition position;   // 選択された位置
        public bool isValid;            // 選択が有効かどうか
        
        public TargetSelection()
        {
            targetType = TargetType.None;
            columnIndex = -1;
            position = GridPosition.Invalid;
            isValid = false;
        }
        
        public void SetColumnTarget(int column)
        {
            targetType = TargetType.Column;
            columnIndex = column;
            position = new GridPosition(column, 0); // 列先頭
            isValid = true;
        }
        
        public void SetEnemyTarget(GridPosition enemyPos)
        {
            targetType = TargetType.EnemyPosition;
            columnIndex = enemyPos.x;
            position = enemyPos;
            isValid = true;
        }
        
        public void Clear()
        {
            targetType = TargetType.None;
            columnIndex = -1;
            position = GridPosition.Invalid;
            isValid = false;
        }
    }

    // ゲームの状態定義
    public enum GameState
    {
        Initializing,    // 初期化中
        PlayerTurn,      // プレイヤーターン
        EnemyTurn,       // 敵ターン
        Victory,         // 勝利
        Defeat,          // 敗北
        Paused           // 一時停止
    }

    // ターン終了理由
    public enum TurnEndReason
    {
        ActionCompleted, // 行動完了
        TimeOut,         // 制限時間切れ
        ForcedEnd        // 強制終了
    }

    // 勝利条件の種類
    public enum VictoryCondition
    {
        AllGatesDestroyed,  // 全ゲート破壊
        AllEnemiesDefeated, // 全敵撃破
        TimeLimit          // 制限ターン内達成
    }

    // プレイヤーデータの基本構造
    [Serializable]
    public class PlayerData
    {
        public int maxHp;           // 最大HP
        public int currentHp;       // 現在HP
        public int baseAttackPower; // 基本攻撃力
        public WeaponData[] equippedWeapons; // 装備武器配列（4個）
        public int[] weaponCooldowns;        // 武器クールダウン配列

        public PlayerData()
        {
            maxHp = 15000;
            currentHp = 15000;
            baseAttackPower = 100;
            equippedWeapons = new WeaponData[4];
            weaponCooldowns = new int[4];
        }

        public bool IsAlive()
        {
            return currentHp > 0;
        }

        public void TakeDamage(int damage)
        {
            currentHp = Mathf.Max(0, currentHp - damage);
        }

        public void Heal(int healAmount)
        {
            currentHp = Mathf.Min(maxHp, currentHp + healAmount);
        }

        public bool CanUseWeapon(int weaponIndex)
        {
            return weaponIndex >= 0 && weaponIndex < 4 && 
                   equippedWeapons[weaponIndex] != null && 
                   weaponCooldowns[weaponIndex] <= 0;
        }
    }

    // 戦闘結果データ
    [Serializable]
    public class BattleResult
    {
        public bool isVictory;
        public VictoryCondition victoryCondition;
        public int turnsUsed;
        public int totalDamageDealt;
        public int totalDamageTaken;
        public int enemiesDefeated;
        public int gatesDestroyed;
    }

    // 基本ゲームマネージャー
    public class BattleManager : MonoBehaviour
    {
        [Header("戦闘設定")]
        [SerializeField] private int maxTurns = 50;         // 最大ターン数
        [SerializeField] private int gateCount = 3;         // ゲート数
        [SerializeField] private float turnTimeLimit = 30f; // ターン制限時間（秒）

        [Header("データベース")]
        [SerializeField] private WeaponDatabase weaponDatabase;
        [SerializeField] private EnemyDatabase enemyDatabase;

        // 戦闘状態管理
        private GameState currentState;
        private int currentTurn;
        private float turnTimer;
        private BattleField battleField;
        private PlayerData playerData;
        private BattleResult battleResult;
        
        // ターゲット選択システム
        private TargetSelection currentTarget;
        private TargetSelection lastSelectedTarget;  // 選択記憶機能
        private bool waitingForWeaponSelection;      // 武器選択待ち状態

        // イベント定義
        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnTurnChanged;
        public event Action<PlayerData> OnPlayerDataChanged;
        public event Action<BattleResult> OnBattleEnded;
        public event Action<TargetSelection> OnTargetSelected;    // ターゲット選択時
        public event Action OnTargetCleared;                      // ターゲットクリア時

        // プロパティ
        public GameState CurrentState => currentState;
        public int CurrentTurn => currentTurn;
        public int MaxTurns => maxTurns;
        public BattleField BattleField => battleField;
        public PlayerData PlayerData => playerData;
        public TargetSelection CurrentTarget => currentTarget;           // 現在のターゲット選択
        public TargetSelection LastSelectedTarget => lastSelectedTarget; // 前回のターゲット選択
        public bool IsWaitingForWeaponSelection => waitingForWeaponSelection; // 武器選択待ちかどうか

        private void Awake()
        {
            InitializeBattle();
        }

        private void Update()
        {
            UpdateTurnTimer();
            UpdateGameLogic();
        }

        // 戦闘の初期化
        private void InitializeBattle()
        {
            currentState = GameState.Initializing;
            currentTurn = 0;
            turnTimer = 0f;
            
            // 戦闘フィールドの初期化
            battleField = new BattleField(gateCount);
            
            // プレイヤーデータの初期化
            playerData = new PlayerData();
            InitializePlayerWeapons();
            
            // 戦闘結果の初期化
            battleResult = new BattleResult();
            
            // ターゲット選択システムの初期化
            currentTarget = new TargetSelection();
            lastSelectedTarget = new TargetSelection();
            waitingForWeaponSelection = false;
            
            // 初期状態をプレイヤーターンに設定
            ChangeGameState(GameState.PlayerTurn);
        }

        // プレイヤー武器の初期化
        private void InitializePlayerWeapons()
        {
            if (weaponDatabase != null && weaponDatabase.Weapons.Length > 0)
            {
                // デフォルト武器を装備（実装時に適切な武器を設定）
                for (int i = 0; i < 4 && i < weaponDatabase.Weapons.Length; i++)
                {
                    playerData.equippedWeapons[i] = weaponDatabase.Weapons[i];
                    playerData.weaponCooldowns[i] = 0;
                }
            }
        }

        // ゲーム状態の変更
        private void ChangeGameState(GameState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnGameStateChanged?.Invoke(newState);
                
                switch (newState)
                {
                    case GameState.PlayerTurn:
                        StartPlayerTurn();
                        break;
                    case GameState.EnemyTurn:
                        StartEnemyTurn();
                        break;
                    case GameState.Victory:
                    case GameState.Defeat:
                        EndBattle();
                        break;
                }
            }
        }

        // ターンタイマーの更新
        private void UpdateTurnTimer()
        {
            if (currentState == GameState.PlayerTurn)
            {
                turnTimer += Time.deltaTime;
                if (turnTimer >= turnTimeLimit)
                {
                    EndPlayerTurn(TurnEndReason.TimeOut);
                }
            }
        }

        // ゲームロジックの更新
        private void UpdateGameLogic()
        {
            CheckVictoryConditions();
            CheckDefeatConditions();
        }

        // プレイヤーターン開始
        private void StartPlayerTurn()
        {
            currentTurn++;
            turnTimer = 0f;
            OnTurnChanged?.Invoke(currentTurn);
            
            // 武器クールダウンを減らす
            for (int i = 0; i < playerData.weaponCooldowns.Length; i++)
            {
                if (playerData.weaponCooldowns[i] > 0)
                    playerData.weaponCooldowns[i]--;
            }
            
            OnPlayerDataChanged?.Invoke(playerData);
        }

        // プレイヤーターン終了
        public void EndPlayerTurn(TurnEndReason reason)
        {
            if (currentState != GameState.PlayerTurn)
                return;

            ChangeGameState(GameState.EnemyTurn);
        }

        // 敵ターン開始
        private void StartEnemyTurn()
        {
            // 敵の行動処理（後のフェーズで詳細実装）
            ProcessEnemyActions();
            
            // プレイヤーターンに戻る
            ChangeGameState(GameState.PlayerTurn);
        }

        // 敵行動の処理（基本実装）
        private void ProcessEnemyActions()
        {
            List<EnemyInstance> enemies = battleField.GetAllEnemies();
            
            foreach (EnemyInstance enemy in enemies)
            {
                if (enemy.CanAct())
                {
                    // 基本的な攻撃行動（詳細は後のフェーズで実装）
                    ProcessEnemyAttack(enemy);
                    
                    // クールダウン処理
                    if (enemy.actionCooldownRemaining > 0)
                        enemy.actionCooldownRemaining--;
                }
                
                enemy.turnsSinceSpawned++;
            }
            
            // 新しい敵の召喚処理
            ProcessEnemySummoning();
        }

        // 敵攻撃の基本処理
        private void ProcessEnemyAttack(EnemyInstance enemy)
        {
            if (enemy.enemyData.primaryAction == EnemyActionType.Attack)
            {
                int damage = enemy.currentAttackPower;
                playerData.TakeDamage(damage);
                OnPlayerDataChanged?.Invoke(playerData);
            }
        }

        // 敵召喚の処理
        private void ProcessEnemySummoning()
        {
            foreach (GateData gate in battleField.Gates)
            {
                if (gate.IsDestroyed())
                    continue;

                if (gate.summonPattern != null && ShouldSummonEnemy(gate))
                {
                    SummonEnemyFromGate(gate);
                    gate.lastSummonTurn = currentTurn;
                }
            }
        }

        // 敵召喚判定
        private bool ShouldSummonEnemy(GateData gate)
        {
            if (gate.summonPattern == null)
                return false;

            int turnsSinceLastSummon = currentTurn - gate.lastSummonTurn;
            return turnsSinceLastSummon >= gate.summonPattern.summonInterval;
        }

        // ゲートからの敵召喚
        private void SummonEnemyFromGate(GateData gate)
        {
            if (gate.summonPattern.allowedEnemyIds == null || gate.summonPattern.allowedEnemyIds.Length == 0)
                return;

            for (int i = 0; i < gate.summonPattern.summonCount; i++)
            {
                GridPosition emptyPos = battleField.GetRandomEmptyPosition();
                if (emptyPos.x == -1) // 空きがない場合
                    break;

                int randomEnemyId = gate.summonPattern.allowedEnemyIds[
                    UnityEngine.Random.Range(0, gate.summonPattern.allowedEnemyIds.Length)];
                
                EnemyData enemyData = enemyDatabase.GetEnemy(randomEnemyId);
                if (enemyData != null)
                {
                    EnemyInstance newEnemy = new EnemyInstance(enemyData, emptyPos.x, emptyPos.y);
                    battleField.PlaceEnemy(newEnemy, emptyPos);
                }
            }
        }

        // 勝利条件のチェック
        private void CheckVictoryConditions()
        {
            if (currentState == GameState.Victory || currentState == GameState.Defeat)
                return;

            // 設計書通りの勝利条件: 制限ターン内にゲート破壊 OR 敵全滅
            
            // 全ゲート破壊チェック
            if (battleField.AreAllGatesDestroyed())
            {
                battleResult.isVictory = true;
                battleResult.victoryCondition = VictoryCondition.AllGatesDestroyed;
                ChangeGameState(GameState.Victory);
                return;
            }

            // 全敵撃破チェック（ゲートが残っていても勝利）
            if (battleField.GetAliveEnemyCount() == 0)
            {
                battleResult.isVictory = true;
                battleResult.victoryCondition = VictoryCondition.AllEnemiesDefeated;
                ChangeGameState(GameState.Victory);
                return;
            }
        }

        // 敗北条件のチェック
        private void CheckDefeatConditions()
        {
            if (currentState == GameState.Victory || currentState == GameState.Defeat)
                return;

            // プレイヤーHP0チェック
            if (!playerData.IsAlive())
            {
                battleResult.isVictory = false;
                ChangeGameState(GameState.Defeat);
                return;
            }

            // 制限ターン超過チェック
            if (currentTurn >= maxTurns)
            {
                battleResult.isVictory = false;
                ChangeGameState(GameState.Defeat);
                return;
            }
        }

        // 戦闘終了処理
        private void EndBattle()
        {
            battleResult.turnsUsed = currentTurn;
            battleResult.gatesDestroyed = gateCount - battleField.GetAliveGateCount();
            
            // 勝利時のアタッチメント選択処理は別途BattleUIで処理されるため、ここでは結果のみ通知
            OnBattleEnded?.Invoke(battleResult);
        }

        // プレイヤーの武器使用
        public bool UseWeapon(int weaponIndex, GridPosition targetPosition)
        {
            if (currentState != GameState.PlayerTurn || !playerData.CanUseWeapon(weaponIndex))
                return false;

            WeaponData weapon = playerData.equippedWeapons[weaponIndex];
            if (weapon == null)
                return false;

            // 基本的な攻撃処理（詳細は後のフェーズで実装）
            ProcessWeaponAttack(weapon, targetPosition);
            
            // クールダウン設定
            playerData.weaponCooldowns[weaponIndex] = weapon.cooldownTurns;
            
            return true;
        }

        // 武器攻撃の基本処理
        private void ProcessWeaponAttack(WeaponData weapon, GridPosition targetPosition)
        {
            int damage = playerData.baseAttackPower + weapon.basePower;
            
            // ターゲットに応じた攻撃処理（基本実装）
            EnemyInstance target = battleField.GetEnemyAt(targetPosition);
            if (target != null)
            {
                target.TakeDamage(damage);
                if (!target.IsAlive())
                {
                    battleField.RemoveEnemy(targetPosition);
                    battleResult.enemiesDefeated++;
                }
            }
            else if (battleField.CanAttackGate(targetPosition.x))
            {
                // ゲート攻撃処理
                GateData gate = battleField.Gates.Find(g => g.position.x == targetPosition.x);
                if (gate != null)
                {
                    gate.TakeDamage(damage);
                }
            }
            
            battleResult.totalDamageDealt += damage;
        }

        // ================================
        // ターゲット選択システム - 公開メソッド
        // ================================

        /// <summary>
        /// 列を選択してゲート攻撃のターゲットに設定
        /// </summary>
        /// <param name="columnIndex">選択する列のインデックス</param>
        /// <returns>選択が成功したかどうか</returns>
        public bool SelectColumnTarget(int columnIndex)
        {
            if (currentState != GameState.PlayerTurn)
            {
                Debug.LogWarning($"Cannot select target during {currentState}");
                return false;
            }

            if (!CanSelectColumn(columnIndex))
            {
                Debug.LogWarning($"Cannot select column {columnIndex}: invalid or blocked");
                return false;
            }

            // 前回の選択を記憶
            CopyTargetSelection(currentTarget, lastSelectedTarget);

            // 新しいターゲットを設定
            currentTarget.SetColumnTarget(columnIndex);
            waitingForWeaponSelection = true;

            Debug.Log($"Selected column {columnIndex} for gate attack");
            OnTargetSelected?.Invoke(currentTarget);
            return true;
        }

        /// <summary>
        /// 敵の位置を選択してターゲットに設定
        /// </summary>
        /// <param name="enemyPosition">選択する敵の位置</param>
        /// <returns>選択が成功したかどうか</returns>
        public bool SelectEnemyTarget(GridPosition enemyPosition)
        {
            if (currentState != GameState.PlayerTurn)
            {
                Debug.LogWarning($"Cannot select target during {currentState}");
                return false;
            }

            if (!battleField.IsValidPosition(enemyPosition))
            {
                Debug.LogWarning($"Invalid enemy position: {enemyPosition}");
                return false;
            }

            EnemyInstance enemy = battleField.GetEnemyAt(enemyPosition);
            if (enemy == null || !enemy.IsAlive())
            {
                Debug.LogWarning($"No alive enemy at position: {enemyPosition}");
                return false;
            }

            // 前回の選択を記憶
            CopyTargetSelection(currentTarget, lastSelectedTarget);

            // 新しいターゲットを設定
            currentTarget.SetEnemyTarget(enemyPosition);
            waitingForWeaponSelection = true;

            Debug.Log($"Selected enemy at position {enemyPosition}");
            OnTargetSelected?.Invoke(currentTarget);
            return true;
        }

        /// <summary>
        /// 現在のターゲット選択をクリア
        /// </summary>
        public void ClearTargetSelection()
        {
            currentTarget.Clear();
            waitingForWeaponSelection = false;
            Debug.Log("Target selection cleared");
            OnTargetCleared?.Invoke();
        }

        /// <summary>
        /// 前回選択したターゲットを再選択（選択記憶機能）
        /// </summary>
        /// <returns>再選択が成功したかどうか</returns>
        public bool ReselectLastTarget()
        {
            if (!lastSelectedTarget.isValid)
            {
                Debug.LogWarning("No previous target to reselect");
                return false;
            }

            // 前回のターゲットを現在のターゲットにコピー
            CopyTargetSelection(lastSelectedTarget, currentTarget);
            waitingForWeaponSelection = true;

            Debug.Log($"Reselected last target: {currentTarget.targetType} at {currentTarget.position}");
            OnTargetSelected?.Invoke(currentTarget);
            return true;
        }

        /// <summary>
        /// 現在選択中のターゲットに対して武器を使用
        /// </summary>
        /// <param name="weaponIndex">使用する武器のインデックス</param>
        /// <returns>攻撃が成功したかどうか</returns>
        public bool UseWeaponWithCurrentTarget(int weaponIndex)
        {
            if (!currentTarget.isValid)
            {
                Debug.LogWarning("No target selected for weapon use");
                return false;
            }

            if (!waitingForWeaponSelection)
            {
                Debug.LogWarning("Not waiting for weapon selection");
                return false;
            }

            bool success = UseWeaponWithTarget(weaponIndex, currentTarget);
            
            if (success)
            {
                // 攻撃後はターゲット選択をクリアしない（連続攻撃のため）
                waitingForWeaponSelection = false;
                Debug.Log($"Weapon {weaponIndex} used successfully on target {currentTarget.targetType}");
            }
            
            return success;
        }

        // ================================
        // ターゲット選択システム - プライベートメソッド
        // ================================

        /// <summary>
        /// 指定した列を選択可能かチェック
        /// </summary>
        private bool CanSelectColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= battleField.Columns)
                return false;

            // 列に敵がいない場合はゲート攻撃可能
            // 敵がいても選択は可能（全体攻撃等で敵を攻撃できる）
            return true;
        }

        /// <summary>
        /// ターゲット選択をコピー
        /// </summary>
        private void CopyTargetSelection(TargetSelection source, TargetSelection destination)
        {
            if (source == null || destination == null) return;
            
            destination.targetType = source.targetType;
            destination.columnIndex = source.columnIndex;
            destination.position = source.position;
            destination.isValid = source.isValid;
        }

        /// <summary>
        /// 指定したターゲットに対して武器を使用する内部処理
        /// </summary>
        private bool UseWeaponWithTarget(int weaponIndex, TargetSelection target)
        {
            if (currentState != GameState.PlayerTurn || !playerData.CanUseWeapon(weaponIndex))
                return false;

            WeaponData weapon = playerData.equippedWeapons[weaponIndex];
            if (weapon == null)
                return false;

            // 武器の攻撃範囲に応じて攻撃処理を分岐
            bool attackExecuted = false;
            
            switch (weapon.attackRange)
            {
                case AttackRange.All:
                    // 全体攻撃は敵のみ、ゲートには当たらない
                    attackExecuted = ProcessAllEnemyAttack(weapon);
                    break;
                    
                case AttackRange.Column:
                    // 縦列攻撃：選択された列の敵とゲートを攻撃
                    attackExecuted = ProcessColumnAttack(weapon, target.columnIndex);
                    break;
                    
                case AttackRange.Row1:
                case AttackRange.Row2:
                    // 列攻撃：敵のみ
                    attackExecuted = ProcessRowAttack(weapon, target.position);
                    break;
                    
                default:
                    // 単体攻撃
                    attackExecuted = ProcessSingleTargetAttack(weapon, target);
                    break;
            }
            
            if (attackExecuted)
            {
                // クールダウン設定
                playerData.weaponCooldowns[weaponIndex] = weapon.cooldownTurns;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 単体ターゲット攻撃処理
        /// </summary>
        private bool ProcessSingleTargetAttack(WeaponData weapon, TargetSelection target)
        {
            int damage = playerData.baseAttackPower + weapon.basePower;
            
            if (target.targetType == TargetType.EnemyPosition)
            {
                // 敵攻撃
                EnemyInstance enemy = battleField.GetEnemyAt(target.position);
                if (enemy != null && enemy.IsAlive())
                {
                    enemy.TakeDamage(damage);
                    if (!enemy.IsAlive())
                    {
                        battleField.RemoveEnemy(target.position);
                        battleResult.enemiesDefeated++;
                    }
                    battleResult.totalDamageDealt += damage;
                    return true;
                }
            }
            else if (target.targetType == TargetType.Column)
            {
                // ゲート攻撃（列に敵がいない場合のみ）
                if (battleField.CanAttackGate(target.columnIndex))
                {
                    GateData gate = battleField.Gates.Find(g => g.position.x == target.columnIndex);
                    if (gate != null && !gate.IsDestroyed())
                    {
                        gate.TakeDamage(damage);
                        battleResult.totalDamageDealt += damage;
                        return true;
                    }
                }
                else
                {
                    // 列に敵がいる場合は一番前の敵を攻撃
                    EnemyInstance frontEnemy = battleField.GetFrontEnemyInColumn(target.columnIndex);
                    if (frontEnemy != null)
                    {
                        frontEnemy.TakeDamage(damage);
                        if (!frontEnemy.IsAlive())
                        {
                            battleField.RemoveEnemy(new GridPosition(frontEnemy.gridX, frontEnemy.gridY));
                            battleResult.enemiesDefeated++;
                        }
                        battleResult.totalDamageDealt += damage;
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// 縦列攻撃処理（敵とゲートを貫通）
        /// </summary>
        private bool ProcessColumnAttack(WeaponData weapon, int columnIndex)
        {
            int damage = playerData.baseAttackPower + weapon.basePower;
            bool anyHit = false;
            
            // 列のすべての敵を攻撃
            List<EnemyInstance> enemiesInColumn = battleField.GetEnemiesInColumn(columnIndex);
            foreach (EnemyInstance enemy in enemiesInColumn)
            {
                if (enemy.IsAlive())
                {
                    enemy.TakeDamage(damage);
                    if (!enemy.IsAlive())
                    {
                        battleField.RemoveEnemy(new GridPosition(enemy.gridX, enemy.gridY));
                        battleResult.enemiesDefeated++;
                    }
                    battleResult.totalDamageDealt += damage;
                    anyHit = true;
                }
            }
            
            // ゲートも攻撃（敵がいても貫通）
            GateData gate = battleField.Gates.Find(g => g.position.x == columnIndex);
            if (gate != null && !gate.IsDestroyed())
            {
                gate.TakeDamage(damage);
                battleResult.totalDamageDealt += damage;
                anyHit = true;
            }
            
            return anyHit;
        }

        /// <summary>
        /// 列攻撃処理（敵のみ）
        /// </summary>
        private bool ProcessRowAttack(WeaponData weapon, GridPosition targetPosition)
        {
            int damage = playerData.baseAttackPower + weapon.basePower;
            bool anyHit = false;
            
            int targetRow = (weapon.attackRange == AttackRange.Row1) ? 0 : 1;
            List<EnemyInstance> enemiesInRow = battleField.GetEnemiesInRow(targetRow);
            
            foreach (EnemyInstance enemy in enemiesInRow)
            {
                if (enemy.IsAlive())
                {
                    enemy.TakeDamage(damage);
                    if (!enemy.IsAlive())
                    {
                        battleField.RemoveEnemy(new GridPosition(enemy.gridX, enemy.gridY));
                        battleResult.enemiesDefeated++;
                    }
                    battleResult.totalDamageDealt += damage;
                    anyHit = true;
                }
            }
            
            return anyHit;
        }

        /// <summary>
        /// 全体攻撃処理（敵のみ、ゲートには当たらない）
        /// </summary>
        private bool ProcessAllEnemyAttack(WeaponData weapon)
        {
            int damage = playerData.baseAttackPower + weapon.basePower;
            bool anyHit = false;
            
            List<EnemyInstance> allEnemies = battleField.GetAllEnemies();
            foreach (EnemyInstance enemy in allEnemies)
            {
                if (enemy.IsAlive())
                {
                    enemy.TakeDamage(damage);
                    if (!enemy.IsAlive())
                    {
                        battleField.RemoveEnemy(new GridPosition(enemy.gridX, enemy.gridY));
                        battleResult.enemiesDefeated++;
                    }
                    battleResult.totalDamageDealt += damage;
                    anyHit = true;
                }
            }
            
            return anyHit;
        }

        // デバッグ用：戦闘状態のリセット
        [ContextMenu("Reset Battle")]
        public void ResetBattle()
        {
            InitializeBattle();
        }
    }
}