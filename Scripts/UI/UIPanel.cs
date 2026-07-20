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
        [Tooltip("레이아웃 갱신 대상 비주얼 컨테이너")]
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

        [Header("Rendering")]
        [SerializeField] private InSceneOverlayCanvasRenderer _overlayRenderer;

        #endregion

        #region Constants

        private const float LOOK_AT_SPEED = 5f;
        private const int DEFAULT_VIDEO_TEXTURE_WIDTH = 1280;
        private const int DEFAULT_VIDEO_TEXTURE_HEIGHT = 720;
        private const int MAX_VIDEO_TEXTURE_SIZE = 2048;
        private const int MIN_VIDEO_TEXTURE_SIZE = 16;

        #endregion

        #region Private Fields

        private bool _isActive;
        private bool _lookAtPlayer;
        private Transform _playerTransform;
        private Material _bgMaterialInstance;
        private bool _themeDefaultsCached;
        private Material _defaultBackgroundMaterial;
        private Color _defaultBackgroundColor;
        private Color _defaultButtonABackgroundColor;
        private Color _defaultButtonAEdgeColor;
        private Color _defaultButtonBBackgroundColor;
        private Color _defaultButtonBEdgeColor;
        private Color _defaultEdgeColor;
        private Color _defaultLogoColor;
        private Color _defaultTitleIconColor;
        private Color _defaultTitleTextColor;
        private Color _defaultContextTextColor;
        private Color _defaultContextSubTextColor;
        private Color _defaultButtonLabelAColor;
        private Color _defaultButtonLabelBColor;
        private Color _defaultTitleContextSplitterColor;
        private Image _titleContextSplitterImage;
        private RenderTexture _videoRenderTexture;
        private bool _hasButtonSelection;

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
        /// UI 패널을 표시한다. 데이터에 따라 요소를 활성화하고 값을 바인딩한다.
        /// </summary>
        public void Show(UIData data)
        {
            CacheOverlayRenderer();
            _isActive = true;
            _hasButtonSelection = false;
            gameObject.SetActive(true);
            ResetThemeVisualState();
            ResetVideoState();

            ConfigureElements(data);
            BindData(data);
            HideEmptyElements(data);
            SetupButtons(data);

            // 레이아웃 강제 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(_designPanel);
            _overlayRenderer?.MarkDirty();
        }

        /// <summary>
        /// UI 패널을 숨기고 콜백을 호출한다.
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
            ResetThemeVisualState();
            if (theme == null) return;

            // 배경 그라디언트 (머티리얼 인스턴스 — 패널 전용)
            Material sourceMaterial = _defaultBackgroundMaterial != null
                ? _defaultBackgroundMaterial
                : _backgroundImage != null ? _backgroundImage.material : null;
            if (_backgroundImage != null && sourceMaterial != null)
            {
                _bgMaterialInstance = new Material(sourceMaterial);

                SetMaterialColor(_bgMaterialInstance, "_ColorTop", theme.backgroundColorTop);
                SetMaterialColor(_bgMaterialInstance, "_ColorBottom", theme.backgroundColorBottom);
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
                var splitterImage = GetTitleContextSplitterImage();
                if (splitterImage != null)
                    splitterImage.color = theme.textColor;
            }

            if (_titleIcon != null)
                _titleIcon.color = theme.textColor;

            _overlayRenderer?.MarkDirty();
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheOverlayRenderer();
            CacheThemeDefaults();
        }

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
            ResetVideoState();
        }

        private void OnDestroy()
        {
            ResetVideoState();
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

            BindVideo(data.useVideo ? data.video : null);
        }

        private void SetupButtons(UIData data)
        {
            SetupButton(_buttonA, _buttonLabelA, data.buttonLabelA, data.useButtonA, 0);
            SetupButton(_buttonB, _buttonLabelB, data.buttonLabelB, data.useButtonB, 1);
        }

        private void SetupButton(Button button, TMP_Text label, string labelText, bool enabled, int index)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = enabled;

                if (enabled)
                    button.onClick.AddListener(() => HandleButtonClicked(index));
            }

            if (label != null)
                label.text = labelText;
        }

        private void HandleButtonClicked(int index)
        {
            if (!_isActive || _hasButtonSelection)
                return;

            _hasButtonSelection = true;
            SetButtonsInteractable(false);
            OnButtonClicked?.Invoke(index);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_buttonA != null)
                _buttonA.interactable = interactable;

            if (_buttonB != null)
                _buttonB.interactable = interactable;

            _overlayRenderer?.MarkDirty();
        }

        private void BindVideo(VideoClip videoClip)
        {
            if (videoClip == null || _videoPlayer == null || _videoSurface == null)
                return;

            EnsureVideoRenderTexture(videoClip);
            if (_videoRenderTexture == null)
                return;

            _videoPlayer.Stop();
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.targetTexture = _videoRenderTexture;
            _videoPlayer.clip = videoClip;
            _videoSurface.texture = _videoRenderTexture;
            _videoPlayer.Play();
            _overlayRenderer?.MarkDirty();
        }

        private void EnsureVideoRenderTexture(VideoClip videoClip)
        {
            Vector2Int size = CalculateVideoTextureSize(videoClip);
            if (_videoRenderTexture != null &&
                _videoRenderTexture.width == size.x &&
                _videoRenderTexture.height == size.y &&
                _videoRenderTexture.IsCreated())
            {
                return;
            }

            ReleaseVideoRenderTexture();

            _videoRenderTexture = new RenderTexture(
                size.x,
                size.y,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                name = $"{name}_VideoRT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            _videoRenderTexture.Create();
        }

        private static Vector2Int CalculateVideoTextureSize(VideoClip videoClip)
        {
            int width = videoClip != null ? (int)videoClip.width : 0;
            int height = videoClip != null ? (int)videoClip.height : 0;

            if (width <= 0)
                width = DEFAULT_VIDEO_TEXTURE_WIDTH;

            if (height <= 0)
                height = DEFAULT_VIDEO_TEXTURE_HEIGHT;

            int maxDimension = Mathf.Max(width, height);
            if (maxDimension > MAX_VIDEO_TEXTURE_SIZE)
            {
                float scale = MAX_VIDEO_TEXTURE_SIZE / (float)maxDimension;
                width = Mathf.RoundToInt(width * scale);
                height = Mathf.RoundToInt(height * scale);
            }

            width = Mathf.Max(MIN_VIDEO_TEXTURE_SIZE, width);
            height = Mathf.Max(MIN_VIDEO_TEXTURE_SIZE, height);
            return new Vector2Int(width, height);
        }

        private void ResetVideoState()
        {
            if (_videoPlayer != null)
            {
                if (_videoPlayer.isPlaying)
                    _videoPlayer.Stop();

                _videoPlayer.targetTexture = null;
                _videoPlayer.clip = null;
            }

            if (_videoSurface != null)
                _videoSurface.texture = null;

            ReleaseVideoRenderTexture();
            _overlayRenderer?.MarkDirty();
        }

        private void ReleaseVideoRenderTexture()
        {
            if (_videoRenderTexture == null)
                return;

            if (_videoRenderTexture.IsCreated())
                _videoRenderTexture.Release();

            if (Application.isPlaying)
                Destroy(_videoRenderTexture);
            else
                DestroyImmediate(_videoRenderTexture);

            _videoRenderTexture = null;
        }

        private void CacheThemeDefaults()
        {
            if (_themeDefaultsCached) return;

            _defaultBackgroundMaterial = _backgroundImage != null ? _backgroundImage.material : null;
            _defaultBackgroundColor = GetImageColor(_backgroundImage);
            _defaultButtonABackgroundColor = GetImageColor(_buttonABackground);
            _defaultButtonAEdgeColor = GetImageColor(_buttonAEdge);
            _defaultButtonBBackgroundColor = GetImageColor(_buttonBBackground);
            _defaultButtonBEdgeColor = GetImageColor(_buttonBEdge);
            _defaultEdgeColor = GetImageColor(_edgeImage);
            _defaultLogoColor = GetImageColor(_logoImage);
            _defaultTitleIconColor = GetImageColor(_titleIcon);
            _defaultTitleTextColor = GetTextColor(_titleText);
            _defaultContextTextColor = GetTextColor(_contextText);
            _defaultContextSubTextColor = GetTextColor(_contextSubText);
            _defaultButtonLabelAColor = GetTextColor(_buttonLabelA);
            _defaultButtonLabelBColor = GetTextColor(_buttonLabelB);
            _defaultTitleContextSplitterColor = GetImageColor(GetTitleContextSplitterImage());
            _themeDefaultsCached = true;
        }

        private void ResetThemeVisualState()
        {
            CacheThemeDefaults();
            ReleaseThemeMaterialInstance();

            if (_backgroundImage != null)
            {
                _backgroundImage.material = _defaultBackgroundMaterial;
                _backgroundImage.color = _defaultBackgroundColor;
            }

            SetImageColor(_buttonABackground, _defaultButtonABackgroundColor);
            SetImageColor(_buttonAEdge, _defaultButtonAEdgeColor);
            SetImageColor(_buttonBBackground, _defaultButtonBBackgroundColor);
            SetImageColor(_buttonBEdge, _defaultButtonBEdgeColor);
            SetImageColor(_edgeImage, _defaultEdgeColor);
            SetImageColor(_logoImage, _defaultLogoColor);
            SetImageColor(_titleIcon, _defaultTitleIconColor);
            SetImageColor(GetTitleContextSplitterImage(), _defaultTitleContextSplitterColor);

            SetTextColor(_titleText, _defaultTitleTextColor);
            SetTextColor(_contextText, _defaultContextTextColor);
            SetTextColor(_contextSubText, _defaultContextSubTextColor);
            SetTextColor(_buttonLabelA, _defaultButtonLabelAColor);
            SetTextColor(_buttonLabelB, _defaultButtonLabelBColor);

            _overlayRenderer?.MarkDirty();
        }

        private void ReleaseThemeMaterialInstance()
        {
            if (_bgMaterialInstance == null) return;

            if (Application.isPlaying)
                Destroy(_bgMaterialInstance);
            else
                DestroyImmediate(_bgMaterialInstance);

            _bgMaterialInstance = null;
        }

        private Image GetTitleContextSplitterImage()
        {
            if (_titleContextSplitterImage == null && _titleContextSplitter != null)
                _titleContextSplitterImage = _titleContextSplitter.GetComponent<Image>();

            return _titleContextSplitterImage;
        }

        private static Color GetImageColor(Image image)
        {
            return image != null ? image.color : Color.white;
        }

        private static Color GetTextColor(TMP_Text text)
        {
            return text != null ? text.color : Color.white;
        }

        private static void SetImageColor(Image image, Color color)
        {
            if (image != null)
                image.color = color;
        }

        private static void SetTextColor(TMP_Text text, Color color)
        {
            if (text != null)
                text.color = color;
        }

        private static void SetMaterialColor(Material material, string propertyName, Color color)
        {
            if (material != null && material.HasProperty(propertyName))
                material.SetColor(propertyName, color);
        }

        private void Cleanup()
        {
            _hasButtonSelection = false;
            OnButtonClicked = null;

            if (_buttonA != null)
                _buttonA.onClick.RemoveAllListeners();

            if (_buttonB != null)
                _buttonB.onClick.RemoveAllListeners();

            ResetVideoState();

            _lookAtPlayer = false;
            _playerTransform = null;

            ResetThemeVisualState();

            if (_smoothFollow != null)
                _smoothFollow.enabled = false;
        }

        private void CacheOverlayRenderer()
        {
            if (_overlayRenderer == null)
                _overlayRenderer = GetComponent<InSceneOverlayCanvasRenderer>();
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
