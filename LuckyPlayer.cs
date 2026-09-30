using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Analytics;
using UnityEngine.UI;
using PlayFab.ProfilesModels;

public class LuckyPlayer : MonoBehaviour
{
    [Header("Player Info")]
    public int playerID; // 1 or 2
    public string playerName;
    public bool isAI = false;
    public bool isLocalPlayer; // True if this player is the local user
    public GameObject cardBack;
    public Sprite playerAvatar;
    public Sprite avatarFrame;
    public Sprite titleImage;
    public Sprite banner;

    [Header("Resources")]
    [SerializeField] private int deploymentPoints = 1;  // Start with 1 DP
    public int currentTurn = 0;       // Start at turn 0, will become 1 on first turn

    [Header("Game Objects")]
    public Deck playerDeck;
    public LuckyHandManager handManager;
    // Note: cardStackTransform removed - cards now spawn directly in hand

    [Header("Dice")]
    public LuckyDice diceWhite;
    public LuckyDice diceRed;
    public LuckyDice diceBlue;

    // Runtime data
    private int cardsPlayedThisTurn = 0;

    // Events
    public System.Action<LuckyPlayer> OnTurnStart;
    public System.Action<LuckyPlayer> OnTurnEnd;
    public System.Action<LuckyPlayer, Card> OnCardPlayed;
    public System.Action<LuckyPlayer, int> OnDPChanged;
    public List<DropZone> allDropZones;
    // Initialize player components
    public void InitializePlayer()
    {
        Debug.Log($"🔧 Initializing Player {playerID}...");

        // Set up hand manager with deck
        if (handManager != null && playerDeck != null)
        {
            handManager.SetPlayerDeck(playerDeck);
            Debug.Log($"🎴 Player {playerID} deck initialized with {playerDeck.CardCount} cards");
        }
        else
        {
            Debug.LogWarning($"❌ Player {playerID} - Missing components: HandManager={handManager != null}, Deck={playerDeck != null}");
        }
        if (MatchTracker.Instance.currentGameMode == MatchTracker.GameMode.VsAI && playerID == 2)
        {
            isAI = true;
            playerName = "AI Opponent";
        }

    }

    public System.Collections.IEnumerator StartTurn()
    {
        currentTurn++;
        cardsPlayedThisTurn = 0;

        Debug.Log($"🔧 Starting turn {currentTurn} for Player {playerID}");

        // Set DP based on turn number: Turn 1 = 2 DP, Turn 2 = 3 DP, etc.
        int newDP = currentTurn + 1;
        SetDeploymentPoints(newDP);

        Debug.Log($"🔧 Set DP to {newDP} for turn {currentTurn}");

        // Draw a card
        Debug.Log($"🔧 About to draw card - handManager: {(handManager != null ? "SET" : "NULL")}");
        if (handManager != null)
        {
            handManager.DrawCard(); // Use single card draw method
        }
        else
        {
            Debug.LogError($"❌ Player {playerID} has no hand manager!");
        }

        //vs AI logic using playerID=2:
        /* if (isAI)
         {
             allDropZones = new List<DropZone>(FindObjectsByType<DropZone>(FindObjectsSortMode.None));
             allDropZones.RemoveAll(zone => zone == null);

             //find cards and drop zones
             List<GameObject> unitCardsInHand = new();
             List<GameObject> effectCardsInHand = new();

             handManager.cardsInHand.ForEach(card =>
             {
                 Card cardComponent = card.GetComponent<LuckyCardDisplay>().GetCard();
                 if (cardComponent.IsUnitCard)
                 {
                     unitCardsInHand.Add(card);
                 }
                 else if (cardComponent.IsEffectCard)
                 {
                     effectCardsInHand.Add(card);
                 }
             });

             //play cards
             foreach (var unitCard in unitCardsInHand)
             {
                 if (deploymentPoints <= 0) break; //no more DP
                 Card card = unitCard.GetComponent<LuckyCardDisplay>().GetCard();
                 if (allDropZones.FindAll(zone => !zone.isOccupied && zone.playerID == 2).Count == 0) break; //no more zones
                 if (!CanAffordCard(card)) continue;
                 if (card.cardType == CardType.SiegeEngine || (card.cardType == CardType.Infantry && card.isArcher))
                 {
                     //prefer back row for archers and siege engines
                     var sortedZones = allDropZones.FindAll(zone => !zone.isOccupied && zone.playerID == 2);
                     sortedZones.Sort((a, b) => b.slotIndex.CompareTo(a.slotIndex));
                     DropZone dropZone = sortedZones[0];
                     if (dropZone != null && !dropZone.isOccupied)
                     {
                         yield return new WaitForSeconds(2f);
                         if (TryPlayCard(card))
                         {
                             dropZone.OnCardDropped(unitCard);
                             StartCoroutine(unitCard.GetComponent<LuckyCardMovement>().HandleUnitCardDeployment(card));
                         }
                     }
                 }
                 else
                 {
                     //prefer front row for other units
                     var sortedZones = allDropZones.FindAll(zone => !zone.isOccupied && zone.playerID == 2);
                     sortedZones.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));
                     DropZone dropZone = sortedZones[0];

                     if (dropZone != null && !dropZone.isOccupied)
                     {
                         yield return new WaitForSeconds(2f);
                         if (TryPlayCard(card))
                         {
                             dropZone.OnCardDropped(unitCard);
                             StartCoroutine(unitCard.GetComponent<LuckyCardMovement>().HandleUnitCardDeployment(card));
                         }
                     }
                 }

             }

             foreach (var effectCard in effectCardsInHand)
             {
                 if (deploymentPoints <= 0) break; //no more DP
                 Card card = effectCard.GetComponent<LuckyCardDisplay>().GetCard();
                 if (!CanAffordCard(card)) continue;
                 if (card.cardType == CardType.Buff)
                 {
                     DropZone dropZone = allDropZones.FindAll(zone => zone.isOccupied && zone.playerID == 2)[0];
                     if (dropZone != null && dropZone.isOccupied)
                     {
                         yield return new WaitForSeconds(2f);
                         if (TryPlayCard(card))
                         {
                             dropZone.OnCardDropped(effectCard);
                             effectCard.GetComponent<LuckyCardMovement>().DisableCardMovement();
                         }
                     }
                 }
                 else if (card.cardType == CardType.Debuff)
                 {
                     DropZone dropZone = allDropZones.FindAll(zone => zone.isOccupied && zone.playerID == 1)[0];
                     if (dropZone != null && !dropZone.isOccupied)
                     {
                         yield return new WaitForSeconds(2f);
                         if (TryPlayCard(card))
                         {
                             dropZone.OnCardDropped(effectCard);
                             effectCard.GetComponent<LuckyCardMovement>().DisableCardMovement();
                         }
                     }
                 }
                 else
                 {
                     yield return new WaitForSeconds(2f);
                     if (TryPlayCard(card))
                     {
                         allDropZones[0].OnCardDropped(effectCard);
                         effectCard.GetComponent<LuckyCardMovement>().DisableCardMovement();
                     }
                 }
             }
             LuckyGameManager.Instance.EndCurrentPlayerTurn();
         }*/

        //vs AI logic using localPlayerID:
        if (isAI)
        {
            allDropZones = new List<DropZone>(FindObjectsByType<DropZone>(FindObjectsSortMode.None));
            allDropZones.RemoveAll(zone => zone == null);

            //find cards and drop zones
            List<GameObject> unitCardsInHand = new();
            List<GameObject> effectCardsInHand = new();

            handManager.cardsInHand.ForEach(card =>
            {
                Card cardComponent = card.GetComponent<LuckyCardDisplay>().GetCard();
                if (cardComponent.IsUnitCard)
                {
                    unitCardsInHand.Add(card);
                }
                else if (cardComponent.IsEffectCard)
                {
                    effectCardsInHand.Add(card);
                }
            });

            //play cards
            foreach (var unitCard in unitCardsInHand)
            {
                if (deploymentPoints <= 0) break; //no more DP
                Card card = unitCard.GetComponent<LuckyCardDisplay>().GetCard();
                if (allDropZones.FindAll(zone => !zone.isOccupied && !zone.isLocalPlayer).Count == 0) break; //no more zones
                if (!CanAffordCard(card)) continue;
                if (card.cardType == CardType.SiegeEngine || (card.cardType == CardType.Infantry && card.isArcher))
                {
                    //prefer back row for archers and siege engines
                    var sortedZones = allDropZones.FindAll(zone => !zone.isOccupied && !zone.isLocalPlayer);
                    sortedZones.Sort((a, b) => b.slotIndex.CompareTo(a.slotIndex));
                    DropZone dropZone = sortedZones[0];
                    if (dropZone != null && !dropZone.isOccupied)
                    {
                        yield return new WaitForSeconds(2f);
                        if (TryPlayCard(card))
                        {
                            dropZone.OnCardDropped(unitCard);
                            StartCoroutine(unitCard.GetComponent<LuckyCardMovement>().HandleUnitCardDeployment(card));
                        }
                    }
                }
                else
                {
                    //prefer front row for other units
                    var sortedZones = allDropZones.FindAll(zone => !zone.isOccupied && !zone.isLocalPlayer);
                    sortedZones.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));
                    DropZone dropZone = sortedZones[0];

                    if (dropZone != null && !dropZone.isOccupied)
                    {
                        yield return new WaitForSeconds(2f);
                        if (TryPlayCard(card))
                        {
                            dropZone.OnCardDropped(unitCard);
                            StartCoroutine(unitCard.GetComponent<LuckyCardMovement>().HandleUnitCardDeployment(card));
                        }
                    }
                }

            }

            foreach (var effectCard in effectCardsInHand)
            {
                if (deploymentPoints <= 0) break; //no more DP
                Card card = effectCard.GetComponent<LuckyCardDisplay>().GetCard();
                if (!CanAffordCard(card)) continue;
                if (card.cardType == CardType.Buff)
                {
                    DropZone dropZone = allDropZones.FindAll(zone => zone.isOccupied && !zone.isLocalPlayer)[0];
                    if (dropZone != null && dropZone.isOccupied)
                    {
                        yield return new WaitForSeconds(2f);
                        if (TryPlayCard(card))
                        {
                            dropZone.OnCardDropped(effectCard);
                            effectCard.GetComponent<LuckyCardMovement>().DisableCardMovement();
                        }
                    }
                }
                else if (card.cardType == CardType.Debuff)
                {
                    DropZone dropZone = allDropZones.FindAll(zone => zone.isOccupied && zone.isLocalPlayer)[0];
                    if (dropZone != null && !dropZone.isOccupied)
                    {
                        yield return new WaitForSeconds(2f);
                        if (TryPlayCard(card))
                        {
                            dropZone.OnCardDropped(effectCard);
                            effectCard.GetComponent<LuckyCardMovement>().DisableCardMovement();
                        }
                    }
                }
                else
                {
                    yield return new WaitForSeconds(2f);
                    if (TryPlayCard(card))
                    {
                        allDropZones[0].OnCardDropped(effectCard);
                        effectCard.GetComponent<LuckyCardMovement>().DisableCardMovement();
                    }
                }
            }
            LuckyGameManager.Instance.EndCurrentPlayerTurn();
        }

        Debug.Log($"🎯 Player {playerID} Turn {currentTurn} - DP: {deploymentPoints}");
        OnTurnStart?.Invoke(this);
    }

    public void EndTurn()
    {
        Debug.Log($"⏭️ Player {playerID} ends turn. Cards played: {cardsPlayedThisTurn}");
        OnTurnEnd?.Invoke(this);
    }

    public bool CanAffordCard(Card card)
    {
        return card != null && deploymentPoints >= card.cost;
    }

    public bool TryPlayCard(Card card)
    {
        if (!CanAffordCard(card))
        {
            Debug.LogWarning($"❌ Player {playerID} cannot afford {card.cardName} (Cost: {card.cost}, DP: {deploymentPoints})");
            return false;
        }

        // Spend DP
        SetDeploymentPoints(deploymentPoints - card.cost);
        cardsPlayedThisTurn++;

        Debug.Log($"✅ Player {playerID} plays {card.cardName} for {card.cost} DP");

        OnCardPlayed?.Invoke(this, card);
        return true;
    }


    /// <summary>
    /// Rolls all dice for a unit card deployment. Call this after a unit card is played.
    /// </summary>
    public System.Collections.IEnumerator RollUnitDice(Card unitCard)
    {
        Debug.Log($"[DEBUG] RollUnitDice called for {unitCard.cardName} (Player {playerID})");
        Debug.Log($"🎲 Rolling dice for {unitCard.cardName}...");
        Debug.Log($"[DEBUG] Rolling ALL dice for player {playerID} after deploying unit card {unitCard.cardName}!");

        // Reset all dice before rolling
        if (diceRed != null) { Debug.Log($"[DEBUG] Resetting diceRed"); diceRed.ResetDice(); } else { Debug.LogWarning("[DEBUG] diceRed is null!"); }
        if (diceBlue != null) { Debug.Log($"[DEBUG] Resetting diceBlue"); diceBlue.ResetDice(); } else { Debug.LogWarning("[DEBUG] diceBlue is null!"); }
        if (diceWhite != null) { Debug.Log($"[DEBUG] Resetting diceWhite"); diceWhite.ResetDice(); } else { Debug.LogWarning("[DEBUG] diceWhite is null!"); }

        Debug.Log($"[DEBUG] Dice states before rolling: diceRed.HasRolled={(diceRed != null ? diceRed.HasRolled : "NULL")}, diceBlue.HasRolled={(diceBlue != null ? diceBlue.HasRolled : "NULL")}, diceWhite.HasRolled={(diceWhite != null ? diceWhite.HasRolled : "NULL")}");

        // Roll all three dice
        if (diceRed != null) { Debug.Log($"[DEBUG] Calling diceRed.RollDice()"); diceRed.RollDice(); }
        if (diceBlue != null) { Debug.Log($"[DEBUG] Calling diceBlue.RollDice()"); diceBlue.RollDice(); }
        if (diceWhite != null) { Debug.Log($"[DEBUG] Calling diceWhite.RollDice()"); diceWhite.RollDice(); }

        // Wait 0.7 seconds before assigning dice values to unitCard
        yield return new WaitForSeconds(0.7f);

        unitCard.ATK = diceRed.Value;
        unitCard.DEF = diceBlue.Value;
        unitCard.COUNT = diceWhite.Value;

        StartCoroutine(WaitForDiceAndDeploy(unitCard));
    }


    private System.Collections.IEnumerator WaitForDiceAndDeploy(Card unitCard)
    {
        // Wait for all dice to finish rolling
        while (!diceRed.HasRolled || !diceBlue.HasRolled || !diceWhite.HasRolled)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Get dice values
        int atkMultiplier = diceRed.Value;
        int defMultiplier = diceBlue.Value;
        int countMultiplier = diceWhite.Value;

        unitCard.ATK = atkMultiplier;
        unitCard.DEF = defMultiplier;
        unitCard.COUNT = countMultiplier;

        Debug.Log($"🎲 Dice Results - ATK: {atkMultiplier}, DEF: {defMultiplier}, COUNT: {countMultiplier}");

    }

    private void SetDeploymentPoints(int newDP)
    {
        deploymentPoints = newDP;
        Debug.Log($"🔧 Player {playerID} DP set to: {newDP}");
        OnDPChanged?.Invoke(this, deploymentPoints);
    }

    public void AddDeploymentPoints(int amount)
    {
        int oldDP = deploymentPoints;
        SetDeploymentPoints(deploymentPoints + amount);
        Debug.Log($"🎯 Player {playerID} gained {amount} DP. Old: {oldDP}, New: {deploymentPoints}");
    }

    // Helper method to start coroutine (since this isn't a MonoBehaviour)
    private new void StartCoroutine(System.Collections.IEnumerator routine)
    {
        if (handManager != null)
        {
            handManager.StartCoroutine(routine);
        }
        else
        {
            // Fallback: find any MonoBehaviour to run the coroutine
            MonoBehaviour coroutineRunner = FindFirstObjectByType<LuckyGameManager>();
            if (coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(routine);
            }
            else
            {
                Debug.LogError($"❌ Player {playerID} - No MonoBehaviour available to start coroutine!");
            }
        }
    }

    // Properties
    public int DeploymentPoints => deploymentPoints;
    public int MaxDeploymentPoints => currentTurn > 0 ? currentTurn + 1 : 1; // Turn 1 = 2 max DP, Turn 2 = 3 max DP, etc.
    public int CurrentTurn => currentTurn;
    public int CardsPlayedThisTurn => cardsPlayedThisTurn;

    // Returns true if any dice are currently animating (not back to initial position)
    public bool AreDiceAnimating()
    {
        // If any dice are rolling or not back to initial position, block interaction
        return (diceRed != null && diceRed.IsAnimating()) ||
               (diceBlue != null && diceBlue.IsAnimating()) ||
               (diceWhite != null && diceWhite.IsAnimating());
    }
}
