using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

using DDOIT.Tools.Scenario.Nodes;
using DDOIT.Tools.Player;
namespace DDOIT.Tools.UI
{
    /// <summary>
    /// 풀링되는 UI 패널 단위. 독립 Canvas(World Space)를 가지며,
    /// DesignPanel(비주얼 컨테이너)과 모든 데이터 요소를 내장한다.
    /// 활성화 플래그에 따라 필요한 요소만 활성화하고 데이터를 바인딩한다.
    /// OVROverlayCanvas(default order=0)와 같은 frame에서 일관된 pose를 보장하기 위해
    /// ExecutionOrder -100 + Update phase에서 LookAt Slerp 처리
    /// (1-frame stale로 인한 간헐적 잔상 방지).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class UIPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("비주얼 컨테이너")]
        [Tooltip("스케일 애니메이션 대상 (배경 포함)")]
        [SerializeField] private RectTransform _designPanel;

        [Header("콘텐츠 요소")]
        [SerializeField] private Image _titleIcon;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _contextText;
        [SerializeField] private TMP_Text _contextSubText;
        [SerializeField] private Image _imageA;
        [SerializeField] private Image _imageSub;
        [SerializeField] private RawImage _videoSurface;
        [SerializeField] private VideoPlayer _videoPlayer;
        [SerializeField] private Button _buttonA;
        [SerializeField] private Button _buttonB;
        [SerializeField] private TMP_Text _buttonLabelA;
        [SerializeField] private TMP_Text _buttonLabelB;
        [SerializeField] private Image _buttonABackground;
        [SerializeField] private Image _buttonAEdge;
        [SerializeField] private Image _buttonBBackground;
        [SerializeField] private Image _buttonBEdge;

        [Header("디자인 요소")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _edgeImage;
        [SerializeField] private Image _logoImage;
        [SerializeField] private GameObject _titleContextSplitter;

        [Header("배치")]
        [SerializeField] private SmoothFollowCanvas _smoothFollow;

        #endregion

        #region Constants

        private const float LOOK_AT_SPEED = 5f;

        #endregion

        #region Private Fields

        private bool _isActive;
        private bool _lookAtPlayer;
        private Transform _playerTransform;
        private Material _bgMaterialInstance;

        #endregion

        #region Properties

        public bool IsActive => _isActive;

        #endregion

        #region Events

        /// <summary>
        /// 버튼 클릭 시 발생. 인자: 버튼 인덱스 (0=A, 1=B).
        /// </summary>
        public event Action<int> OnButtonClicked;

        #endregion

        #region Public Methods

        /// <summary>
        /// UI 패널을 표시한다. 데이터에 따라 요소를 활성화하고 애니메이션을 재생한다.
        /// </summary>
        public void Show(UIData data)
        {
            _isActive = true;
            gameObject.SetActive(true);

            ConfigureElements(data);
            BindData(data);
            HideEmptyElements(data);
            SetupButtons(data);

            // 레이아웃 강제 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(_designPanel);
        }

        /// <summary>
        /// UI 패널을 숨긴다. 축소 애니메이션 후 콜백을 호출한다.
        /// </summary>
        public void Hide(Action onComplete = null)
        {
            if (!_isActive)
            {
                onComplete?.Invoke();
                return;
            }

            _isActive = false;
            Cleanup();
            gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        /// <summary>
        /// 배치 모드를 설정한다.
        /// </summary>
        /// <param name="isFixed">true: 월드 고정, false: SmoothFollow</param>
        /// <param name="anchor">고정 시 위치/회전 기준 Transform (UINode의 Transform)</param>
        /// <param name="lookAtMode">플레이어 바라보기 모드</param>
        public void SetPlacement(bool isFixed, Transform anchor, UILookAtMode lookAtMode)
        {
            if (isFixed)
            {
                if (_smoothFollow != null)
                    _smoothFollow.enabled = false;

                if (anchor != null)
                {
                    transform.position = anchor.position;
                    transform.rotation = anchor.rotation;
                }

                Transform playerT = null;
                if (lookAtMode != UILookAtMode.None && PlayerRig.HasInstance)
                    playerT = PlayerRig.Instance.HeadTransform;

                if (playerT != null && lookAtMode == UILookAtMode.LookOnce)
                {
                    // 1회 즉시 회전 — Canvas 앞면(-Z 반대)이 Player를 향하게 forward를 -dir로
                    Vector3 dir = playerT.position - transform.position;
                    if (dir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(-dir);
                }

                _lookAtPlayer = lookAtMode == UILookAtMode.LookAlways;
                _playerTransform = _lookAtPlayer ? playerT : null;
            }
            else
            {
                _lookAtPlayer = false;
                _playerTransform = null;

                if (_smoothFollow != null)
                {
                    var eyeAnchor = FindCenterEyeAnchor();
                    if (eyeAnchor != null)
                        _smoothFollow.SetTarget(eyeAnchor);
                    else if (PlayerRig.HasInstance)
                        _smoothFollow.SetTarget(PlayerRig.Instance.HeadTransform);

                    _smoothFollow.enabled = true;
                }
            }
        }

        /// <summary>
        /// 테마 색상을 적용한다. 런타임에 UINode가 호출.
        /// </summary>
        public void ApplyTheme(UITheme theme)
        {
            if (theme == null) return;

            // 배경 그라디언트 (머티리얼 인스턴스 — 패널 전용)
            if (_backgroundImage != null && _backgroundImage.material != null)
            {
                if (_bgMaterialInstance == null)
                    _bgMaterialInstance = new Material(_backgroundImage.material);

                _bgMaterialInstance.SetColor("_ColorTop", theme.backgroundColorTop);
                _bgMaterialInstance.SetColor("_ColorBottom", theme.backgroundColorBottom);
                _backgroundImage.material = _bgMaterialInstance;
            }

            // 버튼 background는 단색 — gradient의 top 색상 사용
            if (_buttonABackground != null) _buttonABackground.color = theme.backgroundColorTop;
            if (_buttonBBackground != null) _buttonBBackground.color = theme.backgroundColorTop;

            if (_edgeImage != null)
                _edgeImage.color = theme.edgeColor;

            // Logo, Button Edge는 Edge 색상을 따라감
            if (_logoImage != null)
                _logoImage.color = theme.edgeColor;

            if (_buttonAEdge != null) _buttonAEdge.color = theme.edgeColor;
            if (_buttonBEdge != null) _buttonBEdge.color = theme.edgeColor;

            // 텍스트 색상
            if (_titleText != null)
                _titleText.color = theme.textColor;

            if (_contextText != null)
                _contextText.color = theme.textColor;

            if (_contextSubText != null)
                _contextSubText.color = theme.textColor;

            if (_buttonLabelA != null) _buttonLabelA.color = theme.textColor;
            if (_buttonLabelB != null) _buttonLabelB.color = theme.textColor;

            // Splitter, TitleIcon은 텍스트 색상을 따라감
            if (_titleContextSplitter != null)
            {
                var splitterImage = _titleContextSplitter.GetComponent<Image>();
                if (splitterImage != null)
                    splitterImage.color = theme.textColor;
            }

            if (_titleIcon != null)
                _titleIcon.color = theme.textColor;
        }

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!_lookAtPlayer || _playerTransform == null) return;

            Vector3 direction = _playerTransform.position - transform.position;
            if (direction.sqrMagnitude < 0.001f) return;

            // Canvas 앞면이 Player를 향하게 forward를 -direction으로
            Quaternion targetRot = Quaternion.LookRotation(-direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, LOOK_AT_SPEED * Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
        }

        #endregion

        #region Private Methods - Element Configuration

        private void ConfigureElements(UIData data)
        {
            SetElementActive(_titleText, data.useTitle);
            SetElementActive(_contextText, data.useContext);
            SetElementActive(_imageA, data.useImageA);
            SetElementActive(_imageSub, data.useImageSub);
            SetElementActive(_videoSurface, data.useVideo);
            SetElementActive(_buttonA, data.useButtonA);
            SetElementActive(_buttonB, data.useButtonB);
            SetElementActive(_contextSubText, data.useContextSub);

            // Splitter: Title이 켜진 상태에서 다른 요소가 하나라도 있으면 활성화
            SetGameObjectActive(_titleContextSplitter, data.useTitle && data.HasNonTitleElement);
        }

        private static void SetElementActive(Component element, bool active)
        {
            if (element != null)
                element.gameObject.SetActive(active);
        }

        private static void SetGameObjectActive(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }

        /// <summary>
        /// 활성화된 요소 중 데이터가 비어있으면 숨긴다.
        /// </summary>
        private void HideEmptyElements(UIData data)
        {
            if (_titleText != null && _titleText.gameObject.activeSelf && string.IsNullOrEmpty(data.title))
                _titleText.gameObject.SetActive(false);

            if (_contextText != null && _contextText.gameObject.activeSelf && string.IsNullOrEmpty(data.context))
                _contextText.gameObject.SetActive(false);

            if (_contextSubText != null && _contextSubText.gameObject.activeSelf && string.IsNullOrEmpty(data.contextSub))
                _contextSubText.gameObject.SetActive(false);

            if (_imageA != null && _imageA.gameObject.activeSelf && data.image == null)
                _imageA.gameObject.SetActive(false);

            if (_imageSub != null && _imageSub.gameObject.activeSelf && data.imageSub == null)
                _imageSub.gameObject.SetActive(false);

            if (_videoSurface != null && _videoSurface.gameObject.activeSelf && data.video == null)
                _videoSurface.gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods - Data Binding

        private void BindData(UIData data)
        {
            // Title Icon
            if (_titleIcon != null)
            {
                bool hasIcon = data.titleIcon != null;
                _titleIcon.gameObject.SetActive(hasIcon);
                if (hasIcon)
                    _titleIcon.sprite = data.titleIcon;
            }

            if (_titleText != null)
                _titleText.text = data.title;

            if (_contextText != null)
                _contextText.text = data.context;

            if (_contextSubText != null)
                _contextSubText.text = data.contextSub;

            if (_imageA != null && data.image != null)
                _imageA.sprite = data.image;

            if (_imageSub != null && data.imageSub != null)
                _imageSub.sprite = data.imageSub;

            if (_videoPlayer != null && _videoSurface != null && data.video != null)
            {
                _videoPlayer.clip = data.video;
                _videoPlayer.Play();
            }
        }

        private void SetupButtons(UIData data)
        {
            if (_buttonA != null)
            {
                _buttonA.onClick.RemoveAllListeners();
                _buttonA.onClick.AddListener(() => OnButtonClicked?.Invoke(0));

                if (_buttonLabelA != null)
                    _buttonLabelA.text = data.buttonLabelA;
            }

            if (_buttonB != null)
            {
                _buttonB.onClick.RemoveAllListeners();
                _buttonB.onClick.AddListener(() => OnButtonClicked?.Invoke(1));

                if (_buttonLabelB != null)
                    _buttonLabelB.text = data.buttonLabelB;
            }
        }

        private void Cleanup()
        {
            OnButtonClicked = null;

            if (_buttonA != null)
                _buttonA.onClick.RemoveAllListeners();

            if (_buttonB != null)
                _buttonB.onClick.RemoveAllListeners();

            if (_videoPlayer != null && _videoPlayer.isPlaying)
                _videoPlayer.Stop();

            _lookAtPlayer = false;
            _playerTransform = null;

            if (_bgMaterialInstance != null)
            {
                Destroy(_bgMaterialInstance);
                _bgMaterialInstance = null;
            }

            if (_smoothFollow != null)
                _smoothFollow.enabled = false;
        }

        /// <summary>
        /// OVRCameraRig의 CenterEyeAnchor를 찾는다.
        /// </summary>
        private static Transform FindCenterEyeAnchor()
        {
            var cameraRigType = FindType("OVRCameraRig");
            if (cameraRigType == null)
                return null;

            var cameraRig = FindFirstObjectByType(cameraRigType);
            if (cameraRig == null)
                return null;

            var field = cameraRigType.GetField("centerEyeAnchor");
            if (field != null)
                return field.GetValue(cameraRig) as Transform;

            var property = cameraRigType.GetProperty("centerEyeAnchor");
            return property != null ? property.GetValue(cameraRig) as Transform : null;
        }

        private static Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }

        #endregion
    }
}
