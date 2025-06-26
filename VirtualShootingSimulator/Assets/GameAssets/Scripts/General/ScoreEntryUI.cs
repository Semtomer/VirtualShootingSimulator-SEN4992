using UnityEngine;
using TMPro;

public class ScoreEntryUI : MonoBehaviour
{
    [Header("Multiplayer Texts")]
    public TextMeshProUGUI textP1Name;
    public TextMeshProUGUI textP1Score;
    public TextMeshProUGUI textP2Name;
    public TextMeshProUGUI textP2Score;
    public TextMeshProUGUI textWinner;
    public TextMeshProUGUI textDateMP;

    [Header("Single Player Texts")]
    public TextMeshProUGUI textPlayerNameSP;
    public TextMeshProUGUI textScoreSP;
    public TextMeshProUGUI textResultSP;
    public TextMeshProUGUI textDateSP;

    public void SetMultiplayerData(MultiplayerSessionData data)
    {
        SetSPActive(false);
        SetMPActive(true);

        textP1Name.text = data.player1Name ?? "N/A";
        textP1Score.text = data.p1Score.ToString();
        textP2Name.text = data.player2Name ?? "N/A";
        textP2Score.text = data.p2Score.ToString();
        textWinner.text = data.winner ?? "N/A";
        textDateMP.text = data.date ?? "N/A";
    }

    public void SetSinglePlayerData(SinglePlayerSessionData data)
    {
        SetMPActive(false);
        SetSPActive(true);

        textPlayerNameSP.text = data.player1Name ?? "N/A";
        textScoreSP.text = data.p1Score.ToString();
        textResultSP.text = data.result ?? "N/A";
        textDateSP.text = data.date ?? "N/A";
    }

    private void SetMPActive(bool isActive)
    {
        if (textP1Name != null)
            textP1Name.gameObject.SetActive(isActive);

        if (textP1Score != null)
            textP1Score.gameObject.SetActive(isActive);

        if (textP2Name != null)
            textP2Name.gameObject.SetActive(isActive);

        if (textP2Score != null)
            textP2Score.gameObject.SetActive(isActive);

        if (textWinner != null)
            textWinner.gameObject.SetActive(isActive);

        if (textDateMP != null)
            textDateMP.gameObject.SetActive(isActive);
    }

    private void SetSPActive(bool isActive)
    {
        if (textPlayerNameSP != null)
            textPlayerNameSP.gameObject.SetActive(isActive);

        if (textScoreSP != null)
            textScoreSP.gameObject.SetActive(isActive);

        if (textResultSP != null)
            textResultSP.gameObject.SetActive(isActive);

        if (textDateSP != null)
            textDateSP.gameObject.SetActive(isActive);
    }
}