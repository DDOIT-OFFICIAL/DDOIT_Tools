using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DDOIT.Tools
{
    /// <summary>
    /// DDOIT 씬에 배치. 매니저 초기화 순서를 관리하고 첫 콘텐츠 씬 로드를 SceneManager에 위임한다.
    /// </summary>
    public class BootstrapManager : MonoBehaviour
    {
        [Header("기본 콘텐츠 씬 (빌드 시 첫 로드 씬)")]
        [SerializeField] private AssetReference _defaultContentScene;

        private void Start()
        {
            StartCoroutine(Initialize());
        }

        private IEnumerator Initialize()
        {
            // 매니저 초기화 (순서 중요)
            // TODO: 향후 매니저 추가 시 여기에 추가
            // yield return DataManager.Instance.Initialize();
            yield return SoundManager.Instance.Initialize();
            yield return SceneManager.Instance.Initialize();
            yield return UIManager.Instance.Initialize();

            LoadContentScene();
        }

        private void LoadContentScene()
        {
            #if UNITY_EDITOR
            string debugScene = UnityEditor.SessionState.GetString("BootstrapSceneLoader.PreviousScene", "");
            if (!string.IsNullOrEmpty(debugScene))
            {
                SceneManager.Instance.LoadSceneByPath(debugScene);
                return;
            }
            #endif

            if (_defaultContentScene != null)
            {
                SceneManager.Instance.LoadScene(_defaultContentScene);
            }
        }
    }
}
