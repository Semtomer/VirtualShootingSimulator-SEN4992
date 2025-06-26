using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class PanelGameOver : MonoBehaviour
{
    [Header("Common UI Elements")]
    [SerializeField] private Button buttonRestart;
    [SerializeField] private Button buttonBackToMainMenu;
    [SerializeField] private Button buttonQuit;
    [Tooltip("The parent Transform where historical score entries will be instantiated.")]
    [SerializeField] private Transform dataContainer;

    [Header("Multiplayer Specific UI")]
    [SerializeField] private GameObject multiplayerRoot;
    [SerializeField] private TextMeshProUGUI textMP_Result;
    [SerializeField] private GameObject multiplayerScoreEntryPrefab;

    [Header("Single Player Specific UI")]
    [SerializeField] private GameObject singlePlayerRoot;
    [SerializeField] private TextMeshProUGUI textSP_Result;
    [SerializeField] private GameObject singlePlayerScoreEntryPrefab;

    [Header("Result Colors")]
    [SerializeField] private Color colorWin = Color.green;
    [SerializeField] private Color colorLossOrGameOver = Color.red;
    [SerializeField] private Color colorDrawOrCompleted = Color.yellow;

    private AudioManager audioManager;
    private GameModeType activeMode;
    private GameDifficulty activeDifficulty;

    public void Init(GameModeType mode, GameDifficulty difficulty, object currentSessionData, AudioManager audioMgr)
    {
        Cursor.visible = true;
        activeMode = mode;
        activeDifficulty = difficulty;
        audioManager = audioMgr;

        AddListeners();
        ClearHistoryDisplay();

        if (mode == GameModeType.Multiplayer)
        {
            if (multiplayerRoot != null) multiplayerRoot.SetActive(true);
            else Debug.LogError("PanelGameOver: Multiplayer Root is not assigned for MP mode!", this);

            if (singlePlayerRoot != null) singlePlayerRoot.SetActive(false);

            if (currentSessionData is MultiplayerSessionData mpData)
            {
                PopulateMultiplayerCurrent(mpData);
            }
        }
        else
        {
            if (singlePlayerRoot != null) singlePlayerRoot.SetActive(true);
            else Debug.LogError("PanelGameOver: Single Player Root is not assigned for SP mode!", this);

            if (multiplayerRoot != null) multiplayerRoot.SetActive(false);

            if (currentSessionData is SinglePlayerSessionData spData)
            {
                PopulateSinglePlayerCurrent(spData);
            }
        }

        FetchAndDisplayRelevantHistory();
        Debug.Log($"Game Over Panel initialized for {mode} - {difficulty}.");
    }

    private void AddListeners()
    {
        buttonRestart?.onClick.AddListener(HandleRestartGame);
        buttonBackToMainMenu?.onClick.AddListener(HandleBackToMainMenu);
        buttonQuit?.onClick.AddListener(HandleQuitGame);
    }

    private void HandleRestartGame()
    {
        Cursor.visible = false;
        audioManager?.PlayButtonClickSound();

        Time.timeScale = 1f;

        string sceneToLoad = GameSettings.GetSceneName();

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"Restarting game. Loading scene: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Could not determine scene to load for restart. Check GameSettings.");
        }
    }

    private void HandleBackToMainMenu()
    {
        audioManager?.PlayButtonClickSound();

        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private void HandleQuitGame()
    {
        audioManager?.PlayButtonClickSound();

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void PopulateMultiplayerCurrent(MultiplayerSessionData data)
    {
        if (textMP_Result != null)
        {
            if (data.winner == "Draw")
            {
                textMP_Result.text = "IT'S A DRAW!";
                textMP_Result.color = colorDrawOrCompleted;
            }
            else
            {
                textMP_Result.text = $"{data.winner} WINS!";
                textMP_Result.color = colorWin;
            }
        }
    }

    private void PopulateSinglePlayerCurrent(SinglePlayerSessionData data)
    {
        if (textSP_Result != null)
        {
            textSP_Result.text = data.result.ToUpper();
            textSP_Result.color = (data.result == "Completed") ? colorWin : colorLossOrGameOver;
        }
    }

    private void ClearHistoryDisplay()
    {
        if (dataContainer == null) return;
        foreach (Transform child in dataContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void FetchAndDisplayRelevantHistory()
    {
        if (FirebaseManager.Instance != null)
        {
            Debug.Log($"PanelGameOver: Requesting history for {activeMode} - {activeDifficulty}...");
            FirebaseManager.Instance.FetchSpecificSessionHistory(activeMode, activeDifficulty, OnHistoryReceived);
        }
        else
        {
            Debug.LogWarning("PanelGameOver: FirebaseManager instance not found. Cannot display history.");
        }
    }

    private void OnHistoryReceived(List<object> sessions)
    {
        if (dataContainer == null)
        {
            Debug.LogError("PanelGameOver: DataContainer for history is null!", this);
            return;
        }

        ClearHistoryDisplay();

        if (sessions == null) { Debug.LogWarning("Failed to fetch session history or no history exists for this mode/difficulty."); return; }
        if (sessions.Count == 0) { Debug.Log($"No previous game session history found for {activeMode} - {activeDifficulty}."); return; }

        sessions.Sort((s1, s2) =>
        {
            int score1 = 0, score2 = 0;
            if (activeMode == GameModeType.Multiplayer)
            {
                if (s1 is MultiplayerSessionData mp1) score1 = GetMultiplayerSortScore(mp1);
                if (s2 is MultiplayerSessionData mp2) score2 = GetMultiplayerSortScore(mp2);
            }
            else
            {
                if (s1 is SinglePlayerSessionData sp1) score1 = sp1.p1Score;
                if (s2 is SinglePlayerSessionData sp2) score2 = sp2.p1Score;
            }
            return score2.CompareTo(score1);
        });

        GameObject prefabToUse = (activeMode == GameModeType.Multiplayer) ? multiplayerScoreEntryPrefab : singlePlayerScoreEntryPrefab;
        if (prefabToUse == null)
        {
            Debug.LogError($"Score entry prefab not set for mode {activeMode} in PanelGameOver!", this);
            return;
        }

        Debug.Log($"Displaying {sessions.Count} historical sessions for {activeMode} - {activeDifficulty}.");
        foreach (object sessionObj in sessions)
        {
            GameObject entryInstance = Instantiate(prefabToUse, dataContainer);
            ScoreEntryUI entryUI = entryInstance.GetComponent<ScoreEntryUI>();

            if (entryUI != null)
            {
                if (activeMode == GameModeType.Multiplayer && sessionObj is MultiplayerSessionData mpData)
                {
                    entryUI.SetMultiplayerData(mpData);
                }
                else if (activeMode == GameModeType.SinglePlayer && sessionObj is SinglePlayerSessionData spData)
                {
                    entryUI.SetSinglePlayerData(spData);
                }
            }
            else { Debug.LogError("ScoreEntryUI component not found on instantiated history prefab!", entryInstance); }
        }
    }

    private int GetMultiplayerSortScore(MultiplayerSessionData data)
    {
        if (data == null) return 0;
        if (!string.IsNullOrEmpty(data.winner) && data.winner != "Draw")
        {
            if (data.winner == data.player1Name) return data.p1Score;
            if (data.winner == data.player2Name) return data.p2Score;
        }
        return Mathf.Max(data.p1Score, data.p2Score);
    }

    private void OnDestroy()
    {
        Cursor.visible = false;
        buttonRestart?.onClick.RemoveAllListeners();
        buttonBackToMainMenu?.onClick.RemoveAllListeners();
        buttonQuit?.onClick.RemoveAllListeners();
    }
}