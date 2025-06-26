using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{

    [Tooltip("The TextMeshProUGUI component to display the timer.")]
    [SerializeField] private TextMeshProUGUI timerText;

    private void Start()
    {
        if (timerText == null)
        {
            Debug.LogError("TimerText (TextMeshProUGUI) is not assigned in the Timer script! UI will not update.", this);
            enabled = false;
            return;
        }

        UpdateTimeDisplay(0f);
    }

    public void UpdateTimeDisplay(float timeInSeconds)
    {
        if (timerText == null) return;

        if (timeInSeconds < 0) timeInSeconds = 0;

        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (timeInSeconds <= 10 && timeInSeconds > 0)
        {
            timerText.color = Color.yellow;
        }
        else if (timeInSeconds == 0)
        {
            timerText.color = Color.red;
        }
        else
        {
            timerText.color = Color.white;
        }
    }
}
