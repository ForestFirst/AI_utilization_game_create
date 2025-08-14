using UnityEngine;
using UnityEditor;
using System.Linq;
using BattleSystem;

/// <summary>
/// バトルシステムセットアップ支援ツール
/// </summary>
public class BattleSystemSetupHelper : EditorWindow
{
    [MenuItem("Tools/Battle System/Complete Setup Guide", priority = 0)]
    public static void ShowCompleteSetupGuide()
    {
        Debug.Log("=== バトルシステム完全セットアップガイド ===");
        Debug.Log("");
        
        Debug.Log("🎮 【ステップ1: 基本コンポーネントの確認】");
        ComponentAttachmentGuide.DisplayComponentAttachmentGuide();
        Debug.Log("");
        
        Debug.Log("🔍 【ステップ2: 現在のシーン状態確認】");
        ComponentAttachmentGuide.DisplaySceneObjectsWithComponents();
        Debug.Log("");
        
        Debug.Log("📋 【ステップ3: 不足コンポーネントの分析】");
        AnalyzeMissingComponents();
        Debug.Log("");
        
        Debug.Log("💡 次のステップ: 'Setup Recommended Components' を実行してください");
        Debug.Log("================================================");
    }

    [MenuItem("Tools/Battle System/Quick Setup All", priority = 1)]
    public static void QuickSetupAll()
    {
        Debug.Log("🚀 バトルシステム クイックセットアップ開始...");
        
        // 1. 基本コンポーネントセットアップ
        ComponentAttachmentGuide.SetupBattleSystemComponents();
        
        // 2. データベース作成（複数の手法で検索）
        bool hasAttachmentDB = AssetDatabase.FindAssets("t:AttachmentDatabase").Any() ||
                               AssetDatabase.FindAssets("MainAttachmentDatabase").Any() ||
                               System.IO.File.Exists("Assets/Data/MainAttachmentDatabase.asset");
        bool hasCombooDB = AssetDatabase.FindAssets("t:ComboDatabase").Any() ||
                           AssetDatabase.FindAssets("MainComboDatabase").Any() ||
                           System.IO.File.Exists("Assets/Data/MainComboDatabase.asset");
        
        if (!hasAttachmentDB)
        {
            Debug.Log("📦 AttachmentDatabase not found. Creating...");
            AttachmentDatabaseCreator.CreateAttachmentDatabase();
        }
        
        if (!hasCombooDB)
        {
            Debug.Log("🎯 ComboDatabase not found. Creating...");
            ComboDatabaseCreator.CreateComboDatabase();
        }
        
        // 作成後再確認（複数の手法で検索）
        hasAttachmentDB = AssetDatabase.FindAssets("t:AttachmentDatabase").Any() ||
                          AssetDatabase.FindAssets("MainAttachmentDatabase").Any() ||
                          System.IO.File.Exists("Assets/Data/MainAttachmentDatabase.asset");
        hasCombooDB = AssetDatabase.FindAssets("t:ComboDatabase").Any() ||
                      AssetDatabase.FindAssets("MainComboDatabase").Any() ||
                      System.IO.File.Exists("Assets/Data/MainComboDatabase.asset");
        
        Debug.Log($"📊 データベース作成結果:");
        Debug.Log($"   📦 AttachmentDatabase: {(hasAttachmentDB ? "✅作成済み" : "❌未作成")}");
        Debug.Log($"   🎯 ComboDatabase: {(hasCombooDB ? "✅作成済み" : "❌未作成")}");
        
        Debug.Log("✅ クイックセットアップ完了！");
        Debug.Log("🎯 バトルシステムの動作準備が整いました");
    }

    private static void AnalyzeMissingComponents()
    {
        bool hasBattleManager = Object.FindObjectOfType<BattleManager>() != null;
        bool hasAttachmentSystem = Object.FindObjectOfType<AttachmentSystem>() != null;
        bool hasCanvas = Object.FindObjectOfType<Canvas>() != null;
        bool hasSimpleBattleUI = Object.FindObjectOfType<SimpleBattleUI>() != null;
        bool hasEventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null;
        
        Debug.Log("📊 【コンポーネント存在チェック】");
        Debug.Log($"   📋 BattleManager: {GetStatusIcon(hasBattleManager)}");
        Debug.Log($"   🔗 AttachmentSystem: {GetStatusIcon(hasAttachmentSystem)}");
        Debug.Log($"   🖼️ Canvas: {GetStatusIcon(hasCanvas)}");
        Debug.Log($"   🎮 SimpleBattleUI: {GetStatusIcon(hasSimpleBattleUI)}");
        Debug.Log($"   ⚡ EventSystem: {GetStatusIcon(hasEventSystem)}");
        Debug.Log("");
        
        // データベース確認（複数の手法で検索）
        var attachmentTypeAssets = AssetDatabase.FindAssets("t:AttachmentDatabase");
        var attachmentNameAssets = AssetDatabase.FindAssets("MainAttachmentDatabase");
        var comboTypeAssets = AssetDatabase.FindAssets("t:ComboDatabase");
        var comboNameAssets = AssetDatabase.FindAssets("MainComboDatabase");
        
        // ファイルシステムレベルでの直接確認
        bool attachmentFileExists = System.IO.File.Exists("Assets/Data/MainAttachmentDatabase.asset");
        bool comboFileExists = System.IO.File.Exists("Assets/Data/MainComboDatabase.asset");
        
        Debug.Log($"🔍 【デバッグ】検索結果詳細:");
        Debug.Log($"   AttachmentDatabase (型検索): {attachmentTypeAssets.Length}個");
        Debug.Log($"   MainAttachmentDatabase (名前検索): {attachmentNameAssets.Length}個");
        Debug.Log($"   MainAttachmentDatabase (ファイル確認): {attachmentFileExists}");
        Debug.Log($"   ComboDatabase (型検索): {comboTypeAssets.Length}個");
        Debug.Log($"   MainComboDatabase (名前検索): {comboNameAssets.Length}個");
        Debug.Log($"   MainComboDatabase (ファイル確認): {comboFileExists}");
        
        bool hasAttachmentDB = attachmentTypeAssets.Length > 0 || attachmentNameAssets.Length > 0 || attachmentFileExists;
        bool hasCombooDB = comboTypeAssets.Length > 0 || comboNameAssets.Length > 0 || comboFileExists;
        
        Debug.Log("💾 【データベース存在チェック】");
        Debug.Log($"   🔧 AttachmentDatabase: {GetStatusIcon(hasAttachmentDB)}");
        Debug.Log($"   🎯 ComboDatabase: {GetStatusIcon(hasCombooDB)}");
        Debug.Log("");
        
        // 推奨アクション
        if (!hasBattleManager || !hasAttachmentSystem || !hasCanvas || !hasSimpleBattleUI || !hasEventSystem)
        {
            Debug.Log("⚠️ 【不足コンポーネントあり】");
            Debug.Log("   💡 'Setup Recommended Components' を実行してください");
        }
        
        if (!hasAttachmentDB || !hasCombooDB)
        {
            Debug.Log("⚠️ 【不足データベースあり】");
            Debug.Log("   💡 'Quick Setup All' を実行してください");
        }
        
        if (hasBattleManager && hasAttachmentSystem && hasCanvas && 
            hasSimpleBattleUI && hasEventSystem && hasAttachmentDB && hasCombooDB)
        {
            Debug.Log("✅ 【セットアップ完了】");
            Debug.Log("   🎉 すべてのコンポーネントとデータベースが準備できています！");
        }
    }

    private static string GetStatusIcon(bool exists)
    {
        return exists ? "✅ 存在" : "❌ 不足";
    }

    [MenuItem("Tools/Battle System/Show All Available Tools", priority = 100)]
    public static void ShowAllAvailableTools()
    {
        Debug.Log("=== バトルシステム利用可能ツール一覧 ===");
        Debug.Log("");
        
        Debug.Log("🔧 【セットアップツール】");
        Debug.Log("   • Complete Setup Guide - 完全セットアップガイド");
        Debug.Log("   • Quick Setup All - クイックセットアップ");
        Debug.Log("   • Setup Recommended Components - 推奨コンポーネント設定");
        Debug.Log("");
        
        Debug.Log("📋 【表示・確認ツール】");
        Debug.Log("   • Show Component Attachment Guide - コンポーネントアタッチガイド");
        Debug.Log("   • Show Scene Objects - シーンオブジェクト表示");
        Debug.Log("   • Show Equipped Attachments - 装備中アタッチメント表示");
        Debug.Log("");
        
        Debug.Log("💾 【データベースツール】");
        Debug.Log("   • Create Attachment Database - アタッチメントDB作成");
        Debug.Log("   • Create Combo Database - コンボDB作成");
        Debug.Log("   • Show Attachment-Combo Mapping - アタッチメント・コンボ対応表");
        Debug.Log("");
        
        Debug.Log("🎮 【デバッグツール】");
        Debug.Log("   • Attach Random Attachment - ランダムアタッチメント装着");
        Debug.Log("   • Generate Attachment Options - アタッチメント選択肢生成");
        Debug.Log("   • Clear All Attachments - 全アタッチメント取り外し");
        Debug.Log("   • Attachment System Status - アタッチメントシステム状態");
        Debug.Log("");
        
        Debug.Log("📍 全てのツールは 'Tools > Battle System' メニューからアクセス可能です");
        Debug.Log("================================================");
    }
}