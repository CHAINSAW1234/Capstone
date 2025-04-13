using UnityEngine;
using TMPro;
using UnityEngine.Events;

namespace FireEvacuation
{
    public class TimeManager : MonoBehaviour
    {
        [Header("시간 표시 설정")]
        [SerializeField] private TMP_Text timerText; // 시간을 표시할 Text Mesh Pro UI
        private float elapsedTime = 0f; // 경과 시간 (초 단위)
        private bool isTimerRunning = true; // 타이머가 실행 중인지 여부

        [Header("이벤트 설정")]
        [SerializeField] private UnityEvent onTimerStopped; // 타이머가 멈췄을 때 호출할 이벤트

        private void Start()
        {
            InitializeTimer();
        }

        void InitializeTimer()
        {
            if (timerText == null)
            {
                Debug.LogError("Timer Text (TMP_Text)가 지정되지 않았습니다!", this);
                return;
            }

            elapsedTime = 0f;
            isTimerRunning = true;
            UpdateTimerDisplay();
        }

        void Update()
        {
            if (isTimerRunning)
            {
                elapsedTime += Time.deltaTime;
                UpdateTimerDisplay();
            }
        }

        void UpdateTimerDisplay()
        {
            if (timerText == null) return;

            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        // 외부에서 호출하여 타이머를 멈추는 메서드
        public void StopTimer()
        {
            if (isTimerRunning)
            {
                isTimerRunning = false;
                Debug.Log("Timer stopped at: " + timerText.text);
                onTimerStopped?.Invoke();
            }
        }

        // 현재 경과 시간을 반환하는 메서드 (필요 시 사용)
        public float GetElapsedTime()
        {
            return elapsedTime;
        }

        // 타이머를 재시작하는 메서드 (필요 시 사용)
        public void RestartTimer()
        {
            elapsedTime = 0f;
            isTimerRunning = true;
            UpdateTimerDisplay();
        }
    }
}