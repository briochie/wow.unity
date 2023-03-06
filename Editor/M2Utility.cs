using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace WowUnity
{
    class M2Utility
    {
        private static List<string> importedModelPathQueue = new List<string>();

        public static void QueueMetadata(string filePath)
        {
            importedModelPathQueue.Add(filePath);
        }

        public static void PostProcessImports()
        {
            if (importedModelPathQueue.Count == 0)
            {
                return;
            }

            List<string> iteratingList = new List<string>(importedModelPathQueue);

            foreach (string path in iteratingList)
            {
                M2 metadata = ReadMetadataFor(path);

                if (metadata == null)
                    continue;

                GameObject prefab = FindOrCreatePrefab(path);

                if (metadata.boneData != null)
                {
                    GenerateBoneData(prefab, metadata);
                }

                if (metadata.textureTransforms.Count > 0 && metadata.textureTransforms[0].translation.timestamps.Count > 0)
                {
                    for (int i = 0; i < metadata.textureTransforms.Count; i++)
                    {
                        AnimationClip newClip = AnimationUtility.CreateAnimationClip(metadata.textureTransforms[i]);
                        AssetDatabase.CreateAsset(newClip, Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "[" + i +  "]" + ".anim");
                    }
                }
            }

            //Processing done: remove all paths from the queue
            importedModelPathQueue.Clear();
        }

        public static GameObject FindOrCreatePrefab(string path)
        {
            string prefabPath = Path.ChangeExtension(path, "prefab");
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab == null)
            {
                return GeneratePrefab(path);
            }

            return existingPrefab;
        }

        public static GameObject GeneratePrefab(string path)
        {
            string prefabPath = Path.ChangeExtension(path, "prefab");
            GameObject importedModelObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (importedModelObject == null)
            {
                Debug.LogWarning("Tried to create prefab, but could not find imported model: " + path);
                return null;
            }

            ConfigureRendererMaterials(importedModelObject);

            GameObject rootModelInstance = PrefabUtility.InstantiatePrefab(importedModelObject) as GameObject;

            //Set the object as static, and all it's child objects
            rootModelInstance.isStatic = true;
            foreach (Transform childTransform in rootModelInstance.transform)
            {
                childTransform.gameObject.isStatic = true;
            }

            GameObject newPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(rootModelInstance, prefabPath, InteractionMode.AutomatedAction);
            AssetDatabase.Refresh();
            UnityEngine.Object.DestroyImmediate(rootModelInstance);

            return newPrefab;
        }

        private static void ConfigureRendererMaterials(GameObject importedModelObject)
        {
            //Manage materials for imported models.

            //First, we need to sample all renderers that belong to the specified game object.
            Renderer[] renderers = importedModelObject.GetComponentsInChildren<Renderer>();

            //Now we will loop through all renderers present in the game object
            //and call the MaterialUtility to create the asset.
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                for (int materialIndex = 0; materialIndex < renderers[rendererIndex].sharedMaterials.Length; materialIndex++)
                {
                    //We don't need to worry about repeat materials here,
                    //because the CreateMaterialAsset already handles this case for us.
                    MaterialUtility.ExtractMaterialFromAsset(renderers[rendererIndex].sharedMaterials[materialIndex]);
                }
            }
            AssetDatabase.Refresh();
        }

        public static M2 ReadMetadataFor(string path)
        {
            M2 newMetadata = new M2();
            BoneData newBoneData = new BoneData();
            string pathToMetadata = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ".json";
            string pathToBoneMetadata = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + "_bones.json";

            JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            if (!File.Exists(pathToMetadata))
            {
                return null;
            }

            var sr = new StreamReader(Application.dataPath.Replace("Assets", "") + pathToMetadata);
            var fileContents = sr.ReadToEnd();
            sr.Close();
            newMetadata = JsonConvert.DeserializeObject<M2>(fileContents, settings);

            if (File.Exists(pathToBoneMetadata))
            {
                var bsr = new StreamReader(Application.dataPath.Replace("Assets", "") + pathToBoneMetadata);
                var boneFileContents = bsr.ReadToEnd();
                newBoneData = JsonConvert.DeserializeObject<BoneData>(boneFileContents, settings);
                bsr.Close();
                newMetadata.boneData = newBoneData;
            }

            return newMetadata;
        }

        public static Material GetMaterialData(string materialName, M2 metadata)
        {
            Material data = new Material();
            data.flags = 0;
            data.blendingMode = 0;

            //I have no idea why they sometimes don't match sizes.
            // I'm guessing if there's no material entry, default is intended.
            for(int i = 0; i < metadata.textures.Count; i++)
            {
                Texture texture = metadata.textures[i];
                if (texture.mtlName != materialName)
                    continue;

                if (metadata.materials.Count <= i)
                    i = metadata.materials.Count - 1;
                
                data = metadata.materials[i];
                break;
            }

            return data;
        }

        public static void GenerateBoneData(GameObject prefab, M2 metadata)
        {
            GameObject gameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

            //No renderers? No meshes? Unlikely, but
            if (renderers.Length == 0 || meshFilters.Length == 0)
            {
                return;
            }

            GameObject armitureObject = new GameObject("Armiture");
            List<Transform> bones = new List<Transform>();
            armitureObject.transform.parent = gameObject.transform;

            for (int i = 0; i < metadata.boneData.bones.Count; i++)
            {
                Bone boneData = metadata.boneData.bones[i];
                GameObject newBone = new GameObject("Bone " + i);
                if (boneData.parentBone > -1)
                {
                    //TODO:
                    //Find the parent bone,
                    //set the bone as a child of the parent.
                }
                else
                {
                    newBone.transform.parent = armitureObject.transform;
                }

                Vector3 bonePivotPosition = new Vector3();
                bonePivotPosition.x = boneData.pivot[0];
                bonePivotPosition.y = boneData.pivot[2];
                bonePivotPosition.z = boneData.pivot[1];

                newBone.transform.localPosition = bonePivotPosition;
                bones.Add(newBone.transform);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                //Making an assumption that each geoset
                //will correlate to each "submesh" in the
                //m2 metadata.
                Renderer currentRenderer = renderers[i];
                SubMesh currentSubMeshData = metadata.skin.subMeshes[i];
                Mesh currentMesh = meshFilters[i].sharedMesh;
                SkinnedMeshRenderer newSkinnedMeshRenderer = currentRenderer.gameObject.AddComponent<SkinnedMeshRenderer>();
                newSkinnedMeshRenderer.rootBone = armitureObject.transform;
                newSkinnedMeshRenderer.bones = bones.ToArray();

                for (int j = 0; j < currentMesh.vertexCount; j++)
                {
                    List<BoneWeight1> newWeights = new List<BoneWeight1>();
                    for(int k = 0; k < currentSubMeshData.boneInfluences; k++)
                    {
                        BoneWeight1 newWeight = new BoneWeight1();
                        newWeight.boneIndex = (int)currentSubMeshData.centerBoneIndex;
                        newWeight.weight = 1;
                    }
                }
            }

            PrefabUtility.ApplyPrefabInstance(gameObject, InteractionMode.AutomatedAction);
            UnityEngine.Object.DestroyImmediate(gameObject);
            bones.Clear();
        }

        [Serializable]
        public class M2
        {
            public uint fileDataID;
            public string fileName;
            public string internalName;
            public Skin skin;
            public BoneData boneData;
            public List<Texture> textures = new List<Texture>();
            public List<short> textureTypes = new List<short>();
            public List<Material> materials = new List<Material>();
            public List<short> textureCombos = new List<short>();
            public List<ColorData> colors = new List<ColorData>();
            public List<TextureTransform> textureTransforms = new List<TextureTransform>();
            public List<uint> textureTransformsLookup = new List<uint>();
        }

        [Serializable]
        public class Skin
        {
            public List<SubMesh> subMeshes = new List<SubMesh>();
            public List<TextureUnit> textureUnits = new List<TextureUnit>();
        }

        [Serializable]
        public struct SubMesh
        {
            public bool enabled;
            public uint submeshID;
            public uint level;
            public uint vertexStart;
            public uint vertexCount;
            public uint triangleStart;
            public uint triangleCount;
            public uint boneCount;
            public uint boneStart;
            public uint boneInfluences;
            public uint centerBoneIndex;
            public List<float> centerPosition;
            public List<float> sortCenterPosition;
            public float sortRadius;
        }

        [Serializable]
        public struct TextureUnit
        {
            public uint skinSelectionIndex;
            public uint geosetIndex;
            public uint colorIndex;
        }

        [Serializable]
        public struct Texture
        {
            public string fileNameInternal;
            public string fileNameExternal;
            public string mtlName;
            public short flag;
            public uint fileDataID;
        }

        [Serializable]
        public struct Material
        {
            public short flags;
            public uint blendingMode;
        }

        [Serializable]
        public struct ColorData
        {
            public MultiValueAnimationInformation color;
            public SingleValueAnimationInformation alpha;
        }

        [Serializable]
        public struct TextureTransform
        {
            public MultiValueAnimationInformation translation;
            public MultiValueAnimationInformation rotation;
            public MultiValueAnimationInformation scaling;
        }

        [Serializable]
        public struct SingleValueAnimationInformation
        {
            public uint globalSeq;
            public int interpolation;
            public List<List<uint>> timestamps;
            public List<List<float>> values;
        }

        [Serializable]
        public struct MultiValueAnimationInformation
        {
            public uint globalSeq;
            public int interpolation;
            public List<List<uint>> timestamps;
            public List<List<List<float>>> values;
        }

        [Serializable]
        public struct Bone
        {
            public uint flags;
            public int boneID;
            public int parentBone;
            public int subMeshID;
            public long boneNameCRC;
            public MultiValueAnimationInformation translation;
            public MultiValueAnimationInformation rotation;
            public MultiValueAnimationInformation scale;
            public List<float> pivot;
        }

        [Serializable]
        public class BoneData
        {
            public List<Bone> bones;
            public List<uint?> boneWeights;
            public List<uint?> boneIndicies;
        }
    }
}
