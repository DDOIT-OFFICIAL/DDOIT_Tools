using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TriggerConditionNode))]
    public class TriggerConditionNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _isStepCondition;
        private SerializedProperty _onRelease;
        private SerializedProperty _targetTag;
        private SerializedProperty _colliderSource;

        private void OnEnable()
        {
            _isStepCondition = serializedObject.FindProperty("_isStepCondition");
            _onRelease = serializedObject.FindProperty("_onRelease");
            _targetTag = serializedObject.FindProperty("_targetTag");
            _colliderSource = serializedObject.FindProperty("_colliderSource");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_isStepCondition, new GUIContent("Step 조건"));
            EditorGUILayout.PropertyField(_onRelease);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("트리거 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_targetTag, new GUIContent("감지 태그"));
            EditorGUILayout.PropertyField(_colliderSource, new GUIContent("외부 Collider"));

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

            serializedObject.ApplyModifiedProperties();
        }

        private void ReplaceCollider<T>(TriggerConditionNode node) where T : Collider
        {
            // 이미 같은 타입이면 무시
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
