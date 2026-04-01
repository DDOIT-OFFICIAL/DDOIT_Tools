using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// 카메라 전방 일정 거리의 가상 지점을 부드럽게 추적하는 Canvas용 컴포넌트.
    /// CenterEyeAnchor에 직접 붙이지 않고, 독립된 GameObject에 부착한다.
    /// </summary>
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
        [Tooltip("위치 추적 속도 (낮을수록 부드러움)")]
        [SerializeField, Range(1f, 20f)] private float _positionSpeed = 5f;

        [Tooltip("회전 추적 속도 (낮을수록 부드러움)")]
        [SerializeField, Range(1f, 20f)] private float _rotationSpeed = 3f;

        #endregion

        #region Public Methods

        /// <summary>
        /// 추적 대상을 런타임에 변경한다.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        #endregion

        #region Unity Lifecycle

        private void LateUpdate()
        {
            if (_target == null) return;

            // Yaw(좌우 회전)만 추출 — Roll(머리 기울임), Pitch(상하) 무시
            float yaw = _target.eulerAngles.y;
            Vector3 flatForward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            // 위치: 눈높이 고정 (Y는 target.position.y) + 수평 전방으로 distance
            Vector3 targetPosition = _target.position + flatForward * _distance;

            // 회전: Yaw만 적용
            Quaternion targetRotation = Quaternion.Euler(0f, yaw, 0f);

            float dt = Time.unscaledDeltaTime;
            transform.position = Vector3.Lerp(transform.position, targetPosition, _positionSpeed * dt);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * dt);
        }

        #endregion
    }
}
