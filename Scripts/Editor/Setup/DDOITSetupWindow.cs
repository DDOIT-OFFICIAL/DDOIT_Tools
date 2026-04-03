using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Setup
{
    /// <summary>
    /// DDOIT Tools 초기 설정 윈도우.
    /// 외부 의존성 없이 독립 컴파일되어, 패키지 설치 직후 자동으로 표시된다.
    /// </summary>
    [InitializeOnLoad]
    public class DDOITSetupWindow : EditorWindow
    {
        #region Constants

        private static readonly string[] PROJECT_FOLDERS =
        {
            "01. Scenes", "02. Scripts", "03. Prefabs", "04. Models",
            "05. SO", "06. Textures", "07. Shaders", "08. Materials",
            "09. Sprites", "10. Audios", "11. Animations", "12. Particles",
            "13. Fonts", "14. Timelines", "15. Videos"
        };

        private static readonly DependencyInfo[] REQUIRED_DEPENDENCIES =
        {
            new DependencyInfo("Meta XR All-in-One SDK", "com.meta.xr.sdk.all", "83.0.4",
                "Unity Asset Store에서 먼저 내 에셋에 추가해야 합니다."),
            new DependencyInfo("Lottie Player", "com.gilzoide.lottie-player",
                "https://github.com/gilzoide/unity-lottie-player.git", null),
        };

        private static readonly DependencyInfo[] OPTIONAL_DEPENDENCIES =
        {
            new DependencyInfo("Unity-CLI Connector", "com.youngwoocho02.unity-cli-connector",
                "https://github.com/youngwoocho02/unity-cli.git?path=unity-connector",
                "AI 기반 vibe 코딩 시 권장"),
        };

        private const string SHOWN_KEY = "DDOIT_SetupWindow_Shown";

        #endregion

        #region Types

        private struct DependencyInfo
        {
            public string displayName;
            public string packageId;
            public string versionOrUrl;
            public string note;

            public DependencyInfo(string displayName, string packageId, string versionOrUrl, string note)
            {
                this.displayName = displayName;
                this.packageId = packageId;
                this.versionOrUrl = versionOrUrl;
                this.note = note;
            }
        }

        #endregion

        #region Private Fields

        private Vector2 _scrollPosition;

        #endregion

        #region Auto Open

        static DDOITSetupWindow()
        {
            EditorApplication.delayCall += ShowOnFirstLoad;
        }

        private static void ShowOnFirstLoad()
        {
            // 의존성이 모두 설치된 상태면 팝업하지 않음
            bool allInstalled = REQUIRED_DEPENDENCIES.All(d => IsPackageInstalled(d.packageId));
            if (allInstalled && SessionState.GetBool(SHOWN_KEY, false))
                return;

            SessionState.SetBool(SHOWN_KEY, true);

            if (!allInstalled)
                ShowWindow();
        }

        #endregion

        #region Menu

        [MenuItem("DDOIT Tools/Setup", priority = -99)]
        public static void ShowWindow()
        {
            var window = GetWindow<DDOITSetupWindow>("DDOIT Setup");
            window.minSize = new Vector2(400, 500);
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("DDOIT Tools Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            // ── 의존성 ──
            DrawDependencySection();

            DrawSeparator();

            // ── 프로젝트 초기화 ──
            DrawInitProjectSection();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Dependency Section

        private void DrawDependencySection()
        {
            EditorGUILayout.LabelField("의존성 패키지", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawDependencyList("필수", REQUIRED_DEPENDENCIES);
            EditorGUILayout.Space(4);
            DrawDependencyList("권장", OPTIONAL_DEPENDENCIES);

            EditorGUILayout.Space(4);

            bool hasAnyMissing = REQUIRED_DEPENDENCIES.Any(d => !IsPackageInstalled(d.packageId))
                              || OPTIONAL_DEPENDENCIES.Any(d => !IsPackageInstalled(d.packageId));

            EditorGUI.BeginDisabledGroup(!hasAnyMissing);
            if (GUILayout.Button("미설치 패키지 모두 설치", GUILayout.Height(28)))
                InstallMissingDependencies();
            EditorGUI.EndDisabledGroup();

            if (!hasAnyMissing)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox("모든 의존성 패키지가 설치되어 있습니다.", MessageType.Info);
            }
        }

        private void DrawDependencyList(string label, DependencyInfo[] dependencies)
        {
            EditorGUILayout.LabelField($"[ {label} ]", EditorStyles.miniLabel);
            foreach (var dep in dependencies)
            {
                bool installed = IsPackageInstalled(dep.packageId);

                EditorGUILayout.BeginHorizontal();

                var statusStyle = new GUIStyle(EditorStyles.label);
                statusStyle.normal.textColor = installed
                    ? new Color(0.2f, 0.8f, 0.2f)
                    : new Color(0.8f, 0.2f, 0.2f);
                EditorGUILayout.LabelField(installed ? "v" : "X", statusStyle, GUILayout.Width(16));

                EditorGUILayout.LabelField(dep.displayName);

                EditorGUILayout.EndHorizontal();

                if (!installed && !string.IsNullOrEmpty(dep.note))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox(dep.note, MessageType.None);
                    EditorGUI.indentLevel--;
                }
            }
        }

        #endregion

        #region Init Project Section

        private void DrawInitProjectSection()
        {
            EditorGUILayout.LabelField("프로젝트 초기화", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Assets/ 하위에 폴더 템플릿을 생성하고,\n" +
                "DDOIT, InitScene을 01. Scenes/DDOIT/ 에 복사합니다.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("생성될 폴더 구조:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            foreach (var folder in PROJECT_FOLDERS)
                EditorGUILayout.LabelField(folder);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Init Project", GUILayout.Height(32)))
                ExecuteInitProject();
        }

        #endregion

        #region Actions

        private static void InstallMissingDependencies()
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[DDOITSetupWindow] manifest.json을 찾을 수 없습니다.");
                return;
            }

            string content = File.ReadAllText(manifestPath);
            bool modified = false;

            var allDeps = new List<DependencyInfo>(REQUIRED_DEPENDENCIES);
            allDeps.AddRange(OPTIONAL_DEPENDENCIES);

            foreach (var dep in allDeps)
            {
                if (content.Contains($"\"{dep.packageId}\"")) continue;

                string insertAfter = "\"dependencies\": {";
                int idx = content.IndexOf(insertAfter);
                if (idx < 0) continue;

                int insertPos = idx + insertAfter.Length;
                string entry = $"\n    \"{dep.packageId}\": \"{dep.versionOrUrl}\",";
                content = content.Insert(insertPos, entry);
                modified = true;

                Debug.Log($"[DDOITSetupWindow] 추가: {dep.displayName} ({dep.packageId})");
            }

            if (modified)
            {
                File.WriteAllText(manifestPath, content);
                Debug.Log("[DDOITSetupWindow] manifest.json 수정 완료. Unity가 패키지를 다운로드합니다.");

                EditorUtility.DisplayDialog(
                    "패키지 설치",
                    "manifest.json에 패키지를 추가했습니다.\n" +
                    "Unity가 자동으로 패키지를 다운로드합니다.\n\n" +
                    "Meta XR SDK는 Unity Asset Store에서\n" +
                    "먼저 '내 에셋에 추가'해야 설치됩니다.",
                    "확인");

                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("[DDOITSetupWindow] 모든 패키지가 이미 설치되어 있습니다.");
            }
        }

        private static void ExecuteInitProject()
        {
            // 1. 폴더 템플릿 생성
            foreach (var folder in PROJECT_FOLDERS)
            {
                string path = Path.Combine("Assets", folder);
                if (!AssetDatabase.IsValidFolder(path))
                    AssetDatabase.CreateFolder("Assets", folder);
            }

            // 2. DDOIT 씬 폴더 생성
            string sceneDdoitPath = "Assets/01. Scenes/DDOIT";
            if (!AssetDatabase.IsValidFolder(sceneDdoitPath))
                AssetDatabase.CreateFolder("Assets/01. Scenes", "DDOIT");

            // 3. 패키지에서 씬 복사
            string packageScenesPath = FindPackageScenesPath();
            if (packageScenesPath == null)
            {
                Debug.LogError("[DDOITSetupWindow] DDOIT_Tools 패키지의 Scenes 폴더를 찾을 수 없습니다.");
                return;
            }

            string[] sceneFiles = { "DDOIT.unity", "InitScene.unity" };
            int copiedCount = 0;

            foreach (var scene in sceneFiles)
            {
                string src = Path.Combine(packageScenesPath, scene);
                string dst = Path.Combine(sceneDdoitPath, scene);

                if (!File.Exists(src))
                {
                    Debug.LogWarning($"[DDOITSetupWindow] 원본 씬을 찾을 수 없습니다: {src}");
                    continue;
                }

                if (File.Exists(dst))
                {
                    if (!EditorUtility.DisplayDialog(
                        "씬 덮어쓰기",
                        $"{scene}이 이미 존재합니다. 덮어쓰시겠습니까?",
                        "덮어쓰기", "건너뛰기"))
                        continue;
                }

                AssetDatabase.CopyAsset(src, dst);
                copiedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[DDOITSetupWindow] 프로젝트 초기화 완료 (폴더 {PROJECT_FOLDERS.Length}개, 씬 {copiedCount}개 복사)");
        }

        private static string FindPackageScenesPath()
        {
            if (AssetDatabase.IsValidFolder("Assets/DDOIT_Tools/Scenes"))
                return "Assets/DDOIT_Tools/Scenes";

            if (AssetDatabase.IsValidFolder("Packages/com.ddoit.tools/Scenes"))
                return "Packages/com.ddoit.tools/Scenes";

            return null;
        }

        #endregion

        #region Utility

        private static bool IsPackageInstalled(string packageId)
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath)) return false;
            string content = File.ReadAllText(manifestPath);
            return content.Contains($"\"{packageId}\"");
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(8);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(8);
        }

        #endregion
    }
}
