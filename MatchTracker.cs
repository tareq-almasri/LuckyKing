using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[System.Serializable]
public class MatchTracker : MonoBehaviour
{
    public static MatchTracker Instance { get; private set; }
    public enum GameMode { Campaign, VsAI, Casual, Ranked }
    public GameMode currentGameMode;
    public bool IsPvP => currentGameMode == GameMode.Casual || currentGameMode == GameMode.Ranked;
    public int localPlayerID; // 1 or 2
    public Card assetCard0;
    public Card assetCard1;
    public Card assetCard2;
    public Card assetCard3;
    public Card assetCard4;
    public Card assetCard5;
    public List<Card> deployedCards = new();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Initialize sample cards
       
           Card card1 = Instantiate(assetCard0);
           card1.slotIndex = 7;
           card1.playerID = 1;
           card1.SetUnitStats();
           Card card2 = Instantiate(assetCard0);
           card2.slotIndex = 9;
           card2.playerID = 1;
           card2.SetUnitStats();
           Card card3 = Instantiate(assetCard1);
           card3.slotIndex = 10;
           card3.playerID = 1;
           card3.SetUnitStats();
           Card card4 = Instantiate(assetCard1);
           card4.slotIndex = 4;
           card4.playerID = 1;
           card4.SetUnitStats();
           Card card5 = Instantiate(assetCard2);
           card5.slotIndex = 3;
           card5.playerID = 1;
           card5.SetUnitStats();
           Card card6 = Instantiate(assetCard2);
           card6.slotIndex = 5;
           card6.playerID = 1;
           card6.SetUnitStats();
           Card card7 = Instantiate(assetCard3);
           card7.slotIndex = 1;
           card7.playerID = 1;
           card7.SetUnitStats();
           Card card8 = Instantiate(assetCard3);
           card8.slotIndex = 2;
           card8.playerID = 1;
           card8.SetUnitStats();
           Card card9 = Instantiate(assetCard4);
           card9.slotIndex = 6;
           card9.playerID = 1;
           card9.SetUnitStats();
           Card card10 = Instantiate(assetCard5);
           card10.slotIndex = 8;
           card10.playerID = 1;
           card10.SetUnitStats();
           Card card11 = Instantiate(assetCard0);
           card11.slotIndex = 7;
           card11.playerID = 2;
           card11.SetUnitStats();
           Card card12 = Instantiate(assetCard0);
           card12.slotIndex = 9;
           card12.playerID = 2;
           card12.SetUnitStats();
           Card card13 = Instantiate(assetCard1);
           card13.slotIndex = 10;
           card13.playerID = 2;
           card13.SetUnitStats();
           Card card14 = Instantiate(assetCard1);
           card14.slotIndex = 4;
           card14.playerID = 2;
           card14.SetUnitStats();
           Card card15 = Instantiate(assetCard2);
           card15.slotIndex = 3;
           card15.playerID = 2;
           card15.SetUnitStats();
           Card card16 = Instantiate(assetCard2);
           card16.slotIndex = 5;
           card16.playerID = 2;
           card16.SetUnitStats();
           Card card17 = Instantiate(assetCard3);
           card17.slotIndex = 1;
           card17.playerID = 2;
           card17.SetUnitStats();
           Card card18 = Instantiate(assetCard3);
           card18.slotIndex = 2;
           card18.playerID = 2;
           card18.SetUnitStats();
           Card card19 = Instantiate(assetCard4);
           card19.slotIndex = 6;
           card19.playerID = 2;
           card19.SetUnitStats();
           Card card20 = Instantiate(assetCard5);
           card20.slotIndex = 8;
           card20.playerID = 2;
           card20.SetUnitStats();
           deployedCards.Add(card1);
           deployedCards.Add(card2);
           deployedCards.Add(card3);
           deployedCards.Add(card4);
           deployedCards.Add(card5);
           deployedCards.Add(card6);
           deployedCards.Add(card7);
           deployedCards.Add(card8);
           deployedCards.Add(card9);
           deployedCards.Add(card10);
           deployedCards.Add(card11);
           deployedCards.Add(card12);
           deployedCards.Add(card13);
           deployedCards.Add(card14);
           deployedCards.Add(card15);
           deployedCards.Add(card16);
           deployedCards.Add(card17);
           deployedCards.Add(card18);
           deployedCards.Add(card19);
           deployedCards.Add(card20);
      
    }

    // Call this to save a card when deployed
    public void AddDeployedCard(Card card)
    {
        deployedCards.Add(card);
    }

    // Call this to clear cards (e.g., at start of new game/phase)
    public void ClearDeployedCards()
    {
        deployedCards.Clear();
    }

    // Debug method: Call this on any trigger to print deployed card names
    public void DebugPrintDeployedCards()
    {
        Debug.Log("--- Deployed Cards ---");
        foreach (Card card in deployedCards)
        {
            Debug.Log("Deployed card: " + card.cardName + " (Slot: " + card.slotIndex + ", Player: " + card.playerID + ")");
        }
        Debug.Log("----------------------");
    }

}
