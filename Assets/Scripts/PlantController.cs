using UnityEngine;
using System.Collections.Generic;

public class PlantController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed when growth begins or resets")]
    [SerializeField] private float initialGrowthSpeed = 2f;
    public float GetInitialGrowthSpeed()
    {
        return initialGrowthSpeed;
    }

    [Tooltip("Maximum vertical speed the plant can reach")]
    [SerializeField] private float maxGrowthSpeed = 7f;

    [Tooltip("How fast the plant accelerates upward")]
    [SerializeField] private float accelerationRate = 0.6f;

    [Tooltip("How fast the plant decelerates after a collision")]
    [SerializeField] private float decelerationRate = 0.5f;

    [Tooltip("Speed for left/right swiping")]
    [SerializeField] private float horizontalSpeed = 4f;

    [Tooltip("Smooth time for rotation of the plant head")]
    [SerializeField] private float rotationSmoothTime = 0.2f;

    [Header("Collision Settings")]
    [SerializeField] private Transform plantHead;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float collisionCheckRadius = 0.2f;

    [Header("Buff Settings")]
    [SerializeField] private float ghostBuffDuration = 3f;
    [SerializeField] private float speedBuffMultiplier = 1.5f;
    [SerializeField] private float speedBuffDuration = 5f;

    [Header("Height Settings")]
    [SerializeField] private float heightOffset = -500f;

    [Header("Collision Recovery")]
    [Tooltip("Time the plant decelerates after hitting a horizontal obstacle")]
    [SerializeField] private float sideBumpRecoveryTime = 0.2f;

    [Header("References")]
    [SerializeField] public PlantLife plantLife;

    private float currentGrowthSpeed;
    private bool isGrowing = false;
    private bool isGameOver = false;
    private Vector3 lastPosition;
    private float targetRotation = 0f;
    private float currentRotationVelocity;
    private bool isVerticallyBlocked = false;
    private float sideBumpTimer = 0f;

    private Dictionary<BuffType, float> activeBuffs = new Dictionary<BuffType, float>();

    // Exposed Properties
    public bool IsGrowing => isGrowing;
    public bool IsStuck => isVerticallyBlocked;
    public float CurrentHeight => plantHead.position.y;
    public float DisplayHeight => plantHead.position.y + heightOffset;
    public Transform PlantHead => plantHead;
    public float CurrentVelocity => currentGrowthSpeed;

    private void Start()
    {
        currentGrowthSpeed = initialGrowthSpeed;
        lastPosition = transform.position;
    }

    private void Update()
    {
        UpdateBuffs();
        CheckVerticalCollision();
        UpdateGrowth();

        if (Input.GetKeyDown(KeyCode.L))
        {
            Vector3 teleportPosition = transform.position + Vector3.up * 100f;
            transform.position = teleportPosition;
            Debug.Log("Plant teleported 100 meters up!");
        }
    }

    public void SetGrowing(bool growing)
    {
        if (isGameOver) return;
        isGrowing = growing;
    }

    public void SetHorizontalMovement(float direction)
    {
        if (isGameOver) return;

        if (Mathf.Approximately(direction, 0f)) return;

        bool blocked = Physics2D.CircleCast(
            plantHead.position,
            collisionCheckRadius,
            new Vector2(direction, 0),
            0.05f,
            obstacleLayer
        );

        if (!blocked)
        {
            Vector3 movement = new Vector3(direction * horizontalSpeed * Time.deltaTime, 0, 0);
            transform.Translate(movement);
        }
        else
        {
            // Apply minor upward slowdown on side bump
            sideBumpTimer = sideBumpRecoveryTime;
        }

        targetRotation = Mathf.Abs(direction) > 0.01f ? Mathf.Clamp(-direction * 45f, -45f, 45f) : 0f;
    }

    private void UpdateGrowth()
    {
        if (isGameOver) return;

        // Handle acceleration
        if (isGrowing && !isVerticallyBlocked)
        {
            currentGrowthSpeed = Mathf.MoveTowards(currentGrowthSpeed, maxGrowthSpeed, accelerationRate * Time.deltaTime);
        }

        // Side bump deceleration
        if (sideBumpTimer > 0f)
        {
            sideBumpTimer -= Time.deltaTime;
            currentGrowthSpeed = Mathf.MoveTowards(currentGrowthSpeed, initialGrowthSpeed, decelerationRate * Time.deltaTime);
        }

        float finalSpeed = currentGrowthSpeed;
        if (HasActiveBuff(BuffType.Speed))
        {
            finalSpeed *= speedBuffMultiplier;
        }

        // Don't move vertically if blocked above
        if (!isVerticallyBlocked)
        {
            transform.Translate(Vector3.up * finalSpeed * Time.deltaTime);
        }

        UpdatePlantHeadRotation();
        lastPosition = transform.position;
    }

    private void CheckVerticalCollision()
    {
        if (HasActiveBuff(BuffType.Ghost))
        {
            isVerticallyBlocked = false;
            return;
        }

        RaycastHit2D hit = Physics2D.CircleCast(
            plantHead.position,
            collisionCheckRadius,
            Vector2.up,
            0.05f,
            obstacleLayer
        );

        if (hit.collider != null)
        {
            isVerticallyBlocked = true;

            if (currentGrowthSpeed > initialGrowthSpeed)
            {
                currentGrowthSpeed = initialGrowthSpeed;
            }
        }

        else
        {
            isVerticallyBlocked = false;
        }
    }

    public void HandlePhysicalCollision()
    {
        currentGrowthSpeed = initialGrowthSpeed; // typically 2f
        isVerticallyBlocked = true;
    }


    public void StopPlant()
    {
        isGameOver = true;
        isGrowing = false;
        currentGrowthSpeed = 0f;
    }
    public void ResetState(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        plantHead.rotation = Quaternion.identity; // Reset rotation
        currentGrowthSpeed = initialGrowthSpeed; // Reset speed
        isGrowing = false; // Or true, depending on desired start state
        isGameOver = false;
        isVerticallyBlocked = false;
        sideBumpTimer = 0f;
        targetRotation = 0f;
        currentRotationVelocity = 0f;
        // Optionally reset activeBuffs if they shouldn't persist after death
        // activeBuffs.Clear();
        lastPosition = spawnPosition;
        Debug.Log($"[PlantController] State reset at position {spawnPosition}");
    }

    private void UpdateBuffs()
    {
        List<BuffType> expiredBuffs = new List<BuffType>();

        foreach (var buff in activeBuffs)
        {
            if (buff.Value > 0)
            {
                activeBuffs[buff.Key] -= Time.deltaTime;
                if (activeBuffs[buff.Key] <= 0)
                {
                    expiredBuffs.Add(buff.Key);
                }
            }
        }

        foreach (var buff in expiredBuffs)
        {
            activeBuffs.Remove(buff);
        }
    }

    public void ApplyBuff(BuffType buffType, float duration = 0)
    {
        switch (buffType)
        {
            case BuffType.ExtraLife:
                activeBuffs[buffType] = -1;
                break;
            case BuffType.Speed:
                activeBuffs[buffType] = duration > 0 ? duration : speedBuffDuration;
                break;
            case BuffType.Ghost:
                activeBuffs[buffType] = duration > 0 ? duration : ghostBuffDuration;
                break;
        }
    }

    private bool HasActiveBuff(BuffType buffType)
    {
        return activeBuffs.ContainsKey(buffType);
    }

    private void UpdatePlantHeadRotation()
    {
        if (plantHead != null)
        {
            float currentAngle = plantHead.rotation.eulerAngles.z;
            if (currentAngle > 180f) currentAngle -= 360f;

            float smoothedAngle = Mathf.SmoothDampAngle(
                currentAngle,
                targetRotation,
                ref currentRotationVelocity,
                rotationSmoothTime
            );

            plantHead.rotation = Quaternion.Euler(0f, 0f, smoothedAngle);
        }
    }
}

public enum BuffType
{
    ExtraLife,
    Speed,
    Ghost
}
