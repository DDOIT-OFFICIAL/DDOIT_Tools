using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// 대상의 활성/비활성 상태를 토글하는 노드.
    /// GameObject, Component, ParticleSystem, IToggleable 스크립트를 On/Off 제어한다.
    /// </summary>
    public enum ToggleMode
    {
        GameObject,
        Component,
        Particle,
        Script,
    }

    /// <summary>
    /// ToggleNode의 마지막 실행 상태.
    /// </summary>
    public enum ToggleNodeState
    {
        Idle,
        Applied,
        Failed,
    }

    public class ToggleNode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private ToggleMode _mode;
        [SerializeField] private GameObject _targetObject;
        [SerializeField] private Component _targetComponent;
        [SerializeField] private ParticleSystem _targetParticle;
        [SerializeField] private MonoBehaviour _targetScript;
        [SerializeField] private bool _activate = true;
        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private ToggleNodeState _state = ToggleNodeState.Idle;
        private bool _lastExecutionSucceeded;
        private string _lastExecutionMessage = "Not executed.";

        #endregion

        #region Properties

        /// <summary>ToggleNode는 즉시 실행 노드이며 Step 조건 그룹에 참여하지 않는다.</summary>
        public override bool IsStepCondition => false;
        public ToggleMode Mode => _mode;
        public bool Activate => _activate;
        public ToggleNodeState State => _state;
        public bool LastExecutionSucceeded => _lastExecutionSucceeded;
        public string LastExecutionMessage => _lastExecutionMessage;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            if (!TryApply(out string message))
                Debug.LogWarning($"[ToggleNode] '{gameObject.name}': {message}");

            _lastExecutionMessage = message;
            _onEnd?.Invoke();
        }

        #endregion

        #region Private Methods

        private bool TryApply(out string message)
        {
            _lastExecutionSucceeded = false;
            _state = ToggleNodeState.Failed;

            bool success = _mode switch
            {
                ToggleMode.GameObject => ApplyGameObject(out message),
                ToggleMode.Component => ApplyComponent(out message),
                ToggleMode.Particle => ApplyParticle(out message),
                ToggleMode.Script => ApplyScript(out message),
                _ => Fail($"지원하지 않는 토글 모드입니다: {_mode}", out message),
            };

            if (success)
            {
                _lastExecutionSucceeded = true;
                _state = ToggleNodeState.Applied;
            }

            return success;
        }

        private bool ApplyGameObject(out string message)
        {
            if (_targetObject == null)
                return Fail("대상 GameObject가 없습니다.", out message);

            _targetObject.SetActive(_activate);
            message = $"GameObject '{_targetObject.name}'을 {GetActionLabel()} 상태로 설정했습니다.";
            return true;
        }

        private bool ApplyComponent(out string message)
        {
            if (_targetComponent == null)
                return Fail("대상 Component가 없습니다.", out message);

            if (_targetComponent is Behaviour behaviour)
            {
                behaviour.enabled = _activate;
                message = $"{GetComponentLabel(_targetComponent)} enabled를 {_activate}로 설정했습니다.";
                return true;
            }

            if (_targetComponent is Renderer renderer)
            {
                renderer.enabled = _activate;
                message = $"{GetComponentLabel(_targetComponent)} enabled를 {_activate}로 설정했습니다.";
                return true;
            }

            if (_targetComponent is Collider collider)
            {
                collider.enabled = _activate;
                message = $"{GetComponentLabel(_targetComponent)} enabled를 {_activate}로 설정했습니다.";
                return true;
            }

            return Fail($"'{_targetComponent.GetType().Name}'은 enabled를 지원하지 않습니다.", out message);
        }

        private bool ApplyParticle(out string message)
        {
            if (_targetParticle == null)
                return Fail("대상 ParticleSystem이 없습니다.", out message);

            if (_activate)
            {
                _targetParticle.Play();
                message = $"ParticleSystem '{_targetParticle.name}'을 재생했습니다.";
            }
            else
            {
                _targetParticle.Stop();
                message = $"ParticleSystem '{_targetParticle.name}'을 정지했습니다.";
            }

            return true;
        }

        private bool ApplyScript(out string message)
        {
            if (_targetScript == null)
                return Fail("대상 Script가 없습니다.", out message);

            if (_targetScript is not IToggleable toggleable)
                return Fail($"'{_targetScript.GetType().Name}'은 IToggleable을 구현하지 않습니다.", out message);

            if (_activate)
            {
                toggleable.Go();
                message = $"Script '{_targetScript.GetType().Name}'의 Go()를 호출했습니다.";
            }
            else
            {
                toggleable.Stop();
                message = $"Script '{_targetScript.GetType().Name}'의 Stop()을 호출했습니다.";
            }

            return true;
        }

        private static bool Fail(string reason, out string message)
        {
            message = reason;
            return false;
        }

        private string GetActionLabel()
        {
            return _activate ? "ON" : "OFF";
        }

        private static string GetComponentLabel(Component component)
        {
            return $"Component '{component.GetType().Name}' on '{component.gameObject.name}'";
        }

        #endregion
    }
}
