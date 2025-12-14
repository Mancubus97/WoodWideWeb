
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Metadata;
using static UnityEditor.FilePathAttribute;

namespace WoodWideWeb
{

    public class RootNode
    {
        public SoilCell occupied_cell = null;
        public Vector3 position = Vector3.zero;
        public RootNode parent = null; //if this stays null, it's the first node

        public RootNode(Vector3 pos, RootNode parent)
        {
            position = pos;
            this.parent = parent;
            this.occupied_cell = Soil.GetSoilCell(pos);
        }
    }

    public class TreeBranch : MonoBehaviour
    {
        public TreeBranch branchPrefab;

        public List<RootNode> nodes = new List<RootNode>();
        public List<TreeBranch> branches = new List<TreeBranch>();
        public List<FineBranch> fine_branches = new List<FineBranch>();
        public float nutrientsStored = 0f;
        public float growthCost = 0.5f;
        public int branchRate = 5;

        int rootThickness = 5;

        void CreateRoot(RootNode node, RootNode last)
        {

            for (int i = 0; i < rootThickness; i++)
            {
                if (i != 0)
                {
                    if (last != null && node.position.z != last.position.z)
                        nodes.Add(new RootNode(new Vector3(node.position.x + (float)i, node.position.y, node.position.z), node.parent));
                    else
                        nodes.Add(new RootNode(new Vector3(node.position.x, node.position.y, node.position.z + (float)i), node.parent));
                }
                else
                {
                    nodes.Add(node);
                }
            }
        }

        void CreateFirstNode()
        {
            TreeBranch soil = FindFirstObjectByType<TreeBranch>();
            BoxCollider col = soil.GetComponent<BoxCollider>();

            Vector3 center = col.transform.position;

            RootNode firstNode = new RootNode(new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z), null);

            CreateRoot(firstNode, null);
            //nodes.Add(firstNode);
        }

        void OnValidate()
        {

        }
        void Start()
        {
            if (nodes.Count == 0)
                CreateFirstNode();
        }

        SoilCell DetermineNextCell(RootNode current)
        {
            SoilCell nextCell = null;

            List<SoilCell> candidate_cells = new List<SoilCell>(){
                Soil.GetSoilCell(current.position + new Vector3(0, 0, -20)), // back
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

            if (nextCell == null)
            {
                nextCell = current.parent != null ? current.parent.occupied_cell : null;
            }

            return nextCell;
        }

        public void BranchOff(SoilCell nextCell)
        {
            if (nutrientsStored >= growthCost * 4 && Random.Range(0, branchRate) == 0) //
            {
                Debug.Log("[TreeBranch] Branching off | nutrientsStored: " + nutrientsStored);
                nutrientsStored = nutrientsStored - growthCost * 4;
                Quaternion rot = Random.rotation;
                TreeBranch branch = Instantiate(branchPrefab, nextCell.position, rot);
                branches.Add(branch);
            }
        }

        public void GrowNode()
        {
            Debug.Log("[TreeBranch] Trying to grow node with nutrientsStored: " + nutrientsStored);
            //if (nutrientsStored >= growthCost)
            //{
                nutrientsStored = nutrientsStored - growthCost;
                // last node
                RootNode last = nodes[nodes.Count - 1];

                SoilCell nextCell = DetermineNextCell(last);

                if (nextCell == null)
                {
                    Debug.Log("Returned Null Cell!");
                    return;
                }

                Vector3 newPos = nextCell.position;//last.position + new Vector3(0, -soil.cellSize.y, 0);

                BranchOff(nextCell);

                nutrientsStored += nextCell.nutrients * 0.9f; // absorb some nutrients
                nextCell.nutrients *= 0.1f;


                CreateRoot(new RootNode(newPos, last), last);
                //nodes.Add(new RootNode(newPos, last));
            //}
            //else
            //{

            //}
                
        }

        float elapsedTime = 0f;

        void Update()
        {
            elapsedTime += Time.deltaTime;
            if (!isGrowing)
            {
                StartCoroutine(GrowLoop());
            }
        }

        bool isGrowing = false;

        IEnumerator GrowLoop()
        {
            isGrowing = true;

            while (true)   // grow forever
            {
                GrowNode();
                //yield return new WaitForSeconds(0.0001f);
                yield return new WaitForSeconds(5.0f);
            }
        }


        void OnDrawGizmos()
        {
            if (nodes.Count != 0 && nodes[0] != null)
            {
                int start = Mathf.Max(0, nodes.Count - 300);

                for (int i = start; i < nodes.Count - 1; i++)
                {
                    Gizmos.color = new Color(0.5f, 0.35f, 0.05f, 1f);
                    if (i + 1 + rootThickness - 1 >= 0 && i + 1 + rootThickness - 1 < nodes.Count)
                        Gizmos.DrawLine(nodes[i].position, nodes[i + 1 + rootThickness-1] != null ? nodes[i + 1 + rootThickness-1].position : transform.position);
                }
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 10);
            }
        }
    }

    public class FineBranch : TreeBranch
    {

    }

}
