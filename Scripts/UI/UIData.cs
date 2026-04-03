using UnityEngine;
using UnityEngine.Video;

namespace DDOIT.Tools
{
    /// <summary>
    /// UI 패널에 표시할 콘텐츠 데이터.
    /// 활성화 플래그에 따라 사용되는 요소가 결정된다.
    /// </summary>
    [System.Serializable]
    public struct UIData
    {
        // 활성화 플래그
        public bool useTitle;
        public bool useContext;
        public bool useImageA;
        public bool useImageSub;
        public bool useVideo;
        public bool useButtonA;
        public bool useButtonB;
        public bool useContextSub;

        // 데이터
        public string title;
        public Sprite titleIcon;
        [TextArea(2, 5)] public string context;
        public Sprite image;
        public Sprite imageSub;
        public VideoClip video;
        public string buttonLabelA;
        public string buttonLabelB;
        [TextArea(2, 5)] public string contextSub;

        /// <summary>Title 외 다른 요소가 하나라도 활성화되어 있는지.</summary>
        public bool HasNonTitleElement =>
            useContext || useImageA || useImageSub || useVideo ||
            useButtonA || useButtonB || useContextSub;
    }
}
