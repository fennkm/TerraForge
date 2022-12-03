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
        public TerrainUIController terrainUIController;

        public float GetSize() { return size; }

        public virtual void SetSize(float val)
        {
            sizeVal = Mathf.Clamp(val, 0f, 1f);
            size = Mathf.Lerp(minSize, maxSize, sizeVal);
            terrainUIController.SetCursorSize(size);
        }

        public virtual void Resize(float factor)
        {
            SetSize(sizeVal + factor);
        }

        public abstract void Initialise();
        public virtual void Click() { Debug.Log("The tool function on " + transform.name + " has not been set!"); }
        public virtual void Hold() { Debug.Log("This tool function on " + transform.name + " has not been set!"); }
    }
}