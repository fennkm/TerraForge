using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TerrainMesh;

public class TerrainController : MonoBehaviour
{
    public float height1;
    public float radius1;
    public Vector2 offset1;
    public float height2;
    public float radius2;
    public Vector2 offset2;
    
    public float minWaterDepth;
    public float maxWaterDepth;
    public Color shallowWaterColour;
    public Color deepWaterColour;

    private float prevHeight1;
    private float prevRadius1;
    private Vector2 prevOffset1;
    private float prevHeight2;
    private float prevRadius2;
    private Vector2 prevOffset2;

    private float density = 1f; // Squares per unit
    private float size = 128f;
    private float[,] heightMap;

    public MeshFilter groundMeshFilter;
    public MeshFilter seaMeshFilter;

    private TerrainUIPainter terrainUIPainter;
    private TerrainBuilder terrainBuilder;

    void Awake()
    {
        terrainUIPainter = GetComponent<TerrainUIPainter>();

        terrainBuilder = new TerrainBuilder(size, density, groundMeshFilter, seaMeshFilter);
    }

    void Start()
    {
        terrainUIPainter.setTerrainSize(size);
    }

    // Update is called once per frame
    void Update()
    {
        if (prevHeight1 != height1 || prevRadius1 != radius1 || prevOffset1 != offset1
        || prevHeight2 != height2 || prevRadius2 != radius2 || prevOffset2 != offset2)
        {
            GenerateHeightMap();
            terrainBuilder.UpdateHeightMap(heightMap);
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.mouseScrollDelta.y < 0)
            terrainUIPainter.DecreaseCursorSize();
        else if (Input.GetKey(KeyCode.LeftShift) && Input.mouseScrollDelta.y > 0)
            terrainUIPainter.IncreaseCursorSize();
    }

    void FixedUpdate()
    {
        terrainUIPainter.PaintCursor();
    }

    private void GenerateHeightMap()
    {
        int resolution = (int) (size * density) + 1;

        prevHeight1 = height1;
        prevRadius1 = radius1;
        prevOffset1 = offset1;
        prevHeight2 = height2;
        prevRadius2 = radius2;
        prevOffset2 = offset2;

        heightMap = new float[resolution, resolution];

        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
            {
                heightMap[i, j] = -maxWaterDepth;

                Vector2 disp1 = 
                    new Vector2(i - resolution / 2f + 0.5f, j - resolution / 2f + 0.5f) - offset1 * density;

                float dist1 = disp1.magnitude;

                float val1 = (dist1 / (radius1 * density)) * Mathf.PI;

                if (val1 >= -Mathf.PI && val1 <= Mathf.PI)
                    heightMap[i, j] += (Mathf.Sin(val1 + Mathf.PI / 2) + 1) * height1;

                Vector2 disp2 = 
                    new Vector2(i - resolution / 2f + 0.5f, j - resolution / 2f + 0.5f) - offset2 * density;

                float dist2 = disp2.magnitude;

                float val2 = (dist2 / (radius2 * density)) * Mathf.PI;

                if (val2 >= -Mathf.PI && val2 <= Mathf.PI)
                    heightMap[i, j] += (Mathf.Sin(val2 + Mathf.PI / 2) + 1) * height2;

                if (
                    i < density * size / 10 || j < density * size / 10 ||
                    i > (resolution - 1) - density * size / 10 ||
                    j > (resolution - 1) - density * size / 10
                    )
                    heightMap[i, j] = -maxWaterDepth;
            }

        Texture2D waterTex = new Texture2D(resolution, resolution);

        for (int i = 0; i < resolution; i++)
            for (int j = 0;  j < resolution; j++)
            {
                float depthValue = Mathf.InverseLerp(minWaterDepth, maxWaterDepth, -heightMap[i, j]);
                waterTex.SetPixel(i, j, Color.Lerp(shallowWaterColour, deepWaterColour, depthValue));
            }

        waterTex.Apply();

        seaMeshFilter.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", waterTex);
        seaMeshFilter.GetComponent<MeshRenderer>().material.SetFloat("_Resolution", resolution);
    }
}