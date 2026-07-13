using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

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

        private const string OPENXR_PACKAGE_ID = "com.unity.xr.openxr";
        private const string DDOIT_PACKAGE_ID = "com.ddoit.tools";
        private const string DDOIT_GIT_URL = "https://github.com/DDOIT-OFFICIAL/DDOIT_Tools.git";
        private const string DDOIT_GITHUB_TAGS_API_URL =
            "https://api.github.com/repos/DDOIT-OFFICIAL/DDOIT_Tools/tags?per_page=100";

        private static readonly DependencyInfo[] REQUIRED_DEPENDENCIES =
        {
            new DependencyInfo("Meta XR All-in-One SDK", "com.meta.xr.sdk.all", "203.0.0", DependencyVersionPolicy.ExactVersion,
                "Unity Asset Store에서 먼저 내 에셋에 추가해야 합니다. (Audio/Voice 모듈만 v85.0.0 유지)"),
            new DependencyInfo("Input System", "com.unity.inputsystem", "1.18.0", DependencyVersionPolicy.MinimumVersion, null),
            new DependencyInfo("TextMeshPro", "com.unity.textmeshpro", "4.0.0", DependencyVersionPolicy.MinimumVersion, null),
            new DependencyInfo("Addressables", "com.unity.addressables", "2.8.1", DependencyVersionPolicy.MinimumVersion, null),
            new DependencyInfo("XR Management", "com.unity.xr.management", "4.5.4", DependencyVersionPolicy.MinimumVersion, null),
            new DependencyInfo("OpenXR Plugin", OPENXR_PACKAGE_ID, "1.17.1", DependencyVersionPolicy.MinimumVersion, null),
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
        private const string PACKAGE_UPDATE_PENDING_KEY = "DDOIT_SetupWindow_PackageUpdatePending";
        private const string PACKAGE_UPDATE_TARGET_VERSION_KEY = "DDOIT_SetupWindow_PackageUpdateTargetVersion";
        private const string IMPORT_WORKER_RESTORE_PENDING_KEY = "DDOIT_SetupWindow_ImportWorkerRestorePending";
        private const string IMPORT_WORKER_PREVIOUS_COUNT_KEY = "DDOIT_SetupWindow_ImportWorkerPreviousCount";
        private const string OPENXR_LOADER_TYPE_NAME = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string CURRENT_OPENXR_API_VERSION_FALLBACK = "1.1.54";
        private const string OPENXR_TARGET_API_WARNING_FRAGMENT = "targets an API version with a patch version lower than";
        private const string OPENXR_FEATURES_REFRESHED_SESSION_KEY = "com.unity.xr.openxr.featuresRefreshed";
        private const string OVR_DISABLE_HAND_PINCH_BUTTON_MAPPING_DEFINE = "OVR_DISABLE_HAND_PINCH_BUTTON_MAPPING";
        private const string USE_INPUT_SYSTEM_POSE_CONTROL_DEFINE = "USE_INPUT_SYSTEM_POSE_CONTROL";
        private const string USE_STICK_CONTROL_THUMBSTICKS_DEFINE = "USE_STICK_CONTROL_THUMBSTICKS";
        private const double XR_VALIDATION_STARTUP_CLEANUP_DELAY_SECONDS = 0.75d;
        private const double XR_VALIDATION_STARTUP_CLEANUP_INTERVAL_SECONDS = 0.5d;
        private const double XR_VALIDATION_STARTUP_CLEANUP_TIMEOUT_SECONDS = 180.0d;
        private const int XR_VALIDATION_STARTUP_CLEANUP_STABLE_PASS_COUNT = 6;

        private const string DDOIT_DATA_FOLDER = "Assets/DDOIT_Tools/Data";
        private const string PACKAGE_DATA_PATH_DEV = "Assets/DDOIT_Tools/Data";
        private const string PACKAGE_DATA_PATH_UPM = "Packages/com.ddoit.tools/Data";

        private static readonly string[] XR_VALIDATION_DEFINE_SYMBOLS =
        {
            OVR_DISABLE_HAND_PINCH_BUTTON_MAPPING_DEFINE,
            USE_INPUT_SYSTEM_POSE_CONTROL_DEFINE,
            USE_STICK_CONTROL_THUMBSTICKS_DEFINE,
        };

        private static readonly string[] COMMON_OPENXR_FEATURES =
        {
            "Meta.XR.MetaXRFeature",
            "Meta.XR.MetaXRFoveationFeature",
            "UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile",
            "Meta.XR.OculusTouchControllerProximityProfile",
        };

        private static readonly string[] OPTIMIZE_ASSETS =
        {
            "DDOIT_RPAsset.asset",
            "DDOIT_Renderer.asset",
            "DDOIT_VolumeProfile.asset",
        };

        private const string OPTIMIZE_DEST_FOLDER = "Assets/Settings/DDOIT";
        private const int MOBILE_QUALITY_INDEX = 0;
        private const float QUEST_FIXED_TIMESTEP = 0.01389f;
        private const int QUEST_DSP_BUFFER_SIZE = 256;
        private const string ANDROID_BUILD_COMPRESSION = "Lz4";

        private static readonly string[] OPTIMIZE_AFFECTED_PATHS =
        {
            "Assets/Settings/DDOIT/DDOIT_RPAsset.asset",
            "Assets/Settings/DDOIT/DDOIT_Renderer.asset",
            "Assets/Settings/DDOIT/DDOIT_VolumeProfile.asset",
            "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset",
            "ProjectSettings/GraphicsSettings.asset",
            "ProjectSettings/QualitySettings.asset",
            "ProjectSettings/ProjectSettings.asset",
            "ProjectSettings/TimeManager.asset",
            "ProjectSettings/AudioManager.asset",
            "ProjectSettings/TagManager.asset",
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

        private sealed class OptimizePreflightResult
        {
            public readonly List<string> changes = new List<string>();
            public readonly List<string> warnings = new List<string>();
            public readonly List<string> errors = new List<string>();

            public bool CanApply => errors.Count == 0;
        }

        [DataContract]
        private sealed class GitHubTagInfo
        {
            [DataMember(Name = "name")]
            private string _name;

            public string Name => _name;
        }

        #endregion

        #region Private Fields

        private Vector2 _scrollPosition;
        private static readonly List<string> DependencyInstallErrors = new List<string>();
        private static AddAndRemoveRequest ActiveDependencyInstallRequest;
        private static UnityWebRequest ActivePackageVersionCheckRequest;
        private static AddRequest ActivePackageUpdateRequest;
        private static string ActiveInstallTargetLabel;
        private static bool IsInstallingDependencies;
        private static bool IsCheckingForPackageUpdate;
        private static bool IsUpdatingPackage;
        private static string ActiveInstallScopeLabel;
        private static string LastDependencyVerificationReport;
        private static MessageType LastDependencyVerificationMessageType = MessageType.Info;
        private static string PackageUpdateStatusMessage;
        private static MessageType PackageUpdateStatusMessageType = MessageType.Info;
        private static bool ApplyTimingOptimization = true;
        private static bool ApplyAudioOptimization = true;
        private static bool IsStartupInitializationScheduled;
        private static bool IsXRValidationStartupCleanupScheduled;
        private static double XRValidationStartupCleanupNextRunTime;
        private static double XRValidationStartupCleanupDeadline;
        private static int XRValidationStartupCleanupStablePasses;

        #endregion

        #region Auto Open

        static DDOITSetupWindow()
        {
            ScheduleStartupInitialization();
        }

        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            ScheduleStartupInitialization();
        }

        private static void ScheduleStartupInitialization()
        {
            if (IsStartupInitializationScheduled)
                return;

            IsStartupInitializationScheduled = true;
            EditorApplication.delayCall += InitializeAfterLoad;
        }

        private static void InitializeAfterLoad()
        {
            IsStartupInitializationScheduled = false;

            // EditorWindow restoration can initialize this type inside ScriptableObject construction.
            // Delay Unity native API access until that restoration has completed.
            ScheduleStartupXRValidationCleanup();
            RestorePackageUpdateStatusAfterReload();
            ShowOnFirstLoad();
            RestoreImportWorkersAfterOpenXRInstall();
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

        private static void ScheduleStartupXRValidationCleanup()
        {
            if (Application.isBatchMode || IsXRValidationStartupCleanupScheduled)
                return;

            IsXRValidationStartupCleanupScheduled = true;
            XRValidationStartupCleanupStablePasses = 0;

            double now = EditorApplication.timeSinceStartup;
            XRValidationStartupCleanupNextRunTime = now + XR_VALIDATION_STARTUP_CLEANUP_DELAY_SECONDS;
            XRValidationStartupCleanupDeadline = now + XR_VALIDATION_STARTUP_CLEANUP_TIMEOUT_SECONDS;

            EditorApplication.update += RunStartupXRValidationCleanup;
        }

        private static void StopStartupXRValidationCleanup()
        {
            if (!IsXRValidationStartupCleanupScheduled)
                return;

            IsXRValidationStartupCleanupScheduled = false;
            EditorApplication.update -= RunStartupXRValidationCleanup;
        }

        private static void RunStartupXRValidationCleanup()
        {
            double now = EditorApplication.timeSinceStartup;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                XRValidationStartupCleanupNextRunTime = now + XR_VALIDATION_STARTUP_CLEANUP_INTERVAL_SECONDS;
                return;
            }

            if (now < XRValidationStartupCleanupNextRunTime)
                return;

            try
            {
                if (!IsOpenXRTargetApiStartupCleanupRelevant())
                {
                    StopStartupXRValidationCleanup();
                    return;
                }

                bool validationRulesInitialized = HasOpenXRValidationRulesInitialized();
                if (!validationRulesInitialized)
                {
                    if (now >= XRValidationStartupCleanupDeadline)
                    {
                        StopStartupXRValidationCleanup();
                        Debug.LogWarning(
                            "[DDOITSetupWindow] OpenXR target API validation cleanup timed out before OpenXR validation rules were initialized.");
                        return;
                    }

                    XRValidationStartupCleanupNextRunTime = now + XR_VALIDATION_STARTUP_CLEANUP_INTERVAL_SECONDS;
                    return;
                }

                var warnings = new List<string>();
                int applied = ApplyOpenXRTargetApiPatchWarningCleanup(warnings);
                if (applied > 0)
                    AssetDatabase.SaveAssets();

                int remaining = CountOpenXRTargetApiBuildValidationIssues(BuildTargetGroup.Android)
                              + CountOpenXRTargetApiBuildValidationIssues(BuildTargetGroup.Standalone);

                XRValidationStartupCleanupStablePasses = remaining == 0
                    ? XRValidationStartupCleanupStablePasses + 1
                    : 0;

                if (XRValidationStartupCleanupStablePasses >= XR_VALIDATION_STARTUP_CLEANUP_STABLE_PASS_COUNT
                    || now >= XRValidationStartupCleanupDeadline)
                {
                    StopStartupXRValidationCleanup();

                    if (remaining > 0)
                    {
                        Debug.LogWarning(
                            $"[DDOITSetupWindow] OpenXR target API validation cleanup timed out. Remaining warning count: {remaining}");
                    }

                    return;
                }

                XRValidationStartupCleanupNextRunTime = now + XR_VALIDATION_STARTUP_CLEANUP_INTERVAL_SECONDS;
            }
            catch (Exception ex)
            {
                StopStartupXRValidationCleanup();
                Debug.LogWarning($"[DDOITSetupWindow] Startup XR validation cleanup failed: {ex.Message}");
            }
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

            DrawPackageUpdateSection();

            DrawSeparator();

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

        #region Package Update Section

        private void DrawPackageUpdateSection()
        {
            EditorGUILayout.LabelField("DDOIT Tools 패키지", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            PackageManagerPackageInfo packageInfo = GetInstalledDDOITPackageInfo();
            if (packageInfo == null)
            {
                EditorGUILayout.HelpBox(
                    "현재 프로젝트는 Assets/DDOIT_Tools 개발 원본 모드입니다. " +
                    "최신 릴리스 조회는 가능하지만 실제 업데이트는 Git UPM 소비 프로젝트에서만 사용할 수 있습니다.",
                    MessageType.Info);

                EditorGUILayout.Space(4);
                EditorGUI.BeginDisabledGroup(IsPackageOperationInProgress);
                if (GUILayout.Button("최신 릴리스 확인", GUILayout.Height(28)))
                    CheckForPackageUpdate();
                EditorGUI.EndDisabledGroup();

                DrawPackageUpdateStatus();
                return;
            }

            EditorGUILayout.LabelField("설치 버전", packageInfo.version);
            EditorGUILayout.LabelField("설치 소스", packageInfo.source.ToString());

            if (packageInfo.source != PackageSource.Git)
            {
                EditorGUILayout.HelpBox(
                    "자체 업데이트는 Git URL로 설치된 DDOIT Tools 패키지에서만 사용할 수 있습니다.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(IsPackageOperationInProgress);
            if (GUILayout.Button("최신 릴리스 확인/업데이트", GUILayout.Height(28)))
                CheckForPackageUpdate();
            EditorGUI.EndDisabledGroup();

            DrawPackageUpdateStatus();
        }

        private static void DrawPackageUpdateStatus()
        {
            if (!string.IsNullOrEmpty(PackageUpdateStatusMessage))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(PackageUpdateStatusMessage, PackageUpdateStatusMessageType);
            }
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

            bool hasRequiredMissing = REQUIRED_DEPENDENCIES.Any(RequiresInstallOrUpdate);
            bool hasOptionalMissing = OPTIONAL_DEPENDENCIES.Any(RequiresInstallOrUpdate);

            EditorGUI.BeginDisabledGroup(!hasRequiredMissing || IsPackageOperationInProgress);
            if (GUILayout.Button("필수 패키지 설치/업데이트", GUILayout.Height(28)))
                InstallMissingDependencies(REQUIRED_DEPENDENCIES, "필수 패키지");
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!hasOptionalMissing || IsPackageOperationInProgress);
            if (GUILayout.Button("권장 도구 설치/업데이트", GUILayout.Height(24)))
                InstallMissingDependencies(OPTIONAL_DEPENDENCIES, "권장 도구");
            EditorGUI.EndDisabledGroup();

            if (IsInstallingDependencies)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox($"패키지 설치 진행 중: {ActiveInstallTargetLabel}", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(LastDependencyVerificationReport))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox(LastDependencyVerificationReport, LastDependencyVerificationMessageType);
            }

            if (!hasRequiredMissing && !hasOptionalMissing)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox("모든 의존성 패키지가 설치되어 있습니다.", MessageType.Info);
            }
            else if (!hasRequiredMissing)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.HelpBox("필수 의존성 패키지는 모두 설치되어 있습니다. 권장 도구는 필요 시 별도로 설치하세요.", MessageType.Info);
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

            EditorGUILayout.LabelField("선택 적용 항목:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            ApplyTimingOptimization = EditorGUILayout.ToggleLeft(
                "Fixed Timestep 72Hz 적용 (0.01389)",
                ApplyTimingOptimization);
            ApplyAudioOptimization = EditorGUILayout.ToggleLeft(
                "Audio DSP Buffer 256 적용 (Best Latency)",
                ApplyAudioOptimization);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Optimize 실행 전 현재 설정, 변경 예정 항목, OpenXR validation warning을 다시 확인합니다.",
                MessageType.None);

            if (GUILayout.Button("Optimize Project", GUILayout.Height(32)))
                ExecuteOptimizeProject();
        }

        private static void ExecuteOptimizeProject()
        {
            var preflight = BuildOptimizePreflight();
            if (!preflight.CanApply)
            {
                EditorUtility.DisplayDialog(
                    "프로젝트 최적화 중단",
                    BuildOptimizePreflightMessage(preflight),
                    "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "프로젝트 최적화 Preflight",
                BuildOptimizePreflightMessage(preflight) + "\n\n" +
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
            string dstFolder = OPTIMIZE_DEST_FOLDER;
            EnsureAssetFolder(dstFolder);

            foreach (var assetName in OPTIMIZE_ASSETS)
            {
                string src = $"{srcDataPath}/{assetName}";
                string dst = $"{dstFolder}/{assetName}";

                if (!AssetExists(src))
                {
                    Debug.LogWarning($"[DDOITSetupWindow] 원본 에셋 없음: {src}");
                    continue;
                }

                if (!CopyOrReplaceAssetPreservingGuid(src, dst))
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

                var renderPipelineAsset = rpAsset as UnityEngine.Rendering.RenderPipelineAsset;

                // Quality 0(Mobile)을 먼저 활성화한 뒤, 해당 quality와 Graphics Settings에 RP를 명확히 적용한다.
                QualitySettings.SetQualityLevel(MOBILE_QUALITY_INDEX, true);
                SetQualityRenderPipeline(MOBILE_QUALITY_INDEX, renderPipelineAsset);
                QualitySettings.renderPipeline = renderPipelineAsset;
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = renderPipelineAsset;

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
            QualitySettings.SetQualityLevel(MOBILE_QUALITY_INDEX, true);
            ApplyAndroidDefaultQuality(MOBILE_QUALITY_INDEX);
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

            // ── 4. Physics ──
            if (ApplyTimingOptimization)
            {
                Time.fixedDeltaTime = QUEST_FIXED_TIMESTEP;
                appliedCount++;
                Debug.Log("[DDOITSetupWindow] Fixed Timestep → 0.01389 (72Hz)");
            }
            else
            {
                Debug.Log("[DDOITSetupWindow] Fixed Timestep 적용 건너뜀");
            }

            // ── 5. Audio ──
            if (ApplyAudioOptimization)
            {
                ApplyAudioDspBufferSize(QUEST_DSP_BUFFER_SIZE);
                appliedCount++;
                Debug.Log("[DDOITSetupWindow] DSP Buffer Size → 256");
            }
            else
            {
                Debug.Log("[DDOITSetupWindow] DSP Buffer Size 적용 건너뜀");
            }

            // ── 6. Build Settings ──
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            appliedCount++;
            Debug.Log("[DDOITSetupWindow] Texture Compression → ASTC");

            EditorUserBuildSettings.SetPlatformSettings(
                BuildPipeline.GetBuildTargetName(BuildTarget.Android),
                "BuildCompression",
                ANDROID_BUILD_COMPRESSION);
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

            // ── 8. XR Plug-in Management OpenXR Loader 등록 ──
            var openXrIssues = new List<string>();
            bool androidOpenXrChanged = EnsureOpenXRLoader(BuildTargetGroup.Android, openXrIssues);
            bool standaloneOpenXrChanged = EnsureOpenXRLoader(BuildTargetGroup.Standalone, openXrIssues);
            if (androidOpenXrChanged || standaloneOpenXrChanged)
            {
                appliedCount++;
                Debug.Log("[DDOITSetupWindow] XR Plug-in Management OpenXR loader 등록 완료");
            }

            if (openXrIssues.Count > 0)
            {
                string issueText = string.Join("\n", openXrIssues);
                Debug.LogWarning($"[DDOITSetupWindow] OpenXR loader 자동 등록 확인 필요:\n{issueText}");
                EditorUtility.DisplayDialog(
                    "OpenXR 설정 확인",
                    "일부 플랫폼의 OpenXR loader를 자동 등록하지 못했습니다.\n\n" +
                    issueText + "\n\n" +
                    "Project Settings > XR Plug-in Management에서 수동으로 확인하세요.",
                    "확인");
            }

            // -- 9. XR / Meta validation cleanup --
            var validationIssues = new List<string>();
            var validationWarnings = new List<string>();
            int validationAppliedCount = ApplyXRValidationCleanup(validationIssues, validationWarnings);
            if (validationAppliedCount > 0)
            {
                appliedCount += validationAppliedCount;
                Debug.Log($"[DDOITSetupWindow] XR/Meta validation cleanup applied ({validationAppliedCount} items)");
            }

            if (validationWarnings.Count > 0)
            {
                Debug.LogWarning($"[DDOITSetupWindow] XR/Meta validation cleanup warnings:\n{string.Join("\n", validationWarnings)}");
            }

            if (validationIssues.Count > 0)
            {
                string issueText = string.Join("\n", validationIssues);
                Debug.LogWarning($"[DDOITSetupWindow] XR/Meta validation cleanup needs manual check:\n{issueText}");
                EditorUtility.DisplayDialog(
                    "XR/Meta validation check",
                    "Some XR/Meta validation items could not be cleared automatically.\n\n" +
                    issueText,
                    "?뺤씤");
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "최적화 완료",
                $"Quest VR 최적화 설정이 적용되었습니다.\n" +
                $"총 {appliedCount}개 항목 적용.",
                "확인");

            Debug.Log($"[DDOITSetupWindow] 프로젝트 최적화 완료 ({appliedCount}개 항목)");
        }

        private static OptimizePreflightResult BuildOptimizePreflight()
        {
            var result = new OptimizePreflightResult();

            string srcDataPath = FindPackageDataPath();
            if (srcDataPath == null)
            {
                result.errors.Add("DDOIT_Tools Data 폴더를 찾을 수 없습니다.");
            }
            else
            {
                foreach (var assetName in OPTIMIZE_ASSETS)
                {
                    string src = $"{srcDataPath}/{assetName}";
                    string dst = $"{OPTIMIZE_DEST_FOLDER}/{assetName}";
                    if (!AssetExists(src))
                    {
                        result.errors.Add($"원본 최적화 에셋 없음: {src}");
                        continue;
                    }

                    if (File.Exists(Path.GetFullPath(dst)))
                    {
                        result.changes.Add($"{dst}: 기존 GUID를 보존하고 에셋 내용 갱신");
                    }
                    else
                    {
                        result.changes.Add($"{dst}: 새 에셋 복사 생성");
                    }
                }
            }

            AddChangeIfDifferent(
                result,
                "Graphics Default RP",
                GetAssetName(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline),
                "DDOIT_RPAsset");
            AddChangeIfDifferent(
                result,
                "Active Quality",
                $"{QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})",
                $"{MOBILE_QUALITY_INDEX} ({QualitySettings.names[MOBILE_QUALITY_INDEX]})");
            AddChangeIfDifferent(
                result,
                "Quality Mobile RP",
                GetQualityRenderPipelineName(MOBILE_QUALITY_INDEX),
                "DDOIT_RPAsset");

            int? androidDefaultQuality = GetAndroidDefaultQuality();
            AddChangeIfDifferent(
                result,
                "Android Default Quality",
                androidDefaultQuality.HasValue ? androidDefaultQuality.Value.ToString() : "없음",
                MOBILE_QUALITY_INDEX.ToString());

            AddChangeIfDifferent(result, "Color Space", PlayerSettings.colorSpace.ToString(), ColorSpace.Linear.ToString());
            AddChangeIfDifferent(
                result,
                "Android Graphics APIs",
                string.Join(", ", PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).Select(api => api.ToString())),
                "Vulkan, OpenGLES3");
            AddChangeIfDifferent(
                result,
                "Android Scripting Backend",
                PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android).ToString(),
                ScriptingImplementation.IL2CPP.ToString());
            AddChangeIfDifferent(
                result,
                "Android Target Architectures",
                PlayerSettings.Android.targetArchitectures.ToString(),
                AndroidArchitecture.ARM64.ToString());
            AddChangeIfDifferent(
                result,
                "Managed Stripping Level",
                PlayerSettings.GetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android).ToString(),
                ManagedStrippingLevel.Medium.ToString());
            AddChangeIfDifferent(
                result,
                "Android Min SDK",
                PlayerSettings.Android.minSdkVersion.ToString(),
                AndroidSdkVersions.AndroidApiLevel32.ToString());

            if (ApplyTimingOptimization)
            {
                AddChangeIfDifferent(
                    result,
                    "Fixed Timestep",
                    Time.fixedDeltaTime.ToString("0.#####"),
                    QUEST_FIXED_TIMESTEP.ToString("0.#####"));
            }
            else
            {
                result.warnings.Add("Fixed Timestep 적용이 선택 해제되어 현재 값을 유지합니다.");
            }

            var audioConfig = AudioSettings.GetConfiguration();
            if (ApplyAudioOptimization)
            {
                AddChangeIfDifferent(
                    result,
                    "DSP Buffer Size",
                    audioConfig.dspBufferSize.ToString(),
                    QUEST_DSP_BUFFER_SIZE.ToString());
            }
            else
            {
                result.warnings.Add("DSP Buffer Size 적용이 선택 해제되어 현재 값을 유지합니다.");
            }

            AddChangeIfDifferent(
                result,
                "Texture Compression",
                EditorUserBuildSettings.androidBuildSubtarget.ToString(),
                MobileTextureSubtarget.ASTC.ToString());
            AddChangeIfDifferent(
                result,
                "Build Compression",
                GetAndroidBuildCompression(),
                ANDROID_BUILD_COMPRESSION);

            AddLayerPreflight(result, 3, "Overlay UI");
            AddLayerPreflight(result, 31, "OVROverlayCanvas Rendering");
            AppendOpenXRPreflight(result);
            AppendXRValidationCleanupPreflight(result);

            if (result.changes.Count == 0)
                result.changes.Add("현재 선택된 최적화 항목은 이미 목표값과 일치합니다.");

            return result;
        }

        private static string BuildOptimizePreflightMessage(OptimizePreflightResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Quest VR 최적화 적용 전 확인 결과입니다.");
            sb.AppendLine("기존 DDOIT 설정 에셋은 삭제하지 않고 GUID를 보존한 채 내용만 갱신합니다.");

            AppendMessageSection(sb, "변경 예정", result.changes, "없음", 14);
            AppendMessageSection(sb, "주의 / 수동 확인", result.warnings, "없음", 10);
            AppendMessageSection(sb, "오류", result.errors, "없음", 10);
            AppendMessageSection(sb, "영향 파일", OPTIMIZE_AFFECTED_PATHS, "없음", 12);

            return sb.ToString().TrimEnd();
        }

        private static void AppendMessageSection(
            StringBuilder sb,
            string title,
            IList<string> items,
            string emptyText,
            int maxItems)
        {
            sb.AppendLine();
            sb.AppendLine($"[{title}]");
            if (items == null || items.Count == 0)
            {
                sb.AppendLine($"- {emptyText}");
                return;
            }

            int count = Mathf.Min(items.Count, maxItems);
            for (int i = 0; i < count; i++)
                sb.AppendLine($"- {items[i]}");

            if (items.Count > count)
                sb.AppendLine($"- ... 외 {items.Count - count}개");
        }

        private static void AddChangeIfDifferent(
            OptimizePreflightResult result,
            string label,
            string current,
            string target)
        {
            current = string.IsNullOrEmpty(current) ? "없음" : current;
            target = string.IsNullOrEmpty(target) ? "없음" : target;
            if (current != target)
                result.changes.Add($"{label}: {current} → {target}");
        }

        private static string GetAssetName(UnityEngine.Object asset)
        {
            return asset == null ? "없음" : asset.name;
        }

        private static string GetQualityRenderPipelineName(int qualityIndex)
        {
            var asset = GetProjectSettingsMainAsset("ProjectSettings/QualitySettings.asset");
            if (asset == null)
                return "확인 실패";

            var so = new SerializedObject(asset);
            var prop = so.FindProperty($"m_QualitySettings.Array.data[{qualityIndex}].customRenderPipeline");
            if (prop == null || prop.objectReferenceValue == null)
                return "없음";

            return prop.objectReferenceValue.name;
        }

        private static int? GetAndroidDefaultQuality()
        {
            var asset = GetProjectSettingsMainAsset("ProjectSettings/QualitySettings.asset");
            if (asset == null)
                return null;

            var so = new SerializedObject(asset);
            var defaultQuality = so.FindProperty("m_PerPlatformDefaultQuality");
            if (defaultQuality == null || !defaultQuality.isArray)
                return null;

            for (int i = 0; i < defaultQuality.arraySize; i++)
            {
                var item = defaultQuality.GetArrayElementAtIndex(i);
                var platform = item.FindPropertyRelative("first");
                var quality = item.FindPropertyRelative("second");
                if (platform != null && quality != null && platform.stringValue == "Android")
                    return quality.intValue;
            }

            return null;
        }

        private static string GetAndroidBuildCompression()
        {
            string value = EditorUserBuildSettings.GetPlatformSettings(
                BuildPipeline.GetBuildTargetName(BuildTarget.Android),
                "BuildCompression");
            return string.IsNullOrEmpty(value) ? "없음" : value;
        }

        private static void AddLayerPreflight(OptimizePreflightResult result, int index, string layerName)
        {
            string current = GetLayerName(index);
            if (current == null)
            {
                result.errors.Add($"Layer {index} 정보를 읽을 수 없습니다.");
            }
            else if (string.IsNullOrEmpty(current))
            {
                result.changes.Add($"Layer {index}: {layerName} 등록");
            }
            else if (current != layerName)
            {
                result.warnings.Add($"Layer {index}가 이미 '{current}'로 사용 중이라 '{layerName}' 자동 등록은 건너뜁니다.");
            }
        }

        private static string GetLayerName(int index)
        {
            var tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManagerAsset == null || tagManagerAsset.Length == 0)
                return null;

            var so = new SerializedObject(tagManagerAsset[0]);
            var layersProp = so.FindProperty("layers");
            if (layersProp == null || index >= layersProp.arraySize)
                return null;

            return layersProp.GetArrayElementAtIndex(index).stringValue;
        }

        private static void AppendOpenXRPreflight(OptimizePreflightResult result)
        {
            AppendOpenXRLoaderPreflight(result, BuildTargetGroup.Android);
            AppendOpenXRLoaderPreflight(result, BuildTargetGroup.Standalone);
            AppendOpenXRValidationIssues(result, BuildTargetGroup.Android);
            AppendOpenXRValidationIssues(result, BuildTargetGroup.Standalone);
            AppendOpenXRPredictedTimeWarning(result, BuildTargetGroup.Android);
            AppendOpenXRPredictedTimeWarning(result, BuildTargetGroup.Standalone);
        }

        private static void AppendOpenXRLoaderPreflight(OptimizePreflightResult result, BuildTargetGroup group)
        {
            string issue;
            if (IsOpenXRLoaderEnabled(group, out issue))
                return;

            if (!string.IsNullOrEmpty(issue))
            {
                result.errors.Add($"XR Plug-in Management {group}: {issue}");
                return;
            }

            result.changes.Add($"XR Plug-in Management {group}: OpenXR loader 등록");
        }

        private static void AppendOpenXRValidationIssues(OptimizePreflightResult result, BuildTargetGroup group)
        {
            Type validationType = FindType("UnityEditor.XR.OpenXR.OpenXRProjectValidation");
            Type ruleType = FindType("UnityEngine.XR.OpenXR.Features.OpenXRFeature+ValidationRule");
            if (validationType == null || ruleType == null)
                return;

            var method = validationType.GetMethod(
                "GetCurrentValidationIssues",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
                return;

            object issues = Activator.CreateInstance(typeof(List<>).MakeGenericType(ruleType));
            method.Invoke(null, new[] { issues, group });

            foreach (object issue in (IEnumerable)issues)
            {
                string message = GetReflectedString(issue, "message");
                bool isError = GetReflectedBool(issue, "error");
                string text = $"OpenXR {group}: {message}";
                if (isError)
                    result.errors.Add(text);
                else
                    result.warnings.Add(text);
            }
        }

        private static void AppendOpenXRPredictedTimeWarning(OptimizePreflightResult result, BuildTargetGroup group)
        {
            string openXrVersion = GetResolvedPackageVersion("com.unity.xr.openxr");
            if (string.IsNullOrEmpty(openXrVersion) || !IsVersionAtLeast(openXrVersion, "1.17.1"))
                return;

            Type settingsType = FindType("UnityEngine.XR.OpenXR.OpenXRSettings");
            if (settingsType == null)
                return;

            var method = settingsType.GetMethod(
                "GetSettingsForBuildTargetGroup",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
                return;

            var settings = method.Invoke(null, new object[] { group }) as ScriptableObject;
            if (settings == null)
                return;

            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_useOpenXRPredictedTime");
            if (prop != null && !prop.boolValue)
            {
                result.warnings.Add(
                    $"OpenXR {group}: Use OpenXR Predicted Time이 꺼져 있습니다. OpenXR 1.17.1+ 기본값은 켜짐이며, Optimize는 이 값을 자동 변경하지 않습니다.");
            }
        }

        private static void AppendXRValidationCleanupPreflight(OptimizePreflightResult result)
        {
            int buildValidatorIssues = CountCurrentBuildValidationIssues(BuildTargetGroup.Android)
                + CountCurrentBuildValidationIssues(BuildTargetGroup.Standalone);
            int metaIssues = CountCurrentMetaProjectSetupIssues(BuildTargetGroup.Android)
                + CountCurrentMetaProjectSetupIssues(BuildTargetGroup.Standalone);

            if (buildValidatorIssues > 0)
            {
                result.changes.Add(
                    $"XR Plug-in Management Project Validation: {buildValidatorIssues} fixable/known items will be normalized");
            }

            if (metaIssues > 0)
            {
                result.changes.Add(
                    $"Meta XR Project Setup: {metaIssues} automatic or safe manual items will be cleared");
            }
        }

        private static int ApplyXRValidationCleanup(List<string> issues, List<string> warnings)
        {
            int appliedCount = 0;

            appliedCount += ApplyDeterministicXRSettings(warnings);
            appliedCount += EnsureXRValidationDefineSymbols();
            appliedCount += EnsureInputSystemBackgroundBehavior(warnings);

            appliedCount += EnsureOpenXRFeatureBaseline(BuildTargetGroup.Android, issues);
            appliedCount += EnsureOpenXRFeatureBaseline(BuildTargetGroup.Standalone, issues);
            appliedCount += GenerateOrUpdateAndroidManifestSilently(warnings);

            appliedCount += ApplyMetaProjectSetupFixes(BuildTargetGroup.Android, issues, warnings);
            appliedCount += ApplyMetaProjectSetupFixes(BuildTargetGroup.Standalone, issues, warnings);

            appliedCount += ApplyBuildValidationFixes(BuildTargetGroup.Android, issues, warnings);
            appliedCount += ApplyBuildValidationFixes(BuildTargetGroup.Standalone, issues, warnings);

            appliedCount += ApplyOpenXRTargetApiPatchWarningCleanup(warnings);

            AssetDatabase.SaveAssets();
            return appliedCount;
        }

        private static int ApplyOpenXRTargetApiPatchWarningCleanup(List<string> warnings)
        {
            int appliedCount = 0;

            appliedCount += NormalizeOpenXRFeatureTargetApiVersions(BuildTargetGroup.Android, warnings);
            appliedCount += NormalizeOpenXRFeatureTargetApiVersions(BuildTargetGroup.Standalone, warnings);
            appliedCount += RemoveStaleOpenXRTargetApiValidationRules(BuildTargetGroup.Android);
            appliedCount += RemoveStaleOpenXRTargetApiValidationRules(BuildTargetGroup.Standalone);

            return appliedCount;
        }

        private static int ApplyDeterministicXRSettings(List<string> warnings)
        {
            int changed = 0;

            if (!PlayerSettings.runInBackground)
            {
                PlayerSettings.runInBackground = true;
                changed++;
            }

            if (!PlayerSettings.graphicsJobs)
            {
                PlayerSettings.graphicsJobs = true;
                changed++;
            }

            if (PlayerSettings.graphicsJobMode != UnityEditor.GraphicsJobMode.Legacy)
            {
                PlayerSettings.graphicsJobMode = UnityEditor.GraphicsJobMode.Legacy;
                changed++;
            }

            if (!PlayerSettings.MTRendering)
            {
                PlayerSettings.MTRendering = true;
                changed++;
            }

#pragma warning disable CS0618
            if (!PlayerSettings.GetMobileMTRendering(BuildTargetGroup.Android))
            {
                PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
                changed++;
            }
#pragma warning restore CS0618

            if (!PlayerSettings.use32BitDisplayBuffer)
            {
                PlayerSettings.use32BitDisplayBuffer = true;
                changed++;
            }

            if (PlayerSettings.stereoRenderingPath != StereoRenderingPath.Instancing)
            {
                PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
                changed++;
            }

            if (QualitySettings.pixelLightCount > 1)
            {
                QualitySettings.pixelLightCount = 1;
                changed++;
            }

#if UNITY_2022_2_OR_NEWER
            if (QualitySettings.globalTextureMipmapLimit != 0)
            {
                QualitySettings.globalTextureMipmapLimit = 0;
                changed++;
            }
#endif

            if (QualitySettings.anisotropicFiltering != AnisotropicFiltering.Enable)
            {
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                changed++;
            }

            changed += EnsureGraphicsApis(
                BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan, UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                changed += EnsureGraphicsApis(
                    BuildTarget.StandaloneWindows64,
                    new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
            }
            else
            {
                warnings.Add("Standalone Direct3D11 validation was skipped because the editor is not running on Windows.");
            }

            if (Enum.TryParse("AndroidApiLevel34", out AndroidSdkVersions targetSdkVersion)
                && PlayerSettings.Android.targetSdkVersion != targetSdkVersion)
            {
                PlayerSettings.Android.targetSdkVersion = targetSdkVersion;
                changed++;
            }

            if (PlayerSettings.Android.preferredInstallLocation != AndroidPreferredInstallLocation.Auto)
            {
                PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
                changed++;
            }

            if (PlayerSettings.Android.androidTVCompatibility)
            {
                PlayerSettings.Android.androidTVCompatibility = false;
                changed++;
            }

#if UNITY_2023_2_OR_NEWER
            if (PlayerSettings.Android.applicationEntry != AndroidApplicationEntry.GameActivity)
            {
                PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
                changed++;
            }
#endif

            return changed;
        }

        private static int EnsureGraphicsApis(
            BuildTarget buildTarget,
            UnityEngine.Rendering.GraphicsDeviceType[] expectedApis)
        {
            bool changed = PlayerSettings.GetUseDefaultGraphicsAPIs(buildTarget)
                || !PlayerSettings.GetGraphicsAPIs(buildTarget).SequenceEqual(expectedApis);

            if (!changed)
                return 0;

            PlayerSettings.SetUseDefaultGraphicsAPIs(buildTarget, false);
            PlayerSettings.SetGraphicsAPIs(buildTarget, expectedApis);
            return 1;
        }

        private static int EnsureXRValidationDefineSymbols()
        {
            int changed = 0;
            var targets = new[]
            {
                UnityEditor.Build.NamedBuildTarget.Android,
                UnityEditor.Build.NamedBuildTarget.Standalone,
            };

            foreach (var target in targets)
            {
                foreach (var symbol in XR_VALIDATION_DEFINE_SYMBOLS)
                {
                    changed += EnsureScriptingDefineSymbol(target, symbol);
                }
            }

            return changed;
        }

        private static int EnsureScriptingDefineSymbol(UnityEditor.Build.NamedBuildTarget target, string symbol)
        {
            string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(target);
            var symbols = currentSymbols
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .Distinct()
                .ToList();

            if (symbols.Contains(symbol))
                return 0;

            symbols.Add(symbol);
            PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", symbols));
            return 1;
        }

        private static int EnsureInputSystemBackgroundBehavior(List<string> warnings)
        {
            Type inputSystemType = FindType("UnityEngine.InputSystem.InputSystem");
            if (inputSystemType == null)
                return 0;

            var settingsProperty = inputSystemType.GetProperty(
                "settings",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            object settings = settingsProperty?.GetValue(null);
            if (settings == null)
                return 0;

            var backgroundProperty = settings.GetType().GetProperty(
                "backgroundBehavior",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (backgroundProperty == null || !backgroundProperty.PropertyType.IsEnum)
                return 0;

            try
            {
                object expected = Enum.Parse(backgroundProperty.PropertyType, "ResetAndDisableNonBackgroundDevices");
                object current = backgroundProperty.GetValue(settings);
                if (Equals(current, expected))
                    return 0;

                backgroundProperty.SetValue(settings, expected);
                if (settings is UnityEngine.Object unityObject)
                    EditorUtility.SetDirty(unityObject);
                return 1;
            }
            catch (Exception ex)
            {
                warnings.Add($"Input System background behavior could not be set automatically: {ex.Message}");
                return 0;
            }
        }

        private static int EnsureOpenXRFeatureBaseline(BuildTargetGroup group, List<string> issues)
        {
            int changed = 0;

            foreach (var featureTypeName in COMMON_OPENXR_FEATURES)
            {
                changed += SetOpenXRFeatureEnabled(group, featureTypeName, true, issues);
            }

            if (group == BuildTargetGroup.Android)
            {
                changed += SetOpenXRFeatureEnabled(group, "Meta.XR.MetaXRSubsampledLayout", true, issues);
            }

            if (changed > 0)
                RefreshOpenXRFeatures(group);

            return changed;
        }

        private static int SetOpenXRFeatureEnabled(
            BuildTargetGroup group,
            string featureTypeName,
            bool enabled,
            List<string> issues)
        {
            object settings = GetOpenXRSettings(group);
            if (settings == null)
                return 0;

            Type featureType = FindType(featureTypeName);
            if (featureType == null)
                return 0;

            var getFeatureMethod = settings.GetType().GetMethod(
                "GetFeature",
                new[] { typeof(Type) });
            if (getFeatureMethod == null)
            {
                issues.Add($"OpenXR {group}: GetFeature(Type) API not found.");
                return 0;
            }

            object feature = getFeatureMethod.Invoke(settings, new object[] { featureType });
            if (feature == null)
                return 0;

            var enabledProperty = feature.GetType().GetProperty(
                "enabled",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (enabledProperty == null)
            {
                issues.Add($"OpenXR {group}: enabled property not found for {featureTypeName}.");
                return 0;
            }

            bool current = enabledProperty.GetValue(feature) is bool boolValue && boolValue;
            if (current == enabled)
                return 0;

            enabledProperty.SetValue(feature, enabled);
            if (feature is UnityEngine.Object featureObject)
                EditorUtility.SetDirty(featureObject);
            if (settings is UnityEngine.Object settingsObject)
                EditorUtility.SetDirty(settingsObject);
            return 1;
        }

        private static int GenerateOrUpdateAndroidManifestSilently(List<string> warnings)
        {
            Type manifestType = FindType("OVRManifestPreprocessor");
            if (manifestType == null)
                return 0;

            var method = manifestType.GetMethod(
                "GenerateOrUpdateAndroidManifest",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
                return 0;

            try
            {
                bool shouldUpdate = true;
                var existsMethod = manifestType.GetMethod(
                    "DoesAndroidManifestExist",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var outdatedMethod = manifestType.GetMethod(
                    "IsManifestOutdated",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                bool exists = existsMethod?.Invoke(null, null) is bool existsValue && existsValue;
                if (exists && outdatedMethod != null)
                {
                    object[] args = { null };
                    bool outdated = outdatedMethod.Invoke(null, args) is bool outdatedValue && outdatedValue;
                    shouldUpdate = outdated;
                }

                if (!shouldUpdate)
                    return 0;

                method.Invoke(null, new object[] { true });
                return 1;
            }
            catch (Exception ex)
            {
                string exceptionMessage = ex.InnerException?.Message ?? ex.Message;
                warnings.Add($"Android Manifest silent update failed: {exceptionMessage}");
                return 0;
            }
        }

        private static int ApplyMetaProjectSetupFixes(
            BuildTargetGroup group,
            List<string> issues,
            List<string> warnings)
        {
            Type statusType = FindType("OVRProjectSetupStatus");
            if (statusType == null)
                return 0;

            var computeStatusMethod = statusType.GetMethod(
                "ComputeStatus",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (computeStatusMethod == null)
                return 0;

            int applied = 0;
            var skippedMessages = new HashSet<string>();

            for (int pass = 0; pass < 4; pass++)
            {
                object status = computeStatusMethod.Invoke(null, new object[] { group });
                var tasks = GetReflectedValue(status, "OutstandingTasks") as IEnumerable;
                if (tasks == null)
                    break;

                bool changedThisPass = false;
                foreach (object task in tasks)
                {
                    string message = GetOptionalLambdaValue(GetReflectedValue(task, "Message"), group);
                    string tags = GetReflectedString(task, "Tags");
                    bool hasFix = GetReflectedValue(task, "FixAction") != null;
                    bool hasAsyncFix = GetReflectedValue(task, "AsyncFixAction") != null;

                    if (tags.Contains("ManuallyFixable"))
                    {
                        if (IsSafePlatformSdkManualTask(message))
                        {
                            var setMarkedAsFixedMethod = task.GetType().GetMethod(
                                "SetMarkedAsFixed",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            setMarkedAsFixedMethod?.Invoke(task, new object[] { group, true });
                            applied++;
                            changedThisPass = true;
                        }
                        else if (skippedMessages.Add(message))
                        {
                            warnings.Add($"Meta XR {group}: manual task skipped - {message}");
                        }

                        continue;
                    }

                    if (!hasFix)
                    {
                        if (hasAsyncFix && skippedMessages.Add(message))
                            warnings.Add($"Meta XR {group}: async task skipped - {message}");
                        continue;
                    }

                    if (tags.Contains("HeavyProcessing"))
                    {
                        if (skippedMessages.Add(message))
                            warnings.Add($"Meta XR {group}: heavy task skipped after silent direct setup - {message}");
                        continue;
                    }

                    if (message.Contains("Manual selection of Graphic API, favoring Direct3D11"))
                    {
                        if (skippedMessages.Add(message))
                            issues.Add($"Meta XR {group}: Direct3D11 validation remained after direct setup - {message}");
                        continue;
                    }

                    var fixMethod = task.GetType().GetMethod(
                        "Fix",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (fixMethod == null)
                        continue;

                    bool fixedTask = false;
                    try
                    {
                        object result = fixMethod.Invoke(task, new object[] { group });
                        fixedTask = result is bool boolResult && boolResult;
                    }
                    catch (Exception ex)
                    {
                        string exceptionMessage = ex.InnerException?.Message ?? ex.Message;
                        if (skippedMessages.Add(message))
                            warnings.Add($"Meta XR {group}: failed to fix '{message}' - {exceptionMessage}");
                    }

                    if (fixedTask)
                    {
                        applied++;
                        changedThisPass = true;
                    }
                    else if (skippedMessages.Add(message))
                    {
                        warnings.Add($"Meta XR {group}: fix did not complete - {message}");
                    }
                }

                if (!changedThisPass)
                    break;
            }

            return applied;
        }

        private static int ApplyBuildValidationFixes(
            BuildTargetGroup group,
            List<string> issues,
            List<string> warnings)
        {
            Type validatorType = FindType("Unity.XR.CoreUtils.Editor.BuildValidator");
            Type ruleType = FindType("Unity.XR.CoreUtils.Editor.BuildValidationRule");
            if (validatorType == null || ruleType == null)
                return 0;

            var method = validatorType.GetMethod(
                "GetCurrentValidationIssues",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method == null)
                return 0;

            int applied = 0;
            var skippedMessages = new HashSet<string>();

            for (int pass = 0; pass < 4; pass++)
            {
                object failures = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(ruleType));
                method.Invoke(null, new object[] { failures, group });

                bool changedThisPass = false;
                foreach (object rule in (IEnumerable)failures)
                {
                    string message = GetReflectedString(rule, "Message");
                    if (message.Contains(OPENXR_TARGET_API_WARNING_FRAGMENT))
                        continue;

                    if (message.Contains("Manual selection of Graphic API, favoring Direct3D11"))
                    {
                        if (skippedMessages.Add(message))
                            issues.Add($"Project Validation {group}: Direct3D11 validation remained after direct setup - {message}");
                        continue;
                    }

                    bool automatic = GetReflectedBool(rule, "FixItAutomatic");
                    object fixIt = GetReflectedValue(rule, "FixIt");
                    if (!automatic || fixIt == null)
                    {
                        if (skippedMessages.Add(message))
                            warnings.Add($"Project Validation {group}: no automatic fix - {message}");
                        continue;
                    }

                    var isRuleEnabled = GetReflectedValue(rule, "IsRuleEnabled") as Func<bool>;
                    var checkPredicate = GetReflectedValue(rule, "CheckPredicate") as Func<bool>;
                    if (isRuleEnabled != null && !isRuleEnabled())
                        continue;
                    if (checkPredicate != null && checkPredicate())
                        continue;

                    try
                    {
                        ((Action)fixIt).Invoke();
                        applied++;
                        changedThisPass = true;
                    }
                    catch (Exception ex)
                    {
                        string exceptionMessage = ex.InnerException?.Message ?? ex.Message;
                        if (skippedMessages.Add(message))
                            warnings.Add($"Project Validation {group}: failed to fix '{message}' - {exceptionMessage}");
                    }
                }

                if (!changedThisPass)
                    break;
            }

            return applied;
        }

        private static int NormalizeOpenXRFeatureTargetApiVersions(BuildTargetGroup group, List<string> warnings)
        {
            object settings = GetOpenXRSettings(group);
            if (settings == null)
                return 0;

            var getFeaturesMethod = settings.GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .FirstOrDefault(method => method.Name == "GetFeatures"
                    && !method.IsGenericMethod
                    && method.GetParameters().Length == 0);
            if (getFeaturesMethod == null)
                return 0;

            string currentApiVersion = GetCurrentOpenXRApiVersion();
            int changed = 0;

            object features = getFeaturesMethod.Invoke(settings, null);
            foreach (object feature in (IEnumerable)features)
            {
                var enabledProperty = feature.GetType().GetProperty(
                    "enabled",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                bool enabled = enabledProperty?.GetValue(feature) is bool boolValue && boolValue;
                if (!enabled || !(feature is UnityEngine.Object featureObject))
                    continue;

                var serializedObject = new SerializedObject(featureObject);
                var targetVersionProperty = serializedObject.FindProperty("targetOpenXRApiVersion");
                if (targetVersionProperty == null || string.IsNullOrEmpty(targetVersionProperty.stringValue))
                    continue;

                if (!IsLowerPatchOpenXRApiVersion(targetVersionProperty.stringValue, currentApiVersion))
                    continue;

                string previousVersion = targetVersionProperty.stringValue;
                targetVersionProperty.stringValue = currentApiVersion;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(featureObject);
                changed++;

                warnings.Add(
                    $"OpenXR {group}: normalized {feature.GetType().Name} target API {previousVersion} -> {currentApiVersion}");
            }

            if (changed > 0 && settings is UnityEngine.Object settingsObject)
                EditorUtility.SetDirty(settingsObject);

            return changed;
        }

        private static int CountCurrentBuildValidationIssues(BuildTargetGroup group)
        {
            Type validatorType = FindType("Unity.XR.CoreUtils.Editor.BuildValidator");
            Type ruleType = FindType("Unity.XR.CoreUtils.Editor.BuildValidationRule");
            if (validatorType == null || ruleType == null)
                return 0;

            var method = validatorType.GetMethod(
                "GetCurrentValidationIssues",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method == null)
                return 0;

            object failures = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(ruleType));
            method.Invoke(null, new object[] { failures, group });

            int count = 0;
            foreach (object _ in (IEnumerable)failures)
                count++;

            return count;
        }

        private static int CountOpenXRTargetApiBuildValidationIssues(BuildTargetGroup group)
        {
            Type validatorType = FindType("Unity.XR.CoreUtils.Editor.BuildValidator");
            Type ruleType = FindType("Unity.XR.CoreUtils.Editor.BuildValidationRule");
            if (validatorType == null || ruleType == null)
                return 0;

            var method = validatorType.GetMethod(
                "GetCurrentValidationIssues",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (method == null)
                return 0;

            object failures = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(ruleType));
            method.Invoke(null, new object[] { failures, group });

            int count = 0;
            foreach (object rule in (IEnumerable)failures)
            {
                string message = GetReflectedString(rule, "Message");
                if (string.IsNullOrEmpty(message) || !message.Contains(OPENXR_TARGET_API_WARNING_FRAGMENT))
                    continue;

                var isRuleEnabled = GetReflectedValue(rule, "IsRuleEnabled") as Func<bool>;
                var checkPredicate = GetReflectedValue(rule, "CheckPredicate") as Func<bool>;
                if (isRuleEnabled != null && !isRuleEnabled())
                    continue;
                if (checkPredicate != null && checkPredicate())
                    continue;

                count++;
            }

            return count;
        }

        private static int CountCurrentMetaProjectSetupIssues(BuildTargetGroup group)
        {
            Type statusType = FindType("OVRProjectSetupStatus");
            if (statusType == null)
                return 0;

            var computeStatusMethod = statusType.GetMethod(
                "ComputeStatus",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (computeStatusMethod == null)
                return 0;

            object status = computeStatusMethod.Invoke(null, new object[] { group });
            object count = GetReflectedValue(status, "TotalOutstandingCount");
            return count is int intValue ? intValue : 0;
        }

        private static object GetOpenXRSettings(BuildTargetGroup group)
        {
            Type settingsType = FindType("UnityEngine.XR.OpenXR.OpenXRSettings");
            if (settingsType == null)
                return null;

            var method = settingsType.GetMethod(
                "GetSettingsForBuildTargetGroup",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return method?.Invoke(null, new object[] { group });
        }

        private static void RefreshOpenXRFeatures(BuildTargetGroup group)
        {
            Type helpersType = FindType("UnityEditor.XR.OpenXR.Features.FeatureHelpers");
            var method = helpersType?.GetMethod(
                "RefreshFeatures",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            method?.Invoke(null, new object[] { group });
        }

        private static string GetCurrentOpenXRApiVersion()
        {
            Type versionType = FindType("UnityEngine.XR.OpenXR.OpenXRApiVersion");
            var property = versionType?.GetProperty(
                "Current",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            object current = property?.GetValue(null);
            return current?.ToString() ?? CURRENT_OPENXR_API_VERSION_FALLBACK;
        }

        private static bool IsLowerPatchOpenXRApiVersion(string value, string target)
        {
            if (!Version.TryParse(value, out var currentVersion) || !Version.TryParse(target, out var targetVersion))
                return false;

            return currentVersion.Major == targetVersion.Major
                && currentVersion.Minor == targetVersion.Minor
                && currentVersion.Build >= 0
                && targetVersion.Build >= 0
                && currentVersion.Build < targetVersion.Build;
        }

        private static int RemoveStaleOpenXRTargetApiValidationRules(BuildTargetGroup group)
        {
            if (HasFreshOpenXRTargetApiValidationIssue(group))
                return 0;

            Type validatorType = FindType("Unity.XR.CoreUtils.Editor.BuildValidator");
            if (validatorType == null)
                return 0;

            var platformRulesField = validatorType.GetField(
                "s_PlatformRules",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var platformRules = platformRulesField?.GetValue(null) as System.Collections.IDictionary;
            if (platformRules == null || !platformRules.Contains(group))
                return 0;

            var rules = platformRules[group] as System.Collections.IList;
            if (rules == null)
                return 0;

            int removed = 0;
            for (int i = rules.Count - 1; i >= 0; i--)
            {
                object rule = rules[i];
                string message = GetReflectedString(rule, "Message");
                if (!message.Contains(OPENXR_TARGET_API_WARNING_FRAGMENT))
                    continue;

                rules.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        private static bool HasOpenXRTargetApiValidationRuleRegistered(BuildTargetGroup group)
        {
            Type validatorType = FindType("Unity.XR.CoreUtils.Editor.BuildValidator");
            if (validatorType == null)
                return false;

            var platformRulesField = validatorType.GetField(
                "s_PlatformRules",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var platformRules = platformRulesField?.GetValue(null) as System.Collections.IDictionary;
            if (platformRules == null || !platformRules.Contains(group))
                return false;

            var rules = platformRules[group] as System.Collections.IEnumerable;
            if (rules == null)
                return false;

            foreach (object rule in rules)
            {
                string message = GetReflectedString(rule, "Message");
                if (!string.IsNullOrEmpty(message) && message.Contains(OPENXR_TARGET_API_WARNING_FRAGMENT))
                    return true;
            }

            return false;
        }

        private static bool IsOpenXRTargetApiStartupCleanupRelevant()
        {
            if (FindType("UnityEngine.XR.OpenXR.OpenXRSettings") == null
                || FindType("UnityEditor.XR.OpenXR.OpenXRProjectValidation") == null
                || FindType("Unity.XR.CoreUtils.Editor.BuildValidator") == null)
            {
                return false;
            }

            bool hasTargetMetaFeatures = FindType("Meta.XR.MetaXRFeature") != null
                || FindType("Meta.XR.OculusTouchControllerProximityProfile") != null;
            if (!hasTargetMetaFeatures)
                return false;

            return IsOpenXRLoaderEnabled(BuildTargetGroup.Android, out _)
                || IsOpenXRLoaderEnabled(BuildTargetGroup.Standalone, out _);
        }

        private static bool HasOpenXRValidationRulesInitialized()
        {
            if (SessionState.GetBool(OPENXR_FEATURES_REFRESHED_SESSION_KEY, false))
                return true;

            return HasOpenXRTargetApiValidationRuleRegistered(BuildTargetGroup.Android)
                || HasOpenXRTargetApiValidationRuleRegistered(BuildTargetGroup.Standalone)
                || CountOpenXRTargetApiBuildValidationIssues(BuildTargetGroup.Android) > 0
                || CountOpenXRTargetApiBuildValidationIssues(BuildTargetGroup.Standalone) > 0;
        }

        private static bool HasFreshOpenXRTargetApiValidationIssue(BuildTargetGroup group)
        {
            Type validationType = FindType("UnityEditor.XR.OpenXR.OpenXRProjectValidation");
            Type ruleType = FindType("UnityEngine.XR.OpenXR.Features.OpenXRFeature+ValidationRule");
            if (validationType == null || ruleType == null)
                return false;

            var method = validationType.GetMethod(
                "GetCurrentValidationIssues",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
                return false;

            object issues = Activator.CreateInstance(typeof(List<>).MakeGenericType(ruleType));
            method.Invoke(null, new object[] { issues, group });

            foreach (object issue in (IEnumerable)issues)
            {
                string message = GetReflectedString(issue, "message");
                if (message.Contains(OPENXR_TARGET_API_WARNING_FRAGMENT))
                    return true;
            }

            return false;
        }

        private static bool IsSafePlatformSdkManualTask(string message)
        {
            return message.Contains("Please ignore if you are not using any Platform SDK APIs.");
        }

        private static string GetOptionalLambdaValue(object optionalValue, BuildTargetGroup group)
        {
            if (optionalValue == null)
                return string.Empty;

            var method = optionalValue.GetType().GetMethod(
                "GetValue",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            object value = method?.Invoke(optionalValue, new object[] { group });
            return value == null ? string.Empty : value.ToString();
        }

        private static bool EnsureOpenXRLoader(BuildTargetGroup group, List<string> issues)
        {
            string issue;
            if (IsOpenXRLoaderEnabled(group, out issue))
                return false;

            if (!string.IsNullOrEmpty(issue))
            {
                issues.Add($"{group}: {issue}");
                return false;
            }

            object settings = GetXRGeneralSettingsForBuildTarget(group, true, out issue);
            if (settings == null)
            {
                issues.Add($"{group}: {issue}");
                return false;
            }

            object manager = GetReflectedValue(settings, "AssignedSettings")
                          ?? GetReflectedValue(settings, "Manager");
            if (manager == null)
            {
                issues.Add($"{group}: XR Manager Settings를 찾을 수 없습니다.");
                return false;
            }

            Type metadataStoreType = FindType("UnityEditor.XR.Management.Metadata.XRPackageMetadataStore");
            if (metadataStoreType == null)
            {
                issues.Add($"{group}: XRPackageMetadataStore 타입을 찾을 수 없습니다.");
                return false;
            }

            var assignMethod = metadataStoreType.GetMethod(
                "AssignLoader",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (assignMethod == null)
            {
                issues.Add($"{group}: XR loader 등록 API를 찾을 수 없습니다.");
                return false;
            }

            bool assigned = false;
            try
            {
                object result = assignMethod.Invoke(null, new[] { manager, OPENXR_LOADER_TYPE_NAME, group });
                assigned = result is bool boolValue && boolValue;
            }
            catch (Exception ex)
            {
                issues.Add($"{group}: OpenXR loader 등록 중 예외 발생 - {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }

            if (!assigned && !IsOpenXRLoaderEnabled(group, out _))
            {
                issues.Add($"{group}: OpenXR loader 등록에 실패했습니다.");
                return false;
            }

            AssetDatabase.SaveAssets();
            return true;
        }

        private static bool IsOpenXRLoaderEnabled(BuildTargetGroup group, out string issue)
        {
            issue = null;

            if (string.IsNullOrEmpty(GetResolvedPackageVersion("com.unity.xr.management")))
            {
                issue = "XR Management 패키지가 설치되어 있지 않습니다.";
                return false;
            }

            if (string.IsNullOrEmpty(GetResolvedPackageVersion("com.unity.xr.openxr")))
            {
                issue = "OpenXR Plugin 패키지가 설치되어 있지 않습니다.";
                return false;
            }

            if (FindType(OPENXR_LOADER_TYPE_NAME) == null)
            {
                issue = "OpenXR loader 타입을 찾을 수 없습니다.";
                return false;
            }

            object settings = GetXRGeneralSettingsForBuildTarget(group, false, out issue);
            if (settings == null)
            {
                issue = null;
                return false;
            }

            object manager = GetReflectedValue(settings, "AssignedSettings")
                          ?? GetReflectedValue(settings, "Manager");
            if (manager == null)
                return false;

            object loaders = GetReflectedValue(manager, "activeLoaders")
                          ?? GetReflectedValue(manager, "loaders");
            if (loaders is IEnumerable enumerable)
            {
                foreach (object loader in enumerable)
                {
                    if (loader != null && loader.GetType().FullName == OPENXR_LOADER_TYPE_NAME)
                        return true;
                }
            }

            return false;
        }

        private static object GetXRGeneralSettingsForBuildTarget(
            BuildTargetGroup group,
            bool create,
            out string issue)
        {
            issue = null;

            Type settingsPerBuildTargetType = FindType("UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget");
            if (settingsPerBuildTargetType == null)
            {
                issue = "XRGeneralSettingsPerBuildTarget 타입을 찾을 수 없습니다.";
                return null;
            }

            if (create)
            {
                var getOrCreateMethod = settingsPerBuildTargetType.GetMethod(
                    "GetOrCreate",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (getOrCreateMethod == null)
                {
                    issue = "XR General Settings 생성 API를 찾을 수 없습니다.";
                    return null;
                }

                object perBuildTargetSettings = getOrCreateMethod.Invoke(null, null);
                if (perBuildTargetSettings == null)
                {
                    issue = "XR General Settings asset을 생성할 수 없습니다.";
                    return null;
                }

                var hasManagerMethod = settingsPerBuildTargetType.GetMethod("HasManagerSettingsForBuildTarget");
                var createManagerMethod = settingsPerBuildTargetType.GetMethod("CreateDefaultManagerSettingsForBuildTarget");
                if (hasManagerMethod == null || createManagerMethod == null)
                {
                    issue = "XR Manager Settings 생성 API를 찾을 수 없습니다.";
                    return null;
                }

                bool hasManager = hasManagerMethod.Invoke(perBuildTargetSettings, new object[] { group }) is bool value && value;
                if (!hasManager)
                {
                    createManagerMethod.Invoke(perBuildTargetSettings, new object[] { group });
                    AssetDatabase.SaveAssets();
                }
            }

            var settingsForBuildTargetMethod = settingsPerBuildTargetType.GetMethod(
                "XRGeneralSettingsForBuildTarget",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (settingsForBuildTargetMethod == null)
            {
                issue = "XR General Settings 조회 API를 찾을 수 없습니다.";
                return null;
            }

            return settingsForBuildTargetMethod.Invoke(null, new object[] { group });
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static string GetReflectedString(object instance, string name)
        {
            object value = GetReflectedValue(instance, name);
            return value == null ? string.Empty : value.ToString();
        }

        private static bool GetReflectedBool(object instance, string name)
        {
            object value = GetReflectedValue(instance, name);
            return value is bool boolValue && boolValue;
        }

        private static object GetReflectedValue(object instance, string name)
        {
            if (instance == null)
                return null;

            var flags = System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance;

            var type = instance.GetType();
            var field = type.GetField(name, flags);
            if (field != null)
                return field.GetValue(instance);

            var property = type.GetProperty(name, flags);
            return property != null ? property.GetValue(instance) : null;
        }

        private static bool AssetExists(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null
                || File.Exists(Path.GetFullPath(assetPath));
        }

        private static bool CopyOrReplaceAssetPreservingGuid(string src, string dst)
        {
            string parent = Path.GetDirectoryName(dst)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(parent))
                EnsureAssetFolder(parent);

            string dstFullPath = Path.GetFullPath(dst);

            try
            {
                if (!AssetExists(src))
                    return false;

                if (!File.Exists(dstFullPath))
                    return AssetDatabase.CopyAsset(src, dst);

                string tempFolder = $"{OPTIMIZE_DEST_FOLDER}/__DDOITOptimizeTemp_{Guid.NewGuid():N}";
                string tempPath = $"{tempFolder}/{Path.GetFileName(src)}";

                try
                {
                    EnsureAssetFolder(tempFolder);
                    if (!AssetDatabase.CopyAsset(src, tempPath))
                        return false;

                    FileUtil.ReplaceFile(Path.GetFullPath(tempPath), dstFullPath);
                    AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
                    return true;
                }
                finally
                {
                    if (AssetDatabase.IsValidFolder(tempFolder))
                        AssetDatabase.DeleteAsset(tempFolder);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DDOITSetupWindow] 에셋 갱신 실패: {src} → {dst}\n{ex}");
                return false;
            }
        }

        private static bool SetQualityRenderPipeline(
            int qualityIndex,
            UnityEngine.Rendering.RenderPipelineAsset renderPipelineAsset)
        {
            var asset = GetProjectSettingsMainAsset("ProjectSettings/QualitySettings.asset");
            if (asset == null)
            {
                Debug.LogWarning("[DDOITSetupWindow] QualitySettings.asset을 로드할 수 없습니다.");
                return false;
            }

            var so = new SerializedObject(asset);
            var prop = so.FindProperty($"m_QualitySettings.Array.data[{qualityIndex}].customRenderPipeline");
            if (prop == null)
            {
                Debug.LogWarning($"[DDOITSetupWindow] Quality index {qualityIndex}의 Render Pipeline 필드를 찾을 수 없습니다.");
                return false;
            }

            prop.objectReferenceValue = renderPipelineAsset;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            return true;
        }

        private static void ApplyAudioDspBufferSize(int dspBufferSize)
        {
            var audioConfig = AudioSettings.GetConfiguration();
            audioConfig.dspBufferSize = dspBufferSize;
            AudioSettings.Reset(audioConfig);
            SetProjectSettingsInt("ProjectSettings/AudioManager.asset", "m_DSPBufferSize", dspBufferSize);
        }

        private static bool SetProjectSettingsInt(string assetPath, string propertyName, int value)
        {
            var asset = GetProjectSettingsMainAsset(assetPath);
            if (asset == null)
            {
                Debug.LogWarning($"[DDOITSetupWindow] ProjectSettings 에셋 로드 실패: {assetPath}");
                return false;
            }

            var so = new SerializedObject(asset);
            var prop = so.FindProperty(propertyName);
            if (prop == null || prop.propertyType != SerializedPropertyType.Integer)
            {
                Debug.LogWarning($"[DDOITSetupWindow] ProjectSettings 프로퍼티 없음: {assetPath}/{propertyName}");
                return false;
            }

            prop.intValue = value;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            return true;
        }

        private static UnityEngine.Object GetProjectSettingsMainAsset(string assetPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            return assets != null && assets.Length > 0 ? assets[0] : null;
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

        /// <summary>
        /// GitHub의 DDOIT Tools 태그를 조회하고 최신 안정 릴리스가 있으면 업데이트를 제안한다.
        /// </summary>
        private static void CheckForPackageUpdate()
        {
            if (IsPackageOperationInProgress)
            {
                Debug.LogWarning("[DDOITSetupWindow] 다른 패키지 작업이 진행 중입니다.");
                return;
            }

            PackageManagerPackageInfo packageInfo = GetInstalledDDOITPackageInfo();
            if (packageInfo != null && packageInfo.source != PackageSource.Git)
            {
                Debug.LogWarning("[DDOITSetupWindow] Git URL로 설치된 DDOIT Tools 패키지만 자체 업데이트할 수 있습니다.");
                return;
            }

            IsCheckingForPackageUpdate = true;
            PackageUpdateStatusMessage = "GitHub에서 최신 DDOIT Tools 릴리스를 확인하고 있습니다.";
            PackageUpdateStatusMessageType = MessageType.Info;

            try
            {
                ActivePackageVersionCheckRequest = UnityWebRequest.Get(DDOIT_GITHUB_TAGS_API_URL);
                ActivePackageVersionCheckRequest.timeout = 15;
                ActivePackageVersionCheckRequest.SetRequestHeader("Accept", "application/vnd.github+json");
                ActivePackageVersionCheckRequest.SetRequestHeader("X-GitHub-Api-Version", "2022-11-28");
                ActivePackageVersionCheckRequest.SendWebRequest();

                EditorApplication.update -= ProcessPackageVersionCheckRequest;
                EditorApplication.update += ProcessPackageVersionCheckRequest;
            }
            catch (Exception exception)
            {
                ActivePackageVersionCheckRequest?.Dispose();
                ActivePackageVersionCheckRequest = null;
                IsCheckingForPackageUpdate = false;
                PackageUpdateStatusMessage = $"최신 릴리스 확인 요청을 시작하지 못했습니다: {exception.Message}";
                PackageUpdateStatusMessageType = MessageType.Error;
                Debug.LogError($"[DDOITSetupWindow] 최신 릴리스 확인 요청 실패\n{exception}");
            }

            RepaintOpenSetupWindows();
        }

        private static void ProcessPackageVersionCheckRequest()
        {
            if (ActivePackageVersionCheckRequest == null)
            {
                EditorApplication.update -= ProcessPackageVersionCheckRequest;
                IsCheckingForPackageUpdate = false;
                return;
            }

            if (!ActivePackageVersionCheckRequest.isDone)
                return;

            EditorApplication.update -= ProcessPackageVersionCheckRequest;

            UnityWebRequest request = ActivePackageVersionCheckRequest;
            ActivePackageVersionCheckRequest = null;
            IsCheckingForPackageUpdate = false;

            try
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    string message = request.responseCode == 403
                        ? "GitHub API 요청 한도에 도달했거나 접근이 거부되었습니다. 잠시 후 다시 시도하세요."
                        : $"최신 릴리스 확인에 실패했습니다: {request.error}";

                    PackageUpdateStatusMessage = message;
                    PackageUpdateStatusMessageType = MessageType.Warning;
                    Debug.LogWarning($"[DDOITSetupWindow] {message}");
                    return;
                }

                EvaluateLatestPackageRelease(request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                PackageUpdateStatusMessage = $"최신 릴리스 응답을 처리하지 못했습니다: {exception.Message}";
                PackageUpdateStatusMessageType = MessageType.Error;
                Debug.LogError($"[DDOITSetupWindow] 최신 릴리스 응답 처리 실패\n{exception}");
            }
            finally
            {
                request.Dispose();
                RepaintOpenSetupWindows();
            }
        }

        private static void EvaluateLatestPackageRelease(string responseJson)
        {
            if (!TryGetLatestStableRelease(responseJson, out string latestTag, out Version latestVersion))
            {
                PackageUpdateStatusMessage = "DDOIT Tools의 안정 릴리스 태그를 찾지 못했습니다.";
                PackageUpdateStatusMessageType = MessageType.Warning;
                Debug.LogWarning("[DDOITSetupWindow] GitHub 태그 응답에서 안정 릴리스를 찾지 못했습니다.");
                return;
            }

            PackageManagerPackageInfo packageInfo = GetInstalledDDOITPackageInfo();
            if (packageInfo == null)
            {
                PackageUpdateStatusMessage = $"최신 안정 릴리스는 {latestTag}입니다. 개발 원본 모드에서는 조회만 가능합니다.";
                PackageUpdateStatusMessageType = MessageType.Info;
                Debug.Log($"[DDOITSetupWindow] DDOIT Tools 최신 안정 릴리스: {latestTag}");
                return;
            }

            if (string.IsNullOrWhiteSpace(packageInfo.version) ||
                !Version.TryParse(NormalizeVersion(packageInfo.version), out Version installedVersion))
            {
                PackageUpdateStatusMessage = "현재 설치된 DDOIT Tools 버전을 해석하지 못했습니다.";
                PackageUpdateStatusMessageType = MessageType.Error;
                Debug.LogError("[DDOITSetupWindow] 현재 DDOIT Tools 패키지 버전을 해석하지 못했습니다.");
                return;
            }

            if (latestVersion <= installedVersion)
            {
                PackageUpdateStatusMessage = $"최신 버전입니다. 설치 버전: v{packageInfo.version}";
                PackageUpdateStatusMessageType = MessageType.Info;
                Debug.Log($"[DDOITSetupWindow] DDOIT Tools가 최신 버전입니다: v{packageInfo.version}");
                return;
            }

            PackageUpdateStatusMessage =
                $"새 릴리스가 있습니다. 설치 버전: v{packageInfo.version} / 최신 버전: {latestTag}";
            PackageUpdateStatusMessageType = MessageType.Warning;

            string installedVersionText = packageInfo.version;
            string latestVersionText = latestVersion.ToString();
            EditorApplication.delayCall += () => ConfirmPackageUpdate(
                installedVersionText,
                latestTag,
                latestVersionText);
        }

        private static bool TryGetLatestStableRelease(
            string responseJson,
            out string latestTag,
            out Version latestVersion)
        {
            latestTag = null;
            latestVersion = null;

            if (string.IsNullOrWhiteSpace(responseJson))
                return false;

            var serializer = new DataContractJsonSerializer(typeof(List<GitHubTagInfo>));
            List<GitHubTagInfo> tags;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(responseJson)))
            {
                tags = serializer.ReadObject(stream) as List<GitHubTagInfo>;
            }

            if (tags == null)
                return false;

            foreach (GitHubTagInfo tag in tags)
            {
                if (tag == null || !TryParseStableReleaseTag(tag.Name, out Version candidateVersion))
                    continue;

                if (latestVersion != null && candidateVersion <= latestVersion)
                    continue;

                latestTag = tag.Name;
                latestVersion = candidateVersion;
            }

            return latestVersion != null;
        }

        private static bool TryParseStableReleaseTag(string tagName, out Version version)
        {
            version = null;

            if (string.IsNullOrWhiteSpace(tagName) ||
                !tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string versionText = tagName.Substring(1);
            if (versionText.IndexOf('-') >= 0 || versionText.IndexOf('+') >= 0)
                return false;

            if (!Version.TryParse(versionText, out Version parsedVersion) ||
                parsedVersion.Build < 0 || parsedVersion.Revision >= 0)
            {
                return false;
            }

            version = parsedVersion;
            return true;
        }

        private static void ConfirmPackageUpdate(
            string installedVersion,
            string latestTag,
            string latestVersion)
        {
            if (IsPackageOperationInProgress)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "DDOIT Tools 업데이트",
                $"새 DDOIT Tools 릴리스가 있습니다.\n\n" +
                $"현재 버전: v{installedVersion}\n" +
                $"최신 버전: {latestTag}\n\n" +
                "업데이트 중 스크립트가 다시 컴파일되고 에디터가 잠시 응답하지 않을 수 있습니다.",
                "업데이트",
                "취소");

            if (!confirmed)
            {
                PackageUpdateStatusMessage = $"{latestTag} 업데이트를 취소했습니다.";
                PackageUpdateStatusMessageType = MessageType.Info;
                RepaintOpenSetupWindows();
                return;
            }

            StartPackageUpdate(latestTag, latestVersion);
        }

        private static void StartPackageUpdate(string targetTag, string targetVersion)
        {
            string packageSpec = $"{DDOIT_GIT_URL}#{targetTag}";

            IsUpdatingPackage = true;
            PackageUpdateStatusMessage = $"DDOIT Tools {targetTag} 업데이트를 요청했습니다.";
            PackageUpdateStatusMessageType = MessageType.Info;
            SessionState.SetBool(PACKAGE_UPDATE_PENDING_KEY, true);
            SessionState.SetString(PACKAGE_UPDATE_TARGET_VERSION_KEY, targetVersion);

            try
            {
                Debug.Log($"[DDOITSetupWindow] DDOIT Tools 업데이트 요청: {packageSpec}");
                ActivePackageUpdateRequest = Client.Add(packageSpec);
                EditorApplication.update -= ProcessPackageUpdateRequest;
                EditorApplication.update += ProcessPackageUpdateRequest;
            }
            catch (Exception exception)
            {
                ClearPackageUpdatePendingState();
                ActivePackageUpdateRequest = null;
                IsUpdatingPackage = false;
                PackageUpdateStatusMessage = $"DDOIT Tools 업데이트 요청에 실패했습니다: {exception.Message}";
                PackageUpdateStatusMessageType = MessageType.Error;
                Debug.LogError($"[DDOITSetupWindow] DDOIT Tools 업데이트 요청 실패\n{exception}");
            }

            RepaintOpenSetupWindows();
        }

        private static void ProcessPackageUpdateRequest()
        {
            if (ActivePackageUpdateRequest == null)
            {
                EditorApplication.update -= ProcessPackageUpdateRequest;
                IsUpdatingPackage = false;
                return;
            }

            if (!ActivePackageUpdateRequest.IsCompleted)
                return;

            EditorApplication.update -= ProcessPackageUpdateRequest;

            bool succeeded = ActivePackageUpdateRequest.Status == StatusCode.Success;
            if (succeeded)
            {
                string installedVersion = ActivePackageUpdateRequest.Result != null
                    ? ActivePackageUpdateRequest.Result.version
                    : SessionState.GetString(PACKAGE_UPDATE_TARGET_VERSION_KEY, string.Empty);

                PackageUpdateStatusMessage = $"DDOIT Tools v{installedVersion} 업데이트가 완료되었습니다.";
                PackageUpdateStatusMessageType = MessageType.Info;
                Debug.Log($"[DDOITSetupWindow] {PackageUpdateStatusMessage}");
            }
            else
            {
                string errorMessage = ActivePackageUpdateRequest.Error != null
                    ? ActivePackageUpdateRequest.Error.message
                    : "알 수 없는 Package Manager 오류";

                PackageUpdateStatusMessage = $"DDOIT Tools 업데이트에 실패했습니다: {errorMessage}";
                PackageUpdateStatusMessageType = MessageType.Error;
                Debug.LogError($"[DDOITSetupWindow] {PackageUpdateStatusMessage}");
                ClearPackageUpdatePendingState();
            }

            ActivePackageUpdateRequest = null;
            IsUpdatingPackage = false;
            RepaintOpenSetupWindows();
        }

        private static void RestorePackageUpdateStatusAfterReload()
        {
            if (!SessionState.GetBool(PACKAGE_UPDATE_PENDING_KEY, false))
                return;

            string targetVersion = SessionState.GetString(PACKAGE_UPDATE_TARGET_VERSION_KEY, string.Empty);
            PackageManagerPackageInfo packageInfo = GetInstalledDDOITPackageInfo();
            ClearPackageUpdatePendingState();

            if (packageInfo != null && AreEquivalentVersions(packageInfo.version, targetVersion))
            {
                PackageUpdateStatusMessage = $"DDOIT Tools v{packageInfo.version} 업데이트가 완료되었습니다.";
                PackageUpdateStatusMessageType = MessageType.Info;
                Debug.Log($"[DDOITSetupWindow] {PackageUpdateStatusMessage}");
                return;
            }

            PackageUpdateStatusMessage =
                $"DDOIT Tools v{targetVersion} 업데이트 완료를 확인하지 못했습니다. 최신 릴리스를 다시 확인하세요.";
            PackageUpdateStatusMessageType = MessageType.Warning;
            Debug.LogWarning($"[DDOITSetupWindow] {PackageUpdateStatusMessage}");
        }

        private static void ClearPackageUpdatePendingState()
        {
            SessionState.SetBool(PACKAGE_UPDATE_PENDING_KEY, false);
            SessionState.EraseString(PACKAGE_UPDATE_TARGET_VERSION_KEY);
        }

        private static bool AreEquivalentVersions(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            return Version.TryParse(NormalizeVersion(left), out Version leftVersion) &&
                   Version.TryParse(NormalizeVersion(right), out Version rightVersion) &&
                   leftVersion == rightVersion;
        }

        private static void InstallMissingDependencies(DependencyInfo[] dependencies, string scopeLabel)
        {
            if (IsPackageOperationInProgress)
            {
                Debug.LogWarning("[DDOITSetupWindow] 다른 패키지 작업이 진행 중입니다.");
                return;
            }

            DependencyInstallErrors.Clear();
            ActiveInstallScopeLabel = scopeLabel;

            var targets = dependencies.Where(RequiresInstallOrUpdate).ToList();
            if (targets.Count == 0)
            {
                UpdateDependencyVerificationReport($"{scopeLabel} 검증");
                Debug.Log($"[DDOITSetupWindow] {scopeLabel}: 설치/업데이트 대상이 없습니다.");
                return;
            }

            LastDependencyVerificationReport = null;
            ActiveInstallTargetLabel = string.Join(", ", targets.Select(dependency => dependency.displayName));

            IsInstallingDependencies = true;

            string[] packageSpecs = targets.Select(GetPackageAddSpec).ToArray();
            Debug.Log(
                $"[DDOITSetupWindow] 패키지 일괄 설치 요청: {string.Join(", ", packageSpecs)}");

            SuspendImportWorkersForOpenXRInstall(targets);

            try
            {
                ActiveDependencyInstallRequest = Client.AddAndRemove(packageSpecs, Array.Empty<string>());
                EditorApplication.update += ProcessDependencyInstallRequest;
            }
            catch (Exception exception)
            {
                DependencyInstallErrors.Add($"{ActiveInstallTargetLabel}: {exception.Message}");
                Debug.LogError(
                    $"[DDOITSetupWindow] 패키지 일괄 설치 요청 실패: {ActiveInstallTargetLabel}\n" +
                    exception);
                RestoreImportWorkersAfterOpenXRInstall();
                CompleteDependencyInstallation();
            }

            RepaintOpenSetupWindows();
        }

        private static void ProcessDependencyInstallRequest()
        {
            if (ActiveDependencyInstallRequest == null || !ActiveDependencyInstallRequest.IsCompleted)
                return;

            bool succeeded = ActiveDependencyInstallRequest.Status == StatusCode.Success;
            if (succeeded)
            {
                Debug.Log($"[DDOITSetupWindow] 패키지 일괄 설치 완료: {ActiveInstallTargetLabel}");
            }
            else
            {
                string errorMessage = ActiveDependencyInstallRequest.Error != null
                    ? ActiveDependencyInstallRequest.Error.message
                    : "알 수 없는 Package Manager 오류";
                DependencyInstallErrors.Add($"{ActiveInstallTargetLabel}: {errorMessage}");
                Debug.LogError(
                    $"[DDOITSetupWindow] 패키지 일괄 설치 실패: {ActiveInstallTargetLabel}\n" +
                    errorMessage);
            }

            if (!succeeded)
                RestoreImportWorkersAfterOpenXRInstall();

            CompleteDependencyInstallation();
        }

        private static void SuspendImportWorkersForOpenXRInstall(IReadOnlyCollection<DependencyInfo> targets)
        {
            if (!targets.Any(dependency => dependency.packageId == OPENXR_PACKAGE_ID))
                return;

            int previousWorkerCount = AssetDatabase.DesiredWorkerCount;
            if (previousWorkerCount <= 0)
                return;

            // OpenXR 1.17.1 can create its settings asset from import workers during first load.
            // Keeping that initialization in the main process avoids concurrent GUID reservation.
            SessionState.SetInt(IMPORT_WORKER_PREVIOUS_COUNT_KEY, previousWorkerCount);
            SessionState.SetBool(IMPORT_WORKER_RESTORE_PENDING_KEY, true);
            AssetDatabase.DesiredWorkerCount = 0;
            AssetDatabase.ForceToDesiredWorkerCount();

            Debug.Log(
                $"[DDOITSetupWindow] OpenXR 초기 설치를 위해 Import Worker를 일시 중지했습니다. " +
                $"설치 후 {previousWorkerCount}개로 복구합니다.");
        }

        private static void RestoreImportWorkersAfterOpenXRInstall()
        {
            if (!SessionState.GetBool(IMPORT_WORKER_RESTORE_PENDING_KEY, false))
                return;

            int previousWorkerCount = SessionState.GetInt(
                IMPORT_WORKER_PREVIOUS_COUNT_KEY,
                EditorUserSettings.desiredImportWorkerCount);

            SessionState.SetBool(IMPORT_WORKER_RESTORE_PENDING_KEY, false);
            SessionState.SetInt(IMPORT_WORKER_PREVIOUS_COUNT_KEY, 0);

            if (previousWorkerCount <= 0)
                return;

            AssetDatabase.DesiredWorkerCount = previousWorkerCount;
            AssetDatabase.ForceToDesiredWorkerCount();
            Debug.Log($"[DDOITSetupWindow] Import Worker를 {previousWorkerCount}개로 복구했습니다.");
        }

        private static void CompleteDependencyInstallation()
        {
            IsInstallingDependencies = false;
            EditorApplication.update -= ProcessDependencyInstallRequest;
            ActiveDependencyInstallRequest = null;

            AssetDatabase.Refresh();
            UpdateDependencyVerificationReport($"{ActiveInstallScopeLabel} 설치 후 검증");

            EditorUtility.DisplayDialog(
                "패키지 설치 검증",
                LastDependencyVerificationReport + "\n\n" +
                "Meta XR SDK는 Unity Asset Store에서 먼저 '내 에셋에 추가'해야 설치됩니다.",
                "확인");

            ActiveInstallScopeLabel = null;
            ActiveInstallTargetLabel = null;
            RepaintOpenSetupWindows();
        }

        private static void UpdateDependencyVerificationReport(string title)
        {
            var sb = new StringBuilder();
            int requiredIssues = CountDependencyIssues(REQUIRED_DEPENDENCIES);
            int optionalIssues = CountDependencyIssues(OPTIONAL_DEPENDENCIES);

            sb.AppendLine(title);
            sb.AppendLine();
            AppendDependencyVerificationSection(sb, "필수", REQUIRED_DEPENDENCIES);
            AppendDependencyVerificationSection(sb, "권장", OPTIONAL_DEPENDENCIES);

            if (DependencyInstallErrors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("[설치 요청 오류]");
                foreach (var error in DependencyInstallErrors)
                    sb.AppendLine($"- {error}");
            }

            sb.AppendLine();
            if (requiredIssues == 0)
                sb.AppendLine("결과: 필수 의존성은 모두 설치되어 있습니다.");
            else
                sb.AppendLine($"결과: 필수 의존성 {requiredIssues}개를 확인해야 합니다.");

            if (optionalIssues > 0)
                sb.AppendLine($"참고: 권장 도구 {optionalIssues}개가 아직 미설치/버전 불일치 상태입니다.");

            LastDependencyVerificationReport = sb.ToString().TrimEnd();
            LastDependencyVerificationMessageType = DependencyInstallErrors.Count > 0 || requiredIssues > 0
                ? MessageType.Error
                : optionalIssues > 0
                    ? MessageType.Warning
                    : MessageType.Info;
        }

        private static void AppendDependencyVerificationSection(
            StringBuilder sb,
            string label,
            DependencyInfo[] dependencies)
        {
            sb.AppendLine($"[{label}]");
            foreach (var dep in dependencies)
            {
                var status = GetDependencyStatus(dep);
                string installed = string.IsNullOrEmpty(status.installedVersion)
                    ? "미설치"
                    : status.installedVersion;
                sb.AppendLine(
                    $"- {GetDependencyStatusLabel(status.state)} {dep.displayName}: {installed} / 요구 {GetDependencyRequirementText(dep)}");
            }
        }

        private static int CountDependencyIssues(DependencyInfo[] dependencies)
        {
            return dependencies.Count(RequiresInstallOrUpdate);
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

        private static bool IsPackageOperationInProgress =>
            IsInstallingDependencies || IsCheckingForPackageUpdate || IsUpdatingPackage;

        private static PackageManagerPackageInfo GetInstalledDDOITPackageInfo()
        {
            return PackageManagerPackageInfo.FindForPackageName(DDOIT_PACKAGE_ID);
        }

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

        private static bool ApplyAndroidDefaultQuality(int level)
        {
            var asset = GetProjectSettingsMainAsset("ProjectSettings/QualitySettings.asset");
            if (asset == null)
            {
                Debug.LogWarning("[DDOITSetupWindow] QualitySettings.asset을 로드할 수 없습니다.");
                return false;
            }

            var so = new SerializedObject(asset);
            var defaultQuality = so.FindProperty("m_PerPlatformDefaultQuality");
            if (defaultQuality == null || !defaultQuality.isArray)
            {
                Debug.LogWarning("[DDOITSetupWindow] Android default quality 필드를 찾을 수 없습니다.");
                return false;
            }

            for (int i = 0; i < defaultQuality.arraySize; i++)
            {
                var item = defaultQuality.GetArrayElementAtIndex(i);
                var platform = item.FindPropertyRelative("first");
                var quality = item.FindPropertyRelative("second");
                if (platform != null && quality != null && platform.stringValue == "Android")
                {
                    quality.intValue = level;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(asset);
                    return true;
                }
            }

            int newIndex = defaultQuality.arraySize;
            defaultQuality.InsertArrayElementAtIndex(newIndex);

            var newItem = defaultQuality.GetArrayElementAtIndex(newIndex);
            var newPlatform = newItem.FindPropertyRelative("first");
            var newQuality = newItem.FindPropertyRelative("second");
            if (newPlatform == null || newQuality == null)
            {
                Debug.LogWarning("[DDOITSetupWindow] Android default quality 항목을 생성할 수 없습니다.");
                return false;
            }

            newPlatform.stringValue = "Android";
            newQuality.intValue = level;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            return true;
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
