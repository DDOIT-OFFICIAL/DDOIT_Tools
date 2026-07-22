using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TimerConditionNode))]
    [CanEditMultipleObjects]
    public class TimerConditionNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _timerMode;
        private SerializedProperty _conditionGroup;
        private SerializedProperty _duration;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _timerMode = serializedObject.FindProperty("_timerMode");
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

            EditorGUILayout.LabelField("타이머 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_timerMode, new GUIContent("타이머 모드"));

            bool isCountUp = _timerMode.enumValueIndex == (int)TimerConditionNode.TimerMode.CountUp;

            if (isCountUp)
            {
                EditorGUILayout.HelpBox(
                    "CountUp 모드는 Step 완료 조건에 참여하지 않고 시간 측정만 수행합니다. Release 시 현재 시간이 저장되고 CurrentTime은 0으로 초기화됩니다.",
                    MessageType.Info);

                if (_conditionGroup.intValue > 0)
                {
                    EditorGUILayout.HelpBox(
                        "Condition Group 값이 남아 있어도 CountUp 모드에서는 무시됩니다. Countdown으로 되돌리면 다시 사용됩니다.",
                        MessageType.None);
                }
            }
            else
            {
                ConditionGroupDrawer.Draw(_conditionGroup, (MonoBehaviour)target);
                EditorGUILayout.Space(4);

                EditorGUILayout.PropertyField(_duration, new GUIContent("대기 시간 (초)"));

                if (!executionDisabled && _duration.floatValue <= 0f)
                    EditorGUILayout.HelpBox("대기 시간이 0 이하이면 즉시 완료됩니다.", MessageType.Warning);

                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_onEnd, new GUIContent("타이머 완료 이벤트"));
            }

            DrawRuntimeStatus((TimerConditionNode)target);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawRuntimeStatus(TimerConditionNode node)
        {
            if (!EditorApplication.isPlaying || node == null)
                return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("실행 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current Time", node.CurrentTime.ToString("0.000"));
            EditorGUILayout.LabelField("Elapsed Time", node.ElapsedTime.ToString("0.000"));
            EditorGUILayout.LabelField("Remaining Time", node.RemainingTime.ToString("0.000"));
            EditorGUILayout.LabelField("Running", node.IsRunning ? "True" : "False");
            EditorGUILayout.LabelField("Paused", node.IsPaused ? "True" : "False");
            EditorGUILayout.LabelField("Saved Times", node.SavedTimes.Count.ToString());

            if (node.HasSavedTime)
                EditorGUILayout.LabelField("Last Saved Time", node.LastSavedTime.ToString("0.000"));
        }
    }
}
