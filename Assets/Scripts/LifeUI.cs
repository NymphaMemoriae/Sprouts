using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LifeUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Sprite extraHeartSprite;


    [Header("References")]
    [SerializeField] private PlantLife plantLife;

    private List<Image> heartImages = new();
    private int initialMaxLives; 

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            Image img = child.GetComponent<Image>();
            if (img != null)
                heartImages.Add(img);
        }

        heartImages.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
    }

    private void Start()
    {
        if (plantLife != null)
        {
            plantLife.OnLivesChanged += UpdateHeartsUI;
            initialMaxLives = plantLife.CurrentLives;
            UpdateHeartsUI(plantLife.CurrentLives); // Initial setup
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
            // This heart is within the skin's original maximum life count
            if (i < initialMaxLives) 
            {
                heartImages[i].sprite = (i < lives) ? fullHeartSprite : emptyHeartSprite;
                heartImages[i].gameObject.SetActive(true);
            }
            // This heart is an extra life collected in-game
            else 
            {
                heartImages[i].sprite = extraHeartSprite;
                heartImages[i].gameObject.SetActive(i < lives);
            }
        }
    }
}
