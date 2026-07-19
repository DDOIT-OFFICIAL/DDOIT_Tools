using System;
using UnityEngine;
using MetaCharacterController = Oculus.Interaction.Locomotion.CharacterController;
using MetaFirstPersonLocomotor = Oculus.Interaction.Locomotion.FirstPersonLocomotor;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using DDOIT.Tools.Utilities;
namespace DDOIT.Tools.Player
{
    /// <summary>
    /// Player rig의 외부 진입점. ISDK FirstPersonLocomotor 기반 이동 시스템의 wrapper.
    /// 기존 PlayerController(461 LoC)를 대체.
    /// </summary>
    public class PlayerRig : Singleton<PlayerRig>
    {
        #region Constants

        private const string TURNER_EVENT_BROADCASTER_TYPE_NAME = "Oculus.Interaction.Locomotion.TurnerEventBroadcaster";
        private const int SNAP_TURN_METHOD_ENUM_INDEX = 0;
        private const float DEFAULT_SNAP_TURN_DEGREES = 45f;

        private const string LEFT_CONTROLLER_SLIDE_PATH =
            "OVRInteractionComprehensive/LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerSlideInteractor";

        private const string LEFT_CONTROLLER_TURNER_PATH =
            "OVRInteractionComprehensive/LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerTurnerInteractor";

        private const string LEFT_CONTROLLER_STEP_PATH =
            "OVRInteractionComprehensive/LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerStepInteractor";

        private const string LEFT_CONTROLLER_TELEPORT_PATH =
            "OVRInteractionComprehensive/LeftInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/TeleportControllerInteractor";

        private const string RIGHT_CONTROLLER_SLIDE_PATH =
            "OVRInteractionComprehensive/RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerSlideInteractor";

        private const string RIGHT_CONTROLLER_TURNER_PATH =
            "OVRInteractionComprehensive/RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerTurnerInteractor";

        private const string RIGHT_CONTROLLER_STEP_PATH =
            "OVRInteractionComprehensive/RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/ControllerStepInteractor";

        private const string RIGHT_CONTROLLER_TELEPORT_PATH =
            "OVRInteractionComprehensive/RightInteractions/Interactors/Controller/LocomotionControllerInteractorGroup/TeleportControllerInteractor";

        private const string SMOOTH_MOVEMENT_TUNNELING_PATH =
            "OVRInteractionComprehensive/Locomotor/SmoothMovementTunneling";

        private const string WALL_PENETRATION_TUNNELING_PATH =
            "OVRInteractionComprehensive/Locomotor/WallPenetrationTunneling";

        private const string PLAYER_CONTROLLER_PATH =
            "OVRInteractionComprehensive/Locomotor/PlayerController";

        private const float DEFAULT_GROUND_SNAP_DISTANCE = 10f;

        #endregion

        #region Serialized Fields

        [Header("Rig References")]
        [Tooltip("VR HMD center eye anchor — UI lookAt/follow 등이 참조하는 transform")]
        [SerializeField] private Transform _headTransform;

        [Tooltip("Player rig의 root transform — Teleport 시 위치를 옮길 대상")]
        [SerializeField] private Transform _playerOrigin;

        [Tooltip("WalkingStickLocomotor와 HandWalkingStick들을 묶은 부모 GameObject")]
        [SerializeField] private GameObject _walkingStickRoot;

        [Header("Controller Locomotion")]
        [Tooltip("Play Mode 진입 시 DDOIT 표준 컨트롤러 이동 설정을 자동 적용합니다.")]
        [SerializeField] private bool _applyDefaultControllerProfileOnAwake = true;

        [Tooltip("왼쪽 스틱 이동 입력 오브젝트")]
        [SerializeField] private GameObject _leftControllerSlideInteractor;

        [Tooltip("왼쪽 스틱 회전 입력 오브젝트. DDOIT 기본값에서는 비활성화합니다.")]
        [SerializeField] private GameObject _leftControllerTurnerInteractor;

        [Tooltip("왼쪽 스틱 Step 이동 오브젝트. DDOIT 기본값에서는 비활성화합니다.")]
        [SerializeField] private GameObject _leftControllerStepInteractor;

        [Tooltip("왼쪽 Teleport 이동 오브젝트. DDOIT 기본값에서는 비활성화합니다.")]
        [SerializeField] private GameObject _leftTeleportControllerInteractor;

        [Tooltip("오른쪽 스틱 이동 입력 오브젝트. DDOIT 기본값에서는 비활성화합니다.")]
        [SerializeField] private GameObject _rightControllerSlideInteractor;

        [Tooltip("오른쪽 스틱 스냅턴 입력 오브젝트")]
        [SerializeField] private GameObject _rightControllerTurnerInteractor;

        [Tooltip("오른쪽 스틱 Step 이동 오브젝트. DDOIT 기본값에서는 비활성화합니다.")]
        [SerializeField] private GameObject _rightControllerStepInteractor;

        [Tooltip("오른쪽 Teleport 이동 오브젝트. DDOIT 기본값에서는 비활성화합니다.")]
        [SerializeField] private GameObject _rightTeleportControllerInteractor;

        [Tooltip("이동/회전 시 주변 시야를 좁히는 comfort tunneling 오브젝트")]
        [SerializeField] private GameObject _smoothMovementTunneling;

        [Tooltip("벽 관통 시 주변 시야를 좁히는 comfort tunneling 오브젝트")]
        [SerializeField] private GameObject _wallPenetrationTunneling;

        [Tooltip("스냅턴 1회 회전 각도")]
        [SerializeField] private float _snapTurnDegrees = DEFAULT_SNAP_TURN_DEGREES;

        [Tooltip("켜면 스틱을 놓는 시점에 스냅턴하고, 끄면 스틱 입력 시작 시점에 스냅턴합니다.")]
        [SerializeField] private bool _fireSnapOnUnselect;

        [Tooltip("켜면 Meta comfort tunneling 효과를 사용합니다. DDOIT 기본값은 꺼짐입니다.")]
        [SerializeField] private bool _comfortTunnelingEnabled;

        [Header("Debug")]
        [Tooltip("켜면 스페이스바로 EnableWalkingStick/DisableWalkingStick 토글 가능")]
        [SerializeField] private bool _enableDebugKeyboard;

        #endregion

        #region Private Fields

        private MetaCharacterController _characterController;
        private MetaFirstPersonLocomotor _firstPersonLocomotor;

        #endregion

        #region Properties

        /// <summary> VR HMD의 center eye anchor transform. UI lookAt/follow 등에 사용. </summary>
        public Transform HeadTransform => _headTransform;

        /// <summary> 현재 WalkingStick 모드 활성 여부. </summary>
        public bool IsWalkingStickMode { get; private set; }

        /// <summary> 현재 comfort tunneling 오브젝트 활성 목표값. </summary>
        public bool IsComfortTunnelingEnabled => _comfortTunnelingEnabled;

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
            RecoverLocomotionAfterTeleport();
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
            RecoverLocomotionAfterTeleport();
        }

        #endregion

        #region Public Methods — Controller Locomotion

        /// <summary>
        /// DDOIT 표준 컨트롤러 이동 프로파일을 적용합니다.
        /// 왼쪽 스틱은 이동, 오른쪽 스틱은 스냅턴, comfort tunneling은 기본 비활성입니다.
        /// </summary>
        public void ApplyDefaultControllerLocomotionProfile()
        {
            ResolveDefaultReferences();

            SetActiveIfDifferent(_leftControllerSlideInteractor, true);
            SetActiveIfDifferent(_leftControllerTurnerInteractor, false);
            SetActiveIfDifferent(_leftControllerStepInteractor, false);
            SetActiveIfDifferent(_leftTeleportControllerInteractor, false);
            SetActiveIfDifferent(_rightControllerSlideInteractor, false);
            SetActiveIfDifferent(_rightControllerTurnerInteractor, true);
            SetActiveIfDifferent(_rightControllerStepInteractor, false);
            SetActiveIfDifferent(_rightTeleportControllerInteractor, false);
            SetComfortTunnelingEnabled(_comfortTunnelingEnabled);

            ConfigureSnapTurn(_leftControllerTurnerInteractor);
            ConfigureSnapTurn(_rightControllerTurnerInteractor);
        }

        /// <summary>
        /// Meta comfort tunneling 효과의 활성 여부를 변경합니다.
        /// </summary>
        public void SetComfortTunnelingEnabled(bool enabled)
        {
            _comfortTunnelingEnabled = enabled;
            SetActiveIfDifferent(_smoothMovementTunneling, enabled);
            SetActiveIfDifferent(_wallPenetrationTunneling, enabled);
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

        protected override void Awake()
        {
            base.Awake();

            if (_applyDefaultControllerProfileOnAwake)
                ApplyDefaultControllerLocomotionProfile();
        }

        private void Update()
        {
            HandleDebugKeyboard();
        }

        #endregion

        #region Private — Controller Locomotion

        private void ResolveDefaultReferences()
        {
            if (_playerOrigin == null)
                _playerOrigin = transform;

            Transform origin = _playerOrigin;
            if (origin == null)
                return;

            _leftControllerSlideInteractor = ResolveIfMissing(_leftControllerSlideInteractor, origin, LEFT_CONTROLLER_SLIDE_PATH);
            _leftControllerTurnerInteractor = ResolveIfMissing(_leftControllerTurnerInteractor, origin, LEFT_CONTROLLER_TURNER_PATH);
            _leftControllerStepInteractor = ResolveIfMissing(_leftControllerStepInteractor, origin, LEFT_CONTROLLER_STEP_PATH);
            _leftTeleportControllerInteractor = ResolveIfMissing(_leftTeleportControllerInteractor, origin, LEFT_CONTROLLER_TELEPORT_PATH);
            _rightControllerSlideInteractor = ResolveIfMissing(_rightControllerSlideInteractor, origin, RIGHT_CONTROLLER_SLIDE_PATH);
            _rightControllerTurnerInteractor = ResolveIfMissing(_rightControllerTurnerInteractor, origin, RIGHT_CONTROLLER_TURNER_PATH);
            _rightControllerStepInteractor = ResolveIfMissing(_rightControllerStepInteractor, origin, RIGHT_CONTROLLER_STEP_PATH);
            _rightTeleportControllerInteractor = ResolveIfMissing(_rightTeleportControllerInteractor, origin, RIGHT_CONTROLLER_TELEPORT_PATH);
            _smoothMovementTunneling = ResolveIfMissing(_smoothMovementTunneling, origin, SMOOTH_MOVEMENT_TUNNELING_PATH);
            _wallPenetrationTunneling = ResolveIfMissing(_wallPenetrationTunneling, origin, WALL_PENETRATION_TUNNELING_PATH);
            ResolveLocomotionReferences();
        }

        private void RecoverLocomotionAfterTeleport()
        {
            ResolveLocomotionReferences();

            if (_characterController == null && _firstPersonLocomotor == null)
            {
                Debug.LogWarning("[PlayerRig] Meta locomotion components could not be found. Movement recovery skipped.");
                return;
            }

            bool grounded = _characterController == null
                || _characterController.TryGround(DEFAULT_GROUND_SNAP_DISTANCE);

            if (!grounded)
            {
                Debug.LogWarning("[PlayerRig] Ground could not be found after teleport. Locomotion velocity remains disabled.");
                return;
            }

            if (_firstPersonLocomotor == null)
                return;

            _firstPersonLocomotor.EnableMovement();
            _firstPersonLocomotor.Velocity = Vector3.zero;
        }

        private void ResolveLocomotionReferences()
        {
            if (_characterController != null && _firstPersonLocomotor != null)
                return;

            Transform origin = _playerOrigin != null ? _playerOrigin : transform;
            Transform playerController = origin == null ? null : origin.Find(PLAYER_CONTROLLER_PATH);
            if (playerController == null)
                return;

            if (_characterController == null)
                _characterController = playerController.GetComponent<MetaCharacterController>();

            if (_firstPersonLocomotor == null)
                _firstPersonLocomotor = playerController.GetComponent<MetaFirstPersonLocomotor>();
        }

        private static GameObject ResolveIfMissing(GameObject current, Transform root, string path)
        {
            if (current != null || root == null)
                return current;

            Transform found = root.Find(path);
            return found == null ? null : found.gameObject;
        }

        private static void SetActiveIfDifferent(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private void ConfigureSnapTurn(GameObject turnerObject)
        {
            if (turnerObject == null)
                return;

            Component broadcaster = FindComponentByTypeName(turnerObject, TURNER_EVENT_BROADCASTER_TYPE_NAME);
            if (broadcaster == null)
                return;

            var type = broadcaster.GetType();
            SetEnumProperty(type, broadcaster, "TurnMethod", SNAP_TURN_METHOD_ENUM_INDEX);
            SetFloatProperty(type, broadcaster, "SnapTurnDegrees", _snapTurnDegrees);
            SetBoolProperty(type, broadcaster, "FireSnapOnUnselect", _fireSnapOnUnselect);
        }

        private static Component FindComponentByTypeName(GameObject target, string typeName)
        {
            foreach (var component in target.GetComponents<Component>())
            {
                if (component != null && component.GetType().FullName == typeName)
                    return component;
            }

            return null;
        }

        private static void SetEnumProperty(Type type, object target, string propertyName, int enumIndex)
        {
            var property = type.GetProperty(propertyName);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
                return;

            object value = Enum.ToObject(property.PropertyType, enumIndex);
            property.SetValue(target, value);
        }

        private static void SetFloatProperty(Type type, object target, string propertyName, float value)
        {
            var property = type.GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return;

            property.SetValue(target, value);
        }

        private static void SetBoolProperty(Type type, object target, string propertyName, bool value)
        {
            var property = type.GetProperty(propertyName);
            if (property == null || !property.CanWrite)
                return;

            property.SetValue(target, value);
        }

        #endregion

        #region Private — Debug Input

        private void HandleDebugKeyboard()
        {
            if (!_enableDebugKeyboard) return;

            bool isTogglePressed = false;
#if ENABLE_INPUT_SYSTEM
            isTogglePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!isTogglePressed)
            {
                isTogglePressed = Input.GetKeyDown(KeyCode.Space);
            }
#endif

            if (isTogglePressed)
            {
                if (IsWalkingStickMode) DisableWalkingStick();
                else                    EnableWalkingStick();
            }
        }

        #endregion
    }
}
