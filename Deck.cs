using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

 [CreateAssetMenu(fileName = "New Deck", menuName = "Lucky/Deck")]
public class Deck : ScriptableObject
{
    // Reset all card multipliers in this deck
    public void ResetAllCardMultipliers()
    {
        foreach (var card in cards)
        {
            if (card != null)
            {
                card.ATK = 0;
                card.DEF = 0;
                card.COUNT = 0;
            }
        }
    }
    [Header("Deck Identity")]
    [SerializeField] private string deckName = "New Deck";
    [SerializeField] private string deckID = "";
    
    [Header("Deck Configuration")]
    [SerializeField] private List<Card> cards = new();
    
    [Header("Deck Validation")]
    [SerializeField] private bool isValid = false;
    [SerializeField] private List<string> validationErrors = new();
    
    // Constants
    public const int DECK_SIZE = 30;
    public const int MAX_COPIES_PER_CARD = 3;
    
    // Properties
    public string DeckName 
    { 
        get => deckName; 
        set => deckName = value; 
    }
    public string DeckID 
    { 
        get => deckID; 
        set => deckID = value; 
    }
    public List<Card> Cards => cards;
    public int CardCount => cards.Count;
    public bool IsValid => isValid;
    public List<string> ValidationErrors => validationErrors;
    
    // Add card to deck
    public bool AddCard(Card card)
    {
        if (card == null) return false;
        
        // Check if adding this card would exceed deck size
        if (cards.Count >= DECK_SIZE)
        {
            Debug.LogWarning("Cannot add card: Deck is already at maximum size (30 cards)");
            return false;
        }
        
        // Check if adding this card would exceed the per-card limit
        int currentCount = cards.Count(c => c == card);
        if (currentCount >= MAX_COPIES_PER_CARD)
        {
            Debug.LogWarning($"Cannot add {card.cardName}: Already have {currentCount}/{MAX_COPIES_PER_CARD} copies");
            return false;
        }
        
        cards.Add(card);
        Debug.Log($"Added {card.cardName} to deck. Total cards: {cards.Count}");
        ValidateDeck();
        return true;
    }
    
    // Auto-fill deck with available cards
    [ContextMenu("Auto Fill Deck")]
    public void AutoFillDeck()
    {
        Debug.Log("🔧 Auto-filling deck with all available cards...");
        
        // Clear existing cards
        cards.Clear();
        
        // Find all Card ScriptableObjects in the project
        Card[] allCards = Resources.FindObjectsOfTypeAll<Card>();
        
        if (allCards.Length == 0)
        {
            Debug.LogWarning("❌ No Card assets found in project!");
            return;
        }
        
        Debug.Log($"🔧 Found {allCards.Length} card assets");
        
        // Add cards up to deck limit, respecting per-card limits
        foreach (Card card in allCards)
        {
            if (cards.Count >= DECK_SIZE) break;
            
            // Add up to MAX_COPIES_PER_CARD of each card
            for (int i = 0; i < MAX_COPIES_PER_CARD && cards.Count < DECK_SIZE; i++)
            {
                cards.Add(card);
            }
        }
        
        Debug.Log($"✅ Auto-filled deck with {cards.Count} cards");
        ValidateDeck();
        
        // Mark as dirty for editor
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    // Smart auto-fill with balanced distribution
    [ContextMenu("Smart Fill Deck")]
    public void SmartFillDeck()
    {
        Debug.Log("🧠 Smart-filling deck with balanced card distribution...");
        
        // Clear existing cards
        cards.Clear();
        
        // Find all Card ScriptableObjects
        Card[] allCards = Resources.FindObjectsOfTypeAll<Card>();
        
        if (allCards.Length == 0)
        {
            Debug.LogWarning("❌ No Card assets found in project!");
            return;
        }
        
        // Separate cards by type for balanced distribution
        var unitCards = allCards.Where(c => c.IsUnitCard).ToArray();
        var effectCards = allCards.Where(c => c.IsEffectCard).ToArray();
        
        Debug.Log($"🔧 Found {unitCards.Length} unit cards, {effectCards.Length} effect cards");
        
        // Fill with roughly 70% units, 30% effects
        int targetUnitCards = (int)(DECK_SIZE * 0.7f);
        int targetEffectCards = DECK_SIZE - targetUnitCards;
        
        // Add unit cards
        AddCardsOfType(unitCards, targetUnitCards);
        
        // Add effect cards
        AddCardsOfType(effectCards, targetEffectCards);
        
        // Fill remaining slots with any available cards
        while (cards.Count < DECK_SIZE && allCards.Length > 0)
        {
            foreach (Card card in allCards)
            {
                if (cards.Count >= DECK_SIZE) break;
                
                int currentCount = cards.Count(c => c == card);
                if (currentCount < MAX_COPIES_PER_CARD)
                {
                    cards.Add(card);
                }
            }
            break; // Avoid infinite loop
        }
        
        Debug.Log($"✅ Smart-filled deck with {cards.Count} cards ({cards.Count(c => c.IsUnitCard)} units, {cards.Count(c => c.IsEffectCard)} effects)");
        ValidateDeck();
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    private void AddCardsOfType(Card[] cardArray, int targetCount)
    {
        if (cardArray.Length == 0) return;
        
        int added = 0;
        int cardIndex = 0;
        int copyCount = 0;
        
        while (added < targetCount && cards.Count < DECK_SIZE)
        {
            if (cardIndex >= cardArray.Length)
            {
                cardIndex = 0;
                copyCount++;
                
                if (copyCount >= MAX_COPIES_PER_CARD) break;
            }
            
            cards.Add(cardArray[cardIndex]);
            added++;
            cardIndex++;
        }
    }
    
    // Clear all cards from deck
    [ContextMenu("Clear Deck")]
    public void ClearDeck()
    {
        Debug.Log($"🗑️ Clearing deck '{deckName}' - removing {cards.Count} cards");
        cards.Clear();
        ValidateDeck();
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    // Remove card from deck
    public bool RemoveCard(Card card)
    {
        if (card == null) return false;
        
        bool removed = cards.Remove(card);
        if (removed)
        {
            ValidateDeck();
        }
        return removed;
    }
    
    // Remove card by index
    public bool RemoveCardAt(int index)
    {
        if (index < 0 || index >= cards.Count) return false;
        
        cards.RemoveAt(index);
        ValidateDeck();
        return true;
    }
    
    // Get count of specific card in deck
    public int GetCardCount(Card card)
    {
        if (card == null) return 0;
        return cards.Count(c => c == card);
    }
    
    // Get all unique cards with their counts
    public Dictionary<Card, int> GetCardCounts()
    {
        var cardCounts = new Dictionary<Card, int>();
        
        foreach (var card in cards)
        {
            if (card != null)
            {
                if (cardCounts.ContainsKey(card))
                    cardCounts[card]++;
                else
                    cardCounts[card] = 1;
            }
        }
        
        return cardCounts;
    }
    
    // Validate deck according to rules
    public bool ValidateDeck()
    {
        validationErrors.Clear();
        
        // Check deck identity
        if (string.IsNullOrEmpty(deckName.Trim()))
        {
            validationErrors.Add("Deck must have a name");
        }
        
        if (string.IsNullOrEmpty(deckID.Trim()))
        {
            validationErrors.Add("Deck must have an ID");
        }
        
        // Check deck size
        if (cards.Count != DECK_SIZE)
        {
            validationErrors.Add($"Deck must contain exactly {DECK_SIZE} cards (currently has {cards.Count})");
        }
        
        // Check for null cards
        int nullCards = cards.Count(c => c == null);
        if (nullCards > 0)
        {
            validationErrors.Add($"Deck contains {nullCards} empty card slots");
        }
        
        // Check card copy limits
        var cardCounts = GetCardCounts();
        foreach (var kvp in cardCounts)
        {
            if (kvp.Value > MAX_COPIES_PER_CARD)
            {
                validationErrors.Add($"{kvp.Key.cardName}: {kvp.Value} copies (maximum {MAX_COPIES_PER_CARD} allowed)");
            }
        }
        
        isValid = validationErrors.Count == 0;
        return isValid;
    }
    
    // Shuffle deck (for gameplay)
    public List<Card> GetShuffledDeck()
    {
        Debug.Log($"🔧 GetShuffledDeck called on {name} - Original cards count: {cards.Count}");
        
        if (cards.Count == 0)
        {
            Debug.LogError($"❌ Deck {name} has no cards to shuffle!");
            return new List<Card>();
        }
        
        var shuffledCards = new List<Card>(cards);
        
        // Fisher-Yates shuffle
        for (int i = shuffledCards.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Card temp = shuffledCards[i];
            shuffledCards[i] = shuffledCards[randomIndex];
            shuffledCards[randomIndex] = temp;
        }
        
        Debug.Log($"🔧 Shuffled deck created with {shuffledCards.Count} cards");
        return shuffledCards;
    }
    
    // Get deck statistics
    public DeckStats GetDeckStats()
    {
        var stats = new DeckStats();
        var cardCounts = GetCardCounts();
        
        stats.deckName = deckName;
        stats.deckID = deckID;
        stats.totalCards = cards.Count;
        stats.uniqueCards = cardCounts.Count;
        stats.infantryCount = cards.Count(c => c != null && c.cardType == CardType.Infantry);
        stats.cavalryCount = cards.Count(c => c != null && c.cardType == CardType.Cavalry);
        stats.siegeEngineCount = cards.Count(c => c != null && c.cardType == CardType.SiegeEngine);
        stats.buffCount = cards.Count(c => c != null && c.cardType == CardType.Buff);
        stats.debuffCount = cards.Count(c => c != null && c.cardType == CardType.Debuff);
        stats.logisticsCount = cards.Count(c => c != null && c.cardType == CardType.Logistics);
        stats.averageCost = (float)cards.Where(c => c != null).Average(c => c.cost);
        
        return stats;
    }
    
    // Generate a unique ID for this deck
    [ContextMenu("Generate New ID")]
    public void GenerateNewID()
    {
        deckID = System.Guid.NewGuid().ToString("N")[..8]; // Short 8-character ID
        ValidateDeck();
    }
    
    // Set deck identity
    public void SetDeckIdentity(string name, string id = null)
    {
        deckName = name;
        if (!string.IsNullOrEmpty(id))
        {
            deckID = id;
        }
        else if (string.IsNullOrEmpty(deckID))
        {
            GenerateNewID();
        }
        ValidateDeck();
    }
    
    // Get deck display name (name + ID)
    public string GetDisplayName()
    {
        return $"{deckName} ({deckID})";
    }
}

[System.Serializable]
public class DeckStats
{
    public string deckName;
    public string deckID;
    public int totalCards;
    public int uniqueCards;
    public int infantryCount;
    public int cavalryCount;
    public int siegeEngineCount;
    public int buffCount;
    public int debuffCount;
    public int logisticsCount;
    public float averageCost;
}
