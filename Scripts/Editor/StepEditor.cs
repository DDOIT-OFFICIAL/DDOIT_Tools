using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(Step))]
    public class StepEditor : UnityEditor.Editor
    {
        private SerializedProperty _skip;
        private SerializedProperty _conditionGroupCount;
        private SerializedProperty _onStart;
        private SerializedProperty _onEnd;

        private void OnEnable()
        {
            _skip = serializedObject.FindProperty("_skip");
            _conditionGroupCount = serializedObject.FindProperty("_conditionGroupCount");
            _onStart = serializedObject.FindProperty("_onStart");
            _onEnd = serializedObject.FindProperty("_onEnd");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_skip, new GUIContent("건너뛰기"));
            EditorGUILayout.Space(4);

            // 조건 그룹 수
            DrawConditionGroupCount();
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(_onStart, new GUIContent("Step 시작 이벤트"));
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("Step 종료 이벤트"));

            var step = (Step)target;
            var nodes = step.GetComponentsInChildren<ScenarioNode>(true);

            if (nodes.Length > 0)
            {
                EditorGUILayout.Space();
                DrawNodeListByGroup(nodes, _conditionGroupCount.intValue);
            }

            if (Application.isPlaying && step.IsActive)
            {
                EditorGUILayout.Space();
                DrawRuntimeStatus(nodes, _conditionGroupCount.intValue);
            }

            EditorGUILayout.Space();
            DrawAddNodeButtons(step);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawConditionGroupCount()
        {
            EditorGUILayout.LabelField("조건 그룹", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"그룹 수: {_conditionGroupCount.intValue}", GUILayout.Width(80));

            if (GUILayout.Button("+", GUILayout.Width(24)))
                _conditionGroupCount.intValue++;

            EditorGUI.BeginDisabledGroup(_conditionGroupCount.intValue <= 1);
            if (GUILayout.Button("-", GUILayout.Width(24)))
                _conditionGroupCount.intValue--;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (_conditionGroupCount.intValue > 1)
            {
                EditorGUILayout.HelpBox(
                    "그룹 간 OR: 아무 그룹이든 전원 충족 시 Step 종료\n" +
                    "그룹 내 AND: 같은 그룹의 모든 노드가 충족되어야 그룹 완료",
                    MessageType.None);
            }
        }

        private void DrawNodeListByGroup(ScenarioNode[] nodes, int groupCount)
        {
            // 그룹별 분류
            var grouped = new Dictionary<int, List<ScenarioNode>>();
            var ungrouped = new List<ScenarioNode>();

            foreach (var node in nodes)
            {
                int g = node.ConditionGroup;
                if (g <= 0)
                {
                    ungrouped.Add(node);
                }
                else
                {
                    if (!grouped.ContainsKey(g))
                        grouped[g] = new List<ScenarioNode>();
                    grouped[g].Add(node);
                }
            }

            EditorGUILayout.LabelField($"노드 목록 ({nodes.Length}개)", EditorStyles.boldLabel);

            // 그룹별 표시
            for (int g = 1; g <= groupCount; g++)
            {
                var groupColor = GetGroupColor(g);

                EditorGUILayout.BeginVertical("box");

                // 그룹 헤더
                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = groupColor;
                int count = grouped.ContainsKey(g) ? grouped[g].Count : 0;
                EditorGUILayout.LabelField($"조건 그룹 {g} ({count}개)", EditorStyles.boldLabel);
                GUI.backgroundColor = prevBg;

                if (grouped.ContainsKey(g))
                {
                    foreach (var node in grouped[g])
                        DrawNodeRow(node, groupColor);
                }
                else
                {
                    EditorGUILayout.LabelField("  (노드 없음)", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }

            // 비조건 노드
            if (ungrouped.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"비조건 ({ungrouped.Count}개)", EditorStyles.boldLabel);
                foreach (var node in ungrouped)
                    DrawNodeRow(node, Color.gray);
                EditorGUILayout.EndVertical();
            }

            // 범위 초과 경고
            foreach (var kvp in grouped)
            {
                if (kvp.Key > groupCount)
                {
                    EditorGUILayout.HelpBox(
                        $"그룹 {kvp.Key}에 속한 노드가 있지만 그룹 수({groupCount})를 초과합니다.",
                        MessageType.Warning);
                }
            }
        }

        private static void DrawNodeRow(ScenarioNode node, Color groupColor)
        {
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
            else if (node.IsStepCondition)
            {
                var prevColor = GUI.contentColor;
                GUI.contentColor = groupColor;
                EditorGUILayout.LabelField("●", GUILayout.Width(14));
                GUI.contentColor = prevColor;
            }
            else
            {
                EditorGUILayout.LabelField("—", GUILayout.Width(14));
            }

            // 노드 이름
            if (GUILayout.Button(node.gameObject.name, EditorStyles.label))
            {
                Selection.activeGameObject = node.gameObject;
                EditorGUIUtility.PingObject(node.gameObject);
            }

            // 타입
            var prevContentColor = GUI.contentColor;
            GUI.contentColor = new Color(0.7f, 0.8f, 1f);
            EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel, GUILayout.Width(130));
            GUI.contentColor = prevContentColor;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRuntimeStatus(ScenarioNode[] nodes, int groupCount)
        {
            EditorGUILayout.LabelField("런타임 상태", EditorStyles.boldLabel);

            // 그룹별 분류
            var grouped = new Dictionary<int, List<ScenarioNode>>();
            foreach (var node in nodes)
            {
                int g = node.ConditionGroup;
                if (g <= 0) continue;
                if (!grouped.ContainsKey(g))
                    grouped[g] = new List<ScenarioNode>();
                grouped[g].Add(node);
            }

            if (grouped.Count == 0)
            {
                EditorGUILayout.HelpBox("조건 노드 없음 (외부에서 EndTrigger 호출 필요)", MessageType.None);
            }
            else
            {
                for (int g = 1; g <= groupCount; g++)
                {
                    if (!grouped.ContainsKey(g)) continue;

                    int met = 0;
                    int total = grouped[g].Count;
                    foreach (var node in grouped[g])
                    {
                        if (node.IsConditionMet) met++;
                    }

                    var groupColor = GetGroupColor(g);
                    string status = met == total ? "완료" : $"{met}/{total}";
                    var msgType = met == total ? MessageType.Info : MessageType.Warning;
                    EditorGUILayout.HelpBox($"그룹 {g}: {status}", msgType);
                }
            }

            if (GUILayout.Button("현재 Step 강제 종료"))
                ((Step)target).EndTrigger();

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

            if (GUILayout.Button("+ ToggleNode"))
                CreateNodeChild<ToggleNode>(step.transform, "ToggleNode");

            if (GUILayout.Button("+ AnimatorNode"))
                CreateNodeChild<AnimatorNode>(step.transform, "AnimatorNode");
        }

        private void CreateNodeChild<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            go.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(go, $"Add {name}");
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
