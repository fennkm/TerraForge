using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using VectorSwizzling;

namespace Tools
{
    public class LowerTerrainTool : Tool
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
            terrainController = terrainGraphicsController.terrainController;
        }

        public override void Initialise()
        {
            base.Initialise();
            sizeSlider.value = sizeVal;
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
            float radius = GetSize() / 2f;
            Vector3 coords = terrainGraphicsController.GetCursorPos();

            terrainController.RaiseArea(coords, radius, -intensity);
        }
    }
}