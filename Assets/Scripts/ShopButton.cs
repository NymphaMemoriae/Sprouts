using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ShopButton : MonoBehaviour
{
    [Header("Item Configuration")]
    [SerializeField] private PlantSkinData skinData;

    [Header("Scene References")]
    [SerializeField] private MenuUIManager menuUIManager;
    [SerializeField] private PlantSkinApplier plantSkinApplier;

    [Header("Button UI Elements")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject lockedStateObject;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private GameObject equippedStateObject;

    private string itemID;
    private int itemPrice;

    private void OnEnable()
    {
        // OnEnable is a good place to initialize and refresh
        if (skinData == null)
        {
            Debug.LogError($"[ShopButton] CRITICAL ERROR on {gameObject.name}: The 'Skin Data' asset is NOT ASSIGNED in the Inspector!", gameObject);
            return;
        }

        itemID = skinData.skinName;
        itemPrice = skinData.price;
        Debug.Log($"[ShopButton] OnEnable: Button '{gameObject.name}' is for item '{itemID}' with price '{itemPrice}'.", gameObject);
        RefreshButtonState();
    }

    public void OnButtonClick()
    {
        Debug.LogWarning($"\n--- CLICK DETECTED on button '{gameObject.name}' for skin '{itemID}' ---");
        HandleSkinPurchaseOrEquip();
    }

    private void HandleSkinPurchaseOrEquip()
    {
        bool isUnlocked = itemID == "DefaultSkin" || PlayerPrefsManager.Instance.IsSkinUnlocked(itemID);
        Debug.Log($"[ShopButton - Handle] 1. Is '{itemID}' unlocked? -> {isUnlocked}");

        if (isUnlocked)
        {
            Debug.Log($"[ShopButton - Handle] 2. Path: EQUIP. Equipping skin '{itemID}'.");
            plantSkinApplier.SetPlantType(itemID);
        }
        else
        {
            Debug.Log($"[ShopButton - Handle] 2. Path: PURCHASE. Checking if player can afford it.");
            int playerCoins = PlayerPrefsManager.Instance.LoadMoney();
            Debug.Log($"[ShopButton - Handle] 3. Player has {playerCoins} coins. Item costs {itemPrice}.");

            if (playerCoins >= itemPrice)
            {
                Debug.Log($"[ShopButton - Handle] 4. Player CAN afford it. Proceeding with purchase.");
                //----_____----_____----_____----
                // MODIFICATION: ADDED LOGS AND THE EQUIP CALL AFTER PURCHASE
                //----_____----_____----_____----
                PlayerPrefsManager.Instance.SaveMoney(playerCoins - itemPrice);
                PlayerPrefsManager.Instance.SaveSkinUnlocked(itemID, true);
                Debug.Log($"[ShopButton - Handle] 5. PURCHASE COMPLETE. Unlocked '{itemID}' and saved new coin balance.");
                
                // EQUIP THE ITEM IMMEDIATELY AFTER BUYING IT
                plantSkinApplier.SetPlantType(itemID); 
                Debug.Log($"[ShopButton - Handle] 6. AUTO-EQUIPPING '{itemID}' right after purchase.");
            }
            else
            {
                Debug.Log($"[ShopButton - Handle] 4. Player CANNOT afford it. Purchase blocked.");
            }
        }

        Debug.LogWarning($"--- REFRESHING ALL BUTTONS after click on '{itemID}' ---");
        RefreshAllShopButtons();
    }
    
    public void RefreshButtonState()
    {
        if (string.IsNullOrEmpty(itemID)) return; // Don't refresh if not initialized

        Debug.Log($"[ShopButton - Refresh] Refreshing visuals for '{itemID}':");

        // Step A: Determine state
        bool isUnlocked = itemID == "DefaultSkin" || PlayerPrefsManager.Instance.IsSkinUnlocked(itemID);
        string equippedSkin = PlayerPrefs.GetString("CurrentPlantType", "DefaultSkin");
        bool isEquipped = equippedSkin == itemID;
        int playerCoins = PlayerPrefsManager.Instance.LoadMoney();

        Debug.Log($"[ShopButton - Refresh]   - IsUnlocked check: {isUnlocked}");
        Debug.Log($"[ShopButton - Refresh]   - IsEquipped check: {isEquipped} (Currently equipped: '{equippedSkin}')");
        Debug.Log($"[ShopButton - Refresh]   - Player coins: {playerCoins}");

        // Step B: Apply visuals based on state
        if (isUnlocked)
        {
            lockedStateObject.SetActive(false);
            equippedStateObject.SetActive(isEquipped);
            button.interactable = true;
            Debug.Log($"[ShopButton - Refresh]   - RESULT: Set '{itemID}' to UNLOCKED state. Equipped: {isEquipped}.");
        }
        else // Item is locked
        {
            lockedStateObject.SetActive(true);
            equippedStateObject.SetActive(false);
            priceText.text = itemPrice.ToString();
            button.interactable = playerCoins >= itemPrice;
            Debug.Log($"[ShopButton - Refresh]   - RESULT: Set '{itemID}' to LOCKED state. Button interactable: {button.interactable}.");
        }
    }

    private void RefreshAllShopButtons()
    {
        // This simple approach finds all other ShopButton instances and tells them to refresh.
        ShopButton[] allButtons = FindObjectsOfType<ShopButton>();
        foreach (var btn in allButtons)
        {
            btn.RefreshButtonState();
        }

        // Also refresh the main coin display
        if (menuUIManager != null)
        {
            menuUIManager.UpdateDisplay();
        }
    }
}