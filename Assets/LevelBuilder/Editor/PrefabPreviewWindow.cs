using UnityEditor;
using UnityEngine;

public class PrefabPreviewWindow : EditorWindow
{
    private GameObject _previewPrefab;
    private GameObject _previewInstance;
    private const float ScreenWidth = 1284f;
    private const float ScreenHeight = 2778f;
    private Vector3 _initialCentroid;

    [MenuItem("Tools/Prefab Preview")]
    private static void ShowWindow()
    {
        var window = GetWindow<PrefabPreviewWindow>();
        window.titleContent = new GUIContent("Prefab Preview");
        window.Show();
    }

    private void OnGUI()
    {
        _previewPrefab = (GameObject) EditorGUILayout.ObjectField("Prefab", _previewPrefab, typeof(GameObject), false);

        if (GUILayout.Button("Update Preview"))
        {
            UpdatePreview();
        }

        if (GUILayout.Button("Clear Preview"))
        {
            ClearPreview();
        }
    }

    private void UpdatePreview()
    {
        ClearPreview();

        if (_previewPrefab != null)
        {
            var centralPivot = new GameObject("CentralPivot");
            _previewInstance = (GameObject) PrefabUtility.InstantiatePrefab(_previewPrefab);
            _previewInstance.transform.SetParent(centralPivot.transform);

            var gameLevel = _previewInstance.GetComponent<GameLevel>();
            /*if (gameLevel != null)
            {
                var bounds = gameLevel.GetBounds();
                CalculateInitialCentroid(bounds.center);

                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    var cameraPosition = bounds.center -
                                         sceneView.camera.transform.forward * _levelSettingsConfig.DistanceFromCamera;

                    var cameraRotation = Quaternion.LookRotation(bounds.center - cameraPosition);

                    sceneView.LookAtDirect(cameraPosition, cameraRotation);
                    gameLevel.transform.position += _initialCentroid - new Vector3(0, 0, _levelSettingsConfig.DistanceFromCamera);
                    
                    centralPivot.transform.position = -bounds.center;

                    sceneView.Repaint();
                }
            }*/
        }
    }

    private void CalculateInitialCentroid(Vector3 average)
    {
        _initialCentroid = average;
    }

    private void ClearPreview()
    {
        if (_previewInstance != null)
        {
            DestroyImmediate(_previewInstance.transform.parent
                .gameObject);
        }
    }


    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        ClearPreview();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Handles.BeginGUI();

        var sceneViewSize = sceneView.camera.pixelRect;
        var gameViewRect = new Rect(
            (sceneViewSize.width - ScreenWidth) / 2,
            (sceneViewSize.height - ScreenHeight) / 2,
            ScreenWidth,
            ScreenHeight
        );

        Handles.EndGUI();
    }
}