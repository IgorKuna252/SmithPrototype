using UnityEngine;

[System.Serializable]
public class SavedMeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector3[] normals;
    public Vector2[] uv;
    public Color color;

    public SavedMeshData(MeshFilter filter)
    {
        Mesh m = filter.sharedMesh;
        if (m == null) return;
        vertices  = m.vertices;
        triangles = m.triangles;
        normals   = m.normals;
        uv        = m.uv;

        MeshRenderer mr = filter.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            mr.GetPropertyBlock(block);
            color = block.GetColor("_Color");
        }
    }

    public void ApplyTo(MeshFilter filter)
    {
        if (vertices == null) return;
        Mesh mesh = new Mesh();
        mesh.vertices  = vertices;
        mesh.triangles = triangles;
        mesh.normals   = normals;
        mesh.uv        = uv;
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;

        MeshCollider mc = filter.GetComponent<MeshCollider>();
        if (mc != null) mc.sharedMesh = mesh;

        MeshRenderer mr = filter.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_Color", color);
            mr.SetPropertyBlock(block);
        }
    }

    public static SavedMeshData[] SaveFrom(GameObject weapon)
    {
        MeshFilter[] filters = weapon.GetComponentsInChildren<MeshFilter>();
        SavedMeshData[] data = new SavedMeshData[filters.Length];
        for (int i = 0; i < filters.Length; i++)
            data[i] = new SavedMeshData(filters[i]);
        return data;
    }

    public static void RestoreTo(GameObject weapon, SavedMeshData[] data)
    {
        if (data == null) return;
        MeshFilter[] filters = weapon.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < filters.Length && i < data.Length; i++)
            if (data[i] != null) data[i].ApplyTo(filters[i]);
    }
}
