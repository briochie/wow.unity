using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace WowExportUnityifier
{
    class M2Utility
    {
        private static List<string> importedModelPathQueue = new List<string>();

        public static void QueueMetadata(string filePath)
        {
            importedModelPathQueue.Add(filePath);
        }

        public static void BeginQueue()
        {
            EditorApplication.update -= BeginQueue;

            if (importedModelPathQueue.Count == 0)
            {
                return;
            }

            List<string> iteratingList = new List<string>(importedModelPathQueue);

            foreach (string path in iteratingList)
            {
                importedModelPathQueue.Remove(path);
                M2 metadata = ReadMetadataFor(path);

                if (metadata.fileName == null)
                    continue;

                if (metadata.textureTransforms[0].translation.timestamps.Count > 0)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    GameObject instantiatedGameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    Animation newAnimator = instantiatedGameObject.transform.GetChild(0).gameObject.GetComponent<Animation>();

                    if (newAnimator == null)
                    {
                        newAnimator = instantiatedGameObject.transform.GetChild(0).gameObject.AddComponent<Animation>();
                    }

                    AnimationClip newClip = AnimationUtility.CreateAnimationClip(metadata.textureTransforms[0]);
                    AssetDatabase.CreateAsset(newClip, path + ".anim");
                    newAnimator.clip = newClip;

                    PrefabUtility.ApplyPrefabInstance(instantiatedGameObject, InteractionMode.UserAction);
                    PrefabUtility.SavePrefabAsset(prefab);
                    UnityEngine.Object.DestroyImmediate(instantiatedGameObject);
                }
            }
        }

        public static M2 ReadMetadataFor(string path)
        {
            string pathToMetadata = Path.GetDirectoryName(path) + "/" + Path.GetFileNameWithoutExtension(path) + ".json";

            if (!File.Exists(pathToMetadata))
            {
                return null;
            }

            var sr = new StreamReader(Application.dataPath.Replace("Assets", "") + pathToMetadata);
            var fileContents = sr.ReadToEnd();
            sr.Close();

            return JsonConvert.DeserializeObject<M2>(fileContents);
        }

        public static Material GetMaterialData(string materialName, M2 metadata)
        {
            Material data = new Material();
            data.flags = 0;
            data.blendingMode = 0;

            //I have no idea why they sometimes don't match sizes.
            // I'm guessing if there's no material entry, default is intended.
            for(int i = 0; i < metadata.textures.Length; i++)
            {
                Texture texture = metadata.textures[i];
                if (texture.mtlName != materialName)
                    continue;

                if (metadata.materials.Length <= i)
                    i = metadata.materials.Length - 1;
                
                data = metadata.materials[i];
                break;
            }

            return data;
        }

        [Serializable]
        public class M2
        {
            public uint fileDataID;
            public string fileName;
            public string internalName;
            public Texture[] textures;
            public short[] textureTypes;
            public Material[] materials;
            public short[] textureCombos;
            public ColorData[] colors;
            public TextureTransform[] textureTransforms;
            public uint[] textureTransformsLookup;
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
    }
}
