using System.Collections;
using UnityEngine;
using UnityEngine.Events;

using DDOIT.Tools.Settings;
using DDOIT.Tools.Managers;
using DDOIT.Tools.Player;
namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// 플레이어를 지정 위치로 텔레포트하는 노드.
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

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            if (_destination == null)
            {
                Debug.LogWarning($"[TeleportNode] '{gameObject.name}': 목적지가 없습니다.");
                _onEnd?.Invoke();
                return;
            }

            _coroutine = StartCoroutine(TeleportSequence());
        }

        protected override void OnRelease()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator TeleportSequence()
        {
            float fadeDuration = DDOITSettings.Instance != null
                ? DDOITSettings.Instance.teleportFadeDuration : 1f;
            float halfDuration = fadeDuration * 0.5f;

            // 1. Fade Out
            if (ScreenFadeManager.HasInstance)
                yield return ScreenFadeManager.Instance.FadeToBlack(halfDuration);

            // 2. Teleport
            if (PlayerController.HasInstance)
                PlayerController.Instance.Teleport(_destination.position, _destination.rotation);

            // 3. Fade In
            if (ScreenFadeManager.HasInstance)
                yield return ScreenFadeManager.Instance.FadeClear(halfDuration);

            _coroutine = null;
            _onEnd?.Invoke();
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            if (_coroutine != null)
            {
                StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        #endregion
    }
}
