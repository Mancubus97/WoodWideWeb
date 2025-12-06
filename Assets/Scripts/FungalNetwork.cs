
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Metadata;

namespace WoodWideWeb
{

    public class FungalNode
    {
        public SoilCell occupied_cell = null;
        public Vector3 position = Vector3.zero;
        public FungalNode parent = null; //if this stays null, it's the first node
        //public List<FungalNode> children = new List<FungalNode>();

        public FungalNode(Vector3 pos, FungalNode parent)
        {
            position = pos;
            this.parent = parent;
            this.occupied_cell = Soil.GetSoilCell(pos);
        }
    }

    public class FungalNetwork : MonoBehaviour
    {
        public List<FungalNode> network = new List<FungalNode>();
        public float nutrientsStock = 0f;
        public static FungalNetwork Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
            void CreateFirstNode()
        {
            Soil soil = FindFirstObjectByType<Soil>();
            BoxCollider col = soil.GetComponent<BoxCollider>();

            Vector3 center = col.transform.position;

            FungalNode firstNode = new FungalNode(new Vector3(col.transform.position.x, col.transform.position.y, col.transform.position.z), null);
            network.Add(firstNode);
        }

        void OnValidate()
        {
            if (network.Count == 0)
                CreateFirstNode();
        }
        void Start()
        {

        }

        SoilCell DetermineNextCell(FungalNode current)
        {
            SoilCell nextCell = null;

            List<SoilCell> candidate_cells = new List<SoilCell>(){
                Soil.GetSoilCell(current.position + new Vector3(0, 20, 0)), // up
                Soil.GetSoilCell(current.position + new Vector3(0, -20, 0)), // down
                Soil.GetSoilCell(current.position + new Vector3(-20, 0, 0)), // left
                Soil.GetSoilCell(current.position + new Vector3(20, 0, 0)), // right
                Soil.GetSoilCell(current.position + new Vector3(0, 0, 20)), // forward
                Soil.GetSoilCell(current.position + new Vector3(0, 0, -20)) // back
            };

            int index = Random.Range(0, 6);
            int counter = 0;
            while (candidate_cells[index] == null || candidate_cells[index].nutrients <= Soil.GetSoilCell(current.position).nutrients)
            {
                if (counter > 20)
                {
                    break;
                }
                index = Random.Range(0, 6);
                counter++;
            }
            if (counter <= 20) // found a better cell
                nextCell = candidate_cells[index];

            // METHOD1
            //if (nextCell == null)
            //{
            //    nextCell = candidate_cells[Random.Range(0, 6)];
            //}

            //if (current.parent != null)// Dont need to check for first node
            //{
            //    while (nextCell != null && nextCell == current.parent.occupied_cell) // find a random direction that is not the parent
            //    {
            //        nextCell = candidate_cells[Random.Range(0, 6)];
            //    }
            //}

            // METHOD2
            if (nextCell == null)
            {
                nextCell = current.parent != null ? current.parent.occupied_cell : null;
            }

            return nextCell;
        }

        public void GrowNode()
        {
            // last node
            FungalNode last = network[network.Count - 1];

            SoilCell nextCell = DetermineNextCell(last);

            if (nextCell == null)
            {
                Debug.Log("Returned Null Cell!");
                return;
            }

            var soil = FindFirstObjectByType<Soil>();

            Vector3 newPos = nextCell.position;//last.position + new Vector3(0, -soil.cellSize.y, 0);

            nutrientsStock += nextCell.nutrients * 0.9f; // absorb some nutrients
            nextCell.nutrients *= 0.1f; 

            network.Add(new FungalNode(newPos, last));
            //Debug.Log("Grew node at: " + network[network.Count - 1].position.ToString() + "total nodes: " + network.Count);
        }

        float elapsedTime = 0f;

        void Update()
        {
            elapsedTime += Time.deltaTime;
            if (!isGrowing)
            {
                StartCoroutine(GrowLoop());
            }

            //if (elapsedTime > 60f)
            //{
            //    Debug.Log("Total nutrients collected: " + nutrientsStock + " in " + elapsedTime);
            //    //stop game 
            //    UnityEditor.EditorApplication.isPlaying = false;
            //}
        }

        bool isGrowing = false;

        IEnumerator GrowLoop()
        {
            isGrowing = true;

            while (true)   // grow forever
            {
                GrowNode();
                yield return new WaitForSeconds(0.0001f);
            }
        }


        void OnDrawGizmos()
        {
            if (network[0] != null)
            {
                for (int i = 0; i < network.Count - 1; i++)
                {
                    Gizmos.color = new Color(1f, 0f , 0f, i / (float)network.Count);
                    Gizmos.DrawLine(network[i].position, network[i + 1] != null ? network[i + 1].position : transform.position);
                }
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 10);
            }
        }
    }

}
