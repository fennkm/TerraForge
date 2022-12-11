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

        Vector2 normalisedCursorPos = WorldToNormalisedCoord(GetCursorPos());

        terrainMat.SetFloat("_CursorOffsetX", normalisedCursorPos.x);
        terrainMat.SetFloat("_CursorOffsetY", normalisedCursorPos.y);
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

    private Vector2 NormaliseCoords(Vector2 pos)
    {
        if (pos == Vector2.zero)
            return Vector2.one * -1;
        else
            return pos / terrainController.GetSize() + .5f.xx();
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
            cursorPos =  hit.point.xz();
        else
            cursorPos =  Vector2.zero;
    }

    public Vector2 GetCursorPos()
    {
        return cursorPos;
    }

    public Vector2 WorldToNormalisedCoord(Vector2 coord)
    {
        return coord / terrainController.GetSize() + .5f.xx();
    }

    public Vector2 WorldToTerrainMaskCoord(Vector2 coord)
    {
        return (coord + terrainController.GetSize().xx() / 2f) * terrainDensity;
    }

    public void PaintTerrain(Vector2 pos, float radius, float intensity, int type)
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
