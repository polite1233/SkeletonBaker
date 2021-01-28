using System;
using System.Collections.Generic;
using GPUAnimate;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GPUAnimation
{
    public struct AnimationTextures : IEquatable<AnimationTextures>
    {
        public Texture2D Animation0;
        public Texture2D Animation1;
        public Texture2D Animation2;

        public Color[] texture0Color;
        public Color[] texture1Color;
        public Color[] texture2Color;

        public bool Equals(AnimationTextures other)
        {
            return Animation0 == other.Animation0 && Animation1 == other.Animation1 && Animation2 == other.Animation2;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (ReferenceEquals(Animation0, null) ? 0 : Animation0.GetHashCode());
                hashCode = (hashCode * 397) ^ (ReferenceEquals(Animation1, null) ? 0 : Animation1.GetHashCode());
                hashCode = (hashCode * 397) ^ (ReferenceEquals(Animation2, null) ? 0 : Animation2.GetHashCode());
                return hashCode;
            }
        }
    }

    public static class KeyframeTextureBaker
    {
        public class BakedData
        {
            public AnimationTextures AnimationTextures;
            public Mesh NewMesh;
            public float Framerate;
            public Material[] materials;
            public List<AnimationClipData> Animations = new List<AnimationClipData>();
            public BakedAnimationClip[] bakedAnimationClips;

            public static BakedData Create(MeshClipData metaData)
            {
                MeshClipTexMeta meshClipTexMeta = metaData.meta;
                BakedData bakedData = new BakedData();
                AnimationTextures animationTextures = new AnimationTextures
                {
                    Animation0 = metaData.texture0,
                    Animation1 = metaData.texture1,
                    Animation2 = metaData.texture2
                };
                bakedData.AnimationTextures = animationTextures;
                bakedData.bakedAnimationClips = new BakedAnimationClip[meshClipTexMeta.clipDatas.Length];
                for (int i = 0; i < meshClipTexMeta.clipDatas.Length; i++)
                {
                    AnimationClipData clipData = new AnimationClipData
                    {
                        PixelStart = meshClipTexMeta.clipDatas[i].pixelStart,
                        PixelEnd = meshClipTexMeta.clipDatas[i].pixelEnd,
                        Length = meshClipTexMeta.clipDatas[i].length,
                        Name = meshClipTexMeta.clipDatas[i].name,
                        FrameRate = meshClipTexMeta.clipDatas[i].frameRate
                    };
                    bakedData.Animations.Add(clipData);
                    bakedData.bakedAnimationClips[i] =
                        new BakedAnimationClip(animationTextures, meshClipTexMeta.clipDatas[i]);
                }

                bakedData.NewMesh = metaData.mesh;
                return bakedData;
            }

            public Dictionary<string, AnimationClipData> AnimationsDictionary =
                new Dictionary<string, AnimationClipData>();


            public void ApplyTextures()
            {
                AnimationTextures.Animation0.Apply(false, true);
                AnimationTextures.Animation1.Apply(false, true);
                AnimationTextures.Animation2.Apply(false, true);
            }
        }

        public class AnimationClipData
        {
            // public AnimationClip Clip;
            public int PixelStart;
            public int PixelEnd;
            public float Length;
            public string Name;
            public float FrameRate;
        }

        public static BakedData BakeClips01(GameObject fbx, AnimationClip[] animationClips, float framerate)
        {
            SkinnedMeshRenderer skinRenderer = fbx.GetComponentInChildren<SkinnedMeshRenderer>();
            Mesh mesh = Object.Instantiate(skinRenderer.sharedMesh);
            ProcessMesh(skinRenderer, mesh);

            BakedData bakedData = new BakedData
            {
                Framerate = framerate,
                NewMesh = mesh
            };


            var sampledBoneMatrices = new List<Matrix4x4[,]>();

            int numberOfKeyFrames = 0;

            for (int i = 0; i < animationClips.Length; i++)
            {
                var sampledMatrix =
                    SampleAnimationClip(fbx, animationClips[i], skinRenderer, animationClips[i].frameRate);
                sampledBoneMatrices.Add(sampledMatrix);

                numberOfKeyFrames += sampledMatrix.GetLength(0);
            }

            int numberOfBones = sampledBoneMatrices[0].GetLength(1);

            var tex0 = bakedData.AnimationTextures.Animation0 =
                new Texture2D(numberOfKeyFrames, numberOfBones, TextureFormat.RGBAFloat, false);
            tex0.wrapMode = TextureWrapMode.Clamp;
            tex0.filterMode = FilterMode.Point;
            tex0.anisoLevel = 0;

            var tex1 = bakedData.AnimationTextures.Animation1 =
                new Texture2D(numberOfKeyFrames, numberOfBones, TextureFormat.RGBAFloat, false);
            tex1.wrapMode = TextureWrapMode.Clamp;
            tex1.filterMode = FilterMode.Point;
            tex1.anisoLevel = 0;

            var tex2 = bakedData.AnimationTextures.Animation2 =
                new Texture2D(numberOfKeyFrames, numberOfBones, TextureFormat.RGBAFloat, false);
            tex2.wrapMode = TextureWrapMode.Clamp;
            tex2.filterMode = FilterMode.Point;
            tex2.anisoLevel = 0;

            Color[] texture0Color = new Color[tex0.width * tex0.height];
            Color[] texture1Color = new Color[tex0.width * tex0.height];
            Color[] texture2Color = new Color[tex0.width * tex0.height];

            int runningTotalNumberOfKeyframes = 0;
            for (int i = 0; i < sampledBoneMatrices.Count; i++)
            {
                for (int boneIndex = 0; boneIndex < sampledBoneMatrices[i].GetLength(1); boneIndex++)
                {
                    for (int keyframeIndex = 0; keyframeIndex < sampledBoneMatrices[i].GetLength(0); keyframeIndex++)
                    {
                        int index = Get1DCoord(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex, tex0.width);

                        texture0Color[index] = sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(0);
                        texture1Color[index] = sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(1);
                        texture2Color[index] = sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(2);
                    }
                }

                AnimationClipData clipData = new AnimationClipData
                {
                    // Clip = animationClips[i],
                    Length = animationClips[i].length,
                    FrameRate = animationClips[i].frameRate,
                    Name = animationClips[i].name,
                    PixelStart = runningTotalNumberOfKeyframes + 1,
                    PixelEnd = runningTotalNumberOfKeyframes + sampledBoneMatrices[i].GetLength(0) - 1
                };

                if (animationClips[i].wrapMode == WrapMode.Default) clipData.PixelEnd -= 1;

                bakedData.Animations.Add(clipData);

                runningTotalNumberOfKeyframes += sampledBoneMatrices[i].GetLength(0);
            }

            tex0.SetPixels(texture0Color);
            tex1.SetPixels(texture1Color);
            tex2.SetPixels(texture2Color);

            bakedData.AnimationTextures.texture0Color = texture0Color;
            bakedData.AnimationTextures.texture1Color = texture1Color;
            bakedData.AnimationTextures.texture2Color = texture2Color;

            runningTotalNumberOfKeyframes = 0;
            for (int i = 0; i < sampledBoneMatrices.Count; i++)
            {
                for (int boneIndex = 0; boneIndex < sampledBoneMatrices[i].GetLength(1); boneIndex++)
                {
                    for (int keyframeIndex = 0; keyframeIndex < sampledBoneMatrices[i].GetLength(0); keyframeIndex++)
                    {
                        //int d1_index = Get1DCoord(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex, bakedData.Texture0.width);

                        Color pixel0 = tex0.GetPixel(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex);
                        Color pixel1 = tex1.GetPixel(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex);
                        Color pixel2 = tex2.GetPixel(runningTotalNumberOfKeyframes + keyframeIndex, boneIndex);

                        if ((Vector4) pixel0 != sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(0))
                        {
                            Debug.LogError("Error at (" + (runningTotalNumberOfKeyframes + keyframeIndex) + ", " +
                                           boneIndex + ") expected " +
                                           Format(sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(0)) +
                                           " but got " + Format(pixel0));
                        }

                        if ((Vector4) pixel1 != sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(1))
                        {
                            Debug.LogError("Error at (" + (runningTotalNumberOfKeyframes + keyframeIndex) + ", " +
                                           boneIndex + ") expected " +
                                           Format(sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(1)) +
                                           " but got " + Format(pixel1));
                        }

                        if ((Vector4) pixel2 != sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(2))
                        {
                            Debug.LogError("Error at (" + (runningTotalNumberOfKeyframes + keyframeIndex) + ", " +
                                           boneIndex + ") expected " +
                                           Format(sampledBoneMatrices[i][keyframeIndex, boneIndex].GetRow(2)) +
                                           " but got " + Format(pixel2));
                        }
                    }
                }

                runningTotalNumberOfKeyframes += sampledBoneMatrices[i].GetLength(0);
            }

            // tex0.Apply(false, true);
            // tex1.Apply(false, true);
            // tex2.Apply(false, true);

            bakedData.AnimationsDictionary = new Dictionary<string, AnimationClipData>();
            foreach (var clipData in bakedData.Animations)
            {
                bakedData.AnimationsDictionary[clipData.Name] = clipData;
            }

            return bakedData;
        }

        public static string Format(Vector4 v)
        {
            return "(" + v.x + ", " + v.y + ", " + v.z + ", " + v.w + ")";
        }

        public static string Format(Color v)
        {
            return "(" + v.r + ", " + v.g + ", " + v.b + ", " + v.a + ")";
        }

        private static Mesh CreateMesh(SkinnedMeshRenderer originalRenderer, Mesh mesh = null)
        {
            Mesh newMesh = new Mesh();
            Mesh originalMesh = mesh == null ? originalRenderer.sharedMesh : mesh;
            var boneWeights = originalMesh.boneWeights;

            originalRenderer.BakeMesh(newMesh);

            Vector3[] vertices = originalMesh.vertices;
            Vector2[] boneIds = new Vector2[originalMesh.vertexCount];
            Vector2[] boneInfluences = new Vector2[originalMesh.vertexCount];

            int[] boneRemapping = null;

            if (mesh != null)
            {
                var originalBindPoseMatrices = originalRenderer.sharedMesh.bindposes;
                var newBindPoseMatrices = mesh.bindposes;

                if (newBindPoseMatrices.Length != originalBindPoseMatrices.Length)
                {
                    //Debug.LogError(mesh.name + " - Invalid bind poses, got " + newBindPoseMatrices.Length + ", but expected "
                    //				+ originalBindPoseMatrices.Length);
                }
                else
                {
                    boneRemapping = new int[originalBindPoseMatrices.Length];
                    for (int i = 0; i < boneRemapping.Length; i++)
                    {
                        boneRemapping[i] = Array.FindIndex(originalBindPoseMatrices, x => x == newBindPoseMatrices[i]);
                    }
                }
            }

            var bones = originalRenderer.bones;
            for (int i = 0; i < originalMesh.vertexCount; i++)
            {
                int boneIndex0 = boneWeights[i].boneIndex0;
                int boneIndex1 = boneWeights[i].boneIndex1;

                if (boneRemapping != null)
                {
                    boneIndex0 = boneRemapping[boneIndex0];
                    boneIndex1 = boneRemapping[boneIndex1];
                }

                boneIds[i] = new Vector2((boneIndex0 + 0.5f) / bones.Length, (boneIndex1 + 0.5f) / bones.Length);

                float mostInfluentialBonesWeight = boneWeights[i].weight0 + boneWeights[i].weight1;

                boneInfluences[i] = new Vector2(boneWeights[i].weight0 / mostInfluentialBonesWeight,
                    boneWeights[i].weight1 / mostInfluentialBonesWeight);
            }

            newMesh.vertices = vertices;
            newMesh.uv2 = boneIds;
            newMesh.uv3 = boneInfluences;

            return newMesh;
        }

        private static Matrix4x4[,] SampleAnimationClip(GameObject root, AnimationClip clip,
            SkinnedMeshRenderer renderer, float framerate)
        {
            var bindPoses = renderer.sharedMesh.bindposes;
            var bones = renderer.bones;
            Matrix4x4[,] boneMatrices = new Matrix4x4[Mathf.CeilToInt(framerate * clip.length) + 3, bones.Length];
            for (int i = 1; i < boneMatrices.GetLength(0) - 1; i++)
            {
                float t = (float) (i - 1) / (boneMatrices.GetLength(0) - 3);

                var oldWrapMode = clip.wrapMode;
                clip.wrapMode = WrapMode.Clamp;
                clip.SampleAnimation(root, t * clip.length);
                clip.wrapMode = oldWrapMode;
                
                for (int j = 0; j < bones.Length; j++)
                {
                    boneMatrices[i, j] = bones[j].localToWorldMatrix * bindPoses[j];
                }
            }

            for (int j = 0; j < bones.Length; j++)
            {
                boneMatrices[0, j] = boneMatrices[boneMatrices.GetLength(0) - 2, j];
                boneMatrices[boneMatrices.GetLength(0) - 1, j] = boneMatrices[1, j];
            }

            return boneMatrices;
        }

        public static void ProcessMesh(SkinnedMeshRenderer originalRenderer, Mesh mesh)
        {
            var boneWeights = mesh.boneWeights;

            Vector2[] boneIds = new Vector2[mesh.vertexCount];
            Vector2[] boneInfluences = new Vector2[mesh.vertexCount];

            var bones = originalRenderer.bones;
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                int boneIndex0 = boneWeights[i].boneIndex0;
                int boneIndex1 = boneWeights[i].boneIndex1;

                boneIds[i] = new Vector2((boneIndex0 + 0.5f) / bones.Length, (boneIndex1 + 0.5f) / bones.Length);

                float mostInfluentialBonesWeight = boneWeights[i].weight0 + boneWeights[i].weight1;

                boneInfluences[i] = new Vector2(boneWeights[i].weight0 / mostInfluentialBonesWeight,
                    boneWeights[i].weight1 / mostInfluentialBonesWeight);
            }

            mesh.uv3 = boneIds;
            mesh.uv4 = boneInfluences;
        }

        #region Util methods

        public static void CopyMeshData(this Mesh originalMesh, Mesh newMesh)
        {
            var vertices = originalMesh.vertices;

            newMesh.vertices = vertices;
            newMesh.triangles = originalMesh.triangles;
            newMesh.normals = originalMesh.normals;
            newMesh.uv = originalMesh.uv;
            newMesh.tangents = originalMesh.tangents;
            newMesh.name = originalMesh.name;
        }

        private static float Distance(Color r1, Color r2)
        {
            return Mathf.Abs(r1.r - r2.r) + Mathf.Abs(r1.g - r2.g) + Mathf.Abs(r1.b - r2.b) + Mathf.Abs(r1.a - r2.a);
        }

        private static Color Negate(Color c)
        {
            return new Color(-c.r, -c.g, -c.b, -c.a);
        }

        private static Color GetTranslation(Vector4 rawTranslation, Color rotation)
        {
            Quaternion rot = new Quaternion(rotation.r, rotation.g, rotation.b, rotation.a);
            Quaternion trans = new Quaternion(rawTranslation.x, rawTranslation.y, rawTranslation.z, 0) * rot;

            return new Color(trans.x, trans.y, trans.z, trans.w) * 0.5f;
        }

        private static Color GetRotation(Quaternion rotation)
        {
            return new Color(rotation.x, rotation.y, rotation.z, rotation.w);
        }

        private static int Get1DCoord(int x, int y, int width)
        {
            return y * width + x;
        }

        #endregion
    }
}