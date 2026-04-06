using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
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

        #region ScenarioNode

        protected override void OnInit()
        {
            if (_animator == null)
            {
                Debug.LogWarning($"[AnimatorNode] '{gameObject.name}': Animator가 없습니다.");
                _onEnd?.Invoke();
                return;
            }

            if (string.IsNullOrEmpty(_paramName))
            {
                Debug.LogWarning($"[AnimatorNode] '{gameObject.name}': 파라미터 이름이 비어 있습니다.");
                _onEnd?.Invoke();
                return;
            }

            switch (_paramType)
            {
                case AnimatorParamType.Trigger:
                    _animator.SetTrigger(_paramName);
                    break;
                case AnimatorParamType.Bool:
                    _animator.SetBool(_paramName, _boolValue);
                    break;
                case AnimatorParamType.Int:
                    _animator.SetInteger(_paramName, _intValue);
                    break;
                case AnimatorParamType.Float:
                    _animator.SetFloat(_paramName, _floatValue);
                    break;
            }

            _onEnd?.Invoke();
        }

        #endregion
    }
}
