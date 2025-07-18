// ShopManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    // --- EVENTS ---
    // ShopItemUI and MenuUIManager will subscribe to this to refresh their visuals.
    public static event Action OnShopStateChanged;
    // PlantSkinApplier will subscribe to this to update the preview model.
    public static event Action<PlantSkinData> OnSkinEquipped;

    // --- PUBLIC METHODS (called by ShopItemUI) ---

    /// <summary>
    /// This is the main entry point for a button press. It handles both
    /// purchasing a locked item or equipping an unlocked one.
    /// </summary>
    public void AttemptPurchaseOrEquip(PlantSkinData skin)
    {
        if (skin == null) return;

        bool isUnlocked = PlayerPrefsManager.Instance.IsSkinUnlocked(skin.skinName);

        if (isUnlocked)
        {
            EquipSkin(skin);
        }
        else
        {
            TryPurchaseSkin(skin);
        }

        // After any action, notify all listeners that the shop state has changed
        // so they can update their visuals (coin display, button states, etc).
        OnShopStateChanged?.Invoke();
    }

    // --- PRIVATE LOGIC ---

    private void EquipSkin(PlantSkinData skin)
    {
        // Save the equipped skin choice.
        PlayerPrefs.SetString("CurrentPlantType", skin.skinName);
        PlayerPrefs.Save();
        Debug.Log($"[ShopManager] Equipped Skin: {skin.skinName}");

        // Announce that a new skin has been equipped so the preview model can update.
        OnSkinEquipped?.Invoke(skin);
    }

    private void TryPurchaseSkin(PlantSkinData skin)
    {
        int playerCoins = PlayerPrefsManager.Instance.LoadMoney();

        if (playerCoins >= skin.price)
        {
            // Deduct cost and save
            PlayerPrefsManager.Instance.SaveMoney(playerCoins - skin.price);
            
            // Unlock the skin
            PlayerPrefsManager.Instance.SaveSkinUnlocked(skin.skinName, true);
            Debug.Log($"[ShopManager] Successfully purchased {skin.skinName} for {skin.price} coins.");

            // Automatically equip the skin after purchase.
            EquipSkin(skin);
        }
        else
        {
            Debug.Log($"[ShopManager] Not enough coins to purchase {skin.skinName}.");
            // Optionally, you could invoke a separate event here to show a "Not Enough Coins" message.
            // public static event Action OnPurchaseFailed;
        }
    }
}