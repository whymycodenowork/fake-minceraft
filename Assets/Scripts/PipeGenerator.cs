using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PipeGenerator : MonoBehaviour
{
    [Tooltip("Each 2 bits represent a direction: 00=none, 01=input, 11=output")]
    public ushort directionFlags;
    [Range(0.01f, 0.5f)]
    public float thickness = 0.2f;
    public Material pipeMaterial;

    private MeshFilter meshFilter;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        GeneratePipe();
    }

    void OnValidate()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        GeneratePipe();
    }

    void GeneratePipe()
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();

        float half = 0.5f;
        float t = Mathf.Clamp01(thickness) * half;

        // Add center cube
        AddCube(verts, tris, uvs, Vector3.zero, new Vector3(t, t, t), false);

        // Directions in order: +X, -X, +Y, -Y, +Z, -Z
        Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        for (int i = 0; i < 6; i++)
        {
            int bitOffset = i * 2;
            int flag = (directionFlags >> bitOffset) & 0b11;
            if (flag == 0) continue;
            bool isOutput = (flag == 3);
            // Arm center offset
            Vector3 dir = dirs[i];
            Vector3 armPos = dir * (half - t);
            // Arm scale: thickness for two dims, full length for one
            Vector3 armScale = 2 * t * Vector3.one;
            // Extend along direction: size from center half to edge
            if (dir.x != 0) armScale.x = half;
            if (dir.y != 0) armScale.y = half;
            if (dir.z != 0) armScale.z = half;

            AddCube(verts, tris, uvs, armPos, armScale * 0.5f, isOutput);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        meshFilter.sharedMesh = mesh;
        if (pipeMaterial != null)
            GetComponent<MeshRenderer>().sharedMaterial = pipeMaterial;
    }

    // Adds an axis-aligned cube at center 'offset', with half-sizes 'halfScale'.
    // If reverseUv is true, flips the U coordinate for output arms.
    void AddCube(List<Vector3> verts, List<int> tris, List<Vector2> uvs, Vector3 offset, Vector3 halfScale, bool reverseUv)
    {
        int start = verts.Count;
        // 8 corners
        verts.Add(offset + new Vector3(-halfScale.x, -halfScale.y, -halfScale.z));
        verts.Add(offset + new Vector3(halfScale.x, -halfScale.y, -halfScale.z));
        verts.Add(offset + new Vector3(halfScale.x, halfScale.y, -halfScale.z));
        verts.Add(offset + new Vector3(-halfScale.x, halfScale.y, -halfScale.z));
        verts.Add(offset + new Vector3(-halfScale.x, -halfScale.y, halfScale.z));
        verts.Add(offset + new Vector3(halfScale.x, -halfScale.y, halfScale.z));
        verts.Add(offset + new Vector3(halfScale.x, halfScale.y, halfScale.z));
        verts.Add(offset + new Vector3(-halfScale.x, halfScale.y, halfScale.z));

        // 6 faces
        int[][] faceTris = new int[][] {
            new[]{0,1,2,3}, // back
            new[]{5,4,7,6}, // front
            new[]{4,0,3,7}, // left
            new[]{1,5,6,2}, // right
            new[]{3,2,6,7}, // top
            new[]{4,5,1,0}  // bottom
        };

        Vector2[] faceUv = new Vector2[] {
            new(0,0), new(1,0), new(1,1), new(0,1)
        };

        foreach (var f in faceTris)
        {
            // two tris
            tris.Add(start + f[0]); tris.Add(start + f[1]); tris.Add(start + f[2]);
            tris.Add(start + f[0]); tris.Add(start + f[2]); tris.Add(start + f[3]);
            // UVs
            if (reverseUv)
            {
                uvs.Add(faceUv[1]); uvs.Add(faceUv[0]); uvs.Add(faceUv[3]); uvs.Add(faceUv[2]);
            }
            else
            {
                uvs.Add(faceUv[0]); uvs.Add(faceUv[1]); uvs.Add(faceUv[2]); uvs.Add(faceUv[3]);
            }
        }
    }
}
