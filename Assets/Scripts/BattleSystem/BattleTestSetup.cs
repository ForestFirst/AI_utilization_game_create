using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using BattleSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BattleSystem
{
    /// <summary>
    /// 戦闘テスト環境の自動セットアップを行うエディター用スクリプト
    /// </summary>
    public class BattleTestSetup : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("BattleSystem/Setup Battle Test Scene")]
        public static void SetupBattleTestScene()
        {
            Debug.Log("Setting up Battle Test Scene...");
            
            // 1. BattleTestManagerオブジェクトの作成
            GameObject battleTestObj = CreateBattleTestManager();
            
            // 2. Canvasの作成
            GameObject canvasObj = CreateUICanvas();
            
            // 3. EventSystemの確認・作成
            EnsureEventSystem();
            
            // 4. カメラの設定
            SetupCamera();
            
            // 5. BattleTestManagerの初期化実行
            BattleTestManager testManager = battleTestObj.GetComponent<BattleTestManager>();
            if (testManager != null)
            {
                // UI Canvas参照を設定
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                var canvasField = typeof(BattleTestManager).GetField("uiCanvas", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                canvasField?.SetValue(testManager, canvas);
                
                // テスト環境のセットアップを実行
                testManager.SetupTestEnvironment();
            }
            
            Debug.Log("Battle Test Scene setup completed!");
        }
        
        private static GameObject CreateBattleTestManager()
        {
            // 既存のBattleTestManagerを探す
            BattleTestManager existing = FindObjectOfType<BattleTestManager>();
            if (existing != null)
            {
                Debug.Log("BattleTestManager already exists, using existing one");
                return existing.gameObject;
            }
            
            // 新しいBattleTestManagerを作成
            GameObject obj = new GameObject("BattleTestManager");
            obj.AddComponent<BattleTestManager>();
            
            Debug.Log("Created BattleTestManager");
            return obj;
        }
        
        private static GameObject CreateUICanvas()
        {
            // 既存のCanvasを探す
            Canvas existing = FindObjectOfType<Canvas>();
            if (existing != null)
            {
                Debug.Log("Canvas already exists, using existing one");
                return existing.gameObject;
            }
            
            // 新しいCanvasを作成
            GameObject canvasObj = new GameObject("UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            // CanvasScalerを追加
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // GraphicRaycasterを追加
            canvasObj.AddComponent<GraphicRaycaster>();
            
            Debug.Log("Created UI Canvas");
            return canvasObj;
        }
        
        private static void EnsureEventSystem()
        {
            EventSystem existing = FindObjectOfType<EventSystem>();
            if (existing != null)
            {
                Debug.Log("EventSystem already exists");
                return;
            }
            
            // EventSystemを作成
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            
            Debug.Log("Created EventSystem");
        }
        
        private static void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // メインカメラを作成
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
                
                Debug.Log("Created Main Camera");
            }
            
            // カメラの基本設定
            mainCamera.transform.position = new Vector3(0, 1, -10);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
            
            Debug.Log("Camera setup completed");
        }
        
        [MenuItem("BattleSystem/Reset Battle Test")]
        public static void ResetBattleTest()
        {
            BattleTestManager testManager = FindObjectOfType<BattleTestManager>();
            if (testManager != null)
            {
                testManager.ResetBattle();
                Debug.Log("Battle Test Reset!");
            }
            else
            {
                Debug.LogWarning("No BattleTestManager found in scene");
            }
        }
        
        [MenuItem("BattleSystem/Force UI Setup")]
        public static void ForceUISetup()
        {
            BattleTestManager testManager = FindObjectOfType<BattleTestManager>();
            if (testManager != null)
            {
                testManager.SetupTestEnvironment();
                Debug.Log("UI Setup Forced!");
            }
            else
            {
                Debug.LogWarning("No BattleTestManager found in scene");
            }
        }
#endif
    }
}
