using System.Collections;
using UnityEngine;

namespace DDOIT.Tools
{
    /// <summary>
    /// VR 카메라 앞 Quad 기반 화면 페이드/탈채도 매니저.
    /// 임의의 GameObject에 부착하고, Inspector에서 카메라 Transform을 지정한다.
    /// Quad는 런타임에 카메라 하위에 자동 생성된다.
    /// </summary>
    public class ScreenFadeManager : Singleton<ScreenFadeManager>
    {
        #region Serialized Fields

        [Header("카메라")]
        [Tooltip("Quad를 붙일 카메라 Transform (미지정 시 Camera.main 사용)")]
        [SerializeField] private Transform _cameraTransform;

        [Header("설정")]
        [Tooltip("기본 페이드 시간 (초)")]
        [SerializeField] private float _defaultFadeDuration = 0.5f;

        [Tooltip("셰이더 렌더 큐 (높을수록 나중에 렌더)")]
        [SerializeField] private int _renderQueue = 5000;

        #endregion

        #region Properties

        public float CurrentFadeAlpha { get; private set; }
        public float CurrentDesaturation { get; private set; }
        public bool IsFading { get; private set; }

        #endregion

        #region Private Fields

        private GameObject _fadeQuad;
        private MeshRenderer _meshRenderer;
        private Material _material;

        private Coroutine _fadeCoroutine;
        private Coroutine _desaturationCoroutine;

        private static readonly int ColorID = Shader.PropertyToID("_Color");
        private static readonly int FadeAlphaID = Shader.PropertyToID("_FadeAlpha");
        private static readonly int DesaturationID = Shader.PropertyToID("_Desaturation");

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            CreateFadeMesh();
        }

        protected override void OnDestroy()
        {
            if (_fadeQuad != null) Destroy(_fadeQuad);
            if (_material != null) Destroy(_material);
            base.OnDestroy();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 암전 (화면을 검정으로 페이드)
        /// </summary>
        public Coroutine FadeToBlack(float duration = -1f)
        {
            return FadeToColor(Color.black, duration);
        }

        /// <summary>
        /// 암전 해제 (화면을 밝게 복원)
        /// </summary>
        public Coroutine FadeClear(float duration = -1f)
        {
            float d = duration > 0f ? duration : _defaultFadeDuration;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(AnimateFade(0f, d));
            return _fadeCoroutine;
        }

        /// <summary>
        /// 지정 색으로 페이드
        /// </summary>
        public Coroutine FadeToColor(Color color, float duration = -1f)
        {
            float d = duration > 0f ? duration : _defaultFadeDuration;

            if (_material != null)
                _material.SetColor(ColorID, color);

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(AnimateFade(1f, d));
            return _fadeCoroutine;
        }

        /// <summary>
        /// 탈채도 적용 (amount: 0=원본 색상, 1=완전 흑백)
        /// </summary>
        public Coroutine Desaturate(float amount, float duration = 0f)
        {
            if (_desaturationCoroutine != null)
                StopCoroutine(_desaturationCoroutine);

            if (duration <= 0f)
            {
                SetDesaturation(amount);
                return null;
            }

            _desaturationCoroutine = StartCoroutine(AnimateDesaturation(amount, duration));
            return _desaturationCoroutine;
        }

        #endregion

        #region Private Methods

        private void CreateFadeMesh()
        {
            var shader = Shader.Find("DDOIT/ScreenFade");
            if (shader == null)
            {
                Debug.LogError("[ScreenFadeManager] DDOIT/ScreenFade 셰이더를 찾을 수 없습니다.");
                return;
            }

            // 카메라 Transform 확인
            if (_cameraTransform == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                    _cameraTransform = mainCam.transform;
                else
                {
                    Debug.LogError("[ScreenFadeManager] 카메라를 찾을 수 없습니다. Inspector에서 지정하세요.");
                    return;
                }
            }

            _material = new Material(shader);
            _material.renderQueue = _renderQueue;

            // 카메라 하위에 Quad GameObject 생성
            _fadeQuad = new GameObject("ScreenFadeQuad");
            _fadeQuad.transform.SetParent(_cameraTransform, false);
            _fadeQuad.transform.localPosition = Vector3.zero;
            _fadeQuad.transform.localRotation = Quaternion.identity;

            var meshFilter = _fadeQuad.AddComponent<MeshFilter>();
            _meshRenderer = _fadeQuad.AddComponent<MeshRenderer>();
            _meshRenderer.material = _material;
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;
            _meshRenderer.enabled = false;

            var mesh = new Mesh();

            float width = 2f;
            float height = 2f;
            float depth = 1f;

            mesh.vertices = new[]
            {
                new Vector3(-width, -height, depth),
                new Vector3( width, -height, depth),
                new Vector3(-width,  height, depth),
                new Vector3( width,  height, depth)
            };

            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };

            mesh.normals = new[]
            {
                -Vector3.forward, -Vector3.forward,
                -Vector3.forward, -Vector3.forward
            };

            mesh.uv = new[]
            {
                Vector2.zero, Vector2.right,
                Vector2.up,   Vector2.one
            };

            meshFilter.mesh = mesh;

            SetFadeAlpha(0f);
            SetDesaturation(0f);
        }

        private void SetFadeAlpha(float alpha)
        {
            CurrentFadeAlpha = alpha;
            if (_material != null)
                _material.SetFloat(FadeAlphaID, alpha);
            UpdateRenderer();
        }

        private void SetDesaturation(float amount)
        {
            CurrentDesaturation = amount;
            if (_material != null)
                _material.SetFloat(DesaturationID, amount);
            UpdateRenderer();
        }

        private void UpdateRenderer()
        {
            if (_meshRenderer != null)
                _meshRenderer.enabled = CurrentFadeAlpha > 0.001f || CurrentDesaturation > 0.001f;
        }

        private IEnumerator AnimateFade(float targetAlpha, float duration)
        {
            IsFading = true;
            float startAlpha = CurrentFadeAlpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
                yield return null;
            }

            SetFadeAlpha(targetAlpha);
            IsFading = false;
            _fadeCoroutine = null;
        }

        private IEnumerator AnimateDesaturation(float targetAmount, float duration)
        {
            float startAmount = CurrentDesaturation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetDesaturation(Mathf.Lerp(startAmount, targetAmount, elapsed / duration));
                yield return null;
            }

            SetDesaturation(targetAmount);
            _desaturationCoroutine = null;
        }

        #endregion
    }
}
