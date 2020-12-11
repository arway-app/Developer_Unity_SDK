#if UNITY_EDITOR

using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using System;
using System.IO;
using UnityEditor;
using System.Text.RegularExpressions;

[ScriptedImporter(1, "pcd")]
public class PCDImporter : ScriptedImporter
{



    private GameObject m_point;

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
        // Load Point gameobject
        m_point = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PCD Viewer/Prefabs/Point.prefab");

        // Create Point Cloud gamemobject
        m_pointCloud = new GameObject(assetName);

        // Get file name
        string fileName = dirName + assetName + ".txt";
        // Debug.Log(fileName);

        // Get total number of points in the file
        string points = GetLine(fileName, 10);
        points = Regex.Replace(points, "[^0-9]+", string.Empty);
        int totalPoints = Convert.ToInt32(points);
        // Debug.Log(totalPoints);

        for (int i = 12; i < totalPoints; i++)
        {
            Vector3 xyz = GetXYZValue(fileName, i);

            var newPoint = Instantiate(m_point, xyz, Quaternion.identity);
            newPoint.transform.parent = m_pointCloud.transform;
            // Debug.Log("Instantiated " + i);
        }

        // Save created point cloud file as a prefab
        PrefabUtility.SaveAsPrefabAsset(m_pointCloud, dirName + assetName + ".prefab");
        Debug.Log("Point Cloud created as " + assetName + ".prefab");

        // Delete the converted .txt file 
        File.Delete(dirName + assetName + ".txt");

        // Delete point cloud from scene
        DestroyImmediate(m_pointCloud);
    }

    // Gets a single line from the file
    public string GetLine(string fileName, int line)
    {
        using (var sr = new StreamReader(fileName))
        {
            for (int i = 1; i < line; i++)
                sr.ReadLine();
            return sr.ReadLine();
        }
    }

    // Returns Vector3 coordinates of a given line
    public Vector3 GetXYZValue(string fileName, int line)
    {
        string data = GetLine(fileName, line);

        char[] seperators = { ',', ' ' };

        String[] strlist = data.Split(seperators, StringSplitOptions.None);

        float x_val = float.Parse(strlist[0]);
        float y_val = float.Parse(strlist[1]);
        float z_val = float.Parse(strlist[2]);

        Vector3 xyz = new Vector3(x_val, y_val, z_val);
        // Debug.Log(xyz.ToString("F5"));

        return xyz;
    }


}
#endif