using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardMaterialAdjustment : MonoBehaviour
{
    MeshFilter myMeshFilter;
    MeshRenderer myRenderer;
    Mesh myMesh;
    Material myMaterial;
    Vector3 offsetFromOrigin = Vector3.zero;

    void Awake()
    {
        myMeshFilter = GetComponent<MeshFilter>();
        myRenderer = GetComponent<MeshRenderer>();
        myMesh = myMeshFilter.mesh;
        myMaterial = myRenderer.material;

        for(int x = 0; x < myMesh.vertexCount; x++)
        {
            offsetFromOrigin += myMesh.vertices[x];
        }
        offsetFromOrigin /= myMesh.vertexCount;

        MaterialPropertyBlock newBlock = new MaterialPropertyBlock();
        myRenderer.GetPropertyBlock(newBlock);
        newBlock.SetVector("_MeshOffset", offsetFromOrigin);
        myRenderer.SetPropertyBlock(newBlock);
    }
}
