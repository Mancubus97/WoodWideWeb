using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace WoodWideWeb 
{

    public class SoilCell
    {
        public float nutrients;
        public float water;
        public Vector3 position;

        public SoilCell(Vector3 position, bool hotspot, float nutrients)
        {
            if (hotspot)
            {
                this.nutrients = Random.Range(5.0f, 10.0f);
            }
            else
                this.nutrients = nutrients;

            water = Random.Range(0.2f, 0.8f);
            this.position = position;
        }
    }

    public class Soil : MonoBehaviour
    {
        private Renderer rend;

        static float falloff = 0.002f;

        static int xGrid = 50;
        static int yGrid = 25;
        static int zGrid = 50;

        static SoilCell[,,] grid;

        public Vector3 cellSize;

        Vector3 size;

        void FillGrid(int HighNutrientBlocks)
        {
            BoxCollider col = GetComponent<BoxCollider>();
            size = col.size;

            cellSize = new Vector3(
                size.x / xGrid,
                size.y / yGrid,
                size.z / zGrid
            );

            grid = new SoilCell[xGrid, yGrid, zGrid];

            Vector3 origin = transform.position - size * 0.5f; // bottom-left-back corner

            int randomx = Random.Range(0, xGrid);
            int randomSize = Random.Range(12, 16);
            int randomy = Random.Range(0, yGrid);
            int randomz = Random.Range(0, zGrid);


            Vector3 hotspotPos;
            float nutrients = 0f;
            float base_nutrients = 3f;
            float distance;

            for (int i = 0; i < HighNutrientBlocks; i++)
            {
                randomx = Random.Range(0, xGrid);
                randomSize = Random.Range(6, 10);
                randomy = Random.Range(0, yGrid);
                randomz = Random.Range(0, zGrid);

                hotspotPos = origin + new Vector3(
                    (randomx + 0.5f) * cellSize.x,
                    (randomy + 0.5f) * cellSize.y,
                    (randomz + 0.5f) * cellSize.z
                );

                for (int x = 0; x < xGrid; x++)
                    for (int y = 0; y < yGrid; y++)
                        for (int z = 0; z < zGrid; z++)
                        {
                            Vector3 cellPos = origin + new Vector3(
                                (x + 0.5f) * cellSize.x,
                                (y + 0.5f) * cellSize.y,
                                (z + 0.5f) * cellSize.z
                                );

                            if (x - randomx > -randomSize && x - randomx < randomSize && y - randomy > -randomSize && y - randomy < randomSize && z - randomz > -randomSize && z - randomz < randomSize)
                            {
                                grid[x, y, z] = new SoilCell(cellPos, true, nutrients);
                            }
                            else
                            {
                                distance = Vector3.Distance(cellPos, hotspotPos);
                                nutrients = base_nutrients * Mathf.Exp(-falloff * distance);
                                //Debug.Log("Distance: " + distance + " Nutrients: " + nutrients);
                                if (grid[x, y, z] == null)
                                    grid[x, y, z] = new SoilCell(cellPos, false, nutrients);
                                else if (nutrients > grid[x, y, z].nutrients)
                                    grid[x, y, z] = new SoilCell(cellPos, false, nutrients);
                            }




                        }
            }
            
            // Handle a clean natural random soil
            if (HighNutrientBlocks == 0)
            {
                Debug.Log("making empty soil..");
                for (int x = 0; x < xGrid; x++)
                    for (int y = 0; y < yGrid; y++)
                        for (int z = 0; z < zGrid; z++)
                        {
                            Vector3 cellPos = origin + new Vector3(
                                (x + 0.5f) * cellSize.x,
                                (y + 0.5f) * cellSize.y,
                                (z + 0.5f) * cellSize.z
                                );
                            distance = Vector3.Distance(col.center, new Vector3(Random.Range(0, col.size.x), Random.Range(0, col.size.y), Random.Range(0, col.size.z)));
                            nutrients = base_nutrients * Mathf.Exp(-falloff * distance);

                            grid[x, y, z] = new SoilCell(cellPos, false, nutrients);
                        }

            }
           
        }

        void OnValidate()
        {
            FillGrid(3);
        }
        void Start()
        {
            //rend = GetComponent<Renderer>();
            //if (rend != null)
            //    rend.enabled = false;


            Debug.Log("Soil initialized!");
        }

        void DrawBundles()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null || grid == null)
                return;

            Vector3 size = col.size;
            Vector3 origin = col.center - size * 0.5f; // local origin (bottom-left-back)

            //Draw the soil bounds
            //Gizmos.color = Color.darkGreen;
            //Gizmos.DrawCube(col.center, size);



            // --- Bundle visualization ---
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

                        Gizmos.color = Color.black;
                        Gizmos.DrawWireCube(worldCenter, worldSize);
                    }
                }
            }
        }
        void OnDrawGizmos()
        {
            DrawBundles();

            //draw high nutrient cells
            foreach (SoilCell cell in grid)
            {
                if (cell == null) continue;
                if (cell.nutrients > 5f)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(cell.position, 1f);
                }
                //else
                //{
                //    Gizmos.color = new Color(0f, 1f, 0f, cell.nutrients - 0.8f);
                //    Gizmos.DrawSphere(cell.position, 0.5f);
                //}

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

