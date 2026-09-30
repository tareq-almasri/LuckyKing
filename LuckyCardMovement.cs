using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(LuckyCardDisplay))]
[RequireComponent(typeof(CanvasGroup))]
public class LuckyCardMovement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Plane boardPlane = new(Vector3.up, Vector3.zero);
    [Header("Dragging Settings")]
    public bool isDraggable = true;
    public float hoverScale = 1.1f;

    [Header("Arrow Settings")]
    public float tipOffset = 0.2f;

    [Header("Drag Preview Settings")]
    [Tooltip("Prefab for the deployment arrow (assigned by hand manager)")]
    public GameObject deploymentArrowPrefab;
    public float dragZOffset = -50f;
    public float hoverSpeed = 10f;

    [Header("Card Info")]
    private int ownerPlayerID = -1;
    private LuckyPlayer ownerPlayer;
    private LuckyHandManager ownerHandManager; // Store reference to hand manager

    private RectTransform rectTransform;
    private Transform regularTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private LuckyCardDisplay cardDisplay;

    private Vector3 originalPosition;
    public Vector3 originalScale;
    private Quaternion originalRotation;

    // Shadow management
    private GameObject shadowComponent;
    private Renderer shadowRenderer;
    private Transform originalParent;
    private int originalSiblingIndex;

    private bool isHovering = false;
    public bool isDragging = false;
    public bool wasPlayedSuccessfully = false;
    private bool isUICard = false; // Track if this is a UI card or 3D card

    // Deployment arrow system
    private GameObject deploymentArrow;
    private Canvas deploymentArrowCanvas;
    private Image deploymentArrowImage;
    

    private enum CardState
    {
        InHand,
        Hovering,
        Dragging,
        Returning
    }

    private CardState currentState = CardState.InHand;

    // Add dragStartTime field to class level
    private float dragStartTime = 0f;
    private DropZone[] allDropZones;

    private void Awake()
    {
        if (transform.parent != null && transform.parent.CompareTag("Details"))
        {
            enabled = false;
        }
        // Try to get RectTransform first (for UI cards)
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            isUICard = true;
            originalPosition = rectTransform.anchoredPosition;
        }
        else
        {
            // Fall back to regular Transform (for 3D cards)
            regularTransform = transform;
            isUICard = false;
            originalPosition = regularTransform.localPosition;
        }

        cardDisplay = GetComponent<LuckyCardDisplay>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }

        // Store original transform values
        if (isUICard)
        {
            originalScale = rectTransform.localScale;
            originalRotation = rectTransform.localRotation;
        }
        else
        {
            originalScale = regularTransform.localScale;
            originalRotation = regularTransform.localRotation;
        }

        // Initialize shadow management
        FindShadowComponent();
        UpdateShadowVisibility();

        FindOwnerPlayer();
    }

    void Start()
    {
        // Store original parent for returning to hand
        originalParent = GetParent();
        originalSiblingIndex = transform.GetSiblingIndex();
        // Find all drop zones in the scene
        allDropZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
    }

    private void FindOwnerPlayer()
    {
        LuckyHandManager handManager = GetComponentInParent<LuckyHandManager>();
        if (handManager != null)
        {
      
            LuckyGameManager gameManager = FindFirstObjectByType<LuckyGameManager>();
            if (gameManager != null)
            {
                ownerPlayer = gameManager.GetPlayerByHandManager(handManager);
            }

            if (ownerPlayer != null)
            {
                ownerPlayerID = ownerPlayer.playerID;
                Debug.Log($"🎴 Card owner found: Player {ownerPlayerID}");
            }
        }
    }

    // Helper methods to handle both UI and 3D transforms
    private Vector3 GetCurrentPosition()
    {
        if (isUICard && rectTransform != null)
            return rectTransform.anchoredPosition;
        else if (!isUICard && regularTransform != null)
            return regularTransform.localPosition;
        else
            return transform.localPosition; // Fallback to basic transform
    }

    private void SetPosition(Vector3 position)
    {
        if (isUICard && rectTransform != null)
            rectTransform.anchoredPosition = position;
        else if (!isUICard && regularTransform != null)
            regularTransform.localPosition = position;
        else
            transform.localPosition = position; // Fallback
    }
    private Vector3 GetCurrentScale()
    {
        if (isUICard && rectTransform != null)
            return rectTransform.localScale;
        else if (!isUICard && regularTransform != null)
            return regularTransform.localScale;
        else
            return transform.localScale; // Fallback
    }

    private void SetScale(Vector3 scale)
    {
        if (isUICard && rectTransform != null)
            rectTransform.localScale = scale;
        else if (!isUICard && regularTransform != null)
            regularTransform.localScale = scale;
        else
            transform.localScale = scale; // Fallback
    }

    private void SetAsLastSibling()
    {
        if (isUICard && rectTransform != null)
            rectTransform.SetAsLastSibling();
        else if (!isUICard && regularTransform != null)
            regularTransform.SetAsLastSibling();
        else
            transform.SetAsLastSibling(); // Fallback
    }

    private void SetParent(Transform parent)
    {
        if (isUICard && rectTransform != null)
            rectTransform.SetParent(parent);
        else if (!isUICard && regularTransform != null)
            regularTransform.SetParent(parent);
        else
            transform.SetParent(parent); // Fallback
    }

    private void SetSiblingIndex(int index)
    {
        if (isUICard && rectTransform != null)
            rectTransform.SetSiblingIndex(index);
        else if (!isUICard && regularTransform != null)
            regularTransform.SetSiblingIndex(index);
        else
            transform.SetSiblingIndex(index); // Fallback
    }

    private Transform GetParent()
    {
        if (isUICard && rectTransform != null)
            return rectTransform.parent;
        else if (!isUICard && regularTransform != null)
            return regularTransform.parent;
        else
            return transform.parent; // Fallback
    }

    private int GetSiblingIndex()
    {
        if (isUICard && rectTransform != null)
            return rectTransform.GetSiblingIndex();
        else if (!isUICard && regularTransform != null)
            return regularTransform.GetSiblingIndex();
        else
            return transform.GetSiblingIndex(); // Fallback
    }

    public void SetOwnerPlayer(int playerID)
    {
        ownerPlayerID = playerID;

        // Find the LuckyGameManager to get the players
        LuckyGameManager gameManager = FindFirstObjectByType<LuckyGameManager>();
        if (gameManager != null)
        {
            ownerPlayer = gameManager.GetPlayer(playerID);
        }
    }
    
    public void SetHandManager(LuckyHandManager handManager)
    {
        // Store the hand manager reference
        ownerHandManager = handManager;

        if (handManager != null && ownerPlayer == null)
        {
        
            LuckyGameManager gameManager = FindFirstObjectByType<LuckyGameManager>();
            if (gameManager != null)
            {
                // Try to determine which player this hand belongs to
                // Check if this hand manager belongs to player 1 or player 2
                ownerPlayer = gameManager.GetPlayerByHandManager(handManager);
            }

            if (ownerPlayer != null)
            {
                ownerPlayerID = ownerPlayer.playerID;
                Debug.Log($"🎴 Card owner set via HandManager: Player {ownerPlayerID}");
            }
        }
    }


    private bool CanInteract()
    {
        // Basic interaction checks
        if (!isDraggable)
        {
            Debug.Log($"🚫 {gameObject.name}: Not draggable");
            return false;
        }

        if (isDragging)
        {
            Debug.Log($"🚫 {gameObject.name}: Currently dragging");
            return false;
        }

        if (wasPlayedSuccessfully)
        {
            Debug.Log($"🚫 {gameObject.name}: Already played successfully");
            return false;
        }

        // Only allow interaction if it's this player's turn and dice are not animating
        LuckyGameManager gameManager = FindFirstObjectByType<LuckyGameManager>();
        if (gameManager != null && ownerPlayer != null)
        {
            if (!gameManager.CurrentPlayer.isLocalPlayer)
            {
                return false; // Not this player's turn
            }
            if (gameManager.CurrentPlayer != ownerPlayer)
            {
                Debug.Log($"🚫 {gameObject.name}: Not this player's turn");
                return false;
            }
            if (ownerPlayer.AreDiceAnimating())
            {
                Debug.Log($"🚫 {gameObject.name}: Dice are animating, cannot play card");
                return false;
            }
        }

        // Check with hand manager if this card is allowed to be interacted with
        if (ownerHandManager != null)
        {
            bool canInteract = ownerHandManager.CanInteractWithCard(gameObject);
            if (!canInteract)
            {
                Debug.Log($"🚫 {gameObject.name}: Hand manager blocked interaction");
            }
            return canInteract;
        }
        else
        {
            Debug.Log($"⚠️ {gameObject.name}: No owner hand manager - allowing interaction");
        }

        return true; // Default to true if no hand manager
    }
    public CardDetailsDisplay cardDetailsDisplay;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Always allow hovering, regardless of turn, player, or hand manager
        if (isDragging) return; // Ignore if dragging

        isHovering = true;
        currentState = CardState.Hovering;

        /*originalPosition = GetCurrentPosition();
        originalRotation = isUICard ? rectTransform.localRotation : regularTransform.localRotation;
        */
        SetScale(originalScale * hoverScale);
        SetAsLastSibling();

        // Ensure cardDetailsDisplay is assigned, even if inactive
        if (cardDetailsDisplay == null)
        {
            cardDetailsDisplay = FindFirstObjectByType<CardDetailsDisplay>(FindObjectsInactive.Include);
        }
        // Show card details if available
        if (cardDetailsDisplay != null && !ownerPlayer.isLocalPlayer && ownerHandManager != null && ownerHandManager.cardsInHand.Contains(gameObject))
        {
            cardDetailsDisplay.gameObject.SetActive(false);
        }
        else if (cardDetailsDisplay != null)
        {
            cardDetailsDisplay.gameObject.SetActive(true);
            cardDisplay.GetCard().SetUnitStats();
            cardDetailsDisplay.ShowDetails(cardDisplay.GetCard());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Completely ignore pointer exit events while dragging
        if (isDragging) return;
        isHovering = false;
        SetScale(originalScale);
        // Only hide if this card was showing the details
        if (cardDetailsDisplay != null && cardDetailsDisplay.gameObject.activeSelf)
        {
            cardDetailsDisplay.gameObject.SetActive(false);
        }
        // Delay pointer exit logic to avoid race conditions
      //  StartCoroutine(DelayedPointerExit());
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"🎯 OnBeginDrag called on card: {gameObject.name}");
        if (!CanInteract())
        {
            Debug.Log($"🎯 CanInteract returned false for card: {gameObject.name}");
            return; // Check if interaction is allowed
        }
        Debug.Log($"🎯 OnBeginDrag proceeding - CanInteract: true");
        isDragging = true;
        wasPlayedSuccessfully = false;
        currentState = CardState.Dragging;
        dragStartTime = Time.time; // Record drag start time
        // Keep original card in hand with hover effect (shows it's being dragged)
        originalPosition = GetCurrentPosition();
        originalParent = GetParent();
        originalSiblingIndex = GetSiblingIndex();
        // Create deployment arrow
        CreateDeploymentArrow(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Update deployment arrow position and color
        UpdateDeploymentArrow(eventData);

        // Show green arrow if affordable, red if not
        bool canAfford = ownerPlayer.CanAffordCard(cardDisplay.GetCard());

        //any and all drop zones hide glow 

        foreach (DropZone zone in allDropZones)
        {
            zone.highlightObject.SetActive(false); // Hide all highlights initially
        }

        // show red if invalid zone
        if (DropZoneManager.Instance != null)
        {
            DropZone dropZone = DropZoneManager.Instance.GetDropZoneAtScreenPosition(eventData.position, gameObject, ownerPlayerID, canAfford);
            if (dropZone != null && dropZone.CanAcceptCard(gameObject, ownerPlayerID, canAfford))
            {
                UpdateArrowColor(true); // Show green arrow
            }
            else
            {
                UpdateArrowColor(false); // Show red arrow

            }
        }
        else
        {
            Debug.LogWarning($"❌ DropZoneManager.Instance is null during OnDrag for card {gameObject.name}");
        }

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Minimum drag duration (in seconds)
        float minDragTime = 0.15f;
        float dragDuration = Time.time - dragStartTime;
        isHovering = false;

        if (cardDetailsDisplay != null && cardDetailsDisplay.gameObject.activeSelf)
        {
            cardDetailsDisplay.gameObject.SetActive(false);
        }
        if (dragDuration < minDragTime)
        {
            // Treat as click, not drag
            isDragging = false;
            DestroyDeploymentArrow();
            SetScale(originalScale);
            return;
        }
        // Always reset dragging state first
        isDragging = false;

        // Destroy the deployment arrow
        DestroyDeploymentArrow();

        // Only allow play if affordable
        Card draggedCard = cardDisplay.GetCard();
        if (!ownerPlayer.CanAffordCard(draggedCard))
        {
            SetScale(originalScale);
            return;
        }

        // Use the cursor position for drop detection 
        Vector2 dropPosition = eventData.position;

        if (!CanInteract())
        {
            return; // Check if interaction is allowed
        }

        // Try to drop the card
        bool cardWasPlayed = false;
        if (DropZoneManager.Instance != null)
        {
            cardWasPlayed = DropZoneManager.Instance.TryDropCard(dropPosition, gameObject, ownerPlayerID, ownerPlayer.CanAffordCard(draggedCard));
        }

        if (cardWasPlayed)
        {
            // Only subtract DP if deployment was valid
            bool playResult = ownerPlayer.TryPlayCard(draggedCard);
            if (!playResult)
            {
                Debug.LogWarning($"❌ Could not play card {draggedCard.cardName} - not enough DP");
                SetScale(originalScale);
                return;
            }
            wasPlayedSuccessfully = true;
            if (draggedCard.IsUnitCard)
            {
                // Roll dice for this player after unit card is played
                if (ownerPlayer != null)
                {
                    Debug.Log($"[CARD MOVEMENT] Rolling dice for player {ownerPlayer.playerID} after playing unit card {draggedCard.cardName}");
                    StartCoroutine(HandleUnitCardDeployment(draggedCard));
                }
                else
                {
                    Debug.LogWarning($"❌ Owner player not set for card {gameObject.name} - cannot deploy unit");
                }
            }
            else
            {
                // Effect cards are handled by DropZone (they are destroyed after valid deployment)
                // No further action needed
                DisableCardMovement();
            }
            return;
        }
        //any and all drop zones hide glow 
        foreach (DropZone zone in allDropZones)
        {
            zone.highlightObject.SetActive(false); // Hide all highlights initially
        }
        // If not played, return to hand
        SetScale(originalScale);
    }
    public IEnumerator HandleUnitCardDeployment(Card draggedCard)
    {
        StartCoroutine(ownerPlayer.RollUnitDice(draggedCard));
        Debug.Log($"📦 Unit card {draggedCard.cardName} deployed successfully");
        DisableCardMovement();
        Debug.Log($"📦 Unit card {draggedCard.cardName} deployed and staying on board");
        // Delay before updating the card display
        yield return new WaitForSeconds(0.7f);
        cardDisplay.SetCard(draggedCard); // Update display with new card state
        // Refresh details panel if visible
        if (cardDetailsDisplay != null && cardDetailsDisplay.gameObject.activeSelf)
        {
            cardDisplay.GetCard().SetUnitStats();
            cardDetailsDisplay.ShowDetails(cardDisplay.GetCard());
        }
    }

  


    public void DisableCardMovement()
    {
        Debug.Log($"🔒 Disabling movement for deployed unit card: {gameObject.name}");


        // Remove from hand manager using stored reference (not parent hierarchy)
        if (ownerHandManager != null)
        {
            Debug.Log($"🔧 Removing card from hand via stored HandManager reference");
            ownerHandManager.RemoveCardFromHand(gameObject);
        }
        else
        {
            // Fallback: try to find via parent hierarchy (shouldn't be needed now)
            LuckyHandManager handManager = GetComponentInParent<LuckyHandManager>();
            if (handManager != null)
            {
                Debug.LogWarning($"🔧 Fallback: Found HandManager via parent hierarchy");
                handManager.RemoveCardFromHand(gameObject);
            }
            else
            {
                Debug.LogError($"❌ Could not find HandManager to remove card {gameObject.name} from hand!");
            }
        }

        // Disable dragging and visual effects
        isDraggable = false;
        isDragging = false;
        isHovering = false;
        wasPlayedSuccessfully = true;

        // Reset visual state
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        SetScale(originalScale); // Reset to original size when deployed

        // Enable shadow for deployed cards
        UpdateShadowVisibility();

        // Disable dragging and playing, but keep hover logic enabled
        isDraggable = false;
        isDragging = false;
        // Hover logic remains enabled so deployed cards can still show details and glow
    }

    // Shadow Management
    private void FindShadowComponent()
    {
        if (shadowComponent != null && shadowRenderer != null) return;

        Debug.Log($"🔍 Searching for shadow component in card: {gameObject.name}");

        // Look for CardBack child (which has the shadow)
        Transform cardBack = transform.Find("CardBack");
        if (cardBack != null)
        {
            Debug.Log($"🔍 Found CardBack: {cardBack.name}");
            shadowComponent = cardBack.gameObject;

            // Try to get renderer from CardBack itself
            shadowRenderer = shadowComponent.GetComponent<Renderer>();
            if (shadowRenderer != null)
            {
                Debug.Log($"🌑 Found Renderer on CardBack: {shadowComponent.name} for card {gameObject.name}");
                return;
            }

            // If CardBack is a prefab, the renderer might be on a child
            shadowRenderer = shadowComponent.GetComponentInChildren<Renderer>();
            if (shadowRenderer != null)
            {
                Debug.Log($"🌑 Found Renderer in CardBack children: {shadowRenderer.gameObject.name} for card {gameObject.name}");
                return;
            }

            Debug.LogWarning($"⚠️ CardBack found but no Renderer component found in {cardBack.name} or its children");
        }
        else
        {
            Debug.LogWarning($"⚠️ CardBack not found as child of {gameObject.name}");
        }

        // Debug: List all children
        Debug.Log($"🔍 Children of {gameObject.name}:");
        foreach (Transform child in transform)
        {
            Debug.Log($"  - {child.name}");
        }

        // Fallback: Look for other common shadow names as direct children
        string[] shadowNames = { "Shadow", "shadow", "CardShadow", "DropShadow" };

        foreach (string shadowName in shadowNames)
        {
            Transform shadow = transform.Find(shadowName);
            if (shadow != null)
            {
                shadowComponent = shadow.gameObject;
                shadowRenderer = shadowComponent.GetComponent<Renderer>();

                if (shadowRenderer == null)
                    shadowRenderer = shadowComponent.GetComponentInChildren<Renderer>();

                if (shadowRenderer != null)
                {
                    Debug.Log($"🌑 Found shadow component with Renderer: {shadowComponent.name} for card {gameObject.name}");
                    return;
                }
            }
        }

        // Last fallback: Look for any child with "shadow" or "back" in the name that has a Renderer
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("shadow") || child.name.ToLower().Contains("back"))
            {
                if (!child.TryGetComponent<Renderer>(out var childRenderer))
                    childRenderer = child.GetComponentInChildren<Renderer>();

                if (childRenderer != null)
                {
                    shadowComponent = child.gameObject;
                    shadowRenderer = childRenderer;
                    Debug.Log($"🌑 Found shadow-like component with Renderer: {shadowComponent.name} -> {shadowRenderer.gameObject.name} for card {gameObject.name}");
                    return;
                }
            }
        }

        if (shadowComponent == null || shadowRenderer == null)
        {
            Debug.LogWarning($"⚠️ No shadow component with Renderer found for card {gameObject.name}");
        }
    }

    private void UpdateShadowVisibility()
    {
        if (shadowRenderer == null)
        {
            FindShadowComponent();
        }

        if (shadowRenderer != null)
        {
            // Debug the parent hierarchy
            Debug.Log($"🔍 Checking parent hierarchy for {gameObject.name}:");
            Transform current = transform;
            int level = 0;
            while (current != null && level < 10)
            {
                Debug.Log($"  Level {level}: {current.name} ({current.GetType().Name})");
                if (current.TryGetComponent<LuckyHandManager>(out var handMgr))
                {
                    Debug.Log($"    ✅ Found LuckyHandManager on {current.name}");
                }
                current = current.parent;
                level++;
            }

            // Primary check: if the card was successfully played, it's deployed
            bool isDeployed = wasPlayedSuccessfully;

            // Secondary check: if not played successfully, check if still in hand
            if (!isDeployed)
            {
                LuckyHandManager handManager = GetComponentInParent<LuckyHandManager>();
                bool isInHand = handManager != null;

                // If we can't find hand manager, assume it's in hand initially
                // (cards start in hand and only become deployed after successful play)
                if (handManager == null && !wasPlayedSuccessfully)
                {
                    Debug.Log($"🔍 No HandManager found and not played successfully - assuming IN HAND for {gameObject.name}");
                    isDeployed = false; // Stay in hand
                }
                else
                {
                    isDeployed = !isInHand;
                }
            }

            Debug.Log($"🔍 Shadow check for {gameObject.name}: wasPlayedSuccessfully={wasPlayedSuccessfully}, isDeployed={isDeployed}");

            // Enable shadow casting when deployed, disable when in hand
            if (isDeployed)
            {
                shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                Debug.Log($"🌑 Shadow casting ON for {gameObject.name} (deployed)");
            }
            else
            {
                shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Debug.Log($"🌑 Shadow casting OFF for {gameObject.name} (in hand)");
            }
        }
    }

    // Call this when card state changes
    public void RefreshShadowVisibility()
    {
        UpdateShadowVisibility();
    }

    public bool IsDragging => isDragging;

    void OnDestroy()
    {
        // Clean up deployment arrow if it exists
        DestroyDeploymentArrow();
    }

    // Deployment Arrow System Methods

    private void CreateDeploymentArrow(PointerEventData eventData)
    {
        if (deploymentArrowPrefab == null)
        {
            Debug.LogWarning("🎯 No deployment arrow prefab assigned - cannot create arrow");
            return;
        }

        // Instantiate the arrow prefab (if needed)
        deploymentArrow = Instantiate(deploymentArrowPrefab);
        Debug.Log($"🎯 Created deployment arrow: {deploymentArrow.name}");

        // Get the Canvas component (direct child of DeploymentArrow)
        deploymentArrowCanvas = deploymentArrow.GetComponentInChildren<Canvas>();
        if (deploymentArrowCanvas != null)
        {
            // Setup for World Space rendering
            deploymentArrowCanvas.renderMode = RenderMode.WorldSpace;
            deploymentArrowCanvas.sortingOrder = 1000; // Render on top
            // Do NOT rotate the canvas here; let the arrow image handle rotation
            Debug.Log($"🎯 Canvas configured for World Space rendering");
        }
        else
        {
            Debug.LogError("🎯 No Canvas found in deployment arrow prefab!");
        }

        // Find the ArrowImage inside Canvas: DeploymentArrow -> Canvas -> ArrowImage
        Transform canvasTransform = deploymentArrow.transform.Find("Canvas");
        if (canvasTransform != null)
        {
            Transform arrowImageTransform = canvasTransform.Find("ArrowImage");
            if (arrowImageTransform != null)
            {
                deploymentArrowImage = arrowImageTransform.GetComponent<UnityEngine.UI.Image>();
                if (deploymentArrowImage != null)
                {
                    Debug.Log($"🎯 Found ArrowImage component successfully");
                    deploymentArrowImage.color = Color.white; // Start with neutral color
                }
                else
                {
                    Debug.LogError("🎯 ArrowImage GameObject found but no Image component!");
                }
            }
            else
            {
                Debug.LogError("🎯 ArrowImage not found as child of Canvas");
            }
        }
        else
        {
            Debug.LogError("🎯 Canvas not found as child of DeploymentArrow");
        }

        // Position arrow initially
        UpdateDeploymentArrow(eventData);
    }

    private void UpdateDeploymentArrow(PointerEventData eventData)
    {
        if (deploymentArrow == null) return;

        // Get card world position (start of arrow)
        Vector3 cardWorldPos = transform.position;
        cardWorldPos.y = 5f; // Elevate above the board (optional for visibility)

        // Get cursor world position (end of arrow)


        // Find the arrow image's RectTransform
        RectTransform arrowRect = deploymentArrowImage != null ? deploymentArrowImage.rectTransform : deploymentArrow.GetComponent<RectTransform>();
        if (arrowRect == null) return;

        // Get the canvas RectTransform (assume arrow is under the correct canvas)
        Canvas parentCanvas = arrowRect.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("Deployment arrow is not under a Canvas!");
            return;
        }
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        // Convert card and pointer positions to local positions in the canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Camera.main.WorldToScreenPoint(cardWorldPos), Camera.main, out Vector2 cardLocalPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, Camera.main, out Vector2 pointerLocalPos);

        // Position arrow so its left edge (pivot.x = 0) is at the card position
        arrowRect.localPosition = cardLocalPos;

        // Set arrow length
        float length = Vector2.Distance(cardLocalPos, pointerLocalPos);
        
        arrowRect.sizeDelta = new Vector2(arrowRect.sizeDelta.x, length); // Stretch vertically

        // Rotate arrow to point from card to pointer
        Vector2 dir = pointerLocalPos - cardLocalPos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // Subtract 90 to align arrow image
        arrowRect.localRotation = Quaternion.Euler(0, 0, angle);

        // Make sure the arrow's pivot is set to (0, 0.5) in the prefab for correct stretching

        Debug.Log($"🎯 UI Arrow updated (World Space Canvas) - CardLocal: {cardLocalPos}, PointerLocal: {pointerLocalPos}, Length: {length}, Angle: {angle}");
    }

    private void UpdateArrowColor(bool isValidDrop)
    {
        if (deploymentArrowImage == null) return;

        Color targetColor = isValidDrop ? Color.green : Color.red;
        targetColor.a = 0.8f; // Semi-transparent
        deploymentArrowImage.color = targetColor;

        Debug.Log($"🎯 Arrow color updated to {(isValidDrop ? "GREEN" : "RED")}");
    }

    private void DestroyDeploymentArrow()
    {
        if (deploymentArrow != null)
        {
            Destroy(deploymentArrow); // Remove arrow after use
            deploymentArrow = null;
            deploymentArrowCanvas = null;
            deploymentArrowImage = null;
            Debug.Log($"🎯 Deployment arrow destroyed");
        }
        //any and all drop zones hide glow 
        DropZone[] allDropZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);

        foreach (DropZone zone in allDropZones)
        {
            zone.highlightObject.SetActive(false); // Hide all highlights
        }
    }

    public bool IsHovering => isHovering;
    public bool WasDeployed => wasPlayedSuccessfully;
    public int OwnerPlayerID => ownerPlayerID;
    public LuckyPlayer OwnerPlayer => ownerPlayer;
}
