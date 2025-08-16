using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BattleSystem.Combat;
using BattleSystem.Actions;
using BattleSystem.Events;

namespace BattleSystem
{
    /// <summary>
    /// リファクタリングされた手札システム管理クラス
    /// 単一責任原則に従い、手札の管理とカード使用の制御のみを処理
    /// </summary>
    public class HandSystemRefactored : MonoBehaviour
    {
        #region 設定フィールド
        
        [Header("手札設定")]
        [SerializeField] private int handSize = 5;
        [SerializeField] private bool allowDuplicateCards = true;
        [SerializeField] private bool autoGenerateOnTurnStart = true;
        [SerializeField] private bool debugMode = true;
        
        #endregion
        
        #region 外部システム参照
        
        private BattleManager battleManager;
        private BattleField battleField;
        private WeaponSelectionSystem weaponSelectionSystem;
        private ComboSystem comboSystem;
        
        #endregion
        
        #region 分離されたシステム
        
        private IDamageCalculator damageCalculator;
        private IActionManager actionManager;
        private IHandEventManager eventManager;
        
        #endregion
        
        #region 手札データ
        
        private HandState currentHandState;
        private CardData[] currentHand;
        private List<CardData> usedCards;
        private List<CardData[]> handHistory;
        
        #endregion
        
        #region 統計データ
        
        private int totalCardsPlayed;
        private int totalDamageDealt;
        private Dictionary<string, int> weaponUsageCount;
        
        #endregion
        
        #region プロパティ
        
        public HandState CurrentHandState => currentHandState;
        public CardData[] CurrentHand => currentHand?.ToArray();
        public int HandSize => handSize;
        public int RemainingCards => currentHand?.Count(card => card != null) ?? 0;
        public bool HasUsableCards => GetUsableCards().Length > 0;
        
        // 分離されたシステムへの委譲
        public int RemainingActions => actionManager?.RemainingActions ?? 0;
        public int MaxActionsPerTurn => actionManager?.MaxActionsPerTurn ?? 0;
        public bool CanTakeAction => actionManager?.CanTakeAction ?? false;
        public bool HasActionsRemaining => actionManager?.HasActionsRemaining ?? false;
        
        #endregion
        
        #region イベントプロパティ（委譲）
        
        public event Action<CardData[]> OnHandGenerated
        {
            add => eventManager.OnHandGenerated += value;
            remove => eventManager.OnHandGenerated -= value;
        }
        
        public event Action<CardData> OnCardPlayed
        {
            add => eventManager.OnCardPlayed += value;
            remove => eventManager.OnCardPlayed -= value;
        }
        
        public event Action<CardPlayResult> OnCardPlayResult
        {
            add => eventManager.OnCardPlayResult += value;
            remove => eventManager.OnCardPlayResult -= value;
        }
        
        public event Action<HandState> OnHandStateChanged
        {
            add => eventManager.OnHandStateChanged += value;
            remove => eventManager.OnHandStateChanged -= value;
        }
        
        public event Action OnHandCleared
        {
            add => eventManager.OnHandCleared += value;
            remove => eventManager.OnHandCleared -= value;
        }
        
        public event Action<DamagePreviewInfo> OnDamagePreviewCalculated
        {
            add => eventManager.OnDamagePreviewCalculated += value;
            remove => eventManager.OnDamagePreviewCalculated -= value;
        }
        
        public event Action OnDamagePreviewCleared
        {
            add => eventManager.OnDamagePreviewCleared += value;
            remove => eventManager.OnDamagePreviewCleared -= value;
        }
        
        public event Action OnEnemyDataChanged
        {
            add => eventManager.OnEnemyDataChanged += value;
            remove => eventManager.OnEnemyDataChanged -= value;
        }
        
        public event Action OnBattleFieldChanged
        {
            add => eventManager.OnBattleFieldChanged += value;
            remove => eventManager.OnBattleFieldChanged -= value;
        }
        
        // ActionManagerのイベント
        public event Action<int, int> OnActionsChanged
        {
            add => actionManager.OnActionsChanged += value;
            remove => actionManager.OnActionsChanged -= value;
        }
        
        public event Action OnActionsExhausted
        {
            add => actionManager.OnActionsExhausted += value;
            remove => actionManager.OnActionsExhausted -= value;
        }
        
        public event Action OnAutoTurnEnd
        {
            add => actionManager.OnAutoTurnEnd += value;
            remove => actionManager.OnAutoTurnEnd -= value;
        }
        
        #endregion
        
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
        
        #region 初期化
        
        /// <summary>
        /// 手札システムの初期化
        /// </summary>
        private void InitializeHandSystem()
        {
            // 外部システム参照の取得
            battleManager = GetComponent<BattleManager>();
            weaponSelectionSystem = GetComponent<WeaponSelectionSystem>();
            comboSystem = GetComponent<ComboSystem>();
            
            if (battleManager != null)
                battleField = battleManager.BattleField;
            
            // 分離されたシステムの初期化
            InitializeSeparatedSystems();
            
            // データ初期化
            InitializeData();
            
            LogDebug("HandSystemRefactored initialized");
        }
        
        /// <summary>
        /// 分離されたシステムの初期化
        /// </summary>
        private void InitializeSeparatedSystems()
        {
            // イベントマネージャー
            eventManager = new HandEventManager();
            
            // ダメージ計算機
            damageCalculator = new DamageCalculator(battleManager, battleField, comboSystem);
            
            // 行動マネージャー
            actionManager = new ActionManager(battleManager, this, 1);
            
            LogDebug("Separated systems initialized");
        }
        
        /// <summary>
        /// データの初期化
        /// </summary>
        private void InitializeData()
        {
            currentHand = new CardData[handSize];
            usedCards = new List<CardData>();
            handHistory = new List<CardData[]>();
            weaponUsageCount = new Dictionary<string, int>();
            
            currentHandState = HandState.Empty;
            totalCardsPlayed = 0;
            totalDamageDealt = 0;
        }
        
        #endregion
        
        #region イベントハンドラー登録/解除
        
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
            
            var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
            if (attachmentSystem != null)
            {
                attachmentSystem.OnWeaponCardsGenerated -= HandleWeaponCardsGenerated;
                LogDebug("AttachmentSystem events unregistered");
            }
        }
        
        #endregion
        
        #region イベントハンドラー
        
        /// <summary>
        /// ターン変更時の処理
        /// </summary>
        private void HandleTurnChanged(int turn)
        {
            if (autoGenerateOnTurnStart && battleManager.CurrentState == GameState.PlayerTurn)
            {
                actionManager.InitializeActionsForTurn();
                
                var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
                if (attachmentSystem != null)
                {
                    attachmentSystem.RegenerateWeaponCardsForNewTurn();
                }
                
                GenerateHand();
            }
        }
        
        /// <summary>
        /// ゲーム状態変更時の処理
        /// </summary>
        private void HandleGameStateChanged(GameState newState)
        {
            LogDebug($"ゲーム状態変更: {newState}, 現在の手札状態: {currentHandState}, 残り行動回数: {RemainingActions}");
            
            switch (newState)
            {
                case GameState.PlayerTurn:
                    if (RemainingActions <= 0 || currentHandState == HandState.Empty || currentHandState == HandState.TurnEnded)
                    {
                        actionManager.InitializeActionsForTurn();
                        
                        var attachmentSystem = battleManager?.GetComponent<AttachmentSystem>();
                        if (attachmentSystem != null)
                        {
                            attachmentSystem.RegenerateWeaponCardsForNewTurn();
                        }
                        
                        GenerateHand();
                    }
                    break;
                    
                case GameState.EnemyTurn:
                    ChangeHandState(HandState.TurnEnded);
                    break;
                    
                case GameState.Victory:
                case GameState.Defeat:
                    ClearHand();
                    break;
            }
        }
        
        /// <summary>
        /// プレイヤーデータ変更時の処理
        /// </summary>
        private void HandlePlayerDataChanged(PlayerData playerData)
        {
            if (ShouldRegenerateHand(playerData))
            {
                GenerateHand();
            }
        }
        
        /// <summary>
        /// 武器カード生成時の処理
        /// </summary>
        private void HandleWeaponCardsGenerated(List<CardData> weaponCards)
        {
            LogDebug($"Weapon cards generated: {weaponCards.Count} cards");
            
            try
            {
                GenerateHandFromWeaponCards(weaponCards);
                ChangeHandState(HandState.Generated);
                eventManager.FireHandGenerated(currentHand);
                
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
        
        #region 手札生成
        
        /// <summary>
        /// 手札の生成
        /// </summary>
        public void GenerateHand()
        {
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
                GenerateHandFromWeaponCards(weaponCards);
                ChangeHandState(HandState.Generated);
                RecordHandToHistory();
                eventManager.FireHandGenerated(currentHand);
                
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
            currentHand = new CardData[handSize];
            
            for (int i = 0; i < handSize && i < weaponCards.Count; i++)
            {
                currentHand[i] = weaponCards[i];
            }
            
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
        
        #endregion
        
        #region カード使用
        
        /// <summary>
        /// カードの使用
        /// </summary>
        /// <param name="handIndex">手札のインデックス（0-4）</param>
        /// <returns>使用結果</returns>
        public CardPlayResult PlayCard(int handIndex)
        {
            LogDebug($"=== PlayCard START: handIndex {handIndex} ===");
            
            var result = new CardPlayResult();
            
            try
            {
                if (!ValidateCardPlay(handIndex, out string errorMessage))
                {
                    result.isSuccess = false;
                    result.message = errorMessage;
                    return result;
                }
                
                CardData card = currentHand[handIndex];
                var damageResult = damageCalculator.CalculateDamage(card);
                
                if (damageResult.IsSuccess)
                {
                    result = HandleSuccessfulCardPlay(card, handIndex, damageResult.TotalDamage);
                    
                    // ダメージ適用とUI更新
                    ApplyDamageToTargets(damageResult);
                }
                else
                {
                    result.isSuccess = false;
                    result.message = damageResult.ErrorMessage ?? "攻撃実行に失敗しました";
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error playing card: {ex.Message}");
                result.isSuccess = false;
                result.message = "カード使用中にエラーが発生しました";
            }
            
            eventManager.FireCardPlayResult(result);
            LogDebug($"=== PlayCard END: handIndex {handIndex} ===");
            return result;
        }
        
        /// <summary>
        /// ダメージプレビューの計算（1回クリック時）
        /// </summary>
        /// <param name="handIndex">手札のインデックス</param>
        /// <returns>ダメージプレビュー情報</returns>
        public DamagePreviewInfo CalculateCardPreview(int handIndex)
        {
            if (handIndex < 0 || handIndex >= handSize || currentHand[handIndex] == null)
            {
                return null;
            }
            
            var card = currentHand[handIndex];
            var previewInfo = damageCalculator.CalculatePreviewDamage(card);
            
            if (previewInfo != null)
            {
                eventManager.FireDamagePreviewCalculated(previewInfo);
                LogDebug($"ダメージプレビュー計算: {previewInfo.Description}");
            }
            
            return previewInfo;
        }
        
        /// <summary>
        /// ダメージプレビューのクリア
        /// </summary>
        public void ClearDamagePreview()
        {
            eventManager.FireDamagePreviewCleared();
            LogDebug("ダメージプレビューをクリア");
        }
        
        #endregion
        
        #region ヘルパーメソッド
        
        /// <summary>
        /// カード使用の妥当性チェック
        /// </summary>
        private bool ValidateCardPlay(int handIndex, out string errorMessage)
        {
            errorMessage = "";
            
            if (battleManager.CurrentState != GameState.PlayerTurn && battleManager.CurrentState != GameState.Victory)
            {
                errorMessage = "プレイヤーのターンまたは勝利状態ではありません";
                return false;
            }
            
            if (handIndex < 0 || handIndex >= handSize)
            {
                errorMessage = "無効な手札インデックスです";
                return false;
            }
            
            if (currentHand[handIndex] == null)
            {
                errorMessage = "選択された位置にカードがありません";
                return false;
            }
            
            if (currentHandState != HandState.Generated)
            {
                errorMessage = "手札が使用可能な状態ではありません";
                return false;
            }
            
            if (!IsCardTargetValid(currentHand[handIndex]))
            {
                errorMessage = "カードのターゲットが無効です";
                return false;
            }
            
            if (!IsWeaponUsable(currentHand[handIndex]))
            {
                errorMessage = "武器がクールダウン中です";
                return false;
            }
            
            if (!CanTakeAction)
            {
                errorMessage = "行動回数が残っていません";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// カード使用成功時の処理
        /// </summary>
        private CardPlayResult HandleSuccessfulCardPlay(CardData card, int handIndex, int damageDealt)
        {
            usedCards.Add(card);
            currentHand[handIndex] = null;
            
            totalCardsPlayed++;
            totalDamageDealt += damageDealt;
            UpdateWeaponUsageCount(card.weaponData.weaponName);
            
            SetWeaponCooldown(card.weaponData);
            ChangeHandState(HandState.CardUsed);
            
            eventManager.FireCardPlayed(card);
            
            bool turnEnded = actionManager.ConsumeAction();
            
            var result = new CardPlayResult
            {
                isSuccess = true,
                message = $"{card.displayName}を使用しました",
                playedCard = card,
                damageDealt = damageDealt,
                turnEnded = turnEnded
            };
            
            LogDebug($"Card played successfully: {card.displayName}, Damage: {damageDealt}, Actions remaining: {RemainingActions}");
            return result;
        }
        
        /// <summary>
        /// ダメージ適用とUI更新
        /// </summary>
        private void ApplyDamageToTargets(DamageCalculationResult damageResult)
        {
            foreach (var enemy in damageResult.HitEnemies)
            {
                enemy.TakeDamage(damageResult.TotalDamage);
            }
            
            if (damageResult.HitEnemies.Count > 0)
            {
                eventManager.FireEnemyDataChanged();
            }
            
            if (damageResult.HitGates.Count > 0)
            {
                eventManager.FireBattleFieldChanged();
            }
        }
        
        /// <summary>
        /// カードターゲットの妥当性チェック
        /// </summary>
        private bool IsCardTargetValid(CardData card)
        {
            if (card?.weaponData == null) return false;
            return card.IsValidTarget(3); // デフォルト3列
        }
        
        /// <summary>
        /// 武器使用可能性チェック
        /// </summary>
        private bool IsWeaponUsable(CardData card)
        {
            if (card?.weaponData != null)
            {
                if (card.weaponData.cooldownTurns <= 0 || card.weaponData.canUseConsecutively)
                {
                    return true;
                }
            }
            
            return true; // デフォルトで使用可能
        }
        
        /// <summary>
        /// 武器クールダウン設定
        /// </summary>
        private void SetWeaponCooldown(WeaponData weaponData)
        {
            // 必要に応じて実装
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
        
        #endregion
        
        #region 手札管理
        
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
            eventManager.FireHandCleared();
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
                eventManager.FireHandStateChanged(newState);
                LogDebug($"Hand state changed to: {newState}");
            }
        }
        
        /// <summary>
        /// 手札再生成が必要かチェック
        /// </summary>
        private bool ShouldRegenerateHand(PlayerData playerData)
        {
            return currentHandState == HandState.Empty;
        }
        
        #endregion
        
        #region 統計と履歴
        
        /// <summary>
        /// 手札履歴への記録
        /// </summary>
        private void RecordHandToHistory()
        {
            if (debugMode && currentHand != null)
            {
                handHistory.Add(currentHand.ToArray());
                
                if (handHistory.Count > 10)
                {
                    handHistory.RemoveAt(0);
                }
            }
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
                remainingActions = RemainingActions,
                maxActionsPerTurn = MaxActionsPerTurn,
                actionBonus = 0 // ActionManagerから取得する必要がある場合は実装
            };
        }
        
        #endregion
        
        #region デバッグ
        
        /// <summary>
        /// デバッグログ出力
        /// </summary>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[HandSystemRefactored] {message}");
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
        /// システム情報の取得（デバッグ用）
        /// </summary>
        public string GetSystemInfo()
        {
            return $"[HandSystemRefactored]\n" +
                   $"  手札状態: {currentHandState}\n" +
                   $"  残りカード: {RemainingCards}\n" +
                   $"  {actionManager?.GetActionInfo()}\n" +
                   $"  総カード使用: {totalCardsPlayed}\n" +
                   $"  総ダメージ: {totalDamageDealt}";
        }
        
        #endregion
        
        #region ActionManager委譲メソッド
        
        /// <summary>
        /// 行動回数ボーナスを追加
        /// </summary>
        public void AddActionBonus(int bonus)
        {
            actionManager?.AddActionBonus(bonus);
        }
        
        /// <summary>
        /// 行動回数ボーナスをリセット
        /// </summary>
        public void ResetActionBonus()
        {
            actionManager?.ResetActionBonus();
        }
        
        /// <summary>
        /// 行動回数情報を取得
        /// </summary>
        public string GetActionInfo()
        {
            return actionManager?.GetActionInfo() ?? "ActionManager not available";
        }
        
        #endregion
    }
}