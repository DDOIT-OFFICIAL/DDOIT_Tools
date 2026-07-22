using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using DDOIT.Tools.Player;

namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// Detects trigger enter/exit/stay events from a target tag and completes its Step condition.
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
        private Collider _activeCollider;
        private Collider _currentTarget;
        private GameObject _currentMatchedTarget;
        private readonly Dictionary<GameObject, int> _insideTargetCounts = new Dictionary<GameObject, int>();
        private float _stayElapsedTime;
        private bool _isInside;
        private bool _isTargetTagInvalid;

        #endregion

        #region Properties

        public TriggerDetectMode DetectMode => _detectMode;
        public string TargetTag => _targetTag;
        public Collider ColliderSource => _colliderSource;
        public Collider ActiveCollider => _activeCollider;
        public Collider CurrentTarget => _currentTarget;
        public GameObject CurrentMatchedTarget => _currentMatchedTarget;
        public int InsideTargetCount => _insideTargetCounts.Count;
        public float StayDuration => Mathf.Max(0f, _stayDuration);
        public float StayElapsedTime => _stayElapsedTime;
        public bool IsInside => _isInside;
        public bool IsStayRunning => _stayCoroutine != null;
        public float StayProgress => StayDuration <= 0f ? (_isInside ? 1f : 0f) : Mathf.Clamp01(_stayElapsedTime / StayDuration);

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            CleanupRelay();
            ResetTriggerState();

            _activeCollider = _colliderSource != null ? _colliderSource : GetComponent<Collider>();

            if (_activeCollider == null)
            {
                Debug.LogWarning($"[TriggerConditionNode] {gameObject.name}: Collider is missing. Trigger condition cannot be detected.");
                return;
            }

            if (!_activeCollider.isTrigger)
                _activeCollider.isTrigger = true;

            if (_colliderSource != null)
                RegisterRelay(_activeCollider);
        }

        protected override void OnRelease()
        {
            CleanupRuntimeState();
        }

        #endregion

        #region Trigger Handling - Self

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

        #region Trigger Handling - Relay

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
            if (!CanHandleTrigger(other, out GameObject matchedTarget)) return;

            AddInsideTarget(matchedTarget);
            _isInside = _insideTargetCounts.Count > 0;
            _currentTarget = other;
            _currentMatchedTarget = matchedTarget;

            if (ScenarioManager.DebugMode)
                Debug.Log($"[TriggerConditionNode] '{gameObject.name}' Enter: {other.gameObject.name} (matched: {matchedTarget.name})");

            switch (_detectMode)
            {
                case TriggerDetectMode.Enter:
                    Complete();
                    break;
                case TriggerDetectMode.Stay:
                    if (StayDuration <= 0f)
                    {
                        Complete();
                        return;
                    }

                    StartStayTimer();
                    break;
            }
        }

        private void HandleExit(Collider other)
        {
            if (!CanHandleTrigger(other, out GameObject matchedTarget)) return;

            RemoveInsideTarget(matchedTarget);
            _isInside = _insideTargetCounts.Count > 0;

            if (!_isInside)
            {
                _currentTarget = null;
                _currentMatchedTarget = null;
            }

            if (ScenarioManager.DebugMode)
                Debug.Log($"[TriggerConditionNode] '{gameObject.name}' Exit: {other.gameObject.name} (matched: {matchedTarget.name})");

            switch (_detectMode)
            {
                case TriggerDetectMode.Exit:
                    if (!_isInside)
                        Complete();
                    break;
                case TriggerDetectMode.Stay:
                    if (!_isInside)
                    {
                        StopStayTimer();
                        if (ScenarioManager.DebugMode)
                            Debug.Log($"[TriggerConditionNode] '{gameObject.name}' Stay timer reset");
                    }
                    break;
            }
        }

        private IEnumerator StayTimer()
        {
            _stayElapsedTime = 0f;

            while (_stayElapsedTime < StayDuration)
            {
                if (!_isInside || IsConditionMet)
                {
                    StopStayTimer();
                    yield break;
                }

                _stayElapsedTime += Time.deltaTime;
                yield return null;
            }

            _stayCoroutine = null;

            if (_isInside && !IsConditionMet)
            {
                if (ScenarioManager.DebugMode)
                    Debug.Log($"[TriggerConditionNode] '{gameObject.name}' Stay completed: {StayDuration:0.###}s");
                Complete();
            }
        }

        private void Complete()
        {
            if (IsConditionMet) return;

            StopStayTimer();
            _onEnd?.Invoke();
            SetConditionMet();
        }

        private bool CanHandleTrigger(Collider other, out GameObject matchedTarget)
        {
            matchedTarget = null;

            if (!IsStepCondition || IsConditionMet || other == null) return false;
            if (string.IsNullOrWhiteSpace(_targetTag)) return false;

            return TryGetMatchedTarget(other, out matchedTarget);
        }

        private bool TryGetMatchedTarget(Collider other, out GameObject matchedTarget)
        {
            matchedTarget = null;

            if (HasTargetTag(other.gameObject))
            {
                matchedTarget = other.gameObject;
                return true;
            }

            Rigidbody attachedRigidbody = other.attachedRigidbody;
            if (attachedRigidbody != null && HasTargetTag(attachedRigidbody.gameObject))
            {
                matchedTarget = attachedRigidbody.gameObject;
                return true;
            }

            for (Transform parent = other.transform.parent; parent != null; parent = parent.parent)
            {
                if (!HasTargetTag(parent.gameObject))
                    continue;

                matchedTarget = parent.gameObject;
                return true;
            }

            if (IsPlayerTag())
            {
                PlayerRig playerRig = other.GetComponentInParent<PlayerRig>();
                if (playerRig != null)
                {
                    matchedTarget = playerRig.gameObject;
                    return true;
                }
            }

            return false;
        }

        private bool HasTargetTag(GameObject target)
        {
            if (target == null || _isTargetTagInvalid)
                return false;

            try
            {
                return target.CompareTag(_targetTag);
            }
            catch (UnityException)
            {
                _isTargetTagInvalid = true;
                Debug.LogWarning($"[TriggerConditionNode] {gameObject.name}: Tag '{_targetTag}' is not defined.");
                return false;
            }
        }

        private bool IsPlayerTag()
        {
            return string.Equals(_targetTag, "Player", StringComparison.Ordinal);
        }

        private void AddInsideTarget(GameObject target)
        {
            if (target == null)
                return;

            _insideTargetCounts.TryGetValue(target, out int count);
            _insideTargetCounts[target] = count + 1;
        }

        private void RemoveInsideTarget(GameObject target)
        {
            if (target == null)
                return;

            if (!_insideTargetCounts.TryGetValue(target, out int count))
                return;

            if (count <= 1)
                _insideTargetCounts.Remove(target);
            else
                _insideTargetCounts[target] = count - 1;
        }

        #endregion

        #region Stay Timer

        private void StartStayTimer()
        {
            StopStayTimer();
            _stayCoroutine = StartCoroutine(StayTimer());
        }

        private void StopStayTimer()
        {
            if (_stayCoroutine != null)
            {
                StopCoroutine(_stayCoroutine);
                _stayCoroutine = null;
            }

            _stayElapsedTime = 0f;
        }

        #endregion

        #region Relay

        private void RegisterRelay(Collider source)
        {
            _relay = source.gameObject.GetComponent<TriggerRelay>();
            if (_relay == null)
                _relay = source.gameObject.AddComponent<TriggerRelay>();

            _relay.AddOwner(this);
        }

        private void CleanupRelay()
        {
            if (_relay == null) return;

            _relay.RemoveOwner(this);
            if (!_relay.HasOwners)
                Destroy(_relay);

            _relay = null;
        }

        #endregion

        #region Cleanup

        private void ResetTriggerState()
        {
            StopStayTimer();
            _activeCollider = null;
            _currentTarget = null;
            _currentMatchedTarget = null;
            _insideTargetCounts.Clear();
            _isInside = false;
            _isTargetTagInvalid = false;
        }

        private void CleanupRuntimeState()
        {
            CleanupRelay();
            ResetTriggerState();
        }

        private void OnDisable()
        {
            CleanupRuntimeState();
        }

        private void OnDestroy()
        {
            CleanupRelay();
        }

        #endregion
    }
}
