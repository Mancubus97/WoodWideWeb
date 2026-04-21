using UnityEngine;
using UnityEngine.InputSystem;

public class TreePlacer : MonoBehaviour
{
    [Header("Root Logic")]
    public GameObject treeRootPrefab;
    [Header("Settings")]
    public int treePrototypeIndex = 0;
    public float treeHeightMin = 0.8f;
    public float treeHeightMax = 1.2f;
    public float placementRange = 50f;

    private Terrain terrain;
    private TerrainData terrainData;
    private Camera cam;
    private Mouse mouse;


    void Start()
    {
        terrain = Terrain.activeTerrain;
        terrainData = terrain.terrainData;
        cam = GetComponentInChildren<Camera>();
        mouse = Mouse.current;
    }

    void Update()
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            TryPlaceTree();
        }

        if (mouse.rightButton.wasPressedThisFrame)
        {
            TryRemoveTree();
        }
    }

    void TryPlaceTree()
    {
        if (terrainData.treePrototypes.Length == 0)
        {
            Debug.LogError("No tree prototypes on terrain!");
            return;
        }

        // Cast ray from camera center
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // Try raycast first
        Vector3 placementPoint = Vector3.zero;
        bool found = false;

        // Method 1: raycast against everything
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2000f))
        {
            placementPoint = hit.point;
            found = true;
            Debug.Log($"Raycast hit: {hit.point} on object: {hit.collider.gameObject.name}");
        }

        // Method 2: fallback - cast ray forward and sample terrain height
        if (!found)
        {
            for (float dist = 1f; dist <= placementRange; dist += 1f)
            {
                Vector3 samplePoint = ray.GetPoint(dist);
                float terrainY = terrain.SampleHeight(samplePoint) + terrain.transform.position.y;

                if (samplePoint.y <= terrainY + 1f)
                {
                    placementPoint = new Vector3(samplePoint.x, terrainY, samplePoint.z);
                    found = true;
                    Debug.Log($"Height sample hit at dist {dist}: {placementPoint}");
                    break;
                }
            }
        }

        // Method 3: last resort - straight down from player
        if (!found)
        {
            Vector3 playerPos = transform.position;
            float terrainY = terrain.SampleHeight(playerPos) + terrain.transform.position.y;
            placementPoint = new Vector3(playerPos.x, terrainY, playerPos.z);
            found = true;
            Debug.Log($"Fallback: placing at player feet: {placementPoint}");
        }

        // Snap Y to terrain surface properly
        placementPoint.y = terrain.SampleHeight(placementPoint) + terrain.transform.position.y;

        Debug.Log($"Final placement: {placementPoint} | Terrain origin: {terrain.transform.position}");

        //// Spawn debug sphere
        //GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //debugSphere.transform.position = placementPoint;
        //debugSphere.transform.localScale = Vector3.one * 2f;

        // Check bounds
        Vector3 terrainPos = placementPoint - terrain.transform.position;
        if (terrainPos.x < 0 || terrainPos.z < 0 ||
            terrainPos.x > terrainData.size.x ||
            terrainPos.z > terrainData.size.z)
        {
            Debug.LogWarning($"Out of terrain bounds! terrainPos={terrainPos}, size={terrainData.size}");
            return;
        }

        Vector3 normalizedPos = new Vector3(
            terrainPos.x / terrainData.size.x,
            0,
            terrainPos.z / terrainData.size.z
        );

        Debug.Log($"Normalized pos: {normalizedPos}");

        TreeInstance newTree = new TreeInstance
        {
            position = normalizedPos,
            prototypeIndex = treePrototypeIndex,
            widthScale = Random.Range(treeHeightMin, treeHeightMax),
            heightScale = Random.Range(treeHeightMin, treeHeightMax),
            color = Color.white,
            lightmapColor = Color.white
        };

        var treeList = new System.Collections.Generic.List<TreeInstance>(terrainData.treeInstances);
        treeList.Add(newTree);
        terrainData.treeInstances = treeList.ToArray();
        terrain.Flush();

        // Spawn the logic root at the same position
        if (treeRootPrefab != null)
        {
            Instantiate(treeRootPrefab, placementPoint - new Vector3(0 , 3, 0), Quaternion.identity);
        }
    }

    void TryRemoveTree()
    {
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
        Vector3 lookPoint = ray.GetPoint(placementRange / 2f);

        TreeInstance[] trees = terrainData.treeInstances;
        int closestIndex = -1;
        float closestDist = 10f;

        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 treeWorldPos = Vector3.Scale(trees[i].position, terrainData.size)
                                   + terrain.transform.position;
            float dist = Vector3.Distance(treeWorldPos, lookPoint);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        if (closestIndex >= 0)
        {
            var treeList = new System.Collections.Generic.List<TreeInstance>(trees);
            treeList.RemoveAt(closestIndex);
            terrainData.treeInstances = treeList.ToArray();
            terrain.Flush();
            Debug.Log("Tree removed.");
        }
        else
        {
            Debug.Log("No tree close enough to remove.");
        }
    }
}