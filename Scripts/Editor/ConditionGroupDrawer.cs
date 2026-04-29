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
