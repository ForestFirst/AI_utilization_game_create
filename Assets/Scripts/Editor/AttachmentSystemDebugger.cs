using UnityEngine;
using UnityEditor;
using BattleSystem;

/// <summary>
/// AttachmentSystem用のデバッグツール
/// Unityエディターメニューから装備中のアタッチメント情報を表示
/// </summary>
public class AttachmentSystemDebugger : EditorWindow
{
    [MenuItem("Tools/Battle System/Show Equipped Attachments")]
    public static void ShowEquippedAttachments()
    {
        // 現在のシーンでAttachmentSystemを探す
        AttachmentSystem attachmentSystem = FindObjectOfType<AttachmentSystem>();
        
        if (attachmentSystem == null)
        {
            Debug.LogWarning("🔍 AttachmentSystemが見つかりません。シーンにAttachmentSystemが存在することを確認してください。");
            
            // BattleManagerからAttachmentSystemを探す
            BattleManager battleManager = FindObjectOfType<BattleManager>();
            if (battleManager != null)
            {
                attachmentSystem = battleManager.GetComponent<AttachmentSystem>();
                if (attachmentSystem == null)
                {
                    Debug.LogWarning("🔍 BattleManager上にもAttachmentSystemが見つかりません。");
                    return;
                }
                else
                {
                    Debug.Log("✅ BattleManager上でAttachmentSystemを発見しました。");
                }
            }
            else
            {
                return;
            }
        }
        
        Debug.Log("🔧 AttachmentSystemを発見しました。装備中のアタッチメントを表示します...");
        
        // 装備中のアタッチメントを表示
        attachmentSystem.DisplayEquippedAttachments();
    }

    [MenuItem("Tools/Battle System/Attach Random Attachment")]
    public static void AttachRandomAttachment()
    {
        // 現在のシーンでAttachmentSystemを探す
        AttachmentSystem attachmentSystem = FindObjectOfType<AttachmentSystem>();
        
        if (attachmentSystem == null)
        {
            Debug.LogWarning("🔍 AttachmentSystemが見つかりません。");
            return;
        }
        
        // ランダムアタッチメントを装着
        attachmentSystem.AttachRandomAttachment();
        
        // 結果を表示
        Debug.Log("🎲 ランダムアタッチメント装着後の状態:");
        attachmentSystem.DisplayEquippedAttachments();
    }

    [MenuItem("Tools/Battle System/Generate Attachment Options")]
    public static void GenerateAttachmentOptions()
    {
        // 現在のシーンでAttachmentSystemを探す
        AttachmentSystem attachmentSystem = FindObjectOfType<AttachmentSystem>();
        
        if (attachmentSystem == null)
        {
            Debug.LogWarning("🔍 AttachmentSystemが見つかりません。");
            return;
        }
        
        // アタッチメント選択肢を生成・表示
        attachmentSystem.GenerateOptionsForDebug();
    }

    [MenuItem("Tools/Battle System/Clear All Attachments")]
    public static void ClearAllAttachments()
    {
        // 現在のシーンでAttachmentSystemを探す
        AttachmentSystem attachmentSystem = FindObjectOfType<AttachmentSystem>();
        
        if (attachmentSystem == null)
        {
            Debug.LogWarning("🔍 AttachmentSystemが見つかりません。");
            return;
        }
        
        // 全てのアタッチメントを取り外し
        var attachedAttachments = attachmentSystem.GetAttachedAttachments();
        if (attachedAttachments.Count == 0)
        {
            Debug.Log("💡 装備中のアタッチメントはありません。");
            return;
        }
        
        for (int i = 0; i < attachmentSystem.AttachmentSlots.Count; i++)
        {
            if (!attachmentSystem.AttachmentSlots[i].IsEmpty)
            {
                attachmentSystem.DetachAttachment(i);
            }
        }
        
        Debug.Log("🗑️ 全てのアタッチメントを取り外しました。");
        attachmentSystem.DisplayEquippedAttachments();
    }

    [MenuItem("Tools/Battle System/Attachment System Status")]
    public static void ShowAttachmentSystemStatus()
    {
        // 現在のシーンでAttachmentSystemを探す
        AttachmentSystem attachmentSystem = FindObjectOfType<AttachmentSystem>();
        
        Debug.Log("=== AttachmentSystem ステータス ===");
        
        if (attachmentSystem == null)
        {
            Debug.Log("❌ AttachmentSystem: 見つかりません");
            return;
        }
        
        Debug.Log("✅ AttachmentSystem: 発見");
        Debug.Log($"📦 最大スロット数: {attachmentSystem.AttachmentSlots.Count}");
        Debug.Log($"🔗 データベース: {(attachmentSystem.Database != null ? "設定済み" : "未設定")}");
        
        if (attachmentSystem.Database != null)
        {
            Debug.Log($"📋 利用可能アタッチメント数: {attachmentSystem.Database.PresetAttachments.Length}");
        }
        
        int equippedCount = attachmentSystem.GetAttachedAttachments().Count;
        Debug.Log($"⚡ 装備中アタッチメント数: {equippedCount}");
        
        Debug.Log("===================================");
    }
}