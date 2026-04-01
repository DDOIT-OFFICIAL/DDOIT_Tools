using TMPro;
using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// UI 전역 설정. 모든 Theme에 공통 적용되는 레이아웃/폰트 설정.
    /// 프로젝트당 하나만 존재한다.
    /// </summary>
    [CreateAssetMenu(fileName = "UIGlobalSettings", menuName = "DDOIT/UI Global Settings")]
    public class UIGlobalSettings : ScriptableObject
    {
        public float panelWidth = 800f;

        public int paddingLeft = 40;
        public int paddingRight = 40;
        public int paddingTop = 40;
        public int paddingBottom = 40;
        public float spacing = 20f;

        public TMP_FontAsset titleFont;
        public float titleFontSize = 48f;

        public TMP_FontAsset contextFont;
        public float contextFontSize = 32f;
    }
}
