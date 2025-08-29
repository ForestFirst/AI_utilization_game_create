using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// ステージの難易度レベル
    /// </summary>
    public enum StageDifficulty
    {
        Easy = 1,       // 初心者向け
        Normal = 2,     // 標準難易度
        Hard = 3,       // 上級者向け
        Expert = 4,     // エキスパート
        Master = 5      // マスターレベル
    }

    /// <summary>
    /// ステージタイプ
    /// </summary>
    public enum StageType
    {
        Normal,         // 通常ステージ
        Boss,           // ボスステージ
        Event,          // イベントステージ
        Challenge,      // チャレンジステージ
        Tutorial        // チュートリアルステージ
    }

    /// <summary>
    /// ステージクリア報酬データ
    /// </summary>
    [Serializable]
    public class StageReward
    {
        [Header("報酬基本情報")]
        public int goldReward;              // ゴールド報酬
        public int experienceReward;        // 経験値報酬
        
        [Header("アイテム報酬")]
        public List<string> itemRewards;    // アイテムID一覧
        public List<int> itemQuantities;    // アイテム数量一覧
        
        [Header("武器報酬")]
        public List<string> weaponRewards;  // 武器ID一覧
        public float weaponDropRate;        // 武器ドロップ率（0.0-1.0）
        
        [Header("特別報酬")]
        public bool hasSpecialReward;       // 特別報酬があるか
        public string specialRewardId;      // 特別報酬ID
        public string specialRewardName;    // 特別報酬名

        public StageReward()
        {
            goldReward = 0;
            experienceReward = 0;
            itemRewards = new List<string>();
            itemQuantities = new List<int>();
            weaponRewards = new List<string>();
            weaponDropRate = 0.1f;
            hasSpecialReward = false;
            specialRewardId = "";
            specialRewardName = "";
        }
    }

    /// <summary>
    /// 敵配置パターンデータ
    /// </summary>
    [Serializable]
    public class EnemyFormation
    {
        [Header("敵配置情報")]
        public List<string> enemyIds;       // 敵ID一覧
        public List<GridPosition> positions; // 敵配置位置一覧
        public List<int> enemyLevels;       // 敵レベル一覧
        
        [Header("配置設定")]
        public int waveNumber;              // ウェーブ番号
        public float spawnDelay;            // 出現遅延（秒）
        public bool isRandomFormation;      // ランダム配置か

        public EnemyFormation()
        {
            enemyIds = new List<string>();
            positions = new List<GridPosition>();
            enemyLevels = new List<int>();
            waveNumber = 1;
            spawnDelay = 0f;
            isRandomFormation = false;
        }
    }

    /// <summary>
    /// ステージデータ
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "NewStageData", menuName = "BattleSystem/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("ステージ基本情報")]
        public string stageId;              // ステージ固有ID
        public string stageName;            // ステージ名
        public string stageDescription;     // ステージ説明
        public StageDifficulty difficulty;  // 難易度
        public StageType stageType;         // ステージタイプ
        
        // UI互換性プロパティ
        public string description => stageDescription;
        
        [Header("ステージ設定")]
        public int recommendedLevel;        // 推奨レベル
        public int maxTurns;               // 最大ターン数（0=無制限）
        public int staminaCost;            // スタミナ消費量
        public bool isUnlocked;            // 解放済みか
        
        [Header("戦闘設定")]
        public int battleFieldWidth;       // 戦場幅
        public int battleFieldHeight;      // 戦場高さ
        public int gateCount;              // ゲート数
        public List<int> gateHpList;       // 各ゲートのHP
        
        [Header("敵配置")]
        public List<EnemyFormation> enemyFormations; // 敵配置パターン一覧
        public int totalWaves;             // 総ウェーブ数
        
        [Header("報酬設定")]
        public StageReward clearReward;    // クリア報酬
        public StageReward firstClearBonus; // 初回クリアボーナス
        
        [Header("特殊ルール")]
        public bool hasTimeLimit;          // 制限時間あり
        public float timeLimitSeconds;     // 制限時間（秒）
        public List<string> specialRules;  // 特殊ルール一覧
        
        [Header("前提条件")]
        public List<string> prerequisiteStageIds; // 前提ステージID一覧
        public int requiredPlayerLevel;    // 必要プレイヤーレベル

        /// <summary>
        /// ステージが解放可能かチェック
        /// </summary>
        /// <param name="playerLevel">プレイヤーレベル</param>
        /// <param name="clearedStages">クリア済みステージIDリスト</param>
        /// <returns>解放可能な場合true</returns>
        public bool CanUnlock(int playerLevel, List<string> clearedStages)
        {
            // プレイヤーレベルチェック
            if (playerLevel < requiredPlayerLevel)
                return false;
            
            // 前提ステージクリアチェック
            if (prerequisiteStageIds != null && prerequisiteStageIds.Count > 0)
            {
                foreach (string prerequisiteId in prerequisiteStageIds)
                {
                    if (!clearedStages.Contains(prerequisiteId))
                        return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 難易度に基づく色を取得
        /// </summary>
        /// <returns>難易度色</returns>
        public Color GetDifficultyColor()
        {
            switch (difficulty)
            {
                case StageDifficulty.Easy:
                    return Color.green;
                case StageDifficulty.Normal:
                    return Color.white;
                case StageDifficulty.Hard:
                    return Color.yellow;
                case StageDifficulty.Expert:
                    return Color.red;
                case StageDifficulty.Master:
                    return Color.magenta;
                default:
                    return Color.gray;
            }
        }

        /// <summary>
        /// ステージタイプに基づくアイコン名を取得
        /// </summary>
        /// <returns>アイコン名</returns>
        public string GetStageTypeIcon()
        {
            switch (stageType)
            {
                case StageType.Normal:
                    return "stage_normal";
                case StageType.Boss:
                    return "stage_boss";
                case StageType.Event:
                    return "stage_event";
                case StageType.Challenge:
                    return "stage_challenge";
                case StageType.Tutorial:
                    return "stage_tutorial";
                default:
                    return "stage_unknown";
            }
        }

        /// <summary>
        /// デバッグ情報の取得
        /// </summary>
        /// <returns>デバッグ情報文字列</returns>
        public string GetDebugInfo()
        {
            return $"Stage[{stageId}]: {stageName} | Difficulty: {difficulty} | Type: {stageType} | Level: {recommendedLevel}";
        }
    }

    /// <summary>
    /// ステージ進行状況データ
    /// </summary>
    [Serializable]
    public class StageProgress
    {
        public string stageId;              // ステージID
        public bool isCleared;              // クリア済みか
        public bool firstClearRewarded;     // 初回クリア報酬受け取り済みか
        public int bestClearTurns;          // 最短クリアターン数
        public float bestClearTime;         // 最短クリア時間
        public int clearCount;              // クリア回数
        public DateTime lastPlayedTime;     // 最後にプレイした時間

        public StageProgress(string id)
        {
            stageId = id;
            isCleared = false;
            firstClearRewarded = false;
            bestClearTurns = int.MaxValue;
            bestClearTime = float.MaxValue;
            clearCount = 0;
            lastPlayedTime = DateTime.MinValue;
        }

        /// <summary>
        /// クリア記録を更新
        /// </summary>
        /// <param name="turns">クリアターン数</param>
        /// <param name="time">クリア時間</param>
        public void UpdateClearRecord(int turns, float time)
        {
            if (!isCleared)
            {
                isCleared = true;
            }

            if (turns < bestClearTurns)
            {
                bestClearTurns = turns;
            }

            if (time < bestClearTime)
            {
                bestClearTime = time;
            }

            clearCount++;
            lastPlayedTime = DateTime.Now;
        }
    }
}