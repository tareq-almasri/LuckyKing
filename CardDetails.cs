using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardDetailsDisplay : MonoBehaviour
{
    [Header("References")]
    public GameObject cardPrefab;  
    public GameObject stats;      
    public GameObject total;      
    public GameObject effects;    

    // UI fields for effect cards
    public TMP_Text effectText;

    // UI fields for unit base stats
    public TMP_Text AttackText;
    public TMP_Text HPText;
    public TMP_Text ArmorText;
    public TMP_Text CritRateText;
    public TMP_Text CritDamageText;
    public TMP_Text EvadeText;
    public TMP_Text AttackSpeedText;
    public TMP_Text CountText;
    public TMP_Text TotalAttack;
    public TMP_Text TotalHP;
    public TMP_Text TotalArmor;
    public TMP_Text TotalCritRate;
    public TMP_Text TotalCritDamage;
    public TMP_Text TotalEvade;
    public TMP_Text TotalAttackSpeed;
    public TMP_Text TotalCount;
    public TMP_Text UnitDescription;

    [Header("Formation: Orders")]
    public Toggle order1;
    public Toggle order2;
    public Toggle order3;
    public TMP_Text none;

    void Start()
    {
        Debug.Log("CardDetailsDisplay initialized.");
    }

    public void ShowDetails(Card card)
    {
        gameObject.SetActive(true);

        // Remove previous listeners to avoid stacking
        if (order1 != null) order1.onValueChanged.RemoveAllListeners();
        if (order2 != null) order2.onValueChanged.RemoveAllListeners();
        if (order3 != null) order3.onValueChanged.RemoveAllListeners();

        // stats
        if (card.IsUnitCard)
        {
            card.SetUnitStats(); // Ensure unit stats are set before displaying
            effects.SetActive(false);
            stats.SetActive(true);
            total.SetActive(true);
            AttackText.text = $"{card.baseAttack} x {(card.ATK == 0 ? "ATK" : card.ATK)}";
            HPText.text = $"{card.baseHP} x {(card.DEF == 0 ? "DEF" : card.DEF)}";
            ArmorText.text = $"{card.baseArmor} x {(card.DEF == 0 ? "DEF" : card.DEF)}";
            CritRateText.text = $"{card.baseCritRate} x {(card.COUNT == 0 ? "COUNT" : card.COUNT)}";
            CritDamageText.text = $"{card.baseCritDamage} x {(card.ATK == 0 ? "ATK" : card.ATK)}";
            EvadeText.text = $"{card.baseEvade} x {(card.DEF == 0 ? "DEF" : card.DEF)}";
            AttackSpeedText.text = card.cardType == CardType.SiegeEngine ? $"{card.baseAttackSpeed} x {(card.COUNT == 0 ? "COUNT" : card.COUNT)}" : $"{card.baseAttackSpeed}";
            CountText.text = card.cardType == CardType.SiegeEngine ? "1" : $"{card.baseCount} + {(card.COUNT == 0 ? "COUNT" : card.COUNT)}";
            TotalAttack.text = card.ATK == 0 ? "-" : $"{card.unitStats.attack}";
            TotalHP.text = card.DEF == 0 ? "-" : $"{card.unitStats.hp}";
            TotalArmor.text = card.DEF == 0 ? "-" : $"{card.unitStats.armor}";
            TotalCritRate.text = card.COUNT == 0 ? "-" : $"{card.unitStats.critRate}";
            TotalCritDamage.text = card.ATK == 0 ? "-" : $"{card.unitStats.critDamage}";
            TotalEvade.text = card.DEF == 0 ? "-" : $"{card.unitStats.evade}";
            TotalAttackSpeed.text = card.COUNT == 0 ? "-" : $"{card.unitStats.attackSpeed}";
            TotalCount.text = card.COUNT == 0 ? "-" : $"{card.unitStats.count}";
            UnitDescription.text = card.playerID.ToString();
            if (order1 != null)
            {
                order1.gameObject.SetActive(false);
                none.gameObject.SetActive(true);
            }
            if (order2 != null) order2.gameObject.SetActive(false);
            if (order3 != null) order3.gameObject.SetActive(false);
            if (none != null && (card.cardType == CardType.Cavalry || card.isMelee))
            {
                none.gameObject.SetActive(false);
                if (order1 != null)
                {
                    order1.isOn = card.order1_charge;
                    order1.gameObject.SetActive(true);
                }
                if (order2 != null)
                {
                    order2.isOn = card.order2_slowCharge;
                    order2.gameObject.SetActive(true);
                }
                if (order3 != null)
                {
                    order3.gameObject.SetActive(false);
                }
            }
            if (card.cardType == CardType.Cavalry)
            {
                if (order3 != null)
                {
                    order3.isOn = card.order3_flank;
                    order3.gameObject.SetActive(true);
                }
            }

        }
        else
        {
            stats.SetActive(false);
            // effects
            effects.SetActive(true);
            effectText.text = card.effect;
        }

        // update the cardPrefab visual here if needed
        cardPrefab.GetComponent<LuckyCardDisplay>().SetCard(card);

    }

    public void OnOrderToggleChanged(Card card)
    {
        // Handle order toggle changes
        if (order1 != null && order1.isOn)
        { /* Update formation for order 1 */
            Debug.Log("Order 1 selected");
            order2.isOn = false; // Deselect other orders
            order3.isOn = false;
            card.order1_charge = true;
            card.order2_slowCharge = false;
            card.order3_flank = false;
        }
        if (order2 != null && order2.isOn)
        {
            Debug.Log("Order 2 selected");
            order1.isOn = false;
            order3.isOn = false;
            card.order1_charge = false;
            card.order2_slowCharge = true;
            card.order3_flank = false;
        }
        if (order3 != null && order3.isOn)
        {
            Debug.Log("Order 3 selected");
            order1.isOn = false;
            order2.isOn = false;
            card.order3_flank = true;
            card.order1_charge = false;
            card.order2_slowCharge = false;
        }
    }

    public void HideDetails()
    {
        gameObject.SetActive(false);
    }
}