// CustomResolution.cs
using UnityEngine;

namespace Doody.Settings
{
    [System.Serializable]
    public struct CustomResolution
    {
        public int width;
        public int height;
        public int refreshRate;

        public CustomResolution(int width, int height, int refreshRate)
        {
            this.width = width;
            this.height = height;
            this.refreshRate = refreshRate;
        }

        public override string ToString()
        {
            return $"{width} x {height} @ {refreshRate}Hz";
        }
    }
}