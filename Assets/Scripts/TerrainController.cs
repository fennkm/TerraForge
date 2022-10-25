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

    private Vector2[,] points;

    private float[,] heightMap;

    public MeshFilter groundMeshFilter;
    public MeshFilter seaMeshFilter;
    private Mesh groundMesh;
    private Mesh seaMesh;
    private MeshCollider groundMeshColl;
    private MeshCollider seaMeshColl;

    // Start is called before the first frame update
    void Start()
    {
        groundMesh = groundMeshFilter.mesh;
        seaMesh = seaMeshFilter.mesh;

        groundMeshColl = groundMeshFilter.GetComponent<MeshCollider>();
        seaMeshColl = seaMeshFilter.GetComponent<MeshCollider>();

        resolution = (int) (density * size) + 1;

        points = new Vector2[resolution, resolution];

        float vertSeparation = 1f / density;
        Vector2 topRight = new Vector2(size / 2, size / 2);

        for (int i = 0; i < resolution; i++)
            for (int j = 0; j < resolution; j++)
                points[i,j] = new Vector2(vertSeparation * i, vertSeparation * j) - topRight;

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

    private void UpdateMeshes()
    {
        UpdateGroundMesh();
        UpdateSeaMesh();
    }

    private void UpdateGroundMesh()
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

        bool[,] filled = new bool[resolution - 1, resolution - 1];

        int s = 2;
        while (s <= resolution >> 2) s <<= 1;

        for (; s >= 1; s >>= 1)
        {
            for (int i = 0; i <= (resolution - 1) / 2 - s ; i += s)
                for (int j = 0; j <= (resolution - 1) / 2 - s; j += s)
                    for (int iCase = 0; iCase < 2; iCase++)
                        for (int jCase = 0; jCase < 2; jCase++)
                        {
                            int x = (iCase == 0 ? i : (resolution - 1) - s - i);
                            int y = (jCase == 0 ? j : (resolution - 1) - s - j);

                            if (filled[x, y])
                                continue;

                            bool flat = true;
                            if (s > 1)
                            {
                                float reference = heightMap[x, y];
                                for (int u = 0; u <= s; u++)
                                    for (int v = 0; v <= s; v++)
                                        flat &= heightMap[x + u, y + v] == reference;
                            }
                            
                            if (flat)
                            {
                                Vector3 v0 = points[x, y].x0y() + heightMap[x, y]._0x0();
                                Vector3 v1 = points[x + s, y].x0y() + heightMap[x + s, y]._0x0();
                                Vector3 v2 = points[x, y + s].x0y() + heightMap[x, y + s]._0x0();
                                Vector3 v3 = points[x + s, y + s].x0y() + heightMap[x + s, y + s]._0x0();

                                AddQuad(v0, v2, v3, v1);

                                for (int u = 0; u < s; u++)
                                    for (int v = 0; v < s; v++)
                                        filled[x + u, y + v] = true;
                            }
                        }
        }
        
        groundMesh.Clear();
        groundMesh.vertices = vertexList.ToArray();
        groundMesh.triangles = triangleList.ToArray();
        groundMesh.RecalculateNormals();

        groundMeshColl.sharedMesh = groundMesh;
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
        while (s <= resolution >> 2) s <<= 1;

        for (; s > 1; s >>= 1)
        {
            for (int i = 0; i <= (resolution - 1) / 2 - s ; i += s)
                for (int j = 0; j <= (resolution - 1) / 2 - s; j += s)
                    for (int iCase = 0; iCase < 2; iCase++)
                        for (int jCase = 0; jCase < 2; jCase++)
                        {
                            int x = (iCase == 0 ? i : (resolution - 1) - s - i);
                            int y = (jCase == 0 ? j : (resolution - 1) - s - j);

                            if (filled[x, y])
                                continue;

                            bool empty = true;
                            for (int u = 0; u <= s; u++)
                                for (int v = 0; v <= s; v++)
                                    empty &= heightMap[x + u, y + v] < 0;
                            
                            if (empty)
                            {
                                Vector3 v0 = points[x, y].x0y();
                                Vector3 v1 = points[x + s, y].x0y();
                                Vector3 v2 = points[x, y + s].x0y();
                                Vector3 v3 = points[x + s, y + s].x0y();

                                AddQuad(v0, v2, v3, v1);

                                for (int u = 0; u < s; u++)
                                    for (int v = 0; v < s; v++)
                                        filled[x + u, y + v] = true;
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

        seaMesh.Clear(); // VERY IMPORTANT IF CHANGING NUMBER OF VERTICES
        seaMesh.vertices = vertexList.ToArray();
        seaMesh.triangles = triangleList.ToArray();
        seaMesh.RecalculateNormals();

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