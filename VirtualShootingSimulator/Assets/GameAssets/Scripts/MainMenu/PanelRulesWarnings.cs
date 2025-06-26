using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelRulesWarnings : MonoBehaviour
{
    [SerializeField] private Button _btnBack;

    public event Action OnBackClicked;

    private void AddListener()
    {
        _btnBack.onClick.AddListener(() => OnBackClicked?.Invoke());

    }
    public void Init()
    {
        AddListener();
        Debug.Log("Rules & Warnings initialized");
    }
}
