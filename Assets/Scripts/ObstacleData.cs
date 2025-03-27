using UnityEngine;

[CreateAssetMenu(fileName = "New Obstacle", menuName = "Plant Game/Obstacle Data")]
public class ObstacleData : ScriptableObject
{
    [Header("Basic Settings")]
    public string obstacleName;
    public GameObject prefab;

    [Header("Obstacle Type")]
    [Tooltip("If true, this obstacle behaves as a solid physical object that blocks the plant. If false, the plant can pass through it.")]
    public bool isPhysical;
    
    [Tooltip("If true, this obstacle will only spawn on the sides of the screen.")]
    public bool isSideObstacle;

    [Header("Side Obstacle Settings (Only used if isSideObstacle = true)")]
    [Range(0f, 45f)]
    [Tooltip("Maximum angle (in degrees) the obstacle can be rotated when spawning.")]
    public float maxRotationAngle = 20f;

    [Header("Damage Settings")]
    [Tooltip("How many lives this obstacle removes from the plant upon collision/intersection.")]
    public int damage;

    [Header("Physical Obstacle Settings (Only used if isPhysical = true)")]
    public bool causeBounce = false;
    public float bounceForce = 1f;
    public float bounceRecoveryTime = 0.5f;

    [Header("Visual Feedback")]
    public GameObject collisionEffectPrefab;
    public float effectDuration = 1f;
    public bool changeColorOnCollision = true;
    public Color collisionColor = Color.gray;

    [Header("Audio")]
    public AudioClip collisionSound;
    [Range(0f, 1f)]
    public float volume = 0.7f;

    [Header("Spawn Settings")]
    [Range(0f, 1f)]
    [Tooltip("Relative spawn likelihood. Higher = more likely.")]
    public float spawnWeight = 1f;

    [TextArea(3, 5)]
    public string description;
}