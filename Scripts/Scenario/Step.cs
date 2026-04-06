using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// 하위 Node들을 관할하는 단계.
    /// 조건 그룹 시스템: 노드는 1~N 그룹에 소속되며,
    /// 그룹 내 모든 노드가 충족되면(AND) 해당 그룹이 완료된다.
    /// 그룹 중 하나라도 완료되면(OR) Step이 종료된다.
    /// </summary>
    public class Step : MonoBehaviour
    {
        #region Serialized Fields

        [Header("설정")]
        [Tooltip("이 Step을 건너뛸지 여부")]
        [SerializeField] private bool _skip;

        [Tooltip("조건 그룹 수 (1 이상)")]
        [SerializeField] private int _conditionGroupCount = 1;

        [Header("이벤트")]
        [Tooltip("Step 시작 시")]
        [SerializeField] private UnityEvent _onStart;

        [Tooltip("Step 종료 시")]
        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }
        public bool Skip => _skip;

        /// <summary>조건 그룹 수.</summary>
        public int ConditionGroupCount => _conditionGroupCount;

        #endregion

        #region Private Fields

        private Scenario _parentScenario;
        private ScenarioNode[] _nodes;

        // 그룹별 조건 노드 (key: 그룹 번호 1~N)
        private Dictionary<int, List<ScenarioNode>> _conditionGroups;

        #endregion

        #region Public Methods

        public void StartTrigger()
        {
            gameObject.SetActive(true);
            IsActive = true;

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

            // 조건 노드가 없으면 무한 대기 (외부에서 EndTrigger 호출 필요)
            if (_conditionGroups.Count == 0)
            {
                if (ScenarioManager.DebugMode)
                    Debug.Log($"[Step] '{gameObject.name}' 조건 노드 없음 → 대기 (EndTrigger 호출 필요)");
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
                    EndTrigger();
                    return;
                }
            }
        }

        public void EndTrigger()
        {
            if (!IsActive) return;

            if (ScenarioManager.DebugMode) Debug.Log($"[Step] '{gameObject.name}' 종료");

            if (_nodes != null)
            {
                foreach (var node in _nodes)
                    node.Release();
            }

            _onEnd?.Invoke();
            IsActive = false;
            gameObject.SetActive(false);

            _parentScenario?.OnStepCompleted();
        }

        #endregion
    }
}
