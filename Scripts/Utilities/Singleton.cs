using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// 씬 전환시 파괴되는 제네릭 싱글톤 베이스 클래스
    /// </summary>
    /// <typeparam name="T">싱글톤으로 만들 클래스 타입</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Private Fields
        private static T _instance;
        private static bool _isQuitting = false;
        #endregion

        #region Public Properties
        /// <summary>
        /// 인스턴스가 존재하는지 확인합니다. (새 인스턴스를 생성하지 않음)
        /// OnDestroy에서 싱글톤에 접근할 때 사용하세요.
        /// </summary>
        public static bool HasInstance => (object)_instance != null && _instance;

        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject($"{typeof(T).Name} (Singleton)");
                        _instance = singletonObject.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// 싱글톤 중복 방지 처리
        /// </summary>
        protected virtual void Awake()
        {
            // 에디터에서 Play 모드 재진입 시 플래그 초기화
            _isQuitting = false;

            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
        #endregion
    }
}
