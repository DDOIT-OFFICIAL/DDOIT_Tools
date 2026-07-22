using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TriggerConditionNode))]
    [CanEditMultipleObjects]
    public class TriggerConditionNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _conditionGroup;
        private SerializedProperty _detectMode;
        private SerializedProperty _targetTag;
        private SerializedProperty _colliderSource;
        private SerializedProperty _stayDuration;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _conditionGroup = serializedObject.FindProperty("_conditionGroup");
            _detectMode = serializedObject.FindProperty("_detectMode");
            _targetTag = serializedObject.FindProperty("_targetTag");
            _colliderSource = serializedObject.FindProperty("_colliderSource");
            _stayDuration = serializedObject.FindProperty("_stayDuration");
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

            var node = (TriggerConditionNode)target;
            var mode = (TriggerDetectMode)_detectMode.enumValueIndex;

            EditorGUILayout.LabelField("트리거 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_detectMode, new GUIContent("감지 모드"));
            EditorGUILayout.PropertyField(_targetTag, new GUIContent("감지 태그"));
            EditorGUILayout.PropertyField(_colliderSource, new GUIContent("외부 Collider"));

            if (string.IsNullOrWhiteSpace(_targetTag.stringValue))
                EditorGUILayout.HelpBox("감지 태그가 비어 있으면 Trigger 조건을 감지할 수 없습니다.", MessageType.Warning);

            if (mode == TriggerDetectMode.Stay)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_stayDuration, new GUIContent("체류 시간 (초)"));
                if (!executionDisabled && _stayDuration.floatValue <= 0f)
                    EditorGUILayout.HelpBox("체류 시간이 0 이하이면 진입 즉시 조건을 충족합니다.", MessageType.Warning);
                EditorGUI.indentLevel--;
            }

            DrawModeHelp(mode);
            DrawColliderTools(node);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("감지 완료 이벤트"));

            DrawRuntimeStatus(node);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawModeHelp(TriggerDetectMode mode)
        {
            switch (mode)
            {
                case TriggerDetectMode.Enter:
                    EditorGUILayout.HelpBox("대상이 트리거에 진입하면 조건을 충족합니다.", MessageType.None);
                    break;
                case TriggerDetectMode.Exit:
                    EditorGUILayout.HelpBox("대상이 트리거에서 이탈하면 조건을 충족합니다.", MessageType.None);
                    break;
                case TriggerDetectMode.Stay:
                    EditorGUILayout.HelpBox("대상이 트리거 안에서 지정 시간 동안 체류하면 조건을 충족합니다. 중간에 이탈하면 체류 타이머가 초기화됩니다.", MessageType.None);
                    break;
            }
        }

        private void DrawColliderTools(TriggerConditionNode node)
        {
            if (_colliderSource.objectReferenceValue != null)
                return;

            var col = node.GetComponent<Collider>();

            if (col != null)
            {
                EditorGUILayout.Space(2);
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField("현재 Collider", col, typeof(Collider), true);
                EditorGUI.EndDisabledGroup();

                if (!col.isTrigger)
                    EditorGUILayout.HelpBox("현재 Collider는 Play Mode 진입 시 Trigger로 자동 변경됩니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("자기 GameObject에 Collider가 없고 외부 Collider도 지정되지 않았습니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Collider 추가", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Box"))
                ReplaceCollider<BoxCollider>(node);
            if (GUILayout.Button("Sphere"))
                ReplaceCollider<SphereCollider>(node);
            if (GUILayout.Button("Capsule"))
                ReplaceCollider<CapsuleCollider>(node);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRuntimeStatus(TriggerConditionNode node)
        {
            if (!EditorApplication.isPlaying || node == null)
                return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("실행 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Active Collider", node.ActiveCollider != null ? node.ActiveCollider.name : "None");
            EditorGUILayout.LabelField("Current Target", node.CurrentTarget != null ? node.CurrentTarget.gameObject.name : "None");
            EditorGUILayout.LabelField("Matched Target", node.CurrentMatchedTarget != null ? node.CurrentMatchedTarget.name : "None");
            EditorGUILayout.LabelField("Inside Target Count", node.InsideTargetCount.ToString());
            EditorGUILayout.LabelField("Inside", node.IsInside ? "True" : "False");
            EditorGUILayout.LabelField("Condition Met", node.IsConditionMet ? "True" : "False");

            if (node.DetectMode == TriggerDetectMode.Stay)
            {
                EditorGUILayout.LabelField("Stay Running", node.IsStayRunning ? "True" : "False");
                EditorGUILayout.LabelField("Stay Elapsed", node.StayElapsedTime.ToString("0.000"));

                Rect rect = GUILayoutUtility.GetRect(18f, 18f);
                EditorGUI.ProgressBar(rect, node.StayProgress, $"{node.StayProgress * 100f:0}%");
            }

            Repaint();
        }

        private void ReplaceCollider<T>(TriggerConditionNode node) where T : Collider
        {
            var existing = node.GetComponent<Collider>();
            if (existing is T) return;

            Undo.SetCurrentGroupName($"Collider -> {typeof(T).Name}");

            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var col = Undo.AddComponent<T>(node.gameObject);
            col.isTrigger = true;
        }
    }
}
