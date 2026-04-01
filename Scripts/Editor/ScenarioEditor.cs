using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(Scenario))]
    public class ScenarioEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var scenario = (Scenario)target;
            var steps = scenario.GetComponentsInChildren<Step>(true);

            if (steps.Length > 0)
            {
                EditorGUILayout.Space();
                DrawStepList(steps);
            }

            if (Application.isPlaying && scenario.IsActive)
            {
                EditorGUILayout.Space();
                DrawRuntimeStatus(steps);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("+ Step 추가"))
            {
                int nextNum = GetNextNumber(scenario.transform, "Step");
                var go = new GameObject($"Step_{nextNum:D2}");
                go.AddComponent<Step>();
                go.transform.SetParent(scenario.transform);
                Undo.RegisterCreatedObjectUndo(go, "Add Step");
            }
        }

        private static int GetNextNumber(Transform parent, string prefix)
        {
            int maxNumber = 0;
            var regex = new Regex($"^{prefix}_(\\d+)$");

            for (int i = 0; i < parent.childCount; i++)
            {
                var match = regex.Match(parent.GetChild(i).name);
                if (match.Success)
                {
                    int num = int.Parse(match.Groups[1].Value);
                    if (num > maxNumber) maxNumber = num;
                }
            }

            return maxNumber + 1;
        }

        private void DrawStepList(Step[] steps)
        {
            EditorGUILayout.LabelField($"Step 흐름 ({steps.Length}개)", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                var nodes = step.GetComponentsInChildren<ScenarioNode>(true);

                // 조건 노드 개수 계산
                int conditionCount = 0;
                foreach (var node in nodes)
                {
                    if (node.IsStepCondition) conditionCount++;
                }

                EditorGUILayout.BeginHorizontal();

                // 활성 표시 (플레이 모드)
                if (Application.isPlaying && step.IsActive)
                {
                    var prevColor = GUI.contentColor;
                    GUI.contentColor = Color.green;
                    EditorGUILayout.LabelField("▶", GUILayout.Width(14));
                    GUI.contentColor = prevColor;
                }
                else
                {
                    EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(14));
                }

                // Skip 상태에 따른 이름 표시
                var prevContentColor = GUI.contentColor;
                if (step.Skip)
                    GUI.contentColor = Color.gray;

                string stepName = step.Skip
                    ? $"{step.gameObject.name} [SKIP]"
                    : step.gameObject.name;

                if (GUILayout.Button(stepName, EditorStyles.label))
                {
                    Selection.activeGameObject = step.gameObject;
                    EditorGUIUtility.PingObject(step.gameObject);
                }

                GUI.contentColor = prevContentColor;

                // Node / 조건 개수 표시
                if (!step.Skip)
                {
                    prevContentColor = GUI.contentColor;
                    GUI.contentColor = nodes.Length > 0 ? new Color(0.5f, 0.9f, 1f) : Color.gray;
                    string nodeInfo = conditionCount > 0
                        ? $"Node {nodes.Length} / 조건 {conditionCount}"
                        : $"Node {nodes.Length}개";
                    EditorGUILayout.LabelField(nodeInfo, EditorStyles.miniLabel, GUILayout.Width(120));
                    GUI.contentColor = prevContentColor;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeStatus(Step[] steps)
        {
            EditorGUILayout.LabelField("런타임 상태", EditorStyles.boldLabel);

            Step activeStep = null;
            int stepIndex = -1;
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i].IsActive)
                {
                    activeStep = steps[i];
                    stepIndex = i;
                    break;
                }
            }

            if (activeStep == null)
            {
                EditorGUILayout.HelpBox("대기 중", MessageType.None);
                return;
            }

            var activeNodes = activeStep.GetComponentsInChildren<ScenarioNode>(true);
            int metCount = 0;
            int totalConditions = 0;
            foreach (var node in activeNodes)
            {
                if (node.IsStepCondition)
                {
                    totalConditions++;
                    if (node.IsConditionMet) metCount++;
                }
            }

            string status = $"▶ {activeStep.gameObject.name} ({stepIndex + 1}/{steps.Length})  |  조건 {metCount}/{totalConditions}";

            EditorGUILayout.HelpBox(status, MessageType.Info);

            if (GUILayout.Button("현재 Step 스킵"))
            {
                activeStep.EndTrigger();
            }

            Repaint();
        }
    }
}
