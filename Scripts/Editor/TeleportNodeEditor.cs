using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;
using DDOIT.Tools.Settings;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TeleportNode))]
    [CanEditMultipleObjects]
    public class TeleportNodeEditor : UnityEditor.Editor
    {
        #region Constants

        private const string CONDITION_GROUP_PROPERTY = "_conditionGroup";

        #endregion

        #region Serialized Properties

        private SerializedProperty _conditionGroup;
        private SerializedProperty _destination;
        private SerializedProperty _onEnd;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _conditionGroup = serializedObject.FindProperty(CONDITION_GROUP_PROPERTY);
            _destination = serializedObject.FindProperty("_destination");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        #endregion

        #region Inspector

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool clearedLegacyConditionGroup = ClearHiddenConditionGroup();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            bool executionDisabled = ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            if (clearedLegacyConditionGroup)
            {
                EditorGUILayout.HelpBox(
                    "TeleportNode는 즉시 실행 노드이므로 Step 조건 그룹에 참여하지 않습니다. 숨겨져 있던 기존 조건 그룹 값은 0으로 정리했습니다.",
                    MessageType.Info);
                EditorGUILayout.Space(4);
            }

            EditorGUILayout.LabelField("텔레포트 설정", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_destination, new GUIContent("목적지"));

            DrawFadeDuration();

            if (!executionDisabled)
                DrawWarnings();

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("텔레포트 완료 이벤트"));

            DrawRuntimeStatus((TeleportNode)target);

            serializedObject.ApplyModifiedProperties();
        }

        private bool ClearHiddenConditionGroup()
        {
            if (_conditionGroup == null || serializedObject.isEditingMultipleObjects)
                return false;

            if (_conditionGroup.intValue == 0)
                return false;

            _conditionGroup.intValue = 0;
            return true;
        }

        private void DrawFadeDuration()
        {
            var settings = DDOITSettings.Instance;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField(
                "페이드 시간 (초)",
                settings != null ? Mathf.Max(0f, settings.teleportFadeDuration) : 1f);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.HelpBox("Settings 탭에서 변경 가능", MessageType.None);
        }

        private void DrawWarnings()
        {
            if (_destination.objectReferenceValue == null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox("목적지 Transform이 지정되지 않았습니다.", MessageType.Warning);
            }
        }

        private void DrawRuntimeStatus(TeleportNode node)
        {
            if (!EditorApplication.isPlaying || node == null)
                return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("실행 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("State", node.State.ToString());
            EditorGUILayout.LabelField("Running", node.IsRunning ? "True" : "False");
            EditorGUILayout.LabelField("Released", node.IsReleased ? "True" : "False");
            EditorGUILayout.LabelField("Did Teleport", node.DidTeleport ? "True" : "False");
            EditorGUILayout.LabelField("Last Result", node.LastExecutionSucceeded ? "Success" : "Failed / Not Executed");
            EditorGUILayout.LabelField("Message", node.LastExecutionMessage);
            Repaint();
        }

        #endregion
    }
}
