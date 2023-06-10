using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WowUnity
{
    public class ItemCollectionUtility
    {
        public static readonly char CSV_LINE_SEPERATOR = '\n';
        public static readonly char CSV_COLUMN_SEPERATOR = ';';

        public static readonly float MAXIMUM_DISTANCE_FROM_ORIGIN = 51200f / 3f;
        public static readonly float MAP_SIZE = MAXIMUM_DISTANCE_FROM_ORIGIN * 2f;
        public static readonly float ADT_SIZE = MAP_SIZE / 64f;

        private static List<string> queuedPlacementInformationPaths = new List<string>();
        private static List<string> missingFilesInQueue = new List<string>();

        public static bool isADT(TextAsset modelPlacementInformation)
        {
            return Regex.IsMatch(modelPlacementInformation.name, @"adt_\d{2}_\d{2}");
        }

        public static void GenerateADT(GameObject prefab, TextAsset modelPlacementInformation)
        {
            GameObject instantiatedGameObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instantiatedGameObject.isStatic = true;
            foreach (Transform childTransform in instantiatedGameObject.transform)
            {
                childTransform.gameObject.isStatic = true;
            }

            string path = AssetDatabase.GetAssetPath(prefab);

            ParseFileAndSpawnDoodads(instantiatedGameObject, modelPlacementInformation);

            string parentPath = AssetDatabase.GetAssetPath(prefab);

            if (Path.GetExtension(parentPath) == ".prefab")
            {
                PrefabUtility.ApplyPrefabInstance(instantiatedGameObject, InteractionMode.AutomatedAction);
                PrefabUtility.SavePrefabAsset(prefab);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(instantiatedGameObject, parentPath.Replace(Path.GetExtension(parentPath), ".prefab"));
            }

            Object.DestroyImmediate(instantiatedGameObject);
        }

        private static void ParseFileAndSpawnDoodads(GameObject instantiatedPrefabGObj, TextAsset modelPlacementInformation)
        {
            string[] records = modelPlacementInformation.text.Split(CSV_LINE_SEPERATOR);
            foreach (string record in records.Skip(1))
            {
                string[] fields = record.Split(CSV_COLUMN_SEPERATOR);
                string doodadPath = Path.GetDirectoryName(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instantiatedPrefabGObj)) + Path.DirectorySeparatorChar + fields[0];
                doodadPath = Path.GetFullPath(doodadPath);
                doodadPath = $"Assets{Path.DirectorySeparatorChar}" + doodadPath.Substring(Application.dataPath.Length + 1); //This is so nifty :3

                Vector3 doodadPosition = Vector3.zero;
                Quaternion doodadRotation = Quaternion.identity;
                float doodadScale = float.Parse(fields[8], CultureInfo.InvariantCulture);

                if (isADT(modelPlacementInformation))
                {

                    doodadPosition.x = MAXIMUM_DISTANCE_FROM_ORIGIN - float.Parse(fields[1], CultureInfo.InvariantCulture);
                    doodadPosition.z = (MAXIMUM_DISTANCE_FROM_ORIGIN - float.Parse(fields[3], CultureInfo.InvariantCulture)) * -1f;
                    doodadPosition.y = float.Parse(fields[2], CultureInfo.InvariantCulture);

                    Vector3 eulerRotation = Vector3.zero;
                    eulerRotation.x = float.Parse(fields[6], CultureInfo.InvariantCulture);
                    eulerRotation.y = float.Parse(fields[5], CultureInfo.InvariantCulture) * -1 - 90;
                    eulerRotation.z = float.Parse(fields[4], CultureInfo.InvariantCulture);

                    doodadRotation.eulerAngles = eulerRotation;
                }
                else
                {
                    doodadPosition = new Vector3(
                        float.Parse(fields[1], CultureInfo.InvariantCulture), 
                        float.Parse(fields[3], CultureInfo.InvariantCulture), 
                        float.Parse(fields[2], CultureInfo.InvariantCulture)
                    );
                    doodadRotation = new Quaternion(
                        float.Parse(fields[5], CultureInfo.InvariantCulture) * -1, 
                        float.Parse(fields[7], CultureInfo.InvariantCulture), 
                        float.Parse(fields[6], CultureInfo.InvariantCulture) * -1, 
                        float.Parse(fields[4], CultureInfo.InvariantCulture) * -1
                    );
                }

                SpawnDoodad(doodadPath, doodadPosition, doodadRotation, doodadScale, instantiatedPrefabGObj.transform);
            }
        }

        private static void SpawnDoodad(string path, Vector3 position, Quaternion rotation, float scaleFactor, Transform parent)
        {
            GameObject exisitingPrefab = M2Utility.FindOrCreatePrefab(path);

            if (exisitingPrefab == null)
            {
                Debug.LogWarning("Object was not spawned because it could not be found: " + path);
                return;
            }

            GameObject newDoodadInstance = PrefabUtility.InstantiatePrefab(exisitingPrefab, parent) as GameObject;

            newDoodadInstance.transform.localPosition = position;
            newDoodadInstance.transform.localRotation = rotation;
            newDoodadInstance.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }

        public static void QueuePlacementData(string filePath)
        {
            queuedPlacementInformationPaths.Add(filePath);
        }

        public static void BeginQueue()
        {
            if (queuedPlacementInformationPaths.Count == 0)
            {
                return;
            }

            List<string> iteratingList = new List<string>(queuedPlacementInformationPaths);

            foreach (string path in iteratingList)
            {
                TextAsset placementData = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                string prefabPath = Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileName(path).Replace("_ModelPlacementInformation.csv", ".obj");
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                GenerateADT(prefab, placementData);

                queuedPlacementInformationPaths.Remove(path);
            }

            foreach (string missingFilePath in missingFilesInQueue)
            {
                Debug.Log("Warning, import could not be found: " + missingFilePath);
            }
        }
    }
}
