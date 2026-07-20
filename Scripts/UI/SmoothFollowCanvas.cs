using UnityEngine;

namespace DDOIT.Tools.UI
{
    /// <summary>
    /// 카메라 전방 일정 거리의 가상 지점을 부드럽게 추적하는 Canvas용 컴포넌트.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SmoothFollowCanvas : MonoBehaviour
    {
        #region Serialized Fields

        [Header("추적 대상")]
        [Tooltip("추적할 카메라 Transform (CenterEyeAnchor)")]
        [SerializeField] private Transform _target;

        [Header("배치")]
        [Tooltip("카메라 전방 거리 (m)")]
        [SerializeField] private float _distance = 1.5f;

        [Header("보간")]
        [Tooltip("위치 추적 속도 (높을수록 빠름)")]
        [SerializeField, Range(1f, 20f)] private float _positionSpeed = 5f;

        [Tooltip("회전 추적 속도 (높을수록 빠름)")]
        [SerializeField, Range(1f, 20f)] private float _rotationSpeed = 3f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Assigns the follow target. Use snapImmediately when opening a pooled panel.
        /// </summary>
        public void SetTarget(Transform target, bool snapImmediately = false)
        {
            _target = target;

            if (snapImmediately)
                SnapToTarget();
        }

        /// <summary>
        /// Moves immediately to the current target pose without smoothing.
        /// </summary>
        public void SnapToTarget()
        {
            if (!TryGetTargetPose(out Vector3 targetPosition, out Quaternion targetRotation))
                return;

            transform.SetPositionAndRotation(targetPosition, targetRotation);
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!TryGetTargetPose(out Vector3 targetPosition, out Quaternion targetRotation))
                return;

            float deltaTime = Time.unscaledDeltaTime;
            transform.position = Vector3.Lerp(transform.position, targetPosition, _positionSpeed * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * deltaTime);
        }

        #endregion

        #region Private Methods

        private bool TryGetTargetPose(out Vector3 targetPosition, out Quaternion targetRotation)
        {
            if (_target == null)
            {
                targetPosition = default;
                targetRotation = default;
                return false;
            }

            float yaw = _target.eulerAngles.y;
            Vector3 flatForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            targetPosition = _target.position + flatForward * _distance;
            targetRotation = Quaternion.Euler(0f, yaw, 0f);
            return true;
        }

        #endregion
    }
}
