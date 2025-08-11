using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem
{
    /// <summary>
    /// ゲーム開始時に自動で戦闘UIを作成するマネージャー
    /// 手札システム対応版 - HandUIとHandSystemも自動作成
    /// 任意のGameObjectにアタッチするだけでUIが表示されます
    /// </summary>
    public class AutoBattleUICreator : MonoBehaviour
    {
        [Header("Auto UI Settings")]
        [SerializeField] private bool createOnAwake = true;
        [SerializeField] private bool destroyAfterCreation = false;
        [SerializeField] private bool enableHandSystem = true; // 手札システムの有効化
        
        private void Awake()
        {
            if (createOnAwake)
            {
                CreateBattleUISystem();
                
                if (destroyAfterCreation)
                {
                    Destroy(this);
                }
            }
        }
        
        [ContextMenu("Create Battle UI System")]
        public void CreateBattleUISystem()
        {
            Debug.Log("=== Auto Battle UI Creator Starting (Hand System Edition) ===");
            
            // 1. Canvasの確認・作成
            Canvas canvas = EnsureCanvas();
            
            // 2. EventSystemの確認・作成
            EnsureEventSystem();
            
            // 3. BattleManagerの確認・作成
            BattleManager battleManager = EnsureBattleManager();
            
            // 4. HandSystemの確認・作成（手札システム）
            HandSystem handSystem = null;
            if (enableHandSystem)
            {
                handSystem = EnsureHandSystem(battleManager);
            }
            
            // 5. SimpleBattleUIの追加
            SimpleBattleUI battleUI = EnsureSimpleBattleUI(canvas);
            
            // 6. HandUIの追加（手札UI）
            HandUI handUI = null;
            if (enableHandSystem)
            {
                handUI = EnsureHandUI(canvas);
            }
            
            Debug.Log("=== Battle UI System Ready (Hand System Edition)! ===");
            Debug.Log($"Components created:");
            Debug.Log($"- Canvas: {canvas.name}");
            Debug.Log($"- BattleManager: {battleManager.name}");
            Debug.Log($"- SimpleBattleUI: {battleUI.name}");
            if (enableHandSystem)
            {
                Debug.Log($"- HandSystem: {handSystem?.name ?? "Failed"}");
                Debug.Log($"- HandUI: {handUI?.name ?? "Failed"}");
            }
            Debug.Log("Press Play to see the battle UI with hand system!");
        }
        
        private Canvas EnsureCanvas()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.Log("Creating new Canvas...");
                GameObject canvasObj = new GameObject("Battle UI Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            else
            {
                Debug.Log("Using existing Canvas");
            }
            
            return canvas;
        }
        
        private void EnsureEventSystem()
        {
            UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                Debug.Log("Creating EventSystem...");
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            else
            {
                Debug.Log("EventSystem already exists");
            }
        }
        
        private BattleManager EnsureBattleManager()
        {
            BattleManager battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                GameObject bmObj = new GameObject("BattleManager");
                battleManager = bmObj.AddComponent<BattleManager>();
                Debug.Log("BattleManager created");
            }
            else
            {
                Debug.Log("Using existing BattleManager");
            }
            
            return battleManager;
        }
        
        private HandSystem EnsureHandSystem(BattleManager battleManager)
        {
            HandSystem handSystem = FindObjectOfType<HandSystem>();
            if (handSystem == null)
            {
                // BattleManagerと同じGameObjectに追加
                handSystem = battleManager.gameObject.AddComponent<HandSystem>();
                Debug.Log("HandSystem component added to BattleManager");
            }
            else
            {
                Debug.Log("Using existing HandSystem");
            }
            
            return handSystem;
        }
        
        private SimpleBattleUI EnsureSimpleBattleUI(Canvas canvas)
        {
            SimpleBattleUI battleUI = canvas.GetComponent<SimpleBattleUI>();
            if (battleUI == null)
            {
                battleUI = canvas.gameObject.AddComponent<SimpleBattleUI>();
                Debug.Log("SimpleBattleUI component added to Canvas");
            }
            else
            {
                Debug.Log("Using existing SimpleBattleUI");
            }
            
            return battleUI;
        }
        
        private HandUI EnsureHandUI(Canvas canvas)
        {
            HandUI handUI = FindObjectOfType<HandUI>();
            if (handUI == null)
            {
                // 専用のGameObjectを作成してHandUIを追加
                GameObject handUIObj = new GameObject("HandUI");
                handUI = handUIObj.AddComponent<HandUI>();
                Debug.Log("HandUI component created on new GameObject");
            }
            else
            {
                Debug.Log("Using existing HandUI");
            }
            
            // 🔧 安全な方法: 直接フラグを設定して初期状態を制御
            if (Application.isPlaying)
            {
                // プレイモード中は遅延実行
                Invoke(nameof(SafeHideHandUI), 0.1f);
            }
            
            return handUI;
        }
        
        /// <summary>
        /// 安全な方法でHandUIを非表示にする
        /// </summary>
        private void SafeHideHandUI()
        {
            HandUI handUI = FindObjectOfType<HandUI>();
            if (handUI != null)
            {
                try
                {
                    // SetHandUIVisibleメソッドを呼び出す
                    var method = handUI.GetType().GetMethod("SetHandUIVisible", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (method != null)
                    {
                        method.Invoke(handUI, new object[] { false });
                        Debug.Log("✅ HandUI safely hidden using reflection");
                    }
                    else
                    {
                        Debug.LogWarning("SetHandUIVisible method not found, using fallback...");
                        HideHandUIFallback();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Failed to hide HandUI safely: {ex.Message}");
                    HideHandUIFallback();
                }
            }
        }
        
        /// <summary>
        /// フォールバック: 手札関連オブジェクトを直接制御
        /// </summary>
        private void HideHandUIFallback()
        {
            // Canvasの子要素から手札関連UIを探して非表示にする
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Transform[] allChildren = canvas.GetComponentsInChildren<Transform>(true);
                foreach (Transform child in allChildren)
                {
                    string name = child.name.ToLower();
                    if (name.Contains("hand") || name.Contains("card") || 
                        name.Contains("フィールド") == false) // 戦場以外
                    {
                        if (child.gameObject != canvas.gameObject) // Canvas自体は除外
                        {
                            child.gameObject.SetActive(false);
                            Debug.Log($"✅ Fallback: Hidden {child.name}");
                        }
                    }
                }
            }
        }
        
        // Unityエディタのメニューからも実行可能
        [UnityEngine.ContextMenu("Force Create UI Now")]
        public void ForceCreateUIImmediate()
        {
            CreateBattleUISystem();
            
            // プレイモード中なら即座にUIを表示
            if (Application.isPlaying)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                SimpleBattleUI battleUI = canvas?.GetComponent<SimpleBattleUI>();
                if (battleUI != null)
                {
                    battleUI.RecreateUI();
                }
                
                // HandUIの強制更新
                HandUI handUI = FindObjectOfType<HandUI>();
                if (handUI != null)
                {
                    handUI.ForceUpdateHandDisplay();
                }
            }
        }
        
        // 手札システムの有効/無効を切り替え
        [ContextMenu("Toggle Hand System")]
        public void ToggleHandSystem()
        {
            enableHandSystem = !enableHandSystem;
            Debug.Log($"Hand System enabled: {enableHandSystem}");
            
            if (Application.isPlaying)
            {
                if (enableHandSystem)
                {
                    // 手札システムを有効化
                    BattleManager battleManager = FindObjectOfType<BattleManager>();
                    if (battleManager != null)
                    {
                        EnsureHandSystem(battleManager);
                        Canvas canvas = FindObjectOfType<Canvas>();
                        if (canvas != null)
                        {
                            EnsureHandUI(canvas);
                        }
                    }
                }
                else
                {
                    // 手札システムを無効化
                    HandSystem handSystem = FindObjectOfType<HandSystem>();
                    if (handSystem != null)
                    {
                        Destroy(handSystem);
                    }
                    
                    HandUI handUI = FindObjectOfType<HandUI>();
                    if (handUI != null)
                    {
                        Destroy(handUI.gameObject);
                    }
                }
            }
        }
        
        // デバッグ用：現在のシステム状況を表示
        [ContextMenu("Debug System Status")]
        public void DebugSystemStatus()
        {
            Debug.Log("=== Battle System Status ===");
            
            Canvas canvas = FindObjectOfType<Canvas>();
            Debug.Log($"Canvas: {(canvas != null ? canvas.name : "Not Found")}");
            
            BattleManager battleManager = FindObjectOfType<BattleManager>();
            Debug.Log($"BattleManager: {(battleManager != null ? battleManager.name : "Not Found")}");
            
            SimpleBattleUI simpleBattleUI = FindObjectOfType<SimpleBattleUI>();
            Debug.Log($"SimpleBattleUI: {(simpleBattleUI != null ? simpleBattleUI.name : "Not Found")}");
            
            HandSystem handSystem = FindObjectOfType<HandSystem>();
            Debug.Log($"HandSystem: {(handSystem != null ? handSystem.name : "Not Found")}");
            
            HandUI handUI = FindObjectOfType<HandUI>();
            Debug.Log($"HandUI: {(handUI != null ? handUI.name : "Not Found")}");
            
            UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            Debug.Log($"EventSystem: {(eventSystem != null ? eventSystem.name : "Not Found")}");
            
            Debug.Log($"Application.isPlaying: {Application.isPlaying}");
            Debug.Log($"Hand System Enabled: {enableHandSystem}");
        }
    }
}
