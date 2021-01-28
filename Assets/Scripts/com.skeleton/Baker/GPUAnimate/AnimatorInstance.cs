using GPUAnimate;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace com.skeleton.Baker.GPUAnimate
{
    public class AnimatorInstance
    {
        private Matrix4x4 transform;
        private int clipIndex;
        MaterialPropertyBlock prop;
        AnimatorManager animatorManager;

        public AnimatorInstance(Matrix4x4 TRS, AnimatorManager animatorManager)
        {
            transform = TRS;
            this.animatorManager = animatorManager;
            prop = new MaterialPropertyBlock();

            gameObject = new GameObject();
            gameObject.transform.SetParent(animatorManager.gameObject.transform);
            gameObject.transform.position = transform.GetColumn(3);
            meshFilter = gameObject.AddComponent<MeshFilter>();
            renderer = gameObject.AddComponent<MeshRenderer>();
            randomSeek = Random.Range(0f, 100f);
        }

        private MeshFilter meshFilter;
        private GameObject gameObject;
        private MeshRenderer renderer;
        private float randomSeek;

        public void DrawMesh()
        {
            int lodLevel = FindLodLevel();
            var bakedAnimationClips = animatorManager.bakedDatas[lodLevel].bakedAnimationClips;
            var animationClip = bakedAnimationClips[clipIndex];
            float normalizedTime = animationClip.ComputeNormalizedTime(Time.time + randomSeek);
            float3 sampleCoord = animationClip.ComputeCoordinate(normalizedTime);
            prop.SetVector("_TextureCoord", new float4(sampleCoord, 0));
            meshFilter.mesh = animatorManager.bakedDatas[lodLevel].NewMesh;
            renderer.materials = animatorManager.bakedDatas[lodLevel].materials;
            renderer.SetPropertyBlock(prop);

            // MaterialHelper materialHelper = Camera.main.GetComponent<MaterialHelper>();
            // Graphics.DrawMeshInstanced(meshFilter.mesh, 0, renderer.sharedMaterial,
            //     new[]
            //     {
            //         Matrix4x4.TRS(gameObject.transform.position + Vector3.one, gameObject.transform.rotation, Vector3.one)
            //     }, 1, prop);
        }

        public void DrawMesh(float time)
        {
            int lodLevel = FindLodLevel();
            var bakedAnimationClips = animatorManager.bakedDatas[lodLevel].bakedAnimationClips;
            var animationClip = bakedAnimationClips[clipIndex];
            float normalizedTime = animationClip.ComputeNormalizedTime(time);
            float3 sampleCoord = animationClip.ComputeCoordinate(normalizedTime);
            prop.SetVector("_TextureCoord", new float4(sampleCoord, 0));
            meshFilter.mesh = animatorManager.bakedDatas[lodLevel].NewMesh;
            renderer.materials = animatorManager.bakedDatas[lodLevel].materials;
            renderer.SetPropertyBlock(prop);
        }

        public void SetTRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            transform.SetTRS(position, rotation, scale);
        }

        public void SetClipIndex(int i)
        {
            clipIndex = i;
        }

        public int GetClipIndex()
        {
            return clipIndex;
        }

        public void DrawMesh01(float computeNormalizedTime, Vector3 eye)
        {
        }


        public bool IsDestroy()
        {
            if (gameObject == null)
            {
                Destroy();
                return true;
            }

            return false;
        }

        public void Destroy()
        {
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
                gameObject = null;
                meshFilter = null;
                renderer = null;
            }

            animatorManager = null;
            prop = null;
        }

        private int FindLodLevel()
        {
            Vector3 pos = transform.GetColumn(3);
            float distance = (pos - Camera.main.transform.position).magnitude;
            int result = -1;
            for (int i = 0; i < animatorManager.lodDistances.Length; i++)
            {
                if (distance < animatorManager.lodDistances[i])
                {
                    result = i;
                    break;
                }
            }

            if (result >= 0 && result < animatorManager.bakedDatas.Length)
            {
                return result;
            }

            return animatorManager.bakedDatas.Length - 1;
        }
    }
}