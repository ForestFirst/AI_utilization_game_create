using System;

namespace BattleSystem
{
    /// <summary>
    /// カードのレアリティ
    /// </summary>
    public enum CardRarity
    {
        Common,     // コモン
        Uncommon,   // アンコモン
        Rare,       // レア
        Epic,       // エピック
        Legendary   // レジェンダリー
    }

    /// <summary>
    /// カードのタイプ
    /// </summary>
    public enum CardType
    {
        Attack,     // 攻撃カード
        Defense,    // 防御カード
        Support,    // 支援カード
        Spell,      // 魔法カード
        Special     // 特殊カード
    }

    /// <summary>
    /// カードの対象タイプ
    /// </summary>
    public enum CardTargetType
    {
        None,           // 対象なし
        SingleEnemy,    // 単体敵
        AllEnemies,     // 全敵
        SingleGate,     // 単体ゲート
        AllGates,       // 全ゲート
        Self,           // 自分
        Random          // ランダム
    }

    /// <summary>
    /// カードの効果タイプ
    /// </summary>
    public enum CardEffectType
    {
        Damage,         // ダメージ
        Heal,           // 回復
        Buff,           // バフ
        Debuff,         // デバフ
        Draw,           // ドロー
        Discard,        // 捨てる
        Shuffle,        // シャッフル
        Special         // 特殊効果
    }

    // WeaponTypeとAttackAttributeはWeaponData.csで定義済み - 重複削除

    // GameStateはBattleManager.csで定義済み - 重複削除

    /// <summary>
    /// 戦闘フェーズ
    /// </summary>
    public enum BattlePhase
    {
        Preparation,    // 準備フェーズ
        PlayerTurn,     // プレイヤーターン
        EnemyTurn,      // 敵ターン
        Resolution,     // 解決フェーズ
        TurnEnd        // ターン終了
    }

    // ComboStepはComboSystem.csでclassとして定義済み - 重複削除
}