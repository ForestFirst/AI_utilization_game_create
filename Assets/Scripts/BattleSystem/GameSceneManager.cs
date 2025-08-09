using UnityEngine;
using BattleSystem;

namespace GameManagement
{
    public class GameSceneManager : MonoBehaviour
    {
        [Header("戦闘システム")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private WeaponManager weaponManager;
        
        [Header("プレイヤー・敵オブジェクト")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject enemyObject;
        
        [Header("UIシステム")]
        [SerializeField] private Canvas battleUICanvas;
        [SerializeField] private GameObject weaponUIPanel;
        [SerializeField] private GameObject playerHPPanel;
        [SerializeField] private GameObject enemyHPPanel;
        
        private void Awake()
        {
            InitializeScene();
        }
        
        private void Start()
        {
            StartBattle();
        }
        
        private void InitializeScene()
        {
            // カメラ位置を戦闘視点に設定
            SetupBattleCamera();
            
            // プレイヤーと敵の配置
            SetupBattlePositions();
            
            // UIの初期化
            SetupBattleUI();
            
            Debug.Log("戦闘シーン初期化完了");
        }
        
        private void SetupBattleCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // 後ろ斜め上からの視点に設定
                mainCamera.transform.position = new Vector3(0, 3, -5);
                mainCamera.transform.rotation = Quaternion.Euler(20, 0, 0);
            }
        }
        
        private void SetupBattlePositions()
        {
            if (playerObject != null)
            {
                playerObject.transform.position = new Vector3(-2, 0, 0);
                playerObject.transform.rotation = Quaternion.Euler(0, 45, 0);
            }
            
            if (enemyObject != null)
            {
                enemyObject.transform.position = new Vector3(2, 0, 2);
                enemyObject.transform.rotation = Quaternion.Euler(0, -45, 0);
            }
        }
        
        private void SetupBattleUI()
        {
            if (battleUICanvas != null)
            {
                battleUICanvas.gameObject.SetActive(true);
            }
        }
        
        private void StartBattle()
        {
            if (battleManager != null)
            {
                Debug.Log("戦闘開始！");
                // 戦闘開始処理は後で実装
            }
        }
        
        // デバッグ用：戦闘終了
        [ContextMenu("End Battle")]
        public void EndBattle()
        {
            Debug.Log("戦闘終了");
        }
        
        // デバッグ用：ダメージテスト
        [ContextMenu("Test Damage")]
        public void TestDamage()
        {
            Debug.Log("ダメージテスト: 1,250ダメージ！");
        }
    }
}
