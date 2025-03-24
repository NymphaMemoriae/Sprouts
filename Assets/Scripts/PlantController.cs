using UnityEngine;
using System.Collections.Generic;

public class PlantController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float initialGrowthSpeed = 1f;
    [SerializeField] private float maxGrowthSpeed = 10f;
    [SerializeField] private float accelerationRate = 0.5f;
    [SerializeField] private float decelerationRate = 1f;
    [SerializeField] private float horizontalSpeed = 5f;
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
    [SerializeField] private float heightOffset = -500f; // New offset to start height at -500m

    [Header("References")]
    // Reference to the PlantLife component that manages the plant's lives
    [SerializeField] public PlantLife plantLife;
    
    // Private variables
    private float currentGrowthSpeed;
    private float targetGrowthSpeed;
    private bool isGrowing = false;
    private bool isStuck = false;
    private Vector3 lastPosition;
    private Dictionary<BuffType, float> activeBuffs = new Dictionary<BuffType, float>();
    private float targetRotation = 0f;
    private float currentRotationVelocity;
    private bool isGameOver = false;
    
    // Properties
    public bool IsGrowing => isGrowing;
    public bool IsStuck => isStuck;
    public float CurrentHeight => plantHead.position.y;
    public float DisplayHeight => plantHead.position.y + heightOffset; // New property for display height
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
        // CheckCollisions();
    }
    
    public void SetGrowing(bool growing)
    {
        if (isGameOver) return; // Don't allow growing if game is over
        
        isGrowing = growing;
        targetGrowthSpeed = growing ? maxGrowthSpeed : initialGrowthSpeed;
    }
    
    public void SetHorizontalMovement(float direction)
    {
        if (isGameOver) return; // Don't allow movement if game is over

        // Check for horizontal obstacles
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
            
            // Invert rotation direction because the pivot is at the bottom
            if (Mathf.Abs(direction) > 0.01f)
            {
                targetRotation = Mathf.Clamp(-direction * 45f, -45f, 45f);
            }
            else
            {
                targetRotation = 0f;
            }
            
            // If stuck, check if moving horizontally frees the plant
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
        if (isGameOver) return; // Don't update growth if game is over

        if (isGrowing && !isStuck)
        {
            currentGrowthSpeed = Mathf.MoveTowards(
                currentGrowthSpeed, 
                targetGrowthSpeed, 
                accelerationRate * Time.deltaTime
            );
        }
        else
        {
            currentGrowthSpeed = Mathf.MoveTowards(
                currentGrowthSpeed, 
                initialGrowthSpeed, 
                decelerationRate * Time.deltaTime
            );
        }
        
        // Apply speed buff multiplier if active
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
    

    // private void HandleDamageCollision(int damage, DamageObstacle damageObstacle)
    // {
    //     if (damage <= 0) return;
        
    //     // If an ExtraLife buff is active, remove it and reset collision effects
    //     if (HasActiveBuff(BuffType.ExtraLife))
    //     {
    //         RemoveBuff(BuffType.ExtraLife);
    //         ResetAfterCollision();
    //         return;
    //     }
        
    //     // Use the PlantLife component to apply the damage
    //     if (plantLife != null)
    //     {
    //         plantLife.TakeDamage(damage);
    //     }
        
    //     // If this obstacle is physical, stop growth and possibly apply a bounce effect
    //     if (damageObstacle.obstacleData.isPhysical)
    //     {
    //         isStuck = true;
    //         currentGrowthSpeed = initialGrowthSpeed;
    //         if (damageObstacle.obstacleData.causeBounce)
    //         {
    //             ApplyBounce(damageObstacle.obstacleData.bounceForce, damageObstacle.obstacleData.bounceRecoveryTime);
    //         }
    //     }
    // }
    
    private void ResetAfterCollision()
    {
        currentGrowthSpeed = initialGrowthSpeed;
        targetGrowthSpeed = initialGrowthSpeed;
    }
    
    public void ApplyBuff(BuffType buffType, float duration = 0)
    {
        switch (buffType)
        {
            case BuffType.ExtraLife:
                // Extra life buff remains until used
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
    
    // public void ApplyBounce(float bounceForce, float recoveryTime)
    // {
    //     transform.Translate(Vector3.down * bounceForce);
    //     currentGrowthSpeed = initialGrowthSpeed;
    //     isGrowing = false;
    //     Invoke("RecoverFromBounce", recoveryTime);
    // }
    
    // private void RecoverFromBounce()
    // {
    //     isGrowing = true;
    // }
    
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

    public void HandlePhysicalCollision()
    {
        isStuck = true;
        currentGrowthSpeed = initialGrowthSpeed;
        ResetAfterCollision();
    }

    public void StopPlant()
    {
        isGameOver = true;
        isGrowing = false;
        currentGrowthSpeed = 0f;
        targetGrowthSpeed = 0f;
        isStuck = true; // Prevent any movement
    }
}

// Add the missing BuffType enum definition:
public enum BuffType
{
    ExtraLife,
    Speed,
    Ghost
}