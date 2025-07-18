using UnityEngine;

[RequireComponent(typeof(PlantController))]
public class PlantSounds : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource for the plant's movement sounds. Will be configured to loop.")]
    [SerializeField] private AudioSource movementAudioSource;
    [Tooltip("AudioSource for the biome's soundtrack. Will be configured to loop.")]
    [SerializeField] private AudioSource soundtrackAudioSource;

    [Header("Movement Sound Parameters")]
    [SerializeField] private float minMovementVolume = 0.2f;
    [SerializeField] private float maxMovementVolume = 0.7f;
    [SerializeField] private float minMovementPitch = 0.8f;
    [SerializeField] private float maxMovementPitch = 1.3f;
    [Tooltip("The plant's velocity at which max pitch/volume is reached. Adjust based on typical max plant speed.")]
    [SerializeField] private float velocityForMaxEffect = 7f; // Corresponds to PlantController's default maxGrowthSpeed

    [Header("References")]
    private PlantController plantController;
    private BiomeManager biomeManager;

    private BiomeData currentBiomeData;
    private bool isSubscribed = false;

    void Awake()
    {
        plantController = GetComponent<PlantController>();

        // Ensure AudioSources are assigned and configured
        if (movementAudioSource == null)
        {
            Debug.LogError("[PlantSounds] Movement AudioSource is not assigned!", this);
            enabled = false; // Disable script if essential components are missing
            return;
        }
        if (soundtrackAudioSource == null)
        {
            Debug.LogError("[PlantSounds] Soundtrack AudioSource is not assigned!", this);
            enabled = false;
            return;
        }

        movementAudioSource.loop = true;
        movementAudioSource.playOnAwake = false;

        soundtrackAudioSource.loop = true;
        soundtrackAudioSource.playOnAwake = false;
    }

    void Start()
    {
        // It's crucial BiomeManager is found and its event can be subscribed to.
        // BiomeManager might initialize its first biome in its own Start method.
        biomeManager = FindFirstObjectByType<BiomeManager>();
        if (biomeManager != null)
        {
            biomeManager.OnBiomeTransitionComplete += HandleBiomeTransition;
            isSubscribed = true;
            Debug.Log("[PlantSounds] Subscribed to OnBiomeTransitionComplete.");

            // If BiomeManager already has a current biome when PlantSounds starts,
            // explicitly trigger a transition handle to set initial sounds.
            if (biomeManager.CurrentBiome != null)
            {
                HandleBiomeTransition(biomeManager.CurrentBiome, true); // Assuming it might be the first biome
            }
        }
        else
        {
            Debug.LogError("[PlantSounds] BiomeManager not found! Sounds will not update with biomes.", this);
        }
    }

    void OnDestroy()
    {
        if (biomeManager != null && isSubscribed)
        {
            biomeManager.OnBiomeTransitionComplete -= HandleBiomeTransition;
            isSubscribed = false;
        }
    }

    /// <summary>
    /// This is called by GameManager once the scene is fully loaded and ready.
    /// It kick-starts the soundtrack for the initial biome.
    /// </summary>
    public void PlayInitialSoundtrack()
    {
        // The BiomeManager should have figured out the starting biome by now.
        if (biomeManager == null)
        {
            Debug.LogError("[PlantSounds] Cannot play initial soundtrack, BiomeManager reference is missing!");
            return;
        }

        BiomeData initialBiome = biomeManager.CurrentBiome;
        if (initialBiome != null)
        {
            // We can simply call our existing HandleBiomeTransition logic.
            // The check inside it to not restart the same clip will prevent any issues.
            HandleBiomeTransition(initialBiome, true);
        }
        else
        {
            Debug.LogWarning("[PlantSounds] BiomeManager has no current biome when starting. No music will play yet.");
        }
    }

    private void HandleBiomeTransition(BiomeData newBiome, bool isFirstBiomeOverall)
    {
        // This script only runs in the GameScene, so no scene check is needed.
        if (newBiome == null)
        {
            Debug.LogWarning("[PlantSounds] Received null BiomeData in HandleBiomeTransition.");
            return;
        }

        Debug.Log($"[PlantSounds] Handling biome transition to: {newBiome.biomeName}");
        currentBiomeData = newBiome;

        // --- Plant Movement Sound Logic ---
        if (movementAudioSource.isPlaying)
        {
            movementAudioSource.Stop();
        }
        movementAudioSource.clip = currentBiomeData.plantMovementSound;


        // --- Final Soundtrack Logic ---
        AudioClip clipToPlay = null;

        // 1. Check PlayerPrefs for a custom equipped soundtrack for this biome.
        string equippedSoundtrackID = PlayerPrefsManager.Instance.GetEquippedSoundtrackForBiome(newBiome.biomeName);

        if (equippedSoundtrackID != null)
        {
            // 2. If a custom track is equipped, load its data from "Resources/Soundtracks".
            SoundtrackData equippedSoundtrackData = Resources.Load<SoundtrackData>("Soundtracks/" + equippedSoundtrackID);
            if (equippedSoundtrackData != null)
            {
                clipToPlay = equippedSoundtrackData.audioClip;
            }
        }

        // 3. If no custom track was chosen, use the biome's default soundtrack.
        if (clipToPlay == null)
        {
            // This now correctly uses the 'defaultSoundtrack' variable from BiomeData.cs
            clipToPlay = newBiome.defaultSoundtrack;
        }

        // 4. Stop the currently playing music (if any) and play the new track.
        if (soundtrackAudioSource.isPlaying)
        {
            if (soundtrackAudioSource.clip == clipToPlay) return;
            soundtrackAudioSource.Stop();
        }

        soundtrackAudioSource.clip = clipToPlay;

        if (soundtrackAudioSource.clip != null)
        {
            soundtrackAudioSource.Play();
            Debug.Log($"[PlantSounds] Now playing: {soundtrackAudioSource.clip.name}");
        }
    }

    void Update()
    {
        if (plantController == null || currentBiomeData == null)
        {
            if (movementAudioSource.isPlaying) movementAudioSource.Stop();
            return;
        }

        bool canPlayMovementSound = movementAudioSource.clip != null &&
                                   plantController.IsGrowing &&
                                   !plantController.IsStuck;

        if (canPlayMovementSound)
        {
            if (!movementAudioSource.isPlaying)
            {
                movementAudioSource.Play();
            }

            float currentSpeed = plantController.CurrentVelocity;
            // Use GetMaxGrowthSpeed() from PlantController if you want velocityForMaxEffect to be dynamic
            // For now, using the serialized 'velocityForMaxEffect' for more control from the inspector.
            float normalizedSpeed = Mathf.Clamp01(currentSpeed / velocityForMaxEffect);

            movementAudioSource.pitch = Mathf.Lerp(minMovementPitch, maxMovementPitch, normalizedSpeed);
            movementAudioSource.volume = Mathf.Lerp(minMovementVolume, maxMovementVolume, normalizedSpeed);
        }
        else
        {
            if (movementAudioSource.isPlaying)
            {
                movementAudioSource.Stop(); // Or you could implement a fade out
            }
        }
    }
}