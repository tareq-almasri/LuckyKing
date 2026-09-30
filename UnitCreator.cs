using UnityEngine;

public class UnitCreator : MonoBehaviour
{
    public GameObject unitDetailsPanel; // Panel to show unit details
    public int modelBaseSizeX = 1; // Base size of each model
    public int modelBaseSizeZ = 1; // Base size of each model
    public GameObject unitPrefab;
    public static UnitCreator Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void CreateUnit(Card card, Transform parentTransform, bool isFormation)
    {
        // Create a root object for positioning
        GameObject unitRoot = new GameObject("UnitRoot");
        unitRoot.transform.SetParent(parentTransform);
        unitRoot.transform.localPosition = card.GetSpawnPosition();

        // Instantiate the unit prefab as a child of the root
        GameObject unitObject = Instantiate(unitPrefab, unitRoot.transform);
        unitObject.tag = $"Player{card.playerID}";
        //unitObject.transform.localPosition = Vector3.zero;
        //unitObject.transform.localScale = Vector3.one * modelBaseSize;

        unitObject.GetComponent<Unit>().unitCard = card;
        unitObject.GetComponent<Unit>().unitDetailsPanel = unitDetailsPanel;


        switch (card.cardType)
        {
            case CardType.SiegeEngine:
                modelBaseSizeX = 3;
                modelBaseSizeZ = 4;
                break;
            case CardType.Cavalry:
                modelBaseSizeX = 1;
                modelBaseSizeZ = 2;
                break;
            default:
                modelBaseSizeX = 1;
                modelBaseSizeZ = 1;
                break;
        }
        for (int i = 0; i < card.unitStats.count; i++)
        {
            SpawnModelsInUnit(unitObject, card, isFormation);
        }
        // Arrange spawned models in rows of 6 and center them
        int modelsPerRow = 6;
        float spacingX = modelBaseSizeX * 1.2f;
        float spacingZ = modelBaseSizeZ * 1.2f;
        int totalModels = card.unitStats.count;

        for (int i = 0; i < totalModels; i++)
        {
            Transform modelTransform = unitObject.transform.GetChild(i);
            int row = i / modelsPerRow;
            int col = i % modelsPerRow;

            int modelsInThisRow = Mathf.Min(modelsPerRow, totalModels - row * modelsPerRow);
            float rowWidth = (modelsInThisRow - 1) * spacingX;
            float x = col * spacingX - rowWidth / 2f;
            float z = card.playerID == 1 ? -row * spacingZ : row * spacingZ;

            modelTransform.localPosition = new Vector3(x, 0, z);
        }
        if (isFormation)
        {
            // Set collider size to match the total size of the created unit
            int numRows = Mathf.CeilToInt((float)totalModels / modelsPerRow);
            float totalWidth = Mathf.Min(modelsPerRow, totalModels) * spacingX + 2;
            float totalDepth = numRows * spacingZ + 2;
            BoxCollider collider = unitObject.GetComponent<BoxCollider>();
            collider.size = new Vector3(totalWidth, 1.8f, totalDepth); // 1.6f for Y height, adjust as needed
            collider.center = new Vector3(0, 0.9f, -totalDepth / 4f); // Center collider vertically and horizontally
        }
    }


    public GameObject SpawnModelsInUnit(GameObject unitObject, Card card, bool isFormation)
    {
        if (card.unitPrefab == null)
        {
            Debug.LogWarning($"❌ Cannot spawn model: No prefab provided");
            return null;
        }

        GameObject modelInstance = Instantiate(card.unitPrefab, transform.position, Quaternion.identity);
        modelInstance.tag = $"Player{card.playerID}";
        modelInstance.transform.SetParent(unitObject.transform);
        if (card.playerID == 2) modelInstance.transform.rotation = Quaternion.Euler(0, 180, 0);
        modelInstance.GetComponent<Model>().isFormation = isFormation;
        modelInstance.GetComponent<Model>().unitCard = card;
        modelInstance.GetComponent<Model>().unitStats = card.SetUnitStats();
        modelInstance.GetComponent<Model>().unitDetailsPanel = unitDetailsPanel;

        return modelInstance;
    }
}