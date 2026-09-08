using UnityEngine;
using TMPro;

public class GameTimeUI : MonoBehaviour
{
    [SerializeField] private GameTimeSystem gameTime;
    [SerializeField] private TextMeshProUGUI timeText;

    private void OnEnable()
    {
        gameTime.OnTimeChanged += UpdateTimeUI;
    }

    private void OnDisable()
    {
        gameTime.OnTimeChanged -= UpdateTimeUI;
    }

    private void UpdateTimeUI(int day, int hour, int minute)
    {
        timeText.text = $"Day {day}\n{hour:00}:{minute:00}";
    }
}