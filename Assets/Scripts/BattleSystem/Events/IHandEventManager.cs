using System;

namespace BattleSystem.Events
{
    /// <summary>
    /// 手札システムのイベント管理を提供するインターフェース
    /// </summary>
    public interface IHandEventManager
    {
        #region 手札関連イベント
        
        /// <summary>
        /// 手札生成時のイベント
        /// </summary>
        event Action<CardData[]> OnHandGenerated;
        
        /// <summary>
        /// カード使用時のイベント
        /// </summary>
        event Action<CardData> OnCardPlayed;
        
        /// <summary>
        /// カード使用結果のイベント
        /// </summary>
        event Action<CardPlayResult> OnCardPlayResult;
        
        /// <summary>
        /// 手札状態変更時のイベント
        /// </summary>
        event Action<HandState> OnHandStateChanged;
        
        /// <summary>
        /// 手札クリア時のイベント
        /// </summary>
        event Action OnHandCleared;
        
        #endregion
        
        #region ダメージプレビュー関連イベント
        
        /// <summary>
        /// ダメージプレビュー計算時のイベント
        /// </summary>
        event Action<DamagePreviewInfo> OnDamagePreviewCalculated;
        
        /// <summary>
        /// ダメージプレビュークリア時のイベント
        /// </summary>
        event Action OnDamagePreviewCleared;
        
        #endregion
        
        #region 戦闘データ変更関連イベント
        
        /// <summary>
        /// 敵データ変更時のイベント
        /// </summary>
        event Action OnEnemyDataChanged;
        
        /// <summary>
        /// 戦場データ変更時のイベント
        /// </summary>
        event Action OnBattleFieldChanged;
        
        #endregion
        
        #region イベント発火メソッド
        
        /// <summary>
        /// 手札生成イベントを発火
        /// </summary>
        void FireHandGenerated(CardData[] hand);
        
        /// <summary>
        /// カード使用イベントを発火
        /// </summary>
        void FireCardPlayed(CardData card);
        
        /// <summary>
        /// カード使用結果イベントを発火
        /// </summary>
        void FireCardPlayResult(CardPlayResult result);
        
        /// <summary>
        /// 手札状態変更イベントを発火
        /// </summary>
        void FireHandStateChanged(HandState newState);
        
        /// <summary>
        /// 手札クリアイベントを発火
        /// </summary>
        void FireHandCleared();
        
        /// <summary>
        /// ダメージプレビュー計算イベントを発火
        /// </summary>
        void FireDamagePreviewCalculated(DamagePreviewInfo previewInfo);
        
        /// <summary>
        /// ダメージプレビュークリアイベントを発火
        /// </summary>
        void FireDamagePreviewCleared();
        
        /// <summary>
        /// 敵データ変更イベントを発火
        /// </summary>
        void FireEnemyDataChanged();
        
        /// <summary>
        /// 戦場データ変更イベントを発火
        /// </summary>
        void FireBattleFieldChanged();
        
        #endregion
    }
}