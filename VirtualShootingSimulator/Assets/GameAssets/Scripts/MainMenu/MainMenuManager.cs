using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuManager : MonoBehaviour
{
    [SerializeField] private PanelOptions _settingsPanel;
    [SerializeField] private PanelMainMenu _mainMenuPanel;
    [SerializeField] private PanelScoreBoard _scoreBoardPanel;
    [SerializeField] private PanelRulesWarnings _rulesWarningsPanel;
    [SerializeField] private Slider musicSlider_MainMenu;
    [SerializeField] private Slider sfxSlider_MainMenu;

    private void Start()
    {
        Cursor.visible = true;
        _settingsPanel.Init();
        _mainMenuPanel.Init();
        _scoreBoardPanel.Init();
        _rulesWarningsPanel.Init();

        CloseAllPanels();
        _mainMenuPanel.gameObject.SetActive(true);

        EventListener();

        Debug.Log("Main Menu Manager initialized");

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegisterVolumeSliders(musicSlider_MainMenu, sfxSlider_MainMenu);
        }
        else
        {
            Debug.LogWarning("PanelOptions: AudioManager instance not found on enable. Sliders won't sync.");
            if (musicSlider_MainMenu != null) musicSlider_MainMenu.gameObject.SetActive(false);
            if (sfxSlider_MainMenu != null) sfxSlider_MainMenu.gameObject.SetActive(false);
        }
    }
    private void EventListener()
    {
        _mainMenuPanel.OnPlayClicked += OnPlayClicked;
        _mainMenuPanel.OnScoreBoardClicked += OnScoreBoardClicked;
        _mainMenuPanel.OnRulesWarningsClicked += OnRulesWarningsClicked;
        _mainMenuPanel.OnQuitClicked += OnQuitClicked;

        _settingsPanel.OnBackClicked += OnBackClicked;

        _scoreBoardPanel.OnBackClicked += OnBackClicked;

        _rulesWarningsPanel.OnBackClicked += OnBackClicked;
    }

    private void CloseAllPanels()
    {
        _settingsPanel.gameObject.SetActive(false);
        _mainMenuPanel.gameObject.SetActive(false);
        _scoreBoardPanel.gameObject.SetActive(false);
        _rulesWarningsPanel.gameObject.SetActive(false);
    }

    #region Main Menu
    private void OnPlayClicked()
    {
        Debug.Log("OnPlayClicked");
        CloseAllPanels();
        _settingsPanel.gameObject.SetActive(true);
        AudioManager.Instance.PlayButtonClickSound();

    }

    private void OnScoreBoardClicked()
    {
        Debug.Log("ScoreBoard Panel");
        CloseAllPanels();
        _scoreBoardPanel.gameObject.SetActive(true);
        AudioManager.Instance.PlayButtonClickSound();

    }

    private void OnRulesWarningsClicked()
    {
        Debug.Log("Rules&Warnings Panel");
        CloseAllPanels();
        _rulesWarningsPanel.gameObject.SetActive(true);
        AudioManager.Instance.PlayButtonClickSound();

    }

    private void OnQuitClicked()
    {
        AudioManager.Instance.PlayButtonClickSound();

        Invoke(nameof(Quitting), 0.25f);
    }

    private void Quitting()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    #endregion

    private void OnBackClicked()
    {
        CloseAllPanels();
        _mainMenuPanel.gameObject.SetActive(true);
        AudioManager.Instance.PlayButtonClickSound();
    }
}