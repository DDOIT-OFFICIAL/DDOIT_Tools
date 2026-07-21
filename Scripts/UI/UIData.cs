using UnityEngine;
using UnityEngine.Video;

namespace DDOIT.Tools.UI
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

        /// <summary>Title 요소가 런타임에서 실제로 표시될 수 있는지.</summary>
        public bool HasVisibleTitle =>
            useTitle && (!string.IsNullOrWhiteSpace(title) || titleIcon != null);

        /// <summary>Context 요소가 런타임에서 실제로 표시될 수 있는지.</summary>
        public bool HasVisibleContext =>
            useContext && !string.IsNullOrWhiteSpace(context);

        /// <summary>Image 요소가 런타임에서 실제로 표시될 수 있는지.</summary>
        public bool HasVisibleImageA =>
            useImageA && image != null;

        /// <summary>Image 2 요소가 런타임에서 실제로 표시될 수 있는지.</summary>
        public bool HasVisibleImageSub =>
            useImageSub && imageSub != null;

        /// <summary>Video 요소가 런타임에서 실제로 표시될 수 있는지.</summary>
        public bool HasVisibleVideo =>
            useVideo && video != null;

        /// <summary>Button A 요소가 런타임에서 실제로 표시될 수 있는지.</summary>
        public bool HasVisibleButtonA => useButtonA;

        /// <summary>Button B 요소가 런타임에서 실제로 표시될 수 있는지.</summary>
        public bool HasVisibleButtonB => useButtonB;

        /// <summary>Context Sub 요소가 런타임에서 실제로 표시될 수 있는지.</summary>
        public bool HasVisibleContextSub =>
            useContextSub && !string.IsNullOrWhiteSpace(contextSub);

        /// <summary>런타임에서 실제로 표시될 콘텐츠가 하나라도 있는지.</summary>
        public bool HasVisibleContent =>
            HasVisibleTitle ||
            HasVisibleContext ||
            HasVisibleImageA ||
            HasVisibleImageSub ||
            HasVisibleVideo ||
            HasVisibleButtonA ||
            HasVisibleButtonB ||
            HasVisibleContextSub;
    }
}
