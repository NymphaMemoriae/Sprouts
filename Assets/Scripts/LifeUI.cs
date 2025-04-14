using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LifeUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private int standardLives = 3;

    [Header("References")]
    [SerializeField] private PlantLife plantLife;

    private List<Image> heartImages = new();

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
                heartImages.Add(img);
        }

        heartImages.Sort((a, b) => string.Compare(a.name, b.name));
    }

    private void Start()
    {
        if (plantLife != null)
        {
            plantLife.OnLivesChanged += UpdateHeartsUI;
            UpdateHeartsUI(plantLife.CurrentLives); // Initial setup
        }

        // Hide all extra hearts on start
        for (int i = standardLives; i < heartImages.Count; i++)
        {
            heartImages[i].gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (plantLife != null)
        {
            plantLife.OnLivesChanged -= UpdateHeartsUI;
        }
    }

    private void UpdateHeartsUI(int lives)
    {
        for (int i = 0; i < heartImages.Count; i++)
        {
            if (i < standardLives)
            {
                // Standard lives: swap sprites
                heartImages[i].sprite = (i < lives) ? fullHeartSprite : emptyHeartSprite;
                heartImages[i].enabled = true;
                heartImages[i].gameObject.SetActive(true);
            }
            else
            {
                // Extra lives: toggle entire object visibility
                heartImages[i].gameObject.SetActive(i < lives);
            }
        }
    }
}
