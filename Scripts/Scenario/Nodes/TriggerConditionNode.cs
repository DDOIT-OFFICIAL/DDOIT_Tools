using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// 특정 태그를 가진 객체의 트리거 진입을 감지하는 조건 전용 노드.
    /// _colliderSource가 지정되면 외부 Collider를 사용하고,
    /// 미지정이면 자기 자신의 Collider를 사용한다.
    /// </summary>
    public class TriggerConditionNode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private string _targetTag = "Player";
        [SerializeField] private Collider _colliderSource;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private TriggerRelay _relay;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            CleanupRelay();

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

        #endregion

        #region Trigger Handling

        /// <summary>자기 자신의 Collider에서 발생한 트리거.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (_colliderSource != null) return; // 외부 Collider 사용 중이면 무시
            HandleTrigger(other);
        }

        /// <summary>TriggerRelay를 통해 외부 Collider에서 전달된 트리거.</summary>
        public void OnRelayTriggerEnter(Collider other)
        {
            HandleTrigger(other);
        }

        private void HandleTrigger(Collider other)
        {
            if (!IsStepCondition) return;
            if (IsConditionMet) return;

            if (other.CompareTag(_targetTag))
            {
                if (ScenarioManager.DebugMode)
                    Debug.Log($"[TriggerConditionNode] '{gameObject.name}' 트리거 감지: {other.gameObject.name}");
                _onEnd?.Invoke();
                SetConditionMet();
            }
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
        }

        private void OnDestroy()
        {
            CleanupRelay();
        }

        #endregion
    }
}
