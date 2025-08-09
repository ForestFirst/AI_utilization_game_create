using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleSystem
{
    // アイテムカテゴリの種類
    public enum ItemCategory
    {
        Recovery,       // 回復系
        Enhancement,    // 強化系
        AttributeBoost, // 属性ブースト系
        Utility,        // ユーティリティ系
        Special         // 特殊系
    }

    // アイテム効果の種類
    public enum ItemEffectType
    {
        HealHp,             // HP回復
        IncreaseMaxHp,      // 最大HP増加
        AttackPowerBoost,   // 攻撃力アップ
        AttributeBoost,     // 属性威力アップ
        CriticalRateBoost,  // クリティカル率アップ
        DefenseBoost,       // 防御力アップ
        SpeedBoost,         // 行動速度アップ
        RemoveStatusEffect, // 状態異常回復
        GrantStatusEffect,  // 状態異常付与
        WeaponCooldownReset // 武器クールダウンリセット
    }

    // アイテムの効果レベル
    public enum ItemEffectLevel
    {
        Level1 = 1,  // 基本効果
        Level2 = 2,  // 中級効果
        Level3 = 3   // 上級効果
    }

    // アイテム効果データ
    [Serializable]
    public class ItemEffect
    {
        public ItemEffectType effectType;
        public int effectValue;             // 効果値
        public int duration;                // 効果継続ターン数（0=即時効果）
        public AttackAttribute targetAttribute; // 対象属性（属性ブースト用）
        public float effectMultiplier;      // 効果倍率
        public string effectDescription;    // 効果説明
    }

    // アイテムデータ
    [Serializable]
    public class ItemData
    {
        public int itemId;
        public string itemName;
        public ItemCategory category;
        public ItemEffectLevel effectLevel;
        public ItemEffect[] effects;        // 複数効果対応
        public string description;
        public bool isConsumable;           // 消費アイテムフラグ
        public int maxStackSize;            // 最大スタック数
        public bool canUseInCombat;         // 戦闘中使用可能フラグ
        public int usageCost;               // 使用コスト（SP等）
        public Sprite itemIcon;             // アイテムアイコン
    }

    // アイテムインベントリスロット
    [Serializable]
    public class ItemSlot
    {
        public ItemData itemData;
        public int quantity;
        public int slotIndex;

        public ItemSlot(int index)
        {
            slotIndex = index;
            itemData = null;
            quantity = 0;
        }

        public bool IsEmpty => itemData == null || quantity <= 0;
        public bool IsFull => itemData != null && quantity >= itemData.maxStackSize;
        
        public bool CanAddItem(ItemData item, int amount = 1)
        {
            if (IsEmpty) return true;
            if (itemData.itemId == item.itemId && quantity + amount <= itemData.maxStackSize)
                return true;
            return false;
        }

        public bool AddItem(ItemData item, int amount = 1)
        {
            if (IsEmpty)
            {
                itemData = item;
                quantity = amount;
                return true;
            }
            else if (itemData.itemId == item.itemId && quantity + amount <= itemData.maxStackSize)
            {
                quantity += amount;
                return true;
            }
            return false;
        }

        public bool RemoveItem(int amount = 1)
        {
            if (quantity >= amount)
            {
                quantity -= amount;
                if (quantity <= 0)
                {
                    itemData = null;
                    quantity = 0;
                }
                return true;
            }
            return false;
        }
    }

    // アイテム使用結果
    public struct ItemUsageResult
    {
        public bool wasUsed;
        public ItemData usedItem;
        public ItemEffect[] appliedEffects;
        public string resultMessage;
        public bool itemConsumed;
    }

    // アイテムデータベース
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "BattleSystem/ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemData[] allItems;
        
        public ItemData[] AllItems => allItems;
        
        public ItemData GetItem(int itemId)
        {
            return System.Array.Find(allItems, item => item.itemId == itemId);
        }
        
        public ItemData[] GetItemsByCategory(ItemCategory category)
        {
            return System.Array.FindAll(allItems, item => item.category == category);
        }
        
        public ItemData[] GetItemsByLevel(ItemEffectLevel level)
        {
            return System.Array.FindAll(allItems, item => item.effectLevel == level);
        }
    }

    // アイテムインベントリ管理
    public class ItemInventory
    {
        private ItemSlot[] itemSlots;
        private int maxSlots;
        
        public event Action<int, ItemSlot> OnSlotChanged;
        public event Action<ItemData, int> OnItemAdded;
        public event Action<ItemData, int> OnItemRemoved;
        
        public ItemSlot[] ItemSlots => itemSlots;
        public int MaxSlots => maxSlots;

        public ItemInventory(int slotCount)
        {
            maxSlots = slotCount;
            itemSlots = new ItemSlot[maxSlots];
            
            for (int i = 0; i < maxSlots; i++)
            {
                itemSlots[i] = new ItemSlot(i);
            }
        }

        public bool AddItem(ItemData item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;

            int remainingQuantity = quantity;

            // 既存スロットに追加を試行
            for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
            {
                if (!itemSlots[i].IsEmpty && itemSlots[i].itemData.itemId == item.itemId)
                {
                    int canAdd = Mathf.Min(remainingQuantity, item.maxStackSize - itemSlots[i].quantity);
                    if (canAdd > 0)
                    {
                        itemSlots[i].AddItem(item, canAdd);
                        remainingQuantity -= canAdd;
                        OnSlotChanged?.Invoke(i, itemSlots[i]);
                    }
                }
            }

            // 空のスロットに追加を試行
            for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
            {
                if (itemSlots[i].IsEmpty)
                {
                    int canAdd = Mathf.Min(remainingQuantity, item.maxStackSize);
                    itemSlots[i].AddItem(item, canAdd);
                    remainingQuantity -= canAdd;
                    OnSlotChanged?.Invoke(i, itemSlots[i]);
                }
            }

            int addedQuantity = quantity - remainingQuantity;
            if (addedQuantity > 0)
            {
                OnItemAdded?.Invoke(item, addedQuantity);
            }

            return remainingQuantity == 0;
        }

        public bool RemoveItem(int itemId, int quantity = 1)
        {
            int remainingQuantity = quantity;

            for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
            {
                if (!itemSlots[i].IsEmpty && itemSlots[i].itemData.itemId == itemId)
                {
                    int canRemove = Mathf.Min(remainingQuantity, itemSlots[i].quantity);
                    ItemData removedItem = itemSlots[i].itemData;
                    
                    if (itemSlots[i].RemoveItem(canRemove))
                    {
                        remainingQuantity -= canRemove;
                        OnSlotChanged?.Invoke(i, itemSlots[i]);
                        OnItemRemoved?.Invoke(removedItem, canRemove);
                    }
                }
            }

            return remainingQuantity == 0;
        }

        public int GetItemCount(int itemId)
        {
            int count = 0;
            for (int i = 0; i < maxSlots; i++)
            {
                if (!itemSlots[i].IsEmpty && itemSlots[i].itemData.itemId == itemId)
                {
                    count += itemSlots[i].quantity;
                }
            }
            return count;
        }

        public ItemSlot GetSlot(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < maxSlots)
                return itemSlots[slotIndex];
            return null;
        }

        public bool HasItem(int itemId, int quantity = 1)
        {
            return GetItemCount(itemId) >= quantity;
        }
    }

    // アイテム使用システムメインクラス
    public class ItemUsageSystem : MonoBehaviour
    {
        [Header("アイテムシステム設定")]
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private int inventorySlotCount = 20;
        [SerializeField] private bool allowCombatUsage = true;
        [SerializeField] private bool showUsageEffects = true;

        [Header("効果持続管理")]
        [SerializeField] private int maxActiveEffects = 10;
        
        private BattleManager battleManager;
        private BattleFlowManager battleFlowManager;
        private ItemInventory playerInventory;
        private Dictionary<ItemEffectType, ActiveItemEffect> activeEffects;

        // イベント定義
        public event Action<ItemUsageResult> OnItemUsed;
        public event Action<ItemEffectType, int> OnEffectApplied;
        public event Action<ItemEffectType> OnEffectExpired;

        // プロパティ
        public ItemInventory PlayerInventory => playerInventory;
        public ItemDatabase Database => itemDatabase;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            battleFlowManager = GetComponent<BattleFlowManager>();
            playerInventory = new ItemInventory(inventorySlotCount);
            activeEffects = new Dictionary<ItemEffectType, ActiveItemEffect>();
        }

        private void OnEnable()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged += HandleTurnChanged;
                battleManager.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDisable()
        {
            if (battleManager != null)
            {
                battleManager.OnTurnChanged -= HandleTurnChanged;
                battleManager.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        // ターン変更時の処理
        private void HandleTurnChanged(int turn)
        {
            UpdateActiveEffects();
        }

        // ゲーム状態変更時の処理
        private void HandleGameStateChanged(GameState newState)
        {
            // 必要に応じて状態変更時の処理を実装
        }

        // アイテム使用
        public ItemUsageResult UseItem(int slotIndex)
        {
            ItemUsageResult result = new ItemUsageResult();
            
            ItemSlot slot = playerInventory.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                result.resultMessage = "アイテムが見つかりません";
                return result;
            }

            ItemData item = slot.itemData;
            
            // 戦闘中使用可能性チェック
            if (battleManager.CurrentState != GameState.PlayerTurn && !item.canUseInCombat)
            {
                result.resultMessage = "戦闘中は使用できません";
                return result;
            }

            // アイテム効果の適用
            result = ApplyItemEffects(item);
            
            if (result.wasUsed)
            {
                // 消費アイテムの場合、インベントリから削除
                if (item.isConsumable)
                {
                    playerInventory.RemoveItem(item.itemId, 1);
                    result.itemConsumed = true;
                }

                // 戦闘フローに使用アクションを登録（戦闘中の場合）
                if (battleManager.CurrentState == GameState.PlayerTurn && battleFlowManager != null)
                {
                    BattleAction itemAction = new BattleAction(BattleActionType.ItemUse);
                    itemAction.itemId = item.itemId;
                    battleFlowManager.RegisterPlayerAction(itemAction);
                }

                OnItemUsed?.Invoke(result);

                if (showUsageEffects)
                {
                    Debug.Log($"アイテム使用: {item.itemName} - {result.resultMessage}");
                }
            }

            return result;
        }

        // アイテム効果の適用
        private ItemUsageResult ApplyItemEffects(ItemData item)
        {
            ItemUsageResult result = new ItemUsageResult();
            result.usedItem = item;
            result.appliedEffects = item.effects;
            result.wasUsed = true;

            List<string> effectMessages = new List<string>();

            foreach (ItemEffect effect in item.effects)
            {
                ApplyItemEffect(effect, effectMessages);
            }

            result.resultMessage = string.Join(", ", effectMessages);
            return result;
        }

        // 個別アイテム効果の適用
        private void ApplyItemEffect(ItemEffect effect, List<string> messages)
        {
            PlayerData player = battleManager.PlayerData;

            switch (effect.effectType)
            {
                case ItemEffectType.HealHp:
                    int healAmount = effect.effectValue;
                    int actualHeal = Mathf.Min(healAmount, player.maxHp - player.currentHp);
                    player.Heal(actualHeal);
                    messages.Add($"HP回復 +{actualHeal}");
                    break;

                case ItemEffectType.IncreaseMaxHp:
                    player.maxHp += effect.effectValue;
                    player.currentHp += effect.effectValue; // 最大HP増加分も即座に回復
                    messages.Add($"最大HP増加 +{effect.effectValue}");
                    break;

                case ItemEffectType.AttackPowerBoost:
                    ApplyTimedEffect(effect, messages, "攻撃力アップ");
                    break;

                case ItemEffectType.AttributeBoost:
                    ApplyAttributeBoostEffect(effect, messages);
                    break;

                case ItemEffectType.CriticalRateBoost:
                    ApplyTimedEffect(effect, messages, "クリティカル率アップ");
                    break;

                case ItemEffectType.DefenseBoost:
                    ApplyTimedEffect(effect, messages, "防御力アップ");
                    break;

                case ItemEffectType.SpeedBoost:
                    ApplyTimedEffect(effect, messages, "行動速度アップ");
                    break;

                case ItemEffectType.RemoveStatusEffect:
                    RemoveStatusEffects(effect, messages);
                    break;

                case ItemEffectType.GrantStatusEffect:
                    GrantStatusEffect(effect, messages);
                    break;

                case ItemEffectType.WeaponCooldownReset:
                    ResetWeaponCooldowns(effect, messages);
                    break;
            }

            OnEffectApplied?.Invoke(effect.effectType, effect.effectValue);
        }

        // 時限効果の適用
        private void ApplyTimedEffect(ItemEffect effect, List<string> messages, string effectName)
        {
            if (effect.duration > 0)
            {
                // 時限効果として登録
                ActiveItemEffect activeEffect = new ActiveItemEffect
                {
                    effectType = effect.effectType,
                    value = effect.effectValue,
                    remainingTurns = effect.duration,
                    multiplier = effect.effectMultiplier
                };

                activeEffects[effect.effectType] = activeEffect;
                messages.Add($"{effectName} +{effect.effectValue} ({effect.duration}ターン)");
            }
            else
            {
                // 即時効果
                messages.Add($"{effectName} +{effect.effectValue} (即時)");
            }
        }

        // 属性ブースト効果の適用
        private void ApplyAttributeBoostEffect(ItemEffect effect, List<string> messages)
        {
            string attributeName = GetAttributeDisplayName(effect.targetAttribute);
            ApplyTimedEffect(effect, messages, $"{attributeName}威力アップ");
        }

        // 状態異常回復
        private void RemoveStatusEffects(ItemEffect effect, List<string> messages)
        {
            // 状態異常システムとの連携（後のフェーズで詳細実装）
            messages.Add("状態異常回復");
        }

        // 状態異常付与
        private void GrantStatusEffect(ItemEffect effect, List<string> messages)
        {
            // 状態異常システムとの連携（後のフェーズで詳細実装）
            string attributeName = GetAttributeDisplayName(effect.targetAttribute);
            messages.Add($"{attributeName}耐性付与");
        }

        // 武器クールダウンリセット
        private void ResetWeaponCooldowns(ItemEffect effect, List<string> messages)
        {
            PlayerData player = battleManager.PlayerData;
            int resetCount = 0;

            for (int i = 0; i < player.weaponCooldowns.Length; i++)
            {
                if (player.weaponCooldowns[i] > 0)
                {
                    player.weaponCooldowns[i] = 0;
                    resetCount++;
                }
            }

            if (resetCount > 0)
            {
                messages.Add($"武器クールダウンリセット ({resetCount}個)");
            }
            else
            {
                messages.Add("リセット対象なし");
            }
        }

        // アクティブ効果の更新
        private void UpdateActiveEffects()
        {
            List<ItemEffectType> expiredEffects = new List<ItemEffectType>();

            foreach (var kvp in activeEffects)
            {
                ActiveItemEffect effect = kvp.Value;
                effect.remainingTurns--;

                if (effect.remainingTurns <= 0)
                {
                    expiredEffects.Add(kvp.Key);
                }
                else
                {
                    activeEffects[kvp.Key] = effect;
                }
            }

            // 期限切れ効果の削除
            foreach (ItemEffectType effectType in expiredEffects)
            {
                activeEffects.Remove(effectType);
                OnEffectExpired?.Invoke(effectType);

                if (showUsageEffects)
                {
                    Debug.Log($"アイテム効果終了: {effectType}");
                }
            }
        }

        // アクティブ効果の取得
        public ActiveItemEffect? GetActiveEffect(ItemEffectType effectType)
        {
            if (activeEffects.TryGetValue(effectType, out ActiveItemEffect effect))
                return effect;
            return null;
        }

        // 攻撃力ボーナスの取得
        public int GetAttackPowerBonus()
        {
            if (activeEffects.TryGetValue(ItemEffectType.AttackPowerBoost, out ActiveItemEffect effect))
                return effect.value;
            return 0;
        }

        // 属性ブーストの取得
        public float GetAttributeBoostMultiplier(AttackAttribute attribute)
        {
            // アクティブ効果から該当属性のブーストを検索
            foreach (var effect in activeEffects.Values)
            {
                if (effect.effectType == ItemEffectType.AttributeBoost)
                {
                    // 詳細な属性判定は後のフェーズで実装
                    return effect.multiplier;
                }
            }
            return 1.0f;
        }

        // 属性表示名の取得
        private string GetAttributeDisplayName(AttackAttribute attribute)
        {
            switch (attribute)
            {
                case AttackAttribute.Fire: return "炎";
                case AttackAttribute.Ice: return "氷";
                case AttackAttribute.Thunder: return "雷";
                case AttackAttribute.Wind: return "風";
                case AttackAttribute.Earth: return "土";
                case AttackAttribute.Light: return "光";
                case AttackAttribute.Dark: return "闇";
                default: return "無";
            }
        }

        // デバッグ用：アイテム追加
        [ContextMenu("Add Test Items")]
        public void AddTestItems()
        {
            if (itemDatabase != null && itemDatabase.AllItems.Length > 0)
            {
                foreach (ItemData item in itemDatabase.AllItems)
                {
                    playerInventory.AddItem(item, 3);
                }
                Debug.Log("テストアイテム追加完了");
            }
        }
    }

    // アクティブなアイテム効果
    [Serializable]
    public struct ActiveItemEffect
    {
        public ItemEffectType effectType;
        public int value;
        public int remainingTurns;
        public float multiplier;
    }
}