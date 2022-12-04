using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tools;

public class ToolController : MonoBehaviour
{
    public UIController uiController;
    private TerrainGraphicsController terrainGraphicsController;

    private Tool[] toolList;
    private Tool selectedTool;


    public float resizeFactor;

    // Start is called before the first frame update
    void Start()
    {
        toolList = new Tool[uiController.buttons.Length];

        for (int i = 0; i < toolList.Length; i++)
            toolList[i] = uiController.buttons[i].GetComponent<Tool>();

        selectedTool = toolList[uiController.GetToolIndex()];
        selectedTool.Initialise();

        terrainGraphicsController = uiController.terrainGraphicsController;
    }

    // Update is called once per frame
    void Update()
    {
        if (terrainGraphicsController.CursorActive())
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.mouseScrollDelta.y < 0)
                ModifyCursorSize(-1);
            else if (Input.GetKey(KeyCode.LeftShift) && Input.mouseScrollDelta.y > 0)
                ModifyCursorSize(1);

            if (Input.GetMouseButtonDown(0))
                selectedTool.Click();

            if (Input.GetMouseButton(0))
                selectedTool.Hold();
        }
    }

    public void SelectTool(int index)
    {
        selectedTool = toolList[index];
        selectedTool.Initialise();
    }

    private void ModifyCursorSize(int dir)
    {
        selectedTool.Resize(dir * resizeFactor);
    }
}
