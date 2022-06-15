using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.AssetImporters;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using WowExportUnityifier;

public class WoWExportUnityPostprocessor : AssetPostprocessor
{
    public override int GetPostprocessOrder()
    {
        return 9001; // must be after unitys post processor so it doesn't overwrite our own stuff
    }

    public override uint GetVersion()
    {
        return 1;
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
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.filterMode = FilterMode.Bilinear;
        textureImporter.mipmapEnabled = false;
        textureImporter.sRGBTexture = false;
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
        modelImporter.materialName = ModelImporterMaterialName.BasedOnMaterialName;
        modelImporter.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
    }

    public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] materialAnimation)
    {
        ModelImporter modelImporter = assetImporter as ModelImporter;
        M2Utility.M2 metadata = M2Utility.ReadMetadataFor(assetPath);

        if (!ValidAsset())
        {
            return;
        }

        MaterialUtility.ConfigureMaterial(description, material, assetPath);
    }

    public void OnPostprocessModel(GameObject gameObject)
    {
        if (!ValidAsset())
        {
            return;
        }

        M2Utility.QueueMetadata(assetPath);

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
        if (!ValidAsset() || material.shader.name == MaterialUtility.ADT_CHUNK_SHADER)
        {
            return null;
        }

        ModelImporter modelImporter = assetImporter as ModelImporter;
        modelImporter.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.RecursiveUp);

        return null;
    }
}
