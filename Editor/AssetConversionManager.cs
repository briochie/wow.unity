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
        }
    }
}
