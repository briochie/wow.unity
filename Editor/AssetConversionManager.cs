using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WowUnity
{
    class AssetConversionManager
    {
        public static void ProcessAssets()
        {
            EditorApplication.update -= ProcessAssets;

            M2Utility.PostProcessImports();
            ItemCollectionUtility.BeginQueue();
            LiquidUtility.PostProcessLiquidData();
        }

        public static Mesh duplicateMesh(Mesh originalMesh)
        {
            Mesh newMesh = new Mesh();

            newMesh.vertices = originalMesh.vertices;
            newMesh.triangles = originalMesh.triangles;
            newMesh.uv = originalMesh.uv;
            newMesh.normals = originalMesh.normals;
            newMesh.colors = originalMesh.colors;
            newMesh.tangents = originalMesh.tangents;

            return newMesh;
        }
    }
}
