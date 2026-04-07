using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// 특정 태그를 가진 객체의 트리거 이벤트를 감지하는 조건 노드.
    /// Enter: 진입 시 즉시 충족
    /// Exit: 이탈 시 즉시 충족
    /// Stay: 진입 후 지정 시간 동안 체류하면 충족 (중간에 이탈 시 리셋)
    /// </summary>
    public enum TriggerDetectMode
    {
        Enter,
        Exit,
        Stay,
    }

    public class TriggerConditionNode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private TriggerDetectMode _detectMode = TriggerDetectMode.Enter;
        [SerializeField] private string _targetTag = "Player";
        [SerializeField] private Collider _colliderSource;
        [SerializeField] private float _stayDuration = 2f;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private TriggerRelay _relay;
        private Coroutine _stayCoroutine;
        private bool _isInside;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            CleanupRelay();
            _isInside = false;

            if (_stayCoroutine != null)
            {
                StopCoroutine(_stayCoroutine);
                _stayCoroutine = null;
            }

            Collider col = _colliderSource != null ? _colliderSource : GetComponent<Collider>();

            if (col != null && !col.isTrigger)
                col.isTrigger = true;

            if (_colliderSource != null)
            {
                _relay = _colliderSource.gameObject.GetComponent<TriggerRelay>();
                if (_relay == null)
                    _relay = _colliderSource.gameObject.AddComponent<TriggerRelay>();
                _relay.Setup(this);
            }
        }

        protected override void OnRelease()
        {
            if (_stayCoroutine != null)
            {
                StopCoroutine(_stayCoroutine);
                _stayCoroutine = null;
            }
        }

        #endregion

        #region Trigger Handling — Self

        private void OnTriggerEnter(Collider other)
        {
            if (_colliderSource != null) return;
            HandleEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (_colliderSource != null) return;
            HandleExit(other);
        }

        #endregion

        #region Trigger Handling — Relay

        public void OnRelayTriggerEnter(Collider other)
        {
            HandleEnter(other);
        }

        public void OnRelayTriggerExit(Collider other)
        {
            HandleExit(other);
        }

        #endregion

        #region Logic

        private void HandleEnter(Collider other)
        {
            if (!IsStepCondition || IsConditionMet) return;
            if (!other.CompareTag(_targetTag)) return;

            _isInside = true;

            if (ScenarioManager.DebugMode)
                Debug.Log($"[TriggerConditionNode] '{gameObject.name}' Enter: {other.gameObject.name}");

            switch (_detectMode)
            {
                case TriggerDetectMode.Enter:
                    Complete();
                    break;
                case TriggerDetectMode.Stay:
                    if (_stayCoroutine != null)
                        StopCoroutine(_stayCoroutine);
                    _stayCoroutine = StartCoroutine(StayTimer());
                    break;
            }
        }

        private void HandleExit(Collider other)
        {
            if (!IsStepCondition || IsConditionMet) return;
            if (!other.CompareTag(_targetTag)) return;

            _isInside = false;

            if (ScenarioManager.DebugMode)
                Debug.Log($"[TriggerConditionNode] '{gameObject.name}' Exit: {other.gameObject.name}");

            switch (_detectMode)
            {
                case TriggerDetectMode.Exit:
                    Complete();
                    break;
                case TriggerDetectMode.Stay:
                    if (_stayCoroutine != null)
                    {
                        StopCoroutine(_stayCoroutine);
                        _stayCoroutine = null;
                        if (ScenarioManager.DebugMode)
                            Debug.Log($"[TriggerConditionNode] '{gameObject.name}' 체류 타이머 리셋");
                    }
                    break;
            }
        }

        private IEnumerator StayTimer()
        {
            yield return new WaitForSeconds(_stayDuration);
            _stayCoroutine = null;

            if (_isInside && !IsConditionMet)
            {
                if (ScenarioManager.DebugMode)
                    Debug.Log($"[TriggerConditionNode] '{gameObject.name}' 체류 {_stayDuration}초 완료");
                Complete();
            }
        }

        private void Complete()
        {
            _onEnd?.Invoke();
            SetConditionMet();
        }

        #endregion

        #region Cleanup

        private void CleanupRelay()
        {
            if (_relay != null)
            {
                Destroy(_relay);
                _relay = null;
            }
        }

        private void OnDisable()
        {
            CleanupRelay();
            if (_stayCoroutine != null)
            {
                StopCoroutine(_stayCoroutine);
                _stayCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            CleanupRelay();
        }

        #endregion
    }
}
