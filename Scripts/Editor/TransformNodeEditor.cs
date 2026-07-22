using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario.Nodes;

namespace DDOIT.Tools.Editor
{
    [CustomEditor(typeof(TransformNode))]
    [CanEditMultipleObjects]
    public class TransformNodeEditor : UnityEditor.Editor
    {
        #region Serialized Properties

        private SerializedProperty _conditionGroup;
        private SerializedProperty _target;
        private SerializedProperty _onEnd;

        private SerializedProperty _useTranslate;
        private SerializedProperty _translateTargetMode;
        private SerializedProperty _translateTransform;
        private SerializedProperty _translateOffset;
        private SerializedProperty _translateLocal;
        private SerializedProperty _translateMoveMode;
        private SerializedProperty _translateDuration;
        private SerializedProperty _translateCurve;
        private SerializedProperty _translateSpeed;

        private SerializedProperty _useRotate;
        private SerializedProperty _rotateTargetMode;
        private SerializedProperty _rotateTransform;
        private SerializedProperty _rotateEuler;
        private SerializedProperty _rotateLocal;
        private SerializedProperty _rotateMoveMode;
        private SerializedProperty _rotateDuration;
        private SerializedProperty _rotateCurve;
        private SerializedProperty _rotateSpeed;

        private SerializedProperty _useScale;
        private SerializedProperty _scaleTarget;
        private SerializedProperty _scaleMoveMode;
        private SerializedProperty _scaleDuration;
        private SerializedProperty _scaleCurve;
        private SerializedProperty _scaleSpeed;

        #endregion

        #region Unity Lifecycle

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

        #endregion

        #region Inspector

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (ConditionGroupDrawer.DrawMultiObjectExecutionOnly(serializedObject))
                return;

            bool executionDisabled = ConditionGroupDrawer.DrawExecutionToggle(serializedObject, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            ConditionGroupDrawer.Draw(_conditionGroup, (MonoBehaviour)target);
            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField("대상", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_target, new GUIContent("대상 Transform"));
            DrawBaseWarnings(executionDisabled);
            EditorGUILayout.Space(4);

            DrawSection(
                "이동",
                _useTranslate,
                _translateTargetMode,
                _translateTransform,
                _translateOffset,
                _translateLocal,
                _translateMoveMode,
                _translateDuration,
                _translateCurve,
                _translateSpeed,
                "목표 Transform",
                "상대 이동값",
                "소요 시간 (초)",
                "속도 (units/sec)");

            EditorGUILayout.Space(4);

            DrawSection(
                "회전",
                _useRotate,
                _rotateTargetMode,
                _rotateTransform,
                _rotateEuler,
                _rotateLocal,
                _rotateMoveMode,
                _rotateDuration,
                _rotateCurve,
                _rotateSpeed,
                "목표 Transform",
                "상대 회전값 (Euler)",
                "소요 시간 (초)",
                "속도 (deg/sec)");

            EditorGUILayout.Space(4);

            DrawScaleSection();

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(_onEnd, new GUIContent("완료 이벤트"));

            DrawRuntimeStatus((TransformNode)target);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBaseWarnings(bool executionDisabled)
        {
            if (executionDisabled)
                return;

            if (_target.objectReferenceValue == null)
                EditorGUILayout.HelpBox("대상 Transform이 없으면 실행 시 Transform 처리를 할 수 없습니다.", MessageType.Warning);

            if (!_useTranslate.boolValue && !_useRotate.boolValue && !_useScale.boolValue)
                EditorGUILayout.HelpBox("이동, 회전, 스케일 항목이 모두 꺼져 있습니다.", MessageType.Info);
        }

        private void DrawSection(
            string title,
            SerializedProperty useToggle,
            SerializedProperty targetMode,
            SerializedProperty targetTransform,
            SerializedProperty targetVector3,
            SerializedProperty useLocal,
            SerializedProperty moveMode,
            SerializedProperty duration,
            SerializedProperty curve,
            SerializedProperty speed,
            string transformLabel,
            string vector3Label,
            string durationLabel,
            string speedLabel)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(useToggle, new GUIContent($"{title} 사용"));

            if (!useToggle.boolValue)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(targetMode, new GUIContent("목표 방식"));
            EditorGUI.indentLevel++;
            if (targetMode.enumValueIndex == (int)TransformNode.TargetMode.Transform)
            {
                EditorGUILayout.PropertyField(targetTransform, new GUIContent(transformLabel));
                if (title == "회전")
                    EditorGUILayout.HelpBox("Transform 목표 방식은 Quaternion Shortest로 동작합니다. 최종 자세를 목표 Transform과 같게 맞추며, 180도 회전의 방향은 보장하지 않습니다.", MessageType.Info);
                if (targetTransform.objectReferenceValue == null)
                    EditorGUILayout.HelpBox($"{title} 목표 Transform이 없으면 해당 항목은 완료 처리만 됩니다.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.PropertyField(targetVector3, new GUIContent(vector3Label));
                if (title == "회전")
                    EditorGUILayout.HelpBox("Vector3 회전은 signed relative Euler로 동작합니다. 예: (0, 180, 0)은 +Y 방향, (0, -180, 0)은 -Y 방향으로 회전합니다.", MessageType.Info);
                EditorGUILayout.PropertyField(useLocal, new GUIContent("Local 좌표"));
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(moveMode, new GUIContent("모드"));
            EditorGUI.indentLevel++;
            if (moveMode.enumValueIndex == (int)TransformNode.MoveMode.Duration)
            {
                EditorGUILayout.PropertyField(duration, new GUIContent(durationLabel));
                EditorGUILayout.PropertyField(curve, new GUIContent("이징 커브"));
                if (duration.floatValue <= 0f)
                    EditorGUILayout.HelpBox("소요 시간이 0 이하이면 목표값을 즉시 적용합니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(speed, new GUIContent(speedLabel));
                if (speed.floatValue <= 0f)
                    EditorGUILayout.HelpBox("속도가 0 이하이면 해당 항목은 이동 없이 완료 처리됩니다.", MessageType.Warning);
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        private void DrawScaleSection()
        {
            EditorGUILayout.LabelField("스케일", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_useScale, new GUIContent("스케일 사용"));

            if (!_useScale.boolValue)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_scaleTarget, new GUIContent("목표 스케일"));
            EditorGUILayout.PropertyField(_scaleMoveMode, new GUIContent("모드"));

            EditorGUI.indentLevel++;
            if (_scaleMoveMode.enumValueIndex == (int)TransformNode.MoveMode.Duration)
            {
                EditorGUILayout.PropertyField(_scaleDuration, new GUIContent("소요 시간 (초)"));
                EditorGUILayout.PropertyField(_scaleCurve, new GUIContent("이징 커브"));
                if (_scaleDuration.floatValue <= 0f)
                    EditorGUILayout.HelpBox("소요 시간이 0 이하이면 목표 스케일을 즉시 적용합니다.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(_scaleSpeed, new GUIContent("속도 (units/sec)"));
                if (_scaleSpeed.floatValue <= 0f)
                    EditorGUILayout.HelpBox("속도가 0 이하이면 스케일 변경 없이 완료 처리됩니다.", MessageType.Warning);
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel--;
        }

        private void DrawRuntimeStatus(TransformNode node)
        {
            if (!EditorApplication.isPlaying || node == null)
                return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("실행 상태", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Target", node.Target != null ? node.Target.name : "None");
            EditorGUILayout.LabelField("Running", node.IsRunning ? "True" : "False");
            EditorGUILayout.LabelField("Released", node.IsReleased ? "True" : "False");
            EditorGUILayout.LabelField("Condition Met", node.IsConditionMet ? "True" : "False");

            DrawProgress("Translate", node.UseTranslate, node.TranslateDone, node.TranslateProgress);
            DrawProgress("Rotate", node.UseRotate, node.RotateDone, node.RotateProgress);
            DrawProgress("Scale", node.UseScale, node.ScaleDone, node.ScaleProgress);

            Repaint();
        }

        private static void DrawProgress(string label, bool enabled, bool done, float progress)
        {
            if (!enabled)
                return;

            EditorGUILayout.LabelField($"{label} Done", done ? "True" : "False");
            Rect rect = GUILayoutUtility.GetRect(18f, 18f);
            EditorGUI.ProgressBar(rect, progress, $"{label} {progress * 100f:0}%");
        }

        #endregion
    }
}
