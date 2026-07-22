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

            EditorGUILayout.LabelField("트리거 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_detectMode, new GUIContent("감지 모드"));
            EditorGUILayout.PropertyField(_targetTag, new GUIContent("감지 태그"));
            EditorGUILayout.PropertyField(_colliderSource, new GUIContent("외부 Collider"));

            var mode = (TriggerDetectMode)_detectMode.enumValueIndex;

            // Stay 모드: 체류 시간
            if (mode == TriggerDetectMode.Stay)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_stayDuration, new GUIContent("체류 시간 (초)"));
                if (!executionDisabled && _stayDuration.floatValue <= 0f)
                    EditorGUILayout.HelpBox("체류 시간이 0 이하이면 진입 즉시 충족됩니다.", MessageType.Warning);
                EditorGUI.indentLevel--;
            }

            // 모드별 안내
            switch (mode)
            {
                case TriggerDetectMode.Enter:
                    EditorGUILayout.HelpBox("대상이 트리거에 진입하면 조건 충족", MessageType.None);
                    break;
                case TriggerDetectMode.Exit:
                    EditorGUILayout.HelpBox("대상이 트리거에서 이탈하면 조건 충족", MessageType.None);
                    break;
                case TriggerDetectMode.Stay:
                    EditorGUILayout.HelpBox("대상이 트리거 안에서 지정 시간 동안 체류하면 조건 충족\n중간에 이탈하면 타이머 리셋", MessageType.None);
                    break;
            }

            // Collider 관리
            if (_colliderSource.objectReferenceValue == null)
            {
                var node = (TriggerConditionNode)target;
                var col = node.GetComponent<Collider>();

                if (col != null)
                {
                    EditorGUILayout.Space(2);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField("현재 Collider", col, typeof(Collider), true);
                    EditorGUI.EndDisabledGroup();
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

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("감지 완료 이벤트"));

            serializedObject.ApplyModifiedProperties();
        }

        private void ReplaceCollider<T>(TriggerConditionNode node) where T : Collider
        {
            var existing = node.GetComponent<Collider>();
            if (existing is T) return;

            Undo.SetCurrentGroupName($"Collider → {typeof(T).Name}");

            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var col = Undo.AddComponent<T>(node.gameObject);
            col.isTrigger = true;
        }
    }
}
