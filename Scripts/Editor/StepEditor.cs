using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario;
using ScenarioCls = DDOIT.Tools.Scenario.Scenario;
using DDOIT.Tools.Scenario.Nodes;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(Step))]
    public class StepEditor : UnityEditor.Editor
    {
        private SerializedProperty _skip;
        private SerializedProperty _memo;
        private SerializedProperty _conditionGroupCount;
        private SerializedProperty _defaultTargetStep;
        private SerializedProperty _defaultTargetScenario;
        private SerializedProperty _groupTargetSteps;
        private SerializedProperty _groupTargetScenarios;
        private SerializedProperty _onStart;
        private SerializedProperty _onEnd;

        // 드롭다운 캐시
        private Step[] _siblingSteps;
        private ScenarioCls[] _allScenarios;
        private string[] _targetNames;
        private int _stepOffset;
        private int _scenarioOffset;

        private void OnEnable()
        {
            _skip = serializedObject.FindProperty("_skip");
            _memo = serializedObject.FindProperty("_memo");
            _conditionGroupCount = serializedObject.FindProperty("_conditionGroupCount");
            _defaultTargetStep = serializedObject.FindProperty("_defaultTargetStep");
            _defaultTargetScenario = serializedObject.FindProperty("_defaultTargetScenario");
            _groupTargetSteps = serializedObject.FindProperty("_groupTargetSteps");
            _groupTargetScenarios = serializedObject.FindProperty("_groupTargetScenarios");
            _onStart = serializedObject.FindProperty("_onStart");
            _onEnd = serializedObject.FindProperty("_onEnd");

            RefreshTargetCache();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_skip, new GUIContent("건너뛰기"));
            EditorGUILayout.PropertyField(_memo, new GUIContent("메모"));
            EditorGUILayout.Space(4);

            DrawConditionGroupSection();
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

        #region Condition Group Section

        private void DrawConditionGroupSection()
        {
            EditorGUILayout.LabelField("조건 그룹", EditorStyles.boldLabel);

            // 그룹 수 +/-
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"그룹 수: {_conditionGroupCount.intValue} / {Step.MAX_CONDITION_GROUPS}", GUILayout.Width(110));

            EditorGUI.BeginDisabledGroup(_conditionGroupCount.intValue >= Step.MAX_CONDITION_GROUPS);
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                _conditionGroupCount.intValue++;
                SyncArraySizes();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_conditionGroupCount.intValue <= 0);
            if (GUILayout.Button("-", GUILayout.Width(24)))
            {
                _conditionGroupCount.intValue--;
                SyncArraySizes();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            int groupCount = _conditionGroupCount.intValue;

            // 조건 없음
            if (groupCount == 0)
            {
                EditorGUILayout.HelpBox("조건 그룹 없음 → Step.EndTrigger() 호출 전까지 대기", MessageType.Warning);
                DrawTargetDropdown("완료 시 이동", _defaultTargetStep, _defaultTargetScenario);
            }
            else
            {
                if (groupCount > 1)
                {
                    EditorGUILayout.HelpBox(
                        "그룹 간 OR: 아무 그룹이든 전원 충족 시 Step 종료\n" +
                        "그룹 내 AND: 같은 그룹의 모든 노드가 충족되어야 그룹 완료",
                        MessageType.None);
                }

                SyncArraySizes();

                for (int i = 0; i < groupCount; i++)
                {
                    var groupColor = GetGroupColor(i + 1);
                    var prevBg = GUI.backgroundColor;
                    GUI.backgroundColor = groupColor;

                    var stepElement = i < _groupTargetSteps.arraySize
                        ? _groupTargetSteps.GetArrayElementAtIndex(i) : null;
                    var scenarioElement = i < _groupTargetScenarios.arraySize
                        ? _groupTargetScenarios.GetArrayElementAtIndex(i) : null;

                    if (stepElement != null && scenarioElement != null)
                        DrawTargetDropdown($"그룹 {i + 1} 완료 시 이동", stepElement, scenarioElement);

                    GUI.backgroundColor = prevBg;
                }
            }
        }

        private void DrawTargetDropdown(string label, SerializedProperty stepProp, SerializedProperty scenarioProp)
        {
            if (_targetNames == null) RefreshTargetCache();

            // 현재 선택 인덱스 찾기
            int currentIndex = 0;
            var currentStep = stepProp.objectReferenceValue as Step;
            var currentScenario = scenarioProp.objectReferenceValue as ScenarioCls;

            if (currentStep != null)
            {
                for (int i = 0; i < _siblingSteps.Length; i++)
                {
                    if (_siblingSteps[i] == currentStep)
                    {
                        currentIndex = _stepOffset + i;
                        break;
                    }
                }
            }
            else if (currentScenario != null)
            {
                for (int i = 0; i < _allScenarios.Length; i++)
                {
                    if (_allScenarios[i] == currentScenario)
                    {
                        currentIndex = _scenarioOffset + i;
                        break;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup(label, currentIndex, _targetNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (selected == 0)
                {
                    // 다음 스텝
                    stepProp.objectReferenceValue = null;
                    scenarioProp.objectReferenceValue = null;
                }
                else if (selected >= _stepOffset && selected < _stepOffset + _siblingSteps.Length)
                {
                    // Step 선택
                    stepProp.objectReferenceValue = _siblingSteps[selected - _stepOffset];
                    scenarioProp.objectReferenceValue = null;
                }
                else if (selected >= _scenarioOffset && selected < _scenarioOffset + _allScenarios.Length)
                {
                    // Scenario 선택
                    stepProp.objectReferenceValue = null;
                    scenarioProp.objectReferenceValue = _allScenarios[selected - _scenarioOffset];
                }
            }
        }

        private void SyncArraySizes()
        {
            int count = _conditionGroupCount.intValue;

            while (_groupTargetSteps.arraySize < count)
                _groupTargetSteps.InsertArrayElementAtIndex(_groupTargetSteps.arraySize);
            while (_groupTargetSteps.arraySize > count)
                _groupTargetSteps.DeleteArrayElementAtIndex(_groupTargetSteps.arraySize - 1);

            while (_groupTargetScenarios.arraySize < count)
                _groupTargetScenarios.InsertArrayElementAtIndex(_groupTargetScenarios.arraySize);
            while (_groupTargetScenarios.arraySize > count)
                _groupTargetScenarios.DeleteArrayElementAtIndex(_groupTargetScenarios.arraySize - 1);
        }

        private void RefreshTargetCache()
        {
            var step = (Step)target;
            var scenario = step.GetComponentInParent<ScenarioCls>();

            // 형제 Step
            if (scenario != null)
                _siblingSteps = scenario.GetComponentsInChildren<Step>(true).Where(s => s != step).ToArray();
            else
                _siblingSteps = new Step[0];

            // 모든 Scenario (ScenarioManager 하위)
            var scenarioManager = step.GetComponentInParent<ScenarioManager>();
            if (scenarioManager != null)
                _allScenarios = scenarioManager.GetComponentsInChildren<ScenarioCls>(true);
            else if (scenario != null)
                _allScenarios = new[] { scenario };
            else
                _allScenarios = new ScenarioCls[0];

            // 드롭다운 이름 배열 구성
            // [다음 스텝] [── Step ──] [Step_01] ... [── Scenario ──] [Scenario_01] ...
            var names = new List<string>();
            names.Add("다음 스텝");

            _stepOffset = 1;
            if (_siblingSteps.Length > 0)
            {
                _stepOffset = names.Count;
                for (int i = 0; i < _siblingSteps.Length; i++)
                    names.Add($"  Step: {_siblingSteps[i].gameObject.name}");
            }

            _scenarioOffset = names.Count;
            if (_allScenarios.Length > 0)
            {
                for (int i = 0; i < _allScenarios.Length; i++)
                    names.Add($"  Scenario: {_allScenarios[i].gameObject.name}");
            }

            _targetNames = names.ToArray();
        }

        #endregion

        #region Node List

        private void DrawNodeListByGroup(ScenarioNode[] nodes, int groupCount)
        {
            var grouped = new Dictionary<int, List<ScenarioNode>>();
            var ungrouped = new List<ScenarioNode>();

            foreach (var node in nodes)
            {
                int g = node.ConditionGroup;
                if (g <= 0)
                    ungrouped.Add(node);
                else
                {
                    if (!grouped.ContainsKey(g))
                        grouped[g] = new List<ScenarioNode>();
                    grouped[g].Add(node);
                }
            }

            // 외부 marker callsite 수집 (Scene scan)
            var externalMarkers = ((Step)target).CollectExternalMarkerCallsites();
            var externalByGroup = new Dictionary<int, List<(DDOIT.Tools.Scenario.Nodes.UINode node, int buttonIndex, int group)>>();
            foreach (var ext in externalMarkers)
            {
                if (!externalByGroup.ContainsKey(ext.group))
                    externalByGroup[ext.group] = new List<(DDOIT.Tools.Scenario.Nodes.UINode, int, int)>();
                externalByGroup[ext.group].Add(ext);
            }

            EditorGUILayout.LabelField($"노드 목록 ({nodes.Length}개)", EditorStyles.boldLabel);

            for (int g = 1; g <= groupCount; g++)
            {
                var groupColor = GetGroupColor(g);

                EditorGUILayout.BeginVertical("box");

                var prevBg = GUI.backgroundColor;
                GUI.backgroundColor = groupColor;
                int nodeCount = grouped.ContainsKey(g) ? grouped[g].Count : 0;
                int extCount = externalByGroup.ContainsKey(g) ? externalByGroup[g].Count : 0;
                string title = extCount > 0
                    ? $"조건 그룹 {g} ({nodeCount}개 + 외부 {extCount}개)"
                    : $"조건 그룹 {g} ({nodeCount}개)";
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUI.backgroundColor = prevBg;

                if (grouped.ContainsKey(g))
                {
                    foreach (var node in grouped[g])
                        DrawNodeRow(node, groupColor);
                }

                if (externalByGroup.ContainsKey(g))
                {
                    foreach (var ext in externalByGroup[g])
                        DrawExternalMarkerRow(ext.node, ext.buttonIndex, groupColor);
                }

                if (!grouped.ContainsKey(g) && !externalByGroup.ContainsKey(g))
                {
                    EditorGUILayout.LabelField("  (노드/외부 marker 없음)", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();
            }

            if (ungrouped.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"비조건 ({ungrouped.Count}개)", EditorStyles.boldLabel);
                foreach (var node in ungrouped)
                    DrawNodeRow(node, Color.gray);
                EditorGUILayout.EndVertical();
            }

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

        private static void DrawExternalMarkerRow(DDOIT.Tools.Scenario.Nodes.UINode node, int buttonIndex, Color groupColor)
        {
            EditorGUILayout.BeginHorizontal();

            var prevColor = GUI.contentColor;
            GUI.contentColor = groupColor;
            EditorGUILayout.LabelField("◆", GUILayout.Width(14));
            GUI.contentColor = prevColor;

            string buttonLabel = buttonIndex == 0 ? "버튼 A" : "버튼 B";
            if (GUILayout.Button($"{node.gameObject.name} ▸ {buttonLabel}", EditorStyles.label))
            {
                Selection.activeGameObject = node.gameObject;
                EditorGUIUtility.PingObject(node.gameObject);
            }

            var prevContentColor = GUI.contentColor;
            GUI.contentColor = new Color(0.9f, 0.7f, 0.3f);
            EditorGUILayout.LabelField("UINode (외부)", EditorStyles.miniLabel, GUILayout.Width(130));
            GUI.contentColor = prevContentColor;

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawNodeRow(ScenarioNode node, Color groupColor)
        {
            string typeName = node.GetType().Name;

            EditorGUILayout.BeginHorizontal();

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

            if (GUILayout.Button(node.gameObject.name, EditorStyles.label))
            {
                Selection.activeGameObject = node.gameObject;
                EditorGUIUtility.PingObject(node.gameObject);
            }

            var prevContentColor = GUI.contentColor;
            GUI.contentColor = new Color(0.7f, 0.8f, 1f);
            EditorGUILayout.LabelField(typeName, EditorStyles.miniLabel, GUILayout.Width(130));
            GUI.contentColor = prevContentColor;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Runtime

        private void DrawRuntimeStatus(ScenarioNode[] nodes, int groupCount)
        {
            EditorGUILayout.LabelField("런타임 상태", EditorStyles.boldLabel);

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
                EditorGUILayout.HelpBox("조건 없음 → Step.EndTrigger() 호출 전까지 대기", MessageType.Warning);
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

                    string status = met == total ? "완료" : $"{met}/{total}";
                    var msgType = met == total ? MessageType.Info : MessageType.Warning;
                    EditorGUILayout.HelpBox($"그룹 {g}: {status}", msgType);
                }
            }

            if (GUILayout.Button("현재 Step 강제 종료"))
                ((Step)target).EndTrigger();

            Repaint();
        }

        #endregion

        #region Add Nodes

        private static System.Type[] _cachedNodeTypes;

        private static System.Type[] GetScenarioNodeTypes()
        {
            if (_cachedNodeTypes != null) return _cachedNodeTypes;
            _cachedNodeTypes = TypeCache.GetTypesDerivedFrom<ScenarioNode>()
                .Where(t => !t.IsAbstract && !t.IsGenericType)
                .OrderBy(t => t.Name)
                .ToArray();
            return _cachedNodeTypes;
        }

        private void DrawAddNodeButtons(Step step)
        {
            EditorGUILayout.LabelField("노드 추가", EditorStyles.boldLabel);

            foreach (var type in GetScenarioNodeTypes())
            {
                if (GUILayout.Button($"+ {type.Name}"))
                    CreateNodeChild(step.transform, type);
            }
        }

        private void CreateNodeChild(Transform parent, System.Type componentType)
        {
            var go = new GameObject(componentType.Name);
            go.AddComponent(componentType);
            go.transform.SetParent(parent);
            Undo.RegisterCreatedObjectUndo(go, $"Add {componentType.Name}");
        }

        #endregion

        #region Utility

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

        #endregion
    }
}
