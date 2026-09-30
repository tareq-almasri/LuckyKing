using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LuckyCardDisplay : MonoBehaviour
{
    [Header("Card Visual Elements")]
    public Image cardBackground;
    public Image cardArtwork;           // Card image (sprite)
    public TMP_Text cardNameText;       // Card name (TMP)
    public TMP_Text costText;          // Cost (TMP)

    [Header("Card Info Elements")]
    public Image levelSprite;           // Level (sprite) - will show level icons
    public TMP_Text tierText;           // Tier (TMP) - will show B/A/S
    public Image cardTypeSprite;        // Card type (sprite) - Infantry/Cavalry/etc icons

    [Header("Unit Card Stats")]
    public GameObject unitCardPanel;
    public TMP_Text attackText;         // Atk (TMP)
    public TMP_Text countText;          // Count (TMP)
    public TMP_Text defenseText;        // Def (TMP)

    [Header("Effect Card Elements")]
    public GameObject effectCardPanel;
    public TMP_Text effectDescriptionText;

    [Header("Sprite Arrays for Dynamic Display")]
    public Sprite[] levelSprites;       // Array of 5 sprites for levels 1-5
    public Sprite[] cardTypeSprites;    // Array of 6 sprites for card types
    public string[] tierLabels = { "B", "A", "S" }; // Tier labels

    [Header("Card Data")]
    public Card cardData;

    // Card type colors for Lucky game
    private readonly Color[] cardTypeColors = {
        new(0.6f, 0.25f, 0.15f),  // Infantry - Brown #CC6633
        new(0.1f, 0.4f, 0.7f),  // Cavalry - Blue #3380CC
        new(0.3f, 0.3f, 0.3f),  // SiegeEngine - Gray #666666
        new(0.15f, 0.35f, 0.15f),  // Buff - Green #1A661A
        new(0.55f, 0.15f, 0.15f),  // Debuff - Red #991F1F
        new(0.3f, 0.3f, 0.7f)   // Logistics - Purple #6666CC
    };

    private readonly Color[] tierColors = {
        new(0.3f, 0.4f, 0.8f),  // B - Blue #4D66CC
        new(0.6f, 0.3f, 0.95f),  // A - Purple #994DF2
        new(0.9f, 0.8f, 0.1f)   // S - Gold #E6CC1A
    };

    void Start()
    {
        if (cardData != null)
        {
            UpdateCardDisplay();
        }
    }

    // This method is called in the editor when a value changes
    private void OnValidate()
    {
        if (cardData != null)
        {
            UpdateCardDisplay();
        }
    }

    public void UpdateCardDisplay()
    {
        if (cardData == null) return;
        levelSprite.gameObject.SetActive(false);
        // Update basic card info
        if (cardNameText != null) cardNameText.text = cardData.cardName;
        if (costText != null) costText.text = cardData.cost.ToString();
        if (cardArtwork != null && cardData.artwork != null) cardArtwork.sprite = cardData.artwork;

        // Update level sprite (levels 1-5)
        if (levelSprite != null && levelSprites != null && levelSprites.Length >= cardData.level && cardData.IsUnitCard)
        {
            levelSprite.gameObject.SetActive(true);
            levelSprite.sprite = levelSprites[cardData.level - 1]; // Array is 0-indexed
        }

        // Update tier text (B/A/S)
        if (tierText != null && tierLabels != null && cardData.tier <= tierLabels.Length)
        {
            tierText.text = tierLabels[cardData.tier - 1]; // Array is 0-indexed
            tierText.color = tierColors[cardData.tier - 1]; // Set color based on tier
        }

        // Update card type sprite
        if (cardTypeSprite != null && cardTypeSprites != null && cardTypeSprites.Length > (int)cardData.cardType)
        {
            cardTypeSprite.sprite = cardTypeSprites[(int)cardData.cardType];
        }

        // Set card background color based on type
        //if (cardBackground != null)
        //{
        //  cardBackground.color = cardTypeColors[(int)cardData.cardType];
        //}

        if (cardBackground != null && cardBackground.material != null)
        {
            // Only clone if not already an instance
            if (!cardBackground.material.name.EndsWith("(Instance)"))
            {
                cardBackground.material = new Material(cardBackground.material);
            }
            cardBackground.material.color = cardTypeColors[(int)cardData.cardType]; // or .SetColor("_Color", newColor) for custom shaders
        }

        // Show/hide panels based on card type
        if (cardData.IsUnitCard)
        {
            SetupUnitCard();
        }
        else if (cardData.IsEffectCard)
        {
            SetupEffectCard();
        }
    }

    private void SetupUnitCard()
    {
        cardData.SetUnitStats();
        // Show unit panel, hide effect panel
        if (unitCardPanel != null) unitCardPanel.SetActive(true);
        if (effectCardPanel != null) effectCardPanel.SetActive(false);

        int atkMultiplier = cardData.ATK;
        int defMultiplier = cardData.DEF;
        int countMultiplier = cardData.COUNT;

        if (attackText != null && atkMultiplier > 0)
        {
            attackText.text = atkMultiplier.ToString();
            attackText.fontSize = 0.4f;
            attackText.fontStyle = FontStyles.Bold;
        }
        else if (attackText != null)
        {
            attackText.text = "ATK";
            attackText.fontSize = 0.26f;
            attackText.fontStyle = FontStyles.Normal;
        }
        if (defenseText != null && defMultiplier > 0)
        {
            defenseText.text = defMultiplier.ToString();
            defenseText.fontSize = 0.4f;
            defenseText.fontStyle = FontStyles.Bold;
        }
        else if (defenseText != null)
        {
            defenseText.text = "DEF";
            defenseText.fontSize = 0.26f;
            defenseText.fontStyle = FontStyles.Normal;
        }
        if (countText != null && countMultiplier > 0)
        {
            countText.text = countMultiplier.ToString();
            countText.fontSize = 0.4f; // Adjust font size for better visibility
        }
        else if (countText != null)
        {
            countText.text = "COUNT";
            countText.fontSize = 0.21f; // Adjust font size for better visibility
        }
    }

    private void SetupEffectCard()
    {
        // Show effect panel, hide unit panel
        if (effectCardPanel != null) effectCardPanel.SetActive(true);
        if (unitCardPanel != null) unitCardPanel.SetActive(false);

        // Update effect description
        if (effectDescriptionText != null) effectDescriptionText.text = cardData.effect;
    }

    // Method to get the card data (for compatibility with drag-drop system)
    public Card GetCard()
    {
        return cardData;
    }

    // Method to set card data
    public void SetCard(Card newCard)
    {
        cardData = newCard;
        if (cardData != null)
        {
            UpdateCardDisplay();
        }
    }
}
