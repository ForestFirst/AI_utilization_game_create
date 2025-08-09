using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    // GridPositionは別ファイルで定義済み

    // ゲートデータの構造
    [Serializable]
    public class GateData
    {
        public int gateId;
        public int maxHp;               // 最大HP（基本25,000）
        public int currentHp;           // 現在HP
        public string gateName;
        public GateSummonPattern summonPattern;
        public int lastSummonTurn;      // 最後に召喚したターン
        public GridPosition position;   // ゲートの位置（列番号）
        
        public GateData(int id, int hp, string name, GridPosition pos)
        {
            gateId = id;
            maxHp = hp;
            currentHp = hp;
            gateName = name;
            lastSummonTurn = -1;
            position = pos;
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

        private void InitializeGates(int gateCount)
        {
            for (int i = 0; i < gateCount; i++)
            {
                GridPosition gatePos = new GridPosition(i, -1); // ゲートは行-1に配置
                GateData gate = new GateData(i, 25000, $"Gate_{i}", gatePos);
                gates.Add(gate);
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
            }
        }
    }
}