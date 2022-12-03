using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ToolButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite defaultImg;
    public Sprite selectedImg;
    public GameObject menu;
    public GameObject toolTip;
    
    private bool hovering;
    private float hoverTimer;

    private float toolTipAppearTime = .6f;

    private Image texture;
    private Button button;

    // Start is called before the first frame update
    void Start()
    {
        texture = GetComponent<Image>();
        texture.sprite = defaultImg;

        toolTip.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (hovering)
        {
            hoverTimer += Time.deltaTime;

            if (hoverTimer >= toolTipAppearTime)
            {
                toolTip.transform.position = Input.mousePosition;
                toolTip.SetActive(true);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        hoverTimer = 0f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        toolTip.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
            texture.sprite = selectedImg;
        else
            texture.sprite = defaultImg;
    }
}
