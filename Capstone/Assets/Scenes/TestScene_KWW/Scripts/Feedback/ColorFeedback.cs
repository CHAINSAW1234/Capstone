using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ColorFeedback : MonoBehaviour
{
    [Header("Color Settings")]
    [SerializeField]
    private Color grabbedColor = Color.red; // 물체를 잡았을 때 색상 (빨간색)
    [SerializeField]
    private Color correctPositionColor = Color.green; // 올바른 위치 색상 (초록색)
    [SerializeField]
    private Color incorrectPositionColor = Color.red; // 올바르지 않은 위치 색상 (빨간색)
    [SerializeField]
    [Range(0f, 1f)]
    private float colorBlendAmount = 0.5f; // 원래 색상과 피드백 색상의 혼합 비율 (0: 원래 색상, 1: 피드백 색상)

    [Header("Target Objects")]
    [SerializeField]
    private Collider targetCollider; // 올바른 위치를 나타낼 Collider
    [SerializeField]
    private Renderer targetRenderer; // 올바른 위치를 나타낼 타겟의 렌더러 (targetCollider와 연결)

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable itemInteractable; // 자동으로 가져오거나 추가
    private Renderer itemRenderer; // 색상을 변경할 물건의 렌더러
    private Color originalItemColor; // 물체의 원래 색상 저장
    private Color originalTargetColor; // 타겟의 원래 색상 저장
    private bool isGrabbed = false; // 물체가 잡혔는지 여부
    private bool isInCorrectPosition = false; // 물체가 올바른 위치에 있는지 여부

    void Awake()
    {
        // XRGrabInteractable 자동 가져오기 또는 추가
        itemInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (itemInteractable == null)
        {
            itemInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            Debug.Log("XRGrabInteractable component added to: " + gameObject.name);
        }

        // Renderer 가져오기
        itemRenderer = GetComponent<Renderer>();
        if (itemRenderer == null)
        {
            Debug.LogError("This object does not have a Renderer component! ColorFeedback will not work.");
            enabled = false; // 컴포넌트 비활성화
            return;
        }

        // 원래 색상 저장
        originalItemColor = itemRenderer.material.color;
    }

    void Start()
    {
        // targetRenderer가 targetCollider와 연결 확인
        if (targetCollider != null && targetRenderer == null)
        {
            targetRenderer = targetCollider.GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogWarning("Target Collider does not have a Renderer component. Assign a Renderer manually.");
            }
        }

        if (targetCollider == null)
        {
            Debug.LogError("Target Collider is not assigned! ColorFeedback will not work.");
            enabled = false; // 컴포넌트 비활성화
            return;
        }

        if (targetRenderer == null)
        {
            Debug.LogError("Target Renderer is not assigned and could not be found on Target Collider! ColorFeedback will not work.");
            enabled = false; // 컴포넌트 비활성화
            return;
        }

        // 타겟의 원래 색상 저장
        originalTargetColor = targetRenderer.material.color;

        // Grab/Select 이벤트 구독
        itemInteractable.selectEntered.AddListener(OnGrabbed);
        itemInteractable.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        if (itemInteractable != null)
        {
            itemInteractable.selectEntered.RemoveListener(OnGrabbed);
            itemInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Enter: " + other.name); // 디버깅용 로그
        if (isGrabbed && other == targetCollider)
        {
            isInCorrectPosition = true;
            UpdateColors();
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("Trigger Exit: " + other.name); // 디버깅용 로그
        if (isGrabbed && other == targetCollider)
        {
            isInCorrectPosition = false;
            UpdateColors();
        }
    }

    // 물건이 잡혔을 때 호출
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        UpdateColors(); // 색상 업데이트
        Debug.Log("Item grabbed, color updated.");
    }

    // 물건이 놓였을 때 호출
    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        if (itemRenderer != null)
        {
            // 소켓 안에 있든 밖에 있든 Grab이 풀리면 원래 색상으로 복원
            itemRenderer.material.color = originalItemColor;
            Debug.Log("Item released, color reset to original: " + originalItemColor);
        }
        isInCorrectPosition = false; // 위치 상태 초기화
        UpdateColors(); // 타겟 색상 업데이트
    }

    // 색상 업데이트
    private void UpdateColors()
    {
        if (isGrabbed && itemRenderer != null)
        {
            // 원래 색상과 피드백 색상을 혼합
            Color targetColor = isInCorrectPosition ? correctPositionColor : grabbedColor;
            itemRenderer.material.color = Color.Lerp(originalItemColor, targetColor, colorBlendAmount);
            Debug.Log($"Item color updated. In correct position: {isInCorrectPosition}, Color: {itemRenderer.material.color}");
        }

        if (targetRenderer != null)
        {
            if (isGrabbed)
            {
                // 타겟도 원래 색상과 피드백 색상을 혼합
                Color targetFeedbackColor = isInCorrectPosition ? correctPositionColor : incorrectPositionColor;
                targetRenderer.material.color = Color.Lerp(originalTargetColor, targetFeedbackColor, colorBlendAmount);
            }
            else
            {
                targetRenderer.material.color = originalTargetColor; // Grab이 풀리면 원래 색상으로 복원
            }
            Debug.Log($"Target color updated. Grabbed: {isGrabbed}, In correct position: {isInCorrectPosition}, Color: {targetRenderer.material.color}");
        }
    }

    // Inspector에서 확인용
    private void OnValidate()
    {
        if (targetCollider == null)
        {
            Debug.LogWarning("Target Collider is not assigned!");
        }
        if (targetRenderer == null && targetCollider != null)
        {
            targetRenderer = targetCollider.GetComponent<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogWarning("Target Collider does not have a Renderer component. Assign a Renderer manually.");
            }
        }
        colorBlendAmount = Mathf.Clamp01(colorBlendAmount);
    }
}