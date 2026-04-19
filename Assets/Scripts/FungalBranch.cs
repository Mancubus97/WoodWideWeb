
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Metadata;
using static UnityEditor.FilePathAttribute;

namespace WoodWideWeb
{

    public class FungalNode
    {
        public FungalBranch branch = null;
        public SoilCell occupied_cell = null;
        public Vector3 position = Vector3.zero;
        public FungalNode parent = null; //if this stays null, it's the first node

        public FungalNode(Vector3 pos, FungalNode parent, FungalBranch branch)
        {
            position = pos;
            this.parent = parent;
            this.occupied_cell = Soil.GetSoilCell(pos);
            this.branch = branch;
        }
    }

    public class FungalBranch : MonoBehaviour
    {
        //POINTER
        private SoilCell cell_pointer;
        private SoilCell destination;
        private int destinationIndex;

        public Soil soil;
        public FungalBranch branchPrefab;

        public List<TreeBranch> trees = new List<TreeBranch>();
        public List<FungalNode> nodes = new List<FungalNode>();
        public List<FungalBranch> branches = new List<FungalBranch>();
        public float nutrientsStock = 0f;
        void CreateFirstNode()
        {
            Soil soil = FindFirstObjectByType<Soil>();
            BoxCollider col = soil.GetComponent<BoxCollider>();

            Vector3 center = col.transform.position;

            FungalNode firstNode = new FungalNode(new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z), null, this);
            nodes.Add(firstNode);
        }

        void Start()
        {
            if (nodes.Count == 0)
                CreateFirstNode();
        }
        SoilCell FindTreeCell(FungalNode current)
        {
            SoilCell nextCell = null;
            int lookAhead = 40; // Change this to look further, e.g. 10

            // Build candidate cells for all directions across all distances
            List<SoilCell> candidate_cells = new List<SoilCell>();
            for (int i = 1; i <= lookAhead; i++)
            {
                candidate_cells.Add(Soil.GetSoilCell(current.position + new Vector3(0, soil.cellSize.y * i, 0)));   // up
                candidate_cells.Add(Soil.GetSoilCell(current.position + new Vector3(0, -soil.cellSize.y * i, 0)));  // down
                candidate_cells.Add(Soil.GetSoilCell(current.position + new Vector3(-soil.cellSize.x * i, 0, 0)));  // left
                candidate_cells.Add(Soil.GetSoilCell(current.position + new Vector3(soil.cellSize.x * i, 0, 0)));   // right
                candidate_cells.Add(Soil.GetSoilCell(current.position + new Vector3(0, 0, soil.cellSize.z * i)));   // forward
                candidate_cells.Add(Soil.GetSoilCell(current.position + new Vector3(0, 0, -soil.cellSize.z * i)));  // back
            }

            if (destination != null)
            {
                Debug.Log("Going towards destination");
                return candidate_cells[destinationIndex % 6]; // step back to distance-1 cell in same direction
            }

            int totalCells = lookAhead * 6;

            int index = Random.Range(0, totalCells);
            cell_pointer = candidate_cells[index];
            int counter = 0;
            while (cell_pointer == null || cell_pointer.root == null)
            {
                if (counter > 80)
                    break;

                index = Random.Range(0, totalCells);
                cell_pointer = candidate_cells[index];
                counter++;
            }

            if (counter <= 80) // found a tree cell
            {
                Debug.Log("Found tree cell after " + counter + " tries");
                destination = cell_pointer;
                destinationIndex = index;
                return candidate_cells[index % 6]; // step back to distance-1 cell in same direction
                 
            } 

            if (nextCell == null) // grow random direction otherwise
            {
                nextCell = candidate_cells[Random.Range(0, 6)];
            }
            return nextCell;
        }
        SoilCell FindHighNutrientCell(FungalNode current)
        { 
            SoilCell nextCell = null;

            List<SoilCell> candidate_cells = new List<SoilCell>(){
                Soil.GetSoilCell(current.position + new Vector3(0, soil.cellSize.y, 0)), // up
                Soil.GetSoilCell(current.position + new Vector3(0, -soil.cellSize.y, 0)), // down
                Soil.GetSoilCell(current.position + new Vector3(-soil.cellSize.x, 0, 0)), // left
                Soil.GetSoilCell(current.position + new Vector3(soil.cellSize.x, 0, 0)), // right
                Soil.GetSoilCell(current.position + new Vector3(0, 0, soil.cellSize.z)), // forward
                Soil.GetSoilCell(current.position + new Vector3(0, 0, -soil.cellSize.z)) // back
            };

            int index = Random.Range(0, 6);
            int counter = 0;
            while (candidate_cells[index] == null || candidate_cells[index].nutrients <= Soil.GetSoilCell(current.position).nutrients)
            {
                if (candidate_cells[index] != null && candidate_cells[index].root != null && candidate_cells[index].fungal == null || 
                    counter > 20)
                    break;

                index = Random.Range(0, 6);
                counter++;
            }
            if (counter <= 20) // found a better cell
                nextCell = candidate_cells[index];

            // METHOD1 - if no better cell, try random direction (simulate growing around obstacles)
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

            // METHOD2 - if no better cell, try to grow back towards parent (simulate growing around obstacles)
            if (nextCell == null)
            {
                nextCell = current.parent != null ? current.parent.occupied_cell : null;
            }

            return nextCell;
        }

        public void GrowNode()
        {
            // last node
            FungalNode last = nodes[nodes.Count - 1];

            SoilCell nextCell = null;

            nextCell = trees.Count == 0 ? FindTreeCell(last) : FindHighNutrientCell(last);

            if (nextCell == null)
            {
                Debug.Log("Returned Null Cell!");
                return;
            }

            Vector3 newPos = nextCell.position;

            if (nextCell.nutrients > 5f && Random.Range(0, 25) == 0) // branch off
            {
                Quaternion rot = Random.rotation;
                FungalBranch branch = Instantiate(branchPrefab, nextCell.position, rot);
                branches.Add(branch);
            }

            nutrientsStock += nextCell.nutrients * 0.9f; // absorb some nutrients
            nextCell.nutrients *= 0.1f;

            FungalNode new_node = new FungalNode(newPos, last, this);

            nextCell.fungal = new_node;
            nodes.Add(new_node);
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
        int maxStock = 1000;

        IEnumerator GrowLoop()
        {
            isGrowing = true;

            while (nutrientsStock < maxStock)   // grow if below max stock
            {
                GrowNode();
                //yield return new WaitForSeconds(0.0001f);
                yield return new WaitForSeconds(0.1f);
            }
        }


        void OnDrawGizmos()
        {
            if (nodes.Count != 0 && nodes[0] != null)
            {
                Handles.Label(nodes[0].position, "N: " + nutrientsStock);

                int start = Mathf.Max(0, nodes.Count - 300);
                for (int i = start; i < nodes.Count - 1; i++)
                {
                    Gizmos.color = new Color(1f, 1f , 1f, i / (float)nodes.Count);
                    Gizmos.DrawLine(nodes[i].position, nodes[i + 1] != null ? nodes[i + 1].position : transform.position);
                }
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 10);
            }


            if (cell_pointer != null)
            {
                //draw yellow spehere at cell pointer
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(cell_pointer.position, 5);
            }
             
            

        }
    }

}
