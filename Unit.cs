using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections;
using UnityEngine.EventSystems;

public class Unit : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    // Static cache for all units
    private const float unitCacheInterval = 1f; // seconds
    public bool isAlive = true; // Indicates if the unit is alive
    public Card unitCard;
    public bool isDragging = false; // Now controlled externally
    public int SlotIndex => unitCard.slotIndex;
    public string UnitName => unitCard.cardName;
    public bool isCharging = false;
    public int playerID;
    public int OpponentID => unitCard.playerID == 1 ? 2 : 1;
    public GameObject unitDetailsPanel; // Set externally or via singleton

    public Vector3 GetUnitPosition()
    {
        return transform.position;
    }

    void Start()
    {
        playerID = unitCard.playerID;
    }

    // Called by FormationManager to start dragging
    public void StartDrag()
    {
        isDragging = true;
       SetCollidersTrigger();
    }

    // Called by FormationManager to stop dragging
    public void StopDrag()
    {
        isDragging = false;
       SetCollidersTrigger();
        unitCard.SetSpawnPosition(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z);
    }

    // Called by FormationManager to move the unit during drag
    public void DragTo(Vector3 position)
    {
        if (isDragging)
        {
            // Clamp position to allowed box
            float clampedX = Mathf.Clamp(position.x, -20f, 20f);
            float clampedZ = Mathf.Clamp(position.z, -10f, 6f);
            Vector3 clampedPosition = new Vector3(clampedX, 0.71f, clampedZ);
            transform.position = clampedPosition;
        }
    }

    // Event interface implementations
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        return;
        // Show details panel
        if (unitDetailsPanel != null)
        {
            unitDetailsPanel.SetActive(true);
            var detailsDisplay = unitDetailsPanel.GetComponent<CardDetailsDisplay>();
            if (detailsDisplay != null && unitCard != null)
            {
                detailsDisplay.ShowDetails(unitCard);
                detailsDisplay.order1.onValueChanged.AddListener((isOn) => { if (isOn) detailsDisplay.OnOrderToggleChanged(unitCard); });
                detailsDisplay.order2.onValueChanged.AddListener((isOn) => { if (isOn) detailsDisplay.OnOrderToggleChanged(unitCard); });
                detailsDisplay.order3.onValueChanged.AddListener((isOn) => { if (isOn) detailsDisplay.OnOrderToggleChanged(unitCard); });
            }
        }
        StartDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move unit with pointer/touch
        Ray ray = Camera.main.ScreenPointToRay(eventData.position);
        Plane battlefield = new Plane(Vector3.up, new Vector3(0, 0.71f, 0));
        float distance;
        if (battlefield.Raycast(ray, out distance))
        {
            Vector3 point = ray.GetPoint(distance);
            point.y = 0.71f;
            DragTo(point);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopDrag();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Optional: highlight unit or show hover effect
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Optional: remove highlight or hide hover effect
    }

    // Helper to set all descendant colliders' isTrigger
    private void SetCollidersTrigger()
    {
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            if (collider.CompareTag("colliderSolid"))
            {
                collider.isTrigger = !collider.isTrigger;
            }
        }
        foreach(var rigidbody in GetComponentsInChildren<Rigidbody>())
        {
                rigidbody.useGravity = !rigidbody.useGravity;
        }
    }

    public void SetAliveStatus(bool status)
    {
        isAlive = status;
        //Debug.Log($"Unit alive status set to: {isAlive}");
    }

    // Additional unit-related methods and properties
}
