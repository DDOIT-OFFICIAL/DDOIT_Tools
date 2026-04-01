using System.Collections;
using UnityEngine;

namespace DDOIT.Tools
{
    public class TimerConditionNode : ScenarioNode
    {
        [Header("설정")]
        [SerializeField] private float _duration = 1f;

        private Coroutine _coroutine;

        protected override void OnInit()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            if (!IsStepCondition) return;

            if (_duration <= 0f)
            {
                SetConditionMet();
                return;
            }

            _coroutine = StartCoroutine(Wait());
        }

        private void OnDisable()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(_duration);
            _coroutine = null;
            SetConditionMet();
        }
    }
}
