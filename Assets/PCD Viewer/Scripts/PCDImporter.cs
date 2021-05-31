#if UNITY_EDITOR

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

#if UNITY_2020_1_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
# endif


[ScriptedImporter(1, "pcd")]
public class PCDImporter : ScriptedImporter
{
    private GameObject m_pointCloud;

    // Send asset path to CreatePCD() function
    public override void OnImportAsset(AssetImportContext ctx)
    {
        CreatePCD(ctx.assetPath);
    }

    // Create Point Cloud from the given PCD file
    public void CreatePCD(string assetPath)
    {
        // Get Directory Path
        string dirName = Path.GetDirectoryName(assetPath) + "/";

        // Get File Name
        string assetName = Path.GetFileNameWithoutExtension(assetPath);

        // Convert .pcd to .txt file
        File.Delete(dirName + assetName + ".txt");
        File.Copy(dirName + assetName + ".pcd", dirName + assetName + ".txt");

        // Instantiate Points
        InstantiatePoints(assetName, dirName);
    }

    // Instantiates points under Point Cloud gameobject by getting the Vector3 values line by line
    public void InstantiatePoints(string assetName, string dirName)
    {
        // Create Point Cloud gamemobject
        m_pointCloud = new GameObject(assetName);

        // Get file name
        string fileName = dirName + assetName + ".txt";

        // Get total number of points in the file
        string[] lines = System.IO.File.ReadAllLines(fileName);
        string points = lines[9];

        points = Regex.Replace(points, "[^0-9]+", string.Empty);
        int totalPoints = Convert.ToInt32(points);

        if (totalPoints > lines.Length)
        {
            totalPoints = lines.Length - 1;
        }

        // Add Mesh, Mesh Renderer & Mesh Filter
        MeshRenderer meshRenderer = m_pointCloud.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/PCD Viewer/Materials/Point Mat.mat");

        MeshFilter meshFilter = m_pointCloud.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh = mesh;

        Vector3[] pointsList = new Vector3[totalPoints - 12];
        int[] indices = new int[totalPoints - 12];

        // Instantiate points
        for (int i = 12; i < totalPoints; i++)
        {
            pointsList[i - 12] = GetXYZValue(lines[i - 1]);

            indices[i - 12] = i - 12;
        }

        // Add Points to the Mesh
        mesh.Clear();
        mesh.vertices = pointsList;
        mesh.SetIndices(indices, MeshTopology.Points, 0);

        // Save created point cloud file as a prefab
        // PrefabUtility.SaveAsPrefabAsset(m_pointCloud, dirName + assetName + ".prefab");

        // Delete the converted .txt file 
        File.Delete(dirName + assetName + ".txt");
    }

    // Returns Vector3 coordinates of a given line
    public Vector3 GetXYZValue(string data)
    {
        char[] seperators = { ',', ' ' };

        String[] strlist = data.Split(seperators, StringSplitOptions.None);

        float x_val = float.Parse(strlist[0]);
        float y_val = float.Parse(strlist[1]);
        float z_val = float.Parse(strlist[2]);

        Vector3 xyz = new Vector3(x_val, y_val, z_val);

        return xyz;
    }
}
#endif