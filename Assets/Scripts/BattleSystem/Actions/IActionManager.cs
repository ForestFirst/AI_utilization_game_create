using System;

namespace BattleSystem
{
    /// <summary>
    /// 行動回数管理を提供するインターフェース
    /// </summary>
    public interface IActionManager
    {
        /// <summary>
        /// 残り行動回数
        /// </summary>
        int RemainingActions { get; }
        
        /// <summary>
        /// 最大行動回数
        /// </summary>
        int MaxActionsPerTurn { get; }
        
        /// <summary>
        /// 行動可能かどうか
        /// </summary>
        bool CanTakeAction { get; }
        
        /// <summary>
        /// 行動回数が残っているかどうか
        /// </summary>
        bool HasActionsRemaining { get; }
        
        /// <summary>
        /// 行動回数変更時のイベント (残り, 最大)
        /// </summary>
        event Action<int, int> OnActionsChanged;
        
        /// <summary>
        /// 行動回数0時のイベント
        /// </summary>
        event Action OnActionsExhausted;
        
        /// <summary>
        /// 自動ターン終了時のイベント
        /// </summary>
        event Action OnAutoTurnEnd;
        
        /// <summary>
        /// ターン開始時の行動回数初期化
        /// </summary>
        void InitializeActionsForTurn();
        
        /// <summary>
        /// 行動回数を消費します
        /// </summary>
        /// <returns>ターンが終了したかどうか</returns>
        bool ConsumeAction();
        
        /// <summary>
        /// 行動回数ボーナスを追加します
        /// </summary>
        /// <param name="bonus">追加する行動回数</param>
        void AddActionBonus(int bonus);
        
        /// <summary>
        /// 行動回数ボーナスをリセットします
        /// </summary>
        void ResetActionBonus();
        
        /// <summary>
        /// 現在の行動回数情報を取得します（デバッグ用）
        /// </summary>
        /// <returns>行動回数情報</returns>
        string GetActionInfo();
    }
}