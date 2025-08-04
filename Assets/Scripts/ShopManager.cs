// ShopManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour
{
    // --- EVENTS ---
    public static event Action OnShopStateChanged;
    public static event Action<PlantSkinData> OnSkinEquipped;
    [SerializeField] private ConfirmationPanelUI confirmationPanel;

    private PlantSkinData pendingPurchaseItem;

    // --- PUBLIC METHODS (called by ShopItemUI) ---
    public void AttemptPurchaseOrEquip(PlantSkinData skin)
    {
        if (skin == null) return;

        bool isUnlocked = PlayerPrefsManager.Instance.IsSkinUnlocked(skin.skinName);

        if (isUnlocked)
        {
            EquipSkin(skin);
            OnShopStateChanged?.Invoke();
        }
        else
        {
            RequestPurchaseConfirmation(skin);
        }

       
        
    }

    private void RequestPurchaseConfirmation(PlantSkinData skin)
    {
        if (skin == null) return;

        pendingPurchaseItem = skin;
        string message = $"Purchase {skin.skinName} for {skin.price} coins?";
        
        
        confirmationPanel.Show(message, HandlePurchaseConfirmation);
    }

    
    private void HandlePurchaseConfirmation(bool wasConfirmed)
    {
        if (wasConfirmed && pendingPurchaseItem != null)
        {
           
            TryPurchaseSkin(pendingPurchaseItem);
        }

        
        pendingPurchaseItem = null;

        
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