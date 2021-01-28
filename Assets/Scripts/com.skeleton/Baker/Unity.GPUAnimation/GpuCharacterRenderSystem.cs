using GPUAnimate;
using Plugins.Unity.GPUAnimation;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.GPUAnimation
{
    public struct BakedAnimationClip
    {
        internal float TextureOffset;
        internal float TextureRange;
        internal float OnePixelOffset;
        internal float TextureWidth;
        internal float OneOverTextureWidth;
        internal float OneOverPixelOffset;

        public float AnimationLength;

        public BakedAnimationClip(AnimationTextures animTextures, KeyframeTextureBaker.AnimationClipData clipData)
        {
            float onePixel = 1f / animTextures.Animation0.width;
            float start = (float) clipData.PixelStart / animTextures.Animation0.width;
            float end = (float) clipData.PixelEnd / animTextures.Animation0.width;

            TextureOffset = start;
            TextureRange = end - start;
            OnePixelOffset = onePixel;
            TextureWidth = animTextures.Animation0.width;
            OneOverTextureWidth = 1.0F / TextureWidth;
            OneOverPixelOffset = 1.0F / OnePixelOffset;

            AnimationLength = clipData.Length;
            //Looping = clipData.Clip.wrapMode == WrapMode.Loop;
        }

        public BakedAnimationClip(Texture matrix0, ClipRawData clipData)
        {
            float onePixel = 1f / matrix0.width;
            float start = (float) clipData.pixelStart / matrix0.width;
            float end = (float) clipData.pixelEnd / matrix0.width;

            TextureOffset = start;
            TextureRange = end - start;
            OnePixelOffset = onePixel;
            TextureWidth = matrix0.width;
            OneOverTextureWidth = 1.0F / TextureWidth;
            OneOverPixelOffset = 1.0F / OnePixelOffset;

            AnimationLength = clipData.length;
            //Looping = clipData.Clip.wrapMode == WrapMode.Loop;
        }

        public BakedAnimationClip(AnimationTextures animTextures, ClipData clipData)
        {
            float onePixel = 1f / animTextures.Animation0.width;
            float start = (float) clipData.pixelStart / animTextures.Animation0.width;
            float end = (float) clipData.pixelEnd / animTextures.Animation0.width;

            TextureOffset = start;
            TextureRange = end - start;
            OnePixelOffset = onePixel;
            TextureWidth = animTextures.Animation0.width;
            OneOverTextureWidth = 1.0F / TextureWidth;
            OneOverPixelOffset = 1.0F / OnePixelOffset;

            AnimationLength = clipData.length;
        }

        public float3 ComputeCoordinate(float normalizedTime)
        {
            float texturePosition = normalizedTime * TextureRange + TextureOffset;
            float lowerPixelFloor = math.floor(texturePosition * TextureWidth);

            float lowerPixelCenter = lowerPixelFloor * OneOverTextureWidth;
            float upperPixelCenter = lowerPixelCenter + OnePixelOffset;
            float lerpFactor = (texturePosition - lowerPixelCenter) * OneOverPixelOffset;

            return new float3(lowerPixelCenter, upperPixelCenter, lerpFactor);
        }

        public float ComputeNormalizedTime(float time)
        {
            return Mathf.Repeat(time, AnimationLength) / AnimationLength;
            // if (Looping)
            // 	return Mathf.Repeat(time, AnimationLength) / AnimationLength;
            // else
            // 	return math.saturate(time / AnimationLength);
        }
    }
}