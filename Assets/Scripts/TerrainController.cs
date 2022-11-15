using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TerrainMesh;

public class TerrainController : MonoBehaviour
{
    public float maxHeight;
    public float shallowWaterDepth;
    public float maxWaterDepth;
    public Color shallowWaterColour;
    public Color deepWaterColour;

    private float density = 1f; // Squares per unit
    private float size = 128f;
    private int resolution;
    private float[,] heightMap;

    public MeshFilter groundMeshFilter;
    public MeshFilter seaMeshFilter;

    private TerrainBuilder terrainBuilder;

    void Awake()
    {
        terrainBuilder = new TerrainBuilder(size, density, groundMeshFilter, seaMeshFilter);

        resolution = (int) (size * density) + 1;

        heightMap = new float[resolution, resolution];

        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
                heightMap[i, j] = -maxWaterDepth;

        ApplyHeightMap();
    }

    public void ApplyHeightMap()
    {
        UpdateSeaTex();
        terrainBuilder.UpdateHeightMap(heightMap);
    }

    private void UpdateSeaTex()
    {
        Texture2D waterTex = new Texture2D(resolution, resolution);

        for (int i = 0; i < resolution; i++)
            for (int j = 0;  j < resolution; j++)
            {
                float depthValue = Mathf.InverseLerp(shallowWaterDepth, maxWaterDepth, -heightMap[i, j]);
                waterTex.SetPixel(i, j, Color.Lerp(shallowWaterColour, deepWaterColour, depthValue));
            }

        waterTex.Apply();

        seaMeshFilter.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", waterTex);
        seaMeshFilter.GetComponent<MeshRenderer>().material.SetFloat("_Resolution", resolution);
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

    public float GetSize() { return size; }
    public float GetDensity() { return density; }
    public int GetResolution() { return resolution; }
}