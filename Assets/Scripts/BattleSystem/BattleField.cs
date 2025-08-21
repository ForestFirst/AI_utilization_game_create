using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// ゲートタイプ（仕様書に基づく5種類）
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
        BuffAllEnemies,         // 全敵強化
        IncreaseSpawnRate,      // 召喚率上昇
        DefenseBoost,           // 防御力強化
        AttackBoost,            // 攻撃力強化
        Regeneration           // HP回復
    }

    /// <summary>
    /// ゲート召喚パターン詳細（仕様書A,B,Cパターン対応）
    /// </summary>
    public enum GateSpawnPattern
    {
        None,               // 召喚なし
        PatternA,           // 毎ターン2体召喚
        PatternB,           // 3ターンに1回、3体召喚
        PatternC,           // 初回のみ5体、以降2ターンに1回
        Periodic,           // 定期召喚
        OnDamage,           // ダメージ時召喚
        Continuous,         // 継続召喚
        Defensive           // 防御時召喚
    }

    /// <summary>
    /// ゲートデータの構造（仕様書に基づく拡張版）
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
        public GateSpawnPattern spawnPattern;      // 召喚パターン
        public GateSummonPattern summonPattern;    // 既存システム互換性
        public List<string> assignedEnemyTypes;   // 配置される敵種類
        public int maxEnemiesPerGate;             // ゲート当たり最大敵数
        public int spawnCooldown;                 // 召喚クールダウン
        public int lastSummonTurn;                // 最後に召喚したターン
        
        [Header("召喚設定詳細")]
        public int summonInterval;                // 召喚間隔（ターン）
        public int[] allowedEnemyIds;            // 召喚可能な敵ID配列
        public int summonCount;                  // 一度に召喚する敵数
        public bool isFirstSummonDone;           // 初回召喚完了フラグ（パターンC用）
        
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
            spawnPattern = GateSpawnPattern.PatternA;
            assignedEnemyTypes = new List<string>();
            maxEnemiesPerGate = 2;
            spawnCooldown = 3;
            lastSummonTurn = -1;
            
            // 召喚設定詳細の初期化
            summonInterval = 3;               // デフォルト3ターン間隔
            allowedEnemyIds = new int[] { 0, 1, 2 }; // デフォルト敵ID
            summonCount = 1;                  // デフォルト1体召喚
            isFirstSummonDone = false;
            
            // 戦略効果の初期化
            strategicEffect = GateStrategicEffect.None;
            effectStrength = 1.0f;
            isEffectActive = true;
            
            // 破壊時効果の初期化
            hasDestructionBonus = false;
            destructionReward = 100;
            destructionEffectDescription = "";
            
            // ゲートタイプに応じた初期設定
            ConfigureByType(type);
        }
        
        public bool IsDestroyed()
        {
            return currentHp <= 0;
        }
        
        public void TakeDamage(int damage)
        {
            currentHp = Mathf.Max(0, currentHp - damage);
        }
        
        public float GetHpPercentage()
        {
            return (float)currentHp / maxHp;
        }
        
        /// <summary>
        /// ゲートタイプに応じた設定の適用（仕様書に基づく）
        /// </summary>
        private void ConfigureByType(GateType type)
        {
            switch (type)
            {
                case GateType.Standard:
                    maxHp = 25000;
                    spawnPattern = GateSpawnPattern.PatternA;
                    summonInterval = 3;
                    summonCount = 1;
                    strategicEffect = GateStrategicEffect.None;
                    assignedEnemyTypes.AddRange(new string[] { "BasicEnemy", "Soldier" });
                    break;
                    
                case GateType.Elite:
                    maxHp = 35000;
                    spawnPattern = GateSpawnPattern.OnDamage;
                    summonInterval = 2;
                    summonCount = 1;
                    strategicEffect = GateStrategicEffect.AttackBoost;
                    effectStrength = 1.5f;
                    assignedEnemyTypes.AddRange(new string[] { "EliteEnemy", "Champion" });
                    break;
                    
                case GateType.Support:
                    maxHp = 20000;
                    spawnPattern = GateSpawnPattern.PatternB;
                    summonInterval = 4;
                    summonCount = 2;
                    strategicEffect = GateStrategicEffect.BuffAllEnemies;
                    effectStrength = 1.2f;
                    assignedEnemyTypes.AddRange(new string[] { "Healer", "Buffer" });
                    break;
                    
                case GateType.Summoner:
                    maxHp = 15000;
                    spawnPattern = GateSpawnPattern.PatternC;
                    summonInterval = 2;
                    summonCount = 3;
                    strategicEffect = GateStrategicEffect.IncreaseSpawnRate;
                    effectStrength = 2.0f;
                    assignedEnemyTypes.AddRange(new string[] { "Minion", "Spawn" });
                    break;
                    
                case GateType.Fortress:
                    maxHp = 50000;
                    spawnPattern = GateSpawnPattern.Defensive;
                    summonInterval = 5;
                    summonCount = 1;
                    strategicEffect = GateStrategicEffect.DefenseBoost;
                    effectStrength = 2.0f;
                    assignedEnemyTypes.AddRange(new string[] { "Guardian", "Tank" });
                    hasDestructionBonus = true;
                    destructionReward = 500;
                    break;
            }
            
            currentHp = maxHp;
        }
        
        /// <summary>
        /// 召喚可能かの判定（詳細パターン対応）
        /// </summary>
        public bool CanSummon(int currentTurn)
        {
            if (IsDestroyed() || currentTurn - lastSummonTurn < summonInterval)
                return false;
                
            switch (spawnPattern)
            {
                case GateSpawnPattern.PatternA:
                    return true; // 毎ターン2体召喚
                case GateSpawnPattern.PatternB:
                    return (currentTurn - lastSummonTurn) >= 3; // 3ターンに1回
                case GateSpawnPattern.PatternC:
                    if (!isFirstSummonDone)
                        return true; // 初回召喚
                    return (currentTurn - lastSummonTurn) >= 2; // 以降2ターンに1回
                case GateSpawnPattern.Periodic:
                    return currentTurn % summonInterval == 0;
                case GateSpawnPattern.Continuous:
                    return true;
                case GateSpawnPattern.OnDamage:
                    return currentHp < maxHp * 0.8f;
                case GateSpawnPattern.Defensive:
                    return currentHp < maxHp * 0.5f;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// 召喚実行時の処理
        /// </summary>
        public void OnSummonExecuted(int currentTurn)
        {
            lastSummonTurn = currentTurn;
            
            if (spawnPattern == GateSpawnPattern.PatternC && !isFirstSummonDone)
            {
                isFirstSummonDone = true;
            }
        }
        
        /// <summary>
        /// 戦略効果の適用
        /// </summary>
        public void ApplyStrategicEffect(List<EnemyInstance> targetEnemies)
        {
            if (!isEffectActive || IsDestroyed())
                return;
                
            switch (strategicEffect)
            {
                case GateStrategicEffect.BuffAllEnemies:
                    foreach (var enemy in targetEnemies)
                    {
                        enemy.ApplyBuff("GateBoost", effectStrength, -1);
                    }
                    break;
                    
                case GateStrategicEffect.AttackBoost:
                    foreach (var enemy in targetEnemies.Where(e => e.assignedGateId == gateId))
                    {
                        enemy.ApplyBuff("AttackBoost", effectStrength, -1);
                    }
                    break;
                    
                case GateStrategicEffect.DefenseBoost:
                    foreach (var enemy in targetEnemies.Where(e => e.assignedGateId == gateId))
                    {
                        enemy.ApplyBuff("DefenseBoost", effectStrength, -1);
                    }
                    break;
                    
                case GateStrategicEffect.Regeneration:
                    foreach (var enemy in targetEnemies.Where(e => e.assignedGateId == gateId))
                    {
                        int healAmount = Mathf.RoundToInt(enemy.enemyData.baseHp * 0.1f);
                        enemy.Heal(healAmount);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// ゲート破壊時の処理
        /// </summary>
        public void OnDestroyed()
        {
            currentHp = 0;
            isEffectActive = false;
            
            if (hasDestructionBonus)
            {
                Debug.Log($"Gate {gateName} destroyed! Bonus reward: {destructionReward}");
            }
        }
    }

    // 戦闘フィールドの管理クラス
    public class BattleField
    {
        private int columns;                    // 横幅（ゲート数）
        private const int rows = 2;             // 縦幅（固定2列）
        private EnemyInstance[,] gridEnemies;   // グリッド上の敵配置
        private List<GateData> gates;           // ゲート配列
        private Dictionary<GridPosition, bool> occupiedPositions; // 占有位置管理

        public int Columns => columns;
        public int Rows => rows;
        public List<GateData> Gates => gates;

        public BattleField(int gateCount)
        {
            columns = gateCount;
            gridEnemies = new EnemyInstance[columns, rows];
            gates = new List<GateData>();
            occupiedPositions = new Dictionary<GridPosition, bool>();
            
            // ゲートの初期化
            InitializeGates(gateCount);
        }

        /// <summary>
        /// ゲートの戦略的初期化（仕様書に基づく配置パターン）
        /// </summary>
        private void InitializeGates(int gateCount)
        {
            for (int i = 0; i < gateCount; i++)
            {
                GridPosition gatePos = new GridPosition(i, -1); // ゲートは行-1に配置
                GateType gateType = DetermineStrategicGateType(i, gateCount);
                GateData gate = new GateData(i, 25000, $"Gate_{i}", gatePos, gateType);
                gates.Add(gate);
            }
        }
        
        /// <summary>
        /// 戦略的ゲートタイプの決定（仕様書のパターンA,B,C対応）
        /// </summary>
        private GateType DetermineStrategicGateType(int gateIndex, int totalGates)
        {
            // 仕様書に基づく戦略的ゲート配置ロジック
            switch (totalGates)
            {
                case 1: // ボス戦等
                    return GateType.Fortress;
                    
                case 2: // 1段階：バランス型
                    if (gateIndex == 0) return GateType.Support;  // バフ役
                    return GateType.Standard;                     // 通常敵
                    
                case 3: // パターンA「支援優先」
                    if (gateIndex == 0) return GateType.Support;      // バフ役
                    if (gateIndex == 1) return GateType.Standard;     // 通常敵
                    return GateType.Elite;                            // 強敵
                    
                case 4: // パターンB「速攻勝負」
                    if (gateIndex == 0) return GateType.Support;      // バフ役
                    if (gateIndex == 1) return GateType.Summoner;     // 召喚特化
                    if (gateIndex == 2) return GateType.Standard;     // 通常敵
                    return GateType.Elite;                            // 強敵
                    
                case 5: // パターンC「パズル型」
                    if (gateIndex == 0) return GateType.Support;      // バフ役
                    if (gateIndex == 1) return GateType.Summoner;     // 召喚特化
                    if (gateIndex == 2) return GateType.Standard;     // 通常敵
                    if (gateIndex == 3) return GateType.Elite;        // 強敵
                    return GateType.Fortress;                         // 要塞
                    
                case 6: // 最大複雑度
                    if (gateIndex == 0) return GateType.Support;      // バフ役
                    if (gateIndex == 1) return GateType.Summoner;     // 召喚特化
                    if (gateIndex == 2) return GateType.Standard;     // 通常敵1
                    if (gateIndex == 3) return GateType.Standard;     // 通常敵2
                    if (gateIndex == 4) return GateType.Elite;        // 強敵
                    return GateType.Fortress;                         // 要塞
                    
                default:
                    // デフォルトパターン（7個以上の場合）
                    int pattern = gateIndex % 3;
                    switch (pattern)
                    {
                        case 0:
                            return GateType.Support;
                        case 1:
                            return GateType.Standard;
                        default:
                            return GateType.Elite;
                    }
            }
        }

        // 指定位置に敵を配置
        public bool PlaceEnemy(EnemyInstance enemy, GridPosition position)
        {
            if (!IsValidPosition(position) || IsOccupied(position))
                return false;

            gridEnemies[position.x, position.y] = enemy;
            occupiedPositions[position] = true;
            enemy.gridX = position.x;
            enemy.gridY = position.y;
            return true;
        }

        // 敵を削除
        public bool RemoveEnemy(GridPosition position)
        {
            if (!IsValidPosition(position) || !IsOccupied(position))
                return false;

            gridEnemies[position.x, position.y] = null;
            occupiedPositions.Remove(position);
            return true;
        }

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

        // 既存メソッドは削除し、CanDirectlyAttackGateに統合

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

        // フィールドの状態をリセット
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
            
            // ゲートのHPをリセット
            foreach (GateData gate in gates)
            {
                gate.currentHp = gate.maxHp;
                gate.lastSummonTurn = -1;
                gate.isFirstSummonDone = false;
                gate.isEffectActive = true;
            }
        }
        
        // ========================================
        // ゲート戦闘システム拡張メソッド（仕様書対応）
        // ========================================
        
        /// <summary>
        /// ゲートからの敵召喚処理（仕様書のA,B,Cパターン対応）
        /// </summary>
        public void ProcessGateSummoning(int currentTurn)
        {
            foreach (GateData gate in gates)
            {
                if (gate.CanSummon(currentTurn))
                {
                    SummonEnemiesFromGate(gate, currentTurn);
                    gate.OnSummonExecuted(currentTurn);
                }
            }
        }
        
        /// <summary>
        /// 指定ゲートからの敵召喚実行
        /// </summary>
        private void SummonEnemiesFromGate(GateData gate, int currentTurn)
        {
            int summonCount = GetActualSummonCount(gate, currentTurn);
            
            for (int i = 0; i < summonCount; i++)
            {
                GridPosition emptyPos = GetRandomEmptyPosition();
                if (emptyPos.x == -1) break; // 空きがない場合
                
                // 実際の敵召喚処理（EnemyDatabaseと連携、改良版）
                EnemyData enemyDataToSummon = SelectEnemyForGate(gate);
                if (enemyDataToSummon != null)
                {
                    EnemyInstance newEnemy = new EnemyInstance(enemyDataToSummon, emptyPos.x, emptyPos.y);
                    newEnemy.assignedGateId = gate.gateId;
                    
                    if (PlaceEnemy(newEnemy, emptyPos))
                    {
                        Debug.Log($"ゲート{gate.gateName}({gate.gateType})から{enemyDataToSummon.enemyName}を召喚: ({emptyPos.x}, {emptyPos.y})");
                    }
                }
            }
        }
        
        /// <summary>
        /// パターンに応じた実際の召喚数を取得
        /// </summary>
        private int GetActualSummonCount(GateData gate, int currentTurn)
        {
            switch (gate.spawnPattern)
            {
                case GateSpawnPattern.PatternA:
                    return 2; // 毎ターン2体
                case GateSpawnPattern.PatternB:
                    return 3; // 3ターンに1回3体
                case GateSpawnPattern.PatternC:
                    return !gate.isFirstSummonDone ? 5 : 1; // 初回5体、以降1体
                default:
                    return gate.summonCount; // デフォルト
            }
        }
        
        /// <summary>
        /// ゲートに応じた敵の選択（改良版、実際のEnemyDatabaseと連携可能）
        /// </summary>
        private EnemyData SelectEnemyForGate(GateData gate)
        {
            // ゲートに割り当てられた敵IDがある場合はそれを使用
            if (gate.allowedEnemyIds != null && gate.allowedEnemyIds.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, gate.allowedEnemyIds.Length);
                int selectedEnemyId = gate.allowedEnemyIds[randomIndex];
                
                // TODO: 実際のEnemyDatabaseからデータを取得
                // return enemyDatabase.GetEnemy(selectedEnemyId);
            }
            
            // フォールバック：ゲートタイプに応じたデフォルト敵を作成
            return CreateDefaultEnemyForGateType(gate.gateType);
        }
        
        /// <summary>
        /// ゲートタイプに応じたデフォルト敵データを作成（仮実装）
        /// </summary>
        private EnemyData CreateDefaultEnemyForGateType(GateType gateType)
        {
            EnemyData enemyData = new EnemyData();
            
            switch (gateType)
            {
                case GateType.Elite:
                    enemyData.enemyName = "エリート敵";
                    enemyData.enemyId = 101;
                    enemyData.baseHp = 8000;
                    enemyData.attackPower = 2500;
                    enemyData.category = EnemyCategory.Attacker;
                    break;
                case GateType.Support:
                    enemyData.enemyName = "サポート敵";
                    enemyData.enemyId = 102;
                    enemyData.baseHp = 4000;
                    enemyData.attackPower = 1000;
                    enemyData.category = EnemyCategory.Support;
                    enemyData.primaryAction = EnemyActionType.BuffAlly;
                    break;
                case GateType.Summoner:
                    enemyData.enemyName = "召喚敵";
                    enemyData.enemyId = 103;
                    enemyData.baseHp = 3000;
                    enemyData.attackPower = 800;
                    enemyData.category = EnemyCategory.Special;
                    enemyData.primaryAction = EnemyActionType.Summon;
                    break;
                case GateType.Fortress:
                    enemyData.enemyName = "要塞敵";
                    enemyData.enemyId = 104;
                    enemyData.baseHp = 12000;
                    enemyData.attackPower = 2000;
                    enemyData.category = EnemyCategory.Vanguard;
                    break;
                default: // Standard
                    enemyData.enemyName = "標準敵";
                    enemyData.enemyId = 100;
                    enemyData.baseHp = 5000;
                    enemyData.attackPower = 1500;
                    enemyData.category = EnemyCategory.Attacker;
                    break;
            }
            
            return enemyData;
        }
        
        /// <summary>
        /// ゲートの戦略効果を処理
        /// </summary>
        public void ProcessGateStrategicEffects()
        {
            List<EnemyInstance> allEnemies = GetAllEnemies();
            
            foreach (GateData gate in gates)
            {
                if (!gate.IsDestroyed())
                {
                    gate.ApplyStrategicEffect(allEnemies);
                }
            }
        }
        
        /// <summary>
        /// 指定ゲートを取得
        /// </summary>
        public GateData GetGate(int gateId)
        {
            return gates.Find(g => g.gateId == gateId);
        }
        
        /// <summary>
        /// 生存中のゲートを取得
        /// </summary>
        public List<GateData> GetAliveGates()
        {
            return gates.Where(g => !g.IsDestroyed()).ToList();
        }
        
        /// <summary>
        /// 指定列がゲート列かの判定
        /// </summary>
        public bool IsGateColumn(int column)
        {
            return gates.Any(g => g.position.x == column && !g.IsDestroyed());
        }
        
        /// <summary>
        /// 指定列のゲートを取得
        /// </summary>
        public GateData GetGateInColumn(int column)
        {
            return gates.Find(g => g.position.x == column);
        }
        
        /// <summary>
        /// ゲートへの直接攻撃が可能かの判定（既存CanAttackGateとの統合）
        /// </summary>
        public bool CanDirectlyAttackGate(int column)
        {
            // 配列範囲チェック
            if (column < 0 || column >= columns)
                return false;
                
            // ゲート攻撃条件：対象列に生きているゲートが存在し、その前方に敵がいない
            GateData targetGate = GetGateInColumn(column);
            if (targetGate == null || targetGate.IsDestroyed())
                return false;
                
            // グリッド配列の初期化チェック
            if (gridEnemies == null)
                return true;
                
            // 前方の敵をチェック（簡単な実装）
            for (int row = 0; row < rows; row++)
            {
                if (gridEnemies[column, row] != null && gridEnemies[column, row].IsAlive())
                {
                    return false; // 前方に敵がいる場合は攻撃不可
                }
            }
            
            return true;
        }
    }
}