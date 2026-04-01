using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(ScenarioManager))]
    public class ScenarioManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var manager = (ScenarioManager)target;
            var entryProp = serializedObject.FindProperty("_entryScenario");

            // Entry Scenario 경고
            if (entryProp.objectReferenceValue == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Entry Scenario가 지정되지 않았습니다.", MessageType.Warning);
            }

            var scenarios = manager.GetComponentsInChildren<Scenario>(true);

            if (scenarios.Length > 0)
            {
                EditorGUILayout.Space();
                DrawFlowPreview(entryProp.objectReferenceValue as Scenario);

                EditorGUILayout.Space();
                DrawScenarioList(scenarios);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                DrawRuntimeStatus(scenarios);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("+ 시나리오 추가"))
            {
                int nextNum = GetNextNumber(manager.transform, "Scenario");
                var go = new GameObject($"Scenario_{nextNum:D2}");
                go.AddComponent<Scenario>();
                go.transform.SetParent(manager.transform);
                Undo.RegisterCreatedObjectUndo(go, "Add Scenario");
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

        private void DrawFlowPreview(Scenario entry)
        {
            EditorGUILayout.LabelField("흐름 미리보기", EditorStyles.boldLabel);

            if (entry == null)
            {
                EditorGUILayout.LabelField("  (Entry Scenario 미지정)", EditorStyles.miniLabel);
                return;
            }

            var visited = new HashSet<Scenario>();
            var flow = new System.Text.StringBuilder();
            var current = entry;

            while (current != null && !visited.Contains(current))
            {
                visited.Add(current);
                if (flow.Length > 0) flow.Append("  →  ");
                flow.Append(current.gameObject.name);

                using (var so = new SerializedObject(current))
                {
                    var nextProp = so.FindProperty("_nextScenario");
                    current = nextProp.objectReferenceValue as Scenario;
                }
            }

            if (current != null && visited.Contains(current))
                flow.Append("  →  ⟳ " + current.gameObject.name + " (순환)");
            else
                flow.Append("  →  (종료)");

            EditorGUILayout.HelpBox(flow.ToString(), MessageType.None);
        }

        private void DrawScenarioList(Scenario[] scenarios)
        {
            EditorGUILayout.LabelField($"시나리오 목록 ({scenarios.Length}개)", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            foreach (var scenario in scenarios)
            {
                using var so = new SerializedObject(scenario);
                var nextProp = so.FindProperty("_nextScenario");
                var nextScenario = nextProp.objectReferenceValue as Scenario;
                string nextName = nextScenario != null ? nextScenario.gameObject.name : "(없음)";

                EditorGUILayout.BeginHorizontal();

                // 활성 표시 (플레이 모드)
                if (Application.isPlaying && scenario.IsActive)
                {
                    var prevColor = GUI.contentColor;
                    GUI.contentColor = Color.green;
                    EditorGUILayout.LabelField("▶", GUILayout.Width(14));
                    GUI.contentColor = prevColor;
                }
                else
                {
                    EditorGUILayout.LabelField("○", GUILayout.Width(14));
                }

                // 시나리오 이름 (클릭하면 해당 오브젝트 선택)
                if (GUILayout.Button(scenario.gameObject.name, EditorStyles.label))
                {
                    Selection.activeGameObject = scenario.gameObject;
                    EditorGUIUtility.PingObject(scenario.gameObject);
                }

                // 다음 시나리오 표시
                var prevContentColor = GUI.contentColor;
                GUI.contentColor = nextScenario != null ? new Color(0.5f, 0.9f, 1f) : Color.gray;
                EditorGUILayout.LabelField($"→ {nextName}", EditorStyles.miniLabel, GUILayout.Width(160));
                GUI.contentColor = prevContentColor;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRuntimeStatus(Scenario[] scenarios)
        {
            EditorGUILayout.LabelField("런타임 상태", EditorStyles.boldLabel);

            Scenario activeScenario = null;
            foreach (var s in scenarios)
            {
                if (s.IsActive) { activeScenario = s; break; }
            }

            if (activeScenario == null)
            {
                EditorGUILayout.HelpBox("대기 중", MessageType.None);
                return;
            }

            // 활성 Step 찾기
            var steps = activeScenario.GetComponentsInChildren<Step>(true);
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

            string status = $"▶ {activeScenario.gameObject.name}";
            if (activeStep != null)
                status += $"  >  {activeStep.gameObject.name} ({stepIndex + 1}/{steps.Length})";

            EditorGUILayout.HelpBox(status, MessageType.Info);

            if (GUILayout.Button("현재 시나리오 스킵"))
            {
                activeScenario.EndTrigger();
            }

            // 실시간 갱신
            Repaint();
        }
    }
}
