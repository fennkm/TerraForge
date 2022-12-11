using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using VectorSwizzling;

namespace TerrainMesh
{
    public class TerrainBuilder
    {
        public GameObject parent;
        private float density; // Squares per unit
        private float size;
        private int resolution; // Number of verts on each side
        private int chunkRes;
        private int chunkVertNum;
        private float chunkSize;
        private float vertSeparation;

        private Vector3[,][,] groundPoints;
        private Vector2[,] chunkPositions;
        private Vector3[,][] groundChunkVerts;
        private Vector2[,][] groundChunkUVs;
        private int[,][] groundChunkTriangles;

        private float[,] heightMap;

        private Mesh[,] groundChunkMeshes;
        private Mesh[,] seaChunkMeshes;
        private MeshCollider[,] groundChunkMeshColls;

        private ComputeShader seaMeshGenerator;

        public TerrainBuilder(float size, float density, MeshFilter[,] groundChunkMeshFilters, MeshFilter[,] seaChunkMeshFilters, float seaDepth, ComputeShader seaMeshGenerator)
        {
            this.size = size;
            this.density = density;
            this.seaMeshGenerator = seaMeshGenerator;
            
            chunkRes = groundChunkMeshFilters.GetLength(0);

            groundChunkMeshes = new Mesh[chunkRes, chunkRes];
            groundChunkMeshColls = new MeshCollider[chunkRes, chunkRes];

            seaChunkMeshes = new Mesh[chunkRes, chunkRes];

            for (int i = 0; i < chunkRes; i++)
                for (int j = 0; j < chunkRes; j++)
                {
                    groundChunkMeshes[i, j] = groundChunkMeshFilters[i, j].mesh;
                    groundChunkMeshColls[i, j] = groundChunkMeshFilters[i, j].GetComponent<MeshCollider>();

                    seaChunkMeshes[i, j] = seaChunkMeshFilters[i, j].mesh;
                }
            
            resolution = (int) (density * size) + 1;

            resolution += chunkRes - (resolution - 1) % chunkRes - 1;

            heightMap = new float[resolution, resolution];

            chunkVertNum = (resolution - 1) / chunkRes + 1;
            chunkSize = size / chunkRes;

            vertSeparation = chunkSize / (chunkVertNum - 1);

            chunkPositions = new Vector2[chunkRes, chunkRes];

            groundChunkVerts = new Vector3[chunkRes, chunkRes][];
            groundChunkUVs = new Vector2[chunkRes, chunkRes][];
            groundPoints = new Vector3[resolution, resolution][,];

            for (int n = 0; n < chunkRes; n++)
                for (int m = 0; m < chunkRes; m++)
                {
                    groundChunkVerts[n, m] = new Vector3[chunkVertNum * chunkVertNum];
                    groundChunkUVs[n, m] = new Vector2[chunkVertNum * chunkVertNum];
                    groundPoints[n, m] = new Vector3[chunkVertNum, chunkVertNum];

                    for (int i = 0; i < chunkVertNum; i++)
                        for (int j = 0; j < chunkVertNum; j++)
                        {
                            groundPoints[n, m][i, j] = new Vector3(vertSeparation * i, 0f, vertSeparation * j) - chunkSize.x0x() / 2f;

                            Vector3 coord = groundPoints[n, m][i, j].x0z() - seaDepth._0x0();
                            groundChunkVerts[n, m][i * chunkVertNum + j] = coord;

                            chunkPositions[n, m] = new Vector2(n - (chunkRes - 1) / 2f, m - (chunkRes - 1) / 2f) * chunkSize;
                            groundChunkUVs[n, m][i * chunkVertNum + j] = (coord.xz() + chunkPositions[n, m]) / size + .5f.xx();
                        }
                }
            
            groundChunkTriangles = new int[chunkRes, chunkRes][];

            for (int n = 0; n < chunkRes; n++)
                for (int m = 0; m < chunkRes; m++)
                {
                    groundChunkTriangles[n, m] = new int[(chunkVertNum - 1) * (chunkVertNum - 1) * 6];

                    for (int i = 0; i < chunkVertNum - 1; i++)
                        for (int j = 0; j < chunkVertNum - 1; j++)
                        {
                            int index = (i * (chunkVertNum - 1) + j) * 6;

                            groundChunkTriangles[n, m][index] = i * chunkVertNum + j;
                            groundChunkTriangles[n, m][index + 1] = i * chunkVertNum + (j + 1);
                            groundChunkTriangles[n, m][index + 2] = (i + 1) * chunkVertNum + j;
                            groundChunkTriangles[n, m][index + 3] = (i + 1) * chunkVertNum + (j + 1);
                            groundChunkTriangles[n, m][index + 4] = (i + 1) * chunkVertNum + j;
                            groundChunkTriangles[n, m][index + 5] = i * chunkVertNum + (j + 1);
                        }
                }

            for (int i = 0; i < chunkRes; i++)
                for (int j = 0; j < chunkRes; j++)
                    {
                        groundChunkMeshes[i, j].SetVertices(groundChunkVerts[i, j]);
                        groundChunkMeshes[i, j].SetTriangles(groundChunkTriangles[i, j], 0);
                        groundChunkMeshes[i, j].SetUVs(0, groundChunkUVs[i, j]);
                        groundChunkMeshes[i, j].RecalculateNormals();

                        groundChunkMeshColls[i, j].sharedMesh = groundChunkMeshes[i, j];
                    }
        }

        public int GetMeshResolution() { return resolution; }

        public void UpdateHeightMap(float[,] values, int xFrom, int xTo, int yFrom, int yTo) 
        {
            if (values.GetLength(0) != resolution || values.GetLength(1) != resolution)
                throw new System.IndexOutOfRangeException(
                    "Height map provided (size: " + values.GetLength(0) + "*" + values.GetLength(1) + 
                    ") does not match mesh resolution (size: " + resolution + "*" + resolution + ")!");

            heightMap = values;

            UpdateMeshes(xFrom, xTo, yFrom, yTo);
        }

        public float[,] getHeightMap() { return heightMap; }

        private void UpdateMeshes(int xFrom, int xTo, int yFrom, int yTo)
        {
            int xChunkFrom = xFrom / chunkVertNum;
            int xChunkTo = xTo / chunkVertNum;
            int yChunkFrom = yFrom / chunkVertNum;
            int yChunkTo = yTo / chunkVertNum;

            float a = Time.realtimeSinceStartup;
            UpdateGroundMesh(xChunkFrom, xChunkTo, yChunkFrom, yChunkTo);
            float b = Time.realtimeSinceStartup;
            UpdateSeaMesh(xChunkFrom, xChunkTo, yChunkFrom, yChunkTo);
            float c = Time.realtimeSinceStartup;

            Debug.Log("Ground mesh generation total: " + (b - a));
            Debug.Log("Sea mesh generation total: " + (c - b));
        }

        private void UpdateGroundMesh(int xChunkFrom, int xChunkTo, int yChunkFrom, int yChunkTo)
        {
            float aTime = 0f;
            float bTime = 0f;

            for (int n = xChunkFrom; n <= xChunkTo; n++)
                for (int m = yChunkFrom; m <= yChunkTo; m++)
                {
                    float a = Time.realtimeSinceStartup;
                    for (int i = 0; i < chunkVertNum; i++)
                        for (int j = 0; j < chunkVertNum; j++)
                            groundChunkVerts[n, m][i * chunkVertNum + j].y = 
                                heightMap[n * (chunkVertNum - 1) + i, m * (chunkVertNum - 1) + j];
                    float b = Time.realtimeSinceStartup;

                    groundChunkMeshes[n, m].SetVertices(groundChunkVerts[n, m]);
                    groundChunkMeshes[n, m].RecalculateNormals();

                    groundChunkMeshColls[n, m].sharedMesh = groundChunkMeshes[n, m];
                    float c = Time.realtimeSinceStartup;

                    aTime += b - a;
                    bTime += c - b;
                }

            Debug.Log("Updating vertex heights: " + aTime);
            Debug.Log("Updating ground meshes: " + bTime);
        }

        private void UpdateSeaMesh(int xChunkFrom, int xChunkTo, int yChunkFrom, int yChunkTo)
        {
            float aTime = 0f;
            float bTime = 0f;

            for (int n = xChunkFrom; n <= xChunkTo; n++)
                for (int m = yChunkFrom; m <= yChunkTo; m++)
                {
                    Vector3[] verts = new Vector3[(chunkVertNum - 1) * (chunkVertNum - 1) * 6];
                    int[] tris = new int[(chunkVertNum - 1) * (chunkVertNum - 1) * 9];
                    
                    float a = Time.realtimeSinceStartup;
                    for (int i = 0; i < chunkVertNum; i++)
                        for (int j = 0; j < chunkVertNum; j++)
                            groundPoints[n, m][i, j].y = heightMap[n * (chunkVertNum - 1) + i, m * (chunkVertNum - 1) + j];
                            
                    GetSeaMeshData(n, m, ref verts, ref tris);

                    List<Vector3> vertexList = new List<Vector3>();
                    List<int> triangleList = new List<int>();

                    for (int i = 0; i < chunkVertNum - 1; i++)
                        for (int j = 0; j < chunkVertNum - 1; j++)
                        {
                            int index = i * (chunkVertNum - 1) + j;
                            int firstVertIndex = vertexList.Count;

                            for (int k = 0; k < 9; k++)
                            {
                                if (tris[index * 9 + k] < 0)
                                    break;

                                triangleList.Add(firstVertIndex + tris[index * 9 + k]);
                            }

                            for (int k = 0; k < 6; k++)
                            {
                                if (verts[index * 6 + k].y < 0f)
                                    break;
                                
                                vertexList.Add(verts[index * 6 + k]);
                            }
                        }
                        
                    float b = Time.realtimeSinceStartup;

                    // This is the most time consuming bit!
                    seaChunkMeshes[n, m].Clear(); // VERY IMPORTANT IF CHANGING NUMBER OF VERTICES
                    seaChunkMeshes[n, m].SetVertices(vertexList);
                    seaChunkMeshes[n, m].SetUVs(0, vertexList.Select(e => (e.xz() + chunkPositions[n, m]) / size + .5f.xx()).ToArray());
                    seaChunkMeshes[n, m].SetTriangles(triangleList, 0);
                    
                    float c = Time.realtimeSinceStartup;

                    aTime += b - a;
                    bTime += c - b;
                }
            Debug.Log("Shader running time: " + aTime);
            Debug.Log("Updating sea meshes: " + bTime);
        }

        private void GetSeaMeshData(int chunkX, int chunkY, ref Vector3[] verts, ref int[] tris)
        {
            ComputeBuffer groundVerts = new ComputeBuffer(chunkVertNum * chunkVertNum, sizeof(float) * 3);
            ComputeBuffer seaVerts = new ComputeBuffer((chunkVertNum - 1) * (chunkVertNum - 1) * 6, sizeof(float) * 3);
            ComputeBuffer seaTris = new ComputeBuffer((chunkVertNum - 1) * (chunkVertNum - 1) * 9, sizeof(int));

            groundVerts.SetData(groundPoints[chunkX, chunkY]);

            seaMeshGenerator.SetBuffer(0, "groundVerts", groundVerts);
            seaMeshGenerator.SetBuffer(0, "seaVerts", seaVerts);
            seaMeshGenerator.SetBuffer(0, "seaTris", seaTris);
            seaMeshGenerator.SetInt("resolution", chunkVertNum);

            int threadGroups = Mathf.Max(1, Mathf.CeilToInt((chunkVertNum - 1) / 16f));

            seaMeshGenerator.Dispatch(0, threadGroups, threadGroups, 1);

            seaVerts.GetData(verts);
            seaTris.GetData(tris);

            groundVerts.Release();
            seaVerts.Release();
            seaTris.Release();

        }
    }
}