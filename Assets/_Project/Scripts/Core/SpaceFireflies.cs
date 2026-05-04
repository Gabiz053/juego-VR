using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Ambient floating pixel effect. Each pixel is a small square billboard quad
    /// with a soft glow, distributed in camera-local space so the field drifts
    /// naturally as the player turns their head.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Space Fireflies")]
    public sealed class SpaceFireflies : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[SpaceFireflies]";
        private const float CAMERA_RETRY_INTERVAL = 0.5f;

        private static readonly Color[] PALETTE =
        {
            new Color(0.4f,  0.7f,  1.0f),   // cool blue
            new Color(1.0f,  0.55f, 0.08f),   // amber
            new Color(0.6f,  0.4f,  1.0f),    // purple
            new Color(0.3f,  0.9f,  1.0f),    // cyan
            Color.white,
        };

        #endregion

        #region Inspector

        [Header("Pixels")]
        [Tooltip("Number of floating pixels.")]
        [SerializeField, Range(5, 150)] private int _count = 60;

        [Tooltip("Maximum spawn distance from the camera (metres).")]
        [SerializeField, Range(2f, 30f)] private float _spawnRadius = 12f;

        [Tooltip("Minimum spawn distance from the camera (metres).")]
        [SerializeField, Range(0.5f, 10f)] private float _minRadius = 2f;

        [Header("Pixel shape")]
        [Tooltip("Size of each glowing pixel (metres).")]
        [SerializeField, Range(0.01f, 0.2f)] private float _pixelSize = 0.06f;

        [Tooltip("Overall brightness. Values above 1 saturate the core to pure white.")]
        [SerializeField, Range(0.5f, 8f)] private float _brightness = 3f;

        [Header("Movement")]
        [Tooltip("Drift speed in camera-local space (m/s).")]
        [SerializeField, Range(0f, 0.3f)] private float _speedMax = 0.04f;

        [Header("Lifetime")]
        [SerializeField, Range(2f, 30f)] private float _lifetimeMin = 5f;
        [SerializeField, Range(2f, 60f)] private float _lifetimeMax = 18f;

        [Header("Camera")]
        [Tooltip("Camera the pixels surround. Auto-detected if empty.")]
        [SerializeField] private Transform _cameraTransform;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private PixelInstance[]  _pixels;
        private Mesh             _quadMesh;
        private Material         _mat;
        private Texture2D        _glowTex;
        private Coroutine        _cameraRetry;
        private readonly WaitForSecondsRealtime _retryWait = new(CAMERA_RETRY_INTERVAL);

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            TryCacheCamera();
            if (_cameraTransform == null)
                _cameraRetry = StartCoroutine(CameraRetryRoutine());

            _quadMesh = BuildQuadMesh();
            _glowTex  = BuildGlowTexture();
            _mat      = BuildMaterial();
            _pixels   = new PixelInstance[_count];

            for (int i = 0; i < _count; i++)
            {
                _pixels[i] = CreateInstance(i);
                Spawn(ref _pixels[i], randomAge: true);
            }

            Debug.Log($"{LOG_TAG} {_count} pixels ready.");
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void Update()
        {
            if (_cameraTransform == null) return;

            float dt = Time.deltaTime;

            for (int i = 0; i < _count; i++)
            {
                ref PixelInstance p = ref _pixels[i];

                p.age += dt;
                if (p.age >= p.lifetime)
                {
                    Spawn(ref p, randomAge: false);
                    continue;
                }

                // Drift in world space — pixels float freely, unaffected by head rotation
                p.position += p.velocity * dt;

                Vector3 center = transform.position;
                float dist = (p.position - center).magnitude;
                if (dist > _spawnRadius)
                    p.velocity = (center - p.position).normalized * _speedMax;

                p.xform.position = p.position;

                // Always face the camera (billboard)
                Vector3 toCamera = _cameraTransform.position - p.position;
                if (toCamera.sqrMagnitude > 0.01f)
                    p.xform.rotation = Quaternion.LookRotation(toCamera, _cameraTransform.up);

                // Fade in / out
                float t     = p.age / p.lifetime;
                float alpha = t < 0.15f ? t / 0.15f
                            : t > 0.85f ? (1f - t) / 0.15f
                            : 1f;
                alpha = Mathf.Clamp01(alpha) * _brightness;

                p.mpb.SetColor("_BaseColor",
                    new Color(p.color.r * alpha,
                              p.color.g * alpha,
                              p.color.b * alpha,
                              alpha));
                p.renderer.SetPropertyBlock(p.mpb);
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform != null)
                transform.position = _cameraTransform.position;
        }

        private void OnDestroy()
        {
            if (_cameraRetry != null) StopCoroutine(_cameraRetry);
            if (_mat      != null)   Destroy(_mat);
            if (_quadMesh != null)   Destroy(_quadMesh);
            if (_glowTex  != null)   Destroy(_glowTex);
        }

        #endregion

        #region Internals

        private struct PixelInstance
        {
            public Vector3               position;
            public Vector3               velocity;
            public float                 age;
            public float                 lifetime;
            public Color                 color;
            public Transform             xform;
            public MeshRenderer          renderer;
            public MaterialPropertyBlock mpb;
        }

        private PixelInstance CreateInstance(int index)
        {
            var go = new GameObject($"Pixel_{index:00}");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * _pixelSize;

            var mf        = go.AddComponent<MeshFilter>();
            mf.sharedMesh = _quadMesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial    = _mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
            mr.lightProbeUsage   = UnityEngine.Rendering.LightProbeUsage.Off;

            return new PixelInstance { xform = go.transform, renderer = mr, mpb = new MaterialPropertyBlock() };
        }

        private void Spawn(ref PixelInstance p, bool randomAge)
        {
            p.position = transform.position + Random.insideUnitSphere * _spawnRadius;
            p.velocity = Random.insideUnitSphere * _speedMax;
            p.lifetime = Random.Range(_lifetimeMin, _lifetimeMax);
            p.age      = randomAge ? Random.Range(0f, p.lifetime * 0.7f) : 0f;
            p.color    = PALETTE[Random.Range(0, PALETTE.Length)];
        }

        private static Mesh BuildQuadMesh()
        {
            var mesh = new Mesh { name = "PixelQuad" };
            mesh.vertices  = new[] { new Vector3(-0.5f,-0.5f,0), new Vector3(0.5f,-0.5f,0), new Vector3(-0.5f,0.5f,0), new Vector3(0.5f,0.5f,0) };
            mesh.uv        = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
            mesh.triangles = new[] { 0,2,1, 1,2,3, 0,1,2, 1,3,2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        // Square glow: solid bright center square + soft exponential falloff outward.
        private static Texture2D BuildGlowTexture()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Clamp,
                name       = "Pixel_Glow"
            };

            for (int y = 0; y < S; y++)
            {
                float v = (float)y / (S - 1) - 0.5f;
                for (int x = 0; x < S; x++)
                {
                    float u    = (float)x / (S - 1) - 0.5f;
                    float distU = Mathf.Max(0f, Mathf.Abs(u) - 0.30f); // square core radius = 0.30
                    float distV = Mathf.Max(0f, Mathf.Abs(v) - 0.30f);
                    float dist  = Mathf.Sqrt(distU * distU + distV * distV);
                    float alpha = dist <= 0f ? 1f : Mathf.Exp(-dist * 14f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply(false);
            return tex;
        }

        private Material BuildMaterial()
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Legacy Shaders/Particles/Additive");

            var mat = new Material(sh) { name = "Pixel_Mat" };
            mat.SetFloat("_Surface",  1f);
            mat.SetFloat("_BlendOp",  0f);
            mat.SetFloat("_SrcBlend", 5f);
            mat.SetFloat("_DstBlend", 1f);
            mat.SetFloat("_ZWrite",   0f);
            mat.SetFloat("_Cull",     0f);
            mat.SetColor("_BaseColor", Color.white);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 1;
            mat.mainTexture = _glowTex;
            return mat;
        }

        private void TryCacheCamera()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }

        private IEnumerator CameraRetryRoutine()
        {
            while (_cameraTransform == null)
            {
                yield return _retryWait;
                TryCacheCamera();
            }
            _cameraRetry = null;
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_cameraTransform == null)
                Debug.LogWarning($"{LOG_TAG} _cameraTransform not found.", this);
            if (_lifetimeMin > _lifetimeMax)
                Debug.LogWarning($"{LOG_TAG} _lifetimeMin > _lifetimeMax.", this);
            if (_minRadius >= _spawnRadius)
                Debug.LogWarning($"{LOG_TAG} _minRadius should be less than _spawnRadius.", this);
        }

        #endregion
    }
}
