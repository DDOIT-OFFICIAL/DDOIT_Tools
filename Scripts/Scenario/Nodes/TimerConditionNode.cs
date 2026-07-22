using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// 지정 시간 경과 후 조건을 충족하는 노드.
    /// Step 조건이 꺼져 있어도 타이머 완료 시 _onEnd가 발동된다.
    /// </summary>
    public class TimerConditionNode : ScenarioNode
    {
        #region Nested Types

        public enum TimerMode
        {
            Countdown,
            CountUp
        }

        public enum TimerRecordReason
        {
            Pause,
            Release,
            Manual
        }

        public readonly struct TimerRecord
        {
            public TimerRecord(float time, TimerRecordReason reason)
            {
                Time = time;
                Reason = reason;
            }

            public float Time { get; }
            public TimerRecordReason Reason { get; }
        }

        #endregion

        #region Serialized Fields

        [SerializeField] private TimerMode _timerMode = TimerMode.Countdown;
        [SerializeField] private float _duration = 1f;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private Coroutine _coroutine;
        private readonly List<TimerRecord> _savedTimes = new List<TimerRecord>();

        private float _elapsedTime;
        private float _remainingTime;
        private bool _isRunning;
        private bool _isPaused;
        private bool _isCompleted;
        private bool _hasStarted;

        #endregion

        #region Properties

        public override bool IsStepCondition => _timerMode == TimerMode.Countdown && base.IsStepCondition;
        public TimerMode Mode => _timerMode;
        public float Duration => Mathf.Max(0f, _duration);
        public float ElapsedTime => _elapsedTime;
        public float RemainingTime => _remainingTime;
        public float CurrentTime => _timerMode == TimerMode.Countdown ? _remainingTime : _elapsedTime;
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public IReadOnlyList<TimerRecord> SavedTimes => _savedTimes;
        public bool HasSavedTime => _savedTimes.Count > 0;
        public float LastSavedTime => HasSavedTime ? _savedTimes[_savedTimes.Count - 1].Time : 0f;

        #endregion

        #region Public Methods

        /// <summary>
        /// Pauses the timer and records the current time.
        /// </summary>
        public void PauseTimer()
        {
            if (!_isRunning || _isPaused) return;

            SaveCurrentTime(TimerRecordReason.Pause);
            _isPaused = true;
        }

        /// <summary>
        /// Resumes a paused timer.
        /// </summary>
        public void ResumeTimer()
        {
            if (!_isRunning || !_isPaused) return;

            _isPaused = false;
        }

        /// <summary>
        /// Sets the paused state without changing timer values.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused)
                PauseTimer();
            else
                ResumeTimer();
        }

        /// <summary>
        /// Saves the current timer value as a manual record.
        /// </summary>
        public void SaveCurrentTime()
        {
            SaveCurrentTime(TimerRecordReason.Manual);
        }

        /// <summary>
        /// Clears saved timer records.
        /// </summary>
        public void ClearSavedTimes()
        {
            _savedTimes.Clear();
        }

        /// <summary>
        /// Stops the timer and resets the current value for the selected mode.
        /// </summary>
        public void ResetTimer()
        {
            StopTimerCoroutine();
            ResetRuntimeState();
        }

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            PrepareTimerStart();

            if (_timerMode == TimerMode.Countdown && Duration <= 0f)
            {
                CompleteCountdown();
                return;
            }

            _coroutine = StartCoroutine(RunTimer());
        }

        protected override void OnRelease()
        {
            if (_timerMode == TimerMode.CountUp && _hasStarted)
                SaveCurrentTime(TimerRecordReason.Release);

            ResetTimer();
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            StopTimerCoroutine();
            _isRunning = false;
            _isPaused = false;
        }

        #endregion

        #region Private Methods

        private IEnumerator RunTimer()
        {
            while (_isRunning)
            {
                if (!_isPaused)
                {
                    float deltaTime = Time.deltaTime;
                    _elapsedTime += deltaTime;

                    if (_timerMode == TimerMode.Countdown)
                    {
                        _remainingTime = Mathf.Max(0f, Duration - _elapsedTime);

                        if (_elapsedTime >= Duration)
                        {
                            CompleteCountdown();
                            yield break;
                        }
                    }
                }

                yield return null;
            }
        }

        private void PrepareTimerStart()
        {
            StopTimerCoroutine();

            _elapsedTime = 0f;
            _remainingTime = _timerMode == TimerMode.Countdown ? Duration : 0f;
            _isRunning = true;
            _isPaused = false;
            _isCompleted = false;
            _hasStarted = true;
        }

        private void CompleteCountdown()
        {
            if (_isCompleted) return;

            _isCompleted = true;
            _isRunning = false;
            _isPaused = false;
            _elapsedTime = Duration;
            _remainingTime = 0f;
            _coroutine = null;

            _onEnd?.Invoke();

            if (IsStepCondition) SetConditionMet();
        }

        private void SaveCurrentTime(TimerRecordReason reason)
        {
            _savedTimes.Add(new TimerRecord(CurrentTime, reason));
        }

        private void StopTimerCoroutine()
        {
            if (_coroutine == null) return;

            StopCoroutine(_coroutine);
            _coroutine = null;
        }

        private void ResetRuntimeState()
        {
            _elapsedTime = 0f;
            _remainingTime = _timerMode == TimerMode.Countdown ? Duration : 0f;
            _isRunning = false;
            _isPaused = false;
            _isCompleted = false;
            _hasStarted = false;
        }

        #endregion
    }
}
