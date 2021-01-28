using System.Collections;
using System.Collections.Generic;
using Plugins.Unity.GPUAnimation;
using Unity.GPUAnimation;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace com.skeleton.Baker.GPUAnimate
{
    public class AnimateLoader : MonoBehaviour
    {
        private AnimateData animateData;
        public Material material;
        private List<BakedAnimationClip> bakedData;
        private MaterialPropertyBlock prop;
        private MeshRenderer render;

        private bool loadSuccess;

        private IEnumerator LoadAsset(byte[] datas)
        {
            var createRequest = AssetBundle.LoadFromMemoryAsync(datas);
            yield return createRequest;
            var assets = createRequest.assetBundle.LoadAllAssets<BakedTextureData>();
            var request = createRequest.assetBundle.LoadAssetWithSubAssetsAsync(assets[0].assetName);
            yield return request;
            InitData(request.allAssets, assets[0]);
            if (animateData != null && animateData.isValid)
            {
                InitGame();
                FindObjectOfType<ShowClipButton>()?.ShowClipBtn(animateData, this);
            }

            //createRequest=createRequest.assetBundle.LoadAllAssetsAsync();
        }

        public TextAsset data;

        private void Awake()
        {
            LoadAssetSuccess(data);
        }

        private void LoadAssetSuccess(TextAsset dataAsset)
        {
            StartCoroutine(LoadAsset(dataAsset.bytes));
        }

        private void InitGame()
        {
            prop = new MaterialPropertyBlock();
            bakedData = new List<BakedAnimationClip>();
            render = gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<MeshFilter>().sharedMesh = animateData.mesh;
            foreach (var clipData in animateData.textureData.clipDatas)
            {
                bakedData.Add(new BakedAnimationClip(animateData.matrix0, clipData));
            }

            Material[] materials = new Material[animateData.textureData.subMeshCount];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = Instantiate(material);
                materials[i].mainTexture = animateData.mainTextures[i];
                materials[i].SetTexture("_AnimationTexture0", animateData.matrix0);
                materials[i].SetTexture("_AnimationTexture1", animateData.matrix1);
                materials[i].SetTexture("_AnimationTexture2", animateData.matrix2);
            }

            render.materials = materials;
            loadSuccess = true;
        }

        private void Update()
        {
            if (!loadSuccess)
            {
                return;
            }

            float3 coordinate = bakedData[index]
                .ComputeCoordinate(Time.time / animateData.textureData.clipDatas[index].length % 1);
            prop.SetVector("_TextureCoord", new float4(coordinate, 0));
            render.SetPropertyBlock(prop);
        }

        private int index;

        public void SetClipIndex(int i)
        {
            index = i;
        }

        private void InitData(Object[] allAssets, BakedTextureData data)
        {
            animateData = new AnimateData
            {
                textureData = data
            };
            Texture2D[] mainTexture2Ds = new Texture2D[animateData.textureData.subMeshCount];
            animateData.mainTextures = mainTexture2Ds;
            foreach (var asset in allAssets)
            {
                if (asset is Mesh mesh)
                {
                    animateData.mesh = mesh;
                }
                else if (asset is Texture2D texture2D)
                {
                    if (texture2D.name == AssetsBuildConstants.MATRIX0_TEXTURE_NAME)
                    {
                        animateData.matrix0 = texture2D;
                    }
                    else if (texture2D.name == AssetsBuildConstants.MATRIX1_TEXTURE_NAME)
                    {
                        animateData.matrix1 = texture2D;
                    }
                    else if (texture2D.name == AssetsBuildConstants.MATRIX2_TEXTURE_NAME)
                    {
                        animateData.matrix2 = texture2D;
                    }
                    else if (texture2D.name.Contains(AssetsBuildConstants.MAIN_TEXTURES_NAME))
                    {
                        int index = int.Parse(
                            texture2D.name.Substring(AssetsBuildConstants.MAIN_TEXTURES_NAME.Length + 1));
                        if (mainTexture2Ds.Length > index)
                        {
                            mainTexture2Ds[index] = texture2D;
                        }
                    }
                }
            }
        }

        public class AnimateData
        {
            public BakedTextureData textureData;
            public Texture2D matrix0;
            public Texture2D matrix1;
            public Texture2D matrix2;
            public Mesh mesh;
            public Texture2D[] mainTextures;

            public bool isValid
            {
                get
                {
                    bool valid = true;
                    if (textureData == null)
                    {
                        valid = false;
                        Debug.LogError("textureData is null!!!");
                    }

                    if (matrix0 == null)
                    {
                        valid = false;
                        Debug.LogError($"[{textureData?.assetName}]matrix0 texture is null!!!");
                    }

                    if (matrix1 == null)
                    {
                        valid = false;
                        Debug.LogError($"[{textureData?.assetName}]matrix1 texture is null!!!");
                    }

                    if (matrix2 == null)
                    {
                        valid = false;
                        Debug.LogError($"[{textureData?.assetName}]matrix2 texture is null!!!");
                    }

                    if (mesh == null)
                    {
                        valid = false;
                        Debug.LogError($"[{textureData?.assetName}]mesh is null!!!");
                    }

                    return valid;
                }
            }
        }
    }
}