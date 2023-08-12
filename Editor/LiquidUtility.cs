using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace WowUnity
{
    public class LiquidUtility
    {
        private static List<string> liquidMetadataPathQueue = new List<string>();

        public const float LIQUID_CHUNK_WIDTH = 33.3333f;
        public const float LIQUID_CHUNK_TILE_WIDTH = LIQUID_CHUNK_WIDTH / 8f;

        public const string BASIC_WATER_MATERIAL_PATH = "Packages/wow-export-unityifier.briochie/Runtime/Materials/BasicWater.mat";

        public static void PostProcessLiquidData()
        {
            Material defaultLiquidMaterial = (Material)AssetDatabase.LoadAssetAtPath(BASIC_WATER_MATERIAL_PATH, typeof(Material));
            foreach(string path in liquidMetadataPathQueue)
            {
                LiquidMetadata metadata = ReadMetadataFor(path);
                string directory = Path.GetDirectoryName(path);
                string liquidFileName = Path.GetFileNameWithoutExtension(path);
                //Regex nameMatch = new Regex("(?:[0-9]+_[0-9]+.json)");
                string adtAssetName = liquidFileName.Replace("liquid", "adt");
                string adtPrefabPath = directory + "\\" + adtAssetName + ".prefab";

                if (!File.Exists(adtPrefabPath))
                {
                    continue;
                }

                GameObject liquids;
                GameObject adtGameObject = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(adtPrefabPath)) as GameObject;
                Transform existingLiquidsContainer = adtGameObject.transform.Find("Liquids");
                if (existingLiquidsContainer != null)
                {
                    liquids = existingLiquidsContainer.gameObject;
                }
                else
                {
                    liquids = new GameObject("Liquids");
                    liquids.transform.parent = adtGameObject.transform;
                    liquids.transform.SetSiblingIndex(0);
                }

                for (int i = 0; i < metadata.liquidChunks.Count; i++)
                {
                    //Loop through the liquid data

                    if (metadata.liquidChunks[i].instances.Count <= 0)
                    {
                        continue;
                    }

                    //Create a new water mesh object for each instance.
                    for(int j = 0; j < metadata.liquidChunks[i].instances.Count; j++)
                    {
                        string currentChunkName = adtAssetName.Replace("adt_", "") + '_' + i;
                        GameObject newLiquidObject = new GameObject("Liquid Chunk [" + i + "] - " + j);
                        GameObject adtChunkGameObject = adtGameObject.transform.Find(currentChunkName).gameObject;
                        Mesh adtChunkMesh = adtChunkGameObject.GetComponent<MeshFilter>().sharedMesh;
                        Mesh newMesh = generateLiquidChunkMesh(metadata.liquidChunks[i].instances[j]);

                        AssetDatabase.CreateAsset(newMesh, directory + "\\" + liquidFileName + "_instance_" + i + ".asset");
                        
                        //Add mesh object to heirarchy
                        MeshRenderer newRenderer = newLiquidObject.AddComponent<MeshRenderer>();
                        MeshFilter newMeshFilter = newLiquidObject.AddComponent<MeshFilter>();

                        newMeshFilter.sharedMesh = newMesh;
                        newLiquidObject.transform.parent = liquids.transform;
                        newRenderer.sharedMaterial = defaultLiquidMaterial;
                    }
                }

                PrefabUtility.ApplyPrefabInstance(adtGameObject, InteractionMode.AutomatedAction);
                GameObject.DestroyImmediate(adtGameObject);
            }

            liquidMetadataPathQueue.Clear();
        }

        public static Mesh generateLiquidChunkMesh(Instance liquidInstance)
        {
            Mesh newMesh = new Mesh();
            Vector3 chunkWorldPosition = Vector3.zero; //TODO: Assign this to the real location

            //Step 1 - grab the bitmap
            //Step 2 - build a square for every space
            //Step 3 - Build the mesh
            //Step 4 - Apply the default material

            List<Vector3> newVertices = new List<Vector3>();
            List<int> newTriangles = new List<int>();

            //Go through bit array
            //Each positive bit is a quad to render
            //I will need to create all 81 verticies,
            //then I can use them in order.
            //I can then remove unused verticies
            //If the bitmap is empty, that means it's
            //one solid chunk

            //Create verticies first
            int vertexIndex = 0;
            for (int i = liquidInstance.xOffset; i <= liquidInstance.width + liquidInstance.xOffset; i++)
            {
                uint bitmapRow = liquidInstance.bitmap[i];

                for (int j = liquidInstance.yOffset; j <= liquidInstance.height + liquidInstance.yOffset; j++)
                {
                    float height = liquidInstance.maxHeightLevel;
                    if (liquidInstance.vertexData.height.Count > 0)
                    {
                        height = liquidInstance.vertexData.height[vertexIndex];
                        vertexIndex++;
                    }

                    Vector3 newVertex = new Vector3();
                    newVertex.x = i * LIQUID_CHUNK_TILE_WIDTH;
                    newVertex.y = height;
                    newVertex.z = j * LIQUID_CHUNK_TILE_WIDTH;
                    newVertices.Add(newVertex);
                }
            }

            //Now create triangles, doing two at a time.
            for (int i = 0; i < liquidInstance.width; i++)
            {
                for (int j = 0; j < liquidInstance.height; j++)
                {
                    uint bitmapRow = liquidInstance.bitmap[i];
                    bool bit = (bitmapRow & (1 << j)) != 0;

                    if (bit || (liquidInstance.width == 8 && liquidInstance.height == 8))
                    {
                        int i0 = i + j * (liquidInstance.width + 1);
                        int i1 = i0 + 1;
                        int i2 = i0 + (liquidInstance.width + 1);
                        int i3 = i2 + 1;

                        newTriangles.AddRange(new int[] { i1, i2, i0 });
                        newTriangles.AddRange(new int[] { i3, i2, i1 });

                        //int topLeftQuarter = i + (j * liquidInstance.width);
                        //newTriangles.AddRange(new int[] { topLeftQuarter, topLeftQuarter + 1, topLeftQuarter + 2 + liquidInstance.width });
                        //newTriangles.AddRange(new int[] { topLeftQuarter + 2 + liquidInstance.width, topLeftQuarter + 1 + liquidInstance.width, topLeftQuarter });
                    }
                }
            }

            newMesh.vertices = newVertices.ToArray();
            newMesh.triangles = newTriangles.ToArray();
            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();
            newMesh.RecalculateTangents();

            return newMesh;
        }

        public static void buildChunkSquare(Vector3 offset, List<Vector3> verticies, List<int> triangles)
        {
            List<int> newVerticiesIndexes = new List<int>();

            Vector3[] newVertices = new Vector3[] {
                new Vector3(LIQUID_CHUNK_TILE_WIDTH, 0, LIQUID_CHUNK_TILE_WIDTH) + offset,
                new Vector3(LIQUID_CHUNK_TILE_WIDTH, 0, 0) + offset,
                new Vector3(0, 0, LIQUID_CHUNK_TILE_WIDTH) + offset,
                new Vector3(0, 0, 0) + offset
            };

            for (int i = 0; i < newVertices.Length; i++)
            {
                if (verticies.Contains(newVertices[i]))
                {
                    newVerticiesIndexes.Add(verticies.IndexOf(newVertices[i]));
                }
                else
                {
                    newVerticiesIndexes.Add(verticies.Count);
                    verticies.Add(newVertices[i]);
                }
            }

            int[] newTriangles = new int[]
            {
                newVerticiesIndexes[3], newVerticiesIndexes[1], newVerticiesIndexes[0],
                newVerticiesIndexes[0], newVerticiesIndexes[2], newVerticiesIndexes[3]
            };

            triangles.AddRange(newTriangles);
        }

        internal static void QueueLiquidData(string path)
        {
            liquidMetadataPathQueue.Add(path);
        }

        public static LiquidMetadata ReadMetadataFor(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var sr = new StreamReader(Application.dataPath.Replace("Assets", "") + path);
            var fileContents = sr.ReadToEnd();
            sr.Close();

            return JsonConvert.DeserializeObject<LiquidMetadata>(fileContents);
        }
    }

    public class LiquidMetadata
    {
        public List<LiquidChunk> liquidChunks = new List<LiquidChunk>();
    }

    public class LiquidChunk
    {
        public Attributes attributes;
        public List<Instance> instances = new List<Instance>();
    }

    public struct Attributes
    {
        public ulong fishable;
        public ulong deep;
    }

    public struct Instance
    {
        public uint liquidType;
        public uint liquidObject;
        public float minHeightLevel;
        public float maxHeightLevel;
        public byte xOffset;
        public byte yOffset;
        public byte width;
        public byte height;
        //Always (width * height + 7) / 8 bytes
        public List<uint> bitmap;
        public VertexData vertexData;
        public uint offsetExistsBitmap;
        public uint offsetVertexData;
    }

    public class VertexData
    {
        public List<float> height = new List<float>();
        public List<short> depth = new List<short>();
    }
}
