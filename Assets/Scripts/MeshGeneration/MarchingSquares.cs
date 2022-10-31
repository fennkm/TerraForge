using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VectorSwizzling;

namespace TerrainMesh
{
    public static class MarchingSquares
    {
        public static void BuildMesh(Vector2[,] basePoints, float[,] heightMap, out Vector3[] vertices, out int[] triangles)
        {
            if (basePoints.GetLength(0) != heightMap.GetLength(0) ||
                basePoints.GetLength(1) != heightMap.GetLength(1))
                throw new System.IndexOutOfRangeException(
                    "Base point array (size:" + basePoints.GetLength(0) + "*" + basePoints.GetLength(1) + 
                    ") and height map (size:" + heightMap.GetLength(0) + "*" + heightMap.GetLength(1) + 
                    ") are not the same size!");

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

            int resolution = basePoints.GetLength(0);

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
                            Vector3 v0 = basePoints[i, j].x0y();
                            Vector3 v1 = basePoints[i + s, j].x0y();
                            Vector3 v2 = basePoints[i, j + s].x0y();
                            Vector3 v3 = basePoints[i + s, j + s].x0y();

                            AddQuad(v0, v2, v3, v1);

                            for (int u = 0; u < s; u++)
                                for (int v = 0; v < s; v++)
                                    filled[i + u, j + v] = true;
                        }
                    }

            float square = basePoints[1, 1].x - basePoints[0, 0].x;

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

                    Vector3 v0 = basePoints[i, j].x0y();
                    Vector3 v1 = basePoints[i + 1, j].x0y();
                    Vector3 v2 = basePoints[i, j + 1].x0y();
                    Vector3 v3 = basePoints[i + 1, j + 1].x0y();

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

            vertices = vertexList.ToArray();
            triangles = triangleList.ToArray();
        }
    }
}