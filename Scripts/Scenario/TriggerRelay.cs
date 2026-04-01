using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// 외부 객체의 OnTriggerEnter 이벤트를 TriggerConditionNode로 전달하는 경량 헬퍼.
    /// TriggerConditionNode가 런타임에 자동으로 부착/제거한다.
    /// </summary>
    public class TriggerRelay : MonoBehaviour
    {
        private TriggerConditionNode _owner;

        public void Setup(TriggerConditionNode owner)
        {
            _owner = owner;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_owner != null)
                _owner.OnRelayTriggerEnter(other);
        }
    }
}
