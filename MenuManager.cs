using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Text.RegularExpressions;

public class MenuManager : MonoBehaviourPunCallbacks
{
    private float matchTimer = 0f;
    private bool showTimer = false;

    private bool isMatching = false;
    private bool isRanked = false;
    public GameObject header;
    public Button playerTag;
    public Button rankingsIconButton;
    public Button settingsButton;
    public GameObject menu;
    public Button playButton;
    public Button cardsButton;
    public Button shopButton;
    public Button quitButton;
    public GameObject playMenu;
    public Button campaignButton;
    public Button vsAIButton;
    public Button casualButton;
    public Button rankedButton;
    public GameObject settings;
    public GameObject playerProfile;
    public GameObject cards;
    public GameObject shop;
    public GameObject rankings;
    public TMP_Text timerText;
    public Button cancelMatchButton;
    public Button muteBGM;
    //public GameObject credits;
    //public GameObject help;
    public void Awake()
    {
    }

    void Start()
    {
        ResetMatchingState();
        PhotonNetwork.AutomaticallySyncScene = true;
        if (cancelMatchButton != null)
        {
            cancelMatchButton.onClick.AddListener(CancelMatch);
            cancelMatchButton.gameObject.SetActive(false);
        }
        if (timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }
        if (playerTag != null)
        {
            playerTag.onClick.AddListener(OnPlayerTagClicked);
        }
        if (campaignButton != null)
        {
            campaignButton.onClick.AddListener(OnCampaignButtonClicked);
        }
        if (vsAIButton != null)
        {
            vsAIButton.onClick.AddListener(OnVsAIButtonClicked);
        }
        if (casualButton != null)
        {
            casualButton.onClick.AddListener(() => StartMatching(false));
        }
        if (rankedButton != null)
        {
            rankedButton.onClick.AddListener(() => StartMatching(true));
        }
        if (cardsButton != null)
        {
            cardsButton.onClick.AddListener(OnCardsButtonClicked);
        }
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(OnShopButtonClicked);
        }
        if (rankingsIconButton != null)
        {
            rankingsIconButton.onClick.AddListener(OnRankingsIconButtonClicked);
        }
        foreach (var backButtonObj in GameObject.FindGameObjectsWithTag("BackButton"))
        {
            var button = backButtonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnBackButtonClicked);
            }
        }
        playMenu.SetActive(false);
        settings.SetActive(false);
        playerProfile.SetActive(false);
        cards.SetActive(false);
        shop.SetActive(false);
        rankings.SetActive(false);
    }

    void Update()
    {
        // Show timer during matching
        if (showTimer)
            matchTimer += Time.deltaTime;
        int minutes = Mathf.FloorToInt(matchTimer / 60f);
        int seconds = Mathf.FloorToInt(matchTimer % 60f);
        if (timerText != null)
        {
            timerText.text = $"Matchmaking... {minutes:00}:{seconds:00}";
        }
    }

    private void OnPlayerTagClicked()

    {
        Debug.Log("Player tag clicked");
        playerProfile.SetActive(true);
        header.SetActive(false);
        menu.SetActive(false);
        playMenu.SetActive(false);
    }
    private void OnSettingsButtonClicked()
    {
        settings.SetActive(true);
        header.SetActive(false);
        menu.SetActive(false);
        playMenu.SetActive(false);
    }
    private void OnPlayButtonClicked()
    {
        playMenu.SetActive(true);
        menu.SetActive(false);
    }
    private void OnBackButtonClicked()
    {
        Debug.Log("Back button clicked");
        playMenu.SetActive(false);
        cards.SetActive(false);
        playerProfile.SetActive(false);
        settings.SetActive(false);
        rankings.SetActive(false);
        shop.SetActive(false);
        menu.SetActive(true);
        header.SetActive(true);
    }
    private void OnCampaignButtonClicked()
    {
        MatchTracker.Instance.currentGameMode = MatchTracker.GameMode.Campaign;
        SceneManager.LoadScene("Board");
    }
    private void OnVsAIButtonClicked()
    {
        MatchTracker.Instance.currentGameMode = MatchTracker.GameMode.VsAI;
        SceneManager.LoadScene("Board");
    }
    private void OnCardsButtonClicked()
    {
        cards.SetActive(true);
        menu.SetActive(false);
        header.SetActive(false);
    }
    private void OnShopButtonClicked()
    {
        shop.SetActive(true);
        menu.SetActive(false);
        header.SetActive(false);
    }
    private void OnRankingsIconButtonClicked()
    {
        rankings.SetActive(true);
        menu.SetActive(false);
        header.SetActive(false);
    }

    private void OnQuitButtonClicked()
    {
        Application.Quit();

    }



    // --- PUN2 Matching Logic ---
    private void StartMatching(bool ranked)
    {
        if (isMatching) return;
        isMatching = true;
        isRanked = ranked;
        matchTimer = 0f;
        showTimer = true;
        if (timerText != null) timerText.gameObject.SetActive(true);
        if (cancelMatchButton != null) cancelMatchButton.gameObject.SetActive(true);
        MatchTracker.Instance.currentGameMode = ranked ? MatchTracker.GameMode.Ranked : MatchTracker.GameMode.Casual;
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log($"Connecting to Photon for {(ranked ? "Ranked" : "Casual")} match...");
    }

    // PUN2 Callbacks
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRandomRoom();
        Debug.Log("Connected to Photon Master. Joining random room...");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No room found. Creating new room...");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player entered room. PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}, IsMasterClient: {PhotonNetwork.IsMasterClient}");
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Board");
            ResetMatchingState();
        }
    }
    /*public override void OnJoinedRoom()
    {
        Debug.Log($"Joined Room! PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}, IsMasterClient: {PhotonNetwork.IsMasterClient}");
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master client is loading Board scene.");
            PhotonNetwork.LoadLevel("Board");
        }
    }*/
    private void CancelMatch()
    {
        if (!isMatching) return;
        PhotonNetwork.Disconnect();
        ResetMatchingState();
        Debug.Log("Matchmaking cancelled by user.");
    }

    private void ResetMatchingState()
    {
        isMatching = false;
        showTimer = false;
        matchTimer = 0f;
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (cancelMatchButton != null) cancelMatchButton.gameObject.SetActive(false);
    }

}
