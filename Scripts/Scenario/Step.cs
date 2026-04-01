using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// 하위 Node들을 관할하는 단계.
    /// StartTrigger()로 진입하면 모든 자식 Node를 활성화하고 Init()을 호출한다.
    /// _isStepCondition이 켜진 모든 Node의 조건이 충족되면 자동으로 EndTrigger()가 발동된다.
    /// </summary>
    public class Step : MonoBehaviour
    {
        #region Serialized Fields

        [Header("설정")]
        [Tooltip("이 Step을 건너뛸지 여부")]
        [SerializeField] private bool _skip;

        [Header("이벤트")]
        [Tooltip("Step 시작 시")]
        [SerializeField] private UnityEvent _onStart;

        [Tooltip("Step 종료 시")]
        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }
        public bool Skip => _skip;

        #endregion

        #region Private Fields

        private Scenario _parentScenario;
        private ScenarioNode[] _nodes;
        private ScenarioNode[] _conditionNodes;

        #endregion

        #region Public Methods

        public void StartTrigger()
        {
            gameObject.SetActive(true);
            IsActive = true;

            _parentScenario = GetComponentInParent<Scenario>();
            _nodes = GetComponentsInChildren<ScenarioNode>(true);

            // 조건 노드만 수집
            var conditionList = new List<ScenarioNode>();
            foreach (var node in _nodes)
            {
                if (node.IsStepCondition)
                    conditionList.Add(node);
            }
            _conditionNodes = conditionList.ToArray();

            if (ScenarioManager.DebugMode)
                Debug.Log($"[Step] '{gameObject.name}' 시작 (Node {_nodes.Length}개, 조건 {_conditionNodes.Length}개)");

            _onStart?.Invoke();

            foreach (var node in _nodes)
            {
                node.gameObject.SetActive(true);
                node.Init();
            }

            // 조건 노드가 없으면 무한 대기 (외부에서 EndTrigger 호출 필요)
            if (_conditionNodes.Length == 0)
            {
                if (ScenarioManager.DebugMode)
                    Debug.Log($"[Step] '{gameObject.name}' 조건 노드 없음 → 대기 (EndTrigger 호출 필요)");
            }
        }

        /// <summary>
        /// 노드가 조건 충족을 보고할 때 호출된다.
        /// 모든 조건 노드가 충족되면 Step을 종료한다.
        /// </summary>
        public void OnNodeConditionMet()
        {
            if (!IsActive) return;

            foreach (var node in _conditionNodes)
            {
                if (!node.IsConditionMet) return;
            }

            if (ScenarioManager.DebugMode)
                Debug.Log($"[Step] '{gameObject.name}' 모든 조건 충족 → 종료");
            EndTrigger();
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
