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

        #region ScenarioNode

        protected override void OnInit()
        {
            switch (_mode)
            {
                case ToggleMode.GameObject:
                    if (_targetObject != null)
                        _targetObject.SetActive(_activate);
                    else
                        Debug.LogWarning($"[ToggleNode] '{gameObject.name}': 대상 GameObject가 없습니다.");
                    break;

                case ToggleMode.Component:
                    if (_targetComponent != null)
                    {
                        if (_targetComponent is Behaviour behaviour)
                            behaviour.enabled = _activate;
                        else if (_targetComponent is Renderer renderer)
                            renderer.enabled = _activate;
                        else if (_targetComponent is Collider collider)
                            collider.enabled = _activate;
                        else
                            Debug.LogWarning($"[ToggleNode] '{gameObject.name}': '{_targetComponent.GetType().Name}'은 enabled를 지원하지 않습니다.");
                    }
                    else
                    {
                        Debug.LogWarning($"[ToggleNode] '{gameObject.name}': 대상 Component가 없습니다.");
                    }
                    break;

                case ToggleMode.Particle:
                    if (_targetParticle != null)
                    {
                        if (_activate)
                            _targetParticle.Play();
                        else
                            _targetParticle.Stop();
                    }
                    else
                    {
                        Debug.LogWarning($"[ToggleNode] '{gameObject.name}': 대상 ParticleSystem이 없습니다.");
                    }
                    break;

                case ToggleMode.Script:
                    if (_targetScript is IToggleable toggleable)
                    {
                        if (_activate)
                            toggleable.Go();
                        else
                            toggleable.Stop();
                    }
                    else if (_targetScript != null)
                    {
                        Debug.LogWarning($"[ToggleNode] '{gameObject.name}': '{_targetScript.GetType().Name}'은 IToggleable을 구현하지 않습니다.");
                    }
                    else
                    {
                        Debug.LogWarning($"[ToggleNode] '{gameObject.name}': 대상 Script가 없습니다.");
                    }
                    break;
            }

            _onEnd?.Invoke();
        }

        #endregion
    }
}
