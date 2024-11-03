using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CubeBase : MonoBehaviour
{
    public GameLevel GetGameLevel() => _gameLevel;
    public CubeIconType CubeIconType;
    
    [SerializeField] private MeshRenderer _mainMesh;

    private bool _selected;
    private GameLevel _gameLevel;
    private CubeData _cubeData;

    public void ChangeIconType(CubeIconType cubeIconType)
    {
        var sprite = GetSpriteForIconType(cubeIconType);

        ApplySprite(sprite);
//        Debug.LogWarning("Change icon to " + cubeIconType + " Sprite " + sprite);
    }

    public void SetCubeData(CubeData cubeData)
    {
        _cubeData = cubeData;
    }

    public void SetGameLevel(GameLevel gameLevel)
    {
        _gameLevel = gameLevel;
        _cubeData = gameLevel.CubeData;
    }

    private void ApplySprite(Sprite sprite)
    {
#if UNITY_EDITOR
        if (_mainMesh == null)
            _mainMesh = GetComponent<MeshRenderer>();

        if (_mainMesh == null)
        {
            Debug.LogError("MainMesh not set on CubeCollectable.");
            return;
        }

        var materials = _mainMesh.sharedMaterials;
        if (materials.Length >= 1 && materials[0] != null && sprite != null)
        {
            Material materialToUse;
            if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier<Sprite>(sprite, out string guid,
                    out long localId))
            {
                Debug.LogError("Failed to get GUID for sprite");
                return;
            }

            string materialPath = "Assets/LevelBuilder/Materials/" + guid + "_" + sprite.name + ".mat";
            materialToUse = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (materialToUse == null)
            {
                materialToUse = new Material(materials[0]);
                materialToUse.mainTexture = sprite.texture;
                UnityEditor.AssetDatabase.CreateAsset(materialToUse, materialPath);
                UnityEditor.EditorUtility.SetDirty(materialToUse); // Ensure new material is saved
            }

            Undo.RecordObject(_mainMesh, "Apply Sprite Material"); // Record changes for undo
            materials[0] = materialToUse;
            _mainMesh.materials = materials;
        }
        else
        {
            Debug.LogWarning(
                $"Sprite is null {sprite}, sharedMaterials array does not have enough entries {materials.Length}, or the material at index 0 is null! {materials[0]}");
        }
#else
            if (_mainMesh == null)
            {
                Debug.LogError("MainMesh not set on CubeCollectable.");
                return;
            }

            var materials1 = _mainMesh.sharedMaterials;
            if (materials1.Length > 1 && materials1[1] != null && sprite != null)
            {
                //Undo.RecordObject(materials1[1],
                //  "Apply Sprite Texture");
                materials1[1].mainTexture = sprite.texture;
                _mainMesh.materials = materials1;
            }
            else
            {
                Debug.LogWarning("Sprite is null or sharedMaterials array does not have enough entries!");
            }
#endif
    }

#if UNITY_EDITOR
    public static event Action<GameObject> OnCubeInstantiated;

    private void OnValidate()
    {
        if (_gameLevel == null)
        {
            _gameLevel = GetComponentInParent<GameLevel>();
        }
    }

    public void SetUpNewCube()
    {
        if (_gameLevel == null)
        {
            _gameLevel = GetComponentInParent<GameLevel>();
        }
    }

    private Sprite GetSpriteForIconType(CubeIconType cubeIconType)
    {
        return cubeIconType switch
        {
            CubeIconType.Ground => _cubeData.Ground,
            CubeIconType.GroundAndGrass => _cubeData.GroundAndGrass,
            CubeIconType.IceCave => _cubeData.IceCave,
            CubeIconType.IceCaveBlocks => _cubeData.IceCaveBlocks,
            CubeIconType.Lava => _cubeData.Lava,
            CubeIconType.MossBlock => _cubeData.MossBlock,
            CubeIconType.OceanRock => _cubeData.OceanRock,
            CubeIconType.ShardRock => _cubeData.ShardRock,
            CubeIconType.Stone => _cubeData.Stone,
            CubeIconType.StoneWall => _cubeData.StoneWall,

            _ => _cubeData.NotFound
        };
    }

    private void UpdateCubeType()
    {
        //if (!_selected) return;

        GameObject newCubePrefab = null;
        //switch (_cubeType)
        //{
        //    case CubeType.Collectable:
        //        newCubePrefab = PrefabUtility.InstantiatePrefab(_cubeData.CubeCollectable.gameObject) as GameObject;
        //        break;
        //    case CubeType.Movable:
        //        newCubePrefab = PrefabUtility.InstantiatePrefab(_cubeData.CubeMovable.gameObject) as GameObject;
        //        break;
        //}

        if (newCubePrefab != null)
        {
            newCubePrefab.transform.position = transform.position;
            newCubePrefab.transform.rotation = transform.rotation;
            newCubePrefab.transform.parent = transform.parent;

            var newCubeBase = newCubePrefab.GetComponent<CubeBase>();
            if (newCubeBase != null)
            {
                newCubeBase.SetUpNewCube();
            }

            OnCubeInstantiated?.Invoke(newCubePrefab);
            EditorApplication.delayCall += () => DestroyGameObjectSafe(gameObject);
        }
    }

    private void DestroyGameObjectSafe(GameObject gameObjectToDestroy)
    {
        if (Application.isPlaying)
        {
            Destroy(gameObjectToDestroy);
        }
        else
        {
            DestroyImmediate(gameObjectToDestroy, false);
        }
    }

    public void SelectCube()
    {
        _selected = true;
    }

    public void DeselectCube()
    {
        _selected = false;
    }

    public CubeBase CheckForVectorBlockage(float distance, CubeBase[] allCubes, HashSet<CubeBase> movedCubes)
    {
        Vector3 checkDirection = transform.forward;
        Vector3 startPosition = transform.position;

        for (var step = 1f; step <= distance; step += 1f)
        {
            Vector3 checkPosition = startPosition + checkDirection * step;
            foreach (var otherCube in allCubes)
            {
                if (otherCube != this && !movedCubes.Contains(otherCube as CubeBase) &&
                    Vector3.Distance(otherCube.transform.position, checkPosition) < 0.5f)
                {
                    return otherCube;
                }
            }
        }

        return null;
    }
#endif
}