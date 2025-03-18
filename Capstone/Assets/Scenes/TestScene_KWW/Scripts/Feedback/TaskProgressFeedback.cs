using UnityEngine;
using UnityEngine.UI; // Slider�� ����ϱ� ���� �߰�
using TMPro; // TextMeshPro ���ӽ����̽� ���

public class TaskProgressFeedback : MonoBehaviour
{
    [Header("Task Settings")]
    [SerializeField]
    private TaskData[] tasks; // Inspector���� �߰��� �۾� ���

    [Header("UI Settings")]
    [SerializeField]
    private Slider progressSlider; // ���� ��Ȳ�� ǥ���� Slider
    [SerializeField]
    private TMP_Text progressText; // �ۼ�Ʈ�� ǥ���� TextMeshPro Text (TMP)

    [System.Serializable] // Inspector���� ǥ�� �����ϵ��� ����
    public class TaskData
    {
        [Tooltip("�۾��� �̸�")]
        public string taskName;
        [Tooltip("�۾��� �Ϸ�Ǿ����� ����")]
        public bool isCompleted;
    }

    private int totalTasks; // ��ü �۾� ��
    private int completedTasks; // �Ϸ�� �۾� ��

    void Start()
    {
        // �۾� ��� �ʱ�ȭ
        InitializeTasks();

        // UI ������Ʈ�� �������� �ʾ��� ��� ��� ���
        if (progressSlider == null)
        {
            Debug.LogWarning("Progress Slider is not assigned!");
        }
        if (progressText == null)
        {
            Debug.LogWarning("Progress Text (TMP_Text) is not assigned!");
        }

        // �ʱ� ���� ��Ȳ ������Ʈ
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

        // �Ϸ�� �۾� �� ���
        foreach (TaskData task in tasks)
        {
            if (task.isCompleted)
            {
                completedTasks++;
            }
        }
    }

    // �۾� �Ϸ� ���� ���� �޼���
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

    // ���� ��Ȳ ������Ʈ
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
            progressSlider.value = progress / 100f; // Slider�� 0~1 ���� ���
        }
        if (progressText != null)
        {
            progressText.text = Mathf.RoundToInt(progress) + "%"; // �ۼ�Ʈ�� ǥ��
        }

        Debug.Log($"Progress: {Mathf.RoundToInt(progress)}% ({completedTasks}/{totalTasks} tasks completed)");
    }

    // Inspector���� �۾� �߰� �� ���� ����
    [ContextMenu("Reset Tasks")]
    private void ResetTasks()
    {
        tasks = new TaskData[0]; // �۾� ��� �ʱ�ȭ
        InitializeTasks();
        UpdateProgress();
    }
}