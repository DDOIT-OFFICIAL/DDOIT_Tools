using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// Animator 파라미터를 설정하는 노드.
    /// </summary>
    public enum AnimatorParamType
    {
        Trigger,
        Bool,
        Int,
        Float,
    }

    public class AnimatorNode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private Animator _animator;

        [SerializeField] private AnimatorParamType _paramType = AnimatorParamType.Trigger;
        [SerializeField] private string _paramName;
        [SerializeField] private bool _boolValue = true;
        [SerializeField] private int _intValue;
        [SerializeField] private float _floatValue;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private bool _lastExecutionSucceeded;
        private string _lastExecutionMessage = "Not executed.";

        #endregion

        #region Properties

        /// <summary>AnimatorNode is an immediate execution node and never participates in Step condition groups.</summary>
        public override bool IsStepCondition => false;
        public Animator Animator => _animator;
        public AnimatorParamType ParamType => _paramType;
        public string ParamName => _paramName;
        public bool LastExecutionSucceeded => _lastExecutionSucceeded;
        public string LastExecutionMessage => _lastExecutionMessage;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            if (!TryApplyParameter(out string message))
                Debug.LogWarning($"[AnimatorNode] '{gameObject.name}': {message}");

            _lastExecutionMessage = message;
            _onEnd?.Invoke();
        }

        #endregion

        #region Private Methods

        private bool TryApplyParameter(out string message)
        {
            _lastExecutionSucceeded = false;

            if (_animator == null)
            {
                message = "Animator가 없습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_paramName))
            {
                message = "파라미터 이름이 비어 있습니다.";
                return false;
            }

            AnimatorControllerParameter parameter = FindParameter(_paramName);
            if (parameter == null)
            {
                message = $"Animator에 '{_paramName}' 파라미터가 없습니다.";
                return false;
            }

            AnimatorControllerParameterType expectedType = ToUnityParamType(_paramType);
            if (parameter.type != expectedType)
            {
                message = $"'{_paramName}' 파라미터 타입이 일치하지 않습니다. 현재: {parameter.type}, 필요: {expectedType}.";
                return false;
            }

            ApplyParameter(parameter.nameHash);
            _lastExecutionSucceeded = true;
            message = $"'{_paramName}' {expectedType} 파라미터를 적용했습니다.";
            return true;
        }

        private AnimatorControllerParameter FindParameter(string paramName)
        {
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.name == paramName)
                    return parameter;
            }

            return null;
        }

        private void ApplyParameter(int paramHash)
        {
            switch (_paramType)
            {
                case AnimatorParamType.Trigger:
                    _animator.SetTrigger(paramHash);
                    break;
                case AnimatorParamType.Bool:
                    _animator.SetBool(paramHash, _boolValue);
                    break;
                case AnimatorParamType.Int:
                    _animator.SetInteger(paramHash, _intValue);
                    break;
                case AnimatorParamType.Float:
                    _animator.SetFloat(paramHash, _floatValue);
                    break;
            }
        }

        private static AnimatorControllerParameterType ToUnityParamType(AnimatorParamType type)
        {
            return type switch
            {
                AnimatorParamType.Trigger => AnimatorControllerParameterType.Trigger,
                AnimatorParamType.Bool => AnimatorControllerParameterType.Bool,
                AnimatorParamType.Int => AnimatorControllerParameterType.Int,
                AnimatorParamType.Float => AnimatorControllerParameterType.Float,
                _ => AnimatorControllerParameterType.Trigger,
            };
        }

        #endregion
    }
}
