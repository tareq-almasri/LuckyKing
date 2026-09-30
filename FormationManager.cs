using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Added for new Input System
using UnityEngine.EventSystems;
using System.Text.RegularExpressions; // Added for EventSystem

public class FormationManager : MonoBehaviour
{
    public static FormationManager Instance { get; private set; }
    [Header("All Available Spawn Points")]
    public SpawnPoint[] spawnPoints; // Array of spawn points for units

    [Header("UI Elements")]
    public GameObject unitDetailsPanel; // Panel to show unit details
    public GameObject readyButton;
    public bool isReady = false; // Track if player is ready    
    public bool isFormation = true; // Track if in formation phase
    public Button muteBGM;
    [SerializeField] private AudioSource formationMusic;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("🔧 FormationManager initialized.");
        ResetSpawnPoints();
        unitDetailsPanel.SetActive(false); // Hide details panel at start
        int modelsCount = 0;
        foreach (var item in MatchTracker.Instance.deployedCards)
        {
            if (!item.IsUnitCard || item.playerID != 1) continue;
            modelsCount += item.unitStats.count;

            int spawnIndex = item.slotIndex - 1;
            Debug.Log($"Deploying {item.unitStats.count} models for card: {item.cardName} at spawnIndex: {spawnIndex}");
            if (spawnIndex < 0 || spawnIndex >= 10)
            {
                Debug.LogError($"Invalid spawnIndex: {spawnIndex} for slotIndex: {item.slotIndex}. spawnPoints.Length: {spawnPoints.Length}");
                continue;
            }

            UnitCreator.Instance.CreateUnit(item, spawnPoints[spawnIndex].transform, true);
        }
        Debug.Log($"Deployed models count: {modelsCount}");

        if (muteBGM != null && formationMusic != null)
        {
            muteBGM.onClick.AddListener(() =>
            {
                if (formationMusic != null)
                {
                    formationMusic.mute = !formationMusic.mute;
                }
            });
        }

    }


    // Clears all units from spawn points
    private void ResetSpawnPoints()
    {
        if (spawnPoints == null) return;
        foreach (var sp in spawnPoints)
        {
            if (sp != null)
            {
                sp.ClearSpawnPoint();
            }
        }
    }

    // Update is called once per frame

    // ...existing code...

    // Ready button logic (moved from Update)
    void Update()
    {
        // Use new Input System for click/tap
        bool pointerDown = false;
        Vector2 pointerPos = Vector2.zero;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerDown = true;
            pointerPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            pointerDown = true;
            pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (pointerDown)
        {
            Ray ray = Camera.main.ScreenPointToRay(pointerPos);
            if (!Physics.Raycast(ray, out RaycastHit hit) || hit.collider.GetComponent<Unit>() == null)
            {
                // Prevent hiding if clicking on the unitDetailsPanel or its children
                bool clickedPanel = false;
                if (unitDetailsPanel != null && hit.collider != null)
                {
                    var panelTransform = unitDetailsPanel.transform;
                    var hitTransform = hit.collider.transform;
                    // Check if hit object is the panel or a child of the panel
                    if (hitTransform == panelTransform || hitTransform.IsChildOf(panelTransform))
                        clickedPanel = true;
                }
                if (!clickedPanel && unitDetailsPanel != null)
                    unitDetailsPanel.SetActive(false);
            }
        }

        if (readyButton != null)
        {
            readyButton.SetActive(true);
            readyButton.GetComponent<Button>().onClick.AddListener(() => { isReady = true; Debug.Log("Player is ready."); });
        }
        if (isReady)
        {
            Debug.Log("Both players are ready.");
            isFormation = false;
            SceneManager.LoadScene("Battle");
        }
    }

    internal class SerializationWrapper<T>
    {
        private List<Card> deployedCards;

        public SerializationWrapper(List<Card> deployedCards)
        {
            this.deployedCards = deployedCards;
        }
    }
}