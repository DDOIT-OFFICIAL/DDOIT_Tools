using UnityEngine;
using UnityEngine.Events;

namespace DDOIT.Tools
{
    /// <summary>
    /// UIManager를 통해 UI 패널을 표시하는 노드.
    /// _isStepCondition이 켜져 있고 버튼형 UI(T1C1B2)인 경우,
    /// 버튼 클릭 시 조건을 충족한다.
    /// </summary>
    public enum UILookAtMode
    {
        None,
        LookOnce,
        LookAlways,
    }

    public class UINode : ScenarioNode
    {
        #region Serialized Fields

        [SerializeField] private UIData _uiData;
        [SerializeField] private UITheme _theme;
        [SerializeField] private bool _titleBold = true;

        [SerializeField] private bool _isFixed;
        [SerializeField] private UILookAtMode _lookAtMode;

        [SerializeField] private UnityEvent _onButtonA;
        [SerializeField] private UnityEvent _onButtonB;

        #endregion

        #region Private Fields

        private UIPanel _activePanel;
        private int _selectedButtonIndex = -1;

        #endregion

        #region Properties

        /// <summary>선택된 버튼 인덱스 (0=A, 1=B, -1=미선택).</summary>
        public int SelectedButtonIndex => _selectedButtonIndex;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            _selectedButtonIndex = -1;

            if (!UIManager.HasInstance)
            {
                Debug.LogError($"[UINode] '{gameObject.name}': UIManager가 존재하지 않습니다.");
                if (IsStepCondition) SetConditionMet();
                return;
            }

            // 볼드 래핑
            var data = _uiData;
            if (_titleBold && !string.IsNullOrEmpty(data.title))
                data.title = $"<b>{data.title}</b>";

            _activePanel = UIManager.Instance.OpenUI(data);
            if (_activePanel == null)
            {
                if (IsStepCondition) SetConditionMet();
                return;
            }

            // 테마 적용
            _activePanel.ApplyTheme(_theme);

            // 배치
            _activePanel.SetPlacement(_isFixed, _isFixed ? transform : null, _lookAtMode);

            // 버튼 이벤트 연결
            if (HasButtons(_uiData.type))
            {
                _activePanel.OnButtonClicked += OnButtonClicked;
            }
        }

        protected override void OnRelease()
        {
            ClosePanel();
        }

        #endregion

        #region Lifecycle

        private void OnDisable()
        {
            ClosePanel();
        }

        #endregion

        #region Private Methods

        private void OnButtonClicked(int index)
        {
            _selectedButtonIndex = index;

            if (ScenarioManager.DebugMode)
                Debug.Log($"[UINode] '{gameObject.name}' 버튼 {index} 클릭");

            if (index == 0) _onButtonA?.Invoke();
            else _onButtonB?.Invoke();

            if (IsStepCondition)
                SetConditionMet();
        }

        private void ClosePanel()
        {
            if (_activePanel != null && UIManager.HasInstance)
            {
                UIManager.Instance.CloseUI(_activePanel);
                _activePanel = null;
            }
        }

        private static bool HasButtons(UIType type)
        {
            return type == UIType.T1C1B1 || type == UIType.T1C1B2;
        }

        #endregion
    }
}
