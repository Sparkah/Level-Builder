using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(CubeBase), true)]
    [CanEditMultipleObjects]
    public class CubeEditorTool : UnityEditor.Editor
    {
        public static Action OnCubeCreatorClosed;

        private int _hoverState = -1;
        private int _selectedState = -1;
        private Tool _lastUsedTool = Tool.None;
        private static bool _showRedIndicators = true;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            CubeBase.OnCubeInstantiated += HandleCubeInstantiated;
            _lastUsedTool = Tools.current;
            Tools.current = Tool.None;
            Selection.selectionChanged += OnSelectionChanged;

            OnCubeCreatorClosed += ResetSelectionState;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            CubeBase.OnCubeInstantiated -= HandleCubeInstantiated;
            Tools.current = _lastUsedTool;
            Selection.selectionChanged -= OnSelectionChanged;

            OnCubeCreatorClosed -= ResetSelectionState;
        }

        private void ResetSelectionState()
        {
            _selectedState = -1;
            SceneView.RepaintAll();
        }

        private void HandleCubeInstantiated(GameObject newCube)
        {
            if (newCube != null)
            {
                var newTarget = newCube.GetComponent<CubeBase>();
                if (newTarget != null)
                {
                    Selection.activeObject = newCube;
                    Repaint();
                }
            }
        }

        private void OnSelectionChanged()
        {
            //var selectedCube = Selection.activeGameObject?.GetComponent<CubeBase>();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var cube = target as CubeBase;
            if (cube == null) return;

            if (GUI.changed)
            {
                EditorUtility.SetDirty(cube);
            }

            if (serializedObject.targetObject == null || serializedObject.targetObject.Equals(null))
            {
                EditorGUILayout.HelpBox("Object has been destroyed or is not available.", MessageType.Info);
                return;
            }

            ToggleShowGizmos();
        }

        private void ToggleShowGizmos()
        {
            EditorGUI.BeginChangeCheck();
            _showRedIndicators = EditorGUILayout.Toggle("Show Red Indicators", _showRedIndicators);
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        private void DeleteSelectedCube()
        {
            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<CubeBase>() != null)
            {
                Undo.DestroyObjectImmediate(Selection.activeGameObject);
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Backspace)
            {
                DeleteSelectedCube();
                Event.current.Use();
            }

            if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint ||
                Event.current.type == EventType.ContextClick || Event.current.type == EventType.MouseDown ||
                Event.current.type == EventType.KeyDown || Event.current.type == EventType.MouseUp)
            {
                DrawToggleForRedIndicators();

                var selectedObjects = Selection.transforms;
                foreach (var selectedTransform in selectedObjects)
                {
                    var cubeScript = selectedTransform.GetComponent<CubeBase>();
                    if (cubeScript == null)
                        continue;

                    var directions = new[]
                        {Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.left, Vector3.right};

                    foreach (var direction in directions)
                    {
                        CheckAndDrawSideIndicator(cubeScript, direction);
                    }

                    if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                    {
                        Handles.BeginGUI();
                        CheckButtonHoverAndDraw(50, 15, 45, 15, "X+", selectedTransform, Vector3.right, 90, 0);
                        CheckButtonHoverAndDraw(100, 15, 45, 15, "X-", selectedTransform, Vector3.right, -90, 1);
                        CheckButtonHoverAndDraw(50, 35, 45, 15, "Y+", selectedTransform, Vector3.up, 90, 2);
                        CheckButtonHoverAndDraw(100, 35, 45, 15, "Y-", selectedTransform, Vector3.up, -90, 3);
                        CheckButtonHoverAndDraw(50, 55, 45, 15, "Z+", selectedTransform, Vector3.forward, 90, 4);
                        CheckButtonHoverAndDraw(100, 55, 45, 15, "Z-", selectedTransform, Vector3.forward, -90, 5);
                        Handles.EndGUI();

                        if (_hoverState != -1)
                        {
                            DrawHoverIndicator(selectedTransform, _hoverState);
                        }
                    }

                    Handles.BeginGUI();

                    DrawCubeIconTypeGUI(260, 15);

                    Handles.EndGUI();

                    if (_selectedState != -1)
                    {
                        DrawPreviewCubePlacement();
                    }
                }
            }
        }

        private void DrawToggleForRedIndicators()
        {
            Handles.BeginGUI();
            Vector2 windowSize = new Vector2(SceneView.currentDrawingSceneView.position.width / 2,
                SceneView.currentDrawingSceneView.position.height - 50);
            Vector2 toggleSize = new Vector2(200, 20);
            float padding = 20f;
            Vector2 togglePosition = new Vector2(padding, windowSize.y - toggleSize.y - padding);
            Rect toggleRect = new Rect(togglePosition.x, togglePosition.y, toggleSize.x, toggleSize.y);

            _showRedIndicators = GUI.Toggle(toggleRect, _showRedIndicators, "Show Red Indicators");
            Handles.EndGUI();
        }

        private void DrawPreviewCubePlacement()
        {
            if (target == null) return;

            CubeBase cubeScript = (CubeBase) target;
            Vector3 previewPosition = cubeScript.transform.position + StateIndexToDirection(_selectedState);

            Handles.color = new Color(1, 1, 0, 0.5f);
            Handles.CubeHandleCap(0, previewPosition, Quaternion.identity, 1, EventType.Repaint);
        }

        private Vector3 StateIndexToDirection(int stateIndex)
        {
            switch (stateIndex)
            {
                case 0: return Vector3.forward;
                case 1: return Vector3.back;
                case 2: return Vector3.up;
                case 3: return Vector3.down;
                case 4: return Vector3.left;
                case 5: return Vector3.right;
                default: return Vector3.zero;
            }
        }

        private void DrawCubeIconTypeGUI(float startX, float startY)
        {
            var cubeCollectable = target as CubeBase;

            if (cubeCollectable == null)
            {
                return;
            }

            var types = Enum.GetValues(typeof(CubeIconType)).Cast<CubeIconType>().Where(t => t != CubeIconType.None)
                .ToArray();

            float buttonHeight = 20;
            float padding = 5;

            foreach (var type in types)
            {
                if (GUI.Button(new Rect(startX, startY, 150, buttonHeight), $"To: {type}"))
                {
                    ChangeCubeIconType(cubeCollectable, type);
                }

                startY += buttonHeight + padding;
            }
        }

        private void ChangeCubeIconType(CubeBase cubeCollectable, CubeIconType newType)
        {
            Undo.RecordObject(cubeCollectable, "Change Cube Icon Type");

            cubeCollectable.ChangeIconType(newType);

            EditorUtility.SetDirty(cubeCollectable);
        }

        private void DrawHoverIndicator(Transform targetTransform, int stateIndex)
        {
            Vector3 start = targetTransform.position + targetTransform.up * 1.5f;
            Vector3 end = start;
            Vector3 direction = Vector3.zero;

            Vector3 axis = Vector3.zero;
            float angle = 0f;

            switch (stateIndex)
            {
                case 0: // X+
                    axis = Vector3.right;
                    angle = 90;
                    break;
                case 1: // X-
                    axis = Vector3.right;
                    angle = -90;
                    break;
                case 2: // Y+
                    axis = Vector3.up;
                    angle = 90;
                    break;
                case 3: // Y-
                    axis = Vector3.up;
                    angle = -90;
                    break;
                case 4: // Z+
                    axis = Vector3.forward;
                    angle = 90;
                    break;
                case 5: // Z-
                    axis = Vector3.forward;
                    angle = -90;
                    break;
            }

            Quaternion rotation = Quaternion.AngleAxis(angle, axis);
            direction = rotation * targetTransform.up;

            float size = HandleUtility.GetHandleSize(start) * 0.25f;
            end += direction * size;

            Vector3 startTangent = start + targetTransform.up * size;
            Vector3 endTangent = end + targetTransform.up * size * 0.5f;
            Handles.DrawBezier(start, end, startTangent, endTangent, Color.yellow, null, 2);

            Handles.ArrowHandleCap(0, end, Quaternion.LookRotation(direction), size * 0.5f, EventType.Repaint);
        }

        private void CheckAndDrawSideIndicator(CubeBase cubeScript, Vector3 direction)
        {
            GameLevel gameLevel = cubeScript.GetGameLevel();

            if (gameLevel == null)
            {
                return;
            }


            var allCubes = gameLevel.GetAllLevelCubesEDITOR();
            if (allCubes == null || allCubes.Length == 0)
            {
                Debug.LogWarning("No CubeBase components found within GameLevel.");
                return;
            }

            Color indicatorColor = _selectedState == GetCurrentStateIndex(direction) ? Color.yellow : Color.cyan;

            foreach (var otherCube in allCubes)
            {
                if (otherCube == cubeScript) continue;

                Vector3 relativeDirection =
                    (otherCube.transform.position - cubeScript.transform.position).normalized;

                if (Vector3.Dot(direction, relativeDirection) > 0.95f)
                {
                    float distance = Vector3.Distance(cubeScript.transform.position, otherCube.transform.position);
                    if (distance <= 1.5f)
                    {
                        if (_showRedIndicators)
                        {
                            indicatorColor = Color.red;
                            break;
                        }
                        else
                        {
                            indicatorColor = new Color(0, 0, 0, 0);
                            break;
                        }
                    }
                }
            }

            Vector3 position = cubeScript.transform.position + direction * 0.5f;

            DrawIdentification(position, indicatorColor, 0.3f);

            if (indicatorColor == Color.cyan)
            {
                if (Handles.Button(position, Quaternion.identity, 0.3f, 0.3f, Handles.SphereHandleCap))
                {
                    _selectedState = GetCurrentStateIndex(direction);
                    if (target == null)
                    {
                        Debug.LogError("No target cube selected.");
                        return;
                    }

                    Transform current = ((Component) target).transform;
                    while (current != null)
                    {
                        if (gameLevel != null)
                        {
                            var window = CubeCreator.GetWindow<CubeCreator>("Create Cube");
                            window.SetGameLevel(gameLevel);

                            Vector3 greenSpherePosition = position;
                            Vector3 originalCubePosition = ((Component) target).transform.position;

                            window.SetPositionData(greenSpherePosition, originalCubePosition);
                            window.Show();
                            return;
                        }

                        current = current.parent;
                    }

                    Debug.LogError("GameLevel component not found in any parent.");
                }
                else
                {
                    DrawIdentification(position, indicatorColor, 0.3f);
                }
            }
        }

        private int GetCurrentStateIndex(Vector3 direction)
        {
            if (direction == Vector3.forward) return 0;
            if (direction == Vector3.back) return 1;
            if (direction == Vector3.up) return 2;
            if (direction == Vector3.down) return 3;
            if (direction == Vector3.left) return 4;
            if (direction == Vector3.right) return 5;
            return -1;
        }

        private void DrawIdentification(Vector3 position, Color color, float size)
        {
            Handles.color = color;
            Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
        }


        private void CheckButtonHoverAndDraw(float x, float y, float width, float height, string label,
            Transform targetTransform, Vector3 axis, float angle, int stateIndex)
        {
            var rect = new Rect(x, y, width, height);
            if (rect.Contains(Event.current.mousePosition))
            {
                if (_hoverState != stateIndex)
                {
                    _hoverState = stateIndex;
                    SceneView.RepaintAll();
                }
            }
            else if (_hoverState == stateIndex)
            {
                _hoverState = -1;
                SceneView.RepaintAll();
            }

            if (GUI.Button(rect, label))
            {
                var selectedObjects = Selection.transforms;
                foreach (var selectedTransform in selectedObjects)
                {
                    RotateCube(selectedTransform, axis, angle);
                }
            }
        }

        private void RotateCube(Transform cubeTransform, Vector3 axis, float angle)
        {
            Undo.RecordObject(cubeTransform, "Rotate Cube");
            cubeTransform.Rotate(axis, angle, Space.World);

            EditorUtility.SetDirty(target);
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(cubeTransform.gameObject.scene);
            }
        }
    }
}