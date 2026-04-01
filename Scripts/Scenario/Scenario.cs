using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// Step들을 직렬로 실행하는 시나리오 단위.
    /// 종료 시 _nextScenario가 있으면 해당 시나리오를 시작한다.
    /// </summary>
    public class Scenario : MonoBehaviour
    {
        #region Serialized Fields

        [Header("다음 시나리오")]
        [Tooltip("이 시나리오 종료 후 시작할 시나리오 (없으면 종료)")]
        [SerializeField] private Scenario _nextScenario;

        [Header("이벤트")]
        [Tooltip("시나리오 시작 시")]
        [SerializeField] private UnityEvent _onStart;

        [Tooltip("시나리오 종료 시")]
        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Properties

        public bool IsActive { get; private set; }

        #endregion

        #region Private Fields

        private Step[] _steps;
        private int _currentStepIndex;

        #endregion

        #region Public Methods

        public void StartTrigger()
        {
            gameObject.SetActive(true);
            IsActive = true;

            _steps = GetComponentsInChildren<Step>(true);
            foreach (var step in _steps)
                step.gameObject.SetActive(false);

            _currentStepIndex = -1;

            if (ScenarioManager.DebugMode) Debug.Log($"[Scenario] '{gameObject.name}' 시작");

            _onStart?.Invoke();
            RunNextStep();
        }

        public void EndTrigger()
        {
            if (!IsActive) return;

            if (ScenarioManager.DebugMode) Debug.Log($"[Scenario] '{gameObject.name}' 종료");

            IsActive = false;
            _onEnd?.Invoke();
            gameObject.SetActive(false);

            if (_nextScenario != null)
                _nextScenario.StartTrigger();
        }

        /// <summary>
        /// 런타임에 다음 시나리오를 동적으로 변경한다.
        /// 분기 노드에서 호출 가능.
        /// </summary>
        public void SetNextScenario(Scenario scenario)
        {
            _nextScenario = scenario;
        }

        internal void OnStepCompleted()
        {
            RunNextStep();
        }

        #endregion

        #region Private Methods

        private void RunNextStep()
        {
            _currentStepIndex++;

            // Skip이 켜진 Step은 건너뛴다
            while (_currentStepIndex < _steps.Length && _steps[_currentStepIndex].Skip)
            {
                if (ScenarioManager.DebugMode)
                    Debug.Log($"[Scenario] '{_steps[_currentStepIndex].gameObject.name}' 건너뜀");
                _currentStepIndex++;
            }

            if (_currentStepIndex >= _steps.Length)
            {
                EndTrigger();
                return;
            }

            _steps[_currentStepIndex].StartTrigger();
        }

        #endregion
    }
}
