using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tools
{
    public abstract class Tool : MonoBehaviour
    {
        public float minSize;
        public float maxSize;
        protected float sizeVal;
        private float size;
        public TerrainGraphicsController terrainGraphicsController;

        public float GetSize() { return size; }

        public virtual void Initialise()
        {
            size = Mathf.Lerp(minSize, maxSize, sizeVal);
            terrainGraphicsController.InitialiseCursor(size, minSize);
        }

        public virtual void SetSize(float val)
        {
            sizeVal = Mathf.Clamp(val, 0f, 1f);
            size = Mathf.Lerp(minSize, maxSize, sizeVal);
            terrainGraphicsController.SetCursorSize(size);
        }

        public virtual void Resize(float factor)
        {
            SetSize(sizeVal + factor);
        }
        public virtual void Click() { }
        public virtual void Hold() { }
        public virtual void Release() { }
    }
}