using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VectorSwizzling;

public class TerrainUIController : MonoBehaviour
{
    public Camera mainCamera;
    public Camera uiCamera;
    public RectTransform floorCanvas;
    public RectTransform innerCursor;
    public RectTransform outerCursor;

    private float terrainSize;

    private Vector2 terrainMousePos;
    private Vector2 cursorPos;

    private bool cursorVisible = true;
    private bool terrainFocussed = true;

    public TerrainController terrainController;

    public void ShowCursor() { cursorVisible = true; }
    public void HideCursor() { cursorVisible = false; }
    
    public void Focus() { terrainFocussed = true; }
    public void Unfocus() { terrainFocussed = false; }

    void Start()
    {
        terrainSize = terrainController.GetSize();
        uiCamera.orthographicSize = terrainSize / 2;
    }

    void Update()
    {
    }

    void FixedUpdate()
    {
        PaintCursor();
    }

    public float GetTerrainSize() { return terrainSize; }

    public void SetCursorSize(float val)
    {
        outerCursor.sizeDelta = val.xx0();
    }

    private void PaintCursor()
    {
        terrainMousePos = GetMouseTerrainPoint().xz();

        if (!CursorActive() ||
            terrainMousePos == Vector2.zero ||
            terrainMousePos.x >  terrainSize / 2 ||
            terrainMousePos.x < -terrainSize / 2 ||
            terrainMousePos.y >  terrainSize / 2 ||
            terrainMousePos.y < -terrainSize / 2)
        {
            innerCursor.gameObject.SetActive(false);
            outerCursor.gameObject.SetActive(false);
            return;
        }
        
        innerCursor.gameObject.SetActive(true);
        outerCursor.gameObject.SetActive(true);

        cursorPos = terrainMousePos * (floorCanvas.sizeDelta.x / terrainSize);

        innerCursor.localPosition = cursorPos;
        outerCursor.localPosition = cursorPos;
    }

    public bool CursorActive() { return cursorVisible && terrainFocussed; }

    private Vector3 GetMouseTerrainPoint()
    {
        RaycastHit hit;

        Vector3 mouseVector = 
            mainCamera.ScreenToWorldPoint(Input.mousePosition.xy0() + mainCamera.nearClipPlane._00x()) -
            mainCamera.transform.position;

        if (Physics.Raycast(
                mainCamera.transform.position,
                mouseVector,
                out hit,
                Mathf.Infinity,
                1 << 3))
            return hit.point;
        else
            return Vector3.zero;
    }

    public Vector2 GetMouseGridPos()
    {
        if (terrainMousePos == Vector2.zero)
            return Vector2.one * -1;
        else
            return (terrainMousePos + (terrainController.GetSize() / 2).xx()) / terrainController.GetDensity();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        RaycastHit hit;

        Vector3 mouseVector = 
            mainCamera.ScreenToWorldPoint(Input.mousePosition.xy0() + mainCamera.nearClipPlane._00x()) -
            mainCamera.transform.position;

        Physics.Raycast(
                mainCamera.transform.position,
                mouseVector,
                out hit,
                Mathf.Infinity,
                1 << 3);

        Gizmos.DrawLine(mainCamera.transform.position, hit.point);
    }

    public float UIToWorld()
    {
        return GetTerrainSize() / floorCanvas.sizeDelta.x;
    }

    public float WorldToUI()
    {
        return floorCanvas.sizeDelta.x / GetTerrainSize();
    }
}
