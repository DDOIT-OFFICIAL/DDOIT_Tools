using System.Collections.Generic;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;
namespace DDOIT.Tools.Scenario
{
    /// <summary>
    /// 외부 객체의 트리거 이벤트를 TriggerConditionNode로 전달하는 경량 헬퍼.
    /// TriggerConditionNode가 런타임에 자동으로 부착/제거한다.
    /// </summary>
    public class TriggerRelay : MonoBehaviour
    {
        private readonly List<TriggerConditionNode> _owners = new List<TriggerConditionNode>();

        public void Setup(TriggerConditionNode owner)
        {
            AddOwner(owner);
        }

        public void AddOwner(TriggerConditionNode owner)
        {
            if (owner == null || _owners.Contains(owner)) return;

            _owners.Add(owner);
        }

        public void RemoveOwner(TriggerConditionNode owner)
        {
            if (owner == null) return;

            _owners.Remove(owner);
        }

        public bool HasOwners
        {
            get
            {
                PruneOwners();
                return _owners.Count > 0;
            }
        }

        public int OwnerCount
        {
            get
            {
                PruneOwners();
                return _owners.Count;
            }
        }

        private void PruneOwners()
        {
            for (int i = _owners.Count - 1; i >= 0; i--)
            {
                if (_owners[i] == null)
                    _owners.RemoveAt(i);
            }
        }

        private TriggerConditionNode[] GetOwnerSnapshot()
        {
            PruneOwners();
            return _owners.ToArray();
        }

        private void OnTriggerEnter(Collider other)
        {
            foreach (var owner in GetOwnerSnapshot())
                owner.OnRelayTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            foreach (var owner in GetOwnerSnapshot())
                owner.OnRelayTriggerExit(other);
        }
    }
}
