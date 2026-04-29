using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(AnimatorNode))]
    public class AnimatorNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _animator;
        private SerializedProperty _paramType;
        private SerializedProperty _paramName;
        private SerializedProperty _boolValue;
        private SerializedProperty _intValue;
        private SerializedProperty _floatValue;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _animator = serializedObject.FindProperty("_animator");
            _paramType = serializedObject.FindProperty("_paramType");
            _paramName = serializedObject.FindProperty("_paramName");
            _boolValue = serializedObject.FindProperty("_boolValue");
            _intValue = serializedObject.FindProperty("_intValue");
            _floatValue = serializedObject.FindProperty("_floatValue");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 대상
            EditorGUILayout.LabelField("대상", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_animator, new GUIContent("Animator"));
            EditorGUILayout.Space(4);

            var controller = GetAnimatorController();

            // 파라미터
            EditorGUILayout.LabelField("파라미터", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_paramType, new GUIContent("타입"));

            var type = (AnimatorParamType)_paramType.enumValueIndex;
            DrawParamNameDropdown(controller, type);

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

            // 경고
            if (_animator.objectReferenceValue == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("Animator가 지정되지 않았습니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("완료 이벤트"));

            serializedObject.ApplyModifiedProperties();
        }

        #region Animator Helpers

        private AnimatorController GetAnimatorController()
        {
            var animator = _animator.objectReferenceValue as Animator;
            if (animator == null || animator.runtimeAnimatorController == null)
                return null;

            return animator.runtimeAnimatorController as AnimatorController;
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

        #region Dropdowns

        private void DrawParamNameDropdown(AnimatorController controller, AnimatorParamType type)
        {
            if (controller == null)
            {
                EditorGUILayout.PropertyField(_paramName, new GUIContent("이름"));
                return;
            }

            var unityType = ToUnityParamType(type);
            var names = new List<string>();
            foreach (var param in controller.parameters)
            {
                if (param.type == unityType)
                    names.Add(param.name);
            }

            if (names.Count == 0)
            {
                EditorGUILayout.PropertyField(_paramName, new GUIContent("이름"));
                EditorGUILayout.HelpBox($"{type} 타입의 파라미터가 없습니다.", MessageType.Warning);
                return;
            }

            int selectedIndex = names.IndexOf(_paramName.stringValue);
            if (selectedIndex < 0) selectedIndex = 0;

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUILayout.Popup("이름", selectedIndex, names.ToArray());
            if (EditorGUI.EndChangeCheck())
                _paramName.stringValue = names[selectedIndex];
        }

        #endregion
    }
}
