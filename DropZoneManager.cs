using UnityEngine;
using System.Collections.Generic;

public class DropZoneManager : MonoBehaviour
{
    [Header("Drop Zone Detection")]
    public LayerMask dropZoneLayer = 1; // What layer drop zones are on
    public float raycastDistance = 1000f;
    
    [Header("Visual Feedback")]
    public bool showDebugRays = true;
    
    private static DropZoneManager instance;
    public List<DropZone> allDropZones = new();
    // Highlighting logic has been removed; comments retained for reference
    private Camera mainCamera;
    
    public static DropZoneManager Instance => instance;

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }
        RefreshDropZones();
        
    }
    
    void Start()
    {
        // Find all drop zones in scene
        RefreshDropZones();
        
        // Verify camera
        if (mainCamera == null)
        {
            Debug.LogError("❌ DropZoneManager: No camera found! Raycast will fail.");
        }
        else
        {
            Debug.Log($"✅ DropZoneManager initialized with camera: {mainCamera.name}");
        }
    }
    
    public void RefreshDropZones()
    {
        //allDropZones.Clear();
        allDropZones = new List<DropZone>(FindObjectsByType<DropZone>(FindObjectsSortMode.None));
        allDropZones.RemoveAll(zone => zone == null);
        Debug.Log($"🔄 DropZoneManager: Found {allDropZones.Count} drop zones in scene.");
    }
    
    public DropZone GetDropZoneAtScreenPosition(Vector2 screenPosition, GameObject cardObject, int playerID, bool canAfford)
    {
        if (mainCamera == null)
        {
            Debug.LogError("❌ DropZoneManager: mainCamera is null! Cannot perform raycast.");
            return null;
        }

        // Ensure we have drop zones
        if (allDropZones.Count == 0)
        {
            Debug.LogWarning("⚠️ No drop zones found! Refreshing...");
            RefreshDropZones();

            if (allDropZones.Count == 0)
            {
                Debug.LogError("❌ Still no drop zones after refresh! Check DropZoneCreator.");
                return null;
            }
        }

        // Create ray from screen position
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (showDebugRays)
        {
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 0.1f);
          
        }

        // Raycast to find drop zones
        RaycastHit[] hits = Physics.RaycastAll(ray, raycastDistance, dropZoneLayer);


        // Find the closest valid drop zone
        DropZone bestZone = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            DropZone zone = hit.collider.GetComponent<DropZone>();
            if (zone != null && hit.distance < closestDistance)
            {
                Debug.Log($"🔍 Found DropZone {zone.name} at distance {hit.distance}");

                // Check if this zone can accept the card
                if (zone.CanAcceptCard(cardObject, playerID, canAfford))
                {
                    bestZone = zone;
                    closestDistance = hit.distance;
                    Debug.Log($"✅ Zone {zone.name} can accept card for player {playerID}");
                }
                else
                {
                    Debug.Log($"❌ Zone {zone.name} cannot accept card for player {playerID}");
                }
            }
        }

        if (bestZone == null)
        {
            Debug.LogWarning($"❌ No valid drop zone found at screen position {screenPosition} for player {playerID}");
        }

        return bestZone;
    }
    
   
    public bool TryDropCard(Vector2 screenPosition, GameObject cardObject, int playerID, bool canAfford)
    {
        DropZone dropZone = GetDropZoneAtScreenPosition(screenPosition, cardObject, playerID, canAfford);
        if (dropZone != null && dropZone.CanAcceptCard(cardObject, playerID, canAfford))
        {
            // Successfully dropped card
            dropZone.OnCardDropped(cardObject);
            
            
            Debug.Log($"✅ Card successfully dropped on {dropZone.name}");
            return true;
        }
        
        // Failed to drop
        Debug.Log($"❌ No valid drop zone found at position {screenPosition}");
        return false;
    }
    

}
