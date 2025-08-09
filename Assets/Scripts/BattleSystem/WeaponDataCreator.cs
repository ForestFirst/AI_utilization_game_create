using UnityEngine;

namespace BattleSystem
{
    // サンプル武器データ作成用のスクリプト
    public class WeaponDataCreator : MonoBehaviour
    {
        [ContextMenu("Create Sample Weapon Database")]
        public void CreateSampleWeaponDatabase()
        {
            // WeaponDatabaseアセットを作成
            WeaponDatabase database = ScriptableObject.CreateInstance<WeaponDatabase>();
            
            // サンプル武器データを作成
            WeaponData[] sampleWeapons = new WeaponData[]
            {
                // 炎の剣
                new WeaponData("炎の剣", AttackAttribute.Fire, WeaponType.Sword, 120, AttackRange.SingleFront)
                {
                    criticalRate = 15,
                    cooldownTurns = 0,
                    specialEffect = "炎上効果",
                    effectValue = 20,
                    effectDuration = 2
                },
                
                // 氷の斧
                new WeaponData("氷の斧", AttackAttribute.Ice, WeaponType.Axe, 95, AttackRange.SingleFront)
                {
                    criticalRate = 25,
                    cooldownTurns = 1,
                    specialEffect = "凍結効果",
                    effectValue = 15,
                    effectDuration = 1
                },
                
                // 雷槍
                new WeaponData("雷槍", AttackAttribute.Thunder, WeaponType.Spear, 110, AttackRange.SingleTarget)
                {
                    criticalRate = 20,
                    cooldownTurns = 0,
                    specialEffect = "麻痺効果",
                    effectValue = 10,
                    effectDuration = 2
                },
                
                // 大剣
                new WeaponData("大剣", AttackAttribute.None, WeaponType.Sword, 140, AttackRange.SingleFront)
                {
                    criticalRate = 10,
                    cooldownTurns = 2,
                    specialEffect = "高威力攻撃",
                    effectValue = 0,
                    effectDuration = 0
                }
            };
            
            // リフレクションを使用してプライベートフィールドに値を設定
            var weaponsField = typeof(WeaponDatabase).GetField("weapons", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            weaponsField.SetValue(database, sampleWeapons);
            
            // アセットを保存
            string path = "Assets/WeaponDatabase.asset";
            
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(database, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            
            Debug.Log($"Sample WeaponDatabase created at: {path}");
            Debug.Log($"Created {sampleWeapons.Length} sample weapons:");
            
            foreach (var weapon in sampleWeapons)
            {
                Debug.Log($"  - {weapon.weaponName} ({weapon.attackAttribute}): Power {weapon.basePower}, Crit {weapon.criticalRate}%");
            }
#endif
        }
        
        [ContextMenu("Create Sample Enemy Database")]
        public void CreateSampleEnemyDatabase()
        {
            // EnemyDatabaseの作成（EnemyDataが存在する場合）
            Debug.Log("Sample Enemy Database creation - EnemyData implementation needed");
        }
    }
}
