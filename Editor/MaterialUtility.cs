using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace WowExportUnityifier
{
    class MaterialUtility
    {
        public const string LIT_SHADER = "Universal Render Pipeline/Simple Lit";
        public const string UNLIT_SHADER = "Universal Render Pipeline/Unlit";
        public const string EFFECT_SHADER = "Universal Render Pipeline/Particles/Unlit";
        public const string ADT_CHUNK_SHADER = "wow.export Unityifier/TerrainChunk";

        public enum MaterialFlags : short
        {
            None = 0x0,
            Unlit = 0x1,
            Unfogged = 0x2,
            TwoSided = 0x4
        }

        public enum BlendModes : short
        {
            Opaque = 0,
            AlphaKey = 1,
            Alpha = 2,
            NoAlphaAdd = 3,
            Add = 4,
            Mod = 5,
            Mod2X = 6,
            BlendAdd = 7
        }

        public static Material ConfigureMaterial(MaterialDescription description, Material material, string modelImportPath)
        {
            if (Regex.IsMatch(Path.GetFileNameWithoutExtension(modelImportPath), @"adt_\d{2}_\d{2}"))
                return ProcessADTMaterial(description, material, modelImportPath);

            M2Utility.M2 metadata = M2Utility.ReadMetadataFor(modelImportPath);
            M2Utility.Material materialData = M2Utility.GetMaterialData(material.name, metadata);

            material.shader = Shader.Find(LIT_SHADER);
            material.SetColor("_BaseColor", Color.white);

            // Read a texture property from the material description.
            TexturePropertyDescription textureProperty;
            if (description.TryGetProperty("DiffuseColor", out textureProperty) && textureProperty.texture != null)
            {
                // Assign the texture to the material.
                material.SetTexture("_BaseMap", textureProperty.texture);
            }
                
            ProcessFlagsForMaterial(material, materialData);
            CreateMaterialAsset(material);
            return material;
        }

        public static void ProcessFlagsForMaterial(Material material, M2Utility.Material data)
        {
            //Flags first
            if ((data.flags & (short)MaterialFlags.Unlit) != (short)MaterialFlags.None)
            {
                material.shader = Shader.Find(UNLIT_SHADER);
            }

            if ((data.flags & (short)MaterialFlags.TwoSided) != (short)MaterialFlags.None)
            {
                material.doubleSidedGI = true;
                material.SetFloat("_Cull", 0);
            }

            //Now blend modes
            if (data.blendingMode == (short)BlendModes.AlphaKey)
            {
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetFloat("_AlphaClip", 1);
            }

            if (data.blendingMode == (short)BlendModes.Alpha)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_Blend", 0);
                material.SetFloat("_Surface", 1);
                material.SetFloat("_ZWrite", 0);
            }

            if (data.blendingMode == (short)BlendModes.Add)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetFloat("_Cutoff", 0);
                material.SetFloat("_Blend", 1);
                material.SetFloat("_Surface", 1);
                material.SetFloat("_SrcBlend", 1);
                material.SetFloat("_DstBlend", 1);
                material.SetFloat("_ZWrite", 0);
                material.SetShaderPassEnabled("ShadowCaster", false);
            }
        }

        public static void ProcessMaterialColors(Material material, M2Utility.ColorData data)
        {
            M2Utility.Color color = data.color;
            M2Utility.Alpha alpha = data.alpha;

            Color newColor = new Color(color.values[0][0], color.values[0][1], color.values[0][2]);
            newColor.a = (float)alpha.values[0] / 65535.0f;

            material.SetColor("_BaseColor", newColor);
        }

        public static Material ProcessADTMaterial(MaterialDescription description, Material material, string modelImportPath)
        {
            material.shader = Shader.Find(ADT_CHUNK_SHADER);

            TexturePropertyDescription textureProperty;
            if (description.TryGetProperty("DiffuseColor", out textureProperty) && textureProperty.texture != null)
            {
                material.SetTexture("_BaseMap", textureProperty.texture);
            }

            LoadChunkData(material, modelImportPath);

            return material;
        }

        public static void LoadChunkData(Material mat, string assetPath)
        {
            string jsonFilePath = Path.GetDirectoryName(assetPath) + "\\" + mat.name + ".json";
            var sr = new StreamReader(Application.dataPath.Replace("Assets", "") + jsonFilePath);
            var fileContents = sr.ReadToEnd();
            sr.Close();

            TerrainMaterialGenerator.Chunk newChunk = JsonUtility.FromJson<TerrainMaterialGenerator.Chunk>(fileContents);

            Vector4 scaleVector = new Vector4();
            TerrainMaterialGenerator.Layer currentLayer;
            for (int i = 0; i < newChunk.layers.Count; i++)
            {
                currentLayer = newChunk.layers[i];
                string texturePath = Path.Combine(Path.GetDirectoryName(@assetPath), @currentLayer.file);
                texturePath = Path.GetFullPath(texturePath);
                texturePath = texturePath.Substring(texturePath.IndexOf("Assets\\"));

                Texture2D layerTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D));
                mat.SetTexture("Layer_" + i, layerTexture);
                scaleVector[i] = currentLayer.scale;
            }

            mat.SetVector("Scale", scaleVector);
        }

        public static void CreateMaterialAsset(Material material)
        {
            string path = "Assets/Materials/" + material.name + ".mat";

            if (!Directory.Exists("Assets/Materials"))
            {
                Directory.CreateDirectory("Assets/Materials");
            }

            if (!File.Exists(path))
            {
                AssetDatabase.CreateAsset(material, path);
            }
        }
    }
}
