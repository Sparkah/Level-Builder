using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;

namespace Editor
{
    [CustomEditor(typeof(GameLevel))]
    public class LevelCubeCreatorSceneGUI : UnityEditor.Editor
    {
        private int cubeTypeIndex = 0;
        //private string[] cubeTypeOptions = {"Movable", "Collectable"};
        private int numberOfCubes = 1;
        private Vector3 position = Vector3.zero;
        private Vector3 rotation = Vector3.zero;
        private CubeIconType selectedIconType = CubeIconType.Ground;

        private bool generateOnXPlane = true;
        private bool generateOnYPlane = true;
        private bool generateOnZPlane = true;

        private List<CubeTypeQuantity> cubeTypeQuantities = new List<CubeTypeQuantity>();
        private bool showReplaceWindow = false;
        private GameLevel _gameLevel;

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(50, 10, 300, cubeTypeIndex == 1 ? 310 : 250), GUI.skin.box);
            GUILayout.Label("Cube Creation", EditorStyles.boldLabel);
            //cubeTypeIndex = GUILayout.SelectionGrid(cubeTypeIndex, cubeTypeOptions, 2);
            numberOfCubes = EditorGUILayout.IntSlider("Number of Cubes", numberOfCubes, 1, 270);
            position = EditorGUILayout.Vector3Field("Position", position);
            rotation = EditorGUILayout.Vector3Field("Rotation", rotation);

            GUILayout.Label("Generate on Planes", EditorStyles.boldLabel);
            generateOnXPlane = EditorGUILayout.Toggle("X Plane", generateOnXPlane);
            generateOnYPlane = EditorGUILayout.Toggle("Y Plane", generateOnYPlane);
            generateOnZPlane = EditorGUILayout.Toggle("Z Plane", generateOnZPlane);

            if (cubeTypeIndex == 1) // Collectable
            {
                selectedIconType = (CubeIconType) EditorGUILayout.EnumPopup("Icon Type", selectedIconType);
            }

            if (GUILayout.Button("Create Cubes"))
            {
                CreateCubes();
            }

            GUILayout.EndArea();

            GameLevel gameLevel = (GameLevel) target;
            _gameLevel = gameLevel;

            var allCubes = gameLevel.GetAllLevelCubesEDITOR();
            var collectableCubes =
                allCubes.Where(c => c is CubeBase).Cast<CubeBase>(); //gameLevel.GatherCollectableCubes();

            GUILayout.BeginArea(new Rect(760, 10, 300, 100 + 20 * System.Enum.GetNames(typeof(CubeIconType)).Length),
                GUI.skin.box);
            GUILayout.Label("Cube Counts", EditorStyles.boldLabel);

            GUIStyle collectableStyle = new GUIStyle(GUI.skin.label);
            int totalCollectableCount = collectableCubes.Count();
            collectableStyle.normal.textColor = (totalCollectableCount != 0 && totalCollectableCount % 3 != 0)
                ? Color.red
                : Color.green;
            GUILayout.Label($"Total Cubes: {allCubes.Count()}");
            GUILayout.Label($"Movable Cubes: {allCubes.Count(c => c is CubeBase)}");
            GUILayout.Label($"Collectable Cubes: {totalCollectableCount}", collectableStyle);

            GUILayout.Label("Collectable Cube Types:");
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.BeginVertical();
            foreach (CubeIconType iconType in System.Enum.GetValues(typeof(CubeIconType)))
            {
                int count = collectableCubes.Count(cube => cube.CubeIconType == iconType);

                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = (count != 0 && count % 3 != 0) ? Color.red : Color.green;

                GUILayout.Label($"{iconType}: {count}", style);
            }

            if (GUILayout.Button("Validate Movable Cubes"))
            {
                ValidateMovableCubes();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(50, 260, 300, 40), GUI.skin.box);


            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(50, 300, 300, 40), GUI.skin.box);
            if (GUILayout.Button("Reset Collectables Rotation"))
            {
                ResetCollectablesRotation(collectableCubes);
            }

            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private void ResetCollectablesRotation(IEnumerable<CubeBase> collectableCubes)
        {
            foreach (var cube in collectableCubes)
            {
                cube.transform.rotation = Quaternion.Euler(0, 0, 0);
                EditorUtility.SetDirty(cube);
            }

            Debug.Log("Collectable cubes' rotations reset to (0,0,0).");
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement
                    .EditorSceneManager.GetActiveScene());
            }
        }


        private bool MoveCube(CubeBase cube, HashSet<CubeBase> movedCubes)
        {
            if (cube == null)
            {
                Debug.LogWarning("MoveCube called with null cube.");
                return false;
            }

            if (movedCubes == null)
            {
                Debug.LogError("MoveCube called with null movedCubes.");
                return false;
            }

            if (movedCubes.Contains(cube))
            {
                return false;
            }

            var cubes = GatherAllCubes();
            CubeBase blockage = cube.CheckForVectorBlockage(120, cubes, movedCubes);

            if (blockage != null)
            {
                CubeBase blockingCube = blockage as CubeBase;
                if (blockingCube == null)
                {
                    movedCubes.Add(cube);
                    return MoveCube(blockingCube, movedCubes);
                }
                else
                {
                    return false;
                }
            }

            movedCubes.Add(cube);
            return true;
        }

        private CubeBase[] GatherAllCubes()
        {
            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                GameObject root = prefabStage.prefabContentsRoot;

                CubeBase[] cubes = root.GetComponentsInChildren<CubeBase>();

                return cubes;
            }

            return null;
        }


        private bool SimulateCubeMovements(List<CubeBase> cubes, HashSet<CubeBase> movedCubes,
            int attemptsRemaining)
        {
            if (cubes.Count == 0)
                return true;

            foreach (var cube in cubes)
            {
                if (!movedCubes.Contains(cube) && MoveCube(cube, movedCubes))
                {
                    movedCubes.Add(cube);
                    var remainingCubes = cubes.Where(c => !movedCubes.Contains(c)).ToList();
                    if (remainingCubes.Count == 0 ||
                        SimulateCubeMovements(remainingCubes, movedCubes, attemptsRemaining))
                        return true;
                }
            }

            if (attemptsRemaining > 0)
            {
                return SimulateCubeMovements(cubes, movedCubes, attemptsRemaining - 1);
            }

            return false;
        }

        private void ValidateMovableCubes()
        {
            if (target == null)
            {
                Debug.LogError("ValidateMovableCubes: 'target' is null.");
                return;
            }

            var gameLevel = target as GameLevel;
            if (gameLevel == null)
            {
                Debug.LogError("ValidateMovableCubes: 'target' is not of type GameLevel.");
                return;
            }

            var allCubes = gameLevel.GetAllLevelCubesEDITOR();
            var movableCubes = allCubes.Where(c => c is CubeBase).Cast<CubeBase>().ToList();

            if (movableCubes == null)
            {
                Debug.LogError("ValidateMovableCubes: 'movableCubes' is null after gathering.");
                return;
            }

            const int maxAttempts = 50;

            if (SimulateCubeMovements(movableCubes, new HashSet<CubeBase>(), maxAttempts))
            {
                Debug.Log("The level can be solved.");
            }
            else
            {
                Debug.Log("The level cannot be solved.");
            }
        }


        private void CreateCubes()
        {
            var gameLevel = (GameLevel) target;
            GameObject prefab = null;

            switch (cubeTypeIndex)
            {
                case 0: // Movable
                    prefab = gameLevel.CubeBase.gameObject;
                    break;
                case 1: // Collectable
                    prefab = gameLevel.CubeBase.gameObject;
                    break;
            }

            if (prefab != null)
            {
                int planesSelected = (generateOnXPlane ? 1 : 0) + (generateOnYPlane ? 1 : 0) +
                                     (generateOnZPlane ? 1 : 0);

                planesSelected = Mathf.Max(1, planesSelected);
                int cubesPerDimension = Mathf.CeilToInt(Mathf.Pow(numberOfCubes, 1f / planesSelected));

                float offset = (cubesPerDimension - 1) * 0.5f;

                for (int x = 0; x < (generateOnXPlane ? cubesPerDimension : 1); x++)
                {
                    for (int y = 0; y < (generateOnYPlane ? cubesPerDimension : 1); y++)
                    {
                        for (int z = 0; z < (generateOnZPlane ? cubesPerDimension : 1); z++)
                        {
                            if (x * (generateOnYPlane ? cubesPerDimension : 1) *
                                (generateOnZPlane ? cubesPerDimension : 1) +
                                y * (generateOnZPlane ? cubesPerDimension : 1) + z >= numberOfCubes)
                                break;

                            Vector3 instancePosition = new Vector3(
                                generateOnXPlane ? x * 1f - offset : 0,
                                generateOnYPlane ? y * 1f - offset : 0,
                                generateOnZPlane ? z * 1f - offset : 0
                            ) + position;

                            Quaternion instanceRotation = Quaternion.Euler(rotation);

                            var instance = (GameObject) PrefabUtility.InstantiatePrefab(prefab, gameLevel.transform);
                            instance.transform.position = instancePosition;
                            instance.transform.rotation = instanceRotation;

                            Undo.RegisterCreatedObjectUndo(instance, "Create Cube Instance");
                        }
                    }
                }
            }
        }
    }
}