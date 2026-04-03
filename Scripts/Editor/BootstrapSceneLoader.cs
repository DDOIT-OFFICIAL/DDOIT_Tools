#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    /// <summary>
    /// Play 버튼을 누르면 항상 DDOIT 씬부터 시작하도록 보장하는 에디터 스크립트.
    /// 개발 중인 콘텐츠 씬에서 Play를 눌러도 DDOIT 씬 → 해당 씬 순서로 로드된다.
    /// </summary>
    [InitializeOnLoad]
    public static class BootstrapSceneLoader
    {
        private const string BOOTSTRAP_SCENE_NAME = "DDOIT";
        private const string PREVIOUS_SCENE_KEY = "BootstrapSceneLoader.PreviousScene";
        private const string MENU_PATH = "DDOIT Tools/Bootstrap Scene Loader 활성화";

        /// <summary>
        /// DDOIT 씬 경로를 동적으로 찾는다. Assets/ 또는 Packages/ 어디든 대응.
        /// </summary>
        private static string FindBootstrapScenePath()
        {
            var guids = AssetDatabase.FindAssets($"t:Scene {BOOTSTRAP_SCENE_NAME}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == BOOTSTRAP_SCENE_NAME)
                    return path;
            }
            return null;
        }

        private static bool IsEnabled
        {
            get => EditorPrefs.GetBool(MENU_PATH, true);
            set => EditorPrefs.SetBool(MENU_PATH, value);
        }

        static BootstrapSceneLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem(MENU_PATH)]
        private static void ToggleEnabled()
        {
            IsEnabled = !IsEnabled;
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MENU_PATH, IsEnabled);
            return true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!IsEnabled) return;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                string bootstrapScene = FindBootstrapScenePath();
                if (string.IsNullOrEmpty(bootstrapScene))
                {
                    Debug.LogWarning("[BootstrapSceneLoader] DDOIT 씬을 찾을 수 없습니다.");
                    return;
                }

                string currentScenePath = EditorSceneManager.GetActiveScene().path;

                if (currentScenePath == bootstrapScene)
                {
                    // DDOIT 씬에서 직접 Play한 경우, 저장된 씬 정보 초기화
                    SessionState.EraseString(PREVIOUS_SCENE_KEY);
                    return;
                }

                // 현재 씬에 저장되지 않은 변경이 있으면 저장 여부 확인
                if (EditorSceneManager.GetActiveScene().isDirty)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        // 사용자가 취소를 누른 경우 Play 모드 진입 중단
                        EditorApplication.isPlaying = false;
                        return;
                    }
                }

                // 현재 작업 중인 씬 경로를 저장하고 DDOIT 씬으로 전환
                SessionState.SetString(PREVIOUS_SCENE_KEY, currentScenePath);
                EditorSceneManager.OpenScene(bootstrapScene);
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                // Play 모드 종료 후 원래 작업하던 씬으로 복귀
                string previousScene = SessionState.GetString(PREVIOUS_SCENE_KEY, "");

                if (!string.IsNullOrEmpty(previousScene))
                {
                    SessionState.EraseString(PREVIOUS_SCENE_KEY);
                    EditorSceneManager.OpenScene(previousScene);
                }
            }
        }
    }
}
#endif
