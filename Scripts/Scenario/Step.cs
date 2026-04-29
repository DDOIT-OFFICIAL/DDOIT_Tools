using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using DDOIT.Tools.Settings;
namespace DDOIT.Tools.Scenario
{
    /// <summary>
    /// 하위 Node들을 관할하는 단계.
    /// 조건 그룹 시스템: 노드는 1~N 그룹에 소속되며,
    /// 그룹 내 모든 노드가 충족되면(AND) 해당 그룹이 완료된다.
    /// 그룹 중 하나라도 완료되면(OR) Step이 종료된다.
    /// 각 그룹은 완료 시 이동할 Step 또는 Scenario를 지정할 수 있다.
    /// 조건 그룹이 0개이면 기본 대기 후 자동 진행한다.
    /// </summary>
    public class Step : MonoBehaviour
    {
        private static float DefaultStepWait =>
            DDOITSettings.Instance != null ? DDOITSettings.Instance.defaultStepWait : 0.5f;

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
        private Dictionary<int, List<ScenarioNode>> _conditionGroups;
        private int _completedGroupIndex;
        private Coroutine _defaultWaitCoroutine;

        #endregion

        #region Public Methods

        public void StartTrigger()
        {
            gameObject.SetActive(true);
            IsActive = true;
            _completedGroupIndex = -1;

            _parentScenario = GetComponentInParent<Scenario>();
            _nodes = GetComponentsInChildren<ScenarioNode>(true);

            // 그룹별 조건 노드 수집
            _conditionGroups = new Dictionary<int, List<ScenarioNode>>();
            foreach (var node in _nodes)
            {
                int group = node.ConditionGroup;
                if (group <= 0) continue;

                if (!_conditionGroups.ContainsKey(group))
                    _conditionGroups[group] = new List<ScenarioNode>();

                _conditionGroups[group].Add(node);
            }

            if (ScenarioManager.DebugMode)
            {
                int totalConditions = 0;
                foreach (var g in _conditionGroups.Values)
                    totalConditions += g.Count;
                Debug.Log($"[Step] '{gameObject.name}' 시작 (Node {_nodes.Length}개, 조건 그룹 {_conditionGroups.Count}개, 조건 노드 {totalConditions}개)");
            }

            _onStart?.Invoke();

            foreach (var node in _nodes)
            {
                node.gameObject.SetActive(true);
                node.Init();
            }

            // 조건 노드가 없으면 기본 대기 후 자동 진행
            if (_conditionGroups.Count == 0)
            {
                if (ScenarioManager.DebugMode)
                    Debug.Log($"[Step] '{gameObject.name}' 조건 없음 → {DefaultStepWait}초 대기 후 자동 진행");
                _defaultWaitCoroutine = StartCoroutine(DefaultWait());
            }
        }

        /// <summary>
        /// 노드가 조건 충족을 보고할 때 호출된다.
        /// 아무 그룹이든 전원 충족되면 Step을 종료한다.
        /// </summary>
        public void OnNodeConditionMet()
        {
            if (!IsActive) return;

            foreach (var kvp in _conditionGroups)
            {
                bool groupComplete = true;
                foreach (var node in kvp.Value)
                {
                    if (!node.IsConditionMet)
                    {
                        groupComplete = false;
                        break;
                    }
                }

                if (groupComplete)
                {
                    if (ScenarioManager.DebugMode)
                        Debug.Log($"[Step] '{gameObject.name}' 조건 그룹 {kvp.Key} 완료 → 종료");
                    _completedGroupIndex = kvp.Key;
                    EndTrigger();
                    return;
                }
            }
        }

        public void EndTrigger()
        {
            if (!IsActive) return;

            if (ScenarioManager.DebugMode) Debug.Log($"[Step] '{gameObject.name}' 종료");

            if (_defaultWaitCoroutine != null)
            {
                StopCoroutine(_defaultWaitCoroutine);
                _defaultWaitCoroutine = null;
            }

            if (_nodes != null)
            {
                foreach (var node in _nodes)
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

        private IEnumerator DefaultWait()
        {
            yield return new WaitForSeconds(DefaultStepWait);
            _defaultWaitCoroutine = null;
            _completedGroupIndex = 0;
            EndTrigger();
        }

        private Step ResolveTargetStep()
        {
            if (_completedGroupIndex == 0)
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
            if (_completedGroupIndex == 0)
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
