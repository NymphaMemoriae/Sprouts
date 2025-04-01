using UnityEngine;

[CreateAssetMenu(fileName = "New Side Segment", menuName = "Plant Game/Side Obstacle Segment")]
public class SideObstacleSegment : ScriptableObject
{
    [Header("Segment Info")]
    public string segmentName;

    [Header("Prefabs")]
    public GameObject trunkPrefab;
    public GameObject capPrefab;

    [Header("Trunk Stack Settings")]
    [Tooltip("Minimum number of trunk tiles to spawn before segment switches.")]
    public int minTrunks = 2;

    [Tooltip("Maximum number of trunk tiles to spawn before segment switches.")]
    public int maxTrunks = 4;

    [Header("Spawn Position")]
    [Tooltip("X coordinate for left-side spawn.")]
    public float leftX = -5.5f;

    [Tooltip("X coordinate for right-side spawn.")]
    public float rightX = 5.5f;

    [Header("Tiling Settings")]
    [Tooltip("Vertical spacing between trunk tiles. Should match the visual height of the trunk sprite.")]
    public float verticalSpacing = 15.36f;

    [Header("Cap Spawn Settings")]
    [Tooltip("Minimum number of trunks between caps. Set both to 0 to disable caps.")]
    public int minCapInterval = 0;

    [Tooltip("Maximum number of trunks between caps.")]
    public int maxCapInterval = 0;

}
