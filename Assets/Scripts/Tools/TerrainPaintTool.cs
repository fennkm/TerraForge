using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools
{
    public class TerrainPaintTool : Tool
    {
        public float minIntensity;
        public float maxIntensity;
        private float intensity;
        private int terrain;
        public Slider sizeSlider;

        void Start()
        {
            SetSize(.1f);
            SetIntensity(.15f);
        }

        public override void Initialise()
        {
            SetSize(sizeVal);
        }

        public void SetTerrain(int index)
        {
            terrain = index;
        }

        public void SetIntensity(float val)
        {
            intensity = minIntensity * Mathf.Pow(maxIntensity / minIntensity, val);
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
            float radius = GetSize() / 2 * terrainUIController.UIToWorld();
            Vector2 coords = terrainUIController.GetMouseGridPos();

            terrainUIController.PaintTerrain(coords, radius, intensity, terrain);
        }
    }
}