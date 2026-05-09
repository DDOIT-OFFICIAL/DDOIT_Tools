using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using DDOIT.Tools.Utilities;
using USceneManager = UnityEngine.SceneManagement.SceneManager;
using LoadSceneMode = UnityEngine.SceneManagement.LoadSceneMode;

namespace DDOIT.Tools.Managers
{
    /// <summary>
    /// 씬 전환을 전담하는 매니저.
    /// Addressables 기반으로 씬을 로드/언로드하며, 페이드 전환을 지원한다.
    /// </summary>
    public class SceneManager : Singleton<SceneManager>
    {
        #region Serialized Fields

        [Header("로딩 화면")]
        [Tooltip("로딩 UI 최소 표시 시간 (초). 씬 로드가 빨라도 이 시간만큼 로딩 화면을 유지한다.")]
        [SerializeField] private float _minimumLoadingDisplayTime = 2f;

        #endregion

        #region Properties

        public bool IsLoading { get; private set; }
        public bool IsReady { get; private set; }

        #endregion

        #region Events

        [Header("이벤트")]
        [Tooltip("씬 전환 시작 직후")]
        [SerializeField] private UnityEvent _onLoadStart;

        [Tooltip("암전 + 언로드 완료 후 (로딩 UI 표시 시점)")]
        [SerializeField] private UnityEvent _onReadyForLoading;

        [Tooltip("씬 로드 진행률 (0~1)")]
        [SerializeField] private UnityEvent<float> _onLoadProgress;

        [Tooltip("새 씬 로드 완료 후 (로딩 UI 숨김 시점)")]
        [SerializeField] private UnityEvent _onSceneLoaded;

        [Tooltip("페이드인까지 모두 완료 후")]
        [SerializeField] private UnityEvent _onLoadComplete;

        #endregion

        #region Private Fields

        private AsyncOperationHandle<SceneInstance> _currentSceneHandle;
        private bool _isCurrentSceneAddressable;

        #endregion

        #region Initialization

        /// <summary>
        /// BootstrapManager에서 호출.
        /// </summary>
        public IEnumerator Initialize()
        {
            IsReady = true;
            yield break;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Addressables 기반 씬 전환.
        /// 콘텐츠 스크립트에서 호출하는 주요 메서드.
        /// </summary>
        /// <param name="sceneReference">Addressables에 등록된 씬 AssetReference</param>
        /// <param name="fadeDuration">페이드 시간 (초). 0 이하이면 ScreenFadeManager 기본값 사용.</param>
        public void LoadScene(AssetReference sceneReference, float fadeDuration = 0f)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneManager] 씬 로딩 중 - 요청 무시");
                return;
            }

            StartCoroutine(AddressableLoadRoutine(sceneReference, fadeDuration));
        }

        /// <summary>
        /// 경로 기반 씬 로드 (에디터 디버그용, Addressables 미사용).
        /// Build Settings에 없는 씬도 로드 가능.
        /// </summary>
        public void LoadSceneByPath(string scenePath)
        {
            if (IsLoading) return;
            StartCoroutine(DirectLoadRoutine(scenePath));
        }

        #endregion

        #region Scene Load Routines

        private IEnumerator AddressableLoadRoutine(AssetReference sceneReference, float fadeDuration)
        {
            IsLoading = true;
            _onLoadStart?.Invoke();

            // 1. 암전
            if (ScreenFadeManager.HasInstance)
                yield return ScreenFadeManager.Instance.FadeToBlack(fadeDuration);

            // 2. 기존 콘텐츠 씬 언로드
            yield return UnloadCurrentScene();

            // 3. 로딩 UI 표시 시점
            _onReadyForLoading?.Invoke();
            float loadingStartTime = Time.unscaledTime;

            // 4. Addressables로 새 씬 로드
            var handle = Addressables.LoadSceneAsync(
                sceneReference,
                LoadSceneMode.Additive);

            while (!handle.IsDone)
            {
                _onLoadProgress?.Invoke(handle.PercentComplete);
                yield return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _currentSceneHandle = handle;
                _isCurrentSceneAddressable = true;

                // 콘텐츠 씬을 active로 set → 그 씬의 RenderSettings(skybox/ambient/fog) 적용
                USceneManager.SetActiveScene(handle.Result.Scene);
            }
            else
            {
                Debug.LogError($"[SceneManager] 씬 로드 실패: {sceneReference.RuntimeKey}");
            }

            // 5. 최소 로딩 표시 시간 보장
            float elapsed = Time.unscaledTime - loadingStartTime;
            if (elapsed < _minimumLoadingDisplayTime)
                yield return new WaitForSecondsRealtime(_minimumLoadingDisplayTime - elapsed);

            // 6. 로딩 UI 숨김 시점
            _onSceneLoaded?.Invoke();

            // 7. 밝아짐
            if (ScreenFadeManager.HasInstance)
                yield return ScreenFadeManager.Instance.FadeClear(fadeDuration);

            IsLoading = false;
            _onLoadComplete?.Invoke();
        }

        private IEnumerator DirectLoadRoutine(string scenePath)
        {
            IsLoading = true;

            yield return UnloadCurrentScene();

            #if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scenePath,
                new UnityEngine.SceneManagement.LoadSceneParameters(LoadSceneMode.Additive));
            #else
            USceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            #endif

            // 씬 로드 완료까지 대기
            yield return null;

            // 콘텐츠 씬을 active로 set → 그 씬의 RenderSettings(skybox/ambient/fog) 적용
            var loadedScene = USceneManager.GetSceneByPath(scenePath);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
                USceneManager.SetActiveScene(loadedScene);

            _isCurrentSceneAddressable = false;
            IsLoading = false;
        }

        #endregion

        #region Scene Unload

        private IEnumerator UnloadCurrentScene()
        {
            // Addressables로 로드된 씬이면 Addressables로 언로드 (에셋 번들 해제)
            if (_isCurrentSceneAddressable && _currentSceneHandle.IsValid())
            {
                yield return Addressables.UnloadSceneAsync(_currentSceneHandle);
                _currentSceneHandle = default;
                _isCurrentSceneAddressable = false;
                yield break;
            }

            // 직접 로드된 씬 언로드 (에디터 디버그 등)
            // DDOIT 씬(자신이 속한 씬)을 제외한 모든 씬 언로드
            var bootstrapScene = gameObject.scene;

            for (int i = USceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = USceneManager.GetSceneAt(i);

                if (scene != bootstrapScene && scene.isLoaded)
                {
                    yield return USceneManager.UnloadSceneAsync(scene);
                }
            }

            _isCurrentSceneAddressable = false;
        }

        #endregion
    }
}
