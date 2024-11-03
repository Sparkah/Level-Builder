using UnityEngine;
using UnityEditor;

public class PrefabUpdater : EditorWindow
{
    [MenuItem("Tools/Update All Prefabs")]
    public static void UpdateAllPrefabs()
    {
        string folderPath = "Assets/Data/Prefabs/GameLevels";
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                UpdatePrefab(prefab);
            }
        }
        AssetDatabase.SaveAssets();
    }

    private static void UpdatePrefab(GameObject prefab)
    {
        var collectables = prefab.GetComponentsInChildren<CubeBase>(true);
        foreach (var collectable in collectables)
        {
            collectable.ChangeIconType(collectable.CubeIconType);
            EditorUtility.SetDirty(collectable.gameObject);
        }
    }
}