using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// 지정 시간 경과 후 조건을 충족하는 노드.
    /// Step 조건이 꺼져 있어도 타이머 완료 시 _onEnd가 발동된다.
    /// </summary>
    public class TimerConditionNode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private float _duration = 1f;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private Coroutine _coroutine;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            if (_duration <= 0f)
            {
                _onEnd?.Invoke();
                if (IsStepCondition) SetConditionMet();
                return;
            }

            _coroutine = StartCoroutine(Wait());
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(_duration);
            _coroutine = null;
            _onEnd?.Invoke();
            if (IsStepCondition) SetConditionMet();
        }

        #endregion
    }
}
