using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// SoundManager를 통해 오디오를 재생하는 노드.
    /// SoundDatabase에 등록된 soundName으로 재생한다.
    /// 조건 그룹에 속해 있으면 재생 완료 시 조건을 충족한다.
    /// </summary>
    public class SoundNode : ScenarioNode
    {
        #region Serialized Fields

        [SoundName]
        [SerializeField] private string _soundName;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private Coroutine _coroutine;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            SoundManager.Instance.PlaySound(_soundName);
            _coroutine = StartCoroutine(WaitForAudioEnd());
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

        #region Private Methods

        private IEnumerator WaitForAudioEnd()
        {
            float length = SoundManager.Instance.GetSoundLength(_soundName);
            yield return new WaitForSeconds(length);
            _coroutine = null;
            _onEnd?.Invoke();
            SetConditionMet();
        }

        #endregion
    }
}
