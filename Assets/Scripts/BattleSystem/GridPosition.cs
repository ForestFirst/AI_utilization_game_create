using System;
using UnityEngine;

namespace BattleSystem
{
    // グリッド位置の構造体
    [Serializable]
    public struct GridPosition
    {
        public int x;
        public int y;
        
        public GridPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        
        public static GridPosition Zero => new GridPosition(0, 0);
        public static GridPosition Invalid => new GridPosition(-1, -1);
        
        public bool IsValid()
        {
            return x >= 0 && y >= 0;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is GridPosition other)
            {
                return x == other.x && y == other.y;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode();
        }
        
        public static bool operator ==(GridPosition a, GridPosition b)
        {
            return a.x == b.x && a.y == b.y;
        }
        
        public static bool operator !=(GridPosition a, GridPosition b)
        {
            return !(a == b);
        }
        
        public override string ToString()
        {
            return $"({x}, {y})";
        }
    }
    
    // 戦闘フィールドのサイズ定数
    public static class BattleFieldConstants
    {
        public const int FIELD_WIDTH = 4;
        public const int FIELD_HEIGHT = 3;
        public const int MAX_ENEMIES_PER_FIELD = FIELD_WIDTH * FIELD_HEIGHT;
        public const int MAX_GATES = 3;
    }
}
