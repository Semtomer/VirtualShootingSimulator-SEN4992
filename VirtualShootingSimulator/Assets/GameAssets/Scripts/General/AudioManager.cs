using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("AudioSource for background music.")]
    [SerializeField] public AudioSource musicSource;
    [Tooltip("Primary AudioSource for sound effects.")]
    [SerializeField] public AudioSource sfxSource;
    [Tooltip("Secondary AudioSource for important SFX that shouldn't be interrupted (e.g., player actions).")]
    [SerializeField] public AudioSource sfxSourcePlayerActions;

    [Header("Music Clips")]
    [Tooltip("Background music for main menu.")]
    [SerializeField] private AudioClip musicMainMenu;
    [Tooltip("Background music for snowy levels.")]
    [SerializeField] private AudioClip musicSnowy;
    [Tooltip("Background music for grassy levels.")]
    [SerializeField] private AudioClip musicGrassy;
    [Tooltip("Background music for swamp levels.")]
    [SerializeField] private AudioClip musicSwamp;

    [Header("SFX Clips - Player")]
    [SerializeField] public AudioClip sfxPlayerFire;
    [SerializeField] public AudioClip sfxSpecialAbilityUse;

    [Header("SFX Clips - Chest")]
    [SerializeField] public AudioClip sfxChestOpen;

    [Header("SFX Clips - Enemies")]
    [SerializeField] public AudioClip sfxSkeletonDie;
    [SerializeField] public AudioClip sfxHumanDie;
    [SerializeField] public AudioClip sfxAnimalDie;

    [Header("SFX Clips - Castle")]
    [SerializeField] public AudioClip sfxCastleHit;
    [SerializeField] public AudioClip sfxCastleDestroyed;

    [Header("SFX Clips - Game Flow")]
    [SerializeField] public AudioClip sfxGameOverMP;
    [SerializeField] public AudioClip sfxGameWinSP;
    [SerializeField] public AudioClip sfxGameLoseSP;
    [SerializeField] public AudioClip sfxButtonClick;

    private Slider currentMusicVolumeSlider;
    private Slider currentSfxVolumeSlider;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const float DEFAULT_VOLUME = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
        }
        else
        {
            Debug.LogWarning("Duplicate AudioManager found. Destroying new one.", this);
            Destroy(gameObject);
            return;
        }

        if (musicSource == null) Debug.LogError("AudioManager: MusicSource not assigned!", this);
        if (sfxSource == null) Debug.LogError("AudioManager: SFXSource not assigned!", this);
        if (sfxSourcePlayerActions == null) Debug.LogWarning("AudioManager: SFXSourcePlayerActions not assigned. Important player SFX might be interrupted.", this);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (currentMusicVolumeSlider != null)
            currentMusicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (currentSfxVolumeSlider != null)
            currentSfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"AudioManager: Scene Loaded - {scene.name}");
        if (scene.name == "MainMenu")
        {
            PlayMainMenuMusic();
        }
        else
        {
            PlayLevelMusic(GameSettings.SelectedDifficulty);
        }
    }

    private void LoadVolumeSettings()
    {
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_VOLUME);
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_VOLUME);

        if (musicSource != null) musicSource.volume = musicVol;
        if (sfxSource != null) sfxSource.volume = sfxVol;
        if (sfxSourcePlayerActions != null) sfxSourcePlayerActions.volume = sfxVol;
    }

    public void RegisterVolumeSliders(Slider musicSliderToRegister, Slider sfxSliderToRegister)
    {
        if (currentMusicVolumeSlider != null)
            currentMusicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
        if (currentSfxVolumeSlider != null)
            currentSfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);

        currentMusicVolumeSlider = musicSliderToRegister;
        currentSfxVolumeSlider = sfxSliderToRegister;

        if (currentMusicVolumeSlider != null && musicSource != null)
        {
            currentMusicVolumeSlider.value = musicSource.volume;
            currentMusicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (currentSfxVolumeSlider != null && sfxSource != null)
        {
            currentSfxVolumeSlider.value = sfxSource.volume;
            currentSfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
        }
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlayMainMenuMusic()
    {
        if (musicMainMenu != null) PlayMusic(musicMainMenu);
        else Debug.LogWarning("No main menu music found.");
    }

    public void PlayLevelMusic(GameDifficulty difficulty)
    {
        AudioClip clipToPlay = null;

        switch (difficulty)
        {
            case GameDifficulty.Easy:
                clipToPlay = musicGrassy;
                break;
            case GameDifficulty.Normal:
                clipToPlay = musicSnowy;
                break;
            case GameDifficulty.Hard:
                clipToPlay = musicSwamp;
                break;
        }

        if (clipToPlay != null) PlayMusic(clipToPlay);
        else Debug.LogWarning("No specific music found for current level/difficulty.");
    }

    public void PlayDieSound(GameDifficulty difficulty)
    {
        AudioClip clipToPlay = null;
        switch (difficulty)
        {
            case GameDifficulty.Easy:
                clipToPlay = sfxAnimalDie;
                break;
            case GameDifficulty.Normal:
                clipToPlay = sfxHumanDie;
                break;
            case GameDifficulty.Hard:
                clipToPlay = sfxSkeletonDie;
                break;
        }
        if (clipToPlay != null) PlaySFX(clipToPlay);
        else Debug.LogWarning("No specific music found for current level/difficulty.");
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXPlayerAction(AudioClip clip)
    {
        if (sfxSourcePlayerActions == null)
        {
            PlaySFX(clip);
            if (sfxSourcePlayerActions == null && clip != null)
                Debug.LogWarning("SFXPlayerActions source not set, playing on primary SFX source.");
            return;
        }
        if (clip == null) return;
        sfxSourcePlayerActions.PlayOneShot(clip);
    }


    public void SetSfxVolume(float volume)
    {
        if (sfxSource != null) sfxSource.volume = volume;
        if (sfxSourcePlayerActions != null) sfxSourcePlayerActions.volume = volume;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, volume);
        PlayerPrefs.Save();
    }

    public void PlayButtonClickSound()
    {
        PlaySFX(sfxButtonClick);
    }
}