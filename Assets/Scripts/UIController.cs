using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ToolButton[] buttons;
    private int menuOpen;
    private int toolSelected;

    public TerrainGraphicsController terrainGraphicsController;
    public GameObject settingsMenu;
    public ToolButton settingsButton;

    private const int MENU_NONE = -2;
    private const int MENU_SETTINGS = -1;

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
        terrainGraphicsController.Unfocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        terrainGraphicsController.Focus();
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
        else if (menuOpen != MENU_NONE)
            CloseMenu(menuOpen);

        if (index == MENU_SETTINGS)
        {
            settingsButton.SetSelected(true);
            settingsMenu.SetActive(true);
        }
        else
            buttons[index].menu.SetActive(true);

        menuOpen = index;
    }

    private void CloseMenu(int index)
    {
        if (menuOpen != index)
            return;
        
        if (index == MENU_SETTINGS)
        {
            settingsButton.SetSelected(false);
            settingsMenu.SetActive(false);
        }
        else
            buttons[index].menu.SetActive(false);

        menuOpen = MENU_NONE;
    }

    private void CloseMenus()
    {
        foreach (ToolButton button in buttons)
            button.menu.SetActive(false);

        settingsButton.SetSelected(false);
        settingsMenu.SetActive(false);

        menuOpen = MENU_NONE;
    }

    public void ToggleMenu()
    {
        if (menuOpen == MENU_SETTINGS)
            CloseMenu(MENU_SETTINGS);
        else
            OpenMenu(MENU_SETTINGS);
    }
}
