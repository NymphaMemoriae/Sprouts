using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewPlantSkin", menuName = "Plant Game/Plant Skin Data")]
public class PlantSkinData : ScriptableObject
{
    [Header("Skin Identification")]
    public string skinName = "DefaultSkin"; // Ensure this matches the filename for easy lookup

    [Header("Shop Information")]
    [Tooltip("The cost of the skin in the shop.")]
    public int price = 100;

    [Header("Plant Head Visuals")]
    public Sprite plantHeadSprite;
    public RuntimeAnimatorController plantHeadAnimatorController;

    [Header("Gameplay Stats")]
    public int startingLives = 3;
    public float maxVelocity = 7f;

    [Header("Stem Visuals")]
    public Material stemMaterial;
    
    [Header("Leaf Visuals")] 
    public GameObject leafPrefab;
}