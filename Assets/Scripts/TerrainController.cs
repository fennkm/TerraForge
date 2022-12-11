using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VectorSwizzling;

using TerrainMesh;

public class TerrainController : MonoBehaviour
{
    public float maxHeight;
    public float shallowWaterDepth;
    public float maxWaterDepth;
    public Color shallowWaterColour;
    public Color deepWaterColour;

    public GameObject groundChunk;
    public GameObject seaChunk;

    public ComputeShader seaMeshGenerator;

    private float densityGoal = 4f; // Squares per unit
    private float actualDensity;
    private float size = 128f;
    private int resolution;
    private int chunkRes;
    private float[,] heightMap;
    private Texture2D waterTex;

    public MeshFilter[,] groundChunkMeshFilters;
    public MeshFilter[,] seaChunkMeshFilters;

    private TerrainBuilder terrainBuilder;

    void Awake()
    {
        chunkRes = Mathf.CeilToInt((Mathf.Pow((size * densityGoal), 2) * 4) / 65535f);

        groundChunkMeshFilters = new MeshFilter[chunkRes, chunkRes];
        seaChunkMeshFilters = new MeshFilter[chunkRes, chunkRes];

        float chunkSize = GetSize() / chunkRes;

        for (int i = 0; i < chunkRes; i++)
            for (int j = 0; j < chunkRes; j++)
            {
                Vector2 chunkPos = new Vector2(
                    (i - (chunkRes - 1) / 2f) * chunkSize,
                    (j - (chunkRes - 1) / 2f) * chunkSize);

                groundChunkMeshFilters[i, j] = 
                    Instantiate(groundChunk, chunkPos.x0y(), Quaternion.identity, transform).GetComponent<MeshFilter>();

                seaChunkMeshFilters[i, j] = 
                    Instantiate(seaChunk, chunkPos.x0y(), Quaternion.identity, transform).GetComponent<MeshFilter>();
            }

        terrainBuilder = new TerrainBuilder(size, densityGoal, groundChunkMeshFilters, seaChunkMeshFilters, maxWaterDepth, seaMeshGenerator);

        resolution = terrainBuilder.GetMeshResolution();

        actualDensity = (resolution - 1) / size;

        heightMap = new float[resolution, resolution];

        waterTex = new Texture2D(resolution, resolution);

        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
                heightMap[i, j] = -maxWaterDepth;

        ApplyHeightMap();
    }

    public void ApplyHeightMap(int xFrom, int xTo, int yFrom, int yTo)
    {
        UpdateSeaTex(xFrom, xTo, yFrom, yTo);
        terrainBuilder.UpdateHeightMap(heightMap, xFrom, xTo, yFrom, yTo);
    }

    public void ApplyHeightMap()
    {
        ApplyHeightMap(0, resolution - 1, 0, resolution - 1);
    }

    private void UpdateSeaTex(int xFrom, int xTo, int yFrom, int yTo)
    {
        for (int i = xFrom; i <= xTo; i++)
            for (int j = yFrom;  j <= yTo; j++)
            {
                float depthValue = Mathf.InverseLerp(shallowWaterDepth, maxWaterDepth, -heightMap[i, j]);
                waterTex.SetPixel(i, j, Color.Lerp(shallowWaterColour, deepWaterColour, depthValue));
            }

        waterTex.Apply();

        foreach (MeshFilter meshFilter in seaChunkMeshFilters)
            meshFilter.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", waterTex);
    }

    public void SetHeight(int x, int y, float val)
    {
        if (x > 0 && x < resolution - 1 && y > 0 && y < resolution - 1)
            heightMap[x, y] = Mathf.Clamp(val, -maxWaterDepth, maxHeight);
    }

    public void AddHeight(int x, int y, float val)
    {
        if (x > 0 && x < resolution - 1 && y > 0 && y < resolution - 1)
            heightMap[x, y] = Mathf.Clamp(heightMap[x, y] + val, -maxWaterDepth, maxHeight);
    }

    public Vector2 WorldToMeshCoord(Vector2 coord)
    {
        return (coord + GetSize().xx() / 2f) * actualDensity;
    }

    public void RaiseArea(Vector2 pos, float radius, float intensity)
    {
        Vector2 meshPos = WorldToMeshCoord(pos);
        float meshRadius = radius * actualDensity;

        for (int i = Mathf.Max(0, (int) (meshPos.x - meshRadius)); i < Mathf.Min(resolution, meshPos.x + meshRadius); i++)
            for (int j = Mathf.Max(0, (int) (meshPos.y - meshRadius)); j < Mathf.Min(resolution, meshPos.y + meshRadius); j++)
            {
                float dist = (new Vector2(i, j) - meshPos).magnitude;
                float val = (dist / (meshRadius * 2f)) * Mathf.PI;

                if (val >= -Mathf.PI / 2 && val <= Mathf.PI / 2)
                    AddHeight(i, j, Mathf.Cos(val) * intensity * Time.deltaTime);
            }

        ApplyHeightMap(
            Mathf.Max(0, Mathf.FloorToInt(meshPos.x - meshRadius)),
            Mathf.Min(resolution - 1, Mathf.CeilToInt(meshPos.x + meshRadius)),
            Mathf.Max(0, Mathf.FloorToInt(meshPos.y - meshRadius)),
            Mathf.Min(resolution - 1, Mathf.CeilToInt(meshPos.y + meshRadius)));
    }

    public float GetSize() { return size; }
    public float GetDensity() { return actualDensity; }
    public int GetResolution() { return resolution; }
}