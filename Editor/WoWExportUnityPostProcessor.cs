using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System;

public class WoWExportUnityPostprocessor : AssetPostprocessor
{
    private string SHADER = "Universal Render Pipeline/Simple Lit";
    private string ADT_CHUNK_SHADER = "wow.export Unityifier/TerrainChunk";

    public override int GetPostprocessOrder()
    {
        return 9001; // must be after unitys post processor so it doesn't overwrite our own stuff
    }

    public override uint GetVersion()
    {
        return 0;
    }

    private bool ValidAsset()
    {
        if (!assetPath.Contains(".obj"))
            return false;
        if (assetPath.Contains(".phys.obj"))
            return false;

        return true;
    }

    public void OnPreprocessTexture()
    {
        bool match = Regex.IsMatch(assetPath, @"tex_\d{2}_\d{2}_\d{1,3}(?=\.png)");

        if (!match)
        {
            return;
        }

        TextureImporter textureImporter = (TextureImporter)assetImporter;
        textureImporter.wrapMode = TextureWrapMode.Clamp;
    }

    public void OnPreprocessModel()
    {
        if (!ValidAsset())
        {
            return;
        }

        ModelImporter modelImporter = assetImporter as ModelImporter;
        modelImporter.generateSecondaryUV = true;
        modelImporter.secondaryUVMarginMethod = ModelImporterSecondaryUVMarginMethod.Calculate;
        modelImporter.secondaryUVMinLightmapResolution = 16;
        modelImporter.secondaryUVMinObjectScale = 1;

        modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        modelImporter.materialName = ModelImporterMaterialName.BasedOnTextureName;
        modelImporter.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
    }

    public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] materialAnimation)
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;

        if (!ValidAsset())
        {
            return;
        }

        Shader shader = Shader.Find(SHADER);

        if (Regex.IsMatch(Path.GetFileNameWithoutExtension(assetPath), @"adt_\d{2}_\d{2}"))
            shader = Shader.Find(ADT_CHUNK_SHADER);

        material.shader = shader;

        List<string> props = new List<string>();
        // list the properties of type Vector4 :
        description.GetVector4PropertyNames(props);

        // Read a texture property from the material description.
        TexturePropertyDescription textureProperty;
        if (description.TryGetProperty("DiffuseColor", out textureProperty) && textureProperty.texture != null)
        {
            // Assign the texture to the material.
            material.SetTexture("_BaseMap", textureProperty.texture);
        }

        if (material.GetTexture("_BaseMap") == null)
        {
            return;
        }

        material.SetColor("_BaseColor", Color.white);

        if (material.shader.name == ADT_CHUNK_SHADER)
        {
            LoadChunkData(material);
        }
        else if (material.shader.name == SHADER)
        {
            string path = "Assets/Materials/" + textureProperty.texture.name + ".mat";
            Material prefabMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (prefabMaterial == null)
            {
                AssetDatabase.CreateAsset(material, path);
                prefabMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }
    }

    void LoadChunkData(Material mat)
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

    public void OnPostprocessModel(GameObject gameObject)
    {
        if (!ValidAsset())
        {
            return;
        }

        GameObject physicsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath.Replace(".obj", ".phys.obj"));
        MeshRenderer[] childRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();

        if (physicsPrefab == null || physicsPrefab.GetComponentInChildren<MeshFilter>() == null)
        {
            foreach (MeshRenderer child in childRenderers)
            {
                child.gameObject.AddComponent<MeshCollider>();
            }
        }
        else
        {
            GameObject collider = new GameObject();
            collider.transform.SetParent(gameObject.transform);
            collider.name = "Collision";
            MeshFilter collisionMesh = physicsPrefab.GetComponentInChildren<MeshFilter>();
            MeshCollider parentCollider = collider.AddComponent<MeshCollider>();
            parentCollider.sharedMesh = collisionMesh.sharedMesh;
        }

        ModelImporter modelImporter = assetImporter as ModelImporter;
        modelImporter.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnTextureName, ModelImporterMaterialSearch.RecursiveUp);
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach(string path in importedAssets)
        {
            if (!Path.GetFileName(path).Contains("_ModelPlacementInformation.csv"))
            {
                continue;
            }

            DoodadUtility.QueuePlacementData(path);
        }

        EditorApplication.update += DoodadUtility.BeginQueue;
    }

    private Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        if (!ValidAsset() || material.shader.name == ADT_CHUNK_SHADER)
        {
            return null;
        }

        if (!material.HasProperty("_BaseMap") || material.GetTexture("_BaseMap") == null)
        {
            return null;
        }

        string path = "Assets/Materials/" + material.GetTexture("_BaseMap").name + ".mat";
        Material prefabMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (prefabMaterial == null)
            prefabMaterial = material;

        return prefabMaterial;
    }
}
