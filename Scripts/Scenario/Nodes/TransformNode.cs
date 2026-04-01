using System.Collections;
using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// Transform의 이동/회전을 독립적으로 제어하는 노드.
    /// 이동과 회전은 각각 별도 코루틴으로 실행되며,
    /// _isStepCondition이 켜져 있으면 둘 다 완료되어야 조건을 충족한다.
    /// </summary>
    public class TransformNode : ScenarioNode
    {
        public enum TargetMode { Transform, Vector3 }
        public enum MoveMode { Duration, Speed }

        #region Serialized Fields

        [Header("대상")]
        [SerializeField] private Transform _target;

        // 이동
        [Header("이동")]
        [SerializeField] private bool _useTranslate = true;
        [SerializeField] private TargetMode _translateTargetMode = TargetMode.Transform;
        [SerializeField] private Transform _translateTransform;
        [SerializeField] private Vector3 _translateOffset;
        [SerializeField] private bool _translateLocal;
        [SerializeField] private MoveMode _translateMoveMode = MoveMode.Duration;
        [SerializeField] private float _translateDuration = 1f;
        [SerializeField] private AnimationCurve _translateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _translateSpeed = 5f;

        // 회전
        [Header("회전")]
        [SerializeField] private bool _useRotate;
        [SerializeField] private TargetMode _rotateTargetMode = TargetMode.Vector3;
        [SerializeField] private Transform _rotateTransform;
        [SerializeField] private Vector3 _rotateEuler;
        [SerializeField] private bool _rotateLocal;
        [SerializeField] private MoveMode _rotateMoveMode = MoveMode.Duration;
        [SerializeField] private float _rotateDuration = 1f;
        [SerializeField] private AnimationCurve _rotateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _rotateSpeed = 90f;

        #endregion

        #region Private Fields

        private Coroutine _translateCoroutine;
        private Coroutine _rotateCoroutine;
        private bool _translateDone;
        private bool _rotateDone;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            StopActiveCoroutines();

            _translateDone = !_useTranslate;
            _rotateDone = !_useRotate;

            if (!_useTranslate && !_useRotate)
            {
                if (IsStepCondition) SetConditionMet();
                return;
            }

            if (_target == null)
            {
                Debug.LogWarning($"[TransformNode] {gameObject.name}: Target이 지정되지 않았습니다");
                if (IsStepCondition) SetConditionMet();
                return;
            }

            if (_useTranslate) InitTranslate();
            if (_useRotate) InitRotate();
        }

        #endregion

        #region Translate

        private void InitTranslate()
        {
            Vector3 startPos, endPos;
            bool useLocal = false;

            if (_translateTargetMode == TargetMode.Transform)
            {
                if (_translateTransform == null)
                {
                    Debug.LogWarning($"[TransformNode] {gameObject.name}: Translate Transform이 지정되지 않았습니다");
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
                if (_translateDuration <= 0f) { OnTranslateDone(); return; }

                duration = _translateDuration;
                curve = _translateCurve;
            }
            else
            {
                if (_translateSpeed <= 0f) { OnTranslateDone(); return; }

                float dist = Vector3.Distance(startPos, endPos);
                if (dist < 0.001f) { OnTranslateDone(); return; }

                duration = dist / _translateSpeed;
                curve = AnimationCurve.Linear(0, 0, 1, 1);
            }

            _translateCoroutine = StartCoroutine(
                DoTranslate(startPos, endPos, useLocal, duration, curve));
        }

        private IEnumerator DoTranslate(Vector3 startPos, Vector3 endPos,
            bool useLocal, float duration, AnimationCurve curve)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);

                if (useLocal) _target.localPosition = pos;
                else _target.position = pos;

                yield return null;
            }

            if (useLocal) _target.localPosition = endPos;
            else _target.position = endPos;

            _translateCoroutine = null;
            OnTranslateDone();
        }

        private void OnTranslateDone()
        {
            _translateDone = true;
            CheckCompletion();
        }

        #endregion

        #region Rotate

        private void InitRotate()
        {
            Quaternion startRot, endRot;
            bool useLocal = false;

            if (_rotateTargetMode == TargetMode.Transform)
            {
                if (_rotateTransform == null)
                {
                    Debug.LogWarning($"[TransformNode] {gameObject.name}: Rotate Transform이 지정되지 않았습니다");
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
                if (_rotateDuration <= 0f) { OnRotateDone(); return; }

                duration = _rotateDuration;
                curve = _rotateCurve;
            }
            else
            {
                if (_rotateSpeed <= 0f) { OnRotateDone(); return; }

                float angle = Quaternion.Angle(startRot, endRot);
                if (angle < 0.01f) { OnRotateDone(); return; }

                duration = angle / _rotateSpeed;
                curve = AnimationCurve.Linear(0, 0, 1, 1);
            }

            _rotateCoroutine = StartCoroutine(
                DoRotate(startRot, endRot, useLocal, duration, curve));
        }

        private IEnumerator DoRotate(Quaternion startRot, Quaternion endRot,
            bool useLocal, float duration, AnimationCurve curve)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
                Quaternion rot = Quaternion.Slerp(startRot, endRot, t);

                if (useLocal) _target.localRotation = rot;
                else _target.rotation = rot;

                yield return null;
            }

            if (useLocal) _target.localRotation = endRot;
            else _target.rotation = endRot;

            _rotateCoroutine = null;
            OnRotateDone();
        }

        private void OnRotateDone()
        {
            _rotateDone = true;
            CheckCompletion();
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            StopActiveCoroutines();
        }

        #endregion

        #region Completion

        private void CheckCompletion()
        {
            if (!_translateDone || !_rotateDone) return;

            if (ScenarioManager.DebugMode)
                Debug.Log($"[TransformNode] '{gameObject.name}' 완료");

            if (IsStepCondition) SetConditionMet();
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
        }

        #endregion
    }
}
