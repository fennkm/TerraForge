using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using VectorSwizzling;

namespace TerrainMesh
{
    public class TerrainBuilder
    {
        private float density; // Squares per unit
        private float size;
        private int resolution; // Number of verts on each side

        private Vector2[,] basePoints;
        private Vector3[] groundVerts;
        private Vector2[] groundUVs;
        private int[] groundTriangles;

        private float[,] heightMap;

        private Mesh groundMesh;
        private Mesh seaMesh;

        public TerrainBuilder(float size, float density)
        {
            this.size = size;
            this.density = density;
            
            resolution = (int) (density * size) + 1;

            heightMap = new float[resolution, resolution];

            groundVerts = new Vector3[resolution * resolution];
            groundUVs = new Vector2[resolution * resolution];
            basePoints = new Vector2[resolution, resolution];

            float vertSeparation = 1f / density;
            Vector2 topRight = new Vector2(size / 2, size / 2);

            for (int i = 0; i < resolution; i++)
                for (int j = 0; j < resolution; j++)
                {
                    Vector3 coord = new Vector3(vertSeparation * i, -1f, vertSeparation * j);
                    basePoints[i, j] = coord.xz() - topRight;
                    groundVerts[i *  resolution + j] = coord - topRight.x0y();
                    groundUVs[i *  resolution + j] = coord.xz() / size;
                }

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

            groundMesh = new Mesh();

            groundMesh.SetVertices(groundVerts);
            groundMesh.SetTriangles(groundTriangles, 0);
            groundMesh.SetUVs(0, groundUVs);

            seaMesh = new Mesh();
            UpdateSeaMesh();
        }

        public void UpdateHeightMap(float[,] values, out Mesh groundMesh, out Mesh seaMesh) 
        {
            if (values.GetLength(0) != resolution || values.GetLength(1) != resolution)
                throw new System.IndexOutOfRangeException(
                    "Height map provided (size: " + values.GetLength(0) + "*" + values.GetLength(1) + 
                    ") does not match mesh resolution (size: " + resolution + "*" + resolution + ")!");

            heightMap = values;

            UpdateMeshes();

            groundMesh = this.groundMesh;
            seaMesh = this.seaMesh;
        }

        public float[,] getHeightMap() { return heightMap; }

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

            groundMesh.SetVertices(groundVerts);
            groundMesh.RecalculateNormals();
        }

        private void UpdateSeaMesh()
        {
            Vector3[] vertices;
            int[] triangles;

            MarchingSquares.BuildMesh(basePoints, heightMap, out vertices, out triangles);

            seaMesh.Clear(); // VERY IMPORTANT IF CHANGING NUMBER OF VERTICES
            seaMesh.SetVertices(vertices);
            seaMesh.SetUVs(0, vertices.Select(e => new Vector2(e.x, e.y)).ToArray());
            seaMesh.SetTriangles(triangles, 0);
            seaMesh.RecalculateNormals();
        }
    }
}