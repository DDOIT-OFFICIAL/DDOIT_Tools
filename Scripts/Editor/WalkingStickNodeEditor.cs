using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(WalkingStickNode))]
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
