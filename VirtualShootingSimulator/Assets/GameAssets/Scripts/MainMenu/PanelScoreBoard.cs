using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelScoreBoard : MonoBehaviour
{
    [SerializeField] private Button _btnBack;

    [Header("Level Buttons")]
    [SerializeField] private Button _btnEasy;
    [SerializeField] private Button _btnNormal;
    [SerializeField] private Button _btnHard;

    [Header("Player Toggles")]
    [SerializeField] private Toggle _toggleSingle;
    [SerializeField] private Toggle _toggleMulti;

    [Header("Header Panels")]
    [SerializeField] private GameObject singlePlayerHeader;
    [SerializeField] private GameObject multiplayerHeader;

    [Header("UI Prefab")]
    [SerializeField] private GameObject singlePlayerScoreEntryPrefab;
    [SerializeField] private GameObject multiplayerScoreEntryPrefab;

    [Header("Data Container Transform")]
    [SerializeField] private Transform dataContainer;

    public event Action OnBackClicked;
    private GameDifficulty _selectedLevel = GameDifficulty.Normal;
    private GameModeType _selectedPlayerMode = GameModeType.Multiplayer;

    private bool _isFetchingData = false;
    private bool _isFirebaseReady = false;

    [SerializeField] private AudioManager _audioManager;

    public void Init()
    {
        if (singlePlayerHeader == null || multiplayerHeader == null ||
            singlePlayerScoreEntryPrefab == null || multiplayerScoreEntryPrefab == null ||
            dataContainer == null)
        {
            Debug.LogError("PanelScoreBoard: Inspector references missing! Assign headers, prefabs, and data container.", this);
            enabled = false;
            return;
        }

        AddListener();

        UpdateLevelSelection(_btnNormal, GameDifficulty.Normal);
        HighlightButton(_btnNormal);

        _toggleSingle.isOn = false;
        _toggleMulti.isOn = true;

        FilterContents();
    }

    private void OnEnable()
    {
        FirebaseManager.OnFirebaseInitialized += HandleFirebaseInitialized;

        if (enabled && FirebaseManager.IsInitialized)
        {
            _isFirebaseReady = true;
            FetchAndDisplayScores();
        }
        else if (enabled)
        {
            Debug.Log("PanelScoreBoard OnEnable: Waiting for Firebase initialization...");
        }
    }

    private void OnDisable()
    {
        FirebaseManager.OnFirebaseInitialized -= HandleFirebaseInitialized;
        _isFirebaseReady = false;
    }

    private void HandleFirebaseInitialized()
    {
        _isFirebaseReady = true;
        if (enabled && gameObject.activeInHierarchy)
        {
            FetchAndDisplayScores();
        }
    }

    private void AddListener()
    {
        _btnBack.onClick.AddListener(() => OnBackClicked?.Invoke());

        _btnEasy.onClick.AddListener(() => UpdateLevelSelection(_btnEasy, GameDifficulty.Easy));
        _btnNormal.onClick.AddListener(() => UpdateLevelSelection(_btnNormal, GameDifficulty.Normal));
        _btnHard.onClick.AddListener(() => UpdateLevelSelection(_btnHard, GameDifficulty.Hard));

        _toggleSingle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                if (_toggleMulti.isOn)
                    _toggleMulti.SetIsOnWithoutNotify(false);

                _selectedPlayerMode = GameModeType.SinglePlayer;

                _audioManager.PlayButtonClickSound();

                FilterContents();
            }
            else if (!_toggleMulti.isOn)
            {
                _toggleSingle.SetIsOnWithoutNotify(true);
            }
        });

        _toggleMulti.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                if (_toggleSingle.isOn)
                    _toggleSingle.SetIsOnWithoutNotify(false);

                _selectedPlayerMode = GameModeType.Multiplayer;

                _audioManager.PlayButtonClickSound();

                FilterContents();
            }
            else if (!_toggleSingle.isOn)
            {
                _toggleMulti.SetIsOnWithoutNotify(true);
            }
        });
    }

    private void UpdateLevelSelection(Button selectedButton, GameDifficulty difficulty)
    {
        if (_selectedLevel == difficulty)
            return;

        _selectedLevel = difficulty;

        _audioManager.PlayButtonClickSound();

        ResetButtonColors();
        HighlightButton(selectedButton);

        FilterContents();
    }

    private void ResetButtonColors()
    {
        SetButtonColor(_btnEasy, Color.white);
        SetButtonColor(_btnNormal, Color.white);
        SetButtonColor(_btnHard, Color.white);
    }

    private void HighlightButton(Button btn)
    {
        SetButtonColor(btn, Color.gray);
    }

    private void SetButtonColor(Button btn, Color color)
    {
        if (btn == null)
            return;

        var cb = btn.colors;

        cb.normalColor = color;
        cb.selectedColor = color;
        btn.colors = cb;
    }

    private void FilterContents()
    {
        bool isSinglePlayer = (_selectedPlayerMode == GameModeType.SinglePlayer);

        singlePlayerHeader.SetActive(isSinglePlayer);
        multiplayerHeader.SetActive(!isSinglePlayer);

        ClearScoreData(dataContainer);
        FetchAndDisplayScores();
    }

    private void ClearScoreData(Transform dataContainer)
    {
        if (dataContainer == null)
            return;

        foreach (Transform child in dataContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void FetchAndDisplayScores()
    {
        if (!_isFirebaseReady)
        {
            Debug.Log("FetchAndDisplayScores: Firebase not ready yet. Aborting fetch.");
            return;
        }

        if (_isFetchingData)
            return;

        _isFetchingData = true;
        ClearScoreData(dataContainer);

        FirebaseManager.Instance.FetchSpecificSessionHistory(
            _selectedPlayerMode,
            _selectedLevel,
            (scores) => OnScoresReceived(scores, dataContainer)
        );
    }

    private void OnScoresReceived(List<object> scores, Transform targetDataContainer)
    {
        _isFetchingData = false;

        if (targetDataContainer == null)
        {
            Debug.LogError("Target data container is null in OnScoresReceived.");
            return;
        }

        if (scores == null)
        {
            Debug.LogWarning("Failed to fetch scores or Firebase error.");
            return;
        }

        if (scores.Count == 0)
        {
            Debug.Log($"No scores found for {_selectedPlayerMode} / {_selectedLevel}.");
            return;
        }

        scores.Sort((s1, s2) =>
        {
            int score1 = 0;
            int score2 = 0;

            if (_selectedPlayerMode == GameModeType.Multiplayer)
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

        GameObject prefabToInstantiate = (_selectedPlayerMode == GameModeType.SinglePlayer)
            ? singlePlayerScoreEntryPrefab
            : multiplayerScoreEntryPrefab;

        foreach (object sessionData in scores)
        {
            GameObject entryInstance = Instantiate(prefabToInstantiate, targetDataContainer);
            ScoreEntryUI entryUI = entryInstance.GetComponent<ScoreEntryUI>();

            if (entryUI != null)
            {
                if (_selectedPlayerMode == GameModeType.Multiplayer && sessionData is MultiplayerSessionData mpData)
                {
                    entryUI.SetMultiplayerData(mpData);
                }
                else if (_selectedPlayerMode == GameModeType.SinglePlayer && sessionData is SinglePlayerSessionData spData)
                {
                    entryUI.SetSinglePlayerData(spData);
                }
                else
                {
                    Debug.LogWarning($"Data type mismatch or null data received for mode {_selectedPlayerMode}");
                }
            }
            else
            {
                Debug.LogError($"ScoreEntryUI component not found on the instantiated prefab '{prefabToInstantiate.name}'!");
            }
        }
    }

    private int GetMultiplayerSortScore(MultiplayerSessionData data)
    {
        if (data == null)
            return 0;

        if (!string.IsNullOrEmpty(data.winner) && data.winner != "Draw")
        {
            if (data.winner == data.player1Name)
                return data.p1Score;

            if (data.winner == data.player2Name)
                return data.p2Score;
        }

        return Mathf.Max(data.p1Score, data.p2Score);
    }
}
