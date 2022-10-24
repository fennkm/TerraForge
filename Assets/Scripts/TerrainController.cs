using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TerrainController : MonoBehaviour
{
    public float height;
    public float radius;
    public Vector2 offset;

    private float prevHeight;
    private float prevRadius;
    private Vector2 prevOffset;

    private float vertDensity = 5f; // Verts per unit
    private float landSize = 20f;
    private float size;
    private int resolution; // Number of verts on each side

    private Vector2[,] points;

    private float[,] heightMap;

    public MeshFilter groundMeshFilter;
    public MeshFilter seaMeshFilter;
    private Mesh groundMesh;
    private Mesh seaMesh;

    // Start is called before the first frame update
    void Start()
    {
        groundMesh = groundMeshFilter.mesh;
        seaMesh = seaMeshFilter.mesh;

        size = landSize + 1;

        resolution = (int) (vertDensity * size);

        points = new Vector2[resolution, resolution];

        float vertSeparation = size / (resolution - 1);
        Vector2 topRight = new Vector2(size / 2, size / 2);

        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
                points[i,j] = new Vector2(vertSeparation * i, vertSeparation * j) - topRight;

        GenerateHeightMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (prevHeight != height || prevRadius != radius || prevOffset != offset)
            GenerateHeightMap();
    }

    private void GenerateHeightMap()
    {
        prevHeight = height;
        prevRadius = radius;
        prevOffset = offset;

        heightMap = new float[resolution, resolution];

        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
            {
                Vector2 disp = 
                    new Vector2(i - resolution / 2f + 0.5f, j - resolution / 2f + 0.5f) - offset * vertDensity;

                float dist = disp.magnitude;

                heightMap[i, j] = -1;

                float val = (dist / (radius * vertDensity)) * Mathf.PI;

                if (val >= -Mathf.PI && val <= Mathf.PI)
                    heightMap[i, j] += (Mathf.Sin(val + Mathf.PI / 2) + 1) * height;

                if (
                    i < vertDensity || j < vertDensity ||
                    i > resolution - vertDensity - 1 ||
                    j > resolution - vertDensity - 1
                    )
                    heightMap[i, j] = -1;
            }

        UpdateMeshes();
    }

    private void UpdateMeshes()
    {
        UpdateGroundMesh();
        UpdateSeaMesh();
    }

    private void UpdateGroundMesh()
    {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 2 * 6];

        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
                vertices[i * resolution + j] = new Vector3(points[i, j].x, heightMap[i, j], points[i, j].y);

        for (int i = 0; i < resolution - 1; i++)
            for (int j = 0; j < resolution - 1; j++)
            {
                int index = ((i * resolution) + j) * 6;

                triangles[index] = i * resolution + j;
                triangles[index + 1] = i * resolution + (j + 1);
                triangles[index + 2] = (i + 1) * resolution + j;
                triangles[index + 3] = (i + 1) * resolution + (j + 1);
                triangles[index + 4] = (i + 1) * resolution + j;
                triangles[index + 5] = i * resolution + (j + 1);
            }

        groundMesh.vertices = vertices;
        groundMesh.triangles = triangles;
        groundMesh.RecalculateNormals();
    }

    private void UpdateSeaMesh()
    {

        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();
        List<Vector3> vertexList = new List<Vector3>();
        List<int> triangleList = new List<int>();

        int index = 0;

        int AddVert(Vector3 vert)
        {
            try
            {
                return vertexMap[vert];
            }
            catch (KeyNotFoundException)
            {
                vertexMap.Add(vert, index);
                vertexList.Add(vert);
                return index++;
            }
        }

        void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            int v0 = AddVert(a);
            int v1 = AddVert(b);
            int v2 = AddVert(c);

            triangleList.Add(v0);
            triangleList.Add(v1);
            triangleList.Add(v2);
        }

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(a, b, d);
            AddTriangle(b, c, d);
        }

        void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
        {
            AddTriangle(a, b, e);
            AddTriangle(b, c, e);
            AddTriangle(c, d, e);
        }

        bool[,] filled = new bool[resolution - 1, resolution - 1];

        int s = 2;

        while (s < resolution) s <<= 1;

        s >>= 1;

        for (; s > 1; s >>= 1)
        {
            for (int i = 0; i < resolution - s; i += s)
                for (int j = 0; j < resolution - s; j += s)
                {
                    if (filled[i, j])
                        continue;

                    bool empty = true;
                    for (int x = 0; x <= s; x++)
                        for (int y = 0; y <= s; y++)
                            empty &= heightMap[i + x, j + y] < 0;
                    
                    if (empty)
                    {
                        Vector3 v0 = new Vector3(points[i, j].x, 0f, points[i, j].y);
                        Vector3 v1 = new Vector3(points[i + s, j].x, 0f, points[i + s, j].y);
                        Vector3 v2 = new Vector3(points[i, j + s].x, 0f, points[i, j + s].y);
                        Vector3 v3 = new Vector3(points[i + s, j + s].x, 0f, points[i + s, j + s].y);

                        AddQuad(v0, v2, v3, v1);

                        for (int x = 0; x < s; x++)
                            for (int y = 0; y < s; y++)
                                filled[i + x, j + y] = true;
                    }
                }
        }

        float square = size / (resolution - 1);

        for (int i = 0; i < resolution - 1; i++)
            for (int j = 0; j < resolution - 1; j++)
            {
                if (filled[i, j])
                    continue;

                float val0 = heightMap[i, j];
                float val1 = heightMap[i + 1, j];
                float val2 = heightMap[i, j + 1];
                float val3 = heightMap[i + 1, j + 1];

                int type = 
                    (val0 < 0 ? 1 : 0) |
                    (val1 < 0 ? 1 : 0) << 1 |
                    (val2 < 0 ? 1 : 0) << 2 |
                    (val3 < 0 ? 1 : 0) << 3;
                
                Vector3 v0 = new Vector3(points[i, j].x, 0f, points[i, j].y);
                Vector3 v1 = new Vector3(points[i + 1, j].x, 0f, points[i + 1, j].y);
                Vector3 v2 = new Vector3(points[i, j + 1].x, 0f, points[i, j + 1].y);
                Vector3 v3 = new Vector3(points[i + 1, j + 1].x, 0f, points[i + 1, j + 1].y);

                Vector3 v01 = new Vector3(
                    points[i, j].x + square * Mathf.InverseLerp(val0, val1, 0), 0f, points[i, j].y );
                Vector3 v02 = new Vector3(
                    points[i, j].x, 0f, points[i, j].y + square * Mathf.InverseLerp(val0, val2, 0));
                Vector3 v13 = new Vector3(
                    points[i + 1, j + 1].x, 0f,
                    points[i + 1, j + 1].y - square * Mathf.InverseLerp(val3, val1, 0));
                Vector3 v23 = new Vector3(
                    points[i + 1, j + 1].x - square * Mathf.InverseLerp(val3, val2, 0),
                    0f, points[i + 1, j + 1].y );
                
                switch (type)
                {
                    case 1:
                        AddTriangle(v0, v02, v01);
                        break;

                    case 2:
                        AddTriangle(v01, v13, v1);
                        break;

                    case 4:
                        AddTriangle(v02, v2, v23);
                        break;

                    case 8:
                        AddTriangle(v23, v3, v13);
                        break;

                    case 3:
                        AddQuad(v0, v02, v13, v1);
                        break;

                    case 5:
                        AddQuad(v0, v2, v23, v01);
                        break;

                    case 10:
                        AddQuad(v01, v23, v3, v1);
                        break;

                    case 12:
                        AddQuad(v02, v2, v3, v13);
                        break;

                    case 6:
                        AddTriangle(v01, v13, v1);
                        AddTriangle(v02, v2, v23);
                        break;
                    
                    case 9:
                        AddTriangle(v0, v01, v02);
                        AddTriangle(v23, v3, v13);
                        break;

                    case 7:
                        AddPentagon(v0, v2, v23, v13, v1);
                        break;

                    case 11:
                        AddPentagon(v0, v02, v23, v3, v1);
                        break;

                    case 13:
                        AddPentagon(v0, v2, v3, v13, v01);
                        break;

                    case 14:
                        AddPentagon(v02, v2, v3, v1, v01);
                        break;

                    case 15:
                        AddQuad(v0, v2, v3, v1);
                        break;

                    default:
                        break;
                }
            }

        Vector3[] vertices = vertexList.ToArray();
        int[] triangles = triangleList.ToArray();

        seaMesh.Clear(); // VERY IMPORTANT IF CHANGING NUMBER OF VERTICES
        seaMesh.vertices = vertices;
        seaMesh.triangles = triangles;
        seaMesh.RecalculateNormals();
    }

    // void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.blue;
        
    //     for (int i = 0; i < seaMesh.vertices.Length; i++)
    //     {
    //         Gizmos.DrawSphere(seaMesh.vertices[i], .1f);
    //     }
    // }
}