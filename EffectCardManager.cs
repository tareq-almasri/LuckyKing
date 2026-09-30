using Unity.VisualScripting;
using UnityEngine;

// Add any other using statements needed for Card, LuckyPlayer, DropZone, etc.

public class EffectCardManager : MonoBehaviour
{
    public static EffectCardManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally, make persistent across scenes:
        // DontDestroyOnLoad(this.gameObject);
    }

    // Main entry point for effect cards

    public void ApplyEffect(GameObject effectCardObject, LuckyCardDisplay targetDisplay, LuckyPlayer player)
    {
        Debug.Log("EffectCardManager: Applying effect...");
        if (effectCardObject == null)
        {
            Debug.LogWarning("EffectCardManager: Invalid target object. (effectCardObject is null)");
            return;
        }
        if (!effectCardObject.TryGetComponent<LuckyCardDisplay>(out var lcd))
        {
            Debug.LogWarning("EffectCardManager: effectCardObject has no LuckyCardDisplay component.");
            return;
        }
        Card effectCard = lcd.cardData;
        if (effectCard == null)
        {
            Debug.LogWarning("EffectCardManager: LuckyCardDisplay.cardData is null.");
            return;
        }
        if (targetDisplay == null && effectCard.cardType != CardType.Logistics)
        {
            Debug.LogWarning("EffectCardManager: targetDisplay is null and card is not logistics.");
            return;
        }
        if (player == null)
        {
            Debug.LogWarning("EffectCardManager: LuckyPlayer argument is null.");
            return;
        }
        if (targetDisplay == null)
        {
            Debug.Log("EffectCardManager: targetDisplay is null (logistics card or error)");
        }
        var targetCard = targetDisplay != null ? targetDisplay.GetCard() : null;
        var multiplierType = effectCard.targetField;
        int numericFieldValue = targetCard != null ? targetCard.GetMultiplier((MultiplierType)multiplierType) : 0;
        Debug.Log($"EffectCardManager: targetField = {numericFieldValue}");
        if (effectCard.RequiresRoll && player.diceWhite != null)
        {
            Debug.Log(">>EffectCardManager: Rolling dice for effect.");
            player.diceWhite.ResetDice();
            player.diceWhite.RollDice();
            if (effectCard.targetField == TargetFieldOnCard.COUNT)
            {
                // Handle COUNT multiplier logic
                if (targetCard != null)
                {
                    if (targetCard.GetMultiplier(MultiplierType.COUNT) >= player.diceWhite.Value)
                    {
                        Debug.LogWarning("EffectCardManager: COUNT should be less than dice value.");
                        return;
                    }
                }
            }
        }
        if (effectCard.targetField == TargetFieldOnCard.COUNT && effectCard.effectAmount > 1)
        {
            // Handle COUNT multiplier logic
            if (targetCard != null)
            {
                if (numericFieldValue > 5)
                {
                    Debug.LogWarning("EffectCardManager: COUNT exceeds maximum limit.");
                    return;
                }
            }
        }
        int effectDiceResult = effectCard.RequiresRoll && player.diceWhite != null ? player.diceWhite.Value : 1;

        Debug.Log($"EffectCardManager: Applying {effectCard.operationOfEffect} to {multiplierType} with value {numericFieldValue}, amount {effectCard.effectAmount}, dice {effectDiceResult}");
        switch (effectCard.operationOfEffect)
        {
            case OperationOfEffect.Multiply:
                if (targetCard != null)
                {
                    int multiplyResult = (int)(numericFieldValue * effectCard.effectAmount * effectDiceResult);
                    targetCard.SetMultiplier((MultiplierType)multiplierType, multiplyResult < 2 ? 1 : multiplyResult);
                }
                break;
            case OperationOfEffect.Add:
                if (targetCard != null)
                {
                    int addResult = numericFieldValue + (int)(effectCard.effectAmount * effectDiceResult);
                    targetCard.SetMultiplier((MultiplierType)multiplierType, addResult < 2 ? 1 : addResult);
                }
                break;
            case OperationOfEffect.Set:
                if (targetCard != null)
                    targetCard.SetMultiplier((MultiplierType)multiplierType, (int)effectCard.effectAmount * effectDiceResult);
                else if (effectCard.cardType == CardType.Logistics)
                {
                    StartCoroutine(player.handManager.DrawCardsSequentially((int)effectCard.effectAmount * effectDiceResult));
                }
                break;
            default:
                Debug.LogWarning("EffectCardManager: Unknown effect type.");
                break;
        }
        if (targetDisplay != null && targetCard != null)
        {
            StartCoroutine(SetCardWithDelay(targetDisplay, targetCard, 0.7f)); // 0.7 seconds delay
            Debug.Log($"new value set for {multiplierType}: {targetCard.GetMultiplier((MultiplierType)multiplierType)}");
        }
    }

    private System.Collections.IEnumerator SetCardWithDelay(LuckyCardDisplay display, Card card, float delay)
    {
        yield return new WaitForSeconds(delay);
        display.SetCard(card);
    }

    // Extend with more effect types as needed
}
