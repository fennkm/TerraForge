using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VectorSwizzling;

public class TerrainUIPainter : MonoBehaviour
{
    public Camera mainCamera;
    public Camera uiCamera;
    public RectTransform floorCanvas;
    public RectTransform innerCursor;
    public RectTransform outerCursor;

    public float resizeFactor;
    public float minSize;
    public float maxSize;

    private float terrainSize;

    private Vector2 terrainMousePos;
    private Vector2 cursorPos;

    private bool cursorVisible;

    public void setTerrainSize(float terrainSize)
    {
        uiCamera.orthographicSize = terrainSize / 2;
        this.terrainSize = terrainSize;
    }

    public void showCursor() { cursorVisible = true; }
    public void hideCursor() { cursorVisible = false; }
    public void increaseCursorSize() { modifyCursorSize(1);  }
    public void decreaseCursorSize() { modifyCursorSize(-1); }

    private void modifyCursorSize(int dir)
    {
        outerCursor.sizeDelta = 
            Mathf.Clamp(outerCursor.sizeDelta.x * (1 + dir * resizeFactor), minSize, maxSize).xx0();
    }

    public void paintCursor()
    {
        terrainMousePos = getMouseTerrainPoint().xz();

        if (!cursorVisible ||
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

    private Vector3 getMouseTerrainPoint()
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
}
