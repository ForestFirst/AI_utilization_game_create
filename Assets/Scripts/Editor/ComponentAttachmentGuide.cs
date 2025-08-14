using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using BattleSystem;

/// <summary>
/// オブジェクトにアタッチするコンポーネントの一覧表示ツール
/// </summary>
public class ComponentAttachmentGuide : EditorWindow
{
    [MenuItem("Tools/Battle System/Show Component Attachment Guide")]
    public static void ShowComponentAttachmentGuide()
    {
        DisplayComponentAttachmentGuide();
    }

    [MenuItem("Tools/Battle System/Show Scene Objects")]
    public static void ShowSceneObjects()
    {
        DisplaySceneObjectsWithComponents();
    }

    [MenuItem("Tools/Battle System/Setup Recommended Components")]
    public static void SetupRecommendedComponents()
    {
        SetupBattleSystemComponents();
    }

    /// <summary>
    /// バトルシステム用コンポーネントのアタッチガイドを表示
    /// </summary>
    public static void DisplayComponentAttachmentGuide()
    {
        Debug.Log("=== バトルシステム コンポーネントアタッチガイド ===");
        
        Debug.Log("🎮 【必須コンポーネント構成】");
        Debug.Log("");
        
        // 1. BattleManager
        Debug.Log("📋 BattleManager (メインゲームオブジェクト)");
        Debug.Log("   🔗 アタッチするコンポーネント:");
        Debug.Log("      • BattleManager (必須)");
        Debug.Log("      • AttachmentSystem (必須)");
        Debug.Log("      • ComboSystem (推奨)");
        Debug.Log("   📍 配置: シーンのルートレベル");
        Debug.Log("");
        
        // 2. Canvas UI
        Debug.Log("🖼️ Canvas (UI表示用)");
        Debug.Log("   🔗 アタッチするコンポーネント:");
        Debug.Log("      • Canvas (必須)");
        Debug.Log("      • CanvasScaler (必須)");
        Debug.Log("      • GraphicRaycaster (必須)");
        Debug.Log("      • SimpleBattleUI (必須)");
        Debug.Log("      • AttachmentSelectionUI (推奨)");
        Debug.Log("   📍 配置: UI専用オブジェクト");
        Debug.Log("");
        
        // 3. EventSystem
        Debug.Log("⚡ EventSystem (UI操作用)");
        Debug.Log("   🔗 アタッチするコンポーネント:");
        Debug.Log("      • EventSystem (必須)");
        Debug.Log("      • StandaloneInputModule (必須)");
        Debug.Log("   📍 配置: シーンに1つだけ");
        Debug.Log("");
        
        // 4. Camera
        Debug.Log("📷 Main Camera (表示用)");
        Debug.Log("   🔗 アタッチするコンポーネント:");
        Debug.Log("      • Camera (必須)");
        Debug.Log("      • AudioListener (必須)");
        Debug.Log("   📍 配置: シーンビュー用");
        Debug.Log("");
        
        // 5. Auto Creator
        Debug.Log("🔧 AutoBattleUICreator (自動セットアップ用)");
        Debug.Log("   🔗 アタッチするコンポーネント:");
        Debug.Log("      • AutoBattleUICreator (オプション)");
        Debug.Log("   📍 配置: 自動UI生成が必要な場合");
        Debug.Log("");
        
        Debug.Log("🎯 【推奨セットアップ手順】");
        Debug.Log("1. 空のGameObjectを作成し「BattleManager」と命名");
        Debug.Log("2. BattleManager、AttachmentSystemコンポーネントをアタッチ");
        Debug.Log("3. UI Canvasを作成しSimpleBattleUIをアタッチ");
        Debug.Log("4. EventSystemが存在することを確認");
        Debug.Log("5. Tools > Battle System > Setup Recommended Components で自動セットアップ");
        
        Debug.Log("================================================");
    }

    /// <summary>
    /// 現在のシーン内オブジェクトとコンポーネントを表示
    /// </summary>
    public static void DisplaySceneObjectsWithComponents()
    {
        Debug.Log("=== シーン内オブジェクト・コンポーネント一覧 ===");
        
        // 全てのゲームオブジェクトを取得
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        // バトルシステム関連コンポーネントをフィルタリング
        var relevantObjects = new List<(GameObject obj, Component[] components)>();
        
        foreach (GameObject obj in allObjects)
        {
            Component[] battleComponents = obj.GetComponents<Component>()
                .Where(comp => IsBattleSystemComponent(comp))
                .ToArray();
                
            if (battleComponents.Length > 0 || obj.name.Contains("Battle") || 
                obj.name.Contains("Canvas") || obj.name.Contains("Manager"))
            {
                relevantObjects.Add((obj, obj.GetComponents<Component>()));
            }
        }
        
        if (relevantObjects.Count == 0)
        {
            Debug.Log("❌ バトルシステム関連のオブジェクトが見つかりませんでした");
            Debug.Log("💡 Setup Recommended Components を実行してください");
            return;
        }
        
        foreach (var (obj, components) in relevantObjects)
        {
            Debug.Log($"🎯 {obj.name} {GetObjectStatusIcon(obj)}");
            Debug.Log($"   📍 階層: {GetObjectHierarchyPath(obj)}");
            Debug.Log($"   🔗 アタッチ済みコンポーネント:");
            
            foreach (Component component in components)
            {
                if (component != null)
                {
                    string icon = GetComponentIcon(component);
                    string status = IsBattleSystemComponent(component) ? " ⭐" : "";
                    Debug.Log($"      {icon} {component.GetType().Name}{status}");
                }
            }
            
            // 推奨コンポーネントのチェック
            var missingComponents = GetMissingRecommendedComponents(obj);
            if (missingComponents.Count > 0)
            {
                Debug.Log($"   ⚠️ 推奨コンポーネント未追加:");
                foreach (string missing in missingComponents)
                {
                    Debug.Log($"      🔸 {missing}");
                }
            }
            
            Debug.Log("");
        }
        
        Debug.Log("================================================");
    }

    /// <summary>
    /// 推奨コンポーネントを自動セットアップ
    /// </summary>
    public static void SetupBattleSystemComponents()
    {
        Debug.Log("🔧 バトルシステムコンポーネント自動セットアップ開始...");
        
        // BattleManagerの作成/確認
        SetupBattleManager();
        
        // Canvasの作成/確認
        SetupCanvas();
        
        // EventSystemの作成/確認
        SetupEventSystem();
        
        Debug.Log("✅ バトルシステムコンポーネントセットアップ完了！");
        
        // 結果表示
        DisplaySceneObjectsWithComponents();
    }

    private static void SetupBattleManager()
    {
        BattleManager battleManager = Object.FindObjectOfType<BattleManager>();
        if (battleManager == null)
        {
            GameObject battleManagerObj = new GameObject("BattleManager");
            battleManager = battleManagerObj.AddComponent<BattleManager>();
            Debug.Log("📋 BattleManager作成完了");
        }
        
        // AttachmentSystemの確認
        AttachmentSystem attachmentSystem = battleManager.GetComponent<AttachmentSystem>();
        if (attachmentSystem == null)
        {
            battleManager.gameObject.AddComponent<AttachmentSystem>();
            Debug.Log("🔗 AttachmentSystem追加完了");
        }
    }

    private static void SetupCanvas()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            // CanvasScalerとGraphicRaycasterを文字列で追加（型解決の問題を回避）
            var canvasScalerType = System.Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
            var graphicRaycasterType = System.Type.GetType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");
            
            if (canvasScalerType != null)
                canvasObj.AddComponent(canvasScalerType);
            if (graphicRaycasterType != null)
                canvasObj.AddComponent(graphicRaycasterType);
            Debug.Log("🖼️ Canvas作成完了");
        }
        
        // SimpleBattleUIの確認
        SimpleBattleUI simpleBattleUI = canvas.GetComponent<SimpleBattleUI>();
        if (simpleBattleUI == null)
        {
            canvas.gameObject.AddComponent<SimpleBattleUI>();
            Debug.Log("🎮 SimpleBattleUI追加完了");
        }
    }

    private static void SetupEventSystem()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("⚡ EventSystem作成完了");
        }
    }

    private static bool IsBattleSystemComponent(Component component)
    {
        if (component == null) return false;
        
        string typeName = component.GetType().Name;
        return typeName.Contains("Battle") || 
               typeName.Contains("Attachment") || 
               typeName.Contains("Combo") ||
               typeName == "Canvas" ||
               typeName == "EventSystem";
    }

    private static string GetComponentIcon(Component component)
    {
        if (component == null) return "❓";
        
        string typeName = component.GetType().Name;
        return typeName switch
        {
            "BattleManager" => "📋",
            "AttachmentSystem" => "🔗",
            "SimpleBattleUI" => "🎮",
            "AttachmentSelectionUI" => "🎯",
            "Canvas" => "🖼️",
            "EventSystem" => "⚡",
            "Camera" => "📷",
            "Transform" => "📍",
            "RectTransform" => "🔲",
            _ => "🔧"
        };
    }

    private static string GetObjectStatusIcon(GameObject obj)
    {
        return obj.activeInHierarchy ? "🟢" : "🔴";
    }

    private static string GetObjectHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + " / " + path;
            parent = parent.parent;
        }
        return path;
    }

    private static List<string> GetMissingRecommendedComponents(GameObject obj)
    {
        var missing = new List<string>();
        
        if (obj.name.Contains("BattleManager") || obj.GetComponent<BattleManager>())
        {
            if (!obj.GetComponent<AttachmentSystem>())
                missing.Add("AttachmentSystem");
        }
        
        if (obj.name.Contains("Canvas") || obj.GetComponent<Canvas>())
        {
            if (!obj.GetComponent<SimpleBattleUI>())
                missing.Add("SimpleBattleUI");
            // 型解決の問題を回避するため、文字列比較で確認
            var canvasScalerType = System.Type.GetType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
            var graphicRaycasterType = System.Type.GetType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");
            
            if (canvasScalerType != null && !obj.GetComponent(canvasScalerType))
                missing.Add("CanvasScaler");
            if (graphicRaycasterType != null && !obj.GetComponent(graphicRaycasterType))
                missing.Add("GraphicRaycaster");
        }
        
        return missing;
    }
}