using UnityEngine;
using UnityEngine.Video;

namespace DDOIT.Tools
{
    /// <summary>
    /// UI 패널에 표시할 콘텐츠 데이터.
    /// UIType에 따라 사용되는 필드가 달라진다.
    /// </summary>
    [System.Serializable]
    public struct UIData
    {
        [Tooltip("UI 레이아웃 타입")]
        public UIType type;

        [Tooltip("제목 텍스트")]
        public string title;

        [Tooltip("본문 텍스트")]
        [TextArea(2, 5)] public string context;

        [Tooltip("두 번째 본문 텍스트 (T1C2)")]
        [TextArea(2, 5)] public string contextSub;

        [Tooltip("이미지 (T1C1P1, T1C1P2)")]
        public Sprite image;

        [Tooltip("두 번째 이미지 (T1C1P2)")]
        public Sprite imageSub;

        [Tooltip("비디오 클립 (T1C1V1)")]
        public VideoClip video;

        [Tooltip("버튼 A 텍스트 (T1C1B2)")]
        public string buttonLabelA;

        [Tooltip("버튼 B 텍스트 (T1C1B2)")]
        public string buttonLabelB;
    }
}
