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

namespace WowUnity
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

        public static Material ConfigureMaterial(MaterialDescription description, Material material, string modelImportPath, M2Utility.M2 metadata)
        {
            if (Regex.IsMatch(Path.GetFileNameWithoutExtension(modelImportPath), @"adt_\d{2}_\d{2}"))
                return ProcessADTMaterial(description, material, modelImportPath);

            M2Utility.Material materialData = M2Utility.GetMaterialData(material.name, metadata);
            Color materialColor = Color.white;
            if (metadata != null && metadata.colors.Count > 0)
            {
                materialColor = ProcessMaterialColors(material, metadata);
            }
            
            material.shader = Shader.Find(LIT_SHADER);
            material.SetColor("_BaseColor", materialColor);

            // Read a texture property from the material description.
            TexturePropertyDescription textureProperty;
            if (description.TryGetProperty("DiffuseColor", out textureProperty) && textureProperty.texture != null)
            {
                // Assign the texture to the material.
                material.SetTexture("_MainTex", textureProperty.texture);
            }
                
            ProcessFlagsForMaterial(material, materialData);
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

        public static Color ProcessMaterialColors(Material material, M2Utility.M2 metadata)
        {
            int i, j, k;
            Color newColor = Color.white;
            if (metadata.skin == null || metadata.skin.textureUnits.Count <= 0)
            {
                return newColor;
            }

            for (i = 0; i < metadata.textures.Count; i++)
            {
                if (material.name == metadata.textures[i].mtlName)
                    break;
            }

            for (j = 0; j < metadata.skin.textureUnits.Count; j++)
            {
                if (metadata.skin.textureUnits[j].geosetIndex == i)
                    break;
            }

            if (j < metadata.skin.textureUnits.Count)
                k = (int)metadata.skin.textureUnits[j].colorIndex;
            else
                return newColor;

            if (k < metadata.colors.Count)
            {
                newColor.r = metadata.colors[k].color.values[0][0][0];
                newColor.g = metadata.colors[k].color.values[0][0][1];
                newColor.b = metadata.colors[k].color.values[0][0][2];
                newColor.a = 1;
            }

            return newColor;
        }

        public static Material ProcessADTMaterial(MaterialDescription description, Material material, string modelImportPath)
        {
            material.shader = Shader.Find(ADT_CHUNK_SHADER);

            TexturePropertyDescription textureProperty;
            if (description.TryGetProperty("DiffuseColor", out textureProperty) && textureProperty.texture != null)
            {
                material.SetTexture("_BaseMap", textureProperty.texture);
            }

            LoadMetadataAndConfigureADT(material, modelImportPath);

            return material;
        }

        public static void LoadMetadataAndConfigureADT(Material mat, string assetPath)
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

        public static void ExtractMaterialFromAsset(Material material)
        {
            string assetPath = AssetDatabase.GetAssetPath(material);
            string newMaterialPath = "Assets/Materials/" + material.name + ".mat";
            Material newMaterialAsset;

            if (!Directory.Exists("Assets/Materials"))
            {
                Directory.CreateDirectory("Assets/Materials");
            }
            
            if (!File.Exists(newMaterialPath))
            {
                newMaterialAsset = new Material(material);
                AssetDatabase.CreateAsset(newMaterialAsset, newMaterialPath);
            }
            else
            {
                newMaterialAsset = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);
            }

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(material), newMaterialAsset);

            AssetDatabase.WriteImportSettingsIfDirty(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
