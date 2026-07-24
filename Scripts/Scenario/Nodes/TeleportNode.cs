using System.Collections;
using UnityEngine;
using UnityEngine.Events;

using DDOIT.Tools.Settings;
using DDOIT.Tools.Managers;
using DDOIT.Tools.Player;

namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// TeleportNode의 런타임 실행 상태.
    /// </summary>
    public enum TeleportNodeState
    {
        Idle,
        FadeOut,
        Teleporting,
        FadeIn,
        Completed,
        Failed,
        Released,
    }

    /// <summary>
    /// 플레이어를 지정 위치로 텔레포트하는 즉시 실행 노드.
    /// FadeToBlack → Teleport → FadeClear 순서로 실행한다.
    /// </summary>
    public class TeleportNode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private Transform _destination;
        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private Coroutine _coroutine;
        private TeleportNodeState _state = TeleportNodeState.Idle;
        private bool _isRunning;
        private bool _isReleased;
        private bool _didTeleport;
        private bool _lastExecutionSucceeded;
        private string _lastExecutionMessage = "Not executed.";

        #endregion

        #region Properties

        /// <summary>TeleportNode는 즉시 실행 노드이며 Step 조건 그룹에 참여하지 않는다.</summary>
        public override bool IsStepCondition => false;
        public Transform Destination => _destination;
        public TeleportNodeState State => _state;
        public bool IsRunning => _isRunning;
        public bool IsReleased => _isReleased;
        public bool DidTeleport => _didTeleport;
        public bool LastExecutionSucceeded => _lastExecutionSucceeded;
        public string LastExecutionMessage => _lastExecutionMessage;
        public float FadeDuration => GetFadeDuration();

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            PrepareRun();

            if (_destination == null)
            {
                Complete(false, "목적지가 없습니다.", true);
                return;
            }

            if (!PlayerRig.HasInstance)
            {
                Complete(false, "PlayerRig instance가 없습니다.", true);
                return;
            }

            PlayerRig playerRig = PlayerRig.Instance;
            if (playerRig == null || !playerRig.CanTeleport)
            {
                Complete(false, "PlayerRig의 player origin이 wiring되지 않았습니다.", true);
                return;
            }

            if (!ScreenFadeManager.HasInstance)
                Debug.LogWarning($"[TeleportNode] '{gameObject.name}': ScreenFadeManager가 없어 Fade 없이 텔레포트합니다.");

            _coroutine = StartCoroutine(TeleportSequence(playerRig));
        }

        protected override void OnRelease()
        {
            if (!_isRunning)
            {
                _isReleased = true;
                return;
            }

            CancelActiveRun("Release로 텔레포트 시퀀스를 중단하고 화면 Fade를 복구했습니다.");
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            if (!_isRunning)
                return;

            CancelActiveRun("GameObject 비활성화로 텔레포트 시퀀스를 중단하고 화면 Fade를 복구했습니다.");
        }

        #endregion

        #region Private Methods

        private IEnumerator TeleportSequence(PlayerRig playerRig)
        {
            float halfDuration = GetFadeDuration() * 0.5f;

            if (ScreenFadeManager.HasInstance && halfDuration > 0f)
            {
                _state = TeleportNodeState.FadeOut;
                yield return ScreenFadeManager.Instance.FadeToBlack(halfDuration);
            }

            if (_isReleased)
                yield break;

            _state = TeleportNodeState.Teleporting;
            _didTeleport = playerRig != null && playerRig.TryTeleport(_destination.position, _destination.rotation);
            if (!_didTeleport)
            {
                ClearFadeIfAvailable();
                Complete(false, "PlayerRig 텔레포트에 실패했습니다.", true);
                yield break;
            }

            if (_isReleased)
                yield break;

            if (ScreenFadeManager.HasInstance && halfDuration > 0f)
            {
                _state = TeleportNodeState.FadeIn;
                yield return ScreenFadeManager.Instance.FadeClear(halfDuration);
            }

            if (_isReleased)
                yield break;

            Complete(true, $"'{_destination.name}' 목적지로 텔레포트했습니다.", false);
        }

        private void PrepareRun()
        {
            StopActiveCoroutine();

            _state = TeleportNodeState.Idle;
            _isRunning = true;
            _isReleased = false;
            _didTeleport = false;
            _lastExecutionSucceeded = false;
            _lastExecutionMessage = "Running.";
        }

        private void Complete(bool succeeded, string message, bool logWarning)
        {
            _coroutine = null;
            _isRunning = false;
            _lastExecutionSucceeded = succeeded;
            _lastExecutionMessage = message;
            _state = succeeded ? TeleportNodeState.Completed : TeleportNodeState.Failed;

            if (logWarning)
                Debug.LogWarning($"[TeleportNode] '{gameObject.name}': {message}");

            _onEnd?.Invoke();
        }

        private void CancelActiveRun(string message)
        {
            StopActiveCoroutine();
            ClearFadeIfAvailable();

            _state = TeleportNodeState.Released;
            _isRunning = false;
            _isReleased = true;
            _lastExecutionSucceeded = false;
            _lastExecutionMessage = message;

            if (ScenarioManager.DebugMode)
                Debug.Log($"[TeleportNode] '{gameObject.name}': {message}");
        }

        private void StopActiveCoroutine()
        {
            if (_coroutine == null)
                return;

            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        private static void ClearFadeIfAvailable()
        {
            if (ScreenFadeManager.HasInstance)
                ScreenFadeManager.Instance.ClearFadeImmediate();
        }

        private static float GetFadeDuration()
        {
            return DDOITSettings.Instance != null
                ? Mathf.Max(0f, DDOITSettings.Instance.teleportFadeDuration)
                : 1f;
        }

        #endregion
    }
}
