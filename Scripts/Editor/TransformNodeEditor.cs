using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;
namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TransformNode))]
    [CanEditMultipleObjects]
    public class TransformNodeEditor : UnityEditor.Editor
    {
        // Base
        private SerializedProperty _conditionGroup;
        private SerializedProperty _target;
        private SerializedProperty _onEnd;

        // Translate
        private SerializedProperty _useTranslate;
        private SerializedProperty _translateTargetMode;
        private SerializedProperty _translateTransform;
        private SerializedProperty _translateOffset;
        private SerializedProperty _translateLocal;
        private SerializedProperty _translateMoveMode;
        private SerializedProperty _translateDuration;
        private SerializedProperty _translateCurve;
        private SerializedProperty _translateSpeed;

        // Rotate
        private SerializedProperty _useRotate;
        private SerializedProperty _rotateTargetMode;
        private SerializedProperty _rotateTransform;
        private SerializedProperty _rotateEuler;
        private SerializedProperty _rotateLocal;
        private SerializedProperty _rotateMoveMode;
        private SerializedProperty _rotateDuration;
        private SerializedProperty _rotateCurve;
        private SerializedProperty _rotateSpeed;

        // Scale
        private SerializedProperty _useScale;
        private SerializedProperty _scaleTarget;
        private SerializedProperty _scaleMoveMode;
        private SerializedProperty _scaleDuration;
        private SerializedProperty _scaleCurve;
        private SerializedProperty _scaleSpeed;

        private void OnEnable()
        {
            _conditionGroup = serializedObject.FindProperty("_conditionGroup");
            _target = serializedObject.FindProperty("_target");
            _onEnd = serializedObject.FindProperty("_onEnd");

            _useTranslate = serializedObject.FindProperty("_useTranslate");
            _translateTargetMode = serializedObject.FindProperty("_translateTargetMode");
            _translateTransform = serializedObject.FindProperty("_translateTransform");
            _translateOffset = serializedObject.FindProperty("_translateOffset");
            _translateLocal = serializedObject.FindProperty("_translateLocal");
            _translateMoveMode = serializedObject.FindProperty("_translateMoveMode");
            _translateDuration = serializedObject.FindProperty("_translateDuration");
            _translateCurve = serializedObject.FindProperty("_translateCurve");
            _translateSpeed = serializedObject.FindProperty("_translateSpeed");

            _useRotate = serializedObject.FindProperty("_useRotate");
            _rotateTargetMode = serializedObject.FindProperty("_rotateTargetMode");
            _rotateTransform = serializedObject.FindProperty("_rotateTransform");
            _rotateEuler = serializedObject.FindProperty("_rotateEuler");
            _rotateLocal = serializedObject.FindProperty("_rotateLocal");
            _rotateMoveMode = serializedObject.FindProperty("_rotateMoveMode");
            _rotateDuration = serializedObject.FindProperty("_rotateDuration");
            _rotateCurve = serializedObject.FindProperty("_rotateCurve");
            _rotateSpeed = serializedObject.FindProperty("_rotateSpeed");

            _useScale = serializedObject.FindProperty("_useScale");
            _scaleTarget = serializedObject.FindProperty("_scaleTarget");
            _scaleMoveMode = serializedObject.FindProperty("_scaleMoveMode");
            _scaleDuration = serializedObject.FindProperty("_scaleDuration");
            _scaleCurve = serializedObject.FindProperty("_scaleCurve");
            _scaleSpeed = serializedObject.FindProperty("_scaleSpeed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            ConditionGroupDrawer.Draw(_conditionGroup, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("대상", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_target, new GUIContent("오브젝트"));
            EditorGUILayout.Space(4);

            // 이동
            DrawSection("이동",
                _useTranslate, _translateTargetMode, _translateTransform,
                _translateOffset, _translateLocal, _translateMoveMode,
                _translateDuration, _translateCurve, _translateSpeed,
                "목표 Transform", "상대 오프셋", "소요 시간 (초)", "속도 (units/sec)");

            EditorGUILayout.Space(4);

            // 회전
            DrawSection("회전",
                _useRotate, _rotateTargetMode, _rotateTransform,
                _rotateEuler, _rotateLocal, _rotateMoveMode,
                _rotateDuration, _rotateCurve, _rotateSpeed,
                "목표 Transform", "상대 회전 (Euler)", "소요 시간 (초)", "속도 (deg/sec)");

            EditorGUILayout.Space(4);

            // 스케일
            DrawScaleSection();

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("완료 이벤트"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSection(string title,
            SerializedProperty useToggle, SerializedProperty targetMode,
            SerializedProperty targetTransform, SerializedProperty targetVector3,
            SerializedProperty useLocal, SerializedProperty moveMode,
            SerializedProperty duration, SerializedProperty curve,
            SerializedProperty speed,
            string transformLabel, string vector3Label,
            string durationLabel, string speedLabel)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useToggle, new GUIContent($"{title} 사용"));

            if (!useToggle.boolValue) return;

            EditorGUI.indentLevel++;

            // 목표
            EditorGUILayout.PropertyField(targetMode, new GUIContent("목표 타입"));
            EditorGUI.indentLevel++;
            if (targetMode.enumValueIndex == (int)TransformNode.TargetMode.Transform)
            {
                EditorGUILayout.PropertyField(targetTransform, new GUIContent(transformLabel));
            }
            else
            {
                EditorGUILayout.PropertyField(targetVector3, new GUIContent(vector3Label));
                EditorGUILayout.PropertyField(useLocal, new GUIContent("Local 좌표"));
            }
            EditorGUI.indentLevel--;

            // 모드
            EditorGUILayout.PropertyField(moveMode, new GUIContent("모드"));
            EditorGUI.indentLevel++;
            if (moveMode.enumValueIndex == (int)TransformNode.MoveMode.Duration)
            {
                EditorGUILayout.PropertyField(duration, new GUIContent(durationLabel));
                EditorGUILayout.PropertyField(curve, new GUIContent("이징 커브"));
            }
            else
            {
                EditorGUILayout.PropertyField(speed, new GUIContent(speedLabel));
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        private void DrawScaleSection()
        {
            EditorGUILayout.LabelField("스케일", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useScale, new GUIContent("스케일 사용"));

            if (!_useScale.boolValue) return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_scaleTarget, new GUIContent("목표 스케일"));

            // 모드
            EditorGUILayout.PropertyField(_scaleMoveMode, new GUIContent("모드"));
            EditorGUI.indentLevel++;
            if (_scaleMoveMode.enumValueIndex == (int)TransformNode.MoveMode.Duration)
            {
                EditorGUILayout.PropertyField(_scaleDuration, new GUIContent("소요 시간 (초)"));
                EditorGUILayout.PropertyField(_scaleCurve, new GUIContent("이징 커브"));
            }
            else
            {
                EditorGUILayout.PropertyField(_scaleSpeed, new GUIContent("속도 (units/sec)"));
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }
    }
}
