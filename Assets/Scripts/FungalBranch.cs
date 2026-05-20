using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
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

        public FungalNode(Vector3 position, FungalNode parent, FungalBranch branch)
        {
            this.position = position;
            this.parent = parent;
            this.occupied_cell = Soil.GetSoilCell(position);
            this.branch = branch;
        }
    }

    public class FungalBranch : MonoBehaviour
    {
        //POINTER
        public List<SoilCell> cell_candidates = new List<SoilCell>();
        public SoilCell destination;

        public Soil soil;
        public FungalBranch branchPrefab;

        public List<TreeBranch> trees = new List<TreeBranch>();
        public List<FungalNode> nodes = new List<FungalNode>();
        public List<FungalBranch> branches = new List<FungalBranch>();
        public float nutrientsStock = 0f;
        int width = 4;
        int height = 30;
        int depth = 4;
        int grow_attempts = 0;
        int branchoff_difficulty = 25;

        bool isGrowing = false;
        int maxStock = 5000;
        int fungal_view_distance = 900;
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
            if (destination != null && current.position == destination.position)
            {
                destination = null;
            }

            if (destination != null)
            {
                return destination;
            }

            cell_candidates.Clear();

            for (int x = -width; x <= width; x++)
            {
                for (int y = -height; y < height; y++)
                {
                    for (int z = -depth; z <= depth; z++)
                    {
                        SoilCell cell = Soil.GetSoilCell(current.position + new Vector3(
                            soil.cellSize.x * x,
                            soil.cellSize.y * y,
                            soil.cellSize.z * z
                        ));
                        cell_candidates.Add(cell);

                        if (cell?.root?.branch != null &&
                            cell.root.branch.nodes.Count / Constants.rootThickness < Constants.grown_tree_amount)
                        {
                            destination = cell;
                            cell_candidates.Clear();
                            return destination;
                        }
                    }
                }
            }


            // go to random next cell if no tree is found
            List<SoilCell> validCells = cell_candidates.Where(c => c != null).ToList();
            if (validCells.Count > 0)
                return validCells[Random.Range(0, validCells.Count)];

            return null;
        }
        SoilCell FindHighNutrientCell(FungalNode current)
        {
            Vector3 pos = current.position;
            SoilCell currentCell = Soil.GetSoilCell(pos);

            Vector3[] directions = {
                new Vector3( soil.cellSize.x,  0,              0),
                new Vector3(-soil.cellSize.x,  0,              0),
                new Vector3( 0,               soil.cellSize.y,  0),
                new Vector3( 0,              -soil.cellSize.y,  0),
                new Vector3( 0,               0,               soil.cellSize.z),
                new Vector3( 0,               0,              -soil.cellSize.z)
            };

            var candidates = directions
                .Select(d => Soil.GetSoilCell(pos + d))
                .Where(c => c != null && c.fungal == null && c.nutrients > currentCell.nutrients)
                .ToList();

            if (candidates.Count > 0)
                return candidates[Random.Range(0, candidates.Count)];

            // Fall back to parent if no better cell found
            return current.parent?.occupied_cell;
        }

        public void GrowNode()
        {
            // last node
            FungalNode last = nodes[nodes.Count - 1];

            SoilCell nextCell = null;

            nextCell = trees.Count == 0 ? FindTreeCell(last) : FindHighNutrientCell(last);

            if (nextCell == null)
            {
                //Debug.Log("Returned Null Cell!");
                return;
            }

            Vector3 newPos = nextCell.position;

            if (nextCell.nutrients > Constants.hotspot_lower && Random.Range(0, branchoff_difficulty) == 0) // branch off because we are inside a hotspot
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

        IEnumerator GrowLoop()
        {
            isGrowing = true;

            while (nutrientsStock < maxStock)   // grow if below max stock
            {
                GrowNode();
                // dequeue any fully grown trees
                foreach (TreeBranch tree in trees)
                {
                    if (tree != null && tree.nodes.Count >= Constants.grown_tree_amount)
                    {
                        Debug.Log("Tree fully grown!");
                        Constants.score++;
                        tree.isGrowing = false;
                        trees.Remove(tree);
                        break;
                    }
                }
                //yield return new WaitForSeconds(0.0001f);
                yield return new WaitForSeconds(0.1f);
            }
        }


        void OnDrawGizmos()
        {
            if (nodes.Count != 0 && nodes[0] != null)
            {
                Handles.Label(nodes[0].position, "Fungal N: " + nutrientsStock);

                int start = Mathf.Max(0, nodes.Count - fungal_view_distance);
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

            foreach (SoilCell cell in cell_candidates)
            {
                if (cell != null)
                {
                    //draw yellow cube at cell pointer
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(cell.position, soil.cellSize * 0.5f);
                }
            }



        }
    }

}
