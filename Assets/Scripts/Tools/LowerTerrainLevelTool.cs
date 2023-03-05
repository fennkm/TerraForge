using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using VectorSwizzling;

namespace Tools
{
    public class LowerTerrainLevelTool : Tool
    {
        public float minIntensity;
        public float maxIntensity;
        private float intensity;
        private TerrainController terrainController;
        public Slider sizeSlider;

        private float heightVal;

        void Start()
        {
            SetSize(.5f);
            SetIntensity(.6f);
            terrainController = terrainGraphicsController.terrainController;
        }

        public override void Initialise()
        {
            base.Initialise();
            sizeSlider.value = sizeVal;
        }

        public void SetIntensity(float val)
        {
            intensity = minIntensity + (maxIntensity - minIntensity) * val;
        }

        public override void SetSize(float val)
        {
            base.SetSize(val);
            sizeSlider.value = val;
        }

        public override void Click()
        {
            Vector3 cursorPos = terrainGraphicsController.GetCursorPos();
            heightVal = cursorPos.y;
            terrainGraphicsController.SetStickyCursor(cursorPos);
        }

        public override void Hold()
        {
            float radius = GetSize() / 2f;
            Vector3 coords = terrainGraphicsController.GetCursorPos();

            terrainController.SetAreaToHeight(coords, radius, intensity, heightVal, true, false);
            terrainGraphicsController.AutoPaintTerrain(coords, radius);
        }

        public override void Release()
        {
            terrainGraphicsController.ClearStickyCursor();
        }
    }
}