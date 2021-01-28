using System;
using System.Collections.Generic;
using Unity.GPUAnimation;
using UnityEngine;
using Random = UnityEngine.Random;

namespace com.skeleton.Baker.GPUAnimate
{
    public class AnimatorManager : MonoBehaviour
    {
        public int[] lodDistances = {15, 50, 100};
        public GameObject[] fbxWithLod;

        public AnimationClip[] clips;

        // public Material[] materials;
        public int frames = 24;
        [NonSerialized] public KeyframeTextureBaker.BakedData[] bakedDatas;

        private LinkedList<AnimatorInstance> animatorInstances;

        public Material matTemplate;
        public Texture2D[] mainTexes;
        public Texture2D[] normalTexes;
        public Texture2D[] metallicTexes;

        public void Awake()
        {
            if (fbxWithLod == null || fbxWithLod.Length == 0)
            {
                Debug.LogError("not set fbx!!!!!!!!");
                return;
            }

            if (clips == null || clips.Length == 0)
            {
                Debug.LogError("not set animationClip!!!!!!!!");
                return;
            }

            if (mainTexes == null || mainTexes.Length == 0)
            {
                Debug.LogError("not set _MainTexes!!!!!!!!");
                return;
            }

            animatorInstances = new LinkedList<AnimatorInstance>();
            bakedDatas = new KeyframeTextureBaker.BakedData[fbxWithLod.Length];
            for (int i = 0; i < fbxWithLod.Length; i++)
            {
                GameObject fbx = fbxWithLod[i];

                bakedDatas[i] =
                    KeyframeTextureBaker.BakeClips01(fbx, clips, frames);
                bakedDatas[i].materials = new Material[mainTexes.Length];
                bakedDatas[i].bakedAnimationClips = new BakedAnimationClip[bakedDatas[i].Animations.Count];
                for (int k = 0; k < bakedDatas[i].bakedAnimationClips.Length; k++)
                    bakedDatas[i].bakedAnimationClips[k] =
                        new BakedAnimationClip(bakedDatas[i].AnimationTextures, bakedDatas[i].Animations[k]);
                for (int j = 0; j < mainTexes.Length; j++)
                {
                    bakedDatas[i].materials[j] = Instantiate(matTemplate);
                    bakedDatas[i].materials[j].mainTexture = mainTexes[j];
                    if (normalTexes != null && normalTexes.Length > j)
                    {
                        bakedDatas[i].materials[j].SetTexture("_BumpMap", normalTexes[j]);
                    }

                    if (metallicTexes != null && metallicTexes.Length > j)
                    {
                        bakedDatas[i].materials[j].SetTexture("_MetallicGlossMap", metallicTexes[j]);
                    }

                    bakedDatas[i].materials[j]
                        .SetTexture("_AnimationTexture0", bakedDatas[i].AnimationTextures.Animation0);
                    bakedDatas[i].materials[j]
                        .SetTexture("_AnimationTexture1", bakedDatas[i].AnimationTextures.Animation1);
                    bakedDatas[i].materials[j]
                        .SetTexture("_AnimationTexture2", bakedDatas[i].AnimationTextures.Animation2);
                }
            }

            // bindClickBtn?.onClick.AddListener(click);
        }

        private void Start()
        {
            foreach (var data in bakedDatas)
            {
                data.ApplyTextures();
            }

            Generate();
        }

        public AnimatorInstance CreateInstance(Matrix4x4 TRS)
        {
            AnimatorInstance animatorInstance = new AnimatorInstance(TRS, this);
            animatorInstance.SetClipIndex(Random.Range(0, bakedDatas[0].Animations.Count));
            animatorInstances.AddLast(animatorInstance);
            return animatorInstance;
        }


        public void Update()
        {
            if (animatorInstances == null)
            {
                return;
            }

            var node = animatorInstances.First;
            while (node != null)
            {
                var animatorInstance = node.Value;
                if (animatorInstance.IsDestroy())
                {
                    var next = node.Next;
                    animatorInstances.Remove(node);
                    node = next;
                    continue;
                }

                animatorInstance.DrawMesh();
                node = node.Next;
            }
        }

        private void Generate()
        {
            for (int c = 0; c < clips.Length; c++)
            {
                for (int j = 0; j < 100; j++)
                {
                    Vector3 position = transform.position + new Vector3(j * 5, 0, c * 5);
                    AnimatorInstance animatorInstance = CreateInstance(Matrix4x4.Translate(position));
                    animatorInstance.SetClipIndex(c);
                    animatorInstances.AddLast(animatorInstance);
                }
            }
        }

        public void OnDestroy()
        {
            if (animatorInstances == null)
            {
                return;
            }

            foreach (var animatorInstance in animatorInstances)
            {
                animatorInstance.Destroy();
            }

            animatorInstances.Clear();
            animatorInstances = null;
        }
    }
}