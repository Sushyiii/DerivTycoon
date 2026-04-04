using UnityEngine;
using UnityEditor;

public class BuildSmallBuilding
{
    public static void Execute()
    {
        // Create Folders
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Create Materials
        Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wallMat.color = new Color(0.85f, 0.8f, 0.75f); // Off-white/Beige
        AssetDatabase.CreateAsset(wallMat, "Assets/Materials/WallMat.mat");

        Material roofMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        roofMat.color = new Color(0.2f, 0.2f, 0.25f); // Dark Grey
        AssetDatabase.CreateAsset(roofMat, "Assets/Materials/RoofMat.mat");

        Material doorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        doorMat.color = new Color(0.4f, 0.25f, 0.15f); // Brown
        AssetDatabase.CreateAsset(doorMat, "Assets/Materials/DoorMat.mat");

        Material windowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        windowMat.color = new Color(0.6f, 0.85f, 0.9f); // Light blue
        windowMat.SetFloat("_Smoothness", 0.8f);
        AssetDatabase.CreateAsset(windowMat, "Assets/Materials/WindowMat.mat");

        // Create Hierarchy
        GameObject building = new GameObject("SmallBuilding");

        // Base
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Base";
        baseObj.transform.SetParent(building.transform);
        baseObj.transform.localPosition = new Vector3(0, 1f, 0);
        baseObj.transform.localScale = new Vector3(3f, 2f, 2f);
        baseObj.GetComponent<Renderer>().sharedMaterial = wallMat;

        // Roof
        GameObject roofObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofObj.name = "Roof";
        roofObj.transform.SetParent(building.transform);
        roofObj.transform.localPosition = new Vector3(0, 2.1f, 0);
        roofObj.transform.localScale = new Vector3(3.2f, 0.2f, 2.2f);
        roofObj.GetComponent<Renderer>().sharedMaterial = roofMat;

        // Door
        GameObject doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorObj.name = "Door";
        doorObj.transform.SetParent(building.transform);
        doorObj.transform.localPosition = new Vector3(0, 0.6f, 1.01f);
        doorObj.transform.localScale = new Vector3(0.8f, 1.2f, 0.1f);
        doorObj.GetComponent<Renderer>().sharedMaterial = doorMat;

        // Window 1
        GameObject window1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window1.name = "Window1";
        window1.transform.SetParent(building.transform);
        window1.transform.localPosition = new Vector3(-0.9f, 1.2f, 1.01f);
        window1.transform.localScale = new Vector3(0.6f, 0.6f, 0.1f);
        window1.GetComponent<Renderer>().sharedMaterial = windowMat;

        // Window 2
        GameObject window2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window2.name = "Window2";
        window2.transform.SetParent(building.transform);
        window2.transform.localPosition = new Vector3(0.9f, 1.2f, 1.01f);
        window2.transform.localScale = new Vector3(0.6f, 0.6f, 0.1f);
        window2.GetComponent<Renderer>().sharedMaterial = windowMat;

        // Save Prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(building, "Assets/Prefabs/SmallBuilding.prefab", InteractionMode.UserAction);
        
        Selection.activeGameObject = building;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }
}