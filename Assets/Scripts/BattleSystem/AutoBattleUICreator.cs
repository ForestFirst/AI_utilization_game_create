using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BattleSystem
{
    /// <summary>
    /// ゲーム開始時に自動で戦闘UIを作成するマネージャー
    /// 任意のGameObjectにアタッチするだけでUIが表示されます
    /// </summary>
    public class AutoBattleUICreator : MonoBehaviour
    {
        [Header("Auto UI Settings")]
        [SerializeField] private bool createOnAwake = true;
        [SerializeField] private bool destroyAfterCreation = false;
        
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
            Debug.Log("=== Auto Battle UI Creator Starting ===");
            
            // 1. Canvasの確認・作成
            Canvas canvas = EnsureCanvas();
            
            // 2. EventSystemの確認・作成
            EnsureEventSystem();
            
            // 3. SimpleBattleUIの追加
            SimpleBattleUI battleUI = canvas.GetComponent<SimpleBattleUI>();
            if (battleUI == null)
            {
                battleUI = canvas.gameObject.AddComponent<SimpleBattleUI>();
                Debug.Log("SimpleBattleUI component added to Canvas");
            }
            
            // 4. BattleManagerの確認・作成
            BattleManager battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                GameObject bmObj = new GameObject("BattleManager");
                battleManager = bmObj.AddComponent<BattleManager>();
                Debug.Log("BattleManager created");
            }
            
            Debug.Log("=== Battle UI System Ready! ===");
            Debug.Log("Press Play to see the battle UI!");
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
            }
        }
    }
}
