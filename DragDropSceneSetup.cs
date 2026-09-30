using UnityEngine;

public class DragDropSceneSetup : MonoBehaviour
{
    [Header("Scene Setup")]
    public bool setupOnStart = true;
    
    void Start()
    {
        if (setupOnStart)
        {
            SetupDragDropSystem();
        }
    }
    
    [ContextMenu("Setup Drag & Drop System")]
    public void SetupDragDropSystem()
    {
        Debug.Log("🔧 Setting up Drag & Drop System...");
        
        // 1. Create DropZoneManager if it doesn't exist
        DropZoneManager manager = FindFirstObjectByType<DropZoneManager>();
        if (manager == null)
        {
            GameObject managerObj = new("DropZoneManager");
            manager = managerObj.AddComponent<DropZoneManager>();
            Debug.Log("✅ Created DropZoneManager");
        }
        
        // 2. Create DropZoneCreator if it doesn't exist
        DropZoneCreator creator = FindFirstObjectByType<DropZoneCreator>();
        if (creator == null)
        {
            GameObject creatorObj = new("DropZoneCreator");
            creator = creatorObj.AddComponent<DropZoneCreator>();
            Debug.Log("✅ Created DropZoneCreator");
        }
        
        // 3. Create drop zones
        creator.CreateAllDropZones();
        
        // 4. Make sure all existing cards have LuckyCardMovement
        SetupCardDragComponents();
        
        Debug.Log("🎉 Drag & Drop System setup complete!");
    }
    
    private void SetupCardDragComponents()
    {
        LuckyCardDisplay[] allCards = FindObjectsByType<LuckyCardDisplay>(FindObjectsSortMode.None);
        int setupCount = 0;
        
        foreach (LuckyCardDisplay card in allCards)
        {
            if (!card.TryGetComponent<LuckyCardMovement>(out var movement))
            {
                movement = card.gameObject.AddComponent<LuckyCardMovement>();
                setupCount++;
            }
            
            // Ensure CanvasGroup exists
            if (!card.TryGetComponent<CanvasGroup>(out var canvasGroup))
            {
                card.gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        if (setupCount > 0)
        {
            Debug.Log($"✅ Added LuckyCardMovement to {setupCount} cards");
        }
    }
}
