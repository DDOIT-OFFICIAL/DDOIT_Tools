using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
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
            "13. Fonts", "14. Timelines", "15. Videos", "999. Resources"
        };

        private static readonly string[] PROJECT_SUBFOLDERS =
        {
            "999. Resources/AssetStore"
        };

        private static readonly DependencyInfo[] REQUIRED_DEPENDENCIES =
        {
            new DependencyInfo("Meta XR All-in-One SDK", "com.meta.xr.sdk.all", "203.0.0", DependencyVersionPolicy.ExactVersion,
                "Unity Asset Store에서 먼저 내 에셋에 추가해야 합니다. (Audio/Voice 모듈만 v85.0.0 유지)"),
            new DependencyInfo("Input System", "com.unity.inputsystem", "1.18.0", DependencyVersionPolicy.MinimumVersion, null),
            new DependencyInfo("TextMeshPro", "com.unity.textmeshpro", "4.0.0", DependencyVersionPolicy.MinimumVersion, null),
            new DependencyInfo("Addressables", "com.unity.addressables", "2.8.1", DependencyVersionPolicy.MinimumVersion, null),
            new DependencyInfo("Lottie Player", "com.gilzoide.lottie-player",
                "https://github.com/gilzoide/unity-lottie-player.git", DependencyVersionPolicy.PresenceOnly, null),
        };

        private static readonly DependencyInfo[] OPTIONAL_DEPENDENCIES =
        {
            new DependencyInfo("Unity-CLI Connector", "com.youngwoocho02.unity-cli-connector",
                "https://github.com/youngwoocho02/unity-cli.git?path=unity-connector",
                DependencyVersionPolicy.PresenceOnly, "AI 기반 vibe 코딩 시 권장"),
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
            public DependencyVersionPolicy versionPolicy;
            public string note;

            public DependencyInfo(
                string displayName,
                string packageId,
                string versionOrUrl,
                DependencyVersionPolicy versionPolicy,
                string note)
            {
                this.displayName = displayName;
                this.packageId = packageId;
                this.versionOrUrl = versionOrUrl;
                this.versionPolicy = versionPolicy;
                this.note = note;
            }
        }

        private enum DependencyVersionPolicy
        {
            ExactVersion,
            MinimumVersion,
            PresenceOnly
        }

        private enum DependencyState
        {
            Missing,
            VersionMismatch,
            Installed
        }

        private struct DependencyStatus
        {
            public DependencyState state;
            public string installedVersion;

            public DependencyStatus(DependencyState state, string installedVersion)
            {
                this.state = state;
                this.installedVersion = installedVersion;
            }
        }

        #endregion

        #region Private Fields

        private Vector2 _scrollPosition;
        private static readonly Queue<DependencyInfo> PendingDependencyInstalls = new Queue<DependencyInfo>();
        private static AddRequest ActiveAddRequest;
        private static DependencyInfo ActiveDependency;
        private static bool IsInstallingDependencies;

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
            bool allInstalled = REQUIRED_DEPENDENCIES.All(d => !RequiresInstallOrUpdate(d));
            if (!allInstalled)
                ShowWindow();
        }

        #endregion

        #region Menu

        [MenuItem("DDOIT Tools/Setup", priority = -200)]
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

            bool hasAnyMissing = REQUIRED_DEPENDENCIES.Any(RequiresInstallOrUpdate)
                              || OPTIONAL_DEPENDENCIES.Any(RequiresInstallOrUpdate);

            EditorGUI.BeginDisabledGroup(!hasAnyMissing || IsInstallingDependencies);
            if (GUILayout.Button("미설치 패키지 모두 설치", GUILayout.Height(28)))
                InstallMissingDependencies();
            EditorGUI.EndDisabledGroup();

            if (IsInstallingDependencies)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox($"패키지 설치 진행 중: {ActiveDependency.displayName}", MessageType.Info);
            }

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
                var status = GetDependencyStatus(dep);

                EditorGUILayout.BeginHorizontal();

                var statusStyle = new GUIStyle(EditorStyles.label);
                statusStyle.normal.textColor = status.state == DependencyState.Installed
                    ? new Color(0.2f, 0.8f, 0.2f)
                    : status.state == DependencyState.VersionMismatch
                        ? new Color(0.95f, 0.65f, 0.1f)
                        : new Color(0.8f, 0.2f, 0.2f);
                EditorGUILayout.LabelField(GetDependencyStatusLabel(status.state), statusStyle, GUILayout.Width(36));

                EditorGUILayout.LabelField(dep.displayName);

                EditorGUILayout.EndHorizontal();

                if (status.state == DependencyState.VersionMismatch)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox(
                        $"설치 버전: {status.installedVersion}\n요구 기준: {GetDependencyRequirementText(dep)}",
                        MessageType.Warning);
                    EditorGUI.indentLevel--;
                }

                if (status.state != DependencyState.Installed && !string.IsNullOrEmpty(dep.note))
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
            foreach (var folder in PROJECT_SUBFOLDERS)
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
                "[ Quality ]\n" +
                "  - 활성 Quality → Mobile (idx 0)\n" +
                "  - Android Platform Default → Mobile\n\n" +
                "[ Player Settings ]\n" +
                "  - Linear / Vulkan + OpenGLES3 / IL2CPP / ARM64\n" +
                "  - Managed Stripping: Medium / Min API: 32\n\n" +
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
                {
                    if (!AssetDatabase.DeleteAsset(dst))
                    {
                        Debug.LogError($"[DDOITSetupWindow] 기존 에셋 삭제 실패: {dst}");
                        continue;
                    }
                }

                if (!AssetDatabase.CopyAsset(src, dst))
                {
                    Debug.LogError($"[DDOITSetupWindow] 에셋 복사 실패: {src} → {dst}");
                }
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

                // URP Global Settings의 Default Volume Profile 교체
                string volPath2 = $"{dstFolder}/DDOIT_VolumeProfile.asset";
                var volProfileForGlobal = AssetDatabase.LoadAssetAtPath<ScriptableObject>(volPath2);
                if (volProfileForGlobal != null)
                {
                    ApplyGlobalVolumeProfile(volProfileForGlobal);
                    Debug.Log("[DDOITSetupWindow] URP Global Default Volume Profile → DDOIT_VolumeProfile");
                }
            }

            // ── 2.5 Quality (Quest VR은 Mobile quality 권장) ──
            //   QualitySettings.SetQualityLevel으로 즉시 active 변경 + Android default도 Mobile로
            QualitySettings.SetQualityLevel(0, true);
            ApplyAndroidDefaultQuality(0);
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Quality Level → Mobile (Android default Mobile)");

            // ── 3. Player Settings ──
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                        UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetScriptingBackend(
                UnityEditor.Build.NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(
                UnityEditor.Build.NamedBuildTarget.Android,
                ManagedStrippingLevel.Medium);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Player Settings 적용 완료");

            // ── 4. Physics (ProjectSettings 파일 직접 수정) ──
            ModifyProjectSettingsFile(
                "ProjectSettings/TimeManager.asset",
                "Fixed Timestep: ",
                "Fixed Timestep: 0.01389");
            Time.fixedDeltaTime = 0.01389f;
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Fixed Timestep → 0.01389 (72Hz)");

            // ── 5. Audio (ProjectSettings 파일 직접 수정) ──
            ModifyProjectSettingsFile(
                "ProjectSettings/AudioManager.asset",
                "m_DSPBufferSize: ",
                "m_DSPBufferSize: 256");
            var audioConfig = AudioSettings.GetConfiguration();
            audioConfig.dspBufferSize = 256;
            AudioSettings.Reset(audioConfig);
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] DSP Buffer Size → 256");

            // ── 6. Build Settings ──
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Texture Compression → ASTC");

            EditorUserBuildSettings.SetPlatformSettings(
                BuildPipeline.GetBuildTargetName(BuildTarget.Android),
                "BuildCompression",
                "Lz4");
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Compression Method → LZ4");

            // ── 7. OVR Overlay 레이어 등록 ──
            var layerIssues = new List<string>();
            bool overlayLayerOk = RegisterLayer(3, "Overlay UI", layerIssues);
            bool overlayCanvasLayerOk = RegisterLayer(31, "OVROverlayCanvas Rendering", layerIssues);
            if (overlayLayerOk && overlayCanvasLayerOk)
            {
                appliedCount++;
                Debug.Log("[DDOITSetupWindow] OVR Overlay 레이어 등록 완료");
            }
            else
            {
                string issueText = string.Join("\n", layerIssues);
                Debug.LogWarning($"[DDOITSetupWindow] OVR Overlay 레이어 등록 충돌:\n{issueText}");
                EditorUtility.DisplayDialog(
                    "레이어 충돌",
                    "일부 레이어가 이미 다른 이름으로 사용 중이라 자동 등록하지 않았습니다.\n\n" +
                    issueText,
                    "확인");
            }

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
            if (IsInstallingDependencies)
            {
                Debug.LogWarning("[DDOITSetupWindow] 패키지 설치가 이미 진행 중입니다.");
                return;
            }

            var allDeps = new List<DependencyInfo>(REQUIRED_DEPENDENCIES);
            allDeps.AddRange(OPTIONAL_DEPENDENCIES);

            var targets = allDeps.Where(RequiresInstallOrUpdate).ToList();
            if (targets.Count == 0)
            {
                Debug.Log("[DDOITSetupWindow] 모든 패키지가 이미 설치되어 있습니다.");
                return;
            }

            PendingDependencyInstalls.Clear();
            foreach (var dep in targets)
                PendingDependencyInstalls.Enqueue(dep);

            IsInstallingDependencies = true;
            EditorApplication.update += ProcessDependencyInstallQueue;
            StartNextDependencyInstall();
        }

        private static void ProcessDependencyInstallQueue()
        {
            if (ActiveAddRequest == null || !ActiveAddRequest.IsCompleted)
                return;

            if (ActiveAddRequest.Status == StatusCode.Success)
            {
                Debug.Log($"[DDOITSetupWindow] 설치 완료: {ActiveDependency.displayName}");
            }
            else
            {
                string errorMessage = ActiveAddRequest.Error != null
                    ? ActiveAddRequest.Error.message
                    : "알 수 없는 Package Manager 오류";
                Debug.LogError(
                    $"[DDOITSetupWindow] 설치 실패: {ActiveDependency.displayName}\n" +
                    errorMessage);
            }

            ActiveAddRequest = null;
            StartNextDependencyInstall();
        }

        private static void StartNextDependencyInstall()
        {
            if (PendingDependencyInstalls.Count == 0)
            {
                IsInstallingDependencies = false;
                EditorApplication.update -= ProcessDependencyInstallQueue;
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "패키지 설치",
                    "패키지 설치 요청을 모두 처리했습니다.\n\n" +
                    "Meta XR SDK는 Unity Asset Store에서 먼저 '내 에셋에 추가'해야 설치됩니다.",
                    "확인");

                RepaintOpenSetupWindows();
                return;
            }

            ActiveDependency = PendingDependencyInstalls.Dequeue();
            string packageSpec = GetPackageAddSpec(ActiveDependency);
            Debug.Log($"[DDOITSetupWindow] 설치 요청: {ActiveDependency.displayName} ({packageSpec})");
            ActiveAddRequest = Client.Add(packageSpec);
            RepaintOpenSetupWindows();
        }

        private static string GetPackageAddSpec(DependencyInfo dep)
        {
            if (IsUrl(dep.versionOrUrl))
                return dep.versionOrUrl;

            return $"{dep.packageId}@{dep.versionOrUrl}";
        }

        private static void RepaintOpenSetupWindows()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<DDOITSetupWindow>())
                window.Repaint();
        }

        private static void ExecuteInitProject()
        {
            // 1. 폴더 템플릿 생성
            foreach (var folder in PROJECT_FOLDERS)
                EnsureAssetFolder($"Assets/{folder}");

            foreach (var folder in PROJECT_SUBFOLDERS)
                EnsureAssetFolder($"Assets/{folder}");

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

                    if (!AssetDatabase.DeleteAsset(dst))
                    {
                        Debug.LogError($"[DDOITSetupWindow] 기존 씬 삭제 실패: {dst}");
                        continue;
                    }
                }

                if (AssetDatabase.CopyAsset(src, dst))
                {
                    copiedCount++;
                }
                else
                {
                    Debug.LogError($"[DDOITSetupWindow] 씬 복사 실패: {src} → {dst}");
                }
            }

            // 4. AI Agent 문서 배포
            CopyAgentDocsToProjectRoot();

            AssetDatabase.Refresh();
            int folderCount = PROJECT_FOLDERS.Length + PROJECT_SUBFOLDERS.Length;
            Debug.Log($"[DDOITSetupWindow] 프로젝트 초기화 완료 (폴더 {folderCount}개, 씬 {copiedCount}개 복사)");
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            string parentPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.IsNullOrEmpty(parentPath))
                return;

            EnsureAssetFolder(parentPath);
            AssetDatabase.CreateFolder(parentPath, Path.GetFileName(assetPath));
        }

        /// <summary>
        /// 패키지 내 AI Agent 문서를 프로젝트 루트에 복사한다. 이미 있으면 건너뛴다.
        /// </summary>
        private static void CopyAgentDocsToProjectRoot()
        {
            CopyAgentDocToProjectRoot("AGENTS.md");
            CopyAgentDocToProjectRoot("CLAUDE.md");
        }

        /// <summary>
        /// 지정한 AI Agent 문서를 개발자 모드 또는 UPM 모드 경로에서 찾아 프로젝트 루트로 복사한다.
        /// </summary>
        /// <param name="fileName">복사할 문서 파일명.</param>
        private static void CopyAgentDocToProjectRoot(string fileName)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string destPath = Path.Combine(projectRoot, fileName);

            if (File.Exists(destPath)) return;

            // 개발자 모드
            string devPath = Path.Combine(Application.dataPath, "DDOIT_Tools", "MDs", fileName);
            if (File.Exists(devPath))
            {
                File.Copy(devPath, destPath);
                Debug.Log($"[DDOITSetupWindow] {fileName} → 프로젝트 루트에 배포 완료");
                return;
            }

            // UPM 모드
            string upmPath = Path.GetFullPath(Path.Combine("Packages", "com.ddoit.tools", "MDs", fileName));
            if (File.Exists(upmPath))
            {
                File.Copy(upmPath, destPath);
                Debug.Log($"[DDOITSetupWindow] {fileName} → 프로젝트 루트에 배포 완료");
            }
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

        /// <summary>
        /// URP Global Settings에서 Volume Profile 프로퍼티를 이름 기반으로 찾아 교체한다.
        /// 배열 인덱스에 의존하지 않아 프로젝트마다 안전하게 동작한다.
        /// </summary>
        private static void ApplyGlobalVolumeProfile(ScriptableObject profile)
        {
            // URP Global Settings를 타입 이름으로 찾기 (asmdef 참조 없이)
            var allSettings = Resources.FindObjectsOfTypeAll<ScriptableObject>();
            ScriptableObject globalSettings = null;
            foreach (var s in allSettings)
            {
                if (s.GetType().Name == "UniversalRenderPipelineGlobalSettings")
                {
                    globalSettings = s;
                    break;
                }
            }

            if (globalSettings == null) return;

            var so = new SerializedObject(globalSettings);
            var prop = so.GetIterator();
            prop.Next(true);
            do
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                    (prop.name == "m_DefaultSettingsVolumeProfile" || prop.name == "m_VolumeProfile") &&
                    prop.objectReferenceValue != null &&
                    prop.objectReferenceValue.GetType().Name == "VolumeProfile")
                {
                    prop.objectReferenceValue = profile;
                }
            } while (prop.Next(true));

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// ProjectSettings YAML 파일의 특정 키를 찾아 줄 전체를 교체한다.
        /// </summary>
        /// <summary>
        /// ProjectSettings/QualitySettings.asset의 m_PerPlatformDefaultQuality.Android 값을 변경한다.
        /// 변경된 값은 Editor 재시작/Build Target 전환 시에도 유지된다.
        /// </summary>
        private static void ApplyAndroidDefaultQuality(int level)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", "ProjectSettings", "QualitySettings.asset");
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("[DDOITSetupWindow] QualitySettings.asset 파일 없음");
                return;
            }

            var lines = File.ReadAllLines(fullPath);
            bool inDefaultQuality = false;
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("m_PerPlatformDefaultQuality:"))
                {
                    inDefaultQuality = true;
                    continue;
                }

                if (inDefaultQuality)
                {
                    if (trimmed.StartsWith("Android:"))
                    {
                        string indent = lines[i].Substring(0, lines[i].Length - trimmed.Length);
                        lines[i] = indent + "Android: " + level;
                        break;
                    }
                    // 같은 들여쓰기 수준 끝 = section 종료
                    if (lines[i].Length > 0 && !char.IsWhiteSpace(lines[i][0]) ||
                        (lines[i].StartsWith("  ") && !lines[i].StartsWith("    ")))
                    {
                        // 이미 다른 최상위 키로 넘어감
                        break;
                    }
                }
            }

            File.WriteAllLines(fullPath, lines);
        }

        private static void ModifyProjectSettingsFile(string relativePath, string keyPrefix, string newLine)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", relativePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[DDOITSetupWindow] 파일 없음: {relativePath}");
                return;
            }

            var lines = File.ReadAllLines(fullPath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith(keyPrefix))
                {
                    string indent = lines[i].Substring(0, lines[i].Length - lines[i].TrimStart().Length);
                    lines[i] = indent + newLine;
                    break;
                }
            }

            File.WriteAllLines(fullPath, lines);
        }

        /// <summary>
        /// TagManager에 레이어를 등록한다. 이미 등록되어 있으면 건너뛴다.
        /// </summary>
        private static bool RegisterLayer(int index, string layerName, List<string> issues)
        {
            string tagManagerPath = Path.Combine(Application.dataPath, "..", "ProjectSettings", "TagManager.asset");
            if (!File.Exists(tagManagerPath))
            {
                issues.Add("TagManager.asset 파일을 찾을 수 없습니다.");
                return false;
            }

            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAsset == null || tagManagerAsset.Length == 0)
            {
                issues.Add("TagManager.asset을 로드할 수 없습니다.");
                return false;
            }

            var so = new SerializedObject(tagManagerAsset[0]);
            var layersProp = so.FindProperty("layers");
            if (layersProp == null || index >= layersProp.arraySize)
            {
                issues.Add($"Layer {index}에 접근할 수 없습니다.");
                return false;
            }

            var element = layersProp.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = layerName;
                so.ApplyModifiedProperties();
                return true;
            }

            if (element.stringValue == layerName)
                return true;

            issues.Add($"Layer {index}: 이미 '{element.stringValue}'로 사용 중입니다. 필요한 이름: '{layerName}'");
            return false;
        }

        private static bool RequiresInstallOrUpdate(DependencyInfo dep)
        {
            return GetDependencyStatus(dep).state != DependencyState.Installed;
        }

        private static DependencyStatus GetDependencyStatus(DependencyInfo dep)
        {
            string manifestVersion = GetManifestDependencyVersion(dep.packageId);
            string resolvedVersion = GetResolvedPackageVersion(dep.packageId);
            string installedVersion = resolvedVersion ?? manifestVersion;

            if (string.IsNullOrEmpty(installedVersion))
                return new DependencyStatus(DependencyState.Missing, null);

            switch (dep.versionPolicy)
            {
                case DependencyVersionPolicy.PresenceOnly:
                    return new DependencyStatus(DependencyState.Installed, installedVersion);

                case DependencyVersionPolicy.MinimumVersion:
                    return IsVersionAtLeast(installedVersion, dep.versionOrUrl)
                        ? new DependencyStatus(DependencyState.Installed, installedVersion)
                        : new DependencyStatus(DependencyState.VersionMismatch, installedVersion);

                default:
                    return installedVersion == dep.versionOrUrl
                        ? new DependencyStatus(DependencyState.Installed, installedVersion)
                        : new DependencyStatus(DependencyState.VersionMismatch, installedVersion);
            }
        }

        private static bool IsVersionAtLeast(string installedVersion, string minimumVersion)
        {
            if (System.Version.TryParse(NormalizeVersion(installedVersion), out var installed) &&
                System.Version.TryParse(NormalizeVersion(minimumVersion), out var minimum))
            {
                return installed >= minimum;
            }

            return installedVersion == minimumVersion;
        }

        private static string NormalizeVersion(string version)
        {
            int suffixIndex = version.IndexOf('-');
            if (suffixIndex >= 0)
                version = version.Substring(0, suffixIndex);

            return version;
        }

        private static string GetDependencyRequirementText(DependencyInfo dep)
        {
            switch (dep.versionPolicy)
            {
                case DependencyVersionPolicy.PresenceOnly:
                    return "설치됨";
                case DependencyVersionPolicy.MinimumVersion:
                    return $"{dep.versionOrUrl} 이상";
                default:
                    return dep.versionOrUrl;
            }
        }

        private static string GetResolvedPackageVersion(string packageId)
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageId);
            return packageInfo != null ? packageInfo.version : null;
        }

        private static string GetManifestDependencyVersion(string packageId)
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath)) return null;

            string content = File.ReadAllText(manifestPath);
            string pattern = $"\"{packageId}\"\\s*:\\s*\"([^\"]+)\"";
            var match = System.Text.RegularExpressions.Regex.Match(content, pattern);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string GetDependencyStatusLabel(DependencyState state)
        {
            switch (state)
            {
                case DependencyState.Installed:
                    return "OK";
                case DependencyState.VersionMismatch:
                    return "UPD";
                default:
                    return "X";
            }
        }

        private static bool IsUrl(string value)
        {
            return value.StartsWith("http://") || value.StartsWith("https://") || value.StartsWith("git://");
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
