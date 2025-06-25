using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class ShopButton : MonoBehaviour
{
    // Enum to define what kind of item this button represents
    public enum ShopItemType { PlantSkin, Soundtrack }

    [Header("Item Configuration")]
    [Tooltip("Select the type of item this button sells.")]
    [SerializeField] private ShopItemType itemType;

    [Tooltip("Assign the PlantSkinData asset here if Item Type is PlantSkin.")]
    [SerializeField] private PlantSkinData skinData;
    
    // [Tooltip("Assign the SoundtrackData asset here if Item Type is Soundtrack.")]
    // [SerializeField] private SoundtrackData soundtrackData; // Example for the future

    [Header("Scene References")]
    [Tooltip("Assign the MenuUIManager from your scene.")]
    [SerializeField] private MenuUIManager menuUIManager;
    [Tooltip("Assign the PlantSkinApplier from your scene.")]
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
        RefreshButtonState();
    }

    public void RefreshButtonState()
    {
        bool isUnlocked = false;
        bool isEquipped = false;
        
        // Configure button based on item type
        switch (itemType)
        {
            case ShopItemType.PlantSkin:
                if (skinData == null) return;
                itemID = skinData.skinName;
                itemPrice = skinData.price;
                isUnlocked = PlayerPrefsManager.Instance.IsSkinUnlocked(itemID);
                isEquipped = PlayerPrefs.GetString("CurrentPlantType", "DefaultSkin") == itemID;
                break;

            case ShopItemType.Soundtrack:
                // Future logic for soundtracks would go here.
                // For example:
                // itemID = soundtrackData.soundtrackName;
                // itemPrice = soundtrackData.price;
                // isUnlocked = PlayerPrefsManager.Instance.IsSoundtrackUnlocked(itemID);
                // isEquipped = PlayerPrefsManager.Instance.GetEquippedSoundtrackForBiome("someBiome") == itemID;
                break;
        }

        // Update UI visuals
        if (isUnlocked)
        {
            lockedStateObject.SetActive(false);
            button.interactable = true;
            equippedStateObject.SetActive(isEquipped);
        }
        else // Item is locked
        {
            lockedStateObject.SetActive(true);
            equippedStateObject.SetActive(false);
            priceText.text = itemPrice.ToString();
            
            int playerCoins = PlayerPrefsManager.Instance.LoadMoney();
            button.interactable = playerCoins >= itemPrice;
        }
    }

    public void OnButtonClick()
    {
        // The logic inside the switch block handles the specific action
        switch (itemType)
        {
            case ShopItemType.PlantSkin:
                HandleSkinPurchaseOrEquip();
                break;
            case ShopItemType.Soundtrack:
                // HandleSoundtrackPurchaseOrEquip();
                break;
        }
        
        // Refresh all buttons in the shop to show updated states (e.g., new coin total, equipped status)
        // FindObjectOfType<ShopUIContainer>()?.RefreshAllButtons(); // We can use a simple container
    }

    private void HandleSkinPurchaseOrEquip()
    {
        bool isUnlocked = PlayerPrefsManager.Instance.IsSkinUnlocked(skinData.skinName);

        if (isUnlocked)
        {
            // Equip the skin
            plantSkinApplier.SetPlantType(skinData.skinName);
        }
        else
        {
            // Buy the skin
            int playerCoins = PlayerPrefsManager.Instance.LoadMoney();
            if (playerCoins >= skinData.price)
            {
                PlayerPrefsManager.Instance.SaveMoney(playerCoins - skinData.price);
                PlayerPrefsManager.Instance.SaveSkinUnlocked(skinData.skinName, true);
                menuUIManager.UpdateDisplay(); // Refresh coin display
            }
        }
        RefreshAllShopButtons();
    }

    private void RefreshAllShopButtons()
    {
        // This simple approach finds all other ShopButton instances and tells them to refresh.
        ShopButton[] allButtons = FindObjectsOfType<ShopButton>();
        foreach (var btn in allButtons)
        {
            btn.RefreshButtonState();
        }
    }
}