using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VectorSwizzling;

public class TerrainController : MonoBehaviour
{
    public float height1;
    public float radius1;
    public Vector2 offset1;

    public float height2;
    public float radius2;
    public Vector2 offset2;

    private float prevHeight1;
    private float prevRadius1;
    private Vector2 prevOffset1;
    private float prevHeight2;
    private float prevRadius2;
    private Vector2 prevOffset2;

    private float density = 2f; // Squares per unit
    private float size = 128f;
    private int resolution; // Number of verts on each side

    private Vector3[] groundVerts;
    private int[] groundTriangles;

    private float[,] heightMap;

    public MeshFilter groundMeshFilter;
    public MeshFilter seaMeshFilter;
    private Mesh groundMesh;
    private Mesh seaMesh;
    private MeshCollider groundMeshColl;
    private MeshCollider seaMeshColl;

    // Start is called before the first frame update
    void Awake()
    {
        groundMesh = groundMeshFilter.mesh;
        seaMesh = seaMeshFilter.mesh;

        groundMeshColl = groundMeshFilter.GetComponent<MeshCollider>();
        seaMeshColl = seaMeshFilter.GetComponent<MeshCollider>();

        resolution = (int) (density * size) + 1;

        groundVerts = new Vector3[resolution * resolution];

        float vertSeparation = 1f / density;
        Vector3 topRight = new Vector3(size / 2, 0f, size / 2);

        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
                groundVerts[i *  resolution + j] = 
                    new Vector3(vertSeparation * i, -1f, vertSeparation * j) - topRight;

        groundTriangles = new int[(resolution - 1) * (resolution - 1) * 2 * 6];

        for (int i = 0; i < resolution - 1; i++)
            for (int j = 0; j < resolution - 1; j++)
            {
                int index = ((i * resolution) + j) * 6;

                groundTriangles[index] = i * resolution + j;
                groundTriangles[index + 1] = i * resolution + (j + 1);
                groundTriangles[index + 2] = (i + 1) * resolution + j;
                groundTriangles[index + 3] = (i + 1) * resolution + (j + 1);
                groundTriangles[index + 4] = (i + 1) * resolution + j;
                groundTriangles[index + 5] = i * resolution + (j + 1);
            }

        GenerateHeightMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (prevHeight1 != height1 || prevRadius1 != radius1 || prevOffset1 != offset1
        || prevHeight2 != height2 || prevRadius2 != radius2 || prevOffset2 != offset2)
            GenerateHeightMap();
    }

    private void GenerateHeightMap()
    {
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
                heightMap[i, j] = -1;

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
                    heightMap[i, j] = -1;
            }

        UpdateMeshes();
    }

    private Vector3 getGroundPoint(int x, int y) { return groundVerts[x * resolution + y]; }

    private void UpdateMeshes()
    {
        UpdateGroundMesh();
        UpdateSeaMesh();
    }

    private void UpdateGroundMesh()
    {
        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
                groundVerts[i * resolution + j].y = heightMap[i, j];

        groundMesh.vertices = groundVerts;
        groundMesh.triangles = groundTriangles;
        groundMesh.RecalculateNormals();

        groundMeshColl.sharedMesh = groundMesh;
    }

    private void UpdateSeaMesh()
    {
        List<Vector3> vertexList = new List<Vector3>();
        List<int> triangleList = new List<int>();

        int nextIndex = 0;

        int AddVert(Vector3 vert)
        {
            vertexList.Add(vert);
            return nextIndex++;
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
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e)
        {
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
            AddTriangle(a, d, e);
        }

        bool[,] filled = new bool[resolution - 1, resolution - 1];

        int s = 2;
        while (s <= resolution >> 2) s <<= 1;

        for (; s > 1; s >>= 1)
            for (int i = 0; i <= resolution - 1  - s ; i += s)
                for (int j = 0; j <= resolution - 1 - s; j += s)
                {
                    if (filled[i, j])
                        continue;

                    bool empty = true;
                    for (int u = 0; u <= s; u++)
                        for (int v = 0; v <= s; v++)
                            empty &= heightMap[i + u, j + v] < 0;
                    
                    if (empty)
                    {
                        Vector3 v0 = getGroundPoint(i, j).x0z();
                        Vector3 v1 = getGroundPoint(i + s, j).x0z();
                        Vector3 v2 = getGroundPoint(i, j + s).x0z();
                        Vector3 v3 = getGroundPoint(i + s, j + s).x0z();

                        AddQuad(v0, v2, v3, v1);

                        for (int u = 0; u < s; u++)
                            for (int v = 0; v < s; v++)
                                filled[i + u, j + v] = true;
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

                Vector3 v0 = getGroundPoint(i, j).x0z();
                Vector3 v1 = getGroundPoint(i + 1, j).x0z();
                Vector3 v2 = getGroundPoint(i, j + 1).x0z();
                Vector3 v3 = getGroundPoint(i + 1, j + 1).x0z();

                Vector3 v01 = new Vector3(v0.x + square * Mathf.InverseLerp(val0, val1, 0), 0f, v0.z);
                Vector3 v02 = new Vector3(v0.x, 0f, v0.z + square * Mathf.InverseLerp(val0, val2, 0));
                Vector3 v13 = new Vector3(v3.x, 0f, v3.z - square * Mathf.InverseLerp(val3, val1, 0));
                Vector3 v23 = new Vector3(v3.x - square * Mathf.InverseLerp(val3, val2, 0), 0f, v3.z);
                
                switch (type)
                {
                    case 1:
                        AddTriangle(v0, v02, v01);
                        break;

                    case 2:
                        AddTriangle(v1, v01, v13);
                        break;

                    case 4:
                        AddTriangle(v2, v23, v02);
                        break;

                    case 8:
                        AddTriangle(v3, v13, v23);
                        break;

                    case 3:
                        AddQuad(v1, v0, v02, v13);
                        break;

                    case 5:
                        AddQuad(v0, v2, v23, v01);
                        break;

                    case 10:
                        AddQuad(v3, v1, v01, v23);
                        break;

                    case 12:
                        AddQuad(v2, v3, v13, v02);
                        break;

                    case 6:
                        AddTriangle(v1, v01, v13);
                        AddTriangle(v2, v23, v02);
                        break;
                    
                    case 9:
                        AddTriangle(v0, v01, v02);
                        AddTriangle(v3, v13, v23);
                        break;

                    case 7:
                        AddPentagon(v1, v0, v2, v23, v13);
                        break;

                    case 11:
                        AddPentagon(v3, v1, v0, v02, v23);
                        break;

                    case 13:
                        AddPentagon(v0, v2, v3, v13, v01);
                        break;

                    case 14:
                        AddPentagon(v2, v3, v1, v01, v02);
                        break;

                    case 15:
                        AddQuad(v0, v2, v3, v1);
                        break;

                    default:
                        break;
                }
            }

        seaMesh.Clear(); // VERY IMPORTANT IF CHANGING NUMBER OF VERTICES
        seaMesh.vertices = vertexList.ToArray();
        seaMesh.triangles = triangleList.ToArray();
        seaMesh.RecalculateNormals();

        Debug.Log(seaMesh.vertices.Length);
        Debug.Log(seaMesh.triangles.Length);

        seaMeshColl.sharedMesh = seaMesh;
    }

    // void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.red;
        
    //     for (int i = 0; i < seaMesh.vertices.Length; i++)
    //     {
    //         Gizmos.DrawSphere(groundMesh.vertices[i], .1f);
    //     }
    // }
}