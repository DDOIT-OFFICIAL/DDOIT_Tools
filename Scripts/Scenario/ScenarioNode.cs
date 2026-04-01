using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// 단일 기능을 수행하는 노드의 추상 베이스 클래스.
    /// Step이 활성화되면 Init()이 호출되며,
    /// _isStepCondition이 켜져 있으면 SetConditionMet()으로 Step 완료 조건에 참여한다.
    /// Step이 종료되면 Release()가 호출되어 노드를 정리한다.
    /// </summary>
    public abstract class ScenarioNode : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Step 조건")]
        [Tooltip("이 노드가 Step 완료 조건에 포함되는지 여부")]
        [SerializeField] private bool _isStepCondition;

        [Header("이벤트")]
        [Tooltip("노드 종료 시 (Step 종료 직전)")]
        [SerializeField] private UnityEvent _onRelease;

        #endregion

        #region Properties

        /// <summary>이 노드가 Step 완료 조건인지 여부.</summary>
        public bool IsStepCondition => _isStepCondition;

        /// <summary>이 노드의 조건이 충족되었는지 여부.</summary>
        public bool IsConditionMet { get; private set; }

        protected Step ParentStep => _parentStep;

        #endregion

        #region Private Fields

        private Step _parentStep;

        #endregion

        #region Public Methods

        public void Init()
        {
            _parentStep = GetComponentInParent<Step>();
            IsConditionMet = false;

            if (ScenarioManager.DebugMode) Debug.Log($"[Node] '{gameObject.name}' Init (조건: {_isStepCondition})");

            OnInit();
        }

        public void Release()
        {
            if (ScenarioManager.DebugMode) Debug.Log($"[Node] '{gameObject.name}' Release");

            OnRelease();
            _onRelease?.Invoke();
        }

        #endregion

        #region Protected Methods

        /// <summary>Node 초기화 시 파생 클래스가 구현할 로직.</summary>
        protected abstract void OnInit();

        /// <summary>Node 종료 시 파생 클래스가 정리할 로직. 필요한 경우 override.</summary>
        protected virtual void OnRelease() { }

        /// <summary>
        /// 이 노드의 조건이 충족되었을 때 호출한다.
        /// Step에 조건 충족을 보고하고, Step은 모든 조건 노드를 확인한 뒤 완료 여부를 결정한다.
        /// </summary>
        protected void SetConditionMet()
        {
            if (!_isStepCondition) return;
            if (IsConditionMet) return;

            IsConditionMet = true;

            if (ScenarioManager.DebugMode) Debug.Log($"[Node] '{gameObject.name}' 조건 충족");

            if (_parentStep != null)
                _parentStep.OnNodeConditionMet();
            else
                Debug.LogError($"[ScenarioNode] {gameObject.name}: 상위 Step을 찾을 수 없습니다");
        }

        #endregion
    }
}
