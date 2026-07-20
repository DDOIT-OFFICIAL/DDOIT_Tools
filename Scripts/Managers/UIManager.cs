using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DDOIT.Tools.UI;
using DDOIT.Tools.Utilities;
namespace DDOIT.Tools.Managers
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

        [Header("Theme Settings")]
        [SerializeField] private UITheme _defaultTheme;

        #endregion

        #region Private Fields

        private Queue<UIPanel> _pool;
        private List<UIPanel> _activePanels;
        private int _createdPanelCount;

        #endregion

        #region Properties

        public bool IsReady { get; private set; }
        public bool HasActiveUI => _activePanels != null && _activePanels.Count > 0;
        public int ActiveCount => _activePanels?.Count ?? 0;
        public UITheme DefaultTheme => _defaultTheme;

        #endregion

        #region Initialization

        /// <summary>
        /// BootstrapManager에서 호출. UIPanel 풀을 생성한다.
        /// </summary>
        public IEnumerator Initialize()
        {
            if (IsReady)
            {
                Debug.LogWarning("[UIManager] Initialize()가 이미 완료되어 중복 호출을 무시합니다.");
                ValidatePoolState("Initialize duplicate");
                yield break;
            }

            ResetRuntimeCollections();

            if (_panelPrefab == null)
            {
                Debug.LogError("[UIManager] Panel 프리팹이 지정되지 않아 초기화할 수 없습니다.");
                IsReady = false;
                yield break;
            }

            int initialPoolSize = Mathf.Max(0, _poolSize);
            if (_poolSize < 0)
                Debug.LogWarning($"[UIManager] Pool Size가 음수입니다. 0으로 보정합니다. (설정값: {_poolSize})");

            for (int i = 0; i < initialPoolSize; i++)
            {
                UIPanel panel = CreatePooledPanel();
                if (panel != null)
                    _pool.Enqueue(panel);
            }

            IsReady = true;
            ValidatePoolState("Initialize");
            yield break;
        }

        private UIPanel CreatePooledPanel()
        {
            if (_panelPrefab == null)
            {
                Debug.LogError("[UIManager] Panel 프리팹이 지정되지 않았습니다.");
                return null;
            }

            UIPanel panel = Instantiate(_panelPrefab, transform);
            panel.gameObject.name = $"UIPanel_{_createdPanelCount}";
            panel.gameObject.SetActive(false);
            _createdPanelCount++;
            return panel;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// UI 패널을 표시한다. 풀에서 UIPanel을 꺼내 데이터를 바인딩하고 활성화한다.
        /// </summary>
        /// <returns>활성화된 UIPanel. 풀이 비었으면 새 패널을 생성하여 사용.</returns>
        public UIPanel OpenUI(UIData data)
        {
            return OpenUI(data, null);
        }

        /// <summary>
        /// Opens a pooled UI panel and applies either the requested theme or the manager default theme.
        /// </summary>
        public UIPanel OpenUI(UIData data, UITheme theme)
        {
            if (!IsReady || _pool == null || _activePanels == null)
            {
                Debug.LogError("[UIManager] Initialize()가 완료되기 전에 OpenUI가 호출되었습니다.");
                return null;
            }

            if (_panelPrefab == null)
            {
                Debug.LogError("[UIManager] Panel 프리팹이 지정되지 않았습니다.");
                return null;
            }

            UIPanel panel = AcquirePanel();
            if (panel == null)
            {
                Debug.LogError("[UIManager] 사용 가능한 UIPanel이 없습니다.");
                return null;
            }

            panel.Show(data);
            panel.ApplyTheme(theme != null ? theme : _defaultTheme);

            if (!_activePanels.Contains(panel))
                _activePanels.Add(panel);
            else
                Debug.LogError($"[UIManager] 활성 목록에 이미 포함된 UIPanel입니다: {panel.gameObject.name}");

            ValidatePoolState("OpenUI");
            return panel;
        }

        /// <summary>
        /// UI 패널을 닫고 풀에 반환한다.
        /// </summary>
        public void CloseUI(UIPanel panel)
        {
            if (panel == null) return;

            if (_pool == null || _activePanels == null)
            {
                if (panel.IsActive)
                    panel.Hide();

                return;
            }

            if (!_activePanels.Contains(panel))
            {
                if (panel.IsActive)
                {
                    Debug.LogWarning($"[UIManager] 추적되지 않는 UIPanel 닫기 요청을 받았습니다: {panel.gameObject.name}");
                    panel.Hide();
                }

                ValidatePoolState("CloseUI unmanaged");
                return;
            }

            if (!panel.IsActive)
            {
                ReturnPanelToPool(panel, "CloseUI inactive");
                return;
            }

            panel.Hide(() =>
            {
                ReturnPanelToPool(panel, "CloseUI");
            });
        }

        /// <summary>
        /// 모든 활성 UI 패널을 닫는다.
        /// </summary>
        public void CloseAllUI()
        {
            if (_activePanels == null || _activePanels.Count == 0)
                return;

            var copy = new List<UIPanel>(_activePanels);
            foreach (var panel in copy)
                CloseUI(panel);

            ValidatePoolState("CloseAllUI");
        }

        #endregion

        #region Private Methods

        private UIPanel AcquirePanel()
        {
            while (_pool.Count > 0)
            {
                UIPanel panel = _pool.Dequeue();
                if (panel == null)
                {
                    Debug.LogWarning("[UIManager] Pool에서 null UIPanel을 제거했습니다.");
                    continue;
                }

                if (panel.IsActive || (_activePanels != null && _activePanels.Contains(panel)))
                {
                    Debug.LogError($"[UIManager] Pool 상태 오류: 활성 또는 추적 중인 UIPanel이 Pool에 포함되어 있습니다: {panel.gameObject.name}");
                    continue;
                }

                return panel;
            }

            UIPanel expandedPanel = CreatePooledPanel();
            if (expandedPanel != null)
            {
                Debug.Log(
                    $"[UIManager] Pool 확장 - '{expandedPanel.gameObject.name}' 생성 " +
                    $"(총 {_createdPanelCount}개)");
            }

            return expandedPanel;
        }

        private void ResetRuntimeCollections()
        {
            _pool = new Queue<UIPanel>();
            _activePanels = new List<UIPanel>();
            _createdPanelCount = 0;
        }

        private void ReturnPanelToPool(UIPanel panel, string context)
        {
            if (panel == null) return;

            _activePanels?.Remove(panel);

            if (_pool == null)
                return;

            if (_pool.Contains(panel))
            {
                Debug.LogWarning($"[UIManager] UIPanel이 이미 Pool에 포함되어 있어 중복 반환을 무시합니다: {panel.gameObject.name}");
                ValidatePoolState(context);
                return;
            }

            _pool.Enqueue(panel);
            ValidatePoolState(context);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void ValidatePoolState(string context)
        {
            if (_pool == null || _activePanels == null)
                return;

            var activeSet = new HashSet<UIPanel>();
            foreach (UIPanel panel in _activePanels)
            {
                if (panel == null)
                {
                    Debug.LogError($"[UIManager] Pool 상태 오류({context}): 활성 목록에 null UIPanel이 있습니다.");
                    continue;
                }

                if (!activeSet.Add(panel))
                    Debug.LogError($"[UIManager] Pool 상태 오류({context}): 활성 목록에 중복 UIPanel이 있습니다: {panel.gameObject.name}");
            }

            var poolSet = new HashSet<UIPanel>();
            foreach (UIPanel panel in _pool)
            {
                if (panel == null)
                {
                    Debug.LogError($"[UIManager] Pool 상태 오류({context}): Pool에 null UIPanel이 있습니다.");
                    continue;
                }

                if (!poolSet.Add(panel))
                    Debug.LogError($"[UIManager] Pool 상태 오류({context}): Pool에 중복 UIPanel이 있습니다: {panel.gameObject.name}");

                if (activeSet.Contains(panel))
                    Debug.LogError($"[UIManager] Pool 상태 오류({context}): 같은 UIPanel이 활성 목록과 Pool에 동시에 있습니다: {panel.gameObject.name}");
            }
        }

        #endregion
    }
}
