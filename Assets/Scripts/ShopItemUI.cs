// ShopItemUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ShopItemUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlantSkinData skinData;

    [Header("Component References")]
    [SerializeField] private ShopManager shopManager; // Assign the scene's ShopManager
    [SerializeField] private Button button;

    [Header("UI State Objects")]
    [SerializeField] private GameObject lockedStateObject;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private GameObject equippedStateObject;

    private void OnEnable()
    {
        // Subscribe to the event. When the shop state changes, RefreshVisuals will be called.
        ShopManager.OnShopStateChanged += RefreshVisuals;
        RefreshVisuals(); // Also refresh once on enable
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors when the object is disabled/destroyed.
        ShopManager.OnShopStateChanged -= RefreshVisuals;
    }

    /// <summary>
    /// This method is assigned to the button's OnClick() event in the Inspector.
    /// </summary>
    public void OnButtonPressed()
    {
        shopManager.AttemptPurchaseOrEquip(skinData);
       
        // ShopManager.OnShopStateChanged += RefreshVisuals;
        
    }

   /// <summary>
/// Reads the global state and updates this button's visuals in a clean, sequential manner.
/// </summary>
private void RefreshVisuals()
    {
        if (skinData == null) return;

       
        bool isUnlocked = PlayerPrefsManager.Instance.IsSkinUnlocked(skinData.skinName);
     
        bool isEquipped = isUnlocked && (PlayerPrefs.GetString("CurrentPlantType", "DefaultSkin") == skinData.skinName);

        
        if (lockedStateObject != null)
        {
            lockedStateObject.SetActive(!isUnlocked);
        }
        if (equippedStateObject != null)
        {
            equippedStateObject.SetActive(isEquipped);
        }


        if (priceText != null)
        {
            priceText.gameObject.SetActive(!isUnlocked);
            if (!isUnlocked)
            {
                priceText.text = skinData.price.ToString();
            }
        }

      
        int playerCoins = PlayerPrefsManager.Instance.LoadMoney();
        button.interactable = isUnlocked || (playerCoins >= skinData.price);
    }
}