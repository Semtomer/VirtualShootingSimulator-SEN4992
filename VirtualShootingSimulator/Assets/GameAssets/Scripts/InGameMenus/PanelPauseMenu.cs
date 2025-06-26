using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelPauseMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button buttonResume;
    [SerializeField] private Button buttonBackToMainMenu;
    [SerializeField] private Button buttonQuit;

    [Header("Audio Sliders (Optional)")]
    [SerializeField] private Slider sliderMusic;
    [SerializeField] private Slider sliderSFX;

    public event Action OnResumeClicked;
    public event Action OnBackToMainMenuClicked;
    public event Action OnQuitGameClicked;

    public void Init()
    {
        AddListeners();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegisterVolumeSliders(sliderMusic, sliderSFX);
        }
        else
        {
            if (sliderMusic != null) sliderMusic.gameObject.SetActive(false);
            if (sliderSFX != null) sliderSFX.gameObject.SetActive(false);
            Debug.LogWarning("PanelPauseMenu: AudioManager instance not provided, sound sliders not registered.");
        }
        Debug.Log("Pause Menu Panel initialized and sliders registered (if available).");
    }

    private void OnEnable()
    {
        Cursor.visible = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegisterVolumeSliders(sliderMusic, sliderSFX);
        }
        else
        {
            Debug.LogWarning("PanelPauseMenu: AudioManager instance not found on enable. Sliders won't sync.");
            if (sliderMusic != null) sliderMusic.gameObject.SetActive(false);
            if (sliderSFX != null) sliderSFX.gameObject.SetActive(false);
        }
    }

    private void AddListeners()
    {
        buttonResume?.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClickSound();
            OnResumeClicked?.Invoke();
        });

        buttonBackToMainMenu?.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClickSound();
            OnBackToMainMenuClicked?.Invoke();
        });

        buttonQuit?.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlayButtonClickSound();
            OnQuitGameClicked?.Invoke();
        });
    }

    private void OnDestroy()
    {
        Cursor.visible = false;
        buttonResume?.onClick.RemoveAllListeners();
        buttonBackToMainMenu?.onClick.RemoveAllListeners();
        buttonQuit?.onClick.RemoveAllListeners();
    }
}