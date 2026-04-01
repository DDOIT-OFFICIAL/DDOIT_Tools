using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace DDOIT.Tools
{
    /// <summary>
    /// 각 체험씬에 배치되는 매니저.
    /// 플레이어 초기 위치 설정 및 씬별 SoundDatabase 초기화를 담당한다.
    /// SoundDatabase 로드 완료 후 ScenarioManager를 시작한다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("플레이어 초기 위치")]
        [Tooltip("플레이어가 스폰될 Transform")]
        [SerializeField] private Transform _spawnPoint;

        [Header("사운드")]
        [Tooltip("이 씬에서 사용할 SoundDatabase (Addressables)")]
        [SerializeField] private AssetReference _soundDatabaseReference;

        #endregion

        #region Private Fields

        private AsyncOperationHandle<SoundDatabase> _databaseHandle;
        private bool _databaseLoaded;
        private ScenarioManager _scenarioManager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _scenarioManager = FindFirstObjectByType<ScenarioManager>(FindObjectsInactive.Include);
            InitializePlayerPosition();
            InitializeSoundDatabase();
        }

        private void OnDestroy()
        {
            if (SoundManager.HasInstance && SoundManager.Instance != null)
                SoundManager.Instance.ClearSceneDatabase();

            if (_databaseLoaded && _databaseHandle.IsValid())
                Addressables.Release(_databaseHandle);
        }

        #endregion

        #region Private Methods

        private void InitializePlayerPosition()
        {
            if (_spawnPoint == null || !PlayerController.HasInstance) return;

            PlayerController.Instance.Teleport(_spawnPoint.position, _spawnPoint.rotation);
        }

        private void InitializeSoundDatabase()
        {
            if (_soundDatabaseReference == null || !_soundDatabaseReference.RuntimeKeyIsValid())
            {
                StartScenario();
                return;
            }

            _databaseHandle = Addressables.LoadAssetAsync<SoundDatabase>(_soundDatabaseReference);
            _databaseHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _databaseLoaded = true;
                    SoundManager.Instance.SetSceneDatabase(handle.Result);
                }
                else
                {
                    Debug.LogError("[GameManager] SoundDatabase 로드 실패");
                }

                StartScenario();
            };
        }

        private void StartScenario()
        {
            if (_scenarioManager != null)
                StartCoroutine(WaitAndStartScenario());
        }

        private IEnumerator WaitAndStartScenario()
        {
            // 씬 로딩 중이면 완료까지 대기
            while (SceneManager.HasInstance && SceneManager.Instance.IsLoading)
                yield return null;

            _scenarioManager.StartSequence();
        }

        #endregion
    }
}
