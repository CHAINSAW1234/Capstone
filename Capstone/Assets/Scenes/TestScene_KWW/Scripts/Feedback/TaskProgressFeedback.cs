using UnityEngine;
using UnityEngine.UI; // Slider를 사용하기 위해 추가
using TMPro; // TextMeshPro 네임스페이스 사용

public class TaskProgressFeedback : MonoBehaviour
{
    [Header("Task Settings")]
    [SerializeField]
    private TaskData[] tasks; // Inspector에서 추가할 작업 목록

    [Header("UI Settings")]
    [SerializeField]
    private Slider progressSlider; // 진행 상황을 표시할 Slider
    [SerializeField]
    private TMP_Text progressText; // 퍼센트를 표시할 TextMeshPro Text (TMP)

    [System.Serializable] // Inspector에서 표시 가능하도록 설정
    public class TaskData
    {
        [Tooltip("작업의 이름")]
        public string taskName;
        [Tooltip("작업이 완료되었는지 여부")]
        public bool isCompleted;
    }

    private int totalTasks; // 전체 작업 수
    private int completedTasks; // 완료된 작업 수

    void Start()
    {
        // 작업 목록 초기화
        InitializeTasks();

        // UI 컴포넌트가 설정되지 않았을 경우 경고 출력
        if (progressSlider == null)
        {
            Debug.LogWarning("Progress Slider is not assigned!");
        }
        if (progressText == null)
        {
            Debug.LogWarning("Progress Text (TMP_Text) is not assigned!");
        }

        // 초기 진행 상황 업데이트
        UpdateProgress();
    }

    void InitializeTasks()
    {
        if (tasks == null || tasks.Length == 0)
        {
            Debug.LogWarning("No tasks assigned in Inspector!");
            return;
        }

        totalTasks = tasks.Length;
        completedTasks = 0;

        // 완료된 작업 수 계산
        foreach (TaskData task in tasks)
        {
            if (task.isCompleted)
            {
                completedTasks++;
            }
        }
    }

    // 작업 완료 상태 변경 메서드
    public void SetTaskCompleted(string taskName, bool completed)
    {
        if (tasks == null) return;

        foreach (TaskData task in tasks)
        {
            if (task.taskName == taskName)
            {
                bool previousState = task.isCompleted;
                task.isCompleted = completed;

                if (completed && !previousState)
                {
                    completedTasks++;
                }
                else if (!completed && previousState)
                {
                    completedTasks--;
                }
                UpdateProgress();
                break;
            }
        }
    }

    // 진행 상황 업데이트
    private void UpdateProgress()
    {
        if (totalTasks == 0)
        {
            if (progressSlider != null) progressSlider.value = 0f;
            if (progressText != null) progressText.text = "0%";
            return;
        }

        float progress = (float)completedTasks / totalTasks * 100f;
        if (progressSlider != null)
        {
            progressSlider.value = progress / 100f; // Slider는 0~1 범위 사용
        }
        if (progressText != null)
        {
            progressText.text = Mathf.RoundToInt(progress) + "%"; // 퍼센트로 표시
        }

        Debug.Log($"Progress: {Mathf.RoundToInt(progress)}% ({completedTasks}/{totalTasks} tasks completed)");
    }

    // Inspector에서 작업 추가 및 편집 가능
    [ContextMenu("Reset Tasks")]
    private void ResetTasks()
    {
        tasks = new TaskData[0]; // 작업 목록 초기화
        InitializeTasks();
        UpdateProgress();
    }
}