using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ReplaceWindow : EditorWindow
{
    private List<CubeTypeQuantity> _cubeTypeQuantities;
    private System.Action _onConfirmReplacement;
    private string _prefabName;

    public static void ShowWindow(string prefabName, List<CubeTypeQuantity> cubeTypeQuantities,
        System.Action onConfirmReplacement)
    {
        var window = GetWindow<ReplaceWindow>("Define New Collectable Types");
        window._prefabName = prefabName;
        window._cubeTypeQuantities = cubeTypeQuantities;
        window._onConfirmReplacement = onConfirmReplacement;
        window.Show();
    }

    private void OnEnable()
    {
        //Debug.Log("Enabling");
        //LoadCubeTypeQuantities();
    }

    private void OnDisable()
    {
        //Debug.Log("Saving");
        SaveCubeTypeQuantities();
    }

    private void OnGUI()
    {
        GUILayout.Label("Define New Collectable Types", EditorStyles.boldLabel);

        foreach (var typeQuantity in new List<CubeTypeQuantity>(_cubeTypeQuantities))
        {
            GUILayout.BeginHorizontal();
            typeQuantity.Type = (CubeIconType) EditorGUILayout.EnumPopup(typeQuantity.Type);
            typeQuantity.Quantity = EditorGUILayout.IntField(typeQuantity.Quantity);
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                _cubeTypeQuantities.Remove(typeQuantity);
            }

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Type"))
        {
            _cubeTypeQuantities.Add(new CubeTypeQuantity {Type = CubeIconType.Ground, Quantity = 1});
        }

        if (GUILayout.Button("Confirm Replacement"))
        {
            _onConfirmReplacement.Invoke();
            SaveCubeTypeQuantities();
            Close();
        }

        if (GUILayout.Button("Close"))
        {
            SaveCubeTypeQuantities();
            Close();
        }
    }

    private void SaveCubeTypeQuantities()
    {
        //To JSON
    }

    private void LoadCubeTypeQuantities()
    {
        //From JSON
    }
}