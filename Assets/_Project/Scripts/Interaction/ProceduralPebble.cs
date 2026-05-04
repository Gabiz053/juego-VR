// ------------------------------------------------------------
//  ProceduralPebble.cs  -  _Project.Scripts.Voxel
//  Procedural low-poly stone from a jittered icosphere.
// ------------------------------------------------------------

using UnityEngine;

namespace _Project.Scripts.Voxel
{
    /// <summary>
    /// Generates a convex low-poly pebble mesh at runtime from a jittered
    /// icosahedron.  Each instance produces a unique shape via a random
    /// seed.  Lower hemisphere is clamped to Y=0 for a flat base.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ARmonia/Voxel/Procedural Pebble")]
    public class ProceduralPebble : MonoBehaviour
    {
        #region Constants -----------------------------------------

        /// <summary>Upper bound for randomly generated mesh seeds.</summary>
        private const int MAX_RANDOM_SEED = 99999;

        #endregion

        #region Inspector -----------------------------------------

        [Header("Shape")]
        [Tooltip("Overall size (w, h, d) in metres before PlowTool scale variance.")]
        [SerializeField] private Vector3 _size = new Vector3(0.18f, 0.11f, 0.15f);

        [Tooltip("Per-vertex jitter as a fraction of the smallest half-extent.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _jitterFraction = 0.28f;

        [Tooltip("Mesh seed.  0 = random shape every instantiation.")]
        [SerializeField] private int _seed;

        #endregion

        #region Unity Lifecycle -----------------------------------

        private void Awake() => BuildMesh();

        private void Start() => ValidateReferences();

        #endregion

        #region Internals -----------------------------------------

        /// <summary>
        /// Generates the mesh from a jittered icosahedron, assigns it to
        /// <see cref="MeshFilter"/> and <see cref="MeshCollider"/>.
        /// </summary>
        private void BuildMesh()
        {
            var mf   = GetComponent<MeshFilter>();
            var mc   = GetComponent<MeshCollider>();
            int seed = _seed == 0 ? Random.Range(1, MAX_RANDOM_SEED) : _seed;

            Mesh mesh     = GenerateMesh(_size, _jitterFraction, seed);
            mesh.name     = "PebbleMesh";
            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
            mc.convex     = true;
        }

        private static readonly float PHI = (1f + Mathf.Sqrt(5f)) * 0.5f;

        private static Vector3[] IcoVerts()
        {
            float n = 1f / Mathf.Sqrt(1f + PHI * PHI);
            float p = PHI * n;
            return new[]
            {
                new Vector3(-n,  p,  0), new Vector3( n,  p,  0),
                new Vector3(-n, -p,  0), new Vector3( n, -p,  0),
                new Vector3( 0, -n,  p), new Vector3( 0,  n,  p),
                new Vector3( 0, -n, -p), new Vector3( 0,  n, -p),
                new Vector3( p,  0, -n), new Vector3( p,  0,  n),
                new Vector3(-p,  0, -n), new Vector3(-p,  0,  n),
            };
        }

        private static readonly int[] ICO_TRIS =
        {
             0,11, 5,  0, 5, 1,  0, 1, 7,  0, 7,10,  0,10,11,
             1, 5, 9,  5,11, 4, 11,10, 2, 10, 7, 6,  7, 1, 8,
             3, 9, 4,  3, 4, 2,  3, 2, 6,  3, 6, 8,  3, 8, 9,
             4, 9, 5,  2, 4,11,  6, 2,10,  8, 6, 7,  9, 8, 1,
        };

        /// <summary>
        /// Jitters icosahedron vertices by a random per-vertex offset,
        /// scales to <paramref name="size"/>, clamps the lower hemisphere
        /// to Y=0, and builds a flat-shaded mesh with box-projected UVs.
        /// </summary>
        private static Mesh GenerateMesh(Vector3 size, float jitterFraction, int seed)
        {
            var   rng  = new System.Random(seed);
            var   icoV = IcoVerts();
            float jit  = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f * jitterFraction;

            var shaped = new Vector3[icoV.Length];
            for (int i = 0; i < icoV.Length; i++)
            {
                Vector3 v = icoV[i];
                v.x += (float)(rng.NextDouble() * 2 - 1) * jit / (size.x * 0.5f);
                v.y += (float)(rng.NextDouble() * 2 - 1) * jit / (size.y * 0.5f);
                v.z += (float)(rng.NextDouble() * 2 - 1) * jit / (size.z * 0.5f);
                v    = v.normalized;
                v    = Vector3.Scale(v, size * 0.5f);
                if (v.y < 0f) v.y = 0f;
                shaped[i] = v;
            }

            int triCount = ICO_TRIS.Length / 3;
            var fVerts   = new Vector3[triCount * 3];
            var fUVs     = new Vector2[triCount * 3];
            var fTris    = new int    [triCount * 3];

            for (int t = 0; t < triCount; t++)
            {
                int vi = t * 3;
                int a  = ICO_TRIS[vi], b = ICO_TRIS[vi + 1], c = ICO_TRIS[vi + 2];

                fVerts[vi] = shaped[a]; fVerts[vi + 1] = shaped[b]; fVerts[vi + 2] = shaped[c];
                fUVs[vi]   = BoxUV(shaped[a], size);
                fUVs[vi+1] = BoxUV(shaped[b], size);
                fUVs[vi+2] = BoxUV(shaped[c], size);
                fTris[vi]  = vi; fTris[vi + 1] = vi + 1; fTris[vi + 2] = vi + 2;
            }

            var mesh = new Mesh
            {
                vertices  = fVerts,
                uv        = fUVs,
                triangles = fTris
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Projects a vertex onto a box-mapped UV using the dominant axis.
        /// Produces clean UVs for low-poly stone surfaces.
        /// </summary>
        private static Vector2 BoxUV(Vector3 v, Vector3 size)
        {
            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
            float hx = size.x * 0.5f,  hy = size.y * 0.5f,  hz = size.z * 0.5f;

            if (ax >= ay && ax >= az)
                return new Vector2(v.z / hz * 0.5f + 0.5f, v.y / hy * 0.5f + 0.5f);
            if (ay >= ax && ay >= az)
                return new Vector2(v.x / hx * 0.5f + 0.5f, v.z / hz * 0.5f + 0.5f);
            return new Vector2(v.x / hx * 0.5f + 0.5f, v.y / hy * 0.5f + 0.5f);
        }

        #endregion

        #region Validation ----------------------------------------

        private void ValidateReferences()
        {
            if (GetComponent<MeshFilter>() == null)
                Debug.LogWarning("[ProceduralPebble] MeshFilter is not assigned.", this);
            if (GetComponent<MeshCollider>() == null)
                Debug.LogWarning("[ProceduralPebble] MeshCollider is not assigned.", this);
        }

        #endregion
    }
}
