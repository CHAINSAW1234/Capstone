using UnityEngine;
using UnityEngine.InputSystem; // Input System 사용
using UnityEngine.XR.Interaction.Toolkit; // XR Interaction Toolkit 사용

public class CrouchController : MonoBehaviour
{
    [Header("XR Rig 설정")]
    public GameObject xrRig; // XR Rig의 루트 오브젝트 (Inspector에서 할당)
    public float crouchHeight = 0.5f; // 앉았을 때 낮아지는 높이
    public float crouchSpeed = 5f; // 앉기/일어서기 속도

    [Header("입력 설정")]
    public InputActionReference crouchAction; // 왼쪽 컨트롤러의 X 버튼 액션 (Inspector에서 할당)

    private Vector3 initialPosition; // XR Rig의 초기 위치
    private Vector3 targetPosition; // 목표 위치 (앉기/일어서기)
    private bool isCrouching = false; // 현재 앉아 있는지 여부
    private bool isMoving = false; // 이동 중인지 여부

    private void Awake()
    {
        // Input Action 활성화
        if (crouchAction != null)
        {
            crouchAction.action.Enable();
            crouchAction.action.performed += OnCrouchInput; // X 키 입력 이벤트 연결
        }
        else
        {
            Debug.LogError("Crouch Action이 지정되지 않았습니다! Inspector에서 Input Action Reference를 할당해주세요.");
        }
    }

    private void Start()
    {
        // XR Rig 초기화
        if (xrRig == null)
        {
            Debug.LogError("XR Rig 오브젝트가 지정되지 않았습니다! Inspector에서 XR Rig를 할당해주세요.");
            return;
        }

        // 초기 위치 저장
        initialPosition = xrRig.transform.position;
        targetPosition = initialPosition; // 시작 시 초기 위치로 설정
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (crouchAction != null)
        {
            crouchAction.action.performed -= OnCrouchInput;
            crouchAction.action.Disable();
        }
    }

    private void Update()
    {
        // XR Rig을 목표 위치로 부드럽게 이동
        if (isMoving)
        {
            xrRig.transform.position = Vector3.Lerp(xrRig.transform.position, targetPosition, Time.deltaTime * crouchSpeed);

            // 목표 위치에 거의 도달했는지 확인
            if (Vector3.Distance(xrRig.transform.position, targetPosition) < 0.01f)
            {
                xrRig.transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    // X 키 입력 처리
    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        // 앉기/일어서기 토글
        isCrouching = !isCrouching;

        if (isCrouching)
        {
            // 앉기: XR Rig을 아래로 이동
            targetPosition = initialPosition - new Vector3(0, crouchHeight, 0);
            Debug.Log("앉기 동작 시작");
        }
        else
        {
            // 일어서기: XR Rig을 원래 위치로 복귀
            targetPosition = initialPosition;
            Debug.Log("일어서기 동작 시작");
        }

        isMoving = true; // 이동 시작
    }
}