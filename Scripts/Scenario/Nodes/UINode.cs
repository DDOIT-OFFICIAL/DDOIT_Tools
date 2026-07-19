using UnityEngine;
using UnityEngine.Events;

using DDOIT.Tools.UI;
using DDOIT.Tools.Managers;
namespace DDOIT.Tools.Scenario.Nodes
{
    /// <summary>
    /// UIManager를 통해 UI 패널을 표시하는 노드.
    /// 버튼이 활성화된 경우, 버튼 클릭 시 조건을 충족한다.
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
        [SerializeField] private Sprite _titleIcon;

        [SerializeField] private bool _isFixed;
        [SerializeField] private UILookAtMode _lookAtMode;

        [SerializeField] private UnityEvent _onButtonA;
        [SerializeField] private UnityEvent _onButtonB;

        [SerializeField] private UnityEvent _onEnd;

        #endregion

        #region Private Fields

        private UIPanel _activePanel;
        private int _selectedButtonIndex = -1;

        #endregion

        #region Properties

        /// <summary>선택된 버튼 인덱스 (0=A, 1=B, -1=미선택).</summary>
        public int SelectedButtonIndex => _selectedButtonIndex;

        /// <summary>버튼 A 클릭 이벤트. Step.MarkConditionGroupN을 등록하여 분기 가능.</summary>
        public UnityEvent OnButtonA => _onButtonA;

        /// <summary>버튼 B 클릭 이벤트. Step.MarkConditionGroupN을 등록하여 분기 가능.</summary>
        public UnityEvent OnButtonB => _onButtonB;

        #endregion

        #region ScenarioNode

        protected override void OnInit()
        {
            _selectedButtonIndex = -1;

            if (!UIManager.HasInstance)
            {
                LogOpenFailure("UIManager instance not found.");
                return;
            }

            // 볼드 래핑 + 아이콘
            var data = _uiData;
            if (_titleBold && !string.IsNullOrEmpty(data.title))
                data.title = $"<b>{data.title}</b>";
            data.titleIcon = _titleIcon;

            _activePanel = UIManager.Instance.OpenUI(data, _theme);
            if (_activePanel == null)
            {
                LogOpenFailure("UIManager.OpenUI returned null.");
                return;
            }

            // 테마 적용
            // 배치
            _activePanel.SetPlacement(_isFixed, _isFixed ? transform : null, _lookAtMode);

            // 버튼 이벤트 연결
            if (_uiData.useButtonA || _uiData.useButtonB)
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

            _onEnd?.Invoke();

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

        private void LogOpenFailure(string reason)
        {
            Scenario scenario = GetComponentInParent<Scenario>(true);
            string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "<no scene>";
            string scenarioName = scenario != null ? scenario.name : "<no scenario>";
            string stepName = ParentStep != null ? ParentStep.name : "<no step>";

            Debug.LogError(
                $"[UINode] UI 표시 실패: {reason} " +
                $"(Scene='{sceneName}', Scenario='{scenarioName}', Step='{stepName}', UINode='{gameObject.name}')");
        }

        private bool HasButtons()
        {
            return _uiData.useButtonA || _uiData.useButtonB;
        }

        #endregion
    }
}
