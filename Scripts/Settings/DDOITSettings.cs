using UnityEngine;

namespace DDOIT.Tools.Settings
{
    /// <summary>
    /// DDOIT Tools 전역 설정 ScriptableObject.
    /// 시나리오 시스템 및 공통 설정을 관리한다.
    /// DDOITToolsWindow의 Settings 탭에서 편집 가능.
    /// </summary>
    [CreateAssetMenu(fileName = "DDOITSettings", menuName = "DDOIT/Settings")]
    public class DDOITSettings : ScriptableObject
    {
        #region Singleton Access

        private static DDOITSettings _instance;

        /// <summary>
        /// 런타임/에디터에서 DDOITSettings 에셋을 자동으로 찾아 반환한다.
        /// </summary>
        public static DDOITSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindInstance();
                return _instance;
            }
        }

        private static DDOITSettings FindInstance()
        {
            // Resources 폴더 검색
            var found = Resources.FindObjectsOfTypeAll<DDOITSettings>();
            if (found.Length > 0) return found[0];

#if UNITY_EDITOR
            // 에디터: AssetDatabase 검색
            var guids = UnityEditor.AssetDatabase.FindAssets("t:DDOITSettings");
            if (guids.Length > 0)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<DDOITSettings>(path);
            }
#endif

            return null;
        }

        #endregion

        #region Scenario Settings

        public float defaultStepWait = 0.5f;
        public float teleportFadeDuration = 1f;

        #endregion
    }
}
