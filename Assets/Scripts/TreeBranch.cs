
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Metadata;
using static UnityEditor.FilePathAttribute;

namespace WoodWideWeb
{

    public class RootNode
    {
        public TreeBranch branch = null;
        public SoilCell occupied_cell = null;
        public RootNode parent = null; //if this stays null, it's the first node

        public Vector3 position = Vector3.zero;

        public RootNode(Vector3 position, RootNode parent, TreeBranch branch)
        {
            this.branch = branch;
            this.position = position;
            this.parent = parent;
            this.occupied_cell = Soil.GetSoilCell(position);
        }
    }

    public class TreeBranch : MonoBehaviour
    {
        public TreeBranch branchPrefab;
        public FungalBranch fungal_network = null;
        public List<RootNode> nodes = new List<RootNode>();

        public List<TreeBranch> branches = new List<TreeBranch>();
        private float nutrientsStored = 8f;
        private float growthCost = 3f;
        private int branchRate = 5;
        private float branchoff_cost_multiplier = 4f;
        private float growth_delay = 5f;


        void CreateRoot(RootNode node, RootNode last)
        {

            for (int i = 0; i < Constants.rootThickness; i++)
            {
                if (i != 0)
                {
                    if (last != null && node.position.z != last.position.z)
                    {
                        RootNode newnode = new RootNode(new Vector3(node.position.x + (float)i, node.position.y, node.position.z), node.parent, this);
                        nodes.Add(newnode);
                        newnode.occupied_cell.root = newnode;
                    }
                    else
                    {
                        RootNode newnode = new RootNode(new Vector3(node.position.x, node.position.y, node.position.z + (float)i), node.parent, this);
                        nodes.Add(newnode);
                        newnode.occupied_cell.root = newnode;
                    }
                }
                else
                {
                    nodes.Add(node);
                    node.occupied_cell.root = node;
                }
            }
        }

        void CreateFirstNode()
        {
            TreeBranch soil = FindFirstObjectByType<TreeBranch>();
            BoxCollider col = soil.GetComponent<BoxCollider>();

            Vector3 center = col.transform.position;

            RootNode firstNode = new RootNode(new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z), null, this);

            CreateRoot(firstNode, null);
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
            if (nutrientsStored >= growthCost * branchoff_cost_multiplier && Random.Range(0, branchRate) == 0)
            {
                Debug.Log("[TreeBranch] Branching off with nutrientsStored: " + nutrientsStored);
                nutrientsStored = nutrientsStored - growthCost * branchoff_cost_multiplier;
                Quaternion rot = Random.rotation;
                TreeBranch branch = Instantiate(branchPrefab, nextCell.position, rot);
                branches.Add(branch);
            }
        }

        public void GrowNode()
        {
            if (nodes.Count / Constants.rootThickness >= Constants.grown_tree_amount)
                return;

            if (nutrientsStored >= growthCost)
            {
                nutrientsStored = nutrientsStored - growthCost;
                // last node
                RootNode last = nodes[nodes.Count - 1];

                SoilCell nextCell = DetermineNextCell(last);

                if (nextCell == null)
                {
                    Debug.Log("Returned Null Cell!");
                    return;
                }

                Vector3 newPos = nextCell.position; //last.position + new Vector3(0, -soil.cellSize.y, 0);

                BranchOff(nextCell);

                nutrientsStored += nextCell.nutrients * 0.9f; // absorb some nutrients
                nextCell.nutrients *= 0.1f;

                RootNode new_node = new RootNode(newPos, last, this);

                CreateRoot(new_node, last);
            }
            else
            {
                DrainFromNetwork(growthCost);
            }

        }

        void DrainFromNetwork(float amount)
        {
            if (fungal_network != null)
            {
                if (fungal_network.nutrientsStock < amount)
                {
                    Debug.Log("Not enough nutrients in fungal network to drain! End game?");
                    return;
                }
                fungal_network.nutrientsStock -= amount;
                nutrientsStored += amount;
            }
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

        public bool isGrowing = false;

        IEnumerator GrowLoop()
        {
            isGrowing = true;

            while (true) // grow till grown
            {
                GrowNode();
                //yield return new WaitForSeconds(0.0001f);
                yield return new WaitForSeconds(growth_delay);
            }
        }


        void OnDrawGizmos()
        {
            if (nodes.Count != 0 && nodes[0] != null)
            {
                Handles.Label(nodes[0].position, "N: " + nutrientsStored);

                int start = Mathf.Max(0, nodes.Count - 300);
                for (int i = start; i < nodes.Count - 1; i++)
                {
                    if (nodes.Count / Constants.rootThickness >= Constants.grown_tree_amount)
                        Gizmos.color = Color.green;
                    else
                        Gizmos.color = new Color(0.5f, 0.35f, 0.05f, 1f);
                    if (i + 1 + Constants.rootThickness - 1 >= 0 && i + 1 + Constants.rootThickness - 1 < nodes.Count)
                        Gizmos.DrawLine(nodes[i].position, nodes[i + 1 + Constants.rootThickness - 1] != null ? nodes[i + 1 + Constants.rootThickness - 1].position : transform.position);
                }
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 10);
            }
        }
    }


}
