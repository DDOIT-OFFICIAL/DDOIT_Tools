using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;
using DDOIT.Tools.Scenario;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(ToggleNode))]
    [CanEditMultipleObjects]
    public class ToggleNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _mode;
        private SerializedProperty _targetObject;
        private SerializedProperty _targetComponent;
        private SerializedProperty _targetParticle;
        private SerializedProperty _targetScript;
        private SerializedProperty _activate;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _mode = serializedObject.FindProperty("_mode");
            _targetObject = serializedObject.FindProperty("_targetObject");
            _targetComponent = serializedObject.FindProperty("_targetComponent");
            _targetParticle = serializedObject.FindProperty("_targetParticle");
            _targetScript = serializedObject.FindProperty("_targetScript");
            _activate = serializedObject.FindProperty("_activate");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            bool executionDisabled = ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("토글 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_mode, new GUIContent("모드"));

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
                case ToggleMode.Script:
                    EditorGUILayout.PropertyField(_targetScript, new GUIContent("대상"));
                    if (!executionDisabled)
                        DrawScriptWarnings();
                    break;
            }

            EditorGUILayout.Space(4);
            DrawActivateToggle(mode);

            if (!executionDisabled)
                DrawWarnings(mode);

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("완료 이벤트"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawActivateToggle(ToggleMode mode)
        {
            string onLabel = mode == ToggleMode.Script ? "Go()" : "ON";
            string offLabel = mode == ToggleMode.Script ? "Stop()" : "OFF";

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
            if (GUILayout.Button(onLabel, onStyle))
                _activate.boolValue = true;

            GUI.backgroundColor = !_activate.boolValue ? new Color(0.8f, 0.3f, 0.3f) : Color.white;
            if (GUILayout.Button(offLabel, offStyle))
                _activate.boolValue = false;

            GUI.backgroundColor = originalBg;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawScriptWarnings()
        {
            var script = _targetScript.objectReferenceValue as MonoBehaviour;
            if (script != null && !(script is IToggleable))
            {
                EditorGUILayout.HelpBox(
                    $"'{script.GetType().Name}'은 IToggleable을 구현하지 않습니다.",
                    MessageType.Error);
            }
        }

        private void DrawWarnings(ToggleMode mode)
        {
            bool hasTarget = mode switch
            {
                ToggleMode.GameObject => _targetObject.objectReferenceValue != null,
                ToggleMode.Component => _targetComponent.objectReferenceValue != null,
                ToggleMode.Particle => _targetParticle.objectReferenceValue != null,
                ToggleMode.Script => _targetScript.objectReferenceValue != null,
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
