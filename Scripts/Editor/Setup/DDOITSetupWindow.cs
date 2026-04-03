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

        private const string DDOIT_DATA_FOLDER = "Assets/DDOIT_Tools/Data";
        private const string PACKAGE_DATA_PATH_DEV = "Assets/DDOIT_Tools/Data";
        private const string PACKAGE_DATA_PATH_UPM = "Packages/com.ddoit.tools/Data";

        private static readonly string[] OPTIMIZE_ASSETS =
        {
            "DDOIT_RPAsset.asset",
            "DDOIT_Renderer.asset",
            "DDOIT_VolumeProfile.asset",
        };

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

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            EditorApplication.delayCall += ShowOnFirstLoad;
        }

        private static void ShowOnFirstLoad()
        {
            // 이미 이 세션에서 체크했으면 스킵
            if (SessionState.GetBool(SHOWN_KEY, false))
                return;

            SessionState.SetBool(SHOWN_KEY, true);

            // 필수 의존성이 하나라도 없으면 자동 팝업
            bool allInstalled = REQUIRED_DEPENDENCIES.All(d => IsPackageInstalled(d.packageId));
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

            DrawSeparator();

            // ── 프로젝트 최적화 ──
            DrawOptimizeSection();

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

        #region Optimize Section

        private void DrawOptimizeSection()
        {
            EditorGUILayout.LabelField("프로젝트 최적화 (Quest VR)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Meta Quest VR에 최적화된 프로젝트 설정을 일괄 적용합니다.\n\n" +
                "[ 에셋 교체 ]\n" +
                "  - DDOIT_RPAsset → Graphics Default Pipeline\n" +
                "  - DDOIT_Renderer → Renderer Data\n" +
                "  - DDOIT_VolumeProfile → Default Volume Profile\n\n" +
                "[ Player Settings ]\n" +
                "  - Linear / Vulkan / IL2CPP / ARM64\n" +
                "  - Managed Stripping: Medium\n\n" +
                "[ Physics / Audio ]\n" +
                "  - Fixed Timestep: 72Hz (0.01389)\n" +
                "  - DSP Buffer: 256 (Best Latency)\n\n" +
                "[ Build ]\n" +
                "  - Texture Compression: ASTC\n" +
                "  - Compression Method: LZ4",
                MessageType.Info);

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Optimize Project", GUILayout.Height(32)))
                ExecuteOptimizeProject();
        }

        private static void ExecuteOptimizeProject()
        {
            if (!EditorUtility.DisplayDialog(
                "프로젝트 최적화",
                "Quest VR 최적화 설정을 적용합니다.\n" +
                "기존 Graphics/Quality 설정이 변경됩니다.\n\n" +
                "계속하시겠습니까?",
                "적용", "취소"))
                return;

            int appliedCount = 0;

            // ── 1. 에셋 복사 (UPM → Assets/) ──
            string srcDataPath = FindPackageDataPath();
            if (srcDataPath == null)
            {
                Debug.LogError("[DDOITSetupWindow] DDOIT_Tools Data 폴더를 찾을 수 없습니다.");
                return;
            }

            // 목적지 폴더 확보
            string dstFolder = "Assets/Settings/DDOIT";
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder(dstFolder))
                AssetDatabase.CreateFolder("Assets/Settings", "DDOIT");

            foreach (var assetName in OPTIMIZE_ASSETS)
            {
                string src = $"{srcDataPath}/{assetName}";
                string dst = $"{dstFolder}/{assetName}";

                if (!File.Exists(Path.GetFullPath(src)))
                {
                    Debug.LogWarning($"[DDOITSetupWindow] 원본 에셋 없음: {src}");
                    continue;
                }

                if (File.Exists(Path.GetFullPath(dst)))
                    AssetDatabase.DeleteAsset(dst);

                AssetDatabase.CopyAsset(src, dst);
            }

            AssetDatabase.Refresh();

            // ── 2. RP Asset → Graphics 설정 적용 ──
            string rpAssetPath = $"{dstFolder}/DDOIT_RPAsset.asset";
            var rpAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(rpAssetPath);
            if (rpAsset != null)
            {
                // Renderer 재연결 (복사 후 참조 깨질 수 있음)
                string rendererPath = $"{dstFolder}/DDOIT_Renderer.asset";
                var renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(rendererPath);
                if (renderer != null)
                {
                    var rpSo = new SerializedObject(rpAsset);
                    var rendererList = rpSo.FindProperty("m_RendererDataList");
                    if (rendererList != null && rendererList.arraySize > 0)
                    {
                        rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                        rpSo.ApplyModifiedProperties();
                    }
                }

                // Volume Profile 재연결
                string volPath = $"{dstFolder}/DDOIT_VolumeProfile.asset";
                var volProfile = AssetDatabase.LoadAssetAtPath<ScriptableObject>(volPath);
                if (volProfile != null)
                {
                    var rpSo = new SerializedObject(rpAsset);
                    var volProp = rpSo.FindProperty("m_VolumeProfile");
                    if (volProp != null)
                    {
                        volProp.objectReferenceValue = volProfile;
                        rpSo.ApplyModifiedProperties();
                    }
                }

                // Graphics Settings에 Default RP 설정
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = rpAsset as UnityEngine.Rendering.RenderPipelineAsset;

                // Quality Settings에도 적용
                QualitySettings.renderPipeline = rpAsset as UnityEngine.Rendering.RenderPipelineAsset;

                appliedCount++;
                Debug.Log("[DDOITSetupWindow] DDOIT_RPAsset → Graphics/Quality 적용 완료");
            }

            // ── 3. Player Settings ──
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(
                UnityEditor.Build.NamedBuildTarget.Android,
                ManagedStrippingLevel.Medium);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Player Settings 적용 완료");

            // ── 4. Physics ──
            Time.fixedDeltaTime = 0.01389f;
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Fixed Timestep → 0.01389 (72Hz)");

            // ── 5. Audio ──
            var audioConfig = AudioSettings.GetConfiguration();
            audioConfig.dspBufferSize = 256;
            AudioSettings.Reset(audioConfig);
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] DSP Buffer Size → 256");

            // ── 6. Build Settings ──
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Texture Compression → ASTC");

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "최적화 완료",
                $"Quest VR 최적화 설정이 적용되었습니다.\n" +
                $"총 {appliedCount}개 항목 적용.",
                "확인");

            Debug.Log($"[DDOITSetupWindow] 프로젝트 최적화 완료 ({appliedCount}개 항목)");
        }

        private static string FindPackageDataPath()
        {
            if (AssetDatabase.IsValidFolder(PACKAGE_DATA_PATH_DEV))
                return PACKAGE_DATA_PATH_DEV;

            if (AssetDatabase.IsValidFolder(PACKAGE_DATA_PATH_UPM))
                return PACKAGE_DATA_PATH_UPM;

            return null;
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
