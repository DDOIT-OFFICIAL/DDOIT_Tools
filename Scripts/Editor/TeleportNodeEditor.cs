using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TeleportNode))]
    public class TeleportNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _destination;
        private SerializedProperty _fadeDuration;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _destination = serializedObject.FindProperty("_destination");
            _fadeDuration = serializedObject.FindProperty("_fadeDuration");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("텔레포트 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_destination, new GUIContent("목적지"));

            // TODO: 전체 세팅 SO 연동 시 제거
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_fadeDuration, new GUIContent("페이드 시간 (초)"));
            EditorGUI.EndDisabledGroup();

            if (_destination.objectReferenceValue == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("목적지 Transform이 지정되지 않았습니다.", MessageType.Warning);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("텔레포트 완료 이벤트"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
