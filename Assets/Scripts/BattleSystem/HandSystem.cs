using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// 手札の状態
    /// </summary>
    public enum HandState
    {
        Empty,          // 空の手札
        Generated,      // 手札生成済み
        CardUsed,       // カード使用後
        TurnEnded       // ターン終了
    }

    /// <summary>
    /// カード使用結果
    /// </summary>
    public struct CardPlayResult
    {
        public bool isSuccess;
        public string message;
        public CardData playedCard;
        public int damageDealt;
        public bool turnEnded;
    }

    /// <summary>
    /// 予告ダメージ情報
    /// </summary>
    [Serializable]
    public class PendingDamageInfo
    {
        public CardData usedCard;
        public int calculatedDamage;
        public List<EnemyInstance> targetEnemies;
        public List<GateData> targetGates;
        public string description;
        public float timestamp;
        
        public PendingDamageInfo(CardData card, int damage, string desc)
        {
            usedCard = card;
            calculatedDamage = damage;
            targetEnemies = new List<EnemyInstance>();
            targetGates = new List<GateData>();
            description = desc;
            timestamp = Time.time;
        }
    }

    /// <summary>
    /// 統合ダメージ計算情報
    /// コンボ効果、装備効果、バフ/デバフを含む完全なダメージ内訳
    /// </summary>
    [Serializable]
    public struct DamageCalculationInfo
    {
        public int baseDamage;          // 基本ダメージ
        public float comboMultiplier;   // コンボ倍率
        public int comboDamage;         // コンボ追加ダメージ
        public float otherMultiplier;   // その他効果倍率
        public int otherDamage;         // その他追加ダメージ
        public int finalDamage;         // 最終ダメージ
        public string detailDescription; // 詳細説明
        
        /// <summary>
        /// 詳細な説明文を生成
        /// </summary>
        public string GetDetailedDescription(string cardName)
        {
            if (string.IsNullOrEmpty(detailDescription))
            {
                var parts = new List<string> { $"基本: {baseDamage}" };
                
                if (comboDamage > 0)
                    parts.Add($"コンボ: +{comboDamage} (x{comboMultiplier:F1})");
                
                if (otherDamage > 0)
                    parts.Add($"装備効果: +{otherDamage} (x{otherMultiplier:F1})");
                
                detailDescription = $"{cardName}: {string.Join(", ", parts)} = {finalDamage}ダメージ";
            }
            
            return detailDescription;
        }
    }

    /// <summary>
    /// 手札システム管理クラス
    /// プレイヤーの装備武器から手札を生成し、カードベースの攻撃を管理
    /// </summary>
    public class HandSystem : MonoBehaviour
    {
        [Header("手札設定")]
        [SerializeField] private int handSize = 5;                      // 手札枚数
        [SerializeField] private bool allowDuplicateCards = true;       // 重複カード許可
        [SerializeField] private bool autoGenerateOnTurnStart = true;   // ターン開始時自動生成
        [SerializeField] private bool debugMode = true;                // デバッグモード
        
        [Header("行動回数設定")]
        [SerializeField] private int baseActionsPerTurn = 1;            // 基本行動回数/ターン
        [SerializeField] private bool autoEndTurnWhenActionsExhausted = true; // 行動回数0時自動ターン終了
        [SerializeField] private float autoEndTurnDelay = 0.5f;         // 自動ターン終了時の遅延（秒）

        [Header("カード生成設定")]
        [SerializeField] private bool respectWeaponRange = true;        // 武器の攻撃範囲を考慮
        [SerializeField] private bool excludeInvalidTargets = false;    // 無効ターゲットを除外

        // システム参照
        private BattleManager battleManager;
        private BattleField battleField;
        private WeaponSelectionSystem weaponSelectionSystem;
        private ComboSystem comboSystem;

        // 手札状態
        private HandState currentHandState;
        private CardData[] currentHand;
        private CardData[] fullCardPool;
        private List<CardData> usedCards;           // 使用済みカード履歴
        private List<CardData[]> handHistory;       // 手札履歴（デバッグ用）

        // 行動回数管理
        private int maxActionsPerTurn;          // 現在のターンの最大行動回数
        private int remainingActions;           // 残り行動回数
        private int actionBonus;                // アタッチメント等による行動回数ボーナス
        
        // 統計データ
        private int totalCardsPlayed;
        private int totalDamageDealt;
        private Dictionary<string, int> weaponUsageCount; // 武器別使用回数
        
        // 予告ダメージシステム
        private PendingDamageInfo currentPendingDamage;
        private List<PendingDamageInfo> pendingDamageHistory;

        // イベント定義
        public event Action<CardData[]> OnHandGenerated;           // 手札生成時
        public event Action<CardData> OnCardPlayed;                // カード使用時
        public event Action<CardPlayResult> OnCardPlayResult;      // カード使用結果
        public event Action<HandState> OnHandStateChanged;         // 手札状態変更時
        public event Action OnHandCleared;                         // 手札クリア時
        public event Action<PendingDamageInfo> OnPendingDamageCalculated; // 予告ダメージ計算時
        public event Action<PendingDamageInfo> OnPendingDamageApplied;     // 予告ダメージ適用時
        public event Action OnPendingDamageCleared;                        // 予告ダメージクリア時
        
        // 行動回数関連イベント
        public event Action<int, int> OnActionsChanged;            // 行動回数変更時 (残り, 最大)
        public event Action OnActionsExhausted;                    // 行動回数0時
        public event Action OnAutoTurnEnd;                         // 自動ターン終了時
        
        // 戦闘データ変更関連イベント
        public event Action OnEnemyDataChanged;                    // 敵データ変更時
        public event Action OnBattleFieldChanged;                  // 戦場データ変更時

        // プロパティ
        public HandState CurrentHandState => currentHandState;
        public CardData[] CurrentHand => currentHand?.ToArray(); // コピーを返す
        public int HandSize => handSize;
        public int RemainingCards => currentHand?.Count(card => card != null) ?? 0;
        public bool HasUsableCards => GetUsableCards().Length > 0;
        public PendingDamageInfo CurrentPendingDamage => currentPendingDamage; // 予告ダメージ情報
        public bool HasPendingDamage => currentPendingDamage != null;          // 予告ダメージがあるか
        
        // 行動回数関連プロパティ
        public int RemainingActions => remainingActions;           // 残り行動回数
        public int MaxActionsPerTurn => maxActionsPerTurn;         // 最大行動回数
        public bool CanTakeAction => remainingActions > 0 && currentHandState == HandState.Generated; // 行動可能か
        public bool HasActionsRemaining => remainingActions > 0;   // 行動回数が残っているか

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeHandSystem();
        }

        private void OnEnable()
        {
            RegisterEventHandlers();
        }

        private void OnDisable()
        {
            UnregisterEventHandlers();
        }

        #endregion

        #region System Initialization

        /// <summary>
        /// 手札システムの初期化
        /// </summary>
        private void InitializeHandSystem()
        {
            // システム参照の取得
            battleManager = GetComponent<BattleManager>();
            weaponSelectionSystem = GetComponent<WeaponSelectionSystem>();
            comboSystem = GetComponent<ComboSystem>();
            
            if (battleManager != null)
                battleField = battleManager.BattleField;

            // データ初期化
            currentHand = new CardData[handSize];
            usedCards = new List<CardData>();
            handHistory = new List<CardData[]>();
            weaponUsageCount = new Dictionary<string, int>();
            pendingDamageHistory = new List<PendingDamageInfo>();
            
            // 状態初期化
            currentHandState = HandState.Empty;
            totalCardsPlayed = 0;
            totalDamageDealt = 0;
            currentPendingDamage = null;
            
            // 行動回数初期化
            actionBonus = 0;
            maxActionsPerTurn = baseActionsPerTurn;
            remainingActions = 0;

            LogDebug("HandSystem initialized");
        }

        /// <summary>
        /// イベントハンドラーの登録
        /// </summary>
        private void RegisterEventHandlers()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged += HandleTurnChanged;
                battleManager.OnGameStateChanged += HandleGameStateChanged;
                battleManager.OnPlayerDataChanged += HandlePlayerDataChanged;
            }
            
            // AttachmentSystemのイベント購読
            var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
            if (attachmentSystem != null)
            {
                attachmentSystem.OnWeaponCardsGenerated += HandleWeaponCardsGenerated;
                LogDebug("AttachmentSystem events registered");
            }
        }

        /// <summary>
        /// イベントハンドラーの解除
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged -= HandleTurnChanged;
                battleManager.OnGameStateChanged -= HandleGameStateChanged;
                battleManager.OnPlayerDataChanged -= HandlePlayerDataChanged;
            }
            
            // AttachmentSystemのイベント解除
            var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
            if (attachmentSystem != null)
            {
                attachmentSystem.OnWeaponCardsGenerated -= HandleWeaponCardsGenerated;
                LogDebug("AttachmentSystem events unregistered");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ターン変更時の処理
        /// </summary>
        private void HandleTurnChanged(int turn)
        {
            if (autoGenerateOnTurnStart && battleManager.CurrentState == GameState.PlayerTurn)
            {
                // プレイヤーターン開始時の初期化
                InitializeActionsForTurn();
                
                // ターン開始時に武器カードの列をランダム再生成
                var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
                if (attachmentSystem != null)
                {
                    attachmentSystem.RegenerateWeaponCardsForNewTurn();
                }
                
                // 手札生成は武器カード再生成後に自動的に実行される
                GenerateHand();
            }
        }

        /// <summary>
        /// ゲーム状態変更時の処理
        /// </summary>
        private void HandleGameStateChanged(GameState newState)
        {
            LogDebug($"ゲーム状態変更: {newState}, 現在の手札状態: {currentHandState}, 残り行動回数: {remainingActions}");
            
            switch (newState)
            {
                case GameState.PlayerTurn:
                    // プレイヤーターン開始時の処理
                    LogDebug("プレイヤーターン開始処理を実行");
                    
                    // 行動回数が0または手札が空の場合は初期化
                    if (remainingActions <= 0 || currentHandState == HandState.Empty || currentHandState == HandState.TurnEnded)
                    {
                        LogDebug("行動回数および手札を初期化");
                        InitializeActionsForTurn();
                        
                        // ターン開始時に武器カードの列をランダム再生成
                        var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
                        if (attachmentSystem != null)
                        {
                            attachmentSystem.RegenerateWeaponCardsForNewTurn();
                        }
                        
                        GenerateHand();
                    }
                    break;
                    
                case GameState.EnemyTurn:
                    // 敵ターン開始時は手札を保持
                    LogDebug("敵ターン開始、手札状態をTurnEndedに変更");
                    ChangeHandState(HandState.TurnEnded);
                    break;
                    
                case GameState.Victory:
                case GameState.Defeat:
                    // 戦闘終了時の処理
                    LogDebug("戦闘終了、手札をクリア");
                    ClearHand();
                    break;
            }
        }

        /// <summary>
        /// プレイヤーデータ変更時の処理
        /// </summary>
        private void HandlePlayerDataChanged(PlayerData playerData)
        {
            // 装備変更時は手札を再生成（装備が変わった場合のみ）
            if (ShouldRegenerateHand(playerData))
            {
                GenerateHand();
            }
        }
        
        /// <summary>
        /// AttachmentSystemから武器カードが生成された時の処理
        /// </summary>
        private void HandleWeaponCardsGenerated(List<CardData> weaponCards)
        {
            LogDebug($"Weapon cards generated: {weaponCards.Count} cards");
            
            // 武器カードから直接手札を更新
            try
            {
                GenerateHandFromWeaponCards(weaponCards);
                ChangeHandState(HandState.Generated);
                
                // 手札更新イベントを発火
                OnHandGenerated?.Invoke(currentHand);
                
                LogDebug($"Hand updated from weapon cards: {RemainingCards} cards");
                if (debugMode) LogHandContents();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating hand from weapon cards: {ex.Message}");
                ClearHand();
            }
        }

        #endregion

        #region Hand Generation

        /// <summary>
        /// 手札の生成（AttachmentSystemから武器カードを取得）
        /// </summary>
        public void GenerateHand()
        {
            // AttachmentSystemから武器カードを取得
            var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
            if (attachmentSystem == null)
            {
                LogDebug("Cannot generate hand: AttachmentSystem not found");
                return;
            }

            var weaponCards = attachmentSystem.GetWeaponCards();
            if (weaponCards == null || weaponCards.Count == 0)
            {
                LogDebug("Cannot generate hand: No weapon cards available");
                return;
            }

            try
            {
                // 武器カードから直接手札を生成
                GenerateHandFromWeaponCards(weaponCards);
                
                // 手札状態更新
                ChangeHandState(HandState.Generated);
                
                // 履歴記録
                RecordHandToHistory();
                
                // イベント発火
                OnHandGenerated?.Invoke(currentHand);
                
                LogDebug($"Hand generated from weapon cards: {RemainingCards} cards");
                if (debugMode) LogHandContents();
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error generating hand: {ex.Message}");
                ClearHand();
            }
        }
        
        /// <summary>
        /// 武器カードから手札を生成
        /// </summary>
        private void GenerateHandFromWeaponCards(List<CardData> weaponCards)
        {
            // 手札配列を初期化
            currentHand = new CardData[handSize];
            
            // 武器カードを手札に配置
            for (int i = 0; i < handSize && i < weaponCards.Count; i++)
            {
                currentHand[i] = weaponCards[i];
            }
            
            // 武器カードが手札枚数より少ない場合は繰り返し配置
            if (weaponCards.Count < handSize && weaponCards.Count > 0)
            {
                for (int i = weaponCards.Count; i < handSize; i++)
                {
                    int sourceIndex = i % weaponCards.Count;
                    currentHand[i] = weaponCards[sourceIndex];
                }
            }
            
            LogDebug($"Generated hand with {weaponCards.Count} weapon cards, filled {handSize} slots");
        }

        /// <summary>
        /// カードプール生成
        /// </summary>
        private void GenerateCardPool()
        {
            var equippedWeapons = battleManager.PlayerData.equippedWeapons
                .Where(weapon => weapon != null)
                .ToArray();

            int columnCount = GetColumnCount();
            fullCardPool = CardDataUtility.GenerateCardPool(equippedWeapons, columnCount);
            
            // 無効ターゲットを除外する場合
            if (excludeInvalidTargets)
            {
                fullCardPool = FilterValidCards(fullCardPool);
            }
            
            LogDebug($"Card pool generated: {fullCardPool.Length} cards");
        }

        /// <summary>
        /// 戦場の列数を取得
        /// </summary>
        private int GetColumnCount()
        {
            if (battleField != null)
            {
                // BattleFieldクラスにColumnsプロパティがない場合の代替手段
                try
                {
                    // リフレクションまたは安全な方法で列数を取得
                    return 3; // デフォルト値として3列を使用
                }
                catch
                {
                    return 3; // フォールバック
                }
            }
            return 3; // デフォルト値
        }

        /// <summary>
        /// 手札をプールから抽出
        /// </summary>
        private void ExtractHandFromPool()
        {
            if (fullCardPool == null || fullCardPool.Length == 0)
            {
                // カードプールが空の場合は空の手札を生成
                currentHand = new CardData[handSize];
                return;
            }

            var drawnCards = CardDataUtility.DrawRandomCards(fullCardPool, handSize, allowDuplicateCards);
            
            // 手札配列を初期化
            currentHand = new CardData[handSize];
            
            // 抽出されたカードを手札に配置
            for (int i = 0; i < handSize && i < drawnCards.Length; i++)
            {
                currentHand[i] = drawnCards[i];
            }
        }

        /// <summary>
        /// 有効なカードをフィルタリング
        /// </summary>
        private CardData[] FilterValidCards(CardData[] cards)
        {
            var validCards = new List<CardData>();
            
            foreach (var card in cards)
            {
                if (IsCardTargetValid(card))
                {
                    validCards.Add(card);
                }
            }
            
            return validCards.ToArray();
        }

        #endregion

        #region Card Usage

        /// <summary>
        /// カードの使用
        /// </summary>
        /// <param name="handIndex">手札のインデックス（0-4）</param>
        /// <returns>使用結果</returns>
        public CardPlayResult PlayCard(int handIndex)
        {
            Debug.Log($"=== HandSystem.PlayCard START: handIndex {handIndex} ===");
            var result = new CardPlayResult();
            
            Debug.Log($"Current remainingActions: {remainingActions}");
            Debug.Log($"Current maxActionsPerTurn: {maxActionsPerTurn}");
            Debug.Log($"Current currentHandState: {currentHandState}");
            Debug.Log($"BattleManager currentState: {battleManager?.CurrentState}");
            
            try
            {
                // 基本妥当性チェック
                Debug.Log($"Starting ValidateCardPlay for handIndex {handIndex}");
                if (!ValidateCardPlay(handIndex, out string errorMessage))
                {
                    Debug.LogWarning($"❌ ValidateCardPlay failed: {errorMessage}");
                    result.isSuccess = false;
                    result.message = errorMessage;
                    return result;
                }
                Debug.Log($"✅ ValidateCardPlay passed for handIndex {handIndex}");

                CardData card = currentHand[handIndex];
                Debug.Log($"Card to play: {card?.displayName ?? "NULL"}");
                
                // 攻撃実行
                Debug.Log($"Starting ExecuteCardAttack for {card?.displayName}");
                bool attackSuccess = ExecuteCardAttack(card, out int damageDealt);
                Debug.Log($"ExecuteCardAttack result: {attackSuccess}, damage: {damageDealt}");
                
                if (attackSuccess)
                {
                    Debug.Log($"✅ Attack successful, processing successful card play");
                    
                    // 【修正】実際のダメージを適用
                    if (HasPendingDamage)
                    {
                        Debug.Log($"Applying pending damage...");
                        bool damageApplied = ApplyPendingDamage();
                        Debug.Log($"Damage applied: {damageApplied}");
                    }
                    
                    // 成功時の処理
                    result = HandleSuccessfulCardPlay(card, handIndex, damageDealt);
                    Debug.Log($"HandleSuccessfulCardPlay result: {result.isSuccess}, turnEnded: {result.turnEnded}");
                }
                else
                {
                    Debug.LogWarning($"❌ Attack failed for {card?.displayName}");
                    // 失敗時の処理
                    result.isSuccess = false;
                    result.message = "攻撃実行に失敗しました";
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Error playing card: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
                result.isSuccess = false;
                result.message = "カード使用中にエラーが発生しました";
            }
            
            Debug.Log($"Final result: isSuccess={result.isSuccess}, message={result.message}");
            
            // 結果イベント発火
            OnCardPlayResult?.Invoke(result);
            Debug.Log($"=== HandSystem.PlayCard END: handIndex {handIndex} ===");
            return result;
        }

        /// <summary>
        /// カード使用の妥当性チェック
        /// </summary>
        private bool ValidateCardPlay(int handIndex, out string errorMessage)
        {
            Debug.Log($"=== ValidateCardPlay START: handIndex {handIndex} ===");
            errorMessage = "";
            
            // ゲーム状態チェック（Victory状態でもテスト用に許可）
            Debug.Log($"Checking battleManager.CurrentState: {battleManager?.CurrentState}");
            if (battleManager.CurrentState != GameState.PlayerTurn && battleManager.CurrentState != GameState.Victory)
            {
                errorMessage = "プレイヤーのターンまたは勝利状態ではありません";
                Debug.LogWarning($"❌ Wrong game state: {battleManager.CurrentState}");
                return false;
            }
            Debug.Log($"✅ Game state check passed: {battleManager.CurrentState} (PlayerTurn or Victory allowed)");
            
            // 手札インデックスチェック
            Debug.Log($"Checking handIndex {handIndex} against handSize {handSize}");
            if (handIndex < 0 || handIndex >= handSize)
            {
                errorMessage = "無効な手札インデックスです";
                return false;
            }
            
            // カード存在チェック
            Debug.Log($"Checking if currentHand[{handIndex}] exists: {currentHand[handIndex] != null}");
            if (currentHand[handIndex] == null)
            {
                errorMessage = "選択された位置にカードがありません";
                Debug.LogWarning($"❌ No card at index {handIndex}");
                return false;
            }
            Debug.Log($"✅ Card exists at index {handIndex}: {currentHand[handIndex].displayName}");
            
            // 手札状態チェック
            Debug.Log($"Checking currentHandState: {currentHandState} (should be Generated)");
            if (currentHandState != HandState.Generated)
            {
                errorMessage = "手札が使用可能な状態ではありません";
                Debug.LogWarning($"❌ Wrong hand state: {currentHandState}");
                return false;
            }
            Debug.Log($"✅ Hand state check passed: {currentHandState}");
            
            CardData card = currentHand[handIndex];
            
            // ターゲット妥当性チェック
            Debug.Log($"Checking IsCardTargetValid for {card.displayName}");
            if (!IsCardTargetValid(card))
            {
                errorMessage = "カードのターゲットが無効です";
                Debug.LogWarning($"❌ Invalid target for card {card.displayName}");
                return false;
            }
            Debug.Log($"✅ Card target valid for {card.displayName}");
            
            // クールダウンチェック
            Debug.Log($"Checking IsWeaponUsable for {card.displayName}");
            if (!IsWeaponUsable(card))
            {
                errorMessage = "武器がクールダウン中です";
                Debug.LogWarning($"❌ Weapon on cooldown: {card.displayName}");
                return false;
            }
            Debug.Log($"✅ Weapon usable: {card.displayName}");
            
            // 行動回数チェック
            Debug.Log($"Checking remainingActions: {remainingActions} (should be > 0)");
            if (remainingActions <= 0)
            {
                errorMessage = "行動回数が残っていません";
                Debug.LogWarning($"❌ No actions remaining: {remainingActions}");
                return false;
            }
            Debug.Log($"✅ Actions remaining: {remainingActions}");
            
            Debug.Log($"=== ValidateCardPlay END: ALL CHECKS PASSED ===");
            return true;
        }

        /// <summary>
        /// カード攻撃の実行（予告ダメージシステム対応）
        /// </summary>
        private bool ExecuteCardAttack(CardData card, out int damageDealt)
        {
            damageDealt = 0;
            
            try
            {
                // 予告ダメージを計算（実際には適用しない）
                bool success = CalculatePendingDamage(card, out damageDealt);
                
                LogDebug($"Card damage calculated: {card.displayName}, Damage: {damageDealt}, Success: {success}");
                return success;
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error calculating card damage: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 予告ダメージの計算（カード選択時プレビュー用）
        /// 【修正】統合ダメージ計算を使用してコンボ効果込みの正確なプレビューを提供
        /// </summary>
        /// <param name="card">計算対象のカード</param>
        /// <returns>予告ダメージ情報（nullの場合は計算失敗）</returns>
        public PendingDamageInfo CalculatePreviewDamage(CardData card)
        {
            if (card?.weaponData == null) return null;
            
            LogDebug($"=== CalculatePreviewDamage START: {card.displayName} ===");
            
            // 【修正】統合ダメージ計算を使用（シミュレーションモード）
            var damageInfo = CalculateCompleteDamage(card, simulateCombo: true);
            
            // 予告ダメージ情報を作成（最終ダメージを使用）
            var pendingDamage = new PendingDamageInfo(card, damageInfo.finalDamage, 
                damageInfo.GetDetailedDescription(card.displayName));
            
            // 攻撃範囲に応じたターゲットを計算（ダメージ値は最終ダメージを使用）
            bool hasTargets = false;
            WeaponData weapon = card.weaponData;
            
            switch (weapon.attackRange)
            {
                case AttackRange.All:
                    hasTargets = CalculateAllTargets(pendingDamage, damageInfo.finalDamage);
                    break;
                    
                case AttackRange.Column:
                    hasTargets = CalculateColumnTargets(pendingDamage, card.targetColumn, damageInfo.finalDamage);
                    break;
                    
                case AttackRange.Row1:
                case AttackRange.Row2:
                    hasTargets = CalculateRowTargets(pendingDamage, weapon, damageInfo.finalDamage);
                    break;
                    
                default:
                    hasTargets = CalculateSingleTargets(pendingDamage, card, damageInfo.finalDamage);
                    break;
            }
            
            if (hasTargets)
            {
                LogDebug($"✅ 予告ダメージプレビュー計算完了: {pendingDamage.description}");
                LogDebug($"  - 基本ダメージ: {damageInfo.baseDamage}");
                LogDebug($"  - コンボ効果: {damageInfo.comboDamage} (x{damageInfo.comboMultiplier:F1})");
                LogDebug($"  - 最終ダメージ: {damageInfo.finalDamage}");
                return pendingDamage;
            }
            
            LogDebug($"❌ 有効なターゲットが見つかりませんでした: {card.displayName}");
            return null;
        }
        
        /// <summary>
        /// 予告ダメージの計算（実際の適用は次のターンで実行）
        /// 【修正】統合ダメージ計算を使用してコンボ効果を実際に適用
        /// </summary>
        private bool CalculatePendingDamage(CardData card, out int damageDealt)
        {
            damageDealt = 0;
            
            LogDebug($"=== CalculatePendingDamage START: {card.displayName} ===");
            
            // 【修正】統合ダメージ計算を使用（実際のコンボ処理実行）
            var damageInfo = CalculateCompleteDamage(card, simulateCombo: false);
            
            WeaponData weapon = card.weaponData;
            
            // 予告ダメージ情報を作成（最終ダメージを使用）
            var pendingDamage = new PendingDamageInfo(card, damageInfo.finalDamage, 
                damageInfo.GetDetailedDescription(card.displayName));
            
            // 攻撃範囲に応じたターゲットを計算（ダメージ値は最終ダメージを使用）
            bool hasTargets = false;
            
            switch (weapon.attackRange)
            {
                case AttackRange.All:
                    hasTargets = CalculateAllTargets(pendingDamage, damageInfo.finalDamage);
                    break;
                    
                case AttackRange.Column:
                    hasTargets = CalculateColumnTargets(pendingDamage, card.targetColumn, damageInfo.finalDamage);
                    break;
                    
                case AttackRange.Row1:
                case AttackRange.Row2:
                    hasTargets = CalculateRowTargets(pendingDamage, weapon, damageInfo.finalDamage);
                    break;
                    
                default:
                    hasTargets = CalculateSingleTargets(pendingDamage, card, damageInfo.finalDamage);
                    break;
            }
            
            if (hasTargets)
            {
                // 予告ダメージを保存
                currentPendingDamage = pendingDamage;
                damageDealt = damageInfo.finalDamage;
                
                // イベント発火
                OnPendingDamageCalculated?.Invoke(pendingDamage);
                
                LogDebug($"✅ 予告ダメージ計算完了: {pendingDamage.description}");
                LogDebug($"  - 基本ダメージ: {damageInfo.baseDamage}");
                LogDebug($"  - コンボ効果: {damageInfo.comboDamage} (x{damageInfo.comboMultiplier:F1})");
                LogDebug($"  - 最終ダメージ: {damageInfo.finalDamage}");
                return true;
            }
            
            LogDebug($"❌ 有効なターゲットが見つかりませんでした: {card.displayName}");
            return false;
        }
        
        /// <summary>
        /// 既存システムを使用した攻撃実行（非推奨：予告ダメージシステム使用）
        /// </summary>
        private bool ExecuteAttackWithExistingSystem(CardData card, out int damageDealt)
        {
            damageDealt = 0;
            
            // 攻撃範囲に応じた処理
            bool success = false;
            WeaponData weapon = card.weaponData;
            
            switch (weapon.attackRange)
            {
                case AttackRange.All:
                    success = ExecuteAllAttack(weapon, out damageDealt);
                    break;
                    
                case AttackRange.Column:
                    success = ExecuteColumnAttack(weapon, card.targetColumn, out damageDealt);
                    break;
                    
                case AttackRange.Row1:
                case AttackRange.Row2:
                    success = ExecuteRowAttack(weapon, out damageDealt);
                    break;
                    
                default:
                    success = ExecuteSingleTargetAttack(weapon, card, out damageDealt);
                    break;
            }
            
            return success;
        }

        #endregion

        #region Attack Execution Methods

        /// <summary>
        /// 全体攻撃の実行
        /// </summary>
        private bool ExecuteAllAttack(WeaponData weapon, out int damageDealt)
        {
            damageDealt = 0;
            int baseDamage = GetBaseDamage(weapon);
            
            var allEnemies = GetAllEnemies();
            foreach (var enemy in allEnemies)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    enemy.TakeDamage(baseDamage);
                    damageDealt += baseDamage;
                    
                    if (!enemy.IsAlive())
                    {
                        RemoveEnemy(enemy);
                    }
                }
            }
            
            // 敵データ変更を通知（UI更新のため）
            if (allEnemies.Count > 0)
            {
                OnEnemyDataChanged?.Invoke();
            }
            
            return allEnemies.Count > 0;
        }

        /// <summary>
        /// 縦列攻撃の実行
        /// </summary>
        private bool ExecuteColumnAttack(WeaponData weapon, int columnIndex, out int damageDealt)
        {
            damageDealt = 0;
            int baseDamage = GetBaseDamage(weapon);
            bool anyHit = false;
            
            // 列の敵を攻撃
            var enemiesInColumn = GetEnemiesInColumn(columnIndex);
            foreach (var enemy in enemiesInColumn)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    enemy.TakeDamage(baseDamage);
                    damageDealt += baseDamage;
                    anyHit = true;
                    
                    if (!enemy.IsAlive())
                    {
                        RemoveEnemy(enemy);
                    }
                }
            }
            
            // ゲートも攻撃（簡易実装）
            if (CanAttackGate(columnIndex))
            {
                damageDealt += baseDamage;
                anyHit = true;
                // TODO: ゲートダメージ処理の実装
            }
            
            // 敵データ変更を通知（UI更新のため）
            if (anyHit)
            {
                OnEnemyDataChanged?.Invoke();
            }
            
            return anyHit;
        }

        /// <summary>
        /// 行攻撃の実行
        /// </summary>
        private bool ExecuteRowAttack(WeaponData weapon, out int damageDealt)
        {
            damageDealt = 0;
            int baseDamage = GetBaseDamage(weapon);
            
            int targetRow = (weapon.attackRange == AttackRange.Row1) ? 0 : 1;
            var enemiesInRow = GetEnemiesInRow(targetRow);
            
            foreach (var enemy in enemiesInRow)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    enemy.TakeDamage(baseDamage);
                    damageDealt += baseDamage;
                    
                    if (!enemy.IsAlive())
                    {
                        RemoveEnemy(enemy);
                    }
                }
            }
            
            // 敵データ変更を通知（UI更新のため）
            if (enemiesInRow.Count > 0)
            {
                OnEnemyDataChanged?.Invoke();
            }
            
            return enemiesInRow.Count > 0;
        }

        /// <summary>
        /// 単体攻撃の実行
        /// </summary>
        private bool ExecuteSingleTargetAttack(WeaponData weapon, CardData card, out int damageDealt)
        {
            damageDealt = 0;
            int baseDamage = GetBaseDamage(weapon);
            
            // 対象列の一番前の敵を攻撃
            var frontEnemy = GetFrontEnemyInColumn(card.targetColumn);
            if (frontEnemy != null && frontEnemy.IsAlive())
            {
                frontEnemy.TakeDamage(baseDamage);
                damageDealt = baseDamage;
                
                if (!frontEnemy.IsAlive())
                {
                    RemoveEnemy(frontEnemy);
                }
                
                // 敵データ変更を通知（UI更新のため）
                OnEnemyDataChanged?.Invoke();
                return true;
            }
            
            // 敵がいない場合はゲートを攻撃
            if (CanAttackGate(card.targetColumn))
            {
                damageDealt = baseDamage;
                // TODO: ゲートダメージ処理の実装
                
                // 戦場データ変更を通知（UI更新のため）
                OnBattleFieldChanged?.Invoke();
                return true;
            }
            
            return false;
        }

        #endregion

        #region Pending Damage Calculation Methods
        
        /// <summary>
        /// 全体攻撃のターゲットを計算
        /// </summary>
        private bool CalculateAllTargets(PendingDamageInfo pendingDamage, int baseDamage)
        {
            var allEnemies = GetAllEnemies();
            bool hasTargets = false;
            
            foreach (var enemy in allEnemies)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    pendingDamage.targetEnemies.Add(enemy);
                    hasTargets = true;
                }
            }
            
            return hasTargets;
        }
        
        /// <summary>
        /// 縦列攻撃のターゲットを計算
        /// </summary>
        private bool CalculateColumnTargets(PendingDamageInfo pendingDamage, int columnIndex, int baseDamage)
        {
            bool hasTargets = false;
            
            // 列の敵を攻撃
            var enemiesInColumn = GetEnemiesInColumn(columnIndex);
            foreach (var enemy in enemiesInColumn)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    pendingDamage.targetEnemies.Add(enemy);
                    hasTargets = true;
                }
            }
            
            // ゲートも攻撃（簡易実装）
            if (CanAttackGate(columnIndex))
            {
                // TODO: ゲートダメージ処理の実装
                hasTargets = true;
            }
            
            return hasTargets;
        }
        
        /// <summary>
        /// 行攻撃のターゲットを計算
        /// </summary>
        private bool CalculateRowTargets(PendingDamageInfo pendingDamage, WeaponData weapon, int baseDamage)
        {
            int targetRow = (weapon.attackRange == AttackRange.Row1) ? 0 : 1;
            var enemiesInRow = GetEnemiesInRow(targetRow);
            bool hasTargets = false;
            
            foreach (var enemy in enemiesInRow)
            {
                if (enemy != null && enemy.IsAlive())
                {
                    pendingDamage.targetEnemies.Add(enemy);
                    hasTargets = true;
                }
            }
            
            return hasTargets;
        }
        
        /// <summary>
        /// 単体攻撃のターゲットを計算
        /// </summary>
        private bool CalculateSingleTargets(PendingDamageInfo pendingDamage, CardData card, int baseDamage)
        {
            // 対象列の一番前の敵を攻撃
            var frontEnemy = GetFrontEnemyInColumn(card.targetColumn);
            if (frontEnemy != null && frontEnemy.IsAlive())
            {
                pendingDamage.targetEnemies.Add(frontEnemy);
                return true;
            }
            
            // 敵がいない場合はゲートを攻撃
            if (CanAttackGate(card.targetColumn))
            {
                // TODO: ゲートダメージ処理の実装
                return true;
            }
            
            return false;
        }
        
        #endregion

        #region Battle System Helper Methods

        /// <summary>
        /// 基本ダメージの計算
        /// </summary>
        private int GetBaseDamage(WeaponData weapon)
        {
            return battleManager.PlayerData.baseAttackPower + weapon.basePower;
        }

        /// <summary>
        /// 統合ダメージ計算（コンボ効果、バフ/デバフ、装備効果すべて含む）
        /// 1回クリック時と2回クリック時で同じ結果を保証
        /// </summary>
        /// <param name="card">使用カード</param>
        /// <param name="simulateCombo">コンボ効果をシミュレートするか（1回クリック時=true, 2回クリック時=false）</param>
        /// <returns>最終ダメージ計算結果</returns>
        private DamageCalculationInfo CalculateCompleteDamage(CardData card, bool simulateCombo = true)
        {
            var result = new DamageCalculationInfo();
            
            if (card?.weaponData == null)
            {
                LogDebug("CalculateCompleteDamage: カードまたは武器データがnull");
                return result;
            }

            WeaponData weapon = card.weaponData;
            
            // 1. 基本ダメージ計算
            result.baseDamage = GetBaseDamage(weapon);
            LogDebug($"基本ダメージ: {result.baseDamage}");

            // 2. コンボ効果計算
            result.comboMultiplier = CalculateComboEffect(card, simulateCombo);
            result.comboDamage = Mathf.RoundToInt(result.baseDamage * result.comboMultiplier) - result.baseDamage;
            LogDebug($"コンボ倍率: {result.comboMultiplier}, コンボダメージ: {result.comboDamage}");

            // 3. その他の効果計算（装備効果、バフ/デバフなど）
            result.otherMultiplier = CalculateOtherEffects(card);
            result.otherDamage = Mathf.RoundToInt(result.baseDamage * result.otherMultiplier) - result.baseDamage;
            LogDebug($"その他効果倍率: {result.otherMultiplier}, その他ダメージ: {result.otherDamage}");

            // 4. 最終ダメージ計算
            float totalMultiplier = result.comboMultiplier * result.otherMultiplier;
            result.finalDamage = Mathf.RoundToInt(result.baseDamage * totalMultiplier);
            
            LogDebug($"統合ダメージ計算完了 - 基本:{result.baseDamage}, コンボ:{result.comboDamage}, その他:{result.otherDamage}, 最終:{result.finalDamage}");
            
            return result;
        }

        /// <summary>
        /// コンボ効果の計算
        /// </summary>
        /// <param name="card">使用カード</param>
        /// <param name="simulate">シミュレーションモードか</param>
        /// <returns>コンボ倍率</returns>
        private float CalculateComboEffect(CardData card, bool simulate)
        {
            if (comboSystem == null || card?.weaponData == null)
            {
                LogDebug("ComboSystem または weaponData が null");
                return 1.0f;
            }

            try
            {
                // 武器インデックスを取得
                int weaponIndex = FindWeaponIndex(card.weaponData);
                if (weaponIndex == -1)
                {
                    LogDebug("武器がプレイヤー装備に見つかりません");
                    return 1.0f;
                }

                // シミュレーションモードの場合、実際にコンボ処理は実行せずダメージ計算のみ
                if (simulate)
                {
                    // 現在のコンボ状況を基に予想倍率を計算
                    return CalculateComboPreviewMultiplier(weaponIndex, card);
                }
                else
                {
                    // 実際のコンボ処理を実行（2回クリック時）
                    GridPosition targetPosition = new GridPosition(card.targetColumn, 0);
                    var comboResult = comboSystem.ProcessWeaponUse(weaponIndex, targetPosition);
                    
                    LogDebug($"コンボ処理結果 - 実行済み:{comboResult.wasExecuted}, ダメージ倍率:{comboResult.totalDamageMultiplier}");
                    return comboResult.totalDamageMultiplier;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"コンボ効果計算エラー: {ex.Message}");
                return 1.0f;
            }
        }

        /// <summary>
        /// コンボプレビュー倍率の計算（1回クリック時用）
        /// </summary>
        private float CalculateComboPreviewMultiplier(int weaponIndex, CardData card)
        {
            try
            {
                // 現在のアクティブコンボとコンボ進行状況を確認
                var activeProgresses = comboSystem.ActiveCombos;
                
                float bestMultiplier = 1.0f;
                
                if (activeProgresses != null)
                {
                    foreach (var progress in activeProgresses)
                    {
                        if (progress.comboData != null && progress.comboData.effects != null)
                        {
                            // このコンボが完成した場合のダメージ倍率を計算
                            float multiplier = 1.0f;
                            foreach (var effect in progress.comboData.effects)
                            {
                                if (effect.effectType == ComboEffectType.DamageMultiplier)
                                {
                                    multiplier *= effect.damageMultiplier;
                                }
                            }
                            
                            if (multiplier > bestMultiplier)
                            {
                                bestMultiplier = multiplier;
                            }
                        }
                    }
                }
                
                LogDebug($"コンボプレビュー倍率: {bestMultiplier} (アクティブコンボ数: {activeProgresses?.Count ?? 0})");
                return bestMultiplier;
            }
            catch (Exception ex)
            {
                Debug.LogError($"コンボプレビュー倍率計算エラー: {ex.Message}");
                return 1.0f;
            }
        }

        /// <summary>
        /// その他の効果計算（装備効果、バフ/デバフなど）
        /// </summary>
        private float CalculateOtherEffects(CardData card)
        {
            // 基本倍率
            float multiplier = 1.0f;
            
            // TODO: 装備効果の計算
            // TODO: バフ/デバフ効果の計算
            // TODO: 特殊状態効果の計算
            
            return multiplier;
        }

        /// <summary>
        /// 全敵の取得（BattleField互換）
        /// </summary>
        private List<EnemyInstance> GetAllEnemies()
        {
            if (battleField != null)
            {
                try
                {
                    return battleField.GetAllEnemies();
                }
                catch
                {
                    // フォールバック
                    return new List<EnemyInstance>();
                }
            }
            return new List<EnemyInstance>();
        }

        /// <summary>
        /// 列の敵取得（BattleField互換）
        /// </summary>
        private List<EnemyInstance> GetEnemiesInColumn(int columnIndex)
        {
            if (battleField != null)
            {
                try
                {
                    return battleField.GetEnemiesInColumn(columnIndex);
                }
                catch
                {
                    return new List<EnemyInstance>();
                }
            }
            return new List<EnemyInstance>();
        }

        /// <summary>
        /// 行の敵取得（BattleField互換）
        /// </summary>
        private List<EnemyInstance> GetEnemiesInRow(int rowIndex)
        {
            if (battleField != null)
            {
                try
                {
                    return battleField.GetEnemiesInRow(rowIndex);
                }
                catch
                {
                    return new List<EnemyInstance>();
                }
            }
            return new List<EnemyInstance>();
        }

        /// <summary>
        /// 列の先頭敵取得（BattleField互換）
        /// </summary>
        private EnemyInstance GetFrontEnemyInColumn(int columnIndex)
        {
            if (battleField != null)
            {
                try
                {
                    return battleField.GetFrontEnemyInColumn(columnIndex);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// ゲート攻撃可能性チェック（BattleField互換）
        /// </summary>
        private bool CanAttackGate(int columnIndex)
        {
            if (battleField != null)
            {
                try
                {
                    return battleField.CanAttackGate(columnIndex);
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 敵削除（BattleField互換）
        /// </summary>
        private void RemoveEnemy(EnemyInstance enemy)
        {
            if (battleField != null && enemy != null)
            {
                try
                {
                    battleField.RemoveEnemy(new GridPosition(enemy.gridX, enemy.gridY));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to remove enemy: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 予告ダメージを実際に適用（次のターンボタン押下時に呼び出される）
        /// </summary>
        public bool ApplyPendingDamage()
        {
            if (currentPendingDamage == null)
            {
                LogDebug("予告ダメージがありません");
                return false;
            }
            
            var pendingDamage = currentPendingDamage;
            int totalDamageApplied = 0;
            bool anyTargetHit = false;
            
            try
            {
                // 敵にダメージを適用
                foreach (var enemy in pendingDamage.targetEnemies)
                {
                    if (enemy != null && enemy.IsAlive())
                    {
                        enemy.TakeDamage(pendingDamage.calculatedDamage);
                        totalDamageApplied += pendingDamage.calculatedDamage;
                        anyTargetHit = true;
                        
                        if (!enemy.IsAlive())
                        {
                            RemoveEnemy(enemy);
                        }
                    }
                }
                
                // ゲートにダメージを適用（必要に応じて実装）
                foreach (var gate in pendingDamage.targetGates)
                {
                    if (gate != null && !gate.IsDestroyed())
                    {
                        gate.TakeDamage(pendingDamage.calculatedDamage);
                        totalDamageApplied += pendingDamage.calculatedDamage;
                        anyTargetHit = true;
                    }
                }
                
                // 統計更新
                totalDamageDealt += totalDamageApplied;
                
                // 履歴に追加
                pendingDamageHistory.Add(pendingDamage);
                
                // ComboSystemに武器使用をチェックさせる
                ProcessComboForWeaponUse(pendingDamage);
                
                // イベント発火
                OnPendingDamageApplied?.Invoke(pendingDamage);
                
                // 【修正】敵データ変更をUIに通知
                if (anyTargetHit)
                {
                    OnEnemyDataChanged?.Invoke();
                    LogDebug("敵データ変更イベントを発火（UI更新のため）");
                }
                
                LogDebug($"予告ダメージ適用完了: {pendingDamage.description}, 総ダメージ: {totalDamageApplied}");
                
                // 予告ダメージをクリア
                ClearPendingDamage();
                
                return anyTargetHit;
            }
            catch (Exception ex)
            {
                Debug.LogError($"予告ダメージ適用エラー: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 予告ダメージをクリア
        /// </summary>
        public void ClearPendingDamage()
        {
            if (currentPendingDamage != null)
            {
                currentPendingDamage = null;
                OnPendingDamageCleared?.Invoke();
                LogDebug("予告ダメージをクリアしました");
            }
        }
        
        /// <summary>
        /// 武器使用時のComboSystem連携処理
        /// </summary>
        private void ProcessComboForWeaponUse(PendingDamageInfo damageInfo)
        {
            if (comboSystem == null || damageInfo?.usedCard?.weaponData == null)
            {
                LogDebug("ComboSystem not available or invalid damage info for combo processing");
                return;
            }
            
            try
            {
                // 武器のインデックスを取得
                int weaponIndex = FindWeaponIndex(damageInfo.usedCard.weaponData);
                if (weaponIndex == -1)
                {
                    LogDebug("Weapon not found in player equipment for combo processing");
                    return;
                }
                
                // ターゲット位置を決定（最初のターゲットの位置を使用）
                GridPosition targetPosition = new GridPosition(0, 0); // デフォルト
                if (damageInfo.targetEnemies.Count > 0)
                {
                    var firstEnemy = damageInfo.targetEnemies[0];
                    targetPosition = new GridPosition(firstEnemy.gridX, firstEnemy.gridY);
                }
                
                // ComboSystemに武器使用を通知してコンボチェック実行
                var comboResult = comboSystem.ProcessWeaponUse(weaponIndex, targetPosition);
                
                if (comboResult.wasExecuted)
                {
                    LogDebug($"Combo executed: {comboResult.executedCombo.comboName}");
                    
                    // コンボ効果があれば追加行動などを処理
                    if (comboResult.additionalActionsGranted > 0)
                    {
                        AddActionBonus(comboResult.additionalActionsGranted);
                        LogDebug($"Added {comboResult.additionalActionsGranted} bonus actions from combo");
                    }
                }
                else
                {
                    LogDebug("No combo was triggered from weapon use");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing combo for weapon use: {ex.Message}");
            }
        }
        
        #endregion

        #region Utility Methods

        /// <summary>
        /// カード使用成功時の処理
        /// </summary>
        private CardPlayResult HandleSuccessfulCardPlay(CardData card, int handIndex, int damageDealt)
        {
            // 使用済みカードとして記録
            usedCards.Add(card);
            
            // 手札からカードを削除
            currentHand[handIndex] = null;
            
            // 統計更新
            totalCardsPlayed++;
            totalDamageDealt += damageDealt;
            UpdateWeaponUsageCount(card.weaponData.weaponName);
            
            // 武器クールダウン設定
            SetWeaponCooldown(card.weaponData);
            
            // 手札状態更新
            ChangeHandState(HandState.CardUsed);
            
            // イベント発火
            OnCardPlayed?.Invoke(card);
            
            // 行動回数消費
            bool turnEnded = ConsumeAction();
            
            // 結果作成
            var result = new CardPlayResult
            {
                isSuccess = true,
                message = $"{card.displayName}を使用しました",
                playedCard = card,
                damageDealt = damageDealt,
                turnEnded = turnEnded
            };
            
            LogDebug($"Card played successfully: {card.displayName}, Damage: {damageDealt}, Actions remaining: {remainingActions}");
            return result;
        }

        /// <summary>
        /// 装備武器インデックスの検索
        /// </summary>
        private int FindWeaponIndex(WeaponData weaponData)
        {
            var equippedWeapons = battleManager.PlayerData.equippedWeapons;
            for (int i = 0; i < equippedWeapons.Length; i++)
            {
                if (equippedWeapons[i] != null && 
                    equippedWeapons[i].weaponName == weaponData.weaponName)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// カードターゲットの妥当性チェック
        /// </summary>
        private bool IsCardTargetValid(CardData card)
        {
            if (card?.weaponData == null) return false;
            
            return card.IsValidTarget(GetColumnCount());
        }

        /// <summary>
        /// 武器使用可能性チェック
        /// </summary>
        private bool IsWeaponUsable(CardData card)
        {
            // AttachmentSystemから取得した武器カードの場合、武器自体のクールダウンのみをチェック
            if (card?.weaponData != null)
            {
                // 武器のクールダウンターン数が0の場合は常に使用可能
                if (card.weaponData.cooldownTurns <= 0)
                {
                    return true;
                }
                
                // 連続使用可能フラグがtrueの場合は使用可能
                if (card.weaponData.canUseConsecutively)
                {
                    return true;
                }
            }
            
            // 従来のPlayerDataベースのチェック（フォールバック）
            int weaponIndex = FindWeaponIndex(card.weaponData);
            if (weaponIndex != -1 && battleManager?.PlayerData != null)
            {
                return battleManager.PlayerData.CanUseWeapon(weaponIndex);
            }
            
            // PlayerDataに登録されていない武器カードも使用可能とする
            return true;
        }

        /// <summary>
        /// 武器クールダウン設定
        /// </summary>
        private void SetWeaponCooldown(WeaponData weaponData)
        {
            int weaponIndex = FindWeaponIndex(weaponData);
            if (weaponIndex != -1)
            {
                battleManager.PlayerData.weaponCooldowns[weaponIndex] = weaponData.cooldownTurns;
            }
        }

        #endregion

        #region Action Management
        
        /// <summary>
        /// ターン開始時の行動回数初期化
        /// </summary>
        private void InitializeActionsForTurn()
        {
            // 基本行動回数 + アタッチメントボーナス
            maxActionsPerTurn = baseActionsPerTurn + actionBonus;
            remainingActions = maxActionsPerTurn;
            
            // イベント発火
            OnActionsChanged?.Invoke(remainingActions, maxActionsPerTurn);
            
            LogDebug($"ターン開始：行動回数 {remainingActions}/{maxActionsPerTurn}");
        }
        
        /// <summary>
        /// 行動回数消費と自動ターン終了チェック
        /// </summary>
        /// <returns>ターンが終了したか</returns>
        private bool ConsumeAction()
        {
            if (remainingActions > 0)
            {
                remainingActions--;
                OnActionsChanged?.Invoke(remainingActions, maxActionsPerTurn);
                
                LogDebug($"行動回数消費：残り {remainingActions}/{maxActionsPerTurn}");
                
                // 行動回数が0になった場合の処理
                if (remainingActions <= 0)
                {
                    OnActionsExhausted?.Invoke();
                    
                    if (autoEndTurnWhenActionsExhausted)
                    {
                        return CheckAutoTurnEnd();
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 自動ターン終了の実行
        /// </summary>
        /// <returns>ターン終了が実行されたか</returns>
        private bool CheckAutoTurnEnd()
        {
            if (remainingActions <= 0 && battleManager != null)
            {
                LogDebug($"自動ターン終了を開始 ({autoEndTurnDelay}秒後)");
                
                // 遅延をつけてターン終了
                StartCoroutine(AutoEndTurnCoroutine());
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 自動ターン終了のコルーチン
        /// </summary>
        private System.Collections.IEnumerator AutoEndTurnCoroutine()
        {
            // 指定された時間待機
            yield return new WaitForSeconds(autoEndTurnDelay);
            
            // ターン終了イベント発火
            OnAutoTurnEnd?.Invoke();
            
            // BattleManagerにターン終了を通知
            if (battleManager != null)
            {
                // 予告ダメージがある場合は適用
                if (HasPendingDamage)
                {
                    try
                    {
                        ApplyPendingDamage();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"予告ダメージ適用エラー: {ex.Message}");
                    }
                }
                
                // テスト用: 自動ターン終了後、すぐに行動回数をリセットして継続テスト可能にする
                LogDebug("自動ターン終了完了 - テスト用に行動回数リセット中...");
                
                // テスト用: 1秒待ってから行動回数をリセット
                yield return new WaitForSeconds(1f);
                
                try
                {
                    ResetActionsForContinuousTesting();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"行動回数リセットエラー: {ex.Message}");
                }
                
                // 敵ターンに移行はスキップして、プレイヤーターンを継続
                // battleManager.EndPlayerTurn(TurnEndReason.ActionCompleted); // コメントアウト
            }
        }
        
        /// <summary>
        /// アタッチメント等による行動回数ボーナスを追加
        /// </summary>
        /// <param name="bonus">追加する行動回数</param>
        public void AddActionBonus(int bonus)
        {
            if (bonus > 0)
            {
                actionBonus += bonus;
                
                // 現在のターン中の場合は即座反映
                if (battleManager != null && battleManager.CurrentState == GameState.PlayerTurn)
                {
                    maxActionsPerTurn += bonus;
                    remainingActions += bonus;
                    OnActionsChanged?.Invoke(remainingActions, maxActionsPerTurn);
                }
                
                LogDebug($"行動回数ボーナス追加: +{bonus} (総ボーナス: {actionBonus})");
            }
        }
        
        /// <summary>
        /// 行動回数ボーナスをリセット（アタッチメントリセット時等に使用）
        /// </summary>
        public void ResetActionBonus()
        {
            actionBonus = 0;
            LogDebug("行動回数ボーナスをリセット");
        }
        
        /// <summary>
        /// 現在の行動回数情報を取得（デバッグ用）
        /// </summary>
        /// <returns>行動回数情報</returns>
        public string GetActionInfo()
        {
            return $"行動回数: {remainingActions}/{maxActionsPerTurn} (ベース: {baseActionsPerTurn}, ボーナス: {actionBonus})";
        }
        
        /// <summary>
        /// テスト用: 継続テストのための行動回数リセット
        /// </summary>
        private void ResetActionsForContinuousTesting()
        {
            LogDebug("テスト用: 行動回数をリセット中...");
            
            // 行動回数をリセット
            remainingActions = maxActionsPerTurn;
            
            // 武器カードの列をランダム再生成
            var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
            if (attachmentSystem != null)
            {
                LogDebug("テスト用: 武器カードの列をランダム再生成中...");
                attachmentSystem.RegenerateWeaponCardsForNewTurn();
            }
            
            // 手札を完全に再生成（使用済みカードを復元）
            LogDebug("手札を完全再生成中...");
            GenerateHand();
            
            // 手札状態をGeneratedに確実に設定
            ChangeHandState(HandState.Generated);
            
            // イベント発火
            OnActionsChanged?.Invoke(remainingActions, maxActionsPerTurn);
            
            LogDebug($"✅ テスト用リセット完了: 行動回数 {remainingActions}/{maxActionsPerTurn}, 手札状態: {currentHandState}");
            LogDebug($"✅ 手札枚数: {RemainingCards}, 使用可能カード: {GetUsableCards().Length}");
            LogDebug("✅ カードクリックテストを継続できます!");
        }
        
        /// <summary>
        /// テスト用: 手動で行動回数をリセット（デバッグ用）
        /// </summary>
        [ContextMenu("Reset Actions for Testing")]
        public void ManualResetActionsForTesting()
        {
            ResetActionsForContinuousTesting();
        }
        
        #endregion

        #region Hand Management

        /// <summary>
        /// 使用可能なカードを取得
        /// </summary>
        public CardData[] GetUsableCards()
        {
            if (currentHand == null) return new CardData[0];
            
            return currentHand
                .Where(card => card != null && IsCardTargetValid(card) && IsWeaponUsable(card))
                .ToArray();
        }

        /// <summary>
        /// 手札のクリア
        /// </summary>
        public void ClearHand()
        {
            currentHand = new CardData[handSize];
            ChangeHandState(HandState.Empty);
            OnHandCleared?.Invoke();
            LogDebug("Hand cleared");
        }

        /// <summary>
        /// 手札状態の変更
        /// </summary>
        private void ChangeHandState(HandState newState)
        {
            if (currentHandState != newState)
            {
                currentHandState = newState;
                OnHandStateChanged?.Invoke(newState);
                LogDebug($"Hand state changed to: {newState}");
            }
        }

        /// <summary>
        /// 手札再生成が必要かチェック
        /// </summary>
        private bool ShouldRegenerateHand(PlayerData playerData)
        {
            // 装備変更検出ロジック（簡易実装）
            // 実際の実装では装備変更の詳細な比較が必要
            return currentHandState == HandState.Empty;
        }

        #endregion

        #region Statistics and History

        /// <summary>
        /// 手札履歴への記録
        /// </summary>
        private void RecordHandToHistory()
        {
            if (debugMode && currentHand != null)
            {
                handHistory.Add(currentHand.ToArray());
                
                // 履歴サイズ制限
                if (handHistory.Count > 10)
                {
                    handHistory.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 武器使用回数の更新
        /// </summary>
        private void UpdateWeaponUsageCount(string weaponName)
        {
            if (weaponUsageCount.ContainsKey(weaponName))
                weaponUsageCount[weaponName]++;
            else
                weaponUsageCount[weaponName] = 1;
        }

        /// <summary>
        /// 統計情報の取得
        /// </summary>
        public HandSystemStats GetStats()
        {
            return new HandSystemStats
            {
                totalCardsPlayed = totalCardsPlayed,
                totalDamageDealt = totalDamageDealt,
                weaponUsageCount = new Dictionary<string, int>(weaponUsageCount),
                currentHandState = currentHandState,
                remainingCards = RemainingCards,
                remainingActions = remainingActions,
                maxActionsPerTurn = maxActionsPerTurn,
                actionBonus = actionBonus
            };
        }

        #endregion

        #region Debug and Logging

        /// <summary>
        /// デバッグログ出力
        /// </summary>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[HandSystem] {message}");
            }
        }

        /// <summary>
        /// 手札内容のログ出力
        /// </summary>
        private void LogHandContents()
        {
            if (!debugMode || currentHand == null) return;
            
            string handInfo = "Current Hand:\n";
            for (int i = 0; i < currentHand.Length; i++)
            {
                if (currentHand[i] != null)
                {
                    handInfo += $"  [{i}] {currentHand[i].displayName}\n";
                }
                else
                {
                    handInfo += $"  [{i}] Empty\n";
                }
            }
            Debug.Log(handInfo);
        }

        /// <summary>
        /// 手札システム強制リセット（デバッグ用）
        /// </summary>
        [ContextMenu("Force Reset Hand System")]
        public void ForceReset()
        {
            ClearHand();
            usedCards.Clear();
            handHistory.Clear();
            weaponUsageCount.Clear();
            totalCardsPlayed = 0;
            totalDamageDealt = 0;
            LogDebug("Hand system force reset");
        }

        #endregion
    }

    /// <summary>
    /// 手札システム統計データ
    /// </summary>
    [Serializable]
    public struct HandSystemStats
    {
        public int totalCardsPlayed;
        public int totalDamageDealt;
        public Dictionary<string, int> weaponUsageCount;
        public HandState currentHandState;
        public int remainingCards;
        public int remainingActions;
        public int maxActionsPerTurn;
        public int actionBonus;
    }
}
