using UnityEngine;
using System.Collections.Generic;

public class PlantController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float initialGrowthSpeed = 2f;
    [SerializeField] private float maxGrowthSpeed = 7f;
    [SerializeField] private float accelerationRate = 0.6f;
    [SerializeField] private float decelerationRate = 0.5f;
    [SerializeField] private float horizontalSpeed = 4f;
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
    [SerializeField] private float recoveryTimeAfterCollision = 0.5f;

    [Header("References")]
    [SerializeField] public PlantLife plantLife;

    private float currentGrowthSpeed;
    private float targetGrowthSpeed;
    private float storedPreCollisionSpeed;
    private float recoveryTimer = 0f;

    private bool isGrowing = false;
    private bool isStuck = false;
    private bool isGameOver = false;
    private Vector3 lastPosition;
    private float targetRotation = 0f;
    private float currentRotationVelocity;

    private Dictionary<BuffType, float> activeBuffs = new Dictionary<BuffType, float>();

    public bool IsGrowing => isGrowing;
    public bool IsStuck => isStuck;
    public float CurrentHeight => plantHead.position.y;
    public float DisplayHeight => plantHead.position.y + heightOffset;
    public Transform PlantHead => plantHead;

    private void Start()
    {
        currentGrowthSpeed = initialGrowthSpeed;
        targetGrowthSpeed = initialGrowthSpeed;
        lastPosition = transform.position;
    }

    private void Update()
    {
        UpdateBuffs();
        UpdateGrowth();
    }

    public void SetGrowing(bool growing)
    {
        if (isGameOver) return;
        isGrowing = growing;
        targetGrowthSpeed = growing ? maxGrowthSpeed : initialGrowthSpeed;
    }

    public void SetHorizontalMovement(float direction)
    {
        if (isGameOver) return;

        bool canMoveHorizontally = !Physics2D.CircleCast(
            plantHead.position,
            collisionCheckRadius,
            new Vector2(direction, 0),
            0.1f,
            obstacleLayer
        );

        if (canMoveHorizontally)
        {
            Vector3 movement = new Vector3(direction * horizontalSpeed * Time.deltaTime, 0, 0);
            transform.Translate(movement);

            targetRotation = Mathf.Abs(direction) > 0.01f ? Mathf.Clamp(-direction * 45f, -45f, 45f) : 0f;

            if (isStuck)
            {
                Collider2D hitCollider = Physics2D.OverlapCircle(
                    plantHead.position + Vector3.up * 0.1f,
                    collisionCheckRadius,
                    obstacleLayer
                );

                if (hitCollider == null)
                {
                    isStuck = false;
                    Debug.Log("Plant is no longer stuck");
                }
            }
        }
    }

    private void UpdateGrowth()
    {
        if (isGameOver) return;

        if (recoveryTimer > 0f)
        {
            recoveryTimer -= Time.deltaTime;
            currentGrowthSpeed = Mathf.MoveTowards(currentGrowthSpeed, initialGrowthSpeed, decelerationRate * Time.deltaTime);
        }
        else if (storedPreCollisionSpeed > 0f)
        {
            // Resume previous speed after recovery
            currentGrowthSpeed = storedPreCollisionSpeed;
            storedPreCollisionSpeed = 0f;
        }
        else if (isGrowing && !isStuck)
        {
            currentGrowthSpeed = Mathf.MoveTowards(
                currentGrowthSpeed,
                targetGrowthSpeed,
                accelerationRate * Time.deltaTime
            );
        }
        else
        {
            currentGrowthSpeed = Mathf.Max(
                Mathf.MoveTowards(currentGrowthSpeed, initialGrowthSpeed, decelerationRate * Time.deltaTime),
                initialGrowthSpeed
            );
        }

        if (HasActiveBuff(BuffType.Speed))
        {
            currentGrowthSpeed *= speedBuffMultiplier;
        }

        if (!isStuck)
        {
            transform.Translate(Vector3.up * currentGrowthSpeed * Time.deltaTime);
        }

        UpdatePlantHeadRotation();
        lastPosition = transform.position;
    }

    public void HandlePhysicalCollision()
    {
        isStuck = true;

        // Save current speed for recovery
        storedPreCollisionSpeed = currentGrowthSpeed;

        // Start recovery timer
        recoveryTimer = recoveryTimeAfterCollision;

        currentGrowthSpeed = initialGrowthSpeed;
        targetGrowthSpeed = initialGrowthSpeed;
    }

    public void StopPlant()
    {
        isGameOver = true;
        isGrowing = false;
        currentGrowthSpeed = 0f;
        targetGrowthSpeed = 0f;
        isStuck = true;
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

    private void RemoveBuff(BuffType buffType)
    {
        if (activeBuffs.ContainsKey(buffType))
        {
            activeBuffs.Remove(buffType);
        }
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
