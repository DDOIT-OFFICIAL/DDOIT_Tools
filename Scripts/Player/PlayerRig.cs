using System;
using UnityEngine;
using UnityEngine.InputSystem;

using DDOIT.Tools.Utilities;
namespace DDOIT.Tools.Player
{
    /// <summary>
    /// Player rig의 외부 진입점. ISDK FirstPersonLocomotor 기반 이동 시스템의 wrapper.
    /// 기존 PlayerController(461 LoC)를 대체.
    /// </summary>
    public class PlayerRig : Singleton<PlayerRig>
    {
        #region Serialized Fields

        [Header("Rig References")]
        [Tooltip("VR HMD center eye anchor — UI lookAt/follow 등이 참조하는 transform")]
        [SerializeField] private Transform _headTransform;

        [Tooltip("Player rig의 root transform — Teleport 시 위치를 옮길 대상")]
        [SerializeField] private Transform _playerOrigin;

        [Tooltip("WalkingStickLocomotor와 HandWalkingStick들을 묶은 부모 GameObject")]
        [SerializeField] private GameObject _walkingStickRoot;

        [Header("Debug")]
        [Tooltip("켜면 스페이스바로 EnableWalkingStick/DisableWalkingStick 토글 가능")]
        [SerializeField] private bool _enableDebugKeyboard;

        #endregion

        #region Properties

        /// <summary> VR HMD의 center eye anchor transform. UI lookAt/follow 등에 사용. </summary>
        public Transform HeadTransform => _headTransform;

        /// <summary> 현재 WalkingStick 모드 활성 여부. </summary>
        public bool IsWalkingStickMode { get; private set; }

        #endregion

        #region Public Methods — Teleport

        /// <summary>
        /// 특정 위치로 즉시 텔레포트합니다.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            if (_playerOrigin == null)
            {
                Debug.LogError("[PlayerRig] _playerOrigin이 wiring되지 않았습니다.");
                return;
            }
            _playerOrigin.position = position;
        }

        /// <summary>
        /// 특정 위치와 방향으로 즉시 텔레포트합니다.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (_playerOrigin == null)
            {
                Debug.LogError("[PlayerRig] _playerOrigin이 wiring되지 않았습니다.");
                return;
            }
            _playerOrigin.SetPositionAndRotation(position, rotation);
        }

        #endregion

        #region Public Methods — WalkingStick

        /// <summary>
        /// WalkingStick locomotion 모드를 활성화합니다.
        /// </summary>
        public void EnableWalkingStick()
        {
            if (_walkingStickRoot == null)
            {
                Debug.LogError("[PlayerRig] _walkingStickRoot가 wiring되지 않았습니다.");
                return;
            }
            _walkingStickRoot.SetActive(true);
            IsWalkingStickMode = true;
            Debug.Log("[PlayerRig] WalkingStick 모드 활성화");
        }

        /// <summary>
        /// WalkingStick locomotion 모드를 비활성화합니다.
        /// </summary>
        public void DisableWalkingStick()
        {
            if (_walkingStickRoot == null)
            {
                Debug.LogError("[PlayerRig] _walkingStickRoot가 wiring되지 않았습니다.");
                return;
            }
            _walkingStickRoot.SetActive(false);
            IsWalkingStickMode = false;
            Debug.Log("[PlayerRig] WalkingStick 모드 비활성화");
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            HandleDebugKeyboard();
        }

        #endregion

        #region Private — Debug Input

        private void HandleDebugKeyboard()
        {
            if (!_enableDebugKeyboard) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (IsWalkingStickMode) DisableWalkingStick();
                else                    EnableWalkingStick();
            }
        }

        #endregion
    }
}
