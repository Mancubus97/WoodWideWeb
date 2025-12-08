using UnityEngine;

namespace WoodWideWeb
{
    public class UI : MonoBehaviour
    {


        public TextMesh textMesh;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            textMesh = GetComponent<TextMesh>();
            textMesh.text = "FPS: " + (1.0f / Time.deltaTime).ToString("F2") + "\n";
            //textMesh.text += "Total Nodes: " + FungalBranch.Instance.nodes.Count;
            //foreach (FungalBranch branch in FungalBranch.FindAnyObjectByType<FungalBranch>().branches)
            //{
            //    textMesh.text += "Branch Nodes: " + branch.nodes.Count + "\n";
            //}
            //textMesh.text += "\nNutrient Stock: " + FungalBranch.Instance.nutrientsStock.ToString("F2") + "\n";
        }
    }

}