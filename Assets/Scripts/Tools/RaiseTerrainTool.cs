using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools
{
    public class RaiseTerrainTool : Tool
    {
        public float minIntensity;
        public float maxIntensity;
        private float intensity;
        private TerrainController terrainController;
        public Slider sizeSlider;

        void Start()
        {
            SetSize(.1f);
            SetIntensity(.1f);
            terrainController = terrainUIController.terrainController;
        }

        public override void Initialise()
        {
            SetSize(sizeVal);
        }

        public void SetIntensity(float val)
        {
            intensity = minIntensity * Mathf.Pow(maxIntensity / minIntensity, val);
            Debug.Log(intensity);
        }

        public override void SetSize(float val)
        {
            base.SetSize(val);
            sizeSlider.value = val;
        }

        public override void Click()
        {
        }

        public override void Hold()
        {
            float density = terrainController.GetDensity();
            int resolution = terrainController.GetResolution();

            float diameter = GetSize() * terrainUIController.UIToWorld();
            Vector2 coords = terrainUIController.GetMouseGridPos();

            for (int i = 0; i < resolution; i++)
                for (int j = 0; j < resolution; j++)
                {
                    float dist = (new Vector2(i, j) - coords).magnitude;
                    float val = (dist / (diameter * density)) * Mathf.PI;

                    if (val >= -Mathf.PI / 2 && val <= Mathf.PI / 2)
                        terrainController.AddHeight(i, j, Mathf.Cos(val) * intensity * Time.deltaTime);
                }

            terrainController.ApplyHeightMap();
        }
    }
}