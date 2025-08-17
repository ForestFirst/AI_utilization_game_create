using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// ゲート召喚パターン
    /// </summary>
    public enum GateSummonPattern
    {
        None,           // 召喚なし
        Periodic,       // 定期召喚
        OnDamage,       // ダメージ時召喚
        Continuous,     // 継続召喚
        Defensive       // 防御時召喚
    }

    /// <summary>
    /// ゲートタイプ
    /// </summary>
    public enum GateType
    {
        Standard,       // 標準ゲート
        Elite,          // エリートゲート（強化敵）
        Support,        // サポートゲート（バフ敵）
        Summoner,       // 召喚ゲート（召喚特化）
        Fortress        // 要塞ゲート（高HP）
    }

    /// <summary>
    /// ゲート戦略効果
    /// </summary>
    public enum GateStrategicEffect
    {
        None,                   // 効果なし
        BuffAllEnemies,         // 全敵バフ
        IncreaseSpawnRate,      // 召喚率上昇
        IncreaseDefense,        // 防御力上昇
        HealAllEnemies,         // 全敵回復
        AttackBoost             // 攻撃力上昇
    }

    /// <summary>
    /// ゲートデータの構造
    /// </summary>
    [Serializable]
    public class GateData
    {
        [Header("基本情報")]
        public int gateId;
        public int maxHp;               // 最大HP（基本25,000）
        public int currentHp;           // 現在HP
        public string gateName;
        public GateType gateType;       // ゲートタイプ
        public GridPosition position;   // ゲートの位置（列番号）
        
        [Header("敵配置設定")]
        public GateSummonPattern summonPattern;    // 召喚パターン
        public List<string> assignedEnemyTypes;    // 配置される敵種類
        public int maxEnemiesPerGate;              // ゲート当たり最大敵数
        public int spawnCooldown;                  // 召喚クールダウン
        public int lastSummonTurn;                 // 最後に召喚したターン
        
        [Header("召喚設定詳細")]
        public int summonInterval;                 // 召喚間隔（ターン）
        public int[] allowedEnemyIds;             // 召喚可能な敵ID配列
        public int summonCount;                   // 一度に召喚する敵数
        
        [Header("戦略効果")]
        public GateStrategicEffect strategicEffect;    // 戦略効果
        public float effectStrength;                   // 効果強度（1.0=100%）
        public bool isEffectActive;                    // 効果アクティブか
        
        [Header("破壊時効果")]
        public bool hasDestructionBonus;              // 破壊ボーナスあり
        public int destructionReward;                  // 破壊報酬
        public string destructionEffectDescription;    // 破壊効果説明
        
        public GateData(int id, int hp, string name, GridPosition pos, GateType type = GateType.Standard)
        {
            gateId = id;
            maxHp = hp;
            currentHp = hp;
            gateName = name;
            gateType = type;
            position = pos;
            
            // 敵配置設定の初期化
            summonPattern = GateSummonPattern.Periodic;
            assignedEnemyTypes = new List<string>();
            maxEnemiesPerGate = 2;
            spawnCooldown = 3;
            lastSummonTurn = -1;
            
            // 召喚設定詳細の初期化
            summonInterval = 3;               // デフォルト3ターン間隔
            allowedEnemyIds = new int[] { 0, 1, 2 }; // デフォルト敵ID
            summonCount = 1;                  // デフォルト1体召喚
            
            // 戦略効果の初期化
            strategicEffect = GateStrategicEffect.None;
            effectStrength = 1.0f;
            isEffectActive = true;
            
            // 破壊時効果の初期化
            hasDestructionBonus = false;
            destructionReward = 0;
            destructionEffectDescription = "";
            
            // ゲートタイプに応じた初期設定
            InitializeByType();
        }
        
        /// <summary>
        /// ゲートタイプに応じた初期設定
        /// </summary>
        private void InitializeByType()
        {
            switch (gateType)
            {
                case GateType.Standard:
                    // 標準設定（既定値のまま）
                    break;
                    
                case GateType.Elite:
                    maxHp = (int)(maxHp * 1.5f);
                    currentHp = maxHp;
                    strategicEffect = GateStrategicEffect.AttackBoost;
                    effectStrength = 1.2f;
                    destructionReward = 150;
                    break;
                    
                case GateType.Support:
                    strategicEffect = GateStrategicEffect.BuffAllEnemies;
                    effectStrength = 1.3f;
                    summonPattern = GateSummonPattern.Defensive;
                    destructionReward = 200;
                    break;
                    
                case GateType.Summoner:
                    summonPattern = GateSummonPattern.Continuous;
                    spawnCooldown = 2;
                    maxEnemiesPerGate = 3;
                    strategicEffect = GateStrategicEffect.IncreaseSpawnRate;
                    destructionReward = 100;
                    break;
                    
                case GateType.Fortress:
                    maxHp = maxHp * 2;
                    currentHp = maxHp;
                    strategicEffect = GateStrategicEffect.IncreaseDefense;
                    effectStrength = 1.5f;
                    destructionReward = 300;
                    break;
            }
        }
        
        public bool IsDestroyed()
        {
            return currentHp <= 0;
        }
        
        public void TakeDamage(int damage)
        {
            int oldHp = currentHp;
            currentHp = Mathf.Max(0, currentHp - damage);
            
            // ダメージ時召喚パターンの処理
            if (summonPattern == GateSummonPattern.OnDamage && oldHp > currentHp)
            {
                TriggerDamageSummon();
            }
        }
        
        public float GetHpPercentage()
        {
            return (float)currentHp / maxHp;
        }
        
        /// <summary>
        /// ダメージ時召喚処理
        /// </summary>
        private void TriggerDamageSummon()
        {
            // この処理は後でBattleFieldクラスから呼び出される
            Debug.Log($"Gate {gateId} triggers damage summon!");
        }
        
        /// <summary>
        /// 召喚可能かチェック
        /// </summary>
        /// <param name="currentTurn">現在のターン</param>
        /// <returns>召喚可能な場合true</returns>
        public bool CanSummon(int currentTurn)
        {
            if (IsDestroyed() || summonPattern == GateSummonPattern.None)
                return false;
            
            int turnsSinceLastSummon = currentTurn - lastSummonTurn;
            
            switch (summonPattern)
            {
                case GateSummonPattern.Periodic:
                    return turnsSinceLastSummon >= spawnCooldown;
                    
                case GateSummonPattern.Continuous:
                    return turnsSinceLastSummon >= (spawnCooldown / 2); // より頻繁
                    
                case GateSummonPattern.Defensive:
                    // 体力が50%以下で召喚
                    return GetHpPercentage() <= 0.5f && turnsSinceLastSummon >= spawnCooldown;
                    
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 戦略効果を適用
        /// </summary>
        /// <param name="enemies">効果を適用する敵リスト</param>
        public void ApplyStrategicEffect(List<EnemyInstance> enemies)
        {
            if (!isEffectActive || IsDestroyed() || strategicEffect == GateStrategicEffect.None)
                return;
            
            switch (strategicEffect)
            {
                case GateStrategicEffect.BuffAllEnemies:
                    foreach (var enemy in enemies)
                    {
                        // 攻撃力ブーストを適用（実装はEnemyInstanceの拡張が必要）
                        // enemy.ApplyBuff("AttackBoost", effectStrength);
                    }
                    break;
                    
                case GateStrategicEffect.HealAllEnemies:
                    foreach (var enemy in enemies)
                    {
                        int healAmount = Mathf.RoundToInt(enemy.MaxHp * (effectStrength - 1.0f));
                        enemy.Heal(healAmount);
                    }
                    break;
                    
                case GateStrategicEffect.IncreaseDefense:
                    // 防御力増加処理（実装はEnemyInstanceの拡張が必要）
                    break;
                    
                case GateStrategicEffect.AttackBoost:
                    // 攻撃力増加処理（実装はEnemyInstanceの拡張が必要）
                    break;
            }
        }
        
        /// <summary>
        /// ゲートの詳細情報を取得
        /// </summary>
        /// <returns>詳細情報文字列</returns>
        public string GetDetailInfo()
        {
            return $"Gate {gateId}: {gateName} ({gateType})\n" +
                   $"HP: {currentHp}/{maxHp} ({GetHpPercentage():P0})\n" +
                   $"Effect: {strategicEffect} ({effectStrength:F1}x)\n" +
                   $"Enemies: {assignedEnemyTypes.Count} types";
        }
    }

    /// <summary>
    /// 戦闘フィールドの管理クラス
    /// ゲート数に連動したグリッド構造、ゲート別敵配置、戦略的攻撃機能を提供
    /// </summary>
    public class BattleField
    {
        [Header("グリッド設定")]
        private int columns;                    // 横幅（ゲート数）
        private const int rows = 2;             // 縦幅（固定2列）
        private EnemyInstance[,] gridEnemies;   // グリッド上の敵配置
        private Dictionary<GridPosition, bool> occupiedPositions; // 占有位置管理
        
        [Header("ゲート管理")]
        private List<GateData> gates;           // ゲート配列
        private Dictionary<int, List<EnemyInstance>> gateEnemies; // ゲート別敵管理
        private int currentTurn;                // 現在のターン数
        
        [Header("戦略システム")]
        private GateData selectedTargetGate;    // 選択された攻撃対象ゲート
        private List<GateData> destroyedGates;  // 破壊されたゲート履歴
        
        // イベント定義
        public event Action<GateData> OnGateDestroyed;          // ゲート破壊時
        public event Action<GateData> OnGateSelected;           // ゲート選択時
        public event Action<int, List<EnemyInstance>> OnEnemiesSummoned; // 敵召喚時
        public event Action<GateData, GateStrategicEffect> OnStrategicEffectApplied; // 戦略効果適用時

        // プロパティ
        public int Columns => columns;
        public int Rows => rows;
        public List<GateData> Gates => gates;
        public List<GateData> AliveGates => gates.Where(g => !g.IsDestroyed()).ToList();
        public List<GateData> DestroyedGates => destroyedGates;
        public GateData SelectedTargetGate => selectedTargetGate;
        public int CurrentTurn => currentTurn;

        public BattleField(int gateCount)
        {
            columns = Math.Max(1, Math.Min(gateCount, 6)); // 1-6ゲートに制限
            gridEnemies = new EnemyInstance[columns, rows];
            gates = new List<GateData>();
            gateEnemies = new Dictionary<int, List<EnemyInstance>>();
            occupiedPositions = new Dictionary<GridPosition, bool>();
            destroyedGates = new List<GateData>();
            currentTurn = 0;
            
            // ゲートの初期化
            InitializeGates(columns);
            
            Debug.Log($"BattleField initialized: {columns} gates, {columns}x{rows} grid");
        }

        /// <summary>
        /// ゲートの初期化（仕様書の戦略パターンを考慮）
        /// </summary>
        /// <param name="gateCount">ゲート数</param>
        private void InitializeGates(int gateCount)
        {
            for (int i = 0; i < gateCount; i++)
            {
                GridPosition gatePos = new GridPosition(i, -1); // ゲートは行-1に配置
                
                // ゲートタイプをパターン化（仕様書の戦略パターンを参考）
                GateType gateType = DetermineGateType(i, gateCount);
                GateData gate = new GateData(i, 25000, $"Gate_{i}", gatePos, gateType);
                
                // ゲート別敵タイプを設定
                AssignEnemyTypesToGate(gate, i, gateCount);
                
                gates.Add(gate);
                gateEnemies[i] = new List<EnemyInstance>();
            }
            
            Debug.Log($"Initialized {gateCount} gates with strategic patterns");
        }
        
        /// <summary>
        /// ゲートタイプを決定（戦略的配置）
        /// </summary>
        /// <param name="gateIndex">ゲートインデックス</param>
        /// <param name="totalGates">総ゲート数</param>
        /// <returns>ゲートタイプ</returns>
        private GateType DetermineGateType(int gateIndex, int totalGates)
        {
            // 仕様書の戦略パターンに基づく配置
            switch (totalGates)
            {
                case 2:
                    return gateIndex == 0 ? GateType.Support : GateType.Standard;
                    
                case 3:
                    if (gateIndex == 0) return GateType.Support;      // バフ役
                    if (gateIndex == 1) return GateType.Standard;     // 通常敵
                    return GateType.Elite;                            // 強敵
                    
                case 4:
                    if (gateIndex == 0) return GateType.Support;      // バフ役
                    if (gateIndex == 1) return GateType.Summoner;     // 召喚役
                    if (gateIndex == 2) return GateType.Standard;     // 通常敵
                    return GateType.Fortress;                         // 要塞
                    
                case 5:
                    if (gateIndex == 0) return GateType.Support;      // サポート
                    if (gateIndex == 1) return GateType.Summoner;     // 召喚
                    if (gateIndex == 2) return GateType.Standard;     // 通常
                    if (gateIndex == 3) return GateType.Elite;        // エリート  
                    return GateType.Fortress;                         // 要塞
                    
                case 6:
                    if (gateIndex == 0) return GateType.Support;      // サポート
                    if (gateIndex == 1) return GateType.Summoner;     // 召喚1
                    if (gateIndex == 2) return GateType.Standard;     // 通常1
                    if (gateIndex == 3) return GateType.Standard;     // 通常2
                    if (gateIndex == 4) return GateType.Elite;        // エリート
                    return GateType.Fortress;                         // 要塞
                    
                default:
                    return GateType.Standard;
            }
        }
        
        /// <summary>
        /// ゲートに敵タイプを割り当て
        /// </summary>
        /// <param name="gate">ゲート</param>
        /// <param name="gateIndex">ゲートインデックス</param>
        /// <param name="totalGates">総ゲート数</param>
        private void AssignEnemyTypesToGate(GateData gate, int gateIndex, int totalGates)
        {
            gate.assignedEnemyTypes.Clear();
            
            switch (gate.gateType)
            {
                case GateType.Support:
                    gate.assignedEnemyTypes.AddRange(new[] { "ShieldDrone", "RepairUnit" });
                    break;
                    
                case GateType.Summoner:
                    gate.assignedEnemyTypes.AddRange(new[] { "SummonerBot", "PatrolBot" });
                    break;
                    
                case GateType.Elite:
                    gate.assignedEnemyTypes.AddRange(new[] { "AssaultWalker", "EliteGuard" });
                    break;
                    
                case GateType.Fortress:
                    gate.assignedEnemyTypes.AddRange(new[] { "HeavyTank", "DefenseBot" });
                    break;
                    
                default: // Standard
                    gate.assignedEnemyTypes.AddRange(new[] { "PatrolBot", "BasicDrone" });
                    break;
            }
            
            Debug.Log($"Gate {gateIndex} ({gate.gateType}): {string.Join(", ", gate.assignedEnemyTypes)}");
        }

        #region 敵配置・管理システム
        
        /// <summary>
        /// 指定位置に敵を配置（ゲート別管理対応）
        /// </summary>
        /// <param name="enemy">配置する敵</param>
        /// <param name="position">配置位置</param>
        /// <param name="gateId">所属ゲートID（-1で自動判定）</param>
        /// <returns>配置成功の場合true</returns>
        public bool PlaceEnemy(EnemyInstance enemy, GridPosition position, int gateId = -1)
        {
            if (!IsValidPosition(position) || IsOccupied(position))
                return false;

            gridEnemies[position.x, position.y] = enemy;
            occupiedPositions[position] = true;
            enemy.gridX = position.x;
            enemy.gridY = position.y;
            
            // ゲート別管理に追加
            if (gateId == -1)
                gateId = position.x; // 列番号をゲートIDとして使用
            
            if (gateId >= 0 && gateId < gates.Count && gateEnemies.ContainsKey(gateId))
            {
                gateEnemies[gateId].Add(enemy);
                enemy.assignedGateId = gateId; // EnemyInstanceにassignedGateIdプロパティが必要
                Debug.Log($"Enemy {enemy.EnemyName} placed at Gate {gateId} position ({position.x}, {position.y})");
            }
            
            return true;
        }
        
        /// <summary>
        /// ゲート別に敵を召喚
        /// </summary>
        /// <param name="gateId">ゲートID</param>
        /// <param name="enemyType">敵タイプ</param>
        /// <returns>召喚成功の場合true</returns>
        public bool SummonEnemyFromGate(int gateId, string enemyType = null)
        {
            if (gateId < 0 || gateId >= gates.Count)
                return false;
            
            var gate = gates[gateId];
            if (gate.IsDestroyed() || !gate.CanSummon(currentTurn))
                return false;
            
            // 空いている位置を検索（該当列の中で）
            GridPosition spawnPos = GetEmptyPositionInColumn(gateId);
            if (!spawnPos.IsValid())
                return false;
            
            // 敵タイプの決定
            if (string.IsNullOrEmpty(enemyType) && gate.assignedEnemyTypes.Count > 0)
            {
                enemyType = gate.assignedEnemyTypes[UnityEngine.Random.Range(0, gate.assignedEnemyTypes.Count)];
            }
            
            // TODO: EnemyInstanceの実際の生成は外部システムで行う
            // この関数は配置のみを担当
            Debug.Log($"Gate {gateId} summons {enemyType} at ({spawnPos.x}, {spawnPos.y})");
            
            // 召喚記録を更新
            gate.lastSummonTurn = currentTurn;
            
            return true;
        }
        
        /// <summary>
        /// 指定列の空いている位置を取得
        /// </summary>
        /// <param name="column">列番号</param>
        /// <returns>空いている位置（見つからない場合は無効な位置）</returns>
        private GridPosition GetEmptyPositionInColumn(int column)
        {
            if (column < 0 || column >= columns)
                return GridPosition.Invalid;
            
            // 前列から確認
            for (int row = 0; row < rows; row++)
            {
                GridPosition pos = new GridPosition(column, row);
                if (!IsOccupied(pos))
                    return pos;
            }
            
            return GridPosition.Invalid;
        }

        /// <summary>
        /// 敵を削除（ゲート別管理対応）
        /// </summary>
        /// <param name="position">削除位置</param>
        /// <returns>削除成功の場合true</returns>
        public bool RemoveEnemy(GridPosition position)
        {
            if (!IsValidPosition(position) || !IsOccupied(position))
                return false;

            var enemy = gridEnemies[position.x, position.y];
            if (enemy != null)
            {
                // ゲート別管理から削除
                foreach (var gateEnemyList in gateEnemies.Values)
                {
                    gateEnemyList.Remove(enemy);
                }
            }

            gridEnemies[position.x, position.y] = null;
            occupiedPositions.Remove(position);
            return true;
        }
        
        #endregion
        
        #region ゲート攻撃・選択システム
        
        /// <summary>
        /// ゲートを攻撃対象として選択
        /// </summary>
        /// <param name="gateId">ゲートID</param>
        /// <returns>選択成功の場合true</returns>
        public bool SelectTargetGate(int gateId)
        {
            if (gateId < 0 || gateId >= gates.Count)
                return false;
            
            var gate = gates[gateId];
            if (gate.IsDestroyed())
            {
                Debug.LogWarning($"Cannot select destroyed gate {gateId}");
                return false;
            }
            
            selectedTargetGate = gate;
            OnGateSelected?.Invoke(gate);
            
            Debug.Log($"Gate {gateId} selected as attack target");
            return true;
        }
        
        /// <summary>
        /// 選択されたゲートへの攻撃
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <returns>攻撃成功の場合true</returns>
        public bool AttackSelectedGate(int damage)
        {
            if (selectedTargetGate == null)
            {
                Debug.LogWarning("No gate selected for attack");
                return false;
            }
            
            return AttackGate(selectedTargetGate.gateId, damage);
        }
        
        /// <summary>
        /// 指定ゲートを攻撃
        /// </summary>
        /// <param name="gateId">ゲートID</param>
        /// <param name="damage">ダメージ量</param>
        /// <returns>攻撃成功の場合true</returns>
        public bool AttackGate(int gateId, int damage)
        {
            if (gateId < 0 || gateId >= gates.Count)
                return false;
            
            var gate = gates[gateId];
            if (gate.IsDestroyed())
                return false;
            
            // ゲート前面に敵がいる場合は攻撃不可
            if (!CanAttackGate(gateId))
            {
                Debug.LogWarning($"Cannot attack Gate {gateId}: enemies blocking");
                return false;
            }
            
            int oldHp = gate.currentHp;
            gate.TakeDamage(damage);
            
            Debug.Log($"Gate {gateId} attacked: {oldHp} → {gate.currentHp} (-{damage})");
            
            // ゲート破壊チェック
            if (gate.IsDestroyed())
            {
                HandleGateDestroyed(gate);
            }
            
            return true;
        }
        
        /// <summary>
        /// ゲート破壊時の処理
        /// </summary>
        /// <param name="gate">破壊されたゲート</param>
        private void HandleGateDestroyed(GateData gate)
        {
            destroyedGates.Add(gate);
            
            // 所属敵の処理（ゲート破壊による効果）
            if (gateEnemies.ContainsKey(gate.gateId))
            {
                var gateEnemyList = gateEnemies[gate.gateId];
                foreach (var enemy in gateEnemyList.ToList())
                {
                    // ゲート破壊による敵への影響（例：パニック状態、弱体化など）
                    ApplyGateDestructionEffect(enemy, gate);
                }
            }
            
            // 戦略効果停止
            gate.isEffectActive = false;
            
            OnGateDestroyed?.Invoke(gate);
            
            Debug.Log($"Gate {gate.gateId} destroyed! Reward: {gate.destructionReward}");
        }
        
        /// <summary>
        /// ゲート破壊による敵への効果を適用
        /// </summary>
        /// <param name="enemy">対象敵</param>
        /// <param name="destroyedGate">破壊されたゲート</param>
        private void ApplyGateDestructionEffect(EnemyInstance enemy, GateData destroyedGate)
        {
            switch (destroyedGate.gateType)
            {
                case GateType.Support:
                    // サポートゲート破壊：敵の防御力減少
                    // enemy.RemoveBuff("DefenseBoost");
                    Debug.Log($"Enemy {enemy.EnemyName} loses support buff from Gate {destroyedGate.gateId}");
                    break;
                    
                case GateType.Summoner:
                    // 召喚ゲート破壊：召喚停止
                    Debug.Log($"Enemy summoning stopped from Gate {destroyedGate.gateId}");
                    break;
                    
                case GateType.Elite:
                    // エリートゲート破壊：攻撃力減少
                    Debug.Log($"Enemy {enemy.EnemyName} loses elite bonus from Gate {destroyedGate.gateId}");
                    break;
            }
        }
        
        #endregion

        // 指定位置の敵を取得
        public EnemyInstance GetEnemyAt(GridPosition position)
        {
            if (!IsValidPosition(position))
                return null;
            return gridEnemies[position.x, position.y];
        }

        // 位置が有効かチェック
        public bool IsValidPosition(GridPosition position)
        {
            return position.x >= 0 && position.x < columns && position.y >= 0 && position.y < rows;
        }

        // 位置が占有されているかチェック
        public bool IsOccupied(GridPosition position)
        {
            return occupiedPositions.ContainsKey(position);
        }

        // 空いている位置をランダムで取得
        public GridPosition GetRandomEmptyPosition()
        {
            List<GridPosition> emptyPositions = new List<GridPosition>();
            
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    GridPosition pos = new GridPosition(x, y);
                    if (!IsOccupied(pos))
                        emptyPositions.Add(pos);
                }
            }
            
            if (emptyPositions.Count == 0)
                return new GridPosition(-1, -1); // 無効な位置を返す
            
            return emptyPositions[UnityEngine.Random.Range(0, emptyPositions.Count)];
        }

        // 指定列の敵を取得（前列優先）
        public EnemyInstance GetFrontEnemyInColumn(int column)
        {
            if (column < 0 || column >= columns)
                return null;

            // 1列目から確認
            EnemyInstance frontEnemy = gridEnemies[column, 0];
            if (frontEnemy != null && frontEnemy.IsAlive())
                return frontEnemy;

            // 2列目を確認
            EnemyInstance backEnemy = gridEnemies[column, 1];
            if (backEnemy != null && backEnemy.IsAlive())
                return backEnemy;

            return null;
        }

        // 指定列に敵がいるかチェック
        public bool HasEnemyInColumn(int column)
        {
            return GetFrontEnemyInColumn(column) != null;
        }

        // 指定行のすべての敵を取得
        public List<EnemyInstance> GetEnemiesInRow(int row)
        {
            List<EnemyInstance> enemies = new List<EnemyInstance>();
            
            if (row < 0 || row >= rows)
                return enemies;

            for (int x = 0; x < columns; x++)
            {
                EnemyInstance enemy = gridEnemies[x, row];
                if (enemy != null && enemy.IsAlive())
                    enemies.Add(enemy);
            }
            
            return enemies;
        }

        // 指定列のすべての敵を取得
        public List<EnemyInstance> GetEnemiesInColumn(int column)
        {
            List<EnemyInstance> enemies = new List<EnemyInstance>();
            
            if (column < 0 || column >= columns)
                return enemies;

            for (int y = 0; y < rows; y++)
            {
                EnemyInstance enemy = gridEnemies[column, y];
                if (enemy != null && enemy.IsAlive())
                    enemies.Add(enemy);
            }
            
            return enemies;
        }

        // すべての敵を取得
        public List<EnemyInstance> GetAllEnemies()
        {
            List<EnemyInstance> enemies = new List<EnemyInstance>();
            
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    EnemyInstance enemy = gridEnemies[x, y];
                    if (enemy != null && enemy.IsAlive())
                        enemies.Add(enemy);
                }
            }
            
            return enemies;
        }

        // 生存している敵の数を取得
        public int GetAliveEnemyCount()
        {
            return GetAllEnemies().Count;
        }

        // ゲートへの攻撃が可能な列かチェック
        public bool CanAttackGate(int column)
        {
            return !HasEnemyInColumn(column);
        }

        // すべてのゲートが破壊されたかチェック
        public bool AreAllGatesDestroyed()
        {
            foreach (GateData gate in gates)
            {
                if (!gate.IsDestroyed())
                    return false;
            }
            return true;
        }

        // 生存しているゲートの数を取得
        public int GetAliveGateCount()
        {
            int count = 0;
            foreach (GateData gate in gates)
            {
                if (!gate.IsDestroyed())
                    count++;
            }
            return count;
        }

        #region ターン管理・戦略効果システム
        
        /// <summary>
        /// ターンを進める
        /// </summary>
        public void AdvanceTurn()
        {
            currentTurn++;
            
            // 各ゲートの召喚チェック
            ProcessGateSummoning();
            
            // 戦略効果の適用
            ApplyStrategicEffects();
            
            Debug.Log($"Turn advanced to {currentTurn}");
        }
        
        /// <summary>
        /// ゲート召喚処理
        /// </summary>
        private void ProcessGateSummoning()
        {
            foreach (var gate in gates.Where(g => !g.IsDestroyed()))
            {
                if (gate.CanSummon(currentTurn))
                {
                    // 召喚空きスペースがあるかチェック
                    var emptyPos = GetEmptyPositionInColumn(gate.gateId);
                    if (emptyPos.IsValid())
                    {
                        SummonEnemyFromGate(gate.gateId);
                    }
                }
            }
        }
        
        /// <summary>
        /// 戦略効果の適用
        /// </summary>
        private void ApplyStrategicEffects()
        {
            var allEnemies = GetAllEnemies();
            
            foreach (var gate in gates.Where(g => !g.IsDestroyed() && g.isEffectActive))
            {
                gate.ApplyStrategicEffect(allEnemies);
                
                if (gate.strategicEffect != GateStrategicEffect.None)
                {
                    OnStrategicEffectApplied?.Invoke(gate, gate.strategicEffect);
                }
            }
        }
        
        /// <summary>
        /// 戦略状況の分析
        /// </summary>
        /// <returns>戦略情報</returns>
        public string AnalyzeStrategicSituation()
        {
            var aliveGates = AliveGates;
            var allEnemies = GetAllEnemies();
            
            var analysis = $"=== 戦略状況分析 (Turn {currentTurn}) ===\n";
            analysis += $"生存ゲート: {aliveGates.Count}/{gates.Count}\n";
            analysis += $"総敵数: {allEnemies.Count}\n\n";
            
            // ゲート別分析
            foreach (var gate in aliveGates)
            {
                var gateEnemyCount = gateEnemies.ContainsKey(gate.gateId) ? gateEnemies[gate.gateId].Count : 0;
                analysis += $"Gate {gate.gateId} ({gate.gateType}): HP {gate.GetHpPercentage():P0}, Enemies {gateEnemyCount}\n";
                
                if (gate.strategicEffect != GateStrategicEffect.None)
                {
                    analysis += $"  → Effect: {gate.strategicEffect} ({gate.effectStrength:F1}x)\n";
                }
            }
            
            // 推奨戦略
            analysis += "\n=== 推奨戦略 ===\n";
            var priorityGate = GetStrategicPriorityGate();
            if (priorityGate != null)
            {
                analysis += $"優先攻撃: Gate {priorityGate.gateId} ({GetGateAttackReason(priorityGate)})\n";
            }
            
            return analysis;
        }
        
        /// <summary>
        /// 戦略的優先ゲートを取得
        /// </summary>
        /// <returns>優先攻撃すべきゲート</returns>
        public GateData GetStrategicPriorityGate()
        {
            var aliveGates = AliveGates;
            if (aliveGates.Count == 0) return null;
            
            // 優先度計算（サポート > 召喚 > エリート > 要塞 > 標準）
            var priorityOrder = new Dictionary<GateType, int>
            {
                { GateType.Support, 5 },
                { GateType.Summoner, 4 },
                { GateType.Elite, 3 },
                { GateType.Fortress, 2 },
                { GateType.Standard, 1 }
            };
            
            return aliveGates
                .Where(g => CanAttackGate(g.gateId))
                .OrderByDescending(g => priorityOrder.GetValueOrDefault(g.gateType, 0))
                .ThenBy(g => g.GetHpPercentage()) // HP少ない順
                .FirstOrDefault();
        }
        
        /// <summary>
        /// ゲート攻撃理由を取得
        /// </summary>
        /// <param name="gate">ゲート</param>
        /// <returns>攻撃理由</returns>
        private string GetGateAttackReason(GateData gate)
        {
            switch (gate.gateType)
            {
                case GateType.Support:
                    return "敵全体への支援効果を停止";
                case GateType.Summoner:
                    return "敵召喚を停止";
                case GateType.Elite:
                    return "高威力敵の源を断つ";
                case GateType.Fortress:
                    return "高報酬・防御効果停止";
                default:
                    return "標準的な攻撃対象";
            }
        }
        
        #endregion
        
        #region フィールド管理
        
        /// <summary>
        /// フィールドの状態をリセット
        /// </summary>
        public void ResetField()
        {
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    gridEnemies[x, y] = null;
                }
            }
            occupiedPositions.Clear();
            
            // ゲート別敵管理をクリア
            foreach (var gateEnemyList in gateEnemies.Values)
            {
                gateEnemyList.Clear();
            }
            
            // ゲートのHPをリセット
            foreach (GateData gate in gates)
            {
                gate.currentHp = gate.maxHp;
                gate.lastSummonTurn = -1;
                gate.isEffectActive = true;
            }
            
            // 状態リセット
            destroyedGates.Clear();
            selectedTargetGate = null;
            currentTurn = 0;
            
            Debug.Log("BattleField reset completed");
        }
        
        /// <summary>
        /// フィールド情報を取得
        /// </summary>
        /// <returns>フィールド情報文字列</returns>
        public string GetFieldInfo()
        {
            var info = $"BattleField ({columns}x{rows}):\n";
            info += $"Turn: {currentTurn}\n";
            info += $"Gates: {AliveGates.Count}/{gates.Count} alive\n";
            info += $"Enemies: {GetAliveEnemyCount()} total\n";
            
            if (selectedTargetGate != null)
            {
                info += $"Selected: Gate {selectedTargetGate.gateId}\n";
            }
            
            return info;
        }

        /// <summary>
        /// 生存ゲート数を取得
        /// </summary>
        /// <returns>生存中のゲート数</returns>
        public int GetAliveGateCount()
        {
            return AliveGates.Count;
        }

        /// <summary>
        /// 全ゲートが破壊されているかをチェック
        /// </summary>
        /// <returns>全ゲートが破壊されている場合true</returns>
        public bool AreAllGatesDestroyed()
        {
            return GetAliveGateCount() == 0;
        }
        
        #endregion
    }
}