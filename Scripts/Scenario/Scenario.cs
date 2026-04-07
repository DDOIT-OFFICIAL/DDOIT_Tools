using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// Step들을 실행하는 시나리오 단위.
    /// Step 완료 시 지정된 타겟 Step으로 분기하거나 다음 순번으로 진행한다.
    /// Step이 다른 Scenario로 분기하면 현재 Scenario를 종료하고 타겟을 시작한다.
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
        /// </summary>
        public void SetNextScenario(Scenario scenario)
        {
            _nextScenario = scenario;
        }

        /// <summary>
        /// Step 완료 시 호출. targetStep이 있으면 해당 Step으로 분기, 없으면 다음 순번.
        /// </summary>
        internal void OnStepCompleted(Step targetStep = null)
        {
            if (targetStep != null)
                RunSpecificStep(targetStep);
            else
                RunNextStep();
        }

        /// <summary>
        /// Step이 다른 Scenario로 분기할 때 호출.
        /// 현재 Scenario를 종료하고 타겟 Scenario를 시작한다.
        /// </summary>
        internal void OnStepBranch(Scenario targetScenario)
        {
            if (!IsActive) return;

            if (ScenarioManager.DebugMode)
                Debug.Log($"[Scenario] '{gameObject.name}' → '{targetScenario.gameObject.name}'으로 Scenario 분기");

            IsActive = false;
            _onEnd?.Invoke();
            gameObject.SetActive(false);

            targetScenario.StartTrigger();
        }

        #endregion

        #region Private Methods

        private void RunNextStep()
        {
            _currentStepIndex++;

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

        private void RunSpecificStep(Step target)
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_steps[i] == target)
                {
                    _currentStepIndex = i;

                    if (ScenarioManager.DebugMode)
                        Debug.Log($"[Scenario] '{gameObject.name}' → '{target.gameObject.name}'으로 Step 분기");

                    _steps[i].StartTrigger();
                    return;
                }
            }

            Debug.LogWarning($"[Scenario] '{gameObject.name}': 타겟 Step '{target.gameObject.name}'을 찾을 수 없습니다. 다음 순번으로 진행.");
            RunNextStep();
        }

        #endregion
    }
}
