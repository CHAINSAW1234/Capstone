using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI; // Slider를 사용하기 위해 추가
using TMPro; // TextMeshPro 네임스페이스 사용

public class TaskProgressFeedback : MonoBehaviour
{
    [Header("Progress Settings")]
    [SerializeField] private int totalSteps = 10; // 전체 진행 단계 (예: 10단계)
    [SerializeField] private int currentStep = 0; // 현재 진행 단계

    [Header("UI Settings")]
    [SerializeField] private Slider progressSlider; // 진행 상황을 표시할 Slider
    [SerializeField] private TMP_Text progressText; // 퍼센트를 표시할 TextMeshPro Text (TMP)

    [Header("Progress Events")]
    public UnityEvent onProgressIncreased; // 진행도가 증가할 때 호출될 이벤트
    public UnityEvent onProgressDecreased; // 진행도가 감소할 때 호출될 이벤트

    private void Awake()
    {
        SetupComponents();
    }

    void Start()
    {
        // 초기 진행 상황 업데이트
        UpdateProgress();
    }

    // 필요한 UI 컴포넌트 자동 추가 및 설정
    private void SetupComponents()
    {
        // Slider 컴포넌트 확인 및 추가
        if (progressSlider == null)
        {
            progressSlider = GetComponent<Slider>();
            if (progressSlider == null)
            {
                GameObject sliderObj = new GameObject("ProgressSlider");
                sliderObj.transform.SetParent(transform, false);
                progressSlider = sliderObj.AddComponent<Slider>();
                progressSlider.minValue = 0f;
                progressSlider.maxValue = 1f;
                progressSlider.value = 0f;
                Debug.Log("Progress Slider automatically added to the GameObject.");
            }
        }

        // TMP_Text 컴포넌트 확인 및 추가
        if (progressText == null)
        {
            progressText = GetComponent<TMP_Text>();
            if (progressText == null)
            {
                GameObject textObj = new GameObject("ProgressText");
                textObj.transform.SetParent(transform, false);
                progressText = textObj.AddComponent<TMP_Text>();
                progressText.text = "0%";
                progressText.fontSize = 24;
                Debug.Log("Progress Text (TMP_Text) automatically added to the GameObject.");
            }
        }
    }

    // 진행도를 증가시키는 메서드 (이벤트에서 호출)
    public void IncreaseProgress()
    {
        if (currentStep < totalSteps)
        {
            currentStep++;
            UpdateProgress();
            onProgressIncreased?.Invoke(); // 진행도 증가 이벤트 호출
        }
    }

    // 진행도를 감소시키는 메서드 (이벤트에서 호출)
    public void DecreaseProgress()
    {
        if (currentStep > 0)
        {
            currentStep--;
            UpdateProgress();
            onProgressDecreased?.Invoke(); // 진행도 감소 이벤트 호출
        }
    }

    // 진행 상황 업데이트
    private void UpdateProgress()
    {
        if (totalSteps == 0)
        {
            if (progressSlider != null) progressSlider.value = 0f;
            if (progressText != null) progressText.text = "0%";
            return;
        }

        float progress = (float)currentStep / totalSteps * 100f;
        if (progressSlider != null)
        {
            progressSlider.value = progress / 100f; // Slider는 0~1 범위 사용
        }
        if (progressText != null)
        {
            progressText.text = Mathf.RoundToInt(progress) + "%"; // 퍼센트로 표시
        }

        Debug.Log($"Progress: {Mathf.RoundToInt(progress)}% ({currentStep}/{totalSteps} steps completed)");
    }

    // Inspector에서 설정 가능하도록 속성 추가
    public int TotalSteps
    {
        get => totalSteps;
        set
        {
            totalSteps = Mathf.Max(1, value); // 최소 1로 설정
            UpdateProgress();
        }
    }

    public int CurrentStep
    {
        get => currentStep;
        set
        {
            currentStep = Mathf.Clamp(value, 0, totalSteps);
            UpdateProgress();
        }
    }

    // Inspector에서 진행도 초기화
    [ContextMenu("Reset Progress")]
    private void ResetProgress()
    {
        currentStep = 0;
        UpdateProgress();
    }
}