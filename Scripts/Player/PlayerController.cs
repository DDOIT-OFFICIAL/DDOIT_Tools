using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// CharacterController 기반 자동 이동 시스템.
    /// 웨이포인트 경로 이동, 대상 추적, 텔레포트 기능 제공.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : Singleton<PlayerController>
    {
        #region Constants

        private const float WAYPOINT_TIMEOUT = 30f;
        private const float GRAVITY = 9.81f;
        private const float GROUNDED_VELOCITY = -2f;

        #endregion

        #region Serialized Fields

        [Header("Movement Settings")]
        [SerializeField, Range(1f, 10f)] private float _defaultMoveSpeed = 3.0f;
        [SerializeField, Range(0.01f, 1f)] private float _defaultArrivalThreshold = 0.1f;
        [SerializeField] private bool _enableGravity = true;

        [Header("Ground Check")]
        [SerializeField] private bool _enableGroundCheck = true;
        [SerializeField] private LayerMask _groundLayer = -1;
        [SerializeField, Range(0.1f, 0.5f)] private float _groundCheckDistance = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs;

        #endregion

        #region Private Fields

        private CharacterController _characterController;
        private Transform _transform;
        private Vector3 _velocity;
        private Vector3 _currentMovement;
        private bool _isGrounded;
        private Coroutine _movementCoroutine;
        private bool _isMovementActive;

        #endregion

        #region Properties

        public bool IsMovementActive => _isMovementActive;
        public bool IsGrounded => _isGrounded;
        public Vector3 Velocity => _velocity;

        #endregion

        #region Events

        /// <summary> 자동 이동 시작 시 발생. </summary>
        public event Action OnMovementStarted;

        /// <summary> 자동 이동 완료/중단 시 발생. </summary>
        public event Action OnMovementStopped;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _characterController = GetComponent<CharacterController>();
            _transform = transform;
        }

        private void Update()
        {
            CheckGroundStatus();
            ApplyGravity();
            ApplyMovement();
        }

        #endregion

        #region Public Methods — Movement

        /// <summary>
        /// 웨이포인트 경로를 순차 이동합니다.
        /// </summary>
        /// <param name="waypoints">이동할 웨이포인트 목록 (최소 1개)</param>
        /// <param name="moveSpeed">이동 속도 (m/s). 0 이하이면 Inspector 기본값 사용.</param>
        /// <param name="arrivalThreshold">도착 판정 거리 (m). 0 이하이면 Inspector 기본값 사용.</param>
        /// <param name="onComplete">모든 웨이포인트 도착 시 콜백</param>
        /// <param name="onWaypointReached">각 웨이포인트 도착 시 콜백 (인덱스 전달)</param>
        public void MoveToWaypoints(
            List<Vector3> waypoints,
            float moveSpeed = 0f,
            float arrivalThreshold = 0f,
            Action onComplete = null,
            Action<int> onWaypointReached = null)
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                LogDebug("MoveToWaypoints: waypoints가 비어있습니다.", LogType.Warning);
                onComplete?.Invoke();
                return;
            }

            float speed = moveSpeed > 0f ? moveSpeed : _defaultMoveSpeed;
            float threshold = arrivalThreshold > 0f ? arrivalThreshold : _defaultArrivalThreshold;

            BeginMovement(MoveToWaypointsCoroutine(
                waypoints, speed, threshold, onComplete, onWaypointReached));
        }

        /// <summary>
        /// 대상 Transform을 실시간으로 추적합니다.
        /// 대상 뒤 followDistance 위치를 유지하며, stopCondition이 true이고 도착하면 완료됩니다.
        /// </summary>
        /// <param name="target">추적할 대상 Transform</param>
        /// <param name="followDistance">대상과 유지할 거리 (m)</param>
        /// <param name="moveSpeed">이동 속도 (m/s). 0 이하이면 Inspector 기본값 사용.</param>
        /// <param name="arrivalThreshold">도착 판정 거리 (m). 0 이하이면 Inspector 기본값 사용.</param>
        /// <param name="stopCondition">true를 반환하면 대상이 멈춘 것으로 간주</param>
        /// <param name="onComplete">추적 완료 시 콜백</param>
        public void FollowTarget(
            Transform target,
            float followDistance,
            float moveSpeed = 0f,
            float arrivalThreshold = 0f,
            Func<bool> stopCondition = null,
            Action onComplete = null)
        {
            if (target == null)
            {
                LogDebug("FollowTarget: target이 null입니다.", LogType.Warning);
                onComplete?.Invoke();
                return;
            }

            float speed = moveSpeed > 0f ? moveSpeed : _defaultMoveSpeed;
            float threshold = arrivalThreshold > 0f ? arrivalThreshold : _defaultArrivalThreshold;

            BeginMovement(FollowTargetCoroutine(
                target, followDistance, speed, threshold, stopCondition, onComplete));
        }

        /// <summary>
        /// 현재 진행 중인 자동 이동을 중단합니다.
        /// </summary>
        public void StopMovement()
        {
            if (_movementCoroutine == null) return;

            StopCoroutine(_movementCoroutine);
            FinishMovement();
            LogDebug("자동 이동 중단");
        }

        #endregion

        #region Public Methods — Teleport

        /// <summary>
        /// 특정 위치로 즉시 텔레포트합니다.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            StopMovement();
            _characterController.enabled = false;
            _transform.position = position;
            _velocity = Vector3.zero;
            _characterController.enabled = true;
        }

        /// <summary>
        /// 특정 위치와 방향으로 즉시 텔레포트합니다.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            StopMovement();
            _characterController.enabled = false;
            _transform.SetPositionAndRotation(position, rotation);
            _velocity = Vector3.zero;
            _characterController.enabled = true;
        }

        #endregion

        #region Public Methods — Settings

        /// <summary>
        /// 중력 활성화/비활성화를 설정합니다.
        /// </summary>
        public void SetGravityEnabled(bool enabled)
        {
            _enableGravity = enabled;
            if (!enabled) _velocity.y = 0f;
        }

        #endregion

        #region Private — Physics

        private void CheckGroundStatus()
        {
            if (!_enableGroundCheck)
            {
                _isGrounded = true;
                return;
            }

            float radius = _characterController.radius;
            Vector3 origin = _transform.position + Vector3.up * (radius + 0.01f);

            _isGrounded = Physics.SphereCast(
                origin, radius * 0.9f, Vector3.down, out _, _groundCheckDistance, _groundLayer);
        }

        private void ApplyGravity()
        {
            if (!_enableGravity)
            {
                _velocity.y = 0f;
                return;
            }

            if (_isGrounded && _velocity.y < 0f)
                _velocity.y = GROUNDED_VELOCITY;
            else if (!_isGrounded)
                _velocity.y -= GRAVITY * Time.deltaTime;
        }

        private void ApplyMovement()
        {
            Vector3 movement = _currentMovement * Time.deltaTime;
            movement.y = _velocity.y * Time.deltaTime;
            _characterController.Move(movement);
        }

        #endregion

        #region Private — Movement Coroutines

        private void BeginMovement(IEnumerator routine)
        {
            StopMovement();
            _isMovementActive = true;
            _movementCoroutine = StartCoroutine(routine);
            OnMovementStarted?.Invoke();
        }

        private void FinishMovement()
        {
            _currentMovement = Vector3.zero;
            _isMovementActive = false;
            _movementCoroutine = null;
            OnMovementStopped?.Invoke();
        }

        private IEnumerator MoveToWaypointsCoroutine(
            List<Vector3> waypoints,
            float moveSpeed,
            float arrivalThreshold,
            Action onComplete,
            Action<int> onWaypointReached)
        {
            float arrivalThresholdSqr = arrivalThreshold * arrivalThreshold;

            LogDebug($"웨이포인트 이동 시작 ({waypoints.Count}개 지점)");

            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector3 target = waypoints[i];

                if (!IsValidVector3(target))
                {
                    Debug.LogWarning($"[PlayerController] 유효하지 않은 waypoint[{i}]. 스킵합니다.");
                    continue;
                }

                LogDebug($"Waypoint [{i}]로 이동 중");

                float timeoutTimer = 0f;

                while (true)
                {
                    timeoutTimer += Time.deltaTime;
                    if (timeoutTimer > WAYPOINT_TIMEOUT)
                    {
                        Debug.LogError(
                            $"[PlayerController] Waypoint [{i}] 타임아웃 ({WAYPOINT_TIMEOUT}초). 스킵.");
                        break;
                    }

                    if (GetHorizontalDistanceSqr(_transform.position, target) <= arrivalThresholdSqr)
                    {
                        SnapToHorizontalPosition(target);
                        LogDebug($"Waypoint [{i}] 도착");
                        onWaypointReached?.Invoke(i);
                        break;
                    }

                    Vector3 direction = target - _transform.position;
                    direction.y = 0f;
                    direction.Normalize();

                    _currentMovement = direction * moveSpeed;

                    yield return null;
                }

                yield return null;
            }

            FinishMovement();
            LogDebug("웨이포인트 이동 완료");
            onComplete?.Invoke();
        }

        private IEnumerator FollowTargetCoroutine(
            Transform target,
            float followDistance,
            float moveSpeed,
            float arrivalThreshold,
            Func<bool> stopCondition,
            Action onComplete)
        {
            float arrivalThresholdSqr = arrivalThreshold * arrivalThreshold;

            LogDebug($"대상 추적 시작 (followDistance: {followDistance}m)");

            while (true)
            {
                // 추적 대상이 파괴된 경우 안전 종료
                if (target == null)
                {
                    Debug.LogWarning("[PlayerController] 추적 대상이 파괴되었습니다.");
                    break;
                }

                Vector3 followPos = target.position - target.forward * followDistance;
                float distanceSqr = GetHorizontalDistanceSqr(_transform.position, followPos);

                if (distanceSqr > arrivalThresholdSqr)
                {
                    Vector3 direction = followPos - _transform.position;
                    direction.y = 0f;
                    direction.Normalize();

                    _currentMovement = direction * moveSpeed;
                }
                else
                {
                    _currentMovement = Vector3.zero;

                    if (stopCondition != null && stopCondition())
                        break;
                }

                yield return null;
            }

            FinishMovement();
            LogDebug("대상 추적 완료");
            onComplete?.Invoke();
        }

        private void SnapToHorizontalPosition(Vector3 target)
        {
            Vector3 pos = _transform.position;
            pos.x = target.x;
            pos.z = target.z;

            _characterController.enabled = false;
            _transform.position = pos;
            _characterController.enabled = true;
        }

        #endregion

        #region Utility

        private static bool IsValidVector3(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
                   !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }

        private static float GetHorizontalDistanceSqr(Vector3 a, Vector3 b)
        {
            float dx = b.x - a.x;
            float dz = b.z - a.z;
            return dx * dx + dz * dz;
        }

        private void LogDebug(string message, LogType logType = LogType.Log)
        {
            if (!_enableDebugLogs) return;

            switch (logType)
            {
                case LogType.Warning:
                    Debug.LogWarning($"[PlayerController] {message}");
                    break;
                case LogType.Error:
                    Debug.LogError($"[PlayerController] {message}");
                    break;
                default:
                    Debug.Log($"[PlayerController] {message}");
                    break;
            }
        }

        #endregion

        #region Editor Debug

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_enableGroundCheck || !Application.isPlaying || _characterController == null) return;

            float radius = _characterController.radius;
            Vector3 origin = transform.position + Vector3.up * (radius + 0.01f);

            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(origin + Vector3.down * _groundCheckDistance, radius * 0.9f);
        }

        [ContextMenu("Print Player Info")]
        private void PrintPlayerInfo()
        {
            Debug.Log($"=== PlayerController Info ===\n" +
                      $"IsGrounded: {_isGrounded}\n" +
                      $"IsMovementActive: {_isMovementActive}\n" +
                      $"Velocity: {_velocity}\n" +
                      $"Position: {transform.position}");
        }

        [ContextMenu("Test Waypoint Movement")]
        private void TestWaypointMovement()
        {
            var testPath = new List<Vector3>
            {
                transform.position + Vector3.forward * 5f,
                transform.position + Vector3.forward * 10f,
                transform.position + Vector3.right * 5f
            };

            MoveToWaypoints(testPath,
                onComplete: () => Debug.Log("[PlayerController] Test waypoint complete!"));
        }
#endif

        #endregion
    }
}
