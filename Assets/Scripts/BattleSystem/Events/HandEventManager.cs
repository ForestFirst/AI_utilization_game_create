using System;
using UnityEngine;
using BattleSystem.Combat;

namespace BattleSystem.Events
{
    /// <summary>
    /// 手札システムのイベント管理を担当するクラス
    /// 単一責任原則に従い、イベントの管理のみを処理
    /// </summary>
    public class HandEventManager : IHandEventManager
    {
        #region 手札関連イベント
        
        public event Action<CardData[]> OnHandGenerated;
        public event Action<CardData> OnCardPlayed;
        public event Action<CardPlayResult> OnCardPlayResult;
        public event Action<HandState> OnHandStateChanged;
        public event Action OnHandCleared;
        
        #endregion
        
        #region ダメージプレビュー関連イベント
        
        public event Action<DamagePreviewInfo> OnDamagePreviewCalculated;
        public event Action OnDamagePreviewCleared;
        
        #endregion
        
        #region 戦闘データ変更関連イベント
        
        public event Action OnEnemyDataChanged;
        public event Action OnBattleFieldChanged;
        
        #endregion
        
        #region イベント発火メソッド
        
        /// <summary>
        /// 手札生成イベントを発火
        /// </summary>
        public void FireHandGenerated(CardData[] hand)
        {
            try
            {
                OnHandGenerated?.Invoke(hand);
                Debug.Log($"[HandEventManager] 手札生成イベント発火: {hand?.Length ?? 0}枚");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] 手札生成イベントエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// カード使用イベントを発火
        /// </summary>
        public void FireCardPlayed(CardData card)
        {
            try
            {
                OnCardPlayed?.Invoke(card);
                Debug.Log($"[HandEventManager] カード使用イベント発火: {card?.displayName ?? "Unknown"}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] カード使用イベントエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// カード使用結果イベントを発火
        /// </summary>
        public void FireCardPlayResult(CardPlayResult result)
        {
            try
            {
                OnCardPlayResult?.Invoke(result);
                Debug.Log($"[HandEventManager] カード使用結果イベント発火: 成功={result.isSuccess}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] カード使用結果イベントエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 手札状態変更イベントを発火
        /// </summary>
        public void FireHandStateChanged(HandState newState)
        {
            try
            {
                OnHandStateChanged?.Invoke(newState);
                Debug.Log($"[HandEventManager] 手札状態変更イベント発火: {newState}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] 手札状態変更イベントエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 手札クリアイベントを発火
        /// </summary>
        public void FireHandCleared()
        {
            try
            {
                OnHandCleared?.Invoke();
                Debug.Log("[HandEventManager] 手札クリアイベント発火");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] 手札クリアイベントエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ダメージプレビュー計算イベントを発火
        /// </summary>
        public void FireDamagePreviewCalculated(DamagePreviewInfo previewInfo)
        {
            try
            {
                OnDamagePreviewCalculated?.Invoke(previewInfo);
                Debug.Log($"[HandEventManager] ダメージプレビューイベント発火: {previewInfo?.Description ?? "Unknown"}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] ダメージプレビューイベントエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ダメージプレビュークリアイベントを発火
        /// </summary>
        public void FireDamagePreviewCleared()
        {
            try
            {
                OnDamagePreviewCleared?.Invoke();
                Debug.Log("[HandEventManager] ダメージプレビュークリアイベント発火");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] ダメージプレビュークリアイベントエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 敵データ変更イベントを発火
        /// </summary>
        public void FireEnemyDataChanged()
        {
            try
            {
                OnEnemyDataChanged?.Invoke();
                Debug.Log("[HandEventManager] 敵データ変更イベント発火");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] 敵データ変更イベントエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 戦場データ変更イベントを発火
        /// </summary>
        public void FireBattleFieldChanged()
        {
            try
            {
                OnBattleFieldChanged?.Invoke();
                Debug.Log("[HandEventManager] 戦場データ変更イベント発火");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HandEventManager] 戦場データ変更イベントエラー: {ex.Message}");
            }
        }
        
        #endregion
        
        #region デバッグ機能
        
        /// <summary>
        /// すべてのイベントリスナー数を取得（デバッグ用）
        /// </summary>
        public string GetEventListenerCounts()
        {
            return $"[HandEventManager] イベントリスナー数:\n" +
                   $"  OnHandGenerated: {OnHandGenerated?.GetInvocationList().Length ?? 0}\n" +
                   $"  OnCardPlayed: {OnCardPlayed?.GetInvocationList().Length ?? 0}\n" +
                   $"  OnCardPlayResult: {OnCardPlayResult?.GetInvocationList().Length ?? 0}\n" +
                   $"  OnHandStateChanged: {OnHandStateChanged?.GetInvocationList().Length ?? 0}\n" +
                   $"  OnHandCleared: {OnHandCleared?.GetInvocationList().Length ?? 0}\n" +
                   $"  OnDamagePreviewCalculated: {OnDamagePreviewCalculated?.GetInvocationList().Length ?? 0}\n" +
                   $"  OnDamagePreviewCleared: {OnDamagePreviewCleared?.GetInvocationList().Length ?? 0}\n" +
                   $"  OnEnemyDataChanged: {OnEnemyDataChanged?.GetInvocationList().Length ?? 0}\n" +
                   $"  OnBattleFieldChanged: {OnBattleFieldChanged?.GetInvocationList().Length ?? 0}";
        }
        
        /// <summary>
        /// すべてのイベントリスナーをクリア（デバッグ用）
        /// </summary>
        public void ClearAllEventListeners()
        {
            OnHandGenerated = null;
            OnCardPlayed = null;
            OnCardPlayResult = null;
            OnHandStateChanged = null;
            OnHandCleared = null;
            OnDamagePreviewCalculated = null;
            OnDamagePreviewCleared = null;
            OnEnemyDataChanged = null;
            OnBattleFieldChanged = null;
            
            Debug.Log("[HandEventManager] すべてのイベントリスナーをクリアしました");
        }
        
        #endregion
    }
}