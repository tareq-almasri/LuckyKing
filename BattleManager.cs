//pvp: player2 spawns: own unit's position(-x, 0.71, z+15+(12-z)) rotation(0, 180, 0)
//vs AI: player2 spawn predetermined on map
//player1 spawns: own unit's position(x, 0.71, z-15-(12-z)) rotation(0, 0, 0)

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour
{
    // Singleton instance
    public static BattleManager Instance { get; private set; }
    [SerializeField] private AudioSource battleMusic;
    public Button muteBGM;
    [SerializeField] private AudioSource victoryMusic;
    [SerializeField] private AudioSource victoryCheer;
    [SerializeField] private AudioSource defeatMusic;
    public AudioSource battleCrySound;
    public Camera mainCamera;
    public Button toggleHp;
    [Header("Player1 Spawn Points")]
    public SpawnPoint[] spawnPoints1; // Array of spawn points for units

    [Header("Player2 Spawn Points")]
    public SpawnPoint[] spawnPoints2; // Array of spawn points for units

    [Header("UI Elements")]
    public GameObject unitDetailsPanel; // Panel to show unit details
    public int localPlayerID; // 1 or 2
    public int winnerID = 0;
    private bool hasPlayedVictorySound = false;
    private bool hasPlayedDefeatSound = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        // Clear spawn points first
        ResetSpawnPoints();
    }

    private void Start()
    {
        Debug.Log("🔧 BattleManager initialized.");

        // Ensure spawn points have correct playerID before unit creation
        for (int i = 0; i < spawnPoints1.Length; i++)
        {
            if (spawnPoints1[i] != null) spawnPoints1[i].playerID = 1;
        }
        for (int i = 0; i < spawnPoints2.Length; i++)
        {
            if (spawnPoints2[i] != null) spawnPoints2[i].playerID = 2;
        }
        localPlayerID = MatchTracker.Instance.localPlayerID;
        // Spawn units for player1
        int modelsCount = 0;
        foreach (var item in MatchTracker.Instance.deployedCards)
        {
            if (!item.IsUnitCard || item.playerID != 1) continue;
            modelsCount += item.unitStats.count;
            int spawnIndex = item.slotIndex - 1;
            if (spawnIndex < 0 || spawnIndex >= 10) continue;
            UnitCreator.Instance.CreateUnit(item, spawnPoints1[spawnIndex].transform, false);
        }
        // Spawn units for player2
        foreach (var item in MatchTracker.Instance.deployedCards)
        {
            if (!item.IsUnitCard || item.playerID != 2) continue;
            modelsCount += item.unitStats.count;
            int spawnIndex = item.slotIndex - 1;
            Debug.Log($"spawnIndex: {spawnIndex} item slotIndex: {item.slotIndex}");
            if (spawnIndex < 0 || spawnIndex >= 10) continue;
            UnitCreator.Instance.CreateUnit(item, spawnPoints2[spawnIndex].transform, false);
        }
        Debug.Log($"Deployed models count: {modelsCount}");
        if (unitDetailsPanel != null)
        {
            foreach (var toggle in unitDetailsPanel.GetComponentsInChildren<Toggle>(true))
            {
                toggle.interactable = false;
            }
            unitDetailsPanel.SetActive(false);
        }
        // All assignments done, now start the battle
        Slider[] allSliders = FindObjectsByType<Slider>(FindObjectsSortMode.None); // includes inactive
        if (toggleHp != null && allSliders.Length > 0)
        {
            toggleHp.onClick.AddListener(() =>
            {
                bool anyActive = false;
                foreach (var slider in allSliders)
                {
                    if (slider != null && slider.gameObject != null && slider.gameObject.activeSelf)
                    {
                        anyActive = true;
                        break;
                    }
                }
                foreach (var slider in allSliders)
                {
                    if (slider != null && slider.gameObject != null)
                    {
                        slider.gameObject.SetActive(!anyActive);
                    }
                }
            });
        }
        if (muteBGM != null && battleMusic != null)
        {
            muteBGM.onClick.AddListener(() =>
            {
                if (battleMusic != null)
                {
                    battleMusic.mute = !battleMusic.mute;
                }
            });
        }
        //StartBattle();
    }
    public void StartBattle()
    {
        // Initialize battle settings and start the battle
        Debug.Log("⚔️ Battle Started!");
        if (battleMusic != null && !battleMusic.isPlaying)
        {
            battleMusic.Play();
        }
        if (battleCrySound != null && !battleCrySound.isPlaying)
        {
            battleCrySound.Play();
        }
        Model[] allModels = FindObjectsByType<Model>(FindObjectsSortMode.None);
        foreach (var model in allModels)
        {
            model.Charge();
        }
    }
    private void BackToMenu()
    {
        //return to main menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menus");
    }
    private void Update()
    {
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
            Debug.Log("Pointer down detected.");

            Ray ray = Camera.main.ScreenPointToRay(pointerPos);
            if (!Physics.Raycast(ray, out RaycastHit hit) || hit.collider.GetComponent<Model>() == null)
            {
                Debug.Log("Clicked outside any unit, hiding details panel.");
                if (unitDetailsPanel != null)
                    unitDetailsPanel.SetActive(false);
            }
        }
        if (winnerID == localPlayerID)
        {
            // Show victory screen or handle end of battle
            //u win
            if (battleMusic != null && battleMusic.isPlaying)
            {
                battleMusic.Stop();
            }
            if (victoryMusic != null && !victoryMusic.isPlaying && !hasPlayedVictorySound)
            {
                victoryMusic.Play();
                hasPlayedVictorySound = true;
            }
            if (victoryCheer != null && !victoryCheer.isPlaying)
            {
                victoryCheer.Play();
            }
            Invoke(nameof(BackToMenu), 5f); // Return to menu after 5 seconds
        }
        else if (winnerID != 0 && winnerID != localPlayerID)
        {
            // Show defeat screen or handle end of battle
            //u lose
            if (battleMusic != null && battleMusic.isPlaying)
            {
                battleMusic.Stop();
            }
            if (defeatMusic != null && !defeatMusic.isPlaying && !hasPlayedDefeatSound)
            {
                defeatMusic.Play();
                hasPlayedDefeatSound = true;
            }
            Invoke(nameof(BackToMenu), 5f); // Return to menu after 5 seconds
        }
    }

    // Clears all units from spawn points
    private void ResetSpawnPoints()
    {
        if (spawnPoints1 == null && spawnPoints2 == null) return;
        if (spawnPoints1 != null)
        {
            foreach (var sp in spawnPoints1)
            {
                if (sp != null)
                {
                    sp.ClearSpawnPoint();
                }
            }
        }
        if (spawnPoints2 != null)
        {
            foreach (var sp in spawnPoints2)
            {
                if (sp != null)
                {
                    sp.ClearSpawnPoint();
                }
            }
        }

    
    }
}