using UnityEngine;

public class GameLevel : MonoBehaviour
{
    public GameObject CubeBase;
    public CubeData CubeData;

    public CubeBase[] GetAllLevelCubesEDITOR()
    {
        return GetComponentsInChildren<CubeBase>();
    }
}