namespace DDOIT.Tools
{
    /// <summary>
    /// ToggleNode의 Script 모드에서 호출되는 인터페이스.
    /// 커스텀 동작 스크립트에 구현하면 ToggleNode에서 Go/Stop 제어가 가능하다.
    /// </summary>
    public interface IToggleable
    {
        void Go();
        void Stop();
    }
}
