using System.Collections;
using UnityEngine;
using UnityEngine.Events;

using DDOIT.Tools.Data;
using DDOIT.Tools.Managers;
namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// SoundManager를 통해 SoundDatabase에 등록된 사운드를 재생하는 노드.
    /// 조건 그룹에 속해 있으면 일반 사운드 재생 완료 시 조건을 충족한다.
    /// Loop 사운드는 자동 완료하지 않는다.
    /// </summary>
    public class SoundNode : ScenarioNode
    {
        #region Serialized Fields

        [SoundName]
        [SerializeField] private string _soundName;

        [SerializeField] private bool _stopOnRelease;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private Coroutine _coroutine;
        private SoundManager _soundManager;
        private AudioSource _activeSource;
        private bool _isReleased;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            ResetRuntimeState();

            if (string.IsNullOrWhiteSpace(_soundName))
            {
                LogPlaybackFailure("Sound name is empty.");
                return;
            }

            if (!SoundManager.HasInstance)
            {
                LogPlaybackFailure("SoundManager instance not found.");
                return;
            }

            _soundManager = SoundManager.Instance;
            if (_soundManager == null || !_soundManager.IsReady)
            {
                LogPlaybackFailure("SoundManager is not ready.");
                return;
            }

            _activeSource = _soundManager.PlaySound(_soundName, owner: gameObject);
            if (_activeSource == null || _activeSource.clip == null)
            {
                LogPlaybackFailure($"Failed to play sound '{_soundName}'.");
                ClearPlaybackReference();
                return;
            }

            if (_activeSource.loop)
            {
                if (IsStepCondition)
                    LogPlaybackFailure($"Looping sound '{_soundName}' cannot complete a Step condition automatically.");

                return;
            }

            _coroutine = StartCoroutine(WaitForAudioEnd(_activeSource.clip.length));
        }

        protected override void OnRelease()
        {
            _isReleased = true;
            StopWaitCoroutine();
            StopPlaybackIfNeeded();
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            _isReleased = true;
            StopWaitCoroutine();
            StopPlaybackIfNeeded();
        }

        #endregion

        #region Private Methods

        private IEnumerator WaitForAudioEnd(float length)
        {
            if (length > 0f)
                yield return new WaitForSeconds(length);

            _coroutine = null;

            ClearPlaybackReference();

            _onEnd?.Invoke();

            if (!_isReleased && IsStepCondition)
                SetConditionMet();
        }

        private void ResetRuntimeState()
        {
            StopWaitCoroutine();
            StopPlaybackIfNeeded();
            _isReleased = false;
        }

        private void StopWaitCoroutine()
        {
            if (_coroutine == null)
                return;

            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        private void StopPlaybackIfNeeded()
        {
            if (!_stopOnRelease || _soundManager == null || _activeSource == null)
            {
                ClearPlaybackReference();
                return;
            }

            _soundManager.StopSound(_activeSource);
            ClearPlaybackReference();
        }

        private void ClearPlaybackReference()
        {
            _activeSource = null;
            _soundManager = null;
        }

        private void LogPlaybackFailure(string reason)
        {
            string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "<no scene>";
            string stepName = ParentStep != null ? ParentStep.name : "<no step>";

            Debug.LogError(
                $"[SoundNode] Sound playback failed: {reason} " +
                $"(Scene='{sceneName}', Step='{stepName}', SoundNode='{gameObject.name}')");
        }

        #endregion
    }
}
