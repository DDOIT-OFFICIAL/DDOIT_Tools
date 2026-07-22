using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// Controls a target Transform's position, rotation, and scale over time.
    /// </summary>
    public class TransformNode : ScenarioNode
    {
        public enum TargetMode { Transform, Vector3 }
        public enum MoveMode { Duration, Speed }

        #region Serialized Fields

        [SerializeField] private Transform _target;

        [SerializeField] private bool _useTranslate = true;
        [SerializeField] private TargetMode _translateTargetMode = TargetMode.Transform;
        [SerializeField] private Transform _translateTransform;
        [SerializeField] private Vector3 _translateOffset;
        [SerializeField] private bool _translateLocal;
        [SerializeField] private MoveMode _translateMoveMode = MoveMode.Duration;
        [SerializeField] private float _translateDuration = 1f;
        [SerializeField] private AnimationCurve _translateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _translateSpeed = 5f;

        [SerializeField] private bool _useRotate;
        [SerializeField] private TargetMode _rotateTargetMode = TargetMode.Vector3;
        [SerializeField] private Transform _rotateTransform;
        [SerializeField] private Vector3 _rotateEuler;
        [SerializeField] private bool _rotateLocal;
        [SerializeField] private MoveMode _rotateMoveMode = MoveMode.Duration;
        [SerializeField] private float _rotateDuration = 1f;
        [SerializeField] private AnimationCurve _rotateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _rotateSpeed = 90f;

        [SerializeField] private bool _useScale;
        [SerializeField] private Vector3 _scaleTarget = Vector3.one;
        [SerializeField] private MoveMode _scaleMoveMode = MoveMode.Duration;
        [SerializeField] private float _scaleDuration = 1f;
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _scaleSpeed = 5f;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private Coroutine _translateCoroutine;
        private Coroutine _rotateCoroutine;
        private Coroutine _scaleCoroutine;
        private bool _translateDone;
        private bool _rotateDone;
        private bool _scaleDone;
        private bool _isRunning;
        private bool _isReleased = true;
        private bool _completionInvoked;
        private float _translateProgress;
        private float _rotateProgress;
        private float _scaleProgress;

        #endregion

        #region Properties

        public Transform Target => _target;
        public bool UseTranslate => _useTranslate;
        public bool UseRotate => _useRotate;
        public bool UseScale => _useScale;
        public bool TranslateDone => _translateDone;
        public bool RotateDone => _rotateDone;
        public bool ScaleDone => _scaleDone;
        public bool IsRunning => _isRunning;
        public bool IsReleased => _isReleased;
        public float TranslateProgress => _useTranslate ? Mathf.Clamp01(_translateProgress) : 1f;
        public float RotateProgress => _useRotate ? Mathf.Clamp01(_rotateProgress) : 1f;
        public float ScaleProgress => _useScale ? Mathf.Clamp01(_scaleProgress) : 1f;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            StopActiveCoroutines();

            _isReleased = false;
            _completionInvoked = false;
            _translateDone = !_useTranslate;
            _rotateDone = !_useRotate;
            _scaleDone = !_useScale;
            _translateProgress = _useTranslate ? 0f : 1f;
            _rotateProgress = _useRotate ? 0f : 1f;
            _scaleProgress = _useScale ? 0f : 1f;
            _isRunning = _useTranslate || _useRotate || _useScale;

            if (!_useTranslate && !_useRotate && !_useScale)
            {
                _isRunning = false;
                if (IsStepCondition)
                    SetConditionMet();
                return;
            }

            if (_target == null)
            {
                Debug.LogWarning($"[TransformNode] {gameObject.name}: Target is not assigned.");
                _isRunning = false;
                if (IsStepCondition)
                    SetConditionMet();
                return;
            }

            if (_useTranslate) InitTranslate();
            if (_useRotate) InitRotate();
            if (_useScale) InitScale();
        }

        protected override void OnRelease()
        {
            CancelRuntime();
        }

        #endregion

        #region Translate

        private void InitTranslate()
        {
            Vector3 startPos;
            Vector3 endPos;
            bool useLocal = false;

            if (_translateTargetMode == TargetMode.Transform)
            {
                if (_translateTransform == null)
                {
                    Debug.LogWarning($"[TransformNode] {gameObject.name}: Translate Transform is not assigned.");
                    OnTranslateDone();
                    return;
                }

                startPos = _target.position;
                endPos = _translateTransform.position;
            }
            else
            {
                useLocal = _translateLocal;
                startPos = useLocal ? _target.localPosition : _target.position;
                endPos = startPos + _translateOffset;
            }

            float duration;
            AnimationCurve curve;

            if (_translateMoveMode == MoveMode.Duration)
            {
                if (_translateDuration <= 0f)
                {
                    if (useLocal) _target.localPosition = endPos;
                    else          _target.position = endPos;

                    _translateProgress = 1f;
                    OnTranslateDone();
                    return;
                }

                duration = _translateDuration;
                curve = _translateCurve;
            }
            else
            {
                if (_translateSpeed <= 0f)
                {
                    _translateProgress = 1f;
                    OnTranslateDone();
                    return;
                }

                float distance = Vector3.Distance(startPos, endPos);
                if (distance < 0.001f)
                {
                    _translateProgress = 1f;
                    OnTranslateDone();
                    return;
                }

                duration = distance / _translateSpeed;
                curve = AnimationCurve.Linear(0, 0, 1, 1);
            }

            _translateCoroutine = StartCoroutine(DoTranslate(startPos, endPos, useLocal, duration, curve));
        }

        private IEnumerator DoTranslate(Vector3 startPos, Vector3 endPos, bool useLocal, float duration, AnimationCurve curve)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ShouldCancelRuntime())
                {
                    _translateCoroutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                _translateProgress = Mathf.Clamp01(elapsed / duration);
                float t = curve.Evaluate(_translateProgress);
                Vector3 position = Vector3.Lerp(startPos, endPos, t);

                if (useLocal) _target.localPosition = position;
                else          _target.position = position;

                yield return null;
            }

            if (ShouldCancelRuntime())
            {
                _translateCoroutine = null;
                yield break;
            }

            if (useLocal) _target.localPosition = endPos;
            else          _target.position = endPos;

            _translateCoroutine = null;
            _translateProgress = 1f;
            OnTranslateDone();
        }

        private void OnTranslateDone()
        {
            if (_isReleased) return;

            _translateDone = true;
            _translateProgress = 1f;
            CheckCompletion();
        }

        #endregion

        #region Rotate

        private void InitRotate()
        {
            Quaternion startRot;
            Quaternion endRot;
            bool useLocal = false;
            bool useSignedEuler = _rotateTargetMode == TargetMode.Vector3;

            if (_rotateTargetMode == TargetMode.Transform)
            {
                if (_rotateTransform == null)
                {
                    Debug.LogWarning($"[TransformNode] {gameObject.name}: Rotate Transform is not assigned.");
                    OnRotateDone();
                    return;
                }

                startRot = _target.rotation;
                endRot = _rotateTransform.rotation;
            }
            else
            {
                useLocal = _rotateLocal;
                startRot = useLocal ? _target.localRotation : _target.rotation;
                endRot = startRot * Quaternion.Euler(_rotateEuler);
            }

            float duration;
            AnimationCurve curve;

            if (_rotateMoveMode == MoveMode.Duration)
            {
                if (_rotateDuration <= 0f)
                {
                    if (useLocal) _target.localRotation = endRot;
                    else          _target.rotation = endRot;

                    _rotateProgress = 1f;
                    OnRotateDone();
                    return;
                }

                duration = _rotateDuration;
                curve = _rotateCurve;
            }
            else
            {
                if (_rotateSpeed <= 0f)
                {
                    _rotateProgress = 1f;
                    OnRotateDone();
                    return;
                }

                float angle = useSignedEuler ? GetSignedEulerMagnitude(_rotateEuler) : Quaternion.Angle(startRot, endRot);
                if (angle < 0.01f)
                {
                    _rotateProgress = 1f;
                    OnRotateDone();
                    return;
                }

                duration = angle / _rotateSpeed;
                curve = AnimationCurve.Linear(0, 0, 1, 1);
            }

            _rotateCoroutine = useSignedEuler
                ? StartCoroutine(DoRotateSignedEuler(startRot, _rotateEuler, useLocal, duration, curve))
                : StartCoroutine(DoRotateShortest(startRot, endRot, useLocal, duration, curve));
        }

        private IEnumerator DoRotateShortest(Quaternion startRot, Quaternion endRot, bool useLocal, float duration, AnimationCurve curve)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ShouldCancelRuntime())
                {
                    _rotateCoroutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                _rotateProgress = Mathf.Clamp01(elapsed / duration);
                float t = curve.Evaluate(_rotateProgress);
                Quaternion rotation = Quaternion.Slerp(startRot, endRot, t);

                if (useLocal) _target.localRotation = rotation;
                else          _target.rotation = rotation;

                yield return null;
            }

            if (ShouldCancelRuntime())
            {
                _rotateCoroutine = null;
                yield break;
            }

            if (useLocal) _target.localRotation = endRot;
            else          _target.rotation = endRot;

            _rotateCoroutine = null;
            _rotateProgress = 1f;
            OnRotateDone();
        }

        private IEnumerator DoRotateSignedEuler(Quaternion startRot, Vector3 signedEuler, bool useLocal, float duration, AnimationCurve curve)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ShouldCancelRuntime())
                {
                    _rotateCoroutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                _rotateProgress = Mathf.Clamp01(elapsed / duration);
                float t = curve.Evaluate(_rotateProgress);
                Quaternion rotation = startRot * Quaternion.Euler(signedEuler * t);

                if (useLocal) _target.localRotation = rotation;
                else          _target.rotation = rotation;

                yield return null;
            }

            if (ShouldCancelRuntime())
            {
                _rotateCoroutine = null;
                yield break;
            }

            Quaternion endRot = startRot * Quaternion.Euler(signedEuler);
            if (useLocal) _target.localRotation = endRot;
            else          _target.rotation = endRot;

            _rotateCoroutine = null;
            _rotateProgress = 1f;
            OnRotateDone();
        }

        private static float GetSignedEulerMagnitude(Vector3 signedEuler)
        {
            return Mathf.Max(Mathf.Abs(signedEuler.x), Mathf.Abs(signedEuler.y), Mathf.Abs(signedEuler.z));
        }

        private void OnRotateDone()
        {
            if (_isReleased) return;

            _rotateDone = true;
            _rotateProgress = 1f;
            CheckCompletion();
        }

        #endregion

        #region Scale

        private void InitScale()
        {
            Vector3 startScale = _target.localScale;
            Vector3 endScale = _scaleTarget;

            float duration;
            AnimationCurve curve;

            if (_scaleMoveMode == MoveMode.Duration)
            {
                if (_scaleDuration <= 0f)
                {
                    _target.localScale = endScale;
                    _scaleProgress = 1f;
                    OnScaleDone();
                    return;
                }

                duration = _scaleDuration;
                curve = _scaleCurve;
            }
            else
            {
                if (_scaleSpeed <= 0f)
                {
                    _scaleProgress = 1f;
                    OnScaleDone();
                    return;
                }

                float distance = Vector3.Distance(startScale, endScale);
                if (distance < 0.001f)
                {
                    _scaleProgress = 1f;
                    OnScaleDone();
                    return;
                }

                duration = distance / _scaleSpeed;
                curve = AnimationCurve.Linear(0, 0, 1, 1);
            }

            _scaleCoroutine = StartCoroutine(DoScale(startScale, endScale, duration, curve));
        }

        private IEnumerator DoScale(Vector3 startScale, Vector3 endScale, float duration, AnimationCurve curve)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (ShouldCancelRuntime())
                {
                    _scaleCoroutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                _scaleProgress = Mathf.Clamp01(elapsed / duration);
                float t = curve.Evaluate(_scaleProgress);
                _target.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            if (ShouldCancelRuntime())
            {
                _scaleCoroutine = null;
                yield break;
            }

            _target.localScale = endScale;
            _scaleCoroutine = null;
            _scaleProgress = 1f;
            OnScaleDone();
        }

        private void OnScaleDone()
        {
            if (_isReleased) return;

            _scaleDone = true;
            _scaleProgress = 1f;
            CheckCompletion();
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            CancelRuntime();
        }

        #endregion

        #region Completion

        private void CheckCompletion()
        {
            if (_isReleased || _completionInvoked) return;
            if (!_translateDone || !_rotateDone || !_scaleDone) return;

            _completionInvoked = true;
            _isRunning = false;

            if (ScenarioManager.DebugMode)
                Debug.Log($"[TransformNode] '{gameObject.name}' completed.");

            _onEnd?.Invoke();
            if (IsStepCondition)
                SetConditionMet();
        }

        private void CancelRuntime()
        {
            _isReleased = true;
            _isRunning = false;
            StopActiveCoroutines();
        }

        private bool ShouldCancelRuntime()
        {
            return _isReleased || _target == null;
        }

        private void StopActiveCoroutines()
        {
            if (_translateCoroutine != null)
            {
                StopCoroutine(_translateCoroutine);
                _translateCoroutine = null;
            }

            if (_rotateCoroutine != null)
            {
                StopCoroutine(_rotateCoroutine);
                _rotateCoroutine = null;
            }

            if (_scaleCoroutine != null)
            {
                StopCoroutine(_scaleCoroutine);
                _scaleCoroutine = null;
            }
        }

        #endregion
    }
}
