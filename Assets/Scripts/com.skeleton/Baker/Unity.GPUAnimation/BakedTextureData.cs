using System;
using UnityEngine;

namespace Plugins.Unity.GPUAnimation
{
    public class BakedTextureData : ScriptableObject
    {
        public float length;
        public string assetName;
        public int subMeshCount;
        public ClipRawData[] clipDatas;
    }

    [Serializable]
    public struct ClipRawData 
    {
        public int pixelStart;
        public int pixelEnd;
        public float length;
        public string name;
        public float frameRate;
    }
}