using System;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 手札カードデータ構造
    /// 武器データ + 攻撃対象列情報を組み合わせたカード情報
    /// トランプゲームライクな手札システムで使用
    /// </summary>
    [Serializable]
    public class CardData
    {
        [Header("カード基本情報")]
        public string cardId;           // カード固有ID（手札管理用）
        public WeaponData weaponData;   // 元となる武器データ
        public int targetColumn;        // 攻撃対象列（0=左列, 1=中列, 2=右列...）
        
        [Header("表示情報")]
        public string displayName;      // カード表示名「炎の剣（左列）」
        public string columnName;       // 列名「左列」「中列」「右列」

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public CardData()
        {
            cardId = "";
            weaponData = new WeaponData();
            targetColumn = 0;
            displayName = "";
            columnName = "";
        }

        /// <summary>
        /// カードデータ作成コンストラクタ
        /// </summary>
        /// <param name="weapon">元となる武器データ</param>
        /// <param name="column">攻撃対象列（0始まり）</param>
        /// <param name="totalColumns">総列数（表示名生成用）</param>
        public CardData(WeaponData weapon, int column, int totalColumns = 3)
        {
            // カード固有ID生成（武器名_列番号_タイムスタンプ）
            cardId = GenerateCardId(weapon.weaponName, column);
            
            // 武器データのディープコピー（元データの変更を防ぐ）
            weaponData = CopyWeaponData(weapon);
            
            // 攻撃対象列設定
            targetColumn = Mathf.Clamp(column, 0, totalColumns - 1);
            
            // 列名生成
            columnName = GenerateColumnName(targetColumn, totalColumns);
            
            // 表示名生成
            displayName = GenerateDisplayName(weapon.weaponName, columnName);
        }

        /// <summary>
        /// カード固有ID生成
        /// </summary>
        /// <param name="weaponName">武器名</param>
        /// <param name="column">列番号</param>
        /// <returns>カード固有ID</returns>
        private string GenerateCardId(string weaponName, int column)
        {
            var timestamp = DateTime.Now.Ticks.ToString().Substring(10); // 短縮タイムスタンプ
            return $"{weaponName}_{column}_{timestamp}";
        }

        /// <summary>
        /// 武器データのディープコピー
        /// </summary>
        /// <param name="original">コピー元武器データ</param>
        /// <returns>コピーされた武器データ</returns>
        private WeaponData CopyWeaponData(WeaponData original)
        {
            if (original == null) return new WeaponData();

            var copy = new WeaponData
            {
                weaponName = original.weaponName,
                attackAttribute = original.attackAttribute,
                weaponType = original.weaponType,
                basePower = original.basePower,
                attackRange = original.attackRange,
                criticalRate = original.criticalRate,
                cooldownTurns = original.cooldownTurns,
                specialEffect = original.specialEffect,
                effectValue = original.effectValue,
                effectDuration = original.effectDuration,
                canUseConsecutively = original.canUseConsecutively
            };

            return copy;
        }

        /// <summary>
        /// 列名生成（日本語対応）
        /// </summary>
        /// <param name="column">列番号（0始まり）</param>
        /// <param name="totalColumns">総列数</param>
        /// <returns>列名</returns>
        private string GenerateColumnName(int column, int totalColumns)
        {
            if (totalColumns <= 1) return "";

            // 3列の場合：左列、中列、右列
            if (totalColumns == 3)
            {
                switch (column)
                {
                    case 0: return "左列";
                    case 1: return "中列";
                    case 2: return "右列";
                    default: return $"{column + 1}列";
                }
            }
            // 2列の場合：左列、右列
            else if (totalColumns == 2)
            {
                switch (column)
                {
                    case 0: return "左列";
                    case 1: return "右列";
                    default: return $"{column + 1}列";
                }
            }
            // 4列以上の場合：数字ベース
            else
            {
                return $"{column + 1}列";
            }
        }

        /// <summary>
        /// カード表示名生成
        /// </summary>
        /// <param name="weaponName">武器名</param>
        /// <param name="columnName">列名</param>
        /// <returns>表示名</returns>
        private string GenerateDisplayName(string weaponName, string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
                return weaponName;
            
            return $"{weaponName}（{columnName}）";
        }

        /// <summary>
        /// カードが指定された列を攻撃可能かチェック
        /// </summary>
        /// <param name="availableColumns">利用可能な列のリスト</param>
        /// <returns>攻撃可能な場合true</returns>
        public bool CanAttackColumn(int[] availableColumns)
        {
            if (availableColumns == null || availableColumns.Length == 0)
                return false;

            foreach (int availableColumn in availableColumns)
            {
                if (targetColumn == availableColumn)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 攻撃範囲による対象列の妥当性チェック
        /// </summary>
        /// <param name="totalColumns">戦場の総列数</param>
        /// <returns>妥当な場合true</returns>
        public bool IsValidTarget(int totalColumns)
        {
            // 全体攻撃の場合は常に妥当
            if (weaponData.attackRange == AttackRange.All || 
                weaponData.attackRange == AttackRange.Self)
                return true;

            // 列指定攻撃の場合は列番号の範囲チェック
            return targetColumn >= 0 && targetColumn < totalColumns;
        }

        /// <summary>
        /// カード情報の文字列表現（デバッグ用）
        /// </summary>
        /// <returns>カード情報文字列</returns>
        public override string ToString()
        {
            return $"Card[{cardId}]: {displayName} | Power:{weaponData.basePower} | Range:{weaponData.attackRange} | Target:Column{targetColumn}";
        }

        /// <summary>
        /// 2つのカードが同じ武器・列の組み合わせかチェック
        /// </summary>
        /// <param name="other">比較対象のカード</param>
        /// <returns>同じ組み合わせの場合true</returns>
        public bool IsSameCombination(CardData other)
        {
            if (other == null) return false;
            
            return weaponData.weaponName == other.weaponData.weaponName && 
                   targetColumn == other.targetColumn;
        }
    }

    /// <summary>
    /// カードデータ管理用のユーティリティクラス
    /// </summary>
    public static class CardDataUtility
    {
        /// <summary>
        /// 武器リストと列数からカードプールを生成
        /// </summary>
        /// <param name="weapons">武器データリスト</param>
        /// <param name="totalColumns">総列数</param>
        /// <returns>生成されたカードプール</returns>
        public static CardData[] GenerateCardPool(WeaponData[] weapons, int totalColumns)
        {
            if (weapons == null || weapons.Length == 0 || totalColumns <= 0)
                return new CardData[0];

            var cardPool = new CardData[weapons.Length * totalColumns];
            int index = 0;

            foreach (var weapon in weapons)
            {
                for (int column = 0; column < totalColumns; column++)
                {
                    cardPool[index] = new CardData(weapon, column, totalColumns);
                    index++;
                }
            }

            return cardPool;
        }

        /// <summary>
        /// カードプールから指定枚数をランダム抽出
        /// </summary>
        /// <param name="cardPool">カードプール</param>
        /// <param name="handSize">手札枚数</param>
        /// <param name="allowDuplicates">重複を許可するか</param>
        /// <returns>抽出されたカードリスト</returns>
        public static CardData[] DrawRandomCards(CardData[] cardPool, int handSize, bool allowDuplicates = true)
        {
            if (cardPool == null || cardPool.Length == 0 || handSize <= 0)
                return new CardData[0];

            var hand = new CardData[handSize];
            var random = new System.Random();

            if (allowDuplicates)
            {
                // 重複あり：完全ランダム抽出
                for (int i = 0; i < handSize; i++)
                {
                    int randomIndex = random.Next(cardPool.Length);
                    hand[i] = new CardData(cardPool[randomIndex].weaponData, 
                                         cardPool[randomIndex].targetColumn);
                }
            }
            else
            {
                // 重複なし：シャッフル抽出
                var shuffled = new CardData[cardPool.Length];
                Array.Copy(cardPool, shuffled, cardPool.Length);
                
                // Fisher-Yatesシャッフル
                for (int i = shuffled.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    var temp = shuffled[i];
                    shuffled[i] = shuffled[j];
                    shuffled[j] = temp;
                }

                // 手札枚数分を取得
                int actualHandSize = Mathf.Min(handSize, shuffled.Length);
                for (int i = 0; i < actualHandSize; i++)
                {
                    hand[i] = shuffled[i];
                }
            }

            return hand;
        }
    }
}
