using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DropZoneSetup
{
    public string zoneName;
    public int slotIndex; // Slot index for the zone (1-10)
    public Vector3 position;
    public Vector3 size = new Vector3(3.3f, 2, 4.7f);
    public Vector3 cardPositionOffset = new Vector3(0, -3.8f, 0); // Default offset for cards in this zone
    // All zones are now generic; zoneType logic removed
    public int playerId = -1; // -1 for any player, specific ID for player-only zones
}

public class DropZoneCreator : MonoBehaviour
{
    public int localPlayerID = 1; // Set this based on local player (1 or 2)
    [Header("Drop Zone Setup")]
    public DropZoneSetup[] dropZones = new DropZoneSetup[]
    {
        // Player 1 Zones (10 slots)
        new() { zoneName = "Player1_Slot1", slotIndex = 1, position = new Vector3(-6.15f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot2", slotIndex = 2, position = new Vector3(-2.83f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot3", slotIndex = 3, position = new Vector3(0.45f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot4", slotIndex = 4, position = new Vector3(3.75f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot5", slotIndex = 5, position = new Vector3(7.05f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot6", slotIndex = 6, position = new Vector3(-6.15f, 0, -7.35f), playerId = 1 },
        new() { zoneName = "Player1_Slot7", slotIndex = 7, position = new Vector3(-2.83f, 0, -7.35f), playerId = 1 },
        new() { zoneName = "Player1_Slot8", slotIndex = 8, position = new Vector3(0.45f, 0, -7.35f), playerId = 1 },
        new() { zoneName = "Player1_Slot9", slotIndex = 9, position = new Vector3(3.75f, 0, -7.35f), playerId = 1 },
        new() { zoneName = "Player1_Slot10", slotIndex = 10, position = new Vector3(7.05f, 0, -7.35f), playerId = 1 },
        // Player 2 Zones (10 slots)
        new() { zoneName = "Player2_Slot1", slotIndex = 1, position = new Vector3(7.05f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot2", slotIndex = 2, position = new Vector3(3.75f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot3", slotIndex = 3, position = new Vector3(0.45f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot4", slotIndex = 4, position = new Vector3(-2.83f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot5", slotIndex = 5, position = new Vector3(-6.15f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot6", slotIndex = 6, position = new Vector3(7.05f, 0, 6.9f), playerId = 2 },
        new() { zoneName = "Player2_Slot7", slotIndex = 7, position = new Vector3(3.75f, 0, 6.9f), playerId = 2 },
        new() { zoneName = "Player2_Slot8", slotIndex = 8, position = new Vector3(0.45f, 0, 6.9f), playerId = 2 },
        new() { zoneName = "Player2_Slot9", slotIndex = 9, position = new Vector3(-2.83f, 0, 6.9f), playerId = 2 },
        new() { zoneName = "Player2_Slot10", slotIndex = 10, position = new Vector3(-6.15f, 0, 6.9f), playerId = 2 }
    };

    [Header("Visual Settings")]
    public GameObject highlightPrefab;
    public LayerMask dropZoneLayer = 1;

    [Header("Default Settings")]
    [Tooltip("Default card position offset applied to all zones (can be overridden per zone)")]
    public Vector3 globalCardPositionOffset = new(0, 0, 0); // Slight elevation by default

    [Header("Debug")]
    [Tooltip("If enabled, zones will be recreated at game start (overwrites manual changes!)")]
    public bool recreateZonesOnStart = false; // Changed default to false
    public bool showGizmos = true;
    public bool createVisibleZones = false; // Toggle for creating visible zones (development only)

    private void Awake()
    {
        // Ensure only one DropZoneCreator exists
        if (FindObjectsByType<DropZoneCreator>(FindObjectsSortMode.None).Length > 1)
        {
            Debug.LogError("❌ Multiple DropZoneCreator instances found! There should only be one per scene.");
            Destroy(this);
        }
    }
    void Start()
    {
        Debug.Log($"🎯 DropZoneCreator.Start() - recreateZonesOnStart: {recreateZonesOnStart}");
        localPlayerID = LuckyGameManager.Instance.localPlayerID;
        if (recreateZonesOnStart)
        {
            Debug.Log($"🎯 Creating drop zones at Start()...");
            CreateAllDropZones();
        }
        else
        {
            Debug.Log($"🎯 Not recreating zones - using existing ones if any");

            DropZone[] existingZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
            if (existingZones.Length == 0)
            {
                Debug.LogWarning("⚠️ No drop zones found and recreateZonesOnStart is false! Creating default zones...");
                CreateAllDropZones();
            }
            else
            {
                Debug.Log($"✅ Found {existingZones.Length} existing drop zones");
            }
        }
    }

    [ContextMenu("Create All Drop Zones")]
    public void CreateAllDropZones()
    {
        // Clear existing drop zones
        ClearExistingDropZones();

        foreach (var setup in dropZones)
        {
            CreateDropZone(setup);
        }

        Debug.Log($"🎯 Created {dropZones.Length} drop zones");

    }

    [ContextMenu("Reset Array to Default (20 zones)")]
    public void ResetArrayToDefault()
    {
        dropZones = GetDefaultDropZones();
        Debug.Log($"✅ Reset dropZones array to {dropZones.Length} zones");
    }

    private DropZoneSetup[] GetDefaultDropZones()
    {
        return new DropZoneSetup[]
         {
        // Player 1 Zones (10 slots)
        new() { zoneName = "Player1_Slot1", slotIndex = 1, position = new Vector3(-6.15f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot2", slotIndex = 2, position = new Vector3(-2.83f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot3", slotIndex = 3, position = new Vector3(0.45f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot4", slotIndex = 4, position = new Vector3(3.75f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot5", slotIndex = 5, position = new Vector3(7.05f, 0, -2.65f), playerId = 1 },
        new() { zoneName = "Player1_Slot6", slotIndex = 6, position = new Vector3(-6.15f, 0, -7.35f), playerId = 1 },
        new() { zoneName = "Player1_Slot7", slotIndex = 7, position = new Vector3(-2.83f, 0, -7.35f), playerId = 1 },
        new() { zoneName = "Player1_Slot8", slotIndex = 8, position = new Vector3(0.45f, 0, -7.35f), playerId = 1 },
        new() { zoneName = "Player1_Slot9", slotIndex = 9, position = new Vector3(3.75f, 0, -7.35f), playerId = 1 },
        new() { zoneName = "Player1_Slot10", slotIndex = 10, position = new Vector3(7.05f, 0, -7.35f), playerId = 1 },
        // Player 2 Zones (10 slots)
        new() { zoneName = "Player2_Slot1", slotIndex = 1, position = new Vector3(7.05f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot2", slotIndex = 2, position = new Vector3(3.75f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot3", slotIndex = 3, position = new Vector3(0.45f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot4", slotIndex = 4, position = new Vector3(-2.83f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot5", slotIndex = 5, position = new Vector3(-6.15f, 0, 2.2f), playerId = 2 },
        new() { zoneName = "Player2_Slot6", slotIndex = 6, position = new Vector3(7.05f, 0, 6.9f), playerId = 2 },
        new() { zoneName = "Player2_Slot7", slotIndex = 7, position = new Vector3(3.75f, 0, 6.9f), playerId = 2 },
        new() { zoneName = "Player2_Slot8", slotIndex = 8, position = new Vector3(0.45f, 0, 6.9f), playerId = 2 },
        new() { zoneName = "Player2_Slot9", slotIndex = 9, position = new Vector3(-2.83f, 0, 6.9f), playerId = 2 },
        new() { zoneName = "Player2_Slot10", slotIndex = 10, position = new Vector3(-6.15f, 0, 6.9f), playerId = 2 }
    };
    }

    private void ClearExistingDropZones()
    {
        DropZone[] existingZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        for (int i = existingZones.Length - 1; i >= 0; i--)
        {
            if (existingZones[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(existingZones[i].gameObject);
                }
                else
                {
                    DestroyImmediate(existingZones[i].gameObject);
                }
            }
        }
    }

    private GameObject CreateDropZone(DropZoneSetup setup)
    {
        // Create GameObject
        GameObject zoneObject = new(setup.zoneName);
        zoneObject.transform.position = setup.position;
        if (localPlayerID == 2) zoneObject.transform.Rotate(0, 180, 0);
        // Convert LayerMask to layer index safely
        int layerIndex = 0;
        int mask = dropZoneLayer.value;
        while (mask > 1)
        {
            mask >>= 1;
            layerIndex++;
        }
        zoneObject.layer = layerIndex;

        Debug.Log($"🎯 Creating drop zone {setup.zoneName} on layer {layerIndex} (LayerMask: {dropZoneLayer.value})");

        // Add collider (always needed for functionality)
        BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
        collider.size = setup.size;
        collider.isTrigger = true;


        // Add DropZone component
        DropZone dropZone = zoneObject.AddComponent<DropZone>();
        dropZone.playerID = setup.playerId;
        dropZone.slotIndex = setup.slotIndex;
        dropZone.isOccupied = false;
        dropZone.isLocalPlayer = setup.playerId == localPlayerID;
        // Create highlight object from prefab and parent it to the zone
        if (highlightPrefab != null)
        {
            GameObject highlight = Instantiate(highlightPrefab, zoneObject.transform);
            highlight.name = "highlightObject";
            highlight.transform.localPosition = new Vector3(0, 0.5f, 0);
            // highlight.transform.localScale = setup.size;
            highlight.layer = zoneObject.layer;
            // Optionally disable highlight by default
            highlight.SetActive(false);
            dropZone.highlightObject = highlight;
        }
      
        Vector3 offsetToUse = setup.cardPositionOffset != Vector3.zero ? setup.cardPositionOffset : globalCardPositionOffset;
        dropZone.cardPositionOffset = offsetToUse;


        Debug.Log($"✅ Created {(createVisibleZones ? "visible" : "invisible")} drop zone: {setup.zoneName} at {setup.position}");
        return zoneObject;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        foreach (var setup in dropZones)
        {
            // Color by playerId: blue for player 1, red for player 2, white for others
            if (setup.playerId == 1)
                Gizmos.color = Color.blue;
            else if (setup.playerId == 2)
                Gizmos.color = Color.red;
            else
                Gizmos.color = Color.white;

            Gizmos.DrawWireCube(setup.position, setup.size);
        }
    }

    [ContextMenu("Clear All Drop Zones")]
    public void ClearAllDropZones()
    {
        ClearExistingDropZones();
        Debug.Log("🗑️ Cleared all drop zones");
    }
}
