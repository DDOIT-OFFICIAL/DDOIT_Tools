using System.Collections.Generic;
using UnityEngine;

namespace DDOIT.Tools.UI
{
    /// <summary>
    /// Renders a world-space canvas into a texture and displays that texture on a
    /// late-rendered mesh. This keeps DDOIT UI visible over scene geometry while
    /// allowing hand, controller, and ray visuals to render above it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(RectTransform))]
    public class InSceneOverlayCanvasRenderer : MonoBehaviour
    {
        #region Constants

        private const string SHADER_NAME = "DDOIT/UI In-Scene Overlay";
        private const string DISPLAY_OBJECT_NAME = "__DDOIT_InSceneOverlay_Display";
        private const string CAMERA_OBJECT_NAME = "__DDOIT_InSceneOverlay_Camera";
        private const float CAMERA_DISTANCE = 1f;
        private const int DEFAULT_SOURCE_LAYER = 30;
        private const int DEFAULT_DISPLAY_LAYER = 0;
        private const int DEFAULT_DISPLAY_RENDER_QUEUE = 4500;
        private static readonly int MainTexShaderID = Shader.PropertyToID("_MainTex");

        #endregion

        #region Serialized Fields

        [Header("Canvas")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private RectTransform _rectTransform;

        [Header("Render Texture")]
        [SerializeField, Min(1)] private int _renderScale = 2;
        [SerializeField, Min(128)] private int _maxTextureSize = 2048;
        [SerializeField] private bool _redrawEveryFrame = true;
        [SerializeField, Min(1)] private int _renderInterval = 1;

        [Header("Layers")]
        [SerializeField, Range(0, 31)] private int _sourceLayer = DEFAULT_SOURCE_LAYER;
        [SerializeField, Range(0, 31)] private int _displayLayer = DEFAULT_DISPLAY_LAYER;
        [SerializeField] private bool _excludeSourceLayerFromSceneCameras = true;

        [Header("Display")]
        [SerializeField] private int _displayRenderQueue = DEFAULT_DISPLAY_RENDER_QUEUE;
        [SerializeField] private bool _disableOvrOverlayCanvas = true;

        #endregion

        #region Private Fields

        private readonly Dictionary<Transform, int> _originalLayers = new Dictionary<Transform, int>();
        private readonly Vector3[] _worldCorners = new Vector3[4];

        private Camera _renderCamera;
        private RenderTexture _renderTexture;
        private Material _displayMaterial;
        private Mesh _displayMesh;
        private MeshRenderer _displayRenderer;
        private MeshFilter _displayFilter;
        private Transform _displayTransform;
        private Transform _cameraTransform;
        private int _lastRenderedFrame = -1;
        private bool _dirty = true;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheComponents();
            DisableOvrOverlayCanvasIfPresent();
        }

        private void OnEnable()
        {
            CacheComponents();
            DisableOvrOverlayCanvasIfPresent();
            EnsureRuntimeObjects();
            ApplySourceLayers();
            ExcludeSourceLayerFromSceneCameras();
            MarkDirty();
        }

        private void LateUpdate()
        {
            if (_excludeSourceLayerFromSceneCameras)
                ExcludeSourceLayerFromSceneCameras();

            EnsureRuntimeObjects();
            UpdateRuntimeTransforms();

            if (ShouldRenderThisFrame())
                RenderCanvas();

            InteractionVisualRenderOrder.ApplyAboveOverlay(_displayRenderQueue + 100);
        }

        private void OnDisable()
        {
            RestoreOriginalLayers();
            ReleaseRuntimeObjects();
        }

        private void OnDestroy()
        {
            ReleaseRuntimeObjects();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Schedules the canvas texture to be refreshed on the next LateUpdate.
        /// </summary>
        public void MarkDirty()
        {
            _dirty = true;
        }

        #endregion

        #region Runtime Setup

        private void CacheComponents()
        {
            if (_canvas == null)
                _canvas = GetComponent<Canvas>();

            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
        }

        private void EnsureRuntimeObjects()
        {
            EnsureRenderTexture();
            EnsureDisplayObjects();
            EnsureRenderCamera();
        }

        private void EnsureRenderTexture()
        {
            Vector2Int size = CalculateTextureSize();
            if (_renderTexture != null &&
                _renderTexture.width == size.x &&
                _renderTexture.height == size.y)
            {
                return;
            }

            if (_renderTexture != null)
                _renderTexture.Release();

            _renderTexture = new RenderTexture(
                size.x,
                size.y,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                name = $"{name}_InSceneOverlayRT",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            _renderTexture.Create();

            if (_displayMaterial != null)
                _displayMaterial.SetTexture(MainTexShaderID, _renderTexture);

            if (_renderCamera != null)
                _renderCamera.targetTexture = _renderTexture;

            MarkDirty();
        }

        private void EnsureDisplayObjects()
        {
            if (_displayTransform == null)
            {
                GameObject displayObject = new GameObject(DISPLAY_OBJECT_NAME);
                _displayTransform = displayObject.transform;
                _displayTransform.SetParent(transform, false);
                _displayTransform.localPosition = Vector3.zero;
                _displayTransform.localRotation = Quaternion.identity;
                _displayTransform.localScale = Vector3.one;
            }

            _displayTransform.gameObject.layer = _displayLayer;

            if (_displayFilter == null)
                _displayFilter = GetOrAddComponent<MeshFilter>(_displayTransform.gameObject);

            if (_displayRenderer == null)
                _displayRenderer = GetOrAddComponent<MeshRenderer>(_displayTransform.gameObject);

            if (_displayMesh == null)
            {
                _displayMesh = new Mesh { name = $"{name}_InSceneOverlayMesh" };
                _displayMesh.MarkDynamic();
            }

            _displayFilter.sharedMesh = _displayMesh;

            if (_displayMaterial == null)
            {
                Shader shader = Shader.Find(SHADER_NAME);
                if (shader == null)
                    shader = Shader.Find("Unlit/Transparent");

                _displayMaterial = new Material(shader)
                {
                    name = $"{name}_InSceneOverlayMaterial",
                    renderQueue = _displayRenderQueue
                };
            }

            _displayMaterial.renderQueue = _displayRenderQueue;
            _displayMaterial.SetTexture(MainTexShaderID, _renderTexture);
            _displayRenderer.sharedMaterial = _displayMaterial;
            _displayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _displayRenderer.receiveShadows = false;
            _displayRenderer.allowOcclusionWhenDynamic = false;
        }

        private void EnsureRenderCamera()
        {
            if (_cameraTransform == null)
            {
                GameObject cameraObject = new GameObject(CAMERA_OBJECT_NAME);
                _cameraTransform = cameraObject.transform;
                _cameraTransform.SetParent(transform, false);
            }

            if (_renderCamera == null)
                _renderCamera = GetOrAddComponent<Camera>(_cameraTransform.gameObject);

            _cameraTransform.gameObject.layer = _sourceLayer;

            _renderCamera.enabled = false;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
                _renderCamera.stereoTargetEye = StereoTargetEyeMask.None;

            _renderCamera.clearFlags = CameraClearFlags.SolidColor;
            _renderCamera.backgroundColor = Color.clear;
            _renderCamera.orthographic = true;
            _renderCamera.nearClipPlane = CAMERA_DISTANCE - 0.01f;
            _renderCamera.farClipPlane = CAMERA_DISTANCE + 0.01f;
            _renderCamera.cullingMask = 1 << _sourceLayer;
            _renderCamera.allowHDR = false;
            _renderCamera.allowMSAA = true;
            _renderCamera.targetTexture = _renderTexture;
        }

        #endregion

        #region Rendering

        private void UpdateRuntimeTransforms()
        {
            if (_rectTransform == null || _renderCamera == null || _displayMesh == null)
                return;

            _rectTransform.GetWorldCorners(_worldCorners);

            Vector3 center = (_worldCorners[0] + _worldCorners[1] + _worldCorners[2] + _worldCorners[3]) * 0.25f;
            Vector3 forward = transform.forward;
            _cameraTransform.position = center - forward * CAMERA_DISTANCE;
            _cameraTransform.rotation = transform.rotation;

            float worldHeight = Vector3.Distance(_worldCorners[0], _worldCorners[1]);
            float worldWidth = Vector3.Distance(_worldCorners[0], _worldCorners[3]);
            _renderCamera.orthographicSize = Mathf.Max(worldHeight * 0.5f, 0.0001f);
            _renderCamera.aspect = Mathf.Max(worldWidth / Mathf.Max(worldHeight, 0.0001f), 0.0001f);

            UpdateDisplayMesh();
        }

        private void UpdateDisplayMesh()
        {
            Rect rect = _rectTransform.rect;
            float left = rect.xMin;
            float right = rect.xMax;
            float bottom = rect.yMin;
            float top = rect.yMax;

            var vertices = new[]
            {
                new Vector3(left, bottom, 0f),
                new Vector3(left, top, 0f),
                new Vector3(right, top, 0f),
                new Vector3(right, bottom, 0f)
            };

            var uvs = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };

            _displayMesh.Clear();
            _displayMesh.vertices = vertices;
            _displayMesh.uv = uvs;
            _displayMesh.triangles = new[] { 0, 1, 2, 2, 3, 0 };
            _displayMesh.RecalculateBounds();
        }

        private bool ShouldRenderThisFrame()
        {
            if (_renderCamera == null || _renderTexture == null)
                return false;

            if (_redrawEveryFrame)
                return Time.frameCount != _lastRenderedFrame &&
                       Time.frameCount % Mathf.Max(1, _renderInterval) == 0;

            return _dirty;
        }

        private void RenderCanvas()
        {
            Canvas.ForceUpdateCanvases();
            _renderCamera.Render();
            _lastRenderedFrame = Time.frameCount;
            _dirty = false;
        }

        private Vector2Int CalculateTextureSize()
        {
            if (_rectTransform == null)
                return new Vector2Int(512, 512);

            Rect rect = _rectTransform.rect;
            int width = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(rect.width) * _renderScale));
            int height = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(rect.height) * _renderScale));
            int maxSide = Mathf.Max(width, height);

            if (maxSide > _maxTextureSize)
            {
                float scale = _maxTextureSize / (float)maxSide;
                width = Mathf.Max(1, Mathf.RoundToInt(width * scale));
                height = Mathf.Max(1, Mathf.RoundToInt(height * scale));
            }

            return new Vector2Int(width, height);
        }

        #endregion

        #region Layer Management

        private void ApplySourceLayers()
        {
            _originalLayers.Clear();
            ApplySourceLayerRecursive(transform);

            if (_displayTransform != null)
                _displayTransform.gameObject.layer = _displayLayer;
        }

        private void ApplySourceLayerRecursive(Transform target)
        {
            if (target == _displayTransform)
                return;

            if (!_originalLayers.ContainsKey(target))
                _originalLayers.Add(target, target.gameObject.layer);

            target.gameObject.layer = _sourceLayer;

            for (int i = 0; i < target.childCount; i++)
                ApplySourceLayerRecursive(target.GetChild(i));
        }

        private void RestoreOriginalLayers()
        {
            foreach (KeyValuePair<Transform, int> entry in _originalLayers)
            {
                if (entry.Key != null)
                    entry.Key.gameObject.layer = entry.Value;
            }

            _originalLayers.Clear();
        }

        private void ExcludeSourceLayerFromSceneCameras()
        {
            int sourceMask = 1 << _sourceLayer;
            Camera[] cameras = Camera.allCameras;

            for (int i = 0; i < cameras.Length; i++)
            {
                Camera camera = cameras[i];
                if (camera == null || camera == _renderCamera || camera.targetTexture != null)
                    continue;

                if ((camera.cullingMask & sourceMask) != 0)
                    camera.cullingMask &= ~sourceMask;
            }
        }

        #endregion

        #region Cleanup

        private void ReleaseRuntimeObjects()
        {
            if (_renderCamera != null)
                _renderCamera.targetTexture = null;

            DestroyRuntimeObject(_renderTexture);
            DestroyRuntimeObject(_displayMaterial);
            DestroyRuntimeObject(_displayMesh);

            _renderTexture = null;
            _displayMaterial = null;
            _displayMesh = null;

            if (_displayTransform != null)
                DestroyRuntimeObject(_displayTransform.gameObject);

            if (_cameraTransform != null)
                DestroyRuntimeObject(_cameraTransform.gameObject);

            _displayTransform = null;
            _cameraTransform = null;
            _displayRenderer = null;
            _displayFilter = null;
            _renderCamera = null;
        }

        private static void DestroyRuntimeObject(Object obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
                component = target.AddComponent<T>();

            return component;
        }

        private void DisableOvrOverlayCanvasIfPresent()
        {
            if (!_disableOvrOverlayCanvas)
                return;

            Component[] components = GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component == this)
                    continue;

                if (component.GetType().Name != "OVROverlayCanvas")
                    continue;

                if (component is Behaviour behaviour)
                    behaviour.enabled = false;
            }
        }

        #endregion
    }
}
