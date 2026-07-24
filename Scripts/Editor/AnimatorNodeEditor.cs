using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(AnimatorNode))]
    [CanEditMultipleObjects]
    public class AnimatorNodeEditor : UnityEditor.Editor
    {
        #region Constants

        private const string CONDITION_GROUP_PROPERTY = "_conditionGroup";

        #endregion

        #region Serialized Properties

        private SerializedProperty _conditionGroup;
        private SerializedProperty _animator;
        private SerializedProperty _paramType;
        private SerializedProperty _paramName;
        private SerializedProperty _boolValue;
        private SerializedProperty _intValue;
        private SerializedProperty _floatValue;
        private SerializedProperty _onEnd;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _conditionGroup = serializedObject.FindProperty(CONDITION_GROUP_PROPERTY);
            _animator = serializedObject.FindProperty("_animator");
            _paramType = serializedObject.FindProperty("_paramType");
            _paramName = serializedObject.FindProperty("_paramName");
            _boolValue = serializedObject.FindProperty("_boolValue");
            _intValue = serializedObject.FindProperty("_intValue");
            _floatValue = serializedObject.FindProperty("_floatValue");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        #endregion

        #region Inspector

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool clearedLegacyConditionGroup = ClearHiddenConditionGroup();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            bool executionDisabled = ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            if (clearedLegacyConditionGroup)
            {
                EditorGUILayout.HelpBox(
                    "AnimatorNode는 즉시 실행 노드이므로 Step 조건 그룹에 참여하지 않습니다. 숨겨져 있던 기존 조건 그룹 값은 0으로 정리했습니다.",
                    MessageType.Info);
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.LabelField("대상", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animator, new GUIContent("Animator"));
            EditorGUILayout.Space(4);

            var animator = _animator.objectReferenceValue as Animator;
            var type = (AnimatorParamType)_paramType.enumValueIndex;

            EditorGUILayout.LabelField("파라미터", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_paramType, new GUIContent("타입"));
            DrawParamNameField(animator, type);
            DrawValueField(type);

            if (!executionDisabled)
                DrawWarnings(animator, type);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("완료 이벤트"));

            DrawRuntimeStatus((AnimatorNode)target);

            serializedObject.ApplyModifiedProperties();
        }

        private bool ClearHiddenConditionGroup()
        {
            if (_conditionGroup == null || serializedObject.isEditingMultipleObjects)
                return false;

            if (_conditionGroup.intValue == 0)
                return false;

            _conditionGroup.intValue = 0;
            return true;
        }

        private void DrawValueField(AnimatorParamType type)
        {
            switch (type)
            {
                case AnimatorParamType.Bool:
                    EditorGUILayout.PropertyField(_boolValue, new GUIContent("값"));
                    break;
                case AnimatorParamType.Int:
                    EditorGUILayout.PropertyField(_intValue, new GUIContent("값"));
                    break;
                case AnimatorParamType.Float:
                    EditorGUILayout.PropertyField(_floatValue, new GUIContent("값"));
                    break;
            }
        }

        private void DrawRuntimeStatus(AnimatorNode node)
        {
            if (!EditorApplication.isPlaying || node == null)
                return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("실행 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Last Result", node.LastExecutionSucceeded ? "Success" : "Failed / Not Executed");
            EditorGUILayout.LabelField("Message", node.LastExecutionMessage);
            Repaint();
        }

        #endregion

        #region Parameter UI

        private void DrawParamNameField(Animator animator, AnimatorParamType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                EditorGUILayout.PropertyField(_paramName, new GUIContent("이름"));
                return;
            }

            List<string> matchingNames = GetParameterNames(animator, ToUnityParamType(type));
            if (matchingNames.Count == 0)
            {
                EditorGUILayout.PropertyField(_paramName, new GUIContent("이름"));
                EditorGUILayout.HelpBox($"{type} 타입의 파라미터가 Animator에 없습니다.", MessageType.Warning);
                return;
            }

            string currentName = _paramName.stringValue;
            bool hasCurrentName = !string.IsNullOrWhiteSpace(currentName);
            bool currentIsValid = hasCurrentName && matchingNames.Contains(currentName);

            var options = new List<string>();
            int selectedIndex;

            if (!hasCurrentName)
            {
                options.Add("선택 안 함");
                options.AddRange(matchingNames);
                selectedIndex = 0;
            }
            else if (!currentIsValid)
            {
                options.Add($"현재 값 유지: {currentName}");
                options.AddRange(matchingNames);
                selectedIndex = 0;
            }
            else
            {
                options.AddRange(matchingNames);
                selectedIndex = matchingNames.IndexOf(currentName);
            }

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup("이름", selectedIndex, options.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                if (!hasCurrentName || !currentIsValid)
                {
                    if (nextIndex > 0)
                        _paramName.stringValue = options[nextIndex];
                }
                else
                {
                    _paramName.stringValue = options[nextIndex];
                }
            }

            if (!hasCurrentName)
                EditorGUILayout.HelpBox("Animator 파라미터를 선택해야 실행 시 값을 적용할 수 있습니다.", MessageType.Warning);
            else if (!currentIsValid)
                EditorGUILayout.HelpBox($"'{currentName}' 파라미터가 Animator의 {type} 목록에 없습니다.", MessageType.Warning);
        }

        private static List<string> GetParameterNames(Animator animator, AnimatorControllerParameterType type)
        {
            var names = new List<string>();
            if (animator == null)
                return names;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == type)
                    names.Add(parameter.name);
            }

            return names;
        }

        #endregion

        #region Validation

        private void DrawWarnings(Animator animator, AnimatorParamType type)
        {
            if (animator == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Animator가 지정되지 않았습니다.", MessageType.Warning);
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Animator에 Runtime Animator Controller가 없습니다.", MessageType.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_paramName.stringValue))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("파라미터 이름이 비어 있습니다.", MessageType.Warning);
                return;
            }

            AnimatorControllerParameter parameter = FindParameter(animator, _paramName.stringValue);
            if (parameter == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox($"Animator에 '{_paramName.stringValue}' 파라미터가 없습니다.", MessageType.Warning);
                return;
            }

            AnimatorControllerParameterType expectedType = ToUnityParamType(type);
            if (parameter.type != expectedType)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    $"'{_paramName.stringValue}' 파라미터 타입이 일치하지 않습니다. 현재: {parameter.type}, 필요: {expectedType}.",
                    MessageType.Warning);
            }
        }

        private static AnimatorControllerParameter FindParameter(Animator animator, string paramName)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == paramName)
                    return parameter;
            }

            return null;
        }

        private static AnimatorControllerParameterType ToUnityParamType(AnimatorParamType type)
        {
            return type switch
            {
                AnimatorParamType.Trigger => AnimatorControllerParameterType.Trigger,
                AnimatorParamType.Bool => AnimatorControllerParameterType.Bool,
                AnimatorParamType.Int => AnimatorControllerParameterType.Int,
                AnimatorParamType.Float => AnimatorControllerParameterType.Float,
                _ => AnimatorControllerParameterType.Trigger,
            };
        }

        #endregion
    }
}
