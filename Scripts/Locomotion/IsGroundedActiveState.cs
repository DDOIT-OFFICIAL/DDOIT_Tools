#if DDOIT_META_XR_AVAILABLE
using UnityEngine;

using Oculus.Interaction;

namespace DDOIT.Tools.Locomotion
{
    /// <summary>
    /// ISDK CharacterController.IsGrounded를 IActiveState로 노출.
    /// WalkingStickLocomotor의 _isGrounded slot wiring용. Sample IsGroundedActiveState 미러.
    /// </summary>
    public class IsGroundedActiveState : MonoBehaviour, IActiveState
    {
        [SerializeField]
        private Oculus.Interaction.Locomotion.CharacterController _characterController;

        public bool Active => _characterController != null && _characterController.IsGrounded;
    }
}
#endif
