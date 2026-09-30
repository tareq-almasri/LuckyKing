
using System.Collections.Generic;
using UnityEngine;

public class LuckyHandManager : MonoBehaviour
{
    public static LuckyHandManager Instance { get; private set; }
    [Header("Player Reference")]
    public LuckyPlayer ownerPlayer;
    [Header("Hand Setup")]
    public GameObject cardPrefab;
    public int maxHandSize = 6;

    [Header("Visual Settings")]
    public float cardSpacing = 0.2f;
    public float fanSpread = 5f;
    public float verticalOffset = 0.1f;
    public bool enableFanEffect = true;
    public float cardSpawnDistance = 4f;
    public Vector3 cardSpawnDirection = Vector3.right;
    public float cardAnimationDuration = 0.5f;
    public bool keepCardsFlipped = false;

    public List<GameObject> cardsInHand = new();
    private List<Card> runtimeDeck = new();
    private readonly List<GameObject> animatingCards = new();
    private bool globalInteractionDisabled = false;

    void Start()
    {
        if (ownerPlayer != null && !ownerPlayer.isLocalPlayer)
        {
            keepCardsFlipped = true;
        }
        if(LuckyGameManager.Instance != null && LuckyGameManager.Instance.localPlayerID == 2)
                transform.position += new Vector3(-3, 0, 0);
    }

    public void SetPlayerDeck(Deck deck)
    {
        if (deck == null) return;
        runtimeDeck = deck.GetShuffledDeck();
    }

    public int GetRemainingCardsInDeck() => runtimeDeck?.Count ?? 0;
    public int GetCardsInHandCount() => cardsInHand.Count;

    public void DrawCard()
    {
        if (runtimeDeck == null || runtimeDeck.Count == 0 || cardsInHand.Count >= maxHandSize) return;
        int randomIndex = Random.Range(0, runtimeDeck.Count);
        Card cardAsset = runtimeDeck[randomIndex];
        runtimeDeck.RemoveAt(randomIndex);
        cardAsset.playerID = ownerPlayer.playerID;
        Card cardToDraw = Instantiate(cardAsset);
        AddCardToHand(cardToDraw);
    }

    public void DrawInitialHand(int handSize = 3)
    {
        StartCoroutine(DrawCardsSequentially(handSize));
    }

    public System.Collections.IEnumerator DrawCardsSequentially(int cardCount)
    {
        for (int i = 0; i < cardCount; i++)
        {
            DrawCard();
            yield return new WaitForSeconds(cardAnimationDuration + 0.1f);
        }
        UpdateHandVisuals();
    }

    public void AddCardToHand(Card cardData)
    {
        if (cardData == null) return;
        GameObject newCard = Instantiate(cardPrefab, transform);
        newCard.transform.localPosition = Vector3.zero;
        newCard.transform.localRotation = Quaternion.identity;

        if (newCard.TryGetComponent<LuckyCardDisplay>(out var cardDisplay))
            cardDisplay.SetCard(cardData);

        if (newCard.TryGetComponent<LuckyCardMovement>(out var cardMovement))
        {
            cardMovement.SetHandManager(this);
            cardMovement.cardDetailsDisplay = FindFirstObjectByType<CardDetailsDisplay>();
        }

        cardsInHand.Add(newCard);
        newCard.transform.SetAsLastSibling();
        StartCoroutine(SimpleCardAnimation(newCard));
    }

    private System.Collections.IEnumerator SimpleCardAnimation(GameObject newCard)
    {
        animatingCards.Add(newCard);
        if (newCard.TryGetComponent<LuckyCardMovement>(out var cardMovement))
            cardMovement.enabled = false;
        if (newCard.TryGetComponent<Collider>(out var cardCollider))
            cardCollider.enabled = false;

        Transform cardTransform = newCard.transform;
        Vector3 startPos = cardSpawnDirection.normalized * cardSpawnDistance;
        int cardIndex = cardsInHand.IndexOf(newCard);
        Vector3 finalPos = CalculateCardPosition(cardIndex);
        Quaternion finalRot = CalculateCardRotation(cardIndex);
     
        cardTransform.localPosition = startPos;
        cardTransform.localRotation = finalRot;

        float elapsed = 0f;
        while (elapsed < cardAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / cardAnimationDuration);
            if (keepCardsFlipped)
            {
            cardTransform.localPosition = Vector3.Lerp(startPos, finalPos, t);
                
            }else
            {
            cardTransform.localPosition = Vector3.Lerp(startPos, finalPos, t);
            float currentFlipZ = Mathf.Lerp(180f, 0f, t);
            Quaternion flipRotation = Quaternion.Euler(0, 0, currentFlipZ);
            cardTransform.localRotation = flipRotation * finalRot;
            }
            
            yield return null;
        }
        cardTransform.localPosition = finalPos;
        cardTransform.localRotation = keepCardsFlipped ? Quaternion.Euler(0, 0, 180) * finalRot : finalRot;

        if (newCard.TryGetComponent<LuckyCardMovement>(out cardMovement))
            cardMovement.enabled = true;
        if (newCard.TryGetComponent<Collider>(out cardCollider))
            cardCollider.enabled = true;

        animatingCards.Remove(newCard);
        UpdateHandVisuals();
    }

    private Vector3 CalculateCardPosition(int index)
    {
        float totalWidth = (cardsInHand.Count - 1) * cardSpacing;
        float startX = -totalWidth / 2f;
        float xPos = startX + (index * cardSpacing);
        Vector3 targetPosition = new(xPos, 8 + index * verticalOffset, 0);

        if (enableFanEffect && cardsInHand.Count > 1)
        {
            float normalizedPosition = (index - (cardsInHand.Count - 1) / 2f) / ((cardsInHand.Count - 1) / 2f)/2;
            float archHeight = 0.5f - (normalizedPosition * normalizedPosition);
            targetPosition.z = archHeight;
        }
        return targetPosition;
    }

    private Quaternion CalculateCardRotation(int index)
    {
        if (!enableFanEffect || cardsInHand.Count <= 1)
            return keepCardsFlipped ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
        float centerIndex = (cardsInHand.Count - 1) / 2f;
        float fanAngle = (index - centerIndex) * fanSpread;
        return keepCardsFlipped ? Quaternion.Euler(0, fanAngle, 180) : Quaternion.Euler(0, fanAngle, 0);
    }

    public void RemoveCardFromHand(GameObject cardObject)
    {
        if (cardsInHand.Remove(cardObject))
            UpdateHandVisuals();
    }

    public void UpdateHandVisuals()
    {
        if (cardsInHand.Count == 0) return;
        cardsInHand.RemoveAll(card => card == null);

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            GameObject card = cardsInHand[i];
            if (card == null || animatingCards.Contains(card)) continue;
            Transform cardTransform = card.transform;
            cardTransform.localPosition = CalculateCardPosition(i);
            cardTransform.localRotation = CalculateCardRotation(i);
        }
    }

    public int HandCount => cardsInHand.Count;
    public bool IsHandFull => cardsInHand.Count >= maxHandSize;

    public void SetGlobalInteractionEnabled(bool enabled)
    {
        globalInteractionDisabled = !enabled;
        foreach (GameObject card in cardsInHand)
        {
            if (card == null || animatingCards.Contains(card)) continue;
            if (card.TryGetComponent<LuckyCardMovement>(out var cardMovement))
                cardMovement.enabled = enabled;
            if (card.TryGetComponent<Collider>(out var cardCollider))
                cardCollider.enabled = enabled;
        }
    }

    public bool CanInteractWithCard(GameObject card)
    {
        if (globalInteractionDisabled || animatingCards.Contains(card)) return false;
        return true;
    }
}