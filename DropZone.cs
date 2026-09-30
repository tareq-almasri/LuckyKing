using UnityEngine;
using UnityEngine.UI;

public class DropZone : MonoBehaviour
{
    [Header("Drop Zone Settings")]
    public int slotIndex; // manual assign via creator
    public int playerID = 1; // Which player owns this drop zone (1 or 2)
    public bool isOccupied = false;

    [Header("Card Positioning")]
    [Tooltip("Offset from the zone center where cards will be positioned")]
    public Vector3 cardPositionOffset = new Vector3(0, 1, 0);

    public Vector3 effectDisplayOffset = new Vector3(-11, 2, 0);

    public bool isLocalPlayer;
    private Quaternion endRot;


    [Header("Visual Feedback")]
    public GameObject highlightObject; // Visual indicator when card is over this zone


    void Start()
    {
        // Ensure we have a collider set as trigger
        if (!TryGetComponent<Collider>(out var col))
        {
            // Add a box collider if none exists
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.isTrigger = true;
        if (LuckyGameManager.Instance.localPlayerID == 2)
        {
            effectDisplayOffset = new Vector3(13, 2, 0);
        }
    }


    public bool CanAcceptCard(GameObject cardObject, int playerWhoOwnsCard, bool canAfford)
    {
        if (!canAfford)
        {
            highlightObject.SetActive(true);
            highlightObject.GetComponent<Image>().color = Color.red;
            return false;
        }
        ;
        // Get card information
        LuckyCardDisplay cardDisplay = cardObject.GetComponent<LuckyCardDisplay>();
        if (cardDisplay == null || cardDisplay.cardData == null) return false;

        Card card = cardDisplay.cardData;

        // All zones are generic:
        // Logistics: any zone
        if (card.cardType == CardType.Logistics)
        {
            highlightObject.SetActive(true);
            highlightObject.GetComponent<Image>().color = Color.green;
            return true;
        }
        ;
        // Buff/Debuff: only if a unit is present
        if ((card.cardType == CardType.Buff || card.cardType == CardType.Debuff) && isOccupied)
        {
            highlightObject.SetActive(true);
            highlightObject.GetComponent<Image>().color = Color.green;
            return true;
        }
        ;
        // Unit: only if owner and not occupied
        if (card.IsUnitCard && playerWhoOwnsCard == playerID && !isOccupied)
        {
            highlightObject.SetActive(true);
            highlightObject.GetComponent<Image>().color = Color.green;
            return true;
        }
        ;
        highlightObject.SetActive(true);
        highlightObject.GetComponent<Image>().color = Color.red;
        return false;
    }


    public void OnCardDropped(GameObject cardObject)
    {
        StartCoroutine(HandleCardDrop(cardObject));
        highlightObject.SetActive(false); // Hide highlight after drop
        LuckyCardDisplay cardDisplay = cardObject.GetComponent<LuckyCardDisplay>();
        if (cardDisplay == null || cardDisplay.cardData == null) return;

        Card card = cardDisplay.cardData;
        card.slotIndex = slotIndex; // Assign the slot index to the card

    }

    private System.Collections.IEnumerator SmoothMoveCard(GameObject cardObject, bool isEffectCard = false)
    {
        cardObject.GetComponent<LuckyCardMovement>().isDragging = true;
        Transform cardTransform = cardObject.transform;
        Vector3 startPos = cardTransform.position;
        Vector3 endPos = isEffectCard ? effectDisplayOffset : transform.TransformPoint(cardPositionOffset);

        Quaternion startRot = cardTransform.rotation;
        if (LuckyGameManager.Instance.localPlayerID == 2)
        { endRot = Quaternion.Euler(0, 180, 0); }
        else
        { endRot = Quaternion.identity; }
        float duration = 0.3f;
        float elapsed = 0f;
        // Detach from parent for smooth movement
        if (!isEffectCard) cardTransform.SetParent(null);
        while (elapsed < duration)
        {
            cardTransform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            cardTransform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cardTransform.position = endPos;
        cardTransform.rotation = endRot;
        // Parent the card to this drop zone after movement
        if (!isEffectCard)
        {
            cardTransform.SetParent(transform);
            cardTransform.localPosition = cardPositionOffset;
            cardTransform.localRotation = Quaternion.identity;
            cardTransform.localScale = Vector3.one;
            cardTransform.GetComponent<LuckyCardMovement>().originalScale = Vector3.one;
        }
        else
        {
            cardTransform.localScale = Vector3.one * 2;
        }
    }
    private System.Collections.IEnumerator HandleCardDrop(GameObject cardObject)
    {
        // Get card information
        LuckyCardDisplay cardDisplay = cardObject.GetComponent<LuckyCardDisplay>();
        if (cardDisplay == null || cardDisplay.cardData == null) yield break;

        Card card = cardDisplay.cardData;
        if (card != null && card.IsUnitCard)
        {
            card.SetUnitStats();
            // Mark slot as occupied
            isOccupied = true;
            // Smoothly move the card to this drop zone
            StartCoroutine(SmoothMoveCard(cardObject, false));
        }


        else if (card != null && card.IsEffectCard)
        {
            // Detach the card from its parent before destroying
            //cardObject.transform.SetParent(null);
            StartCoroutine(SmoothMoveCard(cardObject, true));

            if (card.cardType == CardType.Logistics)
            {
                // Handle logistics card    
                LuckyPlayer ownerPlayer = null;
                if (cardObject.TryGetComponent<LuckyCardMovement>(out var movement))
                    ownerPlayer = movement.OwnerPlayer;
                EffectCardManager.Instance.ApplyEffect(cardObject, null, ownerPlayer); // No target display for logistics
            }

            else if (transform.childCount > 0 && isOccupied)
            {
                Transform child = transform.GetChild(1);
                var targetDisplay = child.GetComponent<LuckyCardDisplay>();

                if (EffectCardManager.Instance == null)
                {
                    Debug.LogError("EffectCardManager.Instance is null! Make sure it exists in the scene.");
                }
                else if (targetDisplay == null)
                {
                    Debug.LogError("Target targetDisplay is null!");
                }
                else if (targetDisplay.cardData == null)
                {
                    Debug.LogError("Target targetDisplay.cardData is null!");
                }
                else
                {
                    LuckyPlayer ownerPlayer = null;
                    if (cardObject.TryGetComponent<LuckyCardMovement>(out var movement))
                        ownerPlayer = movement.OwnerPlayer;
                    EffectCardManager.Instance.ApplyEffect(cardObject, targetDisplay, ownerPlayer);
                    card.SetUnitStats();
                }

            }
            yield return new WaitForSeconds(3f); // Wait for the move animation to finish
            Destroy(cardObject);
        }
        else
        {
            Debug.LogWarning($"❌ Card {cardObject.name} cannot be played in this drop zone!");
            yield break;
        }

    }

    public void OnCardRemoved()
    {
        Debug.Log($"🎯 Card removed from {gameObject.name}");
        isOccupied = false;
    }

    // Properties for external access
    public bool IsOccupied => isOccupied;
    public int SlotIndex => slotIndex;
    public int PlayerID => playerID;
    public Vector3 WorldPosition => transform.position;
}
