using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelMainMenu : MonoBehaviour
{
    [SerializeField] private Button _btnPlay;
    [SerializeField] private Button _btnScoreBoard;
    [SerializeField] private Button _btnRulesWarnings;
    [SerializeField] private Button _btnQuit;

    public event Action OnPlayClicked;
    public event Action OnScoreBoardClicked;
    public event Action OnRulesWarningsClicked;
    public event Action OnQuitClicked;

    public void Init()
    {
        AddListener();
        Debug.Log("Main Menu Panel initialized.");
    }

    private void AddListener()
    {
        _btnPlay.onClick.AddListener(() => OnPlayClicked?.Invoke());
        _btnScoreBoard.onClick.AddListener(() => OnScoreBoardClicked?.Invoke());
        _btnRulesWarnings.onClick.AddListener(() => OnRulesWarningsClicked?.Invoke());
        _btnQuit.onClick.AddListener(() => OnQuitClicked?.Invoke());
    }
}