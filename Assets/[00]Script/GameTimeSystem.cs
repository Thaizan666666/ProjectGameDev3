using UnityEngine;
using System;

public class GameTimeSystem : MonoBehaviour
{
    public event Action<int, int, int> OnTimeChanged;
    public event Action<int> OnDayChanged;

    [SerializeField] private float realSecondsPerGameMinute = 1f;

    private float timer;

    public int Day { get; private set; } = 1;
    public int Hour { get; private set; } = 8;
    public int Minute { get; private set; }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= realSecondsPerGameMinute)
        {
            timer = 0;
            AddMinute();
        }
    }

    private void AddMinute()
    {
        Minute++;

        if (Minute >= 60)
        {
            Minute = 0;
            Hour++;
        }

        if (Hour >= 24)
        {
            Hour = 0;
            Day++;
            OnDayChanged?.Invoke(Day);
        }

        OnTimeChanged?.Invoke(Day, Hour, Minute);
    }
}