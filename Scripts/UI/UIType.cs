namespace DDOIT.Tools
{
    /// <summary>
    /// UI 패널 레이아웃 타입.
    /// T=Title, C=Context, P=Picture, V=Video, B=Button. 숫자는 해당 요소의 개수.
    /// </summary>
    public enum UIType
    {
        T1,        // Title
        C1,        // Context
        T1C1,      // Title + Context
        T1C2,      // Title + Context x2
        T1C1P1,    // Title + Context + Picture
        T1C1P2,    // Title + Context + Picture x2
        T1C1V1,    // Title + Context + Video
        T1C1B1,    // Title + Context + Button x1
        T1C1B2,    // Title + Context + Button x2
    }
}
