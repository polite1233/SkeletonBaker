using System;
using Unity.GPUAnimation;
using UnityEngine;

namespace GPUAnimate
{
    public class MeshClipData
    {
        public Texture2D texture0;
        public Texture2D texture1;
        public Texture2D texture2;
        public MeshClipTexMeta meta;
        public Mesh mesh;

        public MeshClipData(Texture2D texture0, Texture2D texture1, Texture2D texture2, MeshClipTexMeta meta, Mesh mesh)
        {
            this.texture0 = texture0;
            this.texture1 = texture1;
            this.texture2 = texture2;
            this.meta = meta;
            this.mesh = mesh;
        }

        public MeshClipData(KeyframeTextureBaker.BakedData bakedData)
        {
            meta = GetMeshClipTexMeta(bakedData);
            texture0 = bakedData.AnimationTextures.Animation0;
            texture1 = bakedData.AnimationTextures.Animation1;
            texture2 = bakedData.AnimationTextures.Animation2;
        }

        private static MeshClipTexMeta GetMeshClipTexMeta(KeyframeTextureBaker.BakedData bakedData)
        {
            MeshClipTexMeta meta = new MeshClipTexMeta
            {
                clipDatas = new ClipData[bakedData.Animations.Count]
            };
            for (int i = 0; i < bakedData.Animations.Count; i++)
            {
                var bakedDataAnimation = bakedData.Animations[i];
                meta.clipDatas[i] = new ClipData
                {
                    frameRate = bakedDataAnimation.FrameRate,
                    pixelStart = bakedDataAnimation.PixelStart,
                    pixelEnd = bakedDataAnimation.PixelEnd,
                    length = bakedDataAnimation.Length,
                    name = bakedDataAnimation.Name
                };
            }

            meta.width = bakedData.AnimationTextures.Animation0.width;
            meta.height = bakedData.AnimationTextures.Animation0.height;
            return meta;
        }
    }

    [Serializable]
    public class MeshClipTexMeta
    {
        public int width;
        public int height;
        public ClipData[] clipDatas;
    }

    public class ClipData
    {
        public float length;
        public float frameRate;
        public int pixelStart;
        public int pixelEnd;
        public string name;
    }
}