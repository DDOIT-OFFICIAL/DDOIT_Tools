using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// UI 테마 (지역 설정). 테마별로 다른 색상을 정의한다.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme_New", menuName = "DDOIT/UI Theme")]
    public class UITheme : ScriptableObject
    {
        public Color backgroundColorTop = new Color(0.17f, 0.17f, 0.17f, 1f);
        public Color backgroundColorBottom = new Color(0.1f, 0.1f, 0.1f, 1f);
        public Color edgeColor = Color.white;
        public Color textColor = Color.white;
    }
}
