using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ToolButton[] buttons;
    private int menuOpen;
    private int toolSelected;

    public TerrainUIController terrainUIController;

    // Start is called before the first frame update
    void Start()
    {
        CloseMenus();

        toolSelected = 0;
        buttons[0].SetSelected(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GetToolIndex() { return toolSelected; }

    public void OnPointerEnter(PointerEventData eventData)
    {
        terrainUIController.Unfocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        terrainUIController.Focus();
    }

    public void ClickTool(int index)
    {
        SelectTool(index);
        
        if (menuOpen == index)
            CloseMenu(index);
        else
            OpenMenu(index);
    }

    private void SelectTool(int index)
    {
        if (toolSelected != index)
        {
            buttons[toolSelected].SetSelected(false);
            buttons[index].SetSelected(true);
            toolSelected = index;
        }
    }

    private void OpenMenu(int index)
    {
        if (menuOpen == index)
            return;
        else if (menuOpen != -1)
            CloseMenu(menuOpen);

        buttons[index].menu.SetActive(true);
        menuOpen = index;
    }

    private void CloseMenu(int index)
    {
        if (menuOpen != index)
            return;
        
        buttons[index].menu.SetActive(false);
        menuOpen = -1;
    }

    private void CloseMenus()
    {
        foreach (ToolButton button in buttons)
            button.menu.SetActive(false);

        menuOpen = -1;
    }
}
