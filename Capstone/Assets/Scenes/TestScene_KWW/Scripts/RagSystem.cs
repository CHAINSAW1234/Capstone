using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가
using UnityEngine.UI; // 필요 시 UI 사용

public class RagSystem : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable; // XRGrabInteractable 컴포넌트
    private Renderer cubeRenderer; // 큐브의 렌더러 (색상 변경용)
    private bool isWet = false; // 헝겊이 젖었는지 상태
    private bool isProtecting = false; // 호흡 보호 상태
    public Transform headTransform; // 머리 위치 (XR Rig의 Main Camera 참조)
    public float protectionDistance = 0.2f; // 머리와의 감지 거리
    public TMP_Text subtitleText; // TextMeshProUGUI로 변경

    void Start()
    {
        // 컴포넌트 초기화
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        cubeRenderer = GetComponent<Renderer>();

        // Material 인스턴스화 (색상 변경 문제 해결)
        cubeRenderer.material = new Material(cubeRenderer.material);

        // 초기 색상 설정 (빨간색)
        cubeRenderer.material.color = Color.red;

        // XR Rig의 머리 위치 참조
        if (headTransform == null)
        {
            headTransform = GameObject.Find("Main Camera").transform; // XR Rig의 카메라
        }

        // UI 텍스트 초기화
        if (subtitleText != null)
        {
            subtitleText.text = "Rag Test Started";
        }
    }

    void Update()
    {
        // 거리 계산
        float distanceToHead = Vector3.Distance(transform.position, headTransform.position);

        // 호흡 보호 상태 체크
        if (isWet)
        {
            if (!isProtecting && distanceToHead <= protectionDistance)
            {
                // 머리와 가까우면 호흡 보호 활성화
                SetProtectionState(true);
            }
            else if (isProtecting && distanceToHead > protectionDistance)
            {
                // 머리에서 멀어지면 호흡 보호 비활성화
                SetProtectionState(false);
            }
        }
    }

    // 물 큐브와 충돌 시 호출
    void OnTriggerEnter(Collider other)
    {
        if (subtitleText != null)
        {
            subtitleText.text = "Collision Detected: " + other.gameObject.name + ", Tag: " + other.tag;
        }
        if (other.CompareTag("Water") && !isWet) // 태그 확인
        {
            WetRag();
        }
    }

    // 헝겊을 젖은 상태로 변경
    void WetRag()
    {
        isWet = true;
        cubeRenderer.material.color = Color.green; // 색상 초록색으로 변경
        if (subtitleText != null)
        {
            subtitleText.text = "Rag is now wet. Color changed to " + cubeRenderer.material.color;
        }
    }

    // 호흡 보호 상태 설정
    void SetProtectionState(bool state)
    {
        isProtecting = state;
        if (subtitleText != null)
        {
            if (isProtecting)
            {
                subtitleText.text = "Breathing Protection Activated";
            }
            else
            {
                subtitleText.text = "Breathing Protection Deactivated";
            }
        }
    }
}