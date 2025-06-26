using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PanelOptions : MonoBehaviour
{
    [SerializeField] private Button _btnBack;

    [Header("Level Buttons")]
    [SerializeField] private Button LevelEasyButton;
    [SerializeField] private Button LevelNormalButton;
    [SerializeField] private Button LevelHardButton;

    [Header("Player Mode Toggles")]
    [SerializeField] private Toggle ToggleSinglePlayer;
    [SerializeField] private Toggle ToggleMultiplayer;

    [Header("Player Input Areas")]
    [SerializeField] private GameObject Player1_Single_InputArea;
    [SerializeField] private GameObject Player1_Multi_InputArea;
    [SerializeField] private GameObject Player2_Multi_InputArea;

    [Header("Nickname Inputs")]
    [SerializeField] private TMP_InputField NicknameP1_Single;
    [SerializeField] private TMP_InputField NicknameP1_Multi;
    [SerializeField] private TMP_InputField NicknameP2_Multi;

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    [Header("Start Button")]
    [SerializeField] private Button ButtonStart;

    public event Action OnBackClicked;

    [SerializeField] private AudioManager _audioManager;

    public void Init()
    {
        AddListener();
        Debug.Log("Settings Panel initialized");

        InitializeUIFromSettings();
    }

    private void InitializeUIFromSettings()
    {
        SelectLevelButton(GameSettings.SelectedDifficulty);

        if (GameSettings.SelectedMode == GameModeType.SinglePlayer)
        {
            ToggleSinglePlayer.isOn = true;
            ToggleMultiplayer.isOn = false;
            ShowSinglePlayerUI();
        }
        else
        {
            ToggleMultiplayer.isOn = true;
            ToggleSinglePlayer.isOn = false;
            ShowMultiPlayerUI();
        }

        NicknameP1_Single.text = GameSettings.Player1Name;
        NicknameP1_Multi.text = GameSettings.Player1Name;
        NicknameP2_Multi.text = GameSettings.Player2Name;
    }

    private void AddListener()
    {
        _btnBack.onClick.AddListener(() => OnBackClicked?.Invoke());

        LevelEasyButton.onClick.AddListener(() => SelectLevel(LevelEasyButton, GameDifficulty.Easy));
        LevelNormalButton.onClick.AddListener(() => SelectLevel(LevelNormalButton, GameDifficulty.Normal));
        LevelHardButton.onClick.AddListener(() => SelectLevel(LevelHardButton, GameDifficulty.Hard));

        ToggleSinglePlayer.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                if (ToggleMultiplayer.isOn)
                    ToggleMultiplayer.SetIsOnWithoutNotify(false);

                GameSettings.SelectedMode = GameModeType.SinglePlayer;

                _audioManager.PlayButtonClickSound();

                ShowSinglePlayerUI();
                UpdateNicknamesFromUI();
            }
            else if (!ToggleMultiplayer.isOn)
            {
                ToggleSinglePlayer.SetIsOnWithoutNotify(true);
            }
        });

        ToggleMultiplayer.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                if (ToggleSinglePlayer.isOn)
                    ToggleSinglePlayer.SetIsOnWithoutNotify(false);

                GameSettings.SelectedMode = GameModeType.Multiplayer;

                _audioManager.PlayButtonClickSound();

                ShowMultiPlayerUI();
                UpdateNicknamesFromUI();
            }
            else if (!ToggleSinglePlayer.isOn)
            {
                ToggleMultiplayer.SetIsOnWithoutNotify(true);
            }
        });

        NicknameP1_Single.onEndEdit.AddListener((value) => UpdateNicknamesFromUI());
        NicknameP1_Multi.onEndEdit.AddListener((value) => UpdateNicknamesFromUI());
        NicknameP2_Multi.onEndEdit.AddListener((value) => UpdateNicknamesFromUI());

        ButtonStart.onClick.AddListener(LoadGameScene);
    }

    private void SelectLevel(Button selectedButton, GameDifficulty difficulty)
    {
        GameSettings.SelectedDifficulty = difficulty;
        SelectLevelButton(difficulty);

        _audioManager.PlayButtonClickSound();

        Debug.Log($"Difficulty set to: {difficulty}");
    }

    private void SelectLevelButton(GameDifficulty difficulty)
    {
        LevelEasyButton.image.color = (difficulty == GameDifficulty.Easy) ? selectedColor : defaultColor;
        LevelNormalButton.image.color = (difficulty == GameDifficulty.Normal) ? selectedColor : defaultColor;
        LevelHardButton.image.color = (difficulty == GameDifficulty.Hard) ? selectedColor : defaultColor;
    }

    private void ShowSinglePlayerUI()
    {
        Player1_Single_InputArea.SetActive(true);
        Player1_Multi_InputArea.SetActive(false);
        Player2_Multi_InputArea.SetActive(false);
    }

    private void ShowMultiPlayerUI()
    {
        Player1_Single_InputArea.SetActive(false);
        Player1_Multi_InputArea.SetActive(true);
        Player2_Multi_InputArea.SetActive(true);
    }

    private void UpdateNicknamesFromUI()
    {
        if (GameSettings.SelectedMode == GameModeType.SinglePlayer)
        {
            GameSettings.Player1Name = string.IsNullOrWhiteSpace(NicknameP1_Single.text) ? "Player 1" : NicknameP1_Single.text;
        }
        else
        {
            GameSettings.Player1Name = string.IsNullOrWhiteSpace(NicknameP1_Multi.text) ? "Player 1" : NicknameP1_Multi.text;
            GameSettings.Player2Name = string.IsNullOrWhiteSpace(NicknameP2_Multi.text) ? "Player 2" : NicknameP2_Multi.text;
        }
    }

    private void LoadGameScene()
    {
        _audioManager.PlayButtonClickSound();

        Invoke(nameof(Loading), 0.25f);
    }

    private void Loading()
    {
        UpdateNicknamesFromUI();

        string sceneName = GameSettings.GetSceneName();

        Debug.Log($"Loading scene: {sceneName} (Mode: {GameSettings.SelectedMode}, Difficulty: {GameSettings.SelectedDifficulty}) with P1='{GameSettings.Player1Name}', P2='{GameSettings.Player2Name}'");

        if (!string.IsNullOrEmpty(sceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError($"Scene '{sceneName}' could not be loaded. Make sure it's added to Build Settings and the name is correct!");
            }
        }
        else
        {
            Debug.LogError("Could not determine a valid scene name based on current settings.");
        }
    }
}