using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable), typeof(Rigidbody), typeof(BoxCollider))]
public class VRPushButton : MonoBehaviour
{
    [Header("Button Settings")]
    [SerializeField] private float pressDistance = 0.05f; // 버튼이 눌리는 거리 (미터 단위)
    [SerializeField] private float returnSpeed = 5f; // 버튼이 원래 위치로 돌아오는 속도
    [SerializeField] private Transform triggerTransform; // Trigger 오브젝트의 Transform

    [Header("Events")]
    public UnityEvent onButtonPressed; // 버튼이 Trigger에 닿았을 때 호출될 이벤트

    private Vector3 initialPosition; // 버튼의 초기 위치
    private bool isPressed = false; // 버튼이 눌린 상태인지 여부
    private bool isTriggerActivated = false; // Trigger 이벤트가 이미 호출되었는지 여부
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Rigidbody rb;
    private BoxCollider buttonCollider;
    private BoxCollider triggerCollider;

    private void Awake()
    {
        SetupComponents();
        SetupEvents();
        initialPosition = transform.localPosition; // 초기 위치 저장
    }

    // 필요한 컴포넌트 자동 추가 및 설정
    private void SetupComponents()
    {
        // XRSimpleInteractable 컴포넌트 확인 및 추가
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }

        // Rigidbody 컴포넌트 확인 및 추가
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true; // 물리적 충돌 없이 스크립트로 위치 제어
        rb.useGravity = false;

        // BoxCollider 컴포넌트 확인 및 추가
        buttonCollider = GetComponent<BoxCollider>();
        if (buttonCollider == null)
        {
            buttonCollider = gameObject.AddComponent<BoxCollider>();
        }

        // Trigger 오브젝트 설정
        if (triggerTransform != null)
        {
            triggerCollider = triggerTransform.GetComponent<BoxCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = triggerTransform.gameObject.AddComponent<BoxCollider>();
            }
            triggerCollider.isTrigger = true; // Trigger로 설정
        }
        else
        {
            Debug.LogWarning("Trigger Transform is not assigned in the Inspector!");
        }
    }

    // XRSimpleInteractable 이벤트 설정
    private void SetupEvents()
    {
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    // 손이 버튼에 닿았을 때 (호버 시작)
    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        isPressed = true;
    }

    // 손이 버튼에서 떨어졌을 때 (호버 종료)
    private void OnHoverExit(HoverExitEventArgs args)
    {
        isPressed = false;
        isTriggerActivated = false; // 버튼이 눌림 상태에서 벗어나면 Trigger 이벤트 플래그 리셋
    }

    private void Update()
    {
        // 버튼 위치 업데이트
        if (isPressed)
        {
            // 버튼을 아래로 이동 (pressDistance만큼)
            Vector3 targetPosition = initialPosition + Vector3.down * pressDistance;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * returnSpeed);

            // Trigger와의 충돌 확인
            CheckTriggerCollision();
        }
        else
        {
            // 원래 위치로 돌아가기
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition, Time.deltaTime * returnSpeed);
        }
    }

    // Trigger와의 충돌 확인
    private void CheckTriggerCollision()
    {
        if (triggerCollider == null) return;

        // 버튼의 BoxCollider와 Trigger의 BoxCollider가 겹치는지 확인
        if (buttonCollider.bounds.Intersects(triggerCollider.bounds))
        {
            // Trigger 이벤트가 아직 호출되지 않았을 경우에만 호출
            if (!isTriggerActivated)
            {
                onButtonPressed?.Invoke(); // 이벤트 호출
                isTriggerActivated = true; // 플래그 설정
            }
        }
    }

    // Inspector에서 컴포넌트가 추가될 때 호출
    private void Reset()
    {
        SetupComponents();

        // Trigger Transform 자동 할당 시도
        if (triggerTransform == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                Transform trigger = parent.Find("Trigger");
                if (trigger != null)
                {
                    triggerTransform = trigger;
                }
            }
        }
    }
}