using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    // 武器選択の結果
    public struct WeaponSelectionResult
    {
        public bool isValid;
        public string errorMessage;
        public WeaponData selectedWeapon;
        public int weaponIndex;
    }

    // 武器使用制限の理由
    public enum WeaponRestrictionReason
    {
        None,                    // 制限なし
        Cooldown,               // クールダウン中
        NotEquipped,            // 装備されていない
        InvalidTarget,          // 無効なターゲット
        OutOfRange,             // 範囲外
        NoValidTargets          // 有効なターゲットなし
    }

    // 武器選択システム管理クラス
    public class WeaponSelectionSystem : MonoBehaviour
    {
        [Header("武器選択設定")]
        [SerializeField] private bool allowEmptyWeaponSlots = false;
        [SerializeField] private bool showCooldownInfo = true;
        [SerializeField] private bool autoSelectValidWeapons = false;

        private BattleManager battleManager;
        private BattleFlowManager battleFlowManager;

        // イベント定義
        public event Action<int, WeaponData> OnWeaponSelected;
        public event Action<int, WeaponRestrictionReason> OnWeaponSelectionFailed;
        public event Action<int[]> OnAvailableWeaponsChanged;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            battleFlowManager = GetComponent<BattleFlowManager>();
        }

        private void OnEnable()
        {
            if (battleManager != null)
            {
                battleManager.OnPlayerDataChanged += HandlePlayerDataChanged;
                battleManager.OnTurnChanged += HandleTurnChanged;
            }
        }

        private void OnDisable()
        {
            if (battleManager != null)
            {
                battleManager.OnPlayerDataChanged -= HandlePlayerDataChanged;
                battleManager.OnTurnChanged -= HandleTurnChanged;
            }
        }

        // プレイヤーデータ変更時の処理
        private void HandlePlayerDataChanged(PlayerData playerData)
        {
            UpdateAvailableWeapons();
        }

        // ターン変更時の処理
        private void HandleTurnChanged(int turn)
        {
            UpdateAvailableWeapons();
        }

        // 利用可能武器の更新
        private void UpdateAvailableWeapons()
        {
            int[] availableWeapons = GetAvailableWeaponIndices();
            OnAvailableWeaponsChanged?.Invoke(availableWeapons);
        }

        // 武器選択の試行
        public WeaponSelectionResult TrySelectWeapon(int weaponIndex, GridPosition targetPosition)
        {
            WeaponSelectionResult result = new WeaponSelectionResult();

            // 基本的な妥当性チェック
            WeaponRestrictionReason restriction = CheckWeaponRestrictions(weaponIndex);
            if (restriction != WeaponRestrictionReason.None)
            {
                result.isValid = false;
                result.errorMessage = GetRestrictionMessage(restriction);
                OnWeaponSelectionFailed?.Invoke(weaponIndex, restriction);
                return result;
            }

            WeaponData weapon = battleManager.PlayerData.equippedWeapons[weaponIndex];

            // ターゲット位置の妥当性チェック
            if (!ValidateWeaponTarget(weapon, targetPosition))
            {
                result.isValid = false;
                result.errorMessage = "無効なターゲットです";
                OnWeaponSelectionFailed?.Invoke(weaponIndex, WeaponRestrictionReason.InvalidTarget);
                return result;
            }

            // 選択成功
            result.isValid = true;
            result.selectedWeapon = weapon;
            result.weaponIndex = weaponIndex;
            
            OnWeaponSelected?.Invoke(weaponIndex, weapon);
            return result;
        }

        // 武器制限のチェック
        public WeaponRestrictionReason CheckWeaponRestrictions(int weaponIndex)
        {
            if (weaponIndex < 0 || weaponIndex >= 4)
                return WeaponRestrictionReason.NotEquipped;

            PlayerData player = battleManager.PlayerData;
            WeaponData weapon = player.equippedWeapons[weaponIndex];

            // 武器が装備されているかチェック
            if (weapon == null)
            {
                if (allowEmptyWeaponSlots)
                    return WeaponRestrictionReason.None;
                else
                    return WeaponRestrictionReason.NotEquipped;
            }

            // クールダウンチェック
            if (player.weaponCooldowns[weaponIndex] > 0)
                return WeaponRestrictionReason.Cooldown;

            // 有効なターゲットの存在チェック
            if (!HasValidTargets(weapon))
                return WeaponRestrictionReason.NoValidTargets;

            return WeaponRestrictionReason.None;
        }

        // 武器のターゲット妥当性チェック
        private bool ValidateWeaponTarget(WeaponData weapon, GridPosition targetPosition)
        {
            if (weapon == null)
                return false;

            BattleField field = battleManager.BattleField;

            switch (weapon.attackRange)
            {
                case AttackRange.SingleFront:
                    return ValidateSingleFrontTarget(field, targetPosition);

                case AttackRange.SingleTarget:
                    return ValidateSingleTarget(field, targetPosition);

                case AttackRange.Row1:
                    return ValidateRowTarget(field, 0);

                case AttackRange.Row2:
                    return ValidateRowTarget(field, 1);

                case AttackRange.Column:
                    return ValidateColumnTarget(field, targetPosition.x);

                case AttackRange.All:
                    return ValidateAllTarget(field);

                case AttackRange.Self:
                    return true; // 自分への行動は常に有効

                default:
                    return false;
            }
        }

        // 一番前の敵ターゲット妥当性チェック
        private bool ValidateSingleFrontTarget(BattleField field, GridPosition target)
        {
            EnemyInstance frontEnemy = field.GetFrontEnemyInColumn(target.x);
            if (frontEnemy != null)
            {
                // 指定位置が実際に一番前の敵の位置と一致するかチェック
                return frontEnemy.gridX == target.x && frontEnemy.gridY == target.y;
            }
            return false;
        }

        // 単体ターゲット妥当性チェック
        private bool ValidateSingleTarget(BattleField field, GridPosition target)
        {
            // 敵への攻撃
            EnemyInstance enemy = field.GetEnemyAt(target);
            if (enemy != null && enemy.IsAlive())
                return true;

            // ゲートへの攻撃
            if (field.CanAttackGate(target.x))
            {
                var gate = field.Gates.Find(g => g.position.x == target.x);
                return gate != null && !gate.IsDestroyed();
            }

            return false;
        }

        // 行ターゲット妥当性チェック
        private bool ValidateRowTarget(BattleField field, int row)
        {
            return field.GetEnemiesInRow(row).Count > 0;
        }

        // 列ターゲット妥当性チェック
        private bool ValidateColumnTarget(BattleField field, int column)
        {
            return field.GetEnemiesInColumn(column).Count > 0 || field.CanAttackGate(column);
        }

        // 全体ターゲット妥当性チェック
        private bool ValidateAllTarget(BattleField field)
        {
            return field.GetAllEnemies().Count > 0 || field.GetAliveGateCount() > 0;
        }

        // 有効なターゲットが存在するかチェック
        private bool HasValidTargets(WeaponData weapon)
        {
            BattleField field = battleManager.BattleField;

            switch (weapon.attackRange)
            {
                case AttackRange.SingleFront:
                    for (int x = 0; x < field.Columns; x++)
                    {
                        if (field.GetFrontEnemyInColumn(x) != null)
                            return true;
                    }
                    return false;

                case AttackRange.SingleTarget:
                    return field.GetAllEnemies().Count > 0 || field.GetAliveGateCount() > 0;

                case AttackRange.Row1:
                    return field.GetEnemiesInRow(0).Count > 0;

                case AttackRange.Row2:
                    return field.GetEnemiesInRow(1).Count > 0;

                case AttackRange.Column:
                    for (int x = 0; x < field.Columns; x++)
                    {
                        if (field.GetEnemiesInColumn(x).Count > 0 || field.CanAttackGate(x))
                            return true;
                    }
                    return false;

                case AttackRange.All:
                    return field.GetAllEnemies().Count > 0 || field.GetAliveGateCount() > 0;

                case AttackRange.Self:
                    return true;

                default:
                    return false;
            }
        }

        // 利用可能な武器インデックス配列を取得
        public int[] GetAvailableWeaponIndices()
        {
            List<int> available = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                if (CheckWeaponRestrictions(i) == WeaponRestrictionReason.None)
                {
                    available.Add(i);
                }
            }

            return available.ToArray();
        }

        // 武器の詳細情報を取得
        public WeaponInfo GetWeaponInfo(int weaponIndex)
        {
            WeaponInfo info = new WeaponInfo();

            if (weaponIndex < 0 || weaponIndex >= 4)
            {
                info.isValid = false;
                return info;
            }

            PlayerData player = battleManager.PlayerData;
            WeaponData weapon = player.equippedWeapons[weaponIndex];

            info.isValid = weapon != null;
            info.weapon = weapon;
            info.weaponIndex = weaponIndex;
            info.cooldownRemaining = player.weaponCooldowns[weaponIndex];
            info.restriction = CheckWeaponRestrictions(weaponIndex);
            info.canUse = info.restriction == WeaponRestrictionReason.None;

            return info;
        }

        // 最適な武器を自動選択
        public int GetOptimalWeaponIndex(GridPosition targetPosition)
        {
            int[] availableWeapons = GetAvailableWeaponIndices();

            if (availableWeapons.Length == 0)
                return -1;

            // 基本的な選択ロジック：最初の利用可能武器を選択
            foreach (int weaponIndex in availableWeapons)
            {
                WeaponData weapon = battleManager.PlayerData.equippedWeapons[weaponIndex];
                if (ValidateWeaponTarget(weapon, targetPosition))
                {
                    return weaponIndex;
                }
            }

            return availableWeapons[0]; // フォールバック
        }

        // 制限理由のメッセージを取得
        private string GetRestrictionMessage(WeaponRestrictionReason reason)
        {
            switch (reason)
            {
                case WeaponRestrictionReason.Cooldown:
                    return "武器がクールダウン中です";
                case WeaponRestrictionReason.NotEquipped:
                    return "武器が装備されていません";
                case WeaponRestrictionReason.InvalidTarget:
                    return "無効なターゲットです";
                case WeaponRestrictionReason.OutOfRange:
                    return "射程外です";
                case WeaponRestrictionReason.NoValidTargets:
                    return "有効なターゲットがありません";
                default:
                    return "不明なエラー";
            }
        }

        // 武器選択の実行（BattleFlowManagerとの連携）
        public bool ExecuteWeaponSelection(int weaponIndex, GridPosition targetPosition)
        {
            WeaponSelectionResult result = TrySelectWeapon(weaponIndex, targetPosition);

            if (!result.isValid)
            {
                Debug.LogWarning($"武器選択失敗: {result.errorMessage}");
                return false;
            }

            // 戦闘行動として登録
            BattleAction action = new BattleAction(BattleActionType.WeaponAttack);
            action.weaponIndex = weaponIndex;
            action.targetPosition = targetPosition;

            return battleFlowManager.RegisterPlayerAction(action);
        }
    }

    // 武器情報構造体
    [Serializable]
    public struct WeaponInfo
    {
        public bool isValid;
        public WeaponData weapon;
        public int weaponIndex;
        public int cooldownRemaining;
        public WeaponRestrictionReason restriction;
        public bool canUse;
    }

    // 武器選択UI支援クラス
    public static class WeaponSelectionHelper
    {
        // 武器の使用可能状態をテキストで取得
        public static string GetWeaponStatusText(WeaponInfo weaponInfo)
        {
            if (!weaponInfo.isValid)
                return "未装備";

            if (!weaponInfo.canUse)
            {
                switch (weaponInfo.restriction)
                {
                    case WeaponRestrictionReason.Cooldown:
                        return $"CT: {weaponInfo.cooldownRemaining}";
                    case WeaponRestrictionReason.NoValidTargets:
                        return "ターゲットなし";
                    default:
                        return "使用不可";
                }
            }

            return "使用可能";
        }

        // 攻撃範囲の説明テキストを取得
        public static string GetAttackRangeDescription(AttackRange range)
        {
            switch (range)
            {
                case AttackRange.SingleFront:
                    return "一番前の敵";
                case AttackRange.SingleTarget:
                    return "単体";
                case AttackRange.Row1:
                    return "前列全体";
                case AttackRange.Row2:
                    return "後列全体";
                case AttackRange.Column:
                    return "縦列貫通";
                case AttackRange.All:
                    return "全体";
                case AttackRange.Self:
                    return "自分";
                default:
                    return "不明";
            }
        }

        // 武器属性の組み合わせ名を取得
        public static string GetWeaponCombinationName(AttackAttribute attackAttr, WeaponType weaponType)
        {
            return $"{GetAttackAttributeName(attackAttr)}{GetWeaponTypeName(weaponType)}";
        }

        private static string GetAttackAttributeName(AttackAttribute attr)
        {
            switch (attr)
            {
                case AttackAttribute.Fire: return "炎";
                case AttackAttribute.Ice: return "氷";
                case AttackAttribute.Thunder: return "雷";
                case AttackAttribute.Wind: return "風";
                case AttackAttribute.Earth: return "土";
                case AttackAttribute.Light: return "光";
                case AttackAttribute.Dark: return "闇";
                case AttackAttribute.None: return "";
                default: return "無";
            }
        }

        private static string GetWeaponTypeName(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Sword: return "剣";
                case WeaponType.Axe: return "斧";
                case WeaponType.Spear: return "槍";
                case WeaponType.Bow: return "弓";
                case WeaponType.Gun: return "銃";
                case WeaponType.Shield: return "盾";
                case WeaponType.Magic: return "魔法";
                case WeaponType.Tool: return "道具";
                default: return "武器";
            }
        }
    }
}