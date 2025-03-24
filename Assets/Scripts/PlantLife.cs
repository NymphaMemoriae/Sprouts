using UnityEngine;
using UnityEngine.Events;

public class PlantLife : MonoBehaviour
{
    [Header("Life Settings")]
    [SerializeField] private int maxLives = 3;
    public int CurrentLives { get; private set; }
    
    [Header("Events")]
    public UnityEvent<int> onLivesChanged;
    public UnityEvent onPlantDeath;
    
    [Header("References")]
    [SerializeField] private Transform plantHead;
    
    [Header("Collision Detection")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    private bool isInvulnerable = false;
    private float invulnerabilityTimer = 0f;
    private PlantController plantController;
    
    private void Awake()
    {
        ResetLives();
    }
    
    private void Start()
    {
        plantController = GetComponentInParent<PlantController>();
        
        // Add collision detector to PlantHead
        var collisionDetector = plantHead.gameObject.AddComponent<PlantCollisionDetector>();
        collisionDetector.Initialize(this);
    }

    private void Update()
    {
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0)
            {
                isInvulnerable = false;
            }
        }
    }

    public void HandleCollision(DamageObstacle obstacle)
    {
        if (isInvulnerable) return;

        if (obstacle.obstacleData != null && obstacle.obstacleData.damage > 0)
        {
            TakeDamage(obstacle.obstacleData.damage);
            
            // Apply invulnerability
            isInvulnerable = true;
            invulnerabilityTimer = invulnerabilityDuration;
        }
    }

    public void ResetLives()
    {
        CurrentLives = maxLives;
        onLivesChanged?.Invoke(CurrentLives);
    }
    
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        CurrentLives -= damage;
        if (CurrentLives < 0)
            CurrentLives = 0;
        
        onLivesChanged?.Invoke(CurrentLives);

        if (CurrentLives <= 0)
        {
            onPlantDeath?.Invoke();
        }
    }
    
    public void AddLife(int extra = 1)
    {
        CurrentLives += extra;
        if (CurrentLives > maxLives)
            CurrentLives = maxLives;
        
        onLivesChanged?.Invoke(CurrentLives);
    }
}

public class PlantCollisionDetector : MonoBehaviour
{
    private PlantLife plantLife;

    public void Initialize(PlantLife life)
    {
        plantLife = life;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (plantLife != null)
        {
            DamageObstacle obstacle = collision.gameObject.GetComponent<DamageObstacle>();
            if (obstacle != null)
            {
                plantLife.HandleCollision(obstacle);
            }
        }
    }
}
