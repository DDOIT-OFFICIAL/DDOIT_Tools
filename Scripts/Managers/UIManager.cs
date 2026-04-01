using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// UI 패널을 관리하는 싱글톤 매니저.
    /// Queue 기반 ObjectPool로 UIPanel을 관리하며,
    /// BootstrapManager에서 Initialize() 호출 후 사용 가능.
    /// </summary>
    public class UIManager : Singleton<UIManager>
    {
        #region Serialized Fields

        [Header("Pool Settings")]
        [Tooltip("풀링할 UIPanel 프리팹")]
        [SerializeField] private UIPanel _panelPrefab;

        [Tooltip("초기 풀 크기")]
        [SerializeField] private int _poolSize = 5;

        #endregion

        #region Private Fields

        private Queue<UIPanel> _pool;
        private List<UIPanel> _activePanels;

        #endregion

        #region Properties

        public bool IsReady { get; private set; }
        public bool HasActiveUI => _activePanels != null && _activePanels.Count > 0;
        public int ActiveCount => _activePanels?.Count ?? 0;

        #endregion

        #region Initialization

        /// <summary>
        /// BootstrapManager에서 호출. UIPanel 풀을 생성한다.
        /// </summary>
        public IEnumerator Initialize()
        {
            _pool = new Queue<UIPanel>();
            _activePanels = new List<UIPanel>();

            for (int i = 0; i < _poolSize; i++)
            {
                UIPanel panel = CreatePooledPanel(i);
                if (panel != null)
                    _pool.Enqueue(panel);
            }

            IsReady = true;
            yield break;
        }

        private UIPanel CreatePooledPanel(int index)
        {
            if (_panelPrefab == null)
            {
                Debug.LogError("[UIManager] Panel 프리팹이 지정되지 않았습니다.");
                return null;
            }

            UIPanel panel = Instantiate(_panelPrefab, transform);
            panel.gameObject.name = $"UIPanel_{index}";
            panel.gameObject.SetActive(false);
            return panel;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// UI 패널을 표시한다. 풀에서 UIPanel을 꺼내 데이터를 바인딩하고 활성화한다.
        /// </summary>
        /// <returns>활성화된 UIPanel. 풀이 비었으면 가장 오래된 패널을 강제 반환 후 재사용.</returns>
        public UIPanel OpenUI(UIData data)
        {
            UIPanel panel = AcquirePanel();
            if (panel == null)
            {
                Debug.LogError("[UIManager] 사용 가능한 UIPanel이 없습니다.");
                return null;
            }

            panel.Show(data);
            _activePanels.Add(panel);
            return panel;
        }

        /// <summary>
        /// UI 패널을 닫는다. 숨김 애니메이션 후 풀에 반환한다.
        /// </summary>
        public void CloseUI(UIPanel panel)
        {
            if (panel == null || !panel.IsActive) return;

            panel.Hide(() =>
            {
                _activePanels.Remove(panel);
                _pool.Enqueue(panel);
            });
        }

        /// <summary>
        /// 모든 활성 UI 패널을 닫는다.
        /// </summary>
        public void CloseAllUI()
        {
            var copy = new List<UIPanel>(_activePanels);
            foreach (var panel in copy)
                CloseUI(panel);
        }

        #endregion

        #region Private Methods

        private UIPanel AcquirePanel()
        {
            if (_pool.Count > 0)
                return _pool.Dequeue();

            return StealOldestPanel();
        }

        /// <summary>
        /// 풀이 비었을 때 가장 오래된 활성 패널을 강제 반환한다.
        /// </summary>
        private UIPanel StealOldestPanel()
        {
            if (_activePanels.Count == 0) return null;

            UIPanel oldest = _activePanels[0];
            Debug.LogWarning($"[UIManager] 풀 소진 - '{oldest.gameObject.name}' 강제 반환");

            _activePanels.RemoveAt(0);

            // 애니메이션 없이 즉시 비활성화
            oldest.Hide(() => { });
            oldest.gameObject.SetActive(false);

            return oldest;
        }

        #endregion
    }
}
