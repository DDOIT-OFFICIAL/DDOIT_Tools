using UnityEngine;
using UnityEngine.Events;

using DDOIT.Tools.Player;
namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// PlayerRig의 WalkingStick locomotion을 활성/비활성하는 시나리오 노드.
    /// 활성화 시점의 사용자 자세(HMD 높이)에 따라 stick 길이가 자동 결정됩니다.
    /// </summary>
    public class WalkingStickNode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private bool _enable = true;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            if (!PlayerRig.HasInstance)
            {
                Debug.LogWarning(
                    $"[WalkingStickNode] '{gameObject.name}': PlayerRig instance가 없습니다. 노드를 건너뜁니다.");
                _onEnd?.Invoke();
                return;
            }

            if (_enable)
                PlayerRig.Instance.EnableWalkingStick();
            else
                PlayerRig.Instance.DisableWalkingStick();

            _onEnd?.Invoke();
        }

        protected override void OnRelease()
        {
        }

        #endregion
    }
}
