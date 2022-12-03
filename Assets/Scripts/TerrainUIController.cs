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

    public Material terrainMat;
    public int terrainTypeNum;
    private float terrainDensity = 8f; // pixels per unit
    private float[][] terrainMaskVals;
    private Texture2DArray terrainMasks;

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

        InitialiseTerrainTex();
    }

    void Update()
    {
    }

    void FixedUpdate()
    {
        PaintCursor();
    }

    private void InitialiseTerrainTex()
    {
        int terrainResolution = (int) (terrainController.GetSize() * terrainDensity);

        terrainMasks = new Texture2DArray(terrainResolution, terrainResolution,
            terrainTypeNum, TextureFormat.RFloat, false, true);

        terrainMaskVals = new float[terrainTypeNum][];

        for (int i = 0; i < terrainTypeNum; i++)
            terrainMaskVals[i] = new float[terrainMasks.width * terrainMasks.height];

        for (int i = 0; i < terrainMaskVals[0].Length; i++) terrainMaskVals[0][i] = 1f;

        ApplyTerrainTex();
    }

    public float GetTerrainSize() { return terrainSize; }

    public void SetCursorSize(float val)
    {
        outerCursor.sizeDelta = val.xx0();
    }

    private void PaintCursor()
    {
        terrainMousePos = GetMouseTerrainPoint().xz();

        if (!CursorActive())
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

    public bool CursorActive() { 
        return 
            cursorVisible && terrainFocussed &&
            terrainMousePos != Vector2.zero &&
            terrainMousePos.x <=  terrainSize / 2 &&
            terrainMousePos.x >= -terrainSize / 2 &&
            terrainMousePos.y <=  terrainSize / 2 &&
            terrainMousePos.y >= -terrainSize / 2 &&
            Input.mousePosition.x >= 0 &&
            Input.mousePosition.y >= 0 &&
            Input.mousePosition.x <= Screen.width &&
            Input.mousePosition.y <= Screen.height; 
    }

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

    public void PaintTerrain(Vector2 pos, float r, float intensity, int type)
    {
        Vector2 texPos = pos * terrainDensity;
        float texRadius = r * terrainDensity;

        for (int j = Mathf.Max(0, (int) (texPos.y - texRadius)); j < Mathf.Min(terrainMasks.width - 1, texPos.y +  texRadius); j++)
            for (int i = Mathf.Max(0, (int) (texPos.x - texRadius)); i < Mathf.Min(terrainMasks.width - 1, texPos.x +  texRadius); i++)
            {
                float dist = (new Vector2(i, j) - texPos).magnitude;

                if (dist >= -texRadius && dist <= texRadius)
                {
                    float val = Mathf.Clamp(intensity * Mathf.Cos((dist * Mathf.PI) / (texRadius * 2)), 0f, 1f);

                    foreach (float[] terrainMaskArr in terrainMaskVals)
                        terrainMaskArr[j * terrainMasks.height + i] *= 1 - val;
                    terrainMaskVals[type][j * terrainMasks.height + i] += val;
                }
            }

        ApplyTerrainTex();
    }

    public void ApplyTerrainTex()
    {
        for (int i = 0; i < terrainTypeNum; i++)
            terrainMasks.SetPixelData<float>(terrainMaskVals[i], 0, i);

        terrainMasks.Apply();
        terrainMat.SetTexture("_TerrainMasks", terrainMasks);
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
