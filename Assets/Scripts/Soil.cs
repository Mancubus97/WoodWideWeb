using UnityEngine;
using UnityEngine.UIElements;

namespace WoodWideWeb 
{

    public class SoilCell
    {
        public float nutrients;
        public float water;
        public Vector3 position;

        public SoilCell(Vector3 position)
        {
            nutrients = Random.Range(0.3f, 1.0f);
            water = Random.Range(0.2f, 0.8f);
            this.position = position;
        }
    }

    public class Soil : MonoBehaviour
    {
        private Renderer rend;

        public static int xGrid = 50;
        public static int yGrid = 25;
        public static int zGrid = 50;

        static SoilCell[,,] grid;

        public Vector3 cellSize;

        void FillGrid()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            Vector3 size = col.size;

            cellSize = new Vector3(
                size.x / xGrid,
                size.y / yGrid,
                size.z / zGrid
            );

            grid = new SoilCell[xGrid, yGrid, zGrid];

            Vector3 origin = transform.position - size * 0.5f; // bottom-left-back corner

            for (int x = 0; x < xGrid; x++)
                for (int y = 0; y < yGrid; y++)
                    for (int z = 0; z < zGrid; z++)
                    {
                        Vector3 cellPos = origin + new Vector3(
                            (x + 0.5f) * cellSize.x,
                            (y + 0.5f) * cellSize.y,
                            (z + 0.5f) * cellSize.z
                        );
                        grid[x, y, z] = new SoilCell(cellPos);
                    }
        }

        void OnValidate()
        {
            FillGrid();
        }
        void Start()
        {
            //rend = GetComponent<Renderer>();
            //if (rend != null)
            //    rend.enabled = false;

            FillGrid();

            Debug.Log("Soil initialized!");
        }

        void OnDrawGizmos()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null || grid == null)
                return;

            // --- Draw collider outline ---
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);

            // --- Bundle visualization ---
            Gizmos.color = Color.gray;

            Vector3 size = col.size;
            Vector3 origin = col.center - size * 0.5f; // local origin (bottom-left-back)

            // Convert local origin to world
            Vector3 worldOrigin = transform.TransformPoint(origin);

            Vector3 cellSize = new Vector3(
                size.x / xGrid,
                size.y / yGrid,
                size.z / zGrid
            );

            int bundleSize = 1000;

            // cube root of bundle size
            int bundleStep = Mathf.Max(1, Mathf.RoundToInt(Mathf.Pow(bundleSize, 1f / 3f)));

            for (int x = 0; x < xGrid; x += bundleStep)
            {
                for (int y = 0; y < yGrid; y += bundleStep)
                {
                    for (int z = 0; z < zGrid; z += bundleStep)
                    {
                        int bx = Mathf.Min(bundleStep, xGrid - x);
                        int by = Mathf.Min(bundleStep, yGrid - y);
                        int bz = Mathf.Min(bundleStep, zGrid - z);

                        // center of this bundle in LOCAL space
                        Vector3 localCenter =
                            origin +
                            new Vector3(
                                (x + bx * 0.5f) * cellSize.x,
                                (y + by * 0.5f) * cellSize.y,
                                (z + bz * 0.5f) * cellSize.z
                            );

                        // convert to world space
                        Vector3 worldCenter = transform.TransformPoint(localCenter);

                        Vector3 worldSize = new Vector3(
                            bx * cellSize.x,
                            by * cellSize.y,
                            bz * cellSize.z
                        );

                        Gizmos.DrawWireCube(worldCenter, worldSize);
                    }
                }
            }
        }



        public static SoilCell GetSoilCell(Vector3 worldPos)
        {
            if (grid == null)
                return null;

            Soil inst = FindFirstObjectByType<Soil>();
            if (inst == null)
                return null;

            BoxCollider col = inst.GetComponent<BoxCollider>();
            Vector3 size = col.size;

            // same origin used in FillGrid
            Vector3 origin = inst.transform.position - size * 0.5f;

            // convert world position to local-grid space
            Vector3 local = worldPos - origin;

            int x = Mathf.FloorToInt(local.x / inst.cellSize.x);
            int y = Mathf.FloorToInt(local.y / inst.cellSize.y);
            int z = Mathf.FloorToInt(local.z / inst.cellSize.z);

            // bounds check
            if (x < 0 || y < 0 || z < 0 ||
                x >= xGrid || y >= yGrid || z >= zGrid)
                return null;

            return grid[x, y, z];
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

