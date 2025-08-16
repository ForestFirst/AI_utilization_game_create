using System;
using System.Collections;
using UnityEngine;

namespace BattleSystem.Actions
{
    /// <summary>
    /// 行動回数管理を担当するクラス
    /// 単一責任原則に従い、行動回数の管理のみを処理
    /// </summary>
    public class ActionManager : IActionManager
    {
        #region プロパティとフィールド
        
        [SerializeField] private int baseActionsPerTurn = 1;
        [SerializeField] private bool autoEndTurnWhenActionsExhausted = true;
        [SerializeField] private float autoEndTurnDelay = 0.5f;
        
        private int maxActionsPerTurn;
        private int remainingActions;
        private int actionBonus;
        
        private readonly BattleManager battleManager;
        private readonly MonoBehaviour coroutineRunner;
        
        public int RemainingActions => remainingActions;
        public int MaxActionsPerTurn => maxActionsPerTurn;
        public bool CanTakeAction => remainingActions > 0;
        public bool HasActionsRemaining => remainingActions > 0;
        
        #endregion
        
        #region イベント
        
        public event Action<int, int> OnActionsChanged;
        public event Action OnActionsExhausted;
        public event Action OnAutoTurnEnd;
        
        #endregion
        
        #region コンストラクタ
        
        public ActionManager(BattleManager battleManager, MonoBehaviour coroutineRunner, int baseActions = 1)
        {
            this.battleManager = battleManager ?? throw new ArgumentNullException(nameof(battleManager));
            this.coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
            this.baseActionsPerTurn = baseActions;
            
            InitializeActionSystem();
        }
        
        #endregion
        
        #region 初期化
        
        /// <summary>
        /// 行動システムの初期化
        /// </summary>
        private void InitializeActionSystem()
        {
            actionBonus = 0;
            maxActionsPerTurn = baseActionsPerTurn;
            remainingActions = 0;
            
            Debug.Log("[ActionManager] Action system initialized");
        }
        
        #endregion
        
        #region 行動回数管理
        
        /// <summary>
        /// ターン開始時の行動回数初期化
        /// </summary>
        public void InitializeActionsForTurn()
        {
            maxActionsPerTurn = baseActionsPerTurn + actionBonus;
            remainingActions = maxActionsPerTurn;
            
            OnActionsChanged?.Invoke(remainingActions, maxActionsPerTurn);
            
            Debug.Log($"[ActionManager] ターン開始：行動回数 {remainingActions}/{maxActionsPerTurn}");
        }
        
        /// <summary>
        /// 行動回数消費と自動ターン終了チェック
        /// </summary>
        /// <returns>ターンが終了したか</returns>
        public bool ConsumeAction()
        {
            if (remainingActions > 0)
            {
                remainingActions--;
                OnActionsChanged?.Invoke(remainingActions, maxActionsPerTurn);
                
                Debug.Log($"[ActionManager] 行動回数消費：残り {remainingActions}/{maxActionsPerTurn}");
                
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
        /// 行動回数ボーナスを追加
        /// </summary>
        /// <param name="bonus">追加する行動回数</param>
        public void AddActionBonus(int bonus)
        {
            if (bonus > 0)
            {
                actionBonus += bonus;
                
                if (battleManager != null && battleManager.CurrentState == GameState.PlayerTurn)
                {
                    maxActionsPerTurn += bonus;
                    remainingActions += bonus;
                    OnActionsChanged?.Invoke(remainingActions, maxActionsPerTurn);
                }
                
                Debug.Log($"[ActionManager] 行動回数ボーナス追加: +{bonus} (総ボーナス: {actionBonus})");
            }
        }
        
        /// <summary>
        /// 行動回数ボーナスをリセット
        /// </summary>
        public void ResetActionBonus()
        {
            actionBonus = 0;
            Debug.Log("[ActionManager] 行動回数ボーナスをリセット");
        }
        
        /// <summary>
        /// 現在の行動回数情報を取得（デバッグ用）
        /// </summary>
        /// <returns>行動回数情報</returns>
        public string GetActionInfo()
        {
            return $"行動回数: {remainingActions}/{maxActionsPerTurn} (ベース: {baseActionsPerTurn}, ボーナス: {actionBonus})";
        }
        
        #endregion
        
        #region 自動ターン終了
        
        /// <summary>
        /// 自動ターン終了の実行
        /// </summary>
        /// <returns>ターン終了が実行されたか</returns>
        private bool CheckAutoTurnEnd()
        {
            if (remainingActions <= 0 && battleManager != null)
            {
                Debug.Log($"[ActionManager] 自動ターン終了を開始 ({autoEndTurnDelay}秒後)");
                
                coroutineRunner.StartCoroutine(AutoEndTurnCoroutine());
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 自動ターン終了のコルーチン
        /// </summary>
        private IEnumerator AutoEndTurnCoroutine()
        {
            yield return new WaitForSeconds(autoEndTurnDelay);
            
            OnAutoTurnEnd?.Invoke();
            
            Debug.Log("[ActionManager] 自動ターン終了完了");
        }
        
        #endregion
        
        #region テスト用メソッド
        
        /// <summary>
        /// テスト用: 継続テストのための行動回数リセット
        /// </summary>
        public void ResetActionsForContinuousTesting()
        {
            Debug.Log("[ActionManager] テスト用: 行動回数をリセット中...");
            
            remainingActions = maxActionsPerTurn;
            OnActionsChanged?.Invoke(remainingActions, maxActionsPerTurn);
            
            Debug.Log($"[ActionManager] ✅ テスト用リセット完了: 行動回数 {remainingActions}/{maxActionsPerTurn}");
        }
        
        #endregion
        
        #region 設定更新
        
        /// <summary>
        /// 基本行動回数を更新
        /// </summary>
        /// <param name="newBaseActions">新しい基本行動回数</param>
        public void UpdateBaseActions(int newBaseActions)
        {
            if (newBaseActions > 0)
            {
                baseActionsPerTurn = newBaseActions;
                Debug.Log($"[ActionManager] 基本行動回数を更新: {baseActionsPerTurn}");
            }
        }
        
        /// <summary>
        /// 自動ターン終了設定を更新
        /// </summary>
        /// <param name="autoEnd">自動ターン終了の有効/無効</param>
        /// <param name="delay">自動ターン終了の遅延時間</param>
        public void UpdateAutoTurnEndSettings(bool autoEnd, float delay = 0.5f)
        {
            autoEndTurnWhenActionsExhausted = autoEnd;
            autoEndTurnDelay = delay;
            
            Debug.Log($"[ActionManager] 自動ターン終了設定を更新: {autoEnd}, 遅延: {delay}秒");
        }
        
        #endregion
    }
}