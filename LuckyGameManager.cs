using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LuckyGameManager : MonoBehaviour
{
    public static LuckyGameManager Instance;
    [Header("Players")]
    [SerializeField] private LuckyPlayer player1;
    [SerializeField] private LuckyPlayer player2;
    public int localPlayerID; // Set this based on local player (1 or 2)

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI turnMessageText;
    [SerializeField] private GameObject turnMessagePanel;
    [SerializeField] public Button endTurnButton;
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private TextMeshProUGUI player1DPText;
    [SerializeField] private TextMeshProUGUI player2DPText;
    [SerializeField] private TextMeshProUGUI turnCounterText;
    [SerializeField] private CardDetailsDisplay cardDetailsDisplay;
    public Camera mainCamera;

    [Header("Game Settings")]
    [SerializeField] private int maxTurnsPhase1 = 8;

    [Header("Turn Message Settings")]
    [Tooltip("Delay before showing turn message after dice finish rolling (seconds)")]
    [SerializeField] private float turnMessageDelay = 1.0f;
    [Tooltip("How long the turn message stays visible (seconds)")]
    [SerializeField] private float turnMessageDuration = 1.5f;

    [Header("Current Game State")]
    [SerializeField] private GamePhase currentPhase = GamePhase.Setup;
    [SerializeField] private LuckyPlayer currentPlayer;
    [SerializeField] private int totalTurns = 1;

    private bool gameStarted = false;
    private bool isPVP = false;

    public enum GamePhase
    {
        Menu,
        Setup,          // Initial setup
        TurnOrder,      // Rolling dice to determine who goes first
        Deployment,     // Phase 1: Playing cards and deploying units
        Formation,      // Phase 2: (Future implementation)
        Battle,         // Phase 3: (Future implementation)
        GameOver
    }

    // Events
    public System.Action<LuckyPlayer> OnPlayerTurnStart;
    public System.Action<LuckyPlayer> OnPlayerTurnEnd;
    public System.Action<GamePhase> OnPhaseChanged;
    public DropZone[] allDropZones;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        Debug.Log("🎮 Initializing Lucky Game...");

        // Setup UI
        if (turnMessagePanel != null) turnMessagePanel.SetActive(false);
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(EndCurrentPlayerTurn);
            endTurnButton.interactable = false;
        }
        if (turnCounterText != null) turnCounterText.text = $"Turn: 0/{maxTurnsPhase1 * 2}";
        allDropZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        allDropZones = System.Array.FindAll(allDropZones, zone => zone != null);
        // --- PVP Assignment Logic ---
        isPVP = MatchTracker.Instance.currentGameMode == MatchTracker.GameMode.Casual || MatchTracker.Instance.currentGameMode == MatchTracker.GameMode.Ranked;
        if (isPVP && Photon.Pun.PhotonNetwork.InRoom)
        {
            GameObject player1Obj = Photon.Pun.PhotonNetwork.Instantiate("Player", new Vector3(-7, 0, -4), Quaternion.identity);
            player1 = player1Obj.GetComponent<LuckyPlayer>();

            GameObject player2Obj = Photon.Pun.PhotonNetwork.Instantiate("Player", new Vector3(11, 0, 4), Quaternion.Euler(0, 180, 0));
            player2 = player2Obj.GetComponent<LuckyPlayer>();

            // Get Photon players sorted by ActorNumber
            var photonPlayers = Photon.Pun.PhotonNetwork.PlayerList;
            // Randomly assign player1/player2 based on ActorNumber order
            // Lower ActorNumber gets player1, higher gets player2
            Photon.Realtime.Player p1Photon = photonPlayers[0].ActorNumber < photonPlayers[1].ActorNumber ? photonPlayers[0] : photonPlayers[1];
            Photon.Realtime.Player p2Photon = photonPlayers[0].ActorNumber < photonPlayers[1].ActorNumber ? photonPlayers[1] : photonPlayers[0];

            // Assign localPlayerID based on which Photon player is local
            if (Photon.Pun.PhotonNetwork.LocalPlayer == p1Photon)
            {
                localPlayerID = 1;
                player1.isLocalPlayer = true;
                player2.isLocalPlayer = false;
            }
            else
            {
                localPlayerID = 2;
                player1.isLocalPlayer = false;
                player2.isLocalPlayer = true;
            }
            // Setup players
            SetupPlayer(player1, 1);
            SetupPlayer(player2, 2);
            // Camera adjustment for player2
            if (localPlayerID == 2)
            {
                mainCamera.transform.position += new Vector3(4, 0, 0);
                mainCamera.transform.rotation = Quaternion.Euler(90, 180, 0);
            }
        }
        else
        {
            // Non-PVP: assign local player as player1
            SetupPlayer(player1, 1);
            SetupPlayer(player2, 2);
            localPlayerID = player1.isLocalPlayer ? 1 : 2;
            MatchTracker.Instance.localPlayerID = localPlayerID;
            if (localPlayerID == 2)
            {
                mainCamera.transform.position += new Vector3(4, 0, 0);
                mainCamera.transform.rotation = Quaternion.Euler(90, 180, 0);
            }
        }
        //reset all card multipliers for both players
        player1.playerDeck.ResetAllCardMultipliers();
        player2.playerDeck.ResetAllCardMultipliers();

        // Debug diceWhite assignments
        Debug.Log($"[DEBUG] player1.diceWhite: {(player1 != null ? player1.diceWhite : null)}");
        Debug.Log($"[DEBUG] player2.diceWhite: {(player2 != null ? player2.diceWhite : null)}");

        // Subscribe to player events (with cleanup on destroy)
        player1.OnDPChanged += (player, dp) => UpdatePlayerDP(player, dp);
        player2.OnDPChanged += (player, dp) => UpdatePlayerDP(player, dp);

        // Initialize DP display for both players
        UpdatePlayerDP(player1, player1.DeploymentPoints);
        UpdatePlayerDP(player2, player2.DeploymentPoints);

        // Find the CardDetailsDisplay in the scene, even if inactive
        if (cardDetailsDisplay == null)
        {
            cardDetailsDisplay = FindFirstObjectByType<CardDetailsDisplay>(); // Finds the first instance, including inactive objects
        }
        if (cardDetailsDisplay != null && cardDetailsDisplay.gameObject.activeSelf)
        {
            cardDetailsDisplay.gameObject.SetActive(false);
        }

        UpdateUI();

        // Start the game
        StartGame();
    }

    void OnDestroy()
    {
        // Clean up event subscriptions to prevent memory leaks
        if (player1 != null)
        {
            player1.OnDPChanged -= (player, dp) => UpdatePlayerDP(player, dp);
        }

        if (player2 != null)
        {
            player2.OnDPChanged -= (player, dp) => UpdatePlayerDP(player, dp);
        }

        // Stop all coroutines
        StopAllCoroutines();

        Debug.Log("🧹 LuckyGameManager cleaned up - events unsubscribed, coroutines stopped");
    }

    void SetupPlayer(LuckyPlayer player, int playerID)
    {
        player.playerID = playerID;
        player.allDropZones = new System.Collections.Generic.List<DropZone>(allDropZones);
        // Do not overwrite playerName here; use Inspector value

        // Initialize dice
        if (player.diceWhite != null) player.diceWhite.playerID = playerID;
        if (player.diceRed != null) player.diceRed.playerID = playerID;
        if (player.diceBlue != null) player.diceBlue.playerID = playerID;

        // Setup hand manager
        if (player.handManager != null && player.playerDeck != null)
        {
            Debug.Log($"🔧 Setting up deck for Player {playerID} - Deck: {player.playerDeck.name} with {player.playerDeck.CardCount} cards");
            player.handManager.SetPlayerDeck(player.playerDeck);
            // Set ownerPlayer reference for correct dice logic
            player.handManager.ownerPlayer = player;
            Debug.Log($"[DEBUG] Set ownerPlayer for handManager of Player {playerID}");
            Debug.Log($"[DEBUG] After assignment: handManager.ownerPlayer is null? {player.handManager.ownerPlayer == null}");
        }
        else
        {
            Debug.LogError($"❌ Player {playerID} setup failed - HandManager: {player.handManager != null}, Deck: {player.playerDeck != null}");
        }

        // Initialize player components (including card stack reference)
        player.InitializePlayer();
    }

    public void StartGame()
    {
        if (gameStarted) return;

        // Reset all card multipliers for both players at game start
        if (player1 != null && player1.playerDeck != null)
            player1.playerDeck.ResetAllCardMultipliers();
        if (player2 != null && player2.playerDeck != null)
            player2.playerDeck.ResetAllCardMultipliers();

        gameStarted = true;
        currentPhase = GamePhase.Setup;
        OnPhaseChanged?.Invoke(currentPhase);

        Debug.Log("🎲 Starting Lucky Game - Ensuring drop zone system is ready...");

        // Ensure drop zone system is initialized before creating any cards
        StartCoroutine(EnsureSystemsReadyThenStart());
    }

    private IEnumerator EnsureSystemsReadyThenStart()
    {
        // Wait for all Start() methods to complete
        yield return null;

        // Check for DropZoneCreator
        DropZoneCreator dropZoneCreator = FindFirstObjectByType<DropZoneCreator>();
        if (dropZoneCreator != null)
        {
            Debug.Log("✅ DropZoneCreator found and ready");
        }
        else
        {
            Debug.LogWarning("⚠️ No DropZoneCreator found!");
        }

        // Now start the actual game sequence
        Debug.Log("🎲 Systems initialized - Starting game sequence...");
        StartCoroutine(DrawInitialHandsThenDetermineTurnOrder());
    }

    private IEnumerator DrawInitialHandsThenDetermineTurnOrder()
    {
        Debug.Log("🎴 Both players drawing their initial 3 cards...");

        // Add 1-second delay to ensure all systems (especially drop zones) are fully initialized
        Debug.Log("⏱️ Waiting 1 second for all systems to initialize before creating first cards...");
        yield return new WaitForSeconds(1.0f);

        // Both players draw initial hands (3 cards each) sequentially
        Debug.Log("🎴 Player 1 drawing initial hand...");
        player1.handManager?.DrawInitialHand(3);

        // Wait for Player 1's cards to finish drawing
        yield return new WaitForSeconds(3.0f); // 3 cards * 0.6s each + buffer

        Debug.Log("🎴 Player 2 drawing initial hand...");
        player2.handManager?.DrawInitialHand(3);

        // Wait for Player 2's cards to finish drawing
        yield return new WaitForSeconds(3.0f); // 3 cards * 0.6s each + buffer

        Debug.Log("✅ Both players have their initial hands. Now rolling for turn order...");

        // Now determine turn order
        currentPhase = GamePhase.TurnOrder;
        OnPhaseChanged?.Invoke(currentPhase);

        StartCoroutine(DetermineTurnOrder());
    }

    private IEnumerator DetermineTurnOrder()
    {

        Debug.Log("🎯 Rolling dice to determine who goes first...");

        // Safety check
        if (player1 == null || player1.diceWhite == null || player2 == null || player2.diceWhite == null)
        {
            Debug.LogError("❌ Cannot determine turn order - white dice not assigned!");
            yield break;
        }

        // Both players roll white dice

        player1.diceWhite.RollDice();
        player2.diceWhite.RollDice();

        // Wait for both dice to finish
        while (!player1.diceWhite.HasRolled || !player2.diceWhite.HasRolled)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Determine winner
        int player1Roll = player1.diceWhite.Value;
        int player2Roll = player2.diceWhite.Value;

        Debug.Log($"🎲 Turn order rolls - Player 1: {player1Roll}, Player 2: {player2Roll}");

        if (player1Roll > player2Roll)
        {
            currentPlayer = player1;
            if (player1.isLocalPlayer)
                StartCoroutine(ShowTurnMessageAfterDelay("You go first!", turnMessageDelay));
            else
                StartCoroutine(ShowTurnMessageAfterDelay("You go second!", turnMessageDelay));
        }
        else if (player2Roll > player1Roll)
        {
            currentPlayer = player2;
            if (player2.isLocalPlayer)
                StartCoroutine(ShowTurnMessageAfterDelay("You go first!", turnMessageDelay));
            else
                StartCoroutine(ShowTurnMessageAfterDelay("You go second!", turnMessageDelay));
        }
        else
        {
            // Tie - wait for dice return, then roll again
            Debug.Log("🔄 Tie! Waiting for dice to return, then rolling again...");

            // Wait for dice to complete their return sequence (slide + wait + return)
            // Total time: slide (1s) + wait (0.7s) + return (1s) = 2.7s + buffer
            yield return new WaitForSeconds(3f);

            // Reset dice states
            player1.diceWhite.ResetDice();
            player2.diceWhite.ResetDice();

            // Brief pause before re-rolling
            yield return new WaitForSeconds(0.5f);

            Debug.Log("🎲 Re-rolling for turn order after tie...");
            StartCoroutine(DetermineTurnOrder());
            yield break;
        }

        // Wait for turn message, then start deployment phase
        yield return new WaitForSeconds(turnMessageDuration);
        StartDeploymentPhase();
    }

    private void ShowTurnMessage(string message)
    {
        if (turnMessageText != null) turnMessageText.text = message;
        if (turnMessagePanel != null) turnMessagePanel.SetActive(true);

        Debug.Log($"📢 Turn Message: {message}");

        // Hide message after duration
        StartCoroutine(HideTurnMessageAfterDelay());
    }

    private IEnumerator HideTurnMessageAfterDelay()
    {
        yield return new WaitForSeconds(turnMessageDuration);
        if (turnMessagePanel != null) turnMessagePanel.SetActive(false);
    }

    private IEnumerator ShowTurnMessageAfterDelay(string message, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowTurnMessage(message);
    }

    private void StartDeploymentPhase()
    {
        currentPhase = GamePhase.Deployment;
        OnPhaseChanged?.Invoke(currentPhase);

        Debug.Log("⚔️ Phase 1: Deployment begins!");
        Debug.Log($"🎯 {currentPlayer.playerName} goes first and gets their turn!");

        // Both players already have their initial 3 cards
        // The first player now gets their normal turn (+1 DP and +1 card)
        StartPlayerTurn(currentPlayer);
    }

    private void StartPlayerTurn(LuckyPlayer player)
    {
        currentPlayer = player;
        StartCoroutine(player.StartTurn());
        totalTurns++;

        // Enable end turn button
        if (endTurnButton != null && currentPlayer.isLocalPlayer) endTurnButton.interactable = true;

        UpdateUI();
        OnPlayerTurnStart?.Invoke(player);
        if (currentPlayer.playerID == localPlayerID) ShowTurnMessage("Your turn!");
        Debug.Log($"🎯 {player.playerName}'s turn begins! (Turn {totalTurns})");
        MatchTracker.Instance.DebugPrintDeployedCards();
    }

    public void EndCurrentPlayerTurn()
    {
        if (currentPlayer == null) return;

        currentPlayer.EndTurn();
        OnPlayerTurnEnd?.Invoke(currentPlayer);

        // Disable end turn button
        if (endTurnButton != null) endTurnButton.interactable = false;

        // Check if deployment phase is over
        if (totalTurns >= maxTurnsPhase1 * 2) // Each player gets maxTurns
        {

            StartCoroutine(EndDeploymentPhase());
            return;
        }

        // Switch to other player
        currentPlayer = (currentPlayer == player1) ? player2 : player1;

        // Start next turn after a brief delay
        StartCoroutine(StartNextTurnDelayed());
    }

    private IEnumerator StartNextTurnDelayed()
    {
        allDropZones = FindObjectsByType<DropZone>(FindObjectsSortMode.None);
        allDropZones = System.Array.FindAll(allDropZones, zone => zone != null);
        yield return new WaitForSeconds(1f);
        StartPlayerTurn(currentPlayer);
    }

    private IEnumerator EndDeploymentPhase()
    {
        Debug.Log("🏁 Phase 1: Deployment complete!");
        Debug.Log("🎮 Transitioning to Formation phase...");

        foreach (var dropZone in allDropZones)
        {
            if (dropZone != null && dropZone.transform.childCount > 1)
            {
                Transform card = dropZone.transform.GetChild(1);
                LuckyCardDisplay cardDisplay = card.GetComponent<LuckyCardDisplay>();
                if (cardDisplay != null && cardDisplay.cardData != null)
                {
                    MatchTracker.Instance.AddDeployedCard(cardDisplay.cardData);
                }
            }
        }
        currentPhase = GamePhase.Formation;
        OnPhaseChanged?.Invoke(currentPhase);

        // Reset all card multipliers for both players at game end
        if (player1 != null && player1.playerDeck != null)
            player1.playerDeck.ResetAllCardMultipliers();
        if (player2 != null && player2.playerDeck != null)
            player2.playerDeck.ResetAllCardMultipliers();

        yield return new WaitForSeconds(1f);
        // switch scene to 'Formation'
        SceneManager.LoadScene("Formation");
    }

    private void UpdateUI()
    {
        Debug.Log("[DEBUG]>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> UpdateUI() entered");
        if (currentPlayerText != null)
        {
            Debug.Log($"[DEBUG] UpdateUI called. Phase: {currentPhase}, CurrentPlayer: {(currentPlayer != null ? currentPlayer.playerName : "NONE")}");
            if (currentPlayer != null)
            {
                Debug.Log($"[DEBUG] CurrentPlayer dice assignments: White={(currentPlayer.diceWhite != null)}, Red={(currentPlayer.diceRed != null)}, Blue={(currentPlayer.diceBlue != null)}");
            }
            // Only show player turn text when we're in deployment phase and have a valid current player
            if (currentPhase == GamePhase.Deployment && currentPlayer != null)
            {
                currentPlayerText.text = $"{currentPlayer.playerName}'s Turn";
            }
            else
            {
                currentPlayerText.text = "";
            }
        }

        if (turnCounterText != null)
        {
            int currentTurn = totalTurns;
            int totalTurnsToShow = maxTurnsPhase1 * 2;
            turnCounterText.text = $"Turn: {currentTurn}/{totalTurnsToShow}";
        }

        // Update DP displays are handled by event callbacks
    }
    private void UpdatePlayerDP(LuckyPlayer player, int dp)
    {
        int maxDP = player.MaxDeploymentPoints;
        string dpText = $"DP: {dp}/{maxDP}";

        if (player.playerID == localPlayerID && player1DPText != null)
        {
            player1DPText.text = dpText;
            Debug.Log($"🔧 Updated Player 1 DP: {dpText}");
        }
        else if (player2DPText != null)
        {
            player2DPText.text = dpText;
            Debug.Log($"🔧 Updated Player 2 DP: {dpText}");
        }
    }


    // Debug methods
    [ContextMenu("Start Game")]
    public void DebugStartGame()
    {
        StartGame();
    }

    [ContextMenu("End Turn")]
    public void DebugEndTurn()
    {
        EndCurrentPlayerTurn();
    }

    // Public properties
    public GamePhase CurrentPhase => currentPhase;
    public LuckyPlayer CurrentPlayer => currentPlayer;
    public LuckyPlayer Player1 => player1;
    public LuckyPlayer Player2 => player2;
    public int TotalTurns => totalTurns;

    // Helper method to get player by ID
    public LuckyPlayer GetPlayer(int playerID)
    {
        switch (playerID)
        {
            case 1: return player1;
            case 2: return player2;
            default: return null;
        }
    }

    // Helper method to get player by their hand manager
    public LuckyPlayer GetPlayerByHandManager(LuckyHandManager handManager)
    {
        if (player1 != null && player1.handManager == handManager)
        {
            return player1;
        }
        if (player2 != null && player2.handManager == handManager)
        {
            return player2;
        }
        return null;
    }

    /// <summary>
    /// Disable/Enable card interactions for all players (useful during game transitions)
    /// </summary>
    public void SetAllCardInteractionsEnabled(bool enabled)
    {
        if (player1 != null ? player1.handManager : null != null)
        {
            player1.handManager.SetGlobalInteractionEnabled(enabled);
        }

        if (player2 != null ? player2.handManager : null != null)
        {
            player2.handManager.SetGlobalInteractionEnabled(enabled);
        }

        Debug.Log($"🎮 All card interactions {(enabled ? "ENABLED" : "DISABLED")}");
    }
}
