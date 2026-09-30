using Unity.AppUI.UI;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Lucky/Card")]
[System.Serializable]
public class Card : ScriptableObject
{
    [Header("Basic Card Info")]
    public string cardName;
    public int cardID; // Unique ID for the card
    public Sprite artwork;
    public int cost;
    public CardType cardType;
    [Range(1, 5)] public int level = 1;       // Card level (1-5, always starts at 1)
    [Range(1, 3)] public int tier = 1;        // Card tier (1=B, 2=A, 3=S)

    [Header("Effect Card Type (Buff/Debuff/Logistics only)")]

    public TargetFieldOnCard targetField;
    public OperationOfEffect operationOfEffect; // How the effect is applied (e.g. multiply, add)
    public float effectAmount; // Amount for buffs/debuffs/logistics
    public bool RequiresRoll;
    [TextArea(2, 4)]
    public string effect;

    [Header("Unit Stats (Infantry/Cavalry/SiegeEngine)")]
    public bool IsUnitCard; // True if this is a unit card (Infantry/Cavalry/SiegeEngine)
    public bool isMelee;
    public bool isArcher;
    public bool hasShield;
    public int unitID; // Unique ID for the unit (if applicable)
    public int baseAttack;
    public int baseHP;
    public int baseArmor;
    public float baseCritRate;
    public int baseCritDamage;
    public float baseEvade;
    public float baseAttackSpeed;
    public int baseCount;
    public float range;
    public bool armorPiercing;
    [TextArea(2, 4)]
    public string unitDescription;

    [Header("Unit Prefab (single model)")]
    public GameObject unitPrefab;

    [Header("Ranged Projectile Prefab")]
    public GameObject projectilePrefab;

    [Header("Multipliers (Unit Cards)")]
    public int ATK = 0;
    public int DEF = 0;
    public int COUNT = 0;

    //Deployment related data:
    [Header("Deployment Data")]
    public int slotIndex = -1; // Which slot this card is deployed to (-1 = auto-assign)
    public int playerID = 1; // Which player owns this card (1 or 2)

    [Header("Formation data")]
    public bool order1_charge = true; //default order
    public bool order2_slowCharge = false;
    public bool order3_flank = false;

    [Header("x y z for battle spawn")]
    public float spawnX = 0f;
    public float spawnY = 0f;
    public float spawnZ = 0f;

    [Header("Unit Stats (Calculated)")]
    public UnitStats unitStats;

    public UnitStats SetUnitStats()
    {
        unitStats = new UnitStats
        {
            unitID = unitID,
            attack = ATK == 0 ? baseAttack : baseAttack * ATK,
            hp = DEF == 0 ? baseHP : baseHP * DEF,
            armor = DEF == 0 ? baseArmor : baseArmor * DEF,
            critRate = COUNT == 0 ? baseCritRate : baseCritRate * COUNT,
            critDamage = ATK == 0 ? baseCritDamage : baseCritDamage * ATK,
            evade = DEF == 0 ? baseEvade : baseEvade * DEF,
            count = IsSiegeEngine ? 1 : (COUNT == 0 ? baseCount : baseCount + COUNT),
            attackSpeed = IsSiegeEngine ? COUNT == 0 ? baseAttackSpeed : baseAttackSpeed * COUNT : baseAttackSpeed,
        };
        return unitStats;
    }
    public void SetMultiplier(MultiplierType type, int value)
    {
        switch (type)
        {
            case MultiplierType.ATK:
                ATK = value;
                break;
            case MultiplierType.DEF:
                DEF = value;
                break;
            case MultiplierType.COUNT:
                COUNT = value;
                break;
        }
    }
    public int GetMultiplier(MultiplierType type)
    {
        return type switch
        {
            MultiplierType.ATK => ATK,
            MultiplierType.DEF => DEF,
            MultiplierType.COUNT => COUNT,
            _ => 0
        };
    }

    public void SetSpawnPosition(float x, float y, float z)
    {
        spawnX = x;
        spawnY = y;
        spawnZ = z;
    }
    public Vector3 GetSpawnPosition()
    {
        return new Vector3(spawnX, spawnY, spawnZ);
    }

    // Property getters that return relevant values based on card type
    // public bool IsUnitCard => cardType == CardType.Infantry || cardType == CardType.Cavalry || cardType == CardType.SiegeEngine;
    public bool IsEffectCard => cardType == CardType.Buff || cardType == CardType.Debuff || cardType == CardType.Logistics;
    public bool HasCombatStats => IsUnitCard;
    public bool IsSiegeEngine => cardType == CardType.SiegeEngine;

    /* public UnitStats SetUnitStats()
     {
         return unitStats = new UnitStats
         {
             unitID = unitID,
             attack = ATK == 0 ? baseAttack : baseAttack * ATK,
             hp = DEF == 0 ? baseHP : baseHP * DEF,
             armor = DEF == 0 ? baseArmor : baseArmor * DEF,
             critRate = COUNT == 0 ? baseCritRate : baseCritRate * COUNT,
             critDamage = ATK == 0 ? baseCritDamage : baseCritDamage * ATK,
             evade = DEF == 0 ? baseEvade : baseEvade * DEF,
             count = IsSiegeEngine ? 1 : (COUNT == 0 ? baseCount : baseCount + COUNT),
             attackSpeed = COUNT == 0 ? baseAttackSpeed : baseAttackSpeed * COUNT,
         };
     }
     // Get only relevant properties for the card type


     public UnitStats GetUnitStats()
     {
         return unitStats;
     }
     // Get only relevant properties for the card type
     */

}

// Data structures for clean card data
[System.Serializable]
public class CardData
{
    public string name;
    public int cardID;
    public int slotIndex;
    public int playerID;
    public Sprite artwork;
    public int cost;
    public CardType type;
    public float range;
    public int level;
    public int tier;
    public bool isUnitCard; // True if this is a unit card (Infantry/Cavalry/SiegeEngine)   
    public string effect;
    public string unitDescription;
    public UnitStats unitStats;
}

[System.Serializable]
public class UnitStats
{
    public int unitID;
    public int attack;
    public int hp;
    public int armor;
    public float critRate;
    public int critDamage;
    public float evade;
    public int count;
    public float attackSpeed;
}

public enum CardType
{
    Infantry,
    Cavalry,
    SiegeEngine,
    Buff,
    Debuff,
    Logistics
}

public enum MultiplierType
{
    ATK,
    DEF,
    COUNT
}
public enum TargetFieldOnCard
{
    ATK,
    DEF,
    COUNT,
    None // For cards that don't target anything
}

public enum OperationOfEffect
{
    Multiply, // e.g., increase by a percentage
    Add,      // e.g., add a flat amount
    Set       // e.g., set to a specific value
}