using UnityEngine;
using UnityEditor;
using BattleSystem;
using System.Collections.Generic;

/// <summary>
/// AttachmentDatabase.asset作成用のエディタースクリプト
/// 全15種類のアタッチメントに対応するコンボが割り当てられたデータベースを生成
/// </summary>
public class AttachmentDatabaseCreator : EditorWindow
{
    [MenuItem("Tools/Battle System/Create Attachment Database")]
    public static void CreateAttachmentDatabase()
    {
        // AttachmentDatabase ScriptableObjectのインスタンスを作成
        AttachmentDatabase database = ScriptableObject.CreateInstance<AttachmentDatabase>();
        
        // OnEnable相当の初期化を手動実行
        var onEnableMethod = typeof(AttachmentDatabase).GetMethod("OnEnable", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        onEnableMethod?.Invoke(database, null);

        // アセットとして保存
        string assetPath = "Assets/Data/MainAttachmentDatabase.asset";
        
        // Dataフォルダが存在しない場合は作成
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        
        AssetDatabase.CreateAsset(database, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 作成されたアセットを選択
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = database;

        // デバッグ情報表示
        DisplayAttachmentComboMapping(database);
        
        Debug.Log($"AttachmentDatabase created successfully with {database.PresetAttachments.Length} attachments!");
        Debug.Log($"Asset saved at: {assetPath}");
    }

    /// <summary>
    /// アタッチメントとコンボの対応表をコンソールに表示
    /// </summary>
    private static void DisplayAttachmentComboMapping(AttachmentDatabase database)
    {
        Dictionary<string, string> mapping = database.GetAttachmentComboMapping();
        
        Debug.Log("=== アタッチメント - コンボ対応表 ===");
        foreach (var kvp in mapping)
        {
            Debug.Log($"🔧 {kvp.Key} → 🎯 {kvp.Value}");
        }
        Debug.Log("=====================================");
    }

    /// <summary>
    /// 詳細情報表示用メニュー
    /// </summary>
    [MenuItem("Tools/Battle System/Show Attachment-Combo Mapping")]
    public static void ShowAttachmentComboMapping()
    {
        // 既存のAttachmentDatabaseを探す
        string[] guids = AssetDatabase.FindAssets("t:AttachmentDatabase");
        if (guids.Length == 0)
        {
            Debug.LogWarning("AttachmentDatabase not found. Please create it first using 'Create Attachment Database'.");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        AttachmentDatabase database = AssetDatabase.LoadAssetAtPath<AttachmentDatabase>(path);
        
        if (database != null)
        {
            DisplayAttachmentComboMapping(database);
        }
        else
        {
            Debug.LogError("Failed to load AttachmentDatabase.");
        }
    }
}