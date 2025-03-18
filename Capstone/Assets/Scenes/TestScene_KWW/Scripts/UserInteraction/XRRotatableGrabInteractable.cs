using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class XRRotatableGrabInteractable : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    [Header("Rotation Settings")]
    [SerializeField]
    private float rotationSpeed = 90f; // 초당 회전 속도 (도 단위)

    // 입력 액션에 대한 참조 (오른손 엄지스틱만 사용)
    private InputAction rightThumbstickAction;
    private Vector2 thumbstickValue;

    protected override void Awake()
    {
        base.Awake();

        // 오른손 엄지스틱 입력 설정 (왼손 입력 제거)
        rightThumbstickAction = new InputAction("RightThumbstick", InputActionType.Value, "<XRController>{RightHand}/thumbstick");
        rightThumbstickAction.performed += ctx => thumbstickValue = ctx.ReadValue<Vector2>();
        rightThumbstickAction.canceled += ctx => thumbstickValue = Vector2.zero;
        rightThumbstickAction.Enable();
    }

    protected override void OnDestroy()
    {
        rightThumbstickAction.Disable();
        rightThumbstickAction.Dispose();
        base.OnDestroy();
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        // 물체가 잡힌 상태에서 Dynamic 업데이트 단계에서 회전 처리
        if (isSelected && updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            RotateObject(Time.deltaTime);
        }
    }

    private void RotateObject(float deltaTime)
    {
        // 엄지스틱 입력이 없으면 회전하지 않음
        if (thumbstickValue == Vector2.zero)
            return;

        // 데드존 추가로 작은 입력 무시
        const float deadZone = 0.1f;
        if (thumbstickValue.magnitude < deadZone)
            return;

        // 엄지스틱 입력에 따라 동적으로 회전 계산
        float horizontalInput = thumbstickValue.x; // 좌우 입력
        float verticalInput = thumbstickValue.y;   // 상하 입력

        // 입력 방향에 따라 로컬 Y축(좌우)과 X축(상하) 회전 적용
        float horizontalRotation = horizontalInput * rotationSpeed * deltaTime; // Y축 회전 (좌우)
        float verticalRotation = -verticalInput * rotationSpeed * deltaTime;    // X축 회전 (상하, 자연스러운 방향 반전)

        // 로컬 좌표계 기준으로 회전 적용
        Quaternion currentRotation = transform.rotation;
        Quaternion horizontalRot = Quaternion.AngleAxis(horizontalRotation, transform.right); // X축 기준으로 조정
        Quaternion verticalRot = Quaternion.AngleAxis(verticalRotation, transform.up);        // Y축 기준으로 조정
        Quaternion newRotation = verticalRot * horizontalRot * currentRotation;

        transform.rotation = newRotation;

        // 타겟 포즈 업데이트로 인터랙터와 동기화 유지
        SetTargetPose(new Pose(transform.position, newRotation));
    }

    // Inspector에서 설정 가능하도록 속성 추가
    public float RotationSpeed
    {
        get => rotationSpeed;
        set => rotationSpeed = value;
    }

    private void DisableDefaultRotation()
    {
        // 회전과 스케일 추적만 비활성화 (위치 추적은 유지)
        trackRotation = false;
        trackScale = false;
        trackPosition = true;
        // MovementType은 기본값 유지
    }

    private void RestoreDefaultSettings()
    {
        // 원래 설정으로 복원
        trackRotation = true;
        trackScale = true;
        trackPosition = true;
    }

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        base.OnSelectEntering(args);
        DisableDefaultRotation();
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        base.OnSelectExiting(args);
        RestoreDefaultSettings();
    }
}