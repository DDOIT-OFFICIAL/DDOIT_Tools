using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(WalkingStickNode))]
    [CanEditMultipleObjects]
    public class WalkingStickNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _enable;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _enable = serializedObject.FindProperty("_enable");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "현재 DDOIT 기본 워크플로는 컨트롤러 이동 중심입니다. WalkingStickNode는 추후 핸드 이동 워크플로 재도입을 위해 보존되지만, Step의 노드 추가 메뉴에서는 숨김 처리됩니다. 기존 씬에 배치된 노드만 계속 편집/실행됩니다.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("WalkingStick 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_enable, new GUIContent("활성화"));

            if (_enable.boolValue)
                EditorGUILayout.HelpBox(
                    "WalkingStick을 활성화합니다. 호출 시점의 HMD 높이로 stick 길이가 자동 결정됩니다.",
                    MessageType.None);
            else
                EditorGUILayout.HelpBox("WalkingStick을 비활성화합니다.", MessageType.None);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("완료 이벤트"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
