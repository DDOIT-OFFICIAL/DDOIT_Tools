using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TimerConditionNode))]
    [CanEditMultipleObjects]
    public class TimerConditionNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _conditionGroup;
        private SerializedProperty _duration;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _conditionGroup = serializedObject.FindProperty("_conditionGroup");
            _duration = serializedObject.FindProperty("_duration");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            bool executionDisabled = ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            ConditionGroupDrawer.Draw(_conditionGroup, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("타이머 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_duration, new GUIContent("대기 시간 (초)"));

            if (!executionDisabled && _duration.floatValue <= 0f)
                EditorGUILayout.HelpBox("대기 시간이 0 이하이면 즉시 완료됩니다.", MessageType.Warning);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("타이머 완료 이벤트"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
