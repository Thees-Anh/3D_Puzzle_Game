using UnityEngine;

namespace PuzzleRoom.Core
{
    /// <summary>Tracks gameplay time. Time.timeScale automatically pauses this timer.</summary>
    public sealed class GameTimer : MonoBehaviour
    {
        public float ElapsedSeconds { get; private set; }
        public bool IsRunning { get; private set; }
        public string FormattedTime => FormatTime(ElapsedSeconds);

        private void Start()
        {
            StartTimer();
        }

        private void Update()
        {
            if (IsRunning)
            {
                ElapsedSeconds += Time.deltaTime;
            }
        }

        public void StartTimer()
        {
            ElapsedSeconds = 0f;
            IsRunning = true;
        }

        public string StopTimer()
        {
            IsRunning = false;
            return FormattedTime;
        }

        public void Restore(float elapsedSeconds, bool running = true)
        {
            ElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            IsRunning = running;
        }

        public static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
