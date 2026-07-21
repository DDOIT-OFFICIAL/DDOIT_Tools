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
            _onEnd = serializedObject.FindProperty("_onEnd");

            RefreshThemeList();
            RefreshGlobalSettings();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Base
            ConditionGroupDrawer.Draw(_conditionGroup, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

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
            DrawWarnings();

            serializedObject.ApplyModifiedProperties();
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

            if (_useButtonA.boolValue)
                EditorGUILayout.PropertyField(_onButtonA, new GUIContent("버튼 A 클릭"));

            if (_useButtonB.boolValue)
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

        private bool HasEnabledNonTitleElement()
        {
            return
                _useContext.boolValue ||
                _useImageA.boolValue ||
                _useImageSub.boolValue ||
                _useVideo.boolValue ||
                _useButtonA.boolValue ||
                _useButtonB.boolValue ||
                _useContextSub.boolValue;
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

            if (_useTitle.boolValue)
            {
                elementHeights.Add(TITLE_ROW_HEIGHT);

                if (HasEnabledNonTitleElement())
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

            if (_conditionGroup.intValue > 0) return;

            if (hasButtonA && !HasActivePersistentCall(_onButtonA) && !HasActivePersistentCall(_onEnd))
            {
                EditorGUILayout.HelpBox(
                    "버튼 A는 표시되지만 클릭 이벤트가 없습니다. 이 UINode도 Step 조건이 아니므로 버튼 A 클릭만으로는 아무 동작도 일어나지 않습니다.",
                    MessageType.Warning);
            }

            if (hasButtonB && !HasActivePersistentCall(_onButtonB) && !HasActivePersistentCall(_onEnd))
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
            int conditionGroup = _conditionGroup.intValue;

            if (conditionGroup > 0)
            {
                if (parentStep == null)
                {
                    EditorGUILayout.HelpBox(
                        "조건 그룹이 지정되어 있지만 상위 Step을 찾을 수 없습니다. 런타임에서 조건 완료를 보고할 대상이 없습니다.",
                        MessageType.Warning);
                }
                else if (conditionGroup > parentStep.ConditionGroupCount)
                {
                    EditorGUILayout.HelpBox(
                        $"조건 그룹 {conditionGroup}은 상위 Step의 그룹 수({parentStep.ConditionGroupCount})를 초과합니다. 런타임에서 이 조건은 Step 완료에 사용되지 않습니다.",
                        MessageType.Warning);
                }

                if (hasButtons)
                {
                    EditorGUILayout.HelpBox(
                        "버튼 클릭 시 이 UINode의 Step 조건이 충족됩니다.",
                        MessageType.Info);

                    if (parentStep != null)
                        DrawExternalMarkerConflictWarnings(parentStep, conditionGroup);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "버튼이 없는 UI를 Step 조건으로 사용하면 조건이 자동 충족되지 않습니다.\n" +
                        "다른 노드나 외부 로직에서 Step을 종료해야 합니다.",
                        MessageType.Warning);
                }

                return;
            }

            if (!hasButtons || parentStep == null || parentStep.ConditionGroupCount <= 0)
                return;

            bool buttonCompletesParentStep =
                HasParentStepCompletionCall(_onButtonA, parentStep) ||
                HasParentStepCompletionCall(_onButtonB, parentStep) ||
                HasParentStepCompletionCall(_onEnd, parentStep);

            if (!buttonCompletesParentStep)
            {
                EditorGUILayout.HelpBox(
                    "이 UINode는 Step 조건이 아니며, 버튼 이벤트도 상위 Step의 EndTrigger 또는 MarkConditionGroupN을 호출하지 않습니다.\n" +
                    "이 UI만으로는 현재 Step이 완료되지 않습니다.",
                    MessageType.Warning);
                return;
            }

            var markerGroups = CollectParentStepMarkerGroups(parentStep);
            if (markerGroups.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"버튼 이벤트가 상위 Step의 외부 marker 그룹 {string.Join(", ", markerGroups)}을 충족합니다.",
                    MessageType.Info);
            }
        }

        private void DrawExternalMarkerConflictWarnings(Step parentStep, int conditionGroup)
        {
            var markerGroups = CollectParentStepMarkerGroups(parentStep);
            foreach (int group in markerGroups)
            {
                if (group > parentStep.ConditionGroupCount)
                {
                    EditorGUILayout.HelpBox(
                        $"버튼 이벤트가 MarkConditionGroup{group}을 호출하지만 상위 Step의 그룹 수는 {parentStep.ConditionGroupCount}입니다. 이 marker는 무시됩니다.",
                        MessageType.Warning);
                }
                else if (group != conditionGroup)
                {
                    EditorGUILayout.HelpBox(
                        $"이 UINode 자체는 그룹 {conditionGroup} 조건인데, 버튼 이벤트는 그룹 {group} marker도 호출합니다. 두 그룹이 동시에 충족되어 의도한 분기보다 낮은 번호 그룹이 먼저 처리될 수 있습니다.\n" +
                        "버튼별 분기를 의도했다면 UINode의 조건 그룹을 '없음'으로 두고 버튼 이벤트에서 MarkConditionGroupN만 호출하는 구성을 권장합니다.",
                        MessageType.Warning);
                }
            }
        }

        private List<int> CollectParentStepMarkerGroups(Step parentStep)
        {
            var groups = new List<int>();
            CollectParentStepMarkerGroups(_onButtonA, parentStep, groups);
            CollectParentStepMarkerGroups(_onButtonB, parentStep, groups);
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
