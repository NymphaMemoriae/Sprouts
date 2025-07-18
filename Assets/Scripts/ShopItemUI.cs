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
        Debug.Log($"[ShopItemUI] Button pressed for skin: {skinData.skinName}");
        ShopManager.OnShopStateChanged += RefreshVisuals;
        
    }

    /// <summary>
    /// Reads the global state from PlayerPrefs and updates just this button's visuals.
    /// </summary>
    private void RefreshVisuals()
    {
        if (skinData == null) return;

        bool isUnlocked = PlayerPrefsManager.Instance.IsSkinUnlocked(skinData.skinName);
        string equippedSkinName = PlayerPrefs.GetString("CurrentPlantType", "DefaultSkin");
        bool isEquipped = (equippedSkinName == skinData.skinName);

        if (isUnlocked)
        {
            lockedStateObject.SetActive(false);
            equippedStateObject.SetActive(isEquipped);
            button.interactable = true;
            Debug.Log($"[ShopItemUI] Skin '{skinData.skinName}' is unlocked. Equipped: {isEquipped}");
        }
        else // Locked
        {
            int playerCoins = PlayerPrefsManager.Instance.LoadMoney();
            lockedStateObject.SetActive(true);
            equippedStateObject.SetActive(false);
            priceText.text = skinData.price.ToString();
            // Button is interactable only if the player can afford it.
            button.interactable = playerCoins >= skinData.price;
            Debug.Log($"[ShopItemUI] Skin '{skinData.skinName}' is locked. Price: {skinData.price}, Player Coins: {playerCoins}");
        }
    }
}