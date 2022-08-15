using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WowExportUnityifier
{
    public class LiquidUtility
    {
        private static List<string> liquidMetadataPathQueue = new List<string>();

        public static void PostProcessFluidVolumes()
        {
            foreach(string path in liquidMetadataPathQueue)
            {
                LiquidMetadata metadata = ReadMetadataFor(path);

                //Parse out the name of the ADT
                //REGEX MAGIC HERE

                //Find the ADT prefab, or create one.
                //GameObject adtPrefab = M2Utility.FindOrCreatePrefab();

                //Loop through the liquid data
                    //Create a mesh based on params
                    //Based on defined offsets.
                    //Apply y = height
                
                //Add mesh object to heirarchy

                //Assign material??
            }

            liquidMetadataPathQueue.Clear();
        }

        internal static void QueueLiquidData(string path)
        {
            liquidMetadataPathQueue.Add(path);
        }

        public static LiquidMetadata ReadMetadataFor(string path)
        {
            string pathToMetadata = Path.GetDirectoryName(path) + "/" + path;

            if (!File.Exists(pathToMetadata))
            {
                return null;
            }

            var sr = new StreamReader(Application.dataPath.Replace("Assets", "") + pathToMetadata);
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
        public VertexData vertexData;
    }

    public class VertexData
    {
        public List<float> height = new List<float>();
        public List<short> depth = new List<short>();
    }
}
