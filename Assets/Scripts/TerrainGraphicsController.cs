using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VectorSwizzling;

public class TerrainGraphicsController : MonoBehaviour
{
    public Camera mainCamera;

    public Material terrainMat;
    public int terrainTypeNum;
    private float terrainDensity = 8f; // pixels per unit
    private float[][] terrainMaskVals;
    private Texture2DArray terrainMasks;

    private float terrainSize;
    private Vector3 cursorPos;

    private bool stickyCursor;
    private Vector3 stickyCursorPos;

    private bool cursorVisible = true;
    private bool terrainFocussed = true;
    private bool autoTerrain = true;

    public TerrainController terrainController;

    public void ShowCursor() { cursorVisible = true; }
    public void HideCursor() { cursorVisible = false; }
    
    public void Focus() { terrainFocussed = true; }
    public void Unfocus() { terrainFocussed = false; }

    void Start()
    {
        terrainSize = terrainController.GetSize();

        InitialiseTerrainTex();
    }

    void Update()
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

    public void InitialiseCursor(float scale, float minScale)
    {
        terrainMat.SetFloat("_InnerCursorScale", minScale * .5f / terrainController.GetSize());
        terrainMat.SetFloat("_OuterCursorScale", scale / terrainController.GetSize());
    }

    public void SetCursorSize(float scale)
    {
        terrainMat.SetFloat("_OuterCursorScale", scale / terrainController.GetSize());
    }

    private void PaintCursor()
    {
        UpdateCursorPos();

        if (!CursorActive())
        {
            terrainMat.SetInt("_PaintCursor", 0);
            return;
        }

        terrainMat.SetInt("_PaintCursor", 1);

        Vector2 normalisedOuterCursorPos = WorldToNormalisedCoord(GetCursorPos());
        Vector2 normalisedInnerCursorPos = WorldToNormalisedCoord(GetStickyCursorPos());

        terrainMat.SetFloat("_OuterCursorOffsetX", normalisedOuterCursorPos.x);
        terrainMat.SetFloat("_OuterCursorOffsetY", normalisedOuterCursorPos.y);
        terrainMat.SetFloat("_InnerCursorOffsetX", normalisedInnerCursorPos.x);
        terrainMat.SetFloat("_InnerCursorOffsetY", normalisedInnerCursorPos.y);
    }

    public bool CursorActive() 
    { 
        Vector2 normalisedCursorPos = WorldToNormalisedCoord(GetCursorPos());

        return 
            cursorVisible && terrainFocussed &&
            normalisedCursorPos.x <= 1f &&
            normalisedCursorPos.x >= 0f &&
            normalisedCursorPos.y <= 1f &&
            normalisedCursorPos.y >= 0f &&
            Input.mousePosition.x >= 0f &&
            Input.mousePosition.y >= 0f &&
            Input.mousePosition.x <= Screen.width &&
            Input.mousePosition.y <= Screen.height; 
    }

    private void UpdateCursorPos()
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
            cursorPos =  hit.point;
        else
            cursorPos =  Vector3.positiveInfinity;
    }

    public Vector3 GetCursorPos()
    {
        return cursorPos;
    }

    public Vector3 GetStickyCursorPos()
    {
        return (stickyCursor ? stickyCursorPos : GetCursorPos());
    }

    public Vector2 WorldToNormalisedCoord(Vector3 coord)
    {
        return coord.xz() / terrainController.GetSize() + .5f.xx();
    }

    public Vector2 WorldToTerrainMaskCoord(Vector3 coord)
    {
        return (coord.xz() + terrainController.GetSize().xx() / 2f) * terrainDensity;
    }

    public Vector3 TerrainMaskToWorldCoord(Vector2 coord)
    {
        return (coord / terrainDensity - terrainController.GetSize().xx() / 2f).x0y();
    }

    public void PaintTerrain(Vector3 pos, float radius, float intensity, int type)
    {
        Vector2 texPos = WorldToTerrainMaskCoord(pos);
        float texRadius = radius * terrainDensity;

        for (int j = Mathf.Max(0, (int) (texPos.y - texRadius)); j < Mathf.Min(terrainMasks.width - 1, texPos.y +  texRadius); j++)
            for (int i = Mathf.Max(0, (int) (texPos.x - texRadius)); i < Mathf.Min(terrainMasks.width - 1, texPos.x +  texRadius); i++)
            {
                float dist = (new Vector2(i, j) - texPos).magnitude;

                if (dist >= -texRadius && dist <= texRadius)
                {
                    float opacity = Mathf.Clamp(intensity * Mathf.Cos((dist * Mathf.PI) / (texRadius * 2)), 0f, 1f);
                    SetTerrain(new Vector2Int(i, j), opacity, type);
                }
            }

        ApplyTerrainTex();
    }

    public void AutoPaintTerrain(Vector3 pos, float radius)
    {
        if (!autoTerrain)
            return;

        Vector2 texPos = WorldToTerrainMaskCoord(pos);
        float texRadius = radius * terrainDensity;

        for (int j = Mathf.Max(0, (int) (texPos.y - texRadius)); j < Mathf.Min(terrainMasks.width - 1, texPos.y +  texRadius); j++)
            for (int i = Mathf.Max(0, (int) (texPos.x - texRadius)); i < Mathf.Min(terrainMasks.width - 1, texPos.x +  texRadius); i++)
            {
                float dist = (new Vector2(i, j) - texPos).magnitude;

                if (dist >= -texRadius && dist <= texRadius)
                {
                    float height = terrainController.HeightAtWorldPoint(TerrainMaskToWorldCoord(new Vector2(i, j)));

                    if (height <= 0f)
                        SetTerrain(new Vector2Int(i, j), 1, 0);
                    else if (height <= 2f)
                    {
                        SetTerrain(new Vector2Int(i, j), 1, 0);
                        SetTerrain(new Vector2Int(i, j), height / 2f, 1);
                    }
                    else if (height <= 5f)
                        SetTerrain(new Vector2Int(i, j), 1, 1);
                    else if (height <= 7f)
                    {
                        SetTerrain(new Vector2Int(i, j), 1, 1);
                        SetTerrain(new Vector2Int(i, j), (height - 5f) / 2f, 2);
                    }
                    else if (height <= 9f)
                        SetTerrain(new Vector2Int(i, j), 1, 2);
                    else if (height <= 10f)
                    {
                        SetTerrain(new Vector2Int(i, j), 1, 2);
                        SetTerrain(new Vector2Int(i, j), height - 9f, 3);
                    }
                    else
                    {
                        SetTerrain(new Vector2Int(i, j), 1, 3);
                    }
                }
            }

        ApplyTerrainTex();
    }

    public void SetTerrain(Vector2Int texPos, float opacity, int type)
    {
        foreach (float[] terrainMaskArr in terrainMaskVals)
            terrainMaskArr[texPos.y * terrainMasks.height + texPos.x] *= 1 - opacity;

        terrainMaskVals[type][texPos.y * terrainMasks.height + texPos.x] += opacity;
    }

    public void ApplyTerrainTex()
    {
        for (int i = 0; i < terrainTypeNum; i++)
            terrainMasks.SetPixelData<float>(terrainMaskVals[i], 0, i);

        terrainMasks.Apply();
        terrainMat.SetTexture("_TerrainMasks", terrainMasks);
    }

    public void SetStickyCursor(Vector3 pos)
    {
        stickyCursorPos = pos;
        stickyCursor = true;
    }

    public void ClearStickyCursor()
    {
        stickyCursor = false;
    }

    public void ToggleAutoTerrain()
    {
        autoTerrain = !autoTerrain;
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
