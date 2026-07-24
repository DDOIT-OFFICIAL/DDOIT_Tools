using UnityEngine;
using UnityEngine.Events;

using DDOIT.Tools.Player;
namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// PlayerRig의 WalkingStick locomotion을 활성/비활성하는 보존용 시나리오 노드.
    /// 활성화 시점의 사용자 자세(HMD 높이)에 따라 stick 길이가 자동 결정됩니다.
    /// 현재 컨트롤러 중심 워크플로에서는 StepEditor의 노드 추가 메뉴에서 숨김 처리되어 있으며,
    /// 추후 핸드 이동 워크플로 재도입을 위해 런타임 호환성을 유지합니다.
    /// </summary>
    public class WalkingStickNode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private bool _enable = true;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Properties

        /// <summary>WalkingStickNode는 보존용 즉시 실행 노드이며 Step 조건 그룹에 참여하지 않는다.</summary>
        public override bool IsStepCondition => false;

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
