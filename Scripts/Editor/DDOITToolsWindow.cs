using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DDOIT.Tools.Editor
{
    /// <summary>
    /// DDOIT Tools 통합 에디터 윈도우.
    /// 현재 탭: UI Theme. 향후 확장 가능.
    /// </summary>
    public class DDOITToolsWindow : EditorWindow
    {
        #region Constants

        private const string PANEL_PREFAB_PATH = "Assets/DDOIT_Tools/Prefabs/UIPanel.prefab";

        private static readonly string[] TAB_NAMES = { "Scene Setup", "UI Theme", "Settings" };

        #endregion

        #region Private Fields

        private int _selectedTab;

        // Global Settings
        private UIGlobalSettings _globalSettings;
        private UnityEditor.Editor _globalSettingsEditor;

        // DDOIT Settings
        private DDOITSettings _ddoitSettings;

        // Theme
        private UITheme[] _themes;
        private string[] _themeNames;
        private int _selectedThemeIndex;
        private UnityEditor.Editor _themeEditor;

        // Scroll
        private Vector2 _scrollPosition;

        #endregion

        #region Menu

        [MenuItem("DDOIT Tools/Tools Window", priority = -100)]
        public static void ShowWindow()
        {
            var window = GetWindow<DDOITToolsWindow>("DDOIT Tools");
            window.minSize = new Vector2(380, 500);
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            RefreshGlobalSettings();
            RefreshThemeList();
            RefreshDDOITSettings();
        }

        private void OnDisable()
        {
            DestroyEditorIfExists(ref _globalSettingsEditor);
            DestroyEditorIfExists(ref _themeEditor);
        }

        private void OnFocus()
        {
            RefreshThemeList();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, TAB_NAMES);
            EditorGUILayout.Space(4);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0:
                    DrawSceneSetupTab();
                    break;
                case 1:
                    DrawUIThemeTab();
                    break;
                case 2:
                    DrawSettingsTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region UI Theme Tab

        private void DrawUIThemeTab()
        {
            DrawGlobalSettingsSection();
            DrawSeparator();
            DrawThemeListSection();
            DrawSeparator();
            DrawThemeLocalSection();
        }

        // ── Global Settings ──

        private void DrawGlobalSettingsSection()
        {
            EditorGUILayout.LabelField("전역 설정 (Global)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (_globalSettings == null)
            {
                EditorGUILayout.HelpBox(
                    "UIGlobalSettings 에셋을 찾을 수 없습니다.\n" +
                    "아래 버튼으로 생성하거나, Create > DDOIT > UI Global Settings 메뉴를 사용하세요.",
                    MessageType.Warning);

                if (GUILayout.Button("UIGlobalSettings 생성"))
                    CreateGlobalSettings();

                return;
            }

            // SO 필드 직접 그리기 (인라인)
            var so = new SerializedObject(_globalSettings);
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("panelWidth"), new GUIContent("패널 너비 (px)"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("ContentRoot Padding", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("paddingLeft"), new GUIContent("L"));
            EditorGUILayout.PropertyField(so.FindProperty("paddingRight"), new GUIContent("R"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("paddingTop"), new GUIContent("T"));
            EditorGUILayout.PropertyField(so.FindProperty("paddingBottom"), new GUIContent("B"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("spacing"), new GUIContent("Spacing"));

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(so.FindProperty("titleFont"), new GUIContent("Title 폰트"));
            EditorGUILayout.PropertyField(so.FindProperty("titleFontSize"), new GUIContent("Title 크기"));

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(so.FindProperty("contextFont"), new GUIContent("Context 폰트"));
            EditorGUILayout.PropertyField(so.FindProperty("contextFontSize"), new GUIContent("Context 크기"));

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(so.FindProperty("titleIcons"), new GUIContent("Title 아이콘 목록"), true);

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            if (GUILayout.Button("전역 설정 → UIPanel 프리팹 적용", GUILayout.Height(28)))
                ApplyGlobalSettingsToPrefab();
        }

        // ── Theme List ──

        private void DrawThemeListSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("테마 목록", EditorStyles.boldLabel);

            if (GUILayout.Button("+", GUILayout.Width(24)))
                CreateNewTheme();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);

            if (_themes == null || _themes.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "UITheme 에셋이 없습니다. + 버튼으로 생성하세요.",
                    MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            _selectedThemeIndex = EditorGUILayout.Popup("선택", _selectedThemeIndex, _themeNames);
            if (EditorGUI.EndChangeCheck())
                RebuildThemeEditor();

            // 선택된 테마 에셋 핑
            if (GUILayout.Button("에셋 선택", GUILayout.Width(80)))
                EditorGUIUtility.PingObject(_themes[_selectedThemeIndex]);
        }

        // ── Theme Local Settings ──

        private void DrawThemeLocalSection()
        {
            if (_themes == null || _themes.Length == 0 || _selectedThemeIndex >= _themes.Length)
                return;

            var theme = _themes[_selectedThemeIndex];
            if (theme == null)
            {
                RefreshThemeList();
                return;
            }

            EditorGUILayout.LabelField($"지역 설정 ({theme.name})", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var so = new SerializedObject(theme);
            so.Update();

            EditorGUILayout.PropertyField(so.FindProperty("backgroundColorTop"), new GUIContent("Background Top"));
            EditorGUILayout.PropertyField(so.FindProperty("backgroundColorBottom"), new GUIContent("Background Bottom"));
            EditorGUILayout.PropertyField(so.FindProperty("edgeColor"), new GUIContent("Edge"));
            EditorGUILayout.PropertyField(so.FindProperty("textColor"), new GUIContent("Text"));

            so.ApplyModifiedProperties();
        }

        #endregion

        #region Scene Setup Tab

        private static readonly string[] INIT_SCENE_OBJECTS =
        {
            "Stage", "InitTr", "GameManager", "ScenarioManager"
        };

        private void DrawSceneSetupTab()
        {
            EditorGUILayout.LabelField("씬 초기 구성", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Init Scene: 콘텐츠 씬에 필요한 기본 오브젝트를 생성합니다.\n" +
                "• Stage — 맵 오브젝트 부모\n" +
                "• InitTr — 플레이어 초기 위치\n" +
                "• GameManager — 씬 매니저 (SpawnPoint 자동 연결)\n" +
                "• ScenarioManager — 시나리오 관리자",
                MessageType.Info);

            EditorGUILayout.Space(4);

            // 이미 존재하는 오브젝트 체크
            var existingNames = GetExistingInitObjects();
            if (existingNames.Length > 0)
            {
                EditorGUILayout.HelpBox(
                    $"이미 존재하는 오브젝트: {string.Join(", ", existingNames)}\n" +
                    "중복 오브젝트는 건너뜁니다.",
                    MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(existingNames.Length == INIT_SCENE_OBJECTS.Length);
            if (GUILayout.Button("Init Scene", GUILayout.Height(32)))
                ExecuteInitScene();
            EditorGUI.EndDisabledGroup();
        }

        private static string[] GetExistingInitObjects()
        {
            return INIT_SCENE_OBJECTS
                .Where(name => GameObject.Find(name) != null)
                .ToArray();
        }

        private static void ExecuteInitScene()
        {
            // 1. Stage
            var stage = FindOrCreate("Stage");

            // 2. InitTr
            var initTr = FindOrCreate("InitTr");

            // 3. GameManager
            var gameManagerObj = FindOrCreate("GameManager", typeof(GameManager));

            // 4. ScenarioManager
            var scenarioManagerObj = FindOrCreate("ScenarioManager", typeof(ScenarioManager));

            // 5. Scenario_01 (ScenarioManager 하위)
            var scenario = FindOrCreateChild(scenarioManagerObj, "Scenario_01", typeof(Scenario));

            // GameManager._spawnPoint ← InitTr 자동 연결
            var gm = gameManagerObj.GetComponent<GameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                var spawnProp = so.FindProperty("_spawnPoint");
                if (spawnProp != null && spawnProp.objectReferenceValue == null)
                {
                    spawnProp.objectReferenceValue = initTr.transform;
                    so.ApplyModifiedProperties();
                }
            }

            // ScenarioManager._entryScenario ← Scenario_01 자동 연결
            var sm = scenarioManagerObj.GetComponent<ScenarioManager>();
            if (sm != null)
            {
                var so = new SerializedObject(sm);
                var entryProp = so.FindProperty("_entryScenario");
                if (entryProp != null && entryProp.objectReferenceValue == null)
                {
                    entryProp.objectReferenceValue = scenario.GetComponent<Scenario>();
                    so.ApplyModifiedProperties();
                }
            }

            Debug.Log("[DDOITToolsWindow] Init Scene 완료: Stage, InitTr, GameManager, ScenarioManager > Scenario_01");
        }

        private static GameObject FindOrCreate(string objectName, params System.Type[] components)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null) return existing;

            var go = components.Length > 0
                ? new GameObject(objectName, components)
                : new GameObject(objectName);

            Undo.RegisterCreatedObjectUndo(go, $"Create {objectName}");
            return go;
        }

        private static GameObject FindOrCreateChild(GameObject parent, string childName, params System.Type[] components)
        {
            var existing = parent.transform.Find(childName);
            if (existing != null) return existing.gameObject;

            var go = components.Length > 0
                ? new GameObject(childName, components)
                : new GameObject(childName);

            Undo.RegisterCreatedObjectUndo(go, $"Create {childName}");
            go.transform.SetParent(parent.transform);
            return go;
        }

        #endregion

        #region Settings Tab

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("전역 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (_ddoitSettings == null)
            {
                EditorGUILayout.HelpBox(
                    "DDOITSettings 에셋을 찾을 수 없습니다.\n" +
                    "아래 버튼으로 생성하세요.",
                    MessageType.Warning);

                if (GUILayout.Button("DDOITSettings 생성", GUILayout.Height(28)))
                    CreateDDOITSettings();

                return;
            }

            var so = new SerializedObject(_ddoitSettings);
            so.Update();

            // ── 시나리오 ──
            EditorGUILayout.LabelField("시나리오", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("defaultStepWait"),
                new GUIContent("Step 기본 대기 시간 (초)", "조건 그룹이 없는 Step의 자동 진행 대기 시간"));
            EditorGUILayout.PropertyField(so.FindProperty("teleportFadeDuration"),
                new GUIContent("텔레포트 페이드 시간 (초)", "TeleportNode의 페이드 전환 총 시간"));

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            // 에셋 위치
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("에셋 선택", GUILayout.Width(80)))
                EditorGUIUtility.PingObject(_ddoitSettings);
            EditorGUILayout.EndHorizontal();
        }

        private void CreateDDOITSettings()
        {
            // Assets/DDOIT_Tools/Data에 생성
            string folder = "Assets/DDOIT_Tools/Data";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/DDOIT_Tools"))
                    AssetDatabase.CreateFolder("Assets", "DDOIT_Tools");
                AssetDatabase.CreateFolder("Assets/DDOIT_Tools", "Data");
            }

            string path = $"{folder}/DDOITSettings.asset";
            if (AssetDatabase.LoadAssetAtPath<DDOITSettings>(path) != null)
            {
                _ddoitSettings = AssetDatabase.LoadAssetAtPath<DDOITSettings>(path);
                return;
            }

            var asset = CreateInstance<DDOITSettings>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _ddoitSettings = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[DDOITToolsWindow] DDOITSettings 생성: {path}");
        }

        private void RefreshDDOITSettings()
        {
            var guids = AssetDatabase.FindAssets("t:DDOITSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _ddoitSettings = AssetDatabase.LoadAssetAtPath<DDOITSettings>(path);
            }
        }

        #endregion

        #region Actions

        private void ApplyGlobalSettingsToPrefab()
        {
            if (_globalSettings == null)
            {
                Debug.LogError("[DDOITToolsWindow] UIGlobalSettings가 없습니다.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PANEL_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[DDOITToolsWindow] UIPanel 프리팹을 찾을 수 없습니다: {PANEL_PREFAB_PATH}");
                return;
            }

            // 프리팹 인스턴스 통해 수정
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            try
            {
                // 1. 패널 너비
                var canvasRT = instance.GetComponent<RectTransform>();
                if (canvasRT != null)
                {
                    var size = canvasRT.sizeDelta;
                    size.x = _globalSettings.panelWidth;
                    canvasRT.sizeDelta = size;
                }

                // 2. ContentRoot Padding & Spacing
                var contentRoot = instance.transform.Find("PanelRoot/ContentRoot");
                if (contentRoot != null)
                {
                    var vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
                    if (vlg != null)
                    {
                        vlg.padding = new RectOffset(
                            _globalSettings.paddingLeft,
                            _globalSettings.paddingRight,
                            _globalSettings.paddingTop,
                            _globalSettings.paddingBottom);
                        vlg.spacing = _globalSettings.spacing;
                    }
                }

                // 3. Title 폰트
                var titleText = FindComponentInChildren<TMP_Text>(instance, "TitleText");
                if (titleText != null)
                {
                    if (_globalSettings.titleFont != null)
                        titleText.font = _globalSettings.titleFont;
                    titleText.fontSize = _globalSettings.titleFontSize;
                }

                // 4. Context 폰트
                var contextNames = new[] { "ContextText", "ContextSubText" };
                foreach (var name in contextNames)
                {
                    var tmp = FindComponentInChildren<TMP_Text>(instance, name);
                    if (tmp != null)
                    {
                        if (_globalSettings.contextFont != null)
                            tmp.font = _globalSettings.contextFont;
                        tmp.fontSize = _globalSettings.contextFontSize;
                    }
                }

                // 프리팹에 적용
                PrefabUtility.ApplyPrefabInstance(instance, InteractionMode.AutomatedAction);
                Debug.Log("[DDOITToolsWindow] 전역 설정이 UIPanel 프리팹에 적용되었습니다.");
            }
            finally
            {
                DestroyImmediate(instance);
            }
        }

        private void CreateGlobalSettings()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "UIGlobalSettings 저장 위치",
                "UIGlobalSettings",
                "asset",
                "UIGlobalSettings 에셋 저장 위치를 선택하세요.",
                "Assets/DDOIT_Tools");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<UIGlobalSettings>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            _globalSettings = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void CreateNewTheme()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "UI Theme 저장 위치",
                "UITheme_New",
                "asset",
                "새 UITheme 에셋 저장 위치를 선택하세요.",
                "Assets/DDOIT_Tools");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<UITheme>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            RefreshThemeList();
            _selectedThemeIndex = System.Array.IndexOf(_themes, asset);
            RebuildThemeEditor();
            EditorGUIUtility.PingObject(asset);
        }

        #endregion

        #region Refresh

        private void RefreshGlobalSettings()
        {
            var guids = AssetDatabase.FindAssets("t:UIGlobalSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _globalSettings = AssetDatabase.LoadAssetAtPath<UIGlobalSettings>(path);
            }
        }

        private void RefreshThemeList()
        {
            var guids = AssetDatabase.FindAssets("t:UITheme");
            _themes = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<UITheme>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(t => t != null)
                .OrderBy(t => t.name)
                .ToArray();

            _themeNames = _themes.Select(t => t.name).ToArray();

            if (_selectedThemeIndex >= _themes.Length)
                _selectedThemeIndex = _themes.Length > 0 ? 0 : -1;

            RebuildThemeEditor();
        }

        private void RebuildThemeEditor()
        {
            DestroyEditorIfExists(ref _themeEditor);

            if (_themes != null && _selectedThemeIndex >= 0 && _selectedThemeIndex < _themes.Length)
                _themeEditor = UnityEditor.Editor.CreateEditor(_themes[_selectedThemeIndex]);
        }

        #endregion

        #region Utility

        private static T FindComponentInChildren<T>(GameObject root, string childName) where T : Component
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var target = transforms.FirstOrDefault(t => t.name == childName);
            return target != null ? target.GetComponent<T>() : null;
        }

        private static void DestroyEditorIfExists(ref UnityEditor.Editor editor)
        {
            if (editor != null)
            {
                DestroyImmediate(editor);
                editor = null;
            }
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
