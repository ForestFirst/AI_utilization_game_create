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

    /// <summary>
    /// 武器タイプ（文字列の代わりにenumとして定義）
    /// </summary>
    public enum WeaponType
    {
        None,       // なし
        Sword,      // 剣
        Bow,        // 弓
        Staff,      // 杖
        Dagger,     // 短剣
        Axe,        // 斧
        Spear,      // 槍
        Magic,      // 魔法
        Gun,        // 銃
        Shield,     // 盾
        Special     // 特殊
    }

    /// <summary>
    /// 攻撃属性
    /// </summary>
    public enum AttackAttribute
    {
        None,       // 無属性
        Physical,   // 物理
        Fire,       // 火
        Water,      // 水
        Earth,      // 土
        Air,        // 風
        Light,      // 光
        Dark,       // 闇
        Lightning,  // 雷
        Ice,        // 氷
        Poison,     // 毒
        Holy,       // 聖
        Chaos       // 混沌
    }

    /// <summary>
    /// ゲームの状態
    /// </summary>
    public enum GameState
    {
        Initializing,   // 初期化中
        MainMenu,       // メインメニュー
        Playing,        // プレイ中
        Paused,         // 一時停止
        Victory,        // 勝利
        Defeat,         // 敗北
        GameOver       // ゲームオーバー
    }

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

    /// <summary>
    /// コンボの段階
    /// </summary>
    public enum ComboStep
    {
        None,       // なし
        First,      // 初段
        Second,     // 二段目
        Third,      // 三段目
        Fourth,     // 四段目
        Final       // 最終段
    }
}