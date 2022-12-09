using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WowExportUnityifier
{
    class WMOUtility
    {
        public static bool AssignVertexColors(WMOUtility.Group group, List<GameObject> gameObjects)
        {
            if (gameObjects.Count != group.renderBatches.Count)
            {
                Debug.LogError("Attempted to assign vertex colors to WMO, but group size did not match object stack!");
                return false;
            }

            for (int i = 0; i < gameObjects.Count; i++)
            {
                GameObject gameObject = gameObjects[i];
                WMOUtility.RenderBatch renderBatch = group.renderBatches[i];
                MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.sharedMesh;
                
                if (mesh == null)
                {
                    Debug.LogError("Attempted to assign vertex colors to WMO, but mesh was missing.");
                    return false;
                }

                mesh.colors = GetVertexColorsInRange(group, renderBatch.firstVertex, renderBatch.lastVertex);
            }

            return true;
        }

        static Color[] GetVertexColorsInRange(WMOUtility.Group group, int start, int end)
        {
            List<byte[]> vertexColors = group.vertexColors.GetRange(start, end - start);
            List<Color> parsedColors = new List<Color>();

            for (int i = 0; i < vertexColors.Count; i++)
            {
                Color newColor = new Color();
                byte[] colorData = vertexColors[i];
                newColor.a = (float)colorData[0] / 255f;
                newColor.r = (float)colorData[1] / 255f;
                newColor.b = (float)colorData[2] / 255f;
                newColor.g = (float)colorData[3] / 255f;
            }

            return parsedColors.ToArray();
        }

        public class WMO
        {
            public uint fileDataID;
            public string fileName;
            public uint version;
            public byte[] ambientColor;
            public uint areaTableID;
            public BitArray flags;
            public List<Group> groups;
            public List<string> groupNames;
            public List<M2Utility.Texture> textures;
        }

        public class Group
        {
            public string groupName;
            public bool enabled;
            public uint version;
            public uint groupID;
            public List<RenderBatch> renderBatches;
            public List<byte[]> vertexColors;
        }

        public class RenderBatch
        {
            public ushort firstVertex;
            public ushort lastVertex;
            public BitArray flags;
            public uint materialID;
        }
    }
}
