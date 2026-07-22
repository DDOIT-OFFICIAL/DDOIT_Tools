using UnityEditor;
using UnityEngine;

using DDOIT.Tools.Scenario;
namespace DDOIT.Tools.Editor
{
    /// <summary>
    /// 조건 그룹 드롭다운을 그리는 공용 유틸리티.
    /// 부모 Step의 그룹 수를 읽어 드롭다운 목록을 구성한다.
    /// </summary>
    public static class ConditionGroupDrawer
    {
        private const string EXECUTION_DISABLED_PROPERTY = "_executionDisabled";

        /// <summary>
        /// ScenarioNode 공통 실행 제외 토글을 그린다.
        /// </summary>
        public static bool DrawExecutionToggle(SerializedObject serializedObject, MonoBehaviour target)
        {
            var executionDisabledProp = serializedObject.FindProperty(EXECUTION_DISABLED_PROPERTY);
            if (executionDisabledProp == null)
                return false;

            EditorGUILayout.PropertyField(executionDisabledProp, new GUIContent("노드 실행 제외"));

            if (executionDisabledProp.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "이 노드는 Step 실행에서 제외됩니다. 조건 그룹, Init, Release, UINode 버튼 조건 marker도 처리하지 않습니다.",
                    MessageType.None);
            }
            else if (target != null && !target.gameObject.activeSelf)
            {
                EditorGUILayout.HelpBox(
                    "GameObject 비활성화는 실행 제외가 아닙니다. Step 시작 시 이 노드는 다시 활성화되어 실행됩니다. 임시 제외는 '노드 실행 제외'를 사용하세요.",
                    MessageType.Info);
            }

            return executionDisabledProp.boolValue;
        }

        /// <summary>
        /// 멀티 오브젝트 편집 시에는 안전한 공통 필드만 노출한다.
        /// </summary>
        public static bool DrawMultiObjectExecutionOnly(SerializedObject serializedObject)
        {
            if (!serializedObject.isEditingMultipleObjects)
                return false;

            DrawExecutionToggle(serializedObject, null);
            EditorGUILayout.HelpBox(
                "노드를 여러 개 선택한 상태입니다. 안전한 일괄 편집을 위해 '노드 실행 제외'만 표시합니다.",
                MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            return true;
        }

        /// <summary>
        /// 조건 그룹 드롭다운을 그린다.
        /// </summary>
        /// <param name="conditionGroupProp">_conditionGroup SerializedProperty (int)</param>
        /// <param name="target">노드의 MonoBehaviour (부모 Step 검색용)</param>
        public static void Draw(SerializedProperty conditionGroupProp, MonoBehaviour target)
        {
            int groupCount = GetParentGroupCount(target);

            // 드롭다운 이름 배열 구성: [없음, 그룹 1, 그룹 2, ...]
            var names = new string[groupCount + 1];
            names[0] = "없음";
            for (int i = 1; i <= groupCount; i++)
                names[i] = $"그룹 {i}";

            int current = conditionGroupProp.intValue;

            // 범위 밖이면 0으로 리셋
            if (current < 0 || current > groupCount)
                current = 0;

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("조건 그룹", current, names);
            if (EditorGUI.EndChangeCheck())
                conditionGroupProp.intValue = selected;
        }

        private static int GetParentGroupCount(MonoBehaviour target)
        {
            if (target == null) return 1;

            var step = target.GetComponentInParent<Step>();
            if (step == null) return 1;

            return Mathf.Max(0, step.ConditionGroupCount);
        }
    }
}
