using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class CubeCreator : EditorWindow
    {
        private int _cubeTypeIndex = 0;
        private CubeIconType _selectedIconType = CubeIconType.Ground;
        private GameLevel _gameLevel;
        private Vector3 _spawnPosition;
        private Vector3 _greenSpherePosition;
        private Vector3 _originalCubePosition;

        private void OnDestroy()
        {
            CubeEditorTool.OnCubeCreatorClosed?.Invoke();
        }

        public void SetPositionData(Vector3 greenSpherePosition, Vector3 originalCubePosition)
        {
            _greenSpherePosition = greenSpherePosition;
            _originalCubePosition = originalCubePosition;
        }

        public void SetGameLevel(GameLevel gameLevel)
        {
            _gameLevel = gameLevel;
        }

        private void OnGUI()
        {
            Event e = Event.current;

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.C)
                {
                    _cubeTypeIndex = 1;
                    CreateCube();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.M)
                {
                    _cubeTypeIndex = 0;
                    CreateCube();
                    e.Use();
                }
            }

            GUILayout.Label("Cube Settings", EditorStyles.boldLabel);

            if (_cubeTypeIndex == 1)
            {
                _selectedIconType = (CubeIconType) EditorGUILayout.EnumPopup("Icon Type", _selectedIconType);
            }

            if (GUILayout.Button("Create Cube"))
            {
                CreateCube();
            }

            if (GUILayout.Button("Close"))
            {
                CubeEditorTool.OnCubeCreatorClosed?.Invoke();
                Close();
            }
        }

        private void CreateCube()
        {
            Vector3 direction = (_greenSpherePosition - _originalCubePosition).normalized;
            Vector3
                spawnPosition =
                    _greenSpherePosition + direction * 0.5f;

            GameObject prefab = _gameLevel.CubeBase;
            if (prefab == null)
            {
                Debug.LogError("The prefab to instantiate is null.");
                return;
            }

            GameObject createdCube;

            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                createdCube =
                    (GameObject) PrefabUtility.InstantiatePrefab(prefab, prefabStage.prefabContentsRoot.transform);
            }
            else
            {
                createdCube = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
            }

            if (createdCube != null)
            {
                Vector3 directionToGreenSphere = (_originalCubePosition - _greenSpherePosition).normalized;
                _spawnPosition = directionToGreenSphere * 0.5f;

                _spawnPosition = spawnPosition;
                createdCube.transform.position = _spawnPosition;

                CubeBase cubeBaseComponent = createdCube.GetComponent<CubeBase>();
                if (cubeBaseComponent != null)
                {
                    cubeBaseComponent.SetGameLevel(_gameLevel);
                }
                else
                {
                    Debug.LogError("Newly created cube does not have a CubeBase component.");
                }

                if (createdCube.TryGetComponent(out CubeBase cubeCollectable))
                {
                    cubeCollectable.ChangeIconType(_selectedIconType);
                }

                Undo.RegisterCreatedObjectUndo(createdCube, "Create Cube");
                Selection.activeGameObject = createdCube;
            }
            else
            {
                Debug.LogError("Failed to instantiate cube prefab.");
            }
            
            CubeEditorTool.OnCubeCreatorClosed?.Invoke();
            Close();
        }
    }
}