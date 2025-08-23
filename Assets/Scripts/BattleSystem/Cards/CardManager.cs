using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BattleSystem;

namespace BattleSystem.Cards
{
    /// <summary>
    /// カード管理を担当するクラス
    /// 武器からカード生成、手札管理、カード効果処理を行う
    /// </summary>
    public class CardManager : MonoBehaviour
    {
        [Header("カード生成設定")]
        [SerializeField] private int handSize = 5;
        [SerializeField] private bool allowDuplicateCards = true;
        [SerializeField] private bool debugMode = false;

        // イベント定義
        public event Action<List<CardData>> OnHandGenerated;
        public event Action<CardData> OnCardPlayed;
        public event Action OnHandCleared;

        // 現在の手札とカードデータ
        private List<CardData> currentHand;
        private Dictionary<string, CardData> cardDatabase;
        private PlayerWeaponData playerWeapons;

        #region Properties

        /// <summary>
        /// 現在の手札
        /// </summary>
        public List<CardData> CurrentHand => currentHand ?? new List<CardData>();

        /// <summary>
        /// 手札枚数
        /// </summary>
        public int HandSize
        {
            get => handSize;
            set => handSize = Mathf.Clamp(value, 1, 10);
        }

        /// <summary>
        /// 重複カード許可
        /// </summary>
        public bool AllowDuplicateCards
        {
            get => allowDuplicateCards;
            set => allowDuplicateCards = value;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// カードマネージャーの初期化
        /// </summary>
        /// <param name="weapons">プレイヤー武器データ</param>
        public void Initialize(PlayerWeaponData weapons)
        {
            playerWeapons = weapons;
            currentHand = new List<CardData>();
            cardDatabase = new Dictionary<string, CardData>();

            InitializeCardDatabase();
            LogDebug("CardManager initialized");
        }

        /// <summary>
        /// カードデータベースの初期化
        /// </summary>
        private void InitializeCardDatabase()
        {
            // 基本カードの登録
            RegisterBasicCards();
            
            // プレイヤー武器からカードを生成
            if (playerWeapons != null)
            {
                GenerateCardsFromWeapons();
            }
        }

        /// <summary>
        /// 基本カードの登録
        /// </summary>
        private void RegisterBasicCards()
        {
            var basicCards = new[]
            {
                CreateBasicCard(1, "基本攻撃", "基本的な攻撃カード", 10, "none"),
                CreateBasicCard(2, "強攻撃", "威力の高い攻撃", 20, "none"),
                CreateBasicCard(3, "連続攻撃", "複数回攻撃", 8, "none"),
            };

            foreach (var card in basicCards)
            {
                cardDatabase[card.cardId] = card;
            }
        }

        /// <summary>
        /// 基本カードを作成
        /// </summary>
        private CardData CreateBasicCard(int id, string name, string description, int damage, string type)
        {
            // 現在のCardData構造では基本カードは作成できないため、ダミーを返す
            var dummyWeapon = new WeaponData();
            return new CardData(dummyWeapon, 0, 3)
            {
                cardId = id.ToString()
            };
        }

        /// <summary>
        /// プレイヤー武器からカードを生成
        /// </summary>
        private void GenerateCardsFromWeapons()
        {
            if (playerWeapons?.weapons == null) return;

            int cardId = 100; // 武器カードは100番台から開始
            
            foreach (var weapon in playerWeapons.weapons)
            {
                if (weapon != null)
                {
                    var weaponCard = CreateWeaponCard(cardId++, weapon);
                    cardDatabase[weaponCard.cardId] = weaponCard;
                }
            }
        }

        /// <summary>
        /// 武器からカードを作成
        /// </summary>
        private CardData CreateWeaponCard(int cardId, WeaponData weapon)
        {
            return new CardData(weapon, 0, 3) // 左列をデフォルトに設定
            {
                cardId = cardId.ToString()
            };
        }

        /// <summary>
        /// 武器のマナコストを計算
        /// </summary>
        private int CalculateManaCost(WeaponData weapon)
        {
            // 攻撃力に基づいてマナコストを計算
            return Mathf.Clamp(weapon.basePower / 10, 1, 5);
        }

        /// <summary>
        /// カードのレアリティを決定
        /// </summary>
        private CardRarity DetermineCardRarity(WeaponData weapon)
        {
            return weapon.basePower switch
            {
                <= 15 => CardRarity.Common,
                <= 30 => CardRarity.Uncommon,
                <= 50 => CardRarity.Rare,
                _ => CardRarity.Epic
            };
        }

        #endregion

        #region Hand Management

        /// <summary>
        /// 手札を生成
        /// </summary>
        /// <returns>生成に成功したかどうか</returns>
        public bool GenerateHand()
        {
            ClearHand();

            if (cardDatabase.Count == 0)
            {
                LogDebug("No cards available for hand generation");
                return false;
            }

            var availableCards = cardDatabase.Values.ToList();
            var generatedCards = new List<CardData>();

            for (int i = 0; i < handSize; i++)
            {
                var selectedCard = SelectRandomCard(availableCards, generatedCards);
                if (selectedCard != null)
                {
                    generatedCards.Add(selectedCard);
                    
                    // 重複不許可の場合は選択済みカードを除外
                    if (!allowDuplicateCards)
                    {
                        availableCards.Remove(selectedCard);
                    }
                }
            }

            currentHand = generatedCards;
            OnHandGenerated?.Invoke(currentHand);
            
            LogDebug($"Hand generated: {currentHand.Count} cards");
            return currentHand.Count > 0;
        }

        /// <summary>
        /// ランダムなカードを選択
        /// </summary>
        private CardData SelectRandomCard(List<CardData> availableCards, List<CardData> alreadySelected)
        {
            if (availableCards.Count == 0) return null;

            // レアリティによる重み付け選択
            var weightedCards = CalculateCardWeights(availableCards);
            var totalWeight = weightedCards.Sum(x => x.weight);
            var randomValue = UnityEngine.Random.Range(0f, totalWeight);

            float currentWeight = 0f;
            foreach (var weightedCard in weightedCards)
            {
                currentWeight += weightedCard.weight;
                if (randomValue <= currentWeight)
                {
                    return weightedCard.card;
                }
            }

            return availableCards.LastOrDefault();
        }

        /// <summary>
        /// カードの重みを計算
        /// </summary>
        private List<(CardData card, float weight)> CalculateCardWeights(List<CardData> cards)
        {
            return cards.Select(card => (card, GetCardWeight(card))).ToList();
        }

        /// <summary>
        /// カードの重みを取得
        /// </summary>
        private float GetCardWeight(CardData card)
        {
            // 武器の威力でレアリティを判定してから重みを返す
            var rarity = DetermineCardRarity(card.weaponData);
            return rarity switch
            {
                CardRarity.Common => 1.0f,
                CardRarity.Uncommon => 0.7f,
                CardRarity.Rare => 0.3f,
                CardRarity.Epic => 0.1f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// 手札をクリア
        /// </summary>
        public void ClearHand()
        {
            currentHand?.Clear();
            OnHandCleared?.Invoke();
            LogDebug("Hand cleared");
        }

        /// <summary>
        /// カードを手札から削除
        /// </summary>
        /// <param name="card">削除するカード</param>
        /// <returns>削除に成功したかどうか</returns>
        public bool RemoveCardFromHand(CardData card)
        {
            if (currentHand == null || card == null) return false;

            var removed = currentHand.Remove(card);
            if (removed)
            {
                OnCardPlayed?.Invoke(card);
                LogDebug($"Card removed from hand: {card.displayName}");
            }

            return removed;
        }

        /// <summary>
        /// 手札にカードを追加
        /// </summary>
        /// <param name="card">追加するカード</param>
        /// <returns>追加に成功したかどうか</returns>
        public bool AddCardToHand(CardData card)
        {
            if (currentHand == null || card == null) return false;
            if (currentHand.Count >= handSize) return false;

            currentHand.Add(card);
            LogDebug($"Card added to hand: {card.displayName}");
            return true;
        }

        #endregion

        #region Card Queries

        /// <summary>
        /// カードIDでカードを取得
        /// </summary>
        /// <param name="cardId">カードID</param>
        /// <returns>カードデータ、見つからない場合はnull</returns>
        public CardData GetCardById(string cardId)
        {
            return cardDatabase.TryGetValue(cardId, out var card) ? card : null;
        }

        /// <summary>
        /// 武器タイプでカードを検索
        /// </summary>
        /// <param name="weaponType">武器タイプ</param>
        /// <returns>該当するカードのリスト</returns>
        public List<CardData> GetCardsByWeaponType(string weaponType)
        {
            return cardDatabase.Values
                .Where(card => card.weaponData.weaponType.ToString() == weaponType)
                .ToList();
        }

        /// <summary>
        /// レアリティでカードを検索
        /// </summary>
        /// <param name="rarity">レアリティ</param>
        /// <returns>該当するカードのリスト</returns>
        public List<CardData> GetCardsByRarity(CardRarity rarity)
        {
            return cardDatabase.Values
                .Where(card => DetermineCardRarity(card.weaponData) == rarity)
                .ToList();
        }

        /// <summary>
        /// 手札内でカードを検索
        /// </summary>
        /// <param name="predicate">検索条件</param>
        /// <returns>該当するカードのリスト</returns>
        public List<CardData> FindCardsInHand(Func<CardData, bool> predicate)
        {
            return currentHand?.Where(predicate).ToList() ?? new List<CardData>();
        }

        #endregion

        #region Validation

        /// <summary>
        /// カードが使用可能かどうかを確認
        /// </summary>
        /// <param name="card">確認するカード</param>
        /// <returns>使用可能かどうか</returns>
        public bool CanPlayCard(CardData card)
        {
            if (card == null) return false;
            if (currentHand == null || !currentHand.Contains(card)) return false;

            // マナコストチェック（実装は別途必要）
            // return HasEnoughMana(card.manaCost);
            
            return true;
        }

        /// <summary>
        /// 手札の有効性をチェック
        /// </summary>
        /// <returns>手札が有効かどうか</returns>
        public bool IsHandValid()
        {
            return currentHand != null && currentHand.Count > 0;
        }

        #endregion

        #region Debug

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        /// <param name="message">ログメッセージ</param>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[CardManager] {message}");
            }
        }

        /// <summary>
        /// 手札情報をログ出力
        /// </summary>
        public void LogHandInfo()
        {
            if (!debugMode) return;

            if (currentHand == null || currentHand.Count == 0)
            {
                Debug.Log("[CardManager] Hand is empty");
                return;
            }

            Debug.Log($"[CardManager] Current Hand ({currentHand.Count} cards):");
            for (int i = 0; i < currentHand.Count; i++)
            {
                var card = currentHand[i];
                Debug.Log($"  {i + 1}. {card.displayName} (ID: {card.cardId}, Damage: {card.weaponData.basePower}, Type: {card.weaponData.weaponType})");
            }
        }

        #endregion
    }
}