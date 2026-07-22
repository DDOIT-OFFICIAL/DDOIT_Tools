using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools.Scenario
{
    /// <summary>
    /// 하위 Node들을 관할하는 단계.
    /// 조건 그룹 시스템: 노드는 1~N 그룹에 소속되며,
    /// 그룹 내 모든 노드가 충족되면(AND) 해당 그룹이 완료된다.
    /// 그룹 중 하나라도 완료되면(OR) Step이 종료된다.
    /// 각 그룹은 완료 시 이동할 Step 또는 Scenario를 지정할 수 있다.
    /// 조건 그룹이 0개이면 Step.EndTrigger()가 호출될 때까지 대기한다.
    /// </summary>
    public class Step : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private bool _skip;
        [SerializeField] private string _memo;
        [SerializeField] private int _conditionGroupCount;

        [SerializeField] private Step _defaultTargetStep;
        [SerializeField] private Scenario _defaultTargetScenario;
        [SerializeField] private Step[] _groupTargetSteps;
        [SerializeField] private Scenario[] _groupTargetScenarios;

        [SerializeField] private UnityEvent _onStart;
        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }
        public bool Skip => _skip;
        public int ConditionGroupCount => _conditionGroupCount;

        #endregion

        #region Private Fields

        private Scenario _parentScenario;
        private ScenarioNode[] _nodes;
        private ScenarioNode[] _runtimeNodes;
        private Dictionary<int, List<ScenarioNode>> _conditionGroups;
        private int _completedGroupIndex;
        private bool _isInitializing;
        private bool _endRequestedDuringInitialization;

        // 외부 marker (UINode 버튼 조건 또는 legacy UnityEvent에서 MarkConditionGroupN 호출)
        private HashSet<int> _expectedExternalMarkers = new HashSet<int>();
        private HashSet<int> _receivedExternalMarkers = new HashSet<int>();

        public const int MAX_CONDITION_GROUPS = 7;

        #endregion

        #region Public Methods

        public void StartTrigger()
        {
            gameObject.SetActive(true);
            IsActive = true;
            _completedGroupIndex = -1;
            _isInitializing = false;
            _endRequestedDuringInitialization = false;

            _parentScenario = GetComponentInParent<Scenario>();
            _nodes = GetComponentsInChildren<ScenarioNode>(true);
            var runtimeNodes = new List<ScenarioNode>();

            // 그룹별 조건 노드 수집
            _conditionGroups = new Dictionary<int, List<ScenarioNode>>();
            foreach (var node in _nodes)
            {
                if (node.IsExecutionDisabled)
                    continue;

                runtimeNodes.Add(node);

                if (node is DDOIT.Tools.Scenario.Nodes.UINode)
                    continue;

                if (!node.IsStepCondition)
                    continue;

                int group = node.ConditionGroup;

                if (!_conditionGroups.ContainsKey(group))
                    _conditionGroups[group] = new List<ScenarioNode>();

                _conditionGroups[group].Add(node);
            }
            _runtimeNodes = runtimeNodes.ToArray();

            // 외부 marker expected 수집 (UINode 버튼 조건 + legacy UnityEvent → MarkConditionGroupN)
            _expectedExternalMarkers.Clear();
            _receivedExternalMarkers.Clear();
            DetectExternalMarkers();

            if (ScenarioManager.DebugMode)
            {
                int totalConditions = 0;
                foreach (var g in _conditionGroups.Values)
                    totalConditions += g.Count;
                int disabledNodes = _nodes.Length - _runtimeNodes.Length;
                Debug.Log($"[Step] '{gameObject.name}' 시작 (Node {_runtimeNodes.Length}/{_nodes.Length}개, 실행 제외 {disabledNodes}개, 조건 그룹 {_conditionGroups.Count}개, 조건 노드 {totalConditions}개, 외부 marker {_expectedExternalMarkers.Count}개)");
            }

            _isInitializing = true;
            try
            {
                _onStart?.Invoke();

                foreach (var node in _runtimeNodes)
                {
                    node.gameObject.SetActive(true);
                    node.Init();
                }
            }
            finally
            {
                _isInitializing = false;
            }

            if (_endRequestedDuringInitialization)
            {
                EndTrigger();
                return;
            }

            // 조건 노드도 외부 marker도 없으면 자동 진행하지 않는다.
            if (_conditionGroups.Count == 0 && _expectedExternalMarkers.Count == 0)
            {
                if (ScenarioManager.DebugMode)
                    Debug.Log($"[Step] '{gameObject.name}' has no conditions and will wait until EndTrigger() is called.");
            }
        }

        /// <summary>
        /// 노드가 조건 충족을 보고할 때 호출된다.
        /// 아무 그룹이든 전원 충족되면(노드+외부 marker AND) Step을 종료한다.
        /// </summary>
        public void OnNodeConditionMet()
        {
            if (!IsActive) return;
            CheckGroupCompletion();
        }

        // ── 외부 marker 충족 메서드 (UnityEvent 등에서 호출) ──
        public void MarkConditionGroup1() => MarkConditionGroup(1);
        public void MarkConditionGroup2() => MarkConditionGroup(2);
        public void MarkConditionGroup3() => MarkConditionGroup(3);
        public void MarkConditionGroup4() => MarkConditionGroup(4);
        public void MarkConditionGroup5() => MarkConditionGroup(5);
        public void MarkConditionGroup6() => MarkConditionGroup(6);
        public void MarkConditionGroup7() => MarkConditionGroup(7);

        public void MarkConditionGroup(int group)
        {
            MarkConditionGroupMet(group);
        }

        public bool IsExternalMarkerReceived(int group)
        {
            return _receivedExternalMarkers != null && _receivedExternalMarkers.Contains(group);
        }

        private void MarkConditionGroupMet(int group)
        {
            if (!IsActive) return;
            if (group <= 0 || group > _conditionGroupCount) return;

            _receivedExternalMarkers.Add(group);

            if (ScenarioManager.DebugMode)
                Debug.Log($"[Step] '{gameObject.name}' 외부 marker 그룹 {group} 충족 보고");

            CheckGroupCompletion();
        }

        private void CheckGroupCompletion()
        {
            if (_endRequestedDuringInitialization)
                return;

            for (int g = 1; g <= _conditionGroupCount; g++)
            {
                bool hasNodes = _conditionGroups.ContainsKey(g);
                bool hasExternal = _expectedExternalMarkers.Contains(g);

                // 그룹이 비어있으면(노드 0 + 외부 marker 0) 자동 완료 방지를 위해 skip
                if (!hasNodes && !hasExternal) continue;

                // 노드 검사
                bool nodesComplete = true;
                if (hasNodes)
                {
                    foreach (var node in _conditionGroups[g])
                    {
                        if (!node.IsConditionMet) { nodesComplete = false; break; }
                    }
                }

                // 외부 marker 검사
                bool externalComplete = !hasExternal || _receivedExternalMarkers.Contains(g);

                if (nodesComplete && externalComplete)
                {
                    if (ScenarioManager.DebugMode)
                        Debug.Log($"[Step] '{gameObject.name}' 조건 그룹 {g} 완료 → 종료");
                    _completedGroupIndex = g;
                    EndTrigger();
                    return;
                }
            }
        }

        private void DetectExternalMarkers()
        {
            // Scene 내 모든 UINode를 scan하여 버튼 조건과 legacy MarkConditionGroupN callsite를 detect
            var allUINodes = UnityEngine.Object.FindObjectsByType<DDOIT.Tools.Scenario.Nodes.UINode>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var ui in allUINodes)
            {
                if (ui.IsExecutionDisabled)
                    continue;

                if (ui.GetComponentInParent<Step>(true) == this)
                {
                    AddButtonConditionMarker(ui, 0);
                    AddButtonConditionMarker(ui, 1);
                }

                if (ui.UsesButtonA)
                    ScanUnityEventForExternalMarkers(ui.OnButtonA);
                if (ui.UsesButtonB)
                    ScanUnityEventForExternalMarkers(ui.OnButtonB);
                if (ui.UsesButtonA || ui.UsesButtonB)
                    ScanUnityEventForExternalMarkers(ui.OnEnd);
            }
        }

        private void AddButtonConditionMarker(DDOIT.Tools.Scenario.Nodes.UINode ui, int buttonIndex)
        {
            int group = buttonIndex == 0 ? ui.ButtonAConditionGroup : ui.ButtonBConditionGroup;
            bool buttonEnabled = buttonIndex == 0 ? ui.UsesButtonA : ui.UsesButtonB;

            if (!buttonEnabled || group <= 0)
                return;

            _expectedExternalMarkers.Add(group);
        }

        private void ScanUnityEventForExternalMarkers(UnityEvent ev)
        {
            if (ev == null) return;
            int count = ev.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (ev.GetPersistentTarget(i) != this) continue;
                string methodName = ev.GetPersistentMethodName(i);
                for (int g = 1; g <= MAX_CONDITION_GROUPS; g++)
                {
                    if (methodName == $"MarkConditionGroup{g}")
                    {
                        _expectedExternalMarkers.Add(g);
                        break;
                    }
                }
            }
        }

        /// <summary>Step Editor에서 외부 marker 가시화용. 자기에게 호출하는 (UINode, 버튼 인덱스, 그룹) 목록 반환.</summary>
        public List<(DDOIT.Tools.Scenario.Nodes.UINode node, int buttonIndex, int group)> CollectExternalMarkerCallsites()
        {
            var result = new List<(DDOIT.Tools.Scenario.Nodes.UINode, int, int)>();
            var allUINodes = UnityEngine.Object.FindObjectsByType<DDOIT.Tools.Scenario.Nodes.UINode>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var ui in allUINodes)
            {
                if (ui.IsExecutionDisabled)
                    continue;

                if (ui.GetComponentInParent<Step>(true) == this)
                {
                    AddButtonConditionCallsite(ui, 0, result);
                    AddButtonConditionCallsite(ui, 1, result);
                }

                if (ui.UsesButtonA)
                    CollectFrom(ui.OnButtonA, ui, 0, result);
                if (ui.UsesButtonB)
                    CollectFrom(ui.OnButtonB, ui, 1, result);
                if (ui.UsesButtonA || ui.UsesButtonB)
                    CollectFrom(ui.OnEnd, ui, 2, result);
            }
            return result;
        }

        private void AddButtonConditionCallsite(
            DDOIT.Tools.Scenario.Nodes.UINode node,
            int buttonIndex,
            List<(DDOIT.Tools.Scenario.Nodes.UINode node, int buttonIndex, int group)> result)
        {
            int group = buttonIndex == 0 ? node.ButtonAConditionGroup : node.ButtonBConditionGroup;
            bool buttonEnabled = buttonIndex == 0 ? node.UsesButtonA : node.UsesButtonB;

            if (!buttonEnabled || group <= 0)
                return;

            AddExternalMarkerCallsite(node, buttonIndex, group, result);
        }

        private void CollectFrom(UnityEvent ev, DDOIT.Tools.Scenario.Nodes.UINode node, int buttonIndex,
            List<(DDOIT.Tools.Scenario.Nodes.UINode, int, int)> result)
        {
            if (ev == null) return;
            int count = ev.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (ev.GetPersistentTarget(i) != this) continue;
                string methodName = ev.GetPersistentMethodName(i);
                for (int g = 1; g <= MAX_CONDITION_GROUPS; g++)
                {
                    if (methodName == $"MarkConditionGroup{g}")
                    {
                        AddExternalMarkerCallsite(node, buttonIndex, g, result);
                        break;
                    }
                }
            }
        }

        private static void AddExternalMarkerCallsite(
            DDOIT.Tools.Scenario.Nodes.UINode node,
            int buttonIndex,
            int group,
            List<(DDOIT.Tools.Scenario.Nodes.UINode node, int buttonIndex, int group)> result)
        {
            foreach (var item in result)
            {
                if (item.node == node && item.buttonIndex == buttonIndex && item.group == group)
                    return;
            }

            result.Add((node, buttonIndex, group));
        }

        public void EndTrigger()
        {
            if (!IsActive) return;

            if (_isInitializing)
            {
                if (!_endRequestedDuringInitialization && ScenarioManager.DebugMode)
                    Debug.Log($"[Step] '{gameObject.name}' 초기화 중 종료 요청 감지 → 초기화 완료 후 종료");

                _endRequestedDuringInitialization = true;
                return;
            }

            if (ScenarioManager.DebugMode) Debug.Log($"[Step] '{gameObject.name}' 종료");

            if (_runtimeNodes != null)
            {
                foreach (var node in _runtimeNodes)
                    node.Release();
            }

            Step targetStep = ResolveTargetStep();
            Scenario targetScenario = ResolveTargetScenario();

            _onEnd?.Invoke();
            IsActive = false;
            gameObject.SetActive(false);

            // Scenario 분기가 있으면 현재 Scenario를 종료하고 타겟 Scenario로
            if (targetScenario != null)
                _parentScenario?.OnStepBranch(targetScenario);
            else
                _parentScenario?.OnStepCompleted(targetStep);
        }

        #endregion

        #region Private Methods

        private Step ResolveTargetStep()
        {
            if (_completedGroupIndex <= 0)
                return _defaultTargetStep;

            if (_completedGroupIndex > 0 &&
                _groupTargetSteps != null &&
                _completedGroupIndex - 1 < _groupTargetSteps.Length)
            {
                return _groupTargetSteps[_completedGroupIndex - 1];
            }

            return null;
        }

        private Scenario ResolveTargetScenario()
        {
            if (_completedGroupIndex <= 0)
                return _defaultTargetScenario;

            if (_completedGroupIndex > 0 &&
                _groupTargetScenarios != null &&
                _completedGroupIndex - 1 < _groupTargetScenarios.Length)
            {
                return _groupTargetScenarios[_completedGroupIndex - 1];
            }

            return null;
        }

        #endregion
    }
}
