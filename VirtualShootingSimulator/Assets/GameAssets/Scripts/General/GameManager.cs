using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region Singleton Instance
    public static GameManager Instance { get; private set; }
    #endregion

    #region Game State
    public GameState CurrentState { get; private set; } = GameState.Playing;
    #endregion

    #region Inspector Fields
    [Header("Game Mode & Difficulty")]
    [Tooltip("Selected mode for this game instance. Loaded from GameSettings.")]
    public GameModeType currentGameMode { get; private set; }
    [Tooltip("Selected difficulty for this game instance. Loaded from GameSettings.")]
    public GameDifficulty currentDifficulty { get; private set; }

    [Header("Game Settings")]
    [Tooltip("Total duration of the match in seconds.")]
    [SerializeField] private float totalGameDuration = 60.0f;
    [Tooltip("The key used to pause/resume the game.")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    [Header("Scoring")]
    [Tooltip("Base points awarded for killing an enemy.")]
    [SerializeField] private int baseKillScore = 10;
    [Tooltip("Multiplier for the distance bonus score (Score += Distance * Multiplier).")]
    [SerializeField] private float distanceScoreMultiplier = 1.5f;

    [Header("Object References")]
    [Tooltip("Reference to the Left Player's Controller.")]
    [SerializeField] private PlayerController playerLeft;
    [Tooltip("Reference to the Right Player's Controller.")]
    [SerializeField] private PlayerController playerRight;
    [Tooltip("Reference to the Left Castle.")]
    [SerializeField] private Castle castleLeft;
    [Tooltip("Reference to the Right Castle.")]
    [SerializeField] private Castle castleRight;
    [Tooltip("Reference to the Left Enemy Spawner.")]
    [SerializeField] private EnemySpawner enemySpawnerLeft;
    [Tooltip("Reference to the Right Enemy Spawner.")]
    [SerializeField] private EnemySpawner enemySpawnerRight;
    [Tooltip("Reference to the Chest Spawner.")]
    [SerializeField] private ChestSpawner chestSpawner;

    [Header("UI References")]
    [Tooltip("Reference to the Score1Manager for Player 1's score UI.")]
    [SerializeField] private Score1Manager score1UIManager;
    [Tooltip("Reference to the Score2Manager for Player 2's score UI (MP only).")]
    [SerializeField] private Score2Manager score2UIManager;
    [Tooltip("Reference to the Timer UI script.")]
    [SerializeField] private Timer gameTimerUI;
    [Tooltip("Prefab of the Pause Menu UI.")]
    [SerializeField] private GameObject pauseMenuPrefab;
    private PanelPauseMenu activePauseMenuInstance;
    [Tooltip("Prefab for the Single Player Game Over UI.")]
    [SerializeField] private GameObject gameOverSinglePlayerPrefab;
    [Tooltip("Prefab for the Multiplayer Game Over UI.")]
    [SerializeField] private GameObject gameOverMultiplayerPrefab;
    private PanelGameOver activeGameOverPanelInstance;
    [SerializeField] private Canvas mainCanvas;
    #endregion

    private string player1Username;
    private string player2Username;

    #region Private Fields

    private bool isSlowActiveLeft = false;
    private bool isSlowActiveRight = false;
    private float slowFactorLeft = 1f;
    private float slowFactorRight = 1f;
    private float slowEndTimeLeft = 0f;
    private float slowEndTimeRight = 0f;

    private bool isWeakenActiveLeft = false;
    private bool isWeakenActiveRight = false;
    private float weakenEndTimeLeft = 0f;
    private float weakenEndTimeRight = 0f;

    private float gameTimeRemaining;
    private int player1Score = 0;
    private int player2Score = 0;
    private float previousTimeScale = 1f;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple GameManager instances detected! Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        LoadSettingsFromGameSettings();

        if (!ValidateReferences())
        {
            Debug.LogError("GameManager reference validation failed! Disabling GameManager.", this);
            enabled = false;
            return;
        }

        if (currentGameMode == GameModeType.SinglePlayer)
        {
            DisableRightSideComponents();
        }
        else
        {
            EnsureRightSideComponentsActive();
        }
    }

    private void LoadSettingsFromGameSettings()
    {
        currentGameMode = GameSettings.SelectedMode;
        currentDifficulty = GameSettings.SelectedDifficulty;
        player1Username = GameSettings.Player1Name;
        player2Username = GameSettings.Player2Name;

        Debug.Log($"GameManager Loaded Settings: Mode={currentGameMode}, Difficulty={currentDifficulty}, P1='{player1Username}', P2='{player2Username}'");
    }

    private void Start()
    {
        Cursor.visible = false;

        if (!enabled)
            return;

        gameTimeRemaining = totalGameDuration;
        player1Score = 0;
        player2Score = 0;

        if (score1UIManager != null)
            score1UIManager.SetScore(player1Score);
        if (currentGameMode == GameModeType.Multiplayer && score2UIManager != null)
        {
            score2UIManager.SetScore(player2Score);
        }
        if (gameTimerUI != null)
        {
            gameTimerUI.UpdateTimeDisplay(gameTimeRemaining);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLevelMusic(currentDifficulty);
        }

        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        previousTimeScale = Time.timeScale;
        Debug.Log($"Game Started! Mode: {currentGameMode}, Diff: {currentDifficulty}, Duration: {totalGameDuration}s. P1: '{player1Username}', P2: '{player2Username}'. Pause: '{pauseKey}'.");

        ResetGlobalAbilities();
    }

    private void EnsureRightSideComponentsActive()
    {
        if (playerRight != null && !playerRight.gameObject.activeSelf)
            playerRight.gameObject.SetActive(true);
        if (castleRight != null && !castleRight.gameObject.activeSelf)
            castleRight.gameObject.SetActive(true);
        if (enemySpawnerRight != null && !enemySpawnerRight.gameObject.activeSelf)
            enemySpawnerRight.gameObject.SetActive(true);
        if (score2UIManager != null && !score2UIManager.gameObject.activeSelf)
            score2UIManager.gameObject.SetActive(true);
    }

    private void DisableRightSideComponents()
    {
        if (playerRight != null)
            playerRight.gameObject.SetActive(false);
        if (castleRight != null)
            castleRight.gameObject.SetActive(false);
        if (enemySpawnerRight != null)
            enemySpawnerRight.gameObject.SetActive(false);
        if (score2UIManager != null)
            score2UIManager.gameObject.SetActive(false);
        Debug.Log("Single Player Mode: Right side components disabled.");
    }

    private void Update()
    {
        if (!enabled)
            return;

        if (CurrentState != GameState.GameOver)
        {
            HandlePauseInput();
        }

        if (CurrentState == GameState.Playing)
        {
            gameTimeRemaining -= Time.deltaTime;

            if (gameTimerUI != null)
            {
                gameTimerUI.UpdateTimeDisplay(gameTimeRemaining);
            }

            UpdateGlobalAbilityStates();

            bool endConditionMet = false;
            string endReason = "";

            if (gameTimeRemaining <= 0f)
            {
                gameTimeRemaining = 0f;
                endConditionMet = true;
                endReason = "Time's Up!";
            }
            else if (castleLeft == null || !castleLeft.gameObject.activeInHierarchy)
            {
                endConditionMet = true;
                endReason = (currentGameMode == GameModeType.Multiplayer) ? "Left Castle Destroyed!" : "Castle Destroyed!";
            }
            else if (currentGameMode == GameModeType.Multiplayer &&
            (castleRight == null || !castleRight.gameObject.activeInHierarchy))
            {
                endConditionMet = true;
                endReason = "Right Castle Destroyed!";
            }

            if (endConditionMet)
            {
                EndGame(endReason);
            }
        }
    }

    private void HandlePauseInput()
    {
        if (CurrentState != GameState.GameOver && Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (playerLeft == null)
        {
            Debug.LogError("GameManager missing Player Left reference!", this);
            isValid = false;
        }

        if (castleLeft == null)
        {
            Debug.LogError("GameManager missing Castle Left reference!", this);
            isValid = false;
        }

        if (enemySpawnerLeft == null)
        {
            Debug.LogError("GameManager missing Enemy Spawner Left reference!", this);
            isValid = false;
        }

        if (chestSpawner == null)
        {
            Debug.LogError("GameManager missing Chest Spawner reference!", this);
            isValid = false;
        }

        if (score1UIManager == null)
        {
            Debug.LogError("GameManager missing Score1Manager reference!", this);
            isValid = false;
        }

        if (gameTimerUI == null)
        {
            Debug.LogError("GameManager missing Timer UI reference!", this);
            isValid = false;
        }

        if (pauseMenuPrefab == null)
        {
            Debug.LogError("GameManager missing Pause Menu Prefab reference!", this);
            isValid = false;
        }

        if (gameOverSinglePlayerPrefab == null)
        {
            Debug.LogError("GameManager missing GameOver SP Prefab!", this);
            isValid = false;
        }

        if (gameOverMultiplayerPrefab == null)
        {
            Debug.LogError("GameManager missing GameOver MP Prefab!", this);
            isValid = false;
        }

        if (currentGameMode == GameModeType.Multiplayer)
        {
            if (playerRight == null)
            {
                Debug.LogError("GameManager missing Player Right reference (Required for Multiplayer)!", this);
                isValid = false;
            }

            if (castleRight == null)
            {
                Debug.LogError("GameManager missing Castle Right reference (Required for Multiplayer)!", this);
                isValid = false;
            }

            if (enemySpawnerRight == null)
            {
                Debug.LogError("GameManager missing Enemy Spawner Right reference (Required for Multiplayer)!", this);
                isValid = false;
            }

            if (score2UIManager == null)
            {
                Debug.LogError("GameManager missing Score2Manager reference (Required for Multiplayer)!", this);
                isValid = false;
            }
        }

        return isValid;
    }
    #endregion

    #region Public Game State Methods
    public bool IsGameOver()
    {
        return CurrentState == GameState.GameOver;
    }

    public bool IsPaused()
    {
        return CurrentState == GameState.Paused;
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.GameOver)
            return;

        if (CurrentState == GameState.Playing)
        {
            PauseGame();
        }
        else if (CurrentState == GameState.Paused)
        {
            ResumeGame();
        }
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing)
            return;

        CurrentState = GameState.Paused;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Debug.Log("GAME PAUSED");

        if (pauseMenuPrefab != null && activePauseMenuInstance == null)
        {
            GameObject menuObject = Instantiate(pauseMenuPrefab, Vector3.zero, Quaternion.identity);

            if (mainCanvas != null)
                menuObject.transform.SetParent(mainCanvas.transform, false);
            else
                Debug.LogWarning("Main Canvas not found! Pause Menu will not be parented to it.");

            activePauseMenuInstance = menuObject.GetComponent<PanelPauseMenu>();
            if (activePauseMenuInstance != null)
            {
                activePauseMenuInstance.Init();

                activePauseMenuInstance.OnResumeClicked += HandleResumeClicked;
                activePauseMenuInstance.OnBackToMainMenuClicked += HandleBackToMainMenuClicked;
                activePauseMenuInstance.OnQuitGameClicked += HandleQuitGameClicked;
            }
        }
        else if (activePauseMenuInstance != null)
        {
            activePauseMenuInstance.gameObject.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        Cursor.visible = false;
        if (CurrentState != GameState.Paused)
            return;

        CurrentState = GameState.Playing;
        Time.timeScale = previousTimeScale;
        Debug.Log("GAME RESUMED");

        if (activePauseMenuInstance != null)
        {
            activePauseMenuInstance.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Pause Menu Event Handlers
    private void HandleResumeClicked()
    {
        ResumeGame();
    }

    private void HandleBackToMainMenuClicked()
    {
        Debug.Log("Returning to Main Menu...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private void HandleQuitGameClicked()
    {
        Debug.Log("Quitting Game from Pause Menu...");
        Application.Quit();
    }
    #endregion

    #region Ability Activation & State Getters
    public void ActivateSlowOnSide(GameSide side, float duration, float factor)
    {
        if (IsGameOver())
            return;

        float endTime = Time.time + duration;
        if (side == GameSide.Left)
        {
            isSlowActiveLeft = true;
            slowFactorLeft = factor;
            slowEndTimeLeft = endTime;
            Debug.Log($"SLOW activated on Left side for {duration}s (Factor: {factor}). Ends at {endTime}");
        }
        else
        {
            isSlowActiveRight = true;
            slowFactorRight = factor;
            slowEndTimeRight = endTime;
            Debug.Log($"SLOW activated on Right side for {duration}s (Factor: {factor}). Ends at {endTime}");
        }
    }

    public void ActivateWeakenOnSide(GameSide side, float duration)
    {
        if (IsGameOver())
            return;

        float endTime = Time.time + duration;
        if (side == GameSide.Left)
        {
            isWeakenActiveLeft = true;
            weakenEndTimeLeft = endTime;
            Debug.Log($"WEAKEN activated on Left side for {duration}s. Ends at {endTime}");
        }
        else
        {
            isWeakenActiveRight = true;
            weakenEndTimeRight = endTime;
            Debug.Log($"WEAKEN activated on Right side for {duration}s. Ends at {endTime}");
        }
    }

    public bool IsSlowActive(GameSide side)
    {
        if (currentGameMode == GameModeType.SinglePlayer)
            side = GameSide.Left;

        return (side == GameSide.Left) ? isSlowActiveLeft : isSlowActiveRight;
    }

    public float GetSlowFactor(GameSide side)
    {
        if (currentGameMode == GameModeType.SinglePlayer)
            side = GameSide.Left;

        if (side == GameSide.Left && isSlowActiveLeft)
            return slowFactorLeft;
        if (side == GameSide.Right && isSlowActiveRight)
            return slowFactorRight;

        return 1f;
    }

    public bool IsWeakenActive(GameSide side)
    {
        if (currentGameMode == GameModeType.SinglePlayer)
            side = GameSide.Left;

        return (side == GameSide.Left) ? isWeakenActiveLeft : isWeakenActiveRight;
    }

    private void UpdateGlobalAbilityStates()
    {
        float currentTime = Time.time;

        if (isSlowActiveLeft && currentTime >= slowEndTimeLeft)
        {
            isSlowActiveLeft = false;
        }
        if (isSlowActiveRight && currentTime >= slowEndTimeRight)
        {
            isSlowActiveRight = false;
        }
        if (isWeakenActiveLeft && currentTime >= weakenEndTimeLeft)
        {
            isWeakenActiveLeft = false;
        }
        if (isWeakenActiveRight && currentTime >= weakenEndTimeRight)
        {
            isWeakenActiveRight = false;
        }
    }

    private void ResetGlobalAbilities()
    {
        isSlowActiveLeft = false;
        isSlowActiveRight = false;
        isWeakenActiveLeft = false;
        isWeakenActiveRight = false;
        slowEndTimeLeft = 0f;
        slowEndTimeRight = 0f;
        weakenEndTimeLeft = 0f;
        weakenEndTimeRight = 0f;
        slowFactorLeft = 1f;
        slowFactorRight = 1f;
    }
    #endregion

    #region Scoring
    public void ReportEnemyKill(Enemy killedEnemy, int killerPlayerID)
    {
        if (IsGameOver())
            return;

        int scoreAwarded = CalculateKillScore(killedEnemy);

        if (currentGameMode == GameModeType.SinglePlayer || killerPlayerID == 1)
        {
            player1Score += scoreAwarded;
            if (score1UIManager != null)
            {
                score1UIManager.SetScore(player1Score);
            }
        }
        else if (currentGameMode == GameModeType.Multiplayer && killerPlayerID == 2)
        {
            player2Score += scoreAwarded;
            if (score2UIManager != null)
            {
                score2UIManager.SetScore(player2Score);
            }
        }
    }

    private int CalculateKillScore(Enemy enemy)
    {
        float distanceBonus = 0f;
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();

        if (movement != null)
        {
            Transform destination = movement.GetFinalDestination();
            if (destination != null)
            {
                float distance = Vector3.Distance(enemy.transform.position, destination.position);
                distanceBonus = Mathf.Max(0f, distance * distanceScoreMultiplier);
            }
        }

        return baseKillScore + Mathf.RoundToInt(distanceBonus);
    }
    #endregion

    #region Game End Logic

    private void EndGame(string reason)
    {
        if (CurrentState == GameState.GameOver)
            return;

        CurrentState = GameState.GameOver;
        Time.timeScale = 0f;
        Debug.Log($"GAME OVER - ({reason}) - Mode: {currentGameMode} - Difficulty: {currentDifficulty}");

        StopAllGameActivity();

        GameObject gameOverPrefabToUse = (currentGameMode == GameModeType.Multiplayer) ? gameOverMultiplayerPrefab : gameOverSinglePlayerPrefab;

        object sessionDataToSave = null;
        string formattedDateTime = System.DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

        if (currentGameMode == GameModeType.Multiplayer)
        {
            string winnerName;
            if (castleLeft == null || !castleLeft.gameObject.activeInHierarchy)
            {
                winnerName = player2Username;
            }
            else if (castleRight == null || !castleRight.gameObject.activeInHierarchy)
            {
                winnerName = player1Username;
            }
            else
            {
                if (player1Score > player2Score)
                {
                    winnerName = player1Username;
                }
                else if (player2Score > player1Score)
                {
                    winnerName = player2Username;
                }
                else
                {
                    winnerName = "Draw";
                }
            }

            MultiplayerSessionData mpData = new MultiplayerSessionData
            {
                player1Name = this.player1Username,
                p1Score = player1Score,
                player2Name = this.player2Username,
                p2Score = player2Score,
                date = formattedDateTime,
                winner = winnerName
            };
            sessionDataToSave = mpData;
            DisplayMultiplayerResults(mpData);
        }
        else
        {
            string resultText = (castleLeft == null || !castleLeft.gameObject.activeInHierarchy) ? "Game Over" : "Completed";

            SinglePlayerSessionData spData = new SinglePlayerSessionData
            {
                player1Name = this.player1Username,
                p1Score = player1Score,
                date = formattedDateTime,
                result = resultText
            };
            sessionDataToSave = spData;
            DisplaySinglePlayerResults(spData);
        }

        if (gameOverPrefabToUse != null && activeGameOverPanelInstance == null)
        {
            GameObject panelObject = Instantiate(gameOverPrefabToUse, Vector3.zero, Quaternion.identity);

            if (mainCanvas != null)
                panelObject.transform.SetParent(mainCanvas.transform, false);
            else
                Debug.LogWarning("Main Canvas not found! Pause Menu will not be parented to it.");

            activeGameOverPanelInstance = panelObject.GetComponent<PanelGameOver>();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
                if (currentGameMode == GameModeType.Multiplayer)
                {
                    AudioManager.Instance.PlaySFXPlayerAction(AudioManager.Instance.sfxGameOverMP);
                }
                else
                {
                    bool playerWon = !(reason.Contains("Castle Destroyed") || reason.Contains("Game Over"));
                    AudioManager.Instance.PlaySFXPlayerAction(playerWon ? AudioManager.Instance.sfxGameWinSP : AudioManager.Instance.sfxGameLoseSP);
                }
            }

            if (activeGameOverPanelInstance != null)
            {
                activeGameOverPanelInstance.Init(currentGameMode, currentDifficulty, sessionDataToSave, AudioManager.Instance);
            }
            else { Debug.LogError("Failed to get PanelGameOver component from instantiated prefab!"); }
        }
        else if (activeGameOverPanelInstance != null)
        {
            activeGameOverPanelInstance.gameObject.SetActive(true);
        }

        if (sessionDataToSave != null && FirebaseManager.Instance != null)
        {
            if (sessionDataToSave is MultiplayerSessionData mpSaveData)
            {
                FirebaseManager.Instance.SaveMultiplayerSession(mpSaveData, currentDifficulty);
            }
            else if (sessionDataToSave is SinglePlayerSessionData spSaveData)
            {
                FirebaseManager.Instance.SaveSinglePlayerSession(spSaveData, currentDifficulty);
            }
        }
        else if (sessionDataToSave != null)
        {
            Debug.LogWarning("FirebaseManager not found. Game results not saved online.");
        }

        if (FirebaseManager.Instance != null)
        {
            Debug.Log($"Requesting history for {currentGameMode} - {currentDifficulty}...");
            FirebaseManager.Instance.FetchSpecificSessionHistory(currentGameMode, currentDifficulty, DisplaySessionHistory);
        }
    }

    private void DisplayMultiplayerResults(MultiplayerSessionData data)
    {
        DisplayEndGameResultsHeader(true);
        DisplayMultiplayerSessionRow(data, "Current");
        DisplayEndGameResultsFooter();
        Debug.Log($" Winner: {data.winner}");
        Debug.Log("-------------------------------------------------------------");
    }

    private void DisplaySinglePlayerResults(SinglePlayerSessionData data)
    {
        Debug.Log("--------------------------------------------------");
        Debug.Log("           FINAL RESULTS (Single Player)          ");
        Debug.Log("--------------------------------------------------");
        Debug.Log($" Player: {data.player1Name,-15} | Score: {data.p1Score,-7}");
        Debug.Log($" Result: {data.result,-15} | Date: {data.date}");
        Debug.Log("--------------------------------------------------");
    }

    private void DisplayEndGameResultsHeader(bool isMultiplayer)
    {
        if (isMultiplayer)
        {
            Debug.Log("-------------------------------------------------------------------------------------");
            Debug.Log(string.Format("| {0,-12} | {1,-15} | {2,-7} | {3,-15} | {4,-7} | {5,-19} | {6,-15} |",
                                    "SESSION ID", "PLAYER 1", "P1 SCORE", "PLAYER 2", "P2 SCORE", "DATE", "WINNER"));
            Debug.Log("-------------------------------------------------------------------------------------");
        }
        else
        {
            Debug.Log("-------------------------------------------------------------");
            Debug.Log(string.Format("| {0,-12} | {1,-15} | {2,-7} | {3,-19} | {4,-15} |",
                                    "SESSION ID", "PLAYER", "SCORE", "DATE", "RESULT"));
            Debug.Log("-------------------------------------------------------------");
        }
    }

    private void DisplayMultiplayerSessionRow(MultiplayerSessionData data, string sessionLabel = null)
    {
        if (data == null)
            return;

        string idToShow = FormatSessionID(data.sessionID, sessionLabel);
        Debug.Log(string.Format("| {0,-12} | {1,-15} | {2,-7} | {3,-15} | {4,-7} | {5,-19} | {6,-15} |",
                                idToShow, data.player1Name ?? "N/A", data.p1Score,
                                data.player2Name ?? "N/A", data.p2Score, data.date ?? "N/A", data.winner ?? "N/A"));
    }
    private void DisplaySinglePlayerSessionRow(SinglePlayerSessionData data, string sessionLabel = null)
    {
        if (data == null)
            return;

        string idToShow = FormatSessionID(data.sessionID, sessionLabel);
        Debug.Log(string.Format("| {0,-12} | {1,-15} | {2,-7} | {3,-19} | {4,-15} |",
                               idToShow, data.player1Name ?? "N/A", data.p1Score,
                               data.date ?? "N/A", data.result ?? "N/A"));
    }

    private string FormatSessionID(string id, string label)
    {
        string idToShow = string.IsNullOrEmpty(label) ? (id ?? "N/A") : label;

        if (idToShow.Length > 12)
            idToShow = idToShow.Substring(idToShow.Length - 9) + "...";

        return idToShow;
    }

    private void DisplayEndGameResultsFooter(bool isMultiplayer = true)
    {
        Debug.Log(isMultiplayer ? "-------------------------------------------------------------------------------------" : "-------------------------------------------------------------");
    }

    private void DisplaySessionHistory(List<object> sessions)
    {
        if (sessions == null)
        {
            Debug.LogWarning("Failed to fetch session history or no history exists.");
            return;
        }

        if (sessions.Count == 0)
        {
            Debug.Log($"No previous game session history found for {currentGameMode} - {currentDifficulty}.");
            return;
        }

        Debug.Log($"\n========== PREVIOUS HISTORY ({currentGameMode} - {currentDifficulty}) ==========");

        sessions.Sort((s1, s2) =>
        {
            int score1 = 0;
            int score2 = 0;

            if (currentGameMode == GameModeType.Multiplayer)
            {
                if (s1 is MultiplayerSessionData mp1)
                {
                    if (!string.IsNullOrEmpty(mp1.winner) && mp1.winner == mp1.player1Name)
                        score1 = mp1.p1Score;
                    else if (!string.IsNullOrEmpty(mp1.winner) && mp1.winner == mp1.player2Name)
                        score1 = mp1.p2Score;
                    else
                        score1 = Mathf.Max(mp1.p1Score, mp1.p2Score);
                }
                if (s2 is MultiplayerSessionData mp2)
                {
                    if (!string.IsNullOrEmpty(mp2.winner) && mp2.winner == mp2.player1Name)
                        score2 = mp2.p1Score;
                    else if (!string.IsNullOrEmpty(mp2.winner) && mp2.winner == mp2.player2Name)
                        score2 = mp2.p2Score;
                    else
                        score2 = Mathf.Max(mp2.p1Score, mp2.p2Score);
                }
            }
            else
            {
                if (s1 is SinglePlayerSessionData sp1)
                    score1 = sp1.p1Score;
                if (s2 is SinglePlayerSessionData sp2)
                    score2 = sp2.p1Score;
            }

            return score2.CompareTo(score1);
        });

        DisplayEndGameResultsHeader(currentGameMode == GameModeType.Multiplayer);

        foreach (object sessionObj in sessions)
        {
            if (currentGameMode == GameModeType.Multiplayer && sessionObj is MultiplayerSessionData mpData)
            {
                DisplayMultiplayerSessionRow(mpData);
            }
            else if (currentGameMode == GameModeType.SinglePlayer && sessionObj is SinglePlayerSessionData spData)
            {
                DisplaySinglePlayerSessionRow(spData);
            }
            else
            {
                Debug.LogWarning($"Received unexpected data type ({sessionObj?.GetType()}) in history for mode {currentGameMode}.");
            }
        }

        DisplayEndGameResultsFooter(currentGameMode == GameModeType.Multiplayer);
        Debug.Log("===========================================================");
    }

    private void StopAllGameActivity()
    {
        Debug.Log("Stopping all game activity...");

        if (enemySpawnerLeft != null)
            enemySpawnerLeft.enabled = false;

        if (enemySpawnerRight != null)
            enemySpawnerRight.enabled = false;

        if (chestSpawner != null)
            chestSpawner.enabled = false;

        if (playerLeft != null)
            playerLeft.enabled = false;
        if (playerRight != null)
            playerRight.enabled = false;

        ResetGlobalAbilities();

        Enemy[] activeEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null && !enemy.IsDead)
            {
                enemy.GetComponent<EnemyMovement>()?.StopMovement();

                enemy.StopAllCoroutines();

                Animator anim = enemy.GetComponent<Animator>();
                if (anim != null && anim.isActiveAndEnabled)
                {
                    anim.SetBool("isWalking", false);
                    anim.SetBool("isAttacking", false);
                    anim.SetBool("isStunned", false);

                    enemy.SetIdleDirection();
                }
            }
        }

        SpecialAbilityChest[] remainingChests = FindObjectsByType<SpecialAbilityChest>(FindObjectsSortMode.None);

        foreach (var chest in remainingChests)
        {
            if (chest != null)
                Destroy(chest.gameObject);
        }
    }
    #endregion
}