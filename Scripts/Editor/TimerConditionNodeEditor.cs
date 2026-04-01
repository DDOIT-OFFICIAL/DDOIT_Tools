using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TimerConditionNode))]
    public class TimerConditionNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _isStepCondition;
        private SerializedProperty _onRelease;
        private SerializedProperty _duration;

        private void OnEnable()
        {
            _isStepCondition = serializedObject.FindProperty("_isStepCondition");
            _onRelease = serializedObject.FindProperty("_onRelease");
            _duration = serializedObject.FindProperty("_duration");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_isStepCondition, new GUIContent("Step 조건"));
            EditorGUILayout.PropertyField(_onRelease);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("타이머 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_duration, new GUIContent("대기 시간 (초)"));

            if (_duration.floatValue <= 0f)
                EditorGUILayout.HelpBox("대기 시간이 0 이하이면 즉시 완료됩니다.", MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
