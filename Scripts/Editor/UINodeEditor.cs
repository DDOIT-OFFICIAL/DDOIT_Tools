using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(UINode))]
    public class UINodeEditor : UnityEditor.Editor
    {
        // Base
        private SerializedProperty _isStepCondition;
        private SerializedProperty _onRelease;

        // UI Data
        private SerializedProperty _uiData;
        private SerializedProperty _uiDataType;
        private SerializedProperty _uiDataTitle;
        private SerializedProperty _uiDataContext;
        private SerializedProperty _uiDataContextSub;
        private SerializedProperty _uiDataImage;
        private SerializedProperty _uiDataImageSub;
        private SerializedProperty _uiDataVideo;
        private SerializedProperty _uiDataButtonLabelA;
        private SerializedProperty _uiDataButtonLabelB;

        // Theme
        private SerializedProperty _theme;
        private UITheme[] _themes;
        private string[] _themeNames;

        // Title Bold / Icon
        private SerializedProperty _titleBold;
        private SerializedProperty _titleIcon;
        private UIGlobalSettings _globalSettings;

        // Placement
        private SerializedProperty _isFixed;
        private SerializedProperty _lookAtMode;

        // Button Events
        private SerializedProperty _onButtonA;
        private SerializedProperty _onButtonB;

        private void OnEnable()
        {
            _isStepCondition = serializedObject.FindProperty("_isStepCondition");
            _onRelease = serializedObject.FindProperty("_onRelease");

            _uiData = serializedObject.FindProperty("_uiData");
            _uiDataType = _uiData.FindPropertyRelative("type");
            _uiDataTitle = _uiData.FindPropertyRelative("title");
            _uiDataContext = _uiData.FindPropertyRelative("context");
            _uiDataContextSub = _uiData.FindPropertyRelative("contextSub");
            _uiDataImage = _uiData.FindPropertyRelative("image");
            _uiDataImageSub = _uiData.FindPropertyRelative("imageSub");
            _uiDataVideo = _uiData.FindPropertyRelative("video");
            _uiDataButtonLabelA = _uiData.FindPropertyRelative("buttonLabelA");
            _uiDataButtonLabelB = _uiData.FindPropertyRelative("buttonLabelB");

            _theme = serializedObject.FindProperty("_theme");
            _titleBold = serializedObject.FindProperty("_titleBold");
            _titleIcon = serializedObject.FindProperty("_titleIcon");

            _isFixed = serializedObject.FindProperty("_isFixed");
            _lookAtMode = serializedObject.FindProperty("_lookAtMode");

            _onButtonA = serializedObject.FindProperty("_onButtonA");
            _onButtonB = serializedObject.FindProperty("_onButtonB");

            RefreshThemeList();
            RefreshGlobalSettings();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Base
            EditorGUILayout.PropertyField(_isStepCondition, new GUIContent("Step 조건"));
            EditorGUILayout.PropertyField(_onRelease);
            EditorGUILayout.Space(4);

            // Theme (dropdown)
            DrawThemeDropdown();
            EditorGUILayout.Space(4);

            // UI Data
            DrawUIDataSection();
            EditorGUILayout.Space(4);

            // Button Events
            var currentType = (UIType)_uiDataType.enumValueIndex;
            if (currentType == UIType.T1C1B1 || currentType == UIType.T1C1B2)
            {
                DrawButtonEventSection();
                EditorGUILayout.Space(4);
            }

            // Placement
            DrawPlacementSection();

            // Warnings
            DrawWarnings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawThemeDropdown()
        {
            EditorGUILayout.LabelField("테마", EditorStyles.boldLabel);

            if (_themes == null || _themes.Length == 0)
            {
                EditorGUILayout.HelpBox("UITheme 에셋이 없습니다.", MessageType.Warning);
                return;
            }

            // 현재 선택된 인덱스 찾기
            var currentTheme = _theme.objectReferenceValue as UITheme;
            int selectedIndex = System.Array.IndexOf(_themes, currentTheme);
            if (selectedIndex < 0) selectedIndex = 0;

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("테마", selectedIndex, _themeNames);
            if (EditorGUI.EndChangeCheck())
                _theme.objectReferenceValue = _themes[selectedIndex];
        }

        private void DrawUIDataSection()
        {
            EditorGUILayout.LabelField("UI 데이터", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_uiDataType, new GUIContent("타입"));
            var type = (UIType)_uiDataType.enumValueIndex;

            EditorGUI.indentLevel++;

            // Title: C1 제외 전부
            if (type != UIType.C1)
            {
                // Title Icon (드롭다운) — 제목 위에 배치
                DrawTitleIconDropdown();

                // Bold 토글 + 제목 입력
                EditorGUILayout.BeginHorizontal();
                _titleBold.boolValue = GUILayout.Toggle(
                    _titleBold.boolValue,
                    "B",
                    EditorStyles.miniButton,
                    GUILayout.Width(24));
                EditorGUILayout.PropertyField(_uiDataTitle, new GUIContent("제목"));
                EditorGUILayout.EndHorizontal();
            }

            // Context: T1 제외 전부
            if (type != UIType.T1)
            {
                EditorGUILayout.PropertyField(_uiDataContext, new GUIContent("본문"));
            }

            // ContextSub: T1C2만
            if (type == UIType.T1C2)
            {
                EditorGUILayout.PropertyField(_uiDataContextSub, new GUIContent("본문 2"));
            }

            // Image: P1, P2
            if (type == UIType.T1C1P1 || type == UIType.T1C1P2)
            {
                EditorGUILayout.PropertyField(_uiDataImage, new GUIContent("이미지"));
            }

            // ImageSub: P2만
            if (type == UIType.T1C1P2)
            {
                EditorGUILayout.PropertyField(_uiDataImageSub, new GUIContent("이미지 2"));
            }

            // Video: V1만
            if (type == UIType.T1C1V1)
            {
                EditorGUILayout.PropertyField(_uiDataVideo, new GUIContent("비디오"));
            }

            // Buttons: B1, B2
            if (type == UIType.T1C1B1 || type == UIType.T1C1B2)
            {
                EditorGUILayout.PropertyField(_uiDataButtonLabelA, new GUIContent("버튼 A"));
            }
            if (type == UIType.T1C1B2)
            {
                EditorGUILayout.PropertyField(_uiDataButtonLabelB, new GUIContent("버튼 B"));
            }

            EditorGUI.indentLevel--;

            // 빈 데이터 힌트
            if (type != UIType.T1 && type != UIType.C1)
            {
                EditorGUILayout.HelpBox(
                    "비어있는 텍스트/이미지 필드는 자동으로 숨겨집니다.",
                    MessageType.None);
            }
        }

        private void DrawButtonEventSection()
        {
            var type = (UIType)_uiDataType.enumValueIndex;

            EditorGUILayout.LabelField("버튼 이벤트", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_onButtonA, new GUIContent("버튼 A 클릭"));

            if (type == UIType.T1C1B2)
                EditorGUILayout.PropertyField(_onButtonB, new GUIContent("버튼 B 클릭"));
        }

        private void DrawPlacementSection()
        {
            EditorGUILayout.LabelField("배치", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(_isFixed, new GUIContent("고정형"));

            if (_isFixed.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_lookAtMode, new GUIContent("바라보기 모드"));
                EditorGUI.indentLevel--;

                var mode = (UILookAtMode)_lookAtMode.enumValueIndex;
                switch (mode)
                {
                    case UILookAtMode.None:
                        EditorGUILayout.HelpBox(
                            "이 노드의 Transform 위치/회전에 UI가 고정됩니다.",
                            MessageType.None);
                        break;
                    case UILookAtMode.LookOnce:
                        EditorGUILayout.HelpBox(
                            "소환 시 플레이어를 1회 바라본 후 고정됩니다.",
                            MessageType.None);
                        break;
                    case UILookAtMode.LookAlways:
                        EditorGUILayout.HelpBox(
                            "플레이어를 항상 추적하여 바라봅니다.",
                            MessageType.None);
                        break;
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "플레이어 전방에서 SmoothFollow로 떠다닙니다.",
                    MessageType.None);
            }
        }

        private void DrawTitleIconDropdown()
        {
            if (_globalSettings == null || _globalSettings.titleIcons == null ||
                _globalSettings.titleIcons.Length == 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("제목 아이콘");
                EditorGUILayout.LabelField("(전역 설정에 아이콘 없음)", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                _titleIcon.objectReferenceValue = null;
                return;
            }

            var icons = _globalSettings.titleIcons.Where(s => s != null).ToArray();
            if (icons.Length == 0)
            {
                _titleIcon.objectReferenceValue = null;
                return;
            }

            // "없음" + 아이콘 이름 목록
            var names = new string[icons.Length + 1];
            names[0] = "(없음)";
            for (int i = 0; i < icons.Length; i++)
                names[i + 1] = icons[i].name;

            // 현재 선택 인덱스
            var current = _titleIcon.objectReferenceValue as Sprite;
            int selectedIndex = 0;
            if (current != null)
            {
                int found = System.Array.IndexOf(icons, current);
                if (found >= 0) selectedIndex = found + 1;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("제목 아이콘", selectedIndex, names);
            if (EditorGUI.EndChangeCheck())
                _titleIcon.objectReferenceValue = selectedIndex == 0 ? null : icons[selectedIndex - 1];

            // 미리보기 (어두운 배경 + 아이콘)
            var iconSprite = _titleIcon.objectReferenceValue as Sprite;
            if (iconSprite != null)
            {
                var previewRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));
                GUI.DrawTexture(previewRect, iconSprite.texture, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawWarnings()
        {
            if (!_isStepCondition.boolValue) return;

            var type = (UIType)_uiDataType.enumValueIndex;

            if (type == UIType.T1C1B1 || type == UIType.T1C1B2)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "버튼 클릭 시 Step 조건이 충족됩니다.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "버튼이 없는 UI를 Step 조건으로 사용하면 조건이 자동 충족되지 않습니다.\n" +
                    "다른 노드나 외부 로직에서 Step을 종료해야 합니다.",
                    MessageType.Warning);
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
        }

        private void RefreshGlobalSettings()
        {
            var guids = AssetDatabase.FindAssets("t:UIGlobalSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _globalSettings = AssetDatabase.LoadAssetAtPath<UIGlobalSettings>(path);
            }
        }
    }
}
