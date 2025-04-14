using UnityEngine;

public class PlantLife : MonoBehaviour
{
    [Header("Life Settings")]
    [SerializeField] private int maxLives = 3;
    public int CurrentLives { get; private set; }

    public event System.Action<int> OnLivesChanged;

    [Header("References")]
    [SerializeField] private Transform plantHead;

    [Tooltip("Optional: Particle burst for damage (e.g. bleeding)")]
    [SerializeField] private ParticleSystem bleedParticles;

    [Tooltip("Optional: Animator for hurt animation")]
    [SerializeField] private Animator animator;

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
            isInvulnerable = true;
            invulnerabilityTimer = invulnerabilityDuration;
        }
    }

    public void ResetLives()
    {
        CurrentLives = maxLives;
        OnLivesChanged?.Invoke(CurrentLives);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        CurrentLives -= damage;
        if (CurrentLives < 0)
            CurrentLives = 0;

        OnLivesChanged?.Invoke(CurrentLives);

        if (bleedParticles != null)
        {
            bleedParticles.Play();
        }

        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }

        if (CurrentLives <= 0)
        {
            plantController?.StopPlant();
            Invoke(nameof(TriggerGameOver), 0.01f);
        }
    }

    public void AddLife(int extra = 1)
    {
        CurrentLives += extra;
        OnLivesChanged?.Invoke(CurrentLives);
    }

    private void TriggerGameOver()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.GameOver);
        }
        else
        {
            Debug.LogError("GameManager.Instance was null when trying to trigger GameOver.");
        }
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
