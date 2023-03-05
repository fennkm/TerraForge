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

    public Vector2 WorldToMeshCoord(Vector3 coord)
    {
        return (coord.xz() + GetSize().xx() / 2f) * actualDensity;
    }

    public float HeightAtWorldPoint(Vector3 worldPos)
    {
        Vector2 meshPoint = WorldToMeshCoord(worldPos);

        Vector2Int a = new Vector2Int(Mathf.FloorToInt(meshPoint.x), Mathf.FloorToInt(meshPoint.y));
        Vector2Int b = a + new Vector2Int(1, 0);
        Vector2Int c = a + new Vector2Int(0, 1);
        Vector2Int d = a + new Vector2Int(1, 1);

        Vector2 uv = meshPoint - a;

        float height = QuadLerp(heightMap[a.x, a.y],
                                heightMap[b.x, b.y],
                                heightMap[c.x, c.y],
                                heightMap[d.x, d.y],
                                uv.x, uv.y);

        return height;
    }

    private float QuadLerp(float a, float b, float c, float d, float u, float v)
    {
        float abu = Mathf.Lerp(a, b, u);
        float dcu = Mathf.Lerp(d, c, u);
        return Mathf.Lerp(abu, dcu, v);
    }

    public void RaiseArea(Vector3 pos, float radius, float intensity)
    {
        Vector2 meshPos = WorldToMeshCoord(pos);
        float meshRadius = radius * actualDensity;

        for (int i = Mathf.Max(0, (int) (meshPos.x - meshRadius)); i < Mathf.Min(resolution, meshPos.x + meshRadius); i++)
            for (int j = Mathf.Max(0, (int) (meshPos.y - meshRadius)); j < Mathf.Min(resolution, meshPos.y + meshRadius); j++)
            {
                float dist = (new Vector2(i, j) - meshPos).magnitude;

                if (dist > meshRadius)
                    continue;

                float val = (dist / (meshRadius * 2f)) * Mathf.PI;

                AddHeight(i, j, Mathf.Cos(val) * intensity * Time.deltaTime);
            }

        ApplyHeightMap(
            Mathf.Max(0, Mathf.FloorToInt(meshPos.x - meshRadius)),
            Mathf.Min(resolution - 1, Mathf.CeilToInt(meshPos.x + meshRadius)),
            Mathf.Max(0, Mathf.FloorToInt(meshPos.y - meshRadius)),
            Mathf.Min(resolution - 1, Mathf.CeilToInt(meshPos.y + meshRadius)));
    }

    public void SetAreaToHeight(Vector3 pos, float radius, float intensity, float heightTo, bool bringDown, bool bringUp)
    {
        Vector2 meshPos = WorldToMeshCoord(pos);
        float meshRadius = radius * actualDensity;

        for (int i = Mathf.Max(0, (int) (meshPos.x - meshRadius)); i < Mathf.Min(resolution, meshPos.x + meshRadius); i++)
            for (int j = Mathf.Max(0, (int) (meshPos.y - meshRadius)); j < Mathf.Min(resolution, meshPos.y + meshRadius); j++)
            {
                if (heightMap[i, j] == heightTo)
                    continue;

                float dist = (new Vector2(i, j) - meshPos).magnitude;

                if (dist > meshRadius)
                    continue;

                float m = intensity;
                float h = heightTo;
                float a = heightMap[i, j];
                float r = meshRadius;
                float x = dist;
                
                float linearFunc = ((a - h) / (r * (1 - m))) * (x - r) + a;
                float boundedFunc = (a >= h ? Mathf.Max(linearFunc, h) : Mathf.Min(linearFunc, h));
                float smoothedFunc = 0.5f * ((h - a) * Mathf.Cos(Mathf.PI * ((boundedFunc - h) / (h - a))) + a + h);
                
                if (!bringDown && heightMap[i, j] >= smoothedFunc)
                    continue;

                if (!bringUp && heightMap[i, j] <= smoothedFunc)
                    continue;

                SetHeight(i, j, smoothedFunc);
            }

        ApplyHeightMap(
            Mathf.Max(0, Mathf.FloorToInt(meshPos.x - meshRadius)),
            Mathf.Min(resolution - 1, Mathf.CeilToInt(meshPos.x + meshRadius)),
            Mathf.Max(0, Mathf.FloorToInt(meshPos.y - meshRadius)),
            Mathf.Min(resolution - 1, Mathf.CeilToInt(meshPos.y + meshRadius)));
    }

    public void SmoothArea(Vector3 pos, float radius, float intensity)
    {
        Vector2 meshPos = WorldToMeshCoord(pos);
        float meshRadius = radius * actualDensity;

        for (int i = Mathf.Max(0, (int) (meshPos.x - meshRadius)); i < Mathf.Min(resolution, meshPos.x + meshRadius); i++)
            for (int j = Mathf.Max(0, (int) (meshPos.y - meshRadius)); j < Mathf.Min(resolution, meshPos.y + meshRadius); j++)
            {
                float dist = (new Vector2(i, j) - meshPos).magnitude;

                if (dist > meshRadius)
                    continue;
                
                float avgHeight = 0;
                float pointsTaken = 0;

                float sampleRadius = meshRadius / 2;

                for (int u = 0; u < sampleRadius; u++)
                    for (int v = 0; v < sampleRadius; v++)
                        if (i + u >= 0 && 
                            i + u < resolution && 
                            j + v >= 0 && 
                            j + v < resolution && 
                            (new Vector2(u, v) - new Vector2(i, j)).magnitude >= sampleRadius)
                        {
                            avgHeight += heightMap[i + u, j + v];
                            pointsTaken++;
                        }

                avgHeight /= pointsTaken;

                float interpolationVal = dist / meshRadius;

                float val = Mathf.Lerp(avgHeight, heightMap[i, j], interpolationVal);

                float height = Mathf.Lerp(heightMap[i, j], val, intensity);

                SetHeight(i, j, height);
            }

        ApplyHeightMap(
            Mathf.Max(0, Mathf.FloorToInt(meshPos.x - meshRadius)),
            Mathf.Min(resolution - 1, Mathf.CeilToInt(meshPos.x + meshRadius)),
            Mathf.Max(0, Mathf.FloorToInt(meshPos.y - meshRadius)),
            Mathf.Min(resolution - 1, Mathf.CeilToInt(meshPos.y + meshRadius)));
    }

    public void RoughenArea(Vector3 pos, float radius, float intensity, float seed)
    {
        Vector2 meshPos = WorldToMeshCoord(pos);
        float meshRadius = radius * actualDensity;

        for (int i = Mathf.Max(0, (int) (meshPos.x - meshRadius)); i < Mathf.Min(resolution, meshPos.x + meshRadius); i++)
            for (int j = Mathf.Max(0, (int) (meshPos.y - meshRadius)); j < Mathf.Min(resolution, meshPos.y + meshRadius); j++)
            {
                float dist = (new Vector2(i, j) - meshPos).magnitude;
                
                if (dist > meshRadius)
                    continue;

                float density = 2f;

                float noiseVal = Mathf.PerlinNoise(density * i / meshRadius + seed, density * j / meshRadius + seed) - 0.5f;

                float val = noiseVal * intensity * Time.deltaTime * (1 - dist / meshRadius);

                AddHeight(i, j, val);
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