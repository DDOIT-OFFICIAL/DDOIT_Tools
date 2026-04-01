using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(Step))]
    public class StepEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var step = (Step)target;
            var nodes = step.GetComponentsInChildren<ScenarioNode>(true);

            if (nodes.Length > 0)
            {
                EditorGUILayout.Space();
                DrawNodeList(nodes);
            }

            if (Application.isPlaying && step.IsActive)
            {
                EditorGUILayout.Space();
                DrawRuntimeStatus(nodes);
            }

            EditorGUILayout.Space();
            DrawAddNodeButtons(step);
        }

        private void DrawNodeList(ScenarioNode[] nodes)
        {
            // 조건 노드 개수 계산
            int conditionCount = 0;
            foreach (var node in nodes)
            {
                if (node.IsStepCondition) conditionCount++;
            }

            EditorGUILayout.LabelField($"노드 목록 ({nodes.Length}개, 조건 {conditionCount}개)", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                string typeName = node.GetType().Name;

                EditorGUILayout.BeginHorizontal();

                // 런타임 조건 충족 표시
                if (Application.isPlaying && node.IsStepCondition)
                {
                    var prevColor = GUI.contentColor;
                    GUI.contentColor = node.IsConditionMet ? Color.green : Color.yellow;
                    EditorGUILayout.LabelField(node.IsConditionMet ? "✓" : "○", GUILayout.Width(14));
                    GUI.contentColor = prevColor;
                }
                else
                {
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(14));
                }

                // 노드 이름 (클릭하면 해당 오브젝트 선택)
                if (GUILayout.Button(node.gameObject.name, EditorStyles.label))
                {
                    Selection.activeGameObject = node.gameObject;
                    EditorGUIUtility.PingObject(node.gameObject);
                }

                // 타입 표시
                var prevContentColor = GUI.contentColor;
                GUI.contentColor = new Color(0.7f, 0.8f, 1f);
                EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel, GUILayout.Width(130));
                GUI.contentColor = prevContentColor;

                // 조건 토글 상태
                prevContentColor = GUI.contentColor;
                if (node.IsStepCondition)
                {
                    GUI.contentColor = new Color(0.5f, 1f, 0.5f);
                    EditorGUILayout.LabelField("[조건]", EditorStyles.miniLabel, GUILayout.Width(40));
                }
                else
                {
                    GUI.contentColor = Color.gray;
                    EditorGUILayout.LabelField("—", EditorStyles.miniLabel, GUILayout.Width(40));
                }
                GUI.contentColor = prevContentColor;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeStatus(ScenarioNode[] nodes)
        {
            EditorGUILayout.LabelField("런타임 상태", EditorStyles.boldLabel);

            int metCount = 0;
            int totalConditions = 0;
            foreach (var node in nodes)
            {
                if (node.IsStepCondition)
                {
                    totalConditions++;
                    if (node.IsConditionMet) metCount++;
                }
            }

            if (totalConditions == 0)
            {
                EditorGUILayout.HelpBox("조건 노드 없음 (즉시 완료)", MessageType.None);
            }
            else
            {
                string status = $"조건 충족: {metCount}/{totalConditions}";
                var msgType = metCount == totalConditions ? MessageType.Info : MessageType.Warning;
                EditorGUILayout.HelpBox(status, msgType);
            }

            if (GUILayout.Button("현재 Step 강제 종료"))
            {
                ((Step)target).EndTrigger();
            }

            Repaint();
        }

        private void DrawAddNodeButtons(Step step)
        {
            EditorGUILayout.LabelField("노드 추가", EditorStyles.boldLabel);

            if (GUILayout.Button("+ SoundNode"))
                CreateNodeChild<SoundNode>(step.transform, "SoundNode");

            if (GUILayout.Button("+ TransformNode"))
                CreateNodeChild<TransformNode>(step.transform, "TransformNode");

            if (GUILayout.Button("+ TriggerCondition"))
                CreateNodeChild<TriggerConditionNode>(step.transform, "TriggerConditionNode");

            if (GUILayout.Button("+ TimerCondition"))
                CreateNodeChild<TimerConditionNode>(step.transform, "TimerConditionNode");

            if (GUILayout.Button("+ UINode"))
                CreateNodeChild<UINode>(step.transform, "UINode");
        }

        private void CreateNodeChild<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            go.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(go, $"Add {name}");
        }
    }
}
