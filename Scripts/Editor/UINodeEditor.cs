using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Managers;
using DDOIT.Tools.Scenario;
using DDOIT.Tools.Scenario.Nodes;
using DDOIT.Tools.UI;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(UINode))]
    [CanEditMultipleObjects]
    public class UINodeEditor : UnityEditor.Editor
    {
        private const float PANEL_LAYOUT_REFERENCE_HEIGHT = 1080f;
        private const float CONTENT_VERTICAL_PADDING = 80f;
        private const float CONTENT_SPACING = 20f;
        private const float TITLE_ROW_HEIGHT = 60f;
        private const float TITLE_CONTEXT_SPLITTER_HEIGHT = 2f;
        private const float IMAGE_HEIGHT = 200f;
        private const float VIDEO_HEIGHT = 300f;
        private const float BUTTON_ROW_HEIGHT = 80f;
        private const float TEXT_MIN_HEIGHT = 80f;
        private const float TEXT_LINE_HEIGHT = 38f;
        private const int TEXT_CHARS_PER_LINE = 24;

        // Base
        private SerializedProperty _conditionGroup;

        // UI Data
        private SerializedProperty _uiData;
        private SerializedProperty _useTitle;
        private SerializedProperty _useContext;
        private SerializedProperty _useImageA;
        private SerializedProperty _useImageSub;
        private SerializedProperty _useVideo;
        private SerializedProperty _useButtonA;
        private SerializedProperty _useButtonB;
        private SerializedProperty _useContextSub;

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
        private SerializedProperty _buttonAConditionGroup;
        private SerializedProperty _buttonBConditionGroup;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _conditionGroup = serializedObject.FindProperty("_conditionGroup");

            _uiData = serializedObject.FindProperty("_uiData");
            _useTitle = _uiData.FindPropertyRelative("useTitle");
            _useContext = _uiData.FindPropertyRelative("useContext");
            _useImageA = _uiData.FindPropertyRelative("useImageA");
            _useImageSub = _uiData.FindPropertyRelative("useImageSub");
            _useVideo = _uiData.FindPropertyRelative("useVideo");
            _useButtonA = _uiData.FindPropertyRelative("useButtonA");
            _useButtonB = _uiData.FindPropertyRelative("useButtonB");
            _useContextSub = _uiData.FindPropertyRelative("useContextSub");

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
            _buttonAConditionGroup = serializedObject.FindProperty("_buttonAConditionGroup");
            _buttonBConditionGroup = serializedObject.FindProperty("_buttonBConditionGroup");
            _onEnd = serializedObject.FindProperty("_onEnd");

            RefreshThemeList();
            RefreshGlobalSettings();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            bool executionDisabled = ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            ClearLegacyConditionGroup();

            // Theme
            DrawThemeDropdown();
            EditorGUILayout.Space(4);

            // Element Toggles
            DrawElementToggles();
            EditorGUILayout.Space(4);

            // UI Data Fields
            DrawUIDataSection();
            EditorGUILayout.Space(4);

            // Button Events
            if (_useButtonA.boolValue || _useButtonB.boolValue)
            {
                DrawButtonEventSection();
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_onEnd, new GUIContent("버튼 클릭 이벤트 (공통)"));
                EditorGUILayout.Space(4);
            }

            // Placement
            DrawPlacementSection();

            // Warnings
            if (!executionDisabled)
                DrawWarnings();

            serializedObject.ApplyModifiedProperties();
        }

        private void ClearLegacyConditionGroup()
        {
            if (_conditionGroup == null || _conditionGroup.intValue <= 0)
                return;

            _conditionGroup.intValue = 0;
            EditorGUILayout.HelpBox(
                "UINode 자체 조건 그룹은 사용하지 않습니다. 버튼 조건 드롭다운으로 조건 그룹을 지정하세요.",
                MessageType.Info);
            EditorGUILayout.Space(4);
        }

        private void DrawThemeDropdown()
        {
            DrawThemeDropdownWithDefault();
        }

        private void DrawElementToggles()
        {
            EditorGUILayout.LabelField("UI 요소", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            DrawToggle(_useTitle, "T", "Title");
            DrawToggle(_useContext, "C", "Context");
            DrawToggle(_useImageA, "P", "Image");
            DrawToggle(_useImageSub, "P", "Image 2");
            DrawToggle(_useVideo, "V", "Video");
            DrawToggle(_useButtonA, "B", "Button A");
            DrawToggle(_useButtonB, "B", "Button B");
            DrawToggle(_useContextSub, "C", "Context Sub");

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawToggle(SerializedProperty prop, string label, string tooltip)
        {
            var style = prop.boolValue ? new GUIStyle(EditorStyles.miniButton)
            {
                normal = { textColor = Color.white },
                fontStyle = FontStyle.Bold
            } : EditorStyles.miniButton;

            var originalBg = GUI.backgroundColor;
            if (prop.boolValue)
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);

            if (GUILayout.Button(new GUIContent(label, tooltip), style, GUILayout.Width(28)))
                prop.boolValue = !prop.boolValue;

            GUI.backgroundColor = originalBg;
        }

        private void DrawUIDataSection()
        {
            EditorGUILayout.LabelField("UI 데이터", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // Title
            if (_useTitle.boolValue)
            {
                // Title Icon (드롭다운)
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

            // Context
            if (_useContext.boolValue)
                EditorGUILayout.PropertyField(_uiDataContext, new GUIContent("본문"));

            // ImageA
            if (_useImageA.boolValue)
                EditorGUILayout.PropertyField(_uiDataImage, new GUIContent("이미지"));

            // ImageSub
            if (_useImageSub.boolValue)
                EditorGUILayout.PropertyField(_uiDataImageSub, new GUIContent("이미지 2"));

            // Video
            if (_useVideo.boolValue)
                EditorGUILayout.PropertyField(_uiDataVideo, new GUIContent("비디오"));

            // ButtonA
            if (_useButtonA.boolValue)
                EditorGUILayout.PropertyField(_uiDataButtonLabelA, new GUIContent("버튼 A"));

            // ButtonB
            if (_useButtonB.boolValue)
                EditorGUILayout.PropertyField(_uiDataButtonLabelB, new GUIContent("버튼 B"));

            // ContextSub
            if (_useContextSub.boolValue)
                EditorGUILayout.PropertyField(_uiDataContextSub, new GUIContent("하단 본문"));

            EditorGUI.indentLevel--;

            // 빈 데이터 힌트
            bool hasMultiple = (_useTitle.boolValue ? 1 : 0) + (_useContext.boolValue ? 1 : 0) +
                               (_useImageA.boolValue ? 1 : 0) + (_useContextSub.boolValue ? 1 : 0) > 1;
            if (hasMultiple)
            {
                EditorGUILayout.HelpBox(
                    "비어있는 텍스트/이미지 필드는 자동으로 숨겨집니다.",
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

            var names = new string[icons.Length + 1];
            names[0] = "(없음)";
            for (int i = 0; i < icons.Length; i++)
                names[i + 1] = icons[i].name;

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

            var iconSprite = _titleIcon.objectReferenceValue as Sprite;
            if (iconSprite != null)
            {
                var previewRect = GUILayoutUtility.GetRect(32, 32, GUILayout.Width(32));
                EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));
                GUI.DrawTexture(previewRect, iconSprite.texture, ScaleMode.ScaleToFit);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawButtonEventSection()
        {
            EditorGUILayout.LabelField("버튼 이벤트", EditorStyles.boldLabel);
            var parentStep = ((UINode)target).GetComponentInParent<Step>(true);

            ResetDisabledButtonConditionGroups();

            if (_useButtonA.boolValue)
            {
                DrawButtonConditionDropdown("버튼 A 조건", _buttonAConditionGroup, parentStep);
                EditorGUILayout.PropertyField(_onButtonA, new GUIContent("버튼 A 클릭"));
            }

            if (_useButtonB.boolValue)
            {
                DrawButtonConditionDropdown("버튼 B 조건", _buttonBConditionGroup, parentStep);
                EditorGUILayout.PropertyField(_onButtonB, new GUIContent("버튼 B 클릭"));
            }
        }

        private void ResetDisabledButtonConditionGroups()
        {
            if (!_useButtonA.boolValue && _buttonAConditionGroup.intValue != 0)
                _buttonAConditionGroup.intValue = 0;

            if (!_useButtonB.boolValue && _buttonBConditionGroup.intValue != 0)
                _buttonBConditionGroup.intValue = 0;
        }

        private static void DrawButtonConditionDropdown(string label, SerializedProperty property, Step parentStep)
        {
            int groupCount = parentStep != null ? parentStep.ConditionGroupCount : 0;
            if (property.intValue < 0)
                property.intValue = 0;

            if (parentStep == null)
            {
                property.intValue = 0;
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Popup(label, 0, new[] { "없음" });
                EditorGUILayout.HelpBox(
                    "상위 Step을 찾을 수 없습니다. 버튼 조건은 Step 하위에 배치된 UINode에서만 설정할 수 있습니다.",
                    MessageType.None);
                return;
            }

            if (property.intValue > groupCount)
            {
                EditorGUILayout.HelpBox(
                    $"{label}이 조건 그룹 {property.intValue}을 사용하도록 저장되어 있지만, 상위 Step의 활성 조건 그룹은 {groupCount}개입니다. 없음으로 보정합니다.",
                    MessageType.Warning);
                property.intValue = 0;
            }

            var options = new string[groupCount + 1];
            options[0] = "없음";
            for (int i = 1; i <= groupCount; i++)
                options[i] = $"조건 그룹 {i}";

            if (groupCount == 0)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Popup(label, 0, options);
                EditorGUILayout.HelpBox(
                    "상위 Step에 활성 조건 그룹이 없습니다. 조건을 사용하려면 Step 인스펙터에서 조건 그룹 수를 먼저 늘리세요.",
                    MessageType.None);
                return;
            }

            property.intValue = EditorGUILayout.Popup(label, property.intValue, options);
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

        private void DrawWarnings()
        {
            EditorGUILayout.Space(4);
            DrawContentWarnings();
            DrawLayoutCapacityWarnings();
            DrawButtonAuthoringWarnings();
            DrawStepCompletionWarnings();
        }

        private void DrawContentWarnings()
        {
            if (!HasVisibleContent())
            {
                string message = HasEnabledElement()
                    ? "UI 요소가 켜져 있지만 실제 표시할 텍스트, 이미지, 비디오, 버튼이 없습니다. 런타임에서는 패널을 열지 않고 오류를 기록합니다."
                    : "활성화된 UI 요소가 없습니다. 런타임에서는 패널을 열지 않고 오류를 기록합니다.";

                EditorGUILayout.HelpBox(message, MessageType.Error);
            }

            if (!_useTitle.boolValue && _titleIcon.objectReferenceValue != null)
            {
                EditorGUILayout.HelpBox(
                    "제목 아이콘이 지정되어 있지만 Title 요소가 꺼져 있습니다. 제목 아이콘은 Title 요소와 함께 사용하는 구성을 권장합니다.",
                    MessageType.Warning);
            }

            if (_useTitle.boolValue && !HasVisibleTitle())
            {
                EditorGUILayout.HelpBox(
                    "Title 요소가 켜져 있지만 실제 표시할 제목 텍스트나 제목 아이콘이 없습니다. 런타임에서 제목 영역은 숨겨집니다.",
                    MessageType.Warning);
            }

            if (_useContext.boolValue && !HasVisibleContext())
            {
                EditorGUILayout.HelpBox(
                    "Context 요소가 켜져 있지만 실제 표시할 본문 텍스트가 없습니다. 런타임에서 해당 요소는 숨겨집니다.",
                    MessageType.Warning);
            }

            if (_useContextSub.boolValue && !HasVisibleContextSub())
            {
                EditorGUILayout.HelpBox(
                    "Context Sub 요소가 켜져 있지만 실제 표시할 하단 본문 텍스트가 없습니다. 런타임에서 해당 요소는 숨겨집니다.",
                    MessageType.Warning);
            }

            if (_useImageA.boolValue && !HasVisibleImageA())
            {
                EditorGUILayout.HelpBox(
                    "Image 요소가 켜져 있지만 실제 표시할 Sprite가 지정되지 않았습니다. 런타임에서 해당 요소는 숨겨집니다.",
                    MessageType.Warning);
            }

            if (_useImageSub.boolValue && !HasVisibleImageSub())
            {
                EditorGUILayout.HelpBox(
                    "Image 2 요소가 켜져 있지만 실제 표시할 Sprite가 지정되지 않았습니다. 런타임에서 해당 요소는 숨겨집니다.",
                    MessageType.Warning);
            }

            if (_useVideo.boolValue && !HasVisibleVideo())
            {
                EditorGUILayout.HelpBox(
                    "Video 요소가 켜져 있지만 실제 표시할 VideoClip이 지정되지 않았습니다. 런타임에서 해당 요소는 숨겨집니다.",
                    MessageType.Warning);
            }
        }

        private bool HasEnabledElement()
        {
            return
                _useTitle.boolValue ||
                _useContext.boolValue ||
                _useImageA.boolValue ||
                _useImageSub.boolValue ||
                _useVideo.boolValue ||
                _useButtonA.boolValue ||
                _useButtonB.boolValue ||
                _useContextSub.boolValue;
        }

        private bool HasVisibleNonTitleElement()
        {
            return
                HasVisibleContext() ||
                HasVisibleImageA() ||
                HasVisibleImageSub() ||
                HasVisibleVideo() ||
                HasVisibleButtonA() ||
                HasVisibleButtonB() ||
                HasVisibleContextSub();
        }

        private bool HasVisibleTitle()
        {
            return _useTitle.boolValue &&
                   (!string.IsNullOrWhiteSpace(_uiDataTitle.stringValue) ||
                    _titleIcon.objectReferenceValue != null);
        }

        private bool HasVisibleContext()
        {
            return _useContext.boolValue &&
                   !string.IsNullOrWhiteSpace(_uiDataContext.stringValue);
        }

        private bool HasVisibleImageA()
        {
            return _useImageA.boolValue &&
                   _uiDataImage.objectReferenceValue != null;
        }

        private bool HasVisibleImageSub()
        {
            return _useImageSub.boolValue &&
                   _uiDataImageSub.objectReferenceValue != null;
        }

        private bool HasVisibleVideo()
        {
            return _useVideo.boolValue &&
                   _uiDataVideo.objectReferenceValue != null;
        }

        private bool HasVisibleButtonA()
        {
            return _useButtonA.boolValue;
        }

        private bool HasVisibleButtonB()
        {
            return _useButtonB.boolValue;
        }

        private bool HasVisibleContextSub()
        {
            return _useContextSub.boolValue &&
                   !string.IsNullOrWhiteSpace(_uiDataContextSub.stringValue);
        }

        private bool HasVisibleContent()
        {
            return
                HasVisibleTitle() ||
                HasVisibleContext() ||
                HasVisibleImageA() ||
                HasVisibleImageSub() ||
                HasVisibleVideo() ||
                HasVisibleButtonA() ||
                HasVisibleButtonB() ||
                HasVisibleContextSub();
        }

        private void DrawLayoutCapacityWarnings()
        {
            if (!HasVisibleContent()) return;

            float estimatedHeight = EstimatePanelContentHeight();
            if (estimatedHeight <= PANEL_LAYOUT_REFERENCE_HEIGHT) return;

            EditorGUILayout.HelpBox(
                $"현재 UI 조합의 예상 최소 높이가 UIPanel 기준 높이({PANEL_LAYOUT_REFERENCE_HEIGHT:0}px)를 넘을 수 있습니다. " +
                $"추정 높이: {estimatedHeight:0}px. 긴 본문/하단 본문, 이미지 2개, 비디오, 버튼을 한 패널에 모두 넣기보다 Step을 나누거나 미디어 수를 줄이는 구성을 권장합니다. " +
                "이 경고는 작성 보조용이며 런타임 실행을 차단하지 않습니다.",
                MessageType.Warning);
        }

        private float EstimatePanelContentHeight()
        {
            var elementHeights = new List<float>();

            if (HasVisibleTitle())
            {
                elementHeights.Add(TITLE_ROW_HEIGHT);

                if (HasVisibleNonTitleElement())
                    elementHeights.Add(TITLE_CONTEXT_SPLITTER_HEIGHT);
            }

            if (HasVisibleContext())
                elementHeights.Add(EstimateTextHeight(_uiDataContext.stringValue));

            if (HasVisibleImageA())
                elementHeights.Add(IMAGE_HEIGHT);

            if (HasVisibleImageSub())
                elementHeights.Add(IMAGE_HEIGHT);

            if (HasVisibleVideo())
                elementHeights.Add(VIDEO_HEIGHT);

            if (HasVisibleButtonA() || HasVisibleButtonB())
                elementHeights.Add(BUTTON_ROW_HEIGHT);

            if (HasVisibleContextSub())
                elementHeights.Add(EstimateTextHeight(_uiDataContextSub.stringValue));

            if (elementHeights.Count == 0)
                return 0f;

            float totalHeight = CONTENT_VERTICAL_PADDING;
            for (int i = 0; i < elementHeights.Count; i++)
                totalHeight += elementHeights[i];

            totalHeight += CONTENT_SPACING * (elementHeights.Count - 1);
            return totalHeight;
        }

        private static float EstimateTextHeight(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0f;

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int estimatedLines = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                int lineLength = Mathf.Max(1, lines[i].Length);
                estimatedLines += Mathf.CeilToInt(lineLength / (float)TEXT_CHARS_PER_LINE);
            }

            return Mathf.Max(TEXT_MIN_HEIGHT, estimatedLines * TEXT_LINE_HEIGHT);
        }

        private void DrawButtonAuthoringWarnings()
        {
            bool hasButtonA = _useButtonA.boolValue;
            bool hasButtonB = _useButtonB.boolValue;
            if (!hasButtonA && !hasButtonB) return;

            if (hasButtonA && string.IsNullOrWhiteSpace(_uiDataButtonLabelA.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "버튼 A가 켜져 있지만 라벨이 비어 있습니다. 버튼 자체는 표시되므로 빈 UI로 차단되지는 않지만, 사용자가 의미를 알기 어렵습니다.",
                    MessageType.Warning);
            }

            if (hasButtonB && string.IsNullOrWhiteSpace(_uiDataButtonLabelB.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "버튼 B가 켜져 있지만 라벨이 비어 있습니다. 버튼 자체는 표시되므로 빈 UI로 차단되지는 않지만, 사용자가 의미를 알기 어렵습니다.",
                    MessageType.Warning);
            }

            if (hasButtonA &&
                _buttonAConditionGroup.intValue <= 0 &&
                !HasActivePersistentCall(_onButtonA) &&
                !HasActivePersistentCall(_onEnd))
            {
                EditorGUILayout.HelpBox(
                    "버튼 A는 표시되지만 클릭 이벤트가 없습니다. 이 UINode도 Step 조건이 아니므로 버튼 A 클릭만으로는 아무 동작도 일어나지 않습니다.",
                    MessageType.Warning);
            }

            if (hasButtonB &&
                _buttonBConditionGroup.intValue <= 0 &&
                !HasActivePersistentCall(_onButtonB) &&
                !HasActivePersistentCall(_onEnd))
            {
                EditorGUILayout.HelpBox(
                    "버튼 B는 표시되지만 클릭 이벤트가 없습니다. 이 UINode도 Step 조건이 아니므로 버튼 B 클릭만으로는 아무 동작도 일어나지 않습니다.",
                    MessageType.Warning);
            }
        }

        private void DrawStepCompletionWarnings()
        {
            bool hasButtons = _useButtonA.boolValue || _useButtonB.boolValue;
            var uiNode = (UINode)target;
            Step parentStep = uiNode.GetComponentInParent<Step>(true);
            bool hasButtonCondition =
                (_useButtonA.boolValue && _buttonAConditionGroup.intValue > 0) ||
                (_useButtonB.boolValue && _buttonBConditionGroup.intValue > 0);

            if (parentStep == null)
            {
                if (hasButtonCondition)
                    EditorGUILayout.HelpBox(
                        "버튼 조건 그룹이 지정되어 있지만 상위 Step을 찾을 수 없습니다.",
                        MessageType.Warning);
                return;
            }

            if (hasButtonCondition)
            {
                var buttonConditions = new List<string>();
                if (_useButtonA.boolValue && _buttonAConditionGroup.intValue > 0)
                    buttonConditions.Add($"A -> 조건 그룹 {_buttonAConditionGroup.intValue}");
                if (_useButtonB.boolValue && _buttonBConditionGroup.intValue > 0)
                    buttonConditions.Add($"B -> 조건 그룹 {_buttonBConditionGroup.intValue}");

                EditorGUILayout.HelpBox(
                    $"버튼 조건: {string.Join(", ", buttonConditions)}",
                    MessageType.Info);
            }

            var legacyMarkerGroups = CollectParentStepMarkerGroups(parentStep);
            if (legacyMarkerGroups.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"버튼/공통 UnityEvent에 MarkConditionGroupN 수동 연결이 남아 있습니다: {string.Join(", ", legacyMarkerGroups)}. 앞으로는 버튼 조건 드롭다운 사용을 권장합니다.",
                    MessageType.Warning);
            }

            foreach (int group in legacyMarkerGroups)
            {
                if (group > parentStep.ConditionGroupCount)
                {
                    EditorGUILayout.HelpBox(
                        $"버튼 UnityEvent가 MarkConditionGroup{group}을 호출하지만 상위 Step의 활성 조건 그룹 수는 {parentStep.ConditionGroupCount}개입니다. 이 marker는 런타임에서 무시됩니다.",
                        MessageType.Warning);
                }
            }

            if (!hasButtons)
                return;

            bool buttonCompletesParentStep =
                hasButtonCondition ||
                (_useButtonA.boolValue && HasParentStepCompletionCall(_onButtonA, parentStep)) ||
                (_useButtonB.boolValue && HasParentStepCompletionCall(_onButtonB, parentStep)) ||
                (hasButtons && HasParentStepCompletionCall(_onEnd, parentStep));

            if (!buttonCompletesParentStep)
            {
                EditorGUILayout.HelpBox(
                    "이 UINode 버튼은 조건 그룹도 만족시키지 않고, 상위 Step의 EndTrigger도 호출하지 않습니다.\n" +
                    "조건 그룹이 없는 Step은 자동 진행하지 않으므로, 이 UI만으로는 현재 Step이 종료되지 않을 수 있습니다.",
                    MessageType.Warning);
            }
        }

        private List<int> CollectParentStepMarkerGroups(Step parentStep)
        {
            var groups = new List<int>();
            if (_useButtonA.boolValue)
                CollectParentStepMarkerGroups(_onButtonA, parentStep, groups);
            if (_useButtonB.boolValue)
                CollectParentStepMarkerGroups(_onButtonB, parentStep, groups);
            if (_useButtonA.boolValue || _useButtonB.boolValue)
                CollectParentStepMarkerGroups(_onEnd, parentStep, groups);
            return groups.Distinct().OrderBy(group => group).ToList();
        }

        private static void CollectParentStepMarkerGroups(SerializedProperty unityEventProperty, Step parentStep, List<int> groups)
        {
            if (parentStep == null) return;

            var calls = GetPersistentCalls(unityEventProperty);
            if (calls == null) return;

            for (int i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                if (!IsActivePersistentCall(call)) continue;
                if (call.FindPropertyRelative("m_Target").objectReferenceValue != parentStep) continue;

                string methodName = call.FindPropertyRelative("m_MethodName").stringValue;
                if (TryGetConditionGroupMarker(methodName, out int group))
                    groups.Add(group);
            }
        }

        private static bool HasParentStepCompletionCall(SerializedProperty unityEventProperty, Step parentStep)
        {
            if (parentStep == null) return false;

            var calls = GetPersistentCalls(unityEventProperty);
            if (calls == null) return false;

            for (int i = 0; i < calls.arraySize; i++)
            {
                var call = calls.GetArrayElementAtIndex(i);
                if (!IsActivePersistentCall(call)) continue;
                if (call.FindPropertyRelative("m_Target").objectReferenceValue != parentStep) continue;

                string methodName = call.FindPropertyRelative("m_MethodName").stringValue;
                if (methodName == nameof(Step.EndTrigger))
                    return true;

                if (TryGetConditionGroupMarker(methodName, out int group) &&
                    group >= 1 &&
                    group <= parentStep.ConditionGroupCount)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasActivePersistentCall(SerializedProperty unityEventProperty)
        {
            var calls = GetPersistentCalls(unityEventProperty);
            if (calls == null) return false;

            for (int i = 0; i < calls.arraySize; i++)
            {
                if (IsActivePersistentCall(calls.GetArrayElementAtIndex(i)))
                    return true;
            }

            return false;
        }

        private static SerializedProperty GetPersistentCalls(SerializedProperty unityEventProperty)
        {
            return unityEventProperty?.FindPropertyRelative("m_PersistentCalls.m_Calls");
        }

        private static bool IsActivePersistentCall(SerializedProperty call)
        {
            var callState = call.FindPropertyRelative("m_CallState");
            return callState == null || callState.enumValueIndex != 0;
        }

        private static bool TryGetConditionGroupMarker(string methodName, out int group)
        {
            const string PREFIX = "MarkConditionGroup";
            group = 0;

            if (string.IsNullOrEmpty(methodName) || !methodName.StartsWith(PREFIX))
                return false;

            string groupText = methodName.Substring(PREFIX.Length);
            return int.TryParse(groupText, out group);
        }

        private bool DrawThemeDropdownWithDefault()
        {
            EditorGUILayout.LabelField("테마", EditorStyles.boldLabel);

            UITheme runtimeDefaultTheme = FindRuntimeDefaultTheme();
            UITheme defaultTheme = runtimeDefaultTheme != null
                ? runtimeDefaultTheme
                : DDOITEditorAssetLocator.FindDefaultTheme();
            string[] themeNames = BuildThemeNames(defaultTheme);
            var currentTheme = _theme.objectReferenceValue as UITheme;
            int selectedIndex = 0;

            if (currentTheme != null && _themes != null)
            {
                int themeIndex = System.Array.IndexOf(_themes, currentTheme);
                selectedIndex = themeIndex >= 0 ? themeIndex + 1 : 0;
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("테마", selectedIndex, themeNames);
            if (EditorGUI.EndChangeCheck())
                _theme.objectReferenceValue = selectedIndex == 0 ? null : _themes[selectedIndex - 1];

            if ((_themes == null || _themes.Length == 0) && defaultTheme == null)
                EditorGUILayout.HelpBox("UITheme 에셋이 없습니다.", MessageType.Warning);

            DrawThemeFallbackHint(currentTheme, runtimeDefaultTheme, defaultTheme);

            return true;
        }

        private static void DrawThemeFallbackHint(
            UITheme currentTheme,
            UITheme runtimeDefaultTheme,
            UITheme editorDefaultTheme)
        {
            if (currentTheme != null)
                return;

            if (runtimeDefaultTheme != null)
            {
                EditorGUILayout.HelpBox(
                    $"테마가 '기본'으로 설정되어 있습니다. 런타임에서는 UIManager의 기본 테마 '{runtimeDefaultTheme.name}'가 적용됩니다.",
                    MessageType.Info);
                return;
            }

            if (editorDefaultTheme != null)
            {
                EditorGUILayout.HelpBox(
                    $"테마가 '기본'으로 설정되어 있습니다. 현재 씬에서 UIManager 기본 테마를 확인할 수 없으므로 런타임 기본 테마는 DDOIT 씬의 UIManager 설정을 따릅니다. 참고 기본 에셋: '{editorDefaultTheme.name}'.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "테마가 '기본'으로 설정되어 있지만 UIManager 기본 테마와 기본 UITheme 에셋을 찾지 못했습니다. 런타임에서 테마 색상이 적용되지 않을 수 있습니다.",
                MessageType.Warning);
        }

        private string[] BuildThemeNames(UITheme defaultTheme)
        {
            int themeCount = _themes?.Length ?? 0;
            var names = new string[themeCount + 1];
            names[0] = "기본 (" + (defaultTheme != null ? defaultTheme.name : "미지정") + ")";

            for (int i = 0; i < themeCount; i++)
                names[i + 1] = _themes[i].name;

            return names;
        }

        private static UITheme FindRuntimeDefaultTheme()
        {
            var manager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            return manager != null ? manager.DefaultTheme : null;
        }

        private void RefreshThemeList()
        {
            _themes = DDOITEditorAssetLocator.FindUIThemes();
        }

        private void RefreshGlobalSettings()
        {
            _globalSettings = DDOITEditorAssetLocator.FindUIGlobalSettings();
        }
    }
}
