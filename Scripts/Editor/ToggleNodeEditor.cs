using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(ToggleNode))]
    public class ToggleNodeEditor : UnityEditor.Editor
    {
        // Toggle
        private SerializedProperty _mode;
        private SerializedProperty _targetObject;
        private SerializedProperty _targetComponent;
        private SerializedProperty _targetParticle;
        private SerializedProperty _activate;

        private void OnEnable()
        {
            _mode = serializedObject.FindProperty("_mode");
            _targetObject = serializedObject.FindProperty("_targetObject");
            _targetComponent = serializedObject.FindProperty("_targetComponent");
            _targetParticle = serializedObject.FindProperty("_targetParticle");
            _activate = serializedObject.FindProperty("_activate");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Mode
            EditorGUILayout.LabelField("토글 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_mode, new GUIContent("모드"));

            // Mode별 대상 필드
            var mode = (ToggleMode)_mode.enumValueIndex;
            switch (mode)
            {
                case ToggleMode.GameObject:
                    EditorGUILayout.PropertyField(_targetObject, new GUIContent("대상"));
                    break;
                case ToggleMode.Component:
                    EditorGUILayout.PropertyField(_targetComponent, new GUIContent("대상"));
                    break;
                case ToggleMode.Particle:
                    EditorGUILayout.PropertyField(_targetParticle, new GUIContent("대상"));
                    break;
            }

            // On/Off 토글
            EditorGUILayout.Space(4);
            DrawActivateToggle();

            // 경고
            DrawWarnings(mode);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActivateToggle()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("동작");

            var onStyle = _activate.boolValue
                ? new GUIStyle(EditorStyles.miniButtonLeft) { fontStyle = FontStyle.Bold }
                : EditorStyles.miniButtonLeft;
            var offStyle = !_activate.boolValue
                ? new GUIStyle(EditorStyles.miniButtonRight) { fontStyle = FontStyle.Bold }
                : EditorStyles.miniButtonRight;

            var originalBg = GUI.backgroundColor;

            GUI.backgroundColor = _activate.boolValue ? new Color(0.3f, 0.8f, 0.3f) : Color.white;
            if (GUILayout.Button("ON", onStyle))
                _activate.boolValue = true;

            GUI.backgroundColor = !_activate.boolValue ? new Color(0.8f, 0.3f, 0.3f) : Color.white;
            if (GUILayout.Button("OFF", offStyle))
                _activate.boolValue = false;

            GUI.backgroundColor = originalBg;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawWarnings(ToggleMode mode)
        {
            bool hasTarget = mode switch
            {
                ToggleMode.GameObject => _targetObject.objectReferenceValue != null,
                ToggleMode.Component => _targetComponent.objectReferenceValue != null,
                ToggleMode.Particle => _targetParticle.objectReferenceValue != null,
                _ => false,
            };

            if (!hasTarget)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("대상이 지정되지 않았습니다.", MessageType.Warning);
            }
        }
    }
}
