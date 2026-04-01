using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// 모든 Scenario를 초기화하고 EntryScenario를 시작하는 최상위 관리자.
    /// </summary>
    public class ScenarioManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("설정")]
        [Tooltip("GameManager에 의해 가장 먼저 시작될 시나리오")]
        [SerializeField] private Scenario _entryScenario;

        [Tooltip("시나리오/스텝/노드의 시작·종료 로그를 출력")]
        [SerializeField] private bool _debugLog;

        #endregion

        #region Properties

        public static bool DebugMode { get; private set; }

        #endregion

        #region Private Fields

        private Scenario[] _scenarios;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            DebugMode = _debugLog;

            _scenarios = GetComponentsInChildren<Scenario>(true);

            foreach (var scenario in _scenarios)
                scenario.gameObject.SetActive(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// EntryScenario를 시작한다. GameManager에서 호출.
        /// </summary>
        public void StartSequence()
        {
            if (_entryScenario != null)
            {
                if (DebugMode) Debug.Log($"[ScenarioManager] 시퀀스 시작 → '{_entryScenario.gameObject.name}'");
                _entryScenario.StartTrigger();
            }
            else
            {
                Debug.LogWarning("[ScenarioManager] EntryScenario가 지정되지 않았습니다");
            }
        }

        #endregion
    }
}
