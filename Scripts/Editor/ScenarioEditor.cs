using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario;
using ScenarioCls = DDOIT.Tools.Scenario.Scenario;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(ScenarioCls))]
    public class ScenarioEditor : UnityEditor.Editor
    {
        private SerializedProperty _nextScenario;
        private SerializedProperty _onStart;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _nextScenario = serializedObject.FindProperty("_nextScenario");
            _onStart = serializedObject.FindProperty("_onStart");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_nextScenario, new GUIContent("다음 시나리오"));
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onStart, new GUIContent("시나리오 시작 이벤트"));
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("시나리오 종료 이벤트"));

            serializedObject.ApplyModifiedProperties();

            var scenario = (ScenarioCls)target;
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

                int conditionCount = 0;
                int disabledCount = 0;
                foreach (var node in nodes)
                {
                    if (node.IsExecutionDisabled)
                        disabledCount++;

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
                    string nodeInfo = BuildNodeInfo(nodes.Length, conditionCount, disabledCount);
                    EditorGUILayout.LabelField(nodeInfo, EditorStyles.miniLabel, GUILayout.Width(150));
                    GUI.contentColor = prevContentColor;
                }

                EditorGUILayout.EndHorizontal();

                // 메모 표시
                using (var stepSo = new SerializedObject(step))
                {
                    string memo = stepSo.FindProperty("_memo").stringValue;
                    if (!string.IsNullOrEmpty(memo))
                    {
                        var prevColor = GUI.contentColor;
                        GUI.contentColor = new Color(0.8f, 0.8f, 0.5f);
                        EditorGUILayout.LabelField($"  \"{memo}\"", EditorStyles.miniLabel);
                        GUI.contentColor = prevColor;
                    }
                }

                // 분기 정보 표시
                if (!step.Skip)
                    DrawStepBranchInfo(step, i, steps);
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawStepBranchInfo(Step step, int stepIndex, Step[] allSteps)
        {
            using var so = new SerializedObject(step);
            int groupCount = so.FindProperty("_conditionGroupCount").intValue;

            if (groupCount == 0)
            {
                // 조건 없음 → 기본 타겟
                var defaultStep = so.FindProperty("_defaultTargetStep").objectReferenceValue as Step;
                var defaultScenario = so.FindProperty("_defaultTargetScenario").objectReferenceValue as ScenarioCls;

                if (defaultStep != null || defaultScenario != null)
                {
                    string targetName = defaultScenario != null
                        ? $"Scenario: {defaultScenario.gameObject.name}"
                        : $"Step: {defaultStep.gameObject.name}";
                    DrawBranchArrow($"  ↳ EndTrigger → {targetName}", new Color(0.6f, 0.6f, 0.6f));
                }
            }
            else
            {
                var groupSteps = so.FindProperty("_groupTargetSteps");
                var groupScenarios = so.FindProperty("_groupTargetScenarios");

                for (int g = 0; g < groupCount; g++)
                {
                    Step targetStep = g < groupSteps.arraySize
                        ? groupSteps.GetArrayElementAtIndex(g).objectReferenceValue as Step : null;
                    ScenarioCls targetScenario = g < groupScenarios.arraySize
                        ? groupScenarios.GetArrayElementAtIndex(g).objectReferenceValue as ScenarioCls : null;

                    if (targetStep == null && targetScenario == null) continue;

                    var groupColor = GetGroupColor(g + 1);
                    string targetName = targetScenario != null
                        ? $"Scenario: {targetScenario.gameObject.name}"
                        : $"Step: {targetStep.gameObject.name}";

                    DrawBranchArrow($"  ↳ 그룹{g + 1} → {targetName}", groupColor);
                }
            }
        }

        private static string BuildNodeInfo(int nodeCount, int conditionCount, int disabledCount)
        {
            var parts = new List<string> { $"Node {nodeCount}" };

            if (disabledCount > 0)
                parts.Add($"제외 {disabledCount}");

            if (conditionCount > 0)
                parts.Add($"조건 {conditionCount}");

            return string.Join(" / ", parts);
        }

        private static void DrawBranchArrow(string text, Color color)
        {
            var prevColor = GUI.contentColor;
            GUI.contentColor = color;
            EditorGUILayout.LabelField(text, EditorStyles.miniLabel);
            GUI.contentColor = prevColor;
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

        private static Color GetGroupColor(int group)
        {
            return group switch
            {
                1 => new Color(0.3f, 0.7f, 1f),
                2 => new Color(1f, 0.6f, 0.3f),
                3 => new Color(0.5f, 1f, 0.5f),
                4 => new Color(1f, 0.5f, 1f),
                5 => new Color(1f, 1f, 0.4f),
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
        }
    }
}
